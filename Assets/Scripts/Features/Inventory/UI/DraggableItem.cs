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

        // Reparent to Canvas so it keeps rendering and stays on top of everything.
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

        // CRITICAL: disable raycastTarget so the cursor passes through to slots underneath.
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

        // Hybrid Drop: if released outside a slot & in Trophy mode,
        // try dropping onto a 3D Snap Point (world). If successful, mark as dropped.
        if (!dropped)
        {
            if (TryHybridWorldDrop())
                dropped = true;
        }

        if (dropped)
        {
            // Backend + UpdateUI already rebuilt all slots; destroy the temporary drag copy.
            Destroy(gameObject);
        }
        else if (parentAfterDrag != null)
        {
            // Restore to original parent with original anchors & size.
            transform.SetParent(parentAfterDrag, true);
            rectTransform.anchorMin = origAnchorMin;
            rectTransform.anchorMax = origAnchorMax;
            rectTransform.offsetMin = origOffsetMin;
            rectTransform.offsetMax = origOffsetMax;
            rectTransform.sizeDelta = origSizeDelta;
            rectTransform.localScale = origLocalScale;
        }
    }

    // Drop to 3D world: raycast from trophy camera to SnapPoint layer.
    // Succeeds if SnapPoint is valid && item has placeablePrefab.
    // Data-driven: moves item from origin slot (Cabinet) to Rack at snap.slotIndex
    // via backend MoveItemToSlot. 3D model is rendered automatically by TrophyRackVisuals
    // (OnInventoryChanged listener) — NOT instantiated directly here.
    public static System.Func<InventorySlotUI, bool> HybridWorldDropHandler;

    // Drop to 3D world: delegated to the active mode listener (e.g., TrophySystemManager).
    private bool TryHybridWorldDrop()
    {
        if (HybridWorldDropHandler != null)
            return HybridWorldDropHandler(OriginSlot);
        return false;
    }

    // Called by InventorySlotUI.OnDrop when backend transaction succeeds.
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