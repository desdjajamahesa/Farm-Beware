using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public Text quantityText;
    public Image backgroundImage;

    [Tooltip("Bar progress per-slot (stasiun dapur). Naik dari bawah ke atas di pinggir slot. Dibuat otomatis bila kosong.")]
    [SerializeField] private Image progressFill;

    // Toggle diagnostik: log nilai fill saat berubah (untuk membuktikan proses bertahap).
    public static bool debugLogProgress;

    private InventoryManagerUI manager;
    private Color defaultColor;

    // Inventori backend yang menaungi slot ini (Player atau Storage).
    public InventoryComponent ownerInventory;

    public int SlotIndex { get; private set; }
    public InventorySlot BoundSlot { get; set; }

    // State progress per-slot (hanya untuk inventory yang memiliki KitchenStation; selain itu null).
    private KitchenStation station;
    private bool stationChecked;

    // Kontainer tunggal indikator progress (track + fill + teks %).
    private GameObject progressContainerGO;
    private Image progressTrack;
    private Text progressText;
    private Color fillDefaultColor = new Color(1f, 0.78f, 0.1f, 0.9f);
    private bool flashing;
    private float flashEndTime;
    private float lastLoggedFill = -1f;
    private RectTransform fillRT;
    private float baseFillHeight = -1f;
    private float fillWidth = 9f;

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

        // Resolve stasiun dapur bila inventory ini adalah stasiun (Sink/Stove).
        station = owner != null ? owner.GetComponent<KitchenStation>() : null;
        stationChecked = true;

        BuildProgressIndicator();
        SetProgressVisible(false);

        if (station != null)
            station.OnProcessCompleted += OnSlotProcessCompleted;
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
        // Catatan: ProgressIndicator (bar progres) TIDAK ikut dihapus.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (progressContainerGO != null && transform.GetChild(i).gameObject == progressContainerGO)
                continue;
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        iconImage = null;
        quantityText = null;

        flashing = false;
        SetProgressVisible(false);

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

    /// <summary>
    /// Bangun indikator progress dalam SATU kontainer (ProgressIndicator):
    /// track gelap + fill emas (naik dari bawah) + teks persen real-time.
    /// Hanya dipakai bila inventory memiliki KitchenStation.
    /// </summary>
    private void BuildProgressIndicator()
    {
        if (progressContainerGO != null)
            return;

        GameObject container = new GameObject("ProgressIndicator", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        RectTransform containerRT = container.GetComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        // Track gelap (strip pinggir kanan slot, memakai ruang kosong di sekitar ikon).
        GameObject trackGO = new GameObject("Track", typeof(RectTransform));
        trackGO.transform.SetParent(container.transform, false);
        RectTransform trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(1f, 0.06f);
        trackRT.anchorMax = new Vector2(1f, 0.94f);
        trackRT.pivot = new Vector2(0.5f, 0.5f);
        trackRT.offsetMin = new Vector2(-16f, 0f);
        trackRT.offsetMax = new Vector2(-4f, 0f);
        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0.03f, 0.03f, 0.03f, 0.85f);
        trackImg.raycastTarget = false;

        // Fill emas (transform-grow: tinggi berubah dari bawah ke atas, pivot bawah).
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(container.transform, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(1f, 0.06f);
        fillRT.anchorMax = new Vector2(1f, 0.06f);
        fillRT.pivot = new Vector2(0.5f, 0f);
        fillRT.anchoredPosition = new Vector2(-10f, 0f);
        fillRT.sizeDelta = new Vector2(fillWidth, 0f);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.type = Image.Type.Simple;
        fillImg.color = fillDefaultColor;
        fillImg.raycastTarget = false;

        // Teks persen (di atas slot, besar & jelas sebagai penanda real-time).
        GameObject pctGO = new GameObject("Percent", typeof(RectTransform));
        pctGO.transform.SetParent(container.transform, false);
        RectTransform pctRT = pctGO.GetComponent<RectTransform>();
        pctRT.anchorMin = new Vector2(0.5f, 1.02f);
        pctRT.anchorMax = new Vector2(0.5f, 1.02f);
        pctRT.pivot = new Vector2(0.5f, 0f);
        pctRT.anchoredPosition = Vector2.zero;
        pctRT.sizeDelta = new Vector2(64f, 20f);
        Text pctText = pctGO.AddComponent<Text>();
        pctText.font = GetFont();
        pctText.fontSize = 13;
        pctText.fontStyle = FontStyle.Bold;
        pctText.color = new Color(1f, 0.95f, 0.3f, 1f);
        pctText.alignment = TextAnchor.MiddleCenter;
        pctText.raycastTarget = false;
        pctText.text = "";

        progressContainerGO = container;
        progressTrack = trackImg;
        this.fillRT = fillRT;
        if (containerRT.rect.height > 0) { baseFillHeight = containerRT.rect.height * 0.88f; }
        progressFill = fillImg;
        progressText = pctText;
    }

    private void SetProgressVisible(bool visible)
    {
        if (progressContainerGO == null)
            return;

        bool wasActive = progressContainerGO.activeSelf;
        if (wasActive != visible)
            progressContainerGO.SetActive(visible);

        // Log HANYA saat ada transisi (hindari spam per-frame).
        if (wasActive != visible && station != null && progressFill != null && ownerInventory != null)
        {
            Debug.Log("[InventorySlotUI] progress " + (visible ? "AKTIF" : "HILANG") + " slot " + SlotIndex
                + " owner " + ownerInventory.gameObject.name + " fill=" + progressFill.fillAmount.ToString("F2"));
        }
    }

    private void Update()
    {
        if (!stationChecked)
        {
            station = ownerInventory != null ? ownerInventory.GetComponent<KitchenStation>() : null;
            stationChecked = true;
        }

        if (station == null || progressFill == null || progressContainerGO == null)
            return;

        // Saat flash "Selesai!" berjalan, jangan diganggu polling.
        if (flashing)
        {
            if (Time.time >= flashEndTime)
            {
                flashing = false;
                progressFill.color = fillDefaultColor;
                progressFill.fillAmount = 0f;
                SetProgressText("");
                SetProgressVisible(false);
            }
            else {
                if (fillRT != null && baseFillHeight > 0) {
                    fillRT.sizeDelta = new Vector2(fillWidth, baseFillHeight);
                }
                SetProgressText("100%");
            }
            return;
        }

        if (!station.IsProcessing(SlotIndex))
        {
            SetProgressVisible(false);
            return;
        }

        SetProgressVisible(true);
        float p = Mathf.Clamp01(station.GetSlotProgress(SlotIndex));
        if (fillRT != null) {
            if (baseFillHeight <= 0 && progressContainerGO != null) {
                RectTransform crt = progressContainerGO.GetComponent<RectTransform>();
                if (crt != null && crt.rect.height > 0) baseFillHeight = crt.rect.height * 0.88f;
            }
            if (baseFillHeight > 0) {
                fillRT.sizeDelta = new Vector2(fillWidth, baseFillHeight * p);
            }
        }
        SetProgressText(Mathf.RoundToInt(p * 100f) + "%");

        // Debug: bukti proses bertahap (log saat nilai berubah >= 1%).
        if (debugLogProgress && Mathf.Abs(p - lastLoggedFill) >= 0.01f)
        {
            lastLoggedFill = p;
            Debug.Log("[InventorySlotUI] debugsink slot " + SlotIndex + " owner " + ownerInventory.gameObject.name
                + " fill=" + p.ToString("F2"));
        }
        lastLoggedFill = p;
    }

    private void SetProgressText(string text)
    {
        if (progressText != null)
            progressText.text = text;
    }

    private void OnSlotProcessCompleted(int slotIndex)
    {
        if (slotIndex != SlotIndex || progressFill == null)
            return;

        // Flash hijau "Selesai!" lalu sembunyikan.
        flashing = true;
        flashEndTime = Time.time + 0.35f;
        progressFill.color = new Color(0.2f, 1f, 0.35f, 0.95f);
        if (fillRT != null && baseFillHeight > 0) {
            fillRT.sizeDelta = new Vector2(fillWidth, baseFillHeight);
        }
        SetProgressVisible(true);
    }

    private void OnDestroy()
    {
        if (station != null)
            station.OnProcessCompleted -= OnSlotProcessCompleted;
    }

    private void OnDisable()
    {
        // Sembunyikan tooltip agar tidak menyangkut saat panel ditutup tiba-tiba.
        if (ItemDisplayUI.Instance != null)
            ItemDisplayUI.Instance.HideHover();

        flashing = false;
        SetProgressVisible(false);
    }
}