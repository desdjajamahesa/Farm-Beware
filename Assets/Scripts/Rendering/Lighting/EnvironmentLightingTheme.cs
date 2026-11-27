using System;
using UnityEngine;

namespace FeaturesRendering.Lighting
{
    /// <summary>
    /// Defines 24-hour environmental time phases.
    /// </summary>
    public enum EnvironmentPhase
    {
        Dawn = 0,       // Dawn / Early Morning (05:00 - 07:00)
        Day = 1,        // Daytime (07:00 - 17:00)
        Dusk = 2,       // Dusk / Late Afternoon (17:00 - 19:30)
        Night = 3       // Night / Night Brawl (19:30 - 05:00)
    }

    /// <summary>
    /// ScriptableObject serving as a Dependency Injection (DI) data asset
    /// for lighting and atmospheric environment configuration in Unity 6 URP.
    /// Stores Kelvin temperature palette, Adaptive Probe Volumes (APV) intensity,
    /// atmospheric fog, and exposure compensation (EV).
    /// </summary>
    [CreateAssetMenu(fileName = "New_EnvironmentLightingTheme", menuName = "Farm-Beware/Rendering/Environment Lighting Theme")]
    public class EnvironmentLightingTheme : ScriptableObject
    {
        [Header("Theme Metadata")]
        [Tooltip("Nama identifikasi tema atmosfer ini.")]
        [SerializeField] private string themeName = "Daylight Clean";

        [Tooltip("Fase waktu yang diasosiasikan dengan tema ini.")]
        [SerializeField] private EnvironmentPhase associatedPhase = EnvironmentPhase.Day;

        [Header("1. Main Light & Kelvin Temperature")]
        [Tooltip("Suhu warna cahaya matahari/bulan dalam skala Kelvin (1500K hangat s/d 20000K dingin). Contoh: Siang 5500K-6500K, Senja 3000K-3500K, Bulan 7500K-9000K.")]
        [Range(1500f, 20000f)]
        [SerializeField] private float colorTemperatureKelvin = 6500f;

        [Tooltip("Warna filter/tint cahaya directional utama.")]
        [SerializeField] private Color lightColorFilter = Color.white;

        [Tooltip("Intensitas directional light utama (lux / multiplier).")]
        [Min(0f)]
        [SerializeField] private float mainLightIntensity = 1.0f;

        [Tooltip("Rotasi sudut pencahayaan utama (Pitch X, Yaw Y, Roll Z).")]
        [SerializeField] private Vector3 mainLightEulerAngles = new Vector3(50f, -30f, 0f);

        [Header("2. Ambient & APV (Adaptive Probe Volumes)")]
        [Tooltip("Warna ambient langit (Sky) untuk mode Trilight/Gradient.")]
        [SerializeField] private Color ambientSkyColor = new Color(0.70f, 0.74f, 0.80f);

        [Tooltip("Warna ambient cakrawala (Equator).")]
        [SerializeField] private Color ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);

        [Tooltip("Warna ambient tanah (Ground).")]
        [SerializeField] private Color ambientGroundColor = new Color(0.35f, 0.32f, 0.28f);

        [Tooltip("Pengali intensitas Adaptive Probe Volumes (APV) untuk indirect diffuse bounce.")]
        [Range(0f, 5f)]
        [SerializeField] private float apvIntensityMultiplier = 1.0f;

        [Tooltip("Pengali pantulan refleksi global (Reflection Probes / Sky Reflection).")]
        [Range(0f, 2f)]
        [SerializeField] private float reflectionIntensity = 1.0f;

        [Header("3. Atmospheric Fog")]
        [Tooltip("Apakah kabut atmosfer diaktifkan pada tema ini?")]
        [SerializeField] private bool fogEnabled = true;

        [Tooltip("Mode kalkulasi kabut Unity RenderSettings.")]
        [SerializeField] private FogMode fogMode = FogMode.Linear;

        [Tooltip("Warna kabut atmosfer.")]
        [SerializeField] private Color fogColor = new Color(0.65f, 0.72f, 0.80f);

        [Tooltip("Jarak awal kabut (Linear Fog). Diatur di luar jangkauan langsung kamera isometrik agar gameplay tetap kontras.")]
        [Min(0f)]
        [SerializeField] private float fogStartDistance = 35f;

        [Tooltip("Jarak akhir kabut (Linear Fog).")]
        [Min(0f)]
        [SerializeField] private float fogEndDistance = 120f;

        [Tooltip("Kerapatan kabut jika menggunakan mode Exponential atau ExponentialSquared.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float fogDensity = 0.01f;

        [Header("4. Post-Processing & Exposure (EV)")]
        [Tooltip("Kompensasi eksposur dalam EV (Exposure Value). Contoh: Siang 0.0 s/d +0.5, Malam -1.0 s/d -2.0.")]
        [Range(-5f, 5f)]
        [SerializeField] private float exposureCompensationEV = 0.0f;

        [Tooltip("Kontras warna pasca-proses.")]
        [Range(-100f, 100f)]
        [SerializeField] private float contrast = 10f;

        [Tooltip("Warna filter color grading global.")]
        [SerializeField] private Color colorFilter = Color.white;

        [Tooltip("Intensitas bloom lembut perimeter.")]
        [Range(0f, 5f)]
        [SerializeField] private float bloomIntensity = 0.25f;

        [Header("5. Camera Clear & Background")]
        [Tooltip("Warna background kamera / solid color fallback.")]
        [SerializeField] private Color cameraBackgroundColor = new Color(0.12f, 0.15f, 0.20f);

        #region Public Properties (Encapsulation)

        public string ThemeName => themeName;
        public EnvironmentPhase AssociatedPhase => associatedPhase;
        public float ColorTemperatureKelvin => colorTemperatureKelvin;
        public Color LightColorFilter => lightColorFilter;
        public float MainLightIntensity => mainLightIntensity;
        public Vector3 MainLightEulerAngles => mainLightEulerAngles;
        public Color AmbientSkyColor => ambientSkyColor;
        public Color AmbientEquatorColor => ambientEquatorColor;
        public Color AmbientGroundColor => ambientGroundColor;
        public float ApvIntensityMultiplier => apvIntensityMultiplier;
        public float ReflectionIntensity => reflectionIntensity;
        public bool FogEnabled => fogEnabled;
        public FogMode FogMode => fogMode;
        public Color FogColor => fogColor;
        public float FogStartDistance => fogStartDistance;
        public float FogEndDistance => fogEndDistance;
        public float FogDensity => fogDensity;
        public float ExposureCompensationEV => exposureCompensationEV;
        public float Contrast => contrast;
        public Color ColorFilter => colorFilter;
        public float BloomIntensity => bloomIntensity;
        public Color CameraBackgroundColor => cameraBackgroundColor;

        #endregion

        #region Kelvin to RGB Utility

        /// <summary>
        /// Converts Kelvin color temperature (1000K - 40000K) to a calibrated RGB Color.
        /// Uses Tanner Helland's algorithm optimized for real-time graphics.
        /// </summary>
        public static Color ConvertKelvinToRGB(float kelvin)
        {
            float temp = Mathf.Clamp(kelvin, 1000f, 40000f) / 100f;
            float red, green, blue;

            // Compute Red
            if (temp <= 66f)
            {
                red = 255f;
            }
            else
            {
                red = temp - 60f;
                red = 329.698727446f * Mathf.Pow(red, -0.1332047592f);
                red = Mathf.Clamp(red, 0f, 255f);
            }

            // Compute Green
            if (temp <= 66f)
            {
                green = temp;
                green = 99.4708025861f * Mathf.Log(green) - 161.1195681661f;
                green = Mathf.Clamp(green, 0f, 255f);
            }
            else
            {
                green = temp - 60f;
                green = 288.1221695283f * Mathf.Pow(green, -0.0755148492f);
                green = Mathf.Clamp(green, 0f, 255f);
            }

            // Compute Blue
            if (temp >= 66f)
            {
                blue = 255f;
            }
            else if (temp <= 19f)
            {
                blue = 0f;
            }
            else
            {
                blue = temp - 10f;
                blue = 138.5177312231f * Mathf.Log(blue) - 305.0447927307f;
                blue = Mathf.Clamp(blue, 0f, 255f);
            }

            return new Color(red / 255f, green / 255f, blue / 255f);
        }

        /// <summary>
        /// Computes the final directional light color by multiplying the Kelvin temperature color and Color Filter.
        /// </summary>
        public Color GetEvaluatedLightColor()
        {
            Color kelvinRgb = ConvertKelvinToRGB(colorTemperatureKelvin);
            return kelvinRgb * lightColorFilter;
        }

        /// <summary>
        /// Immutable pure-value result produced by <see cref="EvaluateSolarTrajectory"/>.
        /// </summary>
        public readonly struct SolarEvaluationResult
        {
            public readonly float Azimuth;
            public readonly float Elevation;
            public readonly float Kelvin;
            public readonly float Intensity;
            public readonly Color LightColor;

            public SolarEvaluationResult(float azimuth, float elevation, float kelvin, float intensity, Color lightColor)
            {
                Azimuth = azimuth;
                Elevation = elevation;
                Kelvin = kelvin;
                Intensity = intensity;
                LightColor = lightColor;
            }
        }

        /// <summary>
        /// Evaluates directional sun azimuth, sinusoidal elevation, afternoon Kelvin warmth curve, intensity, and RGB color.
        /// Pure Zero-GC helper shared by runtime atmosphere directors and sun lighting observers.
        /// </summary>
        public static SolarEvaluationResult EvaluateSolarTrajectory(
            float hour,
            float sunriseHour,
            float sunsetHour,
            float sunriseAzimuth,
            float sunsetAzimuth,
            float minElevation,
            float maxElevation,
            float afternoonStartHour,
            float noonKelvin,
            float duskKelvin,
            float kelvinFalloffExponent,
            float minDayIntensity,
            float maxDayIntensity)
        {
            float dayDuration = Mathf.Max(0.0001f, sunsetHour - sunriseHour);
            float normalizedDay = Mathf.Clamp01((hour - sunriseHour) / dayDuration);

            float azimuth = Mathf.Lerp(sunriseAzimuth, sunsetAzimuth, normalizedDay);
            float elevationFactor = Mathf.Sin(normalizedDay * Mathf.PI);
            float elevation = Mathf.Lerp(minElevation, maxElevation, elevationFactor);

            float kelvin;
            if (hour < afternoonStartHour)
            {
                kelvin = noonKelvin;
            }
            else
            {
                float afternoonDuration = Mathf.Max(0.0001f, sunsetHour - afternoonStartHour);
                float normalizedAfternoon = Mathf.Clamp01((hour - afternoonStartHour) / afternoonDuration);
                float expFactor = Mathf.Pow(normalizedAfternoon, kelvinFalloffExponent);
                kelvin = Mathf.Lerp(noonKelvin, duskKelvin, expFactor);
            }

            float intensity = Mathf.Lerp(minDayIntensity, maxDayIntensity, elevationFactor);
            Color lightColor = ConvertKelvinToRGB(kelvin);
            return new SolarEvaluationResult(azimuth, elevation, kelvin, intensity, lightColor);
        }

        #endregion

        #region Factory & Interpolation (Clean Pure Methods)

        /// <summary>
        /// Creates a new runtime instance by linearly interpolating between two EnvironmentLightingTheme assets.
        /// Side-effect-free pure method ready for smooth Phase 2 transitions.
        /// </summary>
        public static EnvironmentLightingTheme Lerp(EnvironmentLightingTheme from, EnvironmentLightingTheme to, float t)
        {
            if (from == null && to == null) return null;
            if (from == null) return to;
            if (to == null) return from;

            float factor = Mathf.Clamp01(t);
            var result = CreateInstance<EnvironmentLightingTheme>();

            result.themeName = $"{from.themeName} -> {to.themeName} ({factor:P0})";
            result.associatedPhase = factor < 0.5f ? from.associatedPhase : to.associatedPhase;

            result.colorTemperatureKelvin = Mathf.Lerp(from.colorTemperatureKelvin, to.colorTemperatureKelvin, factor);
            result.lightColorFilter = Color.Lerp(from.lightColorFilter, to.lightColorFilter, factor);
            result.mainLightIntensity = Mathf.Lerp(from.mainLightIntensity, to.mainLightIntensity, factor);
            result.mainLightEulerAngles = new Vector3(
                Mathf.LerpAngle(from.mainLightEulerAngles.x, to.mainLightEulerAngles.x, factor),
                Mathf.LerpAngle(from.mainLightEulerAngles.y, to.mainLightEulerAngles.y, factor),
                Mathf.LerpAngle(from.mainLightEulerAngles.z, to.mainLightEulerAngles.z, factor)
            );

            result.ambientSkyColor = Color.Lerp(from.ambientSkyColor, to.ambientSkyColor, factor);
            result.ambientEquatorColor = Color.Lerp(from.ambientEquatorColor, to.ambientEquatorColor, factor);
            result.ambientGroundColor = Color.Lerp(from.ambientGroundColor, to.ambientGroundColor, factor);

            result.apvIntensityMultiplier = Mathf.Lerp(from.apvIntensityMultiplier, to.apvIntensityMultiplier, factor);
            result.reflectionIntensity = Mathf.Lerp(from.reflectionIntensity, to.reflectionIntensity, factor);

            result.fogEnabled = factor < 0.5f ? from.fogEnabled : to.fogEnabled;
            result.fogMode = factor < 0.5f ? from.fogMode : to.fogMode;
            result.fogColor = Color.Lerp(from.fogColor, to.fogColor, factor);
            result.fogStartDistance = Mathf.Lerp(from.fogStartDistance, to.fogStartDistance, factor);
            result.fogEndDistance = Mathf.Lerp(from.fogEndDistance, to.fogEndDistance, factor);
            result.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, factor);

            result.exposureCompensationEV = Mathf.Lerp(from.exposureCompensationEV, to.exposureCompensationEV, factor);
            result.contrast = Mathf.Lerp(from.contrast, to.contrast, factor);
            result.colorFilter = Color.Lerp(from.colorFilter, to.colorFilter, factor);
            result.bloomIntensity = Mathf.Lerp(from.bloomIntensity, to.bloomIntensity, factor);
            result.cameraBackgroundColor = Color.Lerp(from.cameraBackgroundColor, to.cameraBackgroundColor, factor);

            return result;
        }

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            if (fogStartDistance > fogEndDistance)
            {
                fogEndDistance = fogStartDistance + 1.0f;
            }
        }

        #endregion
    }
}
