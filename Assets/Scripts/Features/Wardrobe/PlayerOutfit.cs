using UnityEngine;
using System.Collections.Generic;
using FarmBeware.Logic;

namespace FeaturesWardrobe
{
    public class PlayerOutfit : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Reference to the character model (Player/character) with SkinnedMeshRenderer parts.")]
        [SerializeField] private GameObject characterModel;

        [Header("Runtime State")]
        [Tooltip("Outfit yang sedang dipakai (persisted).")]
        public OutfitData currentOutfit;

        [Tooltip("Koleksi outfit yang sudah di-unlock (cosmetic wardrobe).")]
        public List<OutfitData> unlockedOutfits = new List<OutfitData>();

        // Preview state
        private OutfitData previewOutfit;
        private bool previewingDefault;

        // Cached character renderers
        private SkinnedMeshRenderer[] characterRenderers;

        // Events untuk UI sync
        public event System.Action<OutfitData> OnOutfitChanged;
        public event System.Action<OutfitData> OnPreviewChanged;

        // Properties
        public OutfitData CurrentOutfit => currentOutfit;
        public OutfitData PreviewOutfit => previewOutfit;
        public bool IsPreviewing => previewOutfit != null && previewOutfit != currentOutfit;
        public bool IsPreviewingDefault => previewingDefault;

        private void Awake()
        {
            if (characterModel == null)
                characterModel = GameObject.Find("Player/character"); // fallback

            CacheCharacterRenderers();
        }

        private void CacheCharacterRenderers()
        {
            if (characterModel != null)
                characterRenderers = characterModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        private void OnValidate()
        {
            CacheCharacterRenderers();
        }

        /// <summary>Preview outfit tanpa commit (real-time di mirror).</summary>
        public void TryOn(OutfitData outfit)
        {
            if (outfit == null || !unlockedOutfits.Contains(outfit))
                return;

            previewingDefault = false;
            previewOutfit = outfit;
            ApplyOutfitPreview(previewOutfit);
            OnPreviewChanged?.Invoke(previewOutfit);
        }

        /// <summary>Preview Default: lepas preview, kembali ke currentOutfit atau default (semua varian 0).</summary>
        public void PreviewDefault()
        {
            previewOutfit = null;
            previewingDefault = true;
            ApplyDefaultPreview();
            OnPreviewChanged?.Invoke(currentOutfit);
        }

        /// <summary>Commit preview ke currentOutfit (Save).</summary>
        public void Commit()
        {
            previewOutfit = null;
            previewingDefault = false;

            // Ensure character renderers reflect currentOutfit.
            ApplyOutfit(currentOutfit);
            OnOutfitChanged?.Invoke(currentOutfit);
        }

        /// <summary>Batal preview, kembali ke currentOutfit.</summary>
        public void Revert()
        {
            if (previewOutfit == null && !previewingDefault)
                return;

            previewOutfit = null;
            previewingDefault = false;
            ApplyOutfit(currentOutfit);
            OnPreviewChanged?.Invoke(currentOutfit);
        }

        /// <summary>Apply outfit ke character renderers (toggle parts).</summary>
        public void ApplyOutfit(OutfitData outfit)
        {
            if (outfit == null)
            {
                ApplyDefaultOutfit();
                return;
            }

            if (characterRenderers == null || characterRenderers.Length == 0)
                CacheCharacterRenderers();

            outfit.ApplyToCharacter(characterModel);
        }

        public void ApplyOutfitPreview(OutfitData outfit)
        {
            if (outfit == null)
            {
                ApplyDefaultPreview();
                return;
            }

            if (characterRenderers == null || characterRenderers.Length == 0)
                CacheCharacterRenderers();

            outfit.ApplyToCharacter(characterModel);
        }

        private void ApplyDefaultPreview()
        {
            if (characterRenderers == null || characterRenderers.Length == 0)
                CacheCharacterRenderers();

            // Default = all variant 0
            foreach (var r in characterRenderers)
            {
                if (r.name == "body" || r.name == "hair1" || r.name == "hair2")
                    r.enabled = true;
                else if (r.name == "cloth1" || r.name == "pants1" || r.name == "shoes1_left" || r.name == "shoes1_right")
                    r.enabled = true;
                else
                    r.enabled = false;
            }
        }

        private void ApplyDefaultOutfit()
        {
            if (characterRenderers == null || characterRenderers.Length == 0)
                CacheCharacterRenderers();

            // Same as default preview but persistent
            foreach (var r in characterRenderers)
            {
                if (r.name == "body" || r.name == "hair1" || r.name == "hair2")
                    r.enabled = true;
                else if (r.name == "cloth1" || r.name == "pants1" || r.name == "shoes1_left" || r.name == "shoes1_right")
                    r.enabled = true;
                else
                    r.enabled = false;
            }
        }
    }
}