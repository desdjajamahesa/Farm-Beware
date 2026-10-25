# Farm-Beware: Technical Architecture, Systems Hierarchy & Narrative Lore Reference

---

## 1. Narrative Lore & Story Context

### 1.1 The NexaPhyto Bio-Agrochemical Catastrophe (1989)
*Farm-Beware* takes place in the isolated agricultural valley of Lot 417-B. Decades prior to the events of the game, the property was secretly leased to the **NexaPhyto Bio-Agrochemicals Research Facility**. In the late 1980s, the corporation conducted classified experiments on synthetic mutagenic growth catalysts designed to hyper-accelerate organic yield cycles and bio-engineer military-grade bio-defensive crops.

In the winter of 1989, facility personnel logged **Incident Report #042: Subterranean Tank Rupture**. Tank 04 suffered a catastrophic structural failure, releasing more than 10,000 liters of the concentrated mutagen **Phyto-Toxin Reagent 7** into the secondary drainage basin directly beneath the homestead. Rather than remediating the disaster, NexaPhyto enacted an emergency facility seal, buried the drainage schematics, and abruptly liquidated the property under municipal receivership.

### 1.2 The Protagonist & Lot 417-B
The protagonist, **Asep (The Farmer)**, purchased Lot 417-B through a municipal land auction. Seeking a quiet, self-sustaining country life, Asep signed the standard *Environmental Waiver Clause 14*, which released the municipality from liability regarding unmapped subterranean conduit networks or ecological anomalies.

Upon arrival, Asep finds a rustic homestead equipped with a functional bedroom, kitchen, storage warehouse, outdoor garden plots, an old stone water well, and a high perimeter wooden fence. Unbeknownst to the farmer, the house and surrounding farmland were constructed directly on top of the main effluent drainage junction of the buried NexaPhyto facility.

### 1.3 The Catalytic Bio-Chemical Cycle
The central dramatic irony of the game stems from the daily farming loop:
1. **Soil Tilling & Sowing:** The farmer tills the dry topsoil and plants seeds into the plots.
2. **Watering Catalyst:** Drawing water from the ancient stone well into a 100-liter water bottle, the farmer saturates the soil. Moisture seeps downward through the sediment, dissolving dormant Reagent 7 crystals and washing them into the corroded underground pipes.
3. **Subterranean Mutagenic Binding:** The mutagenic solution binds aggressively to the roots of the growing crops, stimulating uncontrolled cellular division and hyper-aggressive predatory traits.
4. **Nighttime Awakening:** When night falls and ambient light fades, the mutated root clusters break through the topsoil, seeking biological sustenance and converging upon the homestead perimeter.

---

### 1.4 Mutated Flora & Enemy Evolution Taxonomy
The game features three core crops, each evolving into distinctive standard and apex boss forms:

```
[Crop Seed] ───────────► [Standard Night Entity] ───────────► [Apex Boss Evolution]
Sweet Potato            Tuber Maw                           Cyclops Tuber Maw (Specimen C-1)
Taro                    Taro Brute                          Taro Colossus (Specimen T-Colossus)
Corn                    Corn Musketeer                      The Ranger (Specimen Apex)
```

#### A. Sweet Potato (Agility & Metabolic Speed)
- **Biological Trait:** High carbohydrate and sugar content triggers rapid metabolic acceleration.
- **Normal Form — Tuber Maw:** Fast, low-stature burrowing predator. It dives beneath the earth (*Burrow Strike*), tracking the player's vibrations before erupting upwards. When wounded, it uses subterranean tunnels to disengage.
- **Apex Boss — Cyclops Tuber Maw (Specimen C-1):** Multiple adjacent sweet potato root networks fuse into a central neural node dominated by a large, photoreceptive bio-luminescent ocular stalk. It channels energy into a focused tracking beam and summons secondary Tuber Maw minions from the soil.
- **Economic & Combat Value:** Sweet potato dishes enhance player stamina, dash efficiency, and movement velocity. Monster drops provide *Mutated Roots* and *Cyclops Eyes* for mobility-oriented weapon upgrades.

#### B. Taro (Defense & Mineralized Bark)
- **Biological Trait:** Fibrous root jackets absorb heavy mineral leachate from the contaminated bedrock, forming petrified wooden armor.
- **Normal Form — Taro Brute:** Heavy, durable humanoid root entity. Encased in bark armor (*Root Guard*), it absorbs physical impacts and executes heavy downward swings that knock the player backwards.
- **Apex Boss — Taro Colossus (Specimen T-Colossus):** A massive, multi-ton root anchor capable of seismic devastation. It leaps into the air (*Airborne Slam*), casting a tracking shadow on the ground before crashing down with shockwaves that fracture stone pathways. If the player is caught in its shadow, it executes a crushing grapple.
- **Economic & Combat Value:** Taro yields provide high baseline gold value. Cooked taro dishes increase max health and damage mitigation. Drops include *Hardened Roots* and *Colossus Cores* for heavy damage and poise weapon paths.

#### C. Corn (Utility, Sinew & Ballistic Thorns)
- **Biological Trait:** Tall stalk sinew and pressurized seed pods adapt into natural ballistic firing chambers.
- **Normal Form — Corn Musketeer:** Slender, ranged stalk sentinel. It positions itself along perimeter sightlines, charging pressurized kernel shots and firing triple shrapnel bursts across the field.
- **Apex Boss — The Ranger (Specimen Apex):** The ultimate bio-engineered genetic sentinel created by NexaPhyto to guard the primary drainage conduit. A stationary, imposing arboreal hybrid combining flexible cornstalk sinew with toxic projectile thorns. It unleashes mortar-style *Corn Heavens Fall* projectile volleys, summons musketeer reinforcements, and enters a berserk *Kernel Burst* enrage when damaged below 50% health.
- **Economic & Combat Value:** Corn recipes provide rapid health recovery and stamina replenishment. Drops provide *Kernel Shrapnel* and *Cob Cores* for area-of-effect and crowd-control weapon modifications.

---

### 1.5 The 5-Day Campaign & Climax
The campaign progresses across a 5-day cycle of daytime preparation and escalating night assaults:
- **Day 1 (Arrival & Outbreak):** The farmer restores the initial garden plots and repairs basic tools. Night 1 brings the first wave of Tuber Maws testing the wooden barricades.
- **Day 2 (Armored Incursion):** Discovery of contaminated taro roots. Night 2 introduces armored Taro Brutes, demanding weapon upgrades and stamina food management.
- **Day 3 (Crossfire & Discovery):** Discovery of discarded NexaPhyto journal logs in the garage. Corn Musketeers engage the player at range during Night 3.
- **Day 4 (Subterranean Tremors):** Ground vibrations herald the emergence of the first apex specimen (Cyclops Tuber Maw) supported by standard horde units.
- **Day 5 (Total Bio-Containment Failure & Resolution):** The final confrontation. Wave 4 unleashes the Taro Colossus, followed by Wave 5 where The Ranger emerges at the main chemical drainage breach. Upon defeating The Ranger, the facility's automated emergency relief valve disengages, releasing neutralizing vapor through the underground pipe network. The chemical runoff crystallizes into inert agricultural fertilizer, purifying the soil and securing the homestead's future.

---

## 2. Technical Infrastructure & Engineering Patterns

### 2.1 Core Platform & Package Stack
- **Engine Version:** Unity 6000.3.20f1 (Unity 6 LTS)
- **Rendering Pipeline:** Universal Render Pipeline (URP) Deferred+ with Custom Render Graph extensions.
- **Input Architecture:** Unity Input System (v1.19.0) with generated C# wrapper classes and centralized modal gating.
- **AI & Navigation:** Unity AI Navigation (v2.0.13) utilizing runtime `NavMeshSurface` and dynamic `NavMeshObstacle` carving.
- **Cinematics & Tracking:** Cinemachine (v3.1.7) handling isometric camera damping, room framing, and screen shake.
- **User Interface:** Unity UI / uGUI (v2.0.0) with custom Canvas Scaler logic and procedural modal stacks.

### 2.2 Core Architectural Paradigms

```
┌────────────────────────────────────────────────────────┐
│                   PURE LOGIC LAYER                     │
│  - POCO Classes with Zero-GC mathematical algorithms   │
│  - MonsterTacticalEvaluator, AttackTokenDispatcher     │
│  - FarmGrid, StorylineEngine, WaveProgressionEngine    │
│  - 100% decoupled from UnityEngine lifecycle & objects │
└──────────────────────────┬─────────────────────────────┘
                           │ (Data / Invariants)
                           ▼
┌────────────────────────────────────────────────────────┐
│               THIN ADAPTER LAYER (Mono)                │
│  - EnemyBase, NightBrawlManager, FarmlandTile          │
│  - Passes Time.deltaTime, triggers animations/VFX      │
│  - Synchronizes Rigidbody physics & NavMesh movement   │
└──────────────────────────┬─────────────────────────────┘
                           │ (Events & Visuals)
                           ▼
┌────────────────────────────────────────────────────────┐
│                  PRESENTATION & UI                     │
│  - Canvas HUDs, Procedural Bars, ModalStackManager     │
└────────────────────────────────────────────────────────┘
```

1. **Pure Logic POCO vs Thin MonoBehaviour Adapter Separation:**
   All core game formulas (poise state machines, monster combat evaluations, wave quota math, grid soil timers, and storyline conditions) are written as pure C# Plain Old CLR Objects (POCO). They do not inherit from `MonoBehaviour`, perform zero runtime allocations (Zero-GC), and are verified using automated unit tests without requiring a running game simulation. `MonoBehaviour` classes serve strictly as thin adapters bridging Unity events, physics colliders, audio sources, and visual transforms to the pure logic engines.
2. **Centralized Modal Stack & Input Governance:**
   To prevent gameplay input leaks while interacting with menus, chests, stoves, or dialogue overlays, all UI windows register with the `ModalStackManager`. When a modal window opens, it claims an input lock on `PlayerControl.isInputLocked`, freezing movement and action triggers while absorbing raycasts.
3. **Homestead Perimeter 360° NavMesh Carving:**
   The homestead is enclosed by the `HomesteadPerimeter` compound barrier. It features dynamic `NavMeshObstacle` carving around all exterior fence boundaries while maintaining unobstructed clearance through the `MainEntranceGate`. Monster pathfinding dynamically routes around fences to breach through designated entrances.
4. **Day-Night Atmosphere Director & Lighting Synchronization:**
   Lighting is governed by the `LightingAndAtmosphereDirector` and `DaySunLightingObserver`. During the day, warm directional sunlight illuminates the farm, fading to soft twilight during dusk, and transitioning to a moody, dark slate tone at night. House safe zones automatically activate interior lamps, and the player's waist lantern provides 360-degree illumination during nighttime combat.

---

### 2.3 Modular Assembly Definition (.asmdef) Topology
The project codebase is partitioned into 17 isolated `.asmdef` modules to enforce strict dependency boundaries and accelerate incremental compilation:

```
                  ┌────────────────────────┐
                  │ FarmBeware.Core.Runtime│
                  └───────────┬────────────┘
                              │
             ┌────────────────┴────────────────┐
             ▼                                 ▼
┌────────────────────────┐         ┌────────────────────────┐
│ FarmBeware.Data.Runtime│         │FarmBeware.Features.Time│
└────────────┬───────────┘         └───────────┬────────────┘
             │                                 │
             ▼                                 ▼
┌────────────────────────┐         ┌────────────────────────┐
│FarmBeware.Features.Inv │         │FarmBeware.Gameplay.Run │
└────────────┬───────────┘         └───────────┬────────────┘
             │                                 │
             ├─────────────────────────────────┼──────────────────────────────┐
             ▼                                 ▼                              ▼
┌────────────────────────┐         ┌────────────────────────┐    ┌────────────────────────┐
│FarmBeware.Features.Farm│         │FarmBeware.Features.Comb│    │FarmBeware.Features.Kit │
└────────────────────────┘         └────────────────────────┘    └────────────────────────┘
```

1. `FarmBeware.Core.Runtime`: Core service contracts, singletons, state machines, and physics helpers.
2. `FarmBeware.Data.Runtime`: Shared ScriptableObject item definitions, registries, and serialization structs.
3. `FarmBeware.Gameplay.Runtime`: Player controller, stats, equipment, buff managers, and HUD bars.
4. `FarmBeware.Rendering.Runtime`: URP Render Graph features, vision mechanics, and lighting observers.
5. `FarmBeware.Features.Camera`: Isometric camera controller, wall occlusion, and room fade triggers.
6. `FarmBeware.Features.Combat`: Pure tactical AI, attack tokens, wave engine, enemy adapters, and combat UI.
7. `FarmBeware.Features.Economy`: Merchant shop interface, transaction services, and player wallet.
8. `FarmBeware.Features.Farming`: Farmland grid, tile presenters, water well, and crop visuals.
9. `FarmBeware.Features.Interaction`: Interaction zones, interactables, hover labels, and modal stack governance.
10. `FarmBeware.Features.Inventory`: Slot containers, drag-and-drop controllers, item catalog, and chest UI.
11. `FarmBeware.Features.Kitchen`: Multi-burner stove, washing sink, 100L water bottle, and recipe registry.
12. `FarmBeware.Features.SaveSystem`: Game save data structures, save stations, and persistence managers.
13. `FarmBeware.Features.Storyline`: Storyline pure logic engine, journal catalog, beats, and narrative overlays.
14. `FarmBeware.Features.Time`: Day/night time manager, activity timeline, clocks, and atmospheric directors.
15. `FarmBeware.Features.Trophy`: Trophy cabinet interactable, display racks, and showcase controllers.
16. `FarmBeware.Features.Wardrobe`: Outfit mesh swapper, wardrobe UI, and bedroom mirror camera.
17. `FarmBeware.Features.Workbench`: Weapon upgrade UI, basic and special upgrade progression trees.

---

## 3. Scene & Hierarchy Structure

### 3.1 StagingScene Semantic Architecture
The main gameplay environment (`Assets/Scenes/StagingScene.unity`) is structured into 7 distinct semantic layers:

```
StagingScene (Active Scene)
├── _SYSTEMS
│   ├── CameraManager [CameraManager]
│   ├── GameManager [GameInitializer, TimeManager, DayNightTimeManager, CoreBootstrapper]
│   ├── EventSystem [InputSystemUIInputModule]
│   ├── WallOcclusionManager [WallOcclusionManager]
│   ├── TrophySystemManager [TrophySystemManager]
│   ├── FarmingManager [FarmingManager]
│   ├── VisionManager [VisionManager]
│   ├── DailyEconomyManager [DailyEconomyManager]
│   ├── SaveSystemManager [SaveSystemManager]
│   ├── NightBrawlManager [NightBrawlManager, EnemyObjectPool]
│   ├── CombatProjectilePool [CombatProjectilePool]
│   └── StorylineManager [StorylineManager]
├── _LIGHTING_ENVIRONMENT
│   ├── Directional Light [Light, DaySunLightingObserver]
│   ├── HouseSafeZoneLights [HouseSafeZoneLighting]
│   │   ├── Room_Bedroom
│   │   ├── Room_Kitchen
│   │   ├── Room_Foyer
│   │   ├── Room_LivingRoom
│   │   ├── Room_Hallway
│   │   └── Room_Warehouse
│   ├── OutdoorGardenLamps [OutdoorLightingController]
│   │   ├── GardenLamp_MerchantTable
│   │   ├── GardenLamp_PondPier
│   │   └── GardenLamp_MidPath
│   ├── Indirect_Lighting [Reflection_Probes, World_LightProbeGroup]
│   └── Volumes_Atmosphere [Global Volume, Interior_Volume]
├── _CAMERAS
│   └── Main Camera [Camera, AudioListener, IsometricCameraController]
├── _ENTITIES
│   └── Player [Rigidbody, CapsuleCollider, PlayerControl, PlayerInteractor, PlayerOutfit,
│               InventoryComponent, PlayerStats, PlayerEquipment, PlayerBuffManager, 
│               PlayerWallet, VisionSource, PlayerWeaponUpgradeState, PlayerWaterBottle]
│       ├── character (Model & Animator)
│       ├── PlayerLantern (Waist 360° Light Rig)
│       ├── NightAtmosphere_Dust (Particle FX)
│       └── AttackRangeIndicator (Projector Decal)
├── _WORLD
│   ├── Terrain [Terrain, TerrainCollider]
│   ├── Structures
│   │   ├── House_UnifiedFloorCollider (Box Collider sub-floor)
│   │   ├── House (ProBuilder Walls, Doors, Windows)
│   │   ├── HomesteadPerimeter (360° NavMeshObstacle Carved Fence)
│   │   ├── WaterWell (Stone Well Interactable)
│   │   ├── RelaxationArea_Campfire
│   │   ├── GardenEnclosure
│   │   ├── Scarecrow
│   │   ├── NaturalPond
│   │   ├── CraftingShelter
│   │   └── OutdoorWorkshopStorage
│   ├── Zones [CompoundPerimeter, RoomZones, InteriorTriggers]
│   ├── Foliage [Bushes 00-10, Stylized Pine & Oak Trees]
│   └── Pathways [CobblestonePathways]
├── _GAMEPLAY
│   ├── SpawnPoints (Bedroom, Foyer, Kitchen, Front Door, Warehouse)
│   ├── InteractiveStations (StorageChest, Workbench, MerchantTable_Outdoor)
│   ├── MonsterSpawnPoints (SpawnPoint_FrontGate_Left, Center, Right)
│   └── Farmland (FarmlandPlots 1 to 16, Garden Beds Rows 1 to 4)
├── _UI
│   ├── UI_Canvas [Canvas, CanvasScaler, GraphicRaycaster]
│   │   ├── HUD_Hotbar & HUD_HotbarPanel
│   │   ├── HUD_BuffBar & HUD_BuffTooltip
│   │   ├── HUD_CombatPhaseTracker (Triple Clock Display)
│   │   ├── HUD_NightBrawlWaveTracker
│   │   ├── HUD_FloatingCombatText
│   │   ├── HUD_PlayerStatsPanel (Health, Stamina, Hunger, Thirst)
│   │   ├── Inventory_UI_Group
│   │   ├── Panel_MerchantShop
│   │   ├── Panel_Workbench
│   │   ├── PauseMenuUI
│   │   ├── DayTransition_Panel
│   │   ├── StoryBannerPanel, DialoguePanel, OverlayPanel, DreamPanel
│   │   ├── JournalUI
│   │   ├── EnemyHealthBarContainer
│   │   └── BossHealthBarPanel
│   └── InventoryUI [InventoryManagerUI]
└── ModalStackManager [ModalStackManager]
```

### 3.2 MainMenuScene Architecture
The title environment (`Assets/Scenes/MainMenuScene.unity`) coordinates entry into the game loop:
- **Title Canvas:** Hosts the *New Game*, *Load Game*, *Settings*, and *Exit* interactive buttons.
- **Menu Animators:** Governed by `MainMenuController`, `FantasyMenuAnimator`, `HorrorMenuAnimator`, and `PadiDaunAnimator` (providing procedural sway animations for Indonesian rice and leaf corner motifs).
- **Settings Overlay:** Houses the audio volume sliders, resolution settings, and full-screen toggles managed by `SettingsUIController`.
- **Audio Ambience:** Transitions from serene acoustic morning guitar melodies to unsettling night ambient drones upon selecting load options.

---

## 4. Comprehensive Class Catalogue (Zero Source Code)

The following tables document all classes, interfaces, structs, and enumerations across the project, organized by assembly and feature module.

### 4.1 FarmBeware.Core.Runtime
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `CoreBootstrapper` | Class | `MonoBehaviour` | Initializes core runtime services, loads default game state, and configures engine frame rates. |
| `GameInitializer` | Class | `MonoBehaviour` | Bootstraps subsystem lifecycles upon scene awakening. |
| `ModalStackManager` | Class | `MonoBehaviour`, `IModalStackService` | Centralized coordinator managing active modal windows and enforcing input locks on the player. |
| `StateMachine<T>` | Class | `IStateMachine<T>` | Generic, allocation-free finite state machine implementation for game states. |
| `CanvasHelper` | Static Class | None | Mathematical utility calculating safe areas, screen scale factors, and UI anchor positioning. |
| `NonAllocPhysics` | Static Class | None | Allocation-free wrapper for Unity physics queries using pre-allocated buffer caches. |
| `PerformanceManager`| Static Class | None | Profiling and framerate regulation utility. |
| `ServiceLocator` | Static Class | None | Lightweight decoupled registry for looking up global manager references without singleton coupling. |
| `UIModalHelper` | Static Class | None | Helper methods for animating modal alpha fades and canvas group raycast blocking. |
| `IBedInteractable` | Interface | None | Contract defining sleep permissions and time advancement actions on bed entities. |
| `ICameraService` | Interface | None | Contract for requesting camera shakes, mode transitions, and focus targeting. |
| `IDailyEconomyService` | Interface | None | Contract for logging daily revenue, tool purchase debits, and selling prices. |
| `IDailyReportService` | Interface | None | Contract for displaying the end-of-day revenue, crop summary, and monster tally modal. |
| `IDynamicLabelProvider` | Interface | None | Contract providing context-sensitive interact labels (e.g., "Harvest Corn" vs "Till Soil"). |
| `IFadeService` | Interface | None | Contract for triggering full-screen black transitions and scene fades. |
| `IFarmingService` | Interface | None | Contract for querying soil tile hydration, crop growth stages, and plot states. |
| `IFloatingTextService`| Interface | None | Contract for spawning floating damage numbers, healing indicators, and gold gain texts. |
| `IInventorySlot` | Interface | None | Contract defining item data, stack capacity, and occupancy queries for container slots. |
| `IModalStackService` | Interface | None | Contract for pushing, popping, and inspecting the active UI modal stack. |
| `IModalWindow` | Interface | None | Contract implemented by all interactive menus requiring input interception and focus management. |
| `IObstructionExempt` | Interface | None | Contract marking objects (e.g., water well, doorway) that should not be faded by wall occluders. |
| `IOccluder` | Interface | None | Contract for meshes that cast see-through dither patterns when obstructing the player. |
| `IPlayerContext` | Interface | None | Read-only contract exposing player transform, movement velocity, and interaction states. |
| `IPlayerDamageNotifier` | Interface | None | Event dispatcher notifying external observers when the player receives physical damage. |
| `ISinkDropHandler` | Interface | None | Contract for processing item drag-and-drop operations into the washing sink. |
| `ISlotProgressProvider`| Interface | None | Contract exposing operation timers (cooking, washing, refilling) for UI progress bars. |
| `IState<T>` | Interface | None | Generic interface defining `Enter`, `Update`, and `Exit` hooks for state machine states. |
| `IStateMachine<T>` | Interface | None | Contract defining state transition execution and current state queries. |
| `IStorageContainer` | Interface | None | Contract defining item extraction, insertion, and inventory syncing for chests. |
| `IStorylineService` | Interface | None | Contract for triggering story beats, querying journal unlock status, and evaluating narrative flags. |
| `ITimeService` | Interface | None | Contract exposing current in-game hour, minute, day count, and day phase. |
| `ITrophyService` | Interface | None | Contract managing unlocked boss trophies and shelf display points. |
| `IWalletService` | Interface | None | Contract for querying gold balance, depositing currency, and processing purchases. |
| `IWaterService` | Interface | None | Contract for querying water bottle volume, consuming water for cooking, and well refilling. |
| `CameraMode` | Enum | None | Enumerates camera operational modes (IsometricFollow, WardrobeFocus, MirrorInspect, Cutscene). |
| `DayPhase` | Enum | None | Enumerates environmental time periods (Dawn, Day, Dusk, Night). |

---

### 4.2 FarmBeware.Data.Runtime
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `ItemData` | Class | `ScriptableObject` | Base data asset defining item identifier, display name, sprite icon, rarity, and stack limit (20). |
| `ItemRegistrySO` | Class | `ScriptableObject`, `IItemCatalog` | Central ScriptableObject catalog storing lookup tables for all registered items in the game. |
| `GameSaveData` | Class | None | Master POCO save container serializing player stats, inventory, farm tiles, storyline, and time. |
| `SavedContainerInventory` | Class | None | Serialized container payload capturing slot index, item ID, and quantity for storage chests. |
| `SavedEnemyData` | Class | None | Serialized representation of enemy positions, health values, and active wave parameters. |
| `SavedFarmlandTile` | Class | None | Serialized representation of farmland plots (plot index, soil state, crop ID, growth progress). |
| `SavedInventorySlot` | Class | None | Serialized entry representing an individual inventory slot item, stack count, and durability. |
| `SaveManifest` | Class | None | High-level save index containing metadata for all available save slots on disk. |
| `SaveMetadata` | Class | None | Save slot header holding timestamp, day number, player level, and thumbnail reference. |
| `IItemCatalog` | Interface | None | Contract providing item lookup by unique string identifier or enum category. |
| `ItemCategory` | Enum | None | Categorizes items (Tool, Seed, Crop, Food, Material, MonsterDrop, Trophy). |
| `ItemType` | Enum | None | Granular classification of item utility (Consumable, Weapon, Equipment, QuestItem). |
| `ItemRarity` | Enum | None | Defines item tier quality (Common, Uncommon, Rare, Epic, Legendary). |
| `FoodCategory` | Enum | None | Categorizes food types (Recovery, Stamina, BattleBuff, Feast). |

---

### 4.3 FarmBeware.Gameplay.Runtime
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `PlayerControl` | Class | `MonoBehaviour`, `IPlayerContext` | Player locomotion controller processing Rigidbody velocity, smooth wall-sliding, sprinting, and attacks. |
| `PlayerStats` | Class | `MonoBehaviour`, `IDamageable`, `IPlayerDamageNotifier` | Manages player Health, Stamina, Hunger, and Thirst pools, taking damage and applying stat drains. |
| `PlayerBuffManager` | Class | `MonoBehaviour` | Ticks active food buffs, calculates stat multipliers, and purges all buffs upon sleeping. |
| `PlayerEquipment` | Class | `MonoBehaviour` | Handles active tool and weapon slots, instantiating visual handheld models and hitboxes. |
| `PlayerRespawnController` | Class | `MonoBehaviour` | Coordinates death transitions, camera pans, currency penalties, and respawning in the bedroom. |
| `PlayerStarterKit` | Class | `MonoBehaviour` | Populates the player inventory with starter tools (Hoe, Water Bottle, Starter Seeds) on New Game. |
| `PlayerInputActions` | Class | Generated Input Wrapper | Generated C# wrapper for Unity Input System asset binding keyboard and gamepad vectors. |
| `ActiveBuff` | Class | None | POCO data holder tracking active buff effect type, remaining duration, and magnitude. |
| `ActiveBuffHUDController` | Class | `MonoBehaviour` | Manages procedural buff icons on the player HUD with radial cooldown sweep animations. |
| `BuffSlotUI` | Class | `MonoBehaviour`, Pointer Handlers | Renders an individual buff icon, border rarity, and duration fill ring. |
| `BuffTooltipUI` | Class | `MonoBehaviour` | Displays detailed stat modifier descriptions when hovering over active buff icons. |
| `DeathScreenUI` | Class | `MonoBehaviour`, `IModalWindow` | Modal display presented upon player death with restart, reload, and surrender options. |
| `DeathMenuButtonAnimator` | Class | `MonoBehaviour`, Pointer Handlers | Button animation handler providing punch-scale and color tint hover feedback on death menus. |
| `EquipmentSlotUI` | Class | `MonoBehaviour`, Pointer Handlers | UI representation of equipment slots (Handheld, Outfit, Accessory). |
| `FloatingCombatTextManager` | Class | `MonoBehaviour`, `IFloatingTextService` | Pools and animates floating world-space text items above hit characters. |
| `FloatingTextItem` | Class | `MonoBehaviour` | Individual animated text mesh drifting upwards and fading alpha over time. |
| `PlayerStatsDisplayUI` | Class | `MonoBehaviour` | Procedural UI controller binding Health, Stamina, Hunger, and Thirst bar sliders to PlayerStats. |
| `PlayerHealthUI` | Class | `MonoBehaviour` | Bar adapter binding player current and maximum hit points to an Image fill amount. |
| `PlayerStaminaUI` | Class | `MonoBehaviour` | Bar adapter binding player stamina consumption and exhaustion states to UI feedback. |
| `PlayerHungerUI` | Class | `MonoBehaviour` | Bar adapter tracking hunger decay and starvation penalties. |
| `PlayerThirstUI` | Class | `MonoBehaviour` | Bar adapter tracking dehydration decay and stamina regeneration debuffs. |
| `PlayerActions` | Struct | None | Immutable data struct holding raw movement and action vectors from the input device. |
| `IPlayerActions` | Interface | None | Contract defining input callbacks for movement, sprinting, attacking, interacting, and inventory. |
| `EquipmentSlotType` | Enum | None | Identifies equipment attachment slots (PrimaryTool, SecondaryTool, Torso, Head). |
| `RangeIndicatorVisibility`| Enum | None | Controls display behavior of the weapon swing reach circle on the ground. |

---

### 4.4 FarmBeware.Rendering.Runtime
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `WorldSpaceVisionFeature` | Class | `ScriptableRendererFeature` | Custom URP Render Graph pass rendering dynamic vision cone stencil cuts and night fog masks. |
| `DaySunLightingObserver` | Class | `MonoBehaviour` | Updates directional light intensity, color temperature, and shadow bias based on the time of day. |
| `EnvironmentLightingTheme` | Class | `ScriptableObject` | Data container defining ambient gradient colors, sun angles, and fog density per day phase. |
| `VisionManager` | Class | `MonoBehaviour` | Coordinates all active light and vision sources, passing transform matrices to shaders. |
| `VisionSource` | Class | `MonoBehaviour` | Marks an entity (Player Lantern, Garden Lamp) as an illuminator piercing night fog. |
| `VisionTarget` | Class | `MonoBehaviour` | Marks an entity (Monsters, Hidden Chests) requiring illumination to become fully visible. |
| `HeroNightHighlightReceiver` | Class | `MonoBehaviour` | Applies a subtle rim light and fresnel shader override to the player model at night. |
| `DayLightingValidationHarness`| Class | `MonoBehaviour` | Debug harness validating illuminance lux values across daytime transitions. |
| `DeferredPointLightStressTest`| Class | `MonoBehaviour` | Stress-testing component evaluating GPU fill-rate across 100+ active point lights. |
| `GPUResidentDrawerStressTest` | Class | `MonoBehaviour` | Benchmark script validating instanced drawing performance on foliage and crops. |
| `EnvironmentPhase` | Enum | None | Classifies lighting phases (MorningSun, HighNoon, DuskGold, MidnightHorror). |

---

### 4.5 FarmBeware.Features.Camera
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `CameraManager` | Class | `MonoBehaviour`, `ICameraService` | Global coordinator delegating camera focus, field-of-view blends, and priority virtual cameras. |
| `IsometricCameraController` | Class | `MonoBehaviour` | Controls the main isometric follow camera, smoothing target tracking and applying position offsets. |
| `IsometricCamera` | Class | `MonoBehaviour` | Legacy camera rig maintaining fixed 45-degree pitch and orthographic projection ratios. |
| `WallOccluder` | Class | `MonoBehaviour`, `IOccluder` | Casts raycasts from camera to player, swapping obstructed wall materials to transparent dither shaders. |
| `WallOcclusionManager` | Class | `MonoBehaviour` | Maintains registration of all active occluders and processes raycast occlusion sweeps without GC. |
| `WallOcclusionGroup` | Class | `MonoBehaviour` | Aggregates multiple sub-meshes of a room into a unified fade group. |
| `WallOcclusionFader` | Class | `MonoBehaviour` | Lerps wall material transparency alpha when the player moves behind structural geometry. |
| `RoomOcclusionTrigger` | Class | None | Trigger zone marking an enclosed room to fade roof meshes when entered. |
| `CameraMode` | Enum | None | Operational mode of the camera (FreeFollow, LockedStation, InspectZoom). |

---

### 4.6 FarmBeware.Features.Combat
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `EnemyBase` | Class | `MonoBehaviour`, `IDamageable` | Base adapter for monster entities coordinating NavMeshAgent movement, health, attack states, and audio. |
| `NightBrawlManager` | Class | `MonoBehaviour` | Master controller managing nighttime horde spawning, wave timers, milestone banners, and clear checks. |
| `EnemyObjectPool` | Class | `MonoBehaviour` | Object pooling system recycling enemy prefabs to eliminate runtime instantiations. |
| `WeaponHitbox` | Class | `MonoBehaviour` | Trigger volume attached to weapons detecting collisions with enemies and applying damage. |
| `CombatHitVfxEngine` | Class | `MonoBehaviour` | Spawns directional blood splatters, spark particles, and slash decals upon confirmed hits. |
| `HitstopCoordinator` | Class | `MonoBehaviour` | Freezes frame animation momentarily upon high-impact hits to provide tactile combat punch. |
| `TraumaCameraShake` | Class | `MonoBehaviour` | Non-linear camera shake coordinator using Perlin trauma decay formulas. |
| `WeaponTrailController` | Class | `MonoBehaviour` | Activates and gates weapon trail mesh ribbons exclusively during nocturnal combat phases. |
| `CombatTelegraphDecal` | Class | `MonoBehaviour` | Projects expanding red warning decals on the ground prior to heavy monster skill attacks. |
| `CombatProjectile` | Class | `MonoBehaviour` | Projectile entity (e.g. Corn Musketeer kernel thorns) moving with ballistic velocity and raycast collision. |
| `CombatProjectilePool` | Class | `MonoBehaviour` | Pre-allocated pool managing reusable combat projectile instances. |
| `AirborneHazardEntity` | Class | `MonoBehaviour` | Controls falling projectile hazards (e.g., Taro Colossus boulders, Corn Heavens Fall cobs). |
| `WorldItemPickup` | Class | `MonoBehaviour` | Interactive loot drop entity drifting on the ground; preserves items if player inventory is full. |
| `BossSummonAbility` | Class | `MonoBehaviour` | Ability script executing minion spawn bursts for boss entities. |
| `ColossusAirborneAbility` | Class | `MonoBehaviour` | Coordinates the Taro Colossus high-jump leap, shadow tracking, and ground slam sequence. |
| `ColossusGrappleAbility` | Class | `MonoBehaviour` | Detects player proximity during Colossus shadow alignment, executing a ground slam grapple. |
| `RangerEnrageAbility` | Class | `MonoBehaviour` | Monitors Ranger boss health, triggering the 20-projectile Kernel Burst frenzy below 50% HP. |
| `EnemyData` | Class | `ScriptableObject` | Data asset defining base health, armor, movement speed, contact damage, and loot tables for an enemy. |
| `BossHealthBarManager` | Class | `MonoBehaviour` | UI controller managing boss health presentation at the top of the screen. |
| `BossHealthBarSlotUI` | Class | `MonoBehaviour` | Individual animated boss health bar slot with dual-layer fill and name labels. |
| `EnemyHealthBarManager`| Class | `MonoBehaviour` | Pools and aligns overhead mini health bars above damaged standard monsters. |
| `EnemyOverheadBarUI` | Class | `MonoBehaviour` | World-space mini bar displaying current health and poise status above a monster. |
| `EnemyOffscreenIndicatorUI`| Class | `MonoBehaviour` | Renders perimeter screen arrows pointing toward active monsters outside the camera view. |
| `EnemyIndicatorArrow` | Class | `MonoBehaviour` | Individual HUD arrow rotating towards an offscreen enemy's screen-space coordinates. |
| `NightBrawlWaveUI` | Class | `MonoBehaviour` | Displays wave countdowns, remaining enemy counts, and victory announcement banners. |
| `WaveIndicator` | Class | `MonoBehaviour` | Visual marker indicating incoming spawn direction along the farm perimeter. |
| `HitFeedbackRenderer` | Class | `MonoBehaviour` | Flashes character mesh materials bright white/red upon receiving damage. |
| `FloatingCombatTextManager`| Static Class | None | Static utility for dispatching combat numbers. |
| `EnemyPrefabFactory` | Static Class | None | Factory mapping enemy enum types to their respective pooled prefabs. |
| `EnemyLootDropHandler` | Static Class | None | Evaluates drop percentage matrices, spawning gold coins and monster materials upon death. |
| `MonsterTacticalEvaluator`| Class | None | **Pure Logic:** Calculates encirclement ring positions, kiting paths, and ambient wandering vectors. |
| `AttackTokenDispatcher` | Class | None | **Pure Logic:** Regulates enemy attack permissions to prevent simultaneous multi-enemy stuns on the player. |
| `WaveProgressionEngine` | Class | None | **Pure Logic:** Calculates enemy quotas, composition ratios, and wave milestones per night. |
| `CombatStateEvaluator` | Class | None | **Pure Logic:** Evaluates attacker damage vs target armor, calculating true damage and knockback. |
| `DefenseEvaluator` | Class | None | **Pure Logic:** Computes damage reduction percentages based on active defense buffs and armor bark. |
| `PoiseTracker` | Class | None | **Pure Logic:** Tracks cumulative impact damage on an entity, triggering staggers when threshold is breached. |
| `MeleeCombatStateMachine`| Class | None | **Pure Logic:** State machine governing player weapon combos (Swing1, Swing2, Finisher, Recovery). |
| `AttackDefinition` | Struct | None | Immutable data describing attack damage, knockback impulse, telegraph time, and poise damage. |
| `CombatStatModifiers` | Struct | None | Struct holding active damage, attack speed, and critical chance multipliers. |
| `LootDropEntry` | Struct | None | Defines item reference, minimum/maximum drop quantity, and percentage chance. |
| `NightScheduleSummary` | Struct | None | Summary struct containing total waves, enemy count, and estimated night duration. |
| `WaveMilestoneData` | Struct | None | Data describing wave composition, delay timers, and introduction banners. |
| `IDamageable` | Interface | None | Contract implemented by any entity capable of taking damage, knockback, and poise loss. |
| `EnemyType` | Enum | None | Enumerates monster types (TuberMaw, CyclopsTuberMaw, TaroBrute, TaroColossus, CornMusketeer, TheRanger). |
| `MonsterTacticalState` | Enum | None | Evaluator states (Encircling, Charging, Telegraphing, Recovering, Kiting, Fleeing). |
| `AttackTokenPriority` | Enum | None | Token queue priority (Standard, HighImpact, BossSpecial). |
| `CombatActionID` | Enum | None | Action identifiers for combat evaluation. |
| `CombatCommand` | Enum | None | Commands issued from tactical evaluators to thin adapters. |
| `MeleeCombatState` | Enum | None | Player combat animation states (Idle, Windup, ActiveHitbox, Recovery). |
| `StaggerTier` | Enum | None | Severity of poise break (None, LightFlinch, HeavyStumble, FullKnockdown). |

---

### 4.7 FarmBeware.Features.Economy
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `DailyEconomyManager` | Class | `MonoBehaviour`, `IDailyEconomyService` | Tracks daily financial earnings from crops, monsters, and seed expenditures. |
| `PlayerWallet` | Class | `MonoBehaviour`, `IWalletService` | Manages the player's gold balance, broadcasting balance change events to UI listeners. |
| `MerchantShopUI` | Class | `MonoBehaviour`, `IModalWindow` | Fullscreen modal shop interface for purchasing seeds, cooking spices, and equipment blueprints. |
| `MerchantTableInteractable`| Class | `MonoBehaviour`, `IInteractable` | Outdoor merchant stand interactable opening the shop menu. |
| `DailyReportModalUI` | Class | `MonoBehaviour`, `IModalWindow`, `IDailyReportService` | Modal window summarizing end-of-day farming income, expenses, and net profit. |
| `ShopTab` | Enum | None | Tab categories in the merchant shop (Seeds, Ingredients, Upgrades, Tools). |

---

### 4.8 FarmBeware.Features.Farming
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `FarmingManager` | Class | `MonoBehaviour`, `IFarmingService` | Central coordinator of the farm grid, tracking plot hydration and crop growth intervals. |
| `FarmlandTile` | Class | `MonoBehaviour`, `IInteractable`, `IDynamicLabelProvider` | Represents an individual garden plot handling hoe tilling, seed sowing, and crop harvesting. |
| `FarmlandTileInteractable`| Class | `MonoBehaviour`, `IInteractable` | Interaction trigger forwarder forwarding player tool clicks to the tile presenter. |
| `FarmlandTilePresenter` | Class | `MonoBehaviour` | Thin adapter synchronizing visual mesh states (Untilled, Tilled Dry, Tilled Wet) with core soil state. |
| `FarmGridManager` | Class | `MonoBehaviour` | Coordinates spatial plot lookups, finding adjacent tiles and managing grid bounds. |
| `FarmGridVisualizer` | Class | `MonoBehaviour` | Renders editor gizmos and debug boundaries for the garden plots. |
| `CropVisualController` | Class | `MonoBehaviour` | Swaps 3D crop mesh models (Sprout, Stage1, Stage2, Mature) based on growth percentages. |
| `PlantWaterer` | Class | `MonoBehaviour` | Handheld watering can attachment applying water charges and instantiating water droplet VFX. |
| `PlayerFarmInteraction`| Class | `MonoBehaviour` | Coordinates player tool targeting, grid snapping, and soil action execution. |
| `WaterWellInteractable`| Class | `MonoBehaviour`, `IObstructionExempt`, `IInteractable` | Ancient stone well interactable allowing the player to refill the 100L water bottle. |
| `CropGrowthCountdownUI` | Class | `MonoBehaviour` | World-space floating timer indicating time remaining until a crop matures. |
| `CropData` | Class | `ScriptableObject` | Data asset defining growth duration (seconds), seed item, mature item, dirty item, and visual stages. |
| `CropStageVisualDefinition`| Struct | None | Defines mesh asset reference, scale factor, and particle effect for a growth stage. |
| `FarmGrid` | Class | None | **Pure Logic:** Grid data structure indexing soil tiles and running growth ticks. |
| `SoilTile` | Class | None | **Pure Logic:** POCO tracking moisture level, soil condition, crop timer, and harvest readiness. |
| `SoilState` | Enum | None | Core soil conditions (Untilled, TilledDry, TilledWet). |
| `TileState` | Enum | None | Higher-level tile progression states (Empty, Planted, Growing, Mature, Withered). |

---

### 4.9 FarmBeware.Features.Interaction
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `PlayerInteractor` | Class | `MonoBehaviour` | Casts forward overlap sphere checks to find nearby `IInteractable` objects and display prompts. |
| `InteractionZone` | Class | `MonoBehaviour` | Trigger volume activating contextual interaction prompts upon player entry. |
| `Highlightable` | Class | `MonoBehaviour` | Applies shader highlight outlines to interactive objects when hovered or targeted. |
| `HoverLabelController` | Class | `MonoBehaviour` | Displays dynamic context-sensitive world-space labels above focused interactables. |
| `WorldLabel` | Class | `MonoBehaviour` | Individual world-space text label following an object's screen-projected coordinates. |
| `BedInteractable` | Class | `MonoBehaviour`, `IBedInteractable`, `IInteractable` | Interactive bedroom bed allowing the player to confirm sleep and transition to the next day. |
| `BedSleepConfirmationUI`| Class | `MonoBehaviour`, `IModalWindow` | Warning modal presented when sleeping early, cautioning if crops are close to ripening. |
| `StorageInteractable` | Class | `MonoBehaviour`, `IInteractable` | Storage chest interactable opening the chest inventory transfer panel. |
| `GenericFurnitureInteractable`| Class | `MonoBehaviour`, `IInteractable` | Generic interactable for ambient props (Chairs, Radios, Wardrobe mirrors). |
| `WaterRefillProgressUI`| Class | `MonoBehaviour` | Radial progress wheel displaying fill duration when refilling the water bottle at the stone well. |
| `LootDrop` | Struct | None | Data struct containing dropped item reference, drop position, and bounce velocity. |
| `IInteractable` | Interface | None | Core contract defining `Interact`, `CanInteract`, and `GetInteractPrompt` for all interactive entities. |

---

### 4.10 FarmBeware.Features.Inventory
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `InventoryComponent` | Class | `MonoBehaviour` | Manages player inventory slots (4 Hotbar + 12 Backpack), enforcing maxStack limits (20). |
| `InventoryManagerUI` | Class | `MonoBehaviour`, `IModalWindow` | Fullscreen inventory canvas coordinating slot layout, item drag-drop, and quick transfers. |
| `InventorySlotUI` | Class | `MonoBehaviour`, Pointer Handlers | Visual representation of an inventory slot handling click selection, drop events, and hover tooltips. |
| `DraggableItem` | Class | `MonoBehaviour`, Drag Handlers | Component attached to slot icons enabling smooth mouse drag operations across inventory panels. |
| `ItemDisplayUI` | Class | `MonoBehaviour` | Floating mouse icon rendering the item currently being dragged across the screen. |
| `InventorySlot` | Class | None | POCO container tracking item data reference, quantity count, and slot lock status. |
| `InventorySnapshot` | Class | None | Immutable snapshot of inventory state used for rollback safety during container transfers. |
| `InventoryTransactionScope`| Class | `IDisposable` | Transaction guard providing atomic multi-slot transfers with automatic rollback on failure. |
| `ItemDatabase` | Class | `ScriptableObject`, `IItemCatalog` | ScriptableObject database containing registry references to all game items. |
| `FoodItemData` | Class | `ItemData` | Item subtype defining food tier, health restoration, stamina boost, and buff duration. |
| `SeedItemData` | Class | `ItemData` | Item subtype defining associated crop data, planting requirements, and growth speed. |
| `MaterialItemData` | Class | `ItemData` | Item subtype defining crafting raw materials and trade values. |
| `MonsterMaterialItemData`| Class | `ItemData` | Item subtype defining monster body parts used in workbench weapon upgrades. |
| `ToolItemData` | Class | `ItemData` | Item subtype defining equipment category, durability, efficiency, and damage stats. |
| `TrophyItemData` | Class | `ItemData` | Item subtype defining boss trophies displayable on shelves. |
| `BuffEffectData` | Class | None | Encapsulates stat modifier type and numerical value applied upon consumption. |
| `InventorySlotSnapshot` | Struct | None | Value struct recording slot index, item ID, and quantity for transaction auditing. |
| `BuffType` | Enum | None | Types of buff effects (HealthRegen, MaxStamina, MovementSpeed, AttackPower, DefenseArmor). |
| `FoodTier` | Enum | None | Quality tier of prepared food (Small, Medium, Large, Feast). |
| `MaterialTier` | Enum | None | Classification of crafting components (Basic, Refined, Mutated, Apex). |
| `UpgradePath` | Enum | None | Weapon upgrade specialization paths (Speed/Mobility, Power/Defense, Area/Ballistic). |

---

### 4.11 FarmBeware.Features.Kitchen
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `KitchenStove` | Class | `MonoBehaviour`, `IInteractable` | Coordinates the 3-burner cooking stove, managing background simmer timers and food outputs. |
| `StoveInteractable` | Class | `KitchenStation`, `IInteractable`, `ISlotProgressProvider` | Interaction trigger opening the stove cooking interface. |
| `StoveUIManager` | Class | `MonoBehaviour`, `IModalWindow` | Modal window managing recipe selection, ingredient assignment, and cooking progress meters. |
| `KitchenSinkInteractable`| Class | `KitchenStation`, `IModalWindow`, `IInteractable`, `ISlotProgressProvider` | Interactive kitchen sink allowing players to wash dirty harvested crops into clean food ingredients. |
| `SinkManager` | Class | `MonoBehaviour` | Handles washing logic, converting dirty crops (e.g., Dirty Taro) to clean crops over time. |
| `SinkDragDropHandler` | Class | `MonoBehaviour`, Drag/Drop Handlers | Handles dragging dirty crops from hotbar/inventory directly into sink wash slots. |
| `SinkCloseButtonHandler`| Class | `MonoBehaviour` | UI button handler closing the sink wash panel. |
| `PlayerWaterBottle` | Class | `MonoBehaviour`, `IWaterService` | Represents the 100-liter water bottle, tracking volume used for crops, cooking, and hydration. |
| `RefrigeratorInteractable`| Class | `MonoBehaviour`, `IInteractable` | Refrigerator storage interactable providing 16 additional food ingredient storage slots. |
| `DoorInteractable` | Class | `MonoBehaviour`, `IObstructionExempt`, `IInteractable`, `IDynamicLabelProvider` | Room transition door providing localized labels (e.g. "Enter Kitchen") and teleportation. |
| `KitchenStation` | Abstract Class | `MonoBehaviour`, `ISlotProgressProvider` | Base class for interactive kitchen processing stations. |
| `KitchenStationUI` | Class | `MonoBehaviour` | UI base class rendering processing slot tracks and fill meters. |
| `KitchenStationProgressOverlay`| Class | `MonoBehaviour` | Displays world-space progress wheels directly over cooking pots and sinks. |
| `KitchenStationSoundFx`| Class | `MonoBehaviour` | Audio controller playing sizzling, boiling, and water splashing sound effects. |
| `BurnerSlotUI` | Class | None | UI component binding an individual stove burner to its recipe input and progress bar. |
| `CookingSlot` | Class | None | POCO holding active recipe, simmer progress timer, and ingredient escrow for one burner. |
| `CookingEscrowContainer`| Class | None | Container temporarily holding ingredients while cooking to prevent duplication exploits. |
| `KitchenRecipe` | Class | `ScriptableObject` | ScriptableObject defining required ingredients (3 items), water cost, cook time, and output dish. |
| `RecipeIngredient` | Class | None | Data pair specifying item identifier and required count for a recipe. |
| `CookingState` | Enum | None | State of a burner slot (Empty, ReadyToCook, Cooking, Cooked, Burned). |
| `SlotType` | Enum | None | Kitchen processing slot classification (WashSlot, BurnerSlot, OutputSlot). |
| `ThresholdAxis` | Enum | None | Directional axis used for door crossing detection. |

---

### 4.12 FarmBeware.Features.SaveSystem
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `SaveSystemManager` | Class | `MonoBehaviour` | Coordinates disk read/write operations, JSON serialization, and auto-save triggers on sleep. |
| `SaveStationInteractable`| Class | `MonoBehaviour`, `IObstructionExempt`, `IInteractable` | Interactive manual save station located in the bedroom. |
| `SaveSystemUI` | Class | `MonoBehaviour`, `IModalWindow` | Modal menu displaying available save slots, timestamps, day summaries, and overwrite prompts. |

---

### 4.13 FarmBeware.Features.Storyline
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `StorylineManager` | Class | `MonoBehaviour`, `IStorylineService` | Central adapter managing active story beats, evaluating trigger conditions, and driving narrative UI. |
| `StorylineEngine` | Class | None | **Pure Logic:** State machine evaluating beat unlock requirements, tracking completed events, and unlocking journals. |
| `StoryDatabase` | Static Class | None | Factory and static registry storing all default story beats and NexaPhyto archival journal logs. |
| `StoryBeatData` | Class | `ScriptableObject` | Data asset defining beat identifier, day trigger, trigger event type, delivery mode, and dialogue lines. |
| `JournalEntryData` | Class | `ScriptableObject` | Data asset defining collectible NexaPhyto archival report (entry ID, day unlocked, title, author, body text). |
| `StorylineSavePayload` | Class | None | Serialized save payload recording completed beat IDs and unlocked journal entry keys. |
| `StorylineUI` | Class | `MonoBehaviour`, `IModalWindow` | Coordinates narrative UI presentation across FullScreenOverlays, DialoguePanels, and BannerPopups. |
| `JournalUI` | Class | `MonoBehaviour`, `IModalWindow` | Archival journal viewer allowing the player to browse collected NexaPhyto dossiers and lore logs. |
| `BossIntroSequence` | Class | `MonoBehaviour` | Orchestrates dramatic boss introductions with letterbox bars, camera framing, and roar audio. |
| `StoryTriggerType` | Enum | None | Beat trigger criteria (GameStart, DayStart, NightStart, BossEncounter, BossDefeated, SoilTilled, Sleep). |
| `StoryDeliveryType` | Enum | None | Presentation style for narrative (FullScreenOverlay, DialogueBox, BannerPopup, DreamSequence). |

---

### 4.14 FarmBeware.Features.Time
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `DayNightTimeManager` | Class | `MonoBehaviour`, `ITimeService`, `IDayNightTimeService` | Core time clock simulating in-game minutes, day/night phase transitions, and day roll-overs. |
| `TimeManager` | Class | `MonoBehaviour` | Global time scale regulator managing play/pause and debug fast-forward multipliers (x8). |
| `DayActivityTimelineManager`| Class | `MonoBehaviour` | Logs timestamped player activities (planted crops, cooked meals, monsters slain) for the daily report. |
| `DayNightLightingController`| Class | `MonoBehaviour` | Interpolates directional sun angle, color tint, and ambient bounce lighting across the day cycle. |
| `DayNightVolumeController`| Class | `MonoBehaviour` | Blends post-processing volume weights (Color Adjustments, Bloom, Vignette) per day phase. |
| `LightingAndAtmosphereDirector`| Class | `MonoBehaviour` | High-level director coordinating sun, volumes, audio ambiance, and house lights across day phases. |
| `HouseSafeZoneLighting` | Class | `MonoBehaviour` | Manages indoor lighting fixtures, automatically turning on room lamps when dusk approaches. |
| `OutdoorLightingController`| Class | `MonoBehaviour` | Controls outdoor pathway and pier lanterns, turning them on during dusk and night. |
| `OutdoorGardenLamp` | Class | `MonoBehaviour` | Individual garden lamp fixture with emissive mesh materials and point lights. |
| `ShelterLanternController`| Class | `MonoBehaviour` | Manages lighting in the crafting shelter and outdoor storage area. |
| `DayNightAudioController` | Class | `MonoBehaviour` | Crossfades serene daytime birdsong/wind audio with ominous night insect and horror drone tracks. |
| `CombatPhaseTrackerUI` | Class | `MonoBehaviour` | HUD clock displaying Day badge, activity card, and current in-game time in 3 selectable modes. |
| `DayTransitionUI` | Class | `MonoBehaviour` | Full-screen banner overlay presenting stylized "Day X" and "Night Brawl" transition titles. |
| `RoomLightingGroup` | Class | None | Data group binding an individual room's light components to a common switch. |
| `LightEntry` | Class | None | Encapsulates a light reference with target daytime and nighttime lux intensities. |
| `DayActivityEntry` | Struct | None | Immutable record of an activity event (timestamp, category, description, gold value). |
| `VolumePhaseSettings` | Struct | None | Settings struct holding target post-processing values for a specific day phase. |
| `IDayNightTimeService` | Interface | None | Contract exposing time normalized ratios (0.0 to 1.0) and phase event listeners. |
| `ClockDisplayMode` | Enum | None | Display step modes for the clock: FullStep (60s), HalfStep (30s), QuarterStep (15s). |

---

### 4.15 FarmBeware.Features.Trophy
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `TrophySystemManager` | Class | `MonoBehaviour`, `ITrophyService` | Tracks boss defeat flags and activates 3D trophy models on the display cabinet. |
| `TrophyCabinetInteractable`| Class | `MonoBehaviour`, `IInteractable` | Interactive trophy shelf in the living room allowing players to inspect collected boss relics. |
| `TrophyItem` | Class | `MonoBehaviour` | Component attached to 3D trophy models with interaction tooltips and inspection cameras. |
| `TrophyRackVisuals` | Class | `MonoBehaviour` | Manages lighting and velvet pedastal shaders in the trophy display case. |
| `TrophySnapPoint` | Class | `MonoBehaviour` | Transform anchor defining placement position and rotation for a specific boss trophy. |

---

### 4.16 FarmBeware.Features.Wardrobe
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `WardrobeManager` | Class | `MonoBehaviour` | Coordinates unlocked player clothing items, saving and loading outfit customization states. |
| `WardrobeUI` | Class | `MonoBehaviour` | Modal wardrobe interface for selecting hats, shirts, pants, and boots. |
| `PlayerOutfit` | Class | `MonoBehaviour` | Component on the player character maintaining active skinned mesh renderer bindings. |
| `OutfitMeshSwapper` | Class | `MonoBehaviour` | Swaps skinned mesh bones and materials on the character rig when an outfit is selected. |
| `MirrorCamera` | Class | `MonoBehaviour` | Secondary camera rendering a close-up frontal view of the player character to a UI RenderTexture. |
| `WardrobeInteractable` | Class | `MonoBehaviour`, `IInteractable` | Interactive bedroom wardrobe cabinet opening the outfit selection menu. |
| `OutfitData` | Class | `ScriptableObject` | ScriptableObject defining outfit name, category, mesh asset, and thumbnail icon. |
| `WardrobeItemData` | Class | `ScriptableObject` | Data container defining individual clothing piece attributes. |
| `WardrobeSaveData` | Class | None | Serialized payload storing the player's equipped outfit identifiers. |
| `OutfitPartResolver` | Static Class | None | Helper mapping outfit slot categories to character rig bone hierarchies. |
| `ItemSlot` | Class | `MonoBehaviour`, Pointer Handlers | Visual wardrobe slot handling click selection of clothing options. |
| `PartConfig` | Struct | None | Struct pairing a body part category with its skinned mesh renderer reference. |
| `Category` | Enum | None | Outfit categories (Headwear, Top, Bottom, Footwear). |

---

### 4.17 FarmBeware.Features.Workbench
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `PlayerWeaponUpgradeState`| Class | `MonoBehaviour` | Tracks active weapon upgrade tiers, calculating damage, attack speed, and special skill modifiers. |
| `WorkbenchInteractable` | Class | `MonoBehaviour`, `IInteractable` | Interactive crafting table in the garage opening the weapon enhancement panel. |
| `WorkbenchUI` | Class | `MonoBehaviour`, `IModalWindow` | Modal menu displaying basic stat upgrades (Damage Lv.1-3) and special monster material paths. |

---

### 4.18 Assembly-CSharp (Global Menus & Transitions)
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `MainMenuController` | Class | `MonoBehaviour` | Orchestrates main menu scene flow, coordinating new game creation, save loading, and scene loading. |
| `MainMenuManager` | Class | `MonoBehaviour` | Legacy title coordinator. |
| `FantasyMenuAnimator` | Class | `MonoBehaviour` | Drives calm daytime camera drifts and golden leaf particle animations on the title screen. |
| `HorrorMenuAnimator` | Class | `MonoBehaviour` | Drives dramatic red lightning flickers and shadow silhouetting on the title screen. |
| `PadiDaunAnimator` | Class | `MonoBehaviour` | Procedural animator applying subtle sine-wave sway to rice stalks and corner leaf UI flourishes. |
| `PauseMenuAnimator` | Class | `MonoBehaviour` | Animates the in-game ESC pause menu with smooth backdrop blur and button slide-in tweens. |
| `SettingsUIController` | Class | `MonoBehaviour` | Binds settings sliders to master, music, and SFX audio mixers, and controls screen resolutions. |
| `SettingsBarAnimator` | Class | `MonoBehaviour` | Animates volume and brightness slider fill bars. |
| `SettingsButtonAnimator`| Class | `MonoBehaviour` | Animates button hover and click states in the settings menu. |
| `FullscreenToggleAnimator`| Class | `MonoBehaviour` | Animates full-screen checkbox toggle transitions. |
| `UIAnimationHandler` | Class | `MonoBehaviour` | General utility executing bounce and fade tweens on UI canvas groups. |
| `FadeManager` | Class | `MonoBehaviour` | Singleton fading a full-screen black canvas panel for scene transitions. |
| `CloudTransitionManager`| Class | `MonoBehaviour` | Drives decorative cloud wipes across the camera viewport during day phase shifts. |
| `HouseInteriorTrigger` | Class | `MonoBehaviour` | Trigger volume detecting player entry into the house to trigger wall fading and interior audio. |
| `CampfireLightFlicker` | Class | `MonoBehaviour` | Applies randomized Perlin noise intensity modulation to the relaxation area campfire light. |
| `AudioFeedbackType` | Enum | None | Audio cue types (ButtonHover, ButtonClick, ErrorBuzzer, SuccessJingle). |

---

### 4.19 Assembly-CSharp-Editor (Test Suites & Production Tools)
| Type Name | Kind | Inheritance / Interfaces | Architectural Responsibility |
|---|---|---|---|
| `GameplayAndHUDTests` | Static Class | None | Comprehensive 30-test automated audit suite validating time, bed, water, hud, ai, fence, and storyline. |
| `WaterWellRefillTests` | Static Class | None | Automated unit test suite verifying 4-stage water well refill cycles and bottle capacity limits. |
| `CombatAudioClipGenerator`| Static Class | None | Procedural audio utility generating procedural WAV hit impact and weapon swing audio assets. |
| `CombatAudioSetupUtility` | Static Class | None | Editor utility wiring generated audio clips to weapon and monster audio source components. |
| `DayNightLightingControllerEditor`| Class | `Editor` | Custom Inspector editor for `DayNightLightingController` with manual day scrubbing sliders. |
| `SceneLightingEditorUtility`| Static Class | None | Editor tool validating light probes, reflection probes, and static shadow casters. |
| `LightLayerAssignmentUtility`| Class | `EditorWindow` | Editor window auditing and assigning URP light layers across characters and environment meshes. |
| `LightingQualityBakeUtility`| Static Class | None | Editor utility configuring high-quality lightmap baking parameters for URP Deferred+. |
| `ProbeInfrastructureSetupUtility`| Static Class | None | Automated generator creating dense light probe grids across the house interior and farm plots. |
| `WallOcclusionGroupingUtility`| Static Class | None | Utility automating child mesh hierarchy grouping for `WallOcclusionGroup` components. |
| `NightCharacterHighlightSetupUtility`| Static Class | None | Editor tool configuring rim lighting shaders on player and enemy prefabs. |
| `URPGraphicsConfigurationValidator`| Class | `EditorWindow` | Audit window verifying that URP asset settings match target production fidelity guidelines. |
| `PlayModeStartSceneSetter`| Static Class | None | Editor helper ensuring play mode always begins from `MainMenuScene` unless testing. |

---

## 5. Verification & Audit Invariants

To guarantee that the codebase remains robust and stable following any project updates, the project maintains an automated test verification harness:

1. **Compilation Invariant:**
   - 0 compilation errors across all 17 assembly definitions.
   - Verified via `read_console` and `mcpforunity://editor/state`.
2. **Automated Audit Suite (30/30 Invariant):**
   - Executable command:
     `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`
   - Validates all 30 critical system invariants including continuous dusk simulation, bed sleep permissions, water bottle 4-charge depletion, 100L capacity persistence, vertical sink progress bars, procedural clock toggles, 360-degree perimeter fence navigation, and storyline state machines.
