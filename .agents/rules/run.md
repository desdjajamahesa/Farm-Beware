# Run.md

Execution instructions, controls, and configuration details for running **Farm-Beware** in local development and Unity Editor.

---

## 1. System Requirements & Environment

- **Unity Engine**: Unity 6000.3.20f1 (Unity 6 / 2023 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP Deferred+)
- **Target OS**: Windows 10 / 11 (64-bit)
- **Input Framework**: Unity New Input System (`com.unity.inputsystem`)
- **Text Rendering**: TextMeshPro (`com.unity.textmeshpro`)
- **Scripting Backend**: Mono / .NET Standard 2.1

---

## 2. Launching the Project

### 2.1 Opening in Unity Hub
1. Open **Unity Hub**.
2. Click **Add project from disk** and browse to: `C:\Users\HP\Rafi\MyProject\Farm-Beware`
3. Ensure the editor version matches **6000.3.20f1**.

### 2.2 Loading the Primary Staging Scene
1. In the Project window, navigate to: `Assets/Scenes/`
2. Double-click to open: [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)
3. This scene contains all staging integration modules:
   - Interior rooms (Bedroom, Kitchen, Living Room, Garage, Warehouse).
   - Wardrobe, mirror reflection, and 3D trophy cabinet.
   - Farm plots, natural pond, campfire, and perimeter fence.
   - `MonsterSpawnPoints` front-gate spawner hierarchy.
   - Player character, camera controller, and UI canvas.

### 2.3 Entering Play Mode
- Click the **Play** button at the top of the Editor (or press `Ctrl + P`).

---

## 3. Player Controls & Keybindings

| Key / Input | Action | Context |
|---|---|---|
| `W, A, S, D` | Player Movement | Free-roam |
| `Space` | Jump | Free-roam |
| `Left Shift` | Sprint / Run | Free-roam |
| `Left Mouse Button (Click)` | Light Attack Combo (3-Hit Combo) | Weapon equipped |
| `Left Mouse Button (Hold > 0.35s)` | Charged Heavy Attack | Weapon equipped |
| `Left Shift + Left Mouse Button` | Sprint Dash Attack | Sprinting |
| `F` | Front Kick (Knockback) | Free-roam / Combat |
| `Space + Left Mouse Button` | Jump Skill Slam | Airborne |
| `1, 2, 3, 4` | Hotbar Slot Selection | Free-roam |
| `Scroll Wheel` | Cycle Hotbar Slots / Camera Zoom | Free-roam |
| `Right Click (Hold + Drag)` | Orbit Isometric Camera | Free-roam |
| `E` | Interact (Bed, Stove, Sink, Chest, Wardrobe) | Near interactable |
| `Tab` or `I` | Toggle Inventory & Character Sheet | Free-roam |
| `C` | Toggle Character Stats Window | Free-roam |
| `ESC` | Close Active Modal (Priority 1) / Open Pause Menu (Priority 2) | Any time |
| `N` *(Debug)* | Force Trigger Night Brawl Phase immediately | Testing combat waves |

---

## 4. Unity MCP Bridge Integration

AI agents communicate with Unity via MCP tools:
- **Domain Recompilation**: Call `refresh_unity` with `compile="request"`.
- **In-Memory Roslyn Execution**: Execute validation scripts via `execute_code`.
- **Log Monitoring**: Inspect Editor logs via `read_console` (filter: `error`, `warning`).

---

## 5. Critical Scene GameObjects (`StagingScene`)

- **System Managers (`_SYSTEMS`)**:
  - `CameraManager`: Controls camera modes and input locking.
  - `DayNightTimeManager`: Coordinates day/night cycle and bed sleep gate.
  - `SaveLoadService`: Multi-slot game persistence and auto-save.
  - `WallOcclusionManager`: Raycast and sphere non-alloc wall/foliage occlusion.
  - `NightBrawlManager`: Night combat wave orchestrator.
  - `EnemyHealthBarManager` & `BossHealthBarManager`: Dual-tier combat UI pools.
- **Environment Lighting (`_LIGHTING`)**:
  - `Directional Light` (Sun/Moon, Light Layer 0).
  - `Global Volume` (ACES Tonemapping, Bloom, Motion Blur, Chromatic Aberration).
- **Player (`_ENTITIES/Player`)**:
  - `PlayerControl`, `PlayerStats`, `InventoryComponent`, `PlayerEquipment`, `PlayerOutfit`.
- **World & Navigation (`_WORLD`)**:
  - `MonsterSpawnPoints`: Contains front gate children (`SpawnPoint_FrontGate_Left/Center/Right`, `Z >= 52m`).
  - `NavMesh Obstacles`: Campfire, Natural Pond, and Farmland Soil Plots excluded from walkable navigation.
  - `WardrobeRoot/Mirror`: `MirrorCamera` with portrait RenderTexture and wood frame material.

---

## 6. Rendering & Lighting Editor Tools

Access specialized tools from the Unity Editor top menu bar under `Tools > Farm-Beware > Rendering`:
1. **URP & BRG Validator**:
   - Menu: `Tools > Farm-Beware > Rendering > URP & BRG Validator`
   - Validates `DeferredPlus` renderer, GPU Resident Drawer (`InstancedDrawing`), SRP Batcher, and disables static batching.
2. **Light Layer Assignment Utility**:
   - Menu: `Tools > Farm-Beware > Rendering > Light Layer Assignment Utility`
   - Segregates outdoor Sun/Moon to **Layer 0** and indoor lamps to **Layer 1** to prevent night light leakage.
