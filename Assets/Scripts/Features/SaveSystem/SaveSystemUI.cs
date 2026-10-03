using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace FeaturesSaveSystem
{
    /// <summary>
    /// Multi-Slot Save & Load Modal UI for Farm Beware.
    /// Features:
    /// - Custom Save Name input field with 25-character validation and auto-naming fallback.
    /// - Scrollable list of existing saves displaying name, timestamp, Day, Phase, Gold, HP, Location.
    /// - Per-item action buttons: [Load], [Overwrite], [Delete].
    /// - Built-in confirmation dialog for overwrite and deletion to prevent accidental loss.
    /// - Full keyboard ESC support and player movement locking.
    /// </summary>
    public class SaveSystemUI : MonoBehaviour
    {
        public static SaveSystemUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SaveSystemUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set => instance = value;
        }
        private static SaveSystemUI instance;

        [Header("UI Root")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private RectTransform cardRect;

        [Header("New Save Input")]
        [SerializeField] private TMP_InputField saveNameInputField;
        [SerializeField] private Button saveNewButton;
        [SerializeField] private TextMeshProUGUI charCountText;

        [Header("Slots List")]
        [SerializeField] private Transform slotsContentContainer;
        [SerializeField] private GameObject emptyStateText;
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmDialogRoot;
        [SerializeField] private TextMeshProUGUI confirmTitleText;
        [SerializeField] private TextMeshProUGUI confirmMessageText;
        [SerializeField] private Button confirmActionButton;
        [SerializeField] private TextMeshProUGUI confirmActionBtnText;
        [SerializeField] private Button confirmCancelButton;

        [Header("General")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropCloseButton;

        private bool isOpen = false;
        public bool IsOpen => isOpen;

        private Action pendingConfirmAction;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            EnsureUIHierarchy();
        }

        private void OnEnable()
        {
            if (SaveSystemManager.Instance != null)
            {
                SaveSystemManager.Instance.OnSaveListChanged += RefreshSlotList;
            }
        }

        private void OnDisable()
        {
            if (SaveSystemManager.Instance != null)
            {
                SaveSystemManager.Instance.OnSaveListChanged -= RefreshSlotList;
            }

            if (isOpen)
            {
                Time.timeScale = 1f;
                isOpen = false;
                FarmBeware.Core.Runtime.UIModalHelper.IsSaveUIOpen = false;
            }
        }

        private void Update()
        {
            if (!isOpen) return;

            // Handle ESC key
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (confirmDialogRoot != null && confirmDialogRoot.activeSelf)
                {
                    CloseConfirmDialog();
                }
                else
                {
                    Close();
                }
            }
        }

        public void Open()
        {
            EnsureUIHierarchy();

            Time.timeScale = 0f;

            var playerCtx = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IPlayerContext>();
            if (playerCtx != null)
            {
                playerCtx.StopMovement();
                playerCtx.IsInputLocked = true;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            isOpen = true;
            FarmBeware.Core.Runtime.UIModalHelper.IsSaveUIOpen = true;
            if (modalPanel != null)
            {
                modalPanel.SetActive(true);
            }

            if (saveNameInputField != null)
            {
                saveNameInputField.text = "";
                UpdateCharCount("");
            }

            if (statusFeedbackText != null)
                statusFeedbackText.text = "";

            CloseConfirmDialog();
            RefreshSlotList();
        }

        public void Close()
        {
            isOpen = false;
            FarmBeware.Core.Runtime.UIModalHelper.IsSaveUIOpen = false;
            CloseConfirmDialog();

            Time.timeScale = 1f;

            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }

            var closePlayerCtx = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IPlayerContext>();
            if (closePlayerCtx != null)
            {
                closePlayerCtx.IsInputLocked = false;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Prevent ESC from triggering the Pause Menu simultaneously
            FarmBeware.Core.Runtime.UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
        }

        #region New Save Creation

        public void OnSaveNewClicked()
        {
            string rawName = saveNameInputField != null ? saveNameInputField.text : "";
            string sanitized = SaveSystemManager.SanitizeSaveName(rawName);

            bool success = SaveSystemManager.Instance.CreateNewSave(sanitized, out string msg);

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = success
                    ? $"<color=#4ADE80>✓ {msg}</color>"
                    : $"<color=#F87171>✗ {msg}</color>";
            }

            if (success && saveNameInputField != null)
            {
                saveNameInputField.text = "";
                UpdateCharCount("");
            }

            RefreshSlotList();
        }

        private void UpdateCharCount(string text)
        {
            if (charCountText != null)
            {
                int len = text != null ? text.Length : 0;
                charCountText.text = $"{len}/25";
            }
        }

        #endregion

        #region Slot List Rendering

        public void RefreshSlotList()
        {
            if (slotsContentContainer == null) return;

            // Clear old slot cards
            for (int i = slotsContentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(slotsContentContainer.GetChild(i).gameObject);
            }

            var saves = SaveSystemManager.Instance.GetSaveList();

            if (saves == null || saves.Count == 0)
            {
                if (emptyStateText != null) emptyStateText.SetActive(true);
                return;
            }

            if (emptyStateText != null) emptyStateText.SetActive(false);

            foreach (var meta in saves)
            {
                CreateSlotItemCard(slotsContentContainer, meta);
            }
        }

        private void CreateSlotItemCard(Transform parent, SaveMetadata meta)
        {
            // Root Card
            var cardGO = new GameObject($"Slot_{meta.saveId}", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(parent, false);

            var rt = cardGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 96);

            var le = cardGO.AddComponent<LayoutElement>();
            le.minHeight = 96;
            le.preferredHeight = 96;
            le.flexibleWidth = 1;

            var img = cardGO.GetComponent<Image>();
            img.color = new Color(0.14f, 0.17f, 0.25f, 0.95f);

            var outline = cardGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.42f, 0.58f, 0.4f);
            outline.effectDistance = new Vector2(1, -1);

            // Left Info Container
            var leftGO = new GameObject("LeftInfo", typeof(RectTransform));
            leftGO.transform.SetParent(cardGO.transform, false);
            var leftRT = leftGO.GetComponent<RectTransform>();
            leftRT.anchorMin = new Vector2(0, 0);
            leftRT.anchorMax = new Vector2(1, 1);
            leftRT.offsetMin = new Vector2(20, 10);
            leftRT.offsetMax = new Vector2(-360, -10);

            // Display Name
            var nameGO = new GameObject("SaveName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(leftGO.transform, false);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.46f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTmp = nameGO.GetComponent<TextMeshProUGUI>();
            nameTmp.text = $"<b>{meta.displayName}</b>";
            nameTmp.fontSize = 22;
            nameTmp.color = new Color(0.98f, 0.92f, 0.78f);
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Details / Metadata
            var detailGO = new GameObject("Details", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailGO.transform.SetParent(leftGO.transform, false);
            var dRT = detailGO.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0, 0);
            dRT.anchorMax = new Vector2(1, 0.46f);
            dRT.offsetMin = Vector2.zero;
            dRT.offsetMax = Vector2.zero;
            var dTmp = detailGO.GetComponent<TextMeshProUGUI>();
            dTmp.text = $"<color=#94A3B8>{meta.formattedDate}</color>   " +
                        $"<color=#FBBF24>Day {meta.dayNumber} ({meta.phaseName})</color>  •  " +
                        $"<color=#FCD34D>{meta.gold:N0} Gold</color>  •  " +
                        $"<color=#F87171>HP: {meta.currentHealth}/{meta.maxHealth}</color>  •  " +
                        $"<color=#A5B4FC>{meta.locationName}</color>";
            dTmp.fontSize = 14;
            dTmp.color = Color.white;
            dTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Right Actions Container
            var actionsGO = new GameObject("Actions", typeof(RectTransform));
            actionsGO.transform.SetParent(cardGO.transform, false);
            var actRT = actionsGO.GetComponent<RectTransform>();
            actRT.anchorMin = new Vector2(1, 0.5f);
            actRT.anchorMax = new Vector2(1, 0.5f);
            actRT.pivot = new Vector2(1, 0.5f);
            actRT.sizeDelta = new Vector2(340, 54);
            actRT.anchoredPosition = new Vector2(-16, 0);

            var actHlg = actionsGO.AddComponent<HorizontalLayoutGroup>();
            actHlg.spacing = 10;
            actHlg.childAlignment = TextAnchor.MiddleRight;
            actHlg.childControlWidth = false;
            actHlg.childControlHeight = true;
            actHlg.childForceExpandWidth = false;
            actHlg.childForceExpandHeight = true;

            // 1. [LOAD] Button
            CreateMiniButton(actionsGO.transform, "Btn_Load", "Load", new Color(0.18f, 0.48f, 0.88f, 1f), 95, () =>
            {
                OnLoadSlotClicked(meta.saveId);
            });

            // 2. [OVERWRITE] Button
            CreateMiniButton(actionsGO.transform, "Btn_Overwrite", "Overwrite", new Color(0.80f, 0.54f, 0.15f, 1f), 115, () =>
            {
                RequestOverwriteConfirmation(meta);
            });

            // 3. [DELETE] Button
            CreateMiniButton(actionsGO.transform, "Btn_Delete", "Delete", new Color(0.78f, 0.22f, 0.22f, 1f), 95, () =>
            {
                RequestDeleteConfirmation(meta);
            });
        }

        private Button CreateMiniButton(Transform parent, string name, string label, Color bgColor, float width, UnityEngine.Events.UnityAction onClick)
        {
            var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 44);

            var le = btnGO.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.minHeight = 44;
            le.preferredHeight = 44;

            var img = btnGO.GetComponent<Image>();
            img.color = bgColor;

            var btn = btnGO.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.25f;
            colors.pressedColor = bgColor * 0.85f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var tRT = textGO.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 15;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        #endregion

        #region Slot Actions

        private void OnLoadSlotClicked(string saveId)
        {
            bool success = SaveSystemManager.Instance.LoadSave(saveId, out string msg);
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = success
                    ? $"<color=#38BDF8>✓ {msg}</color>"
                    : $"<color=#F87171>✗ {msg}</color>";
            }

            if (success)
            {
                Close();
            }
        }

        private void RequestOverwriteConfirmation(SaveMetadata meta)
        {
            ShowConfirmDialog(
                title: "OVERWRITE SAVE",
                message: $"Are you sure you want to overwrite <color=#FBBF24>'{meta.displayName}'</color> with current game progress?\nThis will replace the previous save file.",
                actionLabel: "OVERWRITE",
                actionColor: new Color(0.85f, 0.55f, 0.15f, 1f),
                onConfirm: () =>
                {
                    bool success = SaveSystemManager.Instance.OverwriteSave(meta.saveId, out string msg);
                    if (statusFeedbackText != null)
                    {
                        statusFeedbackText.text = success
                            ? $"<color=#4ADE80>✓ {msg}</color>"
                            : $"<color=#F87171>✗ {msg}</color>";
                    }
                    RefreshSlotList();
                }
            );
        }

        private void RequestDeleteConfirmation(SaveMetadata meta)
        {
            ShowConfirmDialog(
                title: "DELETE SAVE",
                message: $"Are you sure you want to permanently delete <color=#F87171>'{meta.displayName}'</color>?\n<color=#EF4444>This action cannot be undone.</color>",
                actionLabel: "DELETE",
                actionColor: new Color(0.78f, 0.20f, 0.20f, 1f),
                onConfirm: () =>
                {
                    bool success = SaveSystemManager.Instance.DeleteSave(meta.saveId, out string msg);
                    if (statusFeedbackText != null)
                    {
                        statusFeedbackText.text = success
                            ? $"<color=#F87171>✓ {msg}</color>"
                            : $"<color=#F87171>✗ {msg}</color>";
                    }
                    RefreshSlotList();
                }
            );
        }

        #endregion

        #region Confirmation Dialog

        private void ShowConfirmDialog(string title, string message, string actionLabel, Color actionColor, Action onConfirm)
        {
            if (confirmDialogRoot == null) return;

            pendingConfirmAction = onConfirm;

            if (confirmTitleText != null) confirmTitleText.text = $"<b>{title}</b>";
            if (confirmMessageText != null) confirmMessageText.text = message;

            if (confirmActionBtnText != null) confirmActionBtnText.text = $"<b>{actionLabel}</b>";
            if (confirmActionButton != null)
            {
                var img = confirmActionButton.GetComponent<Image>();
                if (img != null) img.color = actionColor;
                var colors = confirmActionButton.colors;
                colors.normalColor = actionColor;
                colors.highlightedColor = actionColor * 1.2f;
                colors.pressedColor = actionColor * 0.85f;
                confirmActionButton.colors = colors;
            }

            confirmDialogRoot.SetActive(true);
        }

        private void CloseConfirmDialog()
        {
            pendingConfirmAction = null;
            if (confirmDialogRoot != null)
                confirmDialogRoot.SetActive(false);
        }

        private void OnConfirmActionExecuted()
        {
            var action = pendingConfirmAction;
            CloseConfirmDialog();
            action?.Invoke();
        }

        #endregion

        #region UI Hierarchy Construction

        private void EnsureUIHierarchy()
        {
            if (modalPanel != null) return;

            var canvas = GameObject.Find("UI_Canvas");
            if (canvas == null)
            {
                var c = FindFirstObjectByType<Canvas>();
                if (c != null) canvas = c.gameObject;
            }
            if (canvas == null) return;

            var existing = canvas.transform.Find("Panel_SavingSystem");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            BuildUI(canvas.transform);
        }

        private void BuildUI(Transform canvasTr)
        {
            // 1. Root Modal Panel
            modalPanel = new GameObject("Panel_SavingSystem", typeof(RectTransform));
            modalPanel.transform.SetParent(canvasTr, false);
            var rootRT = modalPanel.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero;

            // Backdrop
            var backdropGO = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGO.transform.SetParent(modalPanel.transform, false);
            var bdRT = backdropGO.GetComponent<RectTransform>();
            bdRT.anchorMin = Vector2.zero;
            bdRT.anchorMax = Vector2.one;
            bdRT.sizeDelta = Vector2.zero;
            var bdImg = backdropGO.GetComponent<Image>();
            bdImg.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
            backdropCloseButton = backdropGO.GetComponent<Button>();
            backdropCloseButton.onClick.AddListener(Close);

            // 2. Central Modal Card
            var cardGO = new GameObject("SaveModalCard", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(modalPanel.transform, false);
            cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(1180, 800);
            cardRect.anchoredPosition = Vector2.zero;
            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

            var outline = cardGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.75f, 0.58f, 0.28f, 0.85f); // Antique Gold
            outline.effectDistance = new Vector2(3, -3);

            // 3. Header Bar
            var headerGO = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            headerGO.transform.SetParent(cardGO.transform, false);
            var hRT = headerGO.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1);
            hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.sizeDelta = new Vector2(0, 68);
            hRT.anchoredPosition = Vector2.zero;
            var hImg = headerGO.GetComponent<Image>();
            hImg.color = new Color(0.15f, 0.18f, 0.28f, 1f);

            var titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(headerGO.transform, false);
            var tRT = titleGO.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;
            tRT.offsetMin = new Vector2(24, 0);
            var headerTmp = titleGO.GetComponent<TextMeshProUGUI>();
            headerTmp.text = "<b>SAVING & LOADING SYSTEM</b>";
            headerTmp.fontSize = 26;
            headerTmp.color = new Color(0.96f, 0.88f, 0.72f);
            headerTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var closeGO = new GameObject("Btn_XClose", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(headerGO.transform, false);
            var cRT = closeGO.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(1, 0.5f);
            cRT.anchorMax = new Vector2(1, 0.5f);
            cRT.pivot = new Vector2(1, 0.5f);
            cRT.sizeDelta = new Vector2(44, 44);
            cRT.anchoredPosition = new Vector2(-16, 0);
            var cImg = closeGO.GetComponent<Image>();
            cImg.color = new Color(0.75f, 0.22f, 0.22f, 0.9f);
            closeButton = closeGO.GetComponent<Button>();
            closeButton.onClick.AddListener(Close);

            var xTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            xTextGO.transform.SetParent(closeGO.transform, false);
            var xRT = xTextGO.GetComponent<RectTransform>();
            xRT.anchorMin = Vector2.zero;
            xRT.anchorMax = Vector2.one;
            xRT.sizeDelta = Vector2.zero;
            var xTmp = xTextGO.GetComponent<TextMeshProUGUI>();
            xTmp.text = "<b>X</b>";
            xTmp.fontSize = 20;
            xTmp.color = Color.white;
            xTmp.alignment = TextAlignmentOptions.Center;

            // 4. New Save Section (Input Field + Save Button)
            var newSaveSection = new GameObject("NewSaveSection", typeof(RectTransform), typeof(Image));
            newSaveSection.transform.SetParent(cardGO.transform, false);
            var nsRT = newSaveSection.GetComponent<RectTransform>();
            nsRT.anchorMin = new Vector2(0, 1);
            nsRT.anchorMax = new Vector2(1, 1);
            nsRT.pivot = new Vector2(0.5f, 1);
            nsRT.sizeDelta = new Vector2(-48, 72);
            nsRT.anchoredPosition = new Vector2(0, -84);
            var nsImg = newSaveSection.GetComponent<Image>();
            nsImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);

            // Input Field
            var inputGO = new GameObject("InputField_SaveName", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGO.transform.SetParent(newSaveSection.transform, false);
            var inRT = inputGO.GetComponent<RectTransform>();
            inRT.anchorMin = new Vector2(0, 0.5f);
            inRT.anchorMax = new Vector2(1, 0.5f);
            inRT.pivot = new Vector2(0.5f, 0.5f);
            inRT.offsetMin = new Vector2(18, -26);
            inRT.offsetMax = new Vector2(-220, 26);
            var inImg = inputGO.GetComponent<Image>();
            inImg.color = new Color(0.14f, 0.16f, 0.22f, 1f);

            saveNameInputField = inputGO.GetComponent<TMP_InputField>();
            saveNameInputField.characterLimit = 25;

            // Text Component
            var textCompGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textCompGO.transform.SetParent(inputGO.transform, false);
            var tcRT = textCompGO.GetComponent<RectTransform>();
            tcRT.anchorMin = Vector2.zero;
            tcRT.anchorMax = Vector2.one;
            tcRT.sizeDelta = new Vector2(-24, 0);
            var tcTmp = textCompGO.GetComponent<TextMeshProUGUI>();
            tcTmp.fontSize = 18;
            tcTmp.color = Color.white;
            tcTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Placeholder Component
            var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGO.transform.SetParent(inputGO.transform, false);
            var phRT = phGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.sizeDelta = new Vector2(-24, 0);
            var phTmp = phGO.GetComponent<TextMeshProUGUI>();
            phTmp.text = "Type save name (e.g. Chapter 1 - Hutan)...";
            phTmp.fontSize = 17;
            phTmp.color = new Color(0.55f, 0.60f, 0.70f, 0.65f);
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            saveNameInputField.textComponent = tcTmp;
            saveNameInputField.placeholder = phTmp;
            saveNameInputField.onValueChanged.AddListener(UpdateCharCount);

            // Char count label
            var countGO = new GameObject("CharCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            countGO.transform.SetParent(newSaveSection.transform, false);
            var cCountRT = countGO.GetComponent<RectTransform>();
            cCountRT.anchorMin = new Vector2(1, 0.5f);
            cCountRT.anchorMax = new Vector2(1, 0.5f);
            cCountRT.pivot = new Vector2(1, 0.5f);
            cCountRT.sizeDelta = new Vector2(60, 32);
            cCountRT.anchoredPosition = new Vector2(-215, 0);
            charCountText = countGO.GetComponent<TextMeshProUGUI>();
            charCountText.fontSize = 14;
            charCountText.color = new Color(0.6f, 0.65f, 0.75f);
            charCountText.alignment = TextAlignmentOptions.MidlineRight;
            charCountText.text = "0/25";

            // Save New Button
            var saveBtnGO = new GameObject("Btn_SaveNew", typeof(RectTransform), typeof(Image), typeof(Button));
            saveBtnGO.transform.SetParent(newSaveSection.transform, false);
            var sRT = saveBtnGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(1, 0.5f);
            sRT.anchorMax = new Vector2(1, 0.5f);
            sRT.pivot = new Vector2(1, 0.5f);
            sRT.sizeDelta = new Vector2(185, 52);
            sRT.anchoredPosition = new Vector2(-16, 0);

            var sImg = saveBtnGO.GetComponent<Image>();
            sImg.color = new Color(0.16f, 0.58f, 0.32f, 1f);

            saveNewButton = saveBtnGO.GetComponent<Button>();
            saveNewButton.onClick.AddListener(OnSaveNewClicked);

            var sTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            sTxtGO.transform.SetParent(saveBtnGO.transform, false);
            var stRT = sTxtGO.GetComponent<RectTransform>();
            stRT.anchorMin = Vector2.zero;
            stRT.anchorMax = Vector2.one;
            stRT.sizeDelta = Vector2.zero;
            var sTmp = sTxtGO.GetComponent<TextMeshProUGUI>();
            sTmp.text = "<b>Save New</b>";
            sTmp.fontSize = 18;
            sTmp.color = Color.white;
            sTmp.alignment = TextAlignmentOptions.Center;

            // 5. Status Feedback Banner
            var fbGO = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
            fbGO.transform.SetParent(cardGO.transform, false);
            var fbRT = fbGO.GetComponent<RectTransform>();
            fbRT.anchorMin = new Vector2(0, 1);
            fbRT.anchorMax = new Vector2(1, 1);
            fbRT.pivot = new Vector2(0.5f, 1);
            fbRT.sizeDelta = new Vector2(-48, 28);
            fbRT.anchoredPosition = new Vector2(0, -168);
            statusFeedbackText = fbGO.GetComponent<TextMeshProUGUI>();
            statusFeedbackText.fontSize = 15;
            statusFeedbackText.alignment = TextAlignmentOptions.Center;
            statusFeedbackText.text = "";

            // 6. Scrollable List of Slots
            var scrollGO = new GameObject("ScrollSlots", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(cardGO.transform, false);
            var scRT = scrollGO.GetComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0, 0);
            scRT.anchorMax = new Vector2(1, 1);
            scRT.offsetMin = new Vector2(24, 24);
            scRT.offsetMax = new Vector2(-24, -204);

            var scImg = scrollGO.GetComponent<Image>();
            scImg.color = new Color(0.06f, 0.08f, 0.12f, 0.6f);

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            // Viewport
            var viewGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewGO.transform.SetParent(scrollGO.transform, false);
            var vRT = viewGO.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero;
            vRT.anchorMax = Vector2.one;
            vRT.sizeDelta = Vector2.zero;
            scrollRect.viewport = vRT;

            // Content Container
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewGO.transform, false);
            slotsContentContainer = contentGO.transform;
            var cRT2 = contentGO.GetComponent<RectTransform>();
            cRT2.anchorMin = new Vector2(0, 1);
            cRT2.anchorMax = new Vector2(1, 1);
            cRT2.pivot = new Vector2(0.5f, 1);
            cRT2.sizeDelta = new Vector2(0, 0);

            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cRT2;

            // Empty State Text
            var emptyGO = new GameObject("EmptyStateText", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyGO.transform.SetParent(scrollGO.transform, false);
            var eRT = emptyGO.GetComponent<RectTransform>();
            eRT.anchorMin = Vector2.zero;
            eRT.anchorMax = Vector2.one;
            eRT.sizeDelta = Vector2.zero;
            var eTmp = emptyGO.GetComponent<TextMeshProUGUI>();
            eTmp.text = "<color=#64748B><i>No saved games found yet.\nEnter a name above and click <b>Save New</b> to record your progress!</i></color>";
            eTmp.fontSize = 18;
            eTmp.alignment = TextAlignmentOptions.Center;
            emptyStateText = emptyGO;
            emptyStateText.SetActive(false);

            // 7. Reusable Confirmation Dialog
            BuildConfirmDialog(cardGO.transform);

            modalPanel.SetActive(false);
        }

        private void BuildConfirmDialog(Transform parent)
        {
            confirmDialogRoot = new GameObject("ConfirmDialog", typeof(RectTransform), typeof(Image));
            confirmDialogRoot.transform.SetParent(parent, false);

            var rt = confirmDialogRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var img = confirmDialogRoot.GetComponent<Image>();
            img.color = new Color(0.02f, 0.03f, 0.05f, 0.88f); // Dark overlay

            // Card inside confirm dialog
            var boxGO = new GameObject("ConfirmBox", typeof(RectTransform), typeof(Image));
            boxGO.transform.SetParent(confirmDialogRoot.transform, false);
            var bRT = boxGO.GetComponent<RectTransform>();
            bRT.sizeDelta = new Vector2(560, 300);
            bRT.anchoredPosition = Vector2.zero;
            var bImg = boxGO.GetComponent<Image>();
            bImg.color = new Color(0.14f, 0.17f, 0.25f, 1f);

            var outline = boxGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(boxGO.transform, false);
            var tRT = titleGO.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 1);
            tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.sizeDelta = new Vector2(-36, 48);
            tRT.anchoredPosition = new Vector2(0, -18);
            confirmTitleText = titleGO.GetComponent<TextMeshProUGUI>();
            confirmTitleText.fontSize = 24;
            confirmTitleText.color = new Color(0.96f, 0.88f, 0.72f);
            confirmTitleText.alignment = TextAlignmentOptions.Center;

            // Message
            var msgGO = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            msgGO.transform.SetParent(boxGO.transform, false);
            var mRT = msgGO.GetComponent<RectTransform>();
            mRT.anchorMin = new Vector2(0, 0.5f);
            mRT.anchorMax = new Vector2(1, 0.5f);
            mRT.pivot = new Vector2(0.5f, 0.5f);
            mRT.sizeDelta = new Vector2(-48, 110);
            mRT.anchoredPosition = new Vector2(0, 8);
            confirmMessageText = msgGO.GetComponent<TextMeshProUGUI>();
            confirmMessageText.fontSize = 17;
            confirmMessageText.color = Color.white;
            confirmMessageText.alignment = TextAlignmentOptions.Center;

            // Buttons Container
            var btnBar = new GameObject("ButtonBar", typeof(RectTransform));
            btnBar.transform.SetParent(boxGO.transform, false);
            var bbRT = btnBar.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0, 0);
            bbRT.anchorMax = new Vector2(1, 0);
            bbRT.pivot = new Vector2(0.5f, 0);
            bbRT.sizeDelta = new Vector2(-48, 52);
            bbRT.anchoredPosition = new Vector2(0, 20);

            var hlg = btnBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Cancel Button
            var cancelGO = new GameObject("Btn_Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelGO.transform.SetParent(btnBar.transform, false);
            var cImg = cancelGO.GetComponent<Image>();
            cImg.color = new Color(0.25f, 0.28f, 0.36f, 1f);
            confirmCancelButton = cancelGO.GetComponent<Button>();
            confirmCancelButton.onClick.AddListener(CloseConfirmDialog);

            var cTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTxtGO.transform.SetParent(cancelGO.transform, false);
            var ctRT = cTxtGO.GetComponent<RectTransform>();
            ctRT.anchorMin = Vector2.zero;
            ctRT.anchorMax = Vector2.one;
            ctRT.sizeDelta = Vector2.zero;
            var cTmp = cTxtGO.GetComponent<TextMeshProUGUI>();
            cTmp.text = "<b>Cancel</b>";
            cTmp.fontSize = 17;
            cTmp.color = Color.white;
            cTmp.alignment = TextAlignmentOptions.Center;

            // Action Button
            var actGO = new GameObject("Btn_ConfirmAction", typeof(RectTransform), typeof(Image), typeof(Button));
            actGO.transform.SetParent(btnBar.transform, false);
            var aImg = actGO.GetComponent<Image>();
            aImg.color = new Color(0.78f, 0.22f, 0.22f, 1f);
            confirmActionButton = actGO.GetComponent<Button>();
            confirmActionButton.onClick.AddListener(OnConfirmActionExecuted);

            var aTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            aTxtGO.transform.SetParent(actGO.transform, false);
            var atRT = aTxtGO.GetComponent<RectTransform>();
            atRT.anchorMin = Vector2.zero;
            atRT.anchorMax = Vector2.one;
            atRT.sizeDelta = Vector2.zero;
            confirmActionBtnText = aTxtGO.GetComponent<TextMeshProUGUI>();
            confirmActionBtnText.text = "<b>Confirm</b>";
            confirmActionBtnText.fontSize = 17;
            confirmActionBtnText.color = Color.white;
            confirmActionBtnText.alignment = TextAlignmentOptions.Center;

            confirmDialogRoot.SetActive(false);
        }

        #endregion
    }
}
