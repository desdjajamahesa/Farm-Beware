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
        public static JournalUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
                }
                if (_instance != null && !_instance.gameObject.activeInHierarchy)
                {
                    _instance.gameObject.SetActive(true);
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
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
        }

        private void DisplayCurrentEntry()
        {
            if (_unlockedEntries.Count == 0)
            {
                if (pageTitleText != null) pageTitleText.text = "No Entries Unlocked";
                if (pageAuthorText != null) pageAuthorText.text = string.Empty;
                if (pageBodyText != null) pageBodyText.text = "Explore the farm, plant crops, and survive the night to discover Grandfather's lost journal entries.";
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

        private void EnsureUIHierarchy()
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
                    // Procedural Journal Panel
                    journalRoot = new GameObject("JournalPanel", typeof(RectTransform), typeof(Image));
                    journalRoot.transform.SetParent(targetCanvas.transform, false);

                    var rt = journalRoot.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(850f, 560f);

                    var img = journalRoot.GetComponent<Image>();
                    img.color = new Color(0.12f, 0.11f, 0.10f, 0.98f);

                    // Header
                    var headerGO = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
                    headerGO.transform.SetParent(journalRoot.transform, false);
                    var hRT = headerGO.GetComponent<RectTransform>();
                    hRT.anchorMin = new Vector2(0f, 1f);
                    hRT.anchorMax = new Vector2(1f, 1f);
                    hRT.pivot = new Vector2(0.5f, 1f);
                    hRT.anchoredPosition = new Vector2(0f, -20f);
                    hRT.sizeDelta = new Vector2(800f, 40f);
                    var hTxt = headerGO.GetComponent<TextMeshProUGUI>();
                    hTxt.text = "📖 VERDANT HOLLOW ARCHIVES";
                    hTxt.fontSize = 22;
                    hTxt.fontStyle = FontStyles.Bold;
                    hTxt.color = new Color(0.95f, 0.8f, 0.4f);
                    hTxt.alignment = TextAlignmentOptions.Center;

                    // Page Title
                    var titleGO = new GameObject("PageTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGO.transform.SetParent(journalRoot.transform, false);
                    var tRT = titleGO.GetComponent<RectTransform>();
                    tRT.anchorMin = new Vector2(0.08f, 0.82f);
                    tRT.anchorMax = new Vector2(0.92f, 0.90f);
                    tRT.offsetMin = Vector2.zero;
                    tRT.offsetMax = Vector2.zero;
                    pageTitleText = titleGO.GetComponent<TextMeshProUGUI>();
                    pageTitleText.fontSize = 20;
                    pageTitleText.fontStyle = FontStyles.Bold;
                    pageTitleText.color = Color.white;

                    // Page Author
                    var authorGO = new GameObject("PageAuthor", typeof(RectTransform), typeof(TextMeshProUGUI));
                    authorGO.transform.SetParent(journalRoot.transform, false);
                    var aRT = authorGO.GetComponent<RectTransform>();
                    aRT.anchorMin = new Vector2(0.08f, 0.76f);
                    aRT.anchorMax = new Vector2(0.92f, 0.82f);
                    aRT.offsetMin = Vector2.zero;
                    aRT.offsetMax = Vector2.zero;
                    pageAuthorText = authorGO.GetComponent<TextMeshProUGUI>();
                    pageAuthorText.fontSize = 14;
                    pageAuthorText.fontStyle = FontStyles.Italic;
                    pageAuthorText.color = new Color(0.7f, 0.7f, 0.7f);

                    // Page Body
                    var bodyGO = new GameObject("PageBody", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bodyGO.transform.SetParent(journalRoot.transform, false);
                    var bRT = bodyGO.GetComponent<RectTransform>();
                    bRT.anchorMin = new Vector2(0.08f, 0.16f);
                    bRT.anchorMax = new Vector2(0.92f, 0.75f);
                    bRT.offsetMin = Vector2.zero;
                    bRT.offsetMax = Vector2.zero;
                    pageBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
                    pageBodyText.fontSize = 16;
                    pageBodyText.color = new Color(0.92f, 0.90f, 0.85f);
                    pageBodyText.textWrappingMode = TextWrappingModes.Normal;

                    // Page Number
                    var numGO = new GameObject("PageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
                    numGO.transform.SetParent(journalRoot.transform, false);
                    var nRT = numGO.GetComponent<RectTransform>();
                    nRT.anchorMin = new Vector2(0.4f, 0.05f);
                    nRT.anchorMax = new Vector2(0.6f, 0.12f);
                    nRT.offsetMin = Vector2.zero;
                    nRT.offsetMax = Vector2.zero;
                    pageNumberText = numGO.GetComponent<TextMeshProUGUI>();
                    pageNumberText.fontSize = 15;
                    pageNumberText.alignment = TextAlignmentOptions.Center;
                    pageNumberText.color = new Color(0.6f, 0.6f, 0.6f);

                    // Previous Button
                    var prevGO = new GameObject("PrevButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    prevGO.transform.SetParent(journalRoot.transform, false);
                    var pRT = prevGO.GetComponent<RectTransform>();
                    pRT.anchorMin = new Vector2(0.08f, 0.05f);
                    pRT.anchorMax = new Vector2(0.08f, 0.05f);
                    pRT.pivot = new Vector2(0f, 0f);
                    pRT.sizeDelta = new Vector2(100f, 36f);
                    prevGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.28f);
                    prevPageButton = prevGO.GetComponent<Button>();
                    prevPageButton.onClick.AddListener(PrevPage);
                    var pTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pTxtGO.transform.SetParent(prevGO.transform, false);
                    var pt = pTxtGO.GetComponent<TextMeshProUGUI>();
                    pt.text = "◀ Previous";
                    pt.fontSize = 14;
                    pt.alignment = TextAlignmentOptions.Center;

                    // Next Button
                    var nextGO = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    nextGO.transform.SetParent(journalRoot.transform, false);
                    var nxtRT = nextGO.GetComponent<RectTransform>();
                    nxtRT.anchorMin = new Vector2(0.92f, 0.05f);
                    nxtRT.anchorMax = new Vector2(0.92f, 0.05f);
                    nxtRT.pivot = new Vector2(1f, 0f);
                    nxtRT.sizeDelta = new Vector2(100f, 36f);
                    nextGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.28f);
                    nextPageButton = nextGO.GetComponent<Button>();
                    nextPageButton.onClick.AddListener(NextPage);
                    var nTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    nTxtGO.transform.SetParent(nextGO.transform, false);
                    var nt = nTxtGO.GetComponent<TextMeshProUGUI>();
                    nt.text = "Next ▶";
                    nt.fontSize = 14;
                    nt.alignment = TextAlignmentOptions.Center;

                    // Close Button
                    var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    closeGO.transform.SetParent(journalRoot.transform, false);
                    var cRT = closeGO.GetComponent<RectTransform>();
                    cRT.anchorMin = new Vector2(1f, 1f);
                    cRT.anchorMax = new Vector2(1f, 1f);
                    cRT.pivot = new Vector2(1f, 1f);
                    cRT.anchoredPosition = new Vector2(-15f, -15f);
                    cRT.sizeDelta = new Vector2(36f, 36f);
                    closeGO.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f);
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
        }
    }
}
