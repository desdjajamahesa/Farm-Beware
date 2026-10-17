# Patch Notes

Welcome to the **Farm-Beware** patch notes! Here you'll find the latest updates, balance tweaks, visual enhancements, and bug fixes as we continue refining the farmstead and nighttime combat experience.

---

## 2026-10-17

### Added
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