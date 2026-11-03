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
using FeaturesStoryline.Cutscenes;

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
        private static StorylineManager _instance;
        public static StorylineManager Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    _instance = FindFirstObjectByType<StorylineManager>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name == "MainMenuScene")
            {
                return;
            }

            if (Instance == null && Application.isPlaying)
            {
                var existing = FindFirstObjectByType<StorylineManager>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    var go = new GameObject("StorylineManager");
                    go.AddComponent<StorylineManager>();
                }
            }
        }

        [Header("Narrative Content Configuration")]
        [Tooltip("Catalog of all story beats. If empty, automatically populates from StoryDatabase defaults.")]
        [SerializeField] private List<StoryBeatData> storyBeats = new List<StoryBeatData>();

        [Tooltip("Catalog of all collectible journal entries. If empty, automatically populates from StoryDatabase defaults.")]
        [SerializeField] private List<JournalEntryData> journalEntries = new List<JournalEntryData>();

        [Header("Atmospheric Sky Control (Day 5 Red Sky)")]
        [SerializeField] private bool overrideDay5Atmosphere = true;
        [SerializeField] private Color day5AmbientColor = new Color(0.65f, 0.25f, 0.25f);

        private Color _baselineAmbientLight;
        private float _baselineFogDensity;
        private bool _baselineAtmosphereCached = false;
        private bool _isAtmosphereOverridden = false;

        private readonly StorylineEngine _engine = new StorylineEngine();
        private readonly Queue<StoryBeatData> _pendingNonModalBeats = new Queue<StoryBeatData>();
        private INarrativeFactBoard _facts;
        private bool _isInitialized = false;

        // IStorylineService implementation
        public int CurrentChapter => _engine.CurrentChapter;
        public bool IsStoryComplete => _engine.IsStoryComplete;
        public int UnlockedJournalCount => _engine.UnlockedJournalCount;
        public bool IsJournalOpen => JournalUI.Instance != null && JournalUI.Instance.IsOpen;
        public IReadOnlyCollection<string> TriggeredBeatIds => _engine.TriggeredBeatIds;
        public IReadOnlyCollection<string> UnlockedJournalIds => _engine.UnlockedJournalIds;
        public int PendingNonModalBeatsCount => _pendingNonModalBeats.Count;

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
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene")
            {
                enabled = false;
                return;
            }

            if (Application.isPlaying && Instance != null && Instance != this)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (transform.parent != null && Application.isPlaying)
            {
                transform.SetParent(null);
            }

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            CacheBaselineAtmosphere();
            Initialize();
        }

        public void CacheBaselineAtmosphere()
        {
            if (_baselineAtmosphereCached || _isAtmosphereOverridden)
            {
                return;
            }

            _baselineAmbientLight = RenderSettings.ambientLight;
            _baselineFogDensity = RenderSettings.fogDensity;
            _baselineAtmosphereCached = true;
            Debug.Log($"[StorylineManager] 🌤️ Cached baseline atmosphere: Ambient={_baselineAmbientLight}, Fog={_baselineFogDensity}");
        }

        public void RestoreAtmosphere()
        {
            if (_isAtmosphereOverridden && _baselineAtmosphereCached)
            {
                RenderSettings.ambientLight = _baselineAmbientLight;
                RenderSettings.fogDensity = _baselineFogDensity;
                _isAtmosphereOverridden = false;
                Debug.Log("[StorylineManager] 🌤️ Restored baseline atmosphere settings.");
            }
        }

        public void Initialize()
        {
            if (Application.isPlaying && Instance != null && Instance != this)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

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
            if (Application.isPlaying)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
            }
            SubscribeToGameEvents();
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
                }
                RestoreAtmosphere();
                UnsubscribeFromGameEvents();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
                }
                RestoreAtmosphere();
                UnsubscribeFromGameEvents();
                ServiceLocator.Unregister<IStorylineService>();
                ServiceLocator.Unregister<INarrativeFactBoard>();
                Instance = null;
            }
        }

        public void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (Instance != this) return;
            if (scene.name == "MainMenuScene")
            {
                enabled = false;
                return;
            }
            enabled = true;

            _baselineAtmosphereCached = false;
            _isAtmosphereOverridden = false;
            CacheBaselineAtmosphere();

            SubscribeToGameEvents();

            int currentDay = GetCurrentGameDay();
            _facts?.SetFact(NarrativeFactKey.CurrentDay, currentDay);

            if (currentDay >= 5)
            {
                ApplyAtmosphericEffects(currentDay);
            }

            int threats = _facts != null ? _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) : 0;
            if (threats == 0)
            {
                FlushPendingModalBeats();
            }
        }

        private void Start()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene")
            {
                enabled = false;
                return;
            }

            // Failsafe re-subscription if DefaultExecutionOrder(-800) evaluated before Order 0 managers
            SubscribeToGameEvents();

            int currentDay = GetCurrentGameDay();
            _facts?.SetFact(NarrativeFactKey.CurrentDay, currentDay);
            AssignDailyStoryObjectives(currentDay);

            bool isCutsceneRunningOrPending = currentDay == 0 ||
                                              FarmBeware.Core.Runtime.GameSessionState.IsPendingNewGameIntroCutscene ||
                                              IsCutsceneActive();

            if (!isCutsceneRunningOrPending)
            {
                EvaluateTrigger(StoryTriggerType.GameStart, currentDay);
                if (currentDay >= 1)
                {
                    EvaluateTrigger(StoryTriggerType.Dawn, currentDay);
                }
            }
            else if (currentDay == 0)
            {
                EvaluateTrigger(StoryTriggerType.GameStart, 0);
            }
        }

        /// <summary>
        /// Fully resets the narrative engine, blackboard facts, and triggered beats for a fresh session.
        /// Prevents stale facts (such as IntroCutsceneCompleted) from leaking into a New Game.
        /// </summary>
        public void ResetStorylineState()
        {
            _engine.Reset();
            if (_facts != null)
            {
                _facts.ResetFacts();
            }
            _pendingNonModalBeats.Clear();
            _isAtmosphereOverridden = false;
            RestoreAtmosphere();
            Debug.Log("[StorylineManager] 🔄 Narrative state and FactBoard fully reset for fresh session.");
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

            for (int i = 0; i < storyBeats.Count; i++)
            {
                var beat = storyBeats[i];
                if (beat != null && string.Equals(beat.beatId, "EPILOGUE_STATS", StringComparison.OrdinalIgnoreCase))
                {
                    if (beat.criteria == null || beat.criteria.Length == 0)
                    {
                        beat.criteria = new[] { new NarrativeFactCondition(NarrativeFactKey.TheRangerSlayed, FactOperator.GreaterOrEqual, 1) };
                    }
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
            UnsubscribeFromGameEvents();

            // 1. Time Progression
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandleTimePhaseChanged;
                DayNightTimeManager.Instance.OnDayChanged += HandleDayChanged;
                DayNightTimeManager.Instance.OnDuskWarning += HandleDuskWarning;
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
            FarmlandTile.OnAnySoilTilled += HandleSoilTilled;
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

            // 10. Cross-subsystem Narrative Telemetry Hooks
            NarrativeTelemetryHooks.OnMerchantOpened += HandleMerchantOpened;
            NarrativeTelemetryHooks.OnTrophyInspected += HandleTrophyInspected;
            NarrativeTelemetryHooks.OnCookingCompleted += HandleCookingCompleted;
            NarrativeTelemetryHooks.OnCropWithered += HandleCropWithered;

            // 11. Day/Night Environmental Transition Gating
            if (DayTransitionUI.Instance != null)
            {
                DayTransitionUI.Instance.OnTransitionCompleted -= HandleDayTransitionCompleted;
                DayTransitionUI.Instance.OnTransitionCompleted += HandleDayTransitionCompleted;
            }

            // 12. Cutscene Service Hook
            SubscribeCutsceneEnded();
        }

        private void UnsubscribeFromGameEvents()
        {
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleTimePhaseChanged;
                DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
                DayNightTimeManager.Instance.OnDuskWarning -= HandleDuskWarning;
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
            FarmlandTile.OnAnySoilTilled -= HandleSoilTilled;
            FarmlandTile.OnAnyCropPlanted -= HandleCropPlanted;
            FarmlandTile.OnAnyCropHarvested -= HandleCropHarvested;
            WaterWellInteractable.OnWaterWellRefilled -= HandleWellWaterRefilled;
            NarrativeTelemetry.OnCropWashed -= HandleCropWashed;
            NarrativeTelemetry.OnPlayerSafeZoneChanged -= HandleInteriorStateChanged;
            NarrativeTelemetry.OnWeaponUpgraded -= HandleWeaponUpgradesChanged;
            NarrativeTelemetryHooks.OnMerchantOpened -= HandleMerchantOpened;
            NarrativeTelemetryHooks.OnTrophyInspected -= HandleTrophyInspected;
            NarrativeTelemetryHooks.OnCookingCompleted -= HandleCookingCompleted;
            NarrativeTelemetryHooks.OnCropWithered -= HandleCropWithered;

            if (DayTransitionUI.Instance != null)
            {
                DayTransitionUI.Instance.OnTransitionCompleted -= HandleDayTransitionCompleted;
            }

            UnsubscribeCutsceneEnded();
        }

        #region Event Handlers

        private void HandleDuskWarning()
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.DuskApproaching, currentDay);
        }

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
            AssignDailyStoryObjectives(newDay);

            if (newDay >= 1)
            {
                EvaluateTrigger(StoryTriggerType.GameStart, newDay);
                EvaluateTrigger(StoryTriggerType.Dawn, newDay);
            }
        }

        /// <summary>
        /// Dispatches daily storyline objectives to the unified INarrativeObjectiveService.
        /// </summary>
        public void AssignDailyStoryObjectives(int day)
        {
            var objService = ServiceLocator.Resolve<INarrativeObjectiveService>();
            if (objService == null) return;

            switch (day)
            {
                case 1:
                    objService.AssignStoryObjective(
                        "OBJ_D1_FIRST_STEPS",
                        NarrativeObjectiveCategory.MainStory,
                        "First Steps: Till & Plant",
                        "Till 1 plot, draw well water, and plant Sweet Potato",
                        1,
                        NarrativeFactKey.PlantedCount_SweetPotato);
                    break;

                case 2:
                    objService.AssignStoryObjective(
                        "OBJ_D2_ARMORED_TARO",
                        NarrativeObjectiveCategory.MainStory,
                        "Armored Harvest: Clean Taro",
                        "Harvest Taro and wash 3 roots at the Kitchen Sink",
                        3,
                        NarrativeFactKey.WashedCount_Taro);
                    break;

                case 3:
                    objService.AssignStoryObjective(
                        "OBJ_D3_WORKBENCH",
                        NarrativeObjectiveCategory.MainStory,
                        "Workbench: Harden Weapon",
                        "Upgrade weapon at Workbench before Dusk",
                        1,
                        NarrativeFactKey.WeaponUpgradeTier_Speed);
                    break;

                case 4:
                    objService.AssignStoryObjective(
                        "OBJ_D4_BATTLE_PREP",
                        NarrativeObjectiveCategory.MainStory,
                        "Siege Prep: Cook Battle Food",
                        "Cook 3 combat buff dishes at the Kitchen Stove",
                        3,
                        NarrativeFactKey.MealsCookedCount);
                    break;

                case 5:
                    objService.AssignStoryObjective(
                        "OBJ_D5_PURGE_VALVE",
                        NarrativeObjectiveCategory.MainStory,
                        "Final Stand: Purge Conduit",
                        "Defeat The Ranger and purge subterranean Reagent 7",
                        1,
                        NarrativeFactKey.TheRangerSlayed);
                    break;
            }
        }

        private void HandleMerchantOpened()
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.MerchantOpen, currentDay);
        }

        private void HandleTrophyInspected()
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.TrophyInspected, currentDay);
        }

        private void HandleCookingCompleted()
        {
            if (_facts != null)
            {
                int meals = _facts.GetFact(NarrativeFactKey.MealsCookedCount);
                ServiceLocator.Resolve<INarrativeObjectiveService>()?.UpdateObjectiveProgress("OBJ_D4_BATTLE_PREP", meals);
            }
        }

        private void HandleCropWithered()
        {
            _facts?.AddFact(NarrativeFactKey.DeadCropsCount, 1);
            ServiceLocator.Resolve<INarrativeObjectiveService>()?.ReportProgress(NarrativeFactKey.DeadCropsCount, 1);
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
            if (!string.IsNullOrEmpty(cropName) && cropName.IndexOf("Taro", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _facts?.AddFact(NarrativeFactKey.WashedCount_Taro, count);
                int washed = _facts != null ? _facts.GetFact(NarrativeFactKey.WashedCount_Taro) : 0;
                ServiceLocator.Resolve<INarrativeObjectiveService>()?.UpdateObjectiveProgress("OBJ_D2_ARMORED_TARO", washed);
            }
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
            ServiceLocator.Resolve<INarrativeObjectiveService>()?.CompleteStoryObjective("OBJ_D3_WORKBENCH");
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
            if (_facts != null && _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) == 0)
            {
                FlushPendingModalBeats();
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

                if (enemy.enemyType == EnemyType.TheRanger)
                {
                    _facts?.SetFact(NarrativeFactKey.TheRangerSlayed, 1);
                    ServiceLocator.Resolve<INarrativeObjectiveService>()?.CompleteStoryObjective("OBJ_D5_PURGE_VALVE");
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

        private void HandleSoilTilled(FarmlandTile tile)
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.FirstTill, currentDay);
        }

        public void HandleCropPlanted(SeedItemData seed)
        {
            HandleCropPlanted(null, seed);
        }

        public void HandleCropPlanted(FarmlandTile tile, SeedItemData seed)
        {
            int currentDay = GetCurrentGameDay();

            if (tile != null)
            {
                UpdatePlantedCropCounts();
            }
            else if (_facts != null && seed != null)
            {
                string name = seed.itemName;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.IndexOf("SweetPotato", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Sweet Potato", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("sweet_potato", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _facts.SetFact(NarrativeFactKey.PlantedCount_SweetPotato, _facts.GetFact(NarrativeFactKey.PlantedCount_SweetPotato) + 1);
                    }
                    else if (name.IndexOf("Taro", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _facts.SetFact(NarrativeFactKey.PlantedCount_Taro, _facts.GetFact(NarrativeFactKey.PlantedCount_Taro) + 1);
                    }
                    else if (name.IndexOf("Corn", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _facts.SetFact(NarrativeFactKey.PlantedCount_Corn, _facts.GetFact(NarrativeFactKey.PlantedCount_Corn) + 1);
                    }
                }
            }
            else
            {
                UpdatePlantedCropCounts();
            }

            int sp = _facts != null ? _facts.GetFact(NarrativeFactKey.PlantedCount_SweetPotato) : 0;
            var objService = ServiceLocator.Resolve<INarrativeObjectiveService>();
            objService?.UpdateObjectiveProgress("OBJ_D1_FIRST_STEPS", sp);
            objService?.ReportProgress(NarrativeFactKey.PlantedCount_SweetPotato, 1);

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

            // Gating Invariant 1: Day/Night Environmental Transition & Cutscene Gating
            // When DayTransitionUI is actively playing its cinematic day/night transition,
            // or when an in-engine/timeline cutscene is active, all story deliveries (modal and non-modal)
            // must be safely deferred to avoid visual clipping, text overlapping, and player input locking contention.
            bool isTransitioning = DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsPlayingTransition;
            bool isCutscene = IsCutsceneActive();
            if (isTransitioning || isCutscene)
            {
                if (isTransitioning && DayTransitionUI.Instance != null)
                {
                    DayTransitionUI.Instance.OnTransitionCompleted -= HandleDayTransitionCompleted;
                    DayTransitionUI.Instance.OnTransitionCompleted += HandleDayTransitionCompleted;
                }
                if (isCutscene)
                {
                    SubscribeCutsceneEnded();
                }

                bool isNonModalItem = beat.deliveryType == StoryDeliveryType.HudBanner ||
                                      beat.deliveryType == StoryDeliveryType.FloatingWorldText;

                if (isNonModalItem)
                {
                    _pendingNonModalBeats.Enqueue(beat);
                    Debug.Log($"[StorylineManager] ⏳ Gating Invariant: Deferred non-modal story beat '{beat.beatId}' ({beat.title}) until transition/cutscene finishes.");
                }
                else
                {
                    _engine.EnqueuePendingModalBeat(beat);
                    Debug.Log($"[StorylineManager] ⏳ Gating Invariant: Deferred modal story beat '{beat.beatId}' ({beat.title}) until transition/cutscene finishes.");
                }
                return;
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
            // Modal beats (DialogueBox, JournalPage, FullScreenOverlay) must ONLY be deferred to _pendingModalBeats
            // when ActiveCombatThreatCount > 0. Whenever ActiveCombatThreatCount == 0 (including outdoors in the farmland
            // during the day or immediately after clearing a night wave), modal beats display immediately on screen.
            int threats = _facts != null ? _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) : 0;

            if (threats > 0)
            {
                _engine.EnqueuePendingModalBeat(beat);
                Debug.Log($"[StorylineManager] 🛡️ Hard Safety Invariant: Deferred modal beat '{beat.beatId}' to safe zone queue (Threats: {threats}).");

                var ui = StorylineUI.Instance ?? FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
                if (ui != null)
                {
                    ui.ShowBanner("Archival Log Discovered", "New dossier recovered. Read safely after combat.", 3.5f);
                }
                return;
            }

            // Safe state: deliver immediately
            DispatchToUI(beat);
        }

        /// <summary>
        /// Returns true if any cinematic cutscene (Opening or Night 1) is currently playing.
        /// Used by Storyline and Narrative subsystems to defer modal/dialogue presentations until the cutscene ends.
        /// </summary>
        public bool IsCutsceneActive()
        {
            if (ServiceLocator.TryResolve<ICutsceneService>(out var cutscene) && cutscene != null && cutscene.IsCutscenePlaying)
            {
                return true;
            }
            if (OpeningCutsceneController.HasInstance && OpeningCutsceneController.Instance.IsCutscenePlaying)
            {
                return true;
            }
            if (Night1CutsceneController.HasInstance && Night1CutsceneController.Instance.IsCutscenePlaying)
            {
                return true;
            }
            return false;
        }

        private void SubscribeCutsceneEnded()
        {
            if (ServiceLocator.TryResolve<ICutsceneService>(out var cutsceneService) && cutsceneService != null)
            {
                cutsceneService.OnCutsceneEnded -= HandleCutsceneEnded;
                cutsceneService.OnCutsceneEnded += HandleCutsceneEnded;
            }
            if (Night1CutsceneController.HasInstance && Night1CutsceneController.Instance != null)
            {
                Night1CutsceneController.Instance.OnCutsceneEnded -= HandleCutsceneEnded;
                Night1CutsceneController.Instance.OnCutsceneEnded += HandleCutsceneEnded;
            }
            if (OpeningCutsceneController.HasInstance && OpeningCutsceneController.Instance != null)
            {
                OpeningCutsceneController.Instance.OnCutsceneEnded -= HandleCutsceneEnded;
                OpeningCutsceneController.Instance.OnCutsceneEnded += HandleCutsceneEnded;
            }
        }

        private void UnsubscribeCutsceneEnded()
        {
            if (ServiceLocator.TryResolve<ICutsceneService>(out var cutsceneService) && cutsceneService != null)
            {
                cutsceneService.OnCutsceneEnded -= HandleCutsceneEnded;
            }
            if (Night1CutsceneController.HasInstance && Night1CutsceneController.Instance != null)
            {
                Night1CutsceneController.Instance.OnCutsceneEnded -= HandleCutsceneEnded;
            }
            if (OpeningCutsceneController.HasInstance && OpeningCutsceneController.Instance != null)
            {
                OpeningCutsceneController.Instance.OnCutsceneEnded -= HandleCutsceneEnded;
            }
        }

        private void HandleCutsceneEnded()
        {
            // 1. Flush non-modal beats deferred during cutscene
            while (_pendingNonModalBeats.Count > 0)
            {
                var nonModalBeat = _pendingNonModalBeats.Dequeue();
                if (nonModalBeat != null)
                {
                    DispatchToUI(nonModalBeat);
                }
            }

            // 2. Evaluate Dawn beat ONLY on Day 1+ when narrative facts indicate cutscene completed
            int currentDay = GetCurrentGameDay();
            if (currentDay >= 1 && !HasTriggered("D1_MANIFEST_FOUND") && _facts != null && _facts.GetFact(NarrativeFactKey.IntroCutsceneCompleted) == 1)
            {
                EvaluateTrigger(StoryTriggerType.Dawn, currentDay);
            }

            // 3. Flush pending modal beats if combat state is safe and no cutscene is active
            int threats = _facts != null ? _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) : 0;
            if (threats == 0 && !IsCutsceneActive())
            {
                FlushPendingModalBeats();
            }
        }

        public void FlushPendingModalBeats()
        {
            // Cannot flush if environmental transition is currently running
            if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsPlayingTransition)
            {
                return;
            }

            if (IsCutsceneActive())
            {
                return;
            }

            int threats = _facts != null ? _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) : 0;

            // Cannot flush if still in combat
            if (threats > 0) return;

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

        private void HandleDayTransitionCompleted()
        {
            int currentDay = GetCurrentGameDay();
            bool isNight1Pending = currentDay == 1 && (_facts == null || _facts.GetFact(NarrativeFactKey.Night1CutsceneCompleted) == 0);

            // 1. Flush non-modal beats deferred during environmental transition (if no cutscene running or pending)
            if (!IsCutsceneActive() && !isNight1Pending)
            {
                while (_pendingNonModalBeats.Count > 0)
                {
                    var nonModalBeat = _pendingNonModalBeats.Dequeue();
                    if (nonModalBeat != null)
                    {
                        DispatchToUI(nonModalBeat);
                    }
                }
            }

            // 2. Flush pending modal beats if combat state is safe, no cutscene is active, and Night 1 cutscene is not pending
            int threats = _facts != null ? _facts.GetFact(NarrativeFactKey.ActiveCombatThreatCount) : 0;
            if (threats == 0 && !IsCutsceneActive() && !isNight1Pending)
            {
                FlushPendingModalBeats();
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

            Action onBeatComplete = null;
            if (string.Equals(beat.beatId, "D5_VICTORY", StringComparison.OrdinalIgnoreCase))
            {
                onBeatComplete = () =>
                {
                    TriggerBeat("EPILOGUE_STATS");
                };
            }

            Action chainedBeatComplete = () =>
            {
                onBeatComplete?.Invoke();
                if (_engine != null && _engine.HasPendingModalBeats)
                {
                    FlushPendingModalBeats();
                }
            };

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
                        duration: beat.displayDuration,
                        onComplete: chainedBeatComplete
                    );
                    return;
                }
            }

            var targetUI = StorylineUI.Instance;
            if (targetUI == null)
            {
                targetUI = FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
            }

            if (targetUI != null)
            {
                if (!targetUI.gameObject.activeInHierarchy) targetUI.gameObject.SetActive(true);
                targetUI.enabled = true;
                targetUI.DisplayBeat(beat, chainedBeatComplete);
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

        public void ApplyDay5Atmosphere()
        {
            ApplyAtmosphericEffects(5);
        }

        private void ApplyAtmosphericEffects(int day)
        {
            if (!overrideDay5Atmosphere) return;

            CacheBaselineAtmosphere();

            if (day >= 5)
            {
                RenderSettings.ambientLight = day5AmbientColor;
                _isAtmosphereOverridden = true;
                Debug.Log("[StorylineManager] 🩸 Applied Day 5 Crimson Sky atmosphere.");
            }
            else if (_isAtmosphereOverridden)
            {
                RestoreAtmosphere();
            }
        }

        private int GetCurrentGameDay()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
                return timeService.CurrentDay;
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
            RestoreState(chapter, beats, journals, isComplete, rawFacts, lastPlayedKeys, lastPlayedDays, null);
        }

        public void RestoreState(
            int chapter,
            IEnumerable<string> beats,
            IEnumerable<string> journals,
            bool isComplete,
            int[] rawFacts,
            IList<string> lastPlayedKeys,
            IList<int> lastPlayedDays,
            IList<string> pendingModalBeatIds)
        {
            var payload = new StorylineSavePayload(chapter, beats, journals, isComplete, rawFacts);
            if (lastPlayedKeys != null && lastPlayedDays != null)
            {
                payload.lastPlayedBeatKeys = new List<string>(lastPlayedKeys);
                payload.lastPlayedDays = new List<int>(lastPlayedDays);
            }
            if (pendingModalBeatIds != null)
            {
                payload.pendingModalBeatIds = new List<string>(pendingModalBeatIds);
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

        public void ExportPendingModals(out List<string> pendingBeats)
        {
            var payload = _engine.CreateSavePayload(_facts);
            pendingBeats = new List<string>(payload.pendingModalBeatIds);
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

        /// <summary>
        /// Resets the engine, narrative facts, and journal for fresh test or session state.
        /// </summary>
        public void ResetStorylineSession()
        {
            _engine?.Reset();
            _facts?.ResetFacts();
            _facts?.SetFact(NarrativeFactKey.CurrentDay, 1);
            _facts?.SetFact(NarrativeFactKey.CurrentDayPhase, 0);
            var jUI = JournalUI.Instance ?? FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
            if (jUI != null)
            {
                jUI.SetEntries(journalEntries, _engine != null ? _engine.UnlockedJournalIds : null);
            }
        }

        #endregion
    }
}
