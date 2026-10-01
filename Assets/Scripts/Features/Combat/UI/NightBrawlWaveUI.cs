using System.Collections;
using System.Collections.Generic;
using FeaturesCombat.Wave;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Event-driven, stateless UI Presenter for the Night Brawl Wave Indicator.
    /// Operates without per-frame Update() polling, maintaining strict Zero-GC performance during combat.
    /// </summary>
    public class NightBrawlWaveUI : MonoBehaviour
    {
        [Header("Canvas & Presentation Controls")]
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Progress Bar & Track")]
        [SerializeField] private RectTransform trackRootRect;
        [SerializeField] private Image fillProgressImage;
        [SerializeField] private RectTransform trackerIconRect;
        [SerializeField] private TextMeshProUGUI trackerIconText;

        [Header("Flags Container & Prefabs")]
        [SerializeField] private RectTransform flagsContainerRect;
        [SerializeField] private GameObject milestoneFlagPrefab;

        [Header("Typography & Labels")]
        [SerializeField] private TextMeshProUGUI waveBadgeText;
        [SerializeField] private TextMeshProUGUI progressCounterText;

        [Header("Center Screen Announcement Banner")]
        [SerializeField] private CanvasGroup announcementCanvasGroup;
        [SerializeField] private RectTransform announcementBannerRect;
        [SerializeField] private TextMeshProUGUI announcementTitleText;
        [SerializeField] private TextMeshProUGUI announcementSubtitleText;
        [SerializeField] private Image announcementBadgeImage;

        [Header("Colors & Themes")]
        [SerializeField] private Color normalAnnouncementColor = new Color(0.95f, 0.65f, 0.15f, 1.0f);
        [SerializeField] private Color bossAnnouncementColor = new Color(0.90f, 0.15f, 0.20f, 1.0f);
        [SerializeField] private Color victoryAnnouncementColor = new Color(0.35f, 0.90f, 0.45f, 1.0f);

        // Runtime pooled indicator instances
        private readonly List<WaveIndicator> activeFlags = new List<WaveIndicator>();

        // Coroutine references to prevent overlapping state conflicts
        private Coroutine trackerSlideCoroutine;
        private Coroutine trackerPunchCoroutine;
        private Coroutine announcementCoroutine;
        private Coroutine fadeCoroutine;

        private float currentTrackerProgress = 0f;
        private bool isSubscribed = false;

        private static NightBrawlWaveUI _instance;
        public static NightBrawlWaveUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    NightBrawlWaveUI[] found = FindObjectsByType<NightBrawlWaveUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureHierarchyReferences();

            if (mainCanvasGroup == null)
            {
                mainCanvasGroup = GetComponent<CanvasGroup>();
                if (mainCanvasGroup == null && trackRootRect != null)
                    mainCanvasGroup = trackRootRect.GetComponent<CanvasGroup>();
            }
            SetUIVisibleInstant(false);
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            StopAllRunningCoroutines();
            if (deferredSubscriptionCoroutine != null)
            {
                StopCoroutine(deferredSubscriptionCoroutine);
                deferredSubscriptionCoroutine = null;
            }
        }

        private Coroutine deferredSubscriptionCoroutine;

        private void Start()
        {
            // Fallback subscription if manager initialized after Awake
            if (!isSubscribed)
            {
                SubscribeToEvents();
            }

            // If still not subscribed (manager not ready yet), start deferred retry
            if (!isSubscribed)
            {
                deferredSubscriptionCoroutine = StartCoroutine(RoutineDeferredSubscription());
            }

            // Sync with current phase
            SyncWithCurrentPhase();
        }

        private IEnumerator RoutineDeferredSubscription()
        {
            float timeout = 5f;
            float elapsed = 0f;
            while (!isSubscribed && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.25f);
                elapsed += 0.25f;
                SubscribeToEvents();
            }

            if (isSubscribed)
            {
                SyncWithCurrentPhase();
                Debug.Log("[NightBrawlWaveUI] Deferred subscription successful.");
            }
            else
            {
                Debug.LogWarning("[NightBrawlWaveUI] Failed to subscribe after timeout. NightBrawlManager may not be in scene.");
            }

            deferredSubscriptionCoroutine = null;
        }

        private void SyncWithCurrentPhase()
        {
            if (TimeManager.Instance != null)
            {
                bool isNight = TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night;
                if (isNight)
                {
                    SetUIVisibleInstant(true);
                    if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.WaveEngine != null)
                    {
                        var summary = NightBrawlManager.Instance.WaveEngine.CurrentNightSummary;
                        if (summary.TotalWaves > 0)
                        {
                            HandleNightInitialized(summary);
                            HandleNightProgressChanged(
                                NightBrawlManager.Instance.WaveEngine.NightProgress,
                                NightBrawlManager.Instance.WaveEngine.TotalDefeatedEnemies,
                                NightBrawlManager.Instance.WaveEngine.TotalScheduledEnemies);
                        }
                    }
                }
                else
                {
                    SetUIVisibleInstant(false);
                }
            }
        }
        #region Event Subscriptions (Zero Polling)

        private bool isTimeManagerSubscribed = false;

        private void SubscribeToEvents()
        {
            if (isSubscribed) return;

            if (TimeManager.Instance != null && !isTimeManagerSubscribed)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged; // Defensive
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                isTimeManagerSubscribed = true;
            }

            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.WaveEngine != null)
            {
                var engine = NightBrawlManager.Instance.WaveEngine;
                engine.OnNightInitialized += HandleNightInitialized;
                engine.OnNightProgressChanged += HandleNightProgressChanged;
                engine.OnWaveMilestoneReached += HandleWaveMilestoneReached;
                engine.OnWaveStarted += HandleWaveStarted;
                engine.OnAllWavesCleared += HandleAllWavesCleared;
                isSubscribed = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (TimeManager.Instance != null && isTimeManagerSubscribed)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                isTimeManagerSubscribed = false;
            }

            if (isSubscribed && NightBrawlManager.Instance != null && NightBrawlManager.Instance.WaveEngine != null)
            {
                var engine = NightBrawlManager.Instance.WaveEngine;
                engine.OnNightInitialized -= HandleNightInitialized;
                engine.OnNightProgressChanged -= HandleNightProgressChanged;
                engine.OnWaveMilestoneReached -= HandleWaveMilestoneReached;
                engine.OnWaveStarted -= HandleWaveStarted;
                engine.OnAllWavesCleared -= HandleAllWavesCleared;
            }

            isSubscribed = false;
        }

        #endregion

        #region Event Handlers

        private void HandlePhaseChanged(TimeManager.DayPhase phase)
        {
            if (phase == TimeManager.DayPhase.Night)
            {
                FadeUI(true);
            }
            else
            {
                FadeUI(false);
            }
        }

        private void HandleNightInitialized(NightScheduleSummary summary)
        {
            SetUIVisibleInstant(true);
            BuildMilestoneFlags(summary);
            currentTrackerProgress = 0f;
            UpdateTrackerPosition(0f);

            if (fillProgressImage != null)
                fillProgressImage.fillAmount = 0f;

            if (waveBadgeText != null)
                waveBadgeText.text = $"DAY {summary.Day} • WAVE 1 / {summary.TotalWaves}";

            if (progressCounterText != null)
                progressCounterText.text = $"0 / {summary.TotalScheduledEnemies} DEFEATED";
        }

        private void HandleNightProgressChanged(float progress, int defeated, int total)
        {
            if (fillProgressImage != null)
                fillProgressImage.fillAmount = progress;

            if (progressCounterText != null)
                progressCounterText.text = $"{defeated} / {total} DEFEATED";

            // Slide tracker icon smoothly along the normalized track
            if (trackerSlideCoroutine != null)
                StopCoroutine(trackerSlideCoroutine);

            trackerSlideCoroutine = StartCoroutine(RoutineSlideTracker(progress));

            // Tactile bump impulse on kill
            if (defeated > 0)
            {
                if (trackerPunchCoroutine != null)
                    StopCoroutine(trackerPunchCoroutine);

                trackerPunchCoroutine = StartCoroutine(RoutinePunchTracker());
            }
        }

        private void HandleWaveMilestoneReached(WaveMilestoneData milestone)
        {
            for (int i = 0; i < activeFlags.Count; i++)
            {
                if (activeFlags[i].MilestoneData.WaveIndex == milestone.WaveIndex)
                {
                    activeFlags[i].TriggerMilestonePassed();
                    break;
                }
            }
        }

        private void HandleWaveStarted(int day, int wave)
        {
            int totalWaves = NightBrawlManager.Instance != null && NightBrawlManager.Instance.WaveEngine != null
                ? NightBrawlManager.Instance.WaveEngine.TotalWavesForDay
                : 1;

            if (waveBadgeText != null)
                waveBadgeText.text = $"DAY {day} • WAVE {wave} / {totalWaves}";

            // Determine if current wave is a boss wave
            bool isBoss = false;
            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.WaveEngine != null)
            {
                var summary = NightBrawlManager.Instance.WaveEngine.CurrentNightSummary;
                if (summary.Milestones != null && wave - 1 < summary.Milestones.Length)
                {
                    isBoss = summary.Milestones[wave - 1].IsBossWave;
                }
            }

            if (isBoss)
            {
                string bossTitle = wave == totalWaves && day == 5
                    ? "FINAL CLIMAX: DUAL BOSSES!"
                    : "BOSS ENCOUNTER INCOMING!";
                string subtitle = "Steel your resolve. A monstrous threat approaches!";
                ShowAnnouncement(bossTitle, subtitle, bossAnnouncementColor);
            }
            else if (wave > 1 && wave == totalWaves)
            {
                ShowAnnouncement("FINAL WAVE APPROACHING!", "Defeat the remaining monsters to survive!", normalAnnouncementColor);
            }
            else if (wave > 1)
            {
                ShowAnnouncement($"WAVE {wave} APPROACHING!", "Prepare your defenses!", normalAnnouncementColor);
            }
        }

        private void HandleAllWavesCleared(int day)
        {
            ShowAnnouncement("NIGHT SURVIVED!", "Dawn arrives. The monsters retreat into the shadows.", victoryAnnouncementColor);
            StartCoroutine(RoutineDelayHideAfterVictory());
        }

        private IEnumerator RoutineDelayHideAfterVictory()
        {
            yield return new WaitForSeconds(3.5f);
            FadeUI(false);
        }

        #endregion

        #region Flag Building & Anchor Calculations

        private void BuildMilestoneFlags(NightScheduleSummary summary)
        {
            if (flagsContainerRect == null) return;

            // Clear or recycle existing flags
            for (int i = 0; i < activeFlags.Count; i++)
            {
                if (activeFlags[i] != null)
                    activeFlags[i].gameObject.SetActive(false);
            }

            if (summary.Milestones == null) return;

            for (int i = 0; i < summary.Milestones.Length; i++)
            {
                var data = summary.Milestones[i];
                WaveIndicator flagView = null;

                // Reuse from list if available
                if (i < activeFlags.Count && activeFlags[i] != null)
                {
                    flagView = activeFlags[i];
                    flagView.gameObject.SetActive(true);
                }
                else
                {
                    GameObject flagObj = milestoneFlagPrefab != null
                        ? Instantiate(milestoneFlagPrefab, flagsContainerRect)
                        : CreateFallbackFlagObject(flagsContainerRect);

                    flagView = flagObj.GetComponent<WaveIndicator>() ?? flagObj.AddComponent<WaveIndicator>();
                    activeFlags.Add(flagView);
                }

                flagView.Initialize(data);
            }
        }

        private GameObject CreateFallbackFlagObject(Transform parent)
        {
            GameObject flag = new GameObject("WaveIndicator_Generated", typeof(RectTransform));
            flag.transform.SetParent(parent, false);

            var rt = flag.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(28f, 36f);

            // Pole
            GameObject pole = new GameObject("Pole", typeof(RectTransform), typeof(Image));
            pole.transform.SetParent(flag.transform, false);
            var poleRt = pole.GetComponent<RectTransform>();
            poleRt.anchorMin = new Vector2(0.5f, 0f);
            poleRt.anchorMax = new Vector2(0.5f, 1f);
            poleRt.sizeDelta = new Vector2(3f, 0f);
            poleRt.anchoredPosition = Vector2.zero;
            pole.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.45f, 0.9f);

            // Banner Image
            GameObject banner = new GameObject("Banner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(flag.transform, false);
            var bannerRt = banner.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 0.6f);
            bannerRt.anchorMax = new Vector2(1f, 1f);
            bannerRt.sizeDelta = new Vector2(18f, 16f);
            bannerRt.anchoredPosition = new Vector2(9f, 0f);
            var img = banner.GetComponent<Image>();
            img.color = new Color(0.95f, 0.75f, 0.2f, 1f);

            // Icon / Text
            GameObject txtObj = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(flag.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.5f, 0.6f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.sizeDelta = new Vector2(18f, 16f);
            txtRt.anchoredPosition = new Vector2(9f, 0f);
            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "W";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;

            return flag;
        }

        #endregion

        #region Tactile Animations (Zero-GC Juice)

        private IEnumerator RoutineSlideTracker(float targetProgress)
        {
            float startProgress = currentTrackerProgress;
            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out quad
                float smoothedT = 1f - (1f - t) * (1f - t);

                currentTrackerProgress = Mathf.Lerp(startProgress, targetProgress, smoothedT);
                UpdateTrackerPosition(currentTrackerProgress);
                yield return null;
            }

            currentTrackerProgress = targetProgress;
            UpdateTrackerPosition(currentTrackerProgress);
            trackerSlideCoroutine = null;
        }

        private void UpdateTrackerPosition(float progress)
        {
            if (trackerIconRect == null) return;

            float clamped = Mathf.Clamp01(progress);
            // Normalized anchor positioning ensures 100% responsiveness on any aspect ratio
            trackerIconRect.anchorMin = new Vector2(clamped, 0.5f);
            trackerIconRect.anchorMax = new Vector2(clamped, 0.5f);
            trackerIconRect.pivot = new Vector2(0.5f, 0.5f);
            trackerIconRect.anchoredPosition = Vector2.zero;
        }

        private IEnumerator RoutinePunchTracker()
        {
            if (trackerIconRect == null) yield break;

            Vector3 baseScale = Vector3.one;
            Vector3 punchScale = Vector3.one * 1.35f;
            float duration = 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curve = Mathf.Sin(t * Mathf.PI);

                trackerIconRect.localScale = Vector3.Lerp(baseScale, punchScale, curve);
                yield return null;
            }

            trackerIconRect.localScale = baseScale;
            trackerPunchCoroutine = null;
        }

        public void ShowAnnouncement(string title, string subtitle, Color themeColor)
        {
            if (announcementCoroutine != null)
                StopCoroutine(announcementCoroutine);

            announcementCoroutine = StartCoroutine(RoutineShowAnnouncement(title, subtitle, themeColor));
        }

        private IEnumerator RoutineShowAnnouncement(string title, string subtitle, Color themeColor)
        {
            if (announcementCanvasGroup == null || announcementBannerRect == null)
                yield break;

            if (announcementTitleText != null)
            {
                announcementTitleText.text = title;
                announcementTitleText.color = themeColor;
            }

            if (announcementSubtitleText != null)
            {
                announcementSubtitleText.text = subtitle;
            }

            if (announcementBadgeImage != null)
            {
                announcementBadgeImage.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.25f);
            }

            // Swoop In Animation
            float swoopInDuration = 0.35f;
            float elapsed = 0f;
            Vector3 startScale = new Vector3(0.5f, 0.5f, 1f);
            Vector3 endScale = Vector3.one;

            announcementCanvasGroup.alpha = 0f;
            announcementCanvasGroup.blocksRaycasts = false;

            while (elapsed < swoopInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / swoopInDuration);
                // Overshoot elastic curve
                float overshoot = Mathf.Sin(t * Mathf.PI * 0.5f);

                announcementCanvasGroup.alpha = t;
                announcementBannerRect.localScale = Vector3.LerpUnclamped(startScale, endScale, 1f + 0.15f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            announcementCanvasGroup.alpha = 1f;
            announcementBannerRect.localScale = endScale;

            // Hold display
            yield return new WaitForSeconds(1.8f);

            // Fade Out
            float fadeOutDuration = 0.4f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);

                announcementCanvasGroup.alpha = 1f - t;
                announcementBannerRect.localScale = Vector3.Lerp(endScale, new Vector3(1.1f, 1.1f, 1f), t);
                yield return null;
            }

            announcementCanvasGroup.alpha = 0f;
            announcementCoroutine = null;
        }

        private void FadeUI(bool show)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(RoutineFadeCanvasGroup(show ? 1f : 0f, fadeDuration));
        }

        private IEnumerator RoutineFadeCanvasGroup(float targetAlpha, float duration)
        {
            if (mainCanvasGroup == null) yield break;

            float startAlpha = mainCanvasGroup.alpha;
            float elapsed = 0f;

            mainCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            mainCanvasGroup.alpha = targetAlpha;
            fadeCoroutine = null;
        }

        private void SetUIVisibleInstant(bool visible)
        {
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = visible ? 1f : 0f;
                mainCanvasGroup.blocksRaycasts = visible;
            }

            if (announcementCanvasGroup != null)
            {
                announcementCanvasGroup.alpha = 0f;
                announcementCanvasGroup.blocksRaycasts = false;
            }
        }

        private void StopAllRunningCoroutines()
        {
            if (trackerSlideCoroutine != null) { StopCoroutine(trackerSlideCoroutine); trackerSlideCoroutine = null; }
            if (trackerPunchCoroutine != null) { StopCoroutine(trackerPunchCoroutine); trackerPunchCoroutine = null; }
            if (announcementCoroutine != null) { StopCoroutine(announcementCoroutine); announcementCoroutine = null; }
            if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
        }

        private void EnsureHierarchyReferences()
        {
            if (trackRootRect == null)
            {
                var track = transform.Find("WaveTracker_Panel");
                if (track != null) trackRootRect = track.GetComponent<RectTransform>();
                if (trackRootRect == null) trackRootRect = GetComponent<RectTransform>();
            }

            if (flagsContainerRect == null && trackRootRect != null)
            {
                var flags = trackRootRect.Find("Track_Background/Flags_Container") ?? trackRootRect.Find("Flags_Container");
                if (flags != null) flagsContainerRect = flags.GetComponent<RectTransform>();
            }

            if (trackerIconRect == null && trackRootRect != null)
            {
                var icon = trackRootRect.Find("Track_Background/Tracker_Icon") ?? trackRootRect.Find("Tracker_Icon");
                if (icon != null) trackerIconRect = icon.GetComponent<RectTransform>();
            }
        }

        #endregion
    }
}
