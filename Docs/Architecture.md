# Architecture.md

System design, module relationships, and technical architecture for the **Farm-Beware** project.

---

## 1. High-Level Architecture Overview

Farm-Beware is built on **Unity 6000.3.20f1** utilizing the **Universal Render Pipeline (URP)**, the **New Input System (`UnityEngine.InputSystem`)**, and **TextMeshPro (TMP)**.

The codebase follows a **Feature-Based Modular Architecture** under `Assets/Scripts/Features/`, isolating domain logic into decoupled systems with plain C# logic decoupled from presentation layer `MonoBehaviour` adapters.

```
                              ┌─────────────────────────────┐
                              │     GameInitializer         │
                              └──────────────┬──────────────┘
                                             │
               ┌─────────────────────────────┼─────────────────────────────┐
               ▼                             ▼                             ▼
    ┌──────────────────────┐      ┌──────────────────────┐      ┌──────────────────────┐
    │    CameraManager     │      │     TimeManager      │      │     ItemDatabase     │
    │ (State Machine Cam)  │      │ (Day/Night Cycles)   │      │ (Central Registry)   │
    └──────────┬───────────┘      └──────────┬───────────┘      └──────────┬───────────┘
               │                             │                             │
       ┌───────┴───────┐             ┌───────┴───────┐             ┌───────┴───────┐
       ▼               ▼             ▼               ▼             ▼               ▼
   Gameplay Cam   Mode Cam       Day Phase      Night Phase     Player/Chest     Kitchen/
   (Follow/Zoom) (Trophy/Ward)  (Farm/Cook)    (Brawl Waves)     Inventory       Crafting
```

---

## 2. Core State Machines & Centralized Managers

### 2.1 CameraManager (`Features/Camera/CameraManager.cs`)
The centralized single source of truth for all camera behaviors and transitions:
- **Camera Modes (`CameraMode`)**:
  - `Gameplay`: Enables `IsometricCameraController` for free-roam player navigation.
  - `TrophyMode`: Locks movement and anchors the FP camera in front of the trophy rack.
  - `WardrobeMode`: Positions camera facing the wardrobe and activates mirror reflection rendering.
- **Responsibilities**:
  - Validates mode transitions (prevents illegal jumps between interactive modes without returning to `Gameplay`).
  - Controls player input lock via `PlayerControl.isInputLocked`.
  - Manages cursor state (`Cursor.lockState` and `Cursor.visible`).
  - Fires `OnCameraModeChanged` events for UI and feature modules.

### 2.2 TimeManager (`Features/Time/TimeManager.cs`)
Discrete phase-based time progression manager:
- **Day Phase**: Farming, cooking, weapon upgrades, and combat preparation.
- **Night Phase**: Wave-based monster combat (Night Brawl).
- **Transitions**: Sleeping on the bed (`BedInteractable`) acts as the primary game-loop progression gate via `AdvanceToNextDay()`.

### 2.3 ItemDatabase (`Features/Inventory/Data/ItemDatabase.cs`)
Runtime central item registry loaded from `Resources/Database/ItemDatabase`:
- Indexes 33 active items into a fast string lookup dictionary (`itemLookup`).
- Groups items into categories: `Food`, `Crop`, `Seed`, `Material`, `MonsterDrop`, `Equipment`, and `Trophy`.

---

## 3. Inventory & Storage Architecture (`Features/Inventory/`)

### 3.1 InventoryComponent (`InventoryComponent.cs`)
Core backend component attached to the Player, Test Chest, Refrigerator, and Kitchen Sink:
- **Data Model**: `List<InventorySlot>` storing `ItemData` references and integer quantities.
- **Operations**: `AddItem()`, `RemoveItem()`, `SwapSlots()`, `MoveItemToSlot()`, `TransferItemTo()`.
- **Backend Rules (`CanAcceptItem()`)**:
  - `blockTrophyItems`: Prevents trophy-tier items from entering player storage (trophies belong on the trophy rack/cabinet).
  - `allowedFoodCategories`: Restricts accepted food categories (used by the Refrigerator for produce/meat).
- **Reactive Events**: Triggers `OnInventoryChanged` and `OnHotbarSelected` for stateless UI rendering.

### 3.2 Hotbar System
- The first 4 slots of the player's inventory represent the active hotbar.
- Hotbar selection is driven by number keys `1-4` or mouse scroll.
- `PlayerEquipment` subscribes to hotbar updates and dynamically instantiates or parents 3D item models into the player's hand.

---

## 4. Kitchen & Cooking System (`Features/Kitchen/`)

### 4.1 Genshin-Style Cooking (`GenshinStove.cs` & `StoveUIManager.cs`)
- Replaces legacy slow tick-based cooking with instant, recipe-driven craft interactions.
- Evaluates `KitchenRecipe` ScriptableObjects supporting multiple ingredients (`RecipeIngredient`).
- Validates ingredient availability via `CountItem()`, consumes inputs via `RemoveItem()`, and yields output via `AddItem()`.
- UI utilizes a **Single Central Axis** layout powered by TextMeshPro.

### 4.2 Item-Level Dynamic Washing (`KitchenSinkInteractable.cs`)
- Eliminates 1-to-1 recipe asset bloat for washing mechanics.
- `FoodItemData` and `MaterialItemData` expose `bool isDirty` and `ItemData cleanVariant`.
- The sink interactable creates runtime virtual recipes on the fly when dirty items are inserted.

### 4.3 Refrigerator (`RefrigeratorInteractable.cs`)
- Storage interactable with strict category gating: accepts only raw crops, meat, and prepared dishes.

---

## 5. Wardrobe & Customization (`Features/Wardrobe/`)

- **PlayerOutfit**: Applies mesh/material configurations to character `SkinnedMeshRenderer` components based on `OutfitData`.
- **OutfitPartResolver**: Pure C# logic mapping outfit categories and variants to sub-renderer names.
- **MirrorCamera**: Dedicated off-screen camera rendering the bedroom mirror in real time to a `RenderTexture`.

---

## 6. Camera & Occlusion System (`Features/Camera/`)

- **IsometricCameraController**:
  - Handles isometric perspective with smooth target follow, right-drag orbital rotation, and scroll zoom.
  - Encapsulated by a *Mode Guard*: runs strictly when `CameraManager.CurrentMode == Gameplay`.
- **WallOcclusionManager & WallOccluder**:
  - Raycasts from the main camera to the player position.
  - Dynamically fades obscuring wall meshes to `alpha = 0.15` via material parameter swapping.

---

## 7. UI & Modal Navigation Stack

### 7.1 Canvas Scaler Configuration
- Reference Resolution: **1920 × 1080**
- Scale Mode: **Scale With Screen Size**
- Screen Match Mode: **Match Width Or Height (0.5)** for consistent rendering across 16:9, 16:10, and ultrawide displays.

### 7.2 ESC Modal Priority Stack
To prevent conflicts between closing modals and opening the pause menu:
1. **Priority 1 (Interactive Modal Panels)**: If any panel is active (`Panel_Stove`, `Panel_Sink`, `WardrobeUI`, `InventoryUI`, `ChestUI`), the first `ESC` press closes the panel and restores gameplay input.
2. **Priority 2 (Pause Menu)**: `PauseMenuUI` intercepts `ESC` strictly when all gameplay modal panels are confirmed closed.

---

## 8. Graphics, Lighting & Rendering Architecture

Farm-Beware features a modern high-fidelity rendering pipeline tailored for Unity 6000.3.20f1 URP:

```
                            ┌─────────────────────────────────────────┐
                            │    Render Pipeline Asset: PC_RPAsset    │
                            │  (RenderingMode: DeferredPlus, BRG: On) │
                            └────────────────────┬────────────────────┘
                                                 │
                   ┌─────────────────────────────┼─────────────────────────────┐
                   ▼                             ▼                             ▼
        ┌─────────────────────┐       ┌─────────────────────┐       ┌─────────────────────┐
        │     DEFERRED+       │       │ GPU RESIDENT DRAWER │       │    LIGHT LAYERS     │
        │  Cluster Light Loop │       │ BatchRendererGroup  │       │  Exterior/Interior  │
        │  100+ Point Lights  │       │ Static Batch Guard  │       │  Bitwise Isolation  │
        └──────────┬──────────┘       └──────────┬──────────┘       └──────────┬──────────┘
                   │                             │                             │
                   └─────────────────────────────┼─────────────────────────────┘
                                                 ▼
                               ┌──────────────────────────────────┐
                               │       CUSTOM SHADER PIPELINE     │
                               ├──────────────────────────────────┤
                               │ • MonsterFresnelLit (ForwardOnly)│
                               │ • DitheredBuildingLit (Bayer 4x4)│
                               │ • WorldSpaceVisionMask (Blit)    │
                               └──────────────────────────────────┘
```

### 8.1 Rendering Path & GPU Submission
- **Deferred+ Clustering**: Configured on `PC_Renderer.asset` (`RenderingMode.DeferredPlus`). Employs a 3D clustered tile grid for lighting calculations, allowing dozens of concurrent point and spot lights (lanterns, monster spells, house lights) with negligible per-light draw call overhead.
- **GPU Resident Drawer (BRG)**: `UniversalRenderPipelineAsset.gpuResidentDrawerMode` is set to `InstancedDrawing`. Mesh and transform data reside persistently in GPU VRAM.
- **Static Batching Guard**: Legacy Unity Static Batching is explicitly disabled on Standalone platforms via `URPGraphicsConfigurationValidator.cs` to prevent CPU RAM duplication and fragmented draw batches that defeat BRG instancing.

### 8.2 Atmosphere & Time-of-Day Lighting Pipeline
- **Event-Driven Observers**: Lighting transitions do NOT execute in `Update()`. Controllers subscribe directly to `TimeManager.Instance.OnPhaseChanged`.
- **Day Phase (`DaySunLightingObserver.cs`, `DayNightLightingController.cs`)**:
  - High sun elevation ($50^\circ$), color progression from cool noon (5500K) to warm dusk (3200K).
  - Orthographic soft shadows with Camera-Relative Culling enabled.
  - Adaptive Probe Volumes (APV) support with real-time Sky Occlusion.
- **Night Phase (`HouseSafeZoneLighting.cs`, `NightBrawlManager.cs`)**:
  - Sun dims into a cool moonlight fill ($0.1–0.2\text{ lux}$, soft bluish spectrum `(0.72, 0.82, 0.96)`).
  - Safe-zone amber point lights automatically ignite around player structures.

### 8.3 Combat Readability: Monster Fresnel Shading (`MonsterFresnelLit.shader`)
- Specially tailored for dark isometric combat environments.
- Evaluates a grazing-angle Fresnel equation in world space: $\text{Rim} = \text{pow}(1 - (\mathbf{N}_{WS} \cdot \mathbf{V}_{WS}), \text{Power}) \times \text{Intensity}$.
- **Deferred+ Pass Mandate**: Utilizes `Tags { "LightMode" = "UniversalForwardOnly" }`. In Deferred+ mode, standard `UniversalForward` passes on opaque objects are omitted by the GBuffer pass; using `UniversalForwardOnly` guarantees the monster's opaque mesh is rendered on top of Deferred buffers while preserving full `ShadowCaster` projections.

### 8.4 Interiors & Screen-Door Transparency (`DitheredBuildingLit.shader`)
- **Strict Opaque Tagging**: Retains `RenderType = "Opaque"`, `Queue = "Geometry"`, and `ZWrite On`. Traditional Alpha Blending is strictly prohibited to preserve depth prepass integrity, Z-sorting with furniture, and physical shadow maps.
- **Normalized Bayer $4 \times 4$ Dither Matrix**: Screen-space dither discard operation based on `fmod(abs(screenPos.xy), 4.0)` compared against `(1.0 - _DitherFade)`.
- **Consistent Shadow Pass**: The dither clip function is mirrored identically in the `ShadowCaster` pass, ensuring exterior building silhouettes continue casting accurate shadows even when roofs fade for the camera.
- **HouseInteriorTrigger (`HouseInteriorTrigger.cs`)**: Interpolates `_DitherFade` using `MaterialPropertyBlock` instances to prevent material cloning in RAM.

### 8.5 Light Layers Segregation (`LightLayerAssignmentUtility.cs`)
- Prevents light leakage between indoor and outdoor environments without expensive raytracing:
  - **Light Layer 0 (Exterior)**: Directional Light (Sun/Moon), outdoor terrain, crops, fencing.
  - **Light Layer 1 (Interior)**: `InteriorLamp_*`, `Light_Interior_*`, indoor floor meshes, indoor furniture.
  - **Light Layer 0+1 (Transition)**: Door frames, entry thresholds.
- Accessible via Editor Window: `Tools > Farm-Beware > Rendering > Light Layer Assignment Utility`.

