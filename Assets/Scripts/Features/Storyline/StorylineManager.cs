using System;
using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesStoryline.Data;
using FeaturesStoryline.UI;
using FeaturesTime;
using FeaturesCombat;
using FeaturesFarming;
using FeaturesRendering.Lighting;

namespace FeaturesStoryline
{
    /// <summary>
    /// Thin MonoBehaviour Adapter bridging Unity engine events to the pure StorylineEngine.
    /// Manages the Zero-GC Narrative Fact Blackboard, subscribes to domain telemetry events,
    /// evaluates salience-based QBN rules, and enforces input locking safety invariants.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-800)]
    public class StorylineManager : MonoBehaviour, IStorylineService
    {
        public static StorylineManager Instance { get; private set; }

        [Header("Narrative Content Configuration")]
        [Tooltip("Catalog of all story beats. If empty, automatically populates from StoryDatabase defaults.")]
        [SerializeField] private List<StoryBeatData> storyBeats = new List<StoryBeatData>();

        [Tooltip("Catalog of all collectible journal entries. If empty, automatically populates from StoryDatabase defaults.")]
        [SerializeField] private List<JournalEntryData> journalEntries = new List<JournalEntryData>();

        [Header("Atmospheric Sky Control (Day 5 Red Sky)")]
        [SerializeField] private bool overrideDay5Atmosphere = true;
        [SerializeField] private Color day5AmbientColor = new Color(0.65f, 0.25f, 0.25f);

        private readonly StorylineEngine _engine = new StorylineEngine();
        private INarrativeFactBoard _facts;
        private bool _isInitialized = false;

        // IStorylineService implementation
        public int CurrentChapter => _engine.CurrentChapter;
        public bool IsStoryComplete => _engine.IsStoryComplete;
        public int UnlockedJournalCount => _engine.UnlockedJournalCount;
        public bool IsJournalOpen => JournalUI.Instance != null && JournalUI.Instance.IsOpen;
        public IReadOnlyCollection<string> TriggeredBeatIds => _engine.TriggeredBeatIds;
        public IReadOnlyCollection<string> UnlockedJournalIds => _engine.UnlockedJournalIds;

        public IReadOnlyList<StoryBeatData> AllStoryBeats => storyBeats;
        public IReadOnlyList<JournalEntryData> AllJournalEntries => journalEntries;
        public StorylineEngine Engine => _engine;
        public INarrativeFactBoard FactBoard => _facts;

        public event Action<string> OnStoryBeatTriggered;
        public event Action<string> OnJournalEntryUnlocked;
        public event Action<int> OnChapterChanged;
        public event Action OnStoryCompleted;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }
            Instance = this;

            ServiceLocator.Register<IStorylineService>(this);

            if (!ServiceLocator.TryResolve<INarrativeFactBoard>(out _facts) || _facts == null)
            {
                _facts = new NarrativeFactBoard();
                ServiceLocator.Register<INarrativeFactBoard>(_facts);
            }

            InitializeContent();
            WireEngineEvents();
        }

        private void OnEnable()
        {
            SubscribeToGameEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<IStorylineService>();
                ServiceLocator.Unregister<INarrativeFactBoard>();
                Instance = null;
            }
        }

        private void Start()
        {
            // Failsafe re-subscription if DefaultExecutionOrder(-800) evaluated before Order 0 managers
            SubscribeToGameEvents();

            int currentDay = GetCurrentGameDay();
            _facts?.SetFact(NarrativeFactKey.CurrentDay, currentDay);
            EvaluateTrigger(StoryTriggerType.GameStart, currentDay);
        }

        private void InitializeContent()
        {
            if (_isInitialized) return;

            // 1. Check Resources if Inspector lists are empty
            if (journalEntries == null || journalEntries.Count == 0)
            {
                var resJournals = Resources.LoadAll<JournalEntryData>("Storyline/Journals");
                if (resJournals != null && resJournals.Length > 0)
                {
                    journalEntries = new List<JournalEntryData>(resJournals);
                }
                else
                {
                    journalEntries = StoryDatabase.CreateDefaultJournalEntries();
                }
            }

            if (storyBeats == null || storyBeats.Count == 0)
            {
                var resBeats = Resources.LoadAll<StoryBeatData>("Storyline/Beats");
                if (resBeats != null && resBeats.Length > 0)
                {
                    storyBeats = new List<StoryBeatData>(resBeats);
                }
                else
                {
                    storyBeats = StoryDatabase.CreateDefaultStoryBeats(journalEntries);
                }
            }

            StoryDatabase.BakeTriggerBuckets(storyBeats);
            _isInitialized = true;
        }

        private void WireEngineEvents()
        {
            _engine.OnBeatTriggered += (beatId) => OnStoryBeatTriggered?.Invoke(beatId);
            _engine.OnJournalUnlocked += (entryId) => OnJournalEntryUnlocked?.Invoke(entryId);
            _engine.OnChapterChanged += (chapter) => OnChapterChanged?.Invoke(chapter);
            _engine.OnStoryCompleted += () => OnStoryCompleted?.Invoke();
        }

        private void SubscribeToGameEvents()
        {
            // 1. Time Progression
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandleTimePhaseChanged;
                DayNightTimeManager.Instance.OnDayChanged += HandleDayChanged;
            }

            // 2. Boss Deaths
            EnemyBase.OnAnyEnemyDied += HandleAnyEnemyDied;

            // 3. Combat Waves & Spawns
            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.WaveEngine != null)
            {
                NightBrawlManager.Instance.WaveEngine.OnWaveStarted += HandleWaveStarted;
                NightBrawlManager.Instance.WaveEngine.OnAllWavesCleared += HandleAllWavesCleared;
            }
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned += HandleEnemySpawned;
            }

            // 4. Sleep Hook
            FeaturesInteraction.BedInteractable.OnBedSleepExecuted += HandleBedSleep;

            // 5. Farmland Hooks
            FarmlandTile.OnAnyCropPlanted += HandleCropPlanted;
            FarmlandTile.OnAnyCropHarvested += HandleCropHarvested;

            // 6. Well Refill Telemetry
            WaterWellInteractable.OnWaterWellRefilled += HandleWellWaterRefilled;

            // 7. Kitchen Sink Telemetry (via Core Telemetry)
            NarrativeTelemetry.OnCropWashed += HandleCropWashed;

            // 8. Interior Safe House State (via Core Telemetry)
            NarrativeTelemetry.OnPlayerSafeZoneChanged += HandleInteriorStateChanged;

            // 9. Weapon Upgrades (via Core Telemetry)
            NarrativeTelemetry.OnWeaponUpgraded += HandleWeaponUpgradesChanged;
        }

        private void UnsubscribeFromGameEvents()
        {
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleTimePhaseChanged;
                DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }

            EnemyBase.OnAnyEnemyDied -= HandleAnyEnemyDied;

            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.WaveEngine != null)
            {
                NightBrawlManager.Instance.WaveEngine.OnWaveStarted -= HandleWaveStarted;
                NightBrawlManager.Instance.WaveEngine.OnAllWavesCleared -= HandleAllWavesCleared;
            }
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned -= HandleEnemySpawned;
            }

            FeaturesInteraction.BedInteractable.OnBedSleepExecuted -= HandleBedSleep;
            FarmlandTile.OnAnyCropPlanted -= HandleCropPlanted;
            FarmlandTile.OnAnyCropHarvested -= HandleCropHarvested;
            WaterWellInteractable.OnWaterWellRefilled -= HandleWellWaterRefilled;
            NarrativeTelemetry.OnCropWashed -= HandleCropWashed;
            NarrativeTelemetry.OnPlayerSafeZoneChanged -= HandleInteriorStateChanged;
            NarrativeTelemetry.OnWeaponUpgraded -= HandleWeaponUpgradesChanged;
        }

        #region Event Handlers

        private void HandleTimePhaseChanged(EnvironmentPhase phase)
        {
            int currentDay = GetCurrentGameDay();
            _facts?.SetFact(NarrativeFactKey.CurrentDay, currentDay);
            _facts?.SetFact(NarrativeFactKey.CurrentDayPhase, (int)phase);

            if (phase == EnvironmentPhase.Dawn)
            {
                EvaluateTrigger(StoryTriggerType.Dawn, currentDay);
                ApplyAtmosphericEffects(currentDay);
            }
            else if (phase == EnvironmentPhase.Dusk)
            {
                EvaluateTrigger(StoryTriggerType.Dusk, currentDay);
            }
        }

        /// <summary>
        /// Explicitly transitions the narrative engine and fact board to a specific story chapter.
        /// </summary>
        public void SetChapter(int chapter)
        {
            _facts?.SetFact(NarrativeFactKey.CurrentDay, chapter);
            _engine?.SetChapter(chapter);
        }

        private void HandleDayChanged(int newDay)
        {
            SetChapter(newDay);
            ApplyAtmosphericEffects(newDay);

            if (newDay == 1)
            {
                EvaluateTrigger(StoryTriggerType.GameStart, 1);
            }
        }

        private void HandleWellWaterRefilled(int liters)
        {
            _facts?.AddFact(NarrativeFactKey.TotalWellWaterDrawnLiters, liters);
            _facts?.SetFact(NarrativeFactKey.WaterBottleCurrentLiters, 100);
            EvaluateTrigger(StoryTriggerType.WellWaterRefilled, GetCurrentGameDay());
        }

        private void HandleCropWashed(string cropName, int count)
        {
            _facts?.AddFact(NarrativeFactKey.DirtyCropsWashedCount, count);
            EvaluateTrigger(StoryTriggerType.CropWashedInSink, GetCurrentGameDay());
        }

        private void HandleInteriorStateChanged(bool isInside)
        {
            _facts?.SetFact(NarrativeFactKey.PlayerInHouseSafeZone, isInside ? 1 : 0);
            if (isInside)
            {
                FlushPendingModalBeats();
            }
        }

        private void HandleWeaponUpgradesChanged(int level, bool sweetPotato, bool taro, bool corn)
        {
            _facts?.SetFact(NarrativeFactKey.WeaponUpgradeTier_Speed, sweetPotato ? 1 : 0);
            _facts?.SetFact(NarrativeFactKey.WeaponUpgradeTier_Power, taro ? 1 : 0);
            _facts?.SetFact(NarrativeFactKey.WeaponUpgradeTier_Ballistic, corn ? 1 : 0);
            EvaluateTrigger(StoryTriggerType.WeaponUpgradedAtBench, GetCurrentGameDay());
        }

        private void HandleWaveStarted(int day, int waveIndex)
        {
            if (NightBrawlManager.Instance != null)
            {
                _facts?.SetFact(NarrativeFactKey.ActiveCombatThreatCount, NightBrawlManager.Instance.ActiveEnemiesCount);
            }
            EvaluateTrigger(StoryTriggerType.WaveStart, day, waveIndex);
        }

        private void HandleAllWavesCleared(int day)
        {
            _facts?.SetFact(NarrativeFactKey.ActiveCombatThreatCount, 0);
            EvaluateTrigger(StoryTriggerType.WaveCleared, day);
            FlushPendingModalBeats();
        }

        private void HandleEnemySpawned(EnemyBase enemy)
        {
            if (enemy == null) return;
            _facts?.AddFact(NarrativeFactKey.ActiveCombatThreatCount, 1);

            if (enemy.isBoss || FeaturesCombat.Wave.WaveProgressionEngine.IsBossType(enemy.enemyType))
            {
                int currentDay = GetCurrentGameDay();
                int currentWave = NightBrawlManager.Instance != null ? NightBrawlManager.Instance.CurrentWave : 1;
                EvaluateTrigger(StoryTriggerType.BossSpawn, currentDay, currentWave, enemy.enemyType);
            }
        }

        private void HandleAnyEnemyDied(EnemyBase enemy)
        {
            if (enemy == null) return;
            _facts?.AddFact(NarrativeFactKey.ActiveCombatThreatCount, -1);
            if (_facts != null && _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) < 0)
            {
                _facts.SetFact(NarrativeFactKey.ActiveCombatThreatCount, 0);
            }

            if (enemy.isBoss || FeaturesCombat.Wave.WaveProgressionEngine.IsBossType(enemy.enemyType))
            {
                int currentDay = GetCurrentGameDay();
                int currentWave = NightBrawlManager.Instance != null ? NightBrawlManager.Instance.CurrentWave : 1;
                EvaluateTrigger(StoryTriggerType.BossDeath, currentDay, currentWave, enemy.enemyType);

                if (_facts != null)
                {
                    int mask = _facts.GetFact(NarrativeFactKey.TrophyUnlockedBitmask);
                    if (enemy.enemyType == EnemyType.CyclopsTuberMaw) mask |= 1;
                    else if (enemy.enemyType == EnemyType.TaroColossus) mask |= 2;
                    else if (enemy.enemyType == EnemyType.TheRanger) mask |= 4;
                    _facts.SetFact(NarrativeFactKey.TrophyUnlockedBitmask, mask);
                }

                // If The Ranger was defeated on Day 5, complete story (Epilogue will be chained on victory dismissal)
                if (enemy.enemyType == EnemyType.TheRanger && currentDay >= 5)
                {
                    _engine.MarkStoryComplete();
                }
            }
        }

        private void HandleBedSleep()
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.Sleep, currentDay);
        }

        private void HandleCropPlanted(FarmlandTile tile, SeedItemData seed)
        {
            int currentDay = GetCurrentGameDay();
            UpdatePlantedCropCounts();
            EvaluateTrigger(StoryTriggerType.FirstPlant, currentDay);
        }

        private void HandleCropHarvested(FarmlandTile tile, ItemData crop, int count)
        {
            int currentDay = GetCurrentGameDay();
            UpdatePlantedCropCounts();
            EvaluateTrigger(StoryTriggerType.FirstHarvest, currentDay);
        }

        private void UpdatePlantedCropCounts()
        {
            if (_facts == null) return;

            int sp = 0, taro = 0, corn = 0;

            if (FarmingManager.Instance != null && FarmingManager.Instance.FarmTiles != null)
            {
                var tiles = FarmingManager.Instance.FarmTiles;
                for (int i = 0; i < tiles.Count; i++)
                {
                    var t = tiles[i];
                    if (t == null || t.PlantedSeed == null) continue;
                    string name = t.PlantedSeed.itemName;
                    if (string.IsNullOrEmpty(name)) continue;

                    if (name.IndexOf("SweetPotato", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Sweet Potato", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("sweet_potato", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        sp++;
                    }
                    else if (name.IndexOf("Taro", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        taro++;
                    }
                    else if (name.IndexOf("Corn", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        corn++;
                    }
                }
            }
            else
            {
                var tiles = FindObjectsByType<FarmlandTile>(FindObjectsSortMode.None);
                if (tiles != null)
                {
                    for (int i = 0; i < tiles.Length; i++)
                    {
                        var t = tiles[i];
                        if (t == null || t.PlantedSeed == null) continue;
                        string name = t.PlantedSeed.itemName;
                        if (string.IsNullOrEmpty(name)) continue;

                        if (name.IndexOf("SweetPotato", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Sweet Potato", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("sweet_potato", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sp++;
                        }
                        else if (name.IndexOf("Taro", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            taro++;
                        }
                        else if (name.IndexOf("Corn", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            corn++;
                        }
                    }
                }
            }

            _facts.SetFact(NarrativeFactKey.PlantedCount_SweetPotato, sp);
            _facts.SetFact(NarrativeFactKey.PlantedCount_Taro, taro);
            _facts.SetFact(NarrativeFactKey.PlantedCount_Corn, corn);
        }

        #endregion

        #region Trigger Evaluation & Delivery

        /// <summary>
        /// Evaluates candidates using the Salience-Based QBN Matcher on INarrativeFactBoard.
        /// </summary>
        public void EvaluateTrigger(StoryTriggerType triggerType, int day, int wave = 0, EnemyType? bossType = null)
        {
            InitializeContent();
            _facts?.SetFact(NarrativeFactKey.CurrentDay, day);

            var candidates = StoryDatabase.GetBeatsForTrigger(triggerType);
            if (candidates == null || candidates.Count == 0)
            {
                candidates = storyBeats;
            }

            var winningBeat = _engine.EvaluateTrigger(triggerType, _facts, candidates, day, wave, bossType);
            if (winningBeat != null)
            {
                DeliverStoryBeat(winningBeat);
            }
        }

        private void DeliverStoryBeat(StoryBeatData beat)
        {
            if (beat == null) return;

            _engine.RecordBeatTriggered(beat.beatId);
            if (beat.unlocksJournalEntry && beat.journalEntry != null)
            {
                _engine.UnlockJournal(beat.journalEntry.entryId);
            }

            // Non-modal channel (0.0s input lock, completely uninhibited movement)
            bool isNonModal = beat.deliveryType == StoryDeliveryType.HudBanner ||
                              beat.deliveryType == StoryDeliveryType.FloatingWorldText;

            if (isNonModal)
            {
                DispatchToUI(beat);
                return;
            }

            // Modal channel: enforce Hard Safety Invariant
            int threats = _facts != null ? _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) : 0;
            int inHouse = _facts != null ? _facts.GetFact(NarrativeFactKey.PlayerInHouseSafeZone) : 1;
            int phase = _facts != null ? _facts.GetFact(NarrativeFactKey.CurrentDayPhase) : 1;

            if (threats > 0 || (phase == (int)DayPhase.Night && inHouse == 0))
            {
                _engine.EnqueuePendingModalBeat(beat);
                Debug.Log($"[StorylineManager] 🛡️ Hard Safety Invariant: Deferred modal beat '{beat.beatId}' to safe zone queue (Threats: {threats}, InHouse: {inHouse}).");

                var ui = StorylineUI.Instance ?? FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
                if (ui != null)
                {
                    ui.ShowBanner("Archival Log Discovered", "New dossier recovered. Read safely in the house.", 3.5f);
                }
                return;
            }

            // Safe state: deliver immediately
            DispatchToUI(beat);
        }

        public void FlushPendingModalBeats()
        {
            int threats = _facts != null ? _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) : 0;
            int inHouse = _facts != null ? _facts.GetFact(NarrativeFactKey.PlayerInHouseSafeZone) : 1;

            // Cannot flush if still in combat outside house
            if (threats > 0 && inHouse == 0) return;

            while (_engine.HasPendingModalBeats)
            {
                var beat = _engine.DequeuePendingModalBeat();
                if (beat != null)
                {
                    DispatchToUI(beat);
                    break; // Deliver one modal window at a time to prevent modal stacking collisions
                }
            }
        }

        private void DispatchToUI(StoryBeatData beat)
        {
            Debug.Log($"[StorylineManager] 🎭 Delivering Story Beat: '{beat.beatId}' ({beat.title}) - Delivery: {beat.deliveryType}");

            if (beat.deliveryType == StoryDeliveryType.TimelineCutscene)
            {
                var cutscene = ServiceLocator.Resolve<ICutsceneService>();
                if (cutscene != null)
                {
                    cutscene.PlayOpeningCutscene();
                    return;
                }
                var cutsceneMb = FindFirstObjectByType<FeaturesStoryline.Cutscenes.OpeningCutsceneController>(FindObjectsInactive.Include);
                if (cutsceneMb != null)
                {
                    if (!cutsceneMb.gameObject.activeInHierarchy) cutsceneMb.gameObject.SetActive(true);
                    cutsceneMb.PlayOpeningCutscene();
                    return;
                }
            }

            if (beat.deliveryType == StoryDeliveryType.BossIntro)
            {
                var bossIntro = BossIntroSequence.Instance;
                if (bossIntro == null)
                {
                    bossIntro = FindFirstObjectByType<BossIntroSequence>(FindObjectsInactive.Include);
                }
                if (bossIntro != null)
                {
                    if (!bossIntro.gameObject.activeInHierarchy) bossIntro.gameObject.SetActive(true);
                    bossIntro.PlayBossIntro(
                        beat.title,
                        beat.speakerName,
                        beat.FormatNarrativeText(_facts),
                        duration: beat.displayDuration
                    );
                    return;
                }
            }

            var targetUI = StorylineUI.Instance;
            if (targetUI == null)
            {
                targetUI = FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
            }

            Action onBeatComplete = null;
            if (string.Equals(beat.beatId, "D5_VICTORY", StringComparison.OrdinalIgnoreCase))
            {
                onBeatComplete = () =>
                {
                    int currentDay = GetCurrentGameDay();
                    EvaluateTrigger(StoryTriggerType.Custom, currentDay);
                };
            }

            if (targetUI != null)
            {
                if (!targetUI.gameObject.activeInHierarchy) targetUI.gameObject.SetActive(true);
                targetUI.enabled = true;
                targetUI.DisplayBeat(beat, onBeatComplete);
            }
            else
            {
                var canvas = GameObject.Find("UI_Canvas");
                var uiGO = new GameObject("StorylineUI", typeof(StorylineUI));
                if (canvas != null)
                {
                    uiGO.transform.SetParent(canvas.transform, false);
                }
                if (!Application.isPlaying)
                {
                    uiGO.hideFlags = HideFlags.DontSave;
                }
                var ui = uiGO.GetComponent<StorylineUI>();
                ui.DisplayBeat(beat, onBeatComplete);
            }
        }

        private void ApplyAtmosphericEffects(int day)
        {
            if (!overrideDay5Atmosphere) return;

            if (day >= 5)
            {
                RenderSettings.ambientLight = day5AmbientColor;
                Debug.Log("[StorylineManager] 🩸 Applied Day 5 Crimson Sky atmosphere.");
            }
        }

        private int GetCurrentGameDay()
        {
            if (DayNightTimeManager.Instance != null)
                return DayNightTimeManager.Instance.CurrentDay;
            if (TimeManager.Instance != null)
                return TimeManager.Instance.currentDay;
            return _engine.CurrentChapter;
        }

        #endregion

        #region IStorylineService Implementation

        public bool HasTriggered(string beatId) => _engine.HasTriggered(beatId);

        public bool HasJournalEntry(string entryId) => _engine.HasJournalEntry(entryId);

        public bool TriggerBeat(string beatId)
        {
            InitializeContent();
            for (int i = 0; i < storyBeats.Count; i++)
            {
                var beat = storyBeats[i];
                if (beat != null && string.Equals(beat.beatId, beatId, StringComparison.OrdinalIgnoreCase))
                {
                    if (_facts != null && beat.mutations != null && beat.mutations.Length > 0)
                    {
                        for (int m = 0; m < beat.mutations.Length; m++)
                        {
                            _facts.ApplyMutation(beat.mutations[m]);
                        }
                    }
                    DeliverStoryBeat(beat);
                    return true;
                }
            }

            return _engine.RecordBeatTriggered(beatId);
        }

        public void ShowJournal()
        {
            var jUI = JournalUI.Instance;
            if (jUI == null)
            {
                jUI = FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
            }

            if (jUI != null)
            {
                if (!jUI.gameObject.activeInHierarchy) jUI.gameObject.SetActive(true);
                jUI.OpenJournal();
            }
            else
            {
                var canvas = GameObject.Find("UI_Canvas");
                var jGO = new GameObject("JournalUI", typeof(JournalUI));
                if (canvas != null)
                {
                    jGO.transform.SetParent(canvas.transform, false);
                }
                var j = jGO.GetComponent<JournalUI>();
                j.OpenJournal();
            }
        }

        public void HideJournal()
        {
            var jUI = JournalUI.Instance ?? FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
            if (jUI != null)
            {
                jUI.CloseJournal();
            }
        }

        public void RestoreState(int chapter, IEnumerable<string> beats, IEnumerable<string> journals, bool isComplete)
        {
            var payload = new StorylineSavePayload(chapter, beats, journals, isComplete);
            _engine.RestoreState(payload, _facts, storyBeats);

            var jUI = JournalUI.Instance ?? FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
            if (jUI != null)
            {
                jUI.SetEntries(journalEntries, _engine.UnlockedJournalIds);
            }
        }

        public void RestoreState(
            int chapter,
            IEnumerable<string> beats,
            IEnumerable<string> journals,
            bool isComplete,
            int[] rawFacts,
            IList<string> lastPlayedKeys,
            IList<int> lastPlayedDays)
        {
            var payload = new StorylineSavePayload(chapter, beats, journals, isComplete, rawFacts);
            if (lastPlayedKeys != null && lastPlayedDays != null)
            {
                payload.lastPlayedBeatKeys = new List<string>(lastPlayedKeys);
                payload.lastPlayedDays = new List<int>(lastPlayedDays);
            }
            _engine.RestoreState(payload, _facts, storyBeats);

            var jUI = JournalUI.Instance ?? FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
            if (jUI != null)
            {
                jUI.SetEntries(journalEntries, _engine.UnlockedJournalIds);
            }
        }

        public void ExportCooldowns(out List<string> keys, out List<int> days)
        {
            var payload = _engine.CreateSavePayload(_facts);
            keys = new List<string>(payload.lastPlayedBeatKeys);
            days = new List<int>(payload.lastPlayedDays);
        }

        public void RegisterStoryBeat(StoryBeatData beat)
        {
            if (beat == null || storyBeats.Contains(beat)) return;
            storyBeats.Add(beat);
            StoryDatabase.BakeTriggerBuckets(storyBeats);
        }

        public void RegisterJournalEntry(JournalEntryData entry)
        {
            if (entry == null || journalEntries.Contains(entry)) return;
            journalEntries.Add(entry);
        }

        #endregion
    }
}
