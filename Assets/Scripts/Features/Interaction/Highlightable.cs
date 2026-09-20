using UnityEngine;

namespace FeaturesInteraction
{
    /// <summary>
    /// Penanda objek interaktif yang bisa di-highlight saat di-hover.
    /// Murni visual: menyimpan material asli renderer dan menukar dengan material
    /// highlight (emissive) pada SetHighlight(true/false). Tidak menyentuh logika gameplay.
    /// Jika highlightMaterial belum di-set, otomatis mencari "Mat_Highlight" dari Resources
    /// atau scene agar tidak ada objek interaktif yang gagal berpendar.
    /// </summary>
    public class Highlightable : MonoBehaviour
    {
        [Tooltip("Material highlight (emissive) yang dipakai saat objek di-hover.")]
        [SerializeField] private Material highlightMaterial;

        private Renderer[] cachedRenderers;
        private Material[][] originalMaterials;

        // Shared fallback material agar tidak dimuat berulang kali per instance
        private static Material sharedFallbackMaterial;

        public void SetHighlightMaterial(Material mat)
        {
            highlightMaterial = mat;
        }

        public void SetHighlight(bool on)
        {
            EnsureHighlightMaterial();

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

        /// <summary>
        /// Jika highlightMaterial belum di-assign, coba cari fallback material dari scene atau buat sendiri.
        /// </summary>
        private void EnsureHighlightMaterial()
        {
            if (highlightMaterial != null)
                return;

            // Coba gunakan shared fallback
            if (sharedFallbackMaterial != null)
            {
                highlightMaterial = sharedFallbackMaterial;
                return;
            }

            // Cari dari Highlightable lain di scene yang sudah punya material
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
            // Editor fallback: muat langsung dari path aset
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kitchen/Mat_Highlight.mat");
            if (mat != null)
            {
                sharedFallbackMaterial = mat;
                highlightMaterial = mat;
                return;
            }
#endif

            // Runtime fallback terakhir: buat material emissive sederhana
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

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[cachedRenderers.Length][];
            for (int i = 0; i < cachedRenderers.Length; i++)
                originalMaterials[i] = cachedRenderers[i].sharedMaterials;
        }
    }
}