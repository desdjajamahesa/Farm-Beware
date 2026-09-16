using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace FeaturesWardrobe
{
    [System.Serializable]
    public class WardrobeSaveData
    {
        public string currentOutfitName;
        public int topVariant;
        public int bottomVariant;
        public int shoesVariant;
        public int hatVariant;
        public bool isHatEquipped = true;
        public List<string> unlockedOutfitNames = new List<string>();
    }

    public class PlayerOutfit : MonoBehaviour
    {
        public static PlayerOutfit Instance { get; private set; }

        [Header("Setup")]
        [SerializeField] private GameObject characterModel;

        [Header("Runtime State")]
        public OutfitData currentOutfit;
        public List<OutfitData> unlockedOutfits = new List<OutfitData>();

        [Header("Hat")]
        public bool isHatEquipped = true;

        // Preview state (legacy path, kept for backward compat with WardrobeUI.OnSaveClicked)
        private OutfitData previewOutfit;
        private bool previewingDefault;

        private SkinnedMeshRenderer[] characterRenderers;

        public event System.Action<OutfitData> OnOutfitChanged;
        public event System.Action<OutfitData> OnPreviewChanged;

        public OutfitData CurrentOutfit => currentOutfit;
        public OutfitData PreviewOutfit => previewOutfit;
        public bool IsPreviewing => previewOutfit != null && previewOutfit != currentOutfit;
        public bool IsPreviewingDefault => previewingDefault;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (characterModel == null)
                characterModel = FindCharacterModel();

            CacheCharacterRenderers();

            // Self-heal: if the user forgot to assign the 12 outfit assets
            // in the Inspector (e.g. after a prefab swap), auto-load them
            // from Resources so the wardrobe grid is never empty.
            AutoPopulateUnlockedOutfits();

            LoadWardrobe();
        }

        private void AutoPopulateUnlockedOutfits()
        {
            if (unlockedOutfits == null) unlockedOutfits = new List<OutfitData>();
            if (unlockedOutfits.Count > 0) return;

            var loaded = Resources.LoadAll<OutfitData>("Player/model");
            if (loaded != null && loaded.Length > 0)
            {
                foreach (var o in loaded)
                {
                    if (o != null && !unlockedOutfits.Contains(o))
                        unlockedOutfits.Add(o);
                }
                Debug.Log($"[PlayerOutfit] Auto-loaded {unlockedOutfits.Count} outfits from Resources/Player/model/.");
            }
            else
            {
                Debug.LogWarning("[PlayerOutfit] No OutfitData assets found in Resources/Player/model/. Wardrobe grid will be empty.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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

        public void TryOn(OutfitData outfit)
        {
            if (outfit == null || !unlockedOutfits.Contains(outfit))
                return;
            previewingDefault = false;
            previewOutfit = outfit;
            outfit.ApplyToCharacter(characterModel);
            OnPreviewChanged?.Invoke(previewOutfit);
        }

        public void PreviewDefault()
        {
            previewOutfit = null;
            previewingDefault = true;
            OnPreviewChanged?.Invoke(currentOutfit);
        }

        public void Commit()
        {
            previewOutfit = null;
            previewingDefault = false;
            ApplyOutfit(currentOutfit);
            OnOutfitChanged?.Invoke(currentOutfit);
        }

        public void Revert()
        {
            if (previewOutfit == null && !previewingDefault) return;
            previewOutfit = null;
            previewingDefault = false;
            ApplyOutfit(currentOutfit);
            OnPreviewChanged?.Invoke(currentOutfit);
        }

        public void ApplyOutfit(OutfitData outfit)
        {
            if (outfit == null) return;
            if (characterModel == null)
            {
                characterModel = FindCharacterModel();
                if (characterModel != null) CacheCharacterRenderers();
            }
            if (characterModel == null) return;
            outfit.ApplyToCharacter(characterModel);
        }

        // Whole-outfit equip: sets currentOutfit, applies to model, fires event, persists isHatEquipped.
        public void EquipOutfit(OutfitData outfit)
        {
            if (outfit == null || !unlockedOutfits.Contains(outfit)) return;
            currentOutfit = outfit;
            ApplyOutfit(currentOutfit);
            OnOutfitChanged?.Invoke(currentOutfit);
        }

        public void ToggleHat()
        {
            isHatEquipped = !isHatEquipped;
            SetHatActive(isHatEquipped);
            SaveWardrobe();
        }

        #region Save/Load

        private string SavePath => Path.Combine(Application.persistentDataPath, "wardrobeSave.json");

        private const string OutfitResourcePath = "Player/model/";

        public void SaveWardrobe()
        {
            var data = new WardrobeSaveData
            {
                currentOutfitName = currentOutfit != null ? currentOutfit.name : "Outfit_Set_A",
                topVariant = currentOutfit != null ? currentOutfit.topVariant : 0,
                bottomVariant = currentOutfit != null ? currentOutfit.bottomVariant : 0,
                shoesVariant = currentOutfit != null ? currentOutfit.shoesVariant : 0,
                hatVariant = currentOutfit != null ? currentOutfit.hatVariant : 0,
                isHatEquipped = isHatEquipped,
                unlockedOutfitNames = new List<string>()
            };
            foreach (var o in unlockedOutfits)
                if (o != null) data.unlockedOutfitNames.Add(o.name);

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }

        public void LoadWardrobe()
        {
            EnsureDefaultOutfitsUnlocked();

            if (!File.Exists(SavePath))
            {
                currentOutfit = unlockedOutfits.Count > 0 ? unlockedOutfits[0] : null;
                isHatEquipped = true;
                ApplyOutfit(currentOutfit);
                ApplyHatState();
                SaveWardrobe();
                return;
            }

            var data = JsonUtility.FromJson<WardrobeSaveData>(File.ReadAllText(SavePath));

            // Resolve unlockedOutfits from saved names (filtered by what still exists in Resources).
            if (data.unlockedOutfitNames != null && data.unlockedOutfitNames.Count > 0)
            {
                foreach (var name in data.unlockedOutfitNames)
                {
                    var o = Resources.Load<OutfitData>(OutfitResourcePath + name);
                    if (o != null && !unlockedOutfits.Contains(o)) unlockedOutfits.Add(o);
                }
            }

            // Migration chain: by-name -> 4-int snapshot -> legacy index -> Set A.
            currentOutfit = ResolveOutfit(data);

            // 4-int snapshot for legacy files where currentOutfitName is empty.
            if (currentOutfit != null && string.IsNullOrEmpty(data.currentOutfitName))
            {
                currentOutfit.topVariant = data.topVariant;
                currentOutfit.bottomVariant = data.bottomVariant;
                currentOutfit.shoesVariant = data.shoesVariant;
                currentOutfit.hatVariant = data.hatVariant;
            }

            isHatEquipped = data.isHatEquipped;

            ApplyOutfit(currentOutfit);
            ApplyHatState();

            // Re-save to upgrade legacy schema in place.
            SaveWardrobe();
        }

        private OutfitData ResolveOutfit(WardrobeSaveData data)
        {
            if (!string.IsNullOrEmpty(data.currentOutfitName))
            {
                var found = unlockedOutfits.Find(o => o != null && o.name == data.currentOutfitName);
                if (found != null) return found;
            }
            if (unlockedOutfits.Count > 0) return unlockedOutfits[0];
            return null;
        }

        private void ApplyHatState()
        {
            SetHatActive(isHatEquipped);
        }

        private void InitializeDefaultWardrobe()
        {
            foreach (var name in DefaultOutfitNames)
            {
                var o = Resources.Load<OutfitData>(OutfitResourcePath + name);
                if (o != null && !unlockedOutfits.Contains(o)) unlockedOutfits.Add(o);
            }
            currentOutfit = unlockedOutfits.Count > 0 ? unlockedOutfits[0] : null;
            isHatEquipped = true;
        }

        private void EnsureDefaultOutfitsUnlocked()
        {
            if (unlockedOutfits.Count > 0) return;
            InitializeDefaultWardrobe();
        }

        private static readonly string[] DefaultOutfitNames =
        {
            "Outfit_Set_A", "Outfit_Set_B", "Outfit_Set_C", "Outfit_Set_D",
            "Outfit_Set_E", "Outfit_Set_F", "Outfit_Set_G", "Outfit_Set_H",
            "Outfit_Set_I", "Outfit_Set_J", "Outfit_Set_K", "Outfit_Set_L"
        };

        /// <summary>Finds the character mesh root under Player, handling
        /// Unity duplicate suffixes like "character (1)".</summary>
        private static GameObject FindCharacterModel()
        {
            // Fast path: exact path works for original prefab.
            var go = GameObject.Find("Player/character");
            if (go != null) return go;

            // Fallback: recursive name match under Player (handles "character (1)").
            var player = GameObject.Find("Player");
            if (player == null) return null;

            foreach (var t in player.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name.StartsWith("character"))
                    return t.gameObject;
            }
            return null;
        }

        /// <summary>Finds the "hat" GameObject anywhere in the Player
        /// hierarchy and toggles it. Replaces OutfitMeshSwapper dependency.</summary>
        private void SetHatActive(bool active)
        {
            var root = characterModel != null ? characterModel : FindCharacterModel();
            if (root == null) return;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "hat")
                {
                    t.gameObject.SetActive(active);
                    return;
                }
            }
        }

        #endregion
    }
}
