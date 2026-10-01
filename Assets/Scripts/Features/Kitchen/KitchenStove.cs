using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FeaturesInteraction;
using FeaturesKitchen;

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
/// Kompor memasak (Kitchen Cooking Controller):
/// Bertindak sebagai backend controller mandiri yang mengelola:
/// - State machine siklus memasak (Idle -> Cooking -> Completed / Cancelled).
/// - Pemancaran event C# publik (OnCookingStateChanged, OnCookingStarted, OnCookingProgress, dll).
/// - Validasi bahan & kapasitas inventaris (mencegah masakan hilang/void saat tas penuh).
/// - Transaksi inventaris aman dengan snapshot, atomic water consumption, dan rollback safety net.
/// - Pending output recovery (menampung masakan jika inventaris mendadak penuh).
/// </summary>
[DisallowMultipleComponent]
public class KitchenStove : MonoBehaviour, IInteractable
{
    #region Sub-types

    [Serializable]
    private struct ConsumedIngredientSnapshot
    {
        public ItemData item;
        public int quantity;
    }

    #endregion

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

    #endregion

    #region State & Transactions

    public CookingState CurrentState { get; private set; } = CookingState.Idle;
    public KitchenRecipe ActiveRecipe => _activeRecipe;
    public InventoryComponent ActiveInventory => _activeInventory;
    public KitchenRecipe[] Recipes => recipes;
    public KitchenRecipe[] availableRecipes => recipes;

    [Header("Cooked Output Slot")]
    [Tooltip("Dedicated slot holding cooked dishes until player collects them.")]
    [SerializeField] private InventorySlot cookedOutputSlot = new InventorySlot();
    public InventorySlot CookedOutputSlot => cookedOutputSlot;
    public bool HasCookedOutput => cookedOutputSlot != null && !cookedOutputSlot.IsEmpty && cookedOutputSlot.quantity > 0;

    public ItemData PendingOutputItem => _pendingOutputItem;
    public int PendingOutputCount => _pendingOutputCount;
    public bool HasPendingOutput => _pendingOutputItem != null && _pendingOutputCount > 0;

    private KitchenRecipe _activeRecipe;
    private InventoryComponent _activeInventory;
    private readonly List<ConsumedIngredientSnapshot> _consumedSnapshots = new List<ConsumedIngredientSnapshot>();
    private float _consumedWater = 0f;
    private Coroutine _cookingCoroutine;

    private ItemData _pendingOutputItem;
    private int _pendingOutputCount;

    #endregion

    #region Events

    public event Action<CookingState> OnCookingStateChanged;
    public event Action<KitchenRecipe, float> OnCookingStarted;   // (recipe, duration)
    public event Action<float, float> OnCookingProgress;         // (progress01, remainingSeconds)
    public event Action<KitchenRecipe> OnCookingCompleted;       // (recipe)
    public event Action<KitchenRecipe> OnCookingCancelled;       // (recipe)
    public event Action<ItemData, int> OnCookedOutputChanged;    // (item, count)
    public event Action<ItemData, int> OnPendingOutputChanged;   // (item, count)

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

        if (cookedOutputSlot == null)
            cookedOutputSlot = new InventorySlot();

        if (stoveUI == null)
            stoveUI = StoveUIManager.Instance ?? FindFirstObjectByType<StoveUIManager>(FindObjectsInactive.Include);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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
        if (CurrentState == CookingState.Cooking)
        {
            CancelCooking(refundIngredients: true);
        }
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

    /// <summary>
    /// Checks recipe ingredient availability in inventory, clean water availability,
    /// and ensures stove cooked output slot has space for the dish.
    /// Returns false with descriptive failReason if cooking cannot start.
    /// </summary>
    public bool CanCook(KitchenRecipe recipe, InventoryComponent inventory, out string failReason)
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

        if (CurrentState == CookingState.Cooking)
        {
            failReason = "Stove is currently busy cooking.";
            return false;
        }

        if (recipe.output == null)
        {
            failReason = "Recipe has no output item configured.";
            return false;
        }

        // Validate stove cooked output slot space
        if (cookedOutputSlot != null && !cookedOutputSlot.IsEmpty)
        {
            if (cookedOutputSlot.item != recipe.output)
            {
                failReason = $"Output slot holds '{cookedOutputSlot.item.itemName}'! Collect it first.";
                return false;
            }

            int maxStack = recipe.output.maxStack > 0 ? recipe.output.maxStack : 1;
            if (cookedOutputSlot.quantity + recipe.outputCount > maxStack)
            {
                failReason = $"Stove output is full ({cookedOutputSlot.quantity}/{maxStack})! Collect cooked dish first.";
                return false;
            }
        }

        // 1. Validate clean water availability
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

        // 2. Aggregate and validate raw ingredients
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

    /// <summary>
    /// Practical CanCook overload without out parameter.
    /// </summary>
    public bool CanCook(KitchenRecipe recipe, InventoryComponent inventory)
    {
        return CanCook(recipe, inventory, out _);
    }

    /// <summary>
    /// Starts the cooking process:
    /// 1. Validates ingredients, water, and stove output capacity.
    /// 2. Atomically consumes water from PlayerWaterBottle.
    /// 3. Records ingredient snapshot and removes items from inventory.
    /// 4. Plays audio/visual feedback, enters Cooking state, and starts timer.
    /// </summary>
    public bool StartCooking(KitchenRecipe recipe, InventoryComponent inventory)
    {
        if (!CanCook(recipe, inventory, out string failReason))
        {
            Debug.LogWarning($"[KitchenStove] Cannot start cooking: {failReason}");
            return false;
        }

        if (_cookingCoroutine != null)
        {
            StopCoroutine(_cookingCoroutine);
            _cookingCoroutine = null;
        }

        _activeRecipe = recipe;
        _activeInventory = inventory;
        _consumedSnapshots.Clear();
        _consumedWater = 0f;

        // 1. Consume water atomically
        if (recipe.waterRequired > 0f)
        {
            var bottle = PlayerWaterBottle.Instance;
            if (bottle == null || !bottle.ConsumeWater(recipe.waterRequired))
            {
                Debug.LogWarning("[KitchenStove] Failed to consume water from PlayerWaterBottle! Aborting.");
                _activeRecipe = null;
                _activeInventory = null;
                return false;
            }
            _consumedWater = recipe.waterRequired;
        }

        // 2. Aggregate and consume raw ingredients
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
            _consumedSnapshots.Add(new ConsumedIngredientSnapshot
            {
                item = kvp.Key,
                quantity = kvp.Value
            });
        }

        // 3. Audio & visual feedback
        PlayCookingFeedback();

        // 4. Enter Cooking state and start timer
        SetState(CookingState.Cooking);

        float duration = recipe.processTime > 0f ? recipe.processTime : 1f;
        OnCookingStarted?.Invoke(recipe, duration);

        _cookingCoroutine = StartCoroutine(CookingTimerRoutine(recipe, inventory, duration));
        return true;
    }

    /// <summary>
    /// Cancels active cooking. If refundIngredients == true,
    /// returns all consumed raw ingredients and water to player.
    /// </summary>
    public void CancelCooking(bool refundIngredients = true)
    {
        if (CurrentState != CookingState.Cooking)
            return;

        if (_cookingCoroutine != null)
        {
            StopCoroutine(_cookingCoroutine);
            _cookingCoroutine = null;
        }

        StopCookingFeedback();

        KitchenRecipe cancelledRecipe = _activeRecipe;
        InventoryComponent targetInventory = _activeInventory;

        if (refundIngredients)
        {
            // Refund water
            if (_consumedWater > 0f && PlayerWaterBottle.Instance != null)
            {
                PlayerWaterBottle.Instance.RefillWater(_consumedWater);
            }

            // Refund ingredients to player inventory
            if (targetInventory != null)
            {
                foreach (var snap in _consumedSnapshots)
                {
                    if (snap.item != null && snap.quantity > 0)
                    {
                        int added = targetInventory.AddItemAmount(snap.item, snap.quantity);
                        int leftover = snap.quantity - added;
                        if (leftover > 0)
                        {
                            _pendingOutputItem = snap.item;
                            _pendingOutputCount += leftover;
                            OnPendingOutputChanged?.Invoke(_pendingOutputItem, _pendingOutputCount);
                            Debug.LogWarning($"[KitchenStove] Inventory full during rollback! {leftover}x {snap.item.itemName} held on stove.");
                        }
                    }
                }
            }
        }

        _consumedSnapshots.Clear();
        _consumedWater = 0f;
        _activeRecipe = null;
        _activeInventory = null;

        SetState(CookingState.Cancelled);
        OnCookingCancelled?.Invoke(cancelledRecipe);
        SetState(CookingState.Idle);
    }

    public IEnumerator CookingTimerRoutine(KitchenRecipe recipe, InventoryComponent inventory, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, duration - elapsed);
            float progress01 = Mathf.Clamp01(elapsed / duration);

            OnCookingProgress?.Invoke(progress01, remaining);
            yield return null;
        }

        StopCookingFeedback(playCompleteSound: true);

        // Clear active transaction snapshots
        _consumedSnapshots.Clear();
        _consumedWater = 0f;

        // Place cooked dish directly into stove's cookedOutputSlot (keeps dish on stove)
        if (recipe != null && recipe.output != null)
        {
            if (cookedOutputSlot == null)
                cookedOutputSlot = new InventorySlot();

            if (cookedOutputSlot.IsEmpty)
            {
                cookedOutputSlot.item = recipe.output;
                cookedOutputSlot.quantity = recipe.outputCount;
            }
            else if (cookedOutputSlot.item == recipe.output)
            {
                cookedOutputSlot.quantity += recipe.outputCount;
            }
            else
            {
                // Fallback: deposit into pending output
                _pendingOutputItem = recipe.output;
                _pendingOutputCount += recipe.outputCount;
                OnPendingOutputChanged?.Invoke(_pendingOutputItem, _pendingOutputCount);
            }

            OnCookedOutputChanged?.Invoke(cookedOutputSlot.item, cookedOutputSlot.quantity);
        }

        _cookingCoroutine = null;
        _activeRecipe = null;
        _activeInventory = null;

        SetState(CookingState.Completed);
        OnCookingCompleted?.Invoke(recipe);
        SetState(CookingState.Idle);
    }

    /// <summary>
    /// Collects cooked dishes from the stove's output slot into the player's inventory.
    /// </summary>
    public bool CollectCookedOutput(InventoryComponent targetInventory, out string failReason)
    {
        if (targetInventory == null)
        {
            failReason = "Player inventory not available.";
            return false;
        }

        if (cookedOutputSlot == null || cookedOutputSlot.IsEmpty || cookedOutputSlot.quantity <= 0)
        {
            failReason = "Output slot is empty.";
            return false;
        }

        ItemData dish = cookedOutputSlot.item;
        int countToTransfer = cookedOutputSlot.quantity;

        int added = targetInventory.AddItemAmount(dish, countToTransfer);
        if (added <= 0)
        {
            failReason = $"Inventory is full! Cannot take {dish.itemName}.";
            return false;
        }

        cookedOutputSlot.quantity -= added;
        if (cookedOutputSlot.quantity <= 0)
        {
            cookedOutputSlot.item = null;
            cookedOutputSlot.quantity = 0;
        }

        OnCookedOutputChanged?.Invoke(cookedOutputSlot.item, cookedOutputSlot.quantity);

        if (cookedOutputSlot.quantity > 0)
        {
            failReason = $"Collected {added}x {dish.itemName}. Inventory full, {cookedOutputSlot.quantity} remaining on stove.";
            return true;
        }

        failReason = string.Empty;
        return true;
    }

    /// <summary>
    /// Claims leftover dishes or rollback items held on the stove.
    /// </summary>
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

    private void PlayCookingFeedback()
    {
        if (cookingParticleSystem != null && !cookingParticleSystem.isPlaying)
            cookingParticleSystem.Play();

        if (audioSource != null && cookingLoopSound != null)
        {
            audioSource.clip = cookingLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void StopCookingFeedback(bool playCompleteSound = false)
    {
        if (cookingParticleSystem != null && cookingParticleSystem.isPlaying)
            cookingParticleSystem.Stop();

        if (audioSource != null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            if (playCompleteSound && cookingCompleteSound != null)
            {
                audioSource.PlayOneShot(cookingCompleteSound);
            }
        }
    }

    private void SetState(CookingState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnCookingStateChanged?.Invoke(newState);
    }

    public void SetRecipes(KitchenRecipe[] newRecipes)
    {
        recipes = newRecipes;
    }

    #endregion
}
