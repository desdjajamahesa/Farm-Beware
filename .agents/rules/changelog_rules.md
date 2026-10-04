# Player-Facing Changelog Guidelines (`Docs/CHANGELOG.md`)

When creating or updating `Docs/CHANGELOG.md`, strictly adhere to the following formatting and styling rules:

## 1. Document Structure & Header
- **Title**: `# Patch Notes (Alpha X.Y.Z)` (e.g., `# Patch Notes (Alpha 0.2.0)`).
- **Categories**: Must contain only three standardized markdown headers:
  - `### Added`
  - `### Changed`
  - `### Fixed`

## 2. Bullet Point Formatting
- **Added & Changed**:
  - Format: `* **[Concise Feature Title]**: [Single punchy sentence explaining the feature and player experience].`
  - Example: `* **Character Stats & Equipment Menu**: Press `Tab` to view character attributes, main inventory, and 11 armor/equipment slots.`
- **Fixed**:
  - Format: `* Fixed [direct description of the player-facing issue resolved].`
  - Do NOT bold a feature prefix in `Fixed`. Start directly with `* Fixed ...`.
  - Group minor related visual/collision fixes into one consolidated sentence (e.g., `* Fixed inverted water well roof geometry, missing purple materials on stone pathways, and clipping issues with the kitchen fridge, lanterns, and trophies.`).

## 3. Curated High-Impact Distillation
- Keep each section focused on **4 to 7 high-level bullet points**.
- Avoid overwhelming players with micro-details; summarize related technical tasks into cohesive gameplay highlights.
- Highlight player controls or keybindings where relevant (e.g., `Press Tab`, `A key`).

## 4. Strict Zero-Jargon Rule
- **NEVER** include script files or extensions (`.cs`, `.prefab`, `.shader`, `.unity`).
- **NEVER** include internal class names, method names, enums, or variables (`EnemyObjectPool`, `SyncTransforms()`, `CS1061`, etc.).
- **NEVER** include engine systems, math formulas, or coordinates (`PhysX`, `Vector3`, `URP`, `Layer 12`, etc.).
- Write in 100% natural, polished, engaging English.
