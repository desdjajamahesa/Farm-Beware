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
            if (meshRenderer == null || transparentMaterial == null) return;

            float targetAlpha = isOccluding ? transparentAlpha : 1f;
            
            // Smooth fade using MoveTowards for consistent speed
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            
            // Apply alpha to material
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

        // Reset to original state (call on scene unload or disable)
        private void OnDisable()
        {
            if (meshRenderer != null && originalMaterial != null) {
                meshRenderer.sharedMaterial = originalMaterial;
            }
        }

        private void OnValidate()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
        }
    }
}