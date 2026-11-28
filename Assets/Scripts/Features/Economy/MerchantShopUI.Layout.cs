using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Core.Runtime;

namespace FeaturesEconomy
{
    public partial class MerchantShopUI
    {
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

            ApplyWoodenBoardLayout();

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

                var srt = scrollRect.GetComponent<RectTransform>();
                if (srt != null)
                {
                    WoodenBoardStyle.SetAnchorRegion(srt, WoodenBoardStyle.RightPaneMin, WoodenBoardStyle.RightPaneMax, 10f, 8f, 10f, 8f);
                }
            }

            // 8. Category Filter Bar (Vertical stack in Left Column: All, Weapons & Armor, Seeds, Supplies)
            categoryFilterBar = cardTr.Find("CategoryFilterBar");
            if (categoryFilterBar == null)
            {
                GameObject filterBarGO = new GameObject("CategoryFilterBar", typeof(RectTransform), typeof(VerticalLayoutGroup));
                filterBarGO.transform.SetParent(cardTr, false);
                categoryFilterBar = filterBarGO.transform;

                var fbRect = filterBarGO.GetComponent<RectTransform>();
                WoodenBoardStyle.SetAnchorRegion(fbRect,
                    new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.245f),
                    new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.612f),
                    10f, 4f, 10f, 4f);

                var fLayout = filterBarGO.GetComponent<VerticalLayoutGroup>();
                fLayout.padding = new RectOffset(0, 0, 0, 0);
                fLayout.spacing = 8;
                fLayout.childAlignment = TextAnchor.UpperCenter;
                fLayout.childControlWidth = true;
                fLayout.childControlHeight = true;
                fLayout.childForceExpandWidth = true;
                fLayout.childForceExpandHeight = true;

                string[] labels = new string[] { "ALL", "WEAPONS & ARMOR", "SEEDS", "SUPPLIES" };
                ShopCategoryFilter[] filterEnums = new ShopCategoryFilter[] {
                    ShopCategoryFilter.All,
                    ShopCategoryFilter.WeaponsAndArmor,
                    ShopCategoryFilter.Seeds,
                    ShopCategoryFilter.Supplies
                };

                filterButtons.Clear();
                filterButtonImages.Clear();
                filterButtonTexts.Clear();

                for (int i = 0; i < labels.Length; i++)
                {
                    int idx = i;
                    var fEnum = filterEnums[i];
                    GameObject fBtnGO = new GameObject($"FilterBtn_{labels[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
                    fBtnGO.transform.SetParent(categoryFilterBar, false);

                    var fImg = fBtnGO.GetComponent<Image>();
                    fImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    fImg.type = Image.Type.Sliced;
                    fImg.color = (idx == (int)currentFilter) ? WoodenBoardStyle.TabActive : WoodenBoardStyle.TabInactive;

                    var fBtn = fBtnGO.GetComponent<Button>();
                    fBtn.onClick.AddListener(() => SetCategoryFilter(fEnum));

                    GameObject fTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    fTxtGO.transform.SetParent(fBtnGO.transform, false);
                    var ftr = fTxtGO.GetComponent<RectTransform>();
                    ftr.anchorMin = Vector2.zero;
                    ftr.anchorMax = Vector2.one;
                    ftr.sizeDelta = Vector2.zero;

                    var fTmp = fTxtGO.GetComponent<TextMeshProUGUI>();
                    fTmp.text = labels[i];
                    fTmp.fontSize = 12.5f;
                    fTmp.fontStyle = (idx == (int)currentFilter) ? FontStyles.Bold : FontStyles.Normal;
                    fTmp.alignment = TextAlignmentOptions.Center;
                    fTmp.color = (idx == (int)currentFilter) ? WoodenBoardStyle.TextGoldAccent : WoodenBoardStyle.TextMutedWarm;
                    fTmp.raycastTarget = false;

                    filterButtons.Add(fBtn);
                    filterButtonImages.Add(fImg);
                    filterButtonTexts.Add(fTmp);
                }
            }
            else
            {
                filterButtons.Clear();
                filterButtonImages.Clear();
                filterButtonTexts.Clear();
                ShopCategoryFilter[] filterEnums = new ShopCategoryFilter[] {
                    ShopCategoryFilter.All,
                    ShopCategoryFilter.WeaponsAndArmor,
                    ShopCategoryFilter.Seeds,
                    ShopCategoryFilter.Supplies
                };
                for (int i = 0; i < categoryFilterBar.childCount; i++)
                {
                    var child = categoryFilterBar.GetChild(i);
                    var btn = child.GetComponent<Button>();
                    var img = child.GetComponent<Image>();
                    var txt = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (btn != null && i < filterEnums.Length)
                    {
                        var fEnum = filterEnums[i];
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SetCategoryFilter(fEnum));
                        filterButtons.Add(btn);
                        if (img != null) filterButtonImages.Add(img);
                        if (txt != null) filterButtonTexts.Add(txt);
                    }
                }
            }

            var buyTabTxt = cardTr.Find("TabsBar/Tab_Buy/Text")?.GetComponent<TextMeshProUGUI>();
            if (buyTabTxt != null) buyTabTxt.text = "BUY ITEMS";

            var subTxt = cardTr.Find("HeaderBar/SubtitleText")?.GetComponent<TextMeshProUGUI>();
            if (subTxt != null) subTxt.text = "Purchase seeds, gear, or sell crops";

            EnsureAudioSource();

            var toastTr = cardTr.Find("ToastBannerText");
            if (toastTr != null)
            {
                toastBannerText = toastTr.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject toastGO = new GameObject("ToastBannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
                toastGO.transform.SetParent(cardTr, false);
                var tRect = toastGO.GetComponent<RectTransform>();
                WoodenBoardStyle.SetAnchorRegion(tRect,
                    new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.152f),
                    new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.236f),
                    10f, 2f, 10f, 2f);

                toastBannerText = toastGO.GetComponent<TextMeshProUGUI>();
                toastBannerText.fontSize = 13;
                toastBannerText.fontStyle = FontStyles.Bold;
                toastBannerText.alignment = TextAlignmentOptions.Center;
                toastBannerText.textWrappingMode = TextWrappingModes.Normal;
                toastBannerText.raycastTarget = false;
                toastBannerText.text = "";
            }

            ApplyWoodenBoardLayout();
        }

        /// <summary>
        /// Aligns both procedural and pre-existing scene ShopModalCard hierarchies strictly inside the
        /// two-column WoodenBoard_TwoColumn plaque and left/right plank compartments.
        /// </summary>
        public void ApplyWoodenBoardLayout()
        {
            if (shopPanel == null) return;
            var cardTr = shopPanel.transform.Find("ShopModalCard");
            if (cardTr == null) return;

            var cardRect = cardTr.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.sizeDelta = new Vector2(1180f, 659f);
            }

            var cardImg = cardTr.GetComponent<Image>() ?? cardTr.gameObject.AddComponent<Image>();
            WoodenBoardStyle.ApplyBoardBackground(cardImg);
            cardImg.raycastTarget = true;

            var plaqueTmp = WoodenBoardStyle.EnsurePlaqueTitle<TextMeshProUGUI>(cardTr);
            if (plaqueTmp != null)
            {
                plaqueTmp.text = "MERCHANT SHOP";
                plaqueTmp.fontSize = 22f;
                plaqueTmp.fontStyle = FontStyles.Bold;
                plaqueTmp.alignment = TextAlignmentOptions.Center;
            }

            var headerTr = cardTr.Find("HeaderBar");
            if (headerTr != null)
            {
                var hRt = headerTr.GetComponent<RectTransform>();
                if (hRt != null)
                {
                    hRt.anchorMin = Vector2.zero;
                    hRt.anchorMax = Vector2.one;
                    hRt.pivot = new Vector2(0.5f, 0.5f);
                    hRt.anchoredPosition = Vector2.zero;
                    hRt.offsetMin = Vector2.zero;
                    hRt.offsetMax = Vector2.zero;
                }
                if (headerTr.TryGetComponent<Image>(out var hImg)) hImg.enabled = false;

                var legacyTitle = headerTr.Find("TitleText");
                if (legacyTitle != null) legacyTitle.gameObject.SetActive(false);

                var subTr = headerTr.Find("SubtitleText");
                if (subTr != null)
                {
                    WoodenBoardStyle.SetAnchorRegion(subTr.GetComponent<RectTransform>(),
                        new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.798f),
                        new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.862f),
                        10f, 2f, 10f, 2f);
                    var subTmp = subTr.GetComponent<TextMeshProUGUI>();
                    if (subTmp != null)
                    {
                        subTmp.text = "Purchase seeds, gear, or sell crops";
                        subTmp.fontSize = 12f;
                        subTmp.color = WoodenBoardStyle.TextMutedWarm;
                        subTmp.alignment = TextAlignmentOptions.Center;
                        subTmp.textWrappingMode = TextWrappingModes.Normal;
                        subTmp.raycastTarget = false;
                    }
                }

                var xBtnTr = headerTr.Find("Btn_XClose");
                if (xBtnTr != null)
                {
                    var xRt = xBtnTr.GetComponent<RectTransform>();
                    if (xRt != null)
                    {
                        xRt.anchorMin = WoodenBoardStyle.CloseButtonAnchor;
                        xRt.anchorMax = WoodenBoardStyle.CloseButtonAnchor;
                        xRt.pivot = new Vector2(0.5f, 0.5f);
                        xRt.anchoredPosition = Vector2.zero;
                        xRt.sizeDelta = new Vector2(36f, 36f);
                    }
                    if (xBtnTr.TryGetComponent<Image>(out var xImg))
                    {
                        xImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                        xImg.type = Image.Type.Sliced;
                        xImg.color = WoodenBoardStyle.BtnDanger;
                    }
                }
            }

            var goldPillTr = cardTr.Find("GoldPill");
            if (goldPillTr != null)
            {
                WoodenBoardStyle.SetAnchorRegion(goldPillTr.GetComponent<RectTransform>(),
                    new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.718f),
                    new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.790f),
                    10f, 2f, 10f, 2f);
                if (goldPillTr.TryGetComponent<Image>(out var gpImg))
                {
                    gpImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    gpImg.type = Image.Type.Sliced;
                    gpImg.color = WoodenBoardStyle.DarkOakHeader;
                }
                var goldTmp = goldPillTr.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
                if (goldTmp != null)
                {
                    var gRt = goldTmp.GetComponent<RectTransform>();
                    if (gRt != null)
                    {
                        gRt.anchorMin = Vector2.zero;
                        gRt.anchorMax = Vector2.one;
                        gRt.offsetMin = new Vector2(8f, 0f);
                        gRt.offsetMax = new Vector2(-8f, 0f);
                    }
                    goldTmp.fontSize = 16f;
                    goldTmp.fontStyle = FontStyles.Bold;
                    goldTmp.alignment = TextAlignmentOptions.Center;
                    goldTmp.color = WoodenBoardStyle.TextCream;
                    goldTmp.raycastTarget = false;
                }
            }

            var tabsBarTr = cardTr.Find("TabsBar");
            if (tabsBarTr != null)
            {
                WoodenBoardStyle.SetAnchorRegion(tabsBarTr.GetComponent<RectTransform>(),
                    new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.628f),
                    new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.706f),
                    10f, 2f, 10f, 2f);
                if (tabsBarTr.TryGetComponent<Image>(out var tbImg)) tbImg.enabled = false;

                ConfigureShopTab(tabsBarTr.Find("Tab_Buy"), 0f, 0.485f, "BUY ITEMS");
                ConfigureShopTab(tabsBarTr.Find("Tab_Sell"), 0.515f, 1f, "SELL CROPS");
            }

            var filterTr = cardTr.Find("CategoryFilterBar");
            if (filterTr != null)
            {
                WoodenBoardStyle.SetAnchorRegion(filterTr.GetComponent<RectTransform>(),
                    new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.245f),
                    new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.612f),
                    10f, 4f, 10f, 4f);
                if (filterTr.TryGetComponent<Image>(out var fbBgImg)) fbBgImg.enabled = false;

                bool useManualBands = false;
                if (filterTr.TryGetComponent<HorizontalLayoutGroup>(out var oldHlg))
                {
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(oldHlg);
                    }
                    else
                    {
                        oldHlg.enabled = false;
                        useManualBands = true;
                    }
                }

                if (!useManualBands)
                {
                    var vlg = filterTr.GetComponent<VerticalLayoutGroup>() ?? filterTr.gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.padding = new RectOffset(0, 0, 0, 0);
                    vlg.spacing = 8f;
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = true;
                }

                int childCount = filterTr.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = filterTr.GetChild(i);
                    if (child.TryGetComponent<Image>(out var cImg))
                    {
                        cImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                        cImg.type = Image.Type.Sliced;
                    }
                    var cTmp = child.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (cTmp != null)
                    {
                        cTmp.fontSize = 12.5f;
                        cTmp.alignment = TextAlignmentOptions.Center;
                        cTmp.raycastTarget = false;
                    }
                    if (useManualBands && childCount == 4)
                    {
                        float maxY = 1f - i * 0.255f;
                        float minY = Mathf.Max(0f, maxY - 0.225f);
                        WoodenBoardStyle.SetAnchorRegion(child.GetComponent<RectTransform>(),
                            new Vector2(0f, minY), new Vector2(1f, maxY));
                    }
                }
            }

            var toastBannerTr = cardTr.Find("ToastBannerText");
            if (toastBannerTr != null)
            {
                WoodenBoardStyle.SetAnchorRegion(toastBannerTr.GetComponent<RectTransform>(),
                    new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.152f),
                    new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.236f),
                    10f, 2f, 10f, 2f);
            }

            var bottomBarTr = cardTr.Find("BottomBar");
            if (bottomBarTr != null)
            {
                WoodenBoardStyle.SetAnchorRegion(bottomBarTr.GetComponent<RectTransform>(),
                    new Vector2(WoodenBoardStyle.LeftPaneMin.x, WoodenBoardStyle.LeftPaneMin.y),
                    new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.144f),
                    10f, 4f, 10f, 2f);
                if (bottomBarTr.TryGetComponent<Image>(out var bbImg)) bbImg.enabled = false;

                var closeShopTr = bottomBarTr.Find("Btn_CloseShop");
                if (closeShopTr != null)
                {
                    var cRt = closeShopTr.GetComponent<RectTransform>();
                    if (cRt != null)
                    {
                        cRt.anchorMin = Vector2.zero;
                        cRt.anchorMax = Vector2.one;
                        cRt.pivot = new Vector2(0.5f, 0.5f);
                        cRt.anchoredPosition = Vector2.zero;
                        cRt.offsetMin = Vector2.zero;
                        cRt.offsetMax = Vector2.zero;
                    }
                    if (closeShopTr.TryGetComponent<Image>(out var csImg))
                    {
                        csImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                        csImg.type = Image.Type.Sliced;
                        csImg.color = WoodenBoardStyle.BtnSecondary;
                    }
                    var csTmp = closeShopTr.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    if (csTmp != null)
                    {
                        csTmp.fontSize = 13.5f;
                        csTmp.fontStyle = FontStyles.Bold;
                        csTmp.alignment = TextAlignmentOptions.Center;
                        csTmp.color = Color.white;
                        csTmp.raycastTarget = false;
                    }
                }
            }

            var itemScrollTr = cardTr.Find("ItemScrollView");
            if (itemScrollTr != null)
            {
                WoodenBoardStyle.SetAnchorRegion(itemScrollTr.GetComponent<RectTransform>(),
                    WoodenBoardStyle.RightPaneMin,
                    WoodenBoardStyle.RightPaneMax,
                    10f, 8f, 10f, 8f);
                if (itemScrollTr.TryGetComponent<Image>(out var svImg))
                {
                    svImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                    svImg.type = Image.Type.Sliced;
                    svImg.color = WoodenBoardStyle.DarkOakOverlay;
                }
                var vp = itemScrollTr.Find("Viewport")?.GetComponent<RectTransform>();
                if (vp != null)
                {
                    vp.anchorMin = Vector2.zero;
                    vp.anchorMax = Vector2.one;
                    vp.offsetMin = Vector2.zero;
                    vp.offsetMax = Vector2.zero;
                }
            }
        }

        private static void ConfigureShopTab(Transform tabTr, float minX, float maxX, string label)
        {
            if (tabTr == null) return;
            var rt = tabTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(minX, 0f);
                rt.anchorMax = new Vector2(maxX, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            if (tabTr.TryGetComponent<Image>(out var img))
            {
                img.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
                img.type = Image.Type.Sliced;
            }
            var tmp = tabTr.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = label;
                tmp.fontSize = 12.5f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
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
                var existing = uiCanvasGO.transform.Find("Panel_MerchantShop");
                if (existing != null)
                {
                    shopPanel = existing.gameObject;
                    ResolveReferencesAndBind();
                    return;
                }
            }

            if (screenCanvas == null)
            {
                var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var canvas in allCanvases)
                {
                    if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        screenCanvas = canvas;
                        var existing = canvas.transform.Find("Panel_MerchantShop");
                        if (existing != null)
                        {
                            shopPanel = existing.gameObject;
                            ResolveReferencesAndBind();
                            return;
                        }
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

            // 3. Centered Two-Column Wooden Modal Board (16:9 matching 1024x572)
            GameObject cardGO = new GameObject("ShopModalCard", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(shopPanel.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1180, 659);
            cardRect.anchoredPosition = Vector2.zero;

            var cardImg = cardGO.GetComponent<Image>();
            WoodenBoardStyle.ApplyBoardBackground(cardImg);
            cardImg.raycastTarget = true;

            // Plaque Title
            var plaqueTmp = WoodenBoardStyle.EnsurePlaqueTitle<TextMeshProUGUI>(cardGO.transform);
            if (plaqueTmp != null)
            {
                plaqueTmp.text = "MERCHANT SHOP";
                plaqueTmp.fontSize = 22f;
                plaqueTmp.fontStyle = FontStyles.Bold;
                plaqueTmp.alignment = TextAlignmentOptions.Center;
            }

            // 4. Transparent HeaderBar wrapper preserving HeaderBar/Btn_XClose path
            GameObject headerGO = new GameObject("HeaderBar", typeof(RectTransform));
            headerGO.transform.SetParent(cardGO.transform, false);
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = Vector2.zero;
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            // Subtitle placed compactly inside top of Left Column
            GameObject subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(headerGO.transform, false);
            var subRect = subGO.GetComponent<RectTransform>();
            WoodenBoardStyle.SetAnchorRegion(subRect,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.800f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x, WoodenBoardStyle.LeftPaneMax.y),
                10f, 2f, 10f, 4f);
            var subTmp = subGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Purchase seeds, gear, or sell crops";
            subTmp.fontSize = 12;
            subTmp.color = WoodenBoardStyle.TextMutedWarm;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.textWrappingMode = TextWrappingModes.Normal;
            subTmp.raycastTarget = false;

            // Close [X] Button on Top-Right Wooden Rim
            GameObject xBtnGO = new GameObject("Btn_XClose", typeof(RectTransform), typeof(Image), typeof(Button));
            xBtnGO.transform.SetParent(headerGO.transform, false);
            var xRect = xBtnGO.GetComponent<RectTransform>();
            xRect.anchorMin = WoodenBoardStyle.CloseButtonAnchor;
            xRect.anchorMax = WoodenBoardStyle.CloseButtonAnchor;
            xRect.pivot = new Vector2(0.5f, 0.5f);
            xRect.anchoredPosition = Vector2.zero;
            xRect.sizeDelta = new Vector2(36, 36);
            xBtnGO.GetComponent<Image>().sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            xBtnGO.GetComponent<Image>().type = Image.Type.Sliced;
            xBtnGO.GetComponent<Image>().color = WoodenBoardStyle.BtnDanger;
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
            xtTmp.color = Color.white;
            xtTmp.raycastTarget = false;

            // 5. Gold Pill Bar (Upper Left Column)
            GameObject goldPillGO = new GameObject("GoldPill", typeof(RectTransform), typeof(Image));
            goldPillGO.transform.SetParent(cardGO.transform, false);
            var gpRect = goldPillGO.GetComponent<RectTransform>();
            WoodenBoardStyle.SetAnchorRegion(gpRect,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.720f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.792f),
                10f, 2f, 10f, 2f);
            var gpImg = goldPillGO.GetComponent<Image>();
            gpImg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            gpImg.type = Image.Type.Sliced;
            gpImg.color = WoodenBoardStyle.DarkOakHeader;

            GameObject goldTextGO = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goldTextGO.transform.SetParent(goldPillGO.transform, false);
            var gtRect = goldTextGO.GetComponent<RectTransform>();
            gtRect.anchorMin = Vector2.zero;
            gtRect.anchorMax = Vector2.one;
            gtRect.sizeDelta = new Vector2(-16, 0);
            goldBalanceText = goldTextGO.GetComponent<TextMeshProUGUI>();
            goldBalanceText.text = "Gold: <color=#FFD700>0 G</color>";
            goldBalanceText.fontSize = 16;
            goldBalanceText.fontStyle = FontStyles.Bold;
            goldBalanceText.alignment = TextAlignmentOptions.Center;
            goldBalanceText.color = WoodenBoardStyle.TextCream;
            goldBalanceText.raycastTarget = false;

            // 6. Tabs Bar (Left Column: Buy / Sell)
            GameObject tabsGO = new GameObject("TabsBar", typeof(RectTransform));
            tabsGO.transform.SetParent(cardGO.transform, false);
            var tabsRect = tabsGO.GetComponent<RectTransform>();
            WoodenBoardStyle.SetAnchorRegion(tabsRect,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x, 0.625f),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.708f),
                10f, 2f, 10f, 2f);

            // Buy Tab Button
            GameObject buyTabGO = new GameObject("Tab_Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            buyTabGO.transform.SetParent(tabsGO.transform, false);
            var btr = buyTabGO.GetComponent<RectTransform>();
            btr.anchorMin = new Vector2(0, 0);
            btr.anchorMax = new Vector2(0.485f, 1);
            btr.sizeDelta = Vector2.zero;
            buyTabImage = buyTabGO.GetComponent<Image>();
            buyTabImage.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            buyTabImage.type = Image.Type.Sliced;
            buyTabImage.color = activeTabColor;
            buyTabButton = buyTabGO.GetComponent<Button>();

            GameObject bttGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bttGO.transform.SetParent(buyTabGO.transform, false);
            var bttRect = bttGO.GetComponent<RectTransform>();
            bttRect.anchorMin = Vector2.zero;
            bttRect.anchorMax = Vector2.one;
            bttRect.sizeDelta = Vector2.zero;
            var bttTmp = bttGO.GetComponent<TextMeshProUGUI>();
            bttTmp.text = "BUY ITEMS";
            bttTmp.fontSize = 13;
            bttTmp.fontStyle = FontStyles.Bold;
            bttTmp.alignment = TextAlignmentOptions.Center;
            bttTmp.color = Color.white;
            bttTmp.raycastTarget = false;

            // Sell Tab Button
            GameObject sellTabGO = new GameObject("Tab_Sell", typeof(RectTransform), typeof(Image), typeof(Button));
            sellTabGO.transform.SetParent(tabsGO.transform, false);
            var str = sellTabGO.GetComponent<RectTransform>();
            str.anchorMin = new Vector2(0.515f, 0);
            str.anchorMax = new Vector2(1f, 1);
            str.sizeDelta = Vector2.zero;
            sellTabImage = sellTabGO.GetComponent<Image>();
            sellTabImage.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            sellTabImage.type = Image.Type.Sliced;
            sellTabImage.color = inactiveTabColor;
            sellTabButton = sellTabGO.GetComponent<Button>();

            GameObject sttGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            sttGO.transform.SetParent(sellTabGO.transform, false);
            var sttRect = sttGO.GetComponent<RectTransform>();
            sttRect.anchorMin = Vector2.zero;
            sttRect.anchorMax = Vector2.one;
            sttRect.sizeDelta = Vector2.zero;
            var sttTmp = sttGO.GetComponent<TextMeshProUGUI>();
            sttTmp.text = "SELL CROPS";
            sttTmp.fontSize = 13;
            sttTmp.fontStyle = FontStyles.Bold;
            sttTmp.alignment = TextAlignmentOptions.Center;
            sttTmp.color = Color.white;
            sttTmp.raycastTarget = false;

            // 7. Scroll View Area for Items (Right Wide Plank Column)
            GameObject scrollGO = new GameObject("ItemScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(cardGO.transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            WoodenBoardStyle.SetAnchorRegion(srt, WoodenBoardStyle.RightPaneMin, WoodenBoardStyle.RightPaneMax, 10f, 8f, 10f, 8f);
            var sBg = scrollGO.GetComponent<Image>();
            sBg.sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            sBg.type = Image.Type.Sliced;
            sBg.color = WoodenBoardStyle.DarkOakOverlay;

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

            // 8. Bottom Return Bar (at Bottom of Left Column)
            GameObject bottomBarGO = new GameObject("BottomBar", typeof(RectTransform));
            bottomBarGO.transform.SetParent(cardGO.transform, false);
            var bbRect = bottomBarGO.GetComponent<RectTransform>();
            WoodenBoardStyle.SetAnchorRegion(bbRect,
                new Vector2(WoodenBoardStyle.LeftPaneMin.x, WoodenBoardStyle.LeftPaneMin.y),
                new Vector2(WoodenBoardStyle.LeftPaneMax.x, 0.138f),
                10f, 4f, 10f, 2f);

            // Close Shop Button
            GameObject closeBtnGO = new GameObject("Btn_CloseShop", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGO.transform.SetParent(bottomBarGO.transform, false);
            var cbr = closeBtnGO.GetComponent<RectTransform>();
            cbr.anchorMin = Vector2.zero;
            cbr.anchorMax = Vector2.one;
            cbr.sizeDelta = Vector2.zero;
            closeBtnGO.GetComponent<Image>().sprite = ProceduralUIAssetCache.GetRoundedBarSprite();
            closeBtnGO.GetComponent<Image>().type = Image.Type.Sliced;
            closeBtnGO.GetComponent<Image>().color = WoodenBoardStyle.BtnSecondary;
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
