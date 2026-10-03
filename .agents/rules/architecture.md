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

### 4.1 Kitchen Stove Cooking Architecture (`KitchenStove.cs` & `StoveUIManager.cs`)
The cooking system is decoupled following a strict Backend Controller & View/Presenter architecture:

- **Backend Controller & State Machine (`KitchenStove.cs`)**:
  - **State Machine (`CookingState`)**: `Idle`, `Cooking`, `Completed`, `Cancelled`.
  - **State Transitions**: `Idle` → `Cooking` (on `StartCooking`) → `Completed` (on timer expiration) → `Idle`, or `Cooking` → `Cancelled` (on manual cancel or modal interruption) → `Idle`.
  - **Event-Driven Broadcaster**:
    - `event Action<CookingState> OnCookingStateChanged`
    - `event Action<KitchenRecipe, float> OnCookingStarted`
    - `event Action<float, float> OnCookingProgress`
    - `event Action<KitchenRecipe> OnCookingCompleted`
    - `event Action<KitchenRecipe> OnCookingCancelled`
  - **Transaction & Snapshot Safety**:
    - `CanCook(KitchenRecipe, InventoryComponent, out string failReason)`: Validates both ingredient counts in `InventoryComponent` and clean water levels in `PlayerWaterBottle.Instance`.
    - `StartCooking()`: Snapshots consumed item types/quantities and water volumes, deducts them immediately, transitions state to `Cooking`, and runs the countdown timer coroutine.
    - `CancelCooking(bool refundIngredients = true)`: Stops the timer, restores snapshotted ingredients to `InventoryComponent`, refunds water to `PlayerWaterBottle`, and fires cancellation events.
    - `Finalization`: On timer completion, adds `recipe.output` (`outputCount`) to `InventoryComponent`, clears transaction snapshots, and transitions to `Completed` then `Idle`.

- **View / Presenter (`StoveUIManager.cs`)**:
  - Acts strictly as a presentation layer and user action forwarder.
  - Contains **zero** local countdown coroutines and performs **zero** direct inventory mutations.
  - Subscribes to `KitchenStove` events on `Open()` and cleanly unsubscribes on `Close()` or `OnDestroy()`.
  - Updates progress bar and cook button countdown (`Cooking... (Xs)`) reactively via `OnCookingProgress`.
  - Automatically cancels cooking with full item/water refund if the panel is closed mid-cook.
  - Preserves ESC Modal Priority Stack via `MainMenuController.LastFrameUIPanelClosed` and handles cursor/input locking.

### 4.2 Item-Level Dynamic Washing (`KitchenSinkInteractable.cs`)
- Eliminates 1-to-1 recipe asset bloat for washing mechanics.
- `FoodItemData` and `MaterialItemData` expose `bool isDirty` and `ItemData cleanVariant`.
- The sink interactable creates runtime virtual recipes on the fly when dirty items are inserted.

### 4.3 Refrigerator (`RefrigeratorInteractable.cs`)
- Storage interactable with strict category gating: accepts only raw crops, meat, and prepared dishes.

---

## 5. Wardrobe & Customization Subsystem (`Features/Wardrobe/`)

The wardrobe subsystem integrates character customization with live in-world mirror reflection and dedicated camera orchestration:

- **WardrobeManager (`WardrobeManager.cs`)**:
  - Central controller managing wardrobe session lifecycle, player positioning, UI transitions, and outfit synchronization.
  - **Camera Switching**: Delegates mode transitions strictly through `CameraManager.Instance.SetMode(CameraMode.WardrobeMode, wardrobeRoot)`.
  - **Exact Player Placement**: Teleports and aligns the player character to exact coordinates facing the bedroom mirror:
    - Position: `(27.10, 0.04, 21.04)`
    - Rotation: `Euler(0, 180, 0)` (facing mirror at `(27.10, 1.25, 19.38)`)
    - Physics synchronization: Forces `rb.linearVelocity = Vector3.zero`, sets transform position/rotation, and calls `Physics.SyncTransforms()`.
  - **Chest Lid Animation**: Toggles `chestLidAnimator.SetBool("IsOpen", true/false)` to smoothly open the wardrobe chest on enter and close on exit.
  - **UI & Input Management**: Performs smooth CanvasGroup fading (`uiFadeDuration = 0.3f`), hides player hotbar container during interaction, and restores full movement input and cursor states upon exit with brute-force fallback guards.
- **MirrorCamera Subsystem (`MirrorCamera.cs`)**:
  - Dedicated off-screen camera rendering real-time reflections of the player and bedroom to a `RenderTexture` (`WardrobeMirrorTexture`, portrait aspect `0.5`).
  - **UniversalAdditionalCameraData Hardening**:
    - `renderType = CameraRenderType.Base`
    - `renderPostProcessing = false` (eliminates recursive post-processing passes and VRAM bloat)
    - `depth = -100` (renders before main camera passes so texture is ready for GBuffer/Forward passes)
    - AudioListener disabled by default to avoid duplicate listener warnings.
  - **Mirror Quad & Authentic Reflection**:
    - Quad surface renderer bound to `Mat_Mirror_WoodFrame.mat`.
    - Horizontal reflection flip applied via UV scale and offset: `_BaseMap_ST: (-1, 1, 1, 0)`.
    - Pure wood frame aesthetic (`_Metallic = 0`, `_Smoothness = 0.25`) with natural wood texturing.
  - **Performance Optimization**: MirrorCamera is disabled by default in gameplay; it is only activated while the player is inside WardrobeMode.
- **PlayerOutfit & OutfitPartResolver**:
  - `PlayerOutfit`: Applies meshes and materials to `SkinnedMeshRenderer` components based on `OutfitData`. Manages preview (`TryOn`), commit (`Commit`), and cancel/revert (`Revert`).
  - `OutfitPartResolver`: Pure C# logic mapping outfit categories (Top, Bottom, Shoes, Hat) and variant indices to sub-renderer game objects.

---

## 6. Camera & Occlusion System (`Features/Camera/`)

The camera system combines isometric tactical navigation with intelligent zero-GC wall and canopy occlusion fading:

- **IsometricCameraController (`IsometricCameraController.cs`)**:
  - Handles isometric perspective with smooth target follow, right-drag orbital rotation, and scroll zoom.
  - Encapsulated by a *Mode Guard*: runs strictly when `CameraManager.CurrentMode == Gameplay`.
- **WallOcclusionManager (`WallOcclusionManager.cs`)**:
  - Runs in `LateUpdate()` after camera positioning.
  - **Raycast Occlusion**: Casts multiple raycasts from the main camera to the player's head, center, and base using a pre-allocated `RaycastHit[32]` buffer.
  - **Camera Proximity Occlusion**: Uses `Physics.OverlapSphereNonAlloc` with `cameraProximityRadius = 2.0f` around the camera position (`Collider[16]` buffer) to detect penetrating tree branches, overhead eaves, or nearby foliage geometry with **zero runtime GC allocations**.
  - **Hierarchical Group Fading**: Resolves `WallOcclusionGroup` and `RoomOcclusionTrigger` to fade entire wall facades or roof segments together to prevent visual patchwork.
- **WallOccluder & Linked Additional Renderers (`WallOccluder.cs`)**:
  - Manages per-wall smooth transparency fading (`transparentAlpha = 0.15f`, `fadeSpeed = 4.8f`).
  - **Linked Additional Renderers (`additionalRenderers`, `hideAdditionalRenderersOnFade`)**:
    - Supports attached wall accessories (e.g., bedroom mirror quad, mirror wood frame, hanging lanterns, wall sconces, paintings).
    - When the south/front wall is occluding the player and fades to transparent, all linked renderers fade synchronously.
    - **Dynamic Texture & UV Preservation**: Automatically preserves dynamic textures (`RenderTexture` on mirror) and UV transforms (`GetTextureScale`, `GetTextureOffset`) onto runtime transparent materials, preventing lost reflection textures or incorrect UV flipping during occlusion.
    - **Instant Restoration (`ForceOpaque()`)**: Snaps wall and all linked renderers back to fully opaque instantly with zero transition latency during camera mode transitions (WardrobeMode / TrophyMode).

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

### 8.6 Post-Processing Volume Pipeline & Tactile Juice (`DayNightVolumeController.cs`)
- **Global Volume Hierarchy**: Located under `_LIGHTING/Global Volume` in `StagingScene.unity` with `isGlobal = true`.
- **Volume Profile Overrides**:
  - `Tonemapping`: ACES tonemapper for cinematic dynamic range.
  - `Bloom`: Subtle glow for emissive materials, lanterns, and rim lighting.
  - `ColorAdjustments`: Dynamic day/night contrast, saturation, and exposure compensation.
  - `MotionBlur`: High-quality camera translation smoothing.
  - `ChromaticAberration`: Base post-processing color fringing.
- **Responsive Tactile Combat Juice (`TriggerCombatImpulse`)**:
  - Chromatic aberration is not merely a static aesthetic pass; it functions as responsive game feel feedback.
  - `DayNightVolumeController.Instance.TriggerCombatImpulse(float duration, float maxIntensity)` dynamically spikes and eases chromatic aberration intensity during heavy combat impacts, critical hits, or boss spawns.
  - Zero-GC coroutine lifecycle using cached override references (`VolumeProfile.TryGet<ChromaticAberration>`).
- **Mirror Camera Post-Processing Exclusion (`MirrorCamera.cs`)**:
  - The wardrobe mirror secondary camera explicitly sets `renderPostProcessing = false` on its `UniversalAdditionalCameraData`.
  - Prevents recursive post-processing passes, eliminates VRAM bloat on render textures, and preserves mirror fidelity.

### 8.7 Foliage, Tree LOD & Camera Near-Clipping Architecture
- **Camera Near-Clip Precision Guard**:
  - `Camera.main.nearClipPlane` is configured to `0.08f` (avoiding sub-millimeter values `< 0.03f` that compromise 24-bit depth buffer precision and cause Z-fighting in Deferred+).
  - Overhead leaf near-plane slicing is prevented at the source through the combination of `0.08f` near plane and `WallOcclusionManager` camera proximity fading (`cameraProximityRadius = 2.0f`).
- **Smooth Tree LOD Transitions (`LODFadeMode.CrossFade`)**:
  - All compound and forest trees (`Tree_Stylized_Oak`, `Tree_Stylized_Pine`, etc.) utilize `LODGroup` with crossfading enabled.
  - Eliminates jarring geometric popping during camera orbit and zoom transitions.
- **Two-Sided Leaf Material Shading (`_Cull = 0`)**:
  - Foliage materials (`Mat_Tree_Leaf_Oak`, `Mat_Tree_Leaf_Pine`, `Mat_Bush_Leaf`) enforce two-sided rendering (`_Cull = 0`).
  - Eliminates black backfaces or invisible canopy holes when viewed from steep isometric overhead camera angles.

---

## 9. Combat & Night Brawl System Architecture (`Features/Combat/`)

The combat engine orchestrates wave-based monster survival encounters during the night phase:

```
                            ┌────────────────────────┐
                            │    TimeManager (Night) │
                            └───────────┬────────────┘
                                        │
                                        ▼
                            ┌────────────────────────┐
                            │   NightBrawlManager    │
                            │  (Wave Orchestrator)   │
                            └───────────┬────────────┘
                                        │
                  ┌─────────────────────┼─────────────────────┐
                  ▼                     ▼                     ▼
        ┌───────────────────┐ ┌───────────────────┐ ┌───────────────────┐
        │  Front Gate Left  │ │ Front Gate Center │ │ Front Gate Right  │
        │(16.5, 0.08, 53.0) │ │(21.0, 0.08, 55.0) │ │(25.5, 0.08, 53.0) │
        └─────────┬─────────┘ └─────────┬─────────┘ └─────────┬─────────┘
                  │                     │                     │
                  └─────────────────────┼─────────────────────┘
                                        ▼
                            ┌────────────────────────┐
                            │  NavMesh Snapped Spawn │
                            │ (1.2m Scatter Radius)  │
                            └───────────┬────────────┘
                                        │
                                        ▼
                            ┌────────────────────────┐
                            │    Compound Approach   │
                            │  (Road → Front Gate)   │
                            └────────────────────────┘
```

### 9.1 Front Gate 3-Point Spawn System
- Monsters spawn exclusively outside the compound perimeter fence in front of the main entrance gate (`Z > 52m`):
  - **Point 1 (Left Flank)**: `(16.5, 0.08, 53.0)`
  - **Point 2 (Center Road)**: `(21.0, 0.08, 55.0)`
  - **Point 3 (Right Flank)**: `(25.5, 0.08, 53.0)`
- **Scatter & NavMesh Snapping**:
  - `CalculateRandomSpawnPoint()` randomly picks one of the 3 points, applies a horizontal scatter offset (`spawnScatterRadius = 1.2f`), raycasts downward to locate surface elevation, and snaps to the nearest walkable `NavMesh` area (`NavMesh.SamplePosition`).
- **Compound & Interior Spatial Boundaries**:
  - Compound perimeter (homestead): `X: [0.0, 42.0], Z: [-17.0, 48.0]`.
  - House interior bounds: `X: [9.5, 31.5], Z: [4.5, 26.5]`.
  - If a monster somehow penetrates indoor space or skills attempt to move indoors, `GetNearestOutdoorPosition()` ejects them to the nearest exterior boundary.

### 9.2 Wave Progression Engine (`WaveProgressionEngine.cs`)
- Manages 5 distinct nightly encounter tiers:
  - **Day 1**: 1 Wave (introductory pack).
  - **Day 2**: 2 Waves.
  - **Day 3**: 3 Waves.
  - **Day 4**: 4 Waves (Boss introduction: Cyclops Tuber Maw + minions).
  - **Day 5**: 5 Waves (Climactic finale: 2 simultaneous bosses + heavy horde).
- Cleared encounter sets `TimeManager.Instance.isNightEncounterCleared = true`, unlocking bed sleep to advance to the next day.


