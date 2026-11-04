# Roadmap.md

Development roadmap, milestone tracking, and feature goals for **Farm-Beware** (*Production V0.3*).

---

## 1. Core MVP Game Loop

- **Day Phase**: Free exploration, tilling/watering crops, cooking food buffs, vehicle driving, trading at market.
- **Sleep Transition**: Interacting with bed (`BedInteractable`) initiates night combat or advances to next day once night waves are defeated.
- **Night Phase**: Horde survival brawl against dynamic monster waves. Monster drops provide essential crafting materials and seeds.

---

## 2. Project Milestones

### Milestone 1: House, Kitchen & Foundation (COMPLETED ✅)
- [x] **Camera Architecture**: `CameraManager` handling state transitions (`Gameplay`, `TrophyMode`, `WardrobeMode`).
- [x] **KitchenStove Engine**: Decoupled state machine (`CookingState`), event-driven UI, atomic rollback on cancel.
- [x] **Dynamic Washing**: `KitchenSinkInteractable` with clean variant item transformation.
- [x] **Item Database**: Central registry indexing items with max stack ceiling (20 for items, 1 for gear).
- [x] **Wardrobe & Mirror**: Secondary `MirrorCamera` with synchronized wall occlusion.
- [x] **Modal UI**: 1920×1080 canvas scaling and ESC modal priority stack.

### Milestone 1.5: Graphics, Shaders & Pipeline (COMPLETED ✅)
- [x] **Deferred+ Clustering & BRG**: Unity 6 Render Graph Deferred+ with GPU Resident Drawer (`InstancedDrawing`).
- [x] **Shaders**: `MonsterFresnelLit.shader` (UniversalForwardOnly, instanced MPB) and `DitheredBuildingLit.shader` (Bayer 4x4 dither, Opaque).
- [x] **Post-Processing**: ACES Tonemapping, Bloom, Motion Blur, chromatic aberration combat impulses.

### Milestone 2: Combat Engine & Hardening (COMPLETED ✅)
- [x] **Pure POCO Combat FSM**: `CombatStateEvaluator` 3-hit combo progression, input buffering, early cancel windows.
- [x] **Poise & Multi-Tier CC**: `PoiseTracker` with knockdown tier-upgrade invariant and innate super armor auto-recovery.
- [x] **Priority Token Dispatcher**: `AttackTokenDispatcher` with priority permits (`Minion < Elite < Boss`).
- [x] **Defensive Anti-Exploit**: `DefenseEvaluator` with 300ms dodge i-frames and 350ms whiff lockout.
- [x] **Dual-Tier Health Bars**: Zero-GC pooled overhead bars; cinematic boss HUD with stacked layouts.

### Milestone 3: Persistence, Menus & 79-Test Suite (COMPLETED ✅)
- [x] **Multi-Slot Persistence**: `SaveLoadService` supporting 3 JSON save slots and auto-save triggers.
- [x] **Automated Test Suite**: 79 comprehensive audit tests in `GameplayAndHUDTests.cs` (79/79 Passed).

### Milestone 4: Farming & Agriculture System (COMPLETED ✅)
- [x] **Farmland Grid**: 16 interactive plots with hoe tilling and moisture states (`Dry` → `Tilled` → `Watered`).
- [x] **Crop Growth Engine**: Sweet Potato, Taro, Corn growth cycles, dirty vs clean yields, `WorldItemPickup`.
- [x] **Watering Can**: 4-charge (25L) depletion, well refilling, moisture decay.

### Milestone 5: Economy, Trading Post & Western Scenic Roadway (COMPLETED ✅)
- [x] **Western Scenic Roadway**: 225m Catmull-Rom paved ribbon departing West from Homestead Gate (`X = 20.4`).
- [x] **Commercial Trading Post**: Western Outpost at `(-175, 0, 70)`, 3D isometric depth staging (South-facing arcade).
- [x] **Daily Economy & Shop**: `DailyEconomyManager` price rolling, dirty produce discount, `MerchantShopUI`.
- [x] **Vehicles & Cutscenes**: `ArcadeVehicleController`, opening truck arrival, `StorylineManager` narrative beats.

---

## 3. Upcoming Milestones

### Milestone 6: Weapon Forge & Deep Crafting Workbench (NEXT PRIORITY ⏳)
- [ ] **Player Weapon Upgrades**: Taro (heavy knockback) and Sweet Potato (speed/crit) path progression.
- [ ] **Repair & Reforge**: Durability restoration and material affix attachment.

### Milestone 7: Boss Encounter & Campaign Victory Polish (QUEUED 📋)
- [ ] **Multi-Phase Bosses**: Distinct attack telegraphs, arena hazards, and phase transitions.
