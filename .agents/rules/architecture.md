# Architecture.md

System design, module relationships, and technical architecture for **Farm-Beware**.

---

## 1. Engine & Modular Architecture Overview

- **Engine**: Unity 6000.3.20f1 (Unity 6), URP Deferred+, GPU Resident Drawer (BRG `InstancedDrawing`), New Input System, AI Navigation 2.0.13, Cinemachine 3.1.7.
- **Pattern**: Separation between **Pure C# POCO Logic** (zero allocation) and **Thin MonoBehaviour Adapters** (lifecycle, input, rendering).
- **Core Modules** (`Assets/Scripts/Features/`):
  - `Camera/`: `CameraManager` authority, `IsometricCameraController` (ortho size 6, pitch 30°, yaw 45°), `MirrorCamera`.
  - `Combat/`: Night brawl wave engine, POCO combat FSM, poise tracking, token dispatching, dual-tier UI.
  - `Economy/`: `DailyEconomyManager` price rolling, `TradingPost_ShopCounter`, `ShopInteractable`, `MerchantShopUI`, `PlayerWallet`.
  - `Farming/`: `FarmGridManager` (16 plots), `CropData` (Sweet Potato, Taro, Corn), moisture decay, clean/dirty yields, `WorldItemPickup`.
  - `Inventory/`: 16-slot inventory (4 hotbar + 12 main), registry (`ItemDatabase`), storage chest.
  - `Kitchen/`: Cooking FSM, atomic transaction rollback, washing sink, `PlayerWaterBottle`.
  - `Persistence/`: Multi-slot JSON serialization (`GameData`), auto-save triggers, hydration.
  - `Storyline/`: `StorylineManager`, `NarrativeFactBoard`, cutscene controllers (`Opening`, `Night1`), letterbox UI, `QuestManager`, `QuestHUDUI`.
  - `Time/`: Day/night phase cycles, `DayTransitionUI` banners, triple clock (60s/30s/15s), bed sleep gating.
  - `Vehicles/`: `ArcadeVehicleController`, `VehicleDynamicsCore`, entry/exit, driver sync, `VehicleDrivingHUD`.
  - `Wardrobe/`: Character outfit customization, secondary mirror camera, occlusion.

---

## 2. Core Subsystems & Managers

- **CameraManager & IsometricCameraController**: Orthographic projection (`pitch: 30°`, `yaw: 45°`, offset `[-10, 10, -10]`) viewing SW to NE. Controls modes (`Gameplay`, `TrophyMode`, `WardrobeMode`) and input locking.
- **DayNightTimeManager**: Coordinates day/night loop. Sleep (`BedInteractable`) unlocks strictly after wave clear (`isNightEncounterCleared = true`).
- **StorylineManager & FactBoard**: Pure logic narrative state machine, FIFO pending modal queue, timeline cutscenes.
- **DailyEconomyManager**: Daily fluctuating market prices, dirty produce discount (50%), day transition snapshots.
- **SaveLoadService**: 3-slot JSON save architecture (`GameData`) persisting stats, inventory, equipment, soil plots, facts, day count.

---

## 3. Western Environment & 3D Isometric Staging

- **Western Scenic Roadway (`Road_Homestead_To_TradingPost`)**:
  - Catmull-Rom paved ribbon with dual gravel shoulders from Homestead Gate (`X = 20.4`), traveling West along $Z = 53.5$ past west fence ($X < -0.6$), through 225m of pine foothills to Valley Trading Post at `(-175.0, 0.0, 70.0)`.
  - **Zero-Snag Invariant**: Ribbon, shoulder, and apron colliders disabled so vehicle physics ride on native `TerrainCollider`.
- **Trading Post Complex (`TradingPost_Complex`)**:
  - Located at `(-175.0, 0.0, 70.0)`.
  - **Isometric Depth Staging**: Lodge, ridge beam, and cupola sit in background (North, $+Z = 70$), while veranda boardwalk and shop counter face South into camera lens ($Z = 63.5$) with zero occlusion. Merchant stands behind counter at `Z = 64.25`.

---

## 4. Graphics, Lighting & Boundaries

- **URP Deferred+ & BRG**: Clustered lighting. Static batching disabled to preserve GPU Resident Drawer memory.
- **Shaders**:
  - `MonsterFresnelLit.shader`: `UniversalForwardOnly` with instanced `_HitFlashAmount` in `MaterialPropertyBlock`.
  - `DitheredBuildingLit.shader`: Bayer 4x4 screen-space dither, `RenderType = Opaque`, `ZWrite On`.
- **Light Layers**: Exterior Sun/Moon on Layer 0; Interior lamps on Layer 1.
- **NavMesh Carving**: Campfire, Pond, Farmland Plots, and Trading Post Building carved non-walkable.
