using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FeaturesStoryline.Cutscenes
{
    /// <summary>
    /// Presentation layer for cinematic cutscenes.
    /// Manages 2.39:1 anamorphic letterbox bars, typography-styled subtitles,
    /// and a circular hold-to-skip visual progress gauge.
    /// </summary>
    [DisallowMultipleComponent]
    public class CutsceneLetterboxUI : MonoBehaviour
    {
        private static CutsceneLetterboxUI _instance;
        public static bool HasInstance => _instance != null;
        private static bool _isQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isQuitting = false;
        }

        public static CutsceneLetterboxUI Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CutsceneLetterboxUI>(FindObjectsInactive.Include);
                    if (_instance == null && Application.isPlaying)
                    {
                        var canvasObj = GameObject.Find("UI_Canvas");
                        var go = new GameObject("CutsceneLetterboxUI");
                        if (canvasObj != null)
                        {
                            go.transform.SetParent(canvasObj.transform, false);
                        }
                        _instance = go.AddComponent<CutsceneLetterboxUI>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("UI Canvas & Root")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject letterboxRoot;
        [SerializeField] private CanvasGroup letterboxCanvasGroup;

        [Header("Letterbox Bars")]
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private float barHeight = 125f;

        [Header("Subtitles")]
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup subtitleCanvasGroup;

        [Header("Skip Gauge")]
        [SerializeField] private GameObject skipGaugeRoot;
        [SerializeField] private Image skipFillImage;
        [SerializeField] private TextMeshProUGUI skipPromptText;

        [Header("Intro Story Overlay")]
        [SerializeField] private GameObject introStoryRoot;
        [SerializeField] private CanvasGroup introStoryCanvasGroup;
        [SerializeField] private TextMeshProUGUI introStoryText;
        [SerializeField] private CanvasGroup introStoryTextCanvasGroup;
        [SerializeField] private GameObject[] storyCardObjects = new GameObject[4];
        public GameObject[] StoryCardObjects => storyCardObjects;
        public GameObject IntroStoryRoot => introStoryRoot;
        public GameObject LetterboxRoot => letterboxRoot;
        public TextMeshProUGUI SubtitleText => subtitleText;
        public TextMeshProUGUI SkipPromptText => skipPromptText;
        public Canvas TargetCanvas { get => targetCanvas; set => targetCanvas = value; }

        private Coroutine _fadeRoutine;
        private Coroutine _subtitleFadeRoutine;
        private Coroutine _introTextFadeRoutine;

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

        private void OnEnable()
        {
            EnsureUIHierarchy();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Smoothly fades the cinematic letterbox in or out.
        /// </summary>
        public void SetLetterboxVisible(bool visible, float duration = 0.5f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (letterboxRoot == null || letterboxCanvasGroup == null) return;

            letterboxRoot.SetActive(true);
            if (skipGaugeRoot != null)
            {
                skipGaugeRoot.SetActive(visible);
            }

            if (duration <= 0.05f)
            {
                if (_fadeRoutine != null)
                {
                    StopCoroutine(_fadeRoutine);
                    _fadeRoutine = null;
                }
                letterboxCanvasGroup.alpha = visible ? 1f : 0f;
                if (!visible && letterboxRoot != null)
                {
                    letterboxRoot.SetActive(false);
                }
                onComplete?.Invoke();
                return;
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(RoutineFadeCanvasGroup(letterboxCanvasGroup, visible ? 1f : 0f, duration, () =>
            {
                if (!visible && letterboxRoot != null)
                {
                    letterboxRoot.SetActive(false);
                }
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Updates the subtitle text with a smooth crossfade.
        /// </summary>
        public void SetSubtitle(string text, float fadeDuration = 0.25f)
        {
            EnsureUIHierarchy();
            if (subtitleText == null || subtitleCanvasGroup == null) return;

            if (_subtitleFadeRoutine != null) StopCoroutine(_subtitleFadeRoutine);

            if (string.IsNullOrEmpty(text))
            {
                _subtitleFadeRoutine = StartCoroutine(RoutineFadeCanvasGroup(subtitleCanvasGroup, 0f, fadeDuration, () =>
                {
                    if (subtitleText != null) subtitleText.text = string.Empty;
                }));
                return;
            }

            _subtitleFadeRoutine = StartCoroutine(RoutineCrossfadeSubtitle(text, fadeDuration));
        }

        /// <summary>
        /// Clears active subtitle text.
        /// </summary>
        public void ClearSubtitle(float fadeDuration = 0.25f)
        {
            SetSubtitle(string.Empty, fadeDuration);
        }

        /// <summary>
        /// Updates the visual fill of the hold-to-skip circular gauge.
        /// </summary>
        /// <param name="progress">Normalized value between 0.0 and 1.0</param>
        public void UpdateSkipProgress(float progress)
        {
            EnsureUIHierarchy();
            if (skipFillImage != null)
            {
                skipFillImage.fillAmount = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Controls whether the skip prompt text and gauge are displayed.
        /// </summary>
        public void SetSkipVisible(bool visible)
        {
            EnsureUIHierarchy();
            if (skipGaugeRoot != null)
            {
                skipGaugeRoot.SetActive(visible);
            }
        }

        /// <summary>
        /// Instantly hides letterbox bars, active subtitles, and skip gauge with zero fade latency.
        /// </summary>
        public void SnapLetterboxHidden()
        {
            EnsureUIHierarchy();
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
            if (_subtitleFadeRoutine != null)
            {
                StopCoroutine(_subtitleFadeRoutine);
                _subtitleFadeRoutine = null;
            }

            if (letterboxCanvasGroup != null)
            {
                letterboxCanvasGroup.alpha = 0f;
            }
            if (letterboxRoot != null)
            {
                letterboxRoot.SetActive(false);
            }
            if (skipGaugeRoot != null)
            {
                skipGaugeRoot.SetActive(false);
            }
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
            }
            if (introStoryCanvasGroup != null)
            {
                introStoryCanvasGroup.alpha = 0f;
            }
            if (introStoryRoot != null)
            {
                introStoryRoot.SetActive(false);
            }
            if (introStoryText != null)
            {
                introStoryText.text = string.Empty;
            }
            if (introStoryTextCanvasGroup != null)
            {
                introStoryTextCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Smoothly fades the fullscreen intro story black backdrop in or out.
        /// </summary>
        public void SetIntroBackdropVisible(bool visible, float duration = 0.5f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (introStoryRoot == null || introStoryCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            introStoryRoot.SetActive(true);

            if (duration <= 0.05f)
            {
                introStoryCanvasGroup.alpha = visible ? 1f : 0f;
                if (!visible) introStoryRoot.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(RoutineFadeCanvasGroup(introStoryCanvasGroup, visible ? 1f : 0f, duration, () =>
            {
                if (!visible && introStoryRoot != null) introStoryRoot.SetActive(false);
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Displays an intro story sentence card with smooth alpha fade-in.
        /// </summary>
        public void SetIntroStoryCard(string text, float fadeIn = 0.5f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (introStoryText == null || introStoryTextCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_introTextFadeRoutine != null) StopCoroutine(_introTextFadeRoutine);

            if (!string.IsNullOrEmpty(text) && !text.Contains("PROLOGUE") && !text.StartsWith("<size="))
            {
                introStoryText.text = $"<size=65%><color=#E5B044>—   P R O L O G U E   —</color></size>\n\n“{text}”";
            }
            else
            {
                introStoryText.text = text;
            }

            _introTextFadeRoutine = StartCoroutine(RoutineFadeCanvasGroup(introStoryTextCanvasGroup, 1f, fadeIn, onComplete));
        }

        /// <summary>
        /// Clears the active intro story sentence card with smooth alpha fade-out.
        /// </summary>
        public void ClearIntroStoryCard(float fadeOut = 0.5f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (introStoryTextCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_introTextFadeRoutine != null) StopCoroutine(_introTextFadeRoutine);

            _introTextFadeRoutine = StartCoroutine(RoutineFadeCanvasGroup(introStoryTextCanvasGroup, 0f, fadeOut, () =>
            {
                if (introStoryText != null) introStoryText.text = string.Empty;
                onComplete?.Invoke();
            }));
        }

        private IEnumerator RoutineFadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration, Action onComplete)
        {
            if (cg == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = cg.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / Mathf.Max(0.01f, duration));
                yield return null;
            }

            cg.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private IEnumerator RoutineCrossfadeSubtitle(string newText, float duration)
        {
            // Fade out current text if visible
            if (subtitleCanvasGroup.alpha > 0.05f)
            {
                float halfDuration = duration * 0.5f;
                float elapsed = 0f;
                float startAlpha = subtitleCanvasGroup.alpha;

                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    subtitleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / halfDuration);
                    yield return null;
                }
            }

            string formattedText = newText;
            if (!string.IsNullOrEmpty(formattedText) && !formattedText.StartsWith("“") && !formattedText.StartsWith("\"") && !formattedText.StartsWith("<"))
            {
                formattedText = $"“{formattedText}”";
            }
            subtitleText.text = formattedText;

            // Fade in new text
            float inDuration = duration * 0.5f;
            float inElapsed = 0f;
            while (inElapsed < inDuration)
            {
                inElapsed += Time.unscaledDeltaTime;
                subtitleCanvasGroup.alpha = Mathf.Lerp(0f, 1f, inElapsed / inDuration);
                yield return null;
            }

            subtitleCanvasGroup.alpha = 1f;
        }

        public void EnsureUIHierarchy()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
                if (targetCanvas == null)
                {
                    var canvasObj = GameObject.Find("UI_Canvas");
                    if (canvasObj != null) targetCanvas = canvasObj.GetComponent<Canvas>();
                }
                if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (letterboxRoot == null && targetCanvas != null)
            {
                var existing = targetCanvas.transform.Find("CutsceneLetterboxPanel");
                if (existing != null)
                {
                    letterboxRoot = existing.gameObject;
                }
                else
                {
                    BuildProceduralLetterboxUI();
                }
            }

            if (letterboxRoot != null)
            {
                if (letterboxCanvasGroup == null) letterboxCanvasGroup = letterboxRoot.GetComponent<CanvasGroup>();
                if (topBar == null)
                {
                    var tBar = letterboxRoot.transform.Find("TopBar");
                    if (tBar != null) topBar = tBar.GetComponent<RectTransform>();
                }
                if (bottomBar == null)
                {
                    var bBar = letterboxRoot.transform.Find("BottomBar");
                    if (bBar != null) bottomBar = bBar.GetComponent<RectTransform>();
                }

                if (topBar != null)
                {
                    topBar.sizeDelta = new Vector2(0f, barHeight);
                }
                if (bottomBar != null)
                {
                    bottomBar.sizeDelta = new Vector2(0f, barHeight);
                }

                if (subtitleText == null || subtitleCanvasGroup == null)
                {
                    var sub = letterboxRoot.transform.Find("SubtitleText");
                    if (sub != null)
                    {
                        subtitleText = sub.GetComponent<TextMeshProUGUI>();
                        subtitleCanvasGroup = sub.GetComponent<CanvasGroup>();
                    }
                }

                if (subtitleText != null)
                {
                    subtitleText.fontSize = 28f;
                    subtitleText.enableAutoSizing = true;
                    subtitleText.fontSizeMin = 22f;
                    subtitleText.fontSizeMax = 32f;
                    subtitleText.fontStyle = FontStyles.Bold;
                    subtitleText.alignment = TextAlignmentOptions.Center;
                    subtitleText.color = new Color(0.98f, 0.97f, 0.94f, 1f); // Moonlight Ivory
                    subtitleText.characterSpacing = 1.2f;
                    subtitleText.lineSpacing = 6f;

                    var sRt = subtitleText.rectTransform;
                    if (sRt != null)
                    {
                        sRt.anchorMin = new Vector2(0.12f, 0f);
                        sRt.anchorMax = new Vector2(0.88f, 0f);
                        sRt.pivot = new Vector2(0.5f, 0f);
                        sRt.anchoredPosition = new Vector2(0f, 26f);
                        sRt.sizeDelta = new Vector2(0f, 76f);
                    }
                    EnsureSubtitleBackdropPlate(subtitleText.transform);
                }

                if (skipGaugeRoot == null)
                {
                    var gauge = letterboxRoot.transform.Find("SkipGaugeRoot");
                    if (gauge != null)
                    {
                        skipGaugeRoot = gauge.gameObject;
                        var fill = gauge.Find("SkipFill");
                        if (fill != null) skipFillImage = fill.GetComponent<Image>();
                        var prompt = gauge.Find("SkipPrompt");
                        if (prompt != null) skipPromptText = prompt.GetComponent<TextMeshProUGUI>();
                    }
                }

                // Strictly ensure IntroStoryPanel and cards are present and wired
                var intro = letterboxRoot.transform.Find("IntroStoryPanel");
                if (intro != null)
                {
                    introStoryRoot = intro.gameObject;
                    introStoryCanvasGroup = intro.GetComponent<CanvasGroup>();
                    var card = intro.Find("IntroStoryCardText");
                    if (card != null)
                    {
                        introStoryText = card.GetComponent<TextMeshProUGUI>();
                        introStoryTextCanvasGroup = card.GetComponent<CanvasGroup>();
                    }
                    if (introStoryText != null)
                    {
                        introStoryText.fontSize = 36f;
                        introStoryText.enableAutoSizing = true;
                        introStoryText.fontSizeMin = 26f;
                        introStoryText.fontSizeMax = 42f;
                        introStoryText.alignment = TextAlignmentOptions.Center;
                        introStoryText.color = new Color(0.98f, 0.97f, 0.95f, 1f);
                        introStoryText.lineSpacing = 18f;
                        introStoryText.characterSpacing = 1.2f;
                        introStoryText.fontStyle = FontStyles.Bold;
                    }
                    EnsureStoryCardObjects(intro);
                }
                else
                {
                    BuildProceduralIntroStoryPanel(letterboxRoot.transform);
                }

                if (skipPromptText != null)
                {
                    skipPromptText.fontSize = 15f;
                    skipPromptText.fontStyle = FontStyles.Bold;
                    skipPromptText.alignment = TextAlignmentOptions.Right;
                    skipPromptText.color = new Color(0.85f, 0.88f, 0.92f, 0.95f);
                    skipPromptText.text = "<color=#F5D166><b>[ESC]</b></color> / HOLD <color=#F5D166><b>[SPACE]</b></color> TO SKIP";
                    var pRt = skipPromptText.rectTransform;
                    if (pRt != null)
                    {
                        pRt.anchorMin = new Vector2(0f, 0f);
                        pRt.anchorMax = new Vector2(0.85f, 1f);
                        pRt.offsetMin = Vector2.zero;
                        pRt.offsetMax = Vector2.zero;
                    }
                }
            }
        }

        private void EnsureSubtitleBackdropPlate(Transform subTransform)
        {
            if (subTransform == null || letterboxRoot == null) return;

            var plateTransform = letterboxRoot.transform.Find("SubtitlePlate");
            GameObject plateObj;
            if (plateTransform != null)
            {
                plateObj = plateTransform.gameObject;
            }
            else
            {
                plateObj = new GameObject("SubtitlePlate", typeof(RectTransform), typeof(Image));
                plateObj.transform.SetParent(letterboxRoot.transform, false);
                plateObj.transform.SetSiblingIndex(subTransform.GetSiblingIndex());
            }

            var plateRt = plateObj.GetComponent<RectTransform>();
            plateRt.anchorMin = new Vector2(0.10f, 0f);
            plateRt.anchorMax = new Vector2(0.90f, 0f);
            plateRt.pivot = new Vector2(0.5f, 0f);
            plateRt.anchoredPosition = new Vector2(0f, 20f);
            plateRt.sizeDelta = new Vector2(0f, 88f);

            var img = plateObj.GetComponent<Image>();
            img.color = new Color(0.03f, 0.04f, 0.06f, 0.78f); // Frosted Obsidian Backing Plate
            img.raycastTarget = false;

            // Top gold accent line
            var lineTransform = plateObj.transform.Find("TopAccentLine");
            GameObject lineObj;
            if (lineTransform != null)
            {
                lineObj = lineTransform.gameObject;
            }
            else
            {
                lineObj = new GameObject("TopAccentLine", typeof(RectTransform), typeof(Image));
                lineObj.transform.SetParent(plateObj.transform, false);
            }

            var lineRt = lineObj.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.25f, 1f);
            lineRt.anchorMax = new Vector2(0.75f, 1f);
            lineRt.pivot = new Vector2(0.5f, 1f);
            lineRt.anchoredPosition = Vector2.zero;
            lineRt.sizeDelta = new Vector2(0f, 2f);

            var lineImg = lineObj.GetComponent<Image>();
            lineImg.color = new Color(0.95f, 0.78f, 0.32f, 0.85f); // Subtle Golden Glow Accent
            lineImg.raycastTarget = false;
        }

        private void BuildProceduralLetterboxUI()
        {
            // Root panel
            letterboxRoot = new GameObject("CutsceneLetterboxPanel", typeof(RectTransform), typeof(CanvasGroup));
            letterboxRoot.transform.SetParent(targetCanvas.transform, false);

            var rootRt = letterboxRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            letterboxCanvasGroup = letterboxRoot.GetComponent<CanvasGroup>();
            letterboxCanvasGroup.alpha = 0f;
            letterboxCanvasGroup.blocksRaycasts = false;
            letterboxCanvasGroup.interactable = false;

            // Top Letterbox Bar
            var topBarObj = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBarObj.transform.SetParent(letterboxRoot.transform, false);
            topBar = topBarObj.GetComponent<RectTransform>();
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(0f, barHeight);
            var topImg = topBarObj.GetComponent<Image>();
            topImg.color = new Color(0.04f, 0.05f, 0.08f, 1.0f); // Cinematic Deep Obsidian

            // Bottom Letterbox Bar
            var btmBarObj = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            btmBarObj.transform.SetParent(letterboxRoot.transform, false);
            bottomBar = btmBarObj.GetComponent<RectTransform>();
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.sizeDelta = new Vector2(0f, barHeight);
            var btmImg = btmBarObj.GetComponent<Image>();
            btmImg.color = new Color(0.04f, 0.05f, 0.08f, 1.0f);

            // Subtitle Text Container
            var subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            subObj.transform.SetParent(letterboxRoot.transform, false);
            var subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.12f, 0f);
            subRt.anchorMax = new Vector2(0.88f, 0f);
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.anchoredPosition = new Vector2(0f, 26f);
            subRt.sizeDelta = new Vector2(0f, 76f);

            subtitleText = subObj.GetComponent<TextMeshProUGUI>();
            subtitleText.fontSize = 28f;
            subtitleText.enableAutoSizing = true;
            subtitleText.fontSizeMin = 22f;
            subtitleText.fontSizeMax = 32f;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(0.98f, 0.97f, 0.94f, 1f); // Moonlight Ivory
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.characterSpacing = 1.2f;
            subtitleText.lineSpacing = 6f;
            subtitleText.text = string.Empty;

            subtitleCanvasGroup = subObj.GetComponent<CanvasGroup>();
            subtitleCanvasGroup.alpha = 0f;

            EnsureSubtitleBackdropPlate(subObj.transform);

            // Hold-to-Skip Visual Gauge (bottom right)
            skipGaugeRoot = new GameObject("SkipGaugeRoot", typeof(RectTransform));
            skipGaugeRoot.transform.SetParent(letterboxRoot.transform, false);
            var gaugeRt = skipGaugeRoot.GetComponent<RectTransform>();
            gaugeRt.anchorMin = new Vector2(1f, 0f);
            gaugeRt.anchorMax = new Vector2(1f, 0f);
            gaugeRt.pivot = new Vector2(1f, 0f);
            gaugeRt.anchoredPosition = new Vector2(-28f, 26f);
            gaugeRt.sizeDelta = new Vector2(280f, 40f);

            // Skip Prompt Label
            var promptObj = new GameObject("SkipPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
            promptObj.transform.SetParent(skipGaugeRoot.transform, false);
            var pRt = promptObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(0.85f, 1f);
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;

            skipPromptText = promptObj.GetComponent<TextMeshProUGUI>();
            skipPromptText.fontSize = 15f;
            skipPromptText.fontStyle = FontStyles.Bold;
            skipPromptText.alignment = TextAlignmentOptions.Right;
            skipPromptText.color = new Color(0.85f, 0.88f, 0.92f, 0.95f);
            skipPromptText.text = "<color=#F5D166><b>[ESC]</b></color> / HOLD <color=#F5D166><b>[SPACE]</b></color> TO SKIP";

            // Circular / Radial Fill Background Ring
            var ringBgObj = new GameObject("SkipRingBg", typeof(RectTransform), typeof(Image));
            ringBgObj.transform.SetParent(skipGaugeRoot.transform, false);
            var ringBgRt = ringBgObj.GetComponent<RectTransform>();
            ringBgRt.anchorMin = new Vector2(0.85f, 0.5f);
            ringBgRt.anchorMax = new Vector2(0.85f, 0.5f);
            ringBgRt.pivot = new Vector2(0.5f, 0.5f);
            ringBgRt.sizeDelta = new Vector2(28f, 28f);

            var ringBgImg = ringBgObj.GetComponent<Image>();
            ringBgImg.color = new Color(0.2f, 0.22f, 0.28f, 0.6f);

            // Circular / Radial Fill Image
            var ringFillObj = new GameObject("SkipFill", typeof(RectTransform), typeof(Image));
            ringFillObj.transform.SetParent(skipGaugeRoot.transform, false);
            var ringFillRt = ringFillObj.GetComponent<RectTransform>();
            ringFillRt.anchorMin = new Vector2(0.85f, 0.5f);
            ringFillRt.anchorMax = new Vector2(0.85f, 0.5f);
            ringFillRt.pivot = new Vector2(0.5f, 0.5f);
            ringFillRt.sizeDelta = new Vector2(28f, 28f);

            skipFillImage = ringFillObj.GetComponent<Image>();
            skipFillImage.color = new Color(0.96f, 0.62f, 0.15f, 1f); // Warm Amber
            skipFillImage.type = Image.Type.Filled;
            skipFillImage.fillMethod = Image.FillMethod.Radial360;
            skipFillImage.fillOrigin = (int)Image.Origin360.Top;
            skipFillImage.fillClockwise = true;
            skipFillImage.fillAmount = 0f;

            // Intro Story Backdrop & Story Text (Full Screen Overlay)
            BuildProceduralIntroStoryPanel(letterboxRoot.transform);

            skipGaugeRoot.SetActive(false);
            if (introStoryRoot != null) introStoryRoot.SetActive(false);
            letterboxRoot.SetActive(false);
        }

        private void BuildProceduralIntroStoryPanel(Transform parent)
        {
            var introObj = new GameObject("IntroStoryPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            introObj.transform.SetParent(parent, false);
            introStoryRoot = introObj;
            var introRt = introObj.GetComponent<RectTransform>();
            introRt.anchorMin = Vector2.zero;
            introRt.anchorMax = Vector2.one;
            introRt.offsetMin = Vector2.zero;
            introRt.offsetMax = Vector2.zero;

            var introBg = introObj.GetComponent<Image>();
            introBg.color = new Color(0.02f, 0.03f, 0.04f, 1f); // Deep Void Black

            introStoryCanvasGroup = introObj.GetComponent<CanvasGroup>();
            introStoryCanvasGroup.alpha = 0f;
            introStoryCanvasGroup.blocksRaycasts = false;
            introStoryCanvasGroup.interactable = false;

            var cardObj = new GameObject("IntroStoryCardText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            cardObj.transform.SetParent(introStoryRoot.transform, false);
            var cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.10f, 0.25f);
            cardRt.anchorMax = new Vector2(0.90f, 0.75f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            introStoryText = cardObj.GetComponent<TextMeshProUGUI>();
            introStoryText.fontSize = 36f;
            introStoryText.enableAutoSizing = true;
            introStoryText.fontSizeMin = 26f;
            introStoryText.fontSizeMax = 42f;
            introStoryText.alignment = TextAlignmentOptions.Center;
            introStoryText.color = new Color(0.98f, 0.97f, 0.95f, 1f); // Moonlight Warm Ivory
            introStoryText.lineSpacing = 18f;
            introStoryText.characterSpacing = 1.2f;
            introStoryText.fontStyle = FontStyles.Bold;
            introStoryText.text = string.Empty;

            introStoryTextCanvasGroup = cardObj.GetComponent<CanvasGroup>();
            introStoryTextCanvasGroup.alpha = 0f;

            EnsureStoryCardObjects(introObj.transform);
        }

        public void EnsureStoryCardObjects(Transform introTransform = null)
        {
            if (introTransform == null && introStoryRoot != null)
            {
                introTransform = introStoryRoot.transform;
            }

            if (storyCardObjects == null || storyCardObjects.Length != 4)
            {
                storyCardObjects = new GameObject[4];
            }

            string[] cardTexts = new string[4]
            {
                "<size=65%><color=#E5B044>—   P R O L O G U E   —</color></size>\n\n“Ten years in the army was enough.\nAt thirty, I just wanted a quiet life.”",
                "<size=65%><color=#E5B044>—   P R O L O G U E   —</color></size>\n\n“I used all my savings to buy a cheap piece of land\nfar away from the city.”",
                "<size=65%><color=#E5B044>—   P R O L O G U E   —</color></size>\n\n“For a while, everything went fine.\nI cleared the weeds and built a few small farm plots.”",
                "<size=65%><color=#E5B044>—   P R O L O G U E   —</color></size>\n\n“Life was peaceful...\nuntil the day I started digging in an untouched part of the field.”"
            };

            for (int i = 0; i < 4; i++)
            {
                string cardName = $"IntroStoryCard_{i + 1}";
                Transform existingCard = introTransform != null ? introTransform.Find(cardName) : null;
                if (existingCard == null && storyCardObjects[i] != null)
                {
                    existingCard = storyCardObjects[i].transform;
                }

                if (existingCard != null)
                {
                    storyCardObjects[i] = existingCard.gameObject;
                    var rt = existingCard.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.10f, 0.25f);
                        rt.anchorMax = new Vector2(0.90f, 0.75f);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }
                    var tmp = existingCard.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.fontSize = 36f;
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMin = 26f;
                        tmp.fontSizeMax = 42f;
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.color = new Color(0.98f, 0.97f, 0.95f, 1f);
                        tmp.lineSpacing = 18f;
                        tmp.characterSpacing = 1.2f;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.text = cardTexts[i];
                    }
                }
                else if (introTransform != null)
                {
                    var newCard = new GameObject(cardName, typeof(RectTransform), typeof(TextMeshProUGUI));
                    newCard.transform.SetParent(introTransform, false);
                    var rt = newCard.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.10f, 0.25f);
                    rt.anchorMax = new Vector2(0.90f, 0.75f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var tmp = newCard.GetComponent<TextMeshProUGUI>();
                    tmp.fontSize = 36f;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 26f;
                    tmp.fontSizeMax = 42f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.98f, 0.97f, 0.95f, 1f);
                    tmp.lineSpacing = 18f;
                    tmp.characterSpacing = 1.2f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.text = cardTexts[i];

                    newCard.SetActive(false); // Controlled by Timeline ActivationTrack
                    storyCardObjects[i] = newCard;
                }
            }
        }
    }
}
