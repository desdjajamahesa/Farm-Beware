using UnityEngine;
using System.Collections.Generic;

namespace FeaturesWardrobe
{
    public class PlayerOutfit : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Root transform untuk spawn model outfit (child of Player, posisi relatif ke body).")]
        [SerializeField] private Transform outfitRoot;
        
        [Header("Runtime State")]
        [Tooltip("Outfit yang sedang dipakai (persisted).")]
        public OutfitData currentOutfit;
        
        [Tooltip("Koleksi outfit yang sudah di-unlock (cosmetic wardrobe).")]
        public List<OutfitData> unlockedOutfits = new List<OutfitData>();
        
        // Preview state (tidak di-commit ke currentOutfit sampai Save)
        private OutfitData previewOutfit;
        private GameObject previewModel;
        private GameObject currentModel;
        private bool previewingDefault;
        
        // Events untuk UI sync
        public event System.Action<OutfitData> OnOutfitChanged;      // currentOutfit changed
        public event System.Action<OutfitData> OnPreviewChanged;     // previewOutfit changed
        
        // Properties
        public OutfitData CurrentOutfit => currentOutfit;
        public OutfitData PreviewOutfit => previewOutfit;
        public bool IsPreviewing => previewOutfit != null && previewOutfit != currentOutfit;
        public Transform OutfitRoot => outfitRoot;

        private void Awake()
        {
            if (outfitRoot == null)
            {
                // Auto-create outfitRoot as child if not assigned
                outfitRoot = new GameObject("OutfitRoot").transform;
                outfitRoot.SetParent(transform, false);
                outfitRoot.localPosition = Vector3.zero;
                outfitRoot.localRotation = Quaternion.identity;
            }

            // FIX: TIDAK auto-equip di Awake -> player tetap model aslinya di luar lemari.
            // Outfit hanya diterapkan saat player memilih di wardrobe (TryOn/Commit).
        }

        /// <summary>Preview outfit tanpa commit (real-time di mirror).</summary>
        public void TryOn(OutfitData outfit)
        {
            if (outfit == null || !unlockedOutfits.Contains(outfit))
                return;
            
            previewingDefault = false;
            previewOutfit = outfit;
            RefreshPreview();
            OnPreviewChanged?.Invoke(outfit);
        }

        /// <summary>Preview Default: lepas preview, kembali ke model player asli (tanpa outfit).</summary>
        public void PreviewDefault()
        {
            DestroyPreviewModel();
            previewOutfit = null;
            previewingDefault = true;
            OnPreviewChanged?.Invoke(currentOutfit);
        }

        /// <summary>Commit preview ke currentOutfit (Save).</summary>
        public void Commit()
        {
            if (previewOutfit != null && previewOutfit != currentOutfit)
            {
                currentOutfit = previewOutfit;
                ApplyOutfit(currentOutfit);
                previewOutfit = null;
                previewModel = null;
                previewingDefault = false;
                OnOutfitChanged?.Invoke(currentOutfit);
                return;
            }

            // Pilihan Default di-commit: lepas outfit aktif yang sedang dipakai.
            if (previewingDefault && currentOutfit != null)
            {
                currentOutfit = null;
                DestroyCurrentModel();
                previewingDefault = false;
                OnOutfitChanged?.Invoke(null);
            }
        }

        /// <summary>Batal preview, kembali ke currentOutfit.</summary>
        public void Revert()
        {
            if (previewOutfit == null)
                return;
            
            previewOutfit = null;
            DestroyPreviewModel();
            OnPreviewChanged?.Invoke(currentOutfit);
        }

        /// <summary>Apply outfit permanen (destroy old, spawn new).</summary>
        private void ApplyOutfit(OutfitData outfit)
        {
            if (outfit == null || outfit.fullBodyPrefab == null)
            {
                Debug.LogWarning($"[PlayerOutfit] OutfitData atau fullBodyPrefab null: {outfit?.outfitName}");
                return;
            }

            DestroyCurrentModel();
            
            GameObject spawned = Instantiate(outfit.fullBodyPrefab, outfitRoot);
            spawned.transform.localPosition = Vector3.zero;
            spawned.transform.localRotation = Quaternion.identity;
            
            // Preserve prefab scale (pattern from PlayerEquipment.cs:62)
            spawned.transform.localScale = outfit.fullBodyPrefab.transform.localScale;
            
            currentModel = spawned;
        }

        private void RefreshPreview()
        {
            if (previewOutfit == null || previewOutfit.fullBodyPrefab == null)
            {
                DestroyPreviewModel();
                return;
            }

            DestroyPreviewModel();
            
            GameObject spawned = Instantiate(previewOutfit.fullBodyPrefab, outfitRoot);
            spawned.transform.localPosition = Vector3.zero;
            spawned.transform.localRotation = Quaternion.identity;
            spawned.transform.localScale = previewOutfit.fullBodyPrefab.transform.localScale;
            
            // Visual indicator: slight transparency untuk preview
            var renderers = spawned.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = new Material(mats[i]);
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        mat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 0.9f));
                    }
                    if (mat.HasProperty("_Color"))
                    {
                        var c = mat.GetColor("_Color");
                        mat.SetColor("_Color", new Color(c.r, c.g, c.b, 0.9f));
                    }
                    mats[i] = mat;
                }
                r.materials = mats;
            }
            
            previewModel = spawned;
        }

        private void DestroyCurrentModel()
        {
            if (currentModel != null)
            {
                Destroy(currentModel);
                currentModel = null;
            }
        }

        private void DestroyPreviewModel()
        {
            if (previewModel != null)
            {
                Destroy(previewModel);
                previewModel = null;
            }
        }

        private void OnDestroy()
        {
            DestroyCurrentModel();
            DestroyPreviewModel();
        }
    }
}