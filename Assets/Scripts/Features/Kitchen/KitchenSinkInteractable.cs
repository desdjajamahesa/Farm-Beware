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
    public enum SinkWashState
    {
        Idle,
        Washing
    }

    private InventorySlot inputSlot;
    private InventorySlot outputSlot;
    private bool isWashing;
    private float washProgress;
    private KitchenRecipe virtualRecipe;

    // ── Public Accessors (for SinkManager UI sync) ──
    public static KitchenSinkInteractable Instance { get; private set; }
    public static event System.Action<ItemData, int> OnCropWashed;
    public bool IsPanelOpen => panelSink != null && panelSink.activeSelf;
    public bool IsOpen => IsPanelOpen;
    public void OpenModal() => Interact(null);
    public void CloseModal() => ClosePanel();

    public InventorySlot InputSlot { get { if (inputSlot == null) inputSlot = new InventorySlot(); return inputSlot; } }
    public InventorySlot OutputSlot { get { if (outputSlot == null) outputSlot = new InventorySlot(); return outputSlot; } }
    public SinkWashState CurrentWashState => isWashing ? SinkWashState.Washing : SinkWashState.Idle;
    public bool IsWashing => isWashing;
    public float WashProgress => washProgress;
    public float WashDurationPerItem => washDurationPerItem;

    public override bool IsProcessing(int slot) => isWashing;
    public override float GetSlotProgress(int slot) => isWashing ? Mathf.Clamp01(washProgress) : 0f;

    [Header("Refill Water Settings")]
    [Tooltip("Time required to refill the drinking water bottle in seconds (30s per MVP guideline).")]
    [SerializeField] private float refillWaterDuration = 30f;

    [Header("Daily Bottle Refill Quota")]
    [Tooltip("Maximum completed drinking water bottle refills allowed at the sink per day (TE-51 / TC-BTL-6).")]
    [SerializeField] private int maxBottleRefillsPerDay = 2;
    [SerializeField] private int bottleRefillsUsedToday = 0;

    // ── Water Refill State (Pure-Logic FSM + synchronized fields) ──
    private RefillStationStateMachine _refillFSM;
    private bool hasBottleInSink;
    private bool isRefillingWater;
    private bool isBottleReadyToCollect;
    private float refillWaterProgress;
    private WaterRefillProgressUI refillProgressUI;

    public RefillStationState CurrentRefillState => _refillFSM.State;
    public bool HasBottleInSink => hasBottleInSink;
    public bool IsRefillingWater => isRefillingWater;
    public bool IsBottleReadyToCollect => isBottleReadyToCollect;
    public float RefillWaterProgress => refillWaterProgress;
    public float RefillWaterDuration => refillWaterDuration;
    public int MaxBottleRefillsPerDay => maxBottleRefillsPerDay;
    public int BottleRefillsUsedToday => bottleRefillsUsedToday;
    public int RemainingBottleRefillsToday => maxBottleRefillsPerDay > 0 ? Mathf.Max(0, maxBottleRefillsPerDay - bottleRefillsUsedToday) : 99;

    protected override void OnEnable()
    {
        base.OnEnable();
        TimeManager.OnAnyDayChanged += HandleDayChangedForBottleQuota;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        TimeManager.OnAnyDayChanged -= HandleDayChangedForBottleQuota;
    }

    private void HandleDayChangedForBottleQuota(int newDay)
    {
        bottleRefillsUsedToday = 0;
        SyncUIIfOpen();
    }

    private void SyncRefillFieldsFromFSM()
    {
        hasBottleInSink = _refillFSM.HasItem;
        isRefillingWater = _refillFSM.IsRefilling;
        isBottleReadyToCollect = _refillFSM.IsReadyToCollect;
        refillWaterProgress = _refillFSM.Progress01;
    }

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        refillProgressUI = GetComponent<WaterRefillProgressUI>() ?? gameObject.AddComponent<WaterRefillProgressUI>();
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
        if (item == null || !item.isDirty || item.cleanVariant == null) return null;

        if (virtualRecipe == null)
        {
            virtualRecipe = ScriptableObject.CreateInstance<KitchenRecipe>();
            virtualRecipe.name = "VirtualWashRecipe";
        }

        virtualRecipe.output = item.cleanVariant;
        virtualRecipe.outputCount = 1;
        virtualRecipe.processTime = washDurationPerItem;
        return virtualRecipe;
    }

    public void Interact(GameObject interactor)
    {
        var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
        int currentDay = timeService != null ? timeService.CurrentDay : (FeaturesTime.DayNightTimeManager.Instance != null ? FeaturesTime.DayNightTimeManager.Instance.CurrentDay : 1);
        if (currentDay == 0)
        {
            FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IFloatingTextService>()?.SpawnText(
                transform.position + Vector3.up * 1.2f,
                "Rest first. The harvest begins tomorrow.",
                new Color(1f, 0.8f, 0.4f));
            return;
        }

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
            playerControl.AddInputLock("KitchenSinkUI");

        // Sync UI to current processor state
        if (sinkMgr != null)
            sinkMgr.SyncToProcessor();
    }

    protected override void Update()
    {
        base.Update();

        float timeMultiplier = FeaturesTime.DayNightTimeManager.Instance != null
            ? FeaturesTime.DayNightTimeManager.Instance.FastForwardMultiplier
            : 1f;

        // ── Water Refill Timer (runs always, even when panel is closed) ──
        if (isRefillingWater)
        {
            if (_refillFSM.State != RefillStationState.Refilling)
            {
                _refillFSM.Restore(hasBottleInSink, isBottleReadyToCollect, refillWaterProgress);
            }

            bool completed = _refillFSM.Tick(Time.deltaTime * timeMultiplier, refillWaterDuration);
            SyncRefillFieldsFromFSM();

            if (completed)
            {
                CompleteWaterRefill();
            }
            else
            {
                SyncUIIfOpen();
            }
        }

        // Update clean in-world Screen-Space Refill UI (tanpa sprite mengambang)
        if (refillProgressUI != null)
        {
            float remSec = Mathf.Max(0f, refillWaterDuration * (1f - refillWaterProgress));
            refillProgressUI.UpdateRefillState(isRefillingWater, refillWaterProgress, remSec, isBottleReadyToCollect, "BOTTLE");
        }

        // ── Washing Timer (runs always, even when panel is closed) ──
        if (!isWashing) return;

        if (inputSlot == null || inputSlot.IsEmpty || inputSlot.quantity <= 0)
        {
            StopWashing();
            return;
        }

        washProgress += (Time.deltaTime * timeMultiplier) / washDurationPerItem;
        RaiseProcessProgress(0, Mathf.Clamp01(washProgress));

        if (washProgress >= 1f)
        {
            CompleteWashOneItem();
            washProgress = 0f;
            RaiseProcessCompleted(0);

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
                RaiseProcessStarted(0, washDurationPerItem);
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

        OnCropWashed?.Invoke(cleanVariant, 1);
        FarmBeware.Core.Runtime.NarrativeTelemetry.ReportCropWashed(cleanVariant?.itemName ?? "", 1);
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
        RaiseProcessStarted(0, washDurationPerItem);
    }

    /// <summary>
    /// Stop washing. Called when player cancels (removes items from inputSlot).
    /// NOT called when panel closes.
    /// </summary>
    public void StopWashing()
    {
        isWashing = false;
        washProgress = 0f;
        RaiseProcessCancelled(0);
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

        if (maxBottleRefillsPerDay > 0 && bottleRefillsUsedToday >= maxBottleRefillsPerDay)
        {
            SpawnFloatingAlert($"⚠️ Sink bottle refill limit reached for today ({bottleRefillsUsedToday}/{maxBottleRefillsPerDay})! Rest until tomorrow.");
            return false;
        }

        if (playerInv == null)
            playerInv = ServiceLocator.Resolve<IPlayerContext>()?.GetPlayerComponent<InventoryComponent>();

        var bottle = (playerInv != null ? playerInv.GetComponent<FeaturesKitchen.PlayerWaterBottle>() : null)
            ?? FeaturesKitchen.PlayerWaterBottle.Instance;
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
        _refillFSM.PlaceItem();
        SyncRefillFieldsFromFSM();

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
            int added = playerInv.AddItemAmount(bottleItem, 1);
            if (added <= 0)
            {
                SpawnFloatingAlert("❌ Inventory full! Free space to retrieve Water Bottle.");
                return;
            }
        }

        _refillFSM.ResetToEmpty();
        SyncRefillFieldsFromFSM();

        SpawnFloatingAlert("💧 Water refill cancelled. Bottle returned to inventory.");
        SyncUIIfOpen();
    }

    private void CompleteWaterRefill()
    {
        _refillFSM.CompleteImmediate();
        SyncRefillFieldsFromFSM();
        bottleRefillsUsedToday++;

        // Note: Item stays in the sink and does NOT immediately teleport to player's inventory!
        SpawnFloatingAlert($"💧 Water Bottle in Sink is full! Ready to take ({RemainingBottleRefillsToday}/{maxBottleRefillsPerDay} refills left today).");
        SyncUIIfOpen();
    }

    public void ResetSinkState()
    {
        if (inputSlot == null) inputSlot = new InventorySlot();
        if (outputSlot == null) outputSlot = new InventorySlot();

        inputSlot.item = null;
        inputSlot.quantity = 0;
        outputSlot.item = null;
        outputSlot.quantity = 0;

        isWashing = false;
        washProgress = 0f;

        _refillFSM.ResetToEmpty();
        SyncRefillFieldsFromFSM();
        bottleRefillsUsedToday = 0;

        RaiseProcessCancelled(0);
        SyncUIIfOpen();
    }

    public void RestoreSinkState(
        ItemData inputItem,
        int inputQuantity,
        ItemData outputItem,
        int outputQuantity,
        bool savedWashing,
        float savedWashProgress,
        bool savedHasBottle,
        bool savedBottleReady,
        float savedBottleProgress,
        int savedBottleRefillsUsedToday)
    {
        ResetSinkState();

        if (inputItem != null && inputQuantity > 0)
        {
            inputSlot.item = inputItem;
            inputSlot.quantity = inputQuantity;
        }

        if (outputItem != null && outputQuantity > 0)
        {
            outputSlot.item = outputItem;
            outputSlot.quantity = outputQuantity;
        }

        bottleRefillsUsedToday = Mathf.Max(0, savedBottleRefillsUsedToday);

        if (savedWashing && !inputSlot.IsEmpty && GetCleanVariant(inputSlot.item) != null)
        {
            isWashing = true;
            washProgress = Mathf.Clamp01(savedWashProgress);
            RaiseProcessStarted(0, washDurationPerItem);
        }

        _refillFSM.Restore(savedHasBottle, savedBottleReady, savedBottleProgress);
        SyncRefillFieldsFromFSM();

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

        var bottle = (playerInv != null ? playerInv.GetComponent<FeaturesKitchen.PlayerWaterBottle>() : null)
            ?? FeaturesKitchen.PlayerWaterBottle.Instance;
        if (bottle != null)
        {
            bottle.RefillWater(bottle.MaxWater);
        }

        _refillFSM.ResetToEmpty();
        SyncRefillFieldsFromFSM();

        SpawnFloatingAlert("💧 Taken Full Water Bottle from Sink (4/4 Charges)!");
        SyncUIIfOpen();
        return true;
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
            player.RemoveInputLock("KitchenSinkUI");
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

    public ItemData ResolveCleanVariant(ItemData item) => GetCleanVariant(item);

    private ItemData GetCleanVariant(ItemData item)
    {
        if (item != null && item.isDirty && item.cleanVariant != null)
            return item.cleanVariant;
        return null;
    }

    private void OnDestroy()
    {
        if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }

        if (Instance == this)
            Instance = null;

        if (virtualRecipe != null)
            Destroy(virtualRecipe);
    }
}
