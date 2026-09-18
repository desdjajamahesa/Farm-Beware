using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengatur Post-Processing Volume URP secara dinamis untuk menciptakan nuansa horor saat malam hari.
    /// Mendukung transisi berat (weight lerp) pada Night Volume khusus atau modifikasi runtime parameter Volume.
    /// </summary>
    public class DayNightVolumeController : MonoBehaviour
    {
        [Header("Volume References")]
        [Tooltip("Volume utama yang aktif. Jika kosong, akan mencari Volume di scene.")]
        [SerializeField] private Volume baseVolume;

        [Tooltip("Volume tambahan khusus malam/horor (opsional). Jika diisi, sistem akan me-lerp weight volume ini 0 -> 1 saat malam.")]
        [SerializeField] private Volume nightVolume;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2.5f;

        [Header("Single Profile Dynamic Overrides (Jika tidak menggunakan 2 Volume terpisah)")]
        [SerializeField] private float dayVignetteIntensity = 0.20f;
        [SerializeField] private float nightVignetteIntensity = 0.26f;
        [SerializeField] private float daySaturation = 0f;
        [SerializeField] private float nightSaturation = -10f;

        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private Coroutine transitionCoroutine;

        private void Awake()
        {
            if (baseVolume == null)
            {
                baseVolume = GetComponent<Volume>();
                if (baseVolume == null)
                {
                    baseVolume = FindFirstObjectByType<Volume>();
                }
            }

            CacheOverrides();
        }

        private void CacheOverrides()
        {
            if (baseVolume != null && baseVolume.profile != null)
            {
                baseVolume.profile.TryGet(out vignette);
                baseVolume.profile.TryGet(out colorAdjustments);
            }
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                ApplyInstant(TimeManager.Instance.currentPhase);
            }
            else
            {
                ApplyInstant(TimeManager.DayPhase.Day);
            }
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
            float targetNightWeight = isNight ? 1f : 0f;

            float targetVignette = isNight ? nightVignetteIntensity : dayVignetteIntensity;
            float targetSat = isNight ? nightSaturation : daySaturation;

            float startNightWeight = nightVolume != null ? nightVolume.weight : 0f;
            float startVignette = vignette != null ? vignette.intensity.value : targetVignette;
            float startSat = colorAdjustments != null ? colorAdjustments.saturation.value : targetSat;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                if (nightVolume != null)
                {
                    nightVolume.weight = Mathf.Lerp(startNightWeight, targetNightWeight, t);
                }

                if (vignette != null)
                {
                    vignette.intensity.value = Mathf.Lerp(startVignette, targetVignette, t);
                }

                if (colorAdjustments != null)
                {
                    colorAdjustments.saturation.value = Mathf.Lerp(startSat, targetSat, t);
                }

                yield return null;
            }

            if (nightVolume != null)
                nightVolume.weight = targetNightWeight;

            if (vignette != null)
                vignette.intensity.value = targetVignette;

            if (colorAdjustments != null)
                colorAdjustments.saturation.value = targetSat;

            transitionCoroutine = null;
        }

        public void ApplyInstant(TimeManager.DayPhase phase)
        {
            bool isNight = (phase == TimeManager.DayPhase.Night);

            if (nightVolume != null)
            {
                nightVolume.weight = isNight ? 1f : 0f;
            }

            if (vignette != null)
            {
                vignette.intensity.value = isNight ? nightVignetteIntensity : dayVignetteIntensity;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = isNight ? nightSaturation : daySaturation;
            }
        }
    }
}
