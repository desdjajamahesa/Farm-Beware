# Testing.md

Testing strategies, automated verification, and checklists for **Farm-Beware**.

---

## 1. Quality Assurance Standards

1. **Official Verification Channels (117/117)**:
   - Menu / Roslyn runner: `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`
   - Unity Test Runner (`EditMode` / CI): assembly `FarmBeware.Editor.Tests` (`GameplayAndHUDTests.NUnitAdapter.cs`) via `run_tests`.
   - **Target**: **117/117 Passed** (0 failures, 0 skipped).
2. **Clean Console Mandate**: 0 compilation errors, 0 script warnings, 0 unhandled exceptions via `read_console`.
3. **Zero-SendMessage & Contract Discipline**: Never invoke runtime `SendMessage`; enforce typed interfaces (`Core/Contracts/`) and explicit null/lifecycle guards (no empty `catch {}` blocks).
4. **Anti-Regression 5 Interaction Edges**: Every feature must handle *UI Interrupt* (ESC/modal lock cleanup), *Phase Jump* (Day/Night/Sleep), *Save/Reload* hydration, *Combat Interrupt*, and *Cancel Rollback*.
5. **Continuous Audit Expansion**: Append a new regression test to `GameplayAndHUDTests` for every milestone or bug fix.

---

## 2. Automated Audit Coverage (117 Tests)

- **Combat & Night Brawl (Tests 1–27)**: Poise damage, knockdown invariants, priority attack tokens, perimeter fence, food buff purge on sleep, multi-source input locks, health bars, wave spawning (6–10s), seed drops, combat FSM.
- **Narrative & Vehicles (Tests 28–50)**: Storyline FSM, 5-day Monte Carlo reachability, `OpeningCutsceneController` truck arrival & dismount, `VehicleDynamicsCore`, dual-channel vehicle entry/exit, `NarrativeFactBoard`.
- **Agriculture, UI & Quests (Tests 51–70)**: Farmland plot gating, crop growth night suppression, fridge storage, pointer click bypass, Journal UI (`J`), Quest *First Steps*, zero-GC audit, player rig.
- **Economy, Trading Post & World Map (Tests 71–79, 98–107)**: Day/night cutscene deconfliction, cooking escrow, `MerchantShopUI`, `PlayerWallet`, `DailyEconomyManager`, Trading Post turnaround bay, `WorldMapUI` projection & markers.
- **Upgrades, Cutscenes & Rig Stabilization (Tests 80–97, 108–110)**: Weapon/Armor Lv 1–10, Sweet Potato set bonuses, drag-and-drop equipment, Contextual Combat Stance, `Night1CutsceneController`, `VehicleDrivingHUD` suppression, Wardrobe & rig stability.
- **Architecture, Order Board, Quests, QA Hub & Combat Overhaul (Tests 111–117)**:
  - **Test 111**: Asset deduplication, `ProceduralUIAssetCache`, `IItemCatalog` unification, pure solar trajectory.
  - **Test 112**: Calibrated economy pricing & Outdoor `OrderBoard` (`1.2x` clean-only multiplier).
  - **Test 113**: Quest Log UI (`Q`), HUD incremental binding & Zero-GC updates.
  - **Test 114**: QA & Testing HUB remediation regression (`BUG-009`..`BUG-024`, doorway corners, well refill cap, Day 1 dialogue timing).
  - **Test 115**: `PlayerModular` heavy chains, action cancel windows, animator guards.
  - **Test 116**: `WoodenBoardStyle` UI theme parity & panel isolation.
  - **Test 117**: Architecture hardening (`0` runtime `SendMessage`, `0` empty `catch`, typed contracts, ASMDEF coverage).

---

## 3. Manual PlayMode Verification Checklist

1. **Vehicle & Opening**: `MainMenuScene` -> New Game -> truck arrives -> Ucup dismounts cleanly.
2. **Farming & Economy**: Till (`1`), plant, water (`PlayerWaterBottle`), wash crops at sink, fulfill clean orders at Outdoor Order Board (`1.2x`) or trade at Trading Post.
3. **UI & Modals**: Verify `Tab` (Inventory), `Q` (Quest Log), `M` (World Map), `J` (Journal), and `ESC` pause stack without input lock leaks.
4. **Night Brawl**: Defend perimeter using `PlayerModular` combos, charged heavies, parries (`V`), and dodges (`C`).
