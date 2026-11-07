using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmBeware.Core.Runtime;
using FeaturesWorkbench;

namespace PlayerUI
{
    /// <summary>
    /// Interactive Equipment Slot UI supporting drag-and-drop equipping from inventory,
    /// double-click/right-click quick-equipping, and click-to-unequip back into inventory.
    /// Synchronizes state with PlayerArmorUpgradeState and PlayerEquipment.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler, IPointerClickHandler
    {
        public static event Action<EquipmentSlotUI, bool> OnSlotHoverChanged;

        [Header("Slot Configuration")]
        public EquipmentSlotType slotType;
        public string slotDisplayName = "Equipment Slot";

        [Header("Visual Component References")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image slotBorder;
        [SerializeField] private Image silhouetteIcon;
        [SerializeField] private Text slotLabel;

        [Header("Feedback Colors")]
        [SerializeField] private Color normalBorderColor = new Color(0.32f, 0.35f, 0.44f, 1f);
        [SerializeField] private Color equippedBorderColor = new Color(0.92f, 0.74f, 0.24f, 1f);
        [SerializeField] private Color hoverBorderColor = new Color(1f, 0.88f, 0.42f, 1f);
        [SerializeField] private Color emptyBgColor = new Color(0.13f, 0.14f, 0.19f, 1f);
        [SerializeField] private Color equippedBgColor = new Color(0.20f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color silhouetteColor = new Color(0.78f, 0.82f, 0.90f, 0.42f);

        private Sprite defaultSilhouetteSprite;
        private Text equippedBadgeText;
        private GameObject equippedBadgeRoot;
        private bool isPointerHovered;

        public ItemData EquippedItem { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterInventoryQuickEquipHook()
        {
            InventoryComponent.OnEquipmentUseRequested -= TryQuickEquipFromInventory;
            InventoryComponent.OnEquipmentUseRequested += TryQuickEquipFromInventory;
        }

        static EquipmentSlotUI()
        {
            InventoryComponent.OnEquipmentUseRequested -= TryQuickEquipFromInventory;
            InventoryComponent.OnEquipmentUseRequested += TryQuickEquipFromInventory;
        }

        private void Awake()
        {
            InventoryComponent.OnEquipmentUseRequested -= TryQuickEquipFromInventory;
            InventoryComponent.OnEquipmentUseRequested += TryQuickEquipFromInventory;
            EnsureVisualReferences();
            ApplyBorderAndBackgroundState();
        }

        private void Start()
        {
            EnsureVisualReferences();
            RefreshFromArmorState();
        }

        private void OnEnable()
        {
            EnsureVisualReferences();
            if (PlayerArmorUpgradeState.Instance != null)
            {
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged += HandleEquipmentChanged;
            }
            RefreshFromArmorState();
        }

        private void OnDisable()
        {
            isPointerHovered = false;
            if (PlayerArmorUpgradeState.Instance != null)
            {
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
            }
        }

        public void EnsureVisualReferences()
        {
            var rootImg = GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.raycastTarget = true;
            }

            if (slotBorder == null)
            {
                var borderTr = transform.Find("Border");
                if (borderTr != null) slotBorder = borderTr.GetComponent<Image>();
            }
            if (slotBorder != null)
            {
                slotBorder.raycastTarget = false;
            }

            if (slotBackground == null)
            {
                var bgTr = transform.Find("InnerBg");
                if (bgTr != null) slotBackground = bgTr.GetComponent<Image>();
            }
            if (slotBackground != null)
            {
                slotBackground.raycastTarget = false;
            }

            if (silhouetteIcon == null)
            {
                var silTr = transform.Find("SilhouetteIcon");
                if (silTr != null) silhouetteIcon = silTr.GetComponent<Image>();
            }
            if (silhouetteIcon != null)
            {
                silhouetteIcon.raycastTarget = false;
                silhouetteIcon.preserveAspect = true;
                if (defaultSilhouetteSprite == null && silhouetteIcon.sprite != null && EquippedItem == null)
                {
                    defaultSilhouetteSprite = silhouetteIcon.sprite;
                }
            }

            if (defaultSilhouetteSprite == null)
            {
                defaultSilhouetteSprite = ResolveFallbackSilhouetteSprite(slotType);
            }

            EnsureSlotTagAndBadge();
        }

        private void EnsureSlotTagAndBadge()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Slot Tag Label at bottom of slot
            Transform tagTr = transform.Find("SlotTagLabel");
            if (tagTr == null)
            {
                var tagGo = new GameObject("SlotTagLabel", typeof(RectTransform));
                tagGo.transform.SetParent(transform, false);
                var tagRt = tagGo.GetComponent<RectTransform>();
                tagRt.anchorMin = new Vector2(0f, 0f);
                tagRt.anchorMax = new Vector2(1f, 0f);
                tagRt.pivot = new Vector2(0.5f, 1f);
                tagRt.anchoredPosition = new Vector2(0f, -2f);
                tagRt.sizeDelta = new Vector2(16f, 14f);

                slotLabel = tagGo.AddComponent<Text>();
                slotLabel.font = font;
                slotLabel.fontSize = 9;
                slotLabel.fontStyle = FontStyle.Bold;
                slotLabel.alignment = TextAnchor.UpperCenter;
                slotLabel.color = new Color(0.72f, 0.76f, 0.86f, 0.92f);
                slotLabel.raycastTarget = false;
                slotLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                slotLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }
            else if (slotLabel == null)
            {
                slotLabel = tagTr.GetComponent<Text>();
            }

            if (slotLabel != null)
            {
                slotLabel.text = GetShortSlotTag(slotType);
                slotLabel.raycastTarget = false;
            }

            // 2. Equipped corner badge ("EQ") at top-right of slot
            Transform badgeTr = transform.Find("EquippedBadge");
            if (badgeTr == null)
            {
                equippedBadgeRoot = new GameObject("EquippedBadge", typeof(RectTransform));
                equippedBadgeRoot.transform.SetParent(transform, false);
                var badgeRt = equippedBadgeRoot.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(1f, 1f);
                badgeRt.anchorMax = new Vector2(1f, 1f);
                badgeRt.pivot = new Vector2(1f, 1f);
                badgeRt.anchoredPosition = new Vector2(-3f, -3f);
                badgeRt.sizeDelta = new Vector2(20f, 12f);

                var badgeBg = equippedBadgeRoot.AddComponent<Image>();
                badgeBg.color = new Color(0.14f, 0.42f, 0.24f, 0.95f);
                badgeBg.raycastTarget = false;

                var txtGo = new GameObject("BadgeText", typeof(RectTransform));
                txtGo.transform.SetParent(equippedBadgeRoot.transform, false);
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;

                equippedBadgeText = txtGo.AddComponent<Text>();
                equippedBadgeText.font = font;
                equippedBadgeText.fontSize = 8;
                equippedBadgeText.fontStyle = FontStyle.Bold;
                equippedBadgeText.alignment = TextAnchor.MiddleCenter;
                equippedBadgeText.color = new Color(0.98f, 0.92f, 0.55f, 1f);
                equippedBadgeText.text = "EQ";
                equippedBadgeText.raycastTarget = false;
            }
            else
            {
                equippedBadgeRoot = badgeTr.gameObject;
                equippedBadgeText = badgeTr.GetComponentInChildren<Text>(true);
            }

            if (equippedBadgeRoot != null)
            {
                equippedBadgeRoot.SetActive(EquippedItem != null);
            }
        }

        public static string GetShortSlotTag(EquipmentSlotType type)
        {
            return type switch
            {
                EquipmentSlotType.Helmet => "HEAD",
                EquipmentSlotType.Chest => "CHEST",
                EquipmentSlotType.Gloves => "GLOVES",
                EquipmentSlotType.Pants => "LEGS",
                EquipmentSlotType.Boots => "BOOTS",
                EquipmentSlotType.Necklace => "AMULET",
                EquipmentSlotType.Cape => "CLOAK",
                EquipmentSlotType.Belt => "BELT",
                EquipmentSlotType.Ring1 => "RING I",
                EquipmentSlotType.Ring2 => "RING II",
                EquipmentSlotType.MainWeapon => "WEAPON",
                EquipmentSlotType.OffhandShield => "SHIELD",
                _ => "GEAR"
            };
        }

        private static Sprite ResolveFallbackSilhouetteSprite(EquipmentSlotType type)
        {
#if UNITY_EDITOR
            string fileName = type switch
            {
                EquipmentSlotType.Helmet => "Icon_Equip_Helmet.png",
                EquipmentSlotType.Chest => "Icon_Equip_Chest.png",
                EquipmentSlotType.Gloves => "Icon_Equip_Gloves.png",
                EquipmentSlotType.Pants => "Icon_Equip_Pants.png",
                EquipmentSlotType.Boots => "Icon_Equip_Boots.png",
                EquipmentSlotType.Necklace => "Icon_Equip_Necklace.png",
                EquipmentSlotType.Cape => "Icon_Equip_Cape.png",
                EquipmentSlotType.Belt => "Icon_Equip_Belt.png",
                EquipmentSlotType.Ring1 => "Icon_Equip_Ring.png",
                EquipmentSlotType.Ring2 => "Icon_Equip_Ring.png",
                EquipmentSlotType.MainWeapon => "Icon_Equip_Weapon.png",
                EquipmentSlotType.OffhandShield => "Icon_Equip_Shield.png",
                _ => "Icon_Equip_Chest.png"
            };
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Icons/Equipment/" + fileName);
#else
            return null;
#endif
        }

        private void HandleEquipmentChanged(EquipmentSlotType slot, ItemData item)
        {
            if (slot == slotType)
            {
                SetEquippedItemVisual(item);
            }
        }

        public void Setup(EquipmentSlotType type, string displayName, Sprite icon)
        {
            slotType = type;
            slotDisplayName = displayName;
            EnsureVisualReferences();

            if (icon != null)
            {
                defaultSilhouetteSprite = icon;
            }
            if (silhouetteIcon != null && defaultSilhouetteSprite != null && EquippedItem == null)
            {
                silhouetteIcon.sprite = defaultSilhouetteSprite;
                silhouetteIcon.enabled = true;
                silhouetteIcon.color = silhouetteColor;
            }
            if (slotLabel != null)
            {
                slotLabel.text = GetShortSlotTag(slotType);
            }
            RefreshFromArmorState();
        }

        public void RefreshFromArmorState()
        {
            EnsureVisualReferences();
            var armorState = ResolvePlayerArmorState();
            if (armorState != null)
            {
                var item = armorState.GetEquippedItem(slotType);
                SetEquippedItemVisual(item);
            }
            else
            {
                SetEquippedItemVisual(null);
            }
        }

        public void SetEquippedItemVisual(ItemData item)
        {
            EnsureVisualReferences();
            EquippedItem = item;

            if (silhouetteIcon != null)
            {
                Sprite itemSprite = item != null ? item.itemIcon : null;
                if (item != null && itemSprite == null && item is ToolItemData tool && tool.isRangedWeapon)
                {
                    itemSprite = ToolItemData.GetDefaultRangedSlingshotSprite();
                }

                if (item != null && itemSprite != null)
                {
                    silhouetteIcon.sprite = itemSprite;
                    silhouetteIcon.color = Color.white;
                    silhouetteIcon.enabled = true;
                }
                else
                {
                    silhouetteIcon.sprite = defaultSilhouetteSprite;
                    silhouetteIcon.color = silhouetteColor;
                    silhouetteIcon.enabled = defaultSilhouetteSprite != null;
                }
            }

            if (equippedBadgeRoot != null)
            {
                equippedBadgeRoot.SetActive(item != null);
            }

            if (slotLabel != null)
            {
                slotLabel.color = item != null
                    ? new Color(0.98f, 0.84f, 0.36f, 1f)
                    : new Color(0.72f, 0.76f, 0.86f, 0.88f);
            }

            ApplyBorderAndBackgroundState();
        }

        private void ApplyBorderAndBackgroundState()
        {
            bool hasItem = EquippedItem != null;
            if (slotBorder != null)
            {
                if (isPointerHovered)
                {
                    slotBorder.color = hoverBorderColor;
                }
                else
                {
                    slotBorder.color = hasItem ? equippedBorderColor : normalBorderColor;
                }
            }

            if (slotBackground != null)
            {
                slotBackground.color = hasItem ? equippedBgColor : emptyBgColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerHovered = true;
            ApplyBorderAndBackgroundState();
            OnSlotHoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerHovered = false;
            ApplyBorderAndBackgroundState();
            OnSlotHoverChanged?.Invoke(this, false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            var draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
            if (draggable == null || draggable.OriginSlot == null) return;

            var originSlot = draggable.OriginSlot;
            var invSlot = originSlot.BoundSlot;
            if (invSlot == null || invSlot.IsEmpty || invSlot.item == null) return;

            var itemToEquip = invSlot.item;

            if (!IsValidItemForSlot(itemToEquip, slotType))
            {
                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                fct?.SpawnText(transform.position + Vector3.up * 30f, $"Cannot equip {itemToEquip.itemName} in {GetShortSlotTag(slotType)}!", new Color(1f, 0.4f, 0.4f));
                return;
            }

            var armorState = ResolvePlayerArmorState();
            if (armorState != null)
            {
                armorState.EquipItem(slotType, itemToEquip, out var previousItem);

                var ownerInv = originSlot.ownerInventory;
                if (ownerInv != null)
                {
                    if (previousItem != null)
                    {
                        invSlot.item = previousItem;
                        invSlot.quantity = 1;
                    }
                    else
                    {
                        invSlot.item = null;
                        invSlot.quantity = 0;
                    }
                    ownerInv.OnInventoryChanged?.Invoke();
                }

                draggable.MarkDropped();
                SetEquippedItemVisual(itemToEquip);
                NotifyPlayerEquipmentUpdated();

                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                fct?.SpawnText(transform.position + Vector3.up * 30f, $"Equipped: {itemToEquip.itemName}", new Color(0.4f, 1f, 0.6f));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (EquippedItem == null) return;

            // Click to unequip item back into player inventory
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            var inv = player != null ? player.GetComponent<InventoryComponent>() : null;
            if (inv == null) return;

            ItemData itemBeingUnequipped = EquippedItem;
            if (inv.AddItem(itemBeingUnequipped, 1))
            {
                var armorState = ResolvePlayerArmorState();
                if (armorState != null)
                {
                    armorState.UnequipItem(slotType, out _);
                }

                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                fct?.SpawnText(transform.position + Vector3.up * 30f, $"Unequipped: {itemBeingUnequipped.itemName}", new Color(0.9f, 0.9f, 0.4f));

                SetEquippedItemVisual(null);
                NotifyPlayerEquipmentUpdated();
            }
            else
            {
                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                fct?.SpawnText(transform.position + Vector3.up * 30f, "Inventory Full!", new Color(1f, 0.3f, 0.3f));
            }
        }

        /// <summary>
        /// Quick-equips an EquipmentItemData or Weapon from an inventory slot directly into its target EquipmentSlotType.
        /// Triggered on double-click or right-click in InventorySlotUI.
        /// </summary>
        public static bool TryQuickEquipFromInventory(InventoryComponent inv, int slotIndex)
        {
            if (inv == null || slotIndex < 0 || slotIndex >= inv.slots.Count) return false;
            var invSlot = inv.slots[slotIndex];
            if (invSlot == null || invSlot.IsEmpty || invSlot.item == null) return false;

            var item = invSlot.item;
            if (!TryResolveTargetSlot(item, out var targetSlot))
                return false;

            var armorState = ResolvePlayerArmorState();
            if (armorState == null) return false;

            armorState.EquipItem(targetSlot, item, out var previousItem);

            if (previousItem != null)
            {
                invSlot.item = previousItem;
                invSlot.quantity = 1;
            }
            else
            {
                invSlot.item = null;
                invSlot.quantity = 0;
            }

            inv.OnInventoryChanged?.Invoke();

            // Refresh all active EquipmentSlotUI instances in the scene
            var allSlots = FindObjectsByType<EquipmentSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (allSlots[i] != null && allSlots[i].slotType == targetSlot)
                {
                    allSlots[i].SetEquippedItemVisual(item);
                }
            }

            NotifyPlayerEquipmentUpdated();

            var fct = ServiceLocator.Resolve<IFloatingTextService>();
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            Vector3 textPos = player != null ? player.transform.position + Vector3.up * 1.6f : Vector3.zero;
            fct?.SpawnText(textPos, $"Equipped: {item.itemName} [{GetShortSlotTag(targetSlot)}]", new Color(0.4f, 1f, 0.6f));
            return true;
        }

        public static bool TryResolveTargetSlot(ItemData item, out EquipmentSlotType targetSlot)
        {
            targetSlot = EquipmentSlotType.Helmet;
            if (item == null) return false;

            if (item is EquipmentItemData eqItem)
            {
                targetSlot = eqItem.slotType;
                return true;
            }

            // Exclude agricultural tools (Hoe / Watering Can) from quick-equipping into MainWeapon
            if (item is ToolItemData tool)
            {
                if (tool.isHoe) return false;
                if (tool.isWeapon || tool.isRangedWeapon)
                {
                    targetSlot = EquipmentSlotType.MainWeapon;
                    return true;
                }
            }

            if (item.category == ItemCategory.Weapon ||
                string.Equals(item.itemId, "dummysword", StringComparison.OrdinalIgnoreCase))
            {
                targetSlot = EquipmentSlotType.MainWeapon;
                return true;
            }

            foreach (EquipmentSlotType candidate in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                if (IsValidItemForSlot(item, candidate))
                {
                    targetSlot = candidate;
                    return true;
                }
            }

            return false;
        }

        private static PlayerArmorUpgradeState ResolvePlayerArmorState()
        {
            if (PlayerArmorUpgradeState.Instance != null)
                return PlayerArmorUpgradeState.Instance;

            var existing = FindFirstObjectByType<PlayerArmorUpgradeState>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                return player.GetComponent<PlayerArmorUpgradeState>() ?? player.AddComponent<PlayerArmorUpgradeState>();
            }

            return null;
        }

        private static void NotifyPlayerEquipmentUpdated()
        {
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player == null) return;

            var pe = player.GetComponent<PlayerEquipment>();
            var inv = player.GetComponent<InventoryComponent>();
            if (pe != null)
            {
                pe.UpdateEquipmentVisual(inv != null ? inv.selectedHotbarIndex : 0);
            }

            var statsUI = FindFirstObjectByType<PlayerStatsDisplayUI>(FindObjectsInactive.Include);
            if (statsUI != null)
            {
                statsUI.UpdateAllStats();
            }

            var loadoutUI = FindFirstObjectByType<EquipmentLoadoutPanelUI>(FindObjectsInactive.Include);
            if (loadoutUI != null)
            {
                loadoutUI.RefreshLoadoutDisplay();
            }
        }

        public static bool IsValidItemForSlot(ItemData item, EquipmentSlotType slot)
        {
            if (item == null) return false;

            if (item is EquipmentItemData eqItem)
            {
                return eqItem.slotType == slot;
            }

            if (slot == EquipmentSlotType.MainWeapon)
            {
                if (item is ToolItemData tool && (tool.isWeapon || tool.isRangedWeapon) && !tool.isHoe) return true;
                if (item.category == ItemCategory.Weapon) return true;
                if (string.Equals(item.itemId, "dummysword", StringComparison.OrdinalIgnoreCase)) return true;
            }

            // Fallback heuristics for existing generic items
            string id = item.itemId != null ? item.itemId.ToLowerInvariant() : string.Empty;
            if (slot == EquipmentSlotType.Helmet && (id.Contains("hat") || id.Contains("hood") || id.Contains("helm"))) return true;
            if (slot == EquipmentSlotType.Chest && (id.Contains("suit") || id.Contains("armor") || id.Contains("chest") || id.Contains("shirt"))) return true;
            if (slot == EquipmentSlotType.Boots && (id.Contains("boot") || id.Contains("shoe") || id.Contains("strider"))) return true;
            if (slot == EquipmentSlotType.Gloves && (id.Contains("glove") || id.Contains("grip") || id.Contains("gauntlet"))) return true;
            if ((slot == EquipmentSlotType.Ring1 || slot == EquipmentSlotType.Ring2 || slot == EquipmentSlotType.Necklace) &&
                (id.Contains("heart") || id.Contains("ring") || id.Contains("amulet") || id.Contains("necklace"))) return true;

            return false;
        }
    }
}
