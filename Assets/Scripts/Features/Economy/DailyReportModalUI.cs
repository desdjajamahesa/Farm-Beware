using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace FeaturesEconomy
{
    /// <summary>
    /// Morning Summary / Daily Operations Report modal UI.
    /// Displays Night Brawl results (monsters slain, combat gold) and Farm & Trade operations
    /// (crops harvested, crops sold, market revenue, wallet balance) before advancing to the new day.
    /// </summary>
    public class DailyReportModalUI : MonoBehaviour
    {
        public static DailyReportModalUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DailyReportModalUI>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var canvas = GameObject.Find("UI_Canvas");
                        if (canvas != null)
                        {
                            var go = new GameObject("DailyReportModalUI");
                            go.transform.SetParent(canvas.transform, false);
                            _instance = go.AddComponent<DailyReportModalUI>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        private static DailyReportModalUI _instance;

        private GameObject modalPanel;
        private TextMeshProUGUI headerText;
        private TextMeshProUGUI combatStatsText;
        private TextMeshProUGUI farmStatsText;
        private TextMeshProUGUI walletText;
        private Button continueButton;
        private TextMeshProUGUI continueButtonText;

        private Action onContinueCallback;
        private bool isOpen = false;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUIHierarchy();
        }

        private void Update()
        {
            if (!isOpen) return;

            // Allow pressing Space or Enter or E to continue
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                 Keyboard.current.eKey.wasPressedThisFrame))
            {
                OnContinueClicked();
            }
        }

        public void ShowReport(int completedDay, Action onContinue)
        {
            BuildUIHierarchy();
            onContinueCallback = onContinue;
            isOpen = true;

            int monsters = DailyEconomyManager.Instance != null ? DailyEconomyManager.Instance.dailyMonstersSlain : 0;
            int combatGold = DailyEconomyManager.Instance != null ? DailyEconomyManager.Instance.dailyGoldEarnedCombat : 0;
            int harvested = DailyEconomyManager.Instance != null ? DailyEconomyManager.Instance.dailyCropsHarvested : 0;
            int sold = DailyEconomyManager.Instance != null ? DailyEconomyManager.Instance.dailyCropsSold : 0;
            int tradeGold = DailyEconomyManager.Instance != null ? DailyEconomyManager.Instance.dailyGoldEarnedTrading : 0;
            int totalWallet = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;

            if (headerText != null)
                headerText.text = $"☀️ DAY {completedDay} COMPLETE";

            if (combatStatsText != null)
                combatStatsText.text = $"• Monsters Slain: <b>{monsters}</b>\n• Combat Gold Looted: <b>+{combatGold:N0} G</b>";

            if (farmStatsText != null)
                farmStatsText.text = $"• Crops Harvested: <b>{harvested}</b>\n• Crops Sold: <b>{sold}</b>\n• Trade Revenue: <b>+{tradeGold:N0} G</b>";

            if (walletText != null)
                walletText.text = $"Total Wallet Balance: <color=#FFD700>{totalWallet:N0} G</color>";

            if (continueButtonText != null)
                continueButtonText.text = $"Begin Day {completedDay + 1} ➔";

            if (modalPanel != null)
                modalPanel.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var pc = FindFirstObjectByType<PlayerControl>();
            if (pc != null)
            {
                pc.StopMovement();
                pc.isInputLocked = true;
            }
        }

        private void OnContinueClicked()
        {
            isOpen = false;
            if (modalPanel != null)
                modalPanel.SetActive(false);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            var pc = FindFirstObjectByType<PlayerControl>();
            if (pc != null)
                pc.isInputLocked = false;

            onContinueCallback?.Invoke();
            onContinueCallback = null;
        }

        private void BuildUIHierarchy()
        {
            if (modalPanel != null) return;

            Transform canvasTransform = transform.parent;
            if (canvasTransform == null || canvasTransform.GetComponent<Canvas>() == null)
            {
                var uiCanvas = GameObject.Find("UI_Canvas");
                if (uiCanvas != null)
                {
                    transform.SetParent(uiCanvas.transform, false);
                    canvasTransform = uiCanvas.transform;
                }
            }

            // 1. Root Modal Panel
            modalPanel = new GameObject("Panel_DailyReport", typeof(RectTransform));
            modalPanel.transform.SetParent(transform, false);
            var rootRT = modalPanel.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero;

            // 2. Backdrop Image (dim background)
            var backdropImg = modalPanel.AddComponent<Image>();
            backdropImg.color = new Color(0.04f, 0.05f, 0.08f, 0.82f);
            backdropImg.raycastTarget = true;

            // 3. Card Dialog Container
            GameObject cardGO = new GameObject("Card_Dialog", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(modalPanel.transform, false);
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(520f, 440f);
            cardRT.anchoredPosition = Vector2.zero;

            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(0.10f, 0.12f, 0.18f, 0.96f);

            var vLayout = cardGO.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(28, 28, 24, 24);
            vLayout.spacing = 14;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // Header Title
            GameObject titleGO = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(cardGO.transform, false);
            headerText = titleGO.GetComponent<TextMeshProUGUI>();
            headerText.text = "☀️ DAY 1 COMPLETE";
            headerText.fontSize = 24;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = new Color(1f, 0.88f, 0.35f, 1f);
            var titleElem = titleGO.AddComponent<LayoutElement>();
            titleElem.preferredHeight = 32;

            // Subtitle
            GameObject subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(cardGO.transform, false);
            var subTmp = subGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Morning Operations & Combat Report";
            subTmp.fontSize = 13;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.7f, 0.75f, 0.85f, 0.8f);
            var subElem = subGO.AddComponent<LayoutElement>();
            subElem.preferredHeight = 18;

            // Card A: Combat Box
            CreateStatBox(cardGO.transform, "⚔️ Night Brawl Performance", new Color(0.35f, 0.15f, 0.18f, 0.5f), out combatStatsText);

            // Card B: Farm Box
            CreateStatBox(cardGO.transform, "🌾 Farm & Trade Operations", new Color(0.12f, 0.30f, 0.22f, 0.5f), out farmStatsText);

            // Total Net Worth Bar
            GameObject walletGO = new GameObject("WalletText", typeof(RectTransform), typeof(TextMeshProUGUI));
            walletGO.transform.SetParent(cardGO.transform, false);
            walletText = walletGO.GetComponent<TextMeshProUGUI>();
            walletText.text = "Total Wallet Balance: <color=#FFD700>1,250 G</color>";
            walletText.fontSize = 15;
            walletText.fontStyle = FontStyles.Bold;
            walletText.alignment = TextAlignmentOptions.Center;
            walletText.color = Color.white;
            var wallElem = walletGO.AddComponent<LayoutElement>();
            wallElem.preferredHeight = 26;

            // Continue Button
            GameObject btnGO = new GameObject("Btn_Continue", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(cardGO.transform, false);
            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(0.85f, 0.55f, 0.15f, 1f);

            continueButton = btnGO.GetComponent<Button>();
            continueButton.onClick.AddListener(OnContinueClicked);

            ColorBlock colors = continueButton.colors;
            colors.normalColor = new Color(0.85f, 0.55f, 0.15f, 1f);
            colors.highlightedColor = new Color(1f, 0.70f, 0.25f, 1f);
            colors.pressedColor = new Color(0.70f, 0.45f, 0.10f, 1f);
            continueButton.colors = colors;

            var btnElem = btnGO.AddComponent<LayoutElement>();
            btnElem.preferredHeight = 44;

            GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = btnTextGO.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;

            continueButtonText = btnTextGO.GetComponent<TextMeshProUGUI>();
            continueButtonText.text = "Begin Next Day ➔";
            continueButtonText.fontSize = 16;
            continueButtonText.fontStyle = FontStyles.Bold;
            continueButtonText.alignment = TextAlignmentOptions.Center;
            continueButtonText.color = Color.white;

            modalPanel.SetActive(false);
        }

        private void CreateStatBox(Transform parent, string title, Color bgColor, out TextMeshProUGUI contentTmp)
        {
            GameObject boxGO = new GameObject($"Box_{title}", typeof(RectTransform), typeof(Image));
            boxGO.transform.SetParent(parent, false);

            var img = boxGO.GetComponent<Image>();
            img.color = bgColor;

            var vBox = boxGO.AddComponent<VerticalLayoutGroup>();
            vBox.padding = new RectOffset(14, 14, 10, 10);
            vBox.spacing = 6;
            vBox.childControlWidth = true;
            vBox.childControlHeight = false;
            vBox.childForceExpandWidth = true;

            var elem = boxGO.AddComponent<LayoutElement>();
            elem.preferredHeight = 85;

            // Box Title
            GameObject titleGO = new GameObject("BoxTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(boxGO.transform, false);
            var tTmp = titleGO.GetComponent<TextMeshProUGUI>();
            tTmp.text = title;
            tTmp.fontSize = 13;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(0.9f, 0.95f, 1f, 0.9f);
            var tElem = titleGO.AddComponent<LayoutElement>();
            tElem.preferredHeight = 18;

            // Box Content
            GameObject contentGO = new GameObject("BoxContent", typeof(RectTransform), typeof(TextMeshProUGUI));
            contentGO.transform.SetParent(boxGO.transform, false);
            contentTmp = contentGO.GetComponent<TextMeshProUGUI>();
            contentTmp.text = "• Loading...";
            contentTmp.fontSize = 13;
            contentTmp.color = Color.white;
            var cElem = contentGO.AddComponent<LayoutElement>();
            cElem.preferredHeight = 44;
        }
    }
}
