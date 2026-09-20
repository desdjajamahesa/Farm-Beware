using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI enemiesText;
        [SerializeField] private Image phaseBadgeBackground;

        [Header("Phase Colors")]
        [SerializeField] private Color dayBadgeColor = new Color(0.95f, 0.70f, 0.20f, 0.85f); // Amber / Sun
        [SerializeField] private Color nightBadgeColor = new Color(0.65f, 0.15f, 0.20f, 0.90f); // Blood / Crimson

        [Header("Wave Tracking State")]
        private int currentWave = 1;
        private int totalWaves = 1;
        private int enemiesRemaining = 0;

        private Coroutine pulseCoroutine;

        private void Awake()
        {
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
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
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
                dayText.text = $"DAY {day} / 5";
            }
        }

        private void UpdatePhaseDisplay(TimeManager.DayPhase phase)
        {
            bool isNight = (phase == TimeManager.DayPhase.Night);

            if (phaseText != null)
            {
                phaseText.text = isNight ? "🌙 NIGHT: SURVIVE!" : "☀️ DAY: PREPARE & FARM";
                phaseText.color = isNight ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.95f, 0.7f);
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
                    waveText.text = $"Wave: {currentWave} / {totalWaves}";
                }
                else
                {
                    waveText.gameObject.SetActive(true);
                    waveText.text = "Safe Zone (Daytime)";
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
            if (dayText == null || phaseText == null)
            {
                // Cari atau bangun teks di children
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmps.Length >= 2)
                {
                    dayText = tmps[0];
                    phaseText = tmps[1];
                    if (tmps.Length >= 3) waveText = tmps[2];
                    if (tmps.Length >= 4) enemiesText = tmps[3];
                }
                else
                {
                    BuildProceduralTrackerUI();
                }
            }
        }

        private void BuildProceduralTrackerUI()
        {
            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null)
                rootRt = gameObject.AddComponent<RectTransform>();

            rootRt.sizeDelta = new Vector2(230f, 75f);

            // Background Badge
            GameObject badgeObj = new GameObject("TrackerBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = Vector2.zero;
            badgeRt.anchorMax = Vector2.one;
            badgeRt.offsetMin = Vector2.zero;
            badgeRt.offsetMax = Vector2.zero;
            phaseBadgeBackground = badgeObj.GetComponent<Image>();
            phaseBadgeBackground.color = dayBadgeColor;

            // Day Text
            GameObject dayObj = new GameObject("DayText", typeof(RectTransform), typeof(TextMeshProUGUI));
            dayObj.transform.SetParent(badgeObj.transform, false);
            RectTransform dayRt = dayObj.GetComponent<RectTransform>();
            dayRt.anchorMin = new Vector2(0.05f, 0.65f);
            dayRt.anchorMax = new Vector2(0.95f, 0.95f);
            dayRt.offsetMin = Vector2.zero;
            dayRt.offsetMax = Vector2.zero;
            dayText = dayObj.GetComponent<TextMeshProUGUI>();
            dayText.fontSize = 14f;
            dayText.fontStyle = FontStyles.Bold;
            dayText.alignment = TextAlignmentOptions.Center;
            dayText.text = "DAY 1 / 5";

            // Phase Text
            GameObject phaseObj = new GameObject("PhaseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            phaseObj.transform.SetParent(badgeObj.transform, false);
            RectTransform phaseRt = phaseObj.GetComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0.05f, 0.35f);
            phaseRt.anchorMax = new Vector2(0.95f, 0.65f);
            phaseRt.offsetMin = Vector2.zero;
            phaseRt.offsetMax = Vector2.zero;
            phaseText = phaseObj.GetComponent<TextMeshProUGUI>();
            phaseText.fontSize = 11f;
            phaseText.alignment = TextAlignmentOptions.Center;
            phaseText.text = "☀️ DAY: PREPARE & FARM";

            // Wave & Enemies Row
            GameObject waveObj = new GameObject("WaveText", typeof(RectTransform), typeof(TextMeshProUGUI));
            waveObj.transform.SetParent(badgeObj.transform, false);
            RectTransform waveRt = waveObj.GetComponent<RectTransform>();
            waveRt.anchorMin = new Vector2(0.05f, 0.05f);
            waveRt.anchorMax = new Vector2(0.55f, 0.35f);
            waveRt.offsetMin = Vector2.zero;
            waveRt.offsetMax = Vector2.zero;
            waveText = waveObj.GetComponent<TextMeshProUGUI>();
            waveText.fontSize = 10f;
            waveText.alignment = TextAlignmentOptions.Left;
            waveText.text = "Safe Zone (Daytime)";

            GameObject enemyObj = new GameObject("EnemyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            enemyObj.transform.SetParent(badgeObj.transform, false);
            RectTransform enemyRt = enemyObj.GetComponent<RectTransform>();
            enemyRt.anchorMin = new Vector2(0.55f, 0.05f);
            enemyRt.anchorMax = new Vector2(0.95f, 0.35f);
            enemyRt.offsetMin = Vector2.zero;
            enemyRt.offsetMax = Vector2.zero;
            enemiesText = enemyObj.GetComponent<TextMeshProUGUI>();
            enemiesText.fontSize = 10f;
            enemiesText.alignment = TextAlignmentOptions.Right;
            enemiesText.text = "";
        }
    }
}
