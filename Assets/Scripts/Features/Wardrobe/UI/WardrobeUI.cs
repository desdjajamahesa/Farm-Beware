using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FarmBeware.Logic;
using FeaturesWardrobe;

namespace FeaturesWardrobe
{
    /// <summary>
    /// Main Wardrobe UI controller with modular layout:
    /// - Center: Item Grid (ScrollView with GridLayoutGroup)
    /// - Right: Save/Cancel/ToggleHat Buttons
    /// </summary>
    public class WardrobeUI : MonoBehaviour
    {
        [Header("Canvas & Panel")]
        [SerializeField] private Canvas wardrobeCanvas;
        [SerializeField] private GameObject wardrobePanel;

        [Header("Item Grid")]
        [SerializeField] private ScrollRect itemGridScrollRect;
        [SerializeField] private Transform itemGridContent;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private Vector2 gridCellSize = new Vector2(160f, 240f);
        [SerializeField] private int gridColumns = 3;

        [Header("Action Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button toggleHatButton;

        private List<ItemSlot> itemSlots = new List<ItemSlot>();

        public System.Action OnWardrobeClosed;

        private void Awake()
        {
            InitializeReferences();
            RefreshItemGrid();
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
                        if (PlayerOutfit.Instance != null) PlayerOutfit.Instance.SaveWardrobe();
                        if (WardrobeManager.Instance != null) WardrobeManager.Instance.ExitWardrobeMode();
                    });
                    saveButton = btn;
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

        public void RefreshItemGrid()
        {
            var playerOutfit = WardrobeManager.Instance?.PlayerOutfitProp;
            if (itemGridContent == null || itemSlotPrefab == null || playerOutfit == null) return;

            List<OutfitData> outfits = playerOutfit.unlockedOutfits;
            if (outfits == null || outfits.Count == 0) return;

            foreach (var slot in itemSlots)
                if (slot != null) Destroy(slot.gameObject);
            itemSlots.Clear();

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

            for (int i = 0; i < outfits.Count; i++)
            {
                var outfit = outfits[i];
                var slotGO = Instantiate(itemSlotPrefab, itemGridContent);
                slotGO.name = $"ItemSlot_{outfit.name}";

                var slot = slotGO.GetComponent<ItemSlot>();
                if (slot == null)
                    slot = slotGO.AddComponent<ItemSlot>();

                var itemData = CreateWardrobeItemDataFromOutfit(outfit);
                slot.Setup(itemData, null);
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

        private void OnToggleHatClicked()
        {
            var playerOutfit = PlayerOutfit.Instance;
            if (playerOutfit == null) return;
            playerOutfit.ToggleHat();
        }

        private void OnDestroy()
        {
            if (saveButton != null)
                saveButton.onClick.RemoveAllListeners();
            if (cancelButton != null)
                cancelButton.onClick.RemoveAllListeners();
            if (toggleHatButton != null)
                toggleHatButton.onClick.RemoveAllListeners();
        }
    }
}
