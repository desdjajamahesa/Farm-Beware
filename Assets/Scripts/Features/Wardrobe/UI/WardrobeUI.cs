using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmBeware.Logic;
using FeaturesWardrobe;

namespace FeaturesWardrobe
{
    /// <summary>
    /// Main Wardrobe UI controller with modular layout:
    /// - Left: Category Buttons (Upper Body, Lower Body, Accessories)
    /// - Center-Left: Item Grid (ScrollView with GridLayoutGroup)
    /// - Center-Right: 3D Live Preview (RawImage + PreviewCamera)
    /// - Right: Save/Exit Buttons
    /// </summary>
    public class WardrobeUI : MonoBehaviour
    {
        [Header("Canvas & Panel")]
        [SerializeField] private Canvas wardrobeCanvas;
        [SerializeField] private GameObject wardrobePanel;

        [Header("Category Section (Left)")]
        [SerializeField] private Transform categoryContainer;
        [SerializeField] private GameObject categoryButtonPrefab;
        [SerializeField] private Color categorySelectedColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color categoryNormalColor = Color.white;

        [Header("Item Grid (Center-Left)")]
        [SerializeField] private ScrollRect itemGridScrollRect;
        [SerializeField] private Transform itemGridContent;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private Vector2 gridCellSize = new Vector2(160f, 240f);
        [SerializeField] private int gridColumns = 3;

        [Header("Live 3D Preview (Center-Right)")]
        [SerializeField] private RawImage previewRawImage;

        [Header("Action Buttons (Right)")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button toggleHatButton;

        [Header("Outfit Mesh Swapper")]
        [SerializeField] private OutfitMeshSwapper outfitMeshSwapper;

        // State tracking
        private OutfitPartResolver.Category currentCategory = OutfitPartResolver.Category.Top;
        private WardrobeItemData currentPreviewOutfitData;
        private ItemSlot currentlySelectedSlot;
        private Dictionary<OutfitPartResolver.Category, List<WardrobeItemData>> categoryItems = new Dictionary<OutfitPartResolver.Category, List<WardrobeItemData>>();

        // UI component references
        private List<Button> categoryButtons = new List<Button>();
        private List<ItemSlot> itemSlots = new List<ItemSlot>();

        // Callbacks
        public System.Action<WardrobeItemData> OnItemSelected;
        public System.Action OnWardrobeClosed;

        private void Awake()
        {
            InitializeReferences();
            BuildCategoryButtons();
            RefreshItemGrid(currentCategory);
        }

        private void Start()
        {
            WireActionButtons();
            CreateToggleHatButtonIfMissing();
        }

        private void CreateToggleHatButtonIfMissing()
        {
            if (toggleHatButton != null) return;
            var toggleHatBtnTransform = transform.Find("ToggleHatButton");
            if (toggleHatBtnTransform == null)
            {
                var cancelBtnTransform = transform.Find("CancelButton");
                if (cancelBtnTransform != null)
                {
                    var toggleHatBtnGO = Instantiate(cancelBtnTransform.gameObject, cancelBtnTransform.parent);
                    toggleHatBtnGO.name = "ToggleHatButton";
                    var cancelBtnRect = cancelBtnTransform.GetComponent<RectTransform>();
                    var toggleHatBtnRect = toggleHatBtnGO.GetComponent<RectTransform>();
                    if (cancelBtnRect != null && toggleHatBtnRect != null)
                    {
                        toggleHatBtnRect.anchorMin = cancelBtnRect.anchorMin;
                        toggleHatBtnRect.anchorMax = cancelBtnRect.anchorMax;
                        toggleHatBtnRect.pivot = cancelBtnRect.pivot;
                        toggleHatBtnRect.anchoredPosition = cancelBtnRect.anchoredPosition + new Vector2(0, -60);
                        toggleHatBtnRect.sizeDelta = cancelBtnRect.sizeDelta;
                    }
                    var txt = toggleHatBtnGO.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = "Topi";
                    var btn = toggleHatBtnGO.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OnToggleHatClicked);
                        toggleHatButton = btn;
                    }
                }
            }
        }

        private void OnEnable()
        {
            WireActionButtons();
        }

        private void InitializeReferences()
        {
            if (wardrobePanel == null)
                wardrobePanel = gameObject?.transform?.Find("WardrobePanel")?.gameObject;
            if (categoryContainer == null)
                categoryContainer = transform.Find("CategoryContainer");
            if (itemGridContent == null)
                itemGridContent = transform.Find("ItemGrid/Content");
        }

        private void WireActionButtons()
        {
            var saveBtnTransform = transform.Find("SaveButton");
            if (saveBtnTransform != null)
            {
                var btn = saveBtnTransform.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        OnSaveClicked();
                        if (PlayerOutfit.Instance != null) PlayerOutfit.Instance.SaveWardrobe();
                        if (WardrobeManager.Instance != null) WardrobeManager.Instance.ExitWardrobeMode();
                    });
                    saveButton = btn;
                }
            }
            var exitBtnTransform = transform.Find("ExitButton");
            if (exitBtnTransform != null)
            {
                var btn = exitBtnTransform.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        OnExitClicked();
                        if (WardrobeManager.Instance != null) WardrobeManager.Instance.ExitWardrobeMode();
                    });
                    exitButton = btn;
                }
            }
            var cancelBtnTransform = transform.Find("CancelButton");
            if (cancelBtnTransform != null)
            {
                var btn = cancelBtnTransform.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (WardrobeManager.Instance != null) WardrobeManager.Instance.ExitWardrobeMode();
                    });
                    cancelButton = btn;
                }
            }
            var toggleHatBtnTransform = transform.Find("ToggleHatButton");
            if (toggleHatBtnTransform != null)
            {
                var btn = toggleHatBtnTransform.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OnToggleHatClicked);
                    toggleHatButton = btn;
                }
            }
        }

        private void BuildCategoryButtons()
        {
            if (categoryContainer == null || categoryButtonPrefab == null) return;
            foreach (var btn in categoryButtons)
                if (btn != null) Destroy(btn.gameObject);
            categoryButtons.Clear();
            var categories = System.Enum.GetValues(typeof(OutfitPartResolver.Category));
            for (int i = 0; i < categories.Length; i++)
            {
                var cat = (OutfitPartResolver.Category)categories.GetValue(i);
                var btnGO = Instantiate(categoryButtonPrefab, categoryContainer);
                btnGO.name = $"CategoryButton_{cat}";
                btnGO.SetActive(true);
                var btn = btnGO.GetComponent<Button>();
                var txt = btnGO.GetComponentInChildren<Text>();
                if (txt != null) txt.text = cat.ToString();
                var capturedCat = cat;
                btn.onClick.AddListener(() => OnCategorySelected(capturedCat));
                categoryButtons.Add(btn);
                if (!categoryItems.ContainsKey(cat))
                    categoryItems[cat] = new List<WardrobeItemData>();
            }
            SelectCategoryButton(currentCategory);
        }

        private void OnCategorySelected(OutfitPartResolver.Category cat)
        {
            currentCategory = cat;
            SelectCategoryButton(cat);
            RefreshItemGrid(cat);
        }

        private void SelectCategoryButton(OutfitPartResolver.Category cat)
        {
            for (int i = 0; i < categoryButtons.Count; i++)
            {
                var img = categoryButtons[i].GetComponentInChildren<Image>();
                if (img == null) continue;
                bool isSelected = (OutfitPartResolver.Category)i == cat;
                img.color = isSelected ? categorySelectedColor : categoryNormalColor;
            }
        }

        public void RefreshItemGrid(OutfitPartResolver.Category category)
        {
            var playerOutfit = WardrobeManager.Instance?.PlayerOutfitProp;
            if (itemGridContent == null || itemSlotPrefab == null || playerOutfit == null) return;

            List<OutfitData> outfits = playerOutfit.unlockedOutfits;
            if (outfits == null || outfits.Count == 0) return;

            foreach (var slot in itemSlots)
                if (slot != null) Destroy(slot.gameObject);
            itemSlots.Clear();

            int rowCount = Mathf.CeilToInt((float)outfits.Count / gridColumns);

            GridLayoutGroup gridLayout = itemGridContent.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = itemGridContent.gameObject.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = gridCellSize;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = gridColumns;
                gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
                gridLayout.childAlignment = TextAnchor.UpperCenter;
            }

            if (outfitMeshSwapper == null)
                outfitMeshSwapper = GameObject.FindObjectOfType<OutfitMeshSwapper>();

            for (int i = 0; i < outfits.Count; i++)
            {
                var outfit = outfits[i];
                var slotGO = Instantiate(itemSlotPrefab, itemGridContent);
                slotGO.name = $"ItemSlot_{outfit.name}";

                var slot = slotGO.GetComponent<ItemSlot>();
                if (slot == null)
                    slot = slotGO.AddComponent<ItemSlot>();

                var itemData = CreateWardrobeItemDataFromOutfit(outfit);
                slot.Setup(itemData, OnItemSlotClicked);
                slot.transform.localScale = Vector3.one;

                var btn = slotGO.GetComponent<Button>();
                if (btn != null)
                {
                    OutfitData capturedOutfit = outfit;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        var po = PlayerOutfit.Instance;
                        if (po != null)
                        {
                            po.EquipOutfit(capturedOutfit);
                            po.SaveWardrobe();
                        }
                    });
                }

                itemSlots.Add(slot);
            }

            if (itemGridScrollRect != null)
            {
                var rectTransform = itemGridContent.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                    itemGridScrollRect.content = rectTransform;
                }
            }
        }

        private WardrobeItemData CreateWardrobeItemDataFromOutfit(OutfitData outfit)
        {
            var itemData = ScriptableObject.CreateInstance<WardrobeItemData>();
            itemData.itemId = $"outfit_{outfit.name}";
            itemData.displayName = outfit.outfitName;
            itemData.icon = outfit.icon;
            itemData.previewPrefab = outfit.fullBodyPrefab;
            itemData.category = OutfitPartResolver.Category.Top;
            itemData.description = outfit.description;
            return itemData;
        }

        private List<WardrobeItemData> LoadDefaultItemsForCategory(OutfitPartResolver.Category category)
        {
            var items = new List<WardrobeItemData>();
            int variantCount = OutfitPartResolver.GetVariantCount(category);
            for (int i = 0; i < variantCount; i++)
            {
                var itemData = ScriptableObject.CreateInstance<WardrobeItemData>();
                itemData.itemId = $"item_{category}_{i}";
                itemData.displayName = $"{category} Variant {i + 1}";
                itemData.icon = Resources.Load<Sprite>($"Icons/Wardrobe/{category}_{i}");
                string variantName = category switch
                {
                    OutfitPartResolver.Category.Top => i == 0 ? "cloth1" : "cloth2",
                    OutfitPartResolver.Category.Bottom => i == 0 ? "pants1" : "pants2",
                    OutfitPartResolver.Category.Shoes => i == 0 ? "shoes1_left" : "shoes2_left",
                    OutfitPartResolver.Category.Hat => i == 0 ? "hat" : "",
                    _ => "body"
                };
                itemData.previewPrefab = Resources.Load<GameObject>($"Prefabs/Wardrobe/{variantName}");
                itemData.category = category;
                items.Add(itemData);
            }
            return items;
        }

        private void OnItemSlotClicked(WardrobeItemData itemData)
        {
            if (itemData == null) return;
            ItemSlot selectedSlot = null;
            foreach (var slot in itemSlots)
            {
                if (slot != null && slot.GetItemData() != null && slot.GetItemData().itemId == itemData.itemId)
                {
                    selectedSlot = slot;
                    break;
                }
            }
            if (selectedSlot == null && itemGridContent != null)
            {
                var slotsInGrid = itemGridContent.GetComponentsInChildren<ItemSlot>(true);
                foreach (var slot in slotsInGrid)
                {
                    if (slot != null && slot.GetItemData() != null && slot.GetItemData().itemId == itemData.itemId)
                    {
                        selectedSlot = slot;
                        break;
                    }
                }
            }
            if (currentlySelectedSlot != null)
                currentlySelectedSlot.SetSelected(false);
            currentlySelectedSlot = selectedSlot;
            if (currentlySelectedSlot != null)
                currentlySelectedSlot.SetSelected(true);
            OnItemSelected?.Invoke(itemData);
        }

        private void OnSaveClicked()
        {
            if (currentPreviewOutfitData != null)
            {
                Debug.Log($"[WardrobeUI] Outfit saved: {currentPreviewOutfitData.displayName}");
                if (WardrobeManager.Instance != null && WardrobeManager.Instance.PlayerOutfitProp != null)
                {
                    var outfitData = new OutfitData();
                    outfitData.outfitName = currentPreviewOutfitData.displayName;
                    outfitData.icon = currentPreviewOutfitData.icon;
                    outfitData.topVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Top ? 1 : 0;
                    outfitData.bottomVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Bottom ? 1 : 0;
                    outfitData.shoesVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Shoes ? 1 : 0;
                    outfitData.hatVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Hat ? 1 : 0;
                    outfitData.description = $"Custom outfit: {currentPreviewOutfitData.displayName}";
                    WardrobeManager.Instance.PlayerOutfitProp.TryOn(outfitData);
                    WardrobeManager.Instance.PlayerOutfitProp.Commit();
                }
            }
            OnWardrobeClosed?.Invoke();
        }

        private void OnExitClicked()
        {
            OnCancelClicked();
        }

        private void OnCancelClicked()
        {
            if (currentlySelectedSlot != null)
                currentlySelectedSlot.SetSelected(false);
            currentlySelectedSlot = null;
            OnWardrobeClosed?.Invoke();
        }

        private void OnToggleHatClicked()
        {
            var playerOutfit = PlayerOutfit.Instance;
            if (playerOutfit == null) return;
            playerOutfit.ToggleHat();
        }

        public void RegisterCategoryItems(OutfitPartResolver.Category category, List<WardrobeItemData> items)
        {
            if (!categoryItems.ContainsKey(category))
                categoryItems[category] = new List<WardrobeItemData>();
            categoryItems[category].Clear();
            categoryItems[category].AddRange(items);
            if (currentCategory == category)
                RefreshItemGrid(category);
        }

        public void SetCurrentPreviewOutfit(WardrobeItemData outfitData)
        {
            currentPreviewOutfitData = outfitData;
            if (previewRawImage != null && outfitData != null && outfitData.icon != null)
            {
                Debug.Log($"[WardrobeUI] Preview set: {outfitData?.displayName}");
            }
        }

        private void OnDestroy()
        {
            if (saveButton != null)
                saveButton.onClick.RemoveListener(OnSaveClicked);
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            foreach (var btn in categoryButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveAllListeners();
            }
            categoryButtons.Clear();
        }
    }
}
