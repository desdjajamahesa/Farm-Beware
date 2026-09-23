using UnityEngine;
using FeaturesTime;

namespace FeaturesRendering.Lighting
{
    /// <summary>
    /// Observer/Listener untuk mengontrol rotasi, iluminasi matematis, dan suhu kelvin matahari (Directional Light).
    /// Mematuhi Clean Architecture, SOLID, dan Event-Driven OOP.
    ///
    /// ATURAN ARSITEKTUR KETAT:
    /// Komponen ini TIDAK MEMILIKI kode iterasi di siklus Update() utama.
    /// Seluruh pembaruan posisi azimuth, elevasi, intensitas lux, dan suhu warna kelvin
    /// dipicu secara murni oleh event OnMinuteChanged / OnTimePhaseChanged dari DayNightTimeManager.
    /// Hal ini mengeliminasi overhead CPU per-frame dan memastikan pembaruan terisolasi hanya saat jam bergerak.
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
        [Range(0.05f, 1.0f)]
        [SerializeField] private float nightIntensity = 0.6f;

        [Tooltip("Warna spektrum cahaya bulan malam hari (biru dingin).")]
        [SerializeField] private Color nightMoonlightColor = new Color(0.18f, 0.28f, 0.65f);

        [Tooltip("Sudut rotasi Directional Light di malam hari (Pitch 55°, Yaw 35°).")]
        [SerializeField] private Vector3 nightEulerAngles = new Vector3(55f, 35f, 0f);

        [Header("1. Trajektori Matematis Matahari (Solar Trajectory)")]
        [Tooltip("Jam dimulainya fajar/siang (Matahari mulai terbit di ufuk timur-tenggara).")]
        [Range(4f, 8f)]
        [SerializeField] private float sunriseHour = 6.0f;

        [Tooltip("Jam berakhirnya siang/senja (Matahari tenggelam di ufuk barat-daya).")]
        [Range(17f, 21f)]
        [SerializeField] private float sunsetHour = 18.5f;

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
        [Tooltip("Suhu kelvin di siang hari puncak (10:00 - 14:00). Standar siang seimbang: 5500K.")]
        [Range(4000f, 7500f)]
        [SerializeField] private float noonKelvin = 5500f;

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

        private void OnEnable()
        {
            // Mendaftar sebagai Observer ke DayNightTimeManager
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnMinuteChanged += HandleMinuteChanged;
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandlePhaseChanged;

                // Evaluasi awal saat komponen aktif
                EvaluateSunParameters(DayNightTimeManager.Instance.CurrentHour);
            }
        }

        private void OnDisable()
        {
            // Pelepasan pendaftaran bersih untuk mencegah memory leak / dangling delegate
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnMinuteChanged -= HandleMinuteChanged;
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandlePhaseChanged;
            }
        }

        private void Start()
        {
            // Defensive resolution jika DayNightTimeManager belum siap di Awake/OnEnable
            if (DayNightTimeManager.Instance != null)
            {
                EvaluateSunParameters(DayNightTimeManager.Instance.CurrentHour);
            }
            else
            {
                // Fallback default: tengah hari 12:00
                EvaluateSunParameters(12.0f);
            }
        }

        // CATATAN ARSITEKTUR:
        // Update() DIHAPUSKAN SECARA EKSPLISIT.
        // Tidak ada perulangan polling render loop di komponen ini.

        #endregion

        #region Observer Event Handlers

        /// <summary>
        /// Handler dipanggil secara reaktif hanya ketika menit berganti pada DayNightTimeManager.
        /// </summary>
        private void HandleMinuteChanged(int minute)
        {
            if (DayNightTimeManager.Instance != null)
            {
                EvaluateSunParameters(DayNightTimeManager.Instance.CurrentHour);
            }
        }

        /// <summary>
        /// Handler dipanggil saat transisi fase besar (Dawn, Day, Dusk, Night).
        /// </summary>
        private void HandlePhaseChanged(EnvironmentPhase phase)
        {
            isDaytime = (phase == EnvironmentPhase.Dawn || phase == EnvironmentPhase.Day || phase == EnvironmentPhase.Dusk);

            if (DayNightTimeManager.Instance != null)
            {
                EvaluateSunParameters(DayNightTimeManager.Instance.CurrentHour);
            }
        }

        #endregion

        #region Core Mathematical Solar Model

        /// <summary>
        /// Menghitung trajektori rotasi matahari, suhu kelvin, dan intensitas secara matematis
        /// berdasarkan jam kontinu (0.0f - 24.0f).
        /// </summary>
        public void EvaluateSunParameters(float hour)
        {
            if (sunLight == null) return;

            // Apakah berada di dalam siklus matahari siang?
            if (hour >= sunriseHour && hour <= sunsetHour)
            {
                isDaytime = true;
                float dayDuration = sunsetHour - sunriseHour;
                float normalizedDay = Mathf.Clamp01((hour - sunriseHour) / dayDuration);

                // 1. Azimuth: Pergerakan linear dari sunriseAzimuth (135°) ke sunsetAzimuth (225°)
                currentAzimuth = Mathf.Lerp(sunriseAzimuth, sunsetAzimuth, normalizedDay);

                // 2. Elevasi (Altitude): Kurva sinus halus dari cakrawala -> puncak tengah hari -> cakrawala
                // sin(0) = 0, sin(pi/2) = 1 (tengah hari), sin(pi) = 0
                float elevationFactor = Mathf.Sin(normalizedDay * Mathf.PI);
                currentElevation = Mathf.Lerp(minElevation, maxElevation, elevationFactor);

                // 3. Suhu Kelvin: Penurunan eksponensial di sore hari (5500K -> 3200K)
                if (hour < afternoonStartHour)
                {
                    // Pagi s/d Siang Puncak: Suhu daylight netral stabil (5500K)
                    currentKelvin = noonKelvin;
                }
                else
                {
                    // Sore ke Senja: Penurunan eksponensial akibat hamburan Rayleigh
                    float afternoonDuration = sunsetHour - afternoonStartHour;
                    float normalizedAfternoon = Mathf.Clamp01((hour - afternoonStartHour) / afternoonDuration);

                    // t^p: penurunan lambat di awal sore, lalu melandai curam ke oranye hangat di senja
                    float expFactor = Mathf.Pow(normalizedAfternoon, kelvinFalloffExponent);
                    currentKelvin = Mathf.Lerp(noonKelvin, duskKelvin, expFactor);
                }

                // 4. Intensitas Matahari: Mengikuti elevasi matahari di langit
                currentIntensity = Mathf.Lerp(minDayIntensity, maxIntensity, elevationFactor);

                // Evaluasi warna RGB dari Kelvin
                currentEvaluatedColor = EnvironmentLightingTheme.ConvertKelvinToRGB(currentKelvin);

                // Terapkan ke Directional Light
                ApplyToLight(currentElevation, currentAzimuth, currentEvaluatedColor, currentIntensity);
            }
            else
            {
                // Phase 3: Night Lighting (Pencahayaan Malam Minimal & APV Sync)
                ApplyNightLighting();
            }
        }

        /// <summary>
        /// Mengatur pencahayaan malam hari (Phase 3: Night Lighting):
        /// Meredupkan Directional Light ke intensitas 0.1 - 0.2 lux dengan spektrum biru-keunguan
        /// untuk memastikan visibilitas orientasi spasial pemain di mode Action Survival.
        /// Menyelaraskan ambient dan kabut atmosfer agar APV Sky Occlusion merefleksikan cahaya malam.
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
                currentIntensity = Mathf.Clamp(nightTheme.MainLightIntensity, 0.05f, 1.0f);
                ApplyToLight(nightTheme.MainLightEulerAngles.x, nightTheme.MainLightEulerAngles.y, currentEvaluatedColor, currentIntensity);

                // Sinkronisasi Ambient & APV Sky Occlusion
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
                RenderSettings.ambientSkyColor = new Color(0.15f, 0.18f, 0.28f);
                RenderSettings.ambientEquatorColor = new Color(0.10f, 0.12f, 0.18f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.08f);
                RenderSettings.fogColor = new Color(0.08f, 0.11f, 0.20f);
            }
        }

        private void ApplyToLight(float pitch, float yaw, Color color, float intensity)
        {
            sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            sunLight.color = color;
            sunLight.intensity = intensity;
        }

        /// <summary>
        /// Memastikan konfigurasi bayangan Directional Light terkalibrasi optimal:
        /// Soft Shadows aktif, Shadow Near Plane Offset aman dari Peter-panning.
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
        /// Mengatur rotasi matahari secara manual untuk tujuan pengujian visual / testing harness.
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
