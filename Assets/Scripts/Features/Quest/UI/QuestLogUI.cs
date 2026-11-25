using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using FarmBeware.Core.Runtime;
using FarmBeware.Logic.Quests;
using FeaturesTime;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FarmBeware.Features.Quest.UI
{
    /// <summary>
    /// Screen-space Quest Log modal displaying all quests received by the player,
    /// including onboarding side quests, storyline objectives, live progress bars,
    /// detailed step checklists, and Finished / Unfinished state filtering.
    /// Toggled with [Q] or by clicking the HUD Quest Card.
    /// </summary>
    [DisallowMultipleComponent]
    public class QuestLogUI : MonoBehaviour, IModalWindow
    {
        private static QuestLogUI _instance;
        public static bool HasInstance => _instance != null;
        private static bool _isQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isQuitting = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            if (_isQuitting || !Application.isPlaying) return;
            EnsureInstanceForCurrentScene();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_isQuitting || !Application.isPlaying) return;

            if (string.Equals(scene.name, SceneNames.MainMenuScene, System.StringComparison.Ordinal))
            {
                if (_instance != null && _instance.IsOpen)
                {
                    _instance.CloseQuestLog();
                }
                return;
            }

            EnsureInstanceForCurrentScene();
        }

        private static void EnsureInstanceForCurrentScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (string.Equals(active.name, SceneNames.MainMenuScene, System.StringComparison.Ordinal)) return;
            EnsureInstance();
        }

        /// <summary>
        /// Active singleton instance (searches inactive objects if needed).
        /// </summary>
        public static QuestLogUI Instance
        {
            get
            {
                if (!Application.isPlaying) _isQuitting = false;
                else if (_isQuitting) return null;

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<QuestLogUI>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        /// <summary>
        /// Resolves or creates the QuestLogUI host under the main UI Canvas.
        /// </summary>
        public static QuestLogUI EnsureInstance()
        {
            if (Instance != null) return Instance;

            var host = new GameObject("QuestLogUI");
            Canvas mainCanvas = CanvasHelper.ResolveMainUICanvas();
            if (mainCanvas != null)
            {
                host.transform.SetParent(mainCanvas.transform, false);
            }
            return host.AddComponent<QuestLogUI>();
        }

        [Header("UI References (Auto-Built if Null)")]
        [SerializeField] private GameObject questLogRoot;
        [SerializeField] private Transform questsContent;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private UnityEngine.UI.Button closeXButton;
        [SerializeField] private UnityEngine.UI.Button closeFooterButton;
        [SerializeField] private UnityEngine.UI.Button backdropButton;
        [SerializeField] private UnityEngine.UI.Button filterAllButton;
        [SerializeField] private UnityEngine.UI.Button filterUnfinishedButton;
        [SerializeField] private UnityEngine.UI.Button filterFinishedButton;
        [SerializeField] private TextMeshProUGUI filterAllLabel;
        [SerializeField] private TextMeshProUGUI filterUnfinishedLabel;
        [SerializeField] private TextMeshProUGUI filterFinishedLabel;
        [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;

        private readonly List<QuestEntrySnapshot> _allEntries = new List<QuestEntrySnapshot>(16);
        private readonly List<QuestEntrySnapshot> _filteredEntries = new List<QuestEntrySnapshot>(16);

        private QuestStateFilter _currentFilter = QuestStateFilter.All;
        private QuestLogSummary _summary;
        private bool _isSubscribed;
        private bool _isOpen;
        private bool _ownsProceduralRoot;
        private INarrativeObjectiveService _subscribedObjectiveService;

        public bool IsOpen => _isOpen;
        public bool CanDismissWithEscape => true;
        public QuestStateFilter CurrentFilter => _currentFilter;
        public QuestLogSummary Summary => _summary;
        public IReadOnlyList<QuestEntrySnapshot> AllEntries => _allEntries;
        public IReadOnlyList<QuestEntrySnapshot> DisplayedEntries => _filteredEntries;

        void IModalWindow.OpenModal() => OpenQuestLog();
        void IModalWindow.CloseModal() => CloseQuestLog();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (_isOpen)
            {
                _isOpen = false;
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    ModalStackManager.Instance.PopSpecific(this);
                }
                ServiceLocator.Resolve<IPlayerContext>()?.RemoveInputLock("QuestLogUI");
            }

            if (_ownsProceduralRoot && questLogRoot != null)
            {
                if (Application.isPlaying) Destroy(questLogRoot);
                else DestroyImmediate(questLogRoot);
                questLogRoot = null;
                _ownsProceduralRoot = false;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            SimulateFrame(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Zero-GC frame tick checking the [Q] hotkey and Escape dismissal.
        /// Performs zero heap allocations per frame when idle.
        /// </summary>
        public void SimulateFrame(float deltaTime)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return;

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                if (_isOpen)
                {
                    CloseQuestLog();
                }
                else if (CanOpenWithHotkey())
                {
                    OpenQuestLog();
                }
            }
            else if (_isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseQuestLog();
            }
#endif
        }

        /// <summary>
        /// Toggles visibility of the Quest Log modal.
        /// </summary>
        public void ToggleQuestLog()
        {
            if (_isOpen) CloseQuestLog();
            else OpenQuestLog();
        }

        /// <summary>
        /// Opens the Quest Log modal, locks player movement input, registers on the modal stack,
        /// and refreshes all received quest entries and progress bars.
        /// </summary>
        public void OpenQuestLog()
        {
            EnsureUIHierarchy();
            if (questLogRoot == null) return;

            SubscribeEvents();

            var playerCtx = ServiceLocator.Resolve<IPlayerContext>();
            playerCtx?.AddInputLock("QuestLogUI");

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            _isOpen = true;
            questLogRoot.SetActive(true);
            questLogRoot.transform.SetAsLastSibling();

            if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
            {
                ModalStackManager.Instance.Push(this);
            }

            RefreshQuestList();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// Closes the Quest Log modal and releases its input lock cleanly.
        /// </summary>
        public void CloseQuestLog()
        {
            bool wasOpen = _isOpen || (questLogRoot != null && questLogRoot.activeSelf);
            _isOpen = false;

            if (questLogRoot != null)
            {
                questLogRoot.SetActive(false);
            }

            UnsubscribeEvents();

            if (wasOpen)
            {
                UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    ModalStackManager.Instance.PopSpecific(this);
                }
                ServiceLocator.Resolve<IPlayerContext>()?.RemoveInputLock("QuestLogUI");

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        /// <summary>
        /// Switches the active state filter (All, Unfinished, Finished) and redraws the quest list.
        /// </summary>
        public void SetFilter(QuestStateFilter filter)
        {
            _currentFilter = filter;
            RefreshQuestList();
        }

        /// <summary>
        /// Rebuilds the snapshot of all quests received by the player and updates the rendered UI cards.
        /// </summary>
        public void RefreshQuestList()
        {
            EnsureUIHierarchy();

            QuestProgressModel model = QuestManager.HasInstance && QuestManager.Instance != null
                ? QuestManager.Instance.Model
                : null;
            INarrativeObjectiveService objService = _subscribedObjectiveService ?? ServiceLocator.Resolve<INarrativeObjectiveService>();
            if (objService == null && QuestManager.HasInstance)
            {
                objService = QuestManager.Instance;
            }
            IReadOnlyList<NarrativeObjective> objectives = objService != null ? objService.ActiveObjectives : null;
            int currentDay = ResolveCurrentGameDay();

            QuestLogAggregator.BuildEntries(model, objectives, currentDay, _allEntries);
            _summary = QuestLogAggregator.ComputeSummary(_allEntries);
            QuestLogAggregator.FilterEntries(_allEntries, _currentFilter, _filteredEntries);

            UpdateSummaryAndTabs();
            RebuildQuestCards();
        }

        /// <summary>
        /// Builds the procedural uGUI Panel_QuestLog hierarchy under UI_Canvas if not already constructed.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            if (questLogRoot != null)
            {
                BindButtons();
                return;
            }

            Canvas canvas = CanvasHelper.ResolveMainUICanvas();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("Panel_QuestLog");
            if (existing != null)
            {
                questLogRoot = existing.gameObject;
                _ownsProceduralRoot = false;
                ResolveExistingReferences();
                BindButtons();
                return;
            }

            questLogRoot = new GameObject("Panel_QuestLog", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            _ownsProceduralRoot = true;
            questLogRoot.transform.SetParent(canvas.transform, false);

            var rootRt = questLogRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var overrideCanvas = questLogRoot.GetComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 700;

            // Backdrop dimmer
            var backdropGO = new GameObject("BackdropDimmer", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            backdropGO.transform.SetParent(questLogRoot.transform, false);
            var bdRt = backdropGO.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            var bdImg = backdropGO.GetComponent<UnityEngine.UI.Image>();
            bdImg.color = new Color(0.02f, 0.04f, 0.07f, 0.78f);
            bdImg.raycastTarget = true;
            backdropButton = backdropGO.GetComponent<UnityEngine.UI.Button>();
            backdropButton.transition = UnityEngine.UI.Selectable.Transition.None;

            // Modal card container (16:9 wooden two-column board)
            var cardGO = new GameObject("QuestLogModalCard", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            cardGO.transform.SetParent(questLogRoot.transform, false);
            var cardRt = cardGO.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(1180f, 659f);
            var cardImg = cardGO.GetComponent<UnityEngine.UI.Image>();
            WoodenBoardStyle.ApplyBoardBackground(cardImg);
            cardImg.raycastTarget = true;

            // Plaque Title
            var plaqueTmp = WoodenBoardStyle.EnsurePlaqueTitle<TextMeshProUGUI>(cardGO.transform);
            if (plaqueTmp != null)
            {
                plaqueTmp.text = "QUEST BOARD [Q]";
                plaqueTmp.fontSize = 22f;
                plaqueTmp.fontStyle = FontStyles.Bold;
                plaqueTmp.alignment = TextAlignmentOptions.Center;
            }

            // Header bar (transparent full-rect overlay preserving HeaderBar/Btn_XClose path)
            GameObject headerBar = CreateAnchoredBar(
                cardGO.transform,
                "HeaderBar",
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(0f, 0f, 0f, 0f));

            closeXButton = CreateButton(
                headerBar.transform,
                "Btn_XClose",
                "X",
                36f,
                36f,
                WoodenBoardStyle.BtnDanger,
                Color.white,
                17f);
            var xRt = closeXButton.GetComponent<RectTransform>();
            xRt.anchorMin = WoodenBoardStyle.CloseButtonAnchor;
            xRt.anchorMax = WoodenBoardStyle.CloseButtonAnchor;
            xRt.pivot = new Vector2(0.5f, 0.5f);
            xRt.anchoredPosition = Vector2.zero;

            // Left Column: Summary & Filter Sidebar (preserving FilterSummaryBar/SummaryText & FilterTabs paths)
            GameObject filterBar = CreateAnchoredBar(
                cardGO.transform,
                "FilterSummaryBar",
                new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.225f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x, WoodenBoardStyle.LeftPaneMax.y),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-12f, -12f),
                WoodenBoardStyle.DarkOakOverlay);

            summaryText = CreateLabel(
                filterBar.transform,
                "SummaryText",
                "Accepted: 0\nUnfinished: 0\nFinished: 0",
                14f,
                FontStyles.Bold,
                WoodenBoardStyle.TextCream,
                TextAlignmentOptions.Center,
                new Vector2(0.06f, 0.66f),
                new Vector2(0.94f, 0.96f),
                Vector2.zero);

            GameObject tabsRow = CreateAnchoredBar(
                filterBar.transform,
                "FilterTabs",
                new Vector2(0.06f, 0.04f),
                new Vector2(0.94f, 0.62f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(0f, 0f, 0f, 0f));
            var tabsVlg = tabsRow.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            tabsVlg.padding = new RectOffset(6, 6, 8, 8);
            tabsVlg.spacing = 10f;
            tabsVlg.childAlignment = TextAnchor.UpperCenter;
            tabsVlg.childControlWidth = true;
            tabsVlg.childControlHeight = false;
            tabsVlg.childForceExpandWidth = true;
            tabsVlg.childForceExpandHeight = false;

            filterAllButton = CreateFilterTabButton(tabsRow.transform, "Btn_Filter_All", "All (0)", 220f, out filterAllLabel);
            filterUnfinishedButton = CreateFilterTabButton(tabsRow.transform, "Btn_Filter_Unfinished", "Unfinished (0)", 220f, out filterUnfinishedLabel);
            filterFinishedButton = CreateFilterTabButton(tabsRow.transform, "Btn_Filter_Finished", "Finished (0)", 220f, out filterFinishedLabel);

            // Right Column: ScrollView (QuestLogModalCard/QuestScrollView/Viewport/QuestsContent)
            var scrollGO = new GameObject("QuestScrollView", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.ScrollRect));
            scrollGO.transform.SetParent(cardGO.transform, false);
            var sRt = scrollGO.GetComponent<RectTransform>();
            WoodenBoardStyle.SetAnchorRegion(sRt, WoodenBoardStyle.RightPaneMin, WoodenBoardStyle.RightPaneMax, 10f, 8f, 10f, 8f);
            var sBg = scrollGO.GetComponent<UnityEngine.UI.Image>();
            sBg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            sBg.type = UnityEngine.UI.Image.Type.Sliced;
            sBg.color = WoodenBoardStyle.DarkOakOverlay;
            sBg.raycastTarget = true;

            scrollRect = scrollGO.GetComponent<UnityEngine.UI.ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var vpRt = viewportGO.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            scrollRect.viewport = vpRt;

            var contentGO = new GameObject("QuestsContent", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var contentVlg = contentGO.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(12, 12, 12, 12);
            contentVlg.spacing = 10f;
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            var csf = contentGO.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            questsContent = contentGO.transform;

            // Footer bar (bottom of Left Column, preserving FooterBar/Btn_CloseFooter path)
            GameObject footerBar = CreateAnchoredBar(
                cardGO.transform,
                "FooterBar",
                new Vector2(WoodenBoardStyle.LeftPaneMin.x, WoodenBoardStyle.LeftPaneMin.y),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.215f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-12f, -4f),
                new Color(0f, 0f, 0f, 0f));

            CreateLabel(
                footerBar.transform,
                "FooterHint",
                "Active quests appear first.\nAdvance by farming & cooking.",
                11.5f,
                FontStyles.Normal,
                WoodenBoardStyle.TextMutedWarm,
                TextAlignmentOptions.Top,
                new Vector2(0.04f, 0.45f),
                new Vector2(0.96f, 0.96f),
                Vector2.zero);

            closeFooterButton = CreateButton(
                footerBar.transform,
                "Btn_CloseFooter",
                "Close [Q]",
                200f,
                36f,
                WoodenBoardStyle.BtnSecondary,
                WoodenBoardStyle.TextCream,
                14f);
            var fCloseRt = closeFooterButton.GetComponent<RectTransform>();
            fCloseRt.anchorMin = new Vector2(0.06f, 0.04f);
            fCloseRt.anchorMax = new Vector2(0.94f, 0.42f);
            fCloseRt.offsetMin = Vector2.zero;
            fCloseRt.offsetMax = Vector2.zero;

            BindButtons();
            questLogRoot.SetActive(false);
        }

        private bool CanOpenWithHotkey()
        {
            Scene active = SceneManager.GetActiveScene();
            if (string.Equals(active.name, SceneNames.MainMenuScene, System.StringComparison.Ordinal)) return false;
            if (UIModalHelper.IsSaveUIOpen) return false;
            if (Time.frameCount == UIModalHelper.LastFrameUIPanelClosed) return false;

            if (ServiceLocator.TryResolve<ICutsceneService>(out var cutscene) && cutscene != null && cutscene.IsCutscenePlaying)
            {
                return false;
            }

            if (ModalStackManager.HasInstance && ModalStackManager.Instance != null && ModalStackManager.Instance.HasActiveModal)
            {
                return false;
            }

            var player = ServiceLocator.Resolve<IPlayerContext>();
            if (player != null && player.IsInputLocked)
            {
                return false;
            }

            return true;
        }

        private int ResolveCurrentGameDay()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                return timeService.CurrentDay;
            }
            if (DayNightTimeManager.Instance != null)
            {
                return DayNightTimeManager.Instance.CurrentDay;
            }
            return 1;
        }

        private void SubscribeEvents()
        {
            if (!_isSubscribed && QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestUpdated += HandleQuestDataChanged;
                _isSubscribed = true;
            }

            if (_subscribedObjectiveService == null)
            {
                INarrativeObjectiveService objSvc = ServiceLocator.Resolve<INarrativeObjectiveService>();
                if (objSvc == null && QuestManager.HasInstance)
                {
                    objSvc = QuestManager.Instance;
                }
                if (objSvc != null)
                {
                    _subscribedObjectiveService = objSvc;
                    _subscribedObjectiveService.OnObjectiveStateChanged += HandleObjectiveDataChanged;
                }
            }
        }

        private void UnsubscribeEvents()
        {
            if (_isSubscribed)
            {
                if (QuestManager.HasInstance && QuestManager.Instance != null)
                {
                    QuestManager.Instance.OnQuestUpdated -= HandleQuestDataChanged;
                }
                _isSubscribed = false;
            }

            if (_subscribedObjectiveService != null)
            {
                _subscribedObjectiveService.OnObjectiveStateChanged -= HandleObjectiveDataChanged;
                _subscribedObjectiveService = null;
            }
        }

        private void HandleQuestDataChanged(QuestProgressModel _)
        {
            if (_isOpen) RefreshQuestList();
        }

        private void HandleObjectiveDataChanged(string _, bool __)
        {
            if (_isOpen) RefreshQuestList();
        }

        private void UpdateSummaryAndTabs()
        {
            if (summaryText != null)
            {
                summaryText.text =
                    $"Accepted: <b>{_summary.TotalCount}</b>\n" +
                    $"<color=#FFB74D>Unfinished: {_summary.UnfinishedCount}</color>\n" +
                    $"<color=#81C784>Finished: {_summary.FinishedCount}</color>";
            }

            UpdateFilterTabStyle(filterAllButton, filterAllLabel, _currentFilter == QuestStateFilter.All, $"All ({_summary.TotalCount})");
            UpdateFilterTabStyle(filterUnfinishedButton, filterUnfinishedLabel, _currentFilter == QuestStateFilter.Unfinished, $"Unfinished ({_summary.UnfinishedCount})");
            UpdateFilterTabStyle(filterFinishedButton, filterFinishedLabel, _currentFilter == QuestStateFilter.Finished, $"Finished ({_summary.FinishedCount})");
        }

        private static void UpdateFilterTabStyle(UnityEngine.UI.Button btn, TextMeshProUGUI label, bool active, string text)
        {
            if (label != null)
            {
                label.text = text;
                label.color = active ? new Color(0.14f, 0.08f, 0.03f, 1f) : WoodenBoardStyle.TextCream;
            }
            if (btn != null && btn.TryGetComponent<UnityEngine.UI.Image>(out var img))
            {
                img.color = active ? WoodenBoardStyle.TextGoldAccent : WoodenBoardStyle.TabInactive;
            }
        }

        private void RebuildQuestCards()
        {
            if (questsContent == null) return;
            ClearContainer(questsContent);

            if (_filteredEntries.Count == 0)
            {
                BuildEmptyStateCard(GetEmptyStateMessage());
                return;
            }

            for (int i = 0; i < _filteredEntries.Count; i++)
            {
                BuildQuestCard(_filteredEntries[i]);
            }
        }

        private string GetEmptyStateMessage()
        {
            if (_summary.TotalCount == 0)
            {
                return "No quests received yet.\nComplete Day 0 arrival to unlock your first quests on Day 1.";
            }

            switch (_currentFilter)
            {
                case QuestStateFilter.Unfinished:
                    return "No unfinished quests remaining - all received quests are finished!";
                case QuestStateFilter.Finished:
                    return "No finished quests yet.\nComplete objectives on the farm to record finished quests here.";
                default:
                    return "No quests match the current filter.";
            }
        }

        private void BuildEmptyStateCard(string message)
        {
            var emptyGO = new GameObject("EmptyState", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
            emptyGO.transform.SetParent(questsContent, false);

            var le = emptyGO.GetComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight = 140f;
            le.preferredHeight = 140f;
            le.flexibleWidth = 1f;

            var tmp = emptyGO.GetComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 15.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.66f, 0.74f, 0.84f, 0.9f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
        }

        private void BuildQuestCard(QuestEntrySnapshot entry)
        {
            if (entry == null) return;

            bool isFinished = entry.IsFinished;
            Color cardBgColor = isFinished
                ? new Color(0.14f, 0.22f, 0.14f, 0.78f)
                : WoodenBoardStyle.DarkOakSubtle;
            Color stateAccentColor = isFinished
                ? new Color(0.32f, 0.90f, 0.54f, 1f)
                : new Color(1.00f, 0.62f, 0.26f, 1f);
            Color categoryColor = GetCategoryColor(entry.Category);

            // Compute clean height dynamically based on step lines + description
            int stepLineCount = CountLines(entry.StepsDetail);
            float stepsHeight = Mathf.Max(24f, stepLineCount * 20f);
            float totalCardHeight = 28f + 22f + stepsHeight + 24f + 38f; // padding + top row + desc + steps + progress row

            var cardGO = new GameObject($"QuestRow_{entry.QuestId}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
            cardGO.transform.SetParent(questsContent, false);

            var cardImg = cardGO.GetComponent<UnityEngine.UI.Image>();
            cardImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            cardImg.type = UnityEngine.UI.Image.Type.Sliced;
            cardImg.color = cardBgColor;
            cardImg.raycastTarget = false;

            var cardLe = cardGO.GetComponent<UnityEngine.UI.LayoutElement>();
            cardLe.minHeight = totalCardHeight;
            cardLe.preferredHeight = totalCardHeight;
            cardLe.flexibleWidth = 1f;

            // Left accent stripe
            var stripeGO = new GameObject("StateStripe", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            stripeGO.transform.SetParent(cardGO.transform, false);
            var stripeRt = stripeGO.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 0f);
            stripeRt.anchorMax = new Vector2(0f, 1f);
            stripeRt.pivot = new Vector2(0f, 0.5f);
            stripeRt.offsetMin = new Vector2(0f, 6f);
            stripeRt.offsetMax = new Vector2(6f, -6f);
            var stripeImg = stripeGO.GetComponent<UnityEngine.UI.Image>();
            stripeImg.color = stateAccentColor;
            stripeImg.raycastTarget = false;

            // Inner content container padded from accent stripe
            var innerGO = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
            innerGO.transform.SetParent(cardGO.transform, false);
            var innerRt = innerGO.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(18f, 10f);
            innerRt.offsetMax = new Vector2(-16f, -10f);

            var innerVlg = innerGO.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            innerVlg.spacing = 5f;
            innerVlg.childAlignment = TextAnchor.UpperLeft;
            innerVlg.childControlWidth = true;
            innerVlg.childControlHeight = true;
            innerVlg.childForceExpandWidth = true;
            innerVlg.childForceExpandHeight = false;

            // 1. Header Row (Category + Title + State Pill)
            var topRowGO = new GameObject("TopRow", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup), typeof(UnityEngine.UI.LayoutElement));
            topRowGO.transform.SetParent(innerGO.transform, false);
            var topLe = topRowGO.GetComponent<UnityEngine.UI.LayoutElement>();
            topLe.minHeight = 26f;
            topLe.preferredHeight = 26f;

            var topHlg = topRowGO.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            topHlg.spacing = 8f;
            topHlg.childAlignment = TextAnchor.MiddleLeft;
            topHlg.childControlWidth = false;
            topHlg.childControlHeight = true;
            topHlg.childForceExpandWidth = false;
            topHlg.childForceExpandHeight = false;

            string catHex = ColorUtility.ToHtmlStringRGB(categoryColor);
            var titleGO = new GameObject("QuestTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
            titleGO.transform.SetParent(topRowGO.transform, false);
            var titleLe = titleGO.GetComponent<UnityEngine.UI.LayoutElement>();
            titleLe.flexibleWidth = 1f;
            titleLe.minWidth = 380f;
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = $"<color=#{catHex}>[{QuestLogAggregator.GetCategoryLabel(entry.Category)}]</color>  <b>{entry.Title}</b>";
            titleTmp.fontSize = 16f;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.raycastTarget = false;

            // Finish / Unfinish Pill
            var stateBadgeGO = new GameObject("StateBadge", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
            stateBadgeGO.transform.SetParent(topRowGO.transform, false);
            var stateLe = stateBadgeGO.GetComponent<UnityEngine.UI.LayoutElement>();
            stateLe.minWidth = 114f;
            stateLe.preferredWidth = 114f;
            stateLe.minHeight = 24f;
            stateLe.preferredHeight = 24f;
            var stateImg = stateBadgeGO.GetComponent<UnityEngine.UI.Image>();
            stateImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            stateImg.type = UnityEngine.UI.Image.Type.Sliced;
            stateImg.color = isFinished ? new Color(0.14f, 0.45f, 0.25f, 1f) : new Color(0.46f, 0.26f, 0.12f, 1f);
            stateImg.raycastTarget = false;

            CreateLabel(
                stateBadgeGO.transform,
                "StateText",
                QuestLogAggregator.GetStateLabel(entry.State),
                12f,
                FontStyles.Bold,
                stateAccentColor,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                Vector2.zero);

            // 2. Description line
            var descGO = new GameObject("QuestDescription", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
            descGO.transform.SetParent(innerGO.transform, false);
            var descLe = descGO.GetComponent<UnityEngine.UI.LayoutElement>();
            descLe.minHeight = 20f;
            descLe.preferredHeight = 20f;
            var descTmp = descGO.GetComponent<TextMeshProUGUI>();
            descTmp.text = entry.Description;
            descTmp.fontSize = 12.5f;
            descTmp.color = new Color(0.75f, 0.82f, 0.90f, 1f);
            descTmp.alignment = TextAlignmentOptions.MidlineLeft;
            descTmp.raycastTarget = false;

            // 3. Detailed Step Breakdown
            if (!string.IsNullOrEmpty(entry.StepsDetail))
            {
                var stepsGO = new GameObject("QuestSteps", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
                stepsGO.transform.SetParent(innerGO.transform, false);
                var stepsLe = stepsGO.GetComponent<UnityEngine.UI.LayoutElement>();
                stepsLe.minHeight = stepsHeight;
                stepsLe.preferredHeight = stepsHeight;
                var stepsTmp = stepsGO.GetComponent<TextMeshProUGUI>();
                stepsTmp.text = entry.StepsDetail;
                stepsTmp.fontSize = 13f;
                stepsTmp.color = new Color(0.92f, 0.95f, 0.98f, 1f);
                stepsTmp.richText = true;
                stepsTmp.textWrappingMode = TextWrappingModes.Normal;
                stepsTmp.alignment = TextAlignmentOptions.TopLeft;
                stepsTmp.raycastTarget = false;
            }

            // 4. Progress Bar + Counter Row
            var progRowGO = new GameObject("ProgressRow", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup), typeof(UnityEngine.UI.LayoutElement));
            progRowGO.transform.SetParent(innerGO.transform, false);
            var progLe = progRowGO.GetComponent<UnityEngine.UI.LayoutElement>();
            progLe.minHeight = 20f;
            progLe.preferredHeight = 20f;

            var progHlg = progRowGO.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            progHlg.spacing = 12f;
            progHlg.childAlignment = TextAnchor.MiddleLeft;
            progHlg.childControlWidth = false;
            progHlg.childControlHeight = true;
            progHlg.childForceExpandWidth = false;
            progHlg.childForceExpandHeight = false;

            var trackGO = new GameObject("ProgressTrack", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
            trackGO.transform.SetParent(progRowGO.transform, false);
            var trackLe = trackGO.GetComponent<UnityEngine.UI.LayoutElement>();
            trackLe.flexibleWidth = 1f;
            trackLe.minWidth = 300f;
            trackLe.minHeight = 12f;
            trackLe.preferredHeight = 12f;
            var trackImg = trackGO.GetComponent<UnityEngine.UI.Image>();
            trackImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            trackImg.type = UnityEngine.UI.Image.Type.Sliced;
            trackImg.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);
            trackImg.raycastTarget = false;

            var fillGO = new GameObject("ProgressFill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            fillGO.transform.SetParent(trackGO.transform, false);
            var fillRt = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(entry.ProgressRatio, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGO.GetComponent<UnityEngine.UI.Image>();
            fillImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            fillImg.type = UnityEngine.UI.Image.Type.Sliced;
            fillImg.color = isFinished
                ? new Color(0.28f, 0.85f, 0.50f, 1f)
                : new Color(0.26f, 0.68f, 0.96f, 1f);
            fillImg.raycastTarget = false;

            var countGO = new GameObject("ProgressCount", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
            countGO.transform.SetParent(progRowGO.transform, false);
            var countLe = countGO.GetComponent<UnityEngine.UI.LayoutElement>();
            countLe.minWidth = 112f;
            countLe.preferredWidth = 112f;
            var countTmp = countGO.GetComponent<TextMeshProUGUI>();
            countTmp.text = $"{entry.CurrentProgress}/{entry.TargetProgress} ({entry.ProgressPercent}%)";
            countTmp.fontSize = 12.5f;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.color = stateAccentColor;
            countTmp.alignment = TextAlignmentOptions.MidlineRight;
            countTmp.raycastTarget = false;
        }

        private static Color GetCategoryColor(NarrativeObjectiveCategory category)
        {
            switch (category)
            {
                case NarrativeObjectiveCategory.SideQuest:
                    return new Color(0.32f, 0.82f, 1.0f, 1f);
                case NarrativeObjectiveCategory.SurvivalPrep:
                    return new Color(0.45f, 0.92f, 0.58f, 1f);
                case NarrativeObjectiveCategory.MainStory:
                default:
                    return new Color(1.0f, 0.78f, 0.30f, 1f);
            }
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') count++;
            }
            return count;
        }

        private static void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                child.SetParent(null);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        private void ResolveExistingReferences()
        {
            if (questLogRoot == null) return;

            Transform card = questLogRoot.transform.Find("QuestLogModalCard");
            if (card == null) return;

            summaryText ??= card.Find("FilterSummaryBar/SummaryText")?.GetComponent<TextMeshProUGUI>();
            closeXButton ??= card.Find("HeaderBar/Btn_XClose")?.GetComponent<UnityEngine.UI.Button>();
            closeFooterButton ??= card.Find("FooterBar/Btn_CloseFooter")?.GetComponent<UnityEngine.UI.Button>();
            backdropButton ??= questLogRoot.transform.Find("BackdropDimmer")?.GetComponent<UnityEngine.UI.Button>();
            filterAllButton ??= card.Find("FilterSummaryBar/FilterTabs/Btn_Filter_All")?.GetComponent<UnityEngine.UI.Button>();
            filterUnfinishedButton ??= card.Find("FilterSummaryBar/FilterTabs/Btn_Filter_Unfinished")?.GetComponent<UnityEngine.UI.Button>();
            filterFinishedButton ??= card.Find("FilterSummaryBar/FilterTabs/Btn_Filter_Finished")?.GetComponent<UnityEngine.UI.Button>();
            filterAllLabel ??= filterAllButton?.GetComponentInChildren<TextMeshProUGUI>(true);
            filterUnfinishedLabel ??= filterUnfinishedButton?.GetComponentInChildren<TextMeshProUGUI>(true);
            filterFinishedLabel ??= filterFinishedButton?.GetComponentInChildren<TextMeshProUGUI>(true);
            scrollRect ??= card.Find("QuestScrollView")?.GetComponent<UnityEngine.UI.ScrollRect>();
            questsContent ??= card.Find("QuestScrollView/Viewport/QuestsContent");
        }

        private void BindButtons()
        {
            Rebind(closeXButton, CloseQuestLog);
            Rebind(closeFooterButton, CloseQuestLog);
            Rebind(backdropButton, CloseQuestLog);
            Rebind(filterAllButton, () => SetFilter(QuestStateFilter.All));
            Rebind(filterUnfinishedButton, () => SetFilter(QuestStateFilter.Unfinished));
            Rebind(filterFinishedButton, () => SetFilter(QuestStateFilter.Finished));
        }

        private static void Rebind(UnityEngine.UI.Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private static GameObject CreateAnchoredBar(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            if (bgColor.a > 0f)
            {
                var img = go.AddComponent<UnityEngine.UI.Image>();
                img.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.color = bgColor;
                img.raycastTarget = false;
            }

            return go;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPos = default)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static UnityEngine.UI.Button CreateButton(
            Transform parent,
            string name,
            string label,
            float width,
            float height,
            Color bgColor,
            Color textColor,
            float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = bgColor;
            img.raycastTarget = true;

            CreateLabel(go.transform, "Text", label, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero);
            return go.GetComponent<UnityEngine.UI.Button>();
        }

        private static UnityEngine.UI.Button CreateFilterTabButton(
            Transform parent,
            string name,
            string label,
            float width,
            out TextMeshProUGUI labelOut)
        {
            var btn = CreateButton(parent, name, label, width, 32f, new Color(0.18f, 0.23f, 0.32f, 0.95f), Color.white, 12.5f);
            var le = btn.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.minHeight = 32f;
            le.preferredHeight = 32f;
            labelOut = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            return btn;
        }
    }
}
