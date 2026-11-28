# Patch Notes (Alpha 0.3.0)

Welcome to the **Farm-Beware** patch notes! Here you'll find the latest updates, balance tweaks, visual enhancements, and bug fixes as we continue refining the farmstead and nighttime combat experience.

---

## 2026-11-28

### Added
* **Complete Game Stats & Armor Defense Reference**: Published [GAME_STATS_REFERENCE.md](GAME_STATS_REFERENCE.md) documenting all combat, weapon, armor mitigation, monster, wave, farming, economy, and cooking parameters.
* **Informative Armor Telemetry**: Equipment loadout, Player Stats HUD, and Workbench panels now display live Gear Defense DR (`D / (D + 40)`), Workbench Plating DR, Combined Mitigation %, Effective HP (EHP), Attack Speed, Move Speed, and Critical Chance.
* **Wooden Board Kitchen Sink**: Applied the `WoodenBoard_TwoColumn` aesthetic to the Kitchen Sink modal (`Panel_Sink`) for visual parity across kitchen and shop interfaces.

### Changed
* **60 FPS Combat Polish & Fair Monster Telegraphing**: Unified player move frame data (`CombatFrameDataSheet`), prioritized 2D cancel/buffer matrix, archetype-specific enemy tracking lock-off windows for sidestepping, stagger token eviction, and deferred knockback sequencing.
* **Full Armor Gear Stat Wiring**: Connected all 5 Sweet Potato armor piece stats (`HP`, `DEF`, `MoveSpd`, `AtkSpd`, `Crit`) directly into gameplay alongside diminishing-returns Gear Defense + Workbench Plating (`Lv 1–10`) mitigation.
* **MVP Cooking Duration Calibration**: Aligned all 9 stove recipes (`Taro Chip`, `Roasted Sweet Potato Slice`, `Corn Porridge`, `Taro Dumpling`, `Purple Taro Cake`, `Sweet Potato Porridge Bowl`, `Taro Coconut Stew Bowl`, `Taro Corn Stew`, and `Whole Roasted Sweet Potato Feast`) to the MVP Food Buff schedule (`1m 30s` – `7m`).

### Fixed
* Fixed `PlayerModular` weapon attack trail alignment/scale and bare-hands trail lifecycle (`HandSocket` & `BareHandsTrailAnchor`).
* Fixed monsters occasionally spawning atop roadside pine tree canopies by relocating `SpawnPoint_FrontGate_Right` and enforcing ground-safe NavMesh clearance during wave and summon spawns.
* Fixed Merchant Shop card clipping and compartment alignment inside `WoodenBoard_TwoColumn`.

---

## 2026-11-27

### Added
* **Handcrafted Wooden Board Interface**: Menus, crafting panels, cooking screens, dialogue boxes, and the main menu now share a warm, carved-wood aesthetic with cohesive parchment cards.
* **Responsive Heavy Attack Chains**: Improved heavy attack combos and responsive action cancel timing when swinging melee weapons during nighttime defense.

### Changed
* **Polished Trading Post Modal**: Updated the merchant shop card layout for cleaner browsing, clearer item tooltips, and smoother scrolling across categories.

### Fixed
* Fixed seed pricing exploit at the merchant counter and prevented essential farming tools like the hoe and watering can from being sold.
* Fixed enemy attacks and player strikes clipping through house walls or perimeter doorways during indoor defense.
* Fixed duplicate items, water bottles, and stuck cooking or washing timers across save and reload transitions.
* Fixed save slot food buffs carrying over unintended durations between slots and ensured night enemy waves restore accurately after reloading.
* Fixed monster loot drops (gold, seeds, and mutated roots), adjusted night wave intermission timers to 30 seconds, and allowed dirty corn to be washed cleanly at the sink.
* Fixed HUD day counter synchronization on morning wake-up, Day 1 dialogue pacing, and outdoor water well refill limits.

---

## 2026-11-15

### Added
* **Dedicated Quest Log**: Press `Q` anytime to inspect active objectives and track progress through early homestead milestones.
* **Outdoor Farm Order Board**: Deliver washed, clean crops directly to the outdoor order board for a 1.2x gold payout bonus.

### Changed
* **Calibrated Crop Market Pricing**: Adjusted base crop market rates across Sweet Potato, Taro, and Corn to match harvest yields, while keeping a 50% discount for unwashed produce.

---

## 2026-11-08

### Added
* **Interactive World Map**: Press `M` to view a stylized parchment map of the valley displaying your position, homestead compound, western trading post, and danger breach zones.
* **Western Scenic Roadway & Frontier Trading Post**: Follow the 225-meter western roadway out of the farmstead to explore the commercial trading post, featuring a boardwalk, turnaround bay, and merchant counter.
* **Night 1 Cinematic Awakening**: Experience a cinematic sequence as darkness falls on Day 1, capturing ground tremors, sweet potato mutations erupting from the soil, and frontline defense preparation.
* **Weapon & Armor Equipment Progression**: Suit up with craftable armor sets, upgraded weapon tiers, and a reworked slingshot for ranged crowd control.
* **Farmland Expansion Deeds**: Purchase official land expansion deeds from the trading post to unlock additional garden plots for larger daily yields.
* **Wardrobe Vest Outfits & Atmospheric Transitions**: Unlock Explorer, Ranger, and FieldMaster vests in your wardrobe alongside vivid day-to-night visual banners.

### Changed
* **Paced Nocturnal Wave Intervals**: Tightened monster arrival intervals during night combat to 6–10 seconds for a steadier siege rhythm.
* **Streamlined Starter Inventory**: Cleared late-game weapons and armor from Day 0 starter chests so combat gear is earned through trade and monster loot.
* **Persistent Night Hotbar & Kitchen Curfew**: Kept the hotbar accessible at night for quick rations while guiding you inside to safety when the 18:00 curfew arrives.
* **Smart Seed Stacking & Sleep Growth Credit**: Harvested crops and seeds top off hotbar stacks first, and sleeping early properly advances crop growth time.

### Fixed
* Fixed an issue where the opening truck arrival cutscene could replay unexpectedly after sleeping on Day 0.
* Fixed empty watering cans playing full pouring animations and interacting with dry soil plots when completely drained.
* Fixed weapon upgrades unlocking before acquiring the base weapon or required monster materials.
* Fixed kitchen refrigerators rejecting cooking oil, flour, and seasonings, and prevented duplicate items from spawning in fridge slots.
* Fixed vehicle driving gauges remaining visible during cutscenes and letterbox bars clipping through cabin walls.
* Fixed melee weapon attacks penetrating solid house walls and campfire obstacles trapping patrolling monsters.

---

## 2026-10-26

### Added
* **Cinematic Cutscene Visual & Typography Overhaul**: Upgraded subtitles, prologue story cards, frosted charcoal backing plates, and golden skip prompts for clear readability day or night.
* **Stylized Farmer Character & Animation Suite**: Added 8-way locomotion, dodge rolls, jumping, dedicated planting and watering motions, and a 3-hit melee combo with clean tool alignment.
* **Side Quest "First Steps" Tracker**: Added a HUD checklist guiding new farmers through collecting tools, water, seeds, tilling, planting, and watering.
* **Cockpit Driving Pose & Day 0 Transition**: Stepping into the delivery truck now seats your farmer at the wheel, and sleeping on arrival night smoothly opens Day 1 morning.

### Changed
* **Protagonist Identity Updated to Ucup**: Updated speaker names, dialogue cards, and journal entries to reflect Ucup's story.
* **Expanded Melee Crowd Detection**: Widened close-range strike detection so swings reliably connect against dense nighttime swarms.

### Fixed
* Fixed broken formatting symbols and missing glyphs appearing in prologue headers, cooking menus, and transition cards.
* Fixed Day 1's buried pipe discovery dialogue triggering while planting instead of when striking the metal sign with your hoe.
* Fixed your character standing upright inside the truck cab instead of sitting behind the steering wheel.
* Fixed scene transition and UI teardown warnings during quit or test runs.

---

## 2026-10-25

### Added
* **Cinematic Vehicle Arrival Cutscene**: Rebuilt the opening sequence with a focused cinematic camera angle, smoother truck handling along the road, letterbox narration, and clean hold-to-skip support.

---

## 2026-10-22

### Added
* **Modular Storyline & Lore Progression**: Added a 5-day NexaPhyto bio-hazard narrative campaign featuring journal logs (`J`), glassmorphic dialogue cards, and cinematic boss introductions.
* **Unified Modal Input Stack**: Reading lore journals or adjusting pause menus safely locks movement and weapon swings until modals are dismissed.
* **Corn Crop Expansion**: Added Corn crops and seeds to expand farming rotations alongside Sweet Potato and Taro.

---

## 2026-10-20

### Fixed
* Fixed overhead monster health bars and cinematic boss health bars disappearing during Night Brawl.
* Fixed buff inspection tooltips aligning to the wrong screen corner on different resolutions.
* Fixed enemies continuing to spawn during the 30-second wave intermission after a wave was cleared.
* Fixed boss minions incorrectly filling wave progress bars, and ensured bosses arrive at the climax of their wave.
* Fixed enemies orbiting endlessly without attacking, firing phantom long-range melee hits, or chaining attacks with zero cooldown.
* Fixed seed planting pulling from backpack stacks instead of the active hotbar slot, and added the missing 3D watering can model in hand.
* Fixed missing pink materials in standalone Windows builds and improved ground soil material loading.

---

## 2026-10-17

### Added
* **Triple Clock Speed Display Mode**: Press `T` or `9` to cycle between 60-second, 30-second, and 15-second HUD clock steps.
* **Animated Main Menu & Burrow Danger Rings**: Added swaying botanical accents on main menu buttons and ground warning indicators before subterranean monsters erupt.
* **Golden Sunrise & Crimson Dusk Banners**: Added morning chimes, evening gongs, clean input-protected transitions, and fresh morning resets for temporary food buffs.

### Changed
* **Monster Combat Archetype Balance**: Tuned The Ranger, Taro Colossus, Corn Musketeer, Tuber Maw, and Brutes for clearer strengths and telegraph windows.
* **Direct New Game Flow**: Starting a New Game now enters the farmstead directly through a rolling cloud transition.

### Fixed
* Fixed stepping through the front door occasionally sinking your character into the floorboards.
* Fixed magenta square attack telegraphs and lingering test save files when starting a fresh farm.

---

## 2026-10-16

### Added
* **Tactical Encirclement, Ranged Kiting & Pack Surge**: Melee monsters flank around you while ranged enemies kite backward, and slaying a monster briefly spurs nearby allies forward.

### Changed
* **Front Gate Funneling & Wave Pacing**: Enemies outside perimeter fences path naturally toward the front gate, with up to 3 melee and 2 ranged attackers engaging at once.

### Fixed
* Fixed melee enemies hesitating forever in circles or lunging blindly through solid perimeter fences.

---

## 2026-10-15

### Added
* **Interactive Water Well, 16-Slot Backpack & Collapse Screen**: Refill your 100L watering can at the well with crank animations, manage 16 inventory slots with hotbar clicking, and awaken safely at 6:00 AM if defeated.

### Changed
* **Synchronized Fast-Forward & HUD Timers**: Food buffs accelerate cleanly with fast-forward time, dusk ticks smoothly into nightfall, and stove burners show individual timers.

### Fixed
* Fixed surplus seeds vanishing on harvest when the inventory is full, disabled weapon slash trails during daytime chores, and hardened save slot reliability.

---

## 2026-10-14

### Added
* **Staggered Waves, Combat Sparks & Pause Menu**: Added golden impact hit-sparks, centered circular sink/well gauges, and a cozy wooden pause menu.

### Changed
* **Wave Breathers & Water Source Roles**: Added 30-second intermissions (skippable via `Enter` or `N`), synchronized the 18-minute day clock, and separated well irrigation from sink drinking refills.

### Fixed
* Fixed frozen stove timers, stuck sink progress bars, defeated monsters lingering after battle, and mismatched HUD activity schedule hours.

---

## 2026-10-10

### Added
* **Silhouette Rim Lighting & Fiery Heavy Trails**: Added crisp night readability highlights, golden light-attack arcs, crimson heavy-slam trails, and soft atmospheric moonlight.

### Changed
* **Clean Battle Resets & Balanced Night Shadows**: Dissolved lingering enemy projectiles when night battles end and brightened terrain readability after dusk.

### Fixed
* Fixed light combo attacks skipping hits, restored fiery heavy-attack trails, and prevented jump controls from locking after respawning.

---

## 2026-10-09

### Added
* **Dedicated Watering Can, Drinking Bottles & Dual Tracker**: Separated 100L field irrigation from 4-sip hydration bottles with clear dual HUD gauges and rolling cloud transitions.

### Changed
* **Smooth Stair Traversal & Protected Collapse Menu**: Smoothed sprinting down slopes and prevented accidental Escape dismissal of the defeat recovery screen.

### Fixed
* Fixed surviving monsters lingering after player collapse, stuck enemy health bars after loading, missing night waves after reload, and waking positions in bed.

---

## 2026-10-04

### Added
* **Multi-Slot Save Desk, Health Bars, 3-Hit Combos & 16-Plot Yard**: Introduced bedroom desk saves, enemy/boss health bars, melee combos, the `Tab` character sheet, 16 farmland plots, and camera-fading trees.

### Changed
* **Stacking & Front Gate Sieges**: Stacked seeds and crops up to 20 per slot and funneled night enemies through the north yard gate.

### Fixed
* Fixed leftward movement drift when pressing `A`, enemies respawning inside the yard, ranged projectiles flying over your head, inverted well geometry, and interior clipping.
