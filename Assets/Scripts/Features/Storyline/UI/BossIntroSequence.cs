using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FeaturesStoryline.UI
{
    /// <summary>
    /// Handles dramatic camera focus and title card splash sequences when major boss encounters appear.
    /// Focuses camera towards the spawn gate / boss position, displays red/gold boss title card,
    /// and smoothly restores gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossIntroSequence : MonoBehaviour
    {
        private static BossIntroSequence _instance;
        public static BossIntroSequence Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BossIntroSequence>(FindObjectsInactive.Include);
                }
                if (_instance != null && !_instance.gameObject.activeInHierarchy)
                {
                    _instance.gameObject.SetActive(true);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("UI Canvas & Elements")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject bossCardPanel;
        [SerializeField] private TextMeshProUGUI bossTitleText;
        [SerializeField] private TextMeshProUGUI bossSubtitleText;
        [SerializeField] private TextMeshProUGUI bossLoreText;
        [SerializeField] private CanvasGroup bossCardCanvasGroup;

        private Coroutine _activeIntroRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayBossIntro(string bossTitle, string subtitle, string loreText, Vector3? focusWorldPosition = null, float duration = 3.5f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (_activeIntroRoutine != null) StopCoroutine(_activeIntroRoutine);
            _activeIntroRoutine = StartCoroutine(RoutineBossIntro(bossTitle, subtitle, loreText, focusWorldPosition, duration, onComplete));
        }

        private IEnumerator RoutineBossIntro(string title, string subtitle, string lore, Vector3? focusPos, float duration, Action onComplete)
        {
            Transform prevCamTarget = null;
            GameObject tempCamTarget = null;
            var isoCam = FeaturesCamera.IsometricCameraController.Instance;
            if (focusPos.HasValue && isoCam != null)
            {
                prevCamTarget = isoCam.target;
                tempCamTarget = new GameObject("BossCamFocusTarget");
                tempCamTarget.transform.position = focusPos.Value;
                isoCam.target = tempCamTarget.transform;
            }

            if (bossTitleText != null) bossTitleText.text = title;
            if (bossSubtitleText != null) bossSubtitleText.text = subtitle;
            if (bossLoreText != null) bossLoreText.text = lore;

            if (bossCardPanel != null)
            {
                bossCardPanel.SetActive(true);
                if (bossCardCanvasGroup != null) bossCardCanvasGroup.alpha = 0f;
            }

            // Fade in card
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                if (bossCardCanvasGroup != null) bossCardCanvasGroup.alpha = Mathf.Clamp01(t / 0.4f);
                yield return null;
            }
            if (bossCardCanvasGroup != null) bossCardCanvasGroup.alpha = 1f;

            // Wait for display duration
            yield return new WaitForSecondsRealtime(duration);

            // Fade out
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                if (bossCardCanvasGroup != null) bossCardCanvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.4f);
                yield return null;
            }

            if (bossCardPanel != null) bossCardPanel.SetActive(false);

            if (tempCamTarget != null)
            {
                if (isoCam != null && prevCamTarget != null)
                {
                    isoCam.target = prevCamTarget;
                }
                Destroy(tempCamTarget);
            }

            _activeIntroRoutine = null;

            onComplete?.Invoke();
        }

        private void EnsureUIHierarchy()
        {
            if (targetCanvas == null)
            {
                var canvasObj = GameObject.Find("UI_Canvas");
                if (canvasObj != null) targetCanvas = canvasObj.GetComponent<Canvas>();
                if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (bossCardPanel == null && targetCanvas != null)
            {
                var existing = targetCanvas.transform.Find("BossIntroPanel");
                if (existing != null)
                {
                    bossCardPanel = existing.gameObject;
                    bossCardCanvasGroup = bossCardPanel.GetComponent<CanvasGroup>();
                }
                else
                {
                    bossCardPanel = new GameObject("BossIntroPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    bossCardPanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = bossCardPanel.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.7f);
                    rt.anchorMax = new Vector2(0.5f, 0.7f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(800f, 150f);

                    var img = bossCardPanel.GetComponent<Image>();
                    img.color = new Color(0.12f, 0.02f, 0.02f, 0.90f);

                    bossCardCanvasGroup = bossCardPanel.GetComponent<CanvasGroup>();
                    bossCardCanvasGroup.blocksRaycasts = false;

                    // Title
                    var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGO.transform.SetParent(bossCardPanel.transform, false);
                    var tRT = titleGO.GetComponent<RectTransform>();
                    tRT.anchorMin = new Vector2(0f, 0.6f);
                    tRT.anchorMax = new Vector2(1f, 1f);
                    tRT.offsetMin = new Vector2(20f, 0f);
                    tRT.offsetMax = new Vector2(-20f, -8f);
                    bossTitleText = titleGO.GetComponent<TextMeshProUGUI>();
                    bossTitleText.fontSize = 28;
                    bossTitleText.fontStyle = FontStyles.Bold;
                    bossTitleText.color = new Color(1f, 0.25f, 0.25f);
                    bossTitleText.alignment = TextAlignmentOptions.Center;

                    // Subtitle
                    var subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    subGO.transform.SetParent(bossCardPanel.transform, false);
                    var sRT = subGO.GetComponent<RectTransform>();
                    sRT.anchorMin = new Vector2(0f, 0.35f);
                    sRT.anchorMax = new Vector2(1f, 0.65f);
                    sRT.offsetMin = new Vector2(20f, 0f);
                    sRT.offsetMax = new Vector2(-20f, 0f);
                    bossSubtitleText = subGO.GetComponent<TextMeshProUGUI>();
                    bossSubtitleText.fontSize = 17;
                    bossSubtitleText.fontStyle = FontStyles.Italic;
                    bossSubtitleText.color = new Color(1f, 0.85f, 0.4f);
                    bossSubtitleText.alignment = TextAlignmentOptions.Center;

                    // Lore
                    var loreGO = new GameObject("Lore", typeof(RectTransform), typeof(TextMeshProUGUI));
                    loreGO.transform.SetParent(bossCardPanel.transform, false);
                    var lRT = loreGO.GetComponent<RectTransform>();
                    lRT.anchorMin = new Vector2(0f, 0f);
                    lRT.anchorMax = new Vector2(1f, 0.35f);
                    lRT.offsetMin = new Vector2(20f, 6f);
                    lRT.offsetMax = new Vector2(-20f, 0f);
                    bossLoreText = loreGO.GetComponent<TextMeshProUGUI>();
                    bossLoreText.fontSize = 14;
                    bossLoreText.color = new Color(0.85f, 0.85f, 0.85f);
                    bossLoreText.alignment = TextAlignmentOptions.Center;

                    bossCardPanel.SetActive(false);
                }
            }
        }
    }
}
