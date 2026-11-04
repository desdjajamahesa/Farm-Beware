# Testing.md

Testing strategies, automated verification scripts, manual Play Mode checklists, and troubleshooting for **Farm-Beware**.

---

## 1. Quality Assurance Standards

1. **Official Verification Suite**: Run the comprehensive audit suite via Unity MCP:
   `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`
   **Target**: All 79 audit tests must pass (**79/79 Passed**).
2. **Clean Console Mandate**: Every turn must leave the Editor console with **0 Errors**, **0 Warnings**, and **0 Exceptions** via `read_console`.
3. **Native Assertion Safety**: Never call raw `SendMessage` on inactive objects or during EditMode tests to prevent `Assertion failed on expression: 'ShouldRunBehaviour()'`.

---

## 2. Core Automated Audit Coverage (79 Comprehensive Tests)

- **Combat & Night Brawl (Tests 1–27)**: Poise damage, knockdown invariants, attack token dispatching, 360° perimeter fence boundary, food buff purge on sleep, multi-source input lock, enemy/boss health bars, wave spawning, seed consumption priority, and combat FSM pacing.
- **Narrative & Vehicles (Tests 28–50)**: Storyline FSM, 5-day Monte Carlo reachability (1,000 runs), `OpeningCutsceneController` truck arrival and clean dismount, `VehicleDynamicsCore` kinematics, dual-channel vehicle entry/exit, `NarrativeFactBoard` tokens, Day 0 arrival HUD suppression, and driver visibility synchronization.
- **Agriculture, UI & Quests (Tests 51–70)**: Farmland plot interact gating, crop growth countdown night suppression, fridge food storage, pointer click input lock bypass, Journal UI `[J]` toggle, side quest *First Steps* progression, zero-GC update loop audit, player combat rig, and dummy sword hand socket alignment.
- **Commercial Economy & Trading Post (Tests 71–79)**: Day-to-night cutscene deconfliction, home cooking progression, `ShopInteractable` Day 0 gating & Day 1+ `MerchantShopUI` modal, `PlayerWallet` balance deductions/refunds, `DailyEconomyManager` fluctuating price rolling & dirty produce discount, and Trading Post architectural clearances & 16m turnaround bay (Test 79).

---

## 3. Manual Play Mode Verification Checklist

1. **Vehicle & Arrival**: Day 0 pickup truck arrives at homestead gate. Player dismounts with empty hands and zero inventory.
2. **Agriculture Loop**: Till soil with Hoe (`1`), plant seeds (`Sweet Potato`, `Taro`, `Corn`), water using `PlayerWaterBottle`. Crops grow across day transitions.
3. **Western Corridor & Trading Post**: Drive or walk West from Homestead Gate past fence line along 225m cobblestone road to Valley Trading Post at `(-175, 0, 70)`.
4. **Trading Interaction**: Walk up South-facing veranda steps to `TradingPost_ShopCounter`. Press `E` to open `MerchantShopUI` and sell dirty/clean crops for Gold.
5. **Night Combat**: Defend homestead gate against night brawlers. Use combos, parries (`V`), and dodges (`C`).

---

## 4. Troubleshooting Matrix

| Symptom | Root Cause | Resolution |
|---|---|---|
| `Assertion failed: ShouldRunBehaviour()` | Raw `SendMessage` called on inactive object or in EditMode | Use direct type-safe method or C# reflection (`MethodInfo.Invoke`) |
| Vehicle flips on road | Active mesh colliders on road ribbon or shoulders | Disable colliders; ride 100% on native `TerrainCollider` |
| Trading post occluded by roof | Facade facing North/East under SW isometric camera | Rotate building/counter so front facade faces South (`rot=[0,0,0]`) |
| Turnaround bay obstacle in Test 79 | Colliders inside 8m radius of `TurnaroundBay_Marker` | Reposition marker to open yard center (`[-175, 0, 50]`) |
| Parries spammable without penalty | Missing whiff recovery lockout | Enforce `CanInitiateParry` and 350ms whiff timer in `DefenseEvaluator` |
