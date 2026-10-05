# Architecture.md

System design, module relationships, and technical architecture for **Farm-Beware**.

---

## 1. High-Level Engine & Architecture Overview

- **Engine**: Unity 6000.3.20f1 (Unity 6), URP Deferred+, GPU Resident Drawer (BRG `InstancedDrawing`), New Input System, TextMeshPro.
- **Pattern**: Strict separation between **Pure C# POCO Logic** (zero Unity runtime allocation) and **Thin MonoBehaviour Adapters** (lifecycle, input, rendering).
- **Core Modules** (`Assets/Scripts/Features/`):
  - `Camera/`: Camera orchestration, mode transitions, and raycast/sphere occlusion.
  - `Combat/`: Night brawl wave engine, POCO combat FSM, poise tracking, token dispatching, and dual-tier UI.
  - `Inventory/`: Data-driven inventory, hotbar binding, item registry (`ItemDatabase`).
  - `Kitchen/`: Cooking FSM, atomic transaction snapshots with rollback, dynamic washing sink.
  - `Persistence/`: Multi-slot JSON serialization (`GameData`), auto-save triggers, and hydration.
  - `Time/`: Day/night phase cycles, celestial lighting, and bed sleep gating.
  - `Wardrobe/`: Character outfit customization, secondary mirror camera, and occlusion.

---

## 2. Core Managers & Foundation

- **CameraManager**: Sole authority for camera modes (`Gameplay`, `TrophyMode`, `WardrobeMode`). Manages `PlayerControl.isInputLocked`, cursor lock, and transitions.
- **DayNightTimeManager**: Coordinates day/night game loop. Sleep (`BedInteractable`) unlocks strictly after wave clear (`isNightEncounterCleared = true`).
- **ItemDatabase**: Registry indexing items into dictionary lookup. Enforces max stack ceiling (20 for stackables, 1 for equipment).
- **Persistence**: 3-slot JSON save architecture (`SaveLoadService`, `GameData`). Persists player stats, equipment, inventory, soil plots, and day count atomically.

---

## 3. Combat & Night Brawl Engine (`Features/Combat/`)

- **Pure POCO Combat Stack**:
  - `CombatStateEvaluator`: 3-hit light combo branching, heavy finishers, charged thrusts, dash sweeps, and cancel windows (`minLock = 0.20f`, `ComboResetWindow = 1.5f`). Reset hook `InterruptCombatSequence()` clears timestamps on defensive cancel.
  - `PoiseTracker`: Deterministic poise damage and multi-tier CC (`MicroStagger`, `HeavyStagger`, `Knockdown`). Enforces *Tier-Upgrade-Only rule* (weaker hits never cancel Knockdown) and auto-restores innate Super Armor upon full poise regeneration.
  - `AttackTokenDispatcher`: Anti-dogpiling permit manager (3 melee, 2 ranged). Priority tiers (`Minion = 0, Elite = 1, Boss = 2`) with preemptive eviction to prevent boss starvation.
  - `DefenseEvaluator`: Pure defense evaluator for dodge i-frames (300ms) and precision parries (350ms). Enforces 350ms whiff recovery lockout to eliminate parry spamming.
- **Thin Engine Adapters**:
  - `HitstopCoordinator`: Zero-GC ring registry for micro-freezes. Safely samples NavMesh before warping to prevent console exceptions.
  - `AirborneHazardEntity`: CCD continuous dynamic collision for launched enemies, predictive boundary raycasts, and NavMesh ground snapping.
  - `NightBrawlManager`: Front-gate dynamic spawner (`Z >= 52m`), exterior ejection boundaries, and 5-day wave progression.

---

## 4. Graphics, Lighting & Boundaries

- **URP Deferred+ & BRG**: `PC_Renderer.asset` clustered lighting. Static batching disabled to preserve GPU Resident Drawer memory.
- **Shaders**:
  - `MonsterFresnelLit.shader`: Tagged `UniversalForwardOnly`. Uses `#pragma multi_compile_instancing` and `UNITY_INSTANCING_BUFFER_START(Props)` for `_HitFlashAmount` to preserve BRG batching with `MaterialPropertyBlock`.
  - `DitheredBuildingLit.shader`: Bayer 4x4 screen-space dither, `RenderType = Opaque`, `ZWrite On` to preserve depth prepass and shadow maps.
- **Light Layers**: Exterior Sun/Moon on Layer 0; Interior lamps on Layer 1.
- **NavMesh Carving**: Campfire, Natural Pond, and Farmland Soil Plots carved out as non-walkable.
