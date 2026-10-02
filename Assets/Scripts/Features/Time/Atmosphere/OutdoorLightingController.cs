using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengelola pencahayaan seluruh lampu luar ruangan (Outdoor Garden Lamps) di pekarangan dan area kebun.
    /// Mengatur transisi halus intensitas dan pendaran emissive saat peralihan waktu Day/Night,
    /// serta menjalankan coroutine micro-flicker organik.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutdoorLightingController : MonoBehaviour
    {
        [Header("Managed Lamps")]
        [SerializeField] private List<OutdoorGardenLamp> outdoorLamps = new List<OutdoorGardenLamp>();

        [Header("Transition Settings")]
        [Tooltip("Durasi transisi perubahan cahaya dari siang ke malam (detik).")]
        [SerializeField] private float transitionDuration = 2.0f;

        private Coroutine transitionCoroutine;
        private Coroutine flickerCoroutine;
        private float currentNormalizedNight = 0f;

        public List<OutdoorGardenLamp> OutdoorLamps => outdoorLamps;

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
            float targetVal = isNight ? 1.0f : 0.0f;
            float startVal = currentNormalizedNight;

            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                currentNormalizedNight = Mathf.Lerp(startVal, targetVal, t);

                for (int i = 0; i < outdoorLamps.Count; i++)
                {
                    if (outdoorLamps[i] != null)
                    {
                        outdoorLamps[i].SetNormalizedNight(currentNormalizedNight);
                    }
                }

                yield return null;
            }

            currentNormalizedNight = targetVal;
            for (int i = 0; i < outdoorLamps.Count; i++)
            {
                if (outdoorLamps[i] != null)
                {
                    outdoorLamps[i].SetNormalizedNight(currentNormalizedNight);
                }
            }

            transitionCoroutine = null;

            if (isNight && Application.isPlaying)
            {
                flickerCoroutine = StartCoroutine(FlickerRoutine());
            }
        }

        public void ApplyInstant(TimeManager.DayPhase phase)
        {
            bool isNight = (phase == TimeManager.DayPhase.Night);
            currentNormalizedNight = isNight ? 1.0f : 0.0f;

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

            for (int i = 0; i < outdoorLamps.Count; i++)
            {
                if (outdoorLamps[i] != null)
                {
                    outdoorLamps[i].SetNormalizedNight(currentNormalizedNight);
                }
            }

            if (isNight && Application.isPlaying)
            {
                flickerCoroutine = StartCoroutine(FlickerRoutine());
            }
        }

        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                float time = Time.time;
                for (int i = 0; i < outdoorLamps.Count; i++)
                {
                    var lamp = outdoorLamps[i];
                    if (lamp != null && lamp.EnableFlicker)
                    {
                        float flicker = lamp.EvaluateFlicker(time);
                        lamp.SetNormalizedNight(currentNormalizedNight, flicker);
                    }
                }
                yield return null;
            }
        }

        [ContextMenu("Find All Scene Lamps")]
        public void FindAllLampsInScene()
        {
            outdoorLamps.Clear();
            var found = FindObjectsByType<OutdoorGardenLamp>(FindObjectsSortMode.None);
            outdoorLamps.AddRange(found);
            Debug.Log($"[OutdoorLightingController] Ditemukan {outdoorLamps.Count} lampu taman outdoor di scene.");
        }
    }
}
