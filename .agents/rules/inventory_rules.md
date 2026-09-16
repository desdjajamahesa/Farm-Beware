# Inventory & Item Stack Rules

1. **Max Stack Ceiling (20)**:
   - All stackable items (`ItemData`) in Farm-Beware have a strict maximum stack size of **20** (`maxStack <= 20`).
   - Any new or modified stackable item (crops, food, seeds, materials, monster drops) MUST set `maxStack = 20`.
   - Never set `maxStack` higher than 20.

2. **Non-Stackable Items (1)**:
   - Tools, weapons, equipment, and trophies MUST set `maxStack = 1`.

3. **Code Enforcement**:
   - `ItemData.cs` enforces `[Range(1, 20)]` and clamps `maxStack` in `OnValidate()`.
