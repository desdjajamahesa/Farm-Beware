# Project: Farm-Beware — Cutscene Presentation, Night Day 1 Cinematic & Spacing Tuning

## Architecture
- **Engine & Pipeline**: Unity 6000.3.20f1, Universal Render Pipeline (URP Deferred+), Input System 1.19.0, Cinemachine 3.1.7.
- **Vehicle HUD Cutscene Suppression Architecture**:
  - `VehicleDrivingHUD.cs`: Presenter maintaining `_isDrivingActive` and `ShouldBeVisible() => _isDrivingActive && !IsCutsceneActive()`. Subscribes to `ICutsceneService.OnCutsceneStarted`/`OnCutsceneEnded` and provides `SnapHidden()` to immediately zero alpha and disable Canvas.
  - `ArcadeVehicleController.cs`: Adapter guarding `VehicleDrivingHUD.Instance.Show()` and telemetry updates with `!isCutscenePlaying`.
  - `OpeningCutsceneController.cs`: Orchestrator deactivating `VehicleDrivingCanvas` inside `SetGameplayHUDVisible(false)`.
- **Night Day 1 Cinematic Cutscene Architecture**:
  - `Night1CutsceneController.cs`: Implements `ICutsceneService`. Intercepted in `NightBrawlManager.StartNightBrawl()` on Day 1 when `Night1CutsceneCompleted == 0`, seamlessly playing after `DayTransitionUI` completes.
  - Presentation & Letterboxing: Uses `CutsceneLetterboxUI` (2.39:1 widescreen bars, typography subtitle plates, hold-to-skip circular gauge).
  - Multi-Shot Choreography:
    - Shot 1 (Farmland Plots, `(31.2, 3.2, 29.5)`): Ground tremors and crop shaking.
    - Shot 2 (Emergence Point): Procedural soil burst particle VFX, `TuberMaw` sweet potato mutant eruption.
    - Shot 3 (Front Gate, `(21.9, 4.0, 45.0)`): Night mist & approaching silhouettes outside perimeter.
    - Shot 4 (Ucup Reaction): Framed on player character readying weapon, delivering canonical dialogue matching `StoryDatabase.D1_NIGHT_START`.
  - Clean Transition & Skip: Instant `[ESC]` or 0.5s hold `[SPACE]`, snaps camera to `CameraMode.Gameplay`, releases input locks, sets `Night1CutsceneCompleted = 1`, and begins Wave 1 combat cleanly.
- **Night Brawl Spacing & Pacing Architecture**:
  - `NightBrawlManager.cs`: Tuned staggered arrival intervals `spawnIntervalMin = 6f` and `spawnIntervalMax = 10f` (shortened from legacy 15s–20s). Includes `ValidateSpawnIntervals()` in `Awake()` and `OnValidate()` for defense-in-depth auto-migration.
  - Scene assets (`StorylinePrototypeSandbox.unity`, `StagingScene.unity`): Re-serialized with 6 and 10 values.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Vehicle HUD Cutscene Suppression Presenter | `VehicleDrivingHUD` cutscene state listener, `SnapHidden()`, and visibility gating | M1 | Survey |
| 2 | ArcadeVehicleController Cutscene Guard | Guard `Show()` and telemetry updates during active cutscenes | M1 | Survey |
| 3 | Opening Cutscene Canvas Deactivation | Explicit suppression of `VehicleDrivingCanvas` in `OpeningCutsceneController` | M1 | Survey |
| 4 | Night Brawl Spawn Interval Tuning | Update `spawnIntervalMin` to 6f and `spawnIntervalMax` to 10f in `NightBrawlManager` | M1 | Survey |
| 5 | Spawner Auto-Migration & Validation | `ValidateSpawnIntervals()` in `Awake()`/`OnValidate()` and scene asset re-serialization | M1 | Survey |
| 6 | Night Day 1 Cutscene Controller | Implement `Night1CutsceneController` implementing `ICutsceneService` | M2 | Survey |
| 7 | Night Day 1 Trigger & Lifecycle | Intercept `NightBrawlManager.StartNightBrawl()` on Day 1 awaiting `DayTransitionUI` | M2 | Survey |
| 8 | Multi-Shot Cinematic Choreography | Farmland tremor, sweet potato mutant soil burst, gate focus, and Ucup reaction | M2 | Survey |
| 9 | Soil Burst Particle VFX & Animation | Procedural soil burst particle effect and mutant crop emergence scaling | M2 | Survey |
| 10 | Canonical Story Dialogue Presentation | Integrate `StoryDatabase.D1_NIGHT_START` dialogue through `CutsceneLetterboxUI` | M2 | Survey |
| 11 | Skip & Combat Hand-off State Machine | Seamless skip handling, camera restoration, input unlock, and combat launch | M2 | Survey |
| 12 | Vehicle HUD Cutscene Suppression Test | Automated audit test asserting Vehicle HUD suppression during cutscene and restoration | M3 | Survey |
| 13 | Night Day 1 Cutscene Execution & Skip Test | Automated audit test validating cutscene trigger, skip, camera handoff, and combat start | M3 | Survey |
| 14 | Spawner 6-10s Interval Assertion Test | Extend Test 24 with Part 5 asserting 6f/10f intervals and auto-migration | M3 | Survey |
| 15 | Full Test Suite 100% Pass Verification | Execute all 74+ audit tests in `GameplayAndHUDTests.cs` (0 failures, 0 regressions) | M3 | Survey |
| 16 | PlayMode Interactive Verification | Verify opening cutscene, driving HUD, Day 1 night transition, and combat transition in Unity | M3 | Survey |
| 17 | Zero-GC & Clean Architecture Audit | Independent verification of zero compilation errors, zero GC in update ticks, and clean code | M3 | Survey |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Vehicle HUD Cutscene Suppression & Spacing Tuning | Implement Vehicle HUD suppression across `VehicleDrivingHUD`, `ArcadeVehicleController`, and `OpeningCutsceneController`; tune `NightBrawlManager` spawn intervals to 6–10s with auto-migration | None | COMPLETED |
| M2 | Night Day 1 Cinematic Cutscene Implementation | Create `Night1CutsceneController`, integrate with `NightBrawlManager` and `CutsceneLetterboxUI`, choreograph 4 shots (plots, soil burst VFX, gate, Ucup dialogue), and implement seamless combat transition | M1 | COMPLETED |
| M3 | Comprehensive Automated Testing & Verification | Add audit tests in `GameplayAndHUDTests.cs` for R1, R2, and R3; run all tests (76 passing); review, challenge, and forensic audit | M2 | COMPLETED |

## Interface Contracts
### VehicleDrivingHUD ↔ ICutsceneService
- `bool IsCutsceneActive()`: Checks `ServiceLocator.Get<ICutsceneService>()?.IsCutscenePlaying ?? false`.
- `void SnapHidden()`: Instantly zeroes canvas group alpha and disables `_hudCanvas`.
- `void SetDrivingActive(bool active)`: Tracks whether the player is currently driving.

### Night1CutsceneController ↔ NightBrawlManager
- `void PlayNight1Cutscene(Action onComplete)`: Plays the in-engine cinematic sequence.
- Invokes `onComplete` callback when the sequence finishes or is skipped, immediately launching Wave 1.
- Implements `ICutsceneService` with `bool IsCutscenePlaying { get; }`.

### NightBrawlManager Interval Configuration
- `public float SpawnIntervalMin { get; }` (6.0f)
- `public float SpawnIntervalMax { get; }` (10.0f)
- `public void SetSpawnIntervals(float min, float max)`
- `public void ValidateSpawnIntervals()`: Clamps legacy values (e.g. 15s/20s) to [6f, 10f].

## Code Layout
- `Assets/Scripts/Features/Vehicles/UI/VehicleDrivingHUD.cs` — Vehicle HUD presenter
- `Assets/Scripts/Features/Vehicles/ArcadeVehicleController.cs` — Vehicle controller adapter
- `Assets/Scripts/Features/Storyline/Cutscenes/OpeningCutsceneController.cs` — Opening cutscene orchestrator
- `Assets/Scripts/Features/Storyline/Cutscenes/Night1CutsceneController.cs` — Night Day 1 cinematic cutscene controller
- `Assets/Scripts/Features/Storyline/Cutscenes/CutsceneLetterboxUI.cs` — Letterbox UI presenter
- `Assets/Scripts/Features/Combat/NightBrawlManager.cs` — Night combat manager and wave spawner
- `Assets/Scripts/Core/Contracts/INarrativeFactBoard.cs` — Story telemetry facts
- `Assets/Scripts/Editor/GameplayAndHUDTests.cs` — Automated test suite
