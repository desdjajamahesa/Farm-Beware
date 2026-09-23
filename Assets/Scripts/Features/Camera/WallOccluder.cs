using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCamera
{
    /// <summary>
    /// Per-wall occlusion transparency controller.
    /// Handles fade in/out when wall occludes camera view of player.
    /// </summary>
    public class WallOccluder : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Target alpha when wall is occluding (0 = invisible, 1 = opaque)")]
        [Range(0f, 1f)]
        public float transparentAlpha = 0.15f;

        [Tooltip("Fade speed (higher = faster)")]
        public float fadeSpeed = 8f;

        [Header("References (auto-assigned if empty)")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material originalMaterial;
        [SerializeField] private Material transparentMaterial;

        [Header("Grouping")]
        [Tooltip("The wall group this segment belongs to. When occluded, all segments in this group fade together.")]
        [SerializeField] private WallOcclusionGroup group;
        public WallOcclusionGroup Group
        {
            get
            {
                if (group == null && transform.parent != null)
                {
                    group = transform.parent.GetComponent<WallOcclusionGroup>();
                }
                return group;
            }
            set => group = value;
        }

        // Multi-material support
        private Material[] originalMaterials;
        private Material[] transparentMaterials;
        private bool isUsingTransparentMaterials = false;

        // Runtime
        private float currentAlpha = 1f;
        private bool isOccluding = false;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        [Header("Linked Renderers (for attached objects like mirrors)")]
        [SerializeField] private List<Renderer> additionalRenderers = new List<Renderer>();

        private List<Material[]> additionalOriginalMaterialsList = new List<Material[]>();
        private List<Material[]> additionalTransparentMaterialsList = new List<Material[]>();
        private List<bool> additionalUsingTransparentList = new List<bool>();

        private void Initialize()
        {
            if (isInitialized) return;

            // Auto-get MeshRenderer
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            // Store original materials
            if (meshRenderer != null)
            {
                originalMaterials = meshRenderer.sharedMaterials;
                if (originalMaterial == null && originalMaterials != null && originalMaterials.Length > 0)
                    originalMaterial = originalMaterials[0];
            }

            // Create transparent material instance
            CreateTransparentMaterial();

            currentAlpha = 1f;
            isInitialized = true;
        }

        private void SetupTransparentMaterialProperties(Material mat)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        private void CreateTransparentMaterial()
        {
            // Load base transparent template from assets
            var baseTransparentMat = Resources.Load<Material>("Materials/Walls/Mat_Wall_Transparent");
            if (baseTransparentMat == null) {
#if UNITY_EDITOR
                var guids = UnityEditor.AssetDatabase.FindAssets("Mat_Wall_Transparent t:Material");
                if (guids.Length > 0) {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    baseTransparentMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                }
#endif
            }

            // Create transparent variants for all submeshes on meshRenderer
            if (originalMaterials != null && originalMaterials.Length > 0)
            {
                transparentMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    var orig = originalMaterials[i];
                    if (orig == null) continue;

                    Material tMat = null;
                    if (baseTransparentMat != null)
                    {
                        tMat = new Material(baseTransparentMat);
                        if (orig.HasProperty("_BaseMap") && orig.GetTexture("_BaseMap") != null)
                            tMat.SetTexture("_BaseMap", orig.GetTexture("_BaseMap"));
                        else if (orig.HasProperty("_MainTex") && orig.GetTexture("_MainTex") != null)
                            tMat.SetTexture("_MainTex", orig.GetTexture("_MainTex"));

                        if (orig.HasProperty("_BaseColor"))
                            tMat.SetColor("_BaseColor", orig.GetColor("_BaseColor"));
                        else if (orig.HasProperty("_Color"))
                            tMat.SetColor("_Color", orig.GetColor("_Color"));
                    }
                    else
                    {
                        tMat = new Material(orig);
                    }

                    SetupTransparentMaterialProperties(tMat);
                    tMat.name = orig.name + "_Transparent_Instance";
                    transparentMaterials[i] = tMat;
                }
                if (transparentMaterials.Length > 0)
                    transparentMaterial = transparentMaterials[0];
            }

            // Create transparent materials for additional renderers
            additionalOriginalMaterialsList.Clear();
            additionalTransparentMaterialsList.Clear();
            additionalUsingTransparentList.Clear();
            for (int i = 0; i < additionalRenderers.Count; i++)
            {
                additionalUsingTransparentList.Add(false);
                var rend = additionalRenderers[i];
                if (rend == null)
                {
                    additionalOriginalMaterialsList.Add(null);
                    additionalTransparentMaterialsList.Add(null);
                    continue;
                }

                Material[] origMats = rend.sharedMaterials;
                additionalOriginalMaterialsList.Add(origMats);

                Material[] addTransMats = new Material[origMats.Length];
                for (int m = 0; m < origMats.Length; m++)
                {
                    var orig = origMats[m];
                    if (orig == null) continue;
                    Material tMat = null;
                    if (baseTransparentMat != null)
                    {
                        tMat = new Material(baseTransparentMat);
                        if (orig.HasProperty("_BaseMap") && orig.GetTexture("_BaseMap") != null)
                            tMat.SetTexture("_BaseMap", orig.GetTexture("_BaseMap"));
                        else if (orig.HasProperty("_MainTex") && orig.GetTexture("_MainTex") != null)
                            tMat.SetTexture("_MainTex", orig.GetTexture("_MainTex"));
                        if (orig.HasProperty("_BaseColor"))
                            tMat.SetColor("_BaseColor", orig.GetColor("_BaseColor"));
                        else if (orig.HasProperty("_Color"))
                            tMat.SetColor("_Color", orig.GetColor("_Color"));
                    }
                    else
                    {
                        tMat = new Material(orig);
                    }
                    SetupTransparentMaterialProperties(tMat);
                    tMat.name = orig.name + "_Transparent_Add_" + i + "_" + m;
                    addTransMats[m] = tMat;
                }
                additionalTransparentMaterialsList.Add(addTransMats);
            }
        }

        private void Update()
        {
            if (!isInitialized) Initialize();
            FadeAlpha();
        }

        /// <summary>
        /// Call this to set whether this wall is currently occluding the view
        /// </summary>
        public void SetOccluding(bool occluding)
        {
            if (!isInitialized) Initialize();
            isOccluding = occluding;
        }

        private void FadeAlpha()
        {
            if (!isInitialized) Initialize();

            if (meshRenderer == null && (additionalRenderers == null || additionalRenderers.Count == 0)) return;

            float targetAlpha = isOccluding ? transparentAlpha : 1f;

            // Smooth fade using MoveTowards for consistent speed
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

            // Apply alpha to main renderer materials
            if (meshRenderer != null && transparentMaterials != null && transparentMaterials.Length > 0)
            {
                for (int i = 0; i < transparentMaterials.Length; i++)
                {
                    if (transparentMaterials[i] != null)
                    {
                        if (transparentMaterials[i].HasProperty("_BaseColor"))
                        {
                            Color c = transparentMaterials[i].GetColor("_BaseColor");
                            c.a = currentAlpha;
                            transparentMaterials[i].SetColor("_BaseColor", c);
                        }
                        if (transparentMaterials[i].HasProperty("_Color"))
                        {
                            Color c = transparentMaterials[i].GetColor("_Color");
                            c.a = currentAlpha;
                            transparentMaterials[i].SetColor("_Color", c);
                        }
                    }
                }

                if (currentAlpha < 1f && !isUsingTransparentMaterials)
                {
                    meshRenderer.materials = transparentMaterials;
                    isUsingTransparentMaterials = true;
                }
                else if (currentAlpha >= 1f && isUsingTransparentMaterials)
                {
                    meshRenderer.sharedMaterials = originalMaterials;
                    isUsingTransparentMaterials = false;
                }
            }

            // Apply alpha to additional renderers
            if (additionalRenderers != null)
            {
                for (int i = 0; i < additionalRenderers.Count; i++)
                {
                    var rend = additionalRenderers[i];
                    if (rend == null) continue;

                    var transMats = i < additionalTransparentMaterialsList.Count ? additionalTransparentMaterialsList[i] : null;
                    var origMats = i < additionalOriginalMaterialsList.Count ? additionalOriginalMaterialsList[i] : null;

                    if (transMats == null || origMats == null) continue;

                    for (int m = 0; m < transMats.Length; m++)
                    {
                        if (transMats[m] != null)
                        {
                            if (transMats[m].HasProperty("_BaseColor"))
                            {
                                Color c = transMats[m].GetColor("_BaseColor");
                                c.a = currentAlpha;
                                transMats[m].SetColor("_BaseColor", c);
                            }
                            if (transMats[m].HasProperty("_Color"))
                            {
                                Color c = transMats[m].GetColor("_Color");
                                c.a = currentAlpha;
                                transMats[m].SetColor("_Color", c);
                            }
                        }
                    }

                    bool isTrans = i < additionalUsingTransparentList.Count && additionalUsingTransparentList[i];
                    if (currentAlpha < 1f && !isTrans)
                    {
                        rend.materials = transMats;
                        if (i < additionalUsingTransparentList.Count)
                            additionalUsingTransparentList[i] = true;
                    }
                    else if (currentAlpha >= 1f && isTrans)
                    {
                        rend.sharedMaterials = origMats;
                        if (i < additionalUsingTransparentList.Count)
                            additionalUsingTransparentList[i] = false;
                    }
                }
            }
        }

        // Reset to original state (call on scene unload or disable)
        private void OnDisable()
        {
            if (meshRenderer != null && originalMaterials != null && originalMaterials.Length > 0)
            {
                meshRenderer.sharedMaterials = originalMaterials;
                isUsingTransparentMaterials = false;
            }

            // Restore additional renderers
            if (additionalRenderers != null)
            {
                for (int i = 0; i < additionalRenderers.Count; i++)
                {
                    var rend = additionalRenderers[i];
                    var origMats = i < additionalOriginalMaterialsList.Count ? additionalOriginalMaterialsList[i] : null;
                    if (rend != null && origMats != null && origMats.Length > 0)
                    {
                        rend.sharedMaterials = origMats;
                    }
                    if (i < additionalUsingTransparentList.Count)
                        additionalUsingTransparentList[i] = false;
                }
            }
        }

        private void OnValidate()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
        }
    }
}