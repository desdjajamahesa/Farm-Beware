using UnityEngine;

namespace FeaturesInteraction
{
    /// <summary>
    /// Penanda objek interaktif yang bisa di-highlight saat di-hover.
    /// Murni visual: menyimpan material asli renderer dan menukar dengan material
    /// highlight (emissive) pada SetHighlight(true/false). Tidak menyentuh logika gameplay.
    /// </summary>
    public class Highlightable : MonoBehaviour
    {
        [Tooltip("Material highlight (emissive) yang dipakai saat objek di-hover.")]
        [SerializeField] private Material highlightMaterial;

        private Renderer[] cachedRenderers;
        private Material[][] originalMaterials;

        public void SetHighlightMaterial(Material mat)
        {
            highlightMaterial = mat;
        }

        public void SetHighlight(bool on)
        {
            if (highlightMaterial == null)
                return;

            CacheRenderers();

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer r = cachedRenderers[i];
                if (r == null)
                    continue;

                if (on)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < mats.Length; m++)
                        mats[m] = highlightMaterial;
                    r.sharedMaterials = mats;
                }
                else
                {
                    if (originalMaterials != null && i < originalMaterials.Length)
                    {
                        Material[] mats = new Material[originalMaterials[i].Length];
                        for (int m = 0; m < mats.Length; m++)
                            mats[m] = originalMaterials[i][m];
                        r.sharedMaterials = mats;
                    }
                }
            }
        }

        private void CacheRenderers()
        {
            if (cachedRenderers != null && cachedRenderers.Length > 0)
                return;

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[cachedRenderers.Length][];
            for (int i = 0; i < cachedRenderers.Length; i++)
                originalMaterials[i] = cachedRenderers[i].sharedMaterials;
        }
    }
}