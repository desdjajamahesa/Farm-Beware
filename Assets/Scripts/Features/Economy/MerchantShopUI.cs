using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace FeaturesEconomy
{
    /// <summary>
    /// Merchant Shop 2D Modal UI manager providing responsive Buy (Seeds) and Sell (Crops/Produce) interfaces.
    /// Perfectly anchored to the player's 2D Screen Space Overlay Canvas (UI_Canvas), centered with a dark backdrop,
    /// ensuring zero-lag in-place UI updates, atomic transactions, reliable close triggers, and crisp responsive button feedback.
    /// </summary>
    public class MerchantShopUI : MonoBehaviour
    {
        public static MerchantShopUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<MerchantShopUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set => instance = value;
        }
        private static MerchantShopUI instance;

        [Header("Shop Inventory (Seeds for Sale)")]
        [SerializeField] private List<ItemData> itemsForSale = new List<ItemData>();

        [Header("UI References (Optional - dynamically bound if null)")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private TextMeshProUGUI goldBalanceText;
        [SerializeField] private Transform buyContentContainer;
        [SerializeField] private Transform sellContentContainer;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private Image buyTabImage;
        [SerializeField] private Image sellTabImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button bottomCloseButton;
        [SerializeField] private Button backdropButton;

        private bool isOpen = false;
        public bool IsOpen => isOpen;

        public enum ShopTab { Buy, Sell }
        private ShopTab currentTab = ShopTab.Buy;

        private InventoryComponent playerInventory;
        private PlayerControl playerControl;

        private readonly Color activeTabColor = new Color(0.12f, 0.35f, 0.65f, 1f); // Vibrant Blue
        private readonly Color inactiveTabColor = new Color(0.12f, 0.16f, 0.22f, 1f); // Muted Dark

        // Cached Buy Rows for zero-allocation in-place updates
        private class BuyRowView
        {
            public ItemData item;
            public GameObject rowGO;
            public Button btnBuy1;
            public TextMeshProUGUI txtBuy1;
            public Image imgBuy1;
            public Button btnBuy5;
            public TextMeshProUGUI txtBuy5;
            public Image imgBuy5;
        }
        private readonly List<BuyRowView> cachedBuyRows = new List<BuyRowView>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            LoadSeedCatalog();
            EnsureUIHierarchy();
            ResolveReferencesAndBind();
        }

        private void OnEnable()
        {
            ResolveReferencesAndBind();
        }

        private void Start()
        {
            ResolveReferencesAndBind();

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnGoldChanged -= UpdateGoldDisplay;
                PlayerWallet.Instance.OnGoldChanged += UpdateGoldDisplay;
            }
        }

        private void OnDestroy()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnGoldChanged -= UpdateGoldDisplay;
            }
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!isOpen) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseShop();
            }
        }

        private void LoadSeedCatalog()
        {
            if (itemsForSale != null && itemsForSale.Count > 0) return;

            itemsForSale = new List<ItemData>();
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item != null && item.category == ItemCategory.Seed && !itemsForSale.Contains(item))
                {
                    itemsForSale.Add(item);
                }
            }

#if UNITY_EDITOR
            if (itemsForSale.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (item != null && item.category == ItemCategory.Seed && !itemsForSale.Contains(item))
                    {
                        itemsForSale.Add(item);
                    }
                }
            }
#endif
        }

        public void OpenShop(List<ItemData> customStock = null)
        {
            ResolveReferencesAndBind();

            if (customStock != null && customStock.Count > 0)
            {
                itemsForSale = customStock;
            }
            else if (itemsForSale == null || itemsForSale.Count == 0)
            {
                LoadSeedCatalog();
            }

            playerInventory = GetPlayerInventory();
            if (playerControl == null)
                playerControl = FindFirstObjectByType<PlayerControl>();

            if (playerControl != null)
                playerControl.isInputLocked = true;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            isOpen = true;
            if (shopPanel != null)
                shopPanel.SetActive(true);

            int currentGold = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;
            UpdateGoldDisplay(currentGold);
            SwitchTab(ShopTab.Buy);
        }

        public void CloseShop()
        {
            isOpen = false;
            if (shopPanel != null)
                shopPanel.SetActive(false);

            if (playerControl == null)
                playerControl = FindFirstObjectByType<PlayerControl>();

            if (playerControl != null)
                playerControl.isInputLocked = false;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // Signal MainMenuController that a UI panel just closed to avoid accidental pause menu trigger
            MainMenuController.LastFrameUIPanelClosed = Time.frameCount;
        }

        public void SwitchTab(ShopTab tab)
        {
            currentTab = tab;

            if (buyTabImage != null)
                buyTabImage.color = (currentTab == ShopTab.Buy) ? activeTabColor : inactiveTabColor;
            if (sellTabImage != null)
                sellTabImage.color = (currentTab == ShopTab.Sell) ? activeTabColor : inactiveTabColor;

            if (buyContentContainer != null)
                buyContentContainer.gameObject.SetActive(currentTab == ShopTab.Buy);
            if (sellContentContainer != null)
                sellContentContainer.gameObject.SetActive(currentTab == ShopTab.Sell);

            if (scrollRect != null)
            {
                scrollRect.content = (currentTab == ShopTab.Buy)
                    ? buyContentContainer as RectTransform
                    : sellContentContainer as RectTransform;
            }

            if (currentTab == ShopTab.Buy)
                RefreshBuyList();
            else
                RefreshSellList();
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (goldBalanceText != null)
                goldBalanceText.text = $"Gold: <color=#FFD700>{gold:N0} G</color>";
        }

        /// <summary>
        /// Populates Buy items once or re-validates affordability in-place without destroying UI objects.
        /// </summary>
        private void RefreshBuyList()
        {
            if (buyContentContainer == null) return;

            // If rows are already built for the current catalog, just update affordability in-place!
            if (cachedBuyRows.Count > 0 && cachedBuyRows.Count == itemsForSale.Count)
            {
                UpdateBuyButtonStates();
                return;
            }

            // Otherwise, cleanly reconstruct rows once
            ClearContainer(buyContentContainer);
            cachedBuyRows.Clear();

            if (itemsForSale == null || itemsForSale.Count == 0)
            {
                ShowEmptyState(buyContentContainer, "No seeds available in merchant stock.");
                return;
            }

            int playerGold = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;

            foreach (var item in itemsForSale)
            {
                if (item == null) continue;
                BuildBuyRow(buyContentContainer, item, playerGold);
            }
        }

        /// <summary>
        /// Updates button interactability and text colors in-place without rebuilding UI hierarchy.
        /// </summary>
        private void UpdateBuyButtonStates()
        {
            int playerGold = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;

            foreach (var row in cachedBuyRows)
            {
                if (row == null || row.item == null) continue;

                // Buy 1
                bool canAfford1 = (playerGold >= row.item.buyPrice);
                if (row.btnBuy1 != null)
                {
                    row.btnBuy1.interactable = canAfford1;
                }
                if (row.txtBuy1 != null)
                {
                    row.txtBuy1.text = canAfford1 ? "Buy 1" : "No Gold";
                    row.txtBuy1.color = canAfford1 ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.6f);
                }
                if (row.imgBuy1 != null)
                {
                    row.imgBuy1.color = canAfford1 ? new Color(0.14f, 0.58f, 0.32f, 1f) : new Color(0.24f, 0.26f, 0.3f, 0.5f);
                }

                // Buy 5
                int cost5 = row.item.buyPrice * 5;
                bool canAfford5 = (playerGold >= cost5);
                if (row.btnBuy5 != null)
                {
                    row.btnBuy5.interactable = canAfford5;
                }
                if (row.txtBuy5 != null)
                {
                    row.txtBuy5.text = canAfford5 ? "Buy 5" : $"Buy 5 ({cost5}G)";
                    row.txtBuy5.color = canAfford5 ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.6f);
                }
                if (row.imgBuy5 != null)
                {
                    row.imgBuy5.color = canAfford5 ? new Color(0.12f, 0.48f, 0.28f, 1f) : new Color(0.24f, 0.26f, 0.3f, 0.5f);
                }
            }
        }

        private void BuildBuyRow(Transform parent, ItemData item, int playerGold)
        {
            GameObject rowGO = new GameObject($"BuyRow_{item.itemName}", typeof(RectTransform), typeof(Image));
            rowGO.transform.SetParent(parent, false);

            var rowImg = rowGO.GetComponent<Image>();
            rowImg.color = new Color(0.11f, 0.14f, 0.2f, 0.9f);
            rowImg.raycastTarget = false;

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 8, 8);
            rowLayout.spacing = 14;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var rowElem = rowGO.AddComponent<LayoutElement>();
            rowElem.minHeight = 56;
            rowElem.preferredHeight = 56;
            rowElem.flexibleWidth = 1;

            // 1. Icon
            CreateItemIcon(rowGO.transform, item.itemIcon);

            // 2. Info Text (Name, Description, Price)
            GameObject infoGO = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
            infoGO.transform.SetParent(rowGO.transform, false);
            var infoTmp = infoGO.GetComponent<TextMeshProUGUI>();
            infoTmp.text = $"<b>{item.itemName}</b>\n<color=#FFD700>{item.buyPrice:N0} G</color>  <size=12><color=#8B949E>{item.description}</color></size>";
            infoTmp.fontSize = 15;
            infoTmp.color = Color.white;
            infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
            infoTmp.raycastTarget = false;
            var infoElem = infoGO.AddComponent<LayoutElement>();
            infoElem.minWidth = 320;
            infoElem.preferredWidth = 320;

            // 3. Buy 1 Button
            bool canAfford1 = (playerGold >= item.buyPrice);
            var b1 = CreateResponsiveButton(rowGO.transform, canAfford1 ? "Buy 1" : "No Gold", 85,
                canAfford1 ? new Color(0.14f, 0.58f, 0.32f, 1f) : new Color(0.24f, 0.26f, 0.3f, 0.5f),
                canAfford1,
                () => TryBuyItem(item, 1));

            // 4. Buy 5 Button
            int cost5 = item.buyPrice * 5;
            bool canAfford5 = (playerGold >= cost5);
            var b5 = CreateResponsiveButton(rowGO.transform, canAfford5 ? "Buy 5" : $"Buy 5 ({cost5}G)", 105,
                canAfford5 ? new Color(0.12f, 0.48f, 0.28f, 1f) : new Color(0.24f, 0.26f, 0.3f, 0.5f),
                canAfford5,
                () => TryBuyItem(item, 5));

            cachedBuyRows.Add(new BuyRowView
            {
                item = item,
                rowGO = rowGO,
                btnBuy1 = b1.btn,
                txtBuy1 = b1.tmp,
                imgBuy1 = b1.img,
                btnBuy5 = b5.btn,
                txtBuy5 = b5.tmp,
                imgBuy5 = b5.img
            });
        }

        /// <summary>
        /// Refreshes the Sell tab listing sellable crops and produce from the player's backpack.
        /// </summary>
        private void RefreshSellList()
        {
            if (sellContentContainer == null) return;

            ClearContainer(sellContentContainer);

            playerInventory = GetPlayerInventory();
            if (playerInventory == null || playerInventory.slots == null)
            {
                ShowEmptyState(sellContentContainer, "Backpack inventory could not be loaded.");
                return;
            }

            // Group sellable items from backpack (any item with sellPrice > 0)
            Dictionary<ItemData, int> sellableItems = new Dictionary<ItemData, int>();
            foreach (var slot in playerInventory.slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item != null && slot.item.sellPrice > 0)
                {
                    if (sellableItems.ContainsKey(slot.item))
                        sellableItems[slot.item] += slot.quantity;
                    else
                        sellableItems[slot.item] = slot.quantity;
                }
            }

            if (sellableItems.Count == 0)
            {
                ShowEmptyState(sellContentContainer, "No harvest or produce in backpack to sell.\nGrow seeds on the farm and harvest them first!");
                return;
            }

            foreach (var kvp in sellableItems)
            {
                CreateSellRow(sellContentContainer, kvp.Key, kvp.Value);
            }
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;
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

        private void ShowEmptyState(Transform parent, string message)
        {
            GameObject emptyGO = new GameObject("EmptyState", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyGO.transform.SetParent(parent, false);
            var rect = emptyGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(620, 100);

            var tmp = emptyGO.GetComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.68f, 0.78f, 0.8f);
            tmp.raycastTarget = false;
        }

        private void CreateSellRow(Transform parent, ItemData item, int ownedCount)
        {
            GameObject rowGO = new GameObject($"SellRow_{item.itemName}", typeof(RectTransform), typeof(Image));
            rowGO.transform.SetParent(parent, false);

            var rowImg = rowGO.GetComponent<Image>();
            rowImg.color = new Color(0.11f, 0.14f, 0.2f, 0.9f);
            rowImg.raycastTarget = false;

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 8, 8);
            rowLayout.spacing = 14;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var rowElem = rowGO.AddComponent<LayoutElement>();
            rowElem.minHeight = 56;
            rowElem.preferredHeight = 56;
            rowElem.flexibleWidth = 1;

            // 1. Icon
            CreateItemIcon(rowGO.transform, item.itemIcon);

            // 2. Info Text (Name, Owned Count, Unit Price)
            GameObject infoGO = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
            infoGO.transform.SetParent(rowGO.transform, false);
            var infoTmp = infoGO.GetComponent<TextMeshProUGUI>();
            infoTmp.text = $"<b>{item.itemName}</b>  <color=#00E5FF>(x{ownedCount} in bag)</color>\n<color=#98FB98>+{item.sellPrice:N0} G each</color>";
            infoTmp.fontSize = 15;
            infoTmp.color = Color.white;
            infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
            infoTmp.raycastTarget = false;
            var infoElem = infoGO.AddComponent<LayoutElement>();
            infoElem.minWidth = 300;
            infoElem.preferredWidth = 300;

            // 3. Sell 1 Button
            CreateResponsiveButton(rowGO.transform, "Sell 1", 90, new Color(0.75f, 0.45f, 0.15f, 1f), true, () =>
            {
                TrySellItem(item, 1);
            });

            // 4. Sell All Button
            int totalGain = item.sellPrice * ownedCount;
            CreateResponsiveButton(rowGO.transform, $"Sell All (+{totalGain:N0}G)", 150, new Color(0.85f, 0.38f, 0.15f, 1f), true, () =>
            {
                TrySellItem(item, ownedCount);
            });
        }

        private void CreateItemIcon(Transform parent, Sprite icon)
        {
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(parent, false);
            var img = iconGO.GetComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (icon == null)
            {
                img.color = new Color(0.2f, 0.25f, 0.35f, 0.8f);
            }
            var elem = iconGO.AddComponent<LayoutElement>();
            elem.minWidth = 42;
            elem.preferredWidth = 42;
            elem.minHeight = 42;
            elem.preferredHeight = 42;
        }

        private struct ButtonResult
        {
            public Button btn;
            public Image img;
            public TextMeshProUGUI tmp;
        }

        private ButtonResult CreateResponsiveButton(Transform parent, string label, float width, Color bgColor, bool interactable, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var img = btnGO.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = btnGO.GetComponent<Button>();
            btn.interactable = interactable;
            btn.transition = Selectable.Transition.ColorTint;

            // Rich button feedback colors
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            colors.selectedColor = bgColor;
            colors.disabledColor = new Color(0.24f, 0.26f, 0.3f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f; // Snappy transition
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            var elem = btnGO.AddComponent<LayoutElement>();
            elem.minWidth = width;
            elem.preferredWidth = width;
            elem.minHeight = 36;
            elem.preferredHeight = 36;

            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = interactable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.6f);
            tmp.raycastTarget = false;

            return new ButtonResult { btn = btn, img = img, tmp = tmp };
        }

        /// <summary>
        /// Executes an atomic, strictly verified buy transaction.
        /// Currency is deducted FIRST, with automatic refund if inventory is full.
        /// </summary>
        private void TryBuyItem(ItemData item, int count)
        {
            if (item == null || count <= 0) return;
            if (PlayerWallet.Instance == null) return;

            playerInventory = GetPlayerInventory();
            if (playerInventory == null) return;

            int totalCost = item.buyPrice * count;

            // 1. Strict affordability check
            if (!PlayerWallet.Instance.CanAfford(totalCost))
            {
                ShowFloatingNotify("Not enough Gold!", new Color(1f, 0.3f, 0.3f));
                UpdateBuyButtonStates();
                return;
            }

            // 2. Spend currency FIRST
            bool spent = PlayerWallet.Instance.SpendGold(totalCost);
            if (!spent)
            {
                ShowFloatingNotify("Transaction failed! Insufficient funds.", new Color(1f, 0.3f, 0.3f));
                UpdateBuyButtonStates();
                return;
            }

            // 3. Add to inventory
            bool added = playerInventory.AddItem(item, count);
            if (!added)
            {
                // Automatic refund if inventory is full
                PlayerWallet.Instance.AddGold(totalCost);
                ShowFloatingNotify("Backpack is full! Gold refunded.", new Color(1f, 0.6f, 0.2f));
                UpdateBuyButtonStates();
                return;
            }

            ShowFloatingNotify($"-{totalCost:N0} G (Bought {count}x {item.itemName})", new Color(0.2f, 0.95f, 0.5f));
            UpdateGoldDisplay(PlayerWallet.Instance.CurrentGold);
            UpdateBuyButtonStates();
        }

        /// <summary>
        /// Executes an atomic sell transaction. Deducts item from player backpack, adds gold to wallet, and refreshes UI.
        /// </summary>
        private void TrySellItem(ItemData item, int count)
        {
            if (item == null || count <= 0) return;
            if (PlayerWallet.Instance == null) return;

            playerInventory = GetPlayerInventory();
            if (playerInventory == null) return;

            int owned = playerInventory.CountItem(item);
            int sellCount = Mathf.Min(count, owned);
            if (sellCount <= 0) return;

            int earnedGold = item.sellPrice * sellCount;
            bool removed = playerInventory.RemoveItem(item, sellCount);
            if (removed)
            {
                PlayerWallet.Instance.AddGold(earnedGold);
                ShowFloatingNotify($"+{earnedGold:N0} G (Sold {sellCount}x {item.itemName})", new Color(1f, 0.85f, 0.2f));
                UpdateGoldDisplay(PlayerWallet.Instance.CurrentGold);
                RefreshSellList();
            }
        }

        /// <summary>
        /// Reliably resolves the player's active InventoryComponent, preventing accidental association with chests or storage.
        /// </summary>
        public InventoryComponent GetPlayerInventory()
        {
            if (playerInventory != null && (playerInventory.CompareTag("Player") || playerInventory.GetComponent<PlayerControl>() != null))
                return playerInventory;

            var player = GameObject.FindWithTag("Player");
            if (player != null && player.TryGetComponent<InventoryComponent>(out var inv))
            {
                playerInventory = inv;
                return playerInventory;
            }

            var pc = FindFirstObjectByType<PlayerControl>();
            if (pc != null && pc.TryGetComponent<InventoryComponent>(out inv))
            {
                playerInventory = inv;
                return playerInventory;
            }

            var allInvs = FindObjectsByType<InventoryComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in allInvs)
            {
                if (candidate.CompareTag("Player") || candidate.GetComponent<PlayerControl>() != null)
                {
                    playerInventory = candidate;
                    return playerInventory;
                }
            }

            return null;
        }

        private void ShowFloatingNotify(string msg, Color col)
        {
            if (PlayerUI.FloatingCombatTextManager.Instance != null && playerControl != null)
            {
                PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                    playerControl.transform.position + Vector3.up * 1.5f,
                    msg,
                    col);
            }
        }

        /// <summary>
        /// Resolves all internal UI components and binds runtime onClick listeners.
        /// Guaranteed to execute whether the UI was generated in code or loaded from a saved scene.
        /// </summary>
        public void ResolveReferencesAndBind()
        {
            if (shopPanel == null)
            {
                var uiCanvasGO = GameObject.Find("UI_Canvas");
                if (uiCanvasGO != null)
                {
                    var tr = uiCanvasGO.transform.Find("Panel_MerchantShop");
                    if (tr != null) shopPanel = tr.gameObject;
                }

                if (shopPanel == null)
                {
                    var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var c in canvases)
                    {
                        var tr = c.transform.Find("Panel_MerchantShop");
                        if (tr != null)
                        {
                            shopPanel = tr.gameObject;
                            break;
                        }
                    }
                }
            }

            if (shopPanel == null) return;

            var cardTr = shopPanel.transform.Find("ShopModalCard");
            if (cardTr == null) return;

            // 1. Close [X] Button
            if (closeButton == null)
            {
                var tr = cardTr.Find("HeaderBar/Btn_XClose");
                if (tr != null) closeButton = tr.GetComponent<Button>();
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseShop);
            }

            // 2. Bottom Close Button
            if (bottomCloseButton == null)
            {
                var tr = cardTr.Find("BottomBar/Btn_CloseShop");
                if (tr != null) bottomCloseButton = tr.GetComponent<Button>();
            }
            if (bottomCloseButton != null)
            {
                bottomCloseButton.onClick.RemoveAllListeners();
                bottomCloseButton.onClick.AddListener(CloseShop);
            }

            // 3. Backdrop Dimmer Button
            if (backdropButton == null)
            {
                var tr = shopPanel.transform.Find("BackdropDimmer");
                if (tr != null) backdropButton = tr.GetComponent<Button>();
            }
            if (backdropButton != null)
            {
                backdropButton.onClick.RemoveAllListeners();
                backdropButton.onClick.AddListener(CloseShop);
            }

            // 4. Buy Tab Button
            if (buyTabButton == null)
            {
                var tr = cardTr.Find("TabsBar/Tab_Buy");
                if (tr != null) buyTabButton = tr.GetComponent<Button>();
            }
            if (buyTabButton != null)
            {
                buyTabImage = buyTabButton.GetComponent<Image>();
                buyTabButton.onClick.RemoveAllListeners();
                buyTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Buy));
            }

            // 5. Sell Tab Button
            if (sellTabButton == null)
            {
                var tr = cardTr.Find("TabsBar/Tab_Sell");
                if (tr != null) sellTabButton = tr.GetComponent<Button>();
            }
            if (sellTabButton != null)
            {
                sellTabImage = sellTabButton.GetComponent<Image>();
                sellTabButton.onClick.RemoveAllListeners();
                sellTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Sell));
            }

            // 6. Gold Text
            if (goldBalanceText == null)
            {
                var tr = cardTr.Find("GoldPill/GoldText");
                if (tr != null) goldBalanceText = tr.GetComponent<TextMeshProUGUI>();
            }

            // 7. ScrollRect & Containers
            if (scrollRect == null)
            {
                var tr = cardTr.Find("ItemScrollView");
                if (tr != null) scrollRect = tr.GetComponent<ScrollRect>();
            }
            if (scrollRect != null && scrollRect.viewport != null)
            {
                if (buyContentContainer == null)
                    buyContentContainer = scrollRect.viewport.Find("BuyContent");
                if (sellContentContainer == null)
                    sellContentContainer = scrollRect.viewport.Find("SellContent");
            }
        }

        /// <summary>
        /// Constructs a premium 2D Modal Screen-Space shop dialog under the player's UI_Canvas.
        /// Separates BackdropDimmer from ShopModalCard so internal card clicks NEVER hit the backdrop.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            if (shopPanel != null)
            {
                ResolveReferencesAndBind();
                return;
            }

            Canvas screenCanvas = null;
            var uiCanvasGO = GameObject.Find("UI_Canvas");
            if (uiCanvasGO != null && uiCanvasGO.TryGetComponent<Canvas>(out var c) && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenCanvas = c;
            }

            if (screenCanvas == null)
            {
                var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var canvas in allCanvases)
                {
                    if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        screenCanvas = canvas;
                        break;
                    }
                }
            }

            if (screenCanvas == null) return;

            // 1. Root Container (non-raycasting)
            shopPanel = new GameObject("Panel_MerchantShop", typeof(RectTransform));
            shopPanel.transform.SetParent(screenCanvas.transform, false);

            var rootRect = shopPanel.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;

            // 2. Fullscreen Dimming Backdrop (Sibling 0 - BEHIND modal card)
            GameObject backdropGO = new GameObject("BackdropDimmer", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGO.transform.SetParent(shopPanel.transform, false);
            var backdropRect = backdropGO.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.sizeDelta = Vector2.zero;

            var backdropImg = backdropGO.GetComponent<Image>();
            backdropImg.color = new Color(0.02f, 0.04f, 0.07f, 0.78f);
            backdropImg.raycastTarget = true;

            backdropButton = backdropGO.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            // 3. Centered Modal Dialog Card (Sibling 1 - IN FRONT of backdrop)
            GameObject cardGO = new GameObject("ShopModalCard", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(shopPanel.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(740, 580);
            cardRect.anchoredPosition = Vector2.zero;

            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);
            cardImg.raycastTarget = true;

            // 4. Header Bar
            GameObject headerGO = new GameObject("HeaderBar", typeof(RectTransform));
            headerGO.transform.SetParent(cardGO.transform, false);
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -12);
            headerRect.sizeDelta = new Vector2(-40, 60);

            // Title
            GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(headerGO.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.45f);
            titleRect.anchorMax = new Vector2(0.8f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "MERCHANT SHOP";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.82f, 0.25f, 1f);
            titleTmp.raycastTarget = false;

            // Subtitle
            GameObject subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(headerGO.transform, false);
            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0f);
            subRect.anchorMax = new Vector2(0.8f, 0.45f);
            subRect.sizeDelta = Vector2.zero;
            var subTmp = subGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Purchase farm seeds or trade freshly harvested crops for gold";
            subTmp.fontSize = 13;
            subTmp.color = new Color(0.6f, 0.68f, 0.78f, 1f);
            subTmp.raycastTarget = false;

            // Close [X] Button
            GameObject xBtnGO = new GameObject("Btn_XClose", typeof(RectTransform), typeof(Image), typeof(Button));
            xBtnGO.transform.SetParent(headerGO.transform, false);
            var xRect = xBtnGO.GetComponent<RectTransform>();
            xRect.anchorMin = new Vector2(1, 0.5f);
            xRect.anchorMax = new Vector2(1, 0.5f);
            xRect.pivot = new Vector2(1, 0.5f);
            xRect.sizeDelta = new Vector2(36, 36);
            xBtnGO.GetComponent<Image>().color = new Color(0.24f, 0.15f, 0.18f, 1f);
            closeButton = xBtnGO.GetComponent<Button>();

            GameObject xTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            xTextGO.transform.SetParent(xBtnGO.transform, false);
            var xtRect = xTextGO.GetComponent<RectTransform>();
            xtRect.anchorMin = Vector2.zero;
            xtRect.anchorMax = Vector2.one;
            xtRect.sizeDelta = Vector2.zero;
            var xtTmp = xTextGO.GetComponent<TextMeshProUGUI>();
            xtTmp.text = "X";
            xtTmp.fontSize = 18;
            xtTmp.fontStyle = FontStyles.Bold;
            xtTmp.alignment = TextAlignmentOptions.Center;
            xtTmp.color = new Color(1f, 0.45f, 0.45f);
            xtTmp.raycastTarget = false;

            // 5. Gold Pill Bar
            GameObject goldPillGO = new GameObject("GoldPill", typeof(RectTransform), typeof(Image));
            goldPillGO.transform.SetParent(cardGO.transform, false);
            var gpRect = goldPillGO.GetComponent<RectTransform>();
            gpRect.anchorMin = new Vector2(0, 1);
            gpRect.anchorMax = new Vector2(1, 1);
            gpRect.pivot = new Vector2(0.5f, 1);
            gpRect.anchoredPosition = new Vector2(0, -78);
            gpRect.sizeDelta = new Vector2(-40, 32);
            goldPillGO.GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.26f, 0.85f);

            GameObject goldTextGO = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goldTextGO.transform.SetParent(goldPillGO.transform, false);
            var gtRect = goldTextGO.GetComponent<RectTransform>();
            gtRect.anchorMin = Vector2.zero;
            gtRect.anchorMax = Vector2.one;
            gtRect.sizeDelta = new Vector2(-20, 0);
            goldBalanceText = goldTextGO.GetComponent<TextMeshProUGUI>();
            goldBalanceText.text = "Gold: <color=#FFD700>0 G</color>";
            goldBalanceText.fontSize = 16;
            goldBalanceText.fontStyle = FontStyles.Bold;
            goldBalanceText.alignment = TextAlignmentOptions.MidlineLeft;
            goldBalanceText.color = Color.white;
            goldBalanceText.raycastTarget = false;

            // 6. Tabs Bar
            GameObject tabsGO = new GameObject("TabsBar", typeof(RectTransform));
            tabsGO.transform.SetParent(cardGO.transform, false);
            var tabsRect = tabsGO.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0, 1);
            tabsRect.anchorMax = new Vector2(1, 1);
            tabsRect.pivot = new Vector2(0.5f, 1);
            tabsRect.anchoredPosition = new Vector2(0, -116);
            tabsRect.sizeDelta = new Vector2(-40, 36);

            // Buy Tab Button
            GameObject buyTabGO = new GameObject("Tab_Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            buyTabGO.transform.SetParent(tabsGO.transform, false);
            var btr = buyTabGO.GetComponent<RectTransform>();
            btr.anchorMin = new Vector2(0, 0);
            btr.anchorMax = new Vector2(0.49f, 1);
            btr.sizeDelta = Vector2.zero;
            buyTabImage = buyTabGO.GetComponent<Image>();
            buyTabImage.color = activeTabColor;
            buyTabButton = buyTabGO.GetComponent<Button>();

            GameObject bttGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bttGO.transform.SetParent(buyTabGO.transform, false);
            var bttRect = bttGO.GetComponent<RectTransform>();
            bttRect.anchorMin = Vector2.zero;
            bttRect.anchorMax = Vector2.one;
            bttRect.sizeDelta = Vector2.zero;
            var bttTmp = bttGO.GetComponent<TextMeshProUGUI>();
            bttTmp.text = "BUY SEEDS";
            bttTmp.fontSize = 15;
            bttTmp.fontStyle = FontStyles.Bold;
            bttTmp.alignment = TextAlignmentOptions.Center;
            bttTmp.color = Color.white;
            bttTmp.raycastTarget = false;

            // Sell Tab Button
            GameObject sellTabGO = new GameObject("Tab_Sell", typeof(RectTransform), typeof(Image), typeof(Button));
            sellTabGO.transform.SetParent(tabsGO.transform, false);
            var str = sellTabGO.GetComponent<RectTransform>();
            str.anchorMin = new Vector2(0.51f, 0);
            str.anchorMax = new Vector2(1f, 1);
            str.sizeDelta = Vector2.zero;
            sellTabImage = sellTabGO.GetComponent<Image>();
            sellTabImage.color = inactiveTabColor;
            sellTabButton = sellTabGO.GetComponent<Button>();

            GameObject sttGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            sttGO.transform.SetParent(sellTabGO.transform, false);
            var sttRect = sttGO.GetComponent<RectTransform>();
            sttRect.anchorMin = Vector2.zero;
            sttRect.anchorMax = Vector2.one;
            sttRect.sizeDelta = Vector2.zero;
            var sttTmp = sttGO.GetComponent<TextMeshProUGUI>();
            sttTmp.text = "SELL HARVEST";
            sttTmp.fontSize = 15;
            sttTmp.fontStyle = FontStyles.Bold;
            sttTmp.alignment = TextAlignmentOptions.Center;
            sttTmp.color = Color.white;
            sttTmp.raycastTarget = false;

            // 7. Scroll View Area for Items
            GameObject scrollGO = new GameObject("ItemScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(cardGO.transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.offsetMin = new Vector2(20, 60);
            srt.offsetMax = new Vector2(-20, -160);
            scrollGO.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.85f);

            scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            // Viewport
            GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vprt = vpGO.GetComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.sizeDelta = Vector2.zero;
            scrollRect.viewport = vprt;

            // Buy Content
            GameObject buyContentGO = new GameObject("BuyContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            buyContentGO.transform.SetParent(vpGO.transform, false);
            var bcrt = buyContentGO.GetComponent<RectTransform>();
            bcrt.anchorMin = new Vector2(0, 1);
            bcrt.anchorMax = new Vector2(1, 1);
            bcrt.pivot = new Vector2(0.5f, 1);
            bcrt.sizeDelta = Vector2.zero;

            var bvlg = buyContentGO.GetComponent<VerticalLayoutGroup>();
            bvlg.padding = new RectOffset(8, 8, 8, 8);
            bvlg.spacing = 8;
            bvlg.childControlWidth = true;
            bvlg.childControlHeight = false;
            bvlg.childForceExpandWidth = true;
            bvlg.childForceExpandHeight = false;

            var bcsf = buyContentGO.GetComponent<ContentSizeFitter>();
            bcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            buyContentContainer = buyContentGO.transform;
            scrollRect.content = bcrt;

            // Sell Content
            GameObject sellContentGO = new GameObject("SellContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            sellContentGO.transform.SetParent(vpGO.transform, false);
            var scrt = sellContentGO.GetComponent<RectTransform>();
            scrt.anchorMin = new Vector2(0, 1);
            scrt.anchorMax = new Vector2(1, 1);
            scrt.pivot = new Vector2(0.5f, 1);
            scrt.sizeDelta = Vector2.zero;

            var svlg = sellContentGO.GetComponent<VerticalLayoutGroup>();
            svlg.padding = new RectOffset(8, 8, 8, 8);
            svlg.spacing = 8;
            svlg.childControlWidth = true;
            svlg.childControlHeight = false;
            svlg.childForceExpandWidth = true;
            svlg.childForceExpandHeight = false;

            var scsf = sellContentGO.GetComponent<ContentSizeFitter>();
            scsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sellContentContainer = sellContentGO.transform;
            sellContentGO.SetActive(false);

            // 8. Bottom Return Bar
            GameObject bottomBarGO = new GameObject("BottomBar", typeof(RectTransform));
            bottomBarGO.transform.SetParent(cardGO.transform, false);
            var bbRect = bottomBarGO.GetComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0, 0);
            bbRect.anchorMax = new Vector2(1, 0);
            bbRect.pivot = new Vector2(0.5f, 0);
            bbRect.anchoredPosition = new Vector2(0, 12);
            bbRect.sizeDelta = new Vector2(-40, 36);

            // Close Shop Button
            GameObject closeBtnGO = new GameObject("Btn_CloseShop", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGO.transform.SetParent(bottomBarGO.transform, false);
            var cbr = closeBtnGO.GetComponent<RectTransform>();
            cbr.anchorMin = new Vector2(0.35f, 0);
            cbr.anchorMax = new Vector2(0.65f, 1);
            cbr.sizeDelta = Vector2.zero;
            closeBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 1f);
            bottomCloseButton = closeBtnGO.GetComponent<Button>();

            GameObject cbTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cbTextGO.transform.SetParent(closeBtnGO.transform, false);
            var cbtr = cbTextGO.GetComponent<RectTransform>();
            cbtr.anchorMin = Vector2.zero;
            cbtr.anchorMax = Vector2.one;
            cbtr.sizeDelta = Vector2.zero;
            var cbTmp = cbTextGO.GetComponent<TextMeshProUGUI>();
            cbTmp.text = "CLOSE SHOP (ESC)";
            cbTmp.fontSize = 14;
            cbTmp.fontStyle = FontStyles.Bold;
            cbTmp.alignment = TextAlignmentOptions.Center;
            cbTmp.color = Color.white;
            cbTmp.raycastTarget = false;

            ResolveReferencesAndBind();
            shopPanel.SetActive(false);
        }
    }
}
