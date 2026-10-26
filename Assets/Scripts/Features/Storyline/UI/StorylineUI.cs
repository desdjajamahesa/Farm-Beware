using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FeaturesStoryline.Data;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline.UI
{
    /// <summary>
    /// UI Controller handling all visual and narrative presentations for the Storyline system:
    /// - HUD Banner (non-intrusive upper screen notification)
    /// - Dialogue Box (speaker, text, continue button)
    /// - Full Screen Overlay (cinematic prologue, epilogue, victory screens)
    /// - Dream Sequence (sleep vignette)
    /// - Camera Shake trigger
    /// Automatically guarantees runtime UI elements exist via procedural construction if canvas is unassigned.
    /// </summary>
    [DisallowMultipleComponent]
    public class StorylineUI : MonoBehaviour, IModalWindow
    {
        private static StorylineUI _instance;
        public static bool HasInstance => _instance != null;
        private static bool _isQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isQuitting = false;
        }

        public static StorylineUI Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        bool IModalWindow.IsOpen => (dialoguePanel != null && dialoguePanel.activeSelf) || (overlayPanel != null && overlayPanel.activeSelf);
        void IModalWindow.OpenModal() {}
        void IModalWindow.CloseModal()
        {
            if (dialoguePanel != null && dialoguePanel.activeSelf)
                DismissDialogue();
            else if (overlayPanel != null && overlayPanel.activeSelf)
                DismissOverlay();
        }

        [Header("Canvas & Root Panels")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject bannerPanel;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private GameObject dreamPanel;

        [Header("HUD Banner Elements")]
        [SerializeField] private TextMeshProUGUI bannerTitleText;
        [SerializeField] private TextMeshProUGUI bannerBodyText;
        [SerializeField] private CanvasGroup bannerCanvasGroup;

        [Header("Dialogue Elements")]
        [SerializeField] private TextMeshProUGUI dialogueSpeakerText;
        [SerializeField] private TextMeshProUGUI dialogueBodyText;
        [SerializeField] private Button dialogueContinueButton;

        [Header("Overlay Elements")]
        [SerializeField] private TextMeshProUGUI overlayTitleText;
        [SerializeField] private TextMeshProUGUI overlayBodyText;
        [SerializeField] private TextMeshProUGUI overlayPromptText;
        [SerializeField] private CanvasGroup overlayCanvasGroup;

        [Header("Dream Elements")]
        [SerializeField] private TextMeshProUGUI dreamBodyText;
        [SerializeField] private CanvasGroup dreamCanvasGroup;

        private Coroutine _activeBannerRoutine;
        private Coroutine _activeOverlayRoutine;
        private Coroutine _activeDreamRoutine;
        private Action _onDialogueDismissed;
        private Action _onOverlayDismissed;

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

        private void Update()
        {
            // Allow Space, Enter, or Click to advance dialogue if open
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                bool advanceDialogue = false;
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        advanceDialogue = true;
                    }
                }
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    advanceDialogue = true;
                }

                if (advanceDialogue)
                {
                    DismissDialogue();
                }
            }

            // Allow Space, Enter, or Click to advance overlay if open
            if (overlayPanel != null && overlayPanel.activeSelf && overlayCanvasGroup != null && overlayCanvasGroup.alpha >= 0.95f)
            {
                bool advanceOverlay = false;
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        advanceOverlay = true;
                    }
                }
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    advanceOverlay = true;
                }

                if (advanceOverlay)
                {
                    DismissOverlay();
                }
            }
        }

        public void DisplayBeat(StoryBeatData beat, Action onComplete = null)
        {
            if (beat == null)
            {
                onComplete?.Invoke();
                return;
            }

            var facts = StorylineManager.Instance != null ? StorylineManager.Instance.FactBoard : ServiceLocator.Resolve<INarrativeFactBoard>();
            string formattedTitle = beat.title;
            string formattedText = beat.FormatNarrativeText(facts);
            string formattedHudText = !string.IsNullOrEmpty(beat.hudBannerText) ? beat.FormatHudBannerText(facts) : formattedText;

            // Trigger screen shake if requested
            if (beat.triggersScreenShake)
            {
                TriggerScreenShake(beat.screenShakeIntensity, beat.screenShakeDuration);
            }

            // Play audio cue if present
            if (beat.narrativeAudioCue != null)
            {
                var audioSrc = GetComponent<AudioSource>();
                if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
                audioSrc.PlayOneShot(beat.narrativeAudioCue);
            }

            switch (beat.deliveryType)
            {
                case StoryDeliveryType.HudBanner:
                    ShowBanner(formattedTitle, formattedHudText, beat.displayDuration, onComplete);
                    break;

                case StoryDeliveryType.DialogueBox:
                    ShowDialogue(beat.speakerName, formattedText, onComplete);
                    break;

                case StoryDeliveryType.FullScreenOverlay:
                    ShowOverlay(formattedTitle, formattedText, beat.displayDuration, onComplete);
                    break;

                case StoryDeliveryType.DreamSequence:
                    ShowDream(formattedText, beat.displayDuration, onComplete);
                    break;

                case StoryDeliveryType.FloatingWorldText:
                    ShowFloatingText(formattedText);
                    onComplete?.Invoke();
                    break;

                case StoryDeliveryType.JournalPage:
                    ShowDialogue(string.IsNullOrEmpty(beat.speakerName) ? "Field Notes" : beat.speakerName, formattedText, onComplete);
                    break;

                default:
                    ShowBanner(formattedTitle, formattedText, beat.displayDuration, onComplete);
                    break;
            }
        }

        public void ShowBanner(string title, string message, float duration = 4f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (bannerPanel == null || bannerCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_activeBannerRoutine != null) StopCoroutine(_activeBannerRoutine);
            _activeBannerRoutine = StartCoroutine(RoutineBanner(title, message, duration, onComplete));
        }

        private IEnumerator RoutineBanner(string title, string message, float duration, Action onComplete)
        {
            if (bannerTitleText != null) bannerTitleText.text = string.IsNullOrEmpty(title) ? "STORY BEAT" : title.ToUpperInvariant();
            if (bannerBodyText != null) bannerBodyText.text = message;

            bannerPanel.SetActive(true);
            bannerCanvasGroup.alpha = 0f;
            bannerCanvasGroup.blocksRaycasts = false;
            bannerCanvasGroup.interactable = false;

            // Fade in
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                bannerCanvasGroup.alpha = Mathf.Clamp01(t / 0.35f);
                yield return null;
            }
            bannerCanvasGroup.alpha = 1f;

            // Display duration
            yield return new WaitForSecondsRealtime(Mathf.Max(1.5f, duration));

            // Fade out
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.4f);
                yield return null;
            }
            bannerCanvasGroup.alpha = 0f;
            bannerPanel.SetActive(false);
            _activeBannerRoutine = null;

            onComplete?.Invoke();
        }

        public void ShowDialogue(string speaker, string message, Action onDismiss = null)
        {
            EnsureUIHierarchy();
            if (dialoguePanel == null)
            {
                onDismiss?.Invoke();
                return;
            }

            _onDialogueDismissed = onDismiss;
            dialoguePanel.SetActive(true);

            // Dynamically ensure child text references are resolved if inspector bindings were null
            if (dialogueSpeakerText == null)
                dialogueSpeakerText = dialoguePanel.transform.Find("DialogueSpeaker")?.GetComponent<TextMeshProUGUI>();
            if (dialogueBodyText == null)
                dialogueBodyText = dialoguePanel.transform.Find("DialogueBody")?.GetComponent<TextMeshProUGUI>();
            if (dialogueContinueButton == null)
            {
                dialogueContinueButton = dialoguePanel.transform.Find("DialogueButton")?.GetComponent<Button>();
                if (dialogueContinueButton != null)
                {
                    dialogueContinueButton.onClick.RemoveListener(DismissDialogue);
                    dialogueContinueButton.onClick.AddListener(DismissDialogue);
                }
            }

            // Ensure dialogue panel has top sorting order and last sibling priority
            // so it is NEVER obscured by Workbench, Merchant, or Inventory UI panels
            var panelCanvas = dialoguePanel.GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = dialoguePanel.AddComponent<Canvas>();
            }
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 500;

            if (!dialoguePanel.TryGetComponent<GraphicRaycaster>(out _))
            {
                dialoguePanel.AddComponent<GraphicRaycaster>();
            }

            var dlgRT = dialoguePanel.GetComponent<RectTransform>();
            if (dlgRT != null)
            {
                dlgRT.anchoredPosition = new Vector2(0f, 115f); // Above the hotbar
            }

            dialoguePanel.transform.SetAsLastSibling();

            if (dialogueSpeakerText != null)
                dialogueSpeakerText.text = string.IsNullOrEmpty(speaker) ? "Veteran" : speaker;
            if (dialogueBodyText != null)
                dialogueBodyText.text = message;

            // Notify modal stack
            var modalStack = ServiceLocator.Resolve<IModalStackService>();
            modalStack?.Push(this);
        }

        public void DismissDialogue()
        {
            if (dialoguePanel == null || !dialoguePanel.activeSelf) return;

            dialoguePanel.SetActive(false);
            var modalStack = ServiceLocator.Resolve<IModalStackService>();
            modalStack?.PopSpecific(this);

            var cb = _onDialogueDismissed;
            _onDialogueDismissed = null;
            cb?.Invoke();
        }

        public void ShowOverlay(string title, string message, float minDuration = 4f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (overlayPanel == null || overlayCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            overlayPanel.SetActive(true);

            // Ensure overlay panel renders above all other canvases and panels
            var panelCanvas = overlayPanel.GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = overlayPanel.AddComponent<Canvas>();
            }
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 600;

            if (!overlayPanel.TryGetComponent<GraphicRaycaster>(out _))
            {
                overlayPanel.AddComponent<GraphicRaycaster>();
            }

            overlayPanel.transform.SetAsLastSibling();

            if (_activeOverlayRoutine != null) StopCoroutine(_activeOverlayRoutine);

            _onOverlayDismissed = onComplete;

            // Notify modal stack for input gating
            var modalStack = ServiceLocator.Resolve<IModalStackService>();
            modalStack?.Push(this);

            _activeOverlayRoutine = StartCoroutine(RoutineOverlay(title, message, minDuration));
        }

        private IEnumerator RoutineOverlay(string title, string message, float minDuration)
        {
            if (overlayTitleText != null) overlayTitleText.text = title;
            if (overlayBodyText != null) overlayBodyText.text = message;
            if (overlayPromptText != null) overlayPromptText.text = "Press [SPACE] or Click to Continue";

            overlayPanel.SetActive(true);
            overlayCanvasGroup.alpha = 0f;

            // Fade to black
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                overlayCanvasGroup.alpha = Mathf.Clamp01(t / 0.6f);
                yield return null;
            }
            overlayCanvasGroup.alpha = 1f;

            // Wait for display duration if minDuration is specified
            float elapsed = 0f;
            while (elapsed < minDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _activeOverlayRoutine = null;
        }

        public void DismissOverlay()
        {
            if (overlayPanel == null || !overlayPanel.activeSelf) return;

            if (_activeOverlayRoutine != null)
            {
                StopCoroutine(_activeOverlayRoutine);
                _activeOverlayRoutine = null;
            }

            StartCoroutine(RoutineDismissOverlay());
        }

        private IEnumerator RoutineDismissOverlay()
        {
            if (overlayCanvasGroup != null)
            {
                float t = 0f;
                while (t < 0.35f)
                {
                    t += Time.unscaledDeltaTime;
                    overlayCanvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.35f);
                    yield return null;
                }
                overlayCanvasGroup.alpha = 0f;
            }
            overlayPanel.SetActive(false);

            var modalStack = ServiceLocator.Resolve<IModalStackService>();
            modalStack?.PopSpecific(this);

            var cb = _onOverlayDismissed;
            _onOverlayDismissed = null;
            cb?.Invoke();
        }

        public void ShowDream(string message, float duration = 4f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (dreamPanel == null || dreamCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_activeDreamRoutine != null) StopCoroutine(_activeDreamRoutine);
            _activeDreamRoutine = StartCoroutine(RoutineDream(message, duration, onComplete));
        }

        private IEnumerator RoutineDream(string message, float duration, Action onComplete)
        {
            if (dreamBodyText != null) dreamBodyText.text = message;
            dreamPanel.SetActive(true);
            dreamCanvasGroup.alpha = 0f;

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                dreamCanvasGroup.alpha = Mathf.Clamp01(t / 0.5f);
                yield return null;
            }
            dreamCanvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(duration);

            t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                dreamCanvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.5f);
                yield return null;
            }
            dreamCanvasGroup.alpha = 0f;
            dreamPanel.SetActive(false);
            _activeDreamRoutine = null;

            onComplete?.Invoke();
        }

        private void ShowFloatingText(string message)
        {
            var floatingService = ServiceLocator.Resolve<IFloatingTextService>();
            if (floatingService != null)
            {
                var player = GameObject.FindWithTag("Player");
                Vector3 pos = player != null ? player.transform.position + Vector3.up * 2f : Vector3.up * 2f;
                floatingService.SpawnText(pos, message, new Color(1f, 0.85f, 0.4f));
            }
            else
            {
                ShowBanner("LORE", message, 3f);
            }
        }

        public void TriggerScreenShake(float intensity, float duration)
        {
            if (FeaturesCamera.IsometricCameraController.Instance != null)
            {
                FeaturesCamera.IsometricCameraController.Instance.TriggerShake(duration, intensity);
                return;
            }

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                StartCoroutine(RoutineCameraShake(cam.transform, intensity, duration));
            }
        }

        private IEnumerator RoutineCameraShake(Transform camTransform, float intensity, float duration)
        {
            Vector3 originalPos = camTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / duration);
                Vector3 offset = UnityEngine.Random.insideUnitSphere * (intensity * damp);
                camTransform.localPosition = originalPos + offset;
                yield return null;
            }

            camTransform.localPosition = originalPos;
        }

        public void EnsureUIHierarchy()
        {
            if (targetCanvas == null)
            {
                var canvasObj = GameObject.Find("UI_Canvas");
                if (canvasObj != null) targetCanvas = canvasObj.GetComponent<Canvas>();
                if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasGO = new GameObject("Storyline_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                targetCanvas = canvasGO.GetComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 80;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // 1. Procedural Banner Panel
            if (bannerPanel == null)
            {
                var existingBanner = targetCanvas.transform.Find("StoryBannerPanel");
                if (existingBanner != null)
                {
                    bannerPanel = existingBanner.gameObject;
                    bannerCanvasGroup = bannerPanel.GetComponent<CanvasGroup>();
                    var tTitle = bannerPanel.transform.Find("BannerTitle");
                    if (tTitle != null) bannerTitleText = tTitle.GetComponent<TextMeshProUGUI>();
                    var tBody = bannerPanel.transform.Find("BannerBody");
                    if (tBody != null) bannerBodyText = tBody.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    bannerPanel = new GameObject("StoryBannerPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    bannerPanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = bannerPanel.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -40f);
                    rt.sizeDelta = new Vector2(700f, 100f);

                    var img = bannerPanel.GetComponent<Image>();
                    img.color = new Color(0.08f, 0.10f, 0.12f, 0.92f);

                    bannerCanvasGroup = bannerPanel.GetComponent<CanvasGroup>();
                    bannerCanvasGroup.blocksRaycasts = false;

                    // Title
                    var titleGO = new GameObject("BannerTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGO.transform.SetParent(bannerPanel.transform, false);
                    var titleRT = titleGO.GetComponent<RectTransform>();
                    titleRT.anchorMin = new Vector2(0f, 0.6f);
                    titleRT.anchorMax = new Vector2(1f, 1f);
                    titleRT.offsetMin = new Vector2(16f, 0f);
                    titleRT.offsetMax = new Vector2(-16f, -6f);
                    bannerTitleText = titleGO.GetComponent<TextMeshProUGUI>();
                    bannerTitleText.fontSize = 22;
                    bannerTitleText.fontStyle = FontStyles.Bold;
                    bannerTitleText.color = new Color(1f, 0.85f, 0.4f);
                    bannerTitleText.alignment = TextAlignmentOptions.Center;

                    // Body
                    var bodyGO = new GameObject("BannerBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(bannerPanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0f, 0f);
                    bodyRT.anchorMax = new Vector2(1f, 0.6f);
                    bodyRT.offsetMin = new Vector2(16f, 6f);
                    bodyRT.offsetMax = new Vector2(-16f, 0f);
                    bannerBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    bannerBodyText.fontSize = 18;
                    bannerBodyText.color = Color.white;
                    bannerBodyText.alignment = TextAlignmentOptions.Center;
                    bannerBodyText.textWrappingMode = TextWrappingModes.Normal;
                }
            }
            if (bannerPanel != null)
            {
                bannerPanel.SetActive(false);
                if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 0f;
                if (bannerTitleText != null)
                {
                    bannerTitleText.fontSize = 22;
                    bannerTitleText.text = "";
                }
                if (bannerBodyText != null)
                {
                    bannerBodyText.fontSize = 18;
                    bannerBodyText.text = "";
                }
            }

            // 2. Procedural Dialogue Panel
            if (dialoguePanel == null)
            {
                var existingDlg = targetCanvas.transform.Find("StoryDialoguePanel");
                if (existingDlg != null)
                {
                    dialoguePanel = existingDlg.gameObject;
                }
                else
                {
                    dialoguePanel = new GameObject("StoryDialoguePanel", typeof(RectTransform), typeof(Image));
                    dialoguePanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = dialoguePanel.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(0f, 115f);
                    rt.sizeDelta = new Vector2(920f, 200f);

                    var img = dialoguePanel.GetComponent<Image>();
                    img.color = new Color(0.06f, 0.08f, 0.10f, 0.95f);

                    // Speaker
                    var spkGO = new GameObject("DialogueSpeaker", typeof(RectTransform), typeof(TextMeshProUGUI));
                    spkGO.transform.SetParent(dialoguePanel.transform, false);
                    var spkRT = spkGO.GetComponent<RectTransform>();
                    spkRT.anchorMin = new Vector2(0f, 0.7f);
                    spkRT.anchorMax = new Vector2(1f, 1f);
                    spkRT.offsetMin = new Vector2(24f, 0f);
                    spkRT.offsetMax = new Vector2(-24f, -10f);
                    dialogueSpeakerText = spkGO.GetComponent<TextMeshProUGUI>();
                    dialogueSpeakerText.fontSize = 24;
                    dialogueSpeakerText.fontStyle = FontStyles.Bold;
                    dialogueSpeakerText.color = new Color(0.95f, 0.75f, 0.35f);

                    // Body
                    var bodyGO = new GameObject("DialogueBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(dialoguePanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0f, 0f);
                    bodyRT.anchorMax = new Vector2(1f, 0.7f);
                    bodyRT.offsetMin = new Vector2(24f, 16f);
                    bodyRT.offsetMax = new Vector2(-120f, 0f);
                    dialogueBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    dialogueBodyText.fontSize = 21;
                    dialogueBodyText.color = Color.white;
                    dialogueBodyText.textWrappingMode = TextWrappingModes.Normal;

                    // Button
                    var btnGO = new GameObject("DialogueButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGO.transform.SetParent(dialoguePanel.transform, false);
                    var btnRT = btnGO.GetComponent<RectTransform>();
                    btnRT.anchorMin = new Vector2(1f, 0f);
                    btnRT.anchorMax = new Vector2(1f, 0f);
                    btnRT.pivot = new Vector2(1f, 0f);
                    btnRT.anchoredPosition = new Vector2(-20f, 20f);
                    btnRT.sizeDelta = new Vector2(140f, 36f);
                    btnGO.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.3f);
                    dialogueContinueButton = btnGO.GetComponent<Button>();
                    dialogueContinueButton.onClick.AddListener(DismissDialogue);

                    var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    btnTextGO.transform.SetParent(btnGO.transform, false);
                    var btxt = btnTextGO.GetComponent<TextMeshProUGUI>();
                    btxt.text = "Continue [SPACE] >";
                    btxt.fontSize = 12;
                    btxt.alignment = TextAlignmentOptions.Center;
                    btxt.color = Color.white;
                }
            }

            if (dialoguePanel != null)
            {
                var rt = dialoguePanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, 115f);
                    rt.sizeDelta = new Vector2(920f, 200f);
                }

                if (dialogueSpeakerText == null)
                    dialogueSpeakerText = dialoguePanel.transform.Find("DialogueSpeaker")?.GetComponent<TextMeshProUGUI>();
                if (dialogueBodyText == null)
                    dialogueBodyText = dialoguePanel.transform.Find("DialogueBody")?.GetComponent<TextMeshProUGUI>();

                if (dialogueSpeakerText != null) dialogueSpeakerText.fontSize = 24;
                if (dialogueBodyText != null) dialogueBodyText.fontSize = 21;

                if (dialogueContinueButton == null)
                {
                    dialogueContinueButton = dialoguePanel.transform.Find("DialogueButton")?.GetComponent<Button>();
                    if (dialogueContinueButton != null)
                    {
                        dialogueContinueButton.onClick.RemoveListener(DismissDialogue);
                        dialogueContinueButton.onClick.AddListener(DismissDialogue);
                    }
                }
                var btnTxt = dialoguePanel.transform.Find("DialogueButton/Text")?.GetComponent<TextMeshProUGUI>()
                          ?? dialoguePanel.transform.Find("DialogueButton")?.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null)
                {
                    btnTxt.text = "Continue [SPACE] >";
                }

                var panelCanvas = dialoguePanel.GetComponent<Canvas>();
                if (panelCanvas == null)
                {
                    panelCanvas = dialoguePanel.AddComponent<Canvas>();
                }
                panelCanvas.overrideSorting = true;
                panelCanvas.sortingOrder = 500;
                if (!dialoguePanel.TryGetComponent<GraphicRaycaster>(out _))
                {
                    dialoguePanel.AddComponent<GraphicRaycaster>();
                }
                dialoguePanel.SetActive(false);
            }

            // 3. Procedural Overlay Panel (Prologue/Epilogue)
            if (overlayPanel == null)
            {
                var existingOverlay = targetCanvas.transform.Find("StoryOverlayPanel");
                if (existingOverlay != null)
                {
                    overlayPanel = existingOverlay.gameObject;
                    overlayCanvasGroup = overlayPanel.GetComponent<CanvasGroup>();
                    var tTitle = overlayPanel.transform.Find("OverlayTitle");
                    if (tTitle != null) overlayTitleText = tTitle.GetComponent<TextMeshProUGUI>();
                    var tBody = overlayPanel.transform.Find("OverlayBody");
                    if (tBody != null) overlayBodyText = tBody.GetComponent<TextMeshProUGUI>();
                    var tPrompt = overlayPanel.transform.Find("OverlayPrompt");
                    if (tPrompt != null) overlayPromptText = tPrompt.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    overlayPanel = new GameObject("StoryOverlayPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    overlayPanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = overlayPanel.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var img = overlayPanel.GetComponent<Image>();
                    img.color = new Color(0.02f, 0.02f, 0.03f, 0.98f);

                    overlayCanvasGroup = overlayPanel.GetComponent<CanvasGroup>();

                    // Title
                    var titleGO = new GameObject("OverlayTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGO.transform.SetParent(overlayPanel.transform, false);
                    var titleRT = titleGO.GetComponent<RectTransform>();
                    titleRT.anchorMin = new Vector2(0.1f, 0.65f);
                    titleRT.anchorMax = new Vector2(0.9f, 0.85f);
                    titleRT.offsetMin = Vector2.zero;
                    titleRT.offsetMax = Vector2.zero;
                    overlayTitleText = titleGO.GetComponent<TextMeshProUGUI>();
                    overlayTitleText.fontSize = 42;
                    overlayTitleText.fontStyle = FontStyles.Bold;
                    overlayTitleText.color = new Color(1f, 0.88f, 0.5f);
                    overlayTitleText.alignment = TextAlignmentOptions.Center;

                    // Body
                    var bodyGO = new GameObject("OverlayBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(overlayPanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0.15f, 0.25f);
                    bodyRT.anchorMax = new Vector2(0.85f, 0.65f);
                    bodyRT.offsetMin = Vector2.zero;
                    bodyRT.offsetMax = Vector2.zero;
                    overlayBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    overlayBodyText.fontSize = 24;
                    overlayBodyText.color = new Color(0.9f, 0.9f, 0.92f);
                    overlayBodyText.alignment = TextAlignmentOptions.Center;
                    overlayBodyText.textWrappingMode = TextWrappingModes.Normal;

                    // Prompt
                    var promptGO = new GameObject("OverlayPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
                    promptGO.transform.SetParent(overlayPanel.transform, false);
                    var promptRT = promptGO.GetComponent<RectTransform>();
                    promptRT.anchorMin = new Vector2(0.1f, 0.1f);
                    promptRT.anchorMax = new Vector2(0.9f, 0.2f);
                    promptRT.offsetMin = Vector2.zero;
                    promptRT.offsetMax = Vector2.zero;
                    overlayPromptText = promptGO.GetComponent<TextMeshProUGUI>();
                    overlayPromptText.fontSize = 15;
                    overlayPromptText.color = new Color(0.6f, 0.6f, 0.6f);
                    overlayPromptText.alignment = TextAlignmentOptions.Center;
                }
            }
            if (overlayPanel != null)
            {
                if (overlayTitleText != null) overlayTitleText.fontSize = 42;
                if (overlayBodyText != null) overlayBodyText.fontSize = 24;

                var panelCanvas = overlayPanel.GetComponent<Canvas>();
                if (panelCanvas == null)
                {
                    panelCanvas = overlayPanel.AddComponent<Canvas>();
                }
                panelCanvas.overrideSorting = true;
                panelCanvas.sortingOrder = 600;
                if (!overlayPanel.TryGetComponent<GraphicRaycaster>(out _))
                {
                    overlayPanel.AddComponent<GraphicRaycaster>();
                }
                overlayPanel.SetActive(false);
            }

            // 4. Procedural Dream Panel
            if (dreamPanel == null)
            {
                var existingDream = targetCanvas.transform.Find("StoryDreamPanel");
                if (existingDream != null)
                {
                    dreamPanel = existingDream.gameObject;
                    dreamCanvasGroup = dreamPanel.GetComponent<CanvasGroup>();
                    var tBody = dreamPanel.transform.Find("DreamBody");
                    if (tBody != null) dreamBodyText = tBody.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    dreamPanel = new GameObject("StoryDreamPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    dreamPanel.transform.SetParent(targetCanvas.transform, false);

                    var rt = dreamPanel.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var img = dreamPanel.GetComponent<Image>();
                    img.color = new Color(0.04f, 0.02f, 0.08f, 0.95f);

                    dreamCanvasGroup = dreamPanel.GetComponent<CanvasGroup>();

                    var bodyGO = new GameObject("DreamBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(dreamPanel.transform, false);
                    var bodyRT = bodyGO.GetComponent<RectTransform>();
                    bodyRT.anchorMin = new Vector2(0.15f, 0.3f);
                    bodyRT.anchorMax = new Vector2(0.85f, 0.7f);
                    bodyRT.offsetMin = Vector2.zero;
                    bodyRT.offsetMax = Vector2.zero;
                    dreamBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    dreamBodyText.fontSize = 22;
                    dreamBodyText.fontStyle = FontStyles.Italic;
                    dreamBodyText.color = new Color(0.85f, 0.7f, 1f);
                    dreamBodyText.alignment = TextAlignmentOptions.Center;
                    dreamBodyText.textWrappingMode = TextWrappingModes.Normal;
                }
            }
            if (dreamPanel != null)
            {
                dreamPanel.SetActive(false);
            }
        }
    }
}
