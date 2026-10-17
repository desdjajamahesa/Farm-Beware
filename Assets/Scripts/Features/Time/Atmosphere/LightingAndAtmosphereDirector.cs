using System;
using System.Collections;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesRendering.Lighting;

namespace Features.Time.Atmosphere
{
    /// <summary>
    /// Centralized Facade & Orchestrator for all environment lighting, solar trajectory,
    /// ambient trilight, and atmospheric visual synchronization in Farm-Beware.
    ///
    /// Architectural Constraints:
    /// - Zero per-frame polling in Update() (Purely Event-Driven from ITimeService).
    /// - Thread-safe Unity lifecycle singleton with clean nullification on destroy.
    /// - Deferred ITimeService resolution in Start() to prevent ServiceLocator race conditions.
    /// - Discrete step-syncing with dirty-checking for RenderSettings (prevents per-tick state dirtying).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public class LightingAndAtmosphereDirector : MonoBehaviour
    {
        public static LightingAndAtmosphereDirector Instance { get; private set; }

        #region Serialized Fields

        [Header("Target Light References")]
        [Tooltip("Main Directional Light representing Sun and Moon. If null, auto-resolved in scene.")]
        [SerializeField] private Light sunLight;

        [Header("Atmosphere Themes (ScriptableObjects)")]
        [Tooltip("Theme configuration for Dawn (05:00 - 07:00).")]
        [SerializeField] private EnvironmentLightingTheme dawnTheme;

        [Tooltip("Theme configuration for Day (07:00 - 17:00).")]
        [SerializeField] private EnvironmentLightingTheme dayTheme;

        [Tooltip("Theme configuration for Dusk (17:00 - 19:30).")]
        [SerializeField] private EnvironmentLightingTheme duskTheme;

        [Tooltip("Theme configuration for Night (19:30 - 05:00).")]
        [SerializeField] private EnvironmentLightingTheme nightTheme;

        [Header("Subordinate Controllers (Optional / Auto-Resolved)")]
        [SerializeField] private FeaturesTime.Atmosphere.DayNightVolumeController volumeController;
        [SerializeField] private FeaturesTime.Atmosphere.HouseSafeZoneLighting safeZoneLighting;
        [SerializeField] private FeaturesTime.Atmosphere.OutdoorLightingController outdoorLighting;
        [SerializeField] private FeaturesTime.Atmosphere.ShelterLanternController shelterLantern;

        [Header("Solar Trajectory Calibration")]
        [Range(4f, 8f)]
        [SerializeField] private float sunriseHour = 6.0f;

        [Range(17f, 21f)]
        [SerializeField] private float sunsetHour = 18.0f;

        [Range(0f, 360f)]
        [SerializeField] private float sunriseAzimuth = 135f;

        [Range(0f, 360f)]
        [SerializeField] private float sunsetAzimuth = 225f;

        [Range(30f, 80f)]
        [SerializeField] private float maxElevation = 55f;

        [Range(2f, 20f)]
        [SerializeField] private float minElevation = 10f;

        [Header("Kelvin & Intensity Modulation")]
        [Range(4000f, 7500f)]
        [SerializeField] private float noonKelvin = 5000f;

        [Range(2000f, 4000f)]
        [SerializeField] private float duskKelvin = 3200f;

        [Range(12f, 16f)]
        [SerializeField] private float afternoonStartHour = 14.0f;

        [Range(1.0f, 4.0f)]
        [SerializeField] private float kelvinFalloffExponent = 2.2f;

        [Min(0f)]
        [SerializeField] private float maxDayIntensity = 1.15f;

        [Min(0f)]
        [SerializeField] private float minDayIntensity = 0.2f;

        [Header("Night Preset Parameters")]
        [Range(0.05f, 3.0f)]
        [SerializeField] private float nightIntensity = 1.3f;

        [SerializeField] private Color nightMoonlightColor = new Color(0.58f, 0.76f, 1.0f);
        [SerializeField] private Vector3 nightEulerAngles = new Vector3(55f, 35f, 0f);

        [Header("Character Night Highlight & Shading Floor")]
        [Tooltip("Hour when dusk highlight starts ramping up from 0 to 1.")]
        [Range(14f, 18f)]
        [SerializeField] private float duskHighlightStartHour = 16.0f;

        [Tooltip("Hour when night highlight reaches full intensity 1.0.")]
        [Range(17f, 20f)]
        [SerializeField] private float duskHighlightFullHour = 18.0f;

        [Tooltip("Hour when dawn highlight starts ramping down from 1 to 0.")]
        [Range(4f, 6f)]
        [SerializeField] private float dawnHighlightStartHour = 5.0f;

        [Tooltip("Hour when dawn highlight reaches 0.0 (fully daytime baseline).")]
        [Range(5f, 7f)]
        [SerializeField] private float dawnHighlightEndHour = 6.0f;

        #endregion

        #region Private Fields & Cached State

        private ITimeService registeredTimeService;
        private bool isSubscribed = false;
        private DayPhase currentPhase = DayPhase.Day;

        // RenderSettings Dirty-Check Cache
        private Color cachedSkyColor = Color.clear;
        private Color cachedEquatorColor = Color.clear;
        private Color cachedGroundColor = Color.clear;
        private Color cachedFogColor = Color.clear;
        private float cachedFogStart = -1f;
        private float cachedFogEnd = -1f;

        // Night Highlight Weight Cache
        private float currentNightHighlightWeight = -1f;
        public float CurrentNightHighlightWeight => Mathf.Max(0f, currentNightHighlightWeight);

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

            ResolveComponents();
        }

        private void Start()
        {
            // Deferred resolution in Start() to prevent ServiceLocator race condition
            SubscribeTimeEvents();

            if (registeredTimeService != null)
            {
                if (registeredTimeService.UseContinuousTime)
                {
                    EvaluateSunParameters(registeredTimeService.CurrentHour);
                }
                else
                {
                    ApplyPresetInstant(registeredTimeService.CurrentPhase);
                }
            }
            else
            {
                ApplyPresetInstant(DayPhase.Day);
            }
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            SubscribeTimeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeTimeEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnsubscribeTimeEvents();
        }

        #endregion

        #region Service Locator & Event Subscriptions

        private void ResolveComponents()
        {
            if (sunLight == null)
            {
                foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional)
                    {
                        sunLight = light;
                        break;
                    }
                }
            }

            if (sunLight != null)
            {
                var sunObserver = sunLight.GetComponent<DaySunLightingObserver>();
                if (sunObserver != null)
                {
                    sunObserver.YieldToDirector = true;
                }
            }

            if (volumeController == null)
                volumeController = FindFirstObjectByType<FeaturesTime.Atmosphere.DayNightVolumeController>();

            if (safeZoneLighting == null)
                safeZoneLighting = FindFirstObjectByType<FeaturesTime.Atmosphere.HouseSafeZoneLighting>();

            if (outdoorLighting == null)
                outdoorLighting = FindFirstObjectByType<FeaturesTime.Atmosphere.OutdoorLightingController>();

            if (shelterLantern == null)
                shelterLantern = FindFirstObjectByType<FeaturesTime.Atmosphere.ShelterLanternController>();
        }

        private void SubscribeTimeEvents()
        {
            if (isSubscribed) return;

            HeroNightHighlightReceiver.OnReceiverRegistered += HandleReceiverRegistered;

            registeredTimeService = ServiceLocator.Resolve<ITimeService>();
            if (registeredTimeService != null)
            {
                registeredTimeService.OnMinuteChanged += HandleMinuteChanged;
                registeredTimeService.OnPhaseChanged += HandlePhaseChanged;
                isSubscribed = true;
            }
        }

        private void UnsubscribeTimeEvents()
        {
            HeroNightHighlightReceiver.OnReceiverRegistered -= HandleReceiverRegistered;

            if (registeredTimeService != null && isSubscribed)
            {
                registeredTimeService.OnMinuteChanged -= HandleMinuteChanged;
                registeredTimeService.OnPhaseChanged -= HandlePhaseChanged;
                isSubscribed = false;
            }
            registeredTimeService = null;
        }

        #endregion

        #region Event Handlers

        private void HandleMinuteChanged(int minute)
        {
            if (registeredTimeService != null && registeredTimeService.UseContinuousTime)
            {
                EvaluateSunParameters(registeredTimeService.CurrentHour);
            }
        }

        private void HandlePhaseChanged(int day, DayPhase phase)
        {
            currentPhase = phase;

            if (phase == DayPhase.Night)
            {
                ApplyPresetInstant(DayPhase.Night);
                return;
            }

            if (registeredTimeService != null && registeredTimeService.UseContinuousTime)
            {
                EvaluateSunParameters(registeredTimeService.CurrentHour);
            }
            else
            {
                ApplyPresetInstant(phase);
            }
        }

        #endregion

        #region Solar Trajectory & Ambient Sync

        /// <summary>
        /// Evaluates astronomical solar trajectory, kelvin spectrum, lux, and character night highlight on discrete minute ticks.
        /// </summary>
        public void EvaluateSunParameters(float hour)
        {
            if (sunLight == null || outdoorLighting == null || shelterLantern == null)
            {
                ResolveComponents();
            }

            // Explicit check if system is in Night phase
            bool isNightPhase = (currentPhase == DayPhase.Night) ||
                               (registeredTimeService != null && registeredTimeService.IsNight);

            bool isDay = !isNightPhase && (hour >= sunriseHour && hour < sunsetHour);

            // Always evaluate and dispatch character night highlight weight (Zero-GC)
            float highlightWeight = isNightPhase ? 1.0f : EvaluateNightHighlightWeight(hour);
            DispatchNightHighlightWeight(highlightWeight);

            if (sunLight == null) return;

            if (isDay)
            {
                float dayDuration = sunsetHour - sunriseHour;
                float normalizedDay = Mathf.Clamp01((hour - sunriseHour) / dayDuration);

                // 1. Azimuth
                float azimuth = Mathf.Lerp(sunriseAzimuth, sunsetAzimuth, normalizedDay);

                // 2. Elevation
                float elevationFactor = Mathf.Sin(normalizedDay * Mathf.PI);
                float elevation = Mathf.Lerp(minElevation, maxElevation, elevationFactor);

                // 3. Kelvin Color
                float kelvin;
                if (hour < afternoonStartHour)
                {
                    kelvin = noonKelvin;
                }
                else
                {
                    float afternoonDuration = sunsetHour - afternoonStartHour;
                    float normalizedAfternoon = Mathf.Clamp01((hour - afternoonStartHour) / afternoonDuration);
                    float expFactor = Mathf.Pow(normalizedAfternoon, kelvinFalloffExponent);
                    kelvin = Mathf.Lerp(noonKelvin, duskKelvin, expFactor);
                }

                float intensity = Mathf.Lerp(minDayIntensity, maxDayIntensity, elevationFactor);
                Color lightColor = EnvironmentLightingTheme.ConvertKelvinToRGB(kelvin);

                ApplyLightParameters(elevation, azimuth, lightColor, intensity);

                // Discrete ambient sync (06:00 to 17:00 is Day)
                EnvironmentLightingTheme activeTheme = (hour < 6.0f) ? dawnTheme : ((hour > 17.0f) ? duskTheme : dayTheme);
                if (activeTheme != null)
                {
                    SyncRenderSettingsDiscrete(activeTheme);
                }

                // Subordinate controllers in Day
                if (outdoorLighting != null) outdoorLighting.ApplyInstant(TimeManager.DayPhase.Day);
                if (shelterLantern != null) shelterLantern.ApplyInstant(false);
            }
            else
            {
                // Night Phase
                ApplyLightParameters(nightEulerAngles.x, nightEulerAngles.y, nightMoonlightColor, nightIntensity);
                if (nightTheme != null)
                {
                    SyncRenderSettingsDiscrete(nightTheme);
                }

                // Subordinate controllers in Night
                if (outdoorLighting != null) outdoorLighting.ApplyInstant(TimeManager.DayPhase.Night);
                if (shelterLantern != null) shelterLantern.ApplyInstant(true);
            }
        }

        private void ApplyLightParameters(float pitch, float yaw, Color color, float intensity)
        {
            if (sunLight == null) return;

            sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            sunLight.color = color;
            sunLight.intensity = intensity;
        }

        private void SyncRenderSettingsDiscrete(EnvironmentLightingTheme theme)
        {
            if (theme == null) return;

            ApplyAmbientAndFogDiscrete(
                theme.AmbientSkyColor,
                theme.AmbientEquatorColor,
                theme.AmbientGroundColor,
                theme.FogColor,
                theme.FogStartDistance,
                theme.FogEndDistance
            );
        }

        private void ApplyAmbientAndFogDiscrete(Color sky, Color equator, Color ground, Color fog, float fogStart, float fogEnd)
        {
            if (RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Trilight)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            }

            if (cachedSkyColor != sky)
            {
                cachedSkyColor = sky;
                RenderSettings.ambientSkyColor = sky;
            }

            if (cachedEquatorColor != equator)
            {
                cachedEquatorColor = equator;
                RenderSettings.ambientEquatorColor = equator;
            }

            if (cachedGroundColor != ground)
            {
                cachedGroundColor = ground;
                RenderSettings.ambientGroundColor = ground;
            }

            if (!RenderSettings.fog)
            {
                RenderSettings.fog = true;
            }

            if (RenderSettings.fogMode != FogMode.Linear)
            {
                RenderSettings.fogMode = FogMode.Linear;
            }

            if (cachedFogColor != fog)
            {
                cachedFogColor = fog;
                RenderSettings.fogColor = fog;
            }

            if (Mathf.Abs(cachedFogStart - fogStart) > 0.01f)
            {
                cachedFogStart = fogStart;
                RenderSettings.fogStartDistance = fogStart;
            }

            if (Mathf.Abs(cachedFogEnd - fogEnd) > 0.01f)
            {
                cachedFogEnd = fogEnd;
                RenderSettings.fogEndDistance = fogEnd;
            }
        }

        #endregion

        #region Public API & Instant Preset Application

        /// <summary>
        /// Instantly applies lighting preset for the given Core DayPhase.
        /// Synchronizes sun, RenderSettings, volume, safe zone lights, and outdoor fixtures.
        /// </summary>
        public void ApplyPresetInstant(DayPhase phase)
        {
            if (sunLight == null || outdoorLighting == null || shelterLantern == null || volumeController == null || safeZoneLighting == null)
            {
                ResolveComponents();
            }

            currentPhase = phase;
            bool isNight = (phase == DayPhase.Night);

            if (isNight)
            {
                if (nightTheme != null)
                {
                    ApplyLightParameters(nightEulerAngles.x, nightEulerAngles.y, nightMoonlightColor, nightIntensity);
                    SyncRenderSettingsDiscrete(nightTheme);
                }
                else
                {
                    ApplyLightParameters(nightEulerAngles.x, nightEulerAngles.y, nightMoonlightColor, nightIntensity);
                    ApplyAmbientAndFogDiscrete(
                        new Color(0.36f, 0.40f, 0.55f),
                        new Color(0.26f, 0.30f, 0.42f),
                        new Color(0.22f, 0.24f, 0.28f),
                        new Color(0.18f, 0.24f, 0.34f),
                        52f,
                        115f
                    );
                }
            }
            else if (registeredTimeService != null && registeredTimeService.UseContinuousTime)
            {
                EvaluateSunParameters(registeredTimeService.CurrentHour);
            }
            else
            {
                EnvironmentLightingTheme activeTheme = dayTheme;
                if (phase == DayPhase.Dawn && dawnTheme != null) activeTheme = dawnTheme;
                if (phase == DayPhase.Dusk && duskTheme != null) activeTheme = duskTheme;

                if (activeTheme != null)
                {
                    ApplyLightParameters(
                        activeTheme.MainLightEulerAngles.x,
                        activeTheme.MainLightEulerAngles.y,
                        activeTheme.GetEvaluatedLightColor(),
                        activeTheme.MainLightIntensity
                    );
                    SyncRenderSettingsDiscrete(activeTheme);
                }
                else
                {
                    ApplyLightParameters(50f, -30f, new Color(1.0f, 0.95f, 0.88f), 1.0f);
                    ApplyAmbientAndFogDiscrete(
                        new Color(0.70f, 0.74f, 0.80f),
                        new Color(0.55f, 0.52f, 0.48f),
                        new Color(0.35f, 0.32f, 0.28f),
                        new Color(0.65f, 0.72f, 0.80f),
                        35f,
                        120f
                    );
                }
            }

            // Synchronize subordinate controllers safely
            TimeManager.DayPhase tmPhase = isNight ? TimeManager.DayPhase.Night : TimeManager.DayPhase.Day;
            if (safeZoneLighting != null) safeZoneLighting.ApplyInstant(tmPhase);
            if (outdoorLighting != null) outdoorLighting.ApplyInstant(tmPhase);
            if (volumeController != null) volumeController.ApplyInstant(tmPhase);
            if (shelterLantern != null) shelterLantern.ApplyInstant(isNight);

            // Synchronize character night highlight weight instantly
            float targetWeight = 0.0f;
            switch (phase)
            {
                case DayPhase.Night:
                    targetWeight = 1.0f;
                    break;
                case DayPhase.Dusk:
                    targetWeight = 0.5f;
                    break;
                case DayPhase.Dawn:
                case DayPhase.Day:
                default:
                    targetWeight = 0.0f;
                    break;
            }
            DispatchNightHighlightWeight(targetWeight);
        }

        /// <summary>
        /// Overload accepting FeaturesTime.TimeManager.DayPhase for backward compatibility.
        /// </summary>
        public void ApplyPresetInstant(TimeManager.DayPhase phase)
        {
            if (registeredTimeService == null)
            {
                registeredTimeService = ServiceLocator.Resolve<ITimeService>();
            }

            if (registeredTimeService != null && registeredTimeService.UseContinuousTime && phase != TimeManager.DayPhase.Night)
            {
                ApplyPresetInstant(registeredTimeService.CurrentPhase);
                return;
            }

            ApplyPresetInstant(phase == TimeManager.DayPhase.Night ? DayPhase.Night : DayPhase.Day);
        }

        #endregion

        #region Character Night Highlight & Zero-GC Dispatch

        private void HandleReceiverRegistered(HeroNightHighlightReceiver receiver)
        {
            if (receiver != null && currentNightHighlightWeight >= 0f)
            {
                receiver.ApplyHighlightParameters(currentNightHighlightWeight);
            }
        }

        /// <summary>
        /// Explicit registration for dynamic character receivers.
        /// </summary>
        public void RegisterReceiver(HeroNightHighlightReceiver receiver)
        {
            if (receiver != null && currentNightHighlightWeight >= 0f)
            {
                receiver.ApplyHighlightParameters(currentNightHighlightWeight);
            }
        }

        /// <summary>
        /// Explicit unregistration for dynamic character receivers.
        /// </summary>
        public void UnregisterReceiver(HeroNightHighlightReceiver receiver)
        {
            // HeroNightHighlightReceiver uses static registry, no local storage required.
        }

        /// <summary>
        /// Evaluates smooth night highlight weight [0.0..1.0] based on astronomical hour.
        /// Guaranteed: 0.0 during full daylight (baseline preservation), 1.0 during night,
        /// and smooth Hermite curves during dusk and dawn transitions.
        /// </summary>
        public float EvaluateNightHighlightWeight(float hour)
        {
            if (currentPhase == DayPhase.Night || (registeredTimeService != null && registeredTimeService.IsNight))
            {
                return 1.0f;
            }

            hour = Mathf.Repeat(hour, 24f);

            // Daytime (06:00 to 16:00): Exactly 0.0 (Daytime Baseline preserved)
            if (hour >= dawnHighlightEndHour && hour <= duskHighlightStartHour)
            {
                return 0.0f;
            }

            // Dusk transition (16:00 to 18:00): 0.0 -> 1.0
            if (hour > duskHighlightStartHour && hour < duskHighlightFullHour)
            {
                float t = (hour - duskHighlightStartHour) / (duskHighlightFullHour - duskHighlightStartHour);
                return Mathf.SmoothStep(0.0f, 1.0f, t);
            }

            // Dawn transition (05:00 to 06:00): 1.0 -> 0.0
            if (hour >= dawnHighlightStartHour && hour < dawnHighlightEndHour)
            {
                float t = (hour - dawnHighlightStartHour) / (dawnHighlightEndHour - dawnHighlightStartHour);
                return Mathf.SmoothStep(1.0f, 0.0f, t);
            }

            // Full Night (18:00 to 05:00): Exactly 1.0
            return 1.0f;
        }

        /// <summary>
        /// Dispatches night highlight weight to all registered character receivers with 0 Bytes GC.
        /// </summary>
        private void DispatchNightHighlightWeight(float weight)
        {
            if (Mathf.Approximately(currentNightHighlightWeight, weight))
            {
                return;
            }

            currentNightHighlightWeight = weight;

            var receivers = HeroNightHighlightReceiver.ActiveReceivers;
            if (receivers != null && receivers.Count > 0)
            {
                for (int i = 0; i < receivers.Count; i++)
                {
                    if (receivers[i] != null)
                    {
                        receivers[i].ApplyHighlightParameters(weight);
                    }
                }
            }
            else
            {
                var found = UnityEngine.Object.FindObjectsByType<HeroNightHighlightReceiver>(FindObjectsSortMode.None);
                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] != null)
                    {
                        found[i].ApplyHighlightParameters(weight);
                    }
                }
            }
        }

        #endregion
    }
}

namespace FeaturesTime.Atmosphere
{
    // Type forward alias to ensure existing codebase references resolve smoothly
    public class LightingAndAtmosphereDirector : Features.Time.Atmosphere.LightingAndAtmosphereDirector
    {
    }
}
