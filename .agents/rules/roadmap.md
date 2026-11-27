# Roadmap.md

Roadmap and milestone tracking for **Farm-Beware** (`Alpha 0.3.0`).

---

## 1. Core MVP Game Loop

- **Day Phase**: Exploration, tilling/watering/harvesting, sink washing, cooking buffs, pickup driving, Outdoor Order Board & Trading Post sales, Workbench upgrades.
- **Sleep Transition**: Bed interaction (`BedInteractable`) advances to night brawl or morning harvest credit.
- **Night Phase**: 5-day escalating wave defense (`CyclopsTuberMaw`, `TaroColossus`, `TheRanger`) for seeds and monster materials.

---

## 2. Project Milestones

### Milestone 1: House, Kitchen & Foundation (COMPLETED)
- `CameraManager` modes, `KitchenStove` escrow FSM, `KitchenSinkInteractable` washing, `ItemDatabase`, `Wardrobe` + `MirrorCamera`, 1920x1080 `ModalStackManager`.

### Milestone 1.5: Graphics, Shaders & Pipeline (COMPLETED)
- URP Deferred+ & GPU Resident Drawer (BRG), `MonsterFresnelLit.shader`, `DitheredBuildingLit.shader`, ACES + post-processing stack.

### Milestone 2: Combat Engine & Hardening (COMPLETED)
- `CombatStateEvaluator` 3-hit & heavy chain FSM, `PoiseTracker`, priority `AttackTokenDispatcher`, `DefenseEvaluator` (300ms dodge i-frames, 350ms parry lockout), pooled overhead & boss health bars, `PlayerModular` rig & action cancels.

### Milestone 3: Persistence, Menus & NUnit CI (COMPLETED)
- 3-slot `SaveSystemManager` JSON persistence, stylized fantasy Main/Pause menus, `FarmBeware.Editor.Tests` NUnit EditMode runner (`117/117` passing).

### Milestone 4: Farming & Agriculture (COMPLETED)
- 16–32 farmland plots, calibrated Sweet Potato / Taro / Corn cycles, `PlayerWaterBottle` (100L, well refill cap), crop growth countdown UI.

### Milestone 5: Economy, Trading Post & World Map (COMPLETED)
- 225m Western roadway, `TradingPost_Complex`, `DailyEconomyManager` calibrated pricing (`375–500G` SP, `700–900G` Taro/Corn), Outdoor `OrderBoard` (`1.2x` clean bonus), `WorldMapUI` (`M`) & `MapProjectionMath`.

### Milestone 6: Weapon Forge, Quests & Storyline (COMPLETED)
- Lv 1–10 Weapon/Armor upgrades + Sweet Potato set bonuses, Contextual Combat Stance, 5-day NexaPhyto storyline, Opening & Night 1 cutscenes, Journal (`J`) & Quest Log (`Q`), `WoodenBoardStyle` UI theme.

### Milestone 7: Architecture Hardening & QA Remediation (COMPLETED)
- Full 22-ASMDEF DAG, typed `Core/Contracts/` replacing all runtime `SendMessage`, zero empty `catch {}`, partial `*.Layout.cs` UI split, QA HUB `BUG-009..024` fixes (`d54add2`).

---

## 3. Upcoming Milestones

### Milestone 8: QA V0.3.0 Retest & Final Boss Campaign Polish (IN PROGRESS)
- [ ] Re-verify `BUG-009`..`BUG-024` and remaining `BUG 025`..`BUG 027` in `Alpha 0.3.0` PlayMode QA pass.
- [ ] Final multi-phase boss telegraph polish and Day 5 victory flow end-to-end soak test.
