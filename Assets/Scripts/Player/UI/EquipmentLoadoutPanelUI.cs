using System;
using UnityEngine;
using UnityEngine.UI;
using FarmBeware.Core.Runtime;
using FeaturesWorkbench;
using FeaturesWorkbench.Core;

namespace PlayerUI
{
    /// <summary>
    /// Renders the Tactical Armor Paperdoll, Aggregated Gear Stat Telemetry,
    /// Sweet Potato Set Bonus Tracker, and Live Slot Inspector inside Panel_Equipment (Tab UI).
    /// </summary>
    [DisallowMultipleComponent]
    public class EquipmentLoadoutPanelUI : MonoBehaviour
    {
        [Header("Viewport Container")]
        [SerializeField] private RectTransform viewportRoot;

        [Header("Telemetry Readouts")]
        [SerializeField] private Text headerSubtitleText;
        [SerializeField] private Text gearStatsSummaryText;
        [SerializeField] private Text setHeaderCountText;
        [SerializeField] private Text setTier2Text;
        [SerializeField] private Text setTier4Text;
        [SerializeField] private Text setTier5Text;
        [SerializeField] private Text inspectorTitleText;
        [SerializeField] private Text inspectorDetailText;

        [Header("Paperdoll Body Segments")]
        [SerializeField] private Image paperdollHeadImg;
        [SerializeField] private Image paperdollTorsoImg;
        [SerializeField] private Image paperdollArmsLImg;
        [SerializeField] private Image paperdollArmsRImg;
        [SerializeField] private Image paperdollBootsLImg;
        [SerializeField] private Image paperdollBootsRImg;
        [SerializeField] private Image paperdollCoreGemImg;

        [Header("Paperdoll Status Badges")]
        [SerializeField] private Text badgeHeadText;
        [SerializeField] private Text badgeChestText;
        [SerializeField] private Text badgeGlovesText;
        [SerializeField] private Text badgeBootsText;
        [SerializeField] private Text badgeNecklaceText;
        [SerializeField] private Text badgeWeaponText;

        private static readonly Color SilhouetteUnarmoredColor = new Color(0.22f, 0.25f, 0.32f, 0.85f);
        private static readonly Color SilhouetteArmoredHoodColor = new Color(0.68f, 0.26f, 0.44f, 0.98f);
        private static readonly Color SilhouetteArmoredChestColor = new Color(0.74f, 0.28f, 0.46f, 0.98f);
        private static readonly Color SilhouetteArmoredGlovesColor = new Color(0.88f, 0.56f, 0.24f, 0.98f);
        private static readonly Color SilhouetteArmoredBootsColor = new Color(0.64f, 0.24f, 0.40f, 0.98f);
        private static readonly Color SilhouetteArmoredHeartColor = new Color(1.00f, 0.82f, 0.30f, 1.00f);

        private static readonly Color TierInactiveColor = new Color(0.52f, 0.56f, 0.64f, 0.85f);
        private static readonly Color TierActiveEmeraldColor = new Color(0.38f, 0.95f, 0.62f, 1.00f);
        private static readonly Color TierActiveGoldColor = new Color(1.00f, 0.84f, 0.32f, 1.00f);

        private EquipmentSlotUI currentlyHoveredSlot;

        private void Awake()
        {
            EnsureUIHierarchy();
        }

        private void OnEnable()
        {
            EnsureUIHierarchy();
            EquipmentSlotUI.OnSlotHoverChanged -= HandleSlotHoverChanged;
            EquipmentSlotUI.OnSlotHoverChanged += HandleSlotHoverChanged;

            if (PlayerArmorUpgradeState.Instance != null)
            {
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged += HandleEquipmentChanged;
                PlayerArmorUpgradeState.Instance.OnArmorUpgraded -= HandleArmorUpgraded;
                PlayerArmorUpgradeState.Instance.OnArmorUpgraded += HandleArmorUpgraded;
            }

            RefreshAllSlotsAndDisplay();
        }

        private void OnDisable()
        {
            EquipmentSlotUI.OnSlotHoverChanged -= HandleSlotHoverChanged;
            if (PlayerArmorUpgradeState.Instance != null)
            {
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
                PlayerArmorUpgradeState.Instance.OnArmorUpgraded -= HandleArmorUpgraded;
            }
            currentlyHoveredSlot = null;
        }

        private void HandleEquipmentChanged(EquipmentSlotType slot, ItemData item)
        {
            RefreshLoadoutDisplay();
        }

        private void HandleArmorUpgraded()
        {
            RefreshLoadoutDisplay();
        }

        private void HandleSlotHoverChanged(EquipmentSlotUI slot, bool isHovered)
        {
            if (isHovered)
            {
                currentlyHoveredSlot = slot;
            }
            else if (currentlyHoveredSlot == slot)
            {
                currentlyHoveredSlot = null;
            }

            UpdateInspectorFooter();
        }

        public void RefreshAllSlotsAndDisplay()
        {
            var slots = GetComponentsInChildren<EquipmentSlotUI>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].RefreshFromArmorState();
                }
            }
            RefreshLoadoutDisplay();
        }

        public void EnsureUIHierarchy()
        {
            if (viewportRoot == null)
            {
                var vpTr = transform.Find("CharacterPreview_Viewport");
                if (vpTr != null) viewportRoot = vpTr.GetComponent<RectTransform>();
            }
            if (viewportRoot == null) return;

            var existingContainer = viewportRoot.Find("LoadoutTelemetryContainer");
            if (existingContainer != null && headerSubtitleText != null && gearStatsSummaryText != null && setTier2Text != null)
            {
                return;
            }

            if (existingContainer != null)
            {
                if (Application.isPlaying) Destroy(existingContainer.gameObject);
                else DestroyImmediate(existingContainer.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var containerGo = new GameObject("LoadoutTelemetryContainer", typeof(RectTransform));
            containerGo.transform.SetParent(viewportRoot, false);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.offsetMin = new Vector2(8f, 8f);
            containerRt.offsetMax = new Vector2(-8f, -8f);

            // 1. Top Header Bar (y: -4 to -40)
            var headerBar = CreatePanel(containerRt, "HeaderBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 38f), new Color(0.14f, 0.16f, 0.22f, 0.95f));
            CreateText(headerBar.transform, "HeaderTitle", "ARMOR & LOADOUT PAPERDOLL", font, 10, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.95f, 0.80f, 0.35f, 1f), new Vector2(0f, -4f), new Vector2(-8f, 16f));
            headerSubtitleText = CreateText(headerBar.transform, "HeaderSubtitle", "ARMOR LV.1 • 0% DMG MITIGATION", font, 9, FontStyle.Normal, TextAnchor.LowerCenter, new Color(0.75f, 0.82f, 0.92f, 0.95f), new Vector2(0f, -20f), new Vector2(-8f, 14f));

            // 2. Tactical Silhouette Paperdoll Area (y: -44 to -228, height 184)
            var paperdollPanel = CreatePanel(containerRt, "PaperdollDiagram", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(0f, 182f), new Color(0.08f, 0.09f, 0.13f, 0.92f));

            // Anatomical Stylized Silhouette in center of PaperdollDiagram
            var silRoot = new GameObject("ArmorSilhouette", typeof(RectTransform));
            silRoot.transform.SetParent(paperdollPanel.transform, false);
            var silRt = silRoot.GetComponent<RectTransform>();
            silRt.anchorMin = new Vector2(0.5f, 0.5f);
            silRt.anchorMax = new Vector2(0.5f, 0.5f);
            silRt.pivot = new Vector2(0.5f, 0.5f);
            silRt.anchoredPosition = new Vector2(0f, 4f);
            silRt.sizeDelta = new Vector2(90f, 150f);

            // Head (Hood)
            paperdollHeadImg = CreateBlock(silRt, "Paperdoll_Head", new Vector2(0f, 52f), new Vector2(26f, 26f), SilhouetteUnarmoredColor);
            // Torso (Chest Armor)
            paperdollTorsoImg = CreateBlock(silRt, "Paperdoll_Torso", new Vector2(0f, 12f), new Vector2(38f, 46f), SilhouetteUnarmoredColor);
            // Left & Right Arms (Fang Grips)
            paperdollArmsLImg = CreateBlock(silRt, "Paperdoll_ArmL", new Vector2(-27f, 12f), new Vector2(12f, 42f), SilhouetteUnarmoredColor);
            paperdollArmsRImg = CreateBlock(silRt, "Paperdoll_ArmR", new Vector2(27f, 12f), new Vector2(12f, 42f), SilhouetteUnarmoredColor);
            // Left & Right Boots (Underground Striders)
            paperdollBootsLImg = CreateBlock(silRt, "Paperdoll_BootL", new Vector2(-10f, -38f), new Vector2(14f, 46f), SilhouetteUnarmoredColor);
            paperdollBootsRImg = CreateBlock(silRt, "Paperdoll_BootR", new Vector2(10f, -38f), new Vector2(14f, 46f), SilhouetteUnarmoredColor);
            // Core Amulet (Sweet Potato Heart)
            paperdollCoreGemImg = CreateBlock(silRt, "Paperdoll_CoreGem", new Vector2(0f, 20f), new Vector2(12f, 12f), SilhouetteUnarmoredColor);

            // Status Badges on Left & Right of Paperdoll
            badgeHeadText = CreateSlotCallout(paperdollPanel.transform, "Callout_Head", new Vector2(-68f, 62f), "HEAD: Empty", font);
            badgeChestText = CreateSlotCallout(paperdollPanel.transform, "Callout_Chest", new Vector2(-68f, 20f), "CHEST: Empty", font);
            badgeGlovesText = CreateSlotCallout(paperdollPanel.transform, "Callout_Gloves", new Vector2(-68f, -22f), "HANDS: Empty", font);
            badgeBootsText = CreateSlotCallout(paperdollPanel.transform, "Callout_Boots", new Vector2(-68f, -64f), "FEET: Empty", font);

            badgeNecklaceText = CreateSlotCallout(paperdollPanel.transform, "Callout_Necklace", new Vector2(68f, 42f), "RELIC: Empty", font);
            badgeWeaponText = CreateSlotCallout(paperdollPanel.transform, "Callout_Weapon", new Vector2(68f, -42f), "WPN: Cleaver", font);

            // 3. Aggregated Gear Bonus Bar (y: -228 to -264, height 34)
            var bonusBar = CreatePanel(containerRt, "GearBonusBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -228f), new Vector2(0f, 34f), new Color(0.13f, 0.18f, 0.16f, 0.95f));
            gearStatsSummaryText = CreateText(bonusBar.transform, "GearStatsText", "GEAR BONUS: +0 HP • +0 DEF • +0% SPD • +0% CRIT", font, 9, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.55f, 0.96f, 0.72f, 1f), new Vector2(0f, -2f), new Vector2(-6f, 30f));

            // 4. Sweet Potato Set Bonus Tracker Card (y: -266 to -378, height 112)
            var setCard = CreatePanel(containerRt, "SetBonusCard", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(0f, 112f), new Color(0.12f, 0.13f, 0.18f, 0.96f));
            setHeaderCountText = CreateText(setCard.transform, "SetHeader", "SWEET POTATO COMBAT SET [0/5]", font, 10, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.96f, 0.78f, 0.32f, 1f), new Vector2(0f, -5f), new Vector2(-8f, 18f));
            setTier2Text = CreateText(setCard.transform, "SetTier2", "◇ [2-PC] +10% Attack Speed", font, 9, FontStyle.Normal, TextAnchor.MiddleLeft, TierInactiveColor, new Vector2(4f, -30f), new Vector2(-12f, 22f));
            setTier4Text = CreateText(setCard.transform, "SetTier4", "◇ [4-PC] +15% Post-Dodge Dmg (3s)", font, 9, FontStyle.Normal, TextAnchor.MiddleLeft, TierInactiveColor, new Vector2(4f, -56f), new Vector2(-12f, 22f));
            setTier5Text = CreateText(setCard.transform, "SetTier5", "◇ [5-PC] Shadow Dash Reset", font, 9, FontStyle.Normal, TextAnchor.MiddleLeft, TierInactiveColor, new Vector2(4f, -82f), new Vector2(-12f, 22f));

            // 5. Bottom Slot Inspector & Action Hint Footer (y: -382 to -452, height 70)
            var footerCard = CreatePanel(containerRt, "InspectorFooter", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -382f), new Vector2(0f, 70f), new Color(0.15f, 0.14f, 0.19f, 0.98f));
            inspectorTitleText = CreateText(footerCard.transform, "InspectorTitle", "HOW TO EQUIP GEAR", font, 10, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.98f, 0.86f, 0.42f, 1f), new Vector2(0f, -5f), new Vector2(-10f, 18f));
            inspectorDetailText = CreateText(footerCard.transform, "InspectorDetail", "Double-Click, Right-Click, or Drag Armor & Weapons from Inventory to Equip.\nClick an Equipped Slot to Unequip.", font, 9, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.82f, 0.86f, 0.94f, 0.95f), new Vector2(0f, -24f), new Vector2(-10f, 42f));
        }

        private static GameObject CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;
            return go;
        }

        private static Image CreateBlock(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text CreateSlotCallout(Transform parent, string name, Vector2 centerPos, string initialText, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = centerPos;
            rt.sizeDelta = new Vector2(88f, 26f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.22f, 0.90f);
            bg.raycastTarget = false;

            var txtGo = new GameObject("Label", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(3f, 1f);
            txtRt.offsetMax = new Vector2(-3f, -1f);

            var txt = txtGo.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 8;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.70f, 0.74f, 0.84f, 0.92f);
            txt.text = initialText;
            txt.raycastTarget = false;
            return txt;
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, int fontSize, FontStyle style, TextAnchor align, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.text = content;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        public void RefreshLoadoutDisplay()
        {
            EnsureUIHierarchy();

            var armorState = PlayerArmorUpgradeState.Instance;
            if (armorState == null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null) armorState = player.GetComponent<PlayerArmorUpgradeState>();
            }

            int armorLv = armorState != null ? armorState.armorLevel : 1;
            int mitigationPct = armorState != null ? Mathf.RoundToInt(armorState.DamageMitigationRatio * 100f) : 0;

            ItemData helm = armorState != null ? armorState.GetEquippedItem(EquipmentSlotType.Helmet) : null;
            ItemData chest = armorState != null ? armorState.GetEquippedItem(EquipmentSlotType.Chest) : null;
            ItemData gloves = armorState != null ? armorState.GetEquippedItem(EquipmentSlotType.Gloves) : null;
            ItemData boots = armorState != null ? armorState.GetEquippedItem(EquipmentSlotType.Boots) : null;
            ItemData necklace = armorState != null ? armorState.GetEquippedItem(EquipmentSlotType.Necklace) : null;
            ItemData weapon = armorState != null ? armorState.GetEquippedItem(EquipmentSlotType.MainWeapon) : null;

            // Update Paperdoll Body Segment Colors
            if (paperdollHeadImg != null)
                paperdollHeadImg.color = helm != null ? SilhouetteArmoredHoodColor : SilhouetteUnarmoredColor;
            if (paperdollTorsoImg != null)
                paperdollTorsoImg.color = chest != null ? SilhouetteArmoredChestColor : SilhouetteUnarmoredColor;
            if (paperdollArmsLImg != null)
                paperdollArmsLImg.color = gloves != null ? SilhouetteArmoredGlovesColor : SilhouetteUnarmoredColor;
            if (paperdollArmsRImg != null)
                paperdollArmsRImg.color = gloves != null ? SilhouetteArmoredGlovesColor : SilhouetteUnarmoredColor;
            if (paperdollBootsLImg != null)
                paperdollBootsLImg.color = boots != null ? SilhouetteArmoredBootsColor : SilhouetteUnarmoredColor;
            if (paperdollBootsRImg != null)
                paperdollBootsRImg.color = boots != null ? SilhouetteArmoredBootsColor : SilhouetteUnarmoredColor;
            if (paperdollCoreGemImg != null)
                paperdollCoreGemImg.color = necklace != null ? SilhouetteArmoredHeartColor : new Color(0.18f, 0.20f, 0.26f, 0.5f);

            // Update Paperdoll Callout Badges
            UpdateCalloutBadge(badgeHeadText, "HEAD", helm);
            UpdateCalloutBadge(badgeChestText, "CHEST", chest);
            UpdateCalloutBadge(badgeGlovesText, "HANDS", gloves);
            UpdateCalloutBadge(badgeBootsText, "FEET", boots);
            UpdateCalloutBadge(badgeNecklaceText, "RELIC", necklace);
            UpdateCalloutBadge(badgeWeaponText, "WEAPON", weapon, "Root Cleaver");

            // Aggregate Gear Stats across all slots
            int totalHp = 0;
            int totalDef = 0;
            float totalAtkSpd = 0f;
            float totalMoveSpd = 0f;
            float totalCrit = 0f;

            if (armorState != null)
            {
                foreach (EquipmentSlotType st in Enum.GetValues(typeof(EquipmentSlotType)))
                {
                    if (armorState.GetEquippedItem(st) is EquipmentItemData eq)
                    {
                        totalHp += eq.bonusHealth;
                        totalDef += eq.bonusDefense;
                        totalAtkSpd += eq.bonusAttackSpeed;
                        totalMoveSpd += eq.bonusMoveSpeed;
                        totalCrit += eq.bonusCriticalChance;
                    }
                }
            }

            if (headerSubtitleText != null)
            {
                headerSubtitleText.text = $"ARMOR LV.{armorLv} • {mitigationPct}% MITIGATION (+{totalDef} GEAR DEF)";
            }

            if (gearStatsSummaryText != null)
            {
                int spdPct = Mathf.RoundToInt((totalAtkSpd + totalMoveSpd) * 100f);
                int critPct = Mathf.RoundToInt(totalCrit * 100f);
                gearStatsSummaryText.text = $"GEAR: +{totalHp} HP • +{totalDef} DEF • +{spdPct}% SPD • +{critPct}% CRIT";
            }

            // Update Sweet Potato Set Bonus Tracker
            SetBonusState setBonus = armorState != null
                ? armorState.GetCurrentSetBonus("Sweet Potato")
                : new SetBonusState(0, 0f, 0f, false);

            int count = setBonus.EquippedPieceCount;
            if (setHeaderCountText != null)
            {
                string pips = "";
                for (int i = 0; i < 5; i++)
                {
                    pips += (i < count) ? "■" : "□";
                }
                setHeaderCountText.text = $"SWEET POTATO SET [{count}/5]  {pips}";
                setHeaderCountText.color = count >= 2 ? TierActiveGoldColor : new Color(0.85f, 0.76f, 0.50f, 0.9f);
            }

            if (setTier2Text != null)
            {
                bool active2 = count >= 2;
                setTier2Text.text = active2
                    ? "◆ [2-PC] +10% Attack Speed  [ACTIVE]"
                    : "◇ [2-PC] +10% Attack Speed";
                setTier2Text.color = active2 ? TierActiveEmeraldColor : TierInactiveColor;
                setTier2Text.fontStyle = active2 ? FontStyle.Bold : FontStyle.Normal;
            }

            if (setTier4Text != null)
            {
                bool active4 = count >= 4;
                setTier4Text.text = active4
                    ? "◆ [4-PC] +15% Post-Dodge Surge Dmg  [ACTIVE]"
                    : "◇ [4-PC] +15% Post-Dodge Surge Dmg (3s)";
                setTier4Text.color = active4 ? TierActiveEmeraldColor : TierInactiveColor;
                setTier4Text.fontStyle = active4 ? FontStyle.Bold : FontStyle.Normal;
            }

            if (setTier5Text != null)
            {
                bool active5 = count >= 5;
                setTier5Text.text = active5
                    ? "◆ [5-PC] Shadow Dash Cooldown Reset  [MAX SET]"
                    : "◇ [5-PC] Shadow Dash Cooldown Reset";
                setTier5Text.color = active5 ? TierActiveGoldColor : TierInactiveColor;
                setTier5Text.fontStyle = active5 ? FontStyle.Bold : FontStyle.Normal;
            }

            UpdateInspectorFooter();
        }

        private static void UpdateCalloutBadge(Text badge, string prefix, ItemData item, string emptyFallback = "Empty")
        {
            if (badge == null) return;
            bool hasItem = item != null;
            string shortName = hasItem ? ShortenItemName(item.itemName) : emptyFallback;
            badge.text = $"{prefix}\n{shortName}";
            badge.color = hasItem
                ? new Color(0.98f, 0.90f, 0.52f, 1f)
                : new Color(0.62f, 0.66f, 0.76f, 0.85f);

            var parentImg = badge.transform.parent != null ? badge.transform.parent.GetComponent<Image>() : null;
            if (parentImg != null)
            {
                parentImg.color = hasItem
                    ? new Color(0.18f, 0.28f, 0.22f, 0.96f)
                    : new Color(0.13f, 0.15f, 0.20f, 0.88f);
            }
        }

        private static string ShortenItemName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "Equipped";
            if (fullName.Length <= 13) return fullName;
            if (fullName.Contains("Slingshot", StringComparison.OrdinalIgnoreCase)) return "Slingshot";
            if (fullName.Contains("Swift Root", StringComparison.OrdinalIgnoreCase)) return "Swift Root";
            if (fullName.Contains("Striders", StringComparison.OrdinalIgnoreCase)) return "Striders";
            if (fullName.Contains("Potato Heart", StringComparison.OrdinalIgnoreCase)) return "SP Heart";
            return fullName.Substring(0, 12) + ".";
        }

        private void UpdateInspectorFooter()
        {
            if (inspectorTitleText == null || inspectorDetailText == null) return;

            if (currentlyHoveredSlot != null)
            {
                string tag = EquipmentSlotUI.GetShortSlotTag(currentlyHoveredSlot.slotType);
                ItemData item = currentlyHoveredSlot.EquippedItem;
                if (item != null)
                {
                    inspectorTitleText.text = $"[{tag}] {item.itemName.ToUpperInvariant()}";
                    inspectorTitleText.color = TierActiveGoldColor;

                    if (item is EquipmentItemData eq)
                    {
                        string stats = $"Set: {eq.setName}  •  +{eq.bonusHealth} HP  •  +{eq.bonusDefense} DEF";
                        if (eq.bonusAttackSpeed > 0f) stats += $"  •  +{Mathf.RoundToInt(eq.bonusAttackSpeed * 100f)}% AtkSpd";
                        if (eq.bonusMoveSpeed > 0f) stats += $"  •  +{Mathf.RoundToInt(eq.bonusMoveSpeed * 100f)}% MoveSpd";
                        if (eq.bonusCriticalChance > 0f) stats += $"  •  +{Mathf.RoundToInt(eq.bonusCriticalChance * 100f)}% Crit";
                        inspectorDetailText.text = $"{stats}\n[Click Slot to Unequip to Inventory]";
                    }
                    else if (item is ToolItemData tool)
                    {
                        string mode = tool.isRangedWeapon ? $"Ranged ({tool.projectileRange:0}m Cursor-Aim)" : "Melee Cleaver";
                        inspectorDetailText.text = $"Base Dmg: {tool.baseDamage}  •  Mode: {mode}\n[Click Slot to Unequip to Inventory]";
                    }
                    else
                    {
                        inspectorDetailText.text = $"{item.description}\n[Click Slot to Unequip to Inventory]";
                    }
                }
                else
                {
                    inspectorTitleText.text = $"[{tag} SLOT — EMPTY]";
                    inspectorTitleText.color = new Color(0.75f, 0.80f, 0.90f, 1f);
                    inspectorDetailText.text = "Drag matching gear here, or Double-Click / Right-Click gear in your Inventory to equip.";
                }
                return;
            }

            inspectorTitleText.text = "QUICK-EQUIP CONTROLS";
            inspectorTitleText.color = new Color(0.95f, 0.82f, 0.38f, 1f);
            inspectorDetailText.text = "• Double-Click or Right-Click Armor/Weapons in Bag to Equip\n• Drag & Drop onto Slots  |  Click Equipped Slot to Unequip";
        }
    }
}

