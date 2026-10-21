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
    /// Subscribes to time progression, combat wave milestones, boss deaths, and farm activities.
    /// Dispatches narrative delivery to StorylineUI, BossIntroSequence, and JournalUI.
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
                Instance = null;
            }
        }

        private void Start()
        {
            // Evaluate GameStart trigger (Prologue) on Day 1 start
            int currentDay = GetCurrentGameDay();
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

            // 3. Combat Waves
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
            FeaturesFarming.FarmlandTile.OnAnyCropPlanted += HandleCropPlanted;
            FeaturesFarming.FarmlandTile.OnAnyCropHarvested += HandleCropHarvested;
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
            FeaturesFarming.FarmlandTile.OnAnyCropPlanted -= HandleCropPlanted;
            FeaturesFarming.FarmlandTile.OnAnyCropHarvested -= HandleCropHarvested;
        }

        #region Event Handlers

        private void HandleTimePhaseChanged(EnvironmentPhase phase)
        {
            int currentDay = GetCurrentGameDay();

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

        private void HandleDayChanged(int newDay)
        {
            _engine.SetChapter(newDay);
            ApplyAtmosphericEffects(newDay);
        }

        private void HandleWaveStarted(int day, int waveIndex)
        {
            EvaluateTrigger(StoryTriggerType.WaveStart, day, waveIndex);
        }

        private void HandleAllWavesCleared(int day)
        {
            EvaluateTrigger(StoryTriggerType.WaveCleared, day);
        }

        private void HandleEnemySpawned(EnemyBase enemy)
        {
            if (enemy == null) return;

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

            if (enemy.isBoss || FeaturesCombat.Wave.WaveProgressionEngine.IsBossType(enemy.enemyType))
            {
                int currentDay = GetCurrentGameDay();
                int currentWave = NightBrawlManager.Instance != null ? NightBrawlManager.Instance.CurrentWave : 1;
                EvaluateTrigger(StoryTriggerType.BossDeath, currentDay, currentWave, enemy.enemyType);

                // If The Ranger was defeated on Day 5, trigger Epilogue
                if (enemy.enemyType == EnemyType.TheRanger && currentDay >= 5)
                {
                    _engine.CompleteStory();
                    EvaluateTrigger(StoryTriggerType.Custom, currentDay);
                }
            }
        }

        private void HandleBedSleep()
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.Sleep, currentDay);
        }

        private void HandleCropPlanted(FeaturesFarming.FarmlandTile tile, SeedItemData seed)
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.FirstPlant, currentDay);
        }

        private void HandleCropHarvested(FeaturesFarming.FarmlandTile tile, ItemData crop, int count)
        {
            int currentDay = GetCurrentGameDay();
            EvaluateTrigger(StoryTriggerType.FirstHarvest, currentDay);
        }

        #endregion

        #region Trigger Evaluation & Delivery

        /// <summary>
        /// Evaluates all beats matching trigger condition and fires any valid ones.
        /// </summary>
        public void EvaluateTrigger(StoryTriggerType triggerType, int day, int wave = 0, EnemyType? bossType = null)
        {
            InitializeContent();

            for (int i = 0; i < storyBeats.Count; i++)
            {
                var beat = storyBeats[i];
                if (beat == null) continue;

                if (_engine.CanTrigger(beat, triggerType, day, wave, bossType))
                {
                    ExecuteStoryBeat(beat);
                }
            }
        }

        private void ExecuteStoryBeat(StoryBeatData beat)
        {
            if (!_engine.RecordBeatTriggered(beat)) return;

            Debug.Log($"[StorylineManager] 🎭 Triggered Story Beat: '{beat.beatId}' ({beat.title}) - Delivery: {beat.deliveryType}");

            // Dispatch to BossIntroSequence if BossIntro delivery
            if (beat.deliveryType == StoryDeliveryType.BossIntro)
            {
                if (BossIntroSequence.Instance != null)
                {
                    BossIntroSequence.Instance.PlayBossIntro(
                        beat.title,
                        beat.speakerName,
                        beat.narrativeText,
                        duration: beat.displayDuration
                    );
                    return;
                }
            }

            // Otherwise dispatch to StorylineUI
            var targetUI = StorylineUI.Instance;
            if (targetUI == null)
            {
                targetUI = FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
            }

            if (targetUI != null)
            {
                targetUI.DisplayBeat(beat);
            }
            else
            {
                // Fallback: spawn StorylineUI dynamically if missing
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
                ui.DisplayBeat(beat);
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
                    ExecuteStoryBeat(beat);
                    return true;
                }
            }

            // Fallback direct record if not in catalog
            return _engine.RecordBeatTriggered(beatId);
        }

        public void ShowJournal()
        {
            if (JournalUI.Instance != null)
            {
                JournalUI.Instance.OpenJournal();
            }
            else
            {
                var jGO = new GameObject("JournalUI", typeof(JournalUI));
                var j = jGO.GetComponent<JournalUI>();
                j.OpenJournal();
            }
        }

        public void HideJournal()
        {
            if (JournalUI.Instance != null)
            {
                JournalUI.Instance.CloseJournal();
            }
        }

        public void RestoreState(int chapter, IEnumerable<string> beats, IEnumerable<string> journals, bool isComplete)
        {
            var payload = new StorylineSavePayload(chapter, beats, journals, isComplete);
            _engine.ImportState(payload);

            if (JournalUI.Instance != null)
            {
                JournalUI.Instance.SetEntries(journalEntries, _engine.UnlockedJournalIds);
            }
        }

        /// <summary>
        /// Registers a new story beat at runtime for dynamic feature extensions.
        /// </summary>
        public void RegisterStoryBeat(StoryBeatData beat)
        {
            if (beat == null || storyBeats.Contains(beat)) return;
            storyBeats.Add(beat);
        }

        /// <summary>
        /// Registers a new journal entry at runtime for dynamic feature extensions.
        /// </summary>
        public void RegisterJournalEntry(JournalEntryData entry)
        {
            if (entry == null || journalEntries.Contains(entry)) return;
            journalEntries.Add(entry);
        }

        #endregion
    }
}
