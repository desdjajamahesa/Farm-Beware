using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Backend generik stasiun dapur (Sink & Kompor), murni data-driven:
/// - State & timer hidup DI SINI (bukan di UI).
/// - Bereaksi pada OnInventoryChanged dari stationInventory (auto-mulai saat slot terisi).
/// - Sinyal event untuk listener UI: OnProcessStarted / OnProcessProgress / OnProcessCompleted.
/// </summary>
public abstract class KitchenStation : MonoBehaviour
{
    [Tooltip("Inventori stasiun (slot tempat bahan ditaruh). Jika kosong, memakai komponen sendiri.")]
    [SerializeField] protected InventoryComponent stationInventory;

    [Tooltip("Tujuan hasil (mis. Inventory Player). Kosong = hasil tetap di slot stasiun.")]
    [SerializeField] protected InventoryComponent resultTarget;

    // State processing per-slot.
    private readonly Dictionary<int, KitchenRecipe> activeRecipes = new Dictionary<int, KitchenRecipe>();
    private readonly Dictionary<int, float> remainingTime = new Dictionary<int, float>();
    private readonly Dictionary<int, float> totalDuration = new Dictionary<int, float>();

    public event System.Action<int, float> OnProcessStarted;   // (slot, durasi)
    public event System.Action<int, float> OnProcessProgress;  // (slot, 0..1)
    public event System.Action<int> OnProcessCompleted;        // (slot)

    public InventoryComponent StationInventory { get { return stationInventory; } }

    protected abstract KitchenRecipe FindRecipeFor(ItemData item, int slotIndex);

    protected virtual void Awake()
    {
        if (stationInventory == null)
            stationInventory = GetComponent<InventoryComponent>();
    }

    protected virtual void OnEnable()
    {
        if (stationInventory != null)
            stationInventory.OnInventoryChanged += HandleInventoryChanged;
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

        OnProcessStarted?.Invoke(slotIndex, recipe.processTime);
        OnProcessProgress?.Invoke(slotIndex, 0f);
    }

    private void CancelProcessing(int slotIndex)
    {
        activeRecipes.Remove(slotIndex);
        remainingTime.Remove(slotIndex);
        totalDuration.Remove(slotIndex);
    }

    protected virtual void Update()
    {
        if (activeRecipes.Count == 0 || stationInventory == null)
            return;

        List<int> indices = new List<int>(remainingTime.Keys);
        for (int k = 0; k < indices.Count; k++)
        {
            int i = indices[k];
            if (!activeRecipes.ContainsKey(i) || !remainingTime.TryGetValue(i, out float remaining) ||
                !totalDuration.TryGetValue(i, out float total) || total <= 0f)
                continue;

            // Bila slot berubah/kosong saat proses berjalan -> batal proses.
            InventorySlot slot = SafeSlot(i);
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                CancelProcessing(i);
                continue;
            }

            remainingTime[i] -= Time.deltaTime;
            OnProcessProgress?.Invoke(i, Mathf.Clamp01(1f - remainingTime[i] / total));

            if (remainingTime[i] <= 0f)
                CompleteProcessing(i);
        }
    }

    private void CompleteProcessing(int slotIndex)
    {
        if (!activeRecipes.TryGetValue(slotIndex, out KitchenRecipe recipe))
            return;

        // Kosongkan state DULU agar handler internal tidak mengulang proses.
        activeRecipes.Remove(slotIndex);
        remainingTime.Remove(slotIndex);
        totalDuration.Remove(slotIndex);

        // Konsumsi bahan dari slot.
        InventorySlot slot = SafeSlot(slotIndex);
        if (slot != null && !slot.IsEmpty && slot.item == recipe.input)
            stationInventory.RemoveFromSlot(slotIndex, 1);

        // Hasil: auto-kembali ke resultTarget, atau langsung di slot stasiun.
        bool stored = resultTarget != null && resultTarget.AddItem(recipe.output, recipe.outputCount);
        if (!stored)
            stationInventory.ReplaceItemAt(slotIndex, recipe.output, recipe.outputCount);

        OnProcessCompleted?.Invoke(slotIndex);
    }

    private InventorySlot SafeSlot(int index)
    {
        if (stationInventory == null || stationInventory.slots == null)
            return null;
        return (index >= 0 && index < stationInventory.slots.Count) ? stationInventory.slots[index] : null;
    }
}