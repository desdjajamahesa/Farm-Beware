# Patch Notes (V0.2.0)

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