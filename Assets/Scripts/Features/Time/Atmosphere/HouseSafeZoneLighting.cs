using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengelola lampu rumah (interior, teras, atau lentera luar) sebagai Safe Zone.
    /// Lampu otomatis menyala hangat di malam hari dan padam/redup di siang hari.
    /// </summary>
    public class HouseSafeZoneLighting : MonoBehaviour
    {
        [System.Serializable]
        public class LightEntry
        {
            public Light targetLight;
            [Tooltip("Intensitas saat malam hari (Safe Zone aktif).")]
            public float nightIntensity = 2.0f;
            [Tooltip("Intensitas saat siang hari.")]
            public float dayIntensity = 0.0f;
            [Tooltip("Warna cahaya saat menyala.")]
            public Color lightColor = new Color(1.0f, 0.68f, 0.32f); // Warm Amber
        }

        [Header("Managed Lights")]
        [SerializeField] private List<LightEntry> lights = new List<LightEntry>();

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 2.0f;

        private Coroutine transitionCoroutine;

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

            // Simpan intensitas awal masing-masing lampu
            float[] startIntensities = new float[lights.Count];
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i].targetLight != null)
                {
                    startIntensities[i] = lights[i].targetLight.intensity;
                    lights[i].targetLight.color = lights[i].lightColor;
                    if (!lights[i].targetLight.gameObject.activeSelf)
                        lights[i].targetLight.gameObject.SetActive(true);
                }
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                for (int i = 0; i < lights.Count; i++)
                {
                    if (lights[i].targetLight != null)
                    {
                        float targetVal = isNight ? lights[i].nightIntensity : lights[i].dayIntensity;
                        lights[i].targetLight.intensity = Mathf.Lerp(startIntensities[i], targetVal, t);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i].targetLight != null)
                {
                    float targetVal = isNight ? lights[i].nightIntensity : lights[i].dayIntensity;
                    lights[i].targetLight.intensity = targetVal;
                    if (targetVal <= 0.001f)
                    {
                        lights[i].targetLight.enabled = false;
                    }
                    else
                    {
                        lights[i].targetLight.enabled = true;
                    }
                }
            }

            transitionCoroutine = null;
        }

        public void ApplyInstant(TimeManager.DayPhase phase)
        {
            bool isNight = (phase == TimeManager.DayPhase.Night);

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
        }

        public void RegisterLight(Light l, float nightIntensity = 2.0f, float dayIntensity = 0.0f)
        {
            if (l == null) return;
            lights.Add(new LightEntry
            {
                targetLight = l,
                nightIntensity = nightIntensity,
                dayIntensity = dayIntensity,
                lightColor = new Color(1.0f, 0.68f, 0.32f)
            });
        }
    }
}
