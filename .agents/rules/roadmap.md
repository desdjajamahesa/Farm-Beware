# Roadmap.md

Development roadmap, milestone tracking, and feature goals for **Farm-Beware** (*MVP Guideline V0.1*).

---

## 1. Core MVP Game Loop

- **Day Phase**: Free exploration, tilling/watering crops, cooking food buffs, upgrading weapons at workbench, trading at market.
- **Sleep Transition**: Interacting with bed (`BedInteractable`) initiates night combat or advances to next day once night waves are defeated.
- **Night Phase**: Horde survival brawl against dynamic monster waves. Monster drops provide essential crafting materials and seeds.

---

## 2. Project Milestones

### Milestone 1: House, Kitchen & Foundation (COMPLETED ✅)
- [x] **Camera Architecture**: `CameraManager` handling state transitions (`Gameplay`, `TrophyMode`, `WardrobeMode`) and input locking.
- [x] **KitchenStove Engine**: Decoupled state machine (`CookingState`), event-driven UI, and atomic snapshot & rollback on cancel.
- [x] **Dynamic Washing**: `KitchenSinkInteractable` with dynamic clean variant item transformation.
- [x] **Item Database**: Central registry indexing all items with max stack ceiling (20 for items, 1 for gear).
- [x] **Wardrobe & Mirror**: Secondary `MirrorCamera` (portrait 0.5, depth -100, Base renderType) and synchronized wall occlusion.
- [x] **Modal UI**: 1920×1080 canvas scaling and ESC modal priority stack.

### Milestone 1.5: Graphics, Shaders & Pipeline (COMPLETED ✅)
- [x] **Deferred+ Clustering & BRG**: Unity 6 Render Graph Deferred+ with GPU Resident Drawer (`InstancedDrawing`).
- [x] **Day/Night Lighting**: Mathematical sun rotation and bitwise light layers (0 exterior, 1 interior).
- [x] **Shaders**: `MonsterFresnelLit.shader` (UniversalForwardOnly, instanced MPB `_HitFlashAmount`) and `DitheredBuildingLit.shader` (Bayer 4x4 dither, Opaque queue).
- [x] **Post-Processing**: ACES Tonemapping, Bloom, Motion Blur, and zero-GC chromatic aberration combat impulses.

### Milestone 2: Combat Engine & Hardening (COMPLETED ✅)
- [x] **Pure POCO Combat FSM**: `CombatStateEvaluator` with 3-hit combo progression, input buffering, early cancel windows, and `InterruptCombatSequence()`.
- [x] **Poise & Multi-Tier CC**: `PoiseTracker` with deterministic poise damage, knockdown tier-upgrade invariant, and innate super armor auto-recovery.
- [x] **Priority Token Dispatcher**: `AttackTokenDispatcher` with priority permits (`Minion < Elite < Boss`) and preemptive minion eviction to prevent boss starvation.
- [x] **Defensive Anti-Exploit**: `DefenseEvaluator` with 300ms dodge i-frames, 350ms parry window, and 350ms whiff recovery lockout to eliminate parry spamming.
- [x] **Physics & Lifecycle Hardening**: `AirborneHazardEntity` with CCD ContinuousDynamic, predictive boundary raycasts, safe NavMesh landing, and pooling cleanup in `EnemyBase.OnDisable()`.
- [x] **Dual-Tier Health Bars**: Zero-GC pooled overhead bars with amber ghost damage lag; cinematic boss HUD with single and dual-boss stacked layouts.

### Milestone 3: Persistence, Menus & Localization (COMPLETED ✅)
- [x] **Multi-Slot Persistence**: `SaveLoadService` supporting 3 JSON save slots and auto-save triggers.
- [x] **Menus**: Main menu and pause menu with settings, slot management, and audio sliders.
- [x] **100% English Mandate**: Complete translation of code, Inspector attributes, UI, and tooltips.

---

## 3. Upcoming Milestones

### Milestone 4: Farming System (NEXT PRIORITY ⏳)
- [ ] **Soil Grid & Tilling**: Interactive hoe grid raycasting and soil plot state machine (`Dry` → `Tilled` → `Watered`).
- [ ] **Watering Can**: Water depletion, refilling at well/pond, and moisture decay cycles.
- [ ] **Crop Growth Cycle**: Seed planting, multi-stage visual growth progression across day/night cycles, and harvest collection.

### Milestone 5: Workbench & Economy (QUEUED 📋)
- [ ] **Player Weapon Upgrades**: Taro (heavy knockback) and Sweet Potato (speed/crit) path progression.
- [ ] **Economy**: Day market stall for selling harvest and purchasing seeds.
