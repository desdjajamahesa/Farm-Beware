using System;
using UnityEngine;
using FeaturesRendering.Lighting;

namespace FeaturesTime
{
    /// <summary>
    /// Manajer simulasi waktu lingkungan 24 jam yang independen dan decoupled.
    /// Mematuhi Clean Architecture, SOLID (SRP & IoC), dan Event-Driven OOP.
    ///
    /// PENTING (Prinsip Desain):
    /// Komponen ini murni mengelola matematika progresi waktu dan pemancaran event.
    /// SAMA SEKALI TIDAK memodifikasi parameter grafis, URP, Directional Light,
    /// atau RenderSettings di dalam siklus Update(). Seluruh penyesuaian visual
    /// dilakukan secara terisolasi oleh komponen Observer/Listener (Fase 2).
    /// </summary>
    [DisallowMultipleComponent]
    public class DayNightTimeManager : MonoBehaviour, IDayNightTimeService, FarmBeware.Core.Runtime.ITimeService
    {
        #region Singleton (Awake-Safe Pattern §1.1)

        private static DayNightTimeManager _instance;
        private static bool _isApplicationQuitting = false;

        public static DayNightTimeManager Instance
        {
            get
            {
                if (_isApplicationQuitting) return _instance;

                if (_instance == null)
                {
                    DayNightTimeManager[] found = FindObjectsByType<DayNightTimeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                    {
                        _instance = found[0];
                    }
                    else
                    {
                        var tm = FindFirstObjectByType<TimeManager>();
                        if (tm != null)
                        {
                            _instance = tm.gameObject.AddComponent<DayNightTimeManager>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        #endregion

        #region Serialized Configuration

        [Header("1. Time Progression Settings")]
        [Tooltip("Total durasi dunia nyata (dalam detik) untuk fase siang (06:00 s/d 18:00). Default 960 detik = 16 menit sesuai tabel linimasa.")]
        [SerializeField] private float daytimeDurationRealSeconds = 960f;

        [Tooltip("Total durasi dunia nyata (dalam detik) untuk menyelesaikan 1 hari permainan (24 jam in-game). Default 1920 detik = 32 menit.")]
        [SerializeField] private float realSecondsPerInGameDay = 1920f;

        [Tooltip("Jika true, simulasi waktu berjalan secara realtime kontinu pada siang hari sesuai linimasa (16 menit real = 12 jam in-game).")]
        [SerializeField] private bool useContinuousTime = true;

        [Tooltip("Jam awal saat game pertama kali dimulai (0.0f - 24.0f). Default 6.0f = 06:00.")]
        [Range(0f, 24f)]
        [SerializeField] private float initialHour = 6.0f;

        [Tooltip("Hari awal kalender saat game dimulai.")]
        [Min(1)]
        [SerializeField] private int initialDay = 1;

        [Tooltip("Apakah simulasi waktu otomatis berjalan saat Start().")]
        [SerializeField] private bool autoStart = true;

        [Header("2. Phase Thresholds (24h Clock)")]
        [Tooltip("Jam dimulainya Fajar (Dawn). Default: 5.0 (05:00).")]
        [Range(0f, 24f)]
        [SerializeField] private float dawnStartHour = 5.0f;

        [Tooltip("Jam dimulainya Siang (Day). Default: 6.0 (06:00).")]
        [Range(0f, 24f)]
        [SerializeField] private float dayStartHour = 6.0f;

        [Tooltip("Jam dimulainya Senja (Dusk / Dusk Bell). Default: 15.75 (15:45).")]
        [Range(0f, 24f)]
        [SerializeField] private float duskStartHour = 15.75f;

        [Tooltip("Jam dimulainya Malam / Auto-Sleep (Night). Default: 18.0 (18:00).")]
        [Range(0f, 24f)]
        [SerializeField] private float nightStartHour = 18.0f;

        [Header("3. Dusk Bell & Auto-Sleep Settings")]
        [Tooltip("Jam dibunyikannya lonceng senja (Dusk Bell). Default: 15.75 (15:45 in-game / menit real 13:30).")]
        [Range(0f, 24f)]
        [SerializeField] private float duskWarningHour = 15.75f;

        [Tooltip("Jika true, saat waktu siang mencapai jam 18:00 (16:00 menit real), sistem otomatis memicu tidur dan memulai Night Brawl.")]
        [SerializeField] private bool autoSleepAtNightfall = true;

        [Header("4. Integration Bridge")]
        [Tooltip("Jika true, menyinkronkan event fase dengan TimeManager legacy di project.")]
        [SerializeField] private bool syncWithLegacyTimeManager = true;

        /// <summary>
        /// Flag indicating if legacy time manager synchronization is enabled.
        /// </summary>
        public bool SyncWithLegacyTimeManager => syncWithLegacyTimeManager;

        /// <summary>
        /// Jam awal siang hari (Day start hour, default 6.0f / 06:00).
        /// </summary>
        public float DayStartHour => dayStartHour;

        #endregion

        #region State Fields

        [Header("Runtime Debug View (Read-Only)")]
        [SerializeField] private float currentHour = 6.0f;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private EnvironmentPhase currentPhase = EnvironmentPhase.Day;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private float daytimeElapsedSeconds = 0f;
        [SerializeField] private bool duskWarningTriggered = false;
        [SerializeField] private bool autoSleepTriggered = false;

        private int lastEmittedHour = -1;
        private int lastEmittedMinute = -1;
        private EnvironmentPhase lastEmittedPhase = (EnvironmentPhase)(-1);

        #endregion

        #region Events (IDayNightTimeService Implementation)

        public event Action<int> OnHourChanged;
        public event Action<int> OnMinuteChanged;
        public event Action<EnvironmentPhase> OnTimePhaseChanged;
        public event Action OnDuskWarning;
        public event Action OnAutoSleepTriggered;
        public event Action<int> OnDayChanged;
        public event Action<float> OnNormalizedTimeChanged;

        private Action<int, FarmBeware.Core.Runtime.DayPhase> _corePhaseChanged;
        event Action<int, FarmBeware.Core.Runtime.DayPhase> FarmBeware.Core.Runtime.ITimeService.OnPhaseChanged
        {
            add => _corePhaseChanged += value;
            remove => _corePhaseChanged -= value;
        }

        #endregion

        #region Public Properties

        public float CurrentHour => currentHour;
        public int CurrentHourInt => Mathf.FloorToInt(currentHour) % 24;
        public int CurrentMinuteInt => Mathf.FloorToInt((currentHour - Mathf.Floor(currentHour)) * 60f) % 60;
        public int CurrentDay => currentDay;
        public EnvironmentPhase CurrentPhase => currentPhase;
        public float NormalizedTime => Mathf.Clamp01(currentHour / 24.0f);
        public bool IsPaused => isPaused;
        public bool UseContinuousTime
        {
            get => useContinuousTime;
            set => useContinuousTime = value;
        }
        public float DaytimeDurationRealSeconds => daytimeDurationRealSeconds;
        public float DaytimeElapsedSeconds => daytimeElapsedSeconds;
        public float DaytimeRemainingSeconds => Mathf.Max(0f, daytimeDurationRealSeconds - daytimeElapsedSeconds);

        float FarmBeware.Core.Runtime.ITimeService.TimeOfDay => currentHour;
        FarmBeware.Core.Runtime.DayPhase FarmBeware.Core.Runtime.ITimeService.CurrentPhase => (FarmBeware.Core.Runtime.DayPhase)currentPhase;
        bool FarmBeware.Core.Runtime.ITimeService.IsNight => currentPhase == EnvironmentPhase.Night;
        bool FarmBeware.Core.Runtime.ITimeService.IsNightEncounterCleared => TimeManager.Instance != null ? TimeManager.Instance.isNightEncounterCleared : true;
        bool FarmBeware.Core.Runtime.ITimeService.UseContinuousTime => useContinuousTime;

        void FarmBeware.Core.Runtime.ITimeService.StartNightPhase()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.StartNightPhase();
            else
                SetTime(nightStartHour);
        }

        void FarmBeware.Core.Runtime.ITimeService.AdvanceToNextDay()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.AdvanceToNextDay();
            AdvanceToNextDay();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _isApplicationQuitting = false;
            Instance = this;
            FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(this);

            // Enforce daytime continuous progression & day boundaries
            useContinuousTime = true;
            dayStartHour = 6.0f;
            nightStartHour = 18.0f;
            duskStartHour = 15.75f;
            duskWarningHour = 15.75f;

            if (currentHour < 6.0f || (currentHour >= 6.99f && currentHour <= 7.25f))
            {
                currentHour = dayStartHour;
            }

            currentDay = initialDay;
            isPaused = !autoStart;
            daytimeElapsedSeconds = EvaluateElapsedSecondsFromHour(currentHour, daytimeDurationRealSeconds);
            currentPhase = EvaluatePhase(currentHour);
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                currentDay = TimeManager.Instance.currentDay;
                if (TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
                {
                    SetTime(nightStartHour);
                }
                else
                {
                    // If currentHour is uninitialized or legacy 7.0f-7.25f, align to dayStartHour (06:00)
                    if (currentHour < 6.0f || (currentHour >= 6.99f && currentHour <= 7.25f))
                    {
                        SetTime(dayStartHour);
                    }
                    else
                    {
                        SetTime(currentHour);
                    }
                }
            }
            else
            {
                SetTime(currentHour);
            }

            // Emit initial values to all registered listeners
            int hourInt = CurrentHourInt;
            int minuteInt = CurrentMinuteInt;

            lastEmittedHour = hourInt;
            lastEmittedMinute = minuteInt;
            lastEmittedPhase = currentPhase;

            OnHourChanged?.Invoke(hourInt);
            OnMinuteChanged?.Invoke(minuteInt);
            OnTimePhaseChanged?.Invoke(currentPhase);
            _corePhaseChanged?.Invoke(currentDay, (FarmBeware.Core.Runtime.DayPhase)currentPhase);
            OnDayChanged?.Invoke(currentDay);
            OnNormalizedTimeChanged?.Invoke(NormalizedTime);
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandleTimeManagerPhaseChanged;
                TimeManager.Instance.OnDayChanged += HandleTimeManagerDayChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandleTimeManagerPhaseChanged;
                TimeManager.Instance.OnDayChanged -= HandleTimeManagerDayChanged;
            }
        }

        private void HandleTimeManagerPhaseChanged(TimeManager.DayPhase phase)
        {
            if (phase == TimeManager.DayPhase.Night)
            {
                SetTime(nightStartHour);
            }
            else if (phase == TimeManager.DayPhase.Day)
            {
                daytimeElapsedSeconds = 0f;
                duskWarningTriggered = false;
                autoSleepTriggered = false;
                SetTime(dayStartHour);
            }
        }

        private void HandleTimeManagerDayChanged(int newDay)
        {
            currentDay = newDay;
            daytimeElapsedSeconds = 0f;
            duskWarningTriggered = false;
            autoSleepTriggered = false;
            OnDayChanged?.Invoke(currentDay);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || FarmBeware.Core.Runtime.UIModalHelper.IsSaveUIOpen)
                return;

            if (!useContinuousTime || isPaused)
                return;

            bool isDay = (TimeManager.Instance == null || TimeManager.Instance.currentPhase == TimeManager.DayPhase.Day);

            if (isDay)
            {
                // Advance real daytime timer
                daytimeElapsedSeconds += Time.deltaTime;
                float targetHour = EvaluateDaytimeHour(daytimeElapsedSeconds, daytimeDurationRealSeconds);
                ApplyHourInternal(targetHour);

                // 1. Dusk Bell Trigger (15:45 in-game / 13:30 real minutes / 810s default)
                float duskTriggerSec = 810f * (daytimeDurationRealSeconds / 960f);
                if (!duskWarningTriggered && (daytimeElapsedSeconds >= duskTriggerSec || currentHour >= duskWarningHour))
                {
                    TriggerDuskWarning();
                }

                // 2. Auto-Sleep Trigger (18:00 in-game / 16:00 real minutes / 960s default)
                if (autoSleepAtNightfall && !autoSleepTriggered && (daytimeElapsedSeconds >= daytimeDurationRealSeconds || currentHour >= nightStartHour))
                {
                    TriggerAutoSleep();
                }
            }
        }

        private void TriggerDuskWarning()
        {
            duskWarningTriggered = true;
            OnDuskWarning?.Invoke();

            // Floating text announcement above player
            var floatingText = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IFloatingTextService>();
            var player = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IPlayerContext>();
            if (floatingText != null && player != null && player.Transform != null)
            {
                floatingText.SpawnText(
                    player.Transform.position + Vector3.up * 1.5f,
                    "🔔 Dusk Bell: 2.5 minutes until nightfall! Prepare for battle!",
                    new Color(1f, 0.70f, 0.25f));
            }

            Debug.Log($"[DayNightTimeManager] 🔔 Dusk Bell triggered at {CurrentHourInt:D2}:{CurrentMinuteInt:D2} (2.5 minutes left until nightfall)");
        }

        private void TriggerAutoSleep()
        {
            autoSleepTriggered = true;
            OnAutoSleepTriggered?.Invoke();

            Debug.Log("[DayNightTimeManager] 🌙 Auto-Sleep triggered at 18:00! Night begins.");

            // Floating text alert
            var floatingText = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IFloatingTextService>();
            var player = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IPlayerContext>();
            if (floatingText != null && player != null && player.Transform != null)
            {
                floatingText.SpawnText(
                    player.Transform.position + Vector3.up * 1.5f,
                    "🌙 18:00 — Darkness falls! Night Brawl begins!",
                    new Color(1f, 0.45f, 0.45f));
            }

            // Dismiss active modal if any
            var modalStack = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IModalStackService>();
            if (modalStack != null && modalStack.HasActiveModal)
            {
                modalStack.CloseAll();
            }

            // Transition to night phase
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.StartNightPhase();
            }
            else
            {
                SetTime(nightStartHour);
            }
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            FarmBeware.Core.Runtime.ServiceLocator.Unregister<FarmBeware.Core.Runtime.ITimeService>();
            if (Instance == this)
            {
                Instance = null;
                _isApplicationQuitting = true;
            }
        }

        #endregion

        #region Core Time Simulation Logic

        // Keyframe timeline table: Real Minutes (Seconds) vs In-Game 24h Clock
        private static readonly float[] TimelineRealSeconds = { 0f, 60f, 240f, 660f, 810f, 960f };
        private static readonly float[] TimelineInGameHours = { 6.00f, 6.75f, 9.00f, 14.00f, 15.75f, 18.00f };

        /// <summary>
        /// Evaluates in-game hour monotonically across the 16-minute day phase timeline.
        /// Matches: 0m -> 06:00, 1m -> 06:45, 4m -> 09:00, 11m -> 14:00, 13.5m -> 15:45, 16m -> 18:00.
        /// </summary>
        public static float EvaluateDaytimeHour(float elapsedSeconds, float totalDuration = 960f)
        {
            float scale = totalDuration / 960f;
            float clamped = Mathf.Clamp(elapsedSeconds, 0f, totalDuration);

            for (int i = 0; i < TimelineRealSeconds.Length - 1; i++)
            {
                float t0 = TimelineRealSeconds[i] * scale;
                float t1 = TimelineRealSeconds[i + 1] * scale;
                if (clamped <= t1 || i == TimelineRealSeconds.Length - 2)
                {
                    float segFraction = (t1 > t0) ? (clamped - t0) / (t1 - t0) : 0f;
                    return Mathf.Lerp(TimelineInGameHours[i], TimelineInGameHours[i + 1], segFraction);
                }
            }
            return 18.0f;
        }

        public static float EvaluateElapsedSecondsFromHour(float targetHour, float totalDuration = 960f)
        {
            float scale = totalDuration / 960f;
            float clampedHour = Mathf.Clamp(targetHour, 6.00f, 18.00f);

            for (int i = 0; i < TimelineInGameHours.Length - 1; i++)
            {
                float h0 = TimelineInGameHours[i];
                float h1 = TimelineInGameHours[i + 1];
                if (clampedHour <= h1 || i == TimelineInGameHours.Length - 2)
                {
                    float frac = (h1 > h0) ? (clampedHour - h0) / (h1 - h0) : 0f;
                    float t0 = TimelineRealSeconds[i] * scale;
                    float t1 = TimelineRealSeconds[i + 1] * scale;
                    return Mathf.Lerp(t0, t1, frac);
                }
            }
            return totalDuration;
        }

        private void ApplyHourInternal(float newHour)
        {
            currentHour = Mathf.Repeat(newHour, 24.0f);

            // Normalized time
            OnNormalizedTimeChanged?.Invoke(NormalizedTime);

            // Hour Check
            int hourInt = CurrentHourInt;
            if (hourInt != lastEmittedHour)
            {
                lastEmittedHour = hourInt;
                OnHourChanged?.Invoke(hourInt);
            }

            // Minute Check
            int minuteInt = CurrentMinuteInt;
            if (minuteInt != lastEmittedMinute)
            {
                lastEmittedMinute = minuteInt;
                OnMinuteChanged?.Invoke(minuteInt);
            }

            // Phase Check
            EnvironmentPhase newPhase = EvaluatePhase(currentHour);
            if (newPhase != lastEmittedPhase)
            {
                currentPhase = newPhase;
                lastEmittedPhase = newPhase;
                OnTimePhaseChanged?.Invoke(currentPhase);
                _corePhaseChanged?.Invoke(currentDay, (FarmBeware.Core.Runtime.DayPhase)currentPhase);
            }
        }

        /// <summary>
        /// Mengklasifikasikan jam 24 jam ke dalam 4 kuadran fase lingkungan (Dawn, Day, Dusk, Night).
        /// </summary>
        public EnvironmentPhase EvaluatePhase(float hour)
        {
            if (hour >= dawnStartHour && hour < dayStartHour)
                return EnvironmentPhase.Dawn;
            if (hour >= dayStartHour && hour < duskStartHour)
                return EnvironmentPhase.Day;
            if (hour >= duskStartHour && hour < nightStartHour)
                return EnvironmentPhase.Dusk;

            // Rentang malam: nightStartHour s/d 24:00 ATAU 00:00 s/d dawnStartHour
            return EnvironmentPhase.Night;
        }

        #endregion

        #region IDayNightTimeService Public Commands

        public void SetTime(float targetHour)
        {
            currentHour = Mathf.Repeat(targetHour, 24.0f);
            currentPhase = EvaluatePhase(currentHour);

            daytimeElapsedSeconds = EvaluateElapsedSecondsFromHour(currentHour, daytimeDurationRealSeconds);
            duskWarningTriggered = (currentHour >= duskWarningHour);
            autoSleepTriggered = (currentHour >= nightStartHour);

            lastEmittedHour = CurrentHourInt;
            lastEmittedMinute = CurrentMinuteInt;
            lastEmittedPhase = currentPhase;

            OnNormalizedTimeChanged?.Invoke(NormalizedTime);
            OnHourChanged?.Invoke(lastEmittedHour);
            OnMinuteChanged?.Invoke(lastEmittedMinute);
            OnTimePhaseChanged?.Invoke(currentPhase);
            _corePhaseChanged?.Invoke(currentDay, (FarmBeware.Core.Runtime.DayPhase)currentPhase);
        }

        public void SetDayAndTime(int day, float targetHour)
        {
            currentDay = Mathf.Max(1, day);
            SetTime(targetHour);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void AdvanceHour(float hours)
        {
            SetTime(currentHour + hours);
        }

        public void AdvanceToNextDay()
        {
            daytimeElapsedSeconds = 0f;
            duskWarningTriggered = false;
            autoSleepTriggered = false;

            currentHour = dayStartHour;
            currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : (currentDay + 1);
            currentPhase = EvaluatePhase(currentHour);

            lastEmittedHour = CurrentHourInt;
            lastEmittedMinute = CurrentMinuteInt;
            lastEmittedPhase = currentPhase;

            OnDayChanged?.Invoke(currentDay);
            OnNormalizedTimeChanged?.Invoke(NormalizedTime);
            OnHourChanged?.Invoke(lastEmittedHour);
            OnMinuteChanged?.Invoke(lastEmittedMinute);
            OnTimePhaseChanged?.Invoke(currentPhase);
            _corePhaseChanged?.Invoke(currentDay, (FarmBeware.Core.Runtime.DayPhase)currentPhase);
        }

        public void SkipToNight()
        {
            SetTime(nightStartHour);
            Debug.Log($"[DayNightTimeManager] Mode Malam visual aktif (Jam: {currentHour:F1}, Fase: {currentPhase})");
        }

        #endregion

        #region Context Menu Testing Helpers

        [ContextMenu("Jump To: Dawn (05:30)")]
        private void JumpToDawn() => SetTime(5.5f);

        [ContextMenu("Jump To: Noon (12:00)")]
        private void JumpToNoon() => SetTime(12.0f);

        [ContextMenu("Jump To: Dusk (18:00)")]
        private void JumpToDusk() => SetTime(18.0f);

        [ContextMenu("Jump To: Midnight (00:00)")]
        private void JumpToMidnight() => SetTime(0.0f);

        [ContextMenu("Toggle Pause")]
        private void TogglePause() => SetPaused(!isPaused);

        #endregion
    }
}
