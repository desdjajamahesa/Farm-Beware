# Inventory & Item Stack Rules

1. **Max Stack Ceiling (20)**:
   - All stackable items (`ItemData`) in Farm-Beware have a strict maximum stack size of **20** (`maxStack <= 20`).
   - Any new or modified stackable item (crops, food, seeds, materials, monster drops) MUST set `maxStack = 20`.
   - Never set `maxStack` higher than 20.

2. **Non-Stackable Items (1)**:
   - Tools, weapons, equipment, and trophies MUST set `maxStack = 1`.

3. **Code Enforcement**:
   - `ItemData.cs` enforces `[Range(1, 20)]` and clamps `maxStack` in `OnValidate()`.

4. **Transaction & Snapshot Safety (Crafting / Stations)**:
   - Any production or cooking station that consumes items or liquids over time (e.g., `KitchenStove`) MUST snapshot consumed items and liquids at transaction initiation.
   - If the process is cancelled, interrupted, or closed prematurely, the station must support an atomic rollback (`CancelCooking(refundIngredients: true)`), restoring item counts to `InventoryComponent` and liquid volumes to `PlayerWaterBottle`.
   - Output items must only be added to inventory upon successful completion of the timer, at which point transaction snapshots are cleanly cleared.

