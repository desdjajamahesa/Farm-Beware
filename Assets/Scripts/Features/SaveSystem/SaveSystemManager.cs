using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using FeaturesEconomy;
using FeaturesFarming;
using FeaturesKitchen;
using FeaturesWardrobe;
using FeaturesInteraction;

namespace FeaturesSaveSystem
{
    /// <summary>
    /// Modular Multi-Slot Save & Load Manager for Farm Beware.
    /// Manages custom save names, fast manifest index caching, atomic file writes,
    /// input sanitization, and graceful corrupt-save recovery.
    /// </summary>
    public class SaveSystemManager : MonoBehaviour
    {
        public static SaveSystemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SaveSystemManager>(FindObjectsInactive.Include);
                    if (instance == null)
                    {
                        var go = new GameObject("SaveSystemManager");
                        instance = go.AddComponent<SaveSystemManager>();
                        if (Application.isPlaying)
                            DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }
        private static SaveSystemManager instance;

        public event Action OnSaveListChanged;
        public event Action<GameSaveData> OnSaveCompleted;
        public event Action<GameSaveData> OnLoadCompleted;

        public static string SavesDirectory => Path.Combine(Application.persistentDataPath, "Saves");
        public static string ManifestFilePath => Path.Combine(SavesDirectory, "saves_manifest.json");

        private SaveManifest cachedManifest = new SaveManifest();
        private bool isManifestLoaded = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            EnsureDirectory();
            LoadManifest();
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(SavesDirectory))
            {
                Directory.CreateDirectory(SavesDirectory);
            }
        }

        #region Name Validation & Sanitization

        /// <summary>
        /// Sanitizes player input: removes illegal filesystem characters, trims whitespace,
        /// bounds length between 3 and 25 characters, and applies auto-naming fallback if empty.
        /// </summary>
        public static string SanitizeSaveName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return GenerateDefaultSaveName();
            }

            // Remove invalid filesystem characters: \ / : * ? " < > | and control characters
            string clean = Regex.Replace(rawName, @"[\\/:*?""<>|\a\b\e\f\n\r\t\v\0]", "");
            clean = clean.Trim();

            // Truncate to max 25 characters
            if (clean.Length > 25)
            {
                clean = clean.Substring(0, 25).Trim();
            }

            // If stripped down to less than 3 characters, use default naming fallback
            if (clean.Length < 3)
            {
                return GenerateDefaultSaveName();
            }

            return clean;
        }

        public static string GenerateDefaultSaveName()
        {
            return $"Save_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        #endregion

        #region Manifest Management

        public void LoadManifest()
        {
            EnsureDirectory();

            if (File.Exists(ManifestFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ManifestFilePath);
                    cachedManifest = JsonUtility.FromJson<SaveManifest>(json);
                    if (cachedManifest == null || cachedManifest.slots == null)
                    {
                        cachedManifest = new SaveManifest();
                        RebuildManifestFromDisk();
                    }
                    else
                    {
                        // Clean up any missing files
                        bool changed = false;
                        for (int i = cachedManifest.slots.Count - 1; i >= 0; i--)
                        {
                            string fPath = Path.Combine(SavesDirectory, cachedManifest.slots[i].fileName);
                            if (!File.Exists(fPath))
                            {
                                cachedManifest.slots.RemoveAt(i);
                                changed = true;
                            }
                        }
                        if (changed) SaveManifestToDisk();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveSystemManager] Failed to read manifest, rebuilding from disk: {ex.Message}");
                    RebuildManifestFromDisk();
                }
            }
            else
            {
                RebuildManifestFromDisk();
            }

            isManifestLoaded = true;
        }

        public void SaveManifestToDisk()
        {
            EnsureDirectory();
            try
            {
                string json = JsonUtility.ToJson(cachedManifest, true);
                WriteAtomicFile(ManifestFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystemManager] Failed to write manifest: {ex.Message}");
            }
        }

        public void RebuildManifestFromDisk()
        {
            EnsureDirectory();
            cachedManifest = new SaveManifest();

            var files = Directory.GetFiles(SavesDirectory, "*.json");
            foreach (var file in files)
            {
                string fName = Path.GetFileName(file);
                if (fName.Equals("saves_manifest.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonUtility.FromJson<GameSaveData>(json);
                    if (data != null && data.metadata != null)
                    {
                        if (string.IsNullOrEmpty(data.metadata.saveId))
                            data.metadata.saveId = Path.GetFileNameWithoutExtension(file);

                        data.metadata.fileName = fName;
                        data.metadata.fileSizeBytes = new FileInfo(file).Length;
                        cachedManifest.slots.Add(data.metadata);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveSystemManager] Skipped corrupted save file during manifest rebuild: {fName} - {ex.Message}");
                }
            }

            SaveManifestToDisk();
        }

        public List<SaveMetadata> GetSaveList()
        {
            if (!isManifestLoaded) LoadManifest();

            // Sort newest first
            cachedManifest.slots.Sort((a, b) => string.Compare(b.timestamp, a.timestamp, StringComparison.Ordinal));
            return new List<SaveMetadata>(cachedManifest.slots);
        }

        public SaveMetadata GetMetadataById(string saveId)
        {
            if (!isManifestLoaded) LoadManifest();
            return cachedManifest.slots.Find(s => s.saveId == saveId);
        }

        #endregion

        #region Atomic File Operations

        /// <summary>
        /// Writes data safely using a temporary file to avoid corrupting saves if interrupted.
        /// </summary>
        private static void WriteAtomicFile(string targetPath, string content)
        {
            string tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, content);
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(tempPath, targetPath);
        }

        #endregion

        #region Save / Overwrite / Load / Delete API

        /// <summary>
        /// Creates a brand new save slot with a custom player name.
        /// </summary>
        public bool CreateNewSave(string customName, out string statusMessage)
        {
            EnsureDirectory();

            string safeDisplayName = SanitizeSaveName(customName);
            string saveId = "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string fileName = $"{saveId}.json";
            string filePath = Path.Combine(SavesDirectory, fileName);

            try
            {
                var data = CaptureCurrentGameState();
                data.metadata = new SaveMetadata
                {
                    saveId = saveId,
                    displayName = safeDisplayName,
                    fileName = fileName,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    formattedDate = DateTime.Now.ToString("dd MMM yyyy, HH:mm"),
                    dayNumber = data.currentDay,
                    phaseName = data.currentPhase == 1 ? "Night" : "Day",
                    gold = data.gold,
                    currentHealth = data.health,
                    maxHealth = 100,
                    locationName = GetCurrentPlayerLocationName()
                };

                string json = JsonUtility.ToJson(data, true);
                WriteAtomicFile(filePath, json);

                data.metadata.fileSizeBytes = new FileInfo(filePath).Length;

                cachedManifest.slots.Add(data.metadata);
                cachedManifest.lastPlayedSaveId = saveId;
                SaveManifestToDisk();

                statusMessage = $"Game Saved as '{safeDisplayName}'!";
                Debug.Log($"[SaveSystemManager] New save created successfully: {filePath}");

                ShowNotification($"💾 Saved '{safeDisplayName}' (Day {data.currentDay})", new Color(0.35f, 0.95f, 0.45f));
                OnSaveCompleted?.Invoke(data);
                OnSaveListChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Failed to Save: {ex.Message}";
                Debug.LogError($"[SaveSystemManager] Save creation failed: {ex}");
                ShowNotification("❌ Failed to Save Game!", new Color(1f, 0.35f, 0.35f));
                return false;
            }
        }

        /// <summary>
        /// Overwrites an existing save slot while keeping or updating its name.
        /// </summary>
        public bool OverwriteSave(string saveId, out string statusMessage, string updatedName = null)
        {
            EnsureDirectory();

            var existingMeta = GetMetadataById(saveId);
            if (existingMeta == null)
            {
                statusMessage = "Save slot not found to overwrite.";
                return false;
            }

            string filePath = Path.Combine(SavesDirectory, existingMeta.fileName);

            try
            {
                var data = CaptureCurrentGameState();

                string displayName = !string.IsNullOrWhiteSpace(updatedName)
                    ? SanitizeSaveName(updatedName)
                    : existingMeta.displayName;

                data.metadata = new SaveMetadata
                {
                    saveId = existingMeta.saveId,
                    displayName = displayName,
                    fileName = existingMeta.fileName,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    formattedDate = DateTime.Now.ToString("dd MMM yyyy, HH:mm"),
                    dayNumber = data.currentDay,
                    phaseName = data.currentPhase == 1 ? "Night" : "Day",
                    gold = data.gold,
                    currentHealth = data.health,
                    maxHealth = 100,
                    locationName = GetCurrentPlayerLocationName()
                };

                string json = JsonUtility.ToJson(data, true);
                WriteAtomicFile(filePath, json);

                data.metadata.fileSizeBytes = new FileInfo(filePath).Length;

                // Replace in manifest
                int idx = cachedManifest.slots.FindIndex(s => s.saveId == saveId);
                if (idx >= 0)
                {
                    cachedManifest.slots[idx] = data.metadata;
                }
                cachedManifest.lastPlayedSaveId = saveId;
                SaveManifestToDisk();

                statusMessage = $"Overwrote '{displayName}'!";
                Debug.Log($"[SaveSystemManager] Save overwritten successfully: {filePath}");

                ShowNotification($"💾 Overwrote '{displayName}'", new Color(0.35f, 0.95f, 0.45f));
                OnSaveCompleted?.Invoke(data);
                OnSaveListChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Overwrite Failed: {ex.Message}";
                Debug.LogError($"[SaveSystemManager] Overwrite error: {ex}");
                ShowNotification("❌ Overwrite Failed!", new Color(1f, 0.35f, 0.35f));
                return false;
            }
        }

        /// <summary>
        /// Loads an existing save slot and applies all restored game variables.
        /// </summary>
        public bool LoadSave(string saveId, out string statusMessage)
        {
            var meta = GetMetadataById(saveId);
            if (meta == null)
            {
                statusMessage = "Save slot record not found!";
                ShowNotification("⚠️ Save slot record not found!", new Color(1f, 0.65f, 0.25f));
                return false;
            }

            string filePath = Path.Combine(SavesDirectory, meta.fileName);
            if (!File.Exists(filePath))
            {
                statusMessage = "Save file is missing on disk!";
                ShowNotification("❌ Save file missing on disk!", new Color(1f, 0.35f, 0.35f));
                // Clean manifest
                cachedManifest.slots.Remove(meta);
                SaveManifestToDisk();
                OnSaveListChanged?.Invoke();
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                {
                    statusMessage = "Save data is corrupted.";
                    ShowNotification("❌ Corrupt Save Data!", new Color(1f, 0.35f, 0.35f));
                    return false;
                }

                ApplyLoadedGameState(data);

                cachedManifest.lastPlayedSaveId = saveId;
                SaveManifestToDisk();

                statusMessage = $"Loaded '{meta.displayName}'!";
                Debug.Log($"[SaveSystemManager] Game loaded from {filePath}");

                ShowNotification($"📂 Loaded '{meta.displayName}' (Day {data.currentDay})", new Color(0.35f, 0.85f, 1f));
                OnLoadCompleted?.Invoke(data);
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Load Failed: {ex.Message}";
                Debug.LogError($"[SaveSystemManager] Error while loading {filePath}: {ex}");
                ShowNotification("❌ Error Loading Save!", new Color(1f, 0.35f, 0.35f));
                return false;
            }
        }

        /// <summary>
        /// Deletes a save slot and its file from disk.
        /// </summary>
        public bool DeleteSave(string saveId, out string statusMessage)
        {
            var meta = GetMetadataById(saveId);
            if (meta == null)
            {
                statusMessage = "Save slot not found.";
                return false;
            }

            string filePath = Path.Combine(SavesDirectory, meta.fileName);
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                cachedManifest.slots.Remove(meta);
                if (cachedManifest.lastPlayedSaveId == saveId)
                    cachedManifest.lastPlayedSaveId = null;

                SaveManifestToDisk();

                statusMessage = $"Deleted '{meta.displayName}'!";
                ShowNotification($"🗑️ Deleted '{meta.displayName}'", new Color(0.85f, 0.4f, 0.4f));
                OnSaveListChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Failed to delete save: {ex.Message}";
                Debug.LogError($"[SaveSystemManager] Error deleting {filePath}: {ex}");
                return false;
            }
        }

        #endregion

        #region Game State Capture & Apply

        private GameSaveData CaptureCurrentGameState()
        {
            var data = new GameSaveData
            {
                saveVersion = 1
            };

            // 1. Player Transform & Stats & Inventory
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                data.playerPosX = player.transform.position.x;
                data.playerPosY = player.transform.position.y;
                data.playerPosZ = player.transform.position.z;
                data.playerRotY = player.transform.eulerAngles.y;

                var stats = player.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    data.health = stats.currentHealth;
                    data.stamina = stats.currentStamina;
                    data.hunger = stats.currentHunger;
                    data.thirst = stats.currentThirst;
                }

                var inv = player.GetComponent<InventoryComponent>();
                if (inv != null && inv.slots != null)
                {
                    for (int i = 0; i < inv.slots.Count; i++)
                    {
                        var slot = inv.slots[i];
                        if (slot != null && !slot.IsEmpty && slot.item != null)
                        {
                            string id = !string.IsNullOrEmpty(slot.item.itemId) ? slot.item.itemId : slot.item.name;
                            data.inventorySlots.Add(new SavedInventorySlot
                            {
                                slotIndex = i,
                                itemId = id,
                                quantity = slot.quantity
                            });
                        }
                    }
                }
            }

            // 2. Gold
            if (PlayerWallet.Instance != null)
            {
                data.gold = PlayerWallet.Instance.CurrentGold;
            }

            // 3. Water Bottle
            if (PlayerWaterBottle.Instance != null)
            {
                data.waterBottleAmount = PlayerWaterBottle.Instance.CurrentWater;
            }

            // 4. Time
            if (TimeManager.Instance != null)
            {
                data.currentDay = TimeManager.Instance.currentDay;
                data.currentPhase = (int)TimeManager.Instance.currentPhase;
                data.isNightEncounterCleared = TimeManager.Instance.isNightEncounterCleared;
            }
            else
            {
                data.currentDay = 1;
                data.currentPhase = 0;
            }

            // 5. Farmland Crops
            var tiles = FindObjectsByType<FarmlandTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (tile == null) continue;
                    string seedId = "";
                    if (tile.PlantedSeed != null)
                    {
                        seedId = !string.IsNullOrEmpty(tile.PlantedSeed.itemId) ? tile.PlantedSeed.itemId : tile.PlantedSeed.name;
                    }

                    data.farmlandTiles.Add(new SavedFarmlandTile
                    {
                        tileName = tile.gameObject.name,
                        tileState = (int)tile.CurrentState,
                        seedItemId = seedId,
                        growthProgress = tile.GrowthProgress,
                        currentTimer = tile.GrowthProgress * 30f
                    });
                }
            }

            // 6. Wardrobe
            if (PlayerOutfit.Instance != null && PlayerOutfit.Instance.CurrentOutfit != null)
            {
                data.outfitName = PlayerOutfit.Instance.CurrentOutfit.name;
                data.isHatEquipped = PlayerOutfit.Instance.isHatEquipped;
            }

            return data;
        }

        private void ApplyLoadedGameState(GameSaveData data)
        {
            // 1. Player Transform & CharacterController
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
                player.transform.rotation = Quaternion.Euler(0f, data.playerRotY, 0f);

                if (cc != null) cc.enabled = true;

                // 2. Stats
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.RestoreStats(data.health, data.stamina, data.hunger, data.thirst);
                }

                // 3. Inventory
                var inv = player.GetComponent<InventoryComponent>();
                if (inv != null && inv.slots != null)
                {
                    for (int i = 0; i < inv.slots.Count; i++)
                    {
                        if (inv.slots[i] != null)
                        {
                            inv.slots[i].item = null;
                            inv.slots[i].quantity = 0;
                        }
                    }

                    foreach (var savedSlot in data.inventorySlots)
                    {
                        if (savedSlot.slotIndex >= 0 && savedSlot.slotIndex < inv.slots.Count)
                        {
                            var item = ResolveItem(savedSlot.itemId);
                            if (item != null)
                            {
                                inv.slots[savedSlot.slotIndex].item = item;
                                inv.slots[savedSlot.slotIndex].quantity = savedSlot.quantity;
                            }
                        }
                    }

                    inv.OnInventoryChanged?.Invoke();
                }
            }

            // 4. Gold
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.SetGold(data.gold);
            }

            // 5. Water Bottle
            if (PlayerWaterBottle.Instance != null)
            {
                PlayerWaterBottle.Instance.SetWater(data.waterBottleAmount);
            }

            // 6. Time & Day
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetTimeState(
                    data.currentDay,
                    (TimeManager.DayPhase)data.currentPhase,
                    data.isNightEncounterCleared
                );
            }

            // 7. Farmland Crops
            var tiles = FindObjectsByType<FarmlandTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (tiles != null && data.farmlandTiles != null)
            {
                var tileLookup = new Dictionary<string, FarmlandTile>();
                foreach (var t in tiles)
                {
                    if (t != null && !tileLookup.ContainsKey(t.gameObject.name))
                        tileLookup.Add(t.gameObject.name, t);
                }

                foreach (var savedTile in data.farmlandTiles)
                {
                    if (tileLookup.TryGetValue(savedTile.tileName, out var tile))
                    {
                        SeedItemData seed = null;
                        if (!string.IsNullOrEmpty(savedTile.seedItemId))
                        {
                            var resolved = ResolveItem(savedTile.seedItemId);
                            seed = resolved as SeedItemData;
                        }

                        tile.RestoreCropState(
                            (TileState)savedTile.tileState,
                            seed,
                            savedTile.growthProgress,
                            savedTile.currentTimer
                        );
                    }
                }
            }

            // 8. Wardrobe Outfit
            if (PlayerOutfit.Instance != null && !string.IsNullOrEmpty(data.outfitName))
            {
                var outfit = Resources.Load<OutfitData>("Player/model/" + data.outfitName);
                if (outfit != null)
                {
                    PlayerOutfit.Instance.EquipOutfit(outfit);
                }
            }
        }

        private string GetCurrentPlayerLocationName()
        {
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                var zone = player.GetComponentInChildren<InteractionZone>();
                if (zone == null)
                {
                    var zones = FindObjectsByType<InteractionZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var z in zones)
                    {
                        if (z.ContainsPoint(player.transform.position))
                            return z.gameObject.name.Replace("Zone", "");
                    }
                }
                else
                {
                    return zone.gameObject.name.Replace("Zone", "");
                }
            }
            return "Bedroom";
        }

        private ItemData ResolveItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            if (ItemDatabase.Instance != null)
            {
                var item = ItemDatabase.Instance.GetItem(itemId);
                if (item != null) return item;
            }

            var allItems = Resources.LoadAll<ItemData>("");
            if (allItems != null)
            {
                foreach (var it in allItems)
                {
                    if (it != null && (it.itemId == itemId || it.name == itemId))
                        return it;
                }
            }

            return null;
        }

        private void ShowNotification(string msg, Color col)
        {
            if (PlayerUI.FloatingCombatTextManager.Instance != null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                Vector3 pos = player != null ? player.transform.position + Vector3.up * 1.8f : Vector3.zero;
                PlayerUI.FloatingCombatTextManager.Instance.SpawnText(pos, msg, col);
            }
        }

        #endregion
    }
}
