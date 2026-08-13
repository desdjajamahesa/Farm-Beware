using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public Text quantityText;
    public Image backgroundImage;

    private InventoryManagerUI manager;
    private Color defaultColor;

    // Inventori backend yang menaungi slot ini (Player atau Storage).
    public InventoryComponent ownerInventory;

    public int SlotIndex { get; private set; }
    public InventorySlot BoundSlot { get; set; }

    public bool HasItemIcon
    {
        get { return iconImage != null && iconImage.gameObject.activeSelf; }
    }

    public void Init(InventoryManagerUI inventoryManager, int index, InventoryComponent owner)
    {
        manager = inventoryManager;
        SlotIndex = index;
        ownerInventory = owner;

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
            defaultColor = backgroundImage.color;
    }

    public void SetHighlight(bool isSelected)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = isSelected ? Color.yellow : defaultColor;
    }

    public void SetSlotVisual(InventorySlot slot)
    {
        // Data-driven murni: hapus semua gambar & teks lama di slot ini terlebih dahulu,
        // lalu spawn ulang persis sesuai data array agar Ikon & Quantity tak pernah terpisah.
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        iconImage = null;
        quantityText = null;

        if (slot == null || slot.IsEmpty)
            return;

        // Spawn ulang ikon item.
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(8, 8);
        iconRT.offsetMax = new Vector2(-8, -8);

        Image img = iconGO.AddComponent<Image>();
        img.sprite = slot.item.itemIcon;
        img.preserveAspect = true;
        img.raycastTarget = true;
        iconGO.AddComponent<DraggableItem>();
        iconImage = img;

        // Spawn ulang teks quantity.
        GameObject qtyGO = new GameObject("Quantity", typeof(RectTransform));
        qtyGO.transform.SetParent(transform, false);
        RectTransform qtyRT = qtyGO.GetComponent<RectTransform>();
        // Jangkar ke pojok kanan-bawah dengan sedikit offset agar tidak menabrak padding ikon.
        qtyRT.anchorMin = new Vector2(1, 0);
        qtyRT.anchorMax = new Vector2(1, 0);
        qtyRT.pivot = new Vector2(1, 1);
        qtyRT.anchoredPosition = new Vector2(-2, 2);
        qtyRT.sizeDelta = new Vector2(22, 16);

        Text qText = qtyGO.AddComponent<Text>();
        qText.font = GetFont();
        qText.fontSize = 14;
        qText.color = Color.white;
        qText.alignment = TextAnchor.MiddleRight;
        qText.raycastTarget = false;
        qText.text = slot.quantity.ToString();
        quantityText = qText;
    }

    private static Font _font;

    private static Font GetFont()
    {
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _font;
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem dragItem = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableItem>()
            : null;
        if (dragItem == null) return;

        InventorySlotUI originSlot = dragItem.OriginSlot;
        if (originSlot == null || manager == null || ownerInventory == null)
            return;

        // Dijatkan kembali ke slot asal: tidak ada perubahan data.
        if (originSlot == this)
            return;

        // Perpindahan presisi slot-ke-slot. Backend MoveItemToSlot menangani
        // pindah ke slot kosong, penumpukan item sama, dan pertukaran item berbeda.
        // Berlaku untuk inventory sama maupun antar inventory (Player <-> Storage).
        originSlot.ownerInventory.MoveItemToSlot(originSlot.SlotIndex, ownerInventory, SlotIndex);

        // Tutup visual drag (UpdateUI sudah dipicu via OnInventoryChanged).
        dragItem.MarkDropped();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Gunakan item dengan DOUBLE-CLICK tombol kiri.
        if (eventData.button != PointerEventData.InputButton.Left || eventData.clickCount != 2)
            return;

        // Hanya item di inventory Player yang bisa digunakan; item di Storage tidak.
        if (ownerInventory != null && manager != null && ownerInventory == manager.playerInventory)
        {
            ownerInventory.UseItem(SlotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ownerInventory == null)
            return;

        InventorySlot data = SlotIndex >= 0 && SlotIndex < ownerInventory.slots.Count
            ? ownerInventory.slots[SlotIndex]
            : null;
        if (data == null || data.IsEmpty || ItemDisplayUI.Instance == null)
            return;

        ItemDisplayUI.Instance.ShowHover(data.item.itemName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemDisplayUI.Instance != null)
            ItemDisplayUI.Instance.HideHover();
    }

    private void OnDisable()
    {
        // Sembunyikan tooltip agar tidak menyangkut saat panel ditutup tiba-tiba.
        if (ItemDisplayUI.Instance != null)
            ItemDisplayUI.Instance.HideHover();
    }
}