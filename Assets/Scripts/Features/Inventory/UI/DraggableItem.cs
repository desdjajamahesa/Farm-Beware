using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform rectTransform;
    public Image image;
    public Transform parentAfterDrag;

    private bool dropped;
    private Vector2 origAnchorMin, origAnchorMax, origOffsetMin, origOffsetMax, origSizeDelta;
    private Vector3 origLocalScale;

    public InventorySlotUI OriginSlot { get; private set; }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OriginSlot = GetComponentInParent<InventorySlotUI>();
        parentAfterDrag = transform.parent;
        dropped = false;

        // Save original RectTransform state for restoration.
        origAnchorMin = rectTransform.anchorMin;
        origAnchorMax = rectTransform.anchorMax;
        origOffsetMin = rectTransform.offsetMin;
        origOffsetMax = rectTransform.offsetMax;
        origSizeDelta = rectTransform.sizeDelta;
        origLocalScale = rectTransform.localScale;

        // Pindah ke Canvas agar tetap render tapi selalu di paling atas.
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        // Switch to center-anchored fixed-size so icon follows cursor.
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(60f, 60f);
        rectTransform.localScale = Vector3.one;

        // SANGAT KRUSIAL: matikan raycastTarget agar kursor menembus ke slot di bawahnya.
        if (image != null)
            image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (image != null)
            image.raycastTarget = true;

        // Hybrid Drop: jika dilepas di luar slot & sedang mode Trophy,
        // coba ke Snap Point 3D (dunia). Jika berhasil, tandai sudah di-drop.
        if (!dropped)
        {
            if (TryHybridWorldDrop())
                dropped = true;
        }

        if (dropped)
        {
            // Backend + UpdateUI sudah membangun ulang semua slot; buang salinan drag sementara.
            Destroy(gameObject);
        }
        else if (parentAfterDrag != null)
        {
            // Kembalikan ke posisi asal dengan anchor & ukuran original.
            transform.SetParent(parentAfterDrag, true);
            rectTransform.anchorMin = origAnchorMin;
            rectTransform.anchorMax = origAnchorMax;
            rectTransform.offsetMin = origOffsetMin;
            rectTransform.offsetMax = origOffsetMax;
            rectTransform.sizeDelta = origSizeDelta;
            rectTransform.localScale = origLocalScale;
        }
    }

    // Drop ke dunia 3D: raycast dari kamera trophy ke Layer SnapPoint.
    // Berhasil bila SnapPoint ter-valid && item punya placeablePrefab.
    // Data-driven: pindahkan item dari slot asal (Kabinet) ke Rack pada index snap.slotIndex
    // via backend MoveItemToSlot. Model 3D di-render otomatis oleh TrophyRackVisuals
    // (listener OnInventoryChanged) — TIDAK di-instantiate langsung di sini.
    public static System.Func<InventorySlotUI, bool> HybridWorldDropHandler;

    // Drop ke dunia 3D: didelegasikan ke listener mode aktif (mis. TrophySystemManager).
    private bool TryHybridWorldDrop()
    {
        if (HybridWorldDropHandler != null)
            return HybridWorldDropHandler(OriginSlot);
        return false;
    }

    // Dipanggil oleh InventorySlotUI.OnDrop bila transaksi backend berhasil.
    public void MarkDropped()
    {
        dropped = true;
    }

    private static void CenterInSlot(RectTransform item, RectTransform slot)
    {
        if (item == null || slot == null) return;
        item.anchorMin = Vector2.zero;
        item.anchorMax = Vector2.one;
        item.offsetMin = Vector2.zero;
        item.offsetMax = Vector2.zero;
        item.localScale = Vector3.one;
    }
}