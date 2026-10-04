# Roadmap.md

Development roadmap, milestone tracking, and feature goals for **Farm-Beware** based on the *MVP Version Guideline V0.1*.

---

## 1. Core MVP Game Loop

```
            ┌────────────────────────┐
            │       DAY PHASE        │
            │ (Exploration & Prep)   │
            └───────────┬────────────┘
                        │
       ┌────────────────┴────────────────┐
       ▼                                 ▼
┌──────────────┐                  ┌──────────────┐
│   FARMING    │                  │   COMMERCE   │
│ (Crops/Soil) │                  │ (Sell/Store) │
└──────┬───────┘                  └──────┬───────┘
       │                                 │
       ▼                                 ▼
┌──────────────┐                  ┌──────────────┐
│   COOKING    │                  │  WORKBENCH   │
│ (Food Buffs) │                  │  (Upgrades)  │
└──────┬───────┘                  └──────┬───────┘
       │                                 │
       └────────────────┬────────────────┘
                        │ Bed Sleep
                        ▼
            ┌────────────────────────┐
            │      NIGHT PHASE       │
            │  (Night Brawl Waves)   │
            └───────────┬────────────┘
                        │
                        ▼
            ┌────────────────────────┐
            │     MONSTER DROPS      │
            │  (Crafting Materials)  │
            └────────────────────────┘
```

---

## 2. Project Milestones

### Milestone 1: House, Kitchen & Core Systems (COMPLETED ✅)
- [x] **Camera Architecture**: `CameraManager` handling state transitions (`Gameplay`, `TrophyMode`, `WardrobeMode`) and input locking.
- [x] **KitchenStove Refactor**: Decoupled state machine (`CookingState`), event-driven architecture, and atomic snapshot & rollback on cancel.
- [x] **Dynamic Washing**: `KitchenSinkInteractable` with item-level clean variant transformation.
- [x] **Item Database**: Central registry indexing 33 active items across all MVP categories.
- [x] **Wardrobe & Mirror**: `PlayerOutfit`, `MirrorCamera` (RenderTexture portrait 0.5, depth -100, Base renderType), wood frame material, and synchronized wall occlusion.
- [x] **UI Scaling & Modals**: 1920×1080 canvas scaling and ESC modal priority stack.

---

### Milestone 1.5: Graphics, Shaders & Lighting Pipeline (COMPLETED ✅)
- [x] **Deferred+ Clustering & BRG**: Unity 6 Render Graph Deferred+ rendering with GPU Resident Drawer (`InstancedDrawing`) and Static Batching Guard.
- [x] **Day/Night Lighting**: Soft shadows, mathematical sun rotation (5500K noon to 3200K dusk), and cool moonlight fill.
- [x] **Shaders**: `MonsterFresnelLit.shader` (`UniversalForwardOnly`) for HDR rim readability, and `DitheredBuildingLit.shader` (Bayer 4x4, Opaque queue) for seamless interior entry.
- [x] **Light Layers**: Bitwise isolation separating exterior sun (Layer 0) and interior lamps (Layer 1).
- [x] **Post-Processing & Juice**: Global Volume with ACES Tonemapping, Bloom, Motion Blur, and zero-GC chromatic aberration combat impulses.
- [x] **Foliage Optimization**: Near clip 0.08m, tree LOD crossfades, two-sided leaf materials (`_Cull = 0`), and camera proximity occlusion.

---

### Milestone 2: Night Brawl Combat Engine & Combat UI (COMPLETED ✅)
- [x] **Melee Combat Engine**:
  - 3-Hit combo sequence with generous reset window (`ComboResetWindow = 1.5f`) and early cancel threshold.
  - Attack buffering queue (`bufferedAttackTime <= 0.45f`) for seamless combo hits.
  - Special attacks: Sprint Dash Attack, Charged Heavy Attack (> 0.35s), Front Kick (`F`), and Jump Skill Slam.
- [x] **Dynamic Monster Spawners**:
  - Spawns gathered dynamically from `MonsterSpawnPoints` children outside front gate (`Z >= 52m`).
  - NavMesh surface raycasting and snapping.
- [x] **5-Day Wave Progression**:
  - Days 1–3: Escalating enemy hordes.
  - Day 4: Boss intro (Cyclops Tuber Maw).
  - Day 5: Grand climax with simultaneous dual bosses (Cyclops + The Ranger).
- [x] **Dual-Tier Enemy Health Bar UI**:
  - Floating overhead bars with zero-GC pooling, amber ghost damage lag, and height offsets.
  - Top-center cinematic Boss HUD supporting single and dual-boss stacked layouts with defeat badge.
- [x] **NavMesh Safe Boundaries**:
  - Excluded Campfire, Natural Pond, and Farmland plots from walkable navigation mesh.
- [x] **3D Trophy Redesign**:
  - Modular cup and pedestal aesthetics with shelf snap positioning.

---

### Milestone 3: Persistence, Menus & Localization (COMPLETED ✅)
- [x] **Multi-Slot Persistence**:
  - `SaveLoadService`, `GameData`, and `MultiSlotSaveLoadManager` supporting 3 save slots and auto-save triggers.
- [x] **Main Menu & Pause Menu**:
  - Stylized fantasy menu with settings, slot selection, and audio controls.
- [x] **English Localization**:
  - 100% English translation across player stats UI, modal buttons, tooltips, and trophy logs.

---

### Milestone 4: Farming System (NEXT PRIORITY ⏳)
- [ ] **Soil Grid & Tilling**:
  - Interactive grid plots with tillable and waterable state feedback.
- [ ] **Crop Growth Cycles**:
  - **Sweet Potato**: Fast growth rate, gold revenue crop.
  - **Taro**: Medium growth rate, sturdy combat ingredient.
- [ ] **Harvest Loop**:
  - Direct delivery of `Crop_SweetPotato` and `Crop_Taro` to inventory for cooking or selling.

---

### Milestone 5: Workbench & Weapon Upgrades
- [ ] **Workbench Interactable**: UI for weapon progression and blacksmith enhancements.
- [ ] **Upgrade Cost Formula**: `Gold + Monster Materials → Weapon Enhancement`.
- [ ] **Branching Upgrade Paths**:
  - **Sweet Potato Path**: Agility, attack speed, and sprint dash damage.
  - **Taro Path**: Raw physical damage, armor, and knockback resistance.
