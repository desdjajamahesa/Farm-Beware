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
        public static DayNightAudioController Instance { get; private set; }

        [Header("Audio Clips (Opsional)")]
        [SerializeField] private AudioClip dayAmbienceClip;
        [SerializeField] private AudioClip nightAmbienceClip;
        [SerializeField] private AudioClip duskBellClip;

        [Header("Procedural Fallback")]
        [Tooltip("Jika true dan audio clip kosong, akan menghasilkan audio sintetis latar. Default false agar tidak memunculkan suara berisik/desis kipas.")]
        [SerializeField] private bool enableProceduralFallback = false;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float maxVolume = 0.5f;
        [SerializeField] private float crossfadeDuration = 3.0f;
        [Range(0f, 1f)]
        [SerializeField] private float bellVolume = 0.85f;

        private AudioSource daySource;
        private AudioSource nightSource;
        private AudioSource bellSource;
        private Coroutine crossfadeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            daySource = gameObject.AddComponent<AudioSource>();
            daySource.loop = true;
            daySource.playOnAwake = false;
            daySource.spatialBlend = 0f; // 2D background ambience

            nightSource = gameObject.AddComponent<AudioSource>();
            nightSource.loop = true;
            nightSource.playOnAwake = false;
            nightSource.spatialBlend = 0f;

            bellSource = gameObject.AddComponent<AudioSource>();
            bellSource.loop = false;
            bellSource.playOnAwake = false;
            bellSource.spatialBlend = 0f;

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

            if (duskBellClip == null)
            {
                duskBellClip = CreateProceduralDuskBellClip();
            }
            bellSource.clip = duskBellClip;
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

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnDuskWarning += PlayDuskBell;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnDuskWarning -= PlayDuskBell;
            }
        }

        public void PlayDuskBell()
        {
            if (bellSource == null) return;
            if (bellSource.clip != null)
            {
                bellSource.PlayOneShot(bellSource.clip, bellVolume);
                Debug.Log("[DayNightAudioController] 🔔 Dusk Bell Chime played (DING DONG DING DONG)");
            }
        }

        private void HandlePhaseChanged(TimeManager.DayPhase newPhase)
        {
            if (crossfadeCoroutine != null)
                StopCoroutine(crossfadeCoroutine);

            crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(newPhase == TimeManager.DayPhase.Night));
        }

        public void ApplyInstant(TimeManager.DayPhase phase)
        {
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = null;
            }

            bool isNight = (phase == TimeManager.DayPhase.Night);
            if (daySource != null) daySource.volume = (isNight || daySource.clip == null) ? 0f : maxVolume;
            if (nightSource != null) nightSource.volume = (!isNight || nightSource.clip == null) ? 0f : maxVolume;
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

        private static AudioClip CreateProceduralDuskBellClip()
        {
            int sampleRate = 44100;
            // 4 strikes spaced 0.75s apart: total length ~4.2 seconds
            float duration = 4.2f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[length];

            // 4 bell strikes (Westminster quarters motif: E5, C5, D5, G4)
            float[] strikeTimes = { 0.0f, 0.75f, 1.50f, 2.25f };
            float[] baseFreqs = { 659.25f, 523.25f, 587.33f, 392.00f };

            for (int s = 0; s < strikeTimes.Length; s++)
            {
                int startSample = Mathf.FloorToInt(strikeTimes[s] * sampleRate);
                float f0 = baseFreqs[s];

                for (int i = startSample; i < length; i++)
                {
                    float dt = (float)(i - startSample) / sampleRate;
                    if (dt > 3.0f) break;

                    // Exponential decay envelope
                    float env = Mathf.Exp(-2.8f * dt);

                    // Multi-harmonic bell synthesis (fundamental + minor 3rd + 5th + octave + overtone)
                    float harmonic1 = Mathf.Sin(2f * Mathf.PI * f0 * dt) * 0.40f;
                    float harmonic2 = Mathf.Sin(2f * Mathf.PI * (f0 * 1.2f) * dt) * 0.25f;
                    float harmonic3 = Mathf.Sin(2f * Mathf.PI * (f0 * 1.5f) * dt) * 0.15f;
                    float harmonic4 = Mathf.Sin(2f * Mathf.PI * (f0 * 2.0f) * dt) * 0.12f;
                    float harmonic5 = Mathf.Sin(2f * Mathf.PI * (f0 * 2.75f) * dt) * 0.08f;

                    float strike = (harmonic1 + harmonic2 + harmonic3 + harmonic4 + harmonic5) * env;
                    data[i] += strike * 0.45f;
                }
            }

            // Normalize peak amplitude
            float maxAmp = 0f;
            for (int i = 0; i < length; i++)
            {
                float absVal = Mathf.Abs(data[i]);
                if (absVal > maxAmp) maxAmp = absVal;
            }
            if (maxAmp > 0.001f)
            {
                float norm = 0.85f / maxAmp;
                for (int i = 0; i < length; i++) data[i] *= norm;
            }

            AudioClip bellClip = AudioClip.Create("ProceduralDuskBellChime", length, 1, sampleRate, false);
            bellClip.SetData(data, 0);
            return bellClip;
        }
    }
}
