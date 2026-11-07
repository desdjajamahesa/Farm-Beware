using System;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesRendering.Lighting;

namespace FeaturesTime
{
    /// <summary>
    /// Model entri aktivitas harian berdasarkan linimasa 18 menit (06:00 - 18:00).
    /// </summary>
    [System.Serializable]
    public struct DayActivityEntry
    {
        public float startHour;
        public float endHour;
        public string timeRangeText;
        public string activityDescription;

        public DayActivityEntry(float startHour, float endHour, string timeRangeText, string activityDescription)
        {
            this.startHour = startHour;
            this.endHour = endHour;
            this.timeRangeText = timeRangeText;
            this.activityDescription = activityDescription;
        }
    }

    /// <summary>
    /// Mengelola linimasa aktivitas 18 menit siang hari (Day Phase Timeline).
    /// Memancarkan notifikasi perubahan aktivitas harian dan mempertahankan
    /// status aktivitas aktif di HUD tanpa terputus selama fase siang.
    /// </summary>
    [DisallowMultipleComponent]
    public class DayActivityTimelineManager : MonoBehaviour
    {
        public static DayActivityTimelineManager Instance { get; private set; }

        public event Action<DayActivityEntry> OnActivityChanged;

        private static readonly DayActivityEntry[] Activities = new DayActivityEntry[]
        {
            new DayActivityEntry(6.0f, 6.6667f, "06:00 - 06:40", "Collect overnight harvest, sell, plant Taro/Corn + Sweet Potato"),
            new DayActivityEntry(6.6667f, 8.0f, "06:40 - 08:00", "Fetch water, start the pot (cooking runs in the background)"),
            new DayActivityEntry(8.0f, 10.3333f, "08:00 - 10:20", "Upgrade at the Workbench, trade"),
            new DayActivityEntry(10.3333f, 11.0f, "10:20 - 11:00", "Sweet Potato cycle 1 ripens: harvest, sell, replant"),
            new DayActivityEntry(11.0f, 14.3333f, "11:00 - 14:20", "Cook food, upgrade, expand"),
            new DayActivityEntry(14.3333f, 15.0f, "14:20 - 15:00", "Sweet Potato cycle 2 ripens: harvest, replant"),
            new DayActivityEntry(15.0f, 16.0f, "15:00 - 16:00", "Finalize 3 food slots, check water"),
            new DayActivityEntry(16.0f, 16.3333f, "16:00 - 16:20", "Dusk bell DING DONG (3 minutes until nightfall!)"),
            new DayActivityEntry(16.3333f, 17.0f, "16:20 - 17:00", "Taro/Corn ripen: harvest, sell, replant"),
            new DayActivityEntry(17.0f, 18.0f, "17:00 - 18:00", "Go home, eat buff food, go to bed")
        };

        private int currentActivityIndex = -1;
        public DayActivityEntry CurrentActivity => (currentActivityIndex >= 0 && currentActivityIndex < Activities.Length)
            ? Activities[currentActivityIndex]
            : Activities[0];

        private AudioSource audioSource;
        private static AudioClip cachedChimeClip;

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

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.65f;
        }

        private void Start()
        {
            EvaluateCurrentActivity(forceNotify: true);
        }

        private void OnEnable()
        {
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnMinuteChanged += HandleMinuteChanged;
                DayNightTimeManager.Instance.OnHourChanged += HandleHourChanged;
                DayNightTimeManager.Instance.OnTimePhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnMinuteChanged -= HandleMinuteChanged;
                DayNightTimeManager.Instance.OnHourChanged -= HandleHourChanged;
                DayNightTimeManager.Instance.OnTimePhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandleMinuteChanged(int minute)
        {
            EvaluateCurrentActivity(forceNotify: false);
        }

        private void HandleHourChanged(int hour)
        {
            EvaluateCurrentActivity(forceNotify: false);
        }

        private void HandlePhaseChanged(EnvironmentPhase phase)
        {
            if (phase == EnvironmentPhase.Day)
            {
                currentActivityIndex = -1;
                EvaluateCurrentActivity(forceNotify: true);
            }
        }

        public void EvaluateCurrentActivity(bool forceNotify = false)
        {
            if (DayNightTimeManager.Instance == null) return;

            // Only evaluate during daytime hours
            if (DayNightTimeManager.Instance.CurrentPhase != EnvironmentPhase.Day)
                return;

            float currentHour = DayNightTimeManager.Instance.CurrentHour;
            int matchedIndex = -1;

            for (int i = 0; i < Activities.Length; i++)
            {
                if (currentHour >= Activities[i].startHour && currentHour < Activities[i].endHour)
                {
                    matchedIndex = i;
                    break;
                }
            }

            if (matchedIndex < 0)
            {
                // Fallback boundary clamp
                matchedIndex = currentHour >= 18.0f ? Activities.Length - 1 : 0;
            }

            if (matchedIndex != currentActivityIndex || forceNotify)
            {
                currentActivityIndex = matchedIndex;
                DayActivityEntry active = Activities[currentActivityIndex];

                OnActivityChanged?.Invoke(active);

                if (!forceNotify)
                {
                    NotifyPlayerOfNewActivity(active);
                }
            }
        }

        private void NotifyPlayerOfNewActivity(DayActivityEntry entry)
        {
            // 1. Play pleasant melodic chime
            PlayActivityChime();

            // 2. Spawn alert toast notification on screen
            var floatingText = ServiceLocator.Resolve<IFloatingTextService>();
            var player = ServiceLocator.Resolve<IPlayerContext>();
            if (floatingText != null && player != null && player.Transform != null)
            {
                floatingText.SpawnText(
                    player.Transform.position + Vector3.up * 1.8f,
                    $"[{entry.timeRangeText}] {entry.activityDescription}",
                    new Color(1f, 0.85f, 0.35f));
            }

            Debug.Log($"[DayActivityTimelineManager] 📋 New Activity [{entry.timeRangeText}]: {entry.activityDescription}");
        }

        private void PlayActivityChime()
        {
            if (audioSource == null) return;

            if (cachedChimeClip == null)
            {
                cachedChimeClip = CreateProceduralChimeClip();
            }

            if (cachedChimeClip != null)
            {
                audioSource.PlayOneShot(cachedChimeClip, 0.65f);
            }
        }

        /// <summary>
        /// Generates a pleasant two-tone musical chime (G5 -> C6 ascending interval).
        /// </summary>
        private static AudioClip CreateProceduralChimeClip()
        {
            int sampleRate = 44100;
            float duration = 1.2f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[length];

            // 2 ascending tones: Tone 1 (G5: 783.99 Hz at 0.0s), Tone 2 (C6: 1046.50 Hz at 0.22s)
            float[] toneStarts = { 0.0f, 0.22f };
            float[] toneFreqs = { 783.99f, 1046.50f };

            for (int t = 0; t < toneStarts.Length; t++)
            {
                int startSample = Mathf.FloorToInt(toneStarts[t] * sampleRate);
                float f0 = toneFreqs[t];

                for (int i = startSample; i < length; i++)
                {
                    float dt = (float)(i - startSample) / sampleRate;
                    if (dt > 0.85f) break;

                    float env = Mathf.Exp(-4.0f * dt);
                    float harmonic1 = Mathf.Sin(2f * Mathf.PI * f0 * dt) * 0.50f;
                    float harmonic2 = Mathf.Sin(2f * Mathf.PI * (f0 * 2f) * dt) * 0.18f;
                    float harmonic3 = Mathf.Sin(2f * Mathf.PI * (f0 * 3f) * dt) * 0.08f;

                    data[i] += (harmonic1 + harmonic2 + harmonic3) * env * 0.40f;
                }
            }

            AudioClip clip = AudioClip.Create("ProceduralActivityChime", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
