# Inventory, Item Stack & Storage Rules

Core rules governing item containers, stacking constraints, and storage mechanics in **Farm-Beware**.

---

## 1. Inventory Hierarchy & Capacity

1. **Player Inventory (16 Slots)**:
   - Partitioned into **4 Active Hotbar Slots** (`[0..3]`) and **12 Main Backpack Slots** (`[4..15]`).
   - Hotbar active slot has seed and tool consumption priority during farming and building interactions.
2. **Dedicated Storage Chests**:
   - Independent storage capacity for farming tools, harvested produce, seeds, and crafting materials.
   - Day 1 starter kit populates chest automatically; player wakes up Day 0 and Day 1 with empty hands and empty inventory.
3. **Kitchen Refrigerator Storage**:
   - Permissive cold storage for all food, ingredients, and harvested crops (Sweet Potato, Taro, Corn).

---

## 2. Max Stack Ceiling (20) & Equipment (1)

1. **Max Stack Ceiling (20)**:
   - All stackable items (`ItemData`) in Farm-Beware enforce a strict ceiling of **20** (`maxStack <= 20`).
   - Crops, seeds, materials, food, and monster drops MUST set `maxStack = 20`.
   - Never configure `maxStack` higher than 20.
2. **Non-Stackable Items (1)**:
   - Tools, weapons, armor, vehicles, and trophies MUST set `maxStack = 1`.
3. **Code Enforcement**:
   - `ItemData.cs` enforces `[Range(1, 20)]` and clamps `maxStack` in `OnValidate()`.

---

## 3. Crop Yields: Clean vs. Dirty Quality

1. **Dirty Produce Discount**:
   - Freshly harvested crops spawn in Dirty state by default.
   - Dirty crops sell at a **50% discount** at the Commercial Trading Post (`DailyEconomyManager`).
2. **Washing Station Transformation**:
   - Washing dirty crops at the `KitchenSinkInteractable` consumes water from `PlayerWaterBottle` and transforms items into Clean variants, restoring 100% full market value.

---

## 4. Station Snapshot & Rollback Safety

1. **Atomic Transaction Snapshots**:
   - Stations consuming items or liquids over time (`KitchenStove`) MUST snapshot inputs at transaction start.
2. **Rollback on Cancel**:
   - Premature cancellation (`CancelCooking(refundIngredients: true)`) triggers an atomic rollback, restoring exact item counts to `InventoryComponent` and water to `PlayerWaterBottle`. Output is granted only on timer completion.
