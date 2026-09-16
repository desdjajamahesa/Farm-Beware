using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag & Drop handler for Panel_Sink slots.
/// Uses static ghost tracking to prevent orphaned ghost icons
/// when source slot is destroyed before OnEndDrag fires.
/// </summary>
public class SinkDragDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public enum SlotType { PlayerInventory, InputSlot, OutputSlot }

    [Tooltip("Type of this slot.")]
    public SlotType slotType;

    [Tooltip("Reference to SinkManager.")]
    public SinkManager sinkManager;

    [Tooltip("Index in player inventory (for PlayerInventory slots).")]
    public int inventoryIndex = -1;

    // Static ghost tracking — survives source slot destruction
    private static GameObject activeDragGhost;
    private static Canvas parentCanvas;
    private static SinkDragDropHandler dragSource;

    // Local reference for position tracking during drag
    private GameObject localDragGhost;

    /// <summary>
    /// Force-destroys any orphaned ghost icon. Safe to call anytime.
    /// </summary>
    public static void ForceCleanupGhost()
    {
        if (activeDragGhost != null)
        {
            Destroy(activeDragGhost);
            activeDragGhost = null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        InventorySlot sourceSlot = GetSourceSlot();
        if (sourceSlot == null || sourceSlot.IsEmpty) return;

        // Clean up any pre-existing orphaned ghost first
        ForceCleanupGhost();

        dragSource = this;

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        localDragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        localDragGhost.transform.SetParent(parentCanvas.transform, false);
        localDragGhost.transform.SetAsLastSibling();

        var ghostRT = localDragGhost.GetComponent<RectTransform>();
        ghostRT.sizeDelta = new Vector2(64, 64);

        var ghostImg = localDragGhost.GetComponent<Image>();
        ghostImg.sprite = sourceSlot.item.itemIcon;
        ghostImg.color = new Color(1, 1, 1, 0.8f);
        ghostImg.raycastTarget = false;

        // Store in static reference (survives source destruction)
        activeDragGhost = localDragGhost;

        // Fade source slot via CanvasGroup (NOT Image.color)
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (localDragGhost == null) return;

        RectTransform canvasRT = parentCanvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, eventData.position, eventData.pressEventCamera, out localPoint);

        localDragGhost.GetComponent<RectTransform>().anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Force cleanup via static reference
        ForceCleanupGhost();
        localDragGhost = null;

        // Restore source slot alpha via CanvasGroup
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }

        dragSource = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // CRITICAL: Destroy ghost BEFORE any transfer logic
        ForceCleanupGhost();

        // Support dropping DraggableItem (from HUD hotbar or standard inventory) onto InputSlot
        var draggable = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<DraggableItem>() : null;
        if (draggable != null && draggable.OriginSlot != null)
        {
            if (this.slotType == SlotType.InputSlot)
            {
                int srcIndex = draggable.OriginSlot.SlotIndex;
                if (sinkManager != null)
                {
                    bool transferred = sinkManager.TransferToInputSlot(srcIndex);
                    if (transferred)
                    {
                        draggable.MarkDropped();
                        return;
                    }
                }
            }
        }

        // Get the handler from the dragged object directly
        var draggedHandler = eventData.pointerDrag?.GetComponent<SinkDragDropHandler>();
        if (draggedHandler != null)
        {
            draggedHandler.localDragGhost = null;
        }

        if (draggedHandler == null || draggedHandler.sinkManager == null) return;

        bool success = false;

        // 1. InputSlot → PlayerInventory (cancel wash to specific slot)
        if (draggedHandler.slotType == SlotType.InputSlot && this.slotType == SlotType.PlayerInventory)
        {
            success = sinkManager.TransferFromInputToPlayer(this.inventoryIndex);
        }
        // 2. OutputSlot → PlayerInventory (take clean item to specific slot)
        else if (draggedHandler.slotType == SlotType.OutputSlot && this.slotType == SlotType.PlayerInventory)
        {
            success = sinkManager.TransferFromOutputToPlayer(this.inventoryIndex);
        }
        // 3. PlayerInventory → PlayerInventory (internal swap/rearrange)
        else if (draggedHandler.slotType == SlotType.PlayerInventory && this.slotType == SlotType.PlayerInventory)
        {
            sinkManager.SwapPlayerSlots(draggedHandler.inventoryIndex, this.inventoryIndex);
            success = true;
        }
        // 4. PlayerInventory → InputSlot (put dirty item into washer)
        else if (draggedHandler.slotType == SlotType.PlayerInventory && this.slotType == SlotType.InputSlot)
        {
            success = sinkManager.TransferToInputSlot(draggedHandler.inventoryIndex);
        }

        // Restore dragged slot alpha via CanvasGroup
        var draggedCG = eventData.pointerDrag?.GetComponent<CanvasGroup>();
        if (draggedCG != null)
        {
            draggedCG.alpha = 1f;
            draggedCG.blocksRaycasts = true;
        }

        dragSource = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Ignore clicks that ended a drag operation.
        if (eventData.dragging)
            return;

        if (sinkManager == null)
            return;

        switch (slotType)
        {
            case SlotType.OutputSlot:
                // Click output → transfer clean items to player inventory.
                sinkManager.TransferFromOutputToPlayer();
                break;

            case SlotType.InputSlot:
                // Click input → cancel wash and return dirty items to player.
                sinkManager.TransferFromInputToPlayer();
                break;

            case SlotType.PlayerInventory:
                // Click player slot → send dirty item to washer input.
                // This is already handled by the Button.onClick on the spawned slot,
                // but SinkDragDropHandler's IPointerClickHandler fires first.
                // Delegate to SinkManager to avoid duplicate calls.
                sinkManager.TransferToInputSlot(inventoryIndex);
                break;
        }
    }

    private InventorySlot GetSourceSlot()
    {
        if (sinkManager == null) return null;

        if (slotType == SlotType.PlayerInventory)
        {
            return sinkManager.GetPlayerSlot(inventoryIndex);
        }

        if (slotType == SlotType.OutputSlot)
        {
            return sinkManager.GetOutputSlot();
        }

        if (slotType == SlotType.InputSlot)
        {
            return sinkManager.GetInputSlot();
        }

        return null;
    }
}
