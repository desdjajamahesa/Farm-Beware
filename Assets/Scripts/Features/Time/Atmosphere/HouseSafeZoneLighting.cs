using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FeaturesRendering.Lighting;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengelola sistem pencahayaan interior rumah sebagai Safe Zone (Industry-Standard Dual-Source).
    /// Mendukung hierarki Key Downlight (Spot) + Ambient Radiosity Fill (Point),
    /// kalibrasi suhu Kelvin, sinkronisasi material emissive, dan transisi halus siang/malam.
    /// </summary>
    [DisallowMultipleComponent]
    public class HouseSafeZoneLighting : MonoBehaviour
    {
        [System.Serializable]
        public class RoomLightingGroup
        {
            public string roomName = "Room";

            [Header("Light References")]
            [Tooltip("Key Downlight (Spot Light menghadap ke bawah dari plafon dengan Soft Shadows).")]
            public Light downlight;

            [Tooltip("Ambient Fill Light (Point Light lembut di tengah ruangan untuk radiosity).")]
            public Light fillLight;

            [Tooltip("Renderer kap lampu / fixture fisik untuk pendaran HDR Emissive.")]
            public Renderer fixtureRenderer;

            [Header("Color & Temperature")]
            [Tooltip("Suhu Kelvin warna lampu (misal: 2400K hangat, 4200K utility).")]
            [Range(1000f, 15000f)]
            public float colorTemperatureKelvin = 2800f;

            [Tooltip("Warna filter pengali (default: putih).")]
            public Color lightColorFilter = Color.white;

            [Header("Night Preset (Safe Zone Aktif - Kalibrasi Fisis)")]
            [Tooltip("Intensitas Key Downlight saat malam (rentang 10.0 - 15.0 lux).")]
            [Min(0f)] public float nightDownlightIntensity = 12.0f;

            [Tooltip("Intensitas Ambient Fill saat malam (rentang 2.0 - 3.5 lux).")]
            [Min(0f)] public float nightFillIntensity = 2.5f;

            [Header("Day Preset")]
            [Min(0f)] public float dayDownlightIntensity = 0.0f;
            [Min(0f)] public float dayFillIntensity = 0.0f;

            [Header("Micro-Ambiance")]
            [Tooltip("Aktifkan animasi kedip mikro organik (misal untuk lentera teras / bohlam gudang).")]
            public bool enableMicroFlicker = false;

            [Range(0.01f, 0.25f)] public float flickerAmount = 0.06f;
            [Range(0.5f, 15f)] public float flickerSpeed = 4.0f;

            [System.NonSerialized] public float currentDownlightBaseIntensity;
            [System.NonSerialized] public float currentFillBaseIntensity;

            public Color GetEvaluatedColor()
            {
                Color kelvinColor = EnvironmentLightingTheme.ConvertKelvinToRGB(colorTemperatureKelvin);
                return kelvinColor * lightColorFilter;
            }
        }

        [System.Serializable]
        public class LightEntry
        {
            public Light targetLight;
            public float nightIntensity = 4.0f;
            public float dayIntensity = 0.0f;
            public Color lightColor = Color.white;
        }

        [Header("Room Groups (Dual-Source Architecture)")]
        [SerializeField] private List<RoomLightingGroup> rooms = new List<RoomLightingGroup>();

        [Header("Legacy Managed Lights (Optional / Backward Compatible)")]
        [SerializeField] private List<LightEntry> lights = new List<LightEntry>();

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2.0f;

        private Coroutine transitionCoroutine;
        private Coroutine flickerCoroutine;
        private MaterialPropertyBlock propBlock;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        public List<RoomLightingGroup> Rooms => rooms;

        private void Awake()
        {
            propBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            TimeManager.DayPhase startingPhase = TimeManager.Instance != null 
                ? TimeManager.Instance.currentPhase 
                : TimeManager.DayPhase.Day;

            ApplyInstant(startingPhase);
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
        }

        private void HandlePhaseChanged(TimeManager.DayPhase newPhase)
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(TransitionRoutine(newPhase));
        }

        private IEnumerator TransitionRoutine(TimeManager.DayPhase targetPhase)
        {
            bool isNight = (targetPhase == TimeManager.DayPhase.Night);

            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            // Simpan start intensities untuk rooms
            float[] startDownlights = new float[rooms.Count];
            float[] startFills = new float[rooms.Count];

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                Color col = r.GetEvaluatedColor();

                if (r.downlight != null)
                {
                    startDownlights[i] = r.downlight.intensity;
                    r.downlight.color = col;
                    if (!r.downlight.gameObject.activeSelf) r.downlight.gameObject.SetActive(true);
                }

                if (r.fillLight != null)
                {
                    startFills[i] = r.fillLight.intensity;
                    r.fillLight.color = col;
                    if (!r.fillLight.gameObject.activeSelf) r.fillLight.gameObject.SetActive(true);
                }

                UpdateFixtureEmission(r, isNight ? 1.0f : 0.0f);
            }

            // Simpan start intensities untuk legacy lights
            float[] legacyStarts = new float[lights.Count];
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i].targetLight != null)
                {
                    legacyStarts[i] = lights[i].targetLight.intensity;
                    lights[i].targetLight.color = lights[i].lightColor;
                    if (!lights[i].targetLight.gameObject.activeSelf) lights[i].targetLight.gameObject.SetActive(true);
                }
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                for (int i = 0; i < rooms.Count; i++)
                {
                    var r = rooms[i];
                    float targetDown = isNight ? r.nightDownlightIntensity : r.dayDownlightIntensity;
                    float targetFill = isNight ? r.nightFillIntensity : r.dayFillIntensity;

                    if (r.downlight != null)
                    {
                        r.downlight.intensity = Mathf.Lerp(startDownlights[i], targetDown, t);
                        r.currentDownlightBaseIntensity = r.downlight.intensity;
                    }
                    if (r.fillLight != null)
                    {
                        r.fillLight.intensity = Mathf.Lerp(startFills[i], targetFill, t);
                        r.currentFillBaseIntensity = r.fillLight.intensity;
                    }

                    UpdateFixtureEmission(r, Mathf.Lerp(isNight ? 0f : 1f, isNight ? 1f : 0f, t));
                }

                for (int i = 0; i < lights.Count; i++)
                {
                    if (lights[i].targetLight != null)
                    {
                        float targetVal = isNight ? lights[i].nightIntensity : lights[i].dayIntensity;
                        lights[i].targetLight.intensity = Mathf.Lerp(legacyStarts[i], targetVal, t);
                    }
                }

                yield return null;
            }

            // Final state assignment
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                float targetDown = isNight ? r.nightDownlightIntensity : r.dayDownlightIntensity;
                float targetFill = isNight ? r.nightFillIntensity : r.dayFillIntensity;

                if (r.downlight != null)
                {
                    r.downlight.intensity = targetDown;
                    r.downlight.enabled = (targetDown > 0.001f);
                    r.currentDownlightBaseIntensity = targetDown;
                }
                if (r.fillLight != null)
                {
                    r.fillLight.intensity = targetFill;
                    r.fillLight.enabled = (targetFill > 0.001f);
                    r.currentFillBaseIntensity = targetFill;
                }

                UpdateFixtureEmission(r, isNight ? 1.0f : 0.0f);
            }

            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i].targetLight != null)
                {
                    float targetVal = isNight ? lights[i].nightIntensity : lights[i].dayIntensity;
                    lights[i].targetLight.intensity = targetVal;
                    lights[i].targetLight.enabled = (targetVal > 0.001f);
                }
            }

            transitionCoroutine = null;

            if (isNight && HasAnyFlickerRooms())
            {
                flickerCoroutine = StartCoroutine(FlickerRoutine());
            }
        }

        public void ApplyInstant(TimeManager.DayPhase phase)
        {
            bool isNight = (phase == TimeManager.DayPhase.Night);

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                Color col = r.GetEvaluatedColor();
                float targetDown = isNight ? r.nightDownlightIntensity : r.dayDownlightIntensity;
                float targetFill = isNight ? r.nightFillIntensity : r.dayFillIntensity;

                if (r.downlight != null)
                {
                    r.downlight.color = col;
                    r.downlight.intensity = targetDown;
                    r.downlight.enabled = (targetDown > 0.001f);
                    r.currentDownlightBaseIntensity = targetDown;
                }

                if (r.fillLight != null)
                {
                    r.fillLight.color = col;
                    r.fillLight.intensity = targetFill;
                    r.fillLight.enabled = (targetFill > 0.001f);
                    r.currentFillBaseIntensity = targetFill;
                }

                UpdateFixtureEmission(r, isNight ? 1.0f : 0.0f);
            }

            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i].targetLight != null)
                {
                    float targetVal = isNight ? lights[i].nightIntensity : lights[i].dayIntensity;
                    lights[i].targetLight.color = lights[i].lightColor;
                    lights[i].targetLight.intensity = targetVal;
                    lights[i].targetLight.enabled = (targetVal > 0.001f);
                }
            }

            if (isNight && HasAnyFlickerRooms())
            {
                flickerCoroutine = StartCoroutine(FlickerRoutine());
            }
        }

        private void UpdateFixtureEmission(RoomLightingGroup r, float normalizedIntensity)
        {
            if (r.fixtureRenderer == null) return;

            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            r.fixtureRenderer.GetPropertyBlock(propBlock);

            Color evaluatedColor = r.GetEvaluatedColor();
            // HDR exposure boost for URP Bloom threshold (0.9)
            Color hdrEmission = evaluatedColor * (normalizedIntensity * 2.5f);
            propBlock.SetColor(EmissionColorID, hdrEmission);
            r.fixtureRenderer.SetPropertyBlock(propBlock);
        }

        private bool HasAnyFlickerRooms()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].enableMicroFlicker) return true;
            }
            return false;
        }

        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                float time = Time.time;
                for (int i = 0; i < rooms.Count; i++)
                {
                    var r = rooms[i];
                    if (!r.enableMicroFlicker || r.downlight == null || !r.downlight.enabled) continue;

                    // Perlin noise organik untuk variasi halus
                    float noise = Mathf.PerlinNoise(time * r.flickerSpeed + (i * 17.13f), 0f);
                    float factor = 1.0f + (noise - 0.5f) * 2.0f * r.flickerAmount;
                    r.downlight.intensity = r.currentDownlightBaseIntensity * factor;
                }
                yield return null;
            }
        }

        public void RegisterLight(Light l, float nightIntensity = 4.0f, float dayIntensity = 0.0f)
        {
            if (l == null) return;
            lights.Add(new LightEntry
            {
                targetLight = l,
                nightIntensity = nightIntensity,
                dayIntensity = dayIntensity,
                lightColor = Color.white
            });
        }

        [ContextMenu("Enforce Physical Calibration (Range 18m, Spot 130, Layers)")]
        public void EnforcePhysicalCalibration()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                if (r.downlight != null)
                {
                    r.downlight.type = LightType.Spot;
                    r.downlight.range = 18.0f;
                    r.downlight.spotAngle = 130f;
                    r.downlight.innerSpotAngle = 80f;
                    r.downlight.shadows = LightShadows.Soft;
                    r.downlight.shadowStrength = 0.7f;
                    r.downlight.shadowNormalBias = 0.35f;
                    r.downlight.shadowBias = 0.05f;
                    r.downlight.shadowNearPlane = 0.1f;
                    r.downlight.renderingLayerMask = 1 | 2; // Layer 1 (Default/Floor/Furniture) | Layer 2 (Interior)
                }

                if (r.fillLight != null)
                {
                    r.fillLight.type = LightType.Point;
                    r.fillLight.range = 18.0f;
                    r.fillLight.shadows = LightShadows.None;
                    r.fillLight.renderingLayerMask = 1 | 2; // Layer 1 (Default/Floor/Furniture) | Layer 2 (Interior)
                }
            }
            Debug.Log("[HouseSafeZoneLighting] Physical calibration enforced across all rooms.");
        }
    }
}
