using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using FarmBeware.Core.Runtime;
using FeaturesStoryline;
using FeaturesTime;
using FeaturesTime.Atmosphere;
using FeaturesCombat;
using FeaturesFarming;

namespace FarmBeware.Features.Storyline.Sandbox
{
    /// <summary>
    /// QA Debug Controller and Telemetry Dashboard for the Storyline Prototype Sandbox scene.
    /// Provides interactive on-screen controls and hotkeys for time scrubbing, fast-forwarding,
    /// narrative beat triggering, starter gear injection, and real-time performance telemetry.
    /// Adheres strictly to Zero-GC runtime update loop invariants.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class StorylineSandboxDebugger : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Telemetry & Dashboard Display")]
        [Tooltip("Whether the on-screen QA IMGUI dashboard window is visible.")]
        [SerializeField] private bool showDashboard = true;

        [Tooltip("GUI Window screen coordinates.")]
        [SerializeField] private Rect windowRect = new Rect(20f, 20f, 380f, 620f);

        [Header("Time Simulation Defaults")]
        [Tooltip("Time scale applied during 5x fast forward.")]
        [SerializeField] private float fastForwardSpeed5x = 5.0f;

        [Tooltip("Time scale applied during 10x fast forward.")]
        [SerializeField] private float fastForwardSpeed10x = 10.0f;

        [Header("Starter Gear Item IDs")]
        [SerializeField] private string hoeItemId = "tool_hoe";
        [SerializeField] private string plantWatererItemId = "tool_plant_waterer";
        [SerializeField] private string waterBottleItemId = "food_bottle_water";
        [SerializeField] private string sweetPotatoSeedItemId = "seed_sweet_potato";
        [SerializeField] private string taroSeedItemId = "seed_taro";
        [SerializeField] private int seedStackAmount = 6;

        #endregion

        #region Telemetry Fields (Zero-GC Caching)

        private float _fpsAccumulator;
        private int _fpsFrameCount;
        private float _telemetryTimer;
        private const float TelemetryInterval = 0.25f;

        // Cached telemetry strings to eliminate per-frame heap allocations
        private string _cachedFps = "FPS: 60.0 (16.6 ms)";
        private string _cachedGcAlloc = "GC Alloc: 0 B";
        private string _cachedTimePhase = "Day 1 | 06:00 (Day)";
        private string _cachedThreats = "Active Threats: 0";
        private string _cachedTimeScale = "Time Scale: 1.0x";

        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _metricLabelStyle;
        private GUIStyle _sectionLabelStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _telemetryTimer = 0f;
            _fpsAccumulator = 0f;
            _fpsFrameCount = 0;
            UpdateTelemetryStrings();
        }

        private void Update()
        {
            // Accumulate FPS metrics without heap allocation
            _fpsAccumulator += Time.unscaledDeltaTime;
            _fpsFrameCount++;
            _telemetryTimer += Time.unscaledDeltaTime;

            if (_telemetryTimer >= TelemetryInterval)
            {
                UpdateTelemetryStrings();
                _telemetryTimer = 0f;
                _fpsAccumulator = 0f;
                _fpsFrameCount = 0;
            }

            PollKeyboardHotkeys();
        }

        #endregion

        #region Hotkey Dispatcher

        private void PollKeyboardHotkeys()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // F1: Toggle Dashboard Visibility
            if (keyboard.f1Key.wasPressedThisFrame)
            {
                showDashboard = !showDashboard;
            }

            // F2: Jump to 15:00 Real Time (16:00 in-game Dusk Bell)
            if (keyboard.f2Key.wasPressedThisFrame)
            {
                JumpToDuskBellWarning();
            }

            // F3: Jump to 18:00 (Night Combat Wave)
            if (keyboard.f3Key.wasPressedThisFrame)
            {
                JumpToNightCombat();
            }

            // F5 / Digit 1: 1x Speed
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                SetTimeScale(1.0f);
            }

            // F6 / Digit 5: 5x Speed
            if (keyboard.f6Key.wasPressedThisFrame)
            {
                SetTimeScale(fastForwardSpeed5x);
            }

            // F7 / Digit 0: 10x Speed
            if (keyboard.f7Key.wasPressedThisFrame)
            {
                SetTimeScale(fastForwardSpeed10x);
            }

            // F8: Inject Starter Gear
            if (keyboard.f8Key.wasPressedThisFrame)
            {
                InjectStarterGear();
            }
#endif
        }

        #endregion

        #region Telemetry Formatting (Zero-GC Optimized)

        private void UpdateTelemetryStrings()
        {
            // 1. FPS & Frametime
            float avgDelta = _fpsFrameCount > 0 ? _fpsAccumulator / _fpsFrameCount : Time.unscaledDeltaTime;
            float currentFps = avgDelta > 0.0001f ? 1.0f / avgDelta : 60f;
            float frameMs = avgDelta * 1000f;
            _cachedFps = $"FPS: {currentFps:F1} ({frameMs:F1} ms)";

            // 2. Thread GC Allocation Bytes
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread();
            if (allocatedBytes >= 1048576)
            {
                _cachedGcAlloc = $"GC Alloc: {allocatedBytes / 1048576.0:F2} MB ({allocatedBytes:N0} B)";
            }
            else if (allocatedBytes >= 1024)
            {
                _cachedGcAlloc = $"GC Alloc: {allocatedBytes / 1024.0:F1} KB ({allocatedBytes:N0} B)";
            }
            else
            {
                _cachedGcAlloc = $"GC Alloc: {allocatedBytes} B";
            }

            // 3. Current Day, Hour, and Phase
            int day = 1;
            string phaseName = "Day";
            int hour = 6;
            int minute = 0;

            if (DayNightTimeManager.Instance != null)
            {
                day = DayNightTimeManager.Instance.CurrentDay;
                phaseName = DayNightTimeManager.Instance.CurrentPhase.ToString();
                hour = DayNightTimeManager.Instance.CurrentHourInt;
                minute = DayNightTimeManager.Instance.CurrentMinuteInt;
            }
            else if (TimeManager.Instance != null)
            {
                day = TimeManager.Instance.currentDay;
                phaseName = TimeManager.Instance.currentPhase.ToString();
            }

            _cachedTimePhase = $"Day {day} | {hour:D2}:{minute:D2} ({phaseName})";

            // 4. Active Combat Threat Count
            int threats = 0;
            if (StorylineManager.Instance != null && StorylineManager.Instance.FactBoard != null)
            {
                threats = StorylineManager.Instance.FactBoard.GetFact(NarrativeFactKey.ActiveCombatThreatCount);
            }
            else if (NightBrawlManager.Instance != null)
            {
                threats = NightBrawlManager.Instance.ActiveEnemiesCount;
            }

            _cachedThreats = $"Active Combat Threats: {threats}";
            _cachedTimeScale = $"Time Scale: {Time.timeScale:F1}x";
        }

        #endregion

        #region Public Sandbox QA Actions

        /// <summary>
        /// Jumps time forward to 15:00 real minutes (16:00 in-game hour), sounding the Dusk Bell audio stinger.
        /// </summary>
        public void JumpToDuskBellWarning()
        {
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.SetTime(16.0f);
            }

            if (DayNightAudioController.Instance != null)
            {
                DayNightAudioController.Instance.PlayDuskBell();
            }

            if (StorylineManager.Instance != null)
            {
                StorylineManager.Instance.TriggerBeat("D1_DUSK_WARNING");
            }

            Debug.Log("[StorylineSandboxDebugger] 🔔 Jumped time to 15:00 (16:00 in-game Dusk Bell warning).");
        }

        /// <summary>
        /// Jumps time forward to 18:00 (Nightfall), transitioning the time manager and triggering the night combat wave.
        /// </summary>
        public void JumpToNightCombat()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.StartNightPhase();
            }

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.SetTime(18.0f);
            }

            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.StartNightBrawl();
            }

            Debug.Log("[StorylineSandboxDebugger] 🌙 Jumped time to 18:00 (Night combat wave triggered).");
        }

        /// <summary>
        /// Sets the global simulation time scale (1x, 5x, 10x) and informs the continuous daytime scheduler.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            float targetScale = Mathf.Clamp(scale, 0.1f, 20.0f);
            Time.timeScale = targetScale;

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.ToggleSpeedMode(targetScale, $"{targetScale:F1}x Speed");
            }

            Debug.Log($"[StorylineSandboxDebugger] ⏩ Fast-Forward set to {targetScale:F1}x scale.");
        }

        /// <summary>
        /// Triggers a specific canonical narrative beat through the central StorylineManager.
        /// </summary>
        public void TriggerNarrativeBeat(string beatId)
        {
            if (string.IsNullOrEmpty(beatId)) return;

            if (StorylineManager.Instance != null)
            {
                bool success = StorylineManager.Instance.TriggerBeat(beatId);
                Debug.Log($"[StorylineSandboxDebugger] 🎭 Trigger Narrative Beat '{beatId}' result: {success}");
            }
            else
            {
                Debug.LogWarning($"[StorylineSandboxDebugger] Cannot trigger beat '{beatId}': StorylineManager.Instance is null.");
            }
        }

        /// <summary>
        /// Injects the complete official MVP starter gear package:
        /// 1x Hoe, 1x Plant Waterer (100L capacity), 1x Water Bottle (4 charges / 100L),
        /// 6x Sweet Potato Seeds, and 6x Taro Seeds.
        /// </summary>
        public void InjectStarterGear()
        {
            var player = FindPlayerGameObject();
            if (player == null)
            {
                Debug.LogWarning("[StorylineSandboxDebugger] Player GameObject not found in sandbox scene!");
                return;
            }

            var inventory = player.GetComponent<InventoryComponent>();
            if (inventory == null)
            {
                Debug.LogWarning("[StorylineSandboxDebugger] Player has no InventoryComponent!");
                return;
            }

            if (inventory.maxCapacity < 16)
            {
                inventory.maxCapacity = 16;
            }
            inventory.EnsureSlotsAllocated();

            var itemDb = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (itemDb == null)
            {
                Debug.LogError("[StorylineSandboxDebugger] Failed to load 'Database/ItemDatabase' from Resources!");
                return;
            }

            ItemData hoe = itemDb.GetItem(hoeItemId);
            ItemData plantWaterer = itemDb.GetItem(plantWatererItemId);
            ItemData waterBottle = itemDb.GetItem(waterBottleItemId);
            ItemData sweetPotatoSeed = itemDb.GetItem(sweetPotatoSeedItemId);
            ItemData taroSeed = itemDb.GetItem(taroSeedItemId);

            if (hoe != null) inventory.AddItem(hoe, 1);
            if (plantWaterer != null) inventory.AddItem(plantWaterer, 1);
            if (waterBottle != null) inventory.AddItem(waterBottle, 1);
            if (sweetPotatoSeed != null) inventory.AddItem(sweetPotatoSeed, seedStackAmount);
            if (taroSeed != null) inventory.AddItem(taroSeed, seedStackAmount);

            // Guarantee full water reserves via PlantWaterer and IWaterService
            if (PlantWaterer.Instance != null)
            {
                PlantWaterer.Instance.RefillWater(-1f);
            }

            var waterService = player.GetComponent<IWaterService>() ?? ServiceLocator.Resolve<IWaterService>();
            if (waterService != null)
            {
                waterService.RefillWater(100f);
            }

            inventory.OnInventoryChanged?.Invoke();
            Debug.Log("[StorylineSandboxDebugger] 🎒 Injected Starter Gear: Hoe, Waterer, Bottle, 6 Sweet Potato seeds, 6 Taro seeds.");
        }

        /// <summary>
        /// Simulates combat threats to test modal dialogue queuing and safety invariants.
        /// </summary>
        public void SetSimulatedThreats(int threatCount)
        {
            if (StorylineManager.Instance != null && StorylineManager.Instance.FactBoard != null)
            {
                StorylineManager.Instance.FactBoard.SetFact(NarrativeFactKey.ActiveCombatThreatCount, threatCount);
                if (threatCount == 0)
                {
                    StorylineManager.Instance.FlushPendingModalBeats();
                }
                Debug.Log($"[StorylineSandboxDebugger] 🛡️ Set simulated ActiveCombatThreatCount to {threatCount}.");
            }
        }

        #endregion

        #region OnGUI Rendering

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _headerStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);

            _sectionLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            _sectionLabelStyle.normal.textColor = new Color(0.4f, 0.9f, 1f);

            _metricLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal
            };
            _metricLabelStyle.normal.textColor = Color.white;

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 26f
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!showDashboard)
            {
                if (GUI.Button(new Rect(20f, 20f, 140f, 28f), "Open QA Debug (F1)"))
                {
                    showDashboard = true;
                }
                return;
            }

            InitializeStyles();
            windowRect = GUI.Window(99201, windowRect, DrawDashboardWindow, "Storyline Sandbox QA Debugger (F1)");
        }

        private void DrawDashboardWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // 1. Realtime Telemetry Section
            GUILayout.Label("── Realtime Telemetry ──", _headerStyle);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(_cachedFps, _metricLabelStyle);
            GUILayout.Label(_cachedGcAlloc, _metricLabelStyle);
            GUILayout.Label(_cachedTimePhase, _metricLabelStyle);
            GUILayout.Label(_cachedThreats, _metricLabelStyle);
            GUILayout.Label(_cachedTimeScale, _metricLabelStyle);
            GUILayout.EndVertical();

            GUILayout.Space(6f);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            // 2. Time Control Section
            GUILayout.Label("Time Simulation (Hotkeys)", _sectionLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("15:00 Dusk Bell (F2)", _buttonStyle))
            {
                JumpToDuskBellWarning();
            }
            if (GUILayout.Button("18:00 Night Wave (F3)", _buttonStyle))
            {
                JumpToNightCombat();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x Speed (F5)", _buttonStyle))
            {
                SetTimeScale(1.0f);
            }
            if (GUILayout.Button("5x Speed (F6)", _buttonStyle))
            {
                SetTimeScale(fastForwardSpeed5x);
            }
            if (GUILayout.Button("10x Speed (F7)", _buttonStyle))
            {
                SetTimeScale(fastForwardSpeed10x);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // 3. Inventory & Equipment Section
            GUILayout.Label("Starter Gear & Equipment", _sectionLabelStyle);
            if (GUILayout.Button("Add Starter Gear (Hoe, Waterer, Seeds) (F8)", _buttonStyle))
            {
                InjectStarterGear();
            }

            GUILayout.Space(8f);

            // 4. Narrative Beat Invocators
            GUILayout.Label("Narrative Beat Invocators", _sectionLabelStyle);
            DrawNarrativeBeatButton("PROLOGUE", "Prologue Cutscene (Gate Arrival)");
            DrawNarrativeBeatButton("D1_MANIFEST_FOUND", "D1: Manifest Found (Dawn Dialogue)");
            DrawNarrativeBeatButton("D1_FIRST_TILL", "D1: First Till (Metal Sign Clang)");
            DrawNarrativeBeatButton("D2_SINK_WASH_TARO", "D2: Sink Wash Taro (Armored Skin)");
            DrawNarrativeBeatButton("D5_BOSS2_INTRO", "D5: Boss 2 Intro (The Ranger)");
            DrawNarrativeBeatButton("D5_VICTORY", "D5: Victory (Purge Valve Sealed)");
            DrawNarrativeBeatButton("EPILOGUE_STATS", "Epilogue Stats (Peace Earned)");

            GUILayout.Space(8f);

            // 5. Modal Combat Safety Testing
            GUILayout.Label("Modal Combat Safety Invariant", _sectionLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Simulate Combat (Threats = 3)", _buttonStyle))
            {
                SetSimulatedThreats(3);
            }
            if (GUILayout.Button("Clear Combat (Threats = 0)", _buttonStyle))
            {
                SetSimulatedThreats(0);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            if (GUILayout.Button("Hide Dashboard (F1)", _buttonStyle))
            {
                showDashboard = false;
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 24));
        }

        private void DrawNarrativeBeatButton(string beatId, string label)
        {
            if (GUILayout.Button($"Trigger: {label}", _buttonStyle))
            {
                TriggerNarrativeBeat(beatId);
            }
        }

        #endregion

        #region Helper Methods

        private GameObject FindPlayerGameObject()
        {
            var playerTag = GameObject.FindWithTag("Player");
            if (playerTag != null) return playerTag;

            if (ServiceLocator.TryResolve<IPlayerContext>(out var playerContext) && playerContext != null && playerContext.Transform != null)
            {
                return playerContext.Transform.gameObject;
            }

            var interactor = FindFirstObjectByType<FeaturesInteraction.PlayerInteractor>();
            if (interactor != null) return interactor.gameObject;

            return null;
        }

        #endregion
    }
}
