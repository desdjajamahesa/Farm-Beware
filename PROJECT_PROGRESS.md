# Farm-Beware Project Progress Documentation
## Session: Wall Transparency/Occlusion + Interaction Distance Tuning
## Last Updated: 2025-08-23
## Unity Version: 6000.3.20f1 (Unity 6)
## Scene: StagingScene (Assets/Scenes/StagingScene.unity)

---

## 📋 PROJECT OVERVIEW

**Repository**: Farm-Beware (GitHub: desdjajamahesa/Farm-Beware, branch: rafi-branch)
**Local Path**: C:\Users\HP\Rafi\MyProject\Farm-Beware
**Unity Project**: C:\Users\HP\Rafi\MyProject\Farm-Beware
**Active Scene**: StagingScene (Assets/Scenes/StagingScene.unity)

**Main Camera**: Isometric (orthographic, follows player with offset)
**Player**: "Player" GameObject with PlayerControl, Inventory, Wardrobe, etc.
**Render Pipeline**: URP (Universal Render Pipeline) 17.3.0

---

## 🎮 CURRENT SCENE HIERARCHY (StagingScene)

### Root GameObjects (15 total)
```
Main Camera          - Orthographic isometric camera, IsometricCamera script
Directional Light    - Main light source
Global Volume        - URP Volume for post-processing
Player               - Player character (Asep model), 2 children
GameManager          - GameInitializer, TimeManager
Bedroom              - Room container (9 children)
    ├── Floor
    ├── Wall (8 wall segments with occlusion system)
    ├── Bed
    ├── CarpetCircle
    ├── CarpetSquare
    ├── Chair
    ├── Chest (wardrobe access point)
    ├── Table
    └── TrophyCabinetSystem
InventoryManager     - InventoryManagerUI
UI_Canvas            - All UI (12 children)
EventSystem          - Input System UI Input Module
TrophySystemManager  - Trophy system singleton
TestChest            - Storage interactable
Fridge               - Refrigerator interactable
Kitchen_Sink         - Kitchen sink interactable + progress overlay
Kitchen_Stove        - Stove interactable + progress overlay
Terrain              - Ground terrain
WallOcclusionManager - Singleton for wall transparency
```

---

## 🏗️ IMPLEMENTED SYSTEMS

### 1. **Inventory System** (Complete)
**Location**: `Assets/Scripts/Features/Inventory/`
- `InventoryComponent` - Centralized inventory logic (CanAcceptItem validation)
- `InventoryManagerUI` - Dual-panel UI (Player + Storage), drag-drop, hotbar
- `InventorySlotUI` - Per-slot rendering, progress strip for kitchen
- `ItemDisplayUI` - Tooltips, hover labels, hotbar popups
- `DraggableItem` - Drag-drop with hybrid world drop support

**Items**: Potion, DummySword, Wood, Stone, Seed, Trophy items, Kitchen items (Carrot Dirty/Clean, Rice Raw, Cooked Veggies/Rice)

---

### 2. **Kitchen System** (Complete)
**Location**: `Assets/Scripts/Features/Kitchen/`
- `KitchenStation` (abstract) - Backend processing, batch support, events
- `KitchenSinkInteractable` - Wash recipes (Carrot Dirty → Clean, 3s)
- `StoveInteractable` - Cook recipes (Carrot Clean → Cooked Veggies, 5s; Rice Raw → Cooked Rice, 4s)
- `RefrigeratorInteractable` - Food storage (Vegetable/Fruit only)
- `DoorInteractable` - Teleport to backyard spawn

**UI**: World overlay (renderQueue 4000) + In-UI progress strip (transform-grow) + Sound FX

---

### 3. **Trophy System** (Complete)
**Location**: `Assets/Scripts/Features/Trophy/`
- `TrophySystemManager` - Camera blend 0.6s, player positioning, input lock
- `TrophyRackVisuals` - Visual sync via inventory events
- `TrophySnapPoint` - 4 slots (0-3), layer SnapPoint (10)
- `TrophyCabinetInteractable` - Dual UI (Cabinet left, Rack right)

---

### 4. **Wardrobe + Mirror System** (Complete)
**Location**: `Assets/Scripts/Features/Wardrobe/`
- `OutfitData` (ScriptableObject) - Outfit name, icon, fullBodyPrefab
- `PlayerOutfit` - TryOn/Commit/Revert, cosmetic wardrobe
- `MirrorCamera` - RenderTexture 1024², inner cam for reflection
- `WardrobeManager` - Camera blend 0.6s, UI fade, input lock
- `WardrobeUI` - Fullscreen mirror RawImage + outfit grid (4 outfits + Default)

**Outfits**: Casual, Formal, Sleepwear, Workwear (dummy capsule+sphere prefabs)

---

### 5. **Interaction System** (Complete)
**Location**: `Assets/Scripts/Features/Interaction/`
- `IInteractable` - Interface: `void Interact(GameObject interactor)`
- `PlayerInteractor` - OverlapSphere detection, **interactRadius = 1.5f** (was 2.5f)
- `StorageInteractable` - Generic storage UI
- `BedInteractable` - Sleep (night only), heal + advance day
- `WardrobeInteractable` - Opens wardrobe mode
- `GenericFurnitureInteractable` - Basic furniture interaction
- `WorldLabel` / `Highlightable` - Visual feedback

**Hover System**: `HoverLabelController` → `ItemDisplayUI` (world label + "E — Name" prompt)

---

### 6. **Wall Transparency/Occlusion System** (Complete - Geometry Tuning Needed)
**Location**: `Assets/Scripts/Features/Camera/`
- `WallOccluder.cs` - Per-wall fade in/out, material switching
- `WallOcclusionManager.cs` - Singleton, horizontal raycast at player height (1.8m)

**How It Works**:
```
LateUpdate (20Hz):
  1. Horizontal raycast from player center (y=1.8m) → camera at player height
  2. Layer 10 (OccluderWall), distance = horizontal dist + 3m buffer
  3. Find all WallOccluders hit
  4. Fade IN new occluders (alpha → 0.15), Fade OUT removed (alpha → 1.0)
  4. Smooth fade: Mathf.MoveTowards(alpha, target, 8f * dt)
```

**Current Configuration**:
| Setting | Value |
|---|---|
| `raycastHeight` | 1.8f (was 1.5f) |
| `raycastDistanceBuffer` | 3.0f (was 0.5f) |
| `checkInterval` | 0.03f (was 0.05f, ~33Hz) |
| `transparentAlpha` | 0.15f (15% visible) |
| `fadeSpeed` | 8.0f |
| `occluderLayerMask` | Layer 10 (1024) |

**Wall Colliders** (8 segments, all Layer 10, trigger):
```
cube_10: center=(0.06, 2.0, 2.44) size=(0.25, 4.0, 7.00)
cube_11: center=(-0.06, 2.0, -3.44) size=(0.25, 4.0, 7.00)
cube_20: center=(0.06, 2.0, 0.06) size=(0.25, 4.0, 0.25)
cube_21: center=(-0.06, 2.0, 0.06) size=(0.25, 4.0, 0.25)
cube_22: center=(-0.06, 2.0, -0.06) size=(0.25, 4.0, 0.25)
cube_23: center=(0.06, 2.0, -0.06) size=(0.25, 4.0, 0.25)
cube_24: center=(2.94, 2.0, -0.06) size=(6.00, 4.0, 0.25)
cube_29: center=(2.94, 2.0, -0.06) size=(6.00, 4.0, 0.25)
```

**Known Issue**: Horizontal raycast at y=1.8m from player to camera passes *in front of* walls (closer to camera) due to isometric camera angle (high/steep). Raycast returns 0 hits currently.

**Temporary Workaround**: Increase `raycastDistanceBuffer` to 3f, increase wall height to 4m. Consider:
- Multiple raycasts at different heights (0.5m, 1.5m, 2.5m)
- Lower camera position
- Screen-space occlusion alternative

---

### 7. **Time System** (Complete)
**Location**: `Assets/Scripts/Features/Time/`
- `TimeManager` - Day/Night phases, `OnDayChanged`, `OnPhaseChanged`
- `DayTransitionUI` - Fade panel with "Day X" text

---

### 8. **Player Systems** (Complete)
**Location**: `Assets/Scripts/Player/`
- `PlayerControl` - WASD movement, dash, jump, input lock
- `PlayerStats` - Health (100), heal/take damage
- `PlayerEquipment` - Visual weapon on hotbar select
- `PlayerHealthUI` - HUD health bar sync

---

## 📁 KEY SCRIPTS & LOCATIONS

### Core Gameplay
```
Assets/Scripts/Features/Interaction/
  ├── IInteractable.cs
  ├── PlayerInteractor.cs           // interactRadius = 1.5f
  ├── StorageInteractable.cs
  ├── WardrobeInteractable.cs
  ├── BedInteractable.cs
  ├── GenericFurnitureInteractable.cs
  ├── WorldLabel.cs
  └── Highlightable.cs

Assets/Scripts/Features/Inventory/
  ├── InventoryComponent.cs
  ├── InventorySlot.cs
  ├── ItemData.cs
  └── UI/
      ├── InventoryManagerUI.cs
      ├── InventorySlotUI.cs
      ├── ItemDisplayUI.cs
      └── DraggableItem.cs

Assets/Scripts/Features/Kitchen/
  ├── KitchenStation.cs
  ├── KitchenSinkInteractable.cs
  ├── StoveInteractable.cs
  ├── RefrigeratorInteractable.cs
  ├── DoorInteractable.cs
  ├── KitchenRecipe.cs
  └── UI/
      ├── KitchenStationProgressOverlay.cs
      ├── KitchenStationSoundFx.cs
      └── KitchenStationUI.cs (legacy, hidden)

Assets/Scripts/Features/Trophy/
  ├── TrophySystemManager.cs
  ├── TrophyRackVisuals.cs
  ├── TrophySnapPoint.cs
  └── TrophyItem.cs

Assets/Scripts/Features/Wardrobe/
  ├── OutfitData.cs
  ├── PlayerOutfit.cs
  ├── MirrorCamera.cs
  ├── WardrobeManager.cs
  └── UI/
      └── WardrobeUI.cs

Assets/Scripts/Features/Camera/
  ├── WallOccluder.cs
  └── WallOcclusionManager.cs

Assets/Scripts/Features/Time/
  ├── TimeManager.cs
  └── UI/DayTransitionUI.cs

Assets/Scripts/Player/
  ├── PlayerControl.cs
  ├── PlayerStats.cs
  ├── PlayerEquipment.cs
  ├── PlayerInputActions.cs
  └── UI/PlayerHealthUI.cs
```

### Camera & Behaviour
```
Assets/Scripts/Behaviour/IsometricCamera.cs     // Follows player with offset
Assets/Scripts/Behaviour/IsometricCamera.cs
```

### Editor Tools
```
Assets/Editor/
  ├── WardrobeSetup.cs          // "Farm Beware/Wardrobe/Wire Scene"
  ├── KitchenProgressWiring.cs  // "Farm Beware/Kitchen/Wire World + UI Progress"
  ├── TrophySystemWiring.cs     // "Farm Beware/Trophy System/Wire Scene"
  ├── TrophyAssetFactory.cs     // "Farm Beware/Trophy System/Create Dummy Trophies"
  ├── KitchenSetup.cs           // Legacy (outdated)
  ├── HoverLabelSetup.cs        // "Tools/Wire Hover Labels"
  ├── VisualPolishSetup.cs      // "Polish/Apply Cozy Farm & Room"
  ├── ExportMainScene.cs        // Export MainScene as .unitypackage
  └── GeminiAutomation.cs       // LEGACY - API KEY HARDCODED (DO NOT COMMIT)
```

---

## 🎨 MATERIALS & ASSETS

### Materials
```
Assets/Materials/Kitchen/
  ├── Mat_Floor_Tile.mat
  ├── Mat_Glass.mat
  ├── Mat_Grass.mat
  ├── Mat_Highlight.mat         // Emissive yellow for highlighting
  ├── Mat_Metal.mat
  ├── Mat_Metal_Dark.mat
  ├── Mat_Wall_Cream.mat
  ├── Mat_Wood.mat
  └── Skybox_Cozy.mat

Assets/Materials/Walls/
  └── Mat_Wall_Transparent.mat  // URP Lit, Transparent, Alpha blend, renderQueue=3000

Assets/Materials/Wardrobe/
  ├── Mat_Cermin.mat
  ├── MirrorTexture.renderTexture (1024x1024 ARGB32)
  └── Mat_Outfit_[Casual/Formal/Sleepwear/Workwear].mat
```

### Prefabs
```
Assets/Prefabs/
  ├── Furniture/Kitchen/ (Chair, Door, FoodPrepArea, Fridge, Sink, Stove, Table, Window)
  ├── Trophies/ (TrophyCapsule, TrophyCube, TrophySphere)
  ├── UI/InventorySlot.prefab
  ├── Wardrobe/
  │   ├── Outfit_Casual.prefab
  │   ├── Outfit_Formal.prefab
  │   ├── Outfit_Sleepwear.prefab
  │   └── Outfit_Workwear.prefab
  └── Weapons/DummySword.prefab
```

### ScriptableObjects
```
Assets/Scripts/Features/Inventory/Data/
  ├── Potion, DummySword, Wood, Stone, Seed
  ├── TrophyCapsule, TrophyCube, TrophySphere
  ├── Carrot_Dirty, Carrot_Clean, Rice_Raw
  └── Cooked_Veggies, Cooked_Rice

Assets/Scripts/Features/Kitchen/Data/
  ├── Wash_Carrot.asset (Carrot_Dirty → Carrot_Clean, 3s)
  ├── Cook_Veggies.asset (Carrot_Clean → Cooked_Veggies, 5s)
  └── Cook_Rice.asset (Rice_Raw → Cooked_Rice, 4s)

Assets/Scripts/Features/Wardrobe/Data/
  ├── Casual.asset, Formal.asset, Sleepwear.asset, Workwear.asset
```

---

## ⚙️ KEY CONFIGURATIONS

### PlayerInteractor
```csharp
// Assets/Scripts/Features/Interaction/PlayerInteractor.cs
interactRadius = 1.5f          // Reduced from 2.5f
interactableLayer = -1 (Everything)
```

### IsometricCamera
```csharp
// Assets/Scripts/Behaviour/IsometricCamera.cs
offset = (-10, 10, -10)        // High isometric angle
smoothSpeed = 5f
```

### Input System
```
Assets/InputSystem_Actions.inputactions
Assets/Scripts/Player/PlayerInputActions.cs (generated)
```

### Layers
| Layer | Name | Usage |
|---|---|---|
| 0 | Default | Most objects |
| 8 | Interactable | Interactable objects |
| 9 | Trophy | Trophy items |
| 10 | **OccluderWall** | **Wall occlusion detection** |
| 10 | SnapPoint | Trophy snap points |

---

## 🐛 KNOWN ISSUES & BLOCKERS

### 1. **Wall Occlusion Raycast Not Detecting Walls** 🔴
**Problem**: Horizontal raycast from player (y=1.8) → camera returns 0 hits despite:
- ✅ Walls on Layer 10 with BoxCollider (trigger)
- ✅ Layer collision: Layer 10 ↔ Layer 0 enabled
- ✅ `Physics.queriesHitTriggers = true`
- ✅ Raycast uses `QueryTriggerInteraction.Collide`

**Root Cause**: Isometric camera is high (y≈24) and steep. Horizontal ray at y=1.8 passes **in front of** walls (closer to camera) because camera Z (-11) < Player Z (16) < Walls Z (13-20). Ray passes z=13-20 at x positions *beyond* walls.

**Attempted Fixes**:
- Increased `raycastDistanceBuffer` to 3f
- Increased `raycastHeight` to 1.8f
- Increased wall height to 4m, center.y=2
- Increased check frequency to 33Hz
- Set `Physics.queriesHitTriggers = true`

**Workarounds to Try**:
1. **Increase wall height further** (6-8m) for occlusion detection
2. **Multiple raycasts** at different heights (0.5m, 1.5m, 2.5m, 3.5m)
3. **Lower camera Y position** (reduce from 24 to 15-18)
4. **Use screen-space occlusion** (depth texture comparison)
5. **SphereCast instead of Raycast** (wider detection)

### 2. **Wardrobe Mirror White Screen** 🟡
**Status**: Fixed in source (3D Isometric project) - needs verification in Farm-Beware
- `WardrobeUI.RefreshMirrorTexture()` called in `Start()`, `OnEnable()`, `Update()`
- `WardrobeSetup` wires `mirrorCamera` and `mirrorRenderTexture`
- `WardrobeSetup` calls `btnGO.SetActive(true)` after Instantiate

### 3. **Kitchen Progress Overlay Material** 🟡
- `Mat_Wall_Transparent.mat` created but may need assignment to `KitchenStationProgressOverlay.overlayMaterial`
- Currently shows "overlayMaterial kosong" warning in logs

### 4. **GeminiAutomation.cs** ⚠️
**Location**: `Assets/Editor/GeminiAutomation.cs`
**WARNING**: Contains hardcoded API key. **DO NOT COMMIT TO GIT**.

---

## 📦 PORTING STATUS (3D Isometric → Farm-Beware)

### Completed (Merged to rafi-branch)
| Feature | PR | Status |
|---|---|---|
| Bedroom/Trophy System | #8 | ✅ Merged |
| Kitchen System | #9 | ✅ Merged |
| RafiScene Import | #10 | ✅ Merged |

### Pending Port (Local Only - 3D Isometric)
| Feature | Status |
|---|---|
| Kitchen Progress Overlay (World + In-UI) | Local only |
| Wardrobe + Mirror System | Local only |
| Wall Occlusion System | Local only (this session) |

**Next Port Steps**:
1. Create feature branches in Farm-Beware
2. Copy new scripts, materials, prefabs
3. Run wiring tools (`WardrobeSetup`, `KitchenProgressWiring`)
4. Create PRs to `rafi-branch`

---

## 🚀 NEXT SESSION PRIORITIES

### Immediate (High Priority)
1. **Fix Wall Occlusion Detection** - Try multiple raycast heights or spherecast
2. **Verify Wardrobe Mirror** - Test in play mode, ensure no white screen
3. **Test Kitchen Progress** - Visual + audio feedback working
4. **Test Interaction Distance** - 1.5m feels right for isometric

### Medium Priority
1. **Port Kitchen Progress + Wardrobe** to Farm-Beware repo
2. **Fix Kitchen Progress Material** assignment
3. **Add SphereCast** fallback for occlusion (wider detection)
3. **Multiple raycast heights** for occlusion (0.5, 1.5, 2.5, 3.5m)

### Low Priority
1. **Cinemachine** smooth camera (package installed, unused)
2. **UI Polish** (rounded panels, custom icons)
3. **Backyard dressing** (trees, fence, paths)
4. **Cleanup** - Remove `GeminiAutomation.cs`, legacy scripts

---

## 🔧 QUICK COMMANDS FOR NEXT SESSION

```bash
# Open project
cd "C:\Users\HP\Rafi\MyProject\Farm-Beware"

# Open Unity Editor (ensure MCP server running in Unity)
# Window > MCP for Unity > Start Server (port 8080)

# Quick test commands (in MCP):
# 1. Enter play mode
# 2. Walk player behind walls - check console for [AUDIT] logs
# 3. Interact with Chest (E) - wardrobe should open
# 4. Interact with Sink/Stove - progress overlays should appear
# 5. Check interaction distance (1.5m)
```

---

## 📝 AGENT HANDOFF NOTES

**For Next Agent**: This document contains everything needed to continue. Key things to remember:

1. **Wall occlusion is architecturally complete but geometrically broken** - the raycast geometry doesn't work with current camera/wall positions. Don't rewrite the logic; fix the geometry or add multi-height raycasts.

2. **All systems are modular and event-driven** - `InventoryComponent.OnInventoryChanged`, `KitchenStation.OnProcessProgress`, etc. Don't break the event architecture.

3. **Wardrobe system is complete in source project** - just needs porting. The mirror white-screen bug was fixed in source via `RefreshMirrorTexture()` + `btnGO.SetActive(true)`.

4. **Interaction distance is now 1.5m** - test that it feels natural for isometric view.

5. **Scene is StagingScene** - this is the main working scene. MainCamera is orthographic isometric.

6. **MCP Server must be running in Unity** before connecting - Window > MCP for Unity > Start Server.

---

**Last Session**: 2025-08-23 - Implemented wall occlusion + tuned interaction distance
**Next Session Goal**: Fix occlusion detection geometry, verify all systems in play test