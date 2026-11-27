using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;

public class InventoryComponent : MonoBehaviour
{
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int maxCapacity;

    // BLOCK TROPHY: when true (e.g., Player Inventory), Trophy-type items can NEVER
    // enter this inventory from any path (AddItem/MoveItemToSlot/TransferItemTo).
    [Tooltip("Tidak mengizinkan item tipe Trophy masuk ke inventory ini (Contoh: inventory Player).")]
    [SerializeField] private bool blockTrophyItems;

    [Tooltip("Jika true, 4 slot pertama adalah hotbar. Penambahan item akan memprioritaskan slot inventori utama (indeks >= 4) sebelum hotbar (indeks 0..3).")]
    [SerializeField] private bool hasHotbar = false;

    public bool HasHotbar { get => hasHotbar; set => hasHotbar = value; }

    private void Awake()
    {
        // Auto-detect player inventory so hasHotbar is enabled automatically with 16-slot capacity
        if (CompareTag("Player") || GetComponent<IPlayerContext>() != null || blockTrophyItems)
        {
            hasHotbar = true;
            if (maxCapacity < 16)
            {
                maxCapacity = 16;
            }
        }

        if (slots == null || slots.Count == 0)
        {
            ResetInventory(maxCapacity);
        }
        else
        {
            EnsureSlotsAllocated();
            // Ensure items already set in the Inspector do not have quantity <= 0
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].item != null && slots[i].quantity <= 0)
                {
                    slots[i].quantity = 1;
                }
            }
        }
    }

    // Food category filter for allowed items (empty = unrestricted).
    // Example: Refrigerator = [Vegetable, Fruit]; Sink = washable items only.
    [Tooltip("Pembatas kategori makanan (FoodCategory). Kosong = semua boleh masuk.")]
    [SerializeField] private List<ItemData.FoodCategory> allowedFoodCategories;

    // Public read access for other backend rules (e.g., Trophy Cabinet flow / kitchen stations).
    public bool BlockTrophyItems => blockTrophyItems;
    public void SetBlockTrophyItems(bool block) => blockTrophyItems = block;
    public IReadOnlyList<ItemData.FoodCategory> AllowedFoodCategories => allowedFoodCategories;

    public void SetAllowedFoodCategories(IEnumerable<ItemData.FoodCategory> categories)
    {
        if (allowedFoodCategories == null)
            allowedFoodCategories = new List<ItemData.FoodCategory>();
        else
            allowedFoodCategories.Clear();

        if (categories != null)
            allowedFoodCategories.AddRange(categories);
    }

    [SerializeField] private bool allowAllCropsAndFood = false;
    public bool AllowAllCropsAndFood { get => allowAllCropsAndFood; set => allowAllCropsAndFood = value; }
    public void SetAllowAllCropsAndFood(bool allow) => allowAllCropsAndFood = allow;

    /// <summary>
    /// Single backend rule: determines whether an item is allowed to ENTER this inventory.
    /// Combines blockTrophyItems + food category restrictions.
    /// </summary>
    public bool CanAcceptItem(ItemData item)
    {
        if (item == null)
            return false;

        // Trophies cannot enter blocking inventories (e.g., Player).
        if (blockTrophyItems && item.type == ItemData.ItemType.Trophy)
            return false;

        // Refrigerator / All Crops & Food Storage rule: can store all crops, food, seeds, plants, and cooking ingredients
        if (allowAllCropsAndFood)
        {
            if (item.category == ItemCategory.Crop || 
                item.category == ItemCategory.Food || 
                item.category == ItemCategory.Seed || 
                item is FoodItemData ||
                item is SeedItemData ||
                string.Equals(item.itemId, "mat_coconut", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.itemId, "mat_cooking_oil", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.itemId, "mat_flour", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.itemId, "mat_seasoning", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.itemId, "item_spice", System.StringComparison.OrdinalIgnoreCase) ||
                (item.itemName != null && (
                    item.itemName.IndexOf("Coconut", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.itemName.IndexOf("Cooking Oil", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.itemName.IndexOf("Flour", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.itemName.IndexOf("Seasoning", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.itemName.IndexOf("Spice", System.StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                return true;
            }
            return false;
        }

        // Food category restriction (when list is populated).
        if (allowedFoodCategories != null && allowedFoodCategories.Count > 0)
        {
            bool allowed = false;
            bool allowsVegetable = false;
            for (int i = 0; i < allowedFoodCategories.Count; i++)
            {
                if (allowedFoodCategories[i] == ItemData.FoodCategory.Vegetable)
                    allowsVegetable = true;

                if (item is FoodItemData food && allowedFoodCategories[i] == food.foodCategory)
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed && (item.isDirty || (allowsVegetable && item.category == ItemCategory.Crop)))
            {
                allowed = true;
            }

            if (!allowed)
                return false;
        }

        return true;
    }

    // Listener hook (without altering data structures) to notify UI when slots change.
    public System.Action OnInventoryChanged;
    public static event System.Action<ItemData> OnItemConsumed;
    public static event System.Func<InventoryComponent, int, bool> OnEquipmentUseRequested;

    // Hotbar state & events (first 4 slots).
    public int selectedHotbarIndex = 0;
    public System.Action<int> OnHotbarSelected;

    public void SelectHotbarSlot(int index)
    {
        // Clamp index to hotbar range 0..3.
        index = Mathf.Clamp(index, 0, 3);
        selectedHotbarIndex = index;
        OnHotbarSelected?.Invoke(selectedHotbarIndex);
    }

    public void ResetInventory(int capacity)
    {
        maxCapacity = capacity;
        slots.Clear();
        for (int i = 0; i < maxCapacity; i++)
            slots.Add(new InventorySlot());
    }

    /// <summary>
    /// Ensures the slot list is allocated up to maxCapacity without clearing existing items.
    /// </summary>
    public void EnsureSlotsAllocated()
    {
        if (slots == null)
            slots = new List<InventorySlot>();

        while (slots.Count < maxCapacity)
            slots.Add(new InventorySlot());

        // Normalize: if a slot has an assigned item with quantity <= 0, fix it to 1
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].item != null && slots[i].quantity <= 0)
            {
                slots[i].quantity = 1;
            }
        }
    }

    /// <summary>
    /// Returns the enforced maximum stack limit (tools/weapons = 1, stackables clamped to 1..20).
    /// </summary>
    public static int GetMaxStackLimit(ItemData item)
    {
        if (item == null) return 1;
        if (item.type == ItemData.ItemType.Tool || item.category == ItemCategory.Tool || item.category == ItemCategory.Weapon)
            return 1;
        return Mathf.Clamp(item.maxStack, 1, 20);
    }

    /// <summary>
    /// Adds an item to the inventory.
    /// When hasHotbar is true (e.g., player), items are placed into main inventory slots (index 4+) first,
    /// and only fill the hotbar (index 0..3) if main slots are full.
    /// Returns the number of items SUCCESSFULLY added.
    /// </summary>
    public int AddItemAmount(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return 0;

        // Centralized backend rule: reject blocked trophies / restricted categories.
        if (!CanAcceptItem(item))
            return 0;

        int remaining = amount;
        int maxStack = GetMaxStackLimit(item);

        if (hasHotbar && slots.Count > 4)
        {
            // 1a. Prioritize stacking onto existing matching item stacks in the active Hotbar (indices 0..3)
            int hotbarEnd = Mathf.Min(4, slots.Count);
            for (int i = 0; i < hotbarEnd && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.item == item)
                {
                    int space = maxStack - slot.quantity;
                    int toAdd = Mathf.Min(space, remaining);
                    if (toAdd > 0)
                    {
                        slot.quantity += toAdd;
                        remaining -= toAdd;
                    }
                }
            }

            // 1b. Stack into main inventory slots (index >= 4) that already hold the same item
            for (int i = 4; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.item == item)
                {
                    int space = maxStack - slot.quantity;
                    int toAdd = Mathf.Min(space, remaining);
                    if (toAdd > 0)
                    {
                        slot.quantity += toAdd;
                        remaining -= toAdd;
                    }
                }
            }

            // 2a. Fill empty main inventory slots (index >= 4)
            for (int i = 4; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(maxStack, remaining);
                    slot.item = item;
                    slot.quantity = toAdd;
                    remaining -= toAdd;
                }
            }

            // 2b. If main slots are full and items remain, fill empty hotbar slots (index 0..3)
            for (int i = 0; i < hotbarEnd && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(maxStack, remaining);
                    slot.item = item;
                    slot.quantity = toAdd;
                    remaining -= toAdd;
                }
            }
        }
        else
        {
            // Default inventory without hotbar (e.g., Chest, Storage, Wardrobe, etc.):
            // 1. Stack onto existing non-full slots holding the same item
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.item == item)
                {
                    int space = maxStack - slot.quantity;
                    int toAdd = Mathf.Min(space, remaining);
                    if (toAdd > 0)
                    {
                        slot.quantity += toAdd;
                        remaining -= toAdd;
                    }
                }
            }

            // 2. Fill empty slots if items remain.
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(maxStack, remaining);
                    slot.item = item;
                    slot.quantity = toAdd;
                    remaining -= toAdd;
                }
            }
        }

        int added = amount - remaining;
        if (added > 0)
            OnInventoryChanged?.Invoke();

        return added;
    }

    public bool AddItem(ItemData item, int amount)
    {
        int added = AddItemAmount(item, amount);
        return added == amount;
    }

    /// <summary>
    /// Checks whether the inventory has enough capacity to hold 'amount' units of 'item'.
    /// </summary>
    public bool HasSpaceFor(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;
        if (!CanAcceptItem(item)) return false;

        int remaining = amount;
        int maxStack = GetMaxStackLimit(item);

        // 1. Check existing non-full slots holding the same item
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot != null && !slot.IsEmpty && slot.item == item)
            {
                int space = maxStack - slot.quantity;
                if (space > 0)
                {
                    remaining -= Mathf.Min(space, remaining);
                }
            }
        }

        // 2. Check empty slots
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty)
            {
                remaining -= Mathf.Min(maxStack, remaining);
            }
        }

        return remaining <= 0;
    }

    /// <summary>
    /// Removes a specific amount of items directly from a slot by index.
    /// </summary>
    public bool RemoveItemFromSlot(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || amount <= 0)
            return false;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.quantity < amount)
            return false;

        slot.quantity -= amount;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes a specified amount of an item from the inventory.
    /// If preferredSlotIndex is specified (or hasHotbar is active and the selected hotbar item matches),
    /// quantity is deducted from that slot first before taking from other slots.
    /// Remaining slots are deducted from back to front (highest index to lowest) to keep front/hotbar slots tidy.
    /// </summary>
    public bool RemoveItem(ItemData item, int amount, int preferredSlotIndex = -1)
    {
        if (item == null || amount <= 0)
            return false;

        if (CountTotalQuantity(item) < amount)
            return false;

        int remaining = amount;

        // Determine priority slot: if not explicitly specified,
        // check whether the active hotbar currently holds the same item.
        int resolvedPreferredIndex = preferredSlotIndex;
        if (resolvedPreferredIndex < 0 && hasHotbar && selectedHotbarIndex >= 0 && selectedHotbarIndex < Mathf.Min(4, slots.Count))
        {
            InventorySlot activeHotbarSlot = slots[selectedHotbarIndex];
            if (activeHotbarSlot != null && !activeHotbarSlot.IsEmpty && activeHotbarSlot.item == item)
            {
                resolvedPreferredIndex = selectedHotbarIndex;
            }
        }

        // 1. Deduct from the priority slot first if present
        if (resolvedPreferredIndex >= 0 && resolvedPreferredIndex < slots.Count)
        {
            InventorySlot prefSlot = slots[resolvedPreferredIndex];
            if (prefSlot != null && !prefSlot.IsEmpty && prefSlot.item == item)
            {
                int toRemove = Mathf.Min(prefSlot.quantity, remaining);
                prefSlot.quantity -= toRemove;
                remaining -= toRemove;

                if (prefSlot.quantity <= 0)
                {
                    prefSlot.item = null;
                    prefSlot.quantity = 0;
                }
            }
        }

        // 2. Deduct remaining amount from matching slots (back to front to keep tidy), skipping the already deducted priority slot.
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (i == resolvedPreferredIndex)
                continue;

            InventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty || slot.item != item)
                continue;

            int toRemove = Mathf.Min(slot.quantity, remaining);
            slot.quantity -= toRemove;
            remaining -= toRemove;

            if (slot.quantity <= 0)
            {
                slot.item = null;
                slot.quantity = 0;
            }
        }

        if (remaining == 0)
            OnInventoryChanged?.Invoke();

        return remaining == 0;
    }

    public void TransferItemTo(InventoryComponent targetInventory, ItemData item, int amount)
    {
        if (targetInventory == null || item == null || amount <= 0)
            return;

        if (targetInventory == this)
            return;

        int available = CountTotalQuantity(item);
        int amountToMove = Mathf.Min(amount, available);
        if (amountToMove <= 0)
            return;

        int added = targetInventory.AddItemAmount(item, amountToMove);
        if (added <= 0)
            return;

        RemoveItem(item, added);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// INDEX-based overload: transfers the contents of slot [sourceIndex] in this inventory
    /// to targetInventory. When targetInventory has a hotbar (e.g., Player), items enter
    /// main inventory slots (4+) first, then hotbar (0-3).
    /// Reduces or clears the source slot according to the amount successfully transferred.
    /// </summary>
    /// <param name="targetInventory">Destination inventory for the transfer.</param>
    /// <param name="sourceIndex">Source slot index in this inventory.</param>
    public void TransferItemTo(InventoryComponent targetInventory, int sourceIndex)
    {
        if (targetInventory == null || targetInventory == this)
            return;

        if (sourceIndex < 0 || sourceIndex >= slots.Count)
            return;

        InventorySlot source = slots[sourceIndex];
        if (source == null || source.IsEmpty || source.item == null)
            return;

        int added = targetInventory.AddItemAmount(source.item, source.quantity);
        if (added <= 0)
            return;

        source.quantity -= added;
        if (source.quantity <= 0)
        {
            source.item = null;
            source.quantity = 0;
        }

        OnInventoryChanged?.Invoke();
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
            return;

        ItemData item = slot.item;

        if (item is EquipmentItemData ||
            (item is ToolItemData toolItem && (toolItem.isWeapon || toolItem.isRangedWeapon) && !toolItem.isHoe) ||
            item.category == ItemCategory.Weapon ||
            string.Equals(item.itemId, "dummysword", System.StringComparison.OrdinalIgnoreCase))
        {
            if (OnEquipmentUseRequested != null)
            {
                OnEquipmentUseRequested.Invoke(this, slotIndex);
                return;
            }
        }

        if (item.type == ItemData.ItemType.Consumable && item is FoodItemData food)
        {
            // Special handling for refillable water bottle (100L): drink per sip (25L) without destroying the bottle
            if (item.itemId == "food_bottle_water")
            {
                var bottle = ServiceLocator.Resolve<IWaterService>();
                if (bottle != null)
                {
                    var textService = ServiceLocator.Resolve<IFloatingTextService>();
                    if (bottle.CurrentWater < 1f)
                    {
                        if (textService != null)
                        {
                            textService.SpawnText(
                                transform.position + Vector3.up * 1.5f,
                                "Water bottle is empty! Refill at Kitchen Sink.",
                                new Color(1f, 0.5f, 0.2f));
                        }
                        return;
                    }

                    bottle.DrinkSip(25f);
                    if (textService != null)
                    {
                        int chargesLeft = Mathf.FloorToInt(bottle.CurrentWater / 25f);
                        textService.SpawnText(
                            transform.position + Vector3.up * 1.5f,
                            $"💧 Gulp! (+25 Hydration | {chargesLeft}/4 Charges)",
                            new Color(0.2f, 0.85f, 1f));
                    }
                    return;
                }
            }

            // Apply healing, hunger/thirst restoration, and status buffs via typed contracts
            var vitals = GetComponent<IPlayerVitalsAction>();
            if (food.healAmount > 0)
                vitals?.Heal(food.healAmount);

            if (food.hungerRestore != 0)
                vitals?.Eat(food.hungerRestore);

            if (food.hydrationRestore != 0)
                vitals?.Drink(food.hydrationRestore);

            if (food.buffEffects != null && food.buffEffects.Count > 0)
                GetComponent<IFoodBuffApplier>()?.ApplyBuffs(food.buffEffects);

            Debug.Log($"[Inventory] Mengonsumsi {item.itemName}: Heal={food.healAmount}, Hunger={food.hungerRestore}, Thirst={food.hydrationRestore}, Buffs={food.buffEffects?.Count ?? 0}");

            // Deduct quantity ON THE SPECIFICALLY clicked slot.
            slot.quantity -= 1;
            if (slot.quantity <= 0)
            {
                slot.item = null;
                slot.quantity = 0;
            }

            OnInventoryChanged?.Invoke();
            OnItemConsumed?.Invoke(item);
        }
    }

    public void DropItem(int slotIndex)
    {
        // Validate array bounds and ensure slot is not empty.
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
            return;

        ItemData item = slot.item;
        Debug.Log(string.Format("Dropping {0} to the ground!", item.itemName));

        // Reduce quantity on the dropped slot; clear if depleted.
        slot.quantity -= 1;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }

        OnInventoryChanged?.Invoke();

        // If the dropped item was in the active hotbar slot,
        // notify listeners so the equipment visual system refreshes (e.g., destroys the model).
        if (slotIndex == selectedHotbarIndex)
            OnHotbarSelected?.Invoke(selectedHotbarIndex);
    }

    /// <summary>
    /// Replaces a single slot's contents with another item (food state transitions:
    /// dirty -> clean -> cooked, etc.). Fires OnInventoryChanged.
    /// </summary>
    public void ReplaceItemAt(int index, ItemData item, int quantity)
    {
        if (index < 0 || index >= slots.Count)
            return;

        InventorySlot slot = slots[index];
        if (slot == null)
            return;

        slot.item = item;
        slot.quantity = quantity;
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Decrements quantity on a specific slot (used to consume ingredients
    /// when cooking starts). Fires OnInventoryChanged.
    /// </summary>
    public void RemoveFromSlot(int index, int amount)
    {
        if (index < 0 || index >= slots.Count)
            return;

        InventorySlot slot = slots[index];
        if (slot == null || slot.IsEmpty || amount <= 0)
            return;

        slot.quantity -= amount;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }

        OnInventoryChanged?.Invoke();
    }

    // Precision slot-to-slot transfer between inventories (or within the same inventory).
    // Handles three cases: move to empty slot, stack matching items, and swap different items.
    // Returns true if transfer succeeded, false if failed (bounds check, full capacity, or validation failed).
    public bool MoveItemToSlot(int sourceIndex, InventoryComponent targetInventory, int targetIndex)
    {
        if (targetInventory == null)
            return false;

        if (sourceIndex < 0 || sourceIndex >= slots.Count)
            return false;
        if (targetIndex < 0 || targetIndex >= targetInventory.slots.Count)
            return false;

        InventorySlot sourceSlot = slots[sourceIndex];
        InventorySlot targetSlot = targetInventory.slots[targetIndex];
        if (sourceSlot == null || targetSlot == null)
            return false;

        if (sourceSlot.IsEmpty)
            return false;

        // Centralized backend rule: reject items disallowed in target (trophy/category).
        if (sourceSlot.item != null && !targetInventory.CanAcceptItem(sourceSlot.item))
            return false;

        if (targetSlot.IsEmpty)
        {
            // Move all data from source to target, then clear source.
            targetSlot.item = sourceSlot.item;
            targetSlot.quantity = sourceSlot.quantity;
            sourceSlot.item = null;
            sourceSlot.quantity = 0;
        }
        else if (targetSlot.item == sourceSlot.item)
        {
            // Stacking: fill target's free space with items from source.
            int maxStack = GetMaxStackLimit(targetSlot.item);
            int space = maxStack - targetSlot.quantity;
            if (space > 0)
            {
                int toMove = Mathf.Min(space, sourceSlot.quantity);
                targetSlot.quantity += toMove;
                sourceSlot.quantity -= toMove;
                if (sourceSlot.quantity <= 0)
                {
                    sourceSlot.item = null;
                    sourceSlot.quantity = 0;
                }
            }
            else
            {
                // Target slot full, cannot stack more
                return false;
            }
        }
        else
        {
            // Different items: swap slot data.
            InventorySlot temp = new InventorySlot();
            temp.item = targetSlot.item;
            temp.quantity = targetSlot.quantity;
            targetSlot.item = sourceSlot.item;
            targetSlot.quantity = sourceSlot.quantity;
            sourceSlot.item = temp.item;
            sourceSlot.quantity = temp.quantity;
        }

        OnInventoryChanged?.Invoke();
        if (this != targetInventory)
            targetInventory.OnInventoryChanged?.Invoke();

        if (selectedHotbarIndex == sourceIndex)
            OnHotbarSelected?.Invoke(selectedHotbarIndex);
        if (targetInventory.selectedHotbarIndex == targetIndex)
            targetInventory.OnHotbarSelected?.Invoke(targetInventory.selectedHotbarIndex);

        return true;
    }

    public void SwapSlots(int indexA, int indexB)
    {        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count)
            return;

        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        // Notify UI to redraw all slots (icon + quantity).
        OnInventoryChanged?.Invoke();
    }

    public int CountItem(ItemData item)
    {
        return CountTotalQuantity(item);
    }

    private int CountTotalQuantity(ItemData item)
    {
        int total = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty && slot.item == item)
                total += slot.quantity;
        }
        return total;
    }
}
