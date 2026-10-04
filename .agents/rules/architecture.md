# Architecture.md

System design, module relationships, and technical architecture for the **Farm-Beware** project.

---

## 1. High-Level Architecture Overview

Farm-Beware is built on **Unity 6000.3.20f1 (Unity 6)** using the **Universal Render Pipeline (URP Deferred+)**, the **New Input System (`UnityEngine.InputSystem`)**, and **TextMeshPro (TMP)**.

The codebase follows a **Feature-Based Modular Architecture** under `Assets/Scripts/Features/`, isolating domain logic into decoupled systems with plain C# POCO backend logic separated from `MonoBehaviour` presentation adapters:
- `Features/Camera/`: Camera orchestration, mode transitions, and camera-wall occlusion.
- `Features/Combat/`: Night brawl wave engine, melee combat state machine, and enemy health bar UI.
- `Features/Inventory/`: Data-driven inventory component, hotbar integration, and item registry.
- `Features/Kitchen/`: Stove cooking state machine, dynamic sink washing, and refrigerator storage.
- `Features/Persistence/`: Multi-slot game data serialization, auto-saving, and runtime state hydration.
- `Features/Time/`: Day/night phase cycles, lighting transitions, and sleep progression.
- `Features/Wardrobe/`: Character outfit customization, mirror reflection camera, and chest animations.

---

## 2. Core Managers & Game Foundation

### 2.1 CameraManager (`Features/Camera/CameraManager.cs`)
Single source of truth for all camera behaviors and state transitions:
- **Modes (`CameraMode`)**: `Gameplay` (free isometric follow), `TrophyMode` (fixed shelf inspect), and `WardrobeMode` (mirror preview).
- **Responsibilities**: Validates transitions, toggles camera activation, manages `PlayerControl.isInputLocked`, controls `Cursor.lockState`/`Cursor.visible`, and broadcasts `OnCameraModeChanged`.

### 2.2 DayNightTimeManager (`Features/Time/DayNightTimeManager.cs`)
Coordinates day/night game loop progression:
- **Day Phase**: Free exploration, farming, cooking, and preparation.
- **Night Phase**: Wave-based combat (`NightBrawlManager`).
- **Sleep Gate**: Bed interaction (`BedInteractable.AdvanceToNextDay()`) unlocks only when night waves are cleared (`isNightEncounterCleared = true`).

### 2.3 ItemDatabase (`Features/Inventory/Data/ItemDatabase.cs`)
Central item registry loaded from `Resources/Database/ItemDatabase`:
- Indexes active items in a fast string lookup dictionary (`itemLookup`).
- Categories: `Food`, `Crop`, `Seed`, `Material`, `MonsterDrop`, `Equipment`, `Trophy`.

### 2.4 Save & Persistence System (`Features/Persistence/`)
Multi-slot JSON persistence architecture (`SaveLoadService`, `GameData`, `MultiSlotSaveLoadManager`):
- Supports 3 distinct save slots with metadata (day count, playtime, timestamp).
- Persists player stats, equipment, inventory slots, hotbar state, farm plot states, and day progression.
- Handles atomic serialization and safe deserialization on scene boot.

---

## 3. Combat & Night Brawl Engine (`Features/Combat/`)

### 3.1 NightBrawlManager & Dynamic Monster Spawner
Orchestrates wave-based survival encounters during the night phase:
- **Dynamic Spawn Hierarchy**: Gathers all active child transforms under `MonsterSpawnPoints` (`SpawnPoint_FrontGate_Left`, `SpawnPoint_FrontGate_Center`, `SpawnPoint_FrontGate_Right`) placed outside the front entrance (`Z >= 52m`).
- **Surface Snapping**: Randomly selects a point, applies horizontal scatter (`1.0m`), raycasts surface elevation, and snaps to walkable NavMesh within 2.5m.
- **Indoor Ejection Boundary**: Enforces compound bounds (`X: [0, 42], Z: [-17, 48]`) and house interior bounds (`X: [9.5, 31.5], Z: [4.5, 26.5]`), ejecting penetrating monsters to exterior ground.
- **Wave Progression (`WaveProgressionEngine`)**: Days 1–3 introduce increasing hordes; Day 4 introduces Cyclops Tuber Maw boss; Day 5 unleashes a dual-boss climax (Cyclops + The Ranger).

### 3.2 Melee Combat State Machine (`PlayerControl` & `MeleeCombatStateMachine`)
Decoupled melee execution supporting responsive combos and attacks:
- **3-Hit Combo**: Smooth 3-stage light attack combo with configurable reset window (`ComboResetWindow = 1.5f`) and early cancel threshold (`minLock = 0.20f`).
- **Attack Buffering**: Queues attack inputs pressed during active attack animation (`bufferedAttackTime <= 0.45f`) and consumes them instantly upon hitting the combo branch window.
- **Special Attacks**: Sprint Dash Attack (instant forward sweep while sprinting), Charged Heavy Attack (hold attack > 0.35s), Kick (`F` key, high knockback), and Jump Skill Attack (`Space` + attack).

---

## 4. Combat UI Subsystem (`Features/Combat/UI/`)

### 4.1 Floating Overhead Enemy Bar (`EnemyOverheadBarUI` & `EnemyHealthBarManager`)
- Individual health bars hovering above active monsters with zero-GC object pooling (20 widgets).
- **Ghost Damage Bar**: Amber lag fill delays 0.22s then lerps downward to indicate damage impact.
- **Dynamic Head Offset**: Calibrates bar elevation per monster scale (Tuber Maw 1.15m, Corn Musketeer 1.65m, Taro Brute 2.25m).
- **Camera Frustum Culling**: Hides bars outside camera view or when monster is at 100% HP outside combat.

### 4.2 Top Cinematic Boss Health Bar (`BossHealthBarSlotUI` & `BossHealthBarManager`)
- Top-center HUD frame displaying boss name and thematic subtitle.
- **Multi-Boss Support**: Dynamically adapts between Single Boss layout (560px width) and Dual-Boss Stacked layout (480px width) for Day 5 climax.
- **Phase & Defeat**: Enrage phase divider (e.g., 50% for The Ranger) and animated `DEFEATED` badge transition upon death.

---

## 5. Navigation & Spatial Boundaries

- **Bake Configuration**: Standard baked `NavMesh` with agent radius 0.5m and height 1.8m.
- **Strict Non-Walkable Zones**:
  - `Campfire` (`(24.58, 0.40, -10.01)`): Excluded from NavMesh via carved non-walkable bounds.
  - `Natural Pond` (`(35.53, 0.40, 5.09)`): NavMesh obstacle carved to prevent water pathing.
  - `Farmland Soil Plots` (`(6.0, 0.0, 1.0)` & `(6.0, 0.0, -11.0)`): Excluded from navigation to prevent monster and player path-finding clipping through planted tilled crops.

---

## 6. Kitchen & Cooking Subsystem (`Features/Kitchen/`)

### 6.1 KitchenStove & StoveUIManager
- **State Machine (`CookingState`)**: `Idle` → `Cooking` → `Completed` / `Cancelled`.
- **Atomic Transactions & Rollback**:
  - Validates ingredients in `InventoryComponent` and clean water in `PlayerWaterBottle`.
  - Consumes and snapshots ingredients immediately on `StartCooking()`.
  - If cancelled or UI closed prematurely, `CancelCooking(refundIngredients: true)` performs a full atomic rollback.
  - On timer completion, outputs cooked dish and clears snapshot.
- **Decoupled View**: `StoveUIManager` is a stateless presenter subscribing to stove C# events.

### 6.2 Dynamic Washing (`KitchenSinkInteractable`)
- Embedded transformation logic in `FoodItemData` / `MaterialItemData` (`isDirty`, `cleanVariant`).
- Dynamic virtual recipes created on demand without asset duplication.

---

## 7. Wardrobe, Mirror & Occlusion (`Features/Wardrobe/` & `Features/Camera/`)

### 7.1 Wardrobe & MirrorCamera Subsystem
- **WardrobeManager**: Positions player at `(27.10, 0.04, 21.04)`, `Euler(0, 180, 0)` facing mirror; drives chest lid animator.
- **MirrorCamera**: Dedicated secondary camera rendering to `RenderTexture` (`WardrobeMirrorTexture`, portrait 0.5 aspect, `depth = -100`, `renderType = Base`, `renderPostProcessing = false`).
- **Surface Material**: `Mat_Mirror_WoodFrame.mat` with horizontal flip `_BaseMap_ST: (-1, 1, 1, 0)`.

### 7.2 WallOcclusionManager & WallOccluder
- **Raycast & Non-Alloc Sphere**: Casts rays from camera to player and uses `Physics.OverlapSphereNonAlloc` (`cameraProximityRadius = 2.0f`) to detect occluding walls and foliage with **zero runtime GC**.
- **Linked Renderers (`additionalRenderers`)**: Synchronously fades wall-mounted accessories (bedroom mirror, frame, lamps) preserving dynamic textures and UV transforms.

---

## 8. Graphics, Lighting & URP Deferred+ Pipeline

### 8.1 Rendering Path & Instancing
- **Deferred+ Clustering**: Configured on `PC_Renderer.asset` for low-overhead multi-light evaluation.
- **GPU Resident Drawer (BRG)**: `gpuResidentDrawerMode: InstancedDrawing` with persistent GPU transforms.
- **Static Batching Guard**: Disabled on standalone builds to prevent CPU RAM duplication.

### 8.2 Lighting & Custom Shaders
- **Monster Readability (`MonsterFresnelLit.shader`)**: Evaluates world-space grazing Fresnel. Enforces `Tags { "LightMode" = "UniversalForwardOnly" }` so opaque meshes render cleanly on top of Deferred+ buffers.
- **Interiors & Dither Transparency (`DitheredBuildingLit.shader`)**: Uses Bayer $4 \times 4$ screen-space dither discard with `RenderType = "Opaque"`, `Queue = "Geometry"`, and `ZWrite On` to preserve depth prepass and shadow casting.
- **Light Layer Segregation**:
  - `Light Layer 0 (Exterior)`: Directional Light (Sun/Moon), terrain, outdoor foliage.
  - `Light Layer 1 (Interior)`: Indoor lamps, interior floor, furniture.
- **Post-Processing Juice (`DayNightVolumeController`)**:
  - Global Volume with ACES Tonemapping, Bloom, Motion Blur, and Chromatic Aberration.
  - `TriggerCombatImpulse(duration, intensity)`: Dynamically spikes chromatic aberration on heavy hits or boss spawns.

---

## 9. UI & Modal Navigation Stack

- **Canvas Scaler**: Reference resolution `1920 × 1080`, Match Width Or Height `0.5`.
- **ESC Modal Priority Stack**:
  1. Priority 1 (Gameplay Modals): Closes active panel (`Panel_Stove`, `WardrobeUI`, `InventoryUI`, `ChestUI`) and restores gameplay input.
  2. Priority 2 (Pause Menu): `PauseMenuUI` intercepts `ESC` strictly when all modals are closed.
