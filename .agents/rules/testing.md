# Testing.md

Testing strategies, automated verification, regression prevention, and checklists for **Farm-Beware**.

---

## 1. Quality Assurance Standards

1. **Official Verification Suite**: Run via Unity MCP:
   `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`
   **Target**: All audit tests must pass (**86/86 Passed**).
2. **Clean Console Mandate**: 0 Errors, 0 Warnings, 0 Exceptions via `read_console`.
3. **Native Assertion Safety**: Never call raw `SendMessage` on inactive objects or in EditMode to prevent `Assertion failed: ShouldRunBehaviour()`.
4. **Anti-Regression Interaction Gate**:
   Features must pass the **5 Interaction Edges**:
   - *UI Interrupt*: ESC / Modal open mid-action must not leak input locks.
   - *Phase Jump*: Day/Night transitions mid-feature must not cause NRE or state desync.
   - *Save/Reload*: Hydration must preserve state integrity across sessions.
   - *Combat Interrupt*: Damage/death during interaction must abort safely.
   - *Cancel Rollback*: Aborted actions must restore camera, inputs, and resources.
5. **Continuous Audit Expansion**: When adding milestones, append automated tests to `GameplayAndHUDTests.cs`.

---

## 2. Core Automated Audit Coverage (86 Tests)

- **Combat & Night Brawl (Tests 1–27)**: Poise damage, knockdown invariants, attack tokens, perimeter fence, food buff purge on sleep, multi-source input lock, health bars, wave spawning, seed consumption, combat FSM.
- **Narrative & Vehicles (Tests 28–50)**: Storyline FSM, 5-day Monte Carlo reachability, `OpeningCutsceneController` truck arrival and dismount, `VehicleDynamicsCore`, dual-channel vehicle entry/exit, `NarrativeFactBoard`, driver sync.
- **Agriculture, UI & Quests (Tests 51–70)**: Farmland plot interact gating, crop growth countdown night suppression, fridge storage, pointer click input lock bypass, Journal UI `[J]`, side quest *First Steps*, zero-GC audit, player rig.
- **Commercial Economy & Trading Post (Tests 71–79)**: Day-to-night cutscene deconfliction, cooking, `ShopInteractable` Day 0 gating & Day 1+ `MerchantShopUI`, `PlayerWallet`, `DailyEconomyManager` prices & discounts, Trading Post turnaround bay (Test 79).
- **Weapon & Armor Upgrades (Tests 80–86)**: Weapon Lv 1–10, Armor Lv 1–10, single-path lock, Sweet Potato Set 2/4/5-pc, EquipmentSlotUI drag-and-drop, Save/Load hydration, and Contextual Combat Stance (Test 86).

---

## 3. Manual Play Mode Verification Checklist

1. **Vehicle & Arrival**: Truck arrives at gate; player dismounts cleanly with empty hands.
2. **Agriculture Loop**: Till soil (`1`), plant seeds, water with `PlayerWaterBottle`. Crops grow across day transitions.
3. **Western Corridor & Trading Post**: Travel West along 225m road to Trading Post at `(-175, 0, 70)`.
4. **Trading Interaction**: Interact (`E`) with counter to open `MerchantShopUI` and trade produce.
5. **Workbench Upgrades**: Interact (`E`) with Workbench in garage to upgrade weapon, armor, and craft Sweet Potato set pieces.
6. **Night Combat**: Defend gate using light combos, charged attacks, parries (`V`), and dodges (`C`).

---

## 4. Troubleshooting Matrix

| Symptom | Root Cause | Resolution |
|---|---|---|
| `Assertion failed: ShouldRunBehaviour()` | Raw `SendMessage` on inactive object or in EditMode | Use direct type-safe method or reflection (`MethodInfo.Invoke`) |
| Vehicle flips on road | Active mesh colliders on road ribbon or shoulders | Disable colliders; ride 100% on native `TerrainCollider` |
| Trading post occluded | Facade facing North/East under SW isometric camera | Rotate building/counter so front facade faces South (`rot=[0,0,0]`) |
| Turnaround bay obstacle | Colliders inside 8m radius of `TurnaroundBay_Marker` | Reposition marker to open yard center (`[-175, 0, 50]`) |
| Parries spammable | Missing whiff recovery lockout | Enforce `CanInitiateParry` and 350ms lockout in `DefenseEvaluator` |
