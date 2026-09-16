using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmBeware.Logic;

namespace FeaturesWardrobe
{
    /// <summary>
    /// UI component for a single item slot in the Wardrobe inventory grid.
    /// Handles icon display, selection highlight, and click events.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private Image hoverHighlight;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.2f);

        [Header("State")]
        private WardrobeItemData itemData;
        private bool isSelected = false;
        private Button button;
        private System.Action<WardrobeItemData> onClickCallback;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnSlotClicked);

            if (selectionHighlight != null)
                selectionHighlight.enabled = false;

            if (hoverHighlight != null)
                hoverHighlight.enabled = false;
        }

        public void Setup(WardrobeItemData data, System.Action<WardrobeItemData> clickCallback)
        {
            itemData = data;
            onClickCallback = clickCallback;

            // Ensure button listener is set up (in case Awake didn't run)
            if (button == null)
                button = GetComponent<Button>();
            if (button != null && onClickCallback != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnSlotClicked);
            }

            if (iconImage != null && data != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = true;
            }
            else if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            SetSelected(false);
        }

        private void OnSlotClicked()
        {
            if (itemData != null && onClickCallback != null)
            {
                onClickCallback.Invoke(itemData);
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectionHighlight != null)
                selectionHighlight.enabled = selected;

            if (backgroundImage != null)
                backgroundImage.color = selected ? selectedColor : normalColor;
        }

        public WardrobeItemData GetItemData() => itemData;
        public bool IsSelected => isSelected;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isSelected && hoverHighlight != null)
                hoverHighlight.enabled = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverHighlight != null)
                hoverHighlight.enabled = false;
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnSlotClicked);
        }
    }
}