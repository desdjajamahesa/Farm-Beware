# Architecture.md

System design, ASMDEF topology, and contracts for **Farm-Beware** (`Alpha 0.3.0`).

---

## 1. Engine & Code Separation

- **Engine**: Unity `6000.3.20f1`, URP Deferred+, GPU Resident Drawer (BRG), New Input System `1.19.0`, AI Navigation `2.0.13`, Cinemachine `3.1.7`, uGUI `2.0.0`.
- **Pure Logic vs. Thin Adapter**: Deterministic game math (`CropData`, `MapProjectionMath`, `CombatStateEvaluator`, `PoiseTracker`, `RefillStationStateMachine`, `VehicleDynamicsCore`, `DailyEconomyManager`) runs in Zero-GC pure C# POCOs; `MonoBehaviour` classes serve strictly as thin Unity adapters.
- **Architecture Invariants (Test 117)**: `0` runtime `SendMessage`, `0` empty `catch {}` blocks, typed `Core/Contracts/` cross-module calls, and `ProceduralUIAssetCache` shared asset reuse.

---

## 2. ASMDEF Dependency Graph (22 Runtime + 1 Editor)

- **Tier 0 Core & Data**:
  - `FarmBeware.Core.Runtime`: `ServiceLocator`, `ModalStackManager`, `SceneNames`, `MapProjectionMath`, `ProceduralUIAssetCache`, `WoodenBoardStyle`, contracts (`Core/Contracts/`).
  - `FarmBeware.Data.Runtime`: `ItemData`, `SeedItemData`, `CropData`, `GameSaveData`.
- **Tier 1 Foundation**:
  - `FarmBeware.Features.Common` (`RefillStationStateMachine`), `Time`, `Economy` (`DailyEconomyManager`, `MerchantShopUI`, `OrderBoard`), `Inventory` (`ItemDatabase`, `InventoryComponent`), `SaveSystem` (`SaveSystemManager`), `Interaction`.
- **Tier 2 Feature Modules**:
  - `Camera`, `Environment`, `Farming`, `Kitchen`, `Combat`, `Quest` (`QuestManager`, `QuestLogUI`, `QuestHUDUI`), `Storyline`, `Trophy`, `Vehicles`, `Wardrobe`, `Workbench`, `MainMenu`.
- **Tier 3 Composition & Rendering**:
  - `FarmBeware.Gameplay.Runtime` (`PlayerControl`, `PlayerStats`, `WorldMapUI`), `FarmBeware.Rendering.Runtime`.
- **Editor CI**: `FarmBeware.Editor.Tests` (`GameplayAndHUDTests` + NUnit `EditMode` adapter).

---

## 3. Typed Cross-Module Contracts (`Core/Contracts/`)

- `ICutsceneService` & `ICutsceneSuppressibleHud` (`VehicleDrivingHUD`)
- `IPlayerVitalsAction`, `IStarterKitProvider`, `IStatsDisplayRefreshable`, `IDialogueDismissible`, `IFoodBuffApplier`
- `IFarmingService`, `IQuestService`, `INarrativeFactBoard`, `IItemCatalog`, `IPlayerContext`

---

## 4. Environment, UI Split & Rendering

- **Partial UI Layout Pattern**: `MerchantShopUI`, `WorkbenchUI`, `StoveUIManager`, `StorylineUI`, and `MainMenuController` split visual construction into `*.Layout.cs` files.
- **Western Corridor & Outpost**: 225m Catmull-Rom road (`Road_Homestead_To_TradingPost`) riding native `TerrainCollider` to `TradingPost_Complex` (`-175, 0, 70`), South-facing counter (`Z = 63.5`).
- **Rendering & NavMesh**: URP Deferred+ (Static Batching off for BRG), Light Layer 0 (Sun/Moon) / Layer 1 (Interior), `MonsterFresnelLit` & `DitheredBuildingLit` (Bayer 4x4), NavMesh obstacles on Campfire, Pond, Plots, and Trading Post.
