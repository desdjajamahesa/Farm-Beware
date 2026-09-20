using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengatur Post-Processing Volume URP secara dinamis untuk menciptakan nuansa
    /// cinematic dark-fantasy saat malam hari dan suasana hangat-damai di siang hari.
    /// Mengontrol: Vignette, ColorAdjustments (saturation, contrast, color filter), dan Bloom.
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

        [Header("Vignette")]
        [SerializeField] private float dayVignetteIntensity = 0.20f;
        [SerializeField] private float nightVignetteIntensity = 0.18f;
        [SerializeField] private float dayVignetteSmoothness = 0.30f;
        [SerializeField] private float nightVignetteSmoothness = 0.35f;

        [Header("Color Adjustments")]
        [SerializeField] private float daySaturation = 0f;
        [SerializeField] private float nightSaturation = 0f; // Jaga warna alami tanpa desaturasi berlebih
        [SerializeField] private float dayContrast = 0f;
        [SerializeField] private float nightContrast = 0f; // Nolkan contrast crush agar bayangan malam tidak hitam mati
        [SerializeField] private Color dayColorFilter = Color.white;
        [SerializeField] private Color nightColorFilter = new Color(0.92f, 0.94f, 1.0f); // Tint biru malam lembut

        [Header("Bloom")]
        [SerializeField] private float dayBloomThreshold = 1.15f;
        [SerializeField] private float nightBloomThreshold = 0.95f;
        [SerializeField] private float dayBloomIntensity = 0.25f;
        [SerializeField] private float nightBloomIntensity = 0.30f;

        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private Bloom bloom;
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
                baseVolume.profile.TryGet(out bloom);

                // Jika ColorAdjustments belum ada di profile, buat otomatis
                if (!baseVolume.profile.TryGet(out colorAdjustments))
                {
                    colorAdjustments = baseVolume.profile.Add<ColorAdjustments>(true);
                    colorAdjustments.saturation.overrideState = true;
                    colorAdjustments.contrast.overrideState = true;
                    colorAdjustments.colorFilter.overrideState = true;
                }
                else
                {
                    // Pastikan override state aktif
                    colorAdjustments.saturation.overrideState = true;
                    colorAdjustments.contrast.overrideState = true;
                    colorAdjustments.colorFilter.overrideState = true;
                }
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

            // Target values
            float targetVigIntensity = isNight ? nightVignetteIntensity : dayVignetteIntensity;
            float targetVigSmoothness = isNight ? nightVignetteSmoothness : dayVignetteSmoothness;
            float targetSat = isNight ? nightSaturation : daySaturation;
            float targetCon = isNight ? nightContrast : dayContrast;
            Color targetFilter = isNight ? nightColorFilter : dayColorFilter;
            float targetBloomThres = isNight ? nightBloomThreshold : dayBloomThreshold;
            float targetBloomInt = isNight ? nightBloomIntensity : dayBloomIntensity;

            // Start values
            float startNightWeight = nightVolume != null ? nightVolume.weight : 0f;
            float startVigIntensity = vignette != null ? vignette.intensity.value : targetVigIntensity;
            float startVigSmoothness = vignette != null ? vignette.smoothness.value : targetVigSmoothness;
            float startSat = colorAdjustments != null ? colorAdjustments.saturation.value : targetSat;
            float startCon = colorAdjustments != null ? colorAdjustments.contrast.value : targetCon;
            Color startFilter = colorAdjustments != null ? colorAdjustments.colorFilter.value : targetFilter;
            float startBloomThres = bloom != null ? bloom.threshold.value : targetBloomThres;
            float startBloomInt = bloom != null ? bloom.intensity.value : targetBloomInt;

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
                    vignette.intensity.value = Mathf.Lerp(startVigIntensity, targetVigIntensity, t);
                    vignette.smoothness.value = Mathf.Lerp(startVigSmoothness, targetVigSmoothness, t);
                }

                if (colorAdjustments != null)
                {
                    colorAdjustments.saturation.value = Mathf.Lerp(startSat, targetSat, t);
                    colorAdjustments.contrast.value = Mathf.Lerp(startCon, targetCon, t);
                    colorAdjustments.colorFilter.value = Color.Lerp(startFilter, targetFilter, t);
                }

                if (bloom != null)
                {
                    bloom.threshold.value = Mathf.Lerp(startBloomThres, targetBloomThres, t);
                    bloom.intensity.value = Mathf.Lerp(startBloomInt, targetBloomInt, t);
                }

                yield return null;
            }

            // Final snap
            if (nightVolume != null)
                nightVolume.weight = targetNightWeight;

            if (vignette != null)
            {
                vignette.intensity.value = targetVigIntensity;
                vignette.smoothness.value = targetVigSmoothness;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = targetSat;
                colorAdjustments.contrast.value = targetCon;
                colorAdjustments.colorFilter.value = targetFilter;
            }

            if (bloom != null)
            {
                bloom.threshold.value = targetBloomThres;
                bloom.intensity.value = targetBloomInt;
            }

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
                vignette.smoothness.value = isNight ? nightVignetteSmoothness : dayVignetteSmoothness;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = isNight ? nightSaturation : daySaturation;
                colorAdjustments.contrast.value = isNight ? nightContrast : dayContrast;
                colorAdjustments.colorFilter.value = isNight ? nightColorFilter : dayColorFilter;
            }

            if (bloom != null)
            {
                bloom.threshold.value = isNight ? nightBloomThreshold : dayBloomThreshold;
                bloom.intensity.value = isNight ? nightBloomIntensity : dayBloomIntensity;
            }
        }
    }
}
