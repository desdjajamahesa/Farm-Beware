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
/// Kompor memasak (Genshin-style Cooking Controller):
/// Bertindak sebagai backend controller mandiri yang mengelola:
/// - State machine siklus memasak (Idle -> Cooking -> Completed / Cancelled).
/// - Pemancaran event C# publik (OnCookingStateChanged, OnCookingStarted, OnCookingProgress, dll).
/// - Validasi bahan & transaksi inventaris aman (dengan snapshot & rollback jika dibatalkan).
/// - Menghilangkan tight coupling dengan UI; UI bertindak murni sebagai View/Presenter.
/// </summary>
[DisallowMultipleComponent]
public class GenshinStove : MonoBehaviour, IInteractable
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

    #endregion

    #region State & Transactions

    public CookingState CurrentState { get; private set; } = CookingState.Idle;
    public KitchenRecipe ActiveRecipe => _activeRecipe;
    public InventoryComponent ActiveInventory => _activeInventory;
    public KitchenRecipe[] Recipes => recipes;
    public KitchenRecipe[] availableRecipes => recipes;

    private KitchenRecipe _activeRecipe;
    private InventoryComponent _activeInventory;
    private readonly List<ConsumedIngredientSnapshot> _consumedSnapshots = new List<ConsumedIngredientSnapshot>();
    private float _consumedWater = 0f;
    private Coroutine _cookingCoroutine;

    #endregion

    #region Events

    public event Action<CookingState> OnCookingStateChanged;
    public event Action<KitchenRecipe, float> OnCookingStarted;   // (recipe, duration)
    public event Action<float, float> OnCookingProgress;         // (progress01, remainingSeconds)
    public event Action<KitchenRecipe> OnCookingCompleted;       // (recipe)
    public event Action<KitchenRecipe> OnCookingCancelled;       // (recipe)

    #endregion

    #region Singleton Accessor

    private static GenshinStove _instance;
    public static GenshinStove Instance
    {
        get
        {
            if (_instance == null)
            {
                var found = FindObjectsByType<GenshinStove>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        if (stoveUI == null)
            stoveUI = StoveUIManager.Instance ?? FindFirstObjectByType<StoveUIManager>(FindObjectsInactive.Include);
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
        if (stoveUI == null)
            stoveUI = StoveUIManager.Instance ?? FindFirstObjectByType<StoveUIManager>(FindObjectsInactive.Include);

        if (stoveUI == null)
        {
            Debug.LogWarning("[GenshinStove] StoveUIManager tidak ditemukan!");
            return;
        }

        // Dapatkan inventory pemain
        InventoryComponent playerInv = interactor.GetComponent<InventoryComponent>();
        if (playerInv == null)
        {
            Debug.LogWarning("[GenshinStove] Player tidak punya InventoryComponent!");
            return;
        }

        stoveUI.Open(this, playerInv);
    }

    #endregion

    #region Validation & Transaction Logic

    /// <summary>
    /// Memeriksa ketersediaan seluruh bahan di inventaris dan kecukupan air di PlayerWaterBottle.
    /// Mengembalikan failReason deskriptif jika gagal.
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

        // 1. Validasi ketersediaan air bersih
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

        // 2. Validasi bahan mentah
        var ingredients = recipe.GetAllIngredients();
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null) continue;

            // Jika botol air biasa tapi resep memakai sistem liter air
            if (ingredient.item.itemId == "food_bottle_water" && recipe.waterRequired > 0f)
                continue;

            int owned = inventory.CountItem(ingredient.item);
            if (owned < ingredient.quantity)
            {
                failReason = $"Missing ingredient: {ingredient.item.itemName} ({owned}/{ingredient.quantity}).";
                return false;
            }
        }

        failReason = string.Empty;
        return true;
    }

    /// <summary>
    /// Overload praktis CanCook tanpa parameter out.
    /// </summary>
    public bool CanCook(KitchenRecipe recipe, InventoryComponent inventory)
    {
        return CanCook(recipe, inventory, out _);
    }

    /// <summary>
    /// Memulai proses memasak:
    /// 1. Memvalidasi bahan.
    /// 2. Mencatat snapshot transaksi.
    /// 3. Memotong bahan dan air.
    /// 4. Memasuki state Cooking dan menjalankan timer.
    /// </summary>
    public bool StartCooking(KitchenRecipe recipe, InventoryComponent inventory)
    {
        if (!CanCook(recipe, inventory, out string failReason))
        {
            Debug.LogWarning($"[GenshinStove] Gagal memulai memasak: {failReason}");
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

        // 1. Konsumsi air dan catat snapshot
        if (recipe.waterRequired > 0f && PlayerWaterBottle.Instance != null)
        {
            if (PlayerWaterBottle.Instance.ConsumeWater(recipe.waterRequired))
            {
                _consumedWater = recipe.waterRequired;
            }
        }

        // 2. Konsumsi bahan dan catat snapshot
        var ingredients = recipe.GetAllIngredients();
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null) continue;
            if (ingredient.item.itemId == "food_bottle_water" && recipe.waterRequired > 0f)
                continue;

            inventory.RemoveItem(ingredient.item, ingredient.quantity);
            _consumedSnapshots.Add(new ConsumedIngredientSnapshot
            {
                item = ingredient.item,
                quantity = ingredient.quantity
            });
        }

        // 3. Masuki state Cooking dan jalankan timer
        SetState(CookingState.Cooking);

        float duration = recipe.processTime > 0f ? recipe.processTime : 1f;
        OnCookingStarted?.Invoke(recipe, duration);

        _cookingCoroutine = StartCoroutine(CookingTimerRoutine(recipe, inventory, duration));
        return true;
    }

    /// <summary>
    /// Membatalkan proses memasak. Jika refundIngredients == true,
    /// seluruh bahan mentah dan air yang telah dikonsumsi akan dikembalikan ke pemain.
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

        KitchenRecipe cancelledRecipe = _activeRecipe;
        InventoryComponent targetInventory = _activeInventory;

        if (refundIngredients)
        {
            // Kembalikan air
            if (_consumedWater > 0f && PlayerWaterBottle.Instance != null)
            {
                PlayerWaterBottle.Instance.RefillWater(_consumedWater);
            }

            // Kembalikan bahan mentah ke inventory
            if (targetInventory != null)
            {
                foreach (var snap in _consumedSnapshots)
                {
                    if (snap.item != null && snap.quantity > 0)
                    {
                        targetInventory.AddItem(snap.item, snap.quantity);
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

        // Bersihkan data snapshot transaksi aktif
        _consumedSnapshots.Clear();
        _consumedWater = 0f;

        // Tambahkan hasil masakan ke inventory pemain
        if (inventory != null && recipe.output != null)
        {
            inventory.AddItem(recipe.output, recipe.outputCount);
        }

        _cookingCoroutine = null;
        _activeRecipe = null;
        _activeInventory = null;

        SetState(CookingState.Completed);
        OnCookingCompleted?.Invoke(recipe);
        SetState(CookingState.Idle);
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
