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
    public class DayNightTimeManager : MonoBehaviour, IDayNightTimeService
    {
        #region Singleton (Awake-Safe Pattern §1.1)

        private static DayNightTimeManager _instance;

        public static DayNightTimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    DayNightTimeManager[] found = FindObjectsByType<DayNightTimeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set => _instance = value;
        }

        #endregion

        #region Serialized Configuration

        [Header("1. Time Progression Settings")]
        [Tooltip("Total durasi dunia nyata (dalam detik) untuk menyelesaikan 1 hari permainan (24 jam in-game). Default 720 detik = 12 menit.")]
        [SerializeField] private float realSecondsPerInGameDay = 720f;

        [Tooltip("Jam awal saat game pertama kali dimulai (0.0f - 24.0f). Contoh 6.0f = 06:00.")]
        [Range(0f, 24f)]
        [SerializeField] private float initialHour = 6.0f;

        [Tooltip("Hari awal kalender saat game dimulai.")]
        [Min(1)]
        [SerializeField] private int initialDay = 1;

        [Tooltip("Apakah simulasi waktu otomatis berjalan saat Start().")]
        [SerializeField] private bool autoStart = true;

        [Header("2. Phase Thresholds (24h Clock)")]
        [Tooltip("Jam dimulainya Fajar (Dawn). Contoh: 5.0 (05:00).")]
        [Range(0f, 24f)]
        [SerializeField] private float dawnStartHour = 5.0f;

        [Tooltip("Jam dimulainya Siang (Day). Contoh: 7.0 (07:00).")]
        [Range(0f, 24f)]
        [SerializeField] private float dayStartHour = 7.0f;

        [Tooltip("Jam dimulainya Senja (Dusk). Contoh: 17.0 (17:00).")]
        [Range(0f, 24f)]
        [SerializeField] private float duskStartHour = 17.0f;

        [Tooltip("Jam dimulainya Malam (Night). Contoh: 19.5 (19:30).")]
        [Range(0f, 24f)]
        [SerializeField] private float nightStartHour = 19.5f;

        [Header("3. Integration Bridge")]
        [Tooltip("Jika true, menyinkronkan event fase dengan TimeManager legacy di project.")]
        [SerializeField] private bool syncWithLegacyTimeManager = true;

        #endregion

        #region State Fields

        [Header("Runtime Debug View (Read-Only)")]
        [SerializeField] private float currentHour = 6.0f;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private EnvironmentPhase currentPhase = EnvironmentPhase.Dawn;
        [SerializeField] private bool isPaused = false;

        private int lastEmittedHour = -1;
        private int lastEmittedMinute = -1;
        private EnvironmentPhase lastEmittedPhase = (EnvironmentPhase)(-1);

        #endregion

        #region Events (IDayNightTimeService Implementation)

        public event Action<int> OnHourChanged;
        public event Action<int> OnMinuteChanged;
        public event Action<EnvironmentPhase> OnTimePhaseChanged;
        public event Action<int> OnDayChanged;
        public event Action<float> OnNormalizedTimeChanged;

        #endregion

        #region Public Properties

        public float CurrentHour => currentHour;
        public int CurrentHourInt => Mathf.FloorToInt(currentHour) % 24;
        public int CurrentMinuteInt => Mathf.FloorToInt((currentHour - Mathf.Floor(currentHour)) * 60f) % 60;
        public int CurrentDay => currentDay;
        public EnvironmentPhase CurrentPhase => currentPhase;
        public float NormalizedTime => Mathf.Clamp01(currentHour / 24.0f);
        public bool IsPaused => isPaused;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            currentHour = initialHour;
            currentDay = initialDay;
            isPaused = !autoStart;
            currentPhase = EvaluatePhase(currentHour);
        }

        private void Start()
        {
            // Emit inisialisasi awal ke semua listener yang telah mendaftar di Awake
            int hourInt = CurrentHourInt;
            int minuteInt = CurrentMinuteInt;

            lastEmittedHour = hourInt;
            lastEmittedMinute = minuteInt;
            lastEmittedPhase = currentPhase;

            OnHourChanged?.Invoke(hourInt);
            OnMinuteChanged?.Invoke(minuteInt);
            OnTimePhaseChanged?.Invoke(currentPhase);
            OnDayChanged?.Invoke(currentDay);
            OnNormalizedTimeChanged?.Invoke(NormalizedTime);

            SyncLegacyTimeManager(currentPhase, currentDay);
        }

        private void OnEnable()
        {
            if (syncWithLegacyTimeManager && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandleLegacyPhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandleLegacyPhaseChanged;
            }
        }

        private void HandleLegacyPhaseChanged(TimeManager.DayPhase phase)
        {
            if (phase == TimeManager.DayPhase.Night && currentPhase != EnvironmentPhase.Night)
            {
                SkipToNight();
            }
            else if (phase == TimeManager.DayPhase.Day && currentPhase == EnvironmentPhase.Night)
            {
                SetTime(dayStartHour);
            }
        }

        private void Update()
        {
            // Pintasan keyboard 'N' untuk kompatibilitas penuh dengan sistem legacy
            if (syncWithLegacyTimeManager && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame)
            {
                SkipToNight();
                return;
            }

            if (isPaused || realSecondsPerInGameDay <= 0.01f)
                return;

            // Hitung progresi waktu murni (matematika independen)
            float hourDelta = (Time.deltaTime / realSecondsPerInGameDay) * 24.0f;
            AdvanceHourInternal(hourDelta);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Core Time Simulation Logic

        /// <summary>
        /// Mengembangkan jam in-game dan memancarkan sinyal event jika melewati threshold batas waktu.
        /// Tidak ada render call atau operasi visual dalam metode ini.
        /// </summary>
        private void AdvanceHourInternal(float deltaHours)
        {
            currentHour += deltaHours;

            // Rollover 24 jam -> Hari Baru
            if (currentHour >= 24.0f)
            {
                currentHour -= 24.0f;
                currentDay++;
                OnDayChanged?.Invoke(currentDay);

                if (syncWithLegacyTimeManager && TimeManager.Instance != null)
                {
                    TimeManager.Instance.AdvanceToNextDay();
                }
            }
            else if (currentHour < 0f)
            {
                currentHour += 24.0f;
            }

            // Normalisasi waktu kontinu
            OnNormalizedTimeChanged?.Invoke(NormalizedTime);

            // Cek Threshold Jam
            int hourInt = CurrentHourInt;
            if (hourInt != lastEmittedHour)
            {
                lastEmittedHour = hourInt;
                OnHourChanged?.Invoke(hourInt);
            }

            // Cek Threshold Menit
            int minuteInt = CurrentMinuteInt;
            if (minuteInt != lastEmittedMinute)
            {
                lastEmittedMinute = minuteInt;
                OnMinuteChanged?.Invoke(minuteInt);
            }

            // Cek Threshold Fase Lingkungan
            EnvironmentPhase newPhase = EvaluatePhase(currentHour);
            if (newPhase != lastEmittedPhase)
            {
                currentPhase = newPhase;
                lastEmittedPhase = newPhase;
                OnTimePhaseChanged?.Invoke(currentPhase);
                SyncLegacyTimeManager(currentPhase, currentDay);
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

        private void SyncLegacyTimeManager(EnvironmentPhase phase, int day)
        {
            if (!syncWithLegacyTimeManager || TimeManager.Instance == null)
                return;

            if (phase == EnvironmentPhase.Night && TimeManager.Instance.currentPhase != TimeManager.DayPhase.Night)
            {
                TimeManager.Instance.StartNightPhase();
            }
            else if (phase == EnvironmentPhase.Day && TimeManager.Instance.currentPhase != TimeManager.DayPhase.Day)
            {
                // Day phase pada legacy
            }
        }

        #endregion

        #region IDayNightTimeService Public Commands

        public void SetTime(float targetHour)
        {
            currentHour = Mathf.Repeat(targetHour, 24.0f);
            currentPhase = EvaluatePhase(currentHour);

            lastEmittedHour = CurrentHourInt;
            lastEmittedMinute = CurrentMinuteInt;
            lastEmittedPhase = currentPhase;

            OnNormalizedTimeChanged?.Invoke(NormalizedTime);
            OnHourChanged?.Invoke(lastEmittedHour);
            OnMinuteChanged?.Invoke(lastEmittedMinute);
            OnTimePhaseChanged?.Invoke(currentPhase);

            SyncLegacyTimeManager(currentPhase, currentDay);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void AdvanceHour(float hours)
        {
            AdvanceHourInternal(hours);
        }

        public void AdvanceToNextDay()
        {
            currentHour = dawnStartHour;
            currentDay++;
            currentPhase = EvaluatePhase(currentHour);

            lastEmittedHour = CurrentHourInt;
            lastEmittedMinute = CurrentMinuteInt;
            lastEmittedPhase = currentPhase;

            OnDayChanged?.Invoke(currentDay);
            OnNormalizedTimeChanged?.Invoke(NormalizedTime);
            OnHourChanged?.Invoke(lastEmittedHour);
            OnMinuteChanged?.Invoke(lastEmittedMinute);
            OnTimePhaseChanged?.Invoke(currentPhase);

            if (syncWithLegacyTimeManager && TimeManager.Instance != null)
            {
                TimeManager.Instance.AdvanceToNextDay();
            }
        }

        public void SkipToNight()
        {
            SetTime(nightStartHour);
            if (syncWithLegacyTimeManager && TimeManager.Instance != null && TimeManager.Instance.currentPhase != TimeManager.DayPhase.Night)
            {
                TimeManager.Instance.StartNightPhase();
            }
            Debug.Log($"[DayNightTimeManager] Mode Malam aktif via pintasan 'N' (Jam: {currentHour:F1}, Fase: {currentPhase})");
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
