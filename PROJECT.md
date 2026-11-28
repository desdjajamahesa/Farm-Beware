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
- **5-Day Diurnal Loop & Economy**: 18-minute day cycle, 6 starting farmland plots expandable up to 16 via Land Expansion Deeds (`2,500 G` per +2 plots), indoor 3-burner stove & `WoodenBoard_TwoColumn` kitchen sink, Outdoor Order Board (`1.2×` clean-only bonus), Trading Post (`MerchantShopUI`), calibrated crop pricing (`375–500G` SP, `700–900G` Taro & Corn per unit, 50% dirty discount), and all 9 cooking recipes aligned to the MVP Food Buff table (`90s–420s`).
- **Combat Polish SSOT, Armor Stats & Rig**: `PlayerModular` combat chains driven by a 60 FPS Single Source of Truth (`CombatFrameDataSheet`, 2D cancel priority matrix, `ImpactKinematicsProfile`, `EnemyCombatProfile` tracking lock-off & token eviction), aligned weapon/bare-hands attack trails, ground-safe NavMesh enemy spawning, full wiring of all 5 Sweet Potato armor gear stats (`HP`, `DEF`, `MoveSpd`, `AtkSpd`, `Crit`) combined with Lv 1–10 Workbench plating mitigation + UI telemetry, parries (`V`), dodge rolls (`C`), 10-level Weapon/Armor upgrades, and night horde waves + `CyclopsTuberMaw`, `TaroColossus`, `TheRanger` bosses.
- **Narrative, HUD & Persistence**: 5-day NexaPhyto storyline (`StoryDatabase`), Opening & Night 1 cutscenes, Journal (`J`), Quest Log (`Q`), World Map (`M`), Wardrobe (`WardrobeUI`), 3-slot JSON Save System (`SaveSystemManager`).
- **Stats Reference**: See [Docs/GAME_STATS_REFERENCE.md](Docs/GAME_STATS_REFERENCE.md) for the complete numerical reference (damage, defense, armor mitigation vs monster hits, 60 FPS frame data, enemies, cooking, and economy).

## Automated Verification & CI Status
- **Official Suite**: `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()` — **123/123 Passed**.
- **Unity Test Runner (`EditMode`)**: `FarmBeware.Editor.Tests` (`GameplayAndHUDTests.NUnitAdapter.cs`) — **123/123 Passed**.
- **Compiler & Assets**: 0 compilation errors/warnings; Roslyn analyzer DLLs scoped to Editor-only; 100% English codebase comments and identifiers.
