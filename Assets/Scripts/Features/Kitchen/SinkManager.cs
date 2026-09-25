using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-only Sink Manager. Reads washing state from KitchenSinkInteractable (processor).
/// Processor owns slots and runs washing timer independently of panel visibility.
/// </summary>
public class SinkManager : MonoBehaviour
{
    [Header("Processor Reference")]
    [Tooltip("KitchenSinkInteractable that owns washing logic and slots.")]
    [SerializeField] private KitchenSinkInteractable processor;

    [Header("UI References")]
    [SerializeField] private Image inputSlotImage;
    [SerializeField] private Image outputSlotImage;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI inputCountText;
    [SerializeField] private TextMeshProUGUI outputCountText;

    [Header("Slot Parent Transforms (Anti-Inspector Bug)")]
    [Tooltip("Parent transform for InputSlot. Auto-resolves child Image/TMP if missing.")]
    [SerializeField] private Transform inputSlotUI;
    [Tooltip("Parent transform for OutputSlot. Auto-resolves child Image/TMP if missing.")]
    [SerializeField] private Transform outputSlotUI;

    [Header("Player Inventory Grid")]
    [Tooltip("Parent transform for player inventory slot UIs.")]
    [SerializeField] private Transform playerInventoryGrid;

    [Header("Close Button")]
    [Tooltip("Close button on TopBar.")]
    [SerializeField] private Button closeButton;

    [Header("Item Description")]
    [Tooltip("TMP text for item description at bottom of LeftContent.")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Refill Water Bottle")]
    [SerializeField] private Button refillWaterButton;
    [SerializeField] private TextMeshProUGUI refillWaterButtonText;

    private InventoryComponent playerInventory;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => ClosePanel());
        }
    }

    private void Start()
    {
        // Auto-resolve processor if not assigned
        if (processor == null)
            processor = FindFirstObjectByType<KitchenSinkInteractable>();

        // Anti-Inspector Bug: auto-resolve child references from parent transforms
        if (inputSlotUI != null)
        {
            if (inputSlotImage == null)
                inputSlotImage = inputSlotUI.Find("ItemIcon")?.GetComponent<Image>();
            if (inputCountText == null)
                inputCountText = inputSlotUI.Find("Count")?.GetComponent<TextMeshProUGUI>();
        }
        if (outputSlotUI != null)
        {
            if (outputSlotImage == null)
                outputSlotImage = outputSlotUI.Find("ItemIcon")?.GetComponent<Image>();
            if (outputCountText == null)
                outputCountText = outputSlotUI.Find("Count")?.GetComponent<TextMeshProUGUI>();
        }

        EnsureRefillWaterButton();
    }

    public void SetPlayerInventory(InventoryComponent inv)
    {
        if (inv == null) return;
        playerInventory = inv;
        playerInventory.HasHotbar = true;
    }

    public void EnsurePlayerInventoryRef()
    {
        if (playerInventory != null)
        {
            playerInventory.HasHotbar = true;
            return;
        }

        if (InventoryManagerUI.Instance != null && InventoryManagerUI.Instance.playerInventory != null)
        {
            playerInventory = InventoryManagerUI.Instance.playerInventory;
            playerInventory.HasHotbar = true;
            return;
        }

        var pc = FindFirstObjectByType<PlayerControl>();
        if (pc != null)
        {
            playerInventory = pc.GetComponent<InventoryComponent>();
            if (playerInventory != null)
                playerInventory.HasHotbar = true;
            return;
        }

        var p = GameObject.Find("Player");
        if (p != null)
        {
            playerInventory = p.GetComponent<InventoryComponent>();
            if (playerInventory != null)
                playerInventory.HasHotbar = true;
        }
    }

    private void OnEnable()
    {
        EnsurePlayerInventoryRef();

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnPlayerInventoryChanged;
            playerInventory.OnInventoryChanged += OnPlayerInventoryChanged;
        }

        // Click-to-transfer is now handled by SinkDragDropHandler.OnPointerClick
        // on each slot (InputSlot, OutputSlot, PlayerInventory). Ensure all
        // SinkDragDropHandler children have their sinkManager reference set.
        var handlers = gameObject.GetComponentsInChildren<SinkDragDropHandler>(true);
        foreach (var h in handlers)
        {
            if (h.sinkManager == null)
                h.sinkManager = this;
        }

        PopulatePlayerInventory();
        SyncToProcessor();
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnPlayerInventoryChanged;
        }
        ClearSpawnedSlots();
        // NOTE: Washing does NOT stop here. Processor keeps running on Kitchen_Sink.
    }

    private void OnPlayerInventoryChanged()
    {
        PopulatePlayerInventory();
    }

    /// <summary>
    /// Called when Panel_Sink is closed (by CloseButton or ESC).
    /// Does NOT stop washing — processor continues independently.
    /// </summary>
    public void ClosePanel()
    {
        // NOTE: No StopWashing() call. Processor keeps running.

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var player = FindFirstObjectByType<PlayerControl>();
        if (player != null)
            player.isInputLocked = false;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sync UI to current processor state. Called on OnEnable and after slot changes.
    /// </summary>
    public void SyncToProcessor()
    {
        RefreshSlotVisuals();
        SyncProgressUI();
        EnsureRefillWaterButton();
        UpdateRefillButtonState();
    }

    public void EnsureRefillWaterButton()
    {
        if (refillWaterButton != null) return;

        var existing = transform.Find("Btn_RefillWater") ?? transform.Find("LeftContent/Btn_RefillWater") ?? transform.Find("TopBar/Btn_RefillWater");
        if (existing != null)
        {
            refillWaterButton = existing.GetComponent<Button>();
            refillWaterButtonText = existing.GetComponentInChildren<TextMeshProUGUI>();
            if (refillWaterButton != null)
            {
                refillWaterButton.onClick.RemoveAllListeners();
                refillWaterButton.onClick.AddListener(OnRefillWaterClicked);
            }
            return;
        }

        Transform targetParent = transform.Find("LeftContent") ?? transform.Find("TopBar") ?? transform;

        GameObject btnGO = new GameObject("Btn_RefillWater", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(targetParent, false);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 16f);
        rt.sizeDelta = new Vector2(210f, 36f);

        var img = btnGO.GetComponent<Image>();
        img.color = new Color(0.12f, 0.42f, 0.65f, 1f);

        refillWaterButton = btnGO.GetComponent<Button>();
        refillWaterButton.onClick.AddListener(OnRefillWaterClicked);

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(btnGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        refillWaterButtonText = textGO.GetComponent<TextMeshProUGUI>();
        refillWaterButtonText.text = "💧 Refill Water (100L)";
        refillWaterButtonText.fontSize = 13;
        refillWaterButtonText.fontStyle = FontStyles.Bold;
        refillWaterButtonText.alignment = TextAlignmentOptions.Center;
        refillWaterButtonText.color = Color.white;

        UpdateRefillButtonState();
    }

    public void OnRefillWaterClicked()
    {
        var bottle = FeaturesKitchen.PlayerWaterBottle.Instance;
        if (bottle != null)
        {
            bottle.RefillWater(100f);
            if (PlayerUI.FloatingCombatTextManager.Instance != null)
            {
                var player = FindFirstObjectByType<PlayerControl>();
                Vector3 pos = player != null ? player.transform.position + Vector3.up * 1.5f : transform.position;
                PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                    pos,
                    "💧 Water Bottle Refilled (100/100 L)!",
                    new Color(0.25f, 0.85f, 1f));
            }
            UpdateRefillButtonState();
        }
    }

    public void UpdateRefillButtonState()
    {
        if (refillWaterButtonText == null) return;
        var bottle = FeaturesKitchen.PlayerWaterBottle.Instance;
        if (bottle != null)
        {
            refillWaterButtonText.text = bottle.CurrentWater >= bottle.MaxWater
                ? $"💧 Bottle Full ({Mathf.FloorToInt(bottle.CurrentWater)}/100L)"
                : $"💧 Refill Bottle ({Mathf.FloorToInt(bottle.CurrentWater)}/100L)";
        }
    }

    /// <summary>
    /// Update progress bar and status text from processor state.
    /// </summary>
    private void SyncProgressUI()
    {
        if (processor == null) return;

        if (processor.IsWashing)
        {
            if (progressFillImage != null)
                progressFillImage.fillAmount = processor.WashProgress;

            float remaining = Mathf.Ceil(processor.WashDurationPerItem * (1f - processor.WashProgress));
            UpdateStatus($"Washing... ({remaining:F0}s)");
        }
        else
        {
            if (processor.InputSlot != null && !processor.InputSlot.IsEmpty)
            {
                // Items waiting but not washing (output full, etc.)
                if (processor.OutputSlot != null && !processor.OutputSlot.IsEmpty &&
                    processor.OutputSlot.quantity >= processor.OutputSlot.item.maxStack)
                {
                    UpdateStatus("Output slot full");
                }
                else
                {
                    UpdateStatus("Waiting...");
                }
                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;
            }
            else if (processor.OutputSlot != null && !processor.OutputSlot.IsEmpty)
            {
                UpdateStatus("Done! Collect clean items");
                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;
            }
            else
            {
                UpdateStatus("Place dirty items in the left slot");
                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    // POPULATE PLAYER INVENTORY GRID
    // ═══════════════════════════════════════════════════════

    public void PopulatePlayerInventory()
    {
        ClearSpawnedSlots();

        // Extra safety: destroy any lingering children in grid
        if (playerInventoryGrid != null)
        {
            for (int i = playerInventoryGrid.childCount - 1; i >= 0; i--)
            {
                var child = playerInventoryGrid.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        if (playerInventoryGrid == null || playerInventory == null) return;

        for (int i = 0; i < playerInventory.slots.Count; i++)
        {
            InventorySlot slot = playerInventory.slots[i];
            CreateSlotUI(slot, i);
        }
    }

    private void ClearSpawnedSlots()
    {
        foreach (var go in spawnedSlots)
        {
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
        spawnedSlots.Clear();
    }

    private void CreateSlotUI(InventorySlot slot, int index)
    {
        // Slot container
        var slotGO = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        slotGO.transform.SetParent(playerInventoryGrid, false);
        var slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(80, 80);
        var slotImg = slotGO.GetComponent<Image>();
        slotImg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

        // Icon — render for ALL items (dirty or not)
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.09f, 0.09f);
        iconRT.anchorMax = new Vector2(0.91f, 0.91f);
        iconRT.sizeDelta = Vector2.zero;
        var iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        if (slot.item != null)
        {
            Sprite iconSprite = GetItemSprite(slot.item);
            if (iconSprite != null)
            {
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.enabled = true;
            }
            else
            {
                iconImg.sprite = null;
                iconImg.color = new Color(1f, 1f, 1f, 0f);
                iconImg.enabled = false;
            }
        }
        else
        {
            iconImg.sprite = null;
            iconImg.color = new Color(1f, 1f, 1f, 0f);
            iconImg.enabled = false;
        }

        // Quantity text
        if (slot.quantity > 1)
        {
            var qtyGO = new GameObject("Qty", typeof(RectTransform), typeof(TextMeshProUGUI));
            qtyGO.transform.SetParent(slotGO.transform, false);
            var qtyRT = qtyGO.GetComponent<RectTransform>();
            qtyRT.anchorMin = Vector2.zero;
            qtyRT.anchorMax = Vector2.one;
            qtyRT.sizeDelta = Vector2.zero;
            var qtyTMP = qtyGO.GetComponent<TMPro.TextMeshProUGUI>();
            qtyTMP.text = slot.quantity.ToString();
            qtyTMP.fontSize = 14;
            qtyTMP.color = Color.white;
            qtyTMP.alignment = TextAlignmentOptions.BottomRight;
        }

        // Dirty indicator badge
        if (slot.item != null && IsDirty(slot.item))
        {
            var dirtyGO = new GameObject("DirtyBadge", typeof(RectTransform), typeof(Image));
            dirtyGO.transform.SetParent(slotGO.transform, false);
            var dirtyRT = dirtyGO.GetComponent<RectTransform>();
            dirtyRT.anchorMin = new Vector2(0.65f, 0.65f);
            dirtyRT.anchorMax = new Vector2(1f, 1f);
            dirtyRT.sizeDelta = Vector2.zero;
            var dirtyImg = dirtyGO.GetComponent<Image>();
            dirtyImg.color = new Color(1f, 0.5f, 0f, 0.9f);

            var dirtyLabel = new GameObject("!", typeof(RectTransform), typeof(TextMeshProUGUI));
            dirtyLabel.transform.SetParent(dirtyGO.transform, false);
            var dirtyLabelRT = dirtyLabel.GetComponent<RectTransform>();
            dirtyLabelRT.anchorMin = Vector2.zero;
            dirtyLabelRT.anchorMax = Vector2.one;
            dirtyLabelRT.sizeDelta = Vector2.zero;
            var dirtyTMP = dirtyLabel.GetComponent<TextMeshProUGUI>();
            dirtyTMP.text = "!";
            dirtyTMP.fontSize = 16;
            dirtyTMP.fontStyle = FontStyles.Bold;
            dirtyTMP.color = Color.white;
            dirtyTMP.alignment = TextAlignmentOptions.Center;
        }

        // Hotbar indicator badge for slots 0..3
        if (index < 4)
        {
            var hbGO = new GameObject("HotbarBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            hbGO.transform.SetParent(slotGO.transform, false);
            var hbRT = hbGO.GetComponent<RectTransform>();
            hbRT.anchorMin = new Vector2(0f, 0.65f);
            hbRT.anchorMax = new Vector2(0.45f, 1f);
            hbRT.sizeDelta = Vector2.zero;
            var hbTMP = hbGO.GetComponent<TextMeshProUGUI>();
            hbTMP.text = $"H{index + 1}";
            hbTMP.fontSize = 12;
            hbTMP.fontStyle = FontStyles.Bold;
            hbTMP.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            hbTMP.alignment = TextAlignmentOptions.TopLeft;
        }

        // Click handler — handled by SinkDragDropHandler.OnPointerClick (IPointerClickHandler).
        // Button component kept for visual feedback (highlight/press states) but no onClick.
        int capturedIndex = index;

        // Hover handlers for description
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        int hoverIndex = index;
        entry.callback.AddListener((_) => ShowItemDescription(playerInventory.slots[hoverIndex].item));
        slotGO.AddComponent<UnityEngine.EventSystems.EventTrigger>().triggers.Add(entry);

        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((_) => ShowItemDescription(null));
        slotGO.GetComponent<UnityEngine.EventSystems.EventTrigger>().triggers.Add(exitEntry);

        // Drag-drop handler
        var dragHandler = slotGO.AddComponent<SinkDragDropHandler>();
        dragHandler.slotType = SinkDragDropHandler.SlotType.PlayerInventory;
        dragHandler.sinkManager = this;
        dragHandler.inventoryIndex = index;

        spawnedSlots.Add(slotGO);
    }

    private bool IsDirty(ItemData item)
    {
        if (item is FoodItemData food) return food.isDirty;
        if (item is MaterialItemData mat) return mat.isDirty;
        return false;
    }

    private Sprite GetItemSprite(ItemData item)
    {
        if (item == null) return null;
        if (item.itemIcon != null) return item.itemIcon;
        // Fallback: check for field named "icon" via reflection
        var iconField = item.GetType().GetField("icon");
        if (iconField != null) return iconField.GetValue(item) as Sprite;
        return null;
    }

    /// <summary>
    /// <summary>
    /// Add item to player inventory, prioritizing main inventory slots (4+) before hotbar (0-3).
    /// Returns true if all items were added.
    /// </summary>
    private bool AddItemToMainInventory(ItemData item, int quantity)
    {
        if (playerInventory == null || item == null) return false;
        playerInventory.HasHotbar = true;
        return playerInventory.AddItem(item, quantity);
    }

    // ═══════════════════════════════════════════════════════
    // PUBLIC ACCESSORS (for DragDrop — delegates to processor)
    // ═══════════════════════════════════════════════════════

    public InventorySlot GetPlayerSlot(int index)
    {
        if (playerInventory == null || index < 0 || index >= playerInventory.slots.Count)
            return null;
        return playerInventory.slots[index];
    }

    public InventorySlot GetOutputSlot()
    {
        return processor != null ? processor.OutputSlot : null;
    }

    public InventorySlot GetInputSlot()
    {
        return processor != null ? processor.InputSlot : null;
    }

    public void SwapPlayerSlots(int indexA, int indexB)
    {
        if (indexA == indexB) return;
        if (playerInventory == null) return;
        if (indexA < 0 || indexA >= playerInventory.slots.Count) return;
        if (indexB < 0 || indexB >= playerInventory.slots.Count) return;

        InventorySlot slotA = playerInventory.slots[indexA];
        InventorySlot slotB = playerInventory.slots[indexB];

        // Case 1: Target is empty → move A to B
        if (slotB.IsEmpty)
        {
            slotB.item = slotA.item;
            slotB.quantity = slotA.quantity;
            slotA.item = null;
            slotA.quantity = 0;
        }
        // Case 2: Same item → merge/stack (combine quantities up to maxStack)
        else if (!slotA.IsEmpty && slotA.item == slotB.item)
        {
            int maxStack = slotB.item.maxStack;
            int space = maxStack - slotB.quantity;
            if (space > 0)
            {
                int toMove = Mathf.Min(space, slotA.quantity);
                slotB.quantity += toMove;
                slotA.quantity -= toMove;
                if (slotA.quantity <= 0)
                {
                    slotA.item = null;
                    slotA.quantity = 0;
                }
            }
            // If no space (target full), do nothing — no swap needed for same items
        }
        // Case 3: Different items → swap
        else
        {
            ItemData tempItem = slotA.item;
            int tempQty = slotA.quantity;
            slotA.item = slotB.item;
            slotA.quantity = slotB.quantity;
            slotB.item = tempItem;
            slotB.quantity = tempQty;
        }

        PopulatePlayerInventory();
        RefreshSlotVisuals();
    }

    // ═══════════════════════════════════════════════════════
    // CLICK HANDLERS
    // ═══════════════════════════════════════════════════════

    private void OnPlayerSlotClicked(int slotIndex)
    {
        if (playerInventory == null || slotIndex < 0 || slotIndex >= playerInventory.slots.Count) return;

        InventorySlot playerSlot = playerInventory.slots[slotIndex];
        ItemData itemToMove = playerSlot.item;
        int amountToMove = playerSlot.quantity;

        if (itemToMove == null || amountToMove <= 0) return;

        if (!IsDirty(itemToMove))
        {
            UpdateStatus("Only dirty items can be washed");
            ShowItemDescription(itemToMove);
            return;
        }

        TransferToInputSlot(slotIndex);
        ShowItemDescription(itemToMove);
    }

    public void OnOutputSlotClicked()
    {
        TransferFromOutputToPlayer();
    }

    /// <summary>
    /// Transfer dirty items from player inventory to processor inputSlot.
    /// </summary>
    public bool TransferToInputSlot(int playerSlotIndex)
    {
        EnsurePlayerInventoryRef();
        if (processor == null)
            processor = FindFirstObjectByType<KitchenSinkInteractable>();
        if (processor == null || playerInventory == null) return false;
        if (playerSlotIndex < 0 || playerSlotIndex >= playerInventory.slots.Count) return false;

        InventorySlot playerSlot = playerInventory.slots[playerSlotIndex];
        InventorySlot inputSlot = processor.InputSlot;
        if (playerSlot == null || inputSlot == null) return false;

        ItemData itemToMove = playerSlot.item;
        int amountToMove = playerSlot.quantity;

        if (itemToMove == null || amountToMove <= 0) return false;

        if (!IsDirty(itemToMove))
        {
            UpdateStatus("This item is not dirty!");
            return false;
        }

        if (!inputSlot.IsEmpty && inputSlot.item != itemToMove)
        {
            UpdateStatus("Input slot contains a different item!");
            return false;
        }

        int inputCapacity = itemToMove.maxStack - inputSlot.quantity;
        if (inputCapacity <= 0)
        {
            UpdateStatus("Input slot is full");
            return false;
        }
        if (amountToMove > inputCapacity)
            amountToMove = inputCapacity;

        if (inputSlot.IsEmpty)
        {
            inputSlot.item = itemToMove;
            inputSlot.quantity = amountToMove;
        }
        else
        {
            inputSlot.quantity += amountToMove;
        }

        playerInventory.RemoveFromSlot(playerSlotIndex, amountToMove);

        PopulatePlayerInventory();
        RefreshSlotVisuals();
        processor.StartWashing();
        SyncProgressUI();

        return true;
    }

    /// <summary>
    /// Transfer clean items from processor outputSlot to player inventory.
    /// </summary>
    public bool TransferFromOutputToPlayer(int targetSlotIndex = -1)
    {
        if (processor == null || playerInventory == null) return false;

        InventorySlot outputSlot = processor.OutputSlot;
        if (outputSlot == null || outputSlot.IsEmpty) return false;

        ItemData cleanItem = outputSlot.item;
        int qty = outputSlot.quantity;

        if (targetSlotIndex >= 0)
        {
            InventorySlot targetSlot = playerInventory.slots[targetSlotIndex];
            if (targetSlot != null)
            {
                if (targetSlot.IsEmpty)
                {
                    targetSlot.item = cleanItem;
                    targetSlot.quantity = qty;
                    qty = 0;
                }
                else if (targetSlot.item == cleanItem && targetSlot.quantity < cleanItem.maxStack)
                {
                    int canAdd = cleanItem.maxStack - targetSlot.quantity;
                    int toAdd = Mathf.Min(qty, canAdd);
                    targetSlot.quantity += toAdd;
                    qty -= toAdd;
                }
            }
        }

        if (qty > 0)
        {
            playerInventory.HasHotbar = true;
            int added = playerInventory.AddItemAmount(cleanItem, qty);
            if (added <= 0)
            {
                UpdateStatus("Inventory is full!");
                return false;
            }
            outputSlot.quantity -= added;
            if (outputSlot.quantity <= 0)
            {
                outputSlot.item = null;
                outputSlot.quantity = 0;
            }
        }
        else
        {
            outputSlot.item = null;
            outputSlot.quantity = 0;
        }

        PopulatePlayerInventory();
        RefreshSlotVisuals();
        processor.StartWashing();
        SyncProgressUI();
        UpdateStatus("Clean items moved to inventory");
        return true;
    }

    /// <summary>
    /// Cancel washing and return ALL dirty items from processor inputSlot to player inventory.
    /// </summary>
    public bool TransferFromInputToPlayer(int targetSlotIndex = -1)
    {
        if (processor == null || playerInventory == null) return false;

        InventorySlot inputSlot = processor.InputSlot;
        if (inputSlot == null || inputSlot.IsEmpty || inputSlot.quantity <= 0) return false;

        processor.StopWashing();

        ItemData itemToMove = inputSlot.item;
        int qty = inputSlot.quantity;

        if (targetSlotIndex >= 0)
        {
            InventorySlot targetSlot = playerInventory.slots[targetSlotIndex];
            if (targetSlot != null)
            {
                if (targetSlot.IsEmpty)
                {
                    targetSlot.item = itemToMove;
                    targetSlot.quantity = qty;
                    qty = 0;
                }
                else if (targetSlot.item == itemToMove && targetSlot.quantity < itemToMove.maxStack)
                {
                    int canAdd = itemToMove.maxStack - targetSlot.quantity;
                    int toAdd = Mathf.Min(qty, canAdd);
                    targetSlot.quantity += toAdd;
                    qty -= toAdd;
                }
            }
        }

        if (qty > 0)
        {
            playerInventory.HasHotbar = true;
            int added = playerInventory.AddItemAmount(itemToMove, qty);
            if (added <= 0)
            {
                UpdateStatus("Inventory is full! Cannot return item.");
                return false;
            }
            inputSlot.quantity -= added;
            if (inputSlot.quantity <= 0)
            {
                inputSlot.item = null;
                inputSlot.quantity = 0;
            }
        }
        else
        {
            inputSlot.item = null;
            inputSlot.quantity = 0;
        }

        PopulatePlayerInventory();
        RefreshSlotVisuals();
        SyncProgressUI();
        UpdateStatus("Dirty items returned to inventory");

        return true;
    }

    // ═══════════════════════════════════════════════════════
    // VISUAL HELPERS
    // ═══════════════════════════════════════════════════════

    public void RefreshSlotVisuals()
    {
        RefreshInputSlotVisual();
        RefreshOutputSlotVisual();
    }

    private void RefreshInputSlotVisual()
    {
        if (inputSlotImage == null || processor == null) return;

        InventorySlot inputSlot = processor.InputSlot;
        if (inputSlot == null || inputSlot.IsEmpty || inputSlot.item == null || inputSlot.quantity <= 0)
        {
            inputSlotImage.sprite = null;
            inputSlotImage.color = new Color(1f, 1f, 1f, 0f);
            inputSlotImage.enabled = false;
            if (inputCountText != null) inputCountText.text = "";
            return;
        }

        Sprite iconSprite = GetItemSprite(inputSlot.item);
        if (iconSprite != null)
        {
            inputSlotImage.sprite = iconSprite;
            inputSlotImage.color = Color.white;
            inputSlotImage.enabled = true;
            inputSlotImage.preserveAspect = true;
        }
        else
        {
            inputSlotImage.sprite = null;
            inputSlotImage.color = new Color(1f, 1f, 1f, 0f);
            inputSlotImage.enabled = false;
        }
        if (inputCountText != null)
            inputCountText.text = inputSlot.quantity > 1 ? inputSlot.quantity.ToString() : "";
    }

    private void RefreshOutputSlotVisual()
    {
        if (outputSlotImage == null || processor == null) return;

        InventorySlot outputSlot = processor.OutputSlot;
        if (outputSlot == null || outputSlot.IsEmpty || outputSlot.item == null || outputSlot.quantity <= 0)
        {
            outputSlotImage.sprite = null;
            outputSlotImage.color = new Color(1f, 1f, 1f, 0f);
            outputSlotImage.enabled = false;
            if (outputCountText != null) outputCountText.text = "";
            return;
        }

        Sprite iconSprite = GetItemSprite(outputSlot.item);
        if (iconSprite != null)
        {
            outputSlotImage.sprite = iconSprite;
            outputSlotImage.color = Color.white;
            outputSlotImage.enabled = true;
            outputSlotImage.preserveAspect = true;
        }
        else
        {
            outputSlotImage.sprite = null;
            outputSlotImage.color = new Color(1f, 1f, 1f, 0f);
            outputSlotImage.enabled = false;
        }
        if (outputCountText != null)
            outputCountText.text = outputSlot.quantity > 1 ? outputSlot.quantity.ToString() : "";
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    // ═══════════════════════════════════════════════════════
    // ITEM DESCRIPTION PANEL
    // ═══════════════════════════════════════════════════════

    public void ShowItemDescription(ItemData item)
    {
        if (itemDescriptionText == null) return;

        if (item != null)
        {
            string name = item.itemName ?? "";
            string desc = item.description ?? "";
            itemDescriptionText.text = $"<b>{name}</b>\n{desc}";
        }
        else
        {
            itemDescriptionText.text = "";
        }
    }
}
