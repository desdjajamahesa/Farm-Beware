using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FeaturesTime;
using FeaturesRendering.Lighting;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengatur Post-Processing Volume URP 17 secara dinamis dan filmik untuk siklus Day/Night 24 jam.
    /// Mematuhi Clean Architecture, SOLID, zero polling (Event-Driven OOP), dan zero GC.
    ///
    /// Mengontrol komponen URP Volume Profile:
    /// - Tonemapping (ACES)
    /// - Bloom (Threshold responsif monster rim light & projectiles)
    /// - ColorAdjustments (Exposure EV, Contrast, Saturation, Color Filter)
    /// - WhiteBalance (Temperature ~5500K hangat siang vs ~8000K kebiruan malam, Tint)
    /// - Vignette (Perimeter sirkular untuk fokus action survival)
    /// </summary>
    [DisallowMultipleComponent]
    public class DayNightVolumeController : MonoBehaviour
    {
        #region Sub-Types & Preset Struct

        [Serializable]
        public struct VolumePhaseSettings
        {
            [Header("Tonemapping")]
            [Tooltip("Mode Tonemapping URP. Standar filmik industri: TonemappingMode.ACES.")]
            public TonemappingMode tonemappingMode;

            [Header("Bloom")]
            [Tooltip("Batas luminansi minimum untuk memicu bloom. Siang: ~1.5 - 1.8, Malam/Senja: ~0.8 - 1.0 (menangkap rim light MonsterFresnelLit).")]
            [Min(0f)] public float bloomThreshold;

            [Tooltip("Intensitas pendaran cahaya bloom.")]
            [Min(0f)] public float bloomIntensity;

            [Tooltip("Penyebaran (scatter) pendaran bloom (0 - 1).")]
            [Range(0f, 1f)] public float bloomScatter;

            [Header("Color Adjustments")]
            [Tooltip("Kompensasi eksposur dalam EV (0.0 EV untuk siang, -0.8 s/d -1.2 EV untuk malam).")]
            public float postExposure;

            [Tooltip("Kontras warna (-100 s/d 100).")]
            [Range(-100f, 100f)] public float contrast;

            [Tooltip("Saturasi warna (-100 s/d 100).")]
            [Range(-100f, 100f)] public float saturation;

            [Tooltip("Filter warna global.")]
            public Color colorFilter;

            [Header("White Balance")]
            [Tooltip("Suhu warna White Balance (-100 dingin/kebiruan s/d +100 hangat/kekuningan). Siang: ~0 s/d +10 (hangat ~5500K), Malam: ~ -20 s/d -30 (dingin ~8000K).")]
            [Range(-100f, 100f)] public float temperature;

            [Tooltip("Tint White Balance (-100 hijau s/d +100 magenta). Malam sedikit negatif untuk nuansa cyan/dingin.")]
            [Range(-100f, 100f)] public float tint;

            [Header("Vignette")]
            [Tooltip("Intensitas vignette perimeter layar (Siang: ~0.15 - 0.20, Malam: ~0.35 - 0.45 untuk kesan klaustrofobik).")]
            [Range(0f, 1f)] public float vignetteIntensity;

            [Tooltip("Kehalusan gradasi ke ujung layar.")]
            [Range(0f, 1f)] public float vignetteSmoothness;

            [Tooltip("Warna vignette.")]
            public Color vignetteColor;

            [Header("Lift Gamma Gain (Shadows Midtones Highlights)")]
            [Tooltip("Offset warna bayangan (Lift). Malam: offset indigo/safir untuk mencegah blacks crush.")]
            public Vector4 lift;

            [Tooltip("Pengaturan midtone (Gamma).")]
            public Vector4 gamma;

            [Tooltip("Pengaturan highlight (Gain).")]
            public Vector4 gain;
        }

        #endregion

        #region Serialized Fields

        [Header("Volume References")]
        [Tooltip("Volume URP utama yang dikontrol. Jika kosong, otomatis mencari Volume pada objek ini atau scene.")]
        [SerializeField] private Volume baseVolume;

        [Tooltip("Volume sekunder opsional (misal volume khusus horor/encounter malam). Jika ada, weight akan di-lerp 1.0 saat Night dan 0.0 saat fase lainnya.")]
        [SerializeField] private Volume nightVolume;

        [Header("Transition Settings")]
        [Tooltip("Durasi transisi interpolasi halus (lerp) dalam detik.")]
        [Min(0.1f)]
        [SerializeField] private float transitionDuration = 3.5f;

        [Tooltip("Kurva transisi visual organik (opsional). Jika tidak diisi, menggunakan kurva Mathf.SmoothStep.")]
        [SerializeField] private AnimationCurve transitionCurve;

        [Header("Phase Presets")]
        [SerializeField] private VolumePhaseSettings dawnSettings = CreateDefaultDawnSettings();
        [SerializeField] private VolumePhaseSettings daySettings = CreateDefaultDaySettings();
        [SerializeField] private VolumePhaseSettings duskSettings = CreateDefaultDuskSettings();
        [SerializeField] private VolumePhaseSettings nightSettings = CreateDefaultNightSettings();

        [Header("Combat Juice & Impact Settings")]
        [Tooltip("Jika aktif, otomatis mendengarkan event damage player untuk memicu getaran aberasi kromatik.")]
        [SerializeField] private bool bindPlayerDamageEvent = true;

        [Tooltip("Intensitas puncak getaran aberasi kromatik saat player terkena damage.")]
        [Range(0f, 1f)]
        [SerializeField] private float playerDamagePeakIntensity = 0.65f;

        [Tooltip("Durasi getaran aberasi kromatik player damage dalam detik.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float playerDamageDuration = 0.30f;

        [Header("Runtime Debug View (Read-Only)")]
        [SerializeField] private EnvironmentPhase currentActivePhase = EnvironmentPhase.Day;
        [SerializeField] private bool isTransitioning = false;

        #endregion

        #region Singleton Accessor

        private static DayNightVolumeController _instance;

        public static DayNightVolumeController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<DayNightVolumeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set => _instance = value;
        }

        #endregion

        #region Private Cached Overrides & State

        private VolumeProfile _runtimeProfile;
        private Tonemapping _tonemapping;
        private Bloom _bloom;
        private ColorAdjustments _colorAdjustments;
        private WhiteBalance _whiteBalance;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;
        private LiftGammaGain _liftGammaGain;

        private Coroutine _transitionCoroutine;
        private Coroutine _impulseCoroutine;
        private PlayerStats _cachedPlayerStats;

        #endregion

        #region Public Properties

        public EnvironmentPhase CurrentActivePhase => currentActivePhase;
        public bool IsTransitioning => isTransitioning;
        public float TransitionDuration { get => transitionDuration; set => transitionDuration = Mathf.Max(0.1f, value); }
        public ChromaticAberration ChromaticAberrationOverride => _chromaticAberration;

        #endregion

        #region Factory Presets (Calibrated Defaults)

        public static VolumePhaseSettings CreateDefaultDawnSettings()
        {
            return new VolumePhaseSettings
            {
                tonemappingMode = TonemappingMode.ACES,
                bloomThreshold = 1.5f,
                bloomIntensity = 0.25f,
                bloomScatter = 0.70f,
                postExposure = -0.2f,
                contrast = 8f,
                saturation = 5f,
                colorFilter = new Color(1.0f, 0.95f, 0.90f),
                temperature = 8f,
                tint = 0f,
                vignetteIntensity = 0.18f,
                vignetteSmoothness = 0.35f,
                vignetteColor = Color.black,
                lift = Vector4.zero,
                gamma = Vector4.zero,
                gain = Vector4.one
            };
        }

        public static VolumePhaseSettings CreateDefaultDaySettings()
        {
            return new VolumePhaseSettings
            {
                tonemappingMode = TonemappingMode.ACES,
                bloomThreshold = 1.65f,
                bloomIntensity = 0.22f,
                bloomScatter = 0.65f,
                postExposure = 0.0f,
                contrast = 10f,
                saturation = 8f,
                colorFilter = Color.white,
                temperature = 5f,
                tint = 0f,
                vignetteIntensity = 0.16f,
                vignetteSmoothness = 0.35f,
                vignetteColor = Color.black,
                lift = Vector4.zero,
                gamma = Vector4.zero,
                gain = Vector4.one
            };
        }

        public static VolumePhaseSettings CreateDefaultDuskSettings()
        {
            return new VolumePhaseSettings
            {
                tonemappingMode = TonemappingMode.ACES,
                bloomThreshold = 0.95f,
                bloomIntensity = 0.32f,
                bloomScatter = 0.70f,
                postExposure = -0.4f,
                contrast = 12f,
                saturation = 12f,
                colorFilter = new Color(1.0f, 0.90f, 0.82f),
                temperature = 14f,
                tint = -2f,
                vignetteIntensity = 0.22f,
                vignetteSmoothness = 0.38f,
                vignetteColor = Color.black,
                lift = new Vector4(0.01f, 0.005f, 0.0f, 0.0f),
                gamma = Vector4.zero,
                gain = Vector4.one
            };
        }

        public static VolumePhaseSettings CreateDefaultNightSettings()
        {
            return new VolumePhaseSettings
            {
                tonemappingMode = TonemappingMode.ACES,
                bloomThreshold = 0.85f,
                bloomIntensity = 0.38f,
                bloomScatter = 0.70f,
                postExposure = -0.30f,
                contrast = 8f,
                saturation = 4f,
                colorFilter = new Color(0.92f, 0.95f, 1.0f),
                temperature = -14f,
                tint = -3f,
                vignetteIntensity = 0.22f,
                vignetteSmoothness = 0.35f,
                vignetteColor = Color.black,
                lift = new Vector4(0.015f, 0.025f, 0.065f, 0.0f),
                gamma = Vector4.zero,
                gain = Vector4.one
            };
        }

        #endregion

        #region Unity Lifecycle (Event-Driven Registration)

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogWarning("[DayNightVolumeController] Duplicate instance detected on " + gameObject.name);
            }

            ValidatePresetsIntegrity();
            CacheOverrides();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            // Berlangganan (subscribe) ke DayNightTimeManager.Instance.OnTimePhaseChanged
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandleTimePhaseChanged;
            }

            if (bindPlayerDamageEvent)
            {
                BindPlayerStats();
            }
        }

        private void OnDisable()
        {
            // Batalkan langganan (unsubscribe) bersih saat disabled untuk mencegah dangling delegate / memory leak
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleTimePhaseChanged;
            }

            UnbindPlayerStats();

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
                isTransitioning = false;
            }

            if (_impulseCoroutine != null)
            {
                StopCoroutine(_impulseCoroutine);
                _impulseCoroutine = null;
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = 0f;
            }
        }

        private void Start()
        {
            if (bindPlayerDamageEvent && _cachedPlayerStats == null)
            {
                BindPlayerStats();
            }

            // Defensive resolution: pastikan event terdaftar bila saat Awake/OnEnable DayNightTimeManager belum siap
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleTimePhaseChanged;
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandleTimePhaseChanged;

                // Initial sync pada fase aktif
                ApplyPhaseSettingsInstant(DayNightTimeManager.Instance.CurrentPhase);
            }
            else if (TimeManager.Instance != null)
            {
                // Fallback kompatibilitas dengan legacy TimeManager
                EnvironmentPhase legacyPhase = TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night
                    ? EnvironmentPhase.Night
                    : EnvironmentPhase.Day;
                ApplyPhaseSettingsInstant(legacyPhase);
            }
            else
            {
                ApplyPhaseSettingsInstant(EnvironmentPhase.Day);
            }
        }

        // CATATAN ARSITEKTUR:
        // Update() DIHAPUSKAN SECARA EKSPLISIT.
        // Komponen ini murni event-driven tanpa polling CPU per-frame.

        #endregion

        #region Profile & Overrides Caching (Zero GC & Profile Safety)

        private void CacheOverrides()
        {
            if (baseVolume == null)
            {
                baseVolume = GetComponent<Volume>();
                if (baseVolume == null)
                    baseVolume = FindFirstObjectByType<Volume>();
            }

            if (baseVolume == null)
            {
                Debug.LogWarning("[DayNightVolumeController] Tidak ada komponen Volume yang ditemukan untuk dikontrol.");
                return;
            }

            // Mengambil profile instanced runtime (bukan sharedProfile) agar modifikasi aman dan tidak mengubah asset di disk
            _runtimeProfile = baseVolume.profile;
            if (_runtimeProfile == null)
            {
                Debug.LogWarning("[DayNightVolumeController] Volume tidak memiliki profile yang valid.");
                return;
            }

            // 1. Tonemapping (ACES)
            if (!_runtimeProfile.TryGet(out _tonemapping))
            {
                _tonemapping = _runtimeProfile.Add<Tonemapping>(true);
            }
            _tonemapping.active = true;
            _tonemapping.mode.overrideState = true;
            _tonemapping.mode.value = TonemappingMode.ACES;

            // 2. Bloom
            if (!_runtimeProfile.TryGet(out _bloom))
            {
                _bloom = _runtimeProfile.Add<Bloom>(true);
            }
            _bloom.active = true;
            _bloom.threshold.overrideState = true;
            _bloom.intensity.overrideState = true;
            _bloom.scatter.overrideState = true;

            // 3. Color Adjustments
            if (!_runtimeProfile.TryGet(out _colorAdjustments))
            {
                _colorAdjustments = _runtimeProfile.Add<ColorAdjustments>(true);
            }
            _colorAdjustments.active = true;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.contrast.overrideState = true;
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.colorFilter.overrideState = true;

            // 4. White Balance
            if (!_runtimeProfile.TryGet(out _whiteBalance))
            {
                _whiteBalance = _runtimeProfile.Add<WhiteBalance>(true);
            }
            _whiteBalance.active = true;
            _whiteBalance.temperature.overrideState = true;
            _whiteBalance.tint.overrideState = true;

            // 5. Vignette
            if (!_runtimeProfile.TryGet(out _vignette))
            {
                _vignette = _runtimeProfile.Add<Vignette>(true);
            }
            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;
            _vignette.color.overrideState = true;

            // 6. Chromatic Aberration (Combat Juice / Taktil Impulse)
            if (!_runtimeProfile.TryGet(out _chromaticAberration))
            {
                _chromaticAberration = _runtimeProfile.Add<ChromaticAberration>(true);
            }
            _chromaticAberration.active = true;
            _chromaticAberration.intensity.overrideState = true;
            _chromaticAberration.intensity.value = 0f; // Baseline selalu 0 untuk kestabilan pandangan isometrik

            // 7. Lift Gamma Gain (Shadows Midtones Highlights)
            if (!_runtimeProfile.TryGet(out _liftGammaGain))
            {
                _liftGammaGain = _runtimeProfile.Add<LiftGammaGain>(true);
            }
            _liftGammaGain.active = true;
            _liftGammaGain.lift.overrideState = true;
            _liftGammaGain.gamma.overrideState = true;
            _liftGammaGain.gain.overrideState = true;
        }

        private void ValidatePresetsIntegrity()
        {
            // Guard: jika scene lama memiliki preset kosong/ter-reset ke 0
            if (daySettings.bloomThreshold <= 0.001f && daySettings.bloomIntensity <= 0.001f)
                daySettings = CreateDefaultDaySettings();

            if (nightSettings.bloomThreshold <= 0.001f && nightSettings.bloomIntensity <= 0.001f)
                nightSettings = CreateDefaultNightSettings();

            if (dawnSettings.bloomThreshold <= 0.001f && dawnSettings.bloomIntensity <= 0.001f)
                dawnSettings = CreateDefaultDawnSettings();

            if (duskSettings.bloomThreshold <= 0.001f && duskSettings.bloomIntensity <= 0.001f)
                duskSettings = CreateDefaultDuskSettings();
        }

        #endregion

        #region Event Handlers & Smooth Transition (Zero GC Coroutine)

        private void HandleTimePhaseChanged(EnvironmentPhase newPhase)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            if (transitionDuration <= 0.05f)
            {
                ApplyPhaseSettingsInstant(newPhase);
            }
            else
            {
                _transitionCoroutine = StartCoroutine(TransitionToPhaseRoutine(newPhase));
            }
        }

        private IEnumerator TransitionToPhaseRoutine(EnvironmentPhase targetPhase)
        {
            isTransitioning = true;
            currentActivePhase = targetPhase;
            VolumePhaseSettings target = GetSettingsForPhase(targetPhase);

            // Wajib pastikan TonemappingMode.ACES aktif
            if (_tonemapping != null)
            {
                _tonemapping.mode.value = target.tonemappingMode;
            }

            // Capture nilai awal (value types pada stack - zero GC heap allocation)
            float startBloomThreshold = _bloom != null ? _bloom.threshold.value : target.bloomThreshold;
            float startBloomIntensity = _bloom != null ? _bloom.intensity.value : target.bloomIntensity;
            float startBloomScatter = _bloom != null ? _bloom.scatter.value : target.bloomScatter;

            float startExposure = _colorAdjustments != null ? _colorAdjustments.postExposure.value : target.postExposure;
            float startContrast = _colorAdjustments != null ? _colorAdjustments.contrast.value : target.contrast;
            float startSaturation = _colorAdjustments != null ? _colorAdjustments.saturation.value : target.saturation;
            Color startColorFilter = _colorAdjustments != null ? _colorAdjustments.colorFilter.value : target.colorFilter;

            float startTemperature = _whiteBalance != null ? _whiteBalance.temperature.value : target.temperature;
            float startTint = _whiteBalance != null ? _whiteBalance.tint.value : target.tint;

            float startVigIntensity = _vignette != null ? _vignette.intensity.value : target.vignetteIntensity;
            float startVigSmoothness = _vignette != null ? _vignette.smoothness.value : target.vignetteSmoothness;
            Color startVigColor = _vignette != null ? _vignette.color.value : target.vignetteColor;

            Vector4 startLift = _liftGammaGain != null ? _liftGammaGain.lift.value : target.lift;
            Vector4 startGamma = _liftGammaGain != null ? _liftGammaGain.gamma.value : target.gamma;
            Vector4 startGain = _liftGammaGain != null ? _liftGammaGain.gain.value : target.gain;

            float startNightWeight = nightVolume != null ? nightVolume.weight : 0f;
            float targetNightWeight = (targetPhase == EnvironmentPhase.Night) ? 1.0f : 0.0f;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / transitionDuration);

                // Kurva organik: AnimationCurve kustom atau fallback Mathf.SmoothStep
                float blend = (transitionCurve != null && transitionCurve.length > 1)
                    ? transitionCurve.Evaluate(progress)
                    : Mathf.SmoothStep(0f, 1f, progress);

                if (nightVolume != null)
                {
                    nightVolume.weight = Mathf.Lerp(startNightWeight, targetNightWeight, blend);
                }

                if (_bloom != null)
                {
                    _bloom.threshold.value = Mathf.Lerp(startBloomThreshold, target.bloomThreshold, blend);
                    _bloom.intensity.value = Mathf.Lerp(startBloomIntensity, target.bloomIntensity, blend);
                    _bloom.scatter.value = Mathf.Lerp(startBloomScatter, target.bloomScatter, blend);
                }

                if (_colorAdjustments != null)
                {
                    _colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, target.postExposure, blend);
                    _colorAdjustments.contrast.value = Mathf.Lerp(startContrast, target.contrast, blend);
                    _colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, target.saturation, blend);
                    _colorAdjustments.colorFilter.value = Color.Lerp(startColorFilter, target.colorFilter, blend);
                }

                if (_whiteBalance != null)
                {
                    _whiteBalance.temperature.value = Mathf.Lerp(startTemperature, target.temperature, blend);
                    _whiteBalance.tint.value = Mathf.Lerp(startTint, target.tint, blend);
                }

                if (_vignette != null)
                {
                    _vignette.intensity.value = Mathf.Lerp(startVigIntensity, target.vignetteIntensity, blend);
                    _vignette.smoothness.value = Mathf.Lerp(startVigSmoothness, target.vignetteSmoothness, blend);
                    _vignette.color.value = Color.Lerp(startVigColor, target.vignetteColor, blend);
                }

                if (_liftGammaGain != null)
                {
                    _liftGammaGain.lift.value = Vector4.Lerp(startLift, target.lift, blend);
                    _liftGammaGain.gamma.value = Vector4.Lerp(startGamma, target.gamma, blend);
                    _liftGammaGain.gain.value = Vector4.Lerp(startGain, target.gain, blend);
                }

                yield return null;
            }

            // Snap nilai target di akhir transisi
            ApplyPhaseSettingsInstant(targetPhase);

            _transitionCoroutine = null;
            isTransitioning = false;
        }

        #endregion

        #region Public Commands & Instant Application

        /// <summary>
        /// Menerapkan pengaturan post-processing fase secara instan tanpa interpolasi.
        /// </summary>
        public void ApplyPhaseSettingsInstant(EnvironmentPhase phase)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
                isTransitioning = false;
            }

            if (_impulseCoroutine != null)
            {
                StopCoroutine(_impulseCoroutine);
                _impulseCoroutine = null;
            }

            if (_runtimeProfile == null)
            {
                ValidatePresetsIntegrity();
                CacheOverrides();
            }

            currentActivePhase = phase;
            VolumePhaseSettings target = GetSettingsForPhase(phase);

            if (nightVolume != null)
            {
                nightVolume.weight = (phase == EnvironmentPhase.Night) ? 1.0f : 0.0f;
            }

            if (_tonemapping != null)
            {
                _tonemapping.mode.value = target.tonemappingMode;
            }

            if (_bloom != null)
            {
                _bloom.threshold.value = target.bloomThreshold;
                _bloom.intensity.value = target.bloomIntensity;
                _bloom.scatter.value = target.bloomScatter;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.value = target.postExposure;
                _colorAdjustments.contrast.value = target.contrast;
                _colorAdjustments.saturation.value = target.saturation;
                _colorAdjustments.colorFilter.value = target.colorFilter;
            }

            if (_whiteBalance != null)
            {
                _whiteBalance.temperature.value = target.temperature;
                _whiteBalance.tint.value = target.tint;
            }

            if (_vignette != null)
            {
                _vignette.intensity.value = target.vignetteIntensity;
                _vignette.smoothness.value = target.vignetteSmoothness;
                _vignette.color.value = target.vignetteColor;
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = 0f;
            }

            if (_liftGammaGain != null)
            {
                _liftGammaGain.lift.value = target.lift;
                _liftGammaGain.gamma.value = target.gamma;
                _liftGammaGain.gain.value = target.gain;
            }
        }

        /// <summary>
        /// Mendapatkan struct konfigurasi preset untuk fase yang ditentukan.
        /// </summary>
        public VolumePhaseSettings GetSettingsForPhase(EnvironmentPhase phase)
        {
            switch (phase)
            {
                case EnvironmentPhase.Dawn: return dawnSettings;
                case EnvironmentPhase.Day: return daySettings;
                case EnvironmentPhase.Dusk: return duskSettings;
                case EnvironmentPhase.Night: return nightSettings;
                default: return daySettings;
            }
        }

        /// <summary>
        /// Mengatur struct konfigurasi preset untuk fase tertentu secara runtime / testing.
        /// </summary>
        public void SetSettingsForPhase(EnvironmentPhase phase, VolumePhaseSettings settings)
        {
            switch (phase)
            {
                case EnvironmentPhase.Dawn: dawnSettings = settings; break;
                case EnvironmentPhase.Day: daySettings = settings; break;
                case EnvironmentPhase.Dusk: duskSettings = settings; break;
                case EnvironmentPhase.Night: nightSettings = settings; break;
            }
        }

        #endregion

        #region Combat Juice & Chromatic Impulse

        /// <summary>
        /// Memicu lonjakan aberasi kromatik tajam yang meluruh secara organik (smooth decay)
        /// untuk memberikan umpan balik benturan taktil (misal saat player menerima damage kritis atau saat bos/Taro Colossus spawn).
        /// Zero GC: berjalan murni menggunakan stack value types dan interpolasi frame time.
        /// </summary>
        /// <param name="peakIntensity">Intensitas puncak aberasi kromatik (0.0 s/d 1.0).</param>
        /// <param name="duration">Durasi total getaran dalam detik.</param>
        /// <summary>
        /// Triggers a brief cinematic chromatic aberration impulse for heavy melee combat tactile feedback (juice).
        /// </summary>
        public void TriggerCombatImpulse(float intensity = 0.85f, float duration = 0.25f)
        {
            TriggerChromaticImpulse(intensity, duration);
        }

        public void TriggerChromaticImpulse(float peakIntensity = 0.75f, float duration = 0.35f)
        {
            if (_runtimeProfile == null)
            {
                CacheOverrides();
            }

            if (_chromaticAberration == null)
                return;

            // Hentikan coroutine impulse sebelumnya jika masih aktif agar tidak terjadi tumpang tindih
            if (_impulseCoroutine != null)
            {
                StopCoroutine(_impulseCoroutine);
                _impulseCoroutine = null;
            }

            if (!gameObject.activeInHierarchy || !enabled || duration <= 0.001f)
            {
                _chromaticAberration.intensity.value = 0f;
                return;
            }

            _impulseCoroutine = StartCoroutine(ChromaticImpulseRoutine(peakIntensity, duration));
        }

        private IEnumerator ChromaticImpulseRoutine(float peakIntensity, float totalDuration)
        {
            // Attack phase: ~20% durasi (interpolasi tajam naik ke peak)
            // Decay phase : ~80% durasi (peluruhan halus kembali ke 0f)
            float attackDuration = totalDuration * 0.20f;
            float decayDuration = totalDuration - attackDuration;
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                if (elapsed <= attackDuration)
                {
                    float tAttack = Mathf.Clamp01(elapsed / attackDuration);
                    _chromaticAberration.intensity.value = Mathf.Lerp(0f, peakIntensity, tAttack);
                }
                else
                {
                    float tDecay = Mathf.Clamp01((elapsed - attackDuration) / decayDuration);
                    _chromaticAberration.intensity.value = Mathf.SmoothStep(peakIntensity, 0f, tDecay);
                }

                yield return null;
            }

            // Pastikan baseline kembali tepat ke 0f
            _chromaticAberration.intensity.value = 0f;
            _impulseCoroutine = null;
        }

        private void BindPlayerStats()
        {
            if (_cachedPlayerStats == null)
                _cachedPlayerStats = FindFirstObjectByType<PlayerStats>();

            if (_cachedPlayerStats != null)
            {
                _cachedPlayerStats.OnDamageTaken -= HandlePlayerDamageTaken;
                _cachedPlayerStats.OnDamageTaken += HandlePlayerDamageTaken;
            }
        }

        private void UnbindPlayerStats()
        {
            if (_cachedPlayerStats != null)
            {
                _cachedPlayerStats.OnDamageTaken -= HandlePlayerDamageTaken;
            }
        }

        private void HandlePlayerDamageTaken(int damageAmount)
        {
            TriggerChromaticImpulse(playerDamagePeakIntensity, playerDamageDuration);
        }

        #endregion

        #region Backward Compatibility Overloads

        /// <summary>
        /// Overload kompatibilitas untuk pemanggil berbasis EnvironmentPhase.
        /// </summary>
        public void ApplyInstant(EnvironmentPhase phase) => ApplyPhaseSettingsInstant(phase);

        /// <summary>
        /// Overload kompatibilitas untuk SceneLightingEditorUtility / legacy callers berbasis TimeManager.DayPhase.
        /// </summary>
        public void ApplyInstant(TimeManager.DayPhase phase)
        {
            ApplyPhaseSettingsInstant(phase == TimeManager.DayPhase.Night ? EnvironmentPhase.Night : EnvironmentPhase.Day);
        }

        #endregion

        #region Context Menu Testing Helpers

        [ContextMenu("Jump To: Dawn Settings")]
        private void JumpToDawn() => ApplyPhaseSettingsInstant(EnvironmentPhase.Dawn);

        [ContextMenu("Jump To: Day Settings")]
        private void JumpToDay() => ApplyPhaseSettingsInstant(EnvironmentPhase.Day);

        [ContextMenu("Jump To: Dusk Settings")]
        private void JumpToDusk() => ApplyPhaseSettingsInstant(EnvironmentPhase.Dusk);

        [ContextMenu("Jump To: Night Settings")]
        private void JumpToNight() => ApplyPhaseSettingsInstant(EnvironmentPhase.Night);

        [ContextMenu("Test Chromatic Aberration Impulse (0.75, 0.35s)")]
        private void TestChromaticImpulse() => TriggerChromaticImpulse(0.75f, 0.35f);

        #endregion

        #region Editor Validation & Reset

        private void Reset()
        {
            dawnSettings = CreateDefaultDawnSettings();
            daySettings = CreateDefaultDaySettings();
            duskSettings = CreateDefaultDuskSettings();
            nightSettings = CreateDefaultNightSettings();
            transitionDuration = 3.5f;

            if (baseVolume == null)
                baseVolume = GetComponent<Volume>();
        }

        private void OnValidate()
        {
            if (transitionDuration < 0.1f)
                transitionDuration = 0.1f;
        }

        #endregion
    }
}
