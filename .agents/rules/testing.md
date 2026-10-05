# Testing.md

Testing strategies, automated verification scripts, manual Play Mode checklists, and troubleshooting for **Farm-Beware**.

---

## 1. Quality Assurance Standards

1. **MCP Automated Verification (Editor Mode)**: Fast in-memory Roslyn C# validation via `execute_code` (`compiler: "roslyn"`, `safety_checks: false`) to test pure POCO logic, registries, and scene hierarchies.
2. **Clean Console Mandate**: Every commit must leave the Editor console with **0 Errors** and **0 Exceptions** via `read_console`.
3. **Play Mode Verification**: Check combat responsiveness, input buffering, camera transitions, and modal priority.

---

## 2. Core Automated Verification Suites (MCP Roslyn C#)

### 2.1 POCO Combat Suite
Execute in-memory unit tests validating pure combat logic:
- `PoiseTracker`: Super armor mitigation, poise damage, knockdown tier-upgrade invariant, and automatic super armor restoration upon full regen.
- `AttackTokenDispatcher`: Melee/ranged slot limits, priority allocation (`Minion < Elite < Boss`), and preemptive minion eviction preventing boss starvation.
- `DefenseEvaluator`: Precision parry window (350ms), dodge i-frames (300ms), whiff recovery lockout (350ms) on natural expiration, and lockout cancellation on success.
- `CombatStateEvaluator`: 3-hit combo progression, branch cancel windows, and `InterruptCombatSequence()` timestamp resets.

### 2.2 Scene & Component Integrity
- **Singletons**: Verify `CameraManager`, `DayNightTimeManager`, `NightBrawlManager`, `ItemDatabase`, and `Player` exist.
- **NavMesh Boundaries**: Validate that Campfire, Natural Pond, and Farmland Plots are carved non-walkable.
- **Spawn Hierarchy**: Ensure all `MonsterSpawnPoints` children are positioned outside front gate (`Z >= 50m`).
- **Shaders**: Verify `MonsterFresnelLit` and `DitheredBuildingLit` compile cleanly; verify `Camera.main.nearClipPlane` is between `0.07f` and `0.10f`.

---

## 3. Manual Play Mode Verification Checklist

### 3.1 Combat & Defensive Feel
1. **3-Hit Combo & Buffering**: Equip weapon, click 3 times. Verify Hit 1 → Hit 2 → Hit 3 scale (1.0x → 1.2x → 1.5x) and clicks during swings buffer seamlessly.
2. **Defensive Actions**:
   - Dodge Roll (`C` / `Left Alt`): Verify directional roll matches input direction with 300ms i-frames.
   - Precision Parry (`V` / `Left Ctrl`): Deflecting an enemy hit triggers "PARRY!" text and hitstop. Whiffing triggers 350ms lockout where parry cannot be spammed.
3. **Special Attacks**: Sprint Dash Attack, Charged Heavy (Hold > 0.35s), Front Kick (`F`), Jump Slam (`Space` + Click).

### 3.2 UI, Navigation & Stations
1. **Health Bars**: Overhead bars show ghost damage lag; top boss HUD displays title, health, and `DEFEATED` badge.
2. **Boundaries**: Monsters and player cannot walk through campfire, pond, or farmland plots.
3. **ESC Modal Priority**: ESC closes active panels (`Stove`, `Wardrobe`, `Chest`, `Inventory`) without opening Pause Menu.
4. **Station Rollback**: Closing stove mid-cook refunds 100% of ingredients and water atomically.

---

## 4. Key Troubleshooting Matrix

| Symptom | Root Cause | Resolution |
|---|---|---|
| Mesh invisible in Deferred+ | Pass uses `UniversalForward` | Set `Tags { "LightMode" = "UniversalForwardOnly" }` |
| BRG instancing broken | Shader lacks instancing macros | Use `#pragma multi_compile_instancing` & `UNITY_INSTANCING_BUFFER_START(Props)` |
| Monster clips through fences | Discrete physics during knockback | Enable CCD ContinuousDynamic & predictive raycasts in `AirborneHazardEntity` |
| `Warp` console error on monster | Monster warped off NavMesh | Sample valid ground with `NavMesh.SamplePosition` before calling `Warp` |
| Boss starved of attack tokens | Minions occupy all token slots | Dispatch with `AttackTokenPriority.Boss` to trigger preemptive eviction |
| Parry spammable without penalty | Missing whiff recovery lockout | Enforce `CanInitiateParry` and 350ms whiff timer in `DefenseEvaluator` |
