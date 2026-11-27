using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic kitchen station backend (Sink & Stove), purely data-driven:
/// - State & timers live HERE (not in UI).
/// - Reacts to OnInventoryChanged from stationInventory (auto-starts when slot is filled).
/// - Signals events for UI listeners: OnProcessStarted / OnProcessProgress / OnProcessCompleted.
/// </summary>
public abstract class KitchenStation : MonoBehaviour, FarmBeware.Core.Runtime.ISlotProgressProvider
{
    [Tooltip("Inventori stasiun (slot tempat bahan ditaruh). Jika kosong, memakai komponen sendiri.")]
    [SerializeField] protected InventoryComponent stationInventory;

    [Tooltip("Tujuan hasil (mis. Inventory Player). Kosong = hasil tetap di slot stasiun.")]
    [SerializeField] protected InventoryComponent resultTarget;

    // Per-slot processing state.
    private readonly Dictionary<int, KitchenRecipe> activeRecipes = new Dictionary<int, KitchenRecipe>();
    private readonly Dictionary<int, float> remainingTime = new Dictionary<int, float>();
    private readonly Dictionary<int, float> totalDuration = new Dictionary<int, float>();
    private readonly Dictionary<int, int> processQuantity = new Dictionary<int, int>();

    public event System.Action<int, float> OnProcessStarted;   // (slot, duration)
    public event System.Action<int, float> OnProcessProgress;  // (slot, 0..1)
    public event System.Action<int> OnProcessCompleted;        // (slot)
    public event System.Action<int> OnProcessCancelled;        // (slot) — process cancelled (item removed mid-process)

    protected void RaiseProcessStarted(int slot, float duration) => OnProcessStarted?.Invoke(slot, duration);
    protected void RaiseProcessProgress(int slot, float progress01) => OnProcessProgress?.Invoke(slot, progress01);
    protected void RaiseProcessCompleted(int slot) => OnProcessCompleted?.Invoke(slot);
    protected void RaiseProcessCancelled(int slot) => OnProcessCancelled?.Invoke(slot);

    public InventoryComponent StationInventory { get { return stationInventory; } }

    // Read-only state access for data-driven visuals (e.g., overlay polling),
    // independent of events so UI does not stall when processing started before subscribing.
    public int ActiveSlotCount { get { return activeRecipes.Count; } }
    public virtual bool IsProcessing(int slot) { return activeRecipes.ContainsKey(slot); }
    public virtual float GetSlotProgress(int slot)
    {
        if (!remainingTime.TryGetValue(slot, out float remaining))
            return 0f;
        if (!totalDuration.TryGetValue(slot, out float total) || total <= 0f)
            return 0f;
        return Mathf.Clamp01(1f - remaining / total);
    }

    protected abstract KitchenRecipe FindRecipeFor(ItemData item, int slotIndex);

    protected virtual void Awake()
    {
        if (stationInventory == null)
            stationInventory = GetComponent<InventoryComponent>();
    }

    protected virtual void OnEnable()
    {
        if (stationInventory != null)
        {
            stationInventory.OnInventoryChanged += HandleInventoryChanged;
            // Auto-process items ALREADY in slots when the station activates
            // (e.g., leftover items from a previous session / seeded in editor) — idempotent.
            HandleInventoryChanged();
        }
    }

    protected virtual void OnDisable()
    {
        if (stationInventory != null)
            stationInventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (stationInventory == null || stationInventory.slots == null)
            return;

        for (int i = 0; i < stationInventory.slots.Count; i++)
        {
            InventorySlot slot = stationInventory.slots[i];
            if (slot == null)
                continue;

            bool processing = activeRecipes.ContainsKey(i);

            if (slot.IsEmpty || slot.item == null)
            {
                if (processing)
                    CancelProcessing(i);
                continue;
            }

            KitchenRecipe recipe = FindRecipeFor(slot.item, i);
            if (!processing && recipe != null)
                StartProcessing(i, recipe);
            else if (processing && (recipe == null || recipe != activeRecipes[i]))
                CancelProcessing(i);
        }
    }

    private void StartProcessing(int slotIndex, KitchenRecipe recipe)
    {
        activeRecipes[slotIndex] = recipe;
        remainingTime[slotIndex] = recipe.processTime;
        totalDuration[slotIndex] = recipe.processTime;

        // Store item quantity in slot when processing starts (batch processing).
        InventorySlot slot = SafeSlot(slotIndex);
        if (slot != null && !slot.IsEmpty)
            processQuantity[slotIndex] = slot.quantity;
        else
            processQuantity[slotIndex] = 1;

        OnProcessStarted?.Invoke(slotIndex, recipe.processTime);
        OnProcessProgress?.Invoke(slotIndex, 0f);
    }

    private void CancelProcessing(int slotIndex)
    {
        activeRecipes.Remove(slotIndex);
        remainingTime.Remove(slotIndex);
        totalDuration.Remove(slotIndex);
        processQuantity.Remove(slotIndex);

        OnProcessCancelled?.Invoke(slotIndex);
    }

    protected virtual void Update()
    {
        if (activeRecipes.Count == 0 || stationInventory == null)
            return;

        float timeMultiplier = FeaturesTime.DayNightTimeManager.Instance != null
            ? FeaturesTime.DayNightTimeManager.Instance.FastForwardMultiplier
            : 1f;
        float dt = Mathf.Min(Time.deltaTime * timeMultiplier, 1f * timeMultiplier);

        List<int> indices = new List<int>(remainingTime.Keys);
        for (int k = 0; k < indices.Count; k++)
        {
            int i = indices[k];
            if (!activeRecipes.ContainsKey(i) || !remainingTime.TryGetValue(i, out float remaining) ||
                !totalDuration.TryGetValue(i, out float total) || total <= 0f)
                continue;

            // If slot changes/empties while processing -> cancel process.
            InventorySlot slot = SafeSlot(i);
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                CancelProcessing(i);
                continue;
            }

            remainingTime[i] -= dt;
            OnProcessProgress?.Invoke(i, Mathf.Clamp01(1f - remainingTime[i] / total));

            if (remainingTime[i] <= 0f)
                CompleteProcessing(i);
        }
    }

    private void CompleteProcessing(int slotIndex)
    {
        if (!activeRecipes.TryGetValue(slotIndex, out KitchenRecipe recipe))
            return;

        // Get processed quantity (batch).
        int qty = 1;
        if (processQuantity.TryGetValue(slotIndex, out int savedQty) && savedQty > 0)
            qty = savedQty;

        // Clear state FIRST so internal handlers do not restart the process.
        activeRecipes.Remove(slotIndex);
        remainingTime.Remove(slotIndex);
        totalDuration.Remove(slotIndex);
        processQuantity.Remove(slotIndex);

        // Consume ingredients from slot (batch quantity).
        InventorySlot slot = SafeSlot(slotIndex);
        if (slot != null && !slot.IsEmpty && slot.item == recipe.input)
            stationInventory.RemoveFromSlot(slotIndex, qty);

        // Output: auto-return to resultTarget, or place directly in station slot (batch output).
        bool stored = resultTarget != null && resultTarget.AddItem(recipe.output, qty * recipe.outputCount);
        if (!stored)
            stationInventory.ReplaceItemAt(slotIndex, recipe.output, qty * recipe.outputCount);

        OnProcessCompleted?.Invoke(slotIndex);
    }

    private InventorySlot SafeSlot(int index)
    {
        if (stationInventory == null || stationInventory.slots == null)
            return null;
        return (index >= 0 && index < stationInventory.slots.Count) ? stationInventory.slots[index] : null;
    }
}