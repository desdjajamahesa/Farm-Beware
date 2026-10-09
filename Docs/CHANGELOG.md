# Patch Notes (V0.1.1) - 2026-10-09

### Added
* **Dedicated Farm Plant Waterer**: Introduced a dedicated watering can tool (`tool_plant_waterer`) holding 100L of irrigation water, featuring a custom rustic metal model and icon.
* **Separated Drinking Water Bottle**: Drinking water is now handled independently via the `Bottle of Water` item (4 sips, +25 hydration per sip), exclusively refillable at the Kitchen Sink.
* **Interactive Garden Water Well**: The farm well now features glowing highlight detection when approaching and refills the Plant Waterer to 100L upon interaction with contextual floating text alerts.
* **Player Defeat & Death Screen Menu**: Reaching 0 HP presents a dedicated You Collapsed screen offering **Checkpoint** (awaken in bed at 06:00 morning with restored vitals), **Load Game** (open multi-slot saves), or **Main Menu**.
* **Daily Report Modal Integration**: Morning economy summary now participates in unified modal window stack navigation.
* **Dual Water HUD Readout**: Character sheet now displays independent trackers for hydration sips (`{cur}/{max} Sips`) and farm irrigation litres (`{cur}/{max}L Farm`).

### Changed
* **Farmland Watering Tool Requirement**: Tilled soil plots now strictly require holding the Plant Waterer in the active hotbar slot to water crops.
* **Inventory TAB Toggle**: Pressing `TAB` now seamlessly toggles the character inventory both open and closed with single-frame debounce protection.
* **Stair & Slope Sprint Smoothness**: Added a grounding grace buffer (~0.1s decay) in player physics locomotion, eliminating stutter and sprint toggling when running down slopes or stairs.
* **Non-Dismissible Death Screen**: ESC key cannot dismiss the death screen, ensuring deliberate choice between Checkpoint, Load, or Main Menu.
* **Wardrobe UI Modernization**: Renamed hat option to English ("Toggle Hat"), migrated labels to TextMeshProUGUI, and optimized button click handlers for zero garbage collection.
* **Settings Controller Upgrade**: Modernized menu controllers to Unity 6 `FindFirstObjectByType` APIs.

### Fixed
* **Night Brawl Defeat Cleanup**: Aborted night brawl waves and purged all remaining nighttime monsters upon player collapse, ensuring zero leftover enemies persist into the 06:00 daytime cycle.
* **Persistent Monster Health Bars on Load Game**: Fixed an issue where floating monster and boss health bars remained on screen after loading a saved game.
* **Night Wave Spawning Hangs**: Fixed a bug where loading a game during the night phase stalled enemy wave spawning due to incorrect encounter-cleared flags.
* **Offscreen Indicator Memory Leaks**: Refactored enemy offscreen indicator pulsing to use entity instance IDs, guaranteeing destroyed or pooled monsters never leak in memory collections.
* **Scene Teardown Assertions**: Resolved console assertion failures (`go.IsActive()`) and object creation leaks when switching scenes or quitting.
* **Bed Respawn Ground Clipping**: Sanitized respawn elevation and routed positioning through teleportation physics sync to prevent character clipping into the floor.

---

# Patch Notes (V0.1.0)

### Added
* **Save & Load System**: The bedroom desk can now be used to save and load game progress across multiple slots.
* **Enemy & Boss Health Bars**: Added floating health bars above monsters and a dedicated cinematic health bar at the top of the screen for boss encounters.
* **New Combat Moves**: Added a 3-hit combo slash, Dash Attack, and Charged Heavy Attack.
* **Character Stats & Equipment Menu**: Press `Tab` to view character attributes, main inventory, and 11 armor/equipment slots.
* **16-Plot Farming System**: Complete farming loop including tilling soil, watering, planting seeds, and harvesting crops with dynamic soil feedback.
* **Expanded Homestead Compound**: The outdoor yard is now larger, neatly divided into 4 activity zones with an interactive campfire, pond pier, and workshop area.
* **Dynamic Tree Transparency**: Trees automatically fade to transparent when obstructing the camera's view of the player.

### Changed
* **Inventory Stacking**: Harvested crops and seeds are now capped at a maximum stack size of 20 items per inventory slot.
* **Manual Day/Night Sleep**: Time is fully static; players transition between day and night exclusively by using the debug key or resting in bed after clearing all night monsters.
* **Front Gate Invasions**: Night brawl monsters now funnel directly through the main front gate instead of spawning across the yard perimeter.
* **Lighting & Performance**: Rebalanced outdoor lighting and optimized environmental assets for smoother frame rates.

### Fixed
* Fixed an issue where the character could get stuck or drift upward when moving left with the `A` key.
* Fixed a bug where newly spawned wave enemies appeared at their previous death location instead of outside the front gate.
* Fixed Corn Musketeer projectile trajectories so bullets aim directly at the player rather than flying overhead.
* Fixed inverted water well roof geometry, missing purple materials on stone pathways, and clipping issues with the kitchen fridge, lanterns, and trophies.