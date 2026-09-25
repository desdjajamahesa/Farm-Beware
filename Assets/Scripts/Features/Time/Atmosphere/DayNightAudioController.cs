using System.Collections;
using UnityEngine;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Mengelola audio suasana lingkungan antara Siang dan Malam.
    /// Mendukung crossfade halus antar audio source.
    /// Menyediakan fallback audio prosedural (ambient breeze & horror drone) bila belum ada audio clip.
    /// </summary>
    public class DayNightAudioController : MonoBehaviour
    {
        [Header("Audio Clips (Opsional)")]
        [SerializeField] private AudioClip dayAmbienceClip;
        [SerializeField] private AudioClip nightAmbienceClip;

        [Header("Procedural Fallback")]
        [Tooltip("Jika true dan audio clip kosong, akan menghasilkan audio sintetis latar. Default false agar tidak memunculkan suara berisik/desis kipas.")]
        [SerializeField] private bool enableProceduralFallback = false;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float maxVolume = 0.5f;
        [SerializeField] private float crossfadeDuration = 3.0f;

        private AudioSource daySource;
        private AudioSource nightSource;
        private Coroutine crossfadeCoroutine;

        private void Awake()
        {
            daySource = gameObject.AddComponent<AudioSource>();
            daySource.loop = true;
            daySource.playOnAwake = false;
            daySource.spatialBlend = 0f; // 2D background ambience

            nightSource = gameObject.AddComponent<AudioSource>();
            nightSource.loop = true;
            nightSource.playOnAwake = false;
            nightSource.spatialBlend = 0f;

            // Setup clips
            if (dayAmbienceClip != null)
            {
                daySource.clip = dayAmbienceClip;
            }
            else if (enableProceduralFallback)
            {
                daySource.clip = CreateProceduralDayClip();
            }

            if (nightAmbienceClip != null)
            {
                nightSource.clip = nightAmbienceClip;
            }
            else if (enableProceduralFallback)
            {
                nightSource.clip = CreateProceduralNightClip();
            }
        }

        private void Start()
        {
            if (daySource.clip != null) daySource.Play();
            if (nightSource.clip != null) nightSource.Play();

            bool isNight = (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night);
            daySource.volume = (isNight || daySource.clip == null) ? 0f : maxVolume;
            nightSource.volume = (!isNight || nightSource.clip == null) ? 0f : maxVolume;
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
            if (crossfadeCoroutine != null)
                StopCoroutine(crossfadeCoroutine);

            crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(newPhase == TimeManager.DayPhase.Night));
        }

        private IEnumerator CrossfadeRoutine(bool toNight)
        {
            float targetDayVol = toNight ? 0f : maxVolume;
            float targetNightVol = toNight ? maxVolume : 0f;

            float startDayVol = daySource.volume;
            float startNightVol = nightSource.volume;

            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / crossfadeDuration);

                daySource.volume = Mathf.Lerp(startDayVol, targetDayVol, t);
                nightSource.volume = Mathf.Lerp(startNightVol, targetNightVol, t);

                yield return null;
            }

            daySource.volume = targetDayVol;
            nightSource.volume = targetNightVol;
            crossfadeCoroutine = null;
        }

        // --- AUDIO PROCEDURAL FALLBACKS ---
        private static AudioClip CreateProceduralDayClip()
        {
            int sampleRate = 44100;
            int length = sampleRate * 3; // 3 detik loop
            float[] data = new float[length];

            // Menghasilkan desiran angin lembut (pinkish noise)
            float b0 = 0f, b1 = 0f, b2 = 0f;
            for (int i = 0; i < length; i++)
            {
                float white = (Random.value * 2f - 1f);
                b0 = 0.99886f * b0 + white * 0.0555179f;
                b1 = 0.99332f * b1 + white * 0.0750759f;
                b2 = 0.96900f * b2 + white * 0.1538520f;
                float pink = (b0 + b1 + b2 + white * 0.5362f) * 0.04f;
                data[i] = pink;
            }

            AudioClip clip = AudioClip.Create("ProceduralDayAmbience", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralNightClip()
        {
            int sampleRate = 44100;
            int length = sampleRate * 4; // 4 detik loop
            float[] data = new float[length];

            // Menghasilkan low-frequency horror drone (55Hz sub + modulation)
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float freq1 = 55f + Mathf.Sin(t * 1.5f) * 3f; // 55Hz (A1 note)
                float freq2 = 58.5f; // Dissonant minor second beat
                float wave = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.08f + Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.05f;
                data[i] = wave;
            }

            AudioClip clip = AudioClip.Create("ProceduralNightHorrorDrone", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
