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
using FarmBeware.Core.Runtime;

namespace FeaturesSaveSystem
{
    /// <summary>
    /// Modular Multi-Slot Save & Load Manager for Farm Beware.
    /// Manages custom save names, fast manifest index caching, atomic file writes,
    /// input sanitization, and graceful corrupt-save recovery.
    /// </summary>
    public partial class SaveSystemManager : MonoBehaviour
    {
        private static SaveSystemManager instance;
        private static bool isQuitting = false;

        public static SaveSystemManager Instance
        {
            get
            {
                if (isQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<SaveSystemManager>(FindObjectsInactive.Include);
                    if (instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("SaveSystemManager");
                        instance = go.AddComponent<SaveSystemManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        public event Action OnSaveListChanged;
        public event Action<GameSaveData> OnSaveCompleted;
        public event Action<GameSaveData> OnLoadCompleted;

        public static string SavesDirectory
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Saves");
#else
                return Path.Combine(Application.persistentDataPath, "Saves");
#endif
            }
        }
        public static string ManifestFilePath => Path.Combine(SavesDirectory, "saves_manifest.json");

        private SaveManifest cachedManifest = new SaveManifest();
        private bool isManifestLoaded = false;
        public static string PendingSaveIdToLoad { get; set; } = null;
        public static bool IsPendingNewGameIntroCutscene
        {
            get => GameSessionState.IsPendingNewGameIntroCutscene;
            set => GameSessionState.IsPendingNewGameIntroCutscene = value;
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (SceneNames.IsGameplayScene(scene.name))
            {
                if (!string.IsNullOrEmpty(PendingSaveIdToLoad))
                {
                    string saveToLoad = PendingSaveIdToLoad;
                    PendingSaveIdToLoad = null;
                    IsPendingNewGameIntroCutscene = false;
                    StartCoroutine(RoutineApplyPendingLoad(saveToLoad));
                }
                else if (IsPendingNewGameIntroCutscene)
                {
                    // Synchronously set Day 0 BEFORE Start() runs on any scene components
                    if (FeaturesTime.DayNightTimeManager.Instance != null)
                    {
                        FeaturesTime.DayNightTimeManager.Instance.SetDayAndTime(0, 16.0f);
                        FeaturesTime.DayNightTimeManager.Instance.SetPaused(true);
                    }
                    if (TimeManager.Instance != null)
                    {
                        TimeManager.Instance.SetTimeState(0, TimeManager.DayPhase.Day, false, notifyPhaseChanged: false, notifyDayChanged: false);
                    }

                    StartCoroutine(RoutineApplyNewGameInitialization());
                }
            }
        }

        private System.Collections.IEnumerator RoutineApplyPendingLoad(string saveId)
        {
            yield return new WaitForEndOfFrame();
            bool success = LoadSave(saveId, out string msg);
            Debug.Log($"[SaveSystemManager] Auto-applied pending save on scene start: '{saveId}' - Success: {success}, {msg}");
        }

        private System.Collections.IEnumerator RoutineApplyNewGameInitialization()
        {
            yield return new WaitForEndOfFrame();

            // 1. Confirm Day 0 (Arrival Day)
            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                FeaturesTime.DayNightTimeManager.Instance.SetDayAndTime(0, 16.0f);
                FeaturesTime.DayNightTimeManager.Instance.SetPaused(true);
            }
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetTimeState(0, TimeManager.DayPhase.Day, false, notifyPhaseChanged: false, notifyDayChanged: false);
            }

            var storyline = ServiceLocator.Resolve<IStorylineService>();
            storyline?.ResetStorylineState();

            var facts = ServiceLocator.Resolve<INarrativeFactBoard>();
            facts?.ResetFacts();

            if (ServiceLocator.Resolve<INarrativeObjectiveService>() is IResettableQuestService resettableQuests)
            {
                resettableQuests.ResetQuests();
            }

            // 2. Play Opening Cutscene or Fallback Position
            var cutscene = ServiceLocator.Resolve<ICutsceneService>();
            if (cutscene == null)
            {
                foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (mb is ICutsceneService cs)
                    {
                        cutscene = cs;
                        break;
                    }
                }
            }

            if (cutscene != null)
            {
                if (!cutscene.IsCutscenePlaying)
                {
                    cutscene.PlayOpeningCutscene();
                }
            }
            else
            {
                // Fallback: If no cutscene service exists, teleport player outside gate facing south
                Vector3 gateSpawn = new Vector3(20.40f, 0.04f, 52.00f);
                Quaternion gateRot = Quaternion.Euler(0f, 180f, 0f);

                var playerCtx = ServiceLocator.Resolve<IPlayerContext>();
                if (playerCtx != null)
                {
                    playerCtx.Teleport(gateSpawn, gateRot);
                }
                else
                {
                    var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                    if (player != null)
                    {
                        var rb = player.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                            rb.position = gateSpawn;
                            rb.rotation = gateRot;
                        }
                        player.transform.position = gateSpawn;
                        player.transform.rotation = gateRot;
                        Physics.SyncTransforms();
                    }
                }
                Debug.LogWarning("[SaveSystemManager] ICutsceneService not found in scene. Opening cutscene skipped, player spawned at gate.");
            }

            IsPendingNewGameIntroCutscene = false;
            Debug.Log("[SaveSystemManager] Initialized fresh New Game session on Day 0 outside homestead gate.");
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            isQuitting = false;
            instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            EnsureDirectory();
            LoadManifest();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(SavesDirectory))
            {
                Directory.CreateDirectory(SavesDirectory);
            }

            // Auto-migrate legacy saves from persistentDataPath if SavesDirectory is newly created or missing them
            try
            {
                string legacyPath = Path.Combine(Application.persistentDataPath, "Saves");
                if (Directory.Exists(legacyPath) && !string.Equals(Path.GetFullPath(legacyPath), Path.GetFullPath(SavesDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    var legacyFiles = Directory.GetFiles(legacyPath, "*.json");
                    foreach (var file in legacyFiles)
                    {
                        string destFile = Path.Combine(SavesDirectory, Path.GetFileName(file));
                        if (!File.Exists(destFile))
                        {
                            File.Copy(file, destFile);
                            Debug.Log($"[SaveSystemManager] Auto-migrated save file from legacy path: {Path.GetFileName(file)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystemManager] Legacy saves migration warning: {ex.Message}");
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
                if (fName.Equals("saves_manifest.json", StringComparison.OrdinalIgnoreCase) ||
                    fName.Equals("wardrobeSave.json", StringComparison.OrdinalIgnoreCase))
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

        public string GetLastPlayedSaveId()
        {
            if (!isManifestLoaded) LoadManifest();
            if (!string.IsNullOrEmpty(cachedManifest.lastPlayedSaveId))
            {
                var meta = GetMetadataById(cachedManifest.lastPlayedSaveId);
                if (meta != null && File.Exists(Path.Combine(SavesDirectory, meta.fileName)))
                    return cachedManifest.lastPlayedSaveId;
            }

            var list = GetSaveList();
            if (list != null && list.Count > 0)
            {
                return list[0].saveId;
            }

            return null;
        }

        public void SetLastPlayedSaveId(string saveId)
        {
            if (!isManifestLoaded) LoadManifest();
            cachedManifest.lastPlayedSaveId = saveId;
            SaveManifestToDisk();
        }

        public SaveMetadata GetMetadataById(string saveId)
        {
            if (string.IsNullOrEmpty(saveId)) return null;
            if (!isManifestLoaded) LoadManifest();

            var meta = cachedManifest.slots.Find(s =>
                s.saveId == saveId ||
                (!string.IsNullOrEmpty(s.fileName) && s.fileName.Equals(saveId, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.fileName) && Path.GetFileNameWithoutExtension(s.fileName).Equals(saveId, StringComparison.OrdinalIgnoreCase))
            );

            if (meta == null)
            {
                // Try disk recovery if file exists directly
                string candidate = Path.Combine(SavesDirectory, saveId.EndsWith(".json") ? saveId : $"{saveId}.json");
                if (File.Exists(candidate))
                {
                    RebuildManifestFromDisk();
                    meta = cachedManifest.slots.Find(s =>
                        s.saveId == saveId ||
                        (!string.IsNullOrEmpty(s.fileName) && s.fileName.Equals(Path.GetFileName(candidate), StringComparison.OrdinalIgnoreCase))
                    );
                }
            }

            return meta;
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

        public static bool IsNightCombatActive()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene") return false;
            var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
            if (timeService != null && timeService.IsNight && !timeService.IsNightEncounterCleared)
            {
                return true;
            }
            if (FeaturesCombat.NightBrawlManager.Instance != null && FeaturesCombat.NightBrawlManager.Instance.IsNightBrawlActive)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a brand new save slot with a custom player name.
        /// </summary>
        public bool CreateNewSave(string customName, out string statusMessage)
        {
            if (IsNightCombatActive())
            {
                statusMessage = "Cannot save during Night Brawl! Clear all waves first.";
                ShowNotification("[Warning] Cannot save during Night Brawl! Clear all waves first.", new Color(1f, 0.4f, 0.4f));
                return false;
            }

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

                ShowNotification($"[Saved] '{safeDisplayName}' (Day {data.currentDay})", new Color(0.35f, 0.95f, 0.45f));
                OnSaveCompleted?.Invoke(data);
                OnSaveListChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Failed to Save: {ex.Message}";
                Debug.LogError($"[SaveSystemManager] Save creation failed: {ex}");
                ShowNotification("[Failed] Save Game Failed!", new Color(1f, 0.35f, 0.35f));
                return false;
            }
        }

        /// <summary>
        /// Overwrites an existing save slot while keeping or updating its name.
        /// </summary>
        public bool OverwriteSave(string saveId, out string statusMessage, string updatedName = null)
        {
            if (IsNightCombatActive())
            {
                statusMessage = "Cannot save during Night Brawl! Clear all waves first.";
                ShowNotification("[Warning] Cannot save during Night Brawl! Clear all waves first.", new Color(1f, 0.4f, 0.4f));
                return false;
            }

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

                ShowNotification($"[Overwrote] '{displayName}'", new Color(0.35f, 0.95f, 0.45f));
                OnSaveCompleted?.Invoke(data);
                OnSaveListChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Overwrite Failed: {ex.Message}";
                Debug.LogError($"[SaveSystemManager] Overwrite error: {ex}");
                ShowNotification("[Failed] Overwrite Failed!", new Color(1f, 0.35f, 0.35f));
                return false;
            }
        }

        /// <summary>
        /// Loads an existing save slot and applies all restored game variables.
        /// </summary>
        public bool LoadSave(string saveId, out string statusMessage)
        {
            if (IsNightCombatActive())
            {
                statusMessage = "Cannot load during Night Brawl! Clear all waves first.";
                ShowNotification("[Warning] Cannot load during Night Brawl! Clear all waves first.", new Color(1f, 0.4f, 0.4f));
                return false;
            }

            var meta = GetMetadataById(saveId);
            if (meta == null)
            {
                // Try finding save file directly on disk
                string directFile = Path.Combine(SavesDirectory, saveId.EndsWith(".json") ? saveId : $"{saveId}.json");
                if (File.Exists(directFile))
                {
                    RebuildManifestFromDisk();
                    meta = GetMetadataById(saveId);
                }
            }

            if (meta == null)
            {
                statusMessage = "Save slot record not found!";
                ShowNotification("[Warning] Save slot record not found!", new Color(1f, 0.65f, 0.25f));
                return false;
            }

            string filePath = Path.Combine(SavesDirectory, meta.fileName);
            if (!File.Exists(filePath))
            {
                string tmpPath = filePath + ".tmp";
                if (File.Exists(tmpPath))
                {
                    try { File.Move(tmpPath, filePath); } catch { }
                }
            }

            if (!File.Exists(filePath))
            {
                statusMessage = "Save file is missing on disk!";
                ShowNotification("[Error] Save file missing on disk!", new Color(1f, 0.35f, 0.35f));
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
                    ShowNotification("[Error] Corrupt Save Data!", new Color(1f, 0.35f, 0.35f));
                    return false;
                }

                ApplyLoadedGameState(data);

                // Ensure normal time scale after loading (e.g. from pause or death screen)
                UIModalHelper.ClearAllPauseLocks();

                cachedManifest.lastPlayedSaveId = saveId;
                SaveManifestToDisk();

                statusMessage = $"Loaded '{meta.displayName}'!";
                Debug.Log($"[SaveSystemManager] Game loaded from {filePath}");

                ShowNotification($"[Loaded] '{meta.displayName}' (Day {data.currentDay})", new Color(0.35f, 0.85f, 1f));
                OnLoadCompleted?.Invoke(data);
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Load Failed: {ex.Message}";
                Debug.LogError($"[SaveSystemManager] Error while loading {filePath}: {ex}");
                ShowNotification("[Error] Error Loading Save!", new Color(1f, 0.35f, 0.35f));
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
                ShowNotification($"[Deleted] '{meta.displayName}'", new Color(0.85f, 0.4f, 0.4f));
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

        /// <summary>
        /// Starts a fresh New Game session:
        /// Resets PendingSaveIdToLoad, resets DailyEconomyManager stats,
        /// restores normal timescale, unlocks input, and transitions to StagingScene.
        /// </summary>
        public void StartNewGame()
        {
            PendingSaveIdToLoad = null;
            IsPendingNewGameIntroCutscene = true;
            UIModalHelper.ClearAllPauseLocks();

            var storyline = ServiceLocator.Resolve<IStorylineService>();
            storyline?.ResetStorylineState();

            var facts = ServiceLocator.Resolve<INarrativeFactBoard>();
            facts?.ResetFacts();

            if (ServiceLocator.Resolve<INarrativeObjectiveService>() is IResettableQuestService resettableQuests)
            {
                resettableQuests.ResetQuests();
            }

            if (DailyEconomyManager.Instance != null)
            {
                DailyEconomyManager.Instance.ResetEconomyState();
            }

            // Close any open modals
            if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
            {
                ModalStackManager.Instance.CloseAll();
            }

            if (SaveSystemUI.HasInstance && SaveSystemUI.Instance != null)
            {
                SaveSystemUI.Instance.Close();
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (SceneNames.IsGameplayScene(activeScene))
            {
                StartCoroutine(RoutineApplyNewGameInitialization());
                return;
            }

            Debug.Log("[SaveSystemManager] Starting fresh New Game, transitioning to MainScene...");

            var fade = ServiceLocator.Resolve<IFadeService>();
            if (fade != null)
            {
                fade.FadeIn(0.4f, () =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainScene);
                });
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainScene);
            }
        }

        #endregion
    }
}
