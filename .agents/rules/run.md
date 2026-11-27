# Run.md

Execution instructions, keybindings, and scene configuration for **Farm-Beware** (`Alpha 0.3.0`).

---

## 1. Environment & Setup

- **Unity Engine**: Unity `6000.3.20f1` (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP Deferred+)
- **Input & Text**: New Input System (`1.19.0`), TextMeshPro (`TMPro`)
- **Build Scenes**:
  - [`Assets/Scenes/MainMenuScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/MainMenuScene.unity) (Entry menu scene)
  - [`Assets/Scenes/MainScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/MainScene.unity) (Primary gameplay scene: house interior, wardrobe, 16–32 farmland plots, western roadway, Trading Post, pickup truck, front gate spawners, player, UI)

---

## 2. Player Controls & Keybindings

| Key / Input | Action | Context |
|---|---|---|
| `W, A, S, D` | Player Movement / Vehicle Steering & Throttle | Free-roam / Driving |
| `Space` | Jump (Player) / Handbrake (Vehicle) | Free-roam / Driving |
| `Left Shift` | Sprint / Run | Free-roam |
| `Left Mouse (Click)` | Light Attack Combo (3-hit buffered) | Weapon / Tool equipped |
| `Left Mouse (Hold > 0.35s)` | Charged Heavy Attack | Weapon equipped |
| `Left Shift + Left Mouse` | Sprint Dash Attack | Sprinting |
| `F` | Front Kick (Knockback disruptor) | Free-roam / Combat |
| `Space + Left Mouse` | Jump Skill Slam | Airborne |
| `C` / `Left Alt` | Dodge Roll (300ms i-frames) | Free-roam / Combat |
| `V` / `Left Ctrl` | Precision Deflect / Parry (350ms window, 350ms whiff lockout) | Free-roam / Combat |
| `1, 2, 3, 4` | Hotbar Slot Selection | Free-roam |
| `Scroll Wheel` | Cycle Hotbar Slots / Camera Zoom | Free-roam |
| `Right Click (Hold + Drag)` | Orbit Isometric Camera | Free-roam |
| `E` | Interact (Bed, Stove, Sink, Fridge, Chest, Wardrobe, Well, Farm, Truck, Order Board, Merchant) | Near interactable |
| `Tab` or `I` | Toggle Inventory & Character Sheet | Free-roam |
| `Q` | Toggle Quest Log Modal | Free-roam |
| `M` | Toggle World Map Modal | Free-roam |
| `J` | Toggle Journal & Storyline Lore Modal | Free-roam |
| `ESC` | Hold: Skip Cutscene / Tap: Close Active Modal / Pause Menu | Any time |
| `T` or `9` | Cycle Clock Display Mode (`60s/40m` -> `30s/20m` -> `15s/10m`) | Daytime |
| `0` *(Debug)* | Toggle Fast-Forward (`8x` Speed) | Daytime |
| `N` *(Debug)* | Force Trigger Night Brawl Phase | Testing combat |

---

## 3. Unity MCP Bridge & Verification

- **Domain Recompilation**: Call `refresh_unity`.
- **In-Memory Roslyn Execution**: Execute scripts via `execute_code`.
- **Log Monitoring**: Inspect console via `read_console` (target: 0 errors, 0 warnings).
- **Audit Suite**: Run `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()` or `run_tests(mode: "EditMode", assembly_names: ["FarmBeware.Editor.Tests"])` (117/117 passing).

---

## 4. Critical Scene GameObjects (`MainScene`)

- **System Managers**: `CameraManager`, `IsometricCameraController`, `DayNightTimeManager`, `StorylineManager`, `QuestManager`, `DailyEconomyManager`, `SaveSystemManager`, `NightBrawlManager`.
- **Player & Vehicles**: `Player` (`PlayerControl`, `PlayerStats`, `InventoryComponent`, `PlayerEquipment`, `PlayerOutfit`), pickup truck (`ArcadeVehicleController`).
- **Economy & World**: `Road_Homestead_To_TradingPost`, `TradingPost_Complex` (West outpost), `TradingPost_ShopCounter` (`MerchantShopUI`), `FarmOrderBoard` (`OrderBoardUI`), `FarmlandPlot 1..16` (+ expandable slots).
