using System.Collections;
using UnityEngine;
using FeaturesRendering.Lighting;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengontrol pencahayaan ShelterLantern di CraftingShelter agar hanya hidup di malam hari.
    /// Mendukung transisi intensitas cahaya yang halus (lerp), sinkronisasi emissive glow pada LanternGlass (Zero-GC),
    /// dan efek kedip mikro organik (flicker) saat malam hari.
    /// </summary>
    [DisallowMultipleComponent]
    [SelectionBase]
    public class ShelterLanternController : MonoBehaviour
    {
        [Header("Lamp References")]
        [Tooltip("Komponen Point Light pada lentera. Jika kosong, otomatis mencari di GameObject ini.")]
        [SerializeField] private Light lanternLight;

        [Tooltip("Renderer kaca lentera (LanternGlass) untuk efek pendaran emissive. Jika kosong, otomatis mencari anak objek bernama LanternGlass.")]
        [SerializeField] private Renderer glassRenderer;

        [Header("Lighting Settings")]
        [Tooltip("Intensitas Point Light saat malam hari (lux).")]
        [SerializeField] [Min(0f)] private float nightIntensity = 18.0f;

        [Tooltip("Intensitas Point Light saat siang hari (0 = mati).")]
        [SerializeField] [Min(0f)] private float dayIntensity = 0.0f;

        [Tooltip("Jangkauan Point Light lentera.")]
        [SerializeField] [Range(1f, 20f)] private float lightRange = 5.5f;

        [Tooltip("Warna cahaya lentera (default: putih bersih / neutral white).")]
        [SerializeField] private Color lightColor = Color.white;

        [Header("Glass Emissive Visuals")]
        [Tooltip("Warna pendaran HDR pada kaca saat malam hari (putih bersih).")]
        [SerializeField] [ColorUsage(true, true)] private Color nightEmissionColor = new Color(2.5f, 2.5f, 2.5f, 1.0f);

        [Tooltip("Warna pendaran pada kaca saat siang hari (hitam = tidak berpendar).")]
        [SerializeField] private Color dayEmissionColor = Color.black;

        [Header("Transition Settings")]
        [Tooltip("Durasi transisi perubahan cahaya dari siang ke malam atau sebaliknya (detik).")]
        [SerializeField] private float transitionDuration = 2.0f;

        [Header("Micro Ambiance / Flicker")]
        [Tooltip("Aktifkan animasi kedip halus/organik saat malam hari.")]
        [SerializeField] private bool enableFlicker = true;

        [Tooltip("Amplitudo fluktuasi kedip (misal: 0.04 = fluktuasi 4%).")]
        [SerializeField] [Range(0.01f, 0.2f)] private float flickerAmount = 0.04f;

        [Tooltip("Kecepatan kedip Perlin noise.")]
        [SerializeField] [Range(0.5f, 10f)] private float flickerSpeed = 3.5f;

        private Coroutine transitionCoroutine;
        private Coroutine flickerCoroutine;
        private MaterialPropertyBlock propBlock;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        private float seed;
        private bool isNightActive = false;

        public Light LanternLight => lanternLight;
        public Renderer GlassRenderer => glassRenderer;
        public bool IsNightActive => isNightActive;

        private void Awake()
        {
            ResolveReferences();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            seed = (transform.position.x * 12.9898f + transform.position.z * 78.233f) % 1000f;
            ApplyLightConfiguration();
        }

        private void ResolveReferences()
        {
            if (lanternLight == null)
            {
                lanternLight = GetComponent<Light>() ?? GetComponentInChildren<Light>();
            }

            if (glassRenderer == null)
            {
                Transform glassTransform = transform.Find("LanternGlass");
                if (glassTransform != null)
                {
                    glassRenderer = glassTransform.GetComponent<Renderer>();
                }
                else
                {
                    glassRenderer = GetComponentInChildren<Renderer>();
                }
            }
        }

        private void OnValidate()
        {
            ResolveReferences();
            ApplyLightConfiguration();
        }

        private void ApplyLightConfiguration()
        {
            if (lanternLight != null)
            {
                lanternLight.type = LightType.Point;
                lanternLight.range = lightRange;
                lanternLight.color = lightColor;
            }
        }

        private void Start()
        {
            TimeManager.DayPhase startingPhase = TimeManager.Instance != null
                ? TimeManager.Instance.currentPhase
                : TimeManager.DayPhase.Day;

            ApplyInstant(startingPhase == TimeManager.DayPhase.Night);
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandleTimeManagerPhaseChanged;
            }

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandleDayNightServicePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandleTimeManagerPhaseChanged;
            }

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleDayNightServicePhaseChanged;
            }

            StopAllActiveCoroutines();
        }

        private void HandleTimeManagerPhaseChanged(TimeManager.DayPhase newPhase)
        {
            SetNightState(newPhase == TimeManager.DayPhase.Night);
        }

        private void HandleDayNightServicePhaseChanged(EnvironmentPhase envPhase)
        {
            bool isNight = (envPhase == EnvironmentPhase.Night);
            SetNightState(isNight);
        }

        public void SetNightState(bool isNight)
        {
            if (isNightActive == isNight && transitionCoroutine == null)
                return;

            isNightActive = isNight;

            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            if (gameObject.activeInHierarchy)
            {
                transitionCoroutine = StartCoroutine(TransitionRoutine(isNight));
            }
            else
            {
                ApplyInstant(isNight);
            }
        }

        private IEnumerator TransitionRoutine(bool targetNight)
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            float startIntensity = lanternLight != null ? lanternLight.intensity : (targetNight ? dayIntensity : nightIntensity);
            float targetIntensity = targetNight ? nightIntensity : dayIntensity;

            Color startEmission = targetNight ? dayEmissionColor : nightEmissionColor;
            Color targetEmission = targetNight ? nightEmissionColor : dayEmissionColor;

            if (targetNight && lanternLight != null)
            {
                lanternLight.enabled = true;
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                float currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                Color currentEmission = Color.Lerp(startEmission, targetEmission, t);

                ApplyValues(currentIntensity, currentEmission);

                yield return null;
            }

            ApplyValues(targetIntensity, targetEmission);

            if (!targetNight && lanternLight != null)
            {
                lanternLight.enabled = false;
            }

            transitionCoroutine = null;

            if (targetNight && enableFlicker && gameObject.activeInHierarchy)
            {
                flickerCoroutine = StartCoroutine(FlickerRoutine());
            }
        }

        public void ApplyInstant(bool isNight)
        {
            StopAllActiveCoroutines();

            isNightActive = isNight;
            float intensity = isNight ? nightIntensity : dayIntensity;
            Color emission = isNight ? nightEmissionColor : dayEmissionColor;

            if (lanternLight != null)
            {
                lanternLight.enabled = isNight;
            }

            ApplyValues(intensity, emission);

            if (isNight && enableFlicker && Application.isPlaying && gameObject.activeInHierarchy)
            {
                flickerCoroutine = StartCoroutine(FlickerRoutine());
            }
        }

        private void ApplyValues(float intensity, Color emission)
        {
            if (lanternLight != null)
            {
                lanternLight.intensity = intensity;
            }

            if (glassRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                glassRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorID, emission);
                glassRenderer.SetPropertyBlock(propBlock);
            }
        }

        private IEnumerator FlickerRoutine()
        {
            while (isNightActive)
            {
                float time = Time.time;
                float noise = Mathf.PerlinNoise(time * flickerSpeed + seed, 0f);
                float flickerFactor = 1.0f + (noise - 0.5f) * 2.0f * flickerAmount;

                if (lanternLight != null && lanternLight.enabled)
                {
                    lanternLight.intensity = nightIntensity * flickerFactor;
                }

                if (glassRenderer != null)
                {
                    if (propBlock == null) propBlock = new MaterialPropertyBlock();
                    glassRenderer.GetPropertyBlock(propBlock);
                    propBlock.SetColor(EmissionColorID, nightEmissionColor * flickerFactor);
                    glassRenderer.SetPropertyBlock(propBlock);
                }

                yield return null;
            }
        }

        private void StopAllActiveCoroutines()
        {
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
        }

        #region Editor Testing Context Menus

        [ContextMenu("Preview Night State")]
        private void EditorPreviewNight()
        {
            ResolveReferences();
            ApplyInstant(true);
        }

        [ContextMenu("Preview Day State")]
        private void EditorPreviewDay()
        {
            ResolveReferences();
            ApplyInstant(false);
        }

        #endregion
    }
}
