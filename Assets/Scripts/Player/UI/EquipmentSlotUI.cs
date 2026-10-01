using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlayerUI
{
    public enum EquipmentSlotType
    {
        Helmet,
        Chest,
        Gloves,
        Pants,
        Boots,
        Necklace,
        Cape,
        Belt,
        Ring1,
        Ring2,
        MainWeapon,
        OffhandShield
    }

    /// <summary>
    /// Merepresentasikan wadah slot equipment di panel tengah (Equipment).
    /// Berfungsi sebagai placeholder visual bersiluet yang siap diintegrasikan
    /// saat sistem equip fungsional diimplementasikan.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Konfigurasi Slot")]
        public EquipmentSlotType slotType;
        public string slotDisplayName = "Equipment Slot";

        [Header("Referensi Komponen Visual")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image slotBorder;
        [SerializeField] private Image silhouetteIcon;
        [SerializeField] private Text slotLabel;

        [Header("Warna Feedback")]
        [SerializeField] private Color normalBorderColor = new Color(0.35f, 0.35f, 0.40f, 1f);
        [SerializeField] private Color hoverBorderColor = new Color(1f, 0.85f, 0.40f, 1f);

        private void Awake()
        {
            if (slotBorder != null)
                slotBorder.color = normalBorderColor;
        }

        public void Setup(EquipmentSlotType type, string displayName, Sprite icon)
        {
            slotType = type;
            slotDisplayName = displayName;
            if (silhouetteIcon != null && icon != null)
            {
                silhouetteIcon.sprite = icon;
                silhouetteIcon.enabled = true;
            }
            if (slotLabel != null)
            {
                slotLabel.text = displayName;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (slotBorder != null)
                slotBorder.color = hoverBorderColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (slotBorder != null)
                slotBorder.color = normalBorderColor;
        }
    }
}
