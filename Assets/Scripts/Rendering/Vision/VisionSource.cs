using UnityEngine;

namespace FeaturesRendering.Vision
{
    /// <summary>
    /// Represents a primary vision source (e.g. Player character).
    /// Updates global shader properties for the WorldSpaceVisionMask post-process shader
    /// and registers itself with the VisionManager.
    /// </summary>
    [DisallowMultipleComponent]
    [SelectionBase]
    public class VisionSource : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Vision Bounds")]
        [Tooltip("Outer radius of character vision in world units.")]
        [Min(0.1f)]
        [SerializeField] private float radius = 6.5f;

        [Tooltip("Transition smoothness (falloff width) from full vision to darkness.")]
        [Min(0.01f)]
        [SerializeField] private float smoothness = 3.0f;

        [Header("Post-Process Visuals")]
        [Tooltip("Darkness multiplier for areas outside the vision radius (0 = completely black, 1 = normal brightness).")]
        [Range(0f, 1f)]
        [SerializeField] private float darknessMultiplier = 0.38f;

        [Tooltip("Color saturation for areas outside the vision radius (0 = pure grayscale, 1 = full color).")]
        [Range(0f, 1f)]
        [SerializeField] private float saturationMultiplier = 0.35f;

        [Tooltip("Ambient moonlight color tint applied to the darkened area outside vision.")]
        [SerializeField] private Color nightTint = new Color(0.60f, 0.70f, 0.85f, 1.0f);

        [Tooltip("Brightness multiplier boost in the immediate center around character during night.")]
        [Range(0f, 1f)]
        [SerializeField] private float centerBoost = 0.0f;

        [Header("Anchor & Offset")]
        [Tooltip("Optional local offset from the Transform pivot.")]
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        [Tooltip("If true, this source automatically becomes the active primary vision source in VisionManager.")]
        [SerializeField] private bool isPrimarySource = true;
        #endregion

        #region Properties
        public float Radius => radius;
        public float Smoothness => smoothness;
        public float DarknessMultiplier => darknessMultiplier;
        public float SaturationMultiplier => saturationMultiplier;
        public Color NightTint => nightTint;
        public float CenterBoost => centerBoost;
        public Vector3 WorldPosition => transform.position + localOffset;
        public bool IsPrimarySource => isPrimarySource;
        #endregion

        #region Cached Shader Property IDs
        // Avoid string lookups in runtime loop
        private static readonly int VisionWorldPosId = Shader.PropertyToID("_VisionWorldPos");
        private static readonly int VisionRadiusId = Shader.PropertyToID("_VisionRadius");
        private static readonly int VisionSmoothnessId = Shader.PropertyToID("_VisionSmoothness");
        private static readonly int VisionDarknessId = Shader.PropertyToID("_VisionDarkness");
        private static readonly int VisionSaturationId = Shader.PropertyToID("_VisionSaturation");
        private static readonly int VisionBlendId = Shader.PropertyToID("_VisionBlend");
        private static readonly int VisionNightColorId = Shader.PropertyToID("_VisionNightColor");
        private static readonly int VisionCenterBoostId = Shader.PropertyToID("_VisionCenterBoost");
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (isPrimarySource && VisionManager.Instance != null)
            {
                VisionManager.Instance.RegisterSource(this);
            }
            UpdateShaderGlobals();
        }

        private void OnDisable()
        {
            if (VisionManager.Instance != null)
            {
                VisionManager.Instance.UnregisterSource(this);
            }
        }

        private void LateUpdate()
        {
            if (isPrimarySource)
            {
                UpdateShaderGlobals();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Updates the global shader constants used by WorldSpaceVisionMask.shader
        /// and controls real-time lantern light intensity.
        /// </summary>
        public void UpdateShaderGlobals(float blendWeight = -1f)
        {
            if (blendWeight < 0f)
            {
                blendWeight = VisionManager.Instance != null ? VisionManager.Instance.VisionWeight : 1.0f;
            }

            Vector3 pos = WorldPosition;
            Shader.SetGlobalVector(VisionWorldPosId, new Vector4(pos.x, pos.y, pos.z, 1.0f));
            Shader.SetGlobalFloat(VisionRadiusId, radius);
            Shader.SetGlobalFloat(VisionSmoothnessId, smoothness);
            Shader.SetGlobalFloat(VisionDarknessId, darknessMultiplier);
            Shader.SetGlobalFloat(VisionSaturationId, saturationMultiplier);
            Shader.SetGlobalFloat(VisionBlendId, blendWeight);
            Shader.SetGlobalColor(VisionNightColorId, nightTint);
            Shader.SetGlobalFloat(VisionCenterBoostId, centerBoost);
        }

        public void SetRadius(float newRadius, float newSmoothness = -1f)
        {
            radius = Mathf.Max(0.1f, newRadius);
            if (newSmoothness >= 0f)
            {
                smoothness = Mathf.Max(0.01f, newSmoothness);
            }
            if (isPrimarySource)
            {
                UpdateShaderGlobals();
            }
        }
        #endregion

        #region Editor Gizmos
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = WorldPosition;

            // Outer radius disc (where vision reaches total darkness)
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.9f);
            DrawWireCircleXZ(center, radius);

            // Inner radius disc (where falloff starts)
            float innerRadius = Mathf.Max(0f, radius - smoothness);
            Gizmos.color = new Color(1.0f, 0.9f, 0.2f, 0.7f);
            DrawWireCircleXZ(center, innerRadius);
        }

        private void DrawWireCircleXZ(Vector3 center, float circleRadius, int segments = 48)
        {
            if (circleRadius <= 0.001f) return;

            float step = (2.0f * Mathf.PI) / segments;
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0) * circleRadius, 0f, Mathf.Sin(0) * circleRadius);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * circleRadius, 0f, Mathf.Sin(angle) * circleRadius);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
#endif
        #endregion
    }
}
