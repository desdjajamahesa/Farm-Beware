using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FeaturesTime;

namespace FeaturesTime.UI
{
    /// <summary>
    /// Menampilkan tracker fase waktu (Day/Night) dan gelombang tempur (Combat Wave) di pojok kanan atas HUD.
    /// Memberi peringatan visual saat malam tiba dan siap menerima data wave dari spawner monster.
    /// </summary>
    public class CombatPhaseTrackerUI : MonoBehaviour
    {
        [Header("UI Text References (TMP)")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI enemiesText;
        [SerializeField] private Image phaseBadgeBackground;

        [Header("Day Activity Timeline UI")]
        [SerializeField] private GameObject activityCardRoot;
        [SerializeField] private TextMeshProUGUI activityHeaderBadgeText;
        [SerializeField] private TextMeshProUGUI activityDetailText;

        [Header("Phase Colors")]
        [SerializeField] private Color dayBadgeColor = new Color(0.08f, 0.11f, 0.17f, 0.92f); // Deep Slate
        [SerializeField] private Color nightBadgeColor = new Color(0.18f, 0.05f, 0.08f, 0.94f); // Deep Crimson

        [Header("Clock Display Settings")]
        [Tooltip("Interval kelipatan menit in-game untuk tampilan jam digital (default 15 menit: :00, :15, :30, :45).")]
        [Range(1, 60)]
        [SerializeField] private int clockMinuteStep = 15;

        [Header("Wave Tracking State")]
        private int currentWave = 1;
        private int totalWaves = 1;
        private int enemiesRemaining = 0;

        private Coroutine pulseCoroutine;
        private bool isActivitySubscribed = false;

        private static CombatPhaseTrackerUI _instance;
        public static CombatPhaseTrackerUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    CombatPhaseTrackerUI[] found = FindObjectsByType<CombatPhaseTrackerUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

            EnsureUIReferences();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                UpdateDayText(TimeManager.Instance.currentDay);
                UpdatePhaseDisplay(TimeManager.Instance.currentPhase);
            }
            else
            {
                UpdateDayText(1);
                UpdatePhaseDisplay(TimeManager.DayPhase.Day);
            }

            if (DayNightTimeManager.Instance != null)
            {
                UpdateClockText(DayNightTimeManager.Instance.CurrentHourInt, DayNightTimeManager.Instance.CurrentMinuteInt);
            }
            else
            {
                UpdateClockText(6, 0);
            }

            if (DayActivityTimelineManager.Instance != null)
            {
                HandleActivityChanged(DayActivityTimelineManager.Instance.CurrentActivity);
            }
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnMinuteChanged += HandleMinuteChanged;
                DayNightTimeManager.Instance.OnHourChanged += HandleHourChanged;
            }

            if (DayActivityTimelineManager.Instance != null && !isActivitySubscribed)
            {
                DayActivityTimelineManager.Instance.OnActivityChanged += HandleActivityChanged;
                isActivitySubscribed = true;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (DayNightTimeManager.Instance != null)
            {
                DayNightTimeManager.Instance.OnMinuteChanged -= HandleMinuteChanged;
                DayNightTimeManager.Instance.OnHourChanged -= HandleHourChanged;
            }

            if (DayActivityTimelineManager.Instance != null && isActivitySubscribed)
            {
                DayActivityTimelineManager.Instance.OnActivityChanged -= HandleActivityChanged;
                isActivitySubscribed = false;
            }
        }

        private int FormatSteppedMinute(int minute)
        {
            if (clockMinuteStep <= 1) return minute;
            return (minute / clockMinuteStep) * clockMinuteStep;
        }

        private void HandleMinuteChanged(int minute)
        {
            int hour = DayNightTimeManager.Instance != null ? DayNightTimeManager.Instance.CurrentHourInt : 6;
            int steppedMinute = FormatSteppedMinute(minute);
            if (steppedMinute != lastDisplayMinute)
            {
                lastDisplayMinute = steppedMinute;
                UpdateClockText(hour, steppedMinute);
            }
        }

        private void HandleHourChanged(int hour)
        {
            int minute = DayNightTimeManager.Instance != null ? DayNightTimeManager.Instance.CurrentMinuteInt : 0;
            int steppedMinute = FormatSteppedMinute(minute);
            UpdateClockText(hour, steppedMinute);
        }

        private int lastDisplayHour = -1;
        private int lastDisplayMinute = -1;
        private bool lastDisplayFastForward = false;

        private void Update()
        {
            if (!isActivitySubscribed && DayActivityTimelineManager.Instance != null)
            {
                DayActivityTimelineManager.Instance.OnActivityChanged += HandleActivityChanged;
                HandleActivityChanged(DayActivityTimelineManager.Instance.CurrentActivity);
                isActivitySubscribed = true;
            }

            if (DayNightTimeManager.Instance != null && clockText != null)
            {
                int h = DayNightTimeManager.Instance.CurrentHourInt;
                int m = FormatSteppedMinute(DayNightTimeManager.Instance.CurrentMinuteInt);
                bool ff = DayNightTimeManager.Instance.IsFastForwardActive;
                if (h != lastDisplayHour || m != lastDisplayMinute || ff != lastDisplayFastForward)
                {
                    lastDisplayHour = h;
                    lastDisplayMinute = m;
                    lastDisplayFastForward = ff;
                    UpdateClockText(h, m);
                }
            }
        }

        public void HandleActivityChanged(DayActivityEntry entry)
        {
            if (activityHeaderBadgeText != null)
            {
                activityHeaderBadgeText.text = $"📋 ACTIVITY • {entry.timeRangeText}";
            }
            if (activityDetailText != null)
            {
                activityDetailText.text = entry.activityDescription;
            }
        }

        public void UpdateClockText(int hour, int minute)
        {
            if (clockText != null)
            {
                int displayMinute = FormatSteppedMinute(minute);
                string ffTag = (DayNightTimeManager.Instance != null && DayNightTimeManager.Instance.IsFastForwardActive)
                    ? $" <size=70%><color=#38BDF8>▶▶ {DayNightTimeManager.Instance.FastForwardMultiplier:0.##}x</color></size>"
                    : "";
                clockText.text = $"{hour:D2}:{displayMinute:D2}{ffTag}";
            }
        }

        private void HandleDayChanged(int day)
        {
            UpdateDayText(day);
        }

        private void HandlePhaseChanged(TimeManager.DayPhase phase)
        {
            UpdatePhaseDisplay(phase);

            if (phase == TimeManager.DayPhase.Night)
            {
                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(NightAlertPulseRoutine());
            }
        }

        private void UpdateDayText(int day)
        {
            if (dayText != null)
            {
                dayText.gameObject.SetActive(true);
                dayText.text = $"DAY {day}";
            }
        }

        public void UpdatePhaseDisplay(TimeManager.DayPhase phase)
        {
            bool isNight = (phase == TimeManager.DayPhase.Night);

            if (activityCardRoot != null)
            {
                activityCardRoot.SetActive(!isNight);
            }

            if (dayText != null)
            {
                dayText.gameObject.SetActive(true);
                int day = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;
                dayText.text = $"DAY {day}";
                dayText.color = isNight ? new Color(1f, 0.55f, 0.55f) : new Color(1f, 0.84f, 0.25f);
            }

            if (phaseText != null)
            {
                phaseText.text = isNight ? "🌙 NIGHT" : "☀️ DAY";
                phaseText.color = isNight ? new Color(1f, 0.45f, 0.45f) : new Color(0.95f, 0.96f, 0.98f);
            }

            if (clockText != null)
            {
                clockText.color = isNight ? new Color(1f, 0.80f, 0.80f) : new Color(0.98f, 0.98f, 1f);
            }

            if (phaseBadgeBackground != null)
            {
                phaseBadgeBackground.color = isNight ? nightBadgeColor : dayBadgeColor;
            }

            if (waveText != null)
            {
                if (isNight)
                {
                    waveText.gameObject.SetActive(true);
                    waveText.text = totalWaves > 1 ? $"⚔️ Wave {currentWave} / {totalWaves}" : "⚔️ Beware of Monsters!";
                    waveText.color = new Color(1f, 0.70f, 0.70f);
                }
                else
                {
                    waveText.gameObject.SetActive(true);
                    waveText.text = "🛡️ Safe & Farming";
                    waveText.color = new Color(0.70f, 0.92f, 0.75f);
                }
            }

            if (enemiesText != null)
            {
                if (isNight)
                {
                    enemiesText.gameObject.SetActive(true);
                    enemiesText.text = $"Enemies: {enemiesRemaining}";
                }
                else
                {
                    enemiesText.gameObject.SetActive(false);
                }
            }
        }

        public void SetWaveInfo(int wave, int maxWaves)
        {
            currentWave = wave;
            totalWaves = maxWaves;

            if (waveText != null && TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
            {
                waveText.text = $"Wave: {currentWave} / {totalWaves}";
            }
        }

        public void SetEnemiesRemaining(int remaining)
        {
            enemiesRemaining = remaining;

            if (enemiesText != null && TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
            {
                enemiesText.text = $"Enemies: {enemiesRemaining}";
            }
        }

        public void SetIntermissionCountdown(int secondsRemaining, int nextWave, int maxWaves)
        {
            if (waveText != null)
            {
                waveText.text = $"Prep: {secondsRemaining}s (Next W{nextWave})";
            }
            if (enemiesText != null)
            {
                enemiesText.text = "Prep Time ([Enter] / [N] to Skip)";
            }
        }

        public void ClearIntermissionCountdown()
        {
            if (waveText != null)
            {
                waveText.text = $"Wave: {currentWave} / {totalWaves}";
            }
            if (enemiesText != null)
            {
                enemiesText.text = $"Enemies: {enemiesRemaining}";
            }
        }

        private IEnumerator NightAlertPulseRoutine()
        {
            if (phaseBadgeBackground == null) yield break;

            Vector3 baseScale = Vector3.one;
            Transform t = phaseBadgeBackground.transform;

            for (int p = 0; p < 3; p++)
            {
                float dur = 0.25f;
                for (float time = 0; time < dur; time += Time.deltaTime)
                {
                    float factor = Mathf.Sin((time / dur) * Mathf.PI) * 0.15f;
                    t.localScale = baseScale + new Vector3(factor, factor, 0f);
                    yield return null;
                }
                t.localScale = baseScale;
                yield return new WaitForSeconds(0.1f);
            }

            t.localScale = baseScale;
            pulseCoroutine = null;
        }

        private void EnsureUIReferences()
        {
            if (dayText == null || phaseText == null || clockText == null)
            {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.gameObject.name.Equals("DayText", System.StringComparison.OrdinalIgnoreCase)) dayText = t;
                    else if (t.gameObject.name.Equals("PhaseText", System.StringComparison.OrdinalIgnoreCase)) phaseText = t;
                    else if (t.gameObject.name.Equals("ClockText", System.StringComparison.OrdinalIgnoreCase)) clockText = t;
                    else if (t.gameObject.name.Equals("WaveText", System.StringComparison.OrdinalIgnoreCase)) waveText = t;
                    else if (t.gameObject.name.Equals("EnemyText", System.StringComparison.OrdinalIgnoreCase)) enemiesText = t;
                }

                if (phaseText == null)
                {
                    BuildProceduralTrackerUI();
                }
                else if (clockText == null)
                {
                    Transform parent = phaseBadgeBackground != null ? phaseBadgeBackground.transform : transform;
                    CreateProceduralClockText(parent);
                }
            }

            EnsureActivityUIReferences();
        }

        private void EnsureActivityUIReferences()
        {
            if (activityCardRoot == null || activityHeaderBadgeText == null || activityDetailText == null)
            {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.gameObject.name.Equals("ActivityHeader", System.StringComparison.OrdinalIgnoreCase)) activityHeaderBadgeText = t;
                    else if (t.gameObject.name.Equals("ActivityDetail", System.StringComparison.OrdinalIgnoreCase)) activityDetailText = t;
                }

                Transform existingCard = transform.Find("ActivityCard");
                if (existingCard != null)
                {
                    activityCardRoot = existingCard.gameObject;
                }
                else
                {
                    BuildProceduralActivityUI();
                }
            }
        }

        private void BuildProceduralActivityUI()
        {
            activityCardRoot = new GameObject("ActivityCard", typeof(RectTransform), typeof(Image));
            activityCardRoot.transform.SetParent(transform, false);
            RectTransform cardRt = activityCardRoot.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 0f);
            cardRt.anchorMax = new Vector2(1f, 0f);
            cardRt.pivot = new Vector2(0.5f, 1f);
            cardRt.anchoredPosition = new Vector2(0f, -8f);
            cardRt.sizeDelta = new Vector2(0f, 72f);

            Image bg = activityCardRoot.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.94f);

            GameObject headerObj = new GameObject("ActivityHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(activityCardRoot.transform, false);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.05f, 0.60f);
            headerRt.anchorMax = new Vector2(0.95f, 0.94f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;
            activityHeaderBadgeText = headerObj.GetComponent<TextMeshProUGUI>();
            activityHeaderBadgeText.fontSize = 12f;
            activityHeaderBadgeText.fontStyle = FontStyles.Bold;
            activityHeaderBadgeText.color = new Color(1f, 0.78f, 0.20f);
            activityHeaderBadgeText.alignment = TextAlignmentOptions.Left;
            activityHeaderBadgeText.text = "📋 ACTIVITY • 06:00 - 06:40";

            GameObject detailObj = new GameObject("ActivityDetail", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailObj.transform.SetParent(activityCardRoot.transform, false);
            RectTransform detailRt = detailObj.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.05f, 0.08f);
            detailRt.anchorMax = new Vector2(0.95f, 0.60f);
            detailRt.offsetMin = Vector2.zero;
            detailRt.offsetMax = Vector2.zero;
            activityDetailText = detailObj.GetComponent<TextMeshProUGUI>();
            activityDetailText.fontSize = 11.5f;
            activityDetailText.color = new Color(0.95f, 0.96f, 0.98f);
            activityDetailText.textWrappingMode = TextWrappingModes.Normal;
            activityDetailText.overflowMode = TextOverflowModes.Ellipsis;
            activityDetailText.alignment = TextAlignmentOptions.TopLeft;
            activityDetailText.text = "Collect overnight harvest, sell, plant Taro/Corn + Sweet Potato";
        }

        private void CreateProceduralClockText(Transform parent)
        {
            GameObject clockObj = new GameObject("ClockText", typeof(RectTransform), typeof(TextMeshProUGUI));
            clockObj.transform.SetParent(parent, false);
            RectTransform clockRt = clockObj.GetComponent<RectTransform>();
            clockRt.anchorMin = new Vector2(0.58f, 0.50f);
            clockRt.anchorMax = new Vector2(0.95f, 0.94f);
            clockRt.offsetMin = Vector2.zero;
            clockRt.offsetMax = Vector2.zero;
            clockText = clockObj.GetComponent<TextMeshProUGUI>();
            clockText.fontSize = 18f;
            clockText.fontStyle = FontStyles.Bold;
            clockText.alignment = TextAlignmentOptions.Right;
            clockText.color = new Color(0.98f, 0.98f, 1f);
            clockText.text = "06:00";
        }

        private void BuildProceduralTrackerUI()
        {
            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null)
                rootRt = gameObject.AddComponent<RectTransform>();

            rootRt.sizeDelta = new Vector2(280f, 78f);

            // Background Badge (Deep Slate translucent container)
            GameObject badgeObj = new GameObject("TrackerBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = Vector2.zero;
            badgeRt.anchorMax = Vector2.one;
            badgeRt.offsetMin = Vector2.zero;
            badgeRt.offsetMax = Vector2.zero;
            phaseBadgeBackground = badgeObj.GetComponent<Image>();
            phaseBadgeBackground.color = dayBadgeColor;

            // Day Text (Pill / Tag di kiri atas)
            GameObject dayObj = new GameObject("DayText", typeof(RectTransform), typeof(TextMeshProUGUI));
            dayObj.transform.SetParent(badgeObj.transform, false);
            RectTransform dayRt = dayObj.GetComponent<RectTransform>();
            dayRt.anchorMin = new Vector2(0.05f, 0.52f);
            dayRt.anchorMax = new Vector2(0.32f, 0.92f);
            dayRt.offsetMin = Vector2.zero;
            dayRt.offsetMax = Vector2.zero;
            dayText = dayObj.GetComponent<TextMeshProUGUI>();
            dayText.fontSize = 13.5f;
            dayText.fontStyle = FontStyles.Bold;
            dayText.alignment = TextAlignmentOptions.Left;
            dayText.color = new Color(1f, 0.84f, 0.25f);
            int initialDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;
            dayText.text = $"DAY {initialDay}";
            dayObj.SetActive(true);

            // Phase Text (Berdampingan dengan Day Text)
            GameObject phaseObj = new GameObject("PhaseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            phaseObj.transform.SetParent(badgeObj.transform, false);
            RectTransform phaseRt = phaseObj.GetComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0.32f, 0.52f);
            phaseRt.anchorMax = new Vector2(0.58f, 0.92f);
            phaseRt.offsetMin = Vector2.zero;
            phaseRt.offsetMax = Vector2.zero;
            phaseText = phaseObj.GetComponent<TextMeshProUGUI>();
            phaseText.fontSize = 13.5f;
            phaseText.fontStyle = FontStyles.Bold;
            phaseText.alignment = TextAlignmentOptions.Left;
            phaseText.color = new Color(0.95f, 0.96f, 0.98f);
            phaseText.text = "☀️ DAY";

            // Clock Text (Kanan Atas: HH:mm)
            CreateProceduralClockText(badgeObj.transform);

            // Wave & Status Row (Bawah)
            GameObject waveObj = new GameObject("WaveText", typeof(RectTransform), typeof(TextMeshProUGUI));
            waveObj.transform.SetParent(badgeObj.transform, false);
            RectTransform waveRt = waveObj.GetComponent<RectTransform>();
            waveRt.anchorMin = new Vector2(0.05f, 0.08f);
            waveRt.anchorMax = new Vector2(0.60f, 0.44f);
            waveRt.offsetMin = Vector2.zero;
            waveRt.offsetMax = Vector2.zero;
            waveText = waveObj.GetComponent<TextMeshProUGUI>();
            waveText.fontSize = 11.5f;
            waveText.color = new Color(0.70f, 0.92f, 0.75f);
            waveText.alignment = TextAlignmentOptions.Left;
            waveText.text = "🛡️ Safe & Farming";

            GameObject enemyObj = new GameObject("EnemyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            enemyObj.transform.SetParent(badgeObj.transform, false);
            RectTransform enemyRt = enemyObj.GetComponent<RectTransform>();
            enemyRt.anchorMin = new Vector2(0.60f, 0.08f);
            enemyRt.anchorMax = new Vector2(0.95f, 0.44f);
            enemyRt.offsetMin = Vector2.zero;
            enemyRt.offsetMax = Vector2.zero;
            enemiesText = enemyObj.GetComponent<TextMeshProUGUI>();
            enemiesText.fontSize = 11.5f;
            enemiesText.color = new Color(1f, 0.45f, 0.45f);
            enemiesText.alignment = TextAlignmentOptions.Right;
            enemiesText.text = "";
        }
    }
}
