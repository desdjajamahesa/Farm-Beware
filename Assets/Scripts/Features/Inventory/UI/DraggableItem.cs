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

        // Pindah ke root canvas agar selalu di paling atas dan tidak menghalangi deteksi slot.
        transform.SetParent(transform.root, true);
        transform.SetAsLastSibling();

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
            // Tidak dijatuhkan ke slot valid: kembalikan ke posisi asal.
            transform.SetParent(parentAfterDrag, true);
            CenterInSlot(rectTransform, parentAfterDrag as RectTransform);
        }
    }

    // Drop ke dunia 3D: raycast dari kamera trophy ke Layer SnapPoint.
    // Berhasil bila SnapPoint ter-valid && item punya placeablePrefab.
    // Data-driven: pindahkan item dari slot asal (Kabinet) ke Rack pada index snap.slotIndex
    // via backend MoveItemToSlot. Model 3D di-render otomatis oleh TrophyRackVisuals
    // (listener OnInventoryChanged) — TIDAK di-instantiate langsung di sini.
    private bool TryHybridWorldDrop()
    {
        if (TrophySystemManager.Instance == null || !TrophySystemManager.Instance.IsInTrophyMode)
            return false;

        Camera cam = TrophySystemManager.Instance.TrophyFirstPersonCamera;
        if (cam == null || Mouse.current == null || OriginSlot == null)
            return false;

        ItemData item = OriginSlot.BoundSlot != null ? OriginSlot.BoundSlot.item : null;
        if (item == null || item.placeablePrefab == null)
            return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, LayerMask.GetMask("SnapPoint")))
            return false;

        TrophySnapPoint snap = hit.collider != null ? hit.collider.GetComponent<TrophySnapPoint>() : null;
        if (snap == null || snap.slotIndex < 0)
            return false;

        InventoryComponent rack = TrophySystemManager.Instance.RackInventory;
        if (rack == null || OriginSlot.ownerInventory == null)
            return false;

        // Backend command: pindahkan slot asal ke slot rack pada snap.slotIndex.
        // MoveItemToSlot menangani: pindah ke slot kosong, stack item sama, atau swap item beda.
        OriginSlot.ownerInventory.MoveItemToSlot(OriginSlot.SlotIndex, rack, snap.slotIndex);
        return true;
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