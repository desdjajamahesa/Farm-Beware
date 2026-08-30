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

        private List<Material> additionalOriginalMaterials = new List<Material>();
        private List<Material> additionalTransparentMaterials = new List<Material>();
private void Initialize()
        {
            if (isInitialized) return;

            // Auto-get MeshRenderer
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            // Store original material
            if (meshRenderer != null && originalMaterial == null)
                originalMaterial = meshRenderer.sharedMaterial;

            // Create transparent material instance
            CreateTransparentMaterial();

            currentAlpha = 1f;
            isInitialized = true;
        }

        private void CreateTransparentMaterial()
        {
            // Load the transparent wall material from assets
            var transparentMat = Resources.Load<Material>("Materials/Walls/Mat_Wall_Transparent");
            
            // If not in Resources, try to find it in the project
            if (transparentMat == null) {
                var guids = UnityEditor.AssetDatabase.FindAssets("Mat_Wall_Transparent t:Material");
                if (guids.Length > 0) {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    transparentMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }

            // If still not found, create a runtime instance from original
            if (transparentMat == null && meshRenderer != null && meshRenderer.sharedMaterial != null) {
                transparentMat = new Material(meshRenderer.sharedMaterial);
                transparentMat.name = "Mat_Wall_Transparent_Runtime";
            }

            // Configure for transparency
            if (transparentMat != null) {
                transparentMat = new Material(transparentMat); // Create instance
                transparentMat.name = "Mat_Wall_Transparent_Instance";
                
                // Ensure URP transparent settings
                if (transparentMat.HasProperty("_Surface")) transparentMat.SetFloat("_Surface", 1f);
                if (transparentMat.HasProperty("_Blend")) transparentMat.SetFloat("_Blend", 0f);
                if (transparentMat.HasProperty("_SrcBlend")) transparentMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (transparentMat.HasProperty("_DstBlend")) transparentMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (transparentMat.HasProperty("_ZWrite")) transparentMat.SetFloat("_ZWrite", 0f);
                if (transparentMat.HasProperty("_Cull")) transparentMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                transparentMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                transparentMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentMat.renderQueue = 3000;
            }

            this.transparentMaterial = transparentMat;

            // Create transparent materials for additional renderers
            for (int i = 0; i < additionalRenderers.Count; i++) {
                var rend = additionalRenderers[i];
                if (rend == null) {
                    additionalOriginalMaterials.Add(null);
                    additionalTransparentMaterials.Add(null);
                    continue;
                }

                Material origMat = rend.sharedMaterial;
                additionalOriginalMaterials.Add(origMat);

                Material addTransMat = null;
                if (transparentMat != null) {
                    addTransMat = new Material(transparentMat);
                    addTransMat.name = "Mat_Wall_Transparent_Instance_Additional_" + i;
                }
                additionalTransparentMaterials.Add(addTransMat);
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

            // Apply alpha to main material
            if (meshRenderer != null && transparentMaterial != null) {
                Color c = transparentMaterial.color;
                c.a = currentAlpha;
                transparentMaterial.color = c;

                // Ensure we're using the transparent material when fading
                if (currentAlpha < 1f && meshRenderer.sharedMaterial != transparentMaterial) {
                    meshRenderer.material = transparentMaterial;
                }
                else if (currentAlpha >= 1f && meshRenderer.sharedMaterial != originalMaterial) {
                    meshRenderer.sharedMaterial = originalMaterial;
                }
            }

            // Apply alpha to additional renderers
            if (additionalRenderers != null) {
                for (int i = 0; i < additionalRenderers.Count; i++) {
                    var rend = additionalRenderers[i];
                    var transMat = i < additionalTransparentMaterials.Count ? additionalTransparentMaterials[i] : null;
                    var origMat = i < additionalOriginalMaterials.Count ? additionalOriginalMaterials[i] : null;

                    if (rend == null || transMat == null) continue;

                    Color c = transMat.color;
                    c.a = currentAlpha;
                    transMat.color = c;

                    if (currentAlpha < 1f && rend.sharedMaterial != transMat) {
                        rend.material = transMat;
                    }
                    else if (currentAlpha >= 1f && rend.sharedMaterial != origMat) {
                        rend.sharedMaterial = origMat;
                    }
                }
            }
        }

        // Reset to original state (call on scene unload or disable)
        private void OnDisable()
        {
            if (meshRenderer != null && originalMaterial != null) {
                meshRenderer.sharedMaterial = originalMaterial;
            }
        
        // Restore additional renderers
        if (additionalRenderers != null) {
            for (int i = 0; i < additionalRenderers.Count; i++) {
                var rend = additionalRenderers[i];
                var origMat = i < additionalOriginalMaterials.Count ? additionalOriginalMaterials[i] : null;
                if (rend != null && origMat != null) {
                    rend.sharedMaterial = origMat;
                }
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