using System.Collections.Generic;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;
using UnityEngine;

namespace FeaturesEconomy
{
    /// <summary>
    /// Interactive Commercial Trading Post shop counter component.
    /// Inherits from MerchantTableInteractable to provide seed purchasing, crop sales,
    /// and commercial trading post operations via MerchantShopUI.
    /// Implements IObstructionExempt to ensure porch/eaves geometry does not occlude interaction raycasts.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(WorldLabel))]
    [RequireComponent(typeof(Highlightable))]
    public class ShopInteractable : MerchantTableInteractable, IObstructionExempt
    {
        [Header("Trading Post Configuration")]
        [Tooltip("Display name shown on the world tooltip label when hovering.")]
        [SerializeField] private string shopDisplayName = "Trading Post Merchant (Trade)";

        [Tooltip("If true, custom trading post stock items will be loaded in addition to base seeds.")]
        [SerializeField] private bool includeGeneralProvisions = true;

        protected override void Awake()
        {
            base.Awake();

            var label = GetComponent<WorldLabel>();
            if (label != null && (string.IsNullOrEmpty(label.displayName) || label.displayName == "Merchant Table (Trade)"))
            {
                label.displayName = string.IsNullOrEmpty(shopDisplayName) ? "Trading Post Merchant (Trade)" : shopDisplayName;
            }

            EnsureTradingPostCatalog();
        }

        public override void InitializeDefaultStock()
        {
            base.InitializeDefaultStock();
            EnsureTradingPostCatalog();
        }

        /// <summary>
        /// Populates stock items with the comprehensive commercial catalog.
        /// </summary>
        public void EnsureTradingPostCatalog()
        {
            if (stockItems == null)
            {
                stockItems = new List<ItemData>();
            }

            if (stockItems.Count == 0)
            {
                base.InitializeDefaultStock();
            }

            if (!includeGeneralProvisions) return;

            // Load additional trade items from ItemDatabase if present
            var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db == null || db.GetAllItems() == null) return;

            foreach (var item in db.GetAllItems())
            {
                if (item == null || item.isDirty || (item.itemId != null && item.itemId.Contains("dirty"))) continue;

                // Trading post carries tools, seeds, weapons, armor, and provisions with a valid buy price
                if (item.category == ItemCategory.Seed ||
                    item.category == ItemCategory.Tool ||
                    item.category == ItemCategory.Weapon ||
                    item is EquipmentItemData ||
                    (item.category == ItemCategory.Material && item.buyPrice > 0) ||
                    item.itemId == "dummysword" ||
                    item.itemId == "food_bottle_water" ||
                    (item.buyPrice > 0 && item.category != ItemCategory.Crop && item.category != ItemCategory.MonsterDrop))
                {
                    if (!stockItems.Contains(item))
                    {
                        stockItems.Add(item);
                    }
                }
            }
        }

        public override void Interact(GameObject interactor)
        {
            var timeService = ServiceLocator.Resolve<ITimeService>();
            int currentDay = timeService != null ? timeService.CurrentDay : 1;

            if (currentDay < 1)
            {
                ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                    transform.position + Vector3.up * 1.5f,
                    "Rest first. The harvest begins tomorrow.",
                    new Color(1f, 0.8f, 0.4f));
                return;
            }

            EnsureMerchantShopUI();

            if (MerchantShopUI.Instance != null)
            {
                MerchantShopUI.Instance.OpenShop(stockItems);
            }
            else
            {
                Debug.LogWarning("[ShopInteractable] MerchantShopUI could not be found or initialized!");
            }
        }

        public override bool CanInteract(GameObject interactor)
        {
            return true;
        }

        private static void EnsureMerchantShopUI()
        {
            if (MerchantShopUI.Instance != null) return;

            var existing = Object.FindFirstObjectByType<MerchantShopUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            var uiCanvasGO = GameObject.Find("UI_Canvas");
            Canvas screenCanvas = null;
            if (uiCanvasGO != null && uiCanvasGO.TryGetComponent<Canvas>(out var c) && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenCanvas = c;
            }
            else
            {
                var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var cv in allCanvases)
                {
                    if (cv != null && cv.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        screenCanvas = cv;
                        break;
                    }
                }
            }

            if (screenCanvas != null)
            {
                var shopManagerGO = new GameObject("MerchantShopUI_Panel", typeof(MerchantShopUI));
                shopManagerGO.transform.SetParent(screenCanvas.transform, false);
            }
        }

        /// <summary>
        /// Programmatic factory method to construct an interactive Trading Post Counter entity.
        /// Sets up standard layers, colliders, labels, and highlight materials.
        /// </summary>
        public static GameObject CreateTradingPostShopCounter(Vector3 position, Quaternion rotation)
        {
            var counterGO = new GameObject("TradingPost_ShopCounter");
            counterGO.transform.position = position;
            counterGO.transform.rotation = rotation;
            counterGO.layer = 0; // Layer 0 Default (interactable raycast target, never Layer 12 Wall)
            counterGO.tag = "Untagged";

            // 1. BoxCollider for interaction and player boundaries
            var col = counterGO.AddComponent<BoxCollider>();
            col.size = new Vector3(2.4f, 1.2f, 1.4f);
            col.center = new Vector3(0f, 0.55f, 0f);
            col.isTrigger = false;

            // 2. WorldLabel
            var label = counterGO.AddComponent<WorldLabel>();
            label.displayName = "Trading Post Merchant (Trade)";

            // 3. Highlightable
            counterGO.AddComponent<Highlightable>();

            // 4. ShopInteractable
            var shop = counterGO.AddComponent<ShopInteractable>();
            shop.InitializeDefaultStock();

            return counterGO;
        }
    }
}
