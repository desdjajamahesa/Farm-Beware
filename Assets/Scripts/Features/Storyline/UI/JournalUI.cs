using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FeaturesStoryline.Data;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline.UI
{
    /// <summary>
    /// Collectible Lore Journal Browser UI.
    /// Allows the player to review unlocked lore entries, grandfather notes, and historical documents.
    /// Toggled via 'J' key or pause menu.
    /// </summary>
    [DisallowMultipleComponent]
    public class JournalUI : MonoBehaviour, IModalWindow
    {
        private static JournalUI _instance;
        public static bool HasInstance => _instance != null;
        private static bool _isQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isQuitting = false;
        }

        public static JournalUI Instance
        {
            get
            {
                if (!Application.isPlaying) _isQuitting = false;
                else if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        void IModalWindow.OpenModal() => OpenJournal();
        void IModalWindow.CloseModal() => CloseJournal();

        [Header("Root Panel")]
        [SerializeField] private GameObject journalRoot;
        [SerializeField] private Canvas targetCanvas;

        [Header("Entry List (Left Pane)")]
        [SerializeField] private Transform entryListContainer;
        [SerializeField] private GameObject entryListItemPrefab;

        [Header("Page View (Right Pane)")]
        [SerializeField] private TextMeshProUGUI pageTitleText;
        [SerializeField] private TextMeshProUGUI pageAuthorText;
        [SerializeField] private TextMeshProUGUI pageBodyText;
        [SerializeField] private TextMeshProUGUI pageNumberText;
        [SerializeField] private Image pageIllustrationImage;

        [Header("Navigation Buttons")]
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button closeButton;

        private readonly List<JournalEntryData> _unlockedEntries = new List<JournalEntryData>();
        private readonly List<Image> _entryButtonBackgrounds = new List<Image>();
        private int _currentIndex = 0;

        public bool IsOpen => journalRoot != null && journalRoot.activeSelf;

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
            if (UnityEngine.InputSystem.Keyboard.current == null) return;

            if (UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame)
            {
                if (!IsOpen)
                {
                    var player = ServiceLocator.Resolve<IPlayerContext>();
                    if (player != null && player.IsInputLocked) return;

                    var modalStack = ServiceLocator.Resolve<IModalStackService>();
                    if (modalStack != null && modalStack.HasActiveModal) return;

                    OpenJournal();
                }
                else
                {
                    CloseJournal();
                }
            }
            else if (IsOpen && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseJournal();
            }
        }

        public void ToggleJournal()
        {
            if (IsOpen)
                CloseJournal();
            else
                OpenJournal();
        }

        public void OpenJournal()
        {
            EnsureUIHierarchy();
            RefreshUnlockedEntries();

            if (journalRoot != null)
            {
                journalRoot.SetActive(true);
                DisplayCurrentEntry();

                var modal = ServiceLocator.Resolve<IModalStackService>();
                modal?.Push(this);
            }
        }

        public void CloseJournal()
        {
            if (journalRoot == null || !journalRoot.activeSelf) return;

            journalRoot.SetActive(false);
            var modal = ServiceLocator.Resolve<IModalStackService>();
            modal?.PopSpecific(this);
        }

        public void SetEntries(IEnumerable<JournalEntryData> entries, IEnumerable<string> unlockedIds)
        {
            _unlockedEntries.Clear();
            var set = new HashSet<string>(unlockedIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e != null && set.Contains(e.entryId))
                    {
                        _unlockedEntries.Add(e);
                    }
                }
            }

            _unlockedEntries.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            if (_currentIndex >= _unlockedEntries.Count)
                _currentIndex = Math.Max(0, _unlockedEntries.Count - 1);

            if (IsOpen) DisplayCurrentEntry();
        }

        private void RefreshUnlockedEntries()
        {
            var service = ServiceLocator.Resolve<IStorylineService>();
            var manager = StorylineManager.Instance;

            if (manager != null)
            {
                SetEntries(manager.AllJournalEntries, manager.UnlockedJournalIds);
            }
            RebuildEntrySidebarList();
        }

        private void RebuildEntrySidebarList()
        {
            if (entryListContainer == null) return;

            for (int i = entryListContainer.childCount - 1; i >= 0; i--)
            {
                var child = entryListContainer.GetChild(i);
                child.SetParent(null);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            _entryButtonBackgrounds.Clear();

            if (_unlockedEntries.Count == 0)
            {
                var emptyGO = new GameObject("EmptySidebarHint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                emptyGO.transform.SetParent(entryListContainer, false);
                emptyGO.GetComponent<LayoutElement>().preferredHeight = 60f;
                var tmp = emptyGO.GetComponent<TextMeshProUGUI>();
                tmp.text = "<i>No entries unlocked yet.</i>";
                tmp.fontSize = 13f;
                tmp.color = WoodenBoardStyle.TextMutedWarm;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            for (int i = 0; i < _unlockedEntries.Count; i++)
            {
                int capturedIdx = i;
                var entry = _unlockedEntries[i];
                var rowGO = new GameObject($"EntryBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                rowGO.transform.SetParent(entryListContainer, false);

                var le = rowGO.GetComponent<LayoutElement>();
                le.minHeight = 44f;
                le.preferredHeight = 44f;

                var img = rowGO.GetComponent<Image>();
                img.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                img.type = Image.Type.Sliced;
                img.color = (i == _currentIndex) ? WoodenBoardStyle.TabActive : WoodenBoardStyle.TabInactive;
                _entryButtonBackgrounds.Add(img);

                var btn = rowGO.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectEntry(capturedIdx));

                var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(rowGO.transform, false);
                var trt = txtGO.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(10f, 4f);
                trt.offsetMax = new Vector2(-10f, -4f);

                var tmp = txtGO.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{i + 1}.</b> {entry.title}";
                tmp.fontSize = 13f;
                tmp.color = WoodenBoardStyle.TextCream;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.raycastTarget = false;
            }
        }

        private void SelectEntry(int index)
        {
            if (index < 0 || index >= _unlockedEntries.Count) return;
            _currentIndex = index;
            DisplayCurrentEntry();
        }

        private void DisplayCurrentEntry()
        {
            if (_unlockedEntries.Count == 0)
            {
                if (pageTitleText != null) pageTitleText.text = "No Story Documents Unlocked";
                if (pageAuthorText != null) pageAuthorText.text = string.Empty;
                if (pageBodyText != null) pageBodyText.text = "Explore the farm, clear new plots, and survive the mutant attacks to recover declassified factory dossiers and field logs.";
                if (pageNumberText != null) pageNumberText.text = "0 / 0";
                if (prevPageButton != null) prevPageButton.interactable = false;
                if (nextPageButton != null) nextPageButton.interactable = false;
                return;
            }

            _currentIndex = Mathf.Clamp(_currentIndex, 0, _unlockedEntries.Count - 1);
            var entry = _unlockedEntries[_currentIndex];

            if (pageTitleText != null) pageTitleText.text = entry.title;
            if (pageAuthorText != null) pageAuthorText.text = $"Written by: {entry.author} (Chapter {entry.chapter})";
            if (pageBodyText != null) pageBodyText.text = entry.content;
            if (pageNumberText != null) pageNumberText.text = $"{_currentIndex + 1} / {_unlockedEntries.Count}";

            for (int i = 0; i < _entryButtonBackgrounds.Count; i++)
            {
                if (_entryButtonBackgrounds[i] != null)
                {
                    _entryButtonBackgrounds[i].color = (i == _currentIndex) ? WoodenBoardStyle.TabActive : WoodenBoardStyle.TabInactive;
                }
            }

            if (pageIllustrationImage != null)
            {
                if (entry.pageIllustration != null)
                {
                    pageIllustrationImage.gameObject.SetActive(true);
                    pageIllustrationImage.sprite = entry.pageIllustration;
                }
                else
                {
                    pageIllustrationImage.gameObject.SetActive(false);
                }
            }

            if (prevPageButton != null) prevPageButton.interactable = _currentIndex > 0;
            if (nextPageButton != null) nextPageButton.interactable = _currentIndex < _unlockedEntries.Count - 1;
        }

        public void NextPage()
        {
            if (_currentIndex < _unlockedEntries.Count - 1)
            {
                _currentIndex++;
                DisplayCurrentEntry();
            }
        }

        public void PrevPage()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                DisplayCurrentEntry();
            }
        }

        public void EnsureUIHierarchy()
        {
            if (targetCanvas == null)
            {
                var canvasObj = GameObject.Find("UI_Canvas");
                if (canvasObj != null) targetCanvas = canvasObj.GetComponent<Canvas>();
                if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (journalRoot == null && targetCanvas != null)
            {
                var existing = targetCanvas.transform.Find("JournalPanel");
                if (existing != null)
                {
                    journalRoot = existing.gameObject;
                }
                else
                {
                    // Procedural Two-Column Rustic Wooden Journal Panel (16:9 matching 1024x572)
                    journalRoot = new GameObject("JournalPanel", typeof(RectTransform), typeof(Image));
                    journalRoot.transform.SetParent(targetCanvas.transform, false);

                    var rt = journalRoot.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(1160f, 648f);

                    var img = journalRoot.GetComponent<Image>();
                    WoodenBoardStyle.ApplyBoardBackground(img);
                    img.raycastTarget = true;

                    // Header inside Wooden Plaque
                    var plaqueTmp = WoodenBoardStyle.EnsurePlaqueTitle<TextMeshProUGUI>(journalRoot.transform);
                    if (plaqueTmp != null)
                    {
                        plaqueTmp.text = "FARM JOURNAL [J]";
                        plaqueTmp.fontSize = 21f;
                        plaqueTmp.fontStyle = FontStyles.Bold;
                        plaqueTmp.alignment = TextAlignmentOptions.Center;
                    }

                    // Left Pane: Scrollable Document Entry List
                    var leftPaneGO = new GameObject("EntryListPane", typeof(RectTransform), typeof(Image));
                    leftPaneGO.transform.SetParent(journalRoot.transform, false);
                    WoodenBoardStyle.SetAnchorRegion(leftPaneGO.GetComponent<RectTransform>(), WoodenBoardStyle.LeftPaneMin, WoodenBoardStyle.LeftPaneMax, 8f, 8f, 8f, 8f);
                    var leftPaneImg = leftPaneGO.GetComponent<Image>();
                    leftPaneImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    leftPaneImg.type = Image.Type.Sliced;
                    leftPaneImg.color = WoodenBoardStyle.DarkOakOverlay;

                    var leftHeaderGO = new GameObject("LeftHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
                    leftHeaderGO.transform.SetParent(leftPaneGO.transform, false);
                    var lhRT = leftHeaderGO.GetComponent<RectTransform>();
                    lhRT.anchorMin = new Vector2(0f, 0.90f);
                    lhRT.anchorMax = new Vector2(1f, 0.98f);
                    lhRT.offsetMin = new Vector2(8f, 0f);
                    lhRT.offsetMax = new Vector2(-8f, 0f);
                    var lhTmp = leftHeaderGO.GetComponent<TextMeshProUGUI>();
                    lhTmp.text = "RECOVERED DOSSIERS";
                    lhTmp.fontSize = 13f;
                    lhTmp.fontStyle = FontStyles.Bold;
                    lhTmp.color = WoodenBoardStyle.TextGoldAccent;
                    lhTmp.alignment = TextAlignmentOptions.Center;

                    var leftScrollGO = new GameObject("EntryScrollView", typeof(RectTransform), typeof(ScrollRect));
                    leftScrollGO.transform.SetParent(leftPaneGO.transform, false);
                    var lsRT = leftScrollGO.GetComponent<RectTransform>();
                    lsRT.anchorMin = new Vector2(0f, 0f);
                    lsRT.anchorMax = new Vector2(1f, 0.89f);
                    lsRT.offsetMin = new Vector2(6f, 6f);
                    lsRT.offsetMax = new Vector2(-6f, -4f);

                    var leftScroll = leftScrollGO.GetComponent<ScrollRect>();
                    leftScroll.horizontal = false;
                    leftScroll.vertical = true;
                    leftScroll.scrollSensitivity = 25f;

                    var leftVpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                    leftVpGO.transform.SetParent(leftScrollGO.transform, false);
                    var lvpRT = leftVpGO.GetComponent<RectTransform>();
                    lvpRT.anchorMin = Vector2.zero;
                    lvpRT.anchorMax = Vector2.one;
                    lvpRT.offsetMin = Vector2.zero;
                    lvpRT.offsetMax = Vector2.zero;
                    leftScroll.viewport = lvpRT;

                    var leftContentGO = new GameObject("EntryListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                    leftContentGO.transform.SetParent(leftVpGO.transform, false);
                    var lcRT = leftContentGO.GetComponent<RectTransform>();
                    lcRT.anchorMin = new Vector2(0f, 1f);
                    lcRT.anchorMax = new Vector2(1f, 1f);
                    lcRT.pivot = new Vector2(0.5f, 1f);
                    lcRT.sizeDelta = Vector2.zero;

                    var lcVlg = leftContentGO.GetComponent<VerticalLayoutGroup>();
                    lcVlg.padding = new RectOffset(4, 4, 4, 4);
                    lcVlg.spacing = 6f;
                    lcVlg.childControlWidth = true;
                    lcVlg.childControlHeight = false;
                    lcVlg.childForceExpandWidth = true;
                    lcVlg.childForceExpandHeight = false;
                    leftContentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    leftScroll.content = lcRT;
                    entryListContainer = leftContentGO.transform;

                    // Right Pane: Page View & Navigation
                    var rightPaneGO = new GameObject("PageViewPane", typeof(RectTransform), typeof(Image));
                    rightPaneGO.transform.SetParent(journalRoot.transform, false);
                    WoodenBoardStyle.SetAnchorRegion(rightPaneGO.GetComponent<RectTransform>(), WoodenBoardStyle.RightPaneMin, WoodenBoardStyle.RightPaneMax, 10f, 8f, 10f, 8f);
                    var rightPaneImg = rightPaneGO.GetComponent<Image>();
                    rightPaneImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    rightPaneImg.type = Image.Type.Sliced;
                    rightPaneImg.color = WoodenBoardStyle.DarkOakOverlay;

                    // Page Title
                    var titleGO = new GameObject("PageTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGO.transform.SetParent(rightPaneGO.transform, false);
                    var tRT = titleGO.GetComponent<RectTransform>();
                    tRT.anchorMin = new Vector2(0.04f, 0.87f);
                    tRT.anchorMax = new Vector2(0.96f, 0.96f);
                    tRT.offsetMin = Vector2.zero;
                    tRT.offsetMax = Vector2.zero;
                    pageTitleText = titleGO.GetComponent<TextMeshProUGUI>();
                    pageTitleText.fontSize = 21;
                    pageTitleText.fontStyle = FontStyles.Bold;
                    pageTitleText.color = WoodenBoardStyle.TextGoldAccent;

                    // Page Author
                    var authorGO = new GameObject("PageAuthor", typeof(RectTransform), typeof(TextMeshProUGUI));
                    authorGO.transform.SetParent(rightPaneGO.transform, false);
                    var aRT = authorGO.GetComponent<RectTransform>();
                    aRT.anchorMin = new Vector2(0.04f, 0.80f);
                    aRT.anchorMax = new Vector2(0.96f, 0.87f);
                    aRT.offsetMin = Vector2.zero;
                    aRT.offsetMax = Vector2.zero;
                    pageAuthorText = authorGO.GetComponent<TextMeshProUGUI>();
                    pageAuthorText.fontSize = 14;
                    pageAuthorText.fontStyle = FontStyles.Italic;
                    pageAuthorText.color = WoodenBoardStyle.TextMutedWarm;

                    // Page Body
                    var bodyGO = new GameObject("PageBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(rightPaneGO.transform, false);
                    var bRT = bodyGO.GetComponent<RectTransform>();
                    bRT.anchorMin = new Vector2(0.04f, 0.14f);
                    bRT.anchorMax = new Vector2(0.96f, 0.78f);
                    bRT.offsetMin = Vector2.zero;
                    bRT.offsetMax = Vector2.zero;
                    pageBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    pageBodyText.fontSize = 16;
                    pageBodyText.color = WoodenBoardStyle.TextCream;
                    pageBodyText.textWrappingMode = TextWrappingModes.Normal;

                    // Page Number
                    var numGO = new GameObject("PageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
                    numGO.transform.SetParent(rightPaneGO.transform, false);
                    var nRT = numGO.GetComponent<RectTransform>();
                    nRT.anchorMin = new Vector2(0.38f, 0.03f);
                    nRT.anchorMax = new Vector2(0.62f, 0.11f);
                    nRT.offsetMin = Vector2.zero;
                    nRT.offsetMax = Vector2.zero;
                    pageNumberText = numGO.GetComponent<TextMeshProUGUI>();
                    pageNumberText.fontSize = 15;
                    pageNumberText.alignment = TextAlignmentOptions.Center;
                    pageNumberText.color = WoodenBoardStyle.TextMutedWarm;

                    // Previous Button
                    var prevGO = new GameObject("PrevButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    prevGO.transform.SetParent(rightPaneGO.transform, false);
                    var pRT = prevGO.GetComponent<RectTransform>();
                    pRT.anchorMin = new Vector2(0.04f, 0.03f);
                    pRT.anchorMax = new Vector2(0.04f, 0.03f);
                    pRT.pivot = new Vector2(0f, 0f);
                    pRT.sizeDelta = new Vector2(115f, 36f);
                    prevGO.GetComponent<Image>().sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    prevGO.GetComponent<Image>().type = Image.Type.Sliced;
                    prevGO.GetComponent<Image>().color = WoodenBoardStyle.BtnSecondary;
                    prevPageButton = prevGO.GetComponent<Button>();
                    prevPageButton.onClick.AddListener(PrevPage);
                    var pTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pTxtGO.transform.SetParent(prevGO.transform, false);
                    var pt = pTxtGO.GetComponent<TextMeshProUGUI>();
                    pt.text = "< Previous";
                    pt.fontSize = 14;
                    pt.fontStyle = FontStyles.Bold;
                    pt.color = WoodenBoardStyle.TextCream;
                    pt.alignment = TextAlignmentOptions.Center;

                    // Next Button
                    var nextGO = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    nextGO.transform.SetParent(rightPaneGO.transform, false);
                    var nxtRT = nextGO.GetComponent<RectTransform>();
                    nxtRT.anchorMin = new Vector2(0.96f, 0.03f);
                    nxtRT.anchorMax = new Vector2(0.96f, 0.03f);
                    nxtRT.pivot = new Vector2(1f, 0f);
                    nxtRT.sizeDelta = new Vector2(115f, 36f);
                    nextGO.GetComponent<Image>().sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    nextGO.GetComponent<Image>().type = Image.Type.Sliced;
                    nextGO.GetComponent<Image>().color = WoodenBoardStyle.BtnSecondary;
                    nextPageButton = nextGO.GetComponent<Button>();
                    nextPageButton.onClick.AddListener(NextPage);
                    var nTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    nTxtGO.transform.SetParent(nextGO.transform, false);
                    var nt = nTxtGO.GetComponent<TextMeshProUGUI>();
                    nt.text = "Next >";
                    nt.fontSize = 14;
                    nt.fontStyle = FontStyles.Bold;
                    nt.color = WoodenBoardStyle.TextCream;
                    nt.alignment = TextAlignmentOptions.Center;

                    // Close Button (upper-right rim corner)
                    var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    closeGO.transform.SetParent(journalRoot.transform, false);
                    var cRT = closeGO.GetComponent<RectTransform>();
                    cRT.anchorMin = WoodenBoardStyle.CloseButtonAnchor;
                    cRT.anchorMax = WoodenBoardStyle.CloseButtonAnchor;
                    cRT.pivot = new Vector2(0.5f, 0.5f);
                    cRT.anchoredPosition = Vector2.zero;
                    cRT.sizeDelta = new Vector2(36f, 36f);
                    closeGO.GetComponent<Image>().sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    closeGO.GetComponent<Image>().type = Image.Type.Sliced;
                    closeGO.GetComponent<Image>().color = WoodenBoardStyle.BtnDanger;
                    closeButton = closeGO.GetComponent<Button>();
                    closeButton.onClick.AddListener(CloseJournal);
                    var cTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    cTxtGO.transform.SetParent(closeGO.transform, false);
                    var ct = cTxtGO.GetComponent<TextMeshProUGUI>();
                    ct.text = "✕";
                    ct.fontSize = 18;
                    ct.alignment = TextAlignmentOptions.Center;

                    journalRoot.SetActive(false);
                }
            }

            if (journalRoot != null)
            {
                Transform pagePane = journalRoot.transform.Find("PageViewPane") ?? journalRoot.transform;
                if (pageTitleText == null) pageTitleText = (pagePane.Find("PageTitle") ?? journalRoot.transform.Find("PageTitle"))?.GetComponent<TextMeshProUGUI>();
                if (pageAuthorText == null) pageAuthorText = (pagePane.Find("PageAuthor") ?? journalRoot.transform.Find("PageAuthor"))?.GetComponent<TextMeshProUGUI>();
                if (pageBodyText == null) pageBodyText = (pagePane.Find("PageBody") ?? journalRoot.transform.Find("PageBody"))?.GetComponent<TextMeshProUGUI>();
                if (pageNumberText == null) pageNumberText = (pagePane.Find("PageNumber") ?? journalRoot.transform.Find("PageNumber"))?.GetComponent<TextMeshProUGUI>();
                if (entryListContainer == null) entryListContainer = journalRoot.transform.Find("EntryListPane/EntryScrollView/Viewport/EntryListContent");
                if (prevPageButton == null)
                {
                    prevPageButton = (pagePane.Find("PrevButton") ?? journalRoot.transform.Find("PrevButton"))?.GetComponent<Button>();
                    if (prevPageButton != null)
                    {
                        prevPageButton.onClick.RemoveListener(PrevPage);
                        prevPageButton.onClick.AddListener(PrevPage);
                    }
                }
                if (nextPageButton == null)
                {
                    nextPageButton = (pagePane.Find("NextButton") ?? journalRoot.transform.Find("NextButton"))?.GetComponent<Button>();
                    if (nextPageButton != null)
                    {
                        nextPageButton.onClick.RemoveListener(NextPage);
                        nextPageButton.onClick.AddListener(NextPage);
                    }
                }
                if (closeButton == null)
                {
                    closeButton = journalRoot.transform.Find("CloseButton")?.GetComponent<Button>();
                    if (closeButton != null)
                    {
                        closeButton.onClick.RemoveListener(CloseJournal);
                        closeButton.onClick.AddListener(CloseJournal);
                    }
                }

                var rootCanvas = journalRoot.GetComponent<Canvas>();
                if (rootCanvas == null) rootCanvas = journalRoot.AddComponent<Canvas>();
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = 700;
                if (!journalRoot.TryGetComponent<GraphicRaycaster>(out _))
                {
                    journalRoot.AddComponent<GraphicRaycaster>();
                }
            }

            // Clean up any stray HUD_DocsButton if present
            if (targetCanvas != null)
            {
                var existingBtn = targetCanvas.transform.Find("HUD_DocsButton");
                if (existingBtn != null)
                {
                    if (Application.isPlaying) Destroy(existingBtn.gameObject);
                    else DestroyImmediate(existingBtn.gameObject);
                }
            }
        }
    }
}
