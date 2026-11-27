using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesInteraction
{
    /// <summary>
    /// Marks an interactive object as highlightable on hover.
    /// Purely visual: caches original renderer materials and swaps in an emissive
    /// highlight material on SetHighlight(true/false). Does not touch gameplay logic.
    /// If highlightMaterial is not assigned, automatically locates "Mat_Highlight" from Resources
    /// or the scene so no interactive object fails to glow.
    /// </summary>
    public class Highlightable : MonoBehaviour
    {
        [Tooltip("Material highlight (emissive) yang dipakai saat objek di-hover.")]
        [SerializeField] private Material highlightMaterial;

        private Renderer[] cachedRenderers;
        private Material[][] originalMaterials;

        // Shared fallback material to avoid loading repeatedly per instance
        private static Material sharedFallbackMaterial;

        public void SetHighlightMaterial(Material mat)
        {
            highlightMaterial = mat;
        }

        private bool isHighlighted;
        public bool IsHighlighted => isHighlighted;

        public void SetHighlight(bool on)
        {
            EnsureHighlightMaterial();

            if (highlightMaterial == null)
                return;

            CacheRenderers();

            // If this object is currently camera-occluded (transparent so the player stays visible),
            // do not overwrite with a solid highlight that would hide the player again.
            var selfOcc = GetComponent<IOccluder>() ?? GetComponentInParent<IOccluder>();
            if (on && selfOcc != null && selfOcc.IsOccluding)
            {
                return;
            }

            isHighlighted = on;

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer r = cachedRenderers[i];
                if (r == null)
                    continue;

                var rOcc = r.GetComponent<IOccluder>() ?? r.GetComponentInParent<IOccluder>();
                if (on)
                {
                    if (rOcc != null && rOcc.IsOccluding)
                        continue;

                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < mats.Length; m++)
                        mats[m] = highlightMaterial;
                    r.sharedMaterials = mats;
                }
                else
                {
                    if (rOcc != null && rOcc.IsOccluding)
                    {
                        // Do not restore to opaque material if the wall/door is transparent due to occlusion
                        continue;
                    }

                    if (originalMaterials != null && i < originalMaterials.Length && originalMaterials[i] != null)
                    {
                        Material[] mats = new Material[originalMaterials[i].Length];
                        for (int m = 0; m < mats.Length; m++)
                            mats[m] = originalMaterials[i][m];
                        r.sharedMaterials = mats;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the cached original material for a specific renderer.
        /// Used when an object's base material changes dynamically (e.g., soil tilled/watered/crop grown)
        /// so that un-highlighting (SetHighlight false) restores the new material instead of the old one.
        /// </summary>
        public void UpdateOriginalMaterial(Renderer targetRenderer, Material newMaterial)
        {
            if (targetRenderer == null || newMaterial == null) return;

            CacheRenderers();

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] == targetRenderer)
                {
                    if (originalMaterials != null && i < originalMaterials.Length)
                    {
                        for (int m = 0; m < originalMaterials[i].Length; m++)
                        {
                            originalMaterials[i][m] = newMaterial;
                        }
                    }

                    // If NOT currently highlighted, apply the new material immediately
                    if (!isHighlighted)
                    {
                        targetRenderer.sharedMaterial = newMaterial;
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// If highlightMaterial is unassigned, attempts to find a fallback material in the scene or create one.
        /// </summary>
        private void EnsureHighlightMaterial()
        {
            if (highlightMaterial != null)
                return;

            // Try using the shared fallback
            if (sharedFallbackMaterial != null)
            {
                highlightMaterial = sharedFallbackMaterial;
                return;
            }

            // Search other Highlightable instances in the scene that already have a material
            var allHighlightables = FindObjectsByType<Highlightable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var h in allHighlightables)
            {
                if (h != this && h.highlightMaterial != null)
                {
                    sharedFallbackMaterial = h.highlightMaterial;
                    highlightMaterial = sharedFallbackMaterial;
                    return;
                }
            }

#if UNITY_EDITOR
            // Editor fallback: load directly from asset path
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Kitchen/Mat_Highlight.mat")
                   ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kitchen/Mat_Highlight.mat");
            if (mat != null)
            {
                sharedFallbackMaterial = mat;
                highlightMaterial = mat;
                return;
            }
#endif

            // Final runtime fallback: create a simple emissive material
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var fallback = new Material(shader)
                {
                    name = "Mat_Highlight_Fallback",
                    color = new Color(1f, 0.95f, 0.5f, 1f)
                };
                fallback.EnableKeyword("_EMISSION");
                fallback.SetColor("_EmissionColor", new Color(6f, 5.4f, 1.5f, 3f));
                sharedFallbackMaterial = fallback;
                highlightMaterial = fallback;
            }
        }

        private void CacheRenderers()
        {
            if (cachedRenderers != null && cachedRenderers.Length > 0)
                return;

            var allRends = GetComponentsInChildren<Renderer>(true);
            var list = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < allRends.Length; i++)
            {
                // Do not highlight mirror surfaces or mirror cameras
                if (allRends[i].name.ToLower().Contains("mirror") || allRends[i].GetComponent("MirrorCamera") != null)
                    continue;
                list.Add(allRends[i]);
            }
            cachedRenderers = list.ToArray();
            originalMaterials = new Material[cachedRenderers.Length][];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                var occ = cachedRenderers[i].GetComponent<IOccluder>() ?? cachedRenderers[i].GetComponentInParent<IOccluder>();
                if (occ != null && occ.OriginalMaterials != null && occ.OriginalMaterials.Length > 0)
                {
                    originalMaterials[i] = occ.OriginalMaterials;
                }
                else
                {
                    originalMaterials[i] = cachedRenderers[i].sharedMaterials;
                }
            }
        }
    }
}