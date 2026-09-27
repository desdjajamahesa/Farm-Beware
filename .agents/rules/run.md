# Run.md

Execution instructions, controls, and configuration details for running **Farm-Beware** in local development and Unity Editor.

---

## 1. System Requirements & Environment

- **Unity Engine**: Unity 6000.3.20f1 (Unity 6 / 2023 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Target OS**: Windows 10 / 11 (64-bit)
- **Input Framework**: Unity New Input System (`com.unity.inputsystem`)
- **Text Rendering**: TextMeshPro (`com.unity.textmeshpro`)
- **Scripting Backend**: Mono / .NET Standard 2.1

---

## 2. Launching the Project

### 2.1 Opening in Unity Hub
1. Open **Unity Hub**.
2. Click **Add project from disk** and browse to:
   `C:\Users\HP\Rafi\MyProject\Farm-Beware`
3. Ensure the editor version matches **6000.3.20f1**.

### 2.2 Loading the Primary Staging Scene
1. In the Project window, navigate to:
   `Assets/Scenes/`
2. Double-click to open:
   [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)
3. This scene is the master staging integration environment containing:
   - Bedroom (`Environment/Bedroom`)
   - Kitchen (`Environment/Kitchen`)
   - Wardrobe and Trophy Cabinet (`Environment/WardrobeRoot`, `Environment/TrophyCabinetSystem`)
   - Testing Area & Test Chest (`Environment/Testing/TestChest`)
   - Player Character & Main Camera (`Player`, `Main Camera`)
   - User Interface (`Canvas_UI`)

### 2.3 Entering Play Mode
- Click the **Play** button at the top of the Editor (or press `Ctrl + P`).

---

## 3. Player Controls & Keybindings

| Key | Action | Context |
|---|---|---|
| `W, A, S, D` | Player Movement | Gameplay |
| `Space` | Jump | Gameplay |
| `Left Shift` | Dash | Gameplay |
| `1, 2, 3, 4` | Select Hotbar Slot | Gameplay |
| `Scroll Wheel` | Cycle Hotbar Slots / Camera Zoom | Gameplay |
| `Right Click (Hold + Drag)` | Orbit Isometric Camera | Gameplay |
| `E` | Interact (Bed, Stove, Sink, Chest, Wardrobe) | Near interactable object |
| `Tab` or `I` | Toggle Player Inventory Panel | Gameplay |
| `ESC` | Close Active Modal (Priority 1) / Open Pause Menu (Priority 2) | Any time |
| `N` *(Debug)* | Skip to Night Phase immediately | Testing bed sleep cycle |

---

## 4. Unity MCP Bridge Integration

AI agents and automated workflows communicate through the Unity MCP server:
- **Recompilation & Asset Refresh**:
  Call `refresh_unity` with `compile="request"`.
- **In-Memory Roslyn Execution**:
  Execute validation scripts without creating asset files via `execute_code`.
- **Log Monitoring**:
  Inspect Editor logs using `read_console` (filter: `error`, `warning`).

---

## 5. Critical Scene GameObjects (`StagingScene`)

- **System Managers**: `_SYSTEMS` (contains `CameraManager`, `GameManager`, `EventSystem`, `WallOcclusionManager`, `TrophySystemManager`).
- **Environment Lighting**: `_LIGHTING` (contains `Directional Light`, `Global Volume`).
- **Camera Controller**: `_CAMERAS/Main Camera` (contains `IsometricCameraController`, `CameraManager`, `WallOcclusionManager`).
- **Player**: `_ENTITIES/Player` (contains `PlayerControl`, `PlayerStats`, `InventoryComponent`, `PlayerEquipment`, `PlayerOutfit`).
- **World Structure & Terrain**: `_WORLD/Terrain` and `_WORLD/Structure` (`House`, `House_UnifiedFloorCollider`).
- **World Zones**: `_WORLD/Zones` (`Bedroom`, `Kitchen`, `LivingRoom_Foyer`, `Garage`, `Warehouse`, `MiddleArea_Stairs`).
- **Gameplay Anchors**: `_GAMEPLAY/SpawnPoints` and `_GAMEPLAY/Testing/TestChest` (populated with 21 active MVP test items).
- **User Interface**: `_UI/UI_Canvas` (holds all HUDs, modal panels, and menus).

---

## 6. Rendering & Lighting Editor Tools

Access specialized graphics utilities from the Unity Editor top menu bar under `Tools > Farm-Beware > Rendering`:

1. **URP & BRG Validator** (`URPGraphicsConfigurationValidator.cs`):
   - Menu: `Tools > Farm-Beware > Rendering > URP & BRG Validator`
   - Validates that the active Renderer uses `DeferredPlus`, GPU Resident Drawer is set to `InstancedDrawing`, SRP Batcher is active, and Static Batching is safely disabled to prevent CPU RAM duplication.
2. **Light Layer Assignment Utility** (`LightLayerAssignmentUtility.cs`):
   - Menu: `Tools > Farm-Beware > Rendering > Light Layer Assignment Utility`
   - Provides one-click scene audits and automated segregation:
     - Assigns `Directional Light` to **Light Layer 0 (Exterior)**.
     - Assigns all `InteriorLamp_*` and `Light_Interior_*` to **Light Layer 1 (Interior)**.
     - Guarantees zero light leaking between outdoor night moonlight and indoor warm lighting.

