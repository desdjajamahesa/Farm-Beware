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

- **Player**: `Player` (contains `PlayerControl`, `PlayerStats`, `InventoryComponent`, `PlayerEquipment`, `PlayerOutfit`).
- **Main Camera**: `Main Camera` (contains `IsometricCameraController`, `CameraManager`, `WallOcclusionManager`).
- **Test Chest**: `Environment/Testing/TestChest` (populated with 21 active MVP test items).
- **Stove**: `Environment/Kitchen/stove` (holds `GenshinStove`).
- **Sink**: `Environment/Kitchen/kitchen_sink` (holds `KitchenSinkInteractable`).
- **Refrigerator**: `Environment/Kitchen/refrigerator` (holds `RefrigeratorInteractable`).
- **Bedroom Mirror**: `Environment/Bedroom/Mirror` (holds `MirrorCamera`).
