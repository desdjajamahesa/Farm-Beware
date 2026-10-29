using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using FeaturesInteraction;
using FeaturesKitchen;
using FeaturesInventory;
using FarmBeware.Core.Runtime;

/// <summary>
/// Status state machine proses memasak di kompor.
/// </summary>
public enum CookingState
{
    Idle,
    Cooking,
    Completed,
    Cancelled
}

/// <summary>
/// Mewakili 1 slot / tungku masak di kompor (Burner).
/// </summary>
[Serializable]
public class CookingSlot
{
    public int slotIndex;
    public CookingState state = CookingState.Idle;
    public KitchenRecipe activeRecipe;
    public float remainingTime;
    public float totalDuration;
    public InventoryComponent activeInventory;
    public CookingEscrowContainer escrowContainer = new CookingEscrowContainer();
    public InventoryTransactionScope transactionScope;
    public Coroutine coroutine;
    public InventorySlot outputSlot = new InventorySlot();

    public bool IsIdle => state == CookingState.Idle && (outputSlot == null || outputSlot.IsEmpty);
    public bool IsCooking => state == CookingState.Cooking;
    public bool HasOutput => outputSlot != null && !outputSlot.IsEmpty && outputSlot.quantity > 0;
    public float Progress01 => totalDuration > 0f ? Mathf.Clamp01(1f - remainingTime / totalDuration) : 0f;
}

/// <summary>
/// Kompor memasak (Kitchen Cooking Controller):
/// Mendukung 3 slot/tungku memasak simultan (Concurrent 3-Burner Stove).
/// Berjalan mandiri di scene background bahkan saat panel UI ditutup.
/// </summary>
[DisallowMultipleComponent]
public class KitchenStove : MonoBehaviour, IInteractable
{
    #region Serialized Fields

    [Header("Resep Masak")]
    [Tooltip("Semua resep yang tersedia di kompor ini.")]
    [SerializeField] private KitchenRecipe[] recipes;

    [Header("UI Reference")]
    [Tooltip("StoveUIManager yang mengontrol Panel_Stove. Jika kosong, cari di scene.")]
    [SerializeField] private StoveUIManager stoveUI;

    [Header("Audio & Visual Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cookingLoopSound;
    [SerializeField] private AudioClip cookingCompleteSound;
    [SerializeField] private ParticleSystem cookingParticleSystem;

    [Header("Multi-Slot Burners (Max 3)")]
    [SerializeField] private CookingSlot[] cookingSlots;

    #endregion

    #region State & Transactions

    public const int MaxCookingSlots = 3;

    public CookingSlot[] CookingSlots => cookingSlots;
    public CookingSlot GetSlot(int index) => (cookingSlots != null && index >= 0 && index < cookingSlots.Length) ? cookingSlots[index] : null;
    public bool IsSlotCooking(int index) => (cookingSlots != null && index >= 0 && index < cookingSlots.Length && cookingSlots[index] != null && cookingSlots[index].IsCooking);

    public int ActiveCookingCount
    {
        get
        {
            if (cookingSlots == null) return 0;
            int count = 0;
            for (int i = 0; i < cookingSlots.Length; i++)
            {
                if (cookingSlots[i] != null && cookingSlots[i].IsCooking)
                    count++;
            }
            return count;
        }
    }

    public int AvailableSlotCount
    {
        get
        {
            if (cookingSlots == null) return 0;
            int count = 0;
            for (int i = 0; i < cookingSlots.Length; i++)
            {
                if (cookingSlots[i] != null && cookingSlots[i].IsIdle)
                    count++;
            }
            return count;
        }
    }

    public int GetNextAvailableSlotIndex()
    {
        if (cookingSlots == null) return -1;
        for (int i = 0; i < cookingSlots.Length; i++)
        {
            if (cookingSlots[i] != null && cookingSlots[i].IsIdle)
                return i;
        }
        return -1;
    }

    public bool HasAnyCookedOutput
    {
        get
        {
            if (cookingSlots == null) return false;
            for (int i = 0; i < cookingSlots.Length; i++)
            {
                if (cookingSlots[i] != null && cookingSlots[i].HasOutput)
                    return true;
            }
            return false;
        }
    }

    // ── Backward-compatible properties ──
    public CookingState CurrentState => ActiveCookingCount > 0 ? CookingState.Cooking : (HasAnyCookedOutput ? CookingState.Completed : CookingState.Idle);
    public KitchenRecipe ActiveRecipe
    {
        get
        {
            for (int i = 0; i < MaxCookingSlots; i++)
            {
                if (cookingSlots != null && i < cookingSlots.Length && cookingSlots[i] != null && cookingSlots[i].activeRecipe != null)
                    return cookingSlots[i].activeRecipe;
            }
            return null;
        }
    }
    public InventoryComponent ActiveInventory
    {
        get
        {
            for (int i = 0; i < MaxCookingSlots; i++)
            {
                if (cookingSlots != null && i < cookingSlots.Length && cookingSlots[i] != null && cookingSlots[i].activeInventory != null)
                    return cookingSlots[i].activeInventory;
            }
            return null;
        }
    }

    public KitchenRecipe[] Recipes => recipes;
    public KitchenRecipe[] availableRecipes => recipes;

    [Header("Cooked Output Slot")]
    [Tooltip("Dedicated slot holding cooked dishes until player collects them.")]
    [SerializeField] private InventorySlot cookedOutputSlot = new InventorySlot();
    public InventorySlot CookedOutputSlot => GetFirstOutputSlot() ?? cookedOutputSlot;
    public bool HasCookedOutput => HasAnyCookedOutput || (cookedOutputSlot != null && !cookedOutputSlot.IsEmpty && cookedOutputSlot.quantity > 0);

    public ItemData PendingOutputItem => _pendingOutputItem;
    public int PendingOutputCount => _pendingOutputCount;
    public bool HasPendingOutput => _pendingOutputItem != null && _pendingOutputCount > 0;

    private ItemData _pendingOutputItem;
    private int _pendingOutputCount;

    #endregion

    #region Events

    public event Action<CookingState> OnCookingStateChanged;
    public event Action<KitchenRecipe, float> OnCookingStarted;   // (recipe, duration)
    public event Action<float, float> OnCookingProgress;         // (progress01, remainingSeconds)
    public event Action<KitchenRecipe> OnCookingCompleted;       // (recipe)
    public static event Action<KitchenRecipe> OnAnyCookingCompleted; // Static decoupled event for quest/narrative systems
    public event Action<KitchenRecipe> OnCookingCancelled;       // (recipe)
    public event Action<ItemData, int> OnCookedOutputChanged;    // (item, count)
    public event Action<ItemData, int> OnPendingOutputChanged;   // (item, count)

    // Slot-specific events
    public event Action<int, KitchenRecipe, float> OnSlotCookingStarted;   // (slotIndex, recipe, duration)
    public event Action<int, float, float> OnSlotCookingProgress;         // (slotIndex, progress01, remainingSeconds)
    public event Action<int, KitchenRecipe> OnSlotCookingCompleted;       // (slotIndex, recipe)
    public event Action<int, KitchenRecipe> OnSlotCookingCancelled;       // (slotIndex, recipe)
    public event Action<int, ItemData, int> OnSlotOutputChanged;          // (slotIndex, item, count)

    #endregion

    #region Singleton Accessor

    private static KitchenStove _instance;
    public static KitchenStove Instance
    {
        get
        {
            if (_instance == null)
            {
                var found = FindObjectsByType<KitchenStove>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                    _instance = found[0];
            }
            return _instance;
        }
        private set => _instance = value;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        InitCookingSlots();

        if (cookedOutputSlot == null)
            cookedOutputSlot = new InventorySlot();

        if (stoveUI == null)
            stoveUI = StoveUIManager.Instance ?? FindFirstObjectByType<StoveUIManager>(FindObjectsInactive.Include);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void InitCookingSlots()
    {
        if (cookingSlots == null || cookingSlots.Length != MaxCookingSlots)
        {
            var old = cookingSlots;
            cookingSlots = new CookingSlot[MaxCookingSlots];
            for (int i = 0; i < MaxCookingSlots; i++)
            {
                cookingSlots[i] = (old != null && i < old.Length && old[i] != null)
                    ? old[i]
                    : new CookingSlot();
            }
        }

        for (int i = 0; i < MaxCookingSlots; i++)
        {
            if (cookingSlots[i] == null)
                cookingSlots[i] = new CookingSlot();
            cookingSlots[i].slotIndex = i;
            if (cookingSlots[i].outputSlot == null)
                cookingSlots[i].outputSlot = new InventorySlot();
            if (cookingSlots[i].escrowContainer == null)
                cookingSlots[i].escrowContainer = new CookingEscrowContainer();
        }
    }

    private void Start()
    {
        // Migrate any legacy pending outputs into cooked output slot if empty
        if (HasPendingOutput && (cookedOutputSlot == null || cookedOutputSlot.IsEmpty))
        {
            if (cookedOutputSlot == null) cookedOutputSlot = new InventorySlot();
            cookedOutputSlot.item = _pendingOutputItem;
            cookedOutputSlot.quantity = _pendingOutputCount;
            _pendingOutputItem = null;
            _pendingOutputCount = 0;
            OnCookedOutputChanged?.Invoke(cookedOutputSlot.item, cookedOutputSlot.quantity);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnDisable()
    {
        // Background cooking is preserved; do not cancel on scene deactivate
    }

    #endregion

    #region IInteractable Implementation

    public void Interact(GameObject interactor)
    {
        InventoryComponent playerInv = interactor != null ? interactor.GetComponent<InventoryComponent>() : null;

        // Try claiming legacy pending output if space allows
        if (playerInv != null && HasPendingOutput)
        {
            TryClaimPendingOutput(playerInv);
        }

        if (stoveUI == null)
            stoveUI = StoveUIManager.Instance ?? FindFirstObjectByType<StoveUIManager>(FindObjectsInactive.Include);

        if (stoveUI == null)
        {
            Debug.LogWarning("[KitchenStove] StoveUIManager not found in scene!");
            return;
        }

        if (playerInv == null)
        {
            Debug.LogWarning("[KitchenStove] Interactor does not have an InventoryComponent!");
            return;
        }

        stoveUI.Open(this, playerInv);
    }

    #endregion

    #region Validation & Transaction Logic

    public bool CanCook(KitchenRecipe recipe, InventoryComponent inventory, out string failReason)
    {
        return CanCookSlot(recipe, inventory, -1, out failReason);
    }

    public bool CanCook(KitchenRecipe recipe, InventoryComponent inventory)
    {
        return CanCookSlot(recipe, inventory, -1, out _);
    }

    public bool CanCookSlot(KitchenRecipe recipe, InventoryComponent inventory, int targetSlotIndex, out string failReason)
    {
        if (recipe == null)
        {
            failReason = "No recipe selected.";
            return false;
        }

        if (inventory == null)
        {
            failReason = "Player inventory not found.";
            return false;
        }

        InitCookingSlots();

        int slotIdx = targetSlotIndex >= 0 ? targetSlotIndex : GetNextAvailableSlotIndex();
        if (slotIdx < 0 || slotIdx >= MaxCookingSlots)
        {
            failReason = "All 3 stove burners are busy or holding food!";
            return false;
        }

        var slot = cookingSlots[slotIdx];
        if (slot.IsCooking)
        {
            failReason = $"Burner {slotIdx + 1} is currently cooking.";
            return false;
        }

        if (slot.HasOutput)
        {
            failReason = $"Burner {slotIdx + 1} holds '{slot.outputSlot.item.itemName}'! Collect it first.";
            return false;
        }

        if (recipe.output == null)
        {
            failReason = "Recipe has no output item configured.";
            return false;
        }

        // 1. Validate clean water
        if (recipe.waterRequired > 0f)
        {
            var bottle = PlayerWaterBottle.Instance;
            if (bottle == null || !bottle.HasWater(recipe.waterRequired))
            {
                float currentWater = bottle != null ? bottle.CurrentWater : 0f;
                failReason = $"Not enough clean water (requires {recipe.waterRequired:F0}L, has {Mathf.FloorToInt(currentWater)}L).";
                return false;
            }
        }

        // 2. Validate ingredients
        var aggregatedRequirements = new Dictionary<ItemData, int>();
        var allIngredients = recipe.GetAllIngredients();
        foreach (var ingredient in allIngredients)
        {
            if (ingredient == null || ingredient.item == null)
            {
                failReason = "Recipe contains missing or corrupted ingredient data.";
                return false;
            }

            if (ingredient.item.itemId == "food_bottle_water" && recipe.waterRequired > 0f)
                continue;

            if (aggregatedRequirements.ContainsKey(ingredient.item))
                aggregatedRequirements[ingredient.item] += ingredient.quantity;
            else
                aggregatedRequirements[ingredient.item] = ingredient.quantity;
        }

        foreach (var kvp in aggregatedRequirements)
        {
            int owned = inventory.CountItem(kvp.Key);
            if (owned < kvp.Value)
            {
                failReason = $"Missing ingredient: {kvp.Key.itemName} ({owned}/{kvp.Value}).";
                return false;
            }
        }

        failReason = string.Empty;
        return true;
    }

    public bool StartCooking(KitchenRecipe recipe, InventoryComponent inventory)
    {
        return StartCookingSlot(recipe, inventory, -1);
    }

    public bool StartCookingSlot(KitchenRecipe recipe, InventoryComponent inventory, int targetSlotIndex = -1)
    {
        InitCookingSlots();

        int slotIdx = targetSlotIndex >= 0 ? targetSlotIndex : GetNextAvailableSlotIndex();
        if (!CanCookSlot(recipe, inventory, slotIdx, out string failReason))
        {
            Debug.LogWarning($"[KitchenStove] Cannot start cooking on burner {slotIdx + 1}: {failReason}");
            return false;
        }

        var slot = cookingSlots[slotIdx];
        if (slot.coroutine != null)
        {
            StopCoroutine(slot.coroutine);
            slot.coroutine = null;
        }

        slot.activeRecipe = recipe;
        slot.activeInventory = inventory;
        slot.escrowContainer.Clear();
        slot.transactionScope = InventoryTransactionScope.Begin(inventory);

        // 1. Consume clean water
        if (recipe.waterRequired > 0f)
        {
            var bottle = PlayerWaterBottle.Instance;
            if (bottle == null || !bottle.ConsumeWater(recipe.waterRequired))
            {
                Debug.LogWarning("[KitchenStove] Failed to consume water from PlayerWaterBottle! Aborting.");
                slot.activeRecipe = null;
                slot.activeInventory = null;
                slot.transactionScope?.Dispose();
                slot.transactionScope = null;
                return false;
            }
            slot.escrowContainer.HoldWater(recipe.waterRequired);
        }

        // 2. Consume ingredients
        var aggregatedRequirements = new Dictionary<ItemData, int>();
        foreach (var ingredient in recipe.GetAllIngredients())
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (ingredient.item.itemId == "food_bottle_water" && recipe.waterRequired > 0f)
                continue;

            if (aggregatedRequirements.ContainsKey(ingredient.item))
                aggregatedRequirements[ingredient.item] += ingredient.quantity;
            else
                aggregatedRequirements[ingredient.item] = ingredient.quantity;
        }

        foreach (var kvp in aggregatedRequirements)
        {
            inventory.RemoveItem(kvp.Key, kvp.Value);
            slot.escrowContainer.HoldIngredient(kvp.Key, kvp.Value);
            slot.transactionScope.EscrowItem(kvp.Key, kvp.Value);
        }

        // 3. Audio & visual feedback
        PlayCookingFeedback();

        // 4. Enter Cooking state and start slot timer
        slot.state = CookingState.Cooking;
        float duration = recipe.processTime > 0f ? recipe.processTime : 1f;
        slot.remainingTime = duration;
        slot.totalDuration = duration;

        OnSlotCookingStarted?.Invoke(slotIdx, recipe, duration);
        OnCookingStarted?.Invoke(recipe, duration);
        OnCookingStateChanged?.Invoke(CurrentState);

        slot.coroutine = StartCoroutine(SlotCookingTimerRoutine(slot, recipe, inventory, duration));
        return true;
    }

    private IEnumerator SlotCookingTimerRoutine(CookingSlot slot, KitchenRecipe recipe, InventoryComponent inventory, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float timeMultiplier = FeaturesTime.DayNightTimeManager.Instance != null
                ? FeaturesTime.DayNightTimeManager.Instance.FastForwardMultiplier
                : 1f;
            elapsed += Time.deltaTime * timeMultiplier;
            slot.remainingTime = Mathf.Max(0f, duration - elapsed);
            float progress01 = Mathf.Clamp01(elapsed / duration);

            OnSlotCookingProgress?.Invoke(slot.slotIndex, progress01, slot.remainingTime);
            OnCookingProgress?.Invoke(progress01, slot.remainingTime);
            yield return null;
        }

        // Commit transaction scope and clear escrow
        if (slot.transactionScope != null)
        {
            slot.transactionScope.Commit();
            slot.transactionScope.Dispose();
            slot.transactionScope = null;
        }
        slot.escrowContainer.Clear();

        // Deposit dish into slot's output slot
        if (recipe != null && recipe.output != null)
        {
            if (slot.outputSlot == null) slot.outputSlot = new InventorySlot();
            slot.outputSlot.item = recipe.output;
            slot.outputSlot.quantity = recipe.outputCount;

            // Mirror to legacy cookedOutputSlot if empty
            if (cookedOutputSlot == null || cookedOutputSlot.IsEmpty)
            {
                if (cookedOutputSlot == null) cookedOutputSlot = new InventorySlot();
                cookedOutputSlot.item = recipe.output;
                cookedOutputSlot.quantity = recipe.outputCount;
            }

            OnSlotOutputChanged?.Invoke(slot.slotIndex, slot.outputSlot.item, slot.outputSlot.quantity);
            OnCookedOutputChanged?.Invoke(slot.outputSlot.item, slot.outputSlot.quantity);
        }

        slot.remainingTime = 0f;
        slot.coroutine = null;
        slot.state = CookingState.Completed;

        // Feedback audio
        if (ActiveCookingCount == 0)
        {
            StopCookingFeedback(playCompleteSound: true);
        }
        else if (audioSource != null && cookingCompleteSound != null)
        {
            audioSource.PlayOneShot(cookingCompleteSound);
        }

        // World floating alert
        var floatText = ServiceLocator.Resolve<IFloatingTextService>();
        if (floatText != null && recipe != null && recipe.output != null)
        {
            Vector3 pos = transform.position + Vector3.up * 1.8f;
            floatText.SpawnText(pos, $"🍲 Burner {slot.slotIndex + 1}: {recipe.output.itemName} is ready!", new Color(1f, 0.85f, 0.3f));
        }

        OnSlotCookingCompleted?.Invoke(slot.slotIndex, recipe);
        OnCookingCompleted?.Invoke(recipe);
        OnAnyCookingCompleted?.Invoke(recipe);
        OnCookingStateChanged?.Invoke(CurrentState);
    }

    public void CancelSlotCooking(int slotIndex, bool refundIngredients = true)
    {
        if (slotIndex < 0 || slotIndex >= MaxCookingSlots) return;
        var slot = cookingSlots[slotIndex];
        if (slot.state != CookingState.Cooking) return;

        if (slot.coroutine != null)
        {
            StopCoroutine(slot.coroutine);
            slot.coroutine = null;
        }

        KitchenRecipe cancelledRecipe = slot.activeRecipe;
        InventoryComponent targetInventory = slot.activeInventory;

        if (refundIngredients)
        {
            slot.escrowContainer.ReleaseHold(targetInventory, (overflowItem, overflowCount) =>
            {
                _pendingOutputItem = overflowItem;
                _pendingOutputCount += overflowCount;
                OnPendingOutputChanged?.Invoke(_pendingOutputItem, _pendingOutputCount);
                Debug.LogWarning($"[KitchenStove] Inventory full during rollback! {overflowCount}x {overflowItem.itemName} held on stove.");
            });
        }
        else
        {
            slot.escrowContainer.Clear();
        }

        if (slot.transactionScope != null)
        {
            slot.transactionScope.Commit();
            slot.transactionScope.Dispose();
            slot.transactionScope = null;
        }

        slot.state = CookingState.Idle;
        slot.remainingTime = 0f;
        slot.totalDuration = 0f;
        slot.activeRecipe = null;
        slot.activeInventory = null;

        if (ActiveCookingCount == 0)
            StopCookingFeedback();

        OnSlotCookingCancelled?.Invoke(slotIndex, cancelledRecipe);
        OnCookingCancelled?.Invoke(cancelledRecipe);
        OnCookingStateChanged?.Invoke(CurrentState);
    }

    public void CancelCooking(bool refundIngredients = true)
    {
        CancelAllCooking(refundIngredients);
    }

    public void CancelAllCooking(bool refundIngredients = true)
    {
        for (int i = 0; i < MaxCookingSlots; i++)
        {
            if (cookingSlots[i] != null && cookingSlots[i].IsCooking)
                CancelSlotCooking(i, refundIngredients);
        }
    }

    public bool CollectSlotOutput(int slotIndex, InventoryComponent targetInventory, out string failReason)
    {
        if (slotIndex < 0 || slotIndex >= MaxCookingSlots)
        {
            failReason = "Invalid slot index.";
            return false;
        }

        var slot = cookingSlots[slotIndex];
        if (targetInventory == null)
        {
            failReason = "Player inventory not available.";
            return false;
        }

        if (slot.outputSlot == null || slot.outputSlot.IsEmpty || slot.outputSlot.quantity <= 0)
        {
            failReason = "Burner has no cooked dish.";
            return false;
        }

        ItemData dish = slot.outputSlot.item;
        int countToTransfer = slot.outputSlot.quantity;

        int added = targetInventory.AddItemAmount(dish, countToTransfer);
        if (added <= 0)
        {
            failReason = $"Inventory is full! Cannot take {dish.itemName}.";
            return false;
        }

        slot.outputSlot.quantity -= added;
        if (slot.outputSlot.quantity <= 0)
        {
            slot.outputSlot.item = null;
            slot.outputSlot.quantity = 0;
            slot.state = CookingState.Idle;
        }

        if (cookedOutputSlot != null && cookedOutputSlot.item == dish)
        {
            cookedOutputSlot.quantity = Mathf.Max(0, cookedOutputSlot.quantity - added);
            if (cookedOutputSlot.quantity == 0) cookedOutputSlot.item = null;
        }

        OnSlotOutputChanged?.Invoke(slotIndex, slot.outputSlot.item, slot.outputSlot.quantity);
        OnCookedOutputChanged?.Invoke(cookedOutputSlot != null ? cookedOutputSlot.item : null, cookedOutputSlot != null ? cookedOutputSlot.quantity : 0);
        OnCookingStateChanged?.Invoke(CurrentState);

        if (slot.outputSlot.quantity > 0)
        {
            failReason = $"Collected {added}x {dish.itemName}. Inventory full, {slot.outputSlot.quantity} remaining on stove.";
            return true;
        }

        failReason = string.Empty;
        return true;
    }

    public bool CollectCookedOutput(InventoryComponent targetInventory, out string failReason)
    {
        return CollectAllOutput(targetInventory, out failReason);
    }

    public bool CollectAllOutput(InventoryComponent targetInventory, out string failReason)
    {
        bool anyCollected = false;
        failReason = string.Empty;

        for (int i = 0; i < MaxCookingSlots; i++)
        {
            if (cookingSlots[i] != null && cookingSlots[i].HasOutput)
            {
                if (CollectSlotOutput(i, targetInventory, out string err))
                    anyCollected = true;
                else
                    failReason = err;
            }
        }

        if (!anyCollected && string.IsNullOrEmpty(failReason))
            failReason = "No cooked dishes to collect on stove.";

        return anyCollected;
    }

    private InventorySlot GetFirstOutputSlot()
    {
        if (cookingSlots == null) return null;
        for (int i = 0; i < cookingSlots.Length; i++)
        {
            if (cookingSlots[i] != null && cookingSlots[i].HasOutput)
                return cookingSlots[i].outputSlot;
        }
        return null;
    }

    public bool TryClaimPendingOutput(InventoryComponent inventory)
    {
        if (!HasPendingOutput || inventory == null) return false;

        int added = inventory.AddItemAmount(_pendingOutputItem, _pendingOutputCount);
        _pendingOutputCount -= added;
        if (_pendingOutputCount <= 0)
        {
            _pendingOutputItem = null;
            _pendingOutputCount = 0;
        }

        OnPendingOutputChanged?.Invoke(_pendingOutputItem, _pendingOutputCount);
        return added > 0;
    }

    public void PlayCookingFeedback()
    {
        if (cookingParticleSystem != null && !cookingParticleSystem.isPlaying)
            cookingParticleSystem.Play();

        if (audioSource != null && cookingLoopSound != null)
        {
            audioSource.clip = cookingLoopSound;
            audioSource.loop = true;
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
    }

    public void StopCookingFeedback(bool playCompleteSound = false)
    {
        if (ActiveCookingCount > 0)
            return; // Other slots are still cooking

        if (cookingParticleSystem != null && cookingParticleSystem.isPlaying)
            cookingParticleSystem.Stop();

        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();

            if (playCompleteSound && cookingCompleteSound != null)
            {
                audioSource.PlayOneShot(cookingCompleteSound);
            }
        }
    }

    public void SetRecipes(KitchenRecipe[] newRecipes)
    {
        recipes = newRecipes;
    }

    #endregion
}
