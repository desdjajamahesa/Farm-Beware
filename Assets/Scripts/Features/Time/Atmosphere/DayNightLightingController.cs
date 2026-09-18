using System.Collections;
using UnityEngine;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengontrol transisi visual pencahayaan (Directional Light, Ambient Light, dan Kabut Linear)
    /// secara dinamis antara fase Siang (Day) dan Malam (Night).
    /// Menggunakan Linear Fog agar area gameplay di sekitar kamera tetap terang dan playable,
    /// sedangkan kabut horor menyelimuti area kejauhan/perimeter.
    /// </summary>
    public class DayNightLightingController : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("Directional light utama (Matahari / Bulan). Jika kosong, otomatis mencari di scene.")]
        [SerializeField] private Light mainDirectionalLight;

        [Header("Transition Settings")]
        [Tooltip("Durasi transisi interpolasi halus (lerp) dalam detik.")]
        [SerializeField] private float transitionDuration = 2.5f;

        [Header("Day Preset (Siang Hari - Hangat & Damai)")]
        [SerializeField] private Color dayLightColor = new Color(1.0f, 0.95f, 0.88f);
        [SerializeField] private float dayLightIntensity = 1.0f;
        [SerializeField] private Vector3 dayLightRotation = new Vector3(50f, -30f, 0f);
        [SerializeField] private Color dayAmbientSky = new Color(0.70f, 0.74f, 0.80f);
        [SerializeField] private Color dayAmbientEquator = new Color(0.55f, 0.52f, 0.48f);
        [SerializeField] private Color dayAmbientGround = new Color(0.35f, 0.32f, 0.28f);
        [SerializeField] private bool dayFogEnabled = true;
        [SerializeField] private Color dayFogColor = new Color(0.65f, 0.72f, 0.80f);
        [SerializeField] private float dayFogStart = 35f;
        [SerializeField] private float dayFogEnd = 120f;

        [Header("Night Preset (Malam Hari - Terang Bulan Moody & Playable)")]
        [SerializeField] private Color nightLightColor = new Color(0.48f, 0.62f, 0.82f); // Sinar bulan perak kebiruan
        [SerializeField] private float nightLightIntensity = 0.52f; // Cukup terang untuk bertarung dan eksplorasi
        [SerializeField] private Vector3 nightLightRotation = new Vector3(55f, 130f, 0f);
        [SerializeField] private Color nightAmbientSky = new Color(0.32f, 0.38f, 0.50f);
        [SerializeField] private Color nightAmbientEquator = new Color(0.24f, 0.28f, 0.38f);
        [SerializeField] private Color nightAmbientGround = new Color(0.18f, 0.20f, 0.28f); // Tanah tetap jelas terlihat
        [SerializeField] private bool nightFogEnabled = true;
        [SerializeField] private Color nightFogColor = new Color(0.10f, 0.14f, 0.22f);
        [SerializeField] private float nightFogStart = 22f; // Area dekat karakter bebas kabut buta
        [SerializeField] private float nightFogEnd = 65f;   // Kabut merayap di kejauhan/pinggir layar

        private Coroutine transitionCoroutine;

        private void Awake()
        {
            if (mainDirectionalLight == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional)
                    {
                        mainDirectionalLight = l;
                        break;
                    }
                }
            }

            RenderSettings.fogMode = FogMode.Linear;
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                ApplyPresetInstant(TimeManager.Instance.currentPhase);
            }
            else
            {
                ApplyPresetInstant(TimeManager.DayPhase.Day);
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
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionRoutine(newPhase));
        }

        private IEnumerator TransitionRoutine(TimeManager.DayPhase targetPhase)
        {
            bool isNight = (targetPhase == TimeManager.DayPhase.Night);

            Color targetLightColor = isNight ? nightLightColor : dayLightColor;
            float targetLightIntensity = isNight ? nightLightIntensity : dayLightIntensity;
            Quaternion targetRotation = Quaternion.Euler(isNight ? nightLightRotation : dayLightRotation);

            Color targetAmbSky = isNight ? nightAmbientSky : dayAmbientSky;
            Color targetAmbEquator = isNight ? nightAmbientEquator : dayAmbientEquator;
            Color targetAmbGround = isNight ? nightAmbientGround : dayAmbientGround;

            bool targetFogEnabled = isNight ? nightFogEnabled : dayFogEnabled;
            Color targetFogColor = isNight ? nightFogColor : dayFogColor;
            float targetFogStart = isNight ? nightFogStart : dayFogStart;
            float targetFogEnd = isNight ? nightFogEnd : dayFogEnd;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;

            Color startLightColor = mainDirectionalLight != null ? mainDirectionalLight.color : targetLightColor;
            float startLightIntensity = mainDirectionalLight != null ? mainDirectionalLight.intensity : targetLightIntensity;
            Quaternion startRotation = mainDirectionalLight != null ? mainDirectionalLight.transform.rotation : targetRotation;

            Color startAmbSky = RenderSettings.ambientSkyColor;
            Color startAmbEquator = RenderSettings.ambientEquatorColor;
            Color startAmbGround = RenderSettings.ambientGroundColor;

            Color startFogColor = RenderSettings.fogColor;
            float startFogStart = RenderSettings.fogStartDistance;
            float startFogEnd = RenderSettings.fogEndDistance;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                if (mainDirectionalLight != null)
                {
                    mainDirectionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
                    mainDirectionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
                    mainDirectionalLight.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                }

                RenderSettings.ambientSkyColor = Color.Lerp(startAmbSky, targetAmbSky, t);
                RenderSettings.ambientEquatorColor = Color.Lerp(startAmbEquator, targetAmbEquator, t);
                RenderSettings.ambientGroundColor = Color.Lerp(startAmbGround, targetAmbGround, t);

                RenderSettings.fogColor = Color.Lerp(startFogColor, targetFogColor, t);
                RenderSettings.fogStartDistance = Mathf.Lerp(startFogStart, targetFogStart, t);
                RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, targetFogEnd, t);

                yield return null;
            }

            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = targetLightColor;
                mainDirectionalLight.intensity = targetLightIntensity;
                mainDirectionalLight.transform.rotation = targetRotation;
            }

            RenderSettings.ambientSkyColor = targetAmbSky;
            RenderSettings.ambientEquatorColor = targetAmbEquator;
            RenderSettings.ambientGroundColor = targetAmbGround;

            RenderSettings.fog = targetFogEnabled;
            RenderSettings.fogColor = targetFogColor;
            RenderSettings.fogStartDistance = targetFogStart;
            RenderSettings.fogEndDistance = targetFogEnd;

            transitionCoroutine = null;
        }

        public void ApplyPresetInstant(TimeManager.DayPhase phase)
        {
            bool isNight = (phase == TimeManager.DayPhase.Night);

            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = isNight ? nightLightColor : dayLightColor;
                mainDirectionalLight.intensity = isNight ? nightLightIntensity : dayLightIntensity;
                mainDirectionalLight.transform.rotation = Quaternion.Euler(isNight ? nightLightRotation : dayLightRotation);
            }

            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.ambientSkyColor = isNight ? nightAmbientSky : dayAmbientSky;
            RenderSettings.ambientEquatorColor = isNight ? nightAmbientEquator : dayAmbientEquator;
            RenderSettings.ambientGroundColor = isNight ? nightAmbientGround : dayAmbientGround;

            RenderSettings.fog = isNight ? nightFogEnabled : dayFogEnabled;
            RenderSettings.fogColor = isNight ? nightFogColor : dayFogColor;
            RenderSettings.fogStartDistance = isNight ? nightFogStart : dayFogStart;
            RenderSettings.fogEndDistance = isNight ? nightFogEnd : dayFogEnd;
        }
    }
}
