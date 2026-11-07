# Roadmap.md

Roadmap and milestone tracking for **Farm-Beware** (*Production V0.3*).

---

## 1. Core MVP Game Loop

- **Day Phase**: Exploration, crop tilling/watering, cooking buffs, vehicle driving, trading.
- **Sleep Transition**: Bed interaction (`BedInteractable`) initiates night combat or advances day.
- **Night Phase**: Horde survival brawl against dynamic monster waves for loot and seeds.

---

## 2. Project Milestones

### Milestone 1: House, Kitchen & Foundation (COMPLETED ✅)
- [x] **Camera Architecture**: `CameraManager` handling state transitions.
- [x] **KitchenStove Engine**: Decoupled state machine (`CookingState`), event-driven UI, rollback.
- [x] **Dynamic Washing**: `KitchenSinkInteractable` with clean variant transformation.
- [x] **Item Database**: Central registry indexing items with max stack ceiling.
- [x] **Wardrobe & Mirror**: Secondary `MirrorCamera` with synchronized wall occlusion.
- [x] **Modal UI**: 1920×1080 canvas scaling and ESC modal priority stack.

### Milestone 1.5: Graphics, Shaders & Pipeline (COMPLETED ✅)
- [x] **Deferred+ Clustering & BRG**: Unity 6 Render Graph Deferred+ with GPU Resident Drawer.
- [x] **Shaders**: `MonsterFresnelLit.shader` and `DitheredBuildingLit.shader` (Bayer 4x4).
- [x] **Post-Processing**: ACES Tonemapping, Bloom, Motion Blur, chromatic aberration.

### Milestone 2: Combat Engine & Hardening (COMPLETED ✅)
- [x] **Combat FSM**: `CombatStateEvaluator` 3-hit combo progression, input buffering, cancel windows.
- [x] **Poise & Multi-Tier CC**: `PoiseTracker` with knockdown upgrade invariant and super armor recovery.
- [x] **Priority Token Dispatcher**: `AttackTokenDispatcher` with priority permits (`Minion < Elite < Boss`).
- [x] **Defensive Anti-Exploit**: `DefenseEvaluator` with 300ms dodge i-frames and 350ms whiff lockout.
- [x] **Dual-Tier Health Bars**: Zero-GC pooled overhead bars; cinematic boss HUD.

### Milestone 3: Persistence, Menus & Audit Suite (COMPLETED ✅)
- [x] **Multi-Slot Persistence**: `SaveLoadService` supporting 3 JSON slots and auto-save triggers.
- [x] **Automated Test Suite**: 85 comprehensive audit tests in `GameplayAndHUDTests.cs` (85/85 Passed).

### Milestone 4: Farming & Agriculture System (COMPLETED ✅)
- [x] **Farmland Grid**: 16 interactive plots with hoe tilling and moisture states.
- [x] **Crop Growth Engine**: Sweet Potato, Taro, Corn growth cycles, yields, `WorldItemPickup`.
- [x] **Watering Can**: 4-charge (25L) depletion, well refilling, moisture decay.

### Milestone 5: Economy, Trading Post & Western Roadway (COMPLETED ✅)
- [x] **Western Scenic Roadway**: 225m Catmull-Rom ribbon departing West from Homestead Gate.
- [x] **Commercial Trading Post**: Western Outpost at `(-175, 0, 70)`, 3D isometric depth staging.
- [x] **Daily Economy & Shop**: `DailyEconomyManager` price rolling, dirty discount, `MerchantShopUI`.
- [x] **Vehicles & Cutscenes**: `ArcadeVehicleController`, opening truck arrival, `StorylineManager`.

### Milestone 6: Weapon Forge & Deep Crafting Workbench (COMPLETED ✅)
- [x] **Weapon Upgrades & Specialization**: 10-level scaling (+15% to +150%), 3 paths with 1-path lock.
- [x] **Armor Upgrades & Equipment**: 10-level mitigation (0% to 32%), set crafting & drag-and-drop equipping.
- [x] **Contextual Combat Stance**: Night weapon auto-wield, GDD hotbar rules (hoe muted, food buffs active), dawn restoration.

---

## 3. Upcoming Milestones

### Milestone 7: Boss Encounter & Campaign Victory Polish (QUEUED 📋)
- [ ] **Multi-Phase Bosses**: Distinct attack telegraphs, arena hazards, and phase transitions.
- [ ] **Campaign Climax**: Victory conditions and endgame progression loops.
