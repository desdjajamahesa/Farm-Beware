# Developer Remediation Status Addendum (QA & Testing HUB)

> **Build Target for QA Retest**: `Alpha 0.3.0` (`ProjectSettings.asset`: `bundleVersion: 0.3.0`)  
> **Source CSV Preserved Unmodified**: `Docs/QA & Testing HUB/QA & Testing HUB/Bugs ᴰᴮ/Bugs d8c97560e11a83eb876b01df8cc14f34.csv` (Notion export kept read-only so future Notion syncs do not collide).

---

## 1. Summary of Defect Remediation

| Range | CSV Status (2026-11-22 Export) | Developer State (`V0.3.0`) | Evidence / Notes |
|---|---|---|---|
| `BUG-001` .. `BUG-008` | `Closed` | Verified Fixed | Regression-covered in existing audit tests |
| `BUG-009` .. `BUG-024` | `Open` | **Fixed — Pending QA Retest** | Remediated in `d54add2` & verified via **Test 114** (`114/114` -> `117/117`) |
| `BUG 025` .. `BUG 027` | `Not started` | Unfilled QA Placeholders | Empty reproduction steps in Notion export (`- 1st step : ...`) |

> **CSV Numbering Note**: The 2026-11-22 Notion export omits `BUG 019` and contains two rows numbered `BUG 021` (`Cooking process continues with every save game load` and `Enemy Tuber maw drop has got both of gold and mutated root`). Both are fixed.

---

## 2. Per-Bug Remediation Matrix (`BUG-009` .. `BUG-024`)

| Notion ID | Issue Summary | Root Fix Scope | Automated Verification |
|---|---|---|---|
| `BUG-009` | Night save/load resets enemy count to 1 | Night wave count & active enemy state restored on load (`SaveSystemManager` / `NightBrawlManager`) | Test 114 + Save/Load regression (`117/117`) |
| `BUG 010` | Seed sell vs. buy price exploit at merchant | Calibrated shop buy/sell pricing parity in `DailyEconomyManager` & `MerchantShopUI` | Test 112 + Test 114 |
| `BUG 011` | Initial/wake-up day display offset (`Day 1`) | Morning HUD & `DayNightTimeManager` day counter sync | Test 114 |
| `BUG 012` | Player can melee-attack enemies through cabin walls/doorways | Wall/doorway corner line-of-sight geometry checks in player & enemy combat | Test 114 |
| `BUG 013` | Essential farming tools (Hoe, Watering Can) can be sold | Key-item non-sellable guard in `MerchantShopUI` / `ItemData` | Test 114 |
| `BUG 014` | Food buffs bleed across different save slots | Clear & rehydrate `PlayerBuffManager` state strictly per save slot load | Test 114 |
| `BUG 015` | Incorrect recipe definitions on Kitchen Stove | Synchronized stove recipe requirements (`KitchenRecipe`) | Test 114 |
| `BUG 016` | Water bottles duplicate via sink + save/load | Transactional refill state machine reset on load (`RefillStationStateMachine`) | Test 114 + Test 110 |
| `BUG 017` | Vegetables duplicate via sink washing + save/load | Escrow slot cleanup & state reset across save/load transitions | Test 114 |
| `BUG 018` | Sink cleaning timer persists across save/load | Reset active washing session timer on scene/save hydration | Test 114 |
| `BUG 021` *(slot 19)* | Stove cooking timer persists across save/load | Reset active cooking session timer on save/load hydration | Test 114 |
| `BUG 020` | Food duplicates via stove cooking + save/load | Transactional escrow reset & rollback on load (`StoveUIManager` / `KitchenStation`) | Test 114 |
| `BUG 021` *(dup)* | Tuber Maw drops both gold and mutated root inconsistency | Normalized loot table drops in `EnemyLootDropHandler` | Test 114 |
| `BUG 022` | Tuber Maw drops only seeds without gold/mutated loot | Fixed enemy drop table roll & loot spawning in `EnemyLootDropHandler` | Test 114 |
| `BUG 023` | Break interval between waves only 5 seconds | Enforced 30s tactical intermission timer in `NightBrawlManager` | Test 114 |
| `BUG 024` | Dirty Corn cannot be washed in the kitchen sink | Registered dirty Corn -> clean Corn wash mapping in `KitchenSinkInteractable` / `SinkManager` | Test 114 |

---

## 3. Ready for QA Verification
- All **117/117** EditMode audit tests pass on `Rafi-branch` via both `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()` and Unity Test Runner (`FarmBeware.Editor.Tests`).
- QA can begin the `Alpha 0.3.0` PlayMode retest pass to transition `BUG-009`..`BUG-024` from `Open` to `Closed` inside Notion.
