using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Core.Runtime;
using FeaturesKitchen;

public partial class StoveUIManager
{
    #region 3-Burner Multi-Slot UI Hub

    /// <summary>
    /// Ensures the 3-Burner Multi-Slot UI hub exists and cards are instantiated.
    /// Replaces old single-slot output with 3 dedicated burner status cards.
    /// </summary>
    private void EnsureBurnerUI()
    {
        if (burnerHubRoot != null) return;

        Transform parentZone = null;
        if (cookedSlotRoot != null)
        {
            parentZone = cookedSlotRoot.transform.parent;
            cookedSlotRoot.SetActive(false); // Hide old single-slot HearthTray
        }
        else if (panelStove != null)
        {
            parentZone = panelStove.transform.Find("RightContent/BottomCookZone");
        }

        if (parentZone == null) return;

        Transform existing = parentZone.Find("BurnerHubZone");
        if (existing != null)
        {
            burnerHubRoot = existing.gameObject;
            BindBurnerSlotUIs(burnerHubRoot);
            return;
        }

        GameObject hubGO = new GameObject("BurnerHubZone", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hubGO.transform.SetParent(parentZone, false);
        burnerHubRoot = hubGO;

        var hubRect = hubGO.GetComponent<RectTransform>();
        hubRect.anchorMin = new Vector2(0f, 0.5f);
        hubRect.anchorMax = new Vector2(0f, 0.5f);
        hubRect.pivot = new Vector2(0f, 0.5f);
        hubRect.anchoredPosition = new Vector2(24f, 0f);
        hubRect.sizeDelta = new Vector2(380f, 96f);

        var hlg = hubGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < KitchenStove.MaxCookingSlots; i++)
        {
            burnerSlotUIs[i] = CreateBurnerCard(hubGO.transform, i);
        }
    }

    private BurnerSlotUI CreateBurnerCard(Transform parent, int slotIndex)
    {
        var cardGO = new GameObject($"BurnerCard_{slotIndex + 1}", typeof(RectTransform), typeof(Image));
        cardGO.transform.SetParent(parent, false);

        var cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(118f, 90f);

        var bg = cardGO.GetComponent<Image>();
        bg.color = WoodenBoardStyle.DarkOakSubtle;

        TMP_FontAsset fontAsset = resultName != null ? resultName.font : null;

        // Title
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(cardGO.transform, false);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);
        titleRect.sizeDelta = new Vector2(-8f, 16f);

        var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) titleTmp.font = fontAsset;
        titleTmp.text = $"Burner {slotIndex + 1}";
        titleTmp.fontSize = 11f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = WoodenBoardStyle.TextMutedWarm;

        // Middle Row (Icon & Status Text)
        var midRowGO = new GameObject("MidRow", typeof(RectTransform));
        midRowGO.transform.SetParent(cardGO.transform, false);
        var midRect = midRowGO.GetComponent<RectTransform>();
        midRect.anchorMin = new Vector2(0f, 1f);
        midRect.anchorMax = new Vector2(1f, 1f);
        midRect.pivot = new Vector2(0.5f, 1f);
        midRect.anchoredPosition = new Vector2(0f, -22f);
        midRect.sizeDelta = new Vector2(-8f, 32f);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(midRowGO.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(2f, 0f);
        iconRect.sizeDelta = new Vector2(28f, 28f);

        var iconImg = iconGO.GetComponent<Image>();
        iconImg.enabled = false;

        var statusGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(midRowGO.transform, false);
        var statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.offsetMin = new Vector2(34f, 0f);
        statusRect.offsetMax = new Vector2(0f, 0f);

        var statusTmp = statusGO.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) statusTmp.font = fontAsset;
        statusTmp.text = "Empty\n<color=#22C55E>Ready</color>";
        statusTmp.fontSize = 10f;
        statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
        statusTmp.lineSpacing = -15f;
        statusTmp.overflowMode = TextOverflowModes.Ellipsis;

        // Mini Progress Bar
        var barRootGO = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
        barRootGO.transform.SetParent(cardGO.transform, false);
        var barRootRect = barRootGO.GetComponent<RectTransform>();
        barRootRect.anchorMin = new Vector2(0f, 0f);
        barRootRect.anchorMax = new Vector2(1f, 0f);
        barRootRect.pivot = new Vector2(0.5f, 0f);
        barRootRect.anchoredPosition = new Vector2(0f, 28f);
        barRootRect.sizeDelta = new Vector2(-12f, 4f);

        var barBgImg = barRootGO.GetComponent<Image>();
        barBgImg.color = new Color(0.18f, 0.22f, 0.30f, 1f);

        var barFillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        barFillGO.transform.SetParent(barRootGO.transform, false);
        var barFillRect = barFillGO.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;

        var barFillImg = barFillGO.GetComponent<Image>();
        barFillImg.sprite = GetWhiteSprite();
        barFillImg.color = new Color(0.96f, 0.62f, 0.04f, 1f);
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFillImg.fillAmount = 0f;
        barRootGO.SetActive(false);

        // Action Button
        var btnGO = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 0f);
        btnRect.anchorMax = new Vector2(1f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 4f);
        btnRect.sizeDelta = new Vector2(-12f, 22f);

        var btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.3f, 0.35f, 0.42f, 1f);

        var btn = btnGO.GetComponent<Button>();
        int capturedIdx = slotIndex;
        btn.onClick.AddListener(() => OnBurnerActionClicked(capturedIdx));

        var btnLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLabelGO.transform.SetParent(btnGO.transform, false);
        var btnLabelRect = btnLabelGO.GetComponent<RectTransform>();
        btnLabelRect.anchorMin = Vector2.zero;
        btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero;
        btnLabelRect.offsetMax = Vector2.zero;

        var btnLabelTmp = btnLabelGO.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) btnLabelTmp.font = fontAsset;
        btnLabelTmp.text = "";
        btnLabelTmp.fontSize = 10f;
        btnLabelTmp.fontStyle = FontStyles.Bold;
        btnLabelTmp.alignment = TextAlignmentOptions.Center;
        btnLabelTmp.color = Color.white;

        btnGO.SetActive(false);

        return new BurnerSlotUI
        {
            slotIndex = slotIndex,
            root = cardGO,
            background = bg,
            titleText = titleTmp,
            iconImage = iconImg,
            statusText = statusTmp,
            progressBarRoot = barRootGO,
            progressBarFill = barFillImg,
            actionButton = btn,
            actionButtonText = btnLabelTmp,
            actionButtonImage = btnImg
        };
    }

    private void BindBurnerSlotUIs(GameObject hubGO)
    {
        for (int i = 0; i < KitchenStove.MaxCookingSlots; i++)
        {
            var card = hubGO.transform.Find($"BurnerCard_{i + 1}");
            if (card != null)
            {
                var bg = card.GetComponent<Image>();
                var titleTmp = card.Find("Title")?.GetComponent<TextMeshProUGUI>();
                var iconImg = card.Find("MidRow/Icon")?.GetComponent<Image>();
                var statusTmp = card.Find("MidRow/StatusText")?.GetComponent<TextMeshProUGUI>();
                var barRoot = card.Find("ProgressBar")?.gameObject;
                var barFillImg = card.Find("ProgressBar/Fill")?.GetComponent<Image>();
                if (barFillImg != null)
                {
                    if (barFillImg.sprite == null)
                        barFillImg.sprite = GetWhiteSprite();
                    barFillImg.type = Image.Type.Filled;
                    barFillImg.fillMethod = Image.FillMethod.Horizontal;
                    barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                }
                var btn = card.Find("ActionButton")?.GetComponent<Button>();
                var btnImg = card.Find("ActionButton")?.GetComponent<Image>();
                var btnLabelTmp = card.Find("ActionButton/Label")?.GetComponent<TextMeshProUGUI>();

                if (btn != null)
                {
                    int capturedIdx = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnBurnerActionClicked(capturedIdx));
                }

                burnerSlotUIs[i] = new BurnerSlotUI
                {
                    slotIndex = i,
                    root = card.gameObject,
                    background = bg,
                    titleText = titleTmp,
                    iconImage = iconImg,
                    statusText = statusTmp,
                    progressBarRoot = barRoot,
                    progressBarFill = barFillImg,
                    actionButton = btn,
                    actionButtonText = btnLabelTmp,
                    actionButtonImage = btnImg
                };
            }
        }
    }

    #endregion

    #region Cooked Output Slot Presentation (Legacy Support)

    private void EnsureCookedSlotUI()
    {
        if (cookedSlotRoot != null)
        {
            if (collectCookedButton != null)
            {
                collectCookedButton.onClick.RemoveListener(OnCollectClicked);
                collectCookedButton.onClick.AddListener(OnCollectClicked);
            }
            var existingSlotBtn = cookedSlotRoot.transform.Find("SlotBox")?.GetComponent<Button>();
            if (existingSlotBtn != null)
            {
                existingSlotBtn.onClick.RemoveListener(OnCollectClicked);
                existingSlotBtn.onClick.AddListener(OnCollectClicked);
            }
            return;
        }

        Transform rightContent = panelStove != null ? panelStove.transform.Find("RightContent") : null;
        if (rightContent == null) return;

        Transform found = rightContent.Find("CookedOutputSlotZone");
        if (found != null)
        {
            cookedSlotRoot = found.gameObject;
            cookedSlotIcon = found.Find("SlotBox/Icon")?.GetComponent<Image>();
            cookedSlotCountText = found.Find("SlotBox/Count")?.GetComponent<TextMeshProUGUI>();
            collectCookedButton = found.Find("CollectButton")?.GetComponent<Button>();
            collectButtonText = collectCookedButton != null ? collectCookedButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (collectCookedButton != null)
            {
                collectCookedButton.onClick.RemoveListener(OnCollectClicked);
                collectCookedButton.onClick.AddListener(OnCollectClicked);
            }
            return;
        }
    }


    private bool _woodenThemeApplied = false;

    /// <summary>
    /// Applies the shared WoodenBoard_TwoColumn styling and aligns Left (Recipe List) / Right (Details & Burners)
    /// panes cleanly inside the gold trim borders without breaking serialized scene references.
    /// </summary>
    public void ApplyWoodenBoardTheme()
    {
        if (panelStove == null) return;

        var boardRt = panelStove.GetComponent<RectTransform>();
        if (boardRt != null)
        {
            boardRt.anchorMin = new Vector2(0.5f, 0.5f);
            boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.anchoredPosition = Vector2.zero;
            boardRt.sizeDelta = new Vector2(1240f, 694f);
        }

        var bgImg = panelStove.GetComponent<Image>() ?? panelStove.AddComponent<Image>();
        WoodenBoardStyle.ApplyBoardBackground(bgImg);

        // Align Left Column (direct child hosting recipeListContent)
        Transform leftRoot = FindDirectChildAncestor(panelStove.transform, recipeListContent)
            ?? panelStove.transform.Find("LeftContent")
            ?? panelStove.transform.Find("RecipeListPanel");
        if (leftRoot != null && leftRoot != panelStove.transform)
        {
            WoodenBoardStyle.SetAnchorRegion(leftRoot.GetComponent<RectTransform>(), WoodenBoardStyle.LeftPaneMin, WoodenBoardStyle.LeftPaneMax, 8f, 8f, 8f, 8f);
            if (leftRoot.TryGetComponent<Image>(out var lImg))
            {
                lImg.color = WoodenBoardStyle.DarkOakOverlay;
            }
        }

        // Align Right Column
        Transform rightRoot = panelStove.transform.Find("RightContent");
        if (rightRoot != null)
        {
            WoodenBoardStyle.SetAnchorRegion(rightRoot.GetComponent<RectTransform>(), WoodenBoardStyle.RightPaneMin, WoodenBoardStyle.RightPaneMax, 10f, 8f, 10f, 8f);
            if (rightRoot.TryGetComponent<Image>(out var rImg))
            {
                rImg.color = WoodenBoardStyle.DarkOakOverlay;
            }
        }

        // Suppress legacy flat header bar if present on panelStove
        if (!_woodenThemeApplied)
        {
            string[] legacyHeaders = { "Header", "HeaderBar", "TitleBar", "TopBar" };
            for (int i = 0; i < legacyHeaders.Length; i++)
            {
                Transform h = panelStove.transform.Find(legacyHeaders[i]);
                if (h != null) h.gameObject.SetActive(false);
            }
            _woodenThemeApplied = true;
        }

        // Title inside the protruding wooden plaque
        var plaqueTmp = WoodenBoardStyle.EnsurePlaqueTitle<TextMeshProUGUI>(panelStove.transform);
        if (plaqueTmp != null)
        {
            plaqueTmp.text = "KITCHEN STOVE";
            plaqueTmp.fontSize = 21f;
            plaqueTmp.fontStyle = FontStyles.Bold;
            plaqueTmp.alignment = TextAlignmentOptions.Center;
        }

        // Place Close button in upper-right rim corner
        if (closeButton != null)
        {
            var cRt = closeButton.GetComponent<RectTransform>();
            if (cRt != null)
            {
                if (cRt.parent != panelStove.transform)
                    cRt.SetParent(panelStove.transform, false);
                cRt.anchorMin = WoodenBoardStyle.CloseButtonAnchor;
                cRt.anchorMax = WoodenBoardStyle.CloseButtonAnchor;
                cRt.pivot = new Vector2(0.5f, 0.5f);
                cRt.anchoredPosition = Vector2.zero;
                cRt.sizeDelta = new Vector2(36f, 36f);
            }
            if (closeButton.TryGetComponent<Image>(out var cImg))
            {
                cImg.color = WoodenBoardStyle.BtnDanger;
            }
        }
    }

    private static Transform FindDirectChildAncestor(Transform root, Transform descendant)
    {
        if (root == null || descendant == null) return null;
        Transform curr = descendant;
        while (curr != null && curr.parent != null)
        {
            if (curr.parent == root) return curr;
            curr = curr.parent;
        }
        return null;
    }

    private static Sprite _whiteSprite;
    public static Sprite GetWhiteSprite()
    {
        if (_whiteSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return _whiteSprite;
    }

    private static void SafeDestroy(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(go);
        else
            Destroy(go);
#else
        Destroy(go);
#endif
    }

    #endregion

}
