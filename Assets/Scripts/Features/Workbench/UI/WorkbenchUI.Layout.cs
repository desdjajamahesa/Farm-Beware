using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Core.Runtime;
using FeaturesWorkbench.Core;

namespace FeaturesWorkbench.UI
{
    public partial class WorkbenchUI
    {
        // =====================================================================
        // PROCEDURAL UI HIERARCHY BUILDER (1240x760 WIDESCREEN TACTICAL BENTO)
        // =====================================================================
        private void BuildUIHierarchyIfNeeded()
        {
            if (panelRoot != null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            panelRoot = new GameObject("WorkbenchRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(transform, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            // 1. Tactical Darkened Backdrop
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(panelRoot.transform, false);
            StretchFull(backdrop.GetComponent<RectTransform>());
            backdrop.GetComponent<Image>().color = ColBackdrop;
            backdrop.GetComponent<Button>().onClick.AddListener(Close);

            // 2. Two-Column Rustic Wooden Board Container (16:9 ratio matching 1024x572)
            var borderFrame = CreatePanel("DialogBorder", panelRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ColWindowBorder);
            borderFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 715f);

            var dialog = CreatePanel("Dialog", borderFrame.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
            dialog.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 715f);
            var dialogImg = dialog.GetComponent<Image>();
            WoodenBoardStyle.ApplyBoardBackground(dialogImg);
            dialogImg.raycastTarget = true;

            // 3. Plaque Title + Close Button on Top Rim
            var plaqueTmp = WoodenBoardStyle.EnsurePlaqueTitle<TextMeshProUGUI>(dialog.transform);
            if (plaqueTmp != null)
            {
                plaqueTmp.text = "HOMESTEAD WORKBENCH";
                plaqueTmp.fontSize = 20f;
                plaqueTmp.fontStyle = FontStyles.Bold;
                plaqueTmp.alignment = TextAlignmentOptions.Center;
            }

            var closeBtnObj = CreateButton(dialog.transform, WoodenBoardStyle.CloseButtonAnchor, WoodenBoardStyle.CloseButtonAnchor, "<b>✕</b>", Close, ColAccentCrimson, 16f);
            closeBtnObj.name = "CloseX";
            var closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.sizeDelta = new Vector2(38f, 38f);

            // 3b. Left Column Top Telemetry Card (Gold & Materials inside Left Pane top)
            var telemetryCard = CreateCard("TelemetryHeaderCard", dialog.transform,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x + 0.004f, 0.765f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x - 0.004f, WoodenBoardStyle.LeftPaneMax.y - 0.005f));
            goldText = CreateLabel("GoldText", telemetryCard.transform, new Vector2(0.05f, 0.56f), new Vector2(0.95f, 0.96f),
                "GOLD: 0", 14f, TextAlignmentOptions.MidlineLeft);
            materialLedgerText = CreateLabel("MaterialLedger", telemetryCard.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.54f),
                "", 10.5f, TextAlignmentOptions.MidlineLeft);

            // 4. Primary Navigation Strip (4 Tactical Module Tabs at top of Right Column)
            tabWeaponButton = CreateNavTab(dialog.transform, new Vector2(0.378f, 0.785f), new Vector2(0.510f, 0.858f),
                "<b>01 // WEAPONRY</b>\n<size=10>Forging & Mastery</size>",
                () => SetTab(WorkbenchTab.Weapon), out tabWeaponButtonText, out tabWeaponIndicator);

            tabArmorButton = CreateNavTab(dialog.transform, new Vector2(0.516f, 0.785f), new Vector2(0.648f, 0.858f),
                "<b>02 // ARMOR SETS</b>\n<size=10>Plating & 5-Pc Set</size>",
                () => SetTab(WorkbenchTab.Armor), out tabArmorButtonText, out tabArmorIndicator);

            tabFarmlandButton = CreateNavTab(dialog.transform, new Vector2(0.654f, 0.785f), new Vector2(0.786f, 0.858f),
                "<b>03 // LAND DEEDS</b>\n<size=10>Plot Expansion</size>",
                () => SetTab(WorkbenchTab.Farmland), out tabFarmlandButtonText, out tabFarmlandIndicator);

            tabOverdriveButton = CreateNavTab(dialog.transform, new Vector2(0.792f, 0.785f), new Vector2(0.924f, 0.858f),
                "<b>04 // BIO-REACTOR</b>\n<size=10>Night Overdrive</size>",
                () => SetTab(WorkbenchTab.Overdrive), out tabOverdriveButtonText, out tabOverdriveIndicator);

            // =================================================================
            // TAB 01 CONTENT: WEAPONRY & DOCTRINE TREE (Left Pane / Right Pane)
            // =================================================================
            weaponTabContent = CreateContainer("WeaponTabContent", dialog.transform, Vector2.zero, Vector2.one);

            // Left Column: Weapon Calibration Card (Top) + Arsenal Loadout Foundry (Bottom)
            var wpLeftTop = CreateCard("WeaponCalibrationCard", weaponTabContent.transform,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x + 0.004f, 0.415f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x - 0.004f, 0.752f));
            CreateLabel("SectionTag", wpLeftTop.transform, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.96f),
                "<color=#F5AE2E><b>WEAPON CALIBRATION</b></color>", 11.5f, TextAlignmentOptions.MidlineLeft);

            var iconFrame = CreatePanel("WeaponIconFrame", wpLeftTop.transform, new Vector2(0.05f, 0.50f), new Vector2(0.28f, 0.84f), WoodenBoardStyle.DarkOakInput);
            var iconInner = new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
            iconInner.transform.SetParent(iconFrame.transform, false);
            SetAnchors(iconInner.GetComponent<RectTransform>(), new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            weaponIconImage = iconInner.GetComponent<Image>();
            weaponIconImage.preserveAspect = true;

            weaponPipBarText = CreateLabel("WeaponPipBar", wpLeftTop.transform, new Vector2(0.31f, 0.74f), new Vector2(0.96f, 0.85f),
                "", 11.5f, TextAlignmentOptions.MidlineLeft);

            weaponStatsText = CreateLabel("WeaponStats", wpLeftTop.transform, new Vector2(0.31f, 0.34f), new Vector2(0.96f, 0.73f),
                "", 11.5f, TextAlignmentOptions.TopLeft);

            var upgBtnObj = CreateButton(wpLeftTop.transform, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.25f),
                "FORGE WEAPON UPGRADE", OnClickUpgradeBase, ColBtnPrimary, 11.5f);
            upgradeBaseButton = upgBtnObj.GetComponent<Button>();
            upgradeBaseButtonText = upgBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Left Column Bottom: Arsenal Loadout & Corn Cob Repeater Foundry
            var wpLeftBot = CreateCard("ArsenalFoundryCard", weaponTabContent.transform,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x + 0.004f, WoodenBoardStyle.LeftPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x - 0.004f, 0.402f));
            CreateLabel("ArsenalHeader", wpLeftBot.transform, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.96f),
                "<color=#FFD56B><b>ARSENAL & RANGED FOUNDRY</b></color>", 11f, TextAlignmentOptions.MidlineLeft);

            arsenalStatusText = CreateLabel("ArsenalStatus", wpLeftBot.transform, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.81f),
                "", 10.5f, TextAlignmentOptions.TopLeft);

            var eqCleaverObj = CreateButton(wpLeftBot.transform, new Vector2(0.05f, 0.07f), new Vector2(0.48f, 0.31f),
                "EQUIP CLEAVER", OnClickEquipRootCleaver, ColBtnSecondary, 10.5f);
            equipCleaverButton = eqCleaverObj.GetComponent<Button>();
            equipCleaverButtonText = eqCleaverObj.GetComponentInChildren<TextMeshProUGUI>();

            var repBtnObj = CreateButton(wpLeftBot.transform, new Vector2(0.51f, 0.07f), new Vector2(0.95f, 0.31f),
                "FORGE REPEATER", OnClickCraftOrEquipRepeater, ColAccentAmber, 10.5f);
            craftOrEquipRepeaterButton = repBtnObj.GetComponent<Button>();
            craftOrEquipRepeaterButtonText = repBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Right Column: 6-Node Seed Doctrine Specialization Tree Inspector
            var wpRight = CreateCard("SpecializationMatrixCard", weaponTabContent.transform,
                new Vector2(WoodenBoardStyle.RightPaneMin.x + 0.004f, WoodenBoardStyle.RightPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.RightPaneMax.x - 0.004f, 0.772f));

            specializationHeader = CreateLabel("SpecHeader", wpRight.transform, new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.98f),
                "<b>SEED DOCTRINE SPECIALIZATION MATRIX</b>", 14f, TextAlignmentOptions.MidlineLeft);

            specializationSubHeader = CreateLabel("SpecSubHeader", wpRight.transform, new Vector2(0.03f, 0.865f), new Vector2(0.97f, 0.915f),
                "", 11.5f, TextAlignmentOptions.MidlineLeft);

            // 3 Doctrine Path Selector Tabs (Horizontal Row: 0.755 to 0.855)
            var spBtnObj = CreateButton(wpRight.transform, new Vector2(0.03f, 0.755f), new Vector2(0.335f, 0.855f),
                "Sweet Potato", () => OnClickInspectPath(SpecializationPath.SweetPotato), ColTabInactive, 12f);
            sweetPotatoButton = spBtnObj.GetComponent<Button>();
            sweetPotatoButtonText = spBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            var taroBtnObj = CreateButton(wpRight.transform, new Vector2(0.347f, 0.755f), new Vector2(0.652f, 0.855f),
                "Taro", () => OnClickInspectPath(SpecializationPath.Taro), ColTabInactive, 12f);
            taroButton = taroBtnObj.GetComponent<Button>();
            taroButtonText = taroBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            var cornBtnObj = CreateButton(wpRight.transform, new Vector2(0.665f, 0.755f), new Vector2(0.97f, 0.855f),
                "Corn", () => OnClickInspectPath(SpecializationPath.Corn), ColTabInactive, 12f);
            cornButton = cornBtnObj.GetComponent<Button>();
            cornButtonText = cornBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // 6-Node Bento Grid (2 Columns x 3 Rows: 0.16 to 0.73)
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int row = i / 2; // 0 = top, 1 = mid, 2 = bottom

                float xMin = col == 0 ? 0.03f : 0.51f;
                float xMax = col == 0 ? 0.49f : 0.97f;
                float yMax = 0.73f - (row * 0.19f);
                float yMin = yMax - 0.172f;

                var nodeCard = CreatePanel($"NodeCard_{i + 1}", wpRight.transform, new Vector2(xMin, yMin), new Vector2(xMax, yMax), ColCardSubtle);
                nodeCardBackgrounds[i] = nodeCard.GetComponent<Image>();

                var accent = CreatePanel("AccentBar", nodeCard.transform, new Vector2(0f, 0f), new Vector2(0.018f, 1f), ColAccentAmber);
                nodeAccentBars[i] = accent.GetComponent<Image>();

                nodeCardTexts[i] = CreateLabel("NodeText", nodeCard.transform, new Vector2(0.045f, 0.06f), new Vector2(0.97f, 0.94f),
                    "", 11.5f, TextAlignmentOptions.MidlineLeft);
            }

            // Bottom Action Buttons for Doctrine Tree (0.03 to 0.13)
            var commitObj = CreateButton(wpRight.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.13f),
                "COMMIT TO DOCTRINE PATH", OnClickCommitInspectedPath, ColBtnPrimary, 13.5f);
            commitPathButton = commitObj.GetComponent<Button>();
            commitPathButtonText = commitObj.GetComponentInChildren<TextMeshProUGUI>();

            var advObj = CreateButton(wpRight.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.13f),
                "UNLOCK NEXT DOCTRINE NODE", OnClickAdvanceSpecialization, ColBtnPrimary, 13.5f);
            advanceSpecButton = advObj.GetComponent<Button>();
            advanceSpecButtonText = advObj.GetComponentInChildren<TextMeshProUGUI>();
            advanceSpecButton.gameObject.SetActive(false);

            // =================================================================
            // TAB 02 CONTENT: ARMOR & COMBAT SETS (Left Pane / Right Pane)
            // =================================================================
            armorTabContent = CreateContainer("ArmorTabContent", dialog.transform, Vector2.zero, Vector2.one);

            var arLeftTop = CreateCard("ArmorCalibrationCard", armorTabContent.transform,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x + 0.004f, 0.425f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x - 0.004f, 0.752f));
            CreateLabel("ArmorTag", arLeftTop.transform, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f),
                "<color=#FFD56B><b>HOMESTEAD ARMOR</b></color>", 11.5f, TextAlignmentOptions.MidlineLeft);

            armorPipBarText = CreateLabel("ArmorPipBar", arLeftTop.transform, new Vector2(0.05f, 0.71f), new Vector2(0.95f, 0.83f),
                "", 11.5f, TextAlignmentOptions.MidlineLeft);

            armorStatsText = CreateLabel("ArmorStats", arLeftTop.transform, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.70f),
                "", 11.5f, TextAlignmentOptions.TopLeft);

            var upgArmObj = CreateButton(arLeftTop.transform, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.27f),
                "UPGRADE ARMOR PLATING", OnClickUpgradeArmor, ColBtnPrimary, 11.5f);
            upgradeArmorButton = upgArmObj.GetComponent<Button>();
            upgradeArmorButtonText = upgArmObj.GetComponentInChildren<TextMeshProUGUI>();

            var arLeftBot = CreateCard("SetSynergyCard", armorTabContent.transform,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x + 0.004f, WoodenBoardStyle.LeftPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x - 0.004f, 0.412f));
            setSynergyMatrixText = CreateLabel("SetSynergyText", arLeftBot.transform, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f),
                "", 11f, TextAlignmentOptions.TopLeft);

            // Right Column: 5-Piece Sweet Potato Combat Set Blueprint Matrix
            var arRight = CreateCard("SetBlueprintMatrixCard", armorTabContent.transform,
                new Vector2(WoodenBoardStyle.RightPaneMin.x + 0.004f, WoodenBoardStyle.RightPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.RightPaneMax.x - 0.004f, 0.772f));
            setCraftingContainer = arRight.transform;

            CreateLabel("BlueprintHeader", arRight.transform, new Vector2(0.03f, 0.91f), new Vector2(0.97f, 0.98f),
                "<b>SWEET POTATO COMBAT SET // FORGE BLUEPRINTS (5 PIECES)</b>", 14f, TextAlignmentOptions.MidlineLeft);
            CreateLabel("BlueprintSubHeader", arRight.transform, new Vector2(0.03f, 0.855f), new Vector2(0.97f, 0.905f),
                "<color=#E6D7BA>Craft set pieces using Mutated Roots dropped by Tuber Maws. Crafted pieces can be equipped in 1 click or via Tab Character Sheet.</color>",
                11.5f, TextAlignmentOptions.MidlineLeft);

            InitBlueprintData();
            for (int i = 0; i < setBlueprints.Length; i++)
            {
                int idx = i;
                float yMax = 0.83f - (i * 0.16f);
                float yMin = yMax - 0.142f;

                var bpRow = CreatePanel($"BlueprintRow_{i}", arRight.transform, new Vector2(0.03f, yMin), new Vector2(0.97f, yMax), ColCardSubtle);
                CreatePanel("LeftAccent", bpRow.transform, new Vector2(0f, 0f), new Vector2(0.012f, 1f), ColAccentViolet);

                var badgeObj = CreatePanel("StatusBadge", bpRow.transform, new Vector2(0.56f, 0.56f), new Vector2(0.71f, 0.88f), ColTabInactive);
                setBlueprints[i].StatusBadgeBg = badgeObj.GetComponent<Image>();
                setBlueprints[i].StatusBadgeText = CreateLabel("BadgeTxt", badgeObj.transform, Vector2.zero, Vector2.one,
                    "UNFORGED", 10.5f, TextAlignmentOptions.Center);

                setBlueprints[i].CardText = CreateLabel("BlueprintInfo", bpRow.transform, new Vector2(0.03f, 0.08f), new Vector2(0.55f, 0.92f),
                    "", 12f, TextAlignmentOptions.MidlineLeft);

                var actBtnObj = CreateButton(bpRow.transform, new Vector2(0.73f, 0.16f), new Vector2(0.98f, 0.84f),
                    "CRAFT & EQUIP", () => OnClickBlueprintAction(idx), ColBtnPrimary, 12f);
                setBlueprints[i].ActionButton = actBtnObj.GetComponent<Button>();
                setBlueprints[i].ActionButtonText = actBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            }

            armorTabContent.SetActive(false);

            // =================================================================
            // TAB 03 CONTENT: FARMLAND EXPANSION & LAND DEEDS
            // =================================================================
            farmlandTabContent = CreateContainer("FarmlandTabContent", dialog.transform, Vector2.zero, Vector2.one);

            var flBanner = CreateCard("FarmlandBanner", farmlandTabContent.transform,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x + 0.004f, WoodenBoardStyle.LeftPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x - 0.004f, 0.752f));
            farmlandStatusBannerText = CreateLabel("FlBannerText", flBanner.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f),
                "", 13f, TextAlignmentOptions.TopLeft);

            var flBodyCard = CreateCard("FarmlandTelemetryCard", farmlandTabContent.transform,
                new Vector2(WoodenBoardStyle.RightPaneMin.x + 0.004f, 0.225f),
                new Vector2(WoodenBoardStyle.RightPaneMax.x - 0.004f, 0.772f));
            farmlandTelemetryText = CreateLabel("FlTelemetryText", flBodyCard.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                "", 14f, TextAlignmentOptions.MidlineLeft);

            var expandBtnObj = CreateButton(farmlandTabContent.transform,
                new Vector2(WoodenBoardStyle.RightPaneMin.x + 0.004f, WoodenBoardStyle.RightPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.RightPaneMax.x - 0.004f, 0.205f),
                "PURCHASE LAND DEED (+2 PLOTS)", OnClickExpandFarmland, ColBtnPrimary, 15f);
            expandFarmlandButton = expandBtnObj.GetComponent<Button>();
            expandFarmlandButtonText = expandBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            farmlandTabContent.SetActive(false);

            // =================================================================
            // TAB 04 CONTENT: BIO-CATALYST NIGHT OVERDRIVE REACTOR
            // =================================================================
            overdriveTabContent = CreateContainer("OverdriveTabContent", dialog.transform, Vector2.zero, Vector2.one);

            var odBanner = CreateCard("OverdriveBanner", overdriveTabContent.transform,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x + 0.004f, WoodenBoardStyle.LeftPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x - 0.004f, 0.752f));
            overdriveStatusBannerBg = odBanner.GetComponent<Image>();
            overdriveStatusBannerText = CreateLabel("OdBannerText", odBanner.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f),
                "", 13f, TextAlignmentOptions.TopLeft);

            var odBodyCard = CreateCard("OverdriveTelemetryCard", overdriveTabContent.transform,
                new Vector2(WoodenBoardStyle.RightPaneMin.x + 0.004f, 0.225f),
                new Vector2(WoodenBoardStyle.RightPaneMax.x - 0.004f, 0.772f));
            overdriveTelemetryText = CreateLabel("OdTelemetryText", odBodyCard.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                "", 14f, TextAlignmentOptions.MidlineLeft);

            var odArmBtnObj = CreateButton(overdriveTabContent.transform,
                new Vector2(WoodenBoardStyle.RightPaneMin.x + 0.004f, WoodenBoardStyle.RightPaneMin.y + 0.004f),
                new Vector2(WoodenBoardStyle.RightPaneMax.x - 0.004f, 0.205f),
                "INJECT 2x MONSTER BIO-MATERIALS & ARM NIGHT OVERDRIVE", OnClickArmNightOverdrive, new Color(0.76f, 0.34f, 0.10f, 1f), 14.5f);
            armOverdriveButton = odArmBtnObj.GetComponent<Button>();
            armOverdriveButtonText = odArmBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            overdriveTabContent.SetActive(false);
        }


        // =====================================================================
        // CLEAN UI PRIMITIVE HELPERS
        // =====================================================================
        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateContainer(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateCard(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var border = CreatePanel(name + "_Border", parent, anchorMin, anchorMax, ColCardBorder);
            var inner = CreatePanel(name, border.transform, Vector2.zero, Vector2.one, ColCardBg);
            var rt = inner.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);
            return inner;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, float fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = ColTextPrimary;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Button CreateNavTab(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, Action onClick, out TextMeshProUGUI labelOut, out Image indicatorOut)
        {
            var go = CreateButton(parent, anchorMin, anchorMax, text, onClick, ColTabInactive, 13f);
            var btn = go.GetComponent<Button>();
            labelOut = go.GetComponentInChildren<TextMeshProUGUI>();

            var ind = CreatePanel("ActiveIndicator", go.transform, new Vector2(0f, 0f), new Vector2(1f, 0.08f), ColAccentAmber);
            indicatorOut = ind.GetComponent<Image>();
            return btn;
        }

        private static GameObject CreateButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, Action onClick, Color bgColor, float fontSize = 13f)
        {
            var btnObj = new GameObject("Btn_" + text.Substring(0, Mathf.Min(12, text.Length)).Replace(" ", ""), typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            SetAnchors(btnObj.GetComponent<RectTransform>(), anchorMin, anchorMax);

            var img = btnObj.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8f, 2f);
            trt.offsetMax = new Vector2(-8f, -2f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btnObj;
        }

    }
}
