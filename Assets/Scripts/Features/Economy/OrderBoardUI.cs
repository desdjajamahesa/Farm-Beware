using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FarmBeware.Core.Runtime;
using FeaturesEconomy.PureLogic;

namespace FeaturesEconomy
{
    /// <summary>
    /// 2D Screen-Space Modal UI for the HayDay-style Farm Order Board.
    /// Displays active crop requests with live inventory counts, 1.2x (+20%) bonus payouts,
    /// Deliver actions, and Skip/Refresh order cycling.
    /// </summary>
    public class OrderBoardUI : MonoBehaviour, IModalWindow
    {
        public static OrderBoardUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<OrderBoardUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set => instance = value;
        }
        private static OrderBoardUI instance;

        [Header("UI References (Auto-Built if Null)")]
        [SerializeField] private GameObject boardPanel;
        [SerializeField] private TextMeshProUGUI goldBalanceText;
        [SerializeField] private TextMeshProUGUI marketSummaryText;
        [SerializeField] private Transform ordersContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button bottomCloseButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private TextMeshProUGUI toastBannerText;

        private OrderBoardInteractable activeBoard;
        private InventoryComponent playerInventory;
        private AudioSource audioSource;
        private Coroutine toastRoutine;
        private bool isOpen;

        public bool IsOpen => isOpen;

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
            if (DailyEconomyManager.Instance != null)
            {
                DailyEconomyManager.Instance.OnMarketPricesChanged -= HandleDataChanged;
                DailyEconomyManager.Instance.OnMarketPricesChanged += HandleDataChanged;
            }
        }

        private void OnDisable()
        {
            if (DailyEconomyManager.Instance != null)
            {
                DailyEconomyManager.Instance.OnMarketPricesChanged -= HandleDataChanged;
            }
            UnsubscribeBoard();
        }

        private void OnDestroy()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnGoldChanged -= UpdateGoldDisplay;
            }
            UnsubscribeBoard();
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!isOpen) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseBoard();
            }
        }

        public void OpenBoard(OrderBoardInteractable board)
        {
            EnsureUIHierarchy();
            if (board == null) return;

            if (activeBoard != board)
            {
                UnsubscribeBoard();
                activeBoard = board;
                activeBoard.OnOrdersUpdated += HandleDataChanged;
            }

            playerInventory = ResolvePlayerInventory();
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= HandleDataChanged;
                playerInventory.OnInventoryChanged += HandleDataChanged;
            }

            var wallet = PlayerWallet.Instance ?? ServiceLocator.Resolve<IWalletService>() as PlayerWallet;
            if (wallet != null)
            {
                wallet.OnGoldChanged -= UpdateGoldDisplay;
                wallet.OnGoldChanged += UpdateGoldDisplay;
                UpdateGoldDisplay(wallet.CurrentGold);
            }

            var playerCtx = ServiceLocator.Resolve<IPlayerContext>();
            playerCtx?.AddInputLock("OrderBoardUI");

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            isOpen = true;
            ModalStackManager.Instance?.Push(this);
            if (boardPanel != null)
            {
                boardPanel.SetActive(true);
            }

            RefreshOrderList();
        }

        public void CloseBoard()
        {
            isOpen = false;
            ModalStackManager.Instance?.PopSpecific(this);
            if (boardPanel != null)
            {
                boardPanel.SetActive(false);
            }

            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= HandleDataChanged;
            }

            ServiceLocator.Resolve<IPlayerContext>()?.RemoveInputLock("OrderBoardUI");

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
        }

        void IModalWindow.OpenModal() => OpenBoard(activeBoard ?? FindFirstObjectByType<OrderBoardInteractable>());
        void IModalWindow.CloseModal() => CloseBoard();

        private void UnsubscribeBoard()
        {
            if (activeBoard != null)
            {
                activeBoard.OnOrdersUpdated -= HandleDataChanged;
            }
        }

        private void HandleDataChanged()
        {
            if (!isOpen) return;
            RefreshOrderList();
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (goldBalanceText != null)
            {
                goldBalanceText.text = $"Gold: <color=#FFD700>{gold:N0} G</color>";
            }
        }

        private void RefreshOrderList()
        {
            if (ordersContainer == null) return;
            ClearContainer(ordersContainer);
            UpdateMarketSummary();

            if (activeBoard == null || activeBoard.ActiveOrders == null || activeBoard.ActiveOrders.Count == 0)
            {
                ShowEmptyState("No agricultural orders available right now.");
                return;
            }

            playerInventory = ResolvePlayerInventory();
            IReadOnlyList<BoardOrderSlot> orders = activeBoard.ActiveOrders;
            for (int i = 0; i < orders.Count; i++)
            {
                BuildOrderRow(i, orders[i]);
            }
        }

        private void UpdateMarketSummary()
        {
            if (marketSummaryText == null) return;

            var econ = DailyEconomyManager.Instance;
            int sp = econ != null ? econ.SweetPotatoPrice : 425;
            int taro = econ != null ? econ.TaroPrice : 800;
            int corn = econ != null ? econ.CornPrice : 800;
            marketSummaryText.text =
                $"Today Clean Crop Base Rates: Sweet Potato {sp:N0}G  •  Taro {taro:N0}G  •  Corn {corn:N0}G  |  <color=#52E58A>Board Bonus: 1.2x (+20%, Clean Only)</color>";
        }

        private void BuildOrderRow(int slotIndex, BoardOrderSlot slot)
        {
            if (slot == null) return;

            ItemData item = activeBoard.ResolveOrderItem(slot);
            if (item == null) return;

            int ownedCount = playerInventory != null ? playerInventory.CountItem(item) : 0;
            bool canDeliver = ownedCount >= slot.RequiredQuantity;
            BoardOrderPriceBreakdown pricing = activeBoard.GetOrderPricing(slot);

            GameObject rowGO = new GameObject($"OrderRow_{slotIndex}_{item.itemId}", typeof(RectTransform), typeof(Image));
            rowGO.transform.SetParent(ordersContainer, false);

            var rowImg = rowGO.GetComponent<Image>();
            rowImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            rowImg.type = Image.Type.Sliced;
            rowImg.color = canDeliver
                ? new Color(0.11f, 0.18f, 0.15f, 0.95f)
                : new Color(0.12f, 0.15f, 0.21f, 0.92f);
            rowImg.raycastTarget = false;

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(14, 14, 10, 10);
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var rowElem = rowGO.AddComponent<LayoutElement>();
            rowElem.minHeight = 82f;
            rowElem.preferredHeight = 82f;
            rowElem.flexibleWidth = 1f;

            // 1. Item Icon
            CreateItemIcon(rowGO.transform, item.itemIcon);

            // 2. Request Details
            GameObject infoGO = new GameObject("RequestInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
            infoGO.transform.SetParent(rowGO.transform, false);
            var infoTmp = infoGO.GetComponent<TextMeshProUGUI>();
            string cleanBadge = "<color=#4FC3F7>[CLEAN • 1.2x]</color>";
            string progressColor = canDeliver ? "#52E58A" : "#FF8A65";
            infoTmp.text =
                $"<b>{slot.CustomerName}</b>  {cleanBadge}\n" +
                $"Request: <b>{slot.RequiredQuantity}x {item.itemName}</b>   <color={progressColor}>(Bag: {ownedCount}/{slot.RequiredQuantity})</color>\n" +
                $"<size=11><color=#9AA6B8>{slot.FlavorNote}</color></size>";
            infoTmp.fontSize = 14f;
            infoTmp.color = Color.white;
            infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
            infoTmp.raycastTarget = false;

            var infoElem = infoGO.AddComponent<LayoutElement>();
            infoElem.minWidth = 310f;
            infoElem.preferredWidth = 310f;

            // 3. Payout Column (Normal vs 1.2x Board)
            GameObject priceGO = new GameObject("PayoutInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceGO.transform.SetParent(rowGO.transform, false);
            var priceTmp = priceGO.GetComponent<TextMeshProUGUI>();
            priceTmp.text =
                $"<size=11><color=#8E9BAE>Normal: {pricing.BaseTotalPrice:N0} G</color></size>\n" +
                $"<b><color=#FFD700>{pricing.BoardTotalReward:N0} G</color></b>\n" +
                $"<size=11><color=#52E58A>+{pricing.BonusGold:N0} G (+20%)</color></size>";
            priceTmp.fontSize = 16f;
            priceTmp.color = Color.white;
            priceTmp.alignment = TextAlignmentOptions.MidlineRight;
            priceTmp.raycastTarget = false;

            var priceElem = priceGO.AddComponent<LayoutElement>();
            priceElem.minWidth = 145f;
            priceElem.preferredWidth = 145f;

            // 4. Deliver Button
            Color deliverColor = canDeliver ? new Color(0.16f, 0.62f, 0.34f, 1f) : new Color(0.24f, 0.27f, 0.32f, 0.65f);
            CreateButton(
                rowGO.transform,
                canDeliver ? "Deliver" : "Need Crops",
                110f,
                deliverColor,
                canDeliver,
                () => OnDeliverClicked(slotIndex));

            // 5. Skip / Refresh Button (HayDay-style cycle)
            CreateButton(
                rowGO.transform,
                "New",
                64f,
                new Color(0.28f, 0.33f, 0.42f, 0.95f),
                true,
                () => OnSkipClicked(slotIndex));
        }

        private void OnDeliverClicked(int slotIndex)
        {
            if (activeBoard == null) return;

            playerInventory = ResolvePlayerInventory();
            var wallet = PlayerWallet.Instance ?? ServiceLocator.Resolve<IWalletService>() as PlayerWallet;

            if (activeBoard.TryFulfillOrder(slotIndex, playerInventory, wallet, out _, out string msg))
            {
                PlayCoinSfx();
                ShowToast(msg, new Color(0.35f, 0.95f, 0.55f, 1f), true);
                if (wallet != null)
                {
                    UpdateGoldDisplay(wallet.CurrentGold);
                }
                RefreshOrderList();
            }
            else
            {
                ShowToast(msg, new Color(1f, 0.45f, 0.35f, 1f), false);
                RefreshOrderList();
            }
        }

        private void OnSkipClicked(int slotIndex)
        {
            if (activeBoard == null) return;

            if (activeBoard.TryReplaceOrder(slotIndex))
            {
                ShowToast("Requested a fresh board order slip.", new Color(0.75f, 0.88f, 1f, 1f), false);
                RefreshOrderList();
            }
        }

        private void ShowToast(string message, Color color, bool spawnWorldPopup)
        {
            if (toastBannerText != null)
            {
                toastBannerText.text = message;
                toastBannerText.color = color;
                if (toastRoutine != null) StopCoroutine(toastRoutine);
                toastRoutine = StartCoroutine(RoutineFadeToast());
            }

            if (spawnWorldPopup)
            {
                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                var playerCtx = ServiceLocator.Resolve<IPlayerContext>();
                if (fct != null && playerCtx != null)
                {
                    fct.SpawnText(playerCtx.Transform.position + Vector3.up * 1.5f, message, color);
                }
            }
        }

        private IEnumerator RoutineFadeToast()
        {
            if (toastBannerText == null) yield break;

            Color c = toastBannerText.color;
            toastBannerText.color = new Color(c.r, c.g, c.b, 1f);
            yield return new WaitForSecondsRealtime(2.2f);

            float elapsed = 0f;
            const float fadeDuration = 0.4f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (toastBannerText != null)
                {
                    toastBannerText.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
                }
                yield return null;
            }

            if (toastBannerText != null)
            {
                toastBannerText.text = string.Empty;
            }
            toastRoutine = null;
        }

        private void PlayCoinSfx()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
            audioSource.pitch = Random.Range(0.97f, 1.03f);
            audioSource.PlayOneShot(MerchantShopUI.GetSellCoinClip(), 0.75f);
        }

        private InventoryComponent ResolvePlayerInventory()
        {
            if (playerInventory != null && playerInventory.CompareTag("Player"))
                return playerInventory;

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null && playerObj.TryGetComponent<InventoryComponent>(out var inv))
                return inv;

            var ctx = ServiceLocator.Resolve<IPlayerContext>();
            return ctx != null ? ctx.GetPlayerComponent<InventoryComponent>() : null;
        }

        private void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                child.SetParent(null);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void ShowEmptyState(string message)
        {
            GameObject emptyGO = new GameObject("EmptyState", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyGO.transform.SetParent(ordersContainer, false);
            emptyGO.GetComponent<RectTransform>().sizeDelta = new Vector2(680f, 100f);

            var tmp = emptyGO.GetComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.65f, 0.72f, 0.82f, 0.85f);
            tmp.raycastTarget = false;
        }

        private static void CreateItemIcon(Transform parent, Sprite icon)
        {
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(parent, false);
            var img = iconGO.GetComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (icon == null)
            {
                img.color = new Color(0.22f, 0.27f, 0.35f, 0.8f);
            }

            var elem = iconGO.AddComponent<LayoutElement>();
            elem.minWidth = 46f;
            elem.preferredWidth = 46f;
            elem.minHeight = 46f;
            elem.preferredHeight = 46f;
        }

        private static void CreateButton(
            Transform parent,
            string label,
            float width,
            Color bgColor,
            bool interactable,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);

            var img = btnGO.GetComponent<Image>();
            img.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            img.type = Image.Type.Sliced;
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = btnGO.GetComponent<Button>();
            btn.interactable = interactable;
            btn.onClick.AddListener(onClick);

            var elem = btnGO.AddComponent<LayoutElement>();
            elem.minWidth = width;
            elem.preferredWidth = width;
            elem.minHeight = 40f;
            elem.preferredHeight = 40f;

            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var tr = textGO.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 13f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = interactable ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.7f);
            tmp.raycastTarget = false;
        }

        /// <summary>
        /// Builds the Panel_OrderBoard hierarchy under UI_Canvas if not already present.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            if (boardPanel != null)
            {
                BindButtons();
                return;
            }

            Canvas screenCanvas = FindOverlayCanvas();
            if (screenCanvas == null) return;

            Transform existing = screenCanvas.transform.Find("Panel_OrderBoard");
            if (existing != null)
            {
                boardPanel = existing.gameObject;
                ResolveChildReferences();
                BindButtons();
                return;
            }

            boardPanel = new GameObject("Panel_OrderBoard", typeof(RectTransform));
            boardPanel.transform.SetParent(screenCanvas.transform, false);
            var rootRT = boardPanel.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero;

            // Backdrop dimmer
            GameObject backdropGO = new GameObject("BackdropDimmer", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGO.transform.SetParent(boardPanel.transform, false);
            var bdRT = backdropGO.GetComponent<RectTransform>();
            bdRT.anchorMin = Vector2.zero;
            bdRT.anchorMax = Vector2.one;
            bdRT.sizeDelta = Vector2.zero;
            backdropGO.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.78f);
            backdropButton = backdropGO.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            // Modal card
            GameObject cardGO = new GameObject("OrderBoardModalCard", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(boardPanel.transform, false);
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(820f, 590f);
            var cardImg = cardGO.GetComponent<Image>();
            cardImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            // Header bar
            GameObject headerGO = CreateAnchoredBar(cardGO.transform, "HeaderBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-40f, 58f));
            CreateTMP(headerGO.transform, "TitleText", "FARM ORDER BOARD", 24f, FontStyles.Bold, new Color(1f, 0.82f, 0.25f, 1f), TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.45f), new Vector2(0.85f, 1f));
            CreateTMP(headerGO.transform, "SubtitleText", "Complete washed clean harvest requests outside the fence for 1.2x (+20%) bonus Gold!", 13f, FontStyles.Normal, new Color(0.66f, 0.74f, 0.84f, 1f), TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(0.85f, 0.45f));

            // Close X button
            GameObject xGO = new GameObject("Btn_XClose", typeof(RectTransform), typeof(Image), typeof(Button));
            xGO.transform.SetParent(headerGO.transform, false);
            var xRT = xGO.GetComponent<RectTransform>();
            xRT.anchorMin = new Vector2(1f, 0.5f);
            xRT.anchorMax = new Vector2(1f, 0.5f);
            xRT.pivot = new Vector2(1f, 0.5f);
            xRT.sizeDelta = new Vector2(36f, 36f);
            xGO.GetComponent<Image>().color = new Color(0.26f, 0.14f, 0.18f, 1f);
            closeButton = xGO.GetComponent<Button>();
            CreateTMP(xGO.transform, "Text", "X", 17f, FontStyles.Bold, new Color(1f, 0.45f, 0.45f, 1f), TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            // Gold pill
            GameObject goldPill = CreateAnchoredBar(cardGO.transform, "GoldPill", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(-40f, 32f), new Color(0.14f, 0.18f, 0.26f, 0.9f));
            goldBalanceText = CreateTMP(goldPill.transform, "GoldText", "Gold: <color=#FFD700>0 G</color>", 15f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, new Vector2(-20f, 0f));

            // Market summary strip
            GameObject ratePill = CreateAnchoredBar(cardGO.transform, "MarketRatePill", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -114f), new Vector2(-40f, 28f), new Color(0.10f, 0.14f, 0.20f, 0.9f));
            marketSummaryText = CreateTMP(ratePill.transform, "RateSummary", "", 12f, FontStyles.Normal, new Color(0.84f, 0.90f, 0.96f, 1f), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(-16f, 0f));

            // Scroll view
            GameObject scrollGO = new GameObject("OrderScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(cardGO.transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(20f, 82f);
            srt.offsetMax = new Vector2(-20f, -150f);
            scrollGO.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.11f, 0.85f);

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 25f;

            GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            scroll.viewport = vpRT;

            GameObject contentGO = new GameObject("OrdersContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(vpGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = Vector2.zero;

            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ordersContainer = contentGO.transform;
            scroll.content = contentRT;

            // Toast text
            GameObject toastGO = new GameObject("ToastBannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            toastGO.transform.SetParent(cardGO.transform, false);
            var tRT = toastGO.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0f, 0f);
            tRT.anchorMax = new Vector2(1f, 0f);
            tRT.pivot = new Vector2(0.5f, 0f);
            tRT.anchoredPosition = new Vector2(0f, 52f);
            tRT.sizeDelta = new Vector2(-40f, 24f);
            toastBannerText = toastGO.GetComponent<TextMeshProUGUI>();
            toastBannerText.fontSize = 14f;
            toastBannerText.fontStyle = FontStyles.Bold;
            toastBannerText.alignment = TextAlignmentOptions.Center;
            toastBannerText.raycastTarget = false;
            toastBannerText.text = string.Empty;

            // Bottom close bar
            GameObject bottomGO = CreateAnchoredBar(cardGO.transform, "BottomBar", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-40f, 36f));
            GameObject closeBtnGO = new GameObject("Btn_CloseBoard", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGO.transform.SetParent(bottomGO.transform, false);
            var cbRT = closeBtnGO.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(0.35f, 0f);
            cbRT.anchorMax = new Vector2(0.65f, 1f);
            cbRT.sizeDelta = Vector2.zero;
            closeBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 1f);
            bottomCloseButton = closeBtnGO.GetComponent<Button>();
            CreateTMP(closeBtnGO.transform, "Text", "CLOSE BOARD (ESC)", 14f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            BindButtons();
            boardPanel.SetActive(false);
        }

        private void ResolveChildReferences()
        {
            if (boardPanel == null) return;
            Transform card = boardPanel.transform.Find("OrderBoardModalCard");
            backdropButton ??= boardPanel.transform.Find("BackdropDimmer")?.GetComponent<Button>();
            if (card == null) return;

            closeButton ??= card.Find("HeaderBar/Btn_XClose")?.GetComponent<Button>();
            bottomCloseButton ??= card.Find("BottomBar/Btn_CloseBoard")?.GetComponent<Button>();
            goldBalanceText ??= card.Find("GoldPill/GoldText")?.GetComponent<TextMeshProUGUI>();
            marketSummaryText ??= card.Find("MarketRatePill/RateSummary")?.GetComponent<TextMeshProUGUI>();
            toastBannerText ??= card.Find("ToastBannerText")?.GetComponent<TextMeshProUGUI>();
            ordersContainer ??= card.Find("OrderScrollView/Viewport/OrdersContent");
        }

        private void BindButtons()
        {
            BindClose(closeButton);
            BindClose(bottomCloseButton);
            BindClose(backdropButton);
        }

        private void BindClose(Button btn)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(CloseBoard);
        }

        private static GameObject CreateAnchoredBar(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            Color? bgColor = null)
        {
            GameObject go = bgColor.HasValue
                ? new GameObject(name, typeof(RectTransform), typeof(Image))
                : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            if (bgColor.HasValue)
            {
                var img = go.GetComponent<Image>();
                img.color = bgColor.Value;
                img.raycastTarget = false;
            }
            return go;
        }

        private static TextMeshProUGUI CreateTMP(
            Transform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions align,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2? sizeDelta = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta ?? Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Canvas FindOverlayCanvas()
        {
            var uiCanvasGO = GameObject.Find("UI_Canvas");
            if (uiCanvasGO != null && uiCanvasGO.TryGetComponent<Canvas>(out var c) && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }

            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cv in allCanvases)
            {
                if (cv != null && cv.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return cv;
                }
            }
            return null;
        }
    }
}
