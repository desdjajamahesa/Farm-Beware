using System;
using System.Collections;
using UnityEngine;

namespace FeaturesCombat
{
    /// <summary>
    /// Hades-style stylized danger telegraph ground projector.
    /// Projects an expanding circular timing ring and danger perimeter onto the ground,
    /// driving instanced shader properties (_Progress, _OuterRingColor, _FillColor) via MaterialPropertyBlock
    /// with zero runtime GC heap allocations and zero runtime material duplication.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CombatTelegraphDecal : MonoBehaviour
    {
        private static readonly int OuterRingColorPropertyId = Shader.PropertyToID("_OuterRingColor");
        private static readonly int FillColorPropertyId = Shader.PropertyToID("_FillColor");
        private static readonly int ProgressPropertyId = Shader.PropertyToID("_Progress");
        private static readonly int RingThicknessPropertyId = Shader.PropertyToID("_RingThickness");

        private static Mesh _sharedQuadMesh;
        private static Material _sharedTelegraphMaterial;

        [Header("Telegraph Visuals")]
        [Tooltip("Shared telegraph material using FarmBeware/Combat/CombatTelegraph shader.")]
        [SerializeField] private Material telegraphMaterial;

        [Tooltip("Perimeter danger ring HDR color.")]
        [SerializeField] private Color outerRingColor = new Color(1.8f, 0.22f, 0.1f, 0.95f);

        [Tooltip("Expanding inner charge fill color.")]
        [SerializeField] private Color fillColor = new Color(1.0f, 0.18f, 0.05f, 0.35f);

        [Tooltip("Ring border thickness relative to radius.")]
        [Range(0.02f, 0.2f)]
        [SerializeField] private float ringThickness = 0.06f;

        [Tooltip("Elevation offset above ground plane to prevent z-fighting.")]
        [SerializeField] private float groundOffset = 0.04f;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _chargeCoroutine;
        private float _currentRadius = 1.5f;

        private void Awake()
        {
            EnsureComponents();
        }

        private static void EnsureStaticResources()
        {
            if (_sharedQuadMesh == null)
            {
                // Generate a flat horizontal quad on the XZ plane with normals facing up (+Y)
                _sharedQuadMesh = new Mesh
                {
                    name = "CombatTelegraphQuad",
                    vertices = new Vector3[]
                    {
                        new Vector3(-0.5f, 0f, -0.5f),
                        new Vector3( 0.5f, 0f, -0.5f),
                        new Vector3(-0.5f, 0f,  0.5f),
                        new Vector3( 0.5f, 0f,  0.5f)
                    },
                    uv = new Vector2[]
                    {
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(0f, 1f),
                        new Vector2(1f, 1f)
                    },
                    normals = new Vector3[]
                    {
                        Vector3.up,
                        Vector3.up,
                        Vector3.up,
                        Vector3.up
                    },
                    triangles = new int[] { 0, 2, 1, 2, 3, 1 }
                };
                _sharedQuadMesh.RecalculateBounds();
            }

#if UNITY_EDITOR
            if (_sharedTelegraphMaterial == null)
            {
                _sharedTelegraphMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Art/Materials/Combat/Mat_CombatTelegraph.mat");
            }
#endif
        }

        private void EnsureComponents()
        {
            EnsureStaticResources();

            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }
            if (_meshFilter != null && _meshFilter.sharedMesh == null)
            {
                _meshFilter.sharedMesh = _sharedQuadMesh;
            }

            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }

            if (_meshRenderer != null)
            {
                if (telegraphMaterial != null)
                {
                    _meshRenderer.sharedMaterial = telegraphMaterial;
                }
                else if (_sharedTelegraphMaterial != null)
                {
                    _meshRenderer.sharedMaterial = _sharedTelegraphMaterial;
                }

                _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _meshRenderer.receiveShadows = false;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        /// <summary>
        /// Initializes geometry, radius, and local placement of the ground decal.
        /// </summary>
        public void Initialize(float radius)
        {
            EnsureComponents();
            SetRadius(radius);
            transform.localPosition = new Vector3(0f, groundOffset, 0f);
            transform.localRotation = Quaternion.identity;
            SetProgress(0f);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the horizontal planar radius of the projected telegraph ring.
        /// </summary>
        public void SetRadius(float radius)
        {
            _currentRadius = Mathf.Max(0.2f, radius);
            // Quad unit mesh is 1x1 (-0.5 to +0.5). To cover radius R, diameter is 2*R.
            transform.localScale = new Vector3(_currentRadius * 2f, 1f, _currentRadius * 2f);
        }

        /// <summary>
        /// Updates instanced colors for customizable threat telegraphs (e.g. boss purple, sniper yellow).
        /// </summary>
        public void SetColors(Color outerColor, Color innerFillColor)
        {
            outerRingColor = outerColor;
            fillColor = innerFillColor;

            if (_propertyBlock != null && _meshRenderer != null)
            {
                _propertyBlock.SetColor(OuterRingColorPropertyId, outerRingColor);
                _propertyBlock.SetColor(FillColorPropertyId, fillColor);
                _meshRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// Directly sets telegraph fill progress (0.0 to 1.0) with zero runtime GC allocations.
        /// </summary>
        public void SetProgress(float progress)
        {
            if (_meshRenderer == null || _propertyBlock == null) return;

            float clamped = Mathf.Clamp01(progress);
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(ProgressPropertyId, clamped);
            _propertyBlock.SetColor(OuterRingColorPropertyId, outerRingColor);
            _propertyBlock.SetColor(FillColorPropertyId, fillColor);
            _propertyBlock.SetFloat(RingThicknessPropertyId, ringThickness);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Initiates a smooth charge transition from 0% to 100% over the specified duration.
        /// </summary>
        public void StartCharge(float duration, float radius = -1f, Action onComplete = null)
        {
            EnsureComponents();

            if (radius > 0f)
            {
                SetRadius(radius);
            }

            if (_chargeCoroutine != null)
            {
                StopCoroutine(_chargeCoroutine);
            }

            gameObject.SetActive(true);
            if (_meshRenderer != null) _meshRenderer.enabled = true;

            _chargeCoroutine = StartCoroutine(RoutineCharge(Mathf.Max(0.05f, duration), onComplete));
        }

        private IEnumerator RoutineCharge(float duration, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetProgress(progress);
                yield return null;
            }

            SetProgress(1.0f);
            onComplete?.Invoke();
            _chargeCoroutine = null;
        }

        /// <summary>
        /// Instantly hides the telegraph and stops active charge routines.
        /// </summary>
        public void Hide()
        {
            if (_chargeCoroutine != null)
            {
                StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }

            SetProgress(0f);
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = false;
            }
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (_chargeCoroutine != null)
            {
                StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }
        }
    }
}
