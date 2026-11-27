using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FeaturesStoryline.Data;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline.UI
{
    /// <summary>
    /// Interactive player dialogue choice option with associated fact board mutation.
    /// </summary>
    [Serializable]
    public struct DialogueChoice
    {
        public string text;
        public NarrativeFactMutation mutation;

        public DialogueChoice(string text, NarrativeFactMutation mutation)
        {
            this.text = text;
            this.mutation = mutation;
        }

        public DialogueChoice(string text, NarrativeFactKey key, FactMutationOp op, int operandValue)
        {
            this.text = text;
            this.mutation = new NarrativeFactMutation(key, op, operandValue);
        }
    }

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
    public partial class StorylineUI : MonoBehaviour, IModalWindow, IDialogueDismissible
    {
        public enum DialogueDisplayState
        {
            Hidden,
            NonModalBanner,
            ModalTyping,
            ModalWaitingContinue,
            ModalWaitingChoice
        }

        private static StorylineUI _instance;
        public static bool HasInstance
        {
            get
            {
                if (_instance != null) return true;
                _instance = FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
                return _instance != null;
            }
        }
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
                if (!Application.isPlaying) _isQuitting = false;
                else if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StorylineUI>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            set => _instance = value;
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

        [Header("Typewriter & Audio Feedback")]
        [SerializeField] private float typewriterCharactersPerSecond = 35f;
        [SerializeField] private AudioSource typewriterAudioSource;
        [SerializeField] private AudioClip typewriterAudioChit;

        [Header("Interactive Choice Elements")]
        [SerializeField] private GameObject choiceContainer;
        [SerializeField] private List<Button> choiceButtons = new List<Button>();
        [SerializeField] private List<TextMeshProUGUI> choiceButtonTexts = new List<TextMeshProUGUI>();

        private int _typewriterTotalCharacters = 0;
        private float _typewriterVisibleCount = 0f;
        private bool _isTypewriterActive = false;
        private bool _hasPendingChoices = false;
        private DialogueChoice[] _currentChoices;
        private Action<int> _onChoiceSelected;

        public DialogueDisplayState CurrentDisplayState
        {
            get
            {
                bool modalDialogueOpen = dialoguePanel != null && dialoguePanel.activeSelf;
                bool modalOverlayOpen = overlayPanel != null && overlayPanel.activeSelf;
                if (modalDialogueOpen || modalOverlayOpen)
                {
                    if (_isTypewriterActive) return DialogueDisplayState.ModalTyping;
                    if (_hasPendingChoices) return DialogueDisplayState.ModalWaitingChoice;
                    return DialogueDisplayState.ModalWaitingContinue;
                }
                if (bannerPanel != null && bannerPanel.activeSelf)
                {
                    return DialogueDisplayState.NonModalBanner;
                }
                return DialogueDisplayState.Hidden;
            }
        }

        [Header("Overlay Elements")]
        [SerializeField] private TextMeshProUGUI overlayTitleText;
        [SerializeField] private TextMeshProUGUI overlayBodyText;
        [SerializeField] private TextMeshProUGUI overlayPromptText;
        [SerializeField] private CanvasGroup overlayCanvasGroup;

        [Header("Dream Elements")]
        [SerializeField] private TextMeshProUGUI dreamBodyText;
        [SerializeField] private CanvasGroup dreamCanvasGroup;

        public TextMeshProUGUI BannerTitleText => bannerTitleText;
        public TextMeshProUGUI BannerBodyText => bannerBodyText;
        public TextMeshProUGUI OverlayTitleText => overlayTitleText;
        public TextMeshProUGUI OverlayBodyText => overlayBodyText;
        public TextMeshProUGUI OverlayPromptText => overlayPromptText;
        public TextMeshProUGUI DreamBodyText => dreamBodyText;
        public AudioSource TypewriterAudioSource => typewriterAudioSource;
        public AudioClip TypewriterAudioChit => typewriterAudioChit;

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

        /// <summary>
        /// Frame simulation entry point for automated Zero-GC profiling tests.
        /// Guarantees 0 B GC allocation during active typewriter progression.
        /// </summary>
        public void SimulateFrame(float deltaTime)
        {
            if (!_isTypewriterActive || dialogueBodyText == null) return;

            int prevCharIndex = (int)_typewriterVisibleCount;
            _typewriterVisibleCount += typewriterCharactersPerSecond * deltaTime;
            int newCharIndex = Mathf.Min((int)_typewriterVisibleCount, _typewriterTotalCharacters);

            if (newCharIndex > prevCharIndex)
            {
                dialogueBodyText.maxVisibleCharacters = newCharIndex;

                if (typewriterAudioSource != null && typewriterAudioChit != null && dialogueBodyText.text != null && newCharIndex - 1 < dialogueBodyText.text.Length)
                {
                    char c = dialogueBodyText.text[newCharIndex - 1];
                    if (!char.IsWhiteSpace(c))
                    {
                        typewriterAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                        typewriterAudioSource.PlayOneShot(typewriterAudioChit, 0.4f);
                    }
                }
            }

            if (newCharIndex >= _typewriterTotalCharacters)
            {
                _isTypewriterActive = false;
                dialogueBodyText.maxVisibleCharacters = _typewriterTotalCharacters;
                OnTypewriterCompleted();
            }
        }

        private void Update()
        {
            SimulateFrame(Time.unscaledDeltaTime);

            // Gating Invariant: If any cutscene is active, ensure dialoguePanel remains unconditionally hidden
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                bool cutsceneActive = false;
                if (StorylineManager.Instance != null && StorylineManager.Instance.IsCutsceneActive()) cutsceneActive = true;
                else if (Cutscenes.OpeningCutsceneController.HasInstance && Cutscenes.OpeningCutsceneController.Instance.IsCutscenePlaying) cutsceneActive = true;
                else if (Cutscenes.Night1CutsceneController.HasInstance && Cutscenes.Night1CutsceneController.Instance.IsCutscenePlaying) cutsceneActive = true;
                else if (ServiceLocator.TryResolve<ICutsceneService>(out var cs) && cs != null && cs.IsCutscenePlaying) cutsceneActive = true;

                if (cutsceneActive || FarmBeware.Core.Runtime.GameSessionState.IsPendingNewGameIntroCutscene)
                {
                    dialoguePanel.SetActive(false);
                    _isTypewriterActive = false;
                    return;
                }
            }

            // Allow Space, Enter, or Click to advance dialogue if open
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                bool advanceInput = false;
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        advanceInput = true;
                    }
                }
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    advanceInput = true;
                }

                if (advanceInput)
                {
                    if (_isTypewriterActive)
                    {
                        // Fast-forward: instantly reveal full text without substring allocations
                        _isTypewriterActive = false;
                        if (dialogueBodyText != null)
                        {
                            dialogueBodyText.maxVisibleCharacters = _typewriterTotalCharacters;
                        }
                        OnTypewriterCompleted();
                    }
                    else if (!_hasPendingChoices)
                    {
                        DismissDialogue();
                    }
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

            // Gating: If DayTransitionUI is actively playing, defer banner presentation until transition completes
            if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsPlayingTransition)
            {
                void OnTransitionDone()
                {
                    if (DayTransitionUI.Instance != null)
                        DayTransitionUI.Instance.OnTransitionCompleted -= OnTransitionDone;
                    ShowBanner(title, message, duration, onComplete);
                }
                DayTransitionUI.Instance.OnTransitionCompleted += OnTransitionDone;
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

            // Gating: If DayTransitionUI is actively playing, defer dialogue presentation until transition completes
            if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsPlayingTransition)
            {
                void OnTransitionDone()
                {
                    if (DayTransitionUI.Instance != null)
                        DayTransitionUI.Instance.OnTransitionCompleted -= OnTransitionDone;
                    ShowDialogue(speaker, message, onDismiss);
                }
                DayTransitionUI.Instance.OnTransitionCompleted += OnTransitionDone;
                return;
            }

            // Gating 2: If any cutscene is actively playing, modal dialogue must not appear over cinematic framing
            bool isCutscene = false;
            if (StorylineManager.Instance != null && StorylineManager.Instance.IsCutsceneActive())
            {
                isCutscene = true;
            }
            else if (ServiceLocator.TryResolve<ICutsceneService>(out var cutsceneService) && cutsceneService != null && cutsceneService.IsCutscenePlaying)
            {
                isCutscene = true;
            }
            else if (Cutscenes.OpeningCutsceneController.HasInstance && Cutscenes.OpeningCutsceneController.Instance.IsCutscenePlaying)
            {
                isCutscene = true;
            }
            else if (Cutscenes.Night1CutsceneController.HasInstance && Cutscenes.Night1CutsceneController.Instance.IsCutscenePlaying)
            {
                isCutscene = true;
            }

            if (isCutscene || FarmBeware.Core.Runtime.GameSessionState.IsPendingNewGameIntroCutscene)
            {
                if (dialoguePanel != null && dialoguePanel.activeSelf)
                {
                    dialoguePanel.SetActive(false);
                }
                _isTypewriterActive = false;
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
                dialogueSpeakerText.text = string.IsNullOrEmpty(speaker) ? "Ucup" : speaker;

            if (dialogueBodyText != null)
            {
                dialogueBodyText.text = message;
                dialogueBodyText.maxVisibleCharacters = 0;
                dialogueBodyText.rectTransform.anchorMax = new Vector2(1f, dialogueBodyText.rectTransform.anchorMax.y);
            }

            _typewriterTotalCharacters = !string.IsNullOrEmpty(message) ? message.Length : 0;
            _typewriterVisibleCount = 0f;
            _isTypewriterActive = _typewriterTotalCharacters > 0;

            EnsureChoiceUI();
            if (_hasPendingChoices)
            {
                if (choiceContainer != null) choiceContainer.SetActive(false);
                if (dialogueContinueButton != null) dialogueContinueButton.gameObject.SetActive(false);
            }
            else
            {
                if (choiceContainer != null) choiceContainer.SetActive(false);
                if (dialogueContinueButton != null) dialogueContinueButton.gameObject.SetActive(true);
            }

            if (!_isTypewriterActive)
            {
                if (dialogueBodyText != null) dialogueBodyText.maxVisibleCharacters = 99999;
                OnTypewriterCompleted();
            }

            // Notify modal stack
            var modalStack = ServiceLocator.Resolve<IModalStackService>();
            modalStack?.Push(this);
        }

        public void ShowDialogueWithChoices(string speaker, string message, DialogueChoice[] choices, Action<int> onChoiceSelected = null, Action onComplete = null)
        {
            _hasPendingChoices = choices != null && choices.Length > 0;
            _currentChoices = choices;
            _onChoiceSelected = onChoiceSelected;
            ShowDialogue(speaker, message, onComplete);
        }

        private void OnTypewriterCompleted()
        {
            if (_hasPendingChoices && _currentChoices != null && _currentChoices.Length > 0)
            {
                PresentChoices(_currentChoices);
            }
            else
            {
                if (dialogueContinueButton != null)
                {
                    dialogueContinueButton.gameObject.SetActive(true);
                }
            }
        }

        private void PresentChoices(DialogueChoice[] choices)
        {
            EnsureChoiceUI();
            if (dialogueBodyText != null)
            {
                dialogueBodyText.rectTransform.anchorMax = new Vector2(0.52f, dialogueBodyText.rectTransform.anchorMax.y);
            }
            if (choiceContainer != null)
            {
                choiceContainer.SetActive(true);
            }
            if (dialogueContinueButton != null)
            {
                dialogueContinueButton.gameObject.SetActive(false);
            }

            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (i < choices.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    if (i < choiceButtonTexts.Count && choiceButtonTexts[i] != null)
                    {
                        choiceButtonTexts[i].text = choices[i].text;
                    }
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void HandleChoiceClicked(int choiceIndex)
        {
            if (_currentChoices != null && choiceIndex >= 0 && choiceIndex < _currentChoices.Length)
            {
                var choice = _currentChoices[choiceIndex];
                var facts = StorylineManager.Instance != null ? StorylineManager.Instance.FactBoard : ServiceLocator.Resolve<INarrativeFactBoard>();
                facts?.ApplyMutation(choice.mutation);
                _onChoiceSelected?.Invoke(choiceIndex);
            }

            _hasPendingChoices = false;
            _currentChoices = null;
            _onChoiceSelected = null;
            if (choiceContainer != null) choiceContainer.SetActive(false);
            DismissDialogue();
        }

        private void EnsureChoiceUI()
        {
            if (choiceContainer != null && choiceButtons.Count > 0) return;
            if (dialoguePanel == null) return;

            Transform existing = dialoguePanel.transform.Find("ChoiceContainer");
            if (existing != null)
            {
                choiceContainer = existing.gameObject;
            }
            else
            {
                choiceContainer = new GameObject("ChoiceContainer", typeof(RectTransform));
                choiceContainer.transform.SetParent(dialoguePanel.transform, false);
                var rt = choiceContainer.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.55f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(10f, 12f);
                rt.offsetMax = new Vector2(-16f, -12f);
            }

            choiceButtons.Clear();
            choiceButtonTexts.Clear();

            for (int i = 0; i < 3; i++)
            {
                string btnName = $"ChoiceBtn_{i + 1}";
                Transform foundBtn = choiceContainer.transform.Find(btnName);
                Button btn;
                TextMeshProUGUI txt;

                if (foundBtn != null)
                {
                    btn = foundBtn.GetComponent<Button>();
                    txt = foundBtn.GetComponentInChildren<TextMeshProUGUI>();
                }
                else
                {
                    var btnGO = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGO.transform.SetParent(choiceContainer.transform, false);
                    var bRt = btnGO.GetComponent<RectTransform>();
                    bRt.anchorMin = new Vector2(0f, 1f - (i + 1) * 0.32f);
                    bRt.anchorMax = new Vector2(1f, 1f - i * 0.32f);
                    bRt.offsetMin = new Vector2(0f, 2f);
                    bRt.offsetMax = new Vector2(0f, -2f);

                    var img = btnGO.GetComponent<Image>();
                    img.color = new Color(0.12f, 0.28f, 0.42f, 0.95f);

                    btn = btnGO.GetComponent<Button>();

                    var tGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    tGO.transform.SetParent(btnGO.transform, false);
                    var tRt = tGO.GetComponent<RectTransform>();
                    tRt.anchorMin = Vector2.zero;
                    tRt.anchorMax = Vector2.one;
                    tRt.offsetMin = new Vector2(8f, 0f);
                    tRt.offsetMax = new Vector2(-8f, 0f);

                    txt = tGO.GetComponent<TextMeshProUGUI>();
                    txt.fontSize = 13;
                    txt.alignment = TextAlignmentOptions.MidlineLeft;
                    txt.color = Color.white;
                }

                int capturedIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => HandleChoiceClicked(capturedIndex));

                choiceButtons.Add(btn);
                choiceButtonTexts.Add(txt);
                btn.gameObject.SetActive(false);
            }
        }

        public void DismissDialogue()
        {
            if (dialoguePanel == null) return;

            _isTypewriterActive = false;
            _hasPendingChoices = false;
            _currentChoices = null;
            _onChoiceSelected = null;
            if (choiceContainer != null) choiceContainer.SetActive(false);
            if (dialogueBodyText != null)
            {
                dialogueBodyText.rectTransform.anchorMax = new Vector2(1f, dialogueBodyText.rectTransform.anchorMax.y);
            }

            if (dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(false);
                var modalStack = ServiceLocator.Resolve<IModalStackService>();
                modalStack?.PopSpecific(this);

                var cb = _onDialogueDismissed;
                _onDialogueDismissed = null;
                cb?.Invoke();
            }
        }

        public void ShowOverlay(string title, string message, float minDuration = 4f, Action onComplete = null)
        {
            EnsureUIHierarchy();
            if (overlayPanel == null || overlayCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            // Gating: If DayTransitionUI is actively playing, defer overlay presentation until transition completes
            if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsPlayingTransition)
            {
                void OnTransitionDone()
                {
                    if (DayTransitionUI.Instance != null)
                        DayTransitionUI.Instance.OnTransitionCompleted -= OnTransitionDone;
                    ShowOverlay(title, message, minDuration, onComplete);
                }
                DayTransitionUI.Instance.OnTransitionCompleted += OnTransitionDone;
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
    }
}
