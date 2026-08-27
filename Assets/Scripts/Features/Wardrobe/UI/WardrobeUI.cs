using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmBeware.Logic;

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
        [SerializeField] private Vector2 gridCellSize = new Vector2(80f, 80f);
        [SerializeField] private int gridColumns = 4;

        [Header("Live 3D Preview (Center-Right)")]
        [SerializeField] public PreviewController previewController;
        [SerializeField] private RawImage previewRawImage;

        [Header("Action Buttons (Right)")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button cancelButton;

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
            // Initialize UI references
            InitializeReferences();

            // Setup button listeners
            SetupButtonListeners();

            // Build initial state
            BuildCategoryButtons();
            RefreshItemGrid(currentCategory);
        }

        private void InitializeReferences()
        {
            // Try to find references if not assigned
            if (wardrobePanel == null)
                wardrobePanel = gameObject?.transform?.Find("WardrobePanel")?.gameObject;

            if (categoryContainer == null)
                categoryContainer = transform.Find("CategoryContainer");

            if (itemGridContent == null)
                itemGridContent = transform.Find("ItemGrid/Content");

            if (previewController == null)
                previewController = GetComponentInChildren<PreviewController>(true);

            if (saveButton == null)
                saveButton = transform.Find("ActionButtons/SaveButton")?.GetComponent<Button>();

            if (exitButton == null)
                exitButton = transform.Find("ActionButtons/ExitButton")?.GetComponent<Button>();

            if (cancelButton == null)
                cancelButton = transform.Find("ActionButtons/CancelButton")?.GetComponent<Button>();
        }

        private void SetupButtonListeners()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void BuildCategoryButtons()
        {
            if (categoryContainer == null || categoryButtonPrefab == null) return;

            // Clear existing buttons
            foreach (var btn in categoryButtons)
                if (btn != null) Destroy(btn.gameObject);
            categoryButtons.Clear();

            // Get all available categories from OutfitPartResolver
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

                // Initialize category item list
                if (!categoryItems.ContainsKey(cat))
                    categoryItems[cat] = new List<WardrobeItemData>();
            }

            // Select the default category
            SelectCategoryButton(currentCategory);
        }

        private void OnCategorySelected(OutfitPartResolver.Category cat)
        {
            currentCategory = cat;
            SelectCategoryButton(cat);
            RefreshItemGrid(cat);
            if (previewController != null)
                previewController.CenterView();
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

        private void RefreshItemGrid(OutfitPartResolver.Category category)
        {
            if (itemGridContent == null || itemSlotPrefab == null || previewController == null) return;

            // Get items for this category
            List<WardrobeItemData> items = null;
            if (categoryItems.ContainsKey(category))
            {
                items = categoryItems[category];
            }
            else
            {
                // Try to load items from somewhere - for now, create some default test items
                items = LoadDefaultItemsForCategory(category);
                categoryItems[category] = items;
            }

            // Clear existing slots
            foreach (var slot in itemSlots)
                if (slot != null) Destroy(slot.gameObject);
            itemSlots.Clear();

            // Calculate grid layout
            int rowCount = Mathf.CeilToInt((float)items.Count / gridColumns);

            // Setup GridLayoutGroup
            GridLayoutGroup gridLayout = itemGridContent.GetComponent<GridLayoutGroup>();
            if (gridLayout == null) gridLayout = itemGridContent.gameObject.AddComponent<GridLayoutGroup>();

            gridLayout.cellSize = gridCellSize;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = gridColumns;
            gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            // Instantiate slots for each item
            for (int i = 0; i < items.Count; i++)
            {
                var slotGO = Instantiate(itemSlotPrefab, itemGridContent);
                slotGO.name = $"ItemSlot_{items[i].displayName}";

                var slot = slotGO.GetComponent<ItemSlot>();
                if (slot == null)
                    slot = slotGO.AddComponent<ItemSlot>();

                // Configure the slot
                slot.Setup(items[i], OnItemSlotClicked);
                slot.transform.localScale = Vector3.one;

                itemSlots.Add(slot);
            }

            // Ensure content size fits
            if (itemGridScrollRect != null)
            {
                // Recalculate layout
                var rectTransform = itemGridContent.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                    itemGridScrollRect.content = rectTransform;
                }
            }
        }

        private List<WardrobeItemData> LoadDefaultItemsForCategory(OutfitPartResolver.Category category)
        {
            var items = new List<WardrobeItemData>();

            // Create default wardrobe items based on category
            // These would normally come from ScriptableObjects or JSON config
            int variantCount = OutfitPartResolver.GetVariantCount(category);

            for (int i = 0; i < variantCount; i++)
            {
                var itemData = ScriptableObject.CreateInstance<WardrobeItemData>();
                itemData.itemId = $"item_{category}_{i}";
                itemData.displayName = $"{category} Variant {i + 1}";
                itemData.icon = Resources.Load<Sprite>($"Icons/Wardrobe/{category}_{i}");

                // Map category to appropriate variant names
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

            // Update currently selected slot highlight - find the slot component for this item
            ItemSlot selectedSlot = null;
            foreach (var slot in itemSlots)
            {
                if (slot != null && slot.GetItemData() == itemData)
                {
                    selectedSlot = slot;
                    break;
                }
            }

            if (currentlySelectedSlot != null)
                currentlySelectedSlot.SetSelected(false);

            currentlySelectedSlot = selectedSlot;
            if (currentlySelectedSlot != null)
                currentlySelectedSlot.SetSelected(true);

            // Pass to callback if registered
            OnItemSelected?.Invoke(itemData);

            // Update the 3D preview
            if (previewController != null)
            {
                previewController.SetAvatarAppearance(itemData);
            }
        }

        private void OnSaveClicked()
        {
            // Save the current preview outfit data
            if (currentPreviewOutfitData != null)
            {
                // TODO: Persist the selected outfit - could save to PlayerPrefs, JSON, or database
                Debug.Log($"[WardrobeUI] Outfit saved: {currentPreviewOutfitData.displayName}");

                // Optionally commit to the PlayerOutfit system
                if (WardrobeManager.Instance != null && WardrobeManager.Instance.PlayerOutfitProp != null)
                {
                    // Create a new OutfitData from the selected item
                    var outfitData = new OutfitData();
                    outfitData.outfitName = currentPreviewOutfitData.displayName;
                    outfitData.icon = currentPreviewOutfitData.icon;
                    outfitData.topVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Top ? 1 : 0;
                    outfitData.bottomVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Bottom ? 1 : 0;
                    outfitData.shoesVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Shoes ? 1 : 0;
                    outfitData.hatVariant = currentPreviewOutfitData.category == OutfitPartResolver.Category.Hat ? 1 : 0;
                    outfitData.description = $"Custom outfit: {currentPreviewOutfitData.displayName}";

                    // Apply to player
                    WardrobeManager.Instance.PlayerOutfitProp.TryOn(outfitData);
                    WardrobeManager.Instance.PlayerOutfitProp.Commit();
                }
            }

            // Close wardrobe
            OnWardrobeClosed?.Invoke();
        }

        private void OnExitClicked()
        {
            // Revert any changes and close
            OnCancelClicked();
        }

        private void OnCancelClicked()
        {
            // Reset to previous state
            if (currentlySelectedSlot != null)
                currentlySelectedSlot.SetSelected(false);
            currentlySelectedSlot = null;

            // Reset preview to default
            if (previewController != null)
                previewController.CenterView();

            OnWardrobeClosed?.Invoke();
        }

        // Public methods for external use

        /// <summary>
        /// Register category items from external source (e.g., WardrobeManager)
        /// </summary>
        public void RegisterCategoryItems(OutfitPartResolver.Category category, List<WardrobeItemData> items)
        {
            if (!categoryItems.ContainsKey(category))
                categoryItems[category] = new List<WardrobeItemData>();

            categoryItems[category].Clear();
            categoryItems[category].AddRange(items);

            // If this is the currently selected category, refresh the grid
            if (currentCategory == category)
                RefreshItemGrid(category);
        }

        /// <summary>
        /// Set the current preview outfit data (called from PreviewController or other systems)
        /// </summary>
        public void SetCurrentPreviewOutfit(WardrobeItemData outfitData)
        {
            currentPreviewOutfitData = outfitData;

            // Update the preview image if we have an icon
            if (previewRawImage != null && outfitData != null && outfitData.icon != null)
            {
                // Note: RawImage.sprite works with Sprite, but we store Texture in WardrobeItemData
                // This would need conversion or we use a different approach
                // For now, just log
                Debug.Log($"[WardrobeUI] Preview set: {outfitData?.displayName}");
            }
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (saveButton != null)
                saveButton.onClick.RemoveListener(OnSaveClicked);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);

            // Clear category button listeners
            foreach (var btn in categoryButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveAllListeners();
            }
            categoryButtons.Clear();
        }
    }
}