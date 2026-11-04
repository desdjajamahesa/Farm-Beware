# Run.md

Execution instructions, keybindings, and scene configuration for **Farm-Beware**.

---

## 1. Environment & Setup

- **Unity Engine**: Unity 6000.3.20f1 (Unity 6 / 2023 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP Deferred+)
- **Input & Text**: New Input System (`UnityEngine.InputSystem`), TextMeshPro (`TMPro`)
- **Primary Scene**: [`Assets/Scenes/StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)
  - Contains all integrated systems: interior rooms, wardrobe, 16 farmland plots, western road, trading post, pickup truck, front gate monster spawners, player, and canvas UI.

---

## 2. Player Controls & Keybindings

| Key / Input | Action | Context |
|---|---|---|
| `W, A, S, D` | Player Movement / Vehicle Steering & Throttle | Free-roam / Driving |
| `Space` | Jump (Player) / Handbrake (Vehicle) | Free-roam / Driving |
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
| `E` | Interact (Bed, Stove, Sink, Chest, Wardrobe, Farm, Truck, Shop) | Near interactable |
| `Tab` or `I` | Toggle Inventory & Character Sheet | Free-roam |
| `J` | Toggle Journal & Storyline Docs Modal | Free-roam |
| `ESC` | Hold: Skip Cutscene / Tap: Close Active Modal / Pause Menu | Any time |
| `T` or `9` | Cycle Clock Display Mode (60s/40m -> 30s/20m -> 15s/10m) | Free-roam / Daytime |
| `0` *(Debug)* | Toggle Fast-Forward (8x Speed) | Daytime exploration |
| `N` *(Debug)* | Force Trigger Night Brawl Phase immediately | Testing combat |

---

## 3. Unity MCP Bridge Integration

- **Domain Recompilation**: Call `refresh_unity`.
- **In-Memory Roslyn Execution**: Execute validation scripts via `execute_code`.
- **Log Monitoring**: Inspect Editor console logs via `read_console` (target: 0 errors).
- **Audit Verification**: Run `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`.

---

## 4. Critical Scene GameObjects (`StagingScene`)

- **System Managers (`_SYSTEMS`)**:
  - `CameraManager` & `IsometricCameraController`: Camera modes and input gating.
  - `DayNightTimeManager`: Coordinates day/night cycle and bed sleep gate.
  - `StorylineManager` & `QuestManager`: Narrative state, cutscenes, objective tracking.
  - `DailyEconomyManager`: Daily rolling market prices and trade transactions.
  - `SaveLoadService`: Multi-slot JSON game persistence and auto-save.
  - `NightBrawlManager`: Night combat wave orchestrator.
- **Player & Vehicles (`_ENTITIES`)**:
  - `Player`: `PlayerControl`, `PlayerStats`, `InventoryComponent`, `PlayerEquipment`, `PlayerOutfit`.
  - `OldFarmPickup_Truck`: `ArcadeVehicleController`, `VehicleDynamicsCore`.
  - `TradingPost_ShopCounter`: `ShopInteractable`, `MerchantShopUI` modal at `(-175.5, 0.0, 63.5)`.
- **World & Infrastructure (`_WORLD`)**:
  - `Road_Homestead_To_TradingPost`: 225m Catmull-Rom roadway connecting Gate to West.
  - `TradingPost_Complex`: Outpost at `(-175.0, 0.0, 70.0)` with South-facing trade veranda.
  - `FarmlandPlot 1 to 16`: 16 tilled soil plots with dynamic crop mesh renderers.
