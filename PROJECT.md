# Project: Farm-Beware — Architecture, Core Loop & V0.3.0 Status

## Overview & Stack
- **Version**: Alpha `0.3.0` (`ProjectSettings.asset`: `bundleVersion: 0.3.0`)
- **Engine & Pipeline**: Unity `6000.3.20f1` (Unity 6), Universal Render Pipeline (URP Deferred+), GPU Resident Drawer, New Input System `1.19.0`, AI Navigation `2.0.13`, Cinemachine `3.1.7`, uGUI `2.0.0`.
- **Active Build Scenes**: `Assets/Scenes/MainMenuScene.unity`, `Assets/Scenes/MainScene.unity`.

## System Architecture (22 Runtime ASMDEFs + 1 Editor Test ASMDEF)
- **Pure Logic vs. Thin Adapter**: Game logic (crop pricing/growth, combat FSM, poise, wave progression, map projection math, inventory stacking, refill state machine) resides in Zero-GC pure C# POCOs; `MonoBehaviour` classes act as thin Unity adapters.
- **Typed Interface Contracts (`Assets/Scripts/Core/Contracts/`)**:
  - Cross-module communication uses `ServiceLocator` and explicit contracts (`ICutsceneService`, `ICutsceneSuppressibleHud`, `IPlayerVitalsAction`, `IStarterKitProvider`, `IStatsDisplayRefreshable`, `IDialogueDismissible`, `IFoodBuffApplier`, `IFarmingService`, `IQuestService`, `INarrativeFactBoard`) with **0 runtime `SendMessage`** calls and **0 empty `catch {}`** blocks.
- **Procedural UI Theme & Partial Layout Split**:
  - Unified `WoodenBoardStyle` & shared `ProceduralUIAssetCache`.
  - Oversized UI managers (`MerchantShopUI`, `WorkbenchUI`, `StoveUIManager`, `StorylineUI`, `MainMenuController`) cleanly separated into behavior + `*.Layout.cs` partial files.

## Key Implemented Features (V0.3.0)
- **5-Day Diurnal Loop & Economy**: 18-minute day cycle, 16+ expandable farmland plots (up to 32 via Land Expansion Deeds), indoor stove/sink, Outdoor Order Board (1.2x clean-only bonus), Trading Post (`MerchantShopUI`), calibrated crop pricing (`375–500G` SP, `700–900G` Taro & Corn per unit, 50% dirty discount).
- **Combat, Upgrades & Rig**: `PlayerModular` combat chains, cancel windows, parries (`V`), dodge rolls (`C`), 10-level Weapon/Armor Workbench upgrades, sweet potato armor set bonuses, night horde waves + `CyclopsTuberMaw`, `TaroColossus`, `TheRanger` bosses.
- **Narrative, HUD & Persistence**: 5-day NexaPhyto storyline (`StoryDatabase`), Opening & Night 1 cutscenes, Journal (`J`), Quest Log (`Q`), World Map (`M`), Wardrobe (`WardrobeUI`), 3-slot JSON Save System (`SaveSystemManager`).

## Automated Verification & CI Status
- **Official Suite**: `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()` — **117/117 Passed**.
- **Unity Test Runner (`EditMode`)**: `FarmBeware.Editor.Tests` (`GameplayAndHUDTests.NUnitAdapter.cs`) — **117/117 Passed**.
- **Compiler & Assets**: 0 compilation errors/warnings; Roslyn analyzer DLLs scoped to Editor-only; 100% English codebase comments and identifiers.
