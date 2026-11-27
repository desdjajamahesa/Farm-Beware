using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesRendering.Lighting
{
    /// <summary>
    /// Observer/Listener controlling solar rotation, mathematical illumination, and Kelvin color temperature (Directional Light).
    /// Adheres to Clean Architecture, SOLID, and Event-Driven OOP.
    ///
    /// STRICT ARCHITECTURE RULE:
    /// This component has NO Update() polling iteration for solar progression.
    /// All azimuth, elevation, lux intensity, and Kelvin color temperature updates
    /// are strictly event-driven via DayNightTimeManager.OnMinuteChanged / OnTimePhaseChanged.
    /// This eliminates per-frame CPU overhead and isolates updates to time-tick boundaries.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class DaySunLightingObserver : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Target References")]
        [Tooltip("Directional Light matahari utama. Jika kosong, otomatis mengambil komponen Light pada objek ini.")]
        [SerializeField] private Light sunLight;

        [Header("Optional Theme Injections (DI)")]
        [Tooltip("Referensi ScriptableObject tema siang untuk sinkronisasi nilai dasar.")]
        [SerializeField] private EnvironmentLightingTheme dayTheme;

        [Tooltip("Referensi ScriptableObject tema senja untuk sinkronisasi nilai dasar.")]
        [SerializeField] private EnvironmentLightingTheme duskTheme;

        [Tooltip("Referensi ScriptableObject tema malam untuk sinkronisasi nilai dasar APV dan spektrum cahaya.")]
        [SerializeField] private EnvironmentLightingTheme nightTheme;

        [Header("Phase 3: Night Lighting (Combat Readability)")]
        [Tooltip("Intensitas Directional Light di malam hari.")]
        [Range(0.05f, 3.0f)]
        [SerializeField] private float nightIntensity = 1.5f;

        [Tooltip("Warna spektrum cahaya bulan malam hari (biru dingin).")]
        [SerializeField] private Color nightMoonlightColor = new Color(0.18f, 0.28f, 0.65f);

        [Tooltip("Sudut rotasi Directional Light di malam hari (Pitch 55°, Yaw 35°).")]
        [SerializeField] private Vector3 nightEulerAngles = new Vector3(55f, 35f, 0f);

        [Header("Director Delegation")]
        [Tooltip("Jika true, komponen ini menyerahkan kendali rotasi dan ambient ke LightingAndAtmosphereDirector.")]
        [SerializeField] private bool yieldToDirector = false;

        public bool YieldToDirector
        {
            get => yieldToDirector;
            set => yieldToDirector = value;
        }

        [Header("1. Trajektori Matematis Matahari (Solar Trajectory)")]
        [Tooltip("Jam dimulainya fajar/siang (Matahari mulai terbit di ufuk timur-tenggara).")]
        [Range(4f, 8f)]
        [SerializeField] private float sunriseHour = 6.0f;

        [Tooltip("Jam berakhirnya siang/senja (Matahari tenggelam di ufuk barat-daya).")]
        [Range(17f, 21f)]
        [SerializeField] private float sunsetHour = 18.0f;

        [Tooltip("Sudut Azimuth saat matahari terbit (derajat horizontal). Rekomendasi: 135° (Timur-Tenggara).")]
        [Range(0f, 360f)]
        [SerializeField] private float sunriseAzimuth = 135f;

        [Tooltip("Sudut Azimuth saat matahari terbenam (derajat horizontal). Rekomendasi: 225° (Barat-Daya).")]
        [Range(0f, 360f)]
        [SerializeField] private float sunsetAzimuth = 225f;

        [Tooltip("Elevasi maksimum matahari di tengah hari (derajat pitch vertikal).")]
        [Range(30f, 80f)]
        [SerializeField] private float maxElevation = 55f;

        [Tooltip("Elevasi minimum di cakrawala saat fajar/senja.")]
        [Range(2f, 20f)]
        [SerializeField] private float minElevation = 10f;

        [Header("2. Kurva Eksponensial Suhu Kelvin (5500K -> 3200K)")]
        [Tooltip("Suhu kelvin di siang hari puncak (10:00 - 14:00). Standar siang keemasan pastoral: 5000K.")]
        [Range(4000f, 7500f)]
        [SerializeField] private float noonKelvin = 5000f;

        [Tooltip("Suhu kelvin terendah saat senja (18:30). Oranye hangat atmosfer: 3200K.")]
        [Range(2000f, 4000f)]
        [SerializeField] private float duskKelvin = 3200f;

        [Tooltip("Jam dimulainya transisi penurunan suhu warna di sore hari.")]
        [Range(12f, 16f)]
        [SerializeField] private float afternoonStartHour = 14.0f;

        [Tooltip("Faktor eksponensial penurunan suhu sore ke senja (meniru hamburan Rayleigh).")]
        [Range(1.0f, 4.0f)]
        [SerializeField] private float kelvinFalloffExponent = 2.2f;

        [Header("3. Intensitas Cahaya (Solar Lux)")]
        [Tooltip("Intensitas puncak matahari di tengah hari (lux / multiplier).")]
        [Min(0f)]
        [SerializeField] private float maxIntensity = 1.15f;

        [Tooltip("Intensitas minimum saat fajar/senja sebelum beralih ke cahaya bulan.")]
        [Min(0f)]
        [SerializeField] private float minDayIntensity = 0.2f;

        [Header("4. Shadow Peter-Panning Guard")]
        [Tooltip("Batas Shadow Near Plane Offset Directional Light untuk mencegah pemutusan bayangan dari tanah.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float shadowNearPlaneOffset = 0.15f;

        #endregion

        #region Runtime State (Read-Only)

        [Header("Runtime Evaluated Values (Read Only)")]
        [SerializeField] private float currentAzimuth = 180f;
        [SerializeField] private float currentElevation = 55f;
        [SerializeField] private float currentKelvin = 5500f;
        [SerializeField] private Color currentEvaluatedColor = Color.white;
        [SerializeField] private float currentIntensity = 1.0f;
        [SerializeField] private bool isDaytime = true;

        #endregion

        #region Unity Lifecycle (Event-Driven Registration)

        private void Awake()
        {
            if (sunLight == null)
                sunLight = GetComponent<Light>();

            EnforceShadowSettings();
        }

        private bool isSubscribed = false;

        private void OnEnable()
        {
            if (yieldToDirector) return;
            TrySubscribeTimeService();
        }

        private void Start()
        {
            if (yieldToDirector) return;
            if (!isSubscribed)
            {
                TrySubscribeTimeService();
            }

            // Defensive resolution if ITimeService is not ready in Awake/OnEnable
            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null)
            {
                if (!timeService.UseContinuousTime)
                {
                    if (timeService.IsNight)
                        ApplyNightLighting();
                    else
                        ApplyDayLighting();
                }
                else
                {
                    EvaluateSunParameters(timeService.CurrentHour);
                }
            }
        }

        private void Update()
        {
            if (yieldToDirector) return;
            if (!isSubscribed)
            {
                TrySubscribeTimeService();
            }
        }

        private void TrySubscribeTimeService()
        {
            if (isSubscribed) return;
            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null)
            {
                timeService.OnMinuteChanged += HandleMinuteChanged;
                timeService.OnPhaseChanged += HandlePhaseChanged;
                EvaluateSunParameters(timeService.CurrentHour);
                isSubscribed = true;
            }
        }

        private void OnDisable()
        {
            if (isSubscribed)
            {
                var timeService = ServiceLocator.Resolve<ITimeService>();
                if (timeService != null)
                {
                    timeService.OnMinuteChanged -= HandleMinuteChanged;
                    timeService.OnPhaseChanged -= HandlePhaseChanged;
                }
                isSubscribed = false;
            }
        }

        // ARCHITECTURE NOTE:
        // Solar polling in Update() is intentionally omitted.
        // No continuous render-loop polling occurs in this component.

        #endregion

        #region Observer Event Handlers

        /// <summary>
        /// Reactive handler invoked only when the minute changes on ITimeService.
        /// </summary>
        private void HandleMinuteChanged(int minute)
        {
            if (yieldToDirector) return;

            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null && timeService.UseContinuousTime)
            {
                EvaluateSunParameters(timeService.CurrentHour);
            }
        }

        /// <summary>
        /// Handler invoked on major phase transitions (Dawn, Day, Dusk, Night).
        /// </summary>
        private void HandlePhaseChanged(int day, DayPhase phase)
        {
            if (yieldToDirector) return;

            isDaytime = (phase == DayPhase.Dawn || phase == DayPhase.Day || phase == DayPhase.Dusk);

            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null && !timeService.UseContinuousTime)
            {
                if (isDaytime)
                    ApplyDayLighting();
                else
                    ApplyNightLighting();
                return;
            }

            if (timeService != null)
            {
                EvaluateSunParameters(timeService.CurrentHour);
            }
        }

        #endregion

        #region Core Mathematical Solar Model

        /// <summary>
        /// Mathematically evaluates sun rotation trajectory, Kelvin temperature, and intensity
        /// from the continuous hour (0.0f - 24.0f).
        /// </summary>
        public void EvaluateSunParameters(float hour)
        {
            if (sunLight == null) return;

            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null && !timeService.UseContinuousTime)
            {
                bool isDay = !timeService.IsNight;
                if (isDay)
                    ApplyDayLighting();
                else
                    ApplyNightLighting();
                return;
            }

            // Within the daytime solar cycle?
            if (hour >= sunriseHour && hour <= sunsetHour)
            {
                isDaytime = true;
                var solar = EnvironmentLightingTheme.EvaluateSolarTrajectory(
                    hour,
                    sunriseHour,
                    sunsetHour,
                    sunriseAzimuth,
                    sunsetAzimuth,
                    minElevation,
                    maxElevation,
                    afternoonStartHour,
                    noonKelvin,
                    duskKelvin,
                    kelvinFalloffExponent,
                    minDayIntensity,
                    maxIntensity);

                currentAzimuth = solar.Azimuth;
                currentElevation = solar.Elevation;
                currentKelvin = solar.Kelvin;
                currentIntensity = solar.Intensity;
                currentEvaluatedColor = solar.LightColor;

                ApplyToLight(currentElevation, currentAzimuth, currentEvaluatedColor, currentIntensity);
            }
            else
            {
                // Phase 3: Night Lighting (Minimal Night Lighting & APV Sync)
                ApplyNightLighting();
            }
        }

        /// <summary>
        /// Configures daytime lighting (Phase 2: Day Lighting):
        /// Applies static dayTheme values (pitch 50 deg, yaw -30 deg, lux 1.1, 5000K daylight)
        /// without moving trajectory iteration when continuous time is disabled.
        /// </summary>
        public void ApplyDayLighting()
        {
            if (sunLight == null) return;

            isDaytime = true;

            if (dayTheme != null)
            {
                currentAzimuth = dayTheme.MainLightEulerAngles.y;
                currentElevation = dayTheme.MainLightEulerAngles.x;
                currentIntensity = Mathf.Clamp(dayTheme.MainLightIntensity, 0.1f, 3.0f);
                currentEvaluatedColor = dayTheme.GetEvaluatedLightColor();
                currentKelvin = dayTheme.ColorTemperatureKelvin;
                ApplyToLight(currentElevation, currentAzimuth, currentEvaluatedColor, currentIntensity);

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = dayTheme.AmbientSkyColor;
                RenderSettings.ambientEquatorColor = dayTheme.AmbientEquatorColor;
                RenderSettings.ambientGroundColor = dayTheme.AmbientGroundColor;
                RenderSettings.fogColor = dayTheme.FogColor;
                RenderSettings.fogStartDistance = dayTheme.FogStartDistance;
                RenderSettings.fogEndDistance = dayTheme.FogEndDistance;
            }
            else
            {
                currentAzimuth = -30f;
                currentElevation = 50f;
                currentIntensity = 1.0f;
                currentKelvin = 5000f;
                currentEvaluatedColor = new Color(1.0f, 0.95f, 0.88f);
                ApplyToLight(currentElevation, currentAzimuth, currentEvaluatedColor, currentIntensity);
            }
        }

        /// <summary>
        /// Configures nighttime lighting (Phase 3: Night Lighting):
        /// Dims the Directional Light to 0.1 - 0.2 lux with a blue-violet spectrum
        /// to preserve spatial orientation visibility in Action Survival mode.
        /// Aligns ambient and atmospheric fog so APV Sky Occlusion reflects night lighting.
        /// </summary>
        public void ApplyNightLighting()
        {
            if (sunLight == null) return;

            isDaytime = false;
            currentAzimuth = nightEulerAngles.y;
            currentElevation = nightEulerAngles.x;
            currentIntensity = nightIntensity;
            currentKelvin = 11500f;

            if (nightTheme != null)
            {
                currentEvaluatedColor = nightTheme.GetEvaluatedLightColor();
                currentIntensity = Mathf.Clamp(nightTheme.MainLightIntensity, 0.05f, 3.0f);
                ApplyToLight(nightTheme.MainLightEulerAngles.x, nightTheme.MainLightEulerAngles.y, currentEvaluatedColor, currentIntensity);

                // Sync Ambient & APV Sky Occlusion
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = nightTheme.AmbientSkyColor;
                RenderSettings.ambientEquatorColor = nightTheme.AmbientEquatorColor;
                RenderSettings.ambientGroundColor = nightTheme.AmbientGroundColor;
                RenderSettings.fogColor = nightTheme.FogColor;
                RenderSettings.fogStartDistance = nightTheme.FogStartDistance;
                RenderSettings.fogEndDistance = nightTheme.FogEndDistance;
            }
            else
            {
                currentEvaluatedColor = nightMoonlightColor;
                ApplyToLight(nightEulerAngles.x, nightEulerAngles.y, currentEvaluatedColor, currentIntensity);

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.36f, 0.44f, 0.58f);
                RenderSettings.ambientEquatorColor = new Color(0.28f, 0.34f, 0.44f);
                RenderSettings.ambientGroundColor = new Color(0.22f, 0.26f, 0.32f);
                RenderSettings.fogColor = new Color(0.18f, 0.24f, 0.34f);
            }
        }

        private void ApplyToLight(float pitch, float yaw, Color color, float intensity)
        {
            sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            sunLight.color = color;
            sunLight.intensity = intensity;
        }

        /// <summary>
        /// Ensures Directional Light shadow settings are calibrated:
        /// Soft Shadows enabled, Shadow Near Plane Offset guarded against Peter-panning.
        /// </summary>
        public void EnforceShadowSettings()
        {
            if (sunLight == null) return;

            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowNearPlane = Mathf.Clamp(shadowNearPlaneOffset, 0.05f, 0.3f);
        }

        #endregion

        #region Public Inspection & Testing Interface

        /// <summary>
        /// Manually sets sun rotation for visual verification / testing harness use.
        /// </summary>
        public void SetManualSunRotation(float azimuth, float elevation, float kelvin, float intensity)
        {
            currentAzimuth = azimuth;
            currentElevation = elevation;
            currentKelvin = kelvin;
            currentIntensity = intensity;
            currentEvaluatedColor = EnvironmentLightingTheme.ConvertKelvinToRGB(kelvin);

            ApplyToLight(elevation, azimuth, currentEvaluatedColor, intensity);
        }

        public float CurrentAzimuth => currentAzimuth;
        public float CurrentElevation => currentElevation;
        public float CurrentKelvin => currentKelvin;
        public float CurrentIntensity => currentIntensity;
        public Color CurrentEvaluatedColor => currentEvaluatedColor;

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            if (sunLight == null)
                sunLight = GetComponent<Light>();

            if (sunLight != null)
            {
                EnforceShadowSettings();
            }

            if (sunriseHour >= sunsetHour)
                sunsetHour = sunriseHour + 1.0f;

            if (afternoonStartHour < sunriseHour || afternoonStartHour > sunsetHour)
                afternoonStartHour = sunriseHour + (sunsetHour - sunriseHour) * 0.6f;
        }

        #endregion
    }
}
