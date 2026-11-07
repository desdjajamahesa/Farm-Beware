# Patch Notes

Welcome to the **Farm-Beware** patch notes! Here you'll find the latest updates, balance tweaks, visual enhancements, and bug fixes as we continue refining the farmstead and nighttime combat experience.

---

## 2026-10-26

### Added
* **Cinematic Cutscene Visual & Typography Overhaul**: Completely overhauled cutscene typography and letterbox presentation for high-definition displays:
  * **Cinematic Subtitles**: Subtitle font size increased from 20 to **28** (with TextMeshPro auto-sizing 22 – 32, bold moonlight ivory `#FAF8F2`, and crisp kerning).
  * **Frosted Obsidian Backing Plate & Gold Accent Line**: Added a semi-transparent charcoal backing plate (`SubtitlePlate`) featuring a delicate 2px golden accent line (`#F2C752`) along the top edge, ensuring pristine readability across any 3D environment lighting.
  * **Full-Screen Intro Story Cards**: Sentence cards during the intro black screen now display at **36** font size (auto-sizing 26 – 42, bold, generous line spacing) with pure cinematic quotation marks `“...”` centered on the screen.
  * **Cinematic Letterbox & Amber Skip Prompt**: Letterbox bar height expanded to **125px** (from 110px), and skip gauge prompt upgraded to 15pt bold with warm amber hotkey highlights (`[ESC] / HOLD [SPACE] TO SKIP`).
  * **In-Game Dialogue Sizing**: In-game dialogue box upgraded to 21pt body text and 24pt bold gold speaker name across an expanded 920x200 panel.
* **Player V2 Stylized Character Model & Complete Animation Suite**: Replaced the legacy character model with the brand new, stylized farmer character (`Farmer_Asep_V2`). Equipped with a full suite of responsive, handcrafted generic animations:
  * **Locomotion**: Crisp 8-way directional walking, dynamic running, and sprint animations.
  * **Evasive Roll & Jump**: Smooth, athletic dodge roll (`roll_fit`) and responsive jump physics for traversing the farmstead.
  * **Farming Actions**: Dedicated planting and watering animations (`farm_plantseed`, `farm_watering`) synchronized with tool particles.
  * **Triple-Hit Melee Combo & Right-Click Attack**: Fluid 3-stage combat attack sequence (horizontal slash, downward chop, 360 spin attack) triggered with either left-click or right-click.
  * **HandSocket & Weapon Alignment**: Normalized hand sockets to prevent weapon distortion, ensuring all tools (Hoe, Plant Waterer, Sword, Axe) fit snugly into the farmer's hands.
* **Side Quest "First Steps" HUD Tracker**: Added a beginner quest tracker card prominently docked on the top-right HUD beneath the clock. Guides players step-by-step through their first day on the farm:
  1. Retrieve farming tools (`tool_hoe` and `tool_plant_waterer`) from the homestead.
  2. Stock up on fresh water bottles (`food_bottle_water`) from the kitchen or storage.
  3. Acquire Sweet Potato seeds (`seed_sweet_potato`).
  4. Till farmland soil, plant the seeds, and water the crops to initiate growth.
  * Real-time checklist updates automatically as items enter your inventory or field actions are performed.
* **Dedicated Cockpit Seating & Driving Pose**: Stepping into the delivery truck now transitions the character into a seated driving pose (`PlayerV2_DrivingPose`) inside the cab, with hands grasping the steering wheel and feet resting on the pedals.
* **Day 0 to Day 1 Prologue Sleep Transition**: Day 0 arrival now seamlessly transitions into Day 1 Morning upon sleeping in your bedroom bed. The delivery truck remains permanently parked by the front entrance (`X: 30.55, Y: 0.00, Z: 54.14`), facing the house.

### Changed
* **Protagonist Character Identity Renamed to 'Ucup'**: Updated all dialogue boxes, story beat speaker metadata, and research journal author signatures across the narrative engine from "Veteran" to "Ucup".
* **Combat Hit Registration Expansion**: Expanded melee target detection buffers from 16 to 64 simultaneous colliders, ensuring all strikes connect cleanly even in dense enemy swarms during Night Brawl.
* **Cleaned Test Banners & UI Clutter**: Safely removed obsolete editor test banners from the scene, providing an unobstructed view of the homestead and clean HUD presentation.

### Fixed
* **Fixed Cutscene Text Formatting & Missing Font Glyphs**: Resolved an issue where the prologue header rendered raw `&nbsp;` entities and missing character boxes (`□ &nbsp; P R O L O G U E &nbsp; □`). TextMeshPro does not parse HTML character entities—replaced with standard whitespace and native em-dash glyphs (`—   P R O L O G U E   —`) that are fully supported by the SDF font atlas. Also replaced unsupported star glyphs (`✦`) with native bullet points (`•`) and em-dashes (`—`) across all inventory, cooking, and day transition interfaces.
* **Fixed Day 1 Metal Pipe Discovery Story Trigger**: Corrected narrative beat alignment where striking the buried metal warning sign and cracking the underground pipe ("CLANG! My hoe just hit something metal...") was erroneously triggered when planting seeds (`FirstPlant`). Added a dedicated `StoryTriggerType.FirstTill` event invoked specifically upon successfully tilling farmland soil with the hoe.
* **Fixed Player Standing Inside Vehicle Cab**: Resolved an issue where the character stood upright inside the delivery truck cab while driving, with the head clipping through the roof and feet dangling through the chassis.
* **Fixed Native Engine Assertions on Game Stop**: Resolved "GameObjects can not be made active when they are being destroyed" console errors during game exit by safeguarding singletons (`OpeningCutsceneController`, `StorylineUI`, `JournalUI`) during teardown.
* **Fixed Edit Mode Material Leaks & Destroy Warnings**: Implemented safe lifecycle destruction (`DestroyImmediate` in edit mode, `Destroy` in play mode) and `MaterialPropertyBlock` in kitchen and item visualizers, eliminating compiler warnings during test suite runs.

---

## 2026-10-25

### Added
* **Cinematic Vehicle Arrival Cutscene Overhaul**: Rebuilt the game's opening sequence with a cinematic road trip leading to the homestead:
  * **Cinematic 50mm Lens Perspective**: Replaced wide distorted view with an authentic 50mm cinematic perspective (50° FOV, 0.3 near clip) from an unobstructed northern vantage point (`Z = 57.5 - 62.5`), preventing fence and pillar clipping.
  * **Dynamic Vehicle Dynamics**: Built a responsive vehicle physics model featuring realistic acceleration curves, steering dampening, and suspension friction.
  * **Atmospheric Letterbox & Subtitles**: Sleek cinematic black bars and smooth typewriter subtitle narration introducing the backstory as the truck arrives at the front gate.
  * **Instant & Hold-to-Skip Support**: Players can tap or hold the skip button to smoothly transition directly to live gameplay without camera jitter or animation snapping.

---

## 2026-10-22

### Added
* **Modular Storyline & Lore Progression System**: Implemented an episodic narrative engine spanning the 5-day gameplay cycle:
  * **NexaPhyto Research Lore**: Uncover the dark history of the abandoned NexaPhyto bio-chemical facility, rogue genetic crop experiments, and the source of the nighttime abominations.
  * **In-Game Story Journal (`J` Key)**: Open an interactive journal anytime to review discovered story facts, daily logs, and active narrative milestones.
  * **Dialogue Cards & Cutscene Sequences**: Introduced glassmorphic dialogue popups and cinematic boss introduction sequences (`BossIntroSequence`) that trigger when confronting climactic boss encounters.
* **Centralized Modal Input Management**: Unified all fullscreen and overlay UI panels (Journal, Storyline, Menus) under a centralized input stack, preventing accidental character movement or tool swings while reading lore or adjusting settings.
* **New Crop Data & Farm Grid Lookups**: Added canonical data for Corn (`CropData_Corn.asset`) and expanded farm grid query aliases to support versatile crop rotations.

---

## 2026-10-20

### Fixed
* **Monster & Boss Health Bar Visibility During Night Brawl**: Fixed an issue where overhead health bars above common monsters and the boss health bar panel at the top of the screen disappeared during Night Brawl. All combat health bars are now strictly anchored to the main UI canvas with automated reparenting protection, and monster health bars are immediately visible above active enemies upon spawn.
* **Buff Tooltip UI Canvas Resolution**: Resolved an issue where food and combat buff inspection tooltips could become misparented, guaranteeing tooltips render accurately across all display modes.
* **Night Brawl Wave Spawning & Intermission Fix**: Fixed a bug where monsters continued spawning during the 30-second wave intermission even after all active enemies in the previous wave were defeated. Wave spawns are now immediately and cleanly terminated the moment a wave is cleared.
* **Boss Minion Wave Progress Decoupling**: Fixed an issue where eliminating minions summoned by bosses prematurely filled the wave progress bar and triggered early wave completion. Minions are now treated as dynamic combat adds that do not inflate or corrupt wave milestone quotas. When a boss is defeated, any lingering summoned minions are automatically dispersed.
* **Climax Boss Wave Order**: In boss waves (Day 3 Wave 3, Day 4 Wave 4, and Day 5 Wave 5), common monsters will now always spawn first to test your defenses, with the boss appearing strictly as the final, dramatic climax of the wave.
* **Monster Attack Consistency & Fair Combat Timing**: Fixed three long-standing monster combat AI issues:
  * **Phantom Long-Distance Melee Attacks**: Fixed an issue where *The Ranger* and heavy bosses swung from far across the arena. *The Ranger* now properly uses its dedicated long-range artillery mortar attacks with laser telegraphing, while melee attack ranges and hitbox buffers have been harmonized across all enemy types (*Cyclops Tuber Maw* 2.4m, *Taro Colossus* 2.4m, *Taro Brute* 1.8m, *Tuber Maw* 1.4m). Special skills now strictly validate engagement distance before firing.
  * **Hesitation & Orbiting at Close Range**: Fixed an issue where monsters standing directly next to you circled in place instead of attacking. Distant monsters are no longer allowed to hoard attack permits from 7 meters away, and close-quarters combatants now preempt distant approaching monsters to strike without delay.
  * **Continuous Attacks Without Cooldown**: Eliminated jarring back-to-back attack loops where monsters chained special skills and basic attacks instantaneously with zero recovery. Attack cooldown timers are now stamped strictly when animations finish, and a guaranteed recovery buffer is enforced between skills and standard strikes.
* **Seed Planting Stack Consumption Priority**: Fixed an issue where planting seeds while holding a seed stack in the hotbar incorrectly consumed seeds from duplicate stacks in your main inventory backpack rather than the active hotbar stack in your hands. Active hotbar stacks are now strictly prioritized when planting, seamlessly falling back to backpack stacks only once the hotbar stack is fully depleted.
* **Plant Waterer Visual Mount & 3D Tool Model**: Resolved an issue where equipping the watering can (`tool_plant_waterer`) showed empty player hands due to an unlinked visual prefab. Created and configured the official 3D `PlantWaterer.prefab` with bespoke cylinder body, spout, handle, URP `Mat_Metal_Dark` material, trigger hitbox, and weapon trail controller, fully linked to `Tool_PlantWaterer.asset`.
* **Melee Combat AI Reachability & Strike Precision**: Eliminated the circular deadlock where melee monsters endlessly circled the player without attacking:
  * **Deadlock Elimination**: Fixed an architectural mismatch where melee orbit boundaries were larger than token acquisition thresholds. Combat AI now dynamically sizes orbit rings according to weapon range, and monsters with attack cooldowns ready immediately break out of orbit to close the gap and strike.
  * **Precision Strike Alignment & Zero Whiffing**: Fixed an issue where monsters missed attacks against stationary players due to sluggish rotation during windups. Monsters now snap rotation directly toward the target at the strike climax, expanding telegraph ground coverage and close-proximity hit registration.
  * **Subterranean Burrow Proximity**: Adjusted subterranean burrow eruptions (Tuber Maw) to approach within 1.2m before detonating with an expanded 2.4m eruption radius, preventing misses from meters away.
* **Production-Ready Standalone Windows Build & Material Hardening**: Successfully prepared and compiled the official 64-bit Standalone Windows release build (`Builds/Windows/Farm-Beware.exe`). Implemented dual-layer material and shader hardening: configured `GraphicsSettings.defaultRenderPipeline` to `PC_RPAsset` (preventing fallback to Built-in pipeline pink shaders), whitelisted all runtime shaders in `Always Included Shaders` to prevent shader stripping, bundled farming soil materials into `Assets/Resources/Materials/Farming/`, synchronized enemy prefab renderer material slots, and validated clean execution through an automated startup smoke test.

---

## 2026-10-17

### Added
* **Triple Clock Speed Display Mode**: You can now cycle through three distinct digital clock speeds on your HUD by pressing **T** or **9**! Choose between the classic 60-second full step (+40 in-game minutes), a brisk 30-second half step (+20 in-game minutes), or a new 15-second quarter step (+10 in-game minutes, highlighted with an Emerald badge) to follow your farm's schedule with precision.
* **Charming Main Menu Animations**: Added adorable animated rice stalks and leaf decorations to the main menu buttons! They gently sway in the breeze, breathe with life, and react with a playful bounce whenever you hover over them.
* **Burrow Attack Warning Indicator**: Subterranean enemies (like the pesky TuberMaw) now project a clear red danger circle on the ground and an **"⚠️ Erupting!"** alert right before they burst from the earth, giving you time to dodge or parry!
* **Cinematic Day & Night Banners**: Shifting between peaceful farming days and tense night battles now feels much more atmospheric! Enjoy a radiant golden sunrise banner accompanied by a gentle morning chime (`☀️ DAY X — The morning sun rises. Tend to your crops.`), or brace yourself as the sky darkens into a crimson dusk with a resonant gong (`🌙 NIGHTFALL — The shadows stir. Prepare your defenses.`).
* **Clean Screen Transitions & Input Protection**: Interactions during scene transitions (such as sleeping in your bed, day/night announcements, respawning, and cloud wipes) are now silky-smooth and protected. You will no longer accidentally swing your weapon, trigger hotbar slots, or move your character while transition screens are active.
* **Fresh Morning Reset for Food Buffs**: Temporary food buffs consumed for nighttime combat now naturally wear off when you go to sleep. You will wake up at 6:00 AM feeling fresh with baseline stats and clean HUD icons, ready to cook and feast for the new day.

### Changed
* **Monster Combat Balance**: Re-tuned enemy attributes to make every fight feel distinct, fair, and tactical:
  * **The Ranger**: Now functions as a heavy, stationary siege turret that fires punishing long-range mortar shots.
  * **Taro Colossus**: A colossal boss with thick armor and devastating swings that tests your positioning and patience.
  * **Corn Musketeer**: A quick and nimble ranged shooter that keeps you on your toes from a distance.
  * **TuberMaw**: A sneaky burrower that ambushes from underground before darting away.
  * **PumpkinBrute**: A sturdy front-line brawler designed for close-quarters slugfests.
* **Direct "New Game" Experience**: Clicking **New Game** from the main menu now whisks you directly to your farm through a scenic cloud transition, getting you straight into the action without extra save slot prompts.
* **Cooking System Polish**: Reorganized cooking recipes and food preparation behind the scenes to keep the kitchen running smoothly.

### Fixed
* **Fixed Sinking into the House Floor**: Fixed an issue where stepping through the front door could occasionally cause your character to get stuck waist-deep in the floorboards.
* **Fixed Purple Attack Indicators**: Fixed a visual glitch where monster attack telegraphs would appear as untextured magenta squares instead of glowing danger zones.
* **Clean Slate for New Games**: Removed lingering test save data to ensure starting a new adventure always begins with a completely fresh farm.

---

## 2026-10-16

### Added
* **Tactical Monster Encirclement**: Enemies waiting for an opening will now intelligently surround you in a 360-degree combat circle rather than clustering together, moving clockwise and counter-clockwise to look for a flank.
* **Smart Ranged Kiting**: Ranged adversaries (Corn Musketeer and The Ranger) will now step back and reposition laterally when you get too close, keeping distance instead of letting you pin them against walls.
* **Pack Morale Surge**: Defeating a monster in battle now sends a brief adrenaline rush to nearby pack members, giving them a short movement burst marked by a floating **"⚡ Surge!"** visual indicator.
* **Natural Monster Patrols**: Undetected monsters now wander relaxed paths around their spawn areas rather than standing frozen in place.

### Changed
* **Smart Gate & Fence Navigation**: Enemies outside the farmstead now recognize fences and garden walls as solid boundaries. Instead of mindlessly running into wooden posts, they will actively navigate along fences and funnel through the front gate!
* **Rhythmic Horde Attack Waves**: Up to 3 melee enemies and 2 ranged enemies will engage you simultaneously, while the rest circle and wait for an opening to charge in as soon as an attack finishes.

### Fixed
* **Fixed Melee Enemies Hesitating in Circles**: Fixed an issue where melee enemies would endlessly circle just out of reach without attacking unless you walked up to them.
* **No More Attacking Through Solid Fences**: Monsters will no longer attempt to strike at you through solid perimeter fences or obstacles when they don't have a clear path to reach you.
* **Natural Monster Looking Directions**: Enemies now look forward in the direction they are walking instead of facing sideways into walls when turning corners.

---

## 2026-10-15

### Added
* **Interactive Water Well Station**: Upgraded the homestead garden well into an interactive refill station! Place your Plant Waterer into the well cradle, watch the crank and bucket lower with satisfying stone and splash sounds, and pick up your full 100L can when ready.
* **Spacious 16-Slot Inventory**: Expanded your carrying capacity to 16 full inventory slots (4 quick-access hotbar slots + 12 backpack slots), giving you plenty of room for tools, seeds, harvested produce, and cooked meals.
* **Click-to-Equip Hotbar**: You can now click directly on hotbar icons with the mouse to equip weapons, tools, or seeds in addition to using number keys 1–4.
* **Defeat & Morning Respawn**: If you collapse in battle, a gentle defeat screen will appear. Choosing to rest will wake you up in your bed at 6:00 AM with your health restored and ready to check your morning farm report.

### Changed
* **Fast-Forward Food Buffs**: Using fast-forward mode now accelerates active food buff timers and health regeneration at the same speed as the daytime clock, keeping timers feeling consistent.
* **Continuous Day into Dusk**: The farm clock now ticks smoothly from morning past afternoon dusk all the way to nightfall, and you can safely take an early nap in bed during the late afternoon.
* **Crisp HUD & Digital Clock**: The top-right HUD now features a clear "DAY" badge, a crisp digital clock, and a neat status card that tells you what time of day it is at a glance.
* **Kitchen Progress Timers**: Replaced cooking bars with individual burner timers on the stove and gave the kitchen sink a sleek vertical cleaning meter.

### Fixed
* **Full Inventory Seed Drop Protection**: Harvesting crops when your backpack is completely full now safely drops surplus seeds onto the ground as floating pickups so you never lose your hard work.
* **Clean Daytime Swings**: Weapon slash trails now only appear during nighttime combat, keeping daytime farming swings clean and natural.
* **Shop Scroll Reset**: Opening the merchant shop or switching categories now automatically resets the scroll bar to the top of the item list.
* **Safer Save Files**: Hardened game saves to ensure your farm progress is saved cleanly and securely across all save slots.

---

## 2026-10-14

### Added
* **Staggered Night Waves**: Night brawl enemies now arrive in steady waves every 15 to 20 seconds instead of swarming the yard all at once, giving you breathing room to reposition and plan your strikes.
* **Visceral Combat Hits & Sparks**: Every successful hit now lands with punchy impact sounds, brilliant golden spark bursts, and satisfying micro-hitstops that give melee weapons real weight.
* **Centred Water Refill Gauge**: Refilling your water bottles at the kitchen sink or watering can at the well now displays a clean circular progress wheel in the center of the screen.
* **Cozy Fantasy Pause Menu**: Introduced a warm wooden pause menu with smooth button animations matching the aesthetic of the main menu.

### Changed
* **Wave Preparation Phase**: Added a 30-second breather between night waves with an on-screen timer. Ready to fight early? Simply press `Enter` or `N` to start the next wave immediately!
* **Synchronized Day Schedule**: The daytime clock now runs at a steady pace across a full 18-minute cycle, ensuring the sunset bell and nightfall arrive right on time.
* **Balanced Thirst and Hunger**: Tuned your character's hunger and hydration drain rates so you can farm comfortably throughout the day without constant stopping.
* **Clear Water Sources**: The outdoor garden well now exclusively fills your 100L farming watering can, while drinking water bottles are cleanly refilled at the indoor kitchen sink.

### Fixed
* Fixed the stove cooking timer occasionally freezing or displaying as full before meals were ready.
* Fixed the kitchen sink dishwashing progress meter getting stuck at 100%.
* Fixed defeated monsters lingering awkwardly on the battlefield before disappearing.
* Fixed corn seeds occasionally summoning the wrong enemy types.
* Fixed daytime morning fade animations stalling while the game was paused.
* Fixed activity guide text displaying mismatched hour ranges on the HUD card.

---

## 2026-10-10

### Added
* **Hades-Inspired Silhouette Lighting**: Characters and monsters now feature glowing rim highlights that pop clearly against dark night fields, making combat easy to read even in deep shadows.
* **Dynamic Weapon Slash Trails**: Light attacks now slice with luminous golden arcs, while the leaping heavy slam leaves a sweeping crimson flame trail from jump to impact!
* **Glowing Attack Danger Zones**: Ground danger rings now project clearly under enemies with expanding charge-up animations and bright impact flashes so you always know when to dodge.
* **Atmospheric Lighting**: Fine-tuned environmental lighting across crisp morning, warm afternoon sunlight, golden sunset, and cool moonlight with soft, glare-free bloom.

### Changed
* **Clean Night Battle Resets**: Enemy projectiles now dissolve cleanly when a nighttime battle concludes or resets.
* **Balanced Night Vision**: Softened night shadows so you can easily see crops, paths, and fences without losing the eerie nighttime mood.

### Fixed
* Fixed light attack 3-hit combos occasionally skipping swings and restored the leaping heavy attack's fiery trail.
* Fixed jumping controls getting stuck after respawning and eliminated rare game hiccups when closing menus.

---

## 2026-10-09

### Added
* **Dedicated Farming Watering Can**: Introduced a dedicated 100L watering can tool with a custom rustic metal model and icon, built specifically for tending crops.
* **Separated Drinking Water Bottle**: Thirst is now managed with dedicated water bottles (providing 4 refreshing sips each) that can be refilled at your indoor kitchen sink.
* **Interactive Farmstead Well**: Approaching the outdoor well now highlights it with friendly floating prompt text to quickly refill your watering can.
* **You Collapsed Defeat Screen**: Reaching 0 HP presents a clear recovery menu allowing you to awaken in bed at 6:00 AM, load an existing save, or return to the main menu.
* **Dual Water HUD Tracker**: Your status bar now clearly separates drinking sips from farming irrigation litres.
* **Scenic Cloud Screen Transitions**: Added soft, rolling cloud transitions when starting the game from the main menu.

### Changed
* **Farm Tool Requirement**: Farmland plots now require holding your watering can to water your crops.
* **Smooth Inventory Toggle**: Pressing `Tab` now smoothly toggles your inventory open and closed with instant responsiveness.
* **Smooth Stair & Slope Running**: Fixed jerky movement and sprinting stutters when running down slopes or stairs.
* **Protected Defeat Screen**: Prevented accidental dismissal of the collapse screen with the Escape key so you don't lose your choice.

### Fixed
* Cleaned up remaining night monsters upon player collapse so you never wake up to lingering monsters on your morning farm.
* Fixed an issue where monster health bars could stay stuck on screen after loading a saved game.
* Fixed night waves failing to spawn if you saved and loaded during nighttime.
* Fixed character positioning when waking up in bed so you never clip through the floor.

---

## 2026-10-04

### Added
* **Save & Load System**: Use the bedroom desk to save and resume your farm adventure across multiple save slots.
* **Monster & Boss Health Bars**: Floating health bars now display above enemies, with a dedicated cinematic health bar for boss encounters at the top of the screen.
* **Combo Attacks & Heavy Slams**: Added a 3-hit combo slash, Dash Attack, and Charged Heavy Attack to your combat arsenal.
* **Character Stats & Gear Screen**: Press `Tab` to view your character attributes, backpack inventory, and equipment slots.
* **16-Plot Farmland**: A complete farming loop—till soil, water crops, plant seeds, and harvest produce with responsive soil visuals.
* **Expanded Farmstead Compound**: The outdoor yard is now spacious and divided into 4 clear zones with a cozy campfire, pond pier, and workshop area.
* **Smart Tree Transparency**: Trees automatically fade to transparent whenever they block the camera's view of your character.

### Changed
* **Item Stacking**: Harvested produce and seeds now neatly stack up to 20 items per inventory slot.
* **Front Gate Invasions**: Night brawl monsters now funnel directly through the front yard gate instead of spawning randomly around the perimeter.
* **Lighting & Performance**: Rebalanced farm lighting and optimized assets for smoother frame rates.

### Fixed
* Fixed character drift when moving left with the `A` key.
* Fixed newly spawned wave enemies appearing at their previous defeat location instead of outside the gate.
* Fixed Corn Musketeer bullets flying over your head instead of aiming properly.
* Fixed inverted well roof geometry, missing textures on stone paths, and minor object clipping inside the farmhouse.