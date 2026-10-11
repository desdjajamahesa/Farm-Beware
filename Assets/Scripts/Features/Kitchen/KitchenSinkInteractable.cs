using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;

/// <summary>
/// Kitchen Sink: owns washing processor logic and slots.
/// Runs Update() independently of panel visibility (on Kitchen_Sink, always active).
/// SinkManager reads from this via public accessors for UI display.
/// </summary>
[RequireComponent(typeof(InventoryComponent))]
public class KitchenSinkInteractable : KitchenStation, IInteractable, IModalWindow
{
    [Header("Kitchen Sink Settings")]
    [Tooltip("Default wash duration per item in seconds.")]
    [SerializeField] private float washDurationPerItem = 10f;

    [Tooltip("Allowed food categories that can be washed in the sink.")]
    [SerializeField] private List<ItemData.FoodCategory> allowedCategories =
        new List<ItemData.FoodCategory>
        {
            ItemData.FoodCategory.Vegetable,
            ItemData.FoodCategory.Fruit,
        };

    [Tooltip("If true, washed items are returned directly to player inventory.")]
    [SerializeField] private bool returnWashedToPlayer = false;

    [Header("Panel Reference")]
    [Tooltip("Panel_Sink GameObject (Furnace-style UI).")]
    [SerializeField] private GameObject panelSink;

    // ── Washing State (owned by processor) ──
    private InventorySlot inputSlot;
    private InventorySlot outputSlot;
    private bool isWashing;
    private float washProgress;
    private KitchenRecipe virtualRecipe;

    // ── Public Accessors (for SinkManager UI sync) ──
    public static KitchenSinkInteractable Instance { get; private set; }
    public bool IsPanelOpen => panelSink != null && panelSink.activeSelf;
    public bool IsOpen => IsPanelOpen;
    public void OpenModal() => Interact(null);
    public void CloseModal() => ClosePanel();

    public InventorySlot InputSlot { get { if (inputSlot == null) inputSlot = new InventorySlot(); return inputSlot; } }
    public InventorySlot OutputSlot { get { if (outputSlot == null) outputSlot = new InventorySlot(); return outputSlot; } }
    public bool IsWashing => isWashing;
    public float WashProgress => washProgress;
    public float WashDurationPerItem => washDurationPerItem;

    [Header("Refill Water Settings")]
    [Tooltip("Time required to refill the drinking water bottle in seconds (30s per MVP guideline).")]
    [SerializeField] private float refillWaterDuration = 30f;

    // ── Water Refill State ──
    private bool hasBottleInSink;
    private bool isRefillingWater;
    private bool isBottleReadyToCollect;
    private float refillWaterProgress;
    private GameObject sinkBottleVisualGO;

    public bool HasBottleInSink => hasBottleInSink;
    public bool IsRefillingWater => isRefillingWater;
    public bool IsBottleReadyToCollect => isBottleReadyToCollect;
    public float RefillWaterProgress => refillWaterProgress;
    public float RefillWaterDuration => refillWaterDuration;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        InventoryManagerUI.IsSinkOpenCheck = () => (Instance != null && Instance.IsPanelOpen);

        // Initialize slots
        if (inputSlot == null) inputSlot = new InventorySlot();
        if (outputSlot == null) outputSlot = new InventorySlot();

        virtualRecipe = ScriptableObject.CreateInstance<KitchenRecipe>();
        virtualRecipe.name = "VirtualWashRecipe";

        if (allowedCategories != null && allowedCategories.Count > 0)
            stationInventory?.SetAllowedFoodCategories(allowedCategories);

        if (!returnWashedToPlayer)
        {
            resultTarget = null;
            return;
        }

        if (resultTarget == null)
        {
            GameObject playerGO = GameObject.Find("Player");
            if (playerGO != null)
                resultTarget = playerGO.GetComponent<InventoryComponent>();
        }
    }

    protected override KitchenRecipe FindRecipeFor(ItemData item, int slotIndex)
    {
        if (item == null) return null;

        if (item is FoodItemData food && food.isDirty && food.cleanVariant != null)
        {
            virtualRecipe.output = food.cleanVariant;
            virtualRecipe.outputCount = 1;
            virtualRecipe.processTime = washDurationPerItem;
            return virtualRecipe;
        }

        if (item is MaterialItemData mat && mat.isDirty && mat.cleanVariant != null)
        {
            virtualRecipe.output = mat.cleanVariant;
            virtualRecipe.outputCount = 1;
            virtualRecipe.processTime = washDurationPerItem;
            return virtualRecipe;
        }

        return null;
    }

    public void Interact(GameObject interactor)
    {
        if (panelSink == null) return;

        var sinkMgr = panelSink.GetComponent<SinkManager>();
        if (sinkMgr != null && interactor != null)
        {
            var pInv = interactor.GetComponent<InventoryComponent>();
            if (pInv != null)
                sinkMgr.SetPlayerInventory(pInv);
        }

        panelSink.SetActive(true);

        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.Push(this);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var playerControl = interactor != null ? interactor.GetComponent<IPlayerContext>() : ServiceLocator.Resolve<IPlayerContext>();
        if (playerControl != null)
            playerControl.IsInputLocked = true;

        // Sync UI to current processor state
        if (sinkMgr != null)
            sinkMgr.SyncToProcessor();
    }

    protected override void Update()
    {
        base.Update();

        // ── Water Refill Timer (runs always, even when panel is closed) ──
        if (isRefillingWater)
        {
            refillWaterProgress += Time.deltaTime / Mathf.Max(0.1f, refillWaterDuration);
            if (refillWaterProgress >= 1f)
            {
                CompleteWaterRefill();
            }
            else
            {
                SyncUIIfOpen();
            }
        }

        // ── Washing Timer (runs always, even when panel is closed) ──
        if (!isWashing) return;

        if (inputSlot == null || inputSlot.IsEmpty || inputSlot.quantity <= 0)
        {
            StopWashing();
            return;
        }

        washProgress += Time.deltaTime / washDurationPerItem;

        if (washProgress >= 1f)
        {
            CompleteWashOneItem();
            washProgress = 0f;

            // Continue if more items
            if (inputSlot.quantity > 0)
            {
                ItemData cleanVariant = GetCleanVariant(inputSlot.item);
                if (cleanVariant == null)
                {
                    StopWashing();
                    return;
                }
                // Validate output capacity
                if (!outputSlot.IsEmpty && outputSlot.quantity >= cleanVariant.maxStack)
                {
                    StopWashing();
                    return;
                }
            }
            else
            {
                isWashing = false;
            }

            // Sync UI if panel is open
            SyncUIIfOpen();
        }
        else
        {
            // Update progress UI if panel is open
            SyncUIIfOpen();
        }
    }

    /// <summary>
    /// Transfer 1 dirty item from inputSlot → outputSlot as clean variant.
    /// </summary>
    private void CompleteWashOneItem()
    {
        if (inputSlot.IsEmpty || inputSlot.quantity <= 0) return;

        ItemData cleanVariant = GetCleanVariant(inputSlot.item);
        if (cleanVariant == null) return;

        inputSlot.quantity--;
        if (inputSlot.quantity <= 0)
        {
            inputSlot.item = null;
            inputSlot.quantity = 0;
        }

        if (outputSlot.IsEmpty)
        {
            outputSlot.item = cleanVariant;
            outputSlot.quantity = 1;
        }
        else
        {
            outputSlot.quantity++;
        }
    }

    /// <summary>
    /// Start washing if inputSlot has valid dirty items.
    /// Called by SinkManager after items are added to inputSlot.
    /// </summary>
    public void StartWashing()
    {
        if (isWashing) return;
        if (inputSlot == null || inputSlot.IsEmpty) return;

        ItemData cleanVariant = GetCleanVariant(inputSlot.item);
        if (cleanVariant == null) return;

        if (!outputSlot.IsEmpty)
        {
            if (outputSlot.item != cleanVariant) return;
            if (outputSlot.quantity >= cleanVariant.maxStack) return;
        }

        isWashing = true;
        washProgress = 0f;
    }

    /// <summary>
    /// Stop washing. Called when player cancels (removes items from inputSlot).
    /// NOT called when panel closes.
    /// </summary>
    public void StopWashing()
    {
        isWashing = false;
        washProgress = 0f;
    }

    public bool StartRefillingWater(InventoryComponent playerInv = null)
    {
        if (hasBottleInSink)
        {
            if (isBottleReadyToCollect)
            {
                SpawnFloatingAlert("💧 Full Water Bottle is in the sink! Take it.");
            }
            return false;
        }

        if (playerInv == null)
            playerInv = ServiceLocator.Resolve<IPlayerContext>()?.GetPlayerComponent<InventoryComponent>();

        var bottle = FeaturesKitchen.PlayerWaterBottle.Instance;
        if (bottle != null && bottle.CurrentWater >= bottle.MaxWater)
        {
            SpawnFloatingAlert("💧 Water Bottle is already full!");
            return false;
        }

        var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
        var bottleItem = db != null ? db.GetItem("food_bottle_water") : null;
        if (bottleItem == null)
        {
            Debug.LogWarning("[KitchenSink] 'food_bottle_water' not found in database!");
            return false;
        }

        if (playerInv == null || playerInv.CountItem(bottleItem) <= 0)
        {
            SpawnFloatingAlert("❌ No Water Bottle in inventory to place in sink!");
            return false;
        }

        // Place bottle into the sink (removes from player inventory)
        playerInv.RemoveItem(bottleItem, 1);
        hasBottleInSink = true;
        isRefillingWater = true;
        isBottleReadyToCollect = false;
        refillWaterProgress = 0f;

        UpdateSinkBottleWorldVisual();
        SpawnFloatingAlert("💧 Water Bottle placed in Sink! Refilling (30s)...");
        SyncUIIfOpen();
        return true;
    }

    public void CancelRefillingWater(InventoryComponent playerInv = null)
    {
        if (!hasBottleInSink || isBottleReadyToCollect) return;

        if (playerInv == null)
            playerInv = ServiceLocator.Resolve<IPlayerContext>()?.GetPlayerComponent<InventoryComponent>();

        var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
        var bottleItem = db?.GetItem("food_bottle_water");
        if (bottleItem != null && playerInv != null)
        {
            playerInv.AddItem(bottleItem, 1);
        }

        hasBottleInSink = false;
        isRefillingWater = false;
        refillWaterProgress = 0f;

        UpdateSinkBottleWorldVisual();
        SpawnFloatingAlert("💧 Water refill cancelled. Bottle returned to inventory.");
        SyncUIIfOpen();
    }

    private void CompleteWaterRefill()
    {
        isRefillingWater = false;
        refillWaterProgress = 1f;
        isBottleReadyToCollect = true;

        // Note: Item stays in the sink and does NOT immediately teleport to player's inventory!
        UpdateSinkBottleWorldVisual();
        SpawnFloatingAlert("💧 Water Bottle in Sink is full! Ready to take.");
        SyncUIIfOpen();
    }

    public bool CollectFilledBottle(InventoryComponent playerInv = null)
    {
        if (!hasBottleInSink || !isBottleReadyToCollect) return false;

        if (playerInv == null)
            playerInv = ServiceLocator.Resolve<IPlayerContext>()?.GetPlayerComponent<InventoryComponent>();

        var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
        var bottleItem = db?.GetItem("food_bottle_water");
        if (bottleItem == null || playerInv == null) return false;

        int added = playerInv.AddItemAmount(bottleItem, 1);
        if (added <= 0)
        {
            SpawnFloatingAlert("❌ Inventory full! Free space to take Water Bottle.");
            return false;
        }

        var bottle = FeaturesKitchen.PlayerWaterBottle.Instance;
        if (bottle != null)
        {
            bottle.RefillWater(bottle.MaxWater);
        }

        hasBottleInSink = false;
        isBottleReadyToCollect = false;
        refillWaterProgress = 0f;

        UpdateSinkBottleWorldVisual();
        SpawnFloatingAlert("💧 Taken Full Water Bottle from Sink (4/4 Charges)!");
        SyncUIIfOpen();
        return true;
    }

    private void UpdateSinkBottleWorldVisual()
    {
        if (hasBottleInSink)
        {
            if (sinkBottleVisualGO == null)
            {
                sinkBottleVisualGO = new GameObject("Sink_Bottle_Visual");
                sinkBottleVisualGO.transform.SetParent(transform, false);
                sinkBottleVisualGO.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                sinkBottleVisualGO.transform.localScale = Vector3.one * 0.35f;

                var sr = sinkBottleVisualGO.AddComponent<SpriteRenderer>();
                var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
                var bottleItem = db?.GetItem("food_bottle_water");
                if (bottleItem != null && bottleItem.itemIcon != null)
                {
                    sr.sprite = bottleItem.itemIcon;
                }
            }

            sinkBottleVisualGO.SetActive(true);
            var renderer = sinkBottleVisualGO.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = isBottleReadyToCollect ? Color.white : new Color(0.7f, 0.9f, 1f, 0.9f);
            }
        }
        else
        {
            if (sinkBottleVisualGO != null && sinkBottleVisualGO.activeSelf)
            {
                sinkBottleVisualGO.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (sinkBottleVisualGO != null && sinkBottleVisualGO.activeSelf)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                sinkBottleVisualGO.transform.forward = cam.transform.forward;
            }
        }
    }

    private void SpawnFloatingAlert(string text)
    {
        var floatText = ServiceLocator.Resolve<IFloatingTextService>();
        if (floatText != null)
        {
            var player = ServiceLocator.Resolve<IPlayerContext>();
            Vector3 pos = player != null ? player.Transform.position + Vector3.up * 1.5f : transform.position + Vector3.up * 1.5f;
            floatText.SpawnText(pos, text, new Color(0.25f, 0.85f, 1f));
        }
    }

    public void ClosePanel()
    {
        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }

        if (panelSink != null)
        {
            var sinkMgr = panelSink.GetComponent<SinkManager>();
            if (sinkMgr != null)
                sinkMgr.ClosePanel();
        }

        if (panelSink != null)
            panelSink.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var player = ServiceLocator.Resolve<IPlayerContext>();
        if (player != null)
            player.IsInputLocked = false;
    }

    /// <summary>
    /// Sync SinkManager UI if panel is currently open.
    /// </summary>
    private void SyncUIIfOpen()
    {
        if (panelSink == null || !panelSink.activeSelf) return;
        var sinkMgr = panelSink.GetComponent<SinkManager>();
        if (sinkMgr != null)
            sinkMgr.SyncToProcessor();
    }

    private ItemData GetCleanVariant(ItemData item)
    {
        if (item is FoodItemData food && food.isDirty && food.cleanVariant != null)
            return food.cleanVariant;
        if (item is MaterialItemData mat && mat.isDirty && mat.cleanVariant != null)
            return mat.cleanVariant;
        return null;
    }

    private void OnDestroy()
    {
        if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }

        if (sinkBottleVisualGO != null)
            Destroy(sinkBottleVisualGO);

        if (Instance == this)
            Instance = null;

        if (virtualRecipe != null)
            Destroy(virtualRecipe);
    }
}
