using UnityEngine;
using FeaturesTime;
using FeaturesTime.Atmosphere;
using FeaturesRendering.Lighting;

namespace FeaturesEnvironment
{
    /// <summary>
    /// Mengontrol api unggun: nyala organik di malam hari dan mati di siang hari.
    /// Mengatur kedipan Point Light (Perlin Noise), emisi bara api (EmbersCore),
    /// serta partikel lidah api (FX_Campfire_Flames) dan percikan (FX_Campfire_Sparks).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class CampfireLightFlicker : MonoBehaviour
    {
        [Header("Light Settings")]
        [SerializeField] private float baseIntensity = 6.0f;
        [SerializeField] private float flickerRange = 1.5f;
        [SerializeField] private float flickerSpeed = 6.0f;

        [Header("Particle References (Auto-resolved if null)")]
        [SerializeField] private ParticleSystem flameParticles;
        [SerializeField] private ParticleSystem sparkParticles;

        [Header("Embers Visuals")]
        [SerializeField] private Renderer embersRenderer;
        [ColorUsage(true, true)]
        [SerializeField] private Color nightEmbersColor = new Color(2.0f, 0.6f, 0.1f, 1.0f);
        [SerializeField] private Color dayEmbersColor = Color.black;

        private Light fireLight;
        private Vector3 basePosition;
        private float noiseOffset;
        private bool isNightActive = true;
        private bool isSubscribed = false;
        private MaterialPropertyBlock propBlock;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            fireLight = GetComponent<Light>();
            basePosition = transform.localPosition;
            noiseOffset = Random.Range(0f, 100f);
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                if (flameParticles == null)
                {
                    var t = parent.Find("FX_Campfire_Flames");
                    if (t != null) flameParticles = t.GetComponent<ParticleSystem>();
                }
                if (sparkParticles == null)
                {
                    var t = parent.Find("FX_Campfire_Sparks");
                    if (t != null) sparkParticles = t.GetComponent<ParticleSystem>();
                }
                if (embersRenderer == null)
                {
                    var t = parent.Find("EmbersCore");
                    if (t != null) embersRenderer = t.GetComponent<Renderer>();
                }
            }
        }

        private void Start()
        {
            EnsureSubscriptions();

            bool isNight = false;
            if (DayNightTimeManager.Instance != null)
            {
                isNight = DayNightTimeManager.Instance.CurrentPhase == EnvironmentPhase.Night;
            }
            else if (TimeManager.Instance != null)
            {
                isNight = TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night;
            }

            SetCampfireState(isNight, instant: true);
        }

        private void OnEnable()
        {
            EnsureSubscriptions();
        }

        private void EnsureSubscriptions()
        {
            bool subscribedAny = false;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandleTimeManagerPhaseChanged;
                TimeManager.Instance.OnPhaseChanged += HandleTimeManagerPhaseChanged;
                subscribedAny = true;
            }

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleDayNightPhaseChanged;
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandleDayNightPhaseChanged;
                subscribedAny = true;
            }

            if (subscribedAny)
            {
                isSubscribed = true;
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
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleDayNightPhaseChanged;
            }
            isSubscribed = false;
        }

        private void HandleTimeManagerPhaseChanged(TimeManager.DayPhase newPhase)
        {
            SetCampfireState(newPhase == TimeManager.DayPhase.Night, instant: false);
        }

        private void HandleDayNightPhaseChanged(EnvironmentPhase envPhase)
        {
            SetCampfireState(envPhase == EnvironmentPhase.Night, instant: false);
        }

        public void SetCampfireState(bool isNight, bool instant = false)
        {
            isNightActive = isNight;
            ResolveReferences();

            if (fireLight != null)
            {
                fireLight.enabled = isNight;
                if (!isNight)
                {
                    fireLight.intensity = 0f;
                    transform.localPosition = basePosition;
                }
            }

            if (flameParticles != null)
            {
                if (isNight)
                {
                    if (!flameParticles.isPlaying) flameParticles.Play();
                }
                else
                {
                    flameParticles.Stop(true, instant ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
                }
            }

            if (sparkParticles != null)
            {
                if (isNight)
                {
                    if (!sparkParticles.isPlaying) sparkParticles.Play();
                }
                else
                {
                    sparkParticles.Stop(true, instant ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
                }
            }

            if (embersRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                embersRenderer.GetPropertyBlock(propBlock);
                Color emissiveColor = isNight ? nightEmbersColor : dayEmbersColor;
                propBlock.SetColor(EmissionColorID, emissiveColor);
                embersRenderer.SetPropertyBlock(propBlock);
            }
        }

        private void Update()
        {
            if (fireLight == null || !isNightActive) return;

            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
            fireLight.intensity = baseIntensity + (noise - 0.5f) * flickerRange;

            // Pergeseran mikro posisi lidah api
            float posX = (Mathf.PerlinNoise(Time.time * 4f, noiseOffset + 10f) - 0.5f) * 0.08f;
            float posZ = (Mathf.PerlinNoise(Time.time * 4f, noiseOffset + 20f) - 0.5f) * 0.08f;
            transform.localPosition = basePosition + new Vector3(posX, 0f, posZ);
        }
    }
}
