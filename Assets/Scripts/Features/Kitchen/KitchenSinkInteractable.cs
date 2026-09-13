using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FeaturesInteraction;

/// <summary>
/// Kitchen Sink: owns washing processor logic and slots.
/// Runs Update() independently of panel visibility (on Kitchen_Sink, always active).
/// SinkManager reads from this via public accessors for UI display.
/// </summary>
[RequireComponent(typeof(InventoryComponent))]
public class KitchenSinkInteractable : KitchenStation, IInteractable
{
    [Header("Wastafel Settings")]
    [Tooltip("Durasi cucian default (detik per item).")]
    [SerializeField] private float washDurationPerItem = 2f;

    [Tooltip("Kategori makanan yang boleh dicuci di wastafel.")]
    [SerializeField] private List<ItemData.FoodCategory> allowedCategories =
        new List<ItemData.FoodCategory>
        {
            ItemData.FoodCategory.Vegetable,
            ItemData.FoodCategory.Fruit,
        };

    [Tooltip("Bila true, hasil cuci otomatis dikembalikan ke Inventory Player.")]
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
    public InventorySlot InputSlot => inputSlot;
    public InventorySlot OutputSlot => outputSlot;
    public bool IsWashing => isWashing;
    public float WashProgress => washProgress;
    public float WashDurationPerItem => washDurationPerItem;

    protected override void Awake()
    {
        base.Awake();

        // Initialize slots
        inputSlot = new InventorySlot();
        outputSlot = new InventorySlot();

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
            virtualRecipe.input = item;
            virtualRecipe.output = food.cleanVariant;
            virtualRecipe.processTime = washDurationPerItem;
            virtualRecipe.outputCount = 1;
            return virtualRecipe;
        }

        if (item is MaterialItemData mat && mat.isDirty && mat.cleanVariant != null)
        {
            virtualRecipe.input = item;
            virtualRecipe.output = mat.cleanVariant;
            virtualRecipe.processTime = washDurationPerItem;
            virtualRecipe.outputCount = 1;
            return virtualRecipe;
        }

        return null;
    }

    public void Interact(GameObject interactor)
    {
        if (panelSink == null)
        {
            Debug.LogWarning("[KitchenSinkInteractable] Panel_Sink tidak ditemukan!");
            return;
        }

        panelSink.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var playerControl = interactor.GetComponent<PlayerControl>();
        if (playerControl != null)
            playerControl.isInputLocked = true;

        // Sync UI to current processor state
        var sinkMgr = panelSink.GetComponent<SinkManager>();
        if (sinkMgr != null)
            sinkMgr.SyncToProcessor();
    }

    protected override void Update()
    {
        base.Update();

        if (panelSink != null && panelSink.activeSelf &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
            return;
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

    public void ClosePanel()
    {
        if (panelSink != null)
        {
            var sinkMgr = panelSink.GetComponent<SinkManager>();
            if (sinkMgr != null)
                sinkMgr.ClosePanel();
        }

        if (panelSink != null)
            panelSink.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        var player = FindFirstObjectByType<PlayerControl>();
        if (player != null)
            player.isInputLocked = false;
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
        if (virtualRecipe != null)
            Destroy(virtualRecipe);
    }
}
