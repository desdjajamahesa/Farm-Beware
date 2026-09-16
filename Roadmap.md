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
                    ┌─────────────────────────┴─────────────────────────┐
                    ▼                                                   ▼
         ┌─────────────────────┐                             ┌─────────────────────┐
         │       FARMING       │                             │      COMMERCE       │
         │ (Sweet Potato/Taro) │                             │ (Sell Crops → Gold) │
         └──────────┬──────────┘                             └──────────┬──────────┘
                    │                                                   │
                    ▼                                                   ▼
         ┌─────────────────────┐                             ┌─────────────────────┐
         │       COOKING       │                             │      WORKBENCH      │
         │ (Prepare Food Buffs)│                             │  (Upgrade Weapon)   │
         └──────────┬──────────┘                             └──────────┬──────────┘
                    │                                                   │
                    └─────────────────────────┬─────────────────────────┘
                                              │ Sleep on Bed
                                              ▼
                                  ┌────────────────────────┐
                                  │      NIGHT PHASE       │
                                  │      (Night Brawl)     │
                                  └───────────┬────────────┘
                                              │
                                              ▼
                                  ┌────────────────────────┐
                                  │      COMBAT WAVES      │
                                  │ (Survive Monster Pack) │
                                  └───────────┬────────────┘
                                              │
                                              ▼
                                  ┌────────────────────────┐
                                  │     MONSTER DROPS      │
                                  │ (Crafting Materials)   │
                                  └────────────────────────┘
```

---

## 2. Project Milestones

### Milestone 1: House, Kitchen, & Core Systems (COMPLETED ✅)
- [x] **Centralized Camera Architecture**: `CameraManager` handling state machine transitions, positioning, and input lock.
- [x] **Instant Cooking System**: `GenshinStove` and `StoveUIManager` with Single Central Axis TextMeshPro layout.
- [x] **Dynamic Washing Mechanic**: `KitchenSinkInteractable` with item-level dirty/clean data transformation.
- [x] **Item Overhaul & Central Registry**: 33 active items in `ItemDatabase` (8 Food, 2 Crops, 2 Seeds, 4 Kitchen Materials, 4 Monster Drops, 1 Dummy Sword, 12 Trophies).
- [x] **Wardrobe System**: `PlayerOutfit` with in-world `MirrorCamera` live preview.
- [x] **UI Scaling & ESC Modal Priority**: 1920×1080 reference resolution and hierarchical modal close stack.
- [x] **Repository Cleanup**: Dead assets removed, pipeline diagram and design docs preserved in `Docs/MVP_Design/`.

---

### Milestone 2: Farming System (NEXT PRIORITY ⏳)
- [ ] **Soil Grid & Tilling**: Tillable, waterable crop tiles with visual state feedback.
- [ ] **Crop Growth Cycles**:
  - **Sweet Potato**: Fast growth rate, 750–1000 Gold base harvest value.
  - **Taro**: Medium growth rate, higher yield/combat utility.
- [ ] **Harvest Loop**: Direct harvest delivery of `Crop_SweetPotato` and `Crop_Taro` to the player inventory for selling or cooking.

---

### Milestone 3: Night Brawl Combat Engine
- [ ] **Primary Weapon System**:
  - 1 Core melee weapon (Sword / Combat Hoe) with light attack combo, heavy strike, and dash attack.
- [ ] **5-Day Wave Progression**:
  - **Day 1**: 1 Wave (Normal encounter).
  - **Day 2**: 2 Waves.
  - **Day 3**: 3 Waves.
  - **Day 4**: 4 Waves (First Boss Introduction + 4 normal enemies).
  - **Day 5**: 5 Waves (Climax: 2 Bosses simultaneously + 3 normal enemies).
- [ ] **Enemy Evolution (2 Seeds → 4 Monster Forms)**:
  - *Sweet Potato Evolution*: **Tuber Maw** (Normal) → **Cyclops Tuber Maw** (Boss).
  - *Taro Evolution*: **Taro Brute** (Normal) → **Taro Colossus** (Boss).
- [ ] **Monster Drops**:
  - Tuber Maw → `Mutated Root` (Normal Material).
  - Cyclops Tuber Maw → `Cyclops Eye` (Boss Material).
  - Taro Brute → `Hardened Root` (Normal Material).
  - Taro Colossus → `Colossus Core` (Boss Material).

---

### Milestone 4: Workbench & Weapon Upgrades
- [ ] **Workbench / Garage Interactable**: UI for weapon enhancement.
- [ ] **Upgrade Cost Formula**: `Gold + Monster Materials → Weapon Upgrade`.
- [ ] **Branching Upgrade Paths**:
  - **Sweet Potato Path**: Focuses on *Speed*, *Attack Speed*, and *Mobility/Dash*.
  - **Taro Path**: Focuses on *Power*, *Knockback*, *Armor*, and defensive perks.

---

### Milestone 5: Audio, Visual Polish, & Game Feel
- [ ] **Cooking Effects**: Smoke/fire VFX on the stove, cooking audio effects, and UI "Dish Created!" feedback.
- [ ] **Modular Kitchen Walls**: Split monolithic kitchen wall meshes into Layer 12 segments for fine-grained `WallOccluder` fading.
- [ ] **3D Trophy Models**: Replace placeholder colored cube prefabs with custom 3D trophy models.
