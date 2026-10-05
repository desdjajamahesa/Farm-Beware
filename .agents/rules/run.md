# Run.md

Execution instructions, keybindings, and scene configuration for **Farm-Beware**.

---

## 1. Environment & Setup

- **Unity Engine**: Unity 6000.3.20f1 (Unity 6 / 2023 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP Deferred+)
- **Input & Text**: New Input System (`UnityEngine.InputSystem`), TextMeshPro (`TMPro`)
- **Primary Scene**: [`Assets/Scenes/StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)
  - Contains all integrated features: interior rooms, wardrobe mirror, 3D trophy cabinet, farmland plots, pond, campfire, front gate monster spawners, player, and canvas UI.

---

## 2. Player Controls & Keybindings

| Key / Input | Action | Context |
|---|---|---|
| `W, A, S, D` | Player Movement (Analog magnitude supported) | Free-roam |
| `Space` | Jump | Free-roam |
| `Left Shift` | Sprint / Run | Free-roam |
| `Left Mouse (Click)` | Light Attack Combo (3-Hit Combo with buffering) | Weapon equipped |
| `Left Mouse (Hold > 0.35s)` | Charged Heavy Attack | Weapon equipped |
| `Left Shift + Left Mouse` | Sprint Dash Attack | Sprinting |
| `F` | Front Kick (Knockback disruptor) | Free-roam / Combat |
| `Space + Left Mouse` | Jump Skill Slam | Airborne |
| `C` / `Left Alt` | Dodge Roll (300ms i-frames) | Free-roam / Combat |
| `V` / `Left Ctrl` | Precision Deflect / Parry (350ms window, 350ms whiff lockout) | Free-roam / Combat |
| `1, 2, 3, 4` | Hotbar Slot Selection | Free-roam |
| `Scroll Wheel` | Cycle Hotbar Slots / Camera Zoom | Free-roam |
| `Right Click (Hold + Drag)` | Orbit Isometric Camera | Free-roam |
| `E` | Interact (Bed, Stove, Sink, Chest, Wardrobe) | Near interactable |
| `Tab` or `I` | Toggle Inventory & Character Sheet | Free-roam |
| `ESC` | Close Active Modal (Priority 1) / Pause Menu (Priority 2) | Any time |
| `N` *(Debug)* | Force Trigger Night Brawl Phase immediately | Testing combat |

---

## 3. Unity MCP Bridge Integration

Autonomous AI agents communicate with Unity via MCP tools:
- **Domain Recompilation**: Call `refresh_unity`.
- **In-Memory Roslyn Execution**: Execute validation scripts via `execute_code`.
- **Log Monitoring**: Inspect Editor console logs via `read_console` (target: 0 errors).

---

## 4. Critical Scene GameObjects (`StagingScene`)

- **System Managers (`_SYSTEMS`)**:
  - `CameraManager`: Controls camera modes (`Gameplay`, `TrophyMode`, `WardrobeMode`) and input locking.
  - `DayNightTimeManager`: Coordinates day/night cycle and bed sleep gate.
  - `SaveLoadService`: Multi-slot JSON game persistence and auto-save.
  - `NightBrawlManager`: Night combat wave orchestrator.
  - `EnemyHealthBarManager` & `BossHealthBarManager`: Dual-tier combat UI pools.
- **Environment Lighting (`_LIGHTING`)**:
  - `Directional Light` (Sun/Moon, Light Layer 0).
  - `Global Volume` (ACES Tonemapping, Bloom, Motion Blur, Chromatic Aberration).
- **Player (`_ENTITIES/Player`)**:
  - `PlayerControl`, `PlayerStats`, `InventoryComponent`, `PlayerEquipment`, `PlayerOutfit`.
- **World & Navigation (`_WORLD`)**:
  - `MonsterSpawnPoints`: Front gate children (`SpawnPoint_FrontGate_Left/Center/Right`, `Z >= 52m`).
  - `NavMesh Obstacles`: Campfire, Natural Pond, and Farmland Plots carved as non-walkable.
  - `WardrobeRoot/Mirror`: `MirrorCamera` with portrait RenderTexture and wood frame material.

---

## 5. Rendering & Lighting Editor Utilities

Top menu bar under `Tools > Farm-Beware > Rendering`:
1. **URP & BRG Validator**: Validates Deferred+ renderer, GPU Resident Drawer (`InstancedDrawing`), SRP Batcher, and disables static batching.
2. **Light Layer Assignment Utility**: Separates Sun/Moon (Layer 0) from interior lamps (Layer 1).
