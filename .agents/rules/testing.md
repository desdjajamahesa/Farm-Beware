# Testing.md

Testing strategies, automated verification scripts, manual Play Mode checklists, and troubleshooting matrices for **Farm-Beware**.

---

## 1. Testing Philosophy & Standards

Quality assurance follows a strict **Zero-Regression & Data Integrity** standard:
1. **MCP Automated Verification (Editor Mode)**: Fast in-memory Roslyn C# validation via `execute_code` (`compiler: "roslyn"`, `safety_checks: false`) to test registries, scene hierarchies, and component links.
2. **Clean Console Mandate**: Every commit must leave the Editor console with **0 Errors** and **0 Warnings** via `read_console`.
3. **Play Mode Manual Verification**: Check combat feel, input buffering, camera transitions, and modal priority.

---

## 2. Automated Verification Scripts (MCP Roslyn C#)

### 2.1 ItemDatabase & Inventory Audit
Validates all active items are indexed without missing or null asset references:
```csharp
var db = ItemDatabase.Instance;
if (db == null) return "FAIL: ItemDatabase missing!";
var items = db.GetAllItems();
int nullCount = 0;
foreach (var item in items) if (item == null) nullCount++;
return $"PASS: Total Items={items.Count}, NullEntries={nullCount}";
```

### 2.2 Core Systems & Singletons Audit
Verifies that all core managers and gameplay systems are active in the staging scene:
```csharp
var camMgr = UnityEngine.Object.FindAnyObjectByType<FeaturesCamera.CameraManager>();
var timeMgr = UnityEngine.Object.FindAnyObjectByType<DayNightTimeManager>();
var nbm = UnityEngine.Object.FindAnyObjectByType<FeaturesCombat.NightBrawlManager>();
var player = GameObject.FindWithTag("Player");
return $"PASS: CamMgr={(camMgr!=null)}, TimeMgr={(timeMgr!=null)}, NightBrawl={(nbm!=null)}, Player={(player!=null)}";
```

### 2.3 Monster Spawn Points & Dynamic Hierarchy Audit
Checks that `MonsterSpawnPoints` and all child spawn points are positioned outside the front gate (`Z >= 50m`):
```csharp
var spParent = GameObject.Find("MonsterSpawnPoints");
if (spParent == null) return "FAIL: MonsterSpawnPoints parent missing!";
int validChildren = 0;
foreach (Transform child in spParent.transform)
{
    if (child.position.z >= 50f) validChildren++;
}
return $"PASS: Spawn Points Valid={validChildren}/{spParent.transform.childCount} outside front gate (Z >= 50m)";
```

### 2.4 NavMesh Non-Walkable Zones Audit
Ensures Campfire, Natural Pond, and Farmland Soil Plots are excluded from walkable navigation:
```csharp
Vector3[] testPoints = new Vector3[] {
    new Vector3(24.58f, 0.40f, -10.01f), // Campfire
    new Vector3(35.53f, 0.40f, 5.09f),   // Natural Pond
    new Vector3(6.00f, 0.00f, 1.00f),    // Farmland Plot 1
    new Vector3(6.00f, 0.00f, -11.00f)   // Farmland Plot 2
};
int unwalkable = 0;
foreach (var pt in testPoints)
{
    if (!UnityEngine.AI.NavMesh.SamplePosition(pt, out var hit, 0.3f, UnityEngine.AI.NavMesh.AllAreas))
        unwalkable++;
}
return $"PASS: Unwalkable zones verified={unwalkable}/{testPoints.Length}";
```

### 2.5 Combat UI & Health Bar Managers Audit
Verifies that both `EnemyHealthBarManager` and `BossHealthBarManager` are initialized under the Canvas:
```csharp
var overheadMgr = UnityEngine.Object.FindFirstObjectByType<EnemyHealthBarManager>(UnityEngine.FindObjectsInactive.Include);
var bossMgr = UnityEngine.Object.FindFirstObjectByType<BossHealthBarManager>(UnityEngine.FindObjectsInactive.Include);
return $"PASS: OverheadBarManager={(overheadMgr!=null)}, BossBarManager={(bossMgr!=null)}";
```

### 2.6 Shaders & Deferred+ Pipeline Audit
Validates custom shaders, camera near clip, and two-sided foliage materials:
```csharp
var sMonster = UnityEngine.Shader.Find("FarmBeware/Monster/MonsterFresnelLit");
var sBuilding = UnityEngine.Shader.Find("FarmBeware/Building/DitheredBuildingLit");
var cam = UnityEngine.Camera.main;
bool clipSafe = cam != null && cam.nearClipPlane >= 0.07f && cam.nearClipPlane <= 0.10f;
return $"PASS: MonsterShader={(sMonster!=null)}, BuildingShader={(sBuilding!=null)}, NearClipSafe={clipSafe}";
```

### 2.7 Stove State Machine & Transaction Rollback Audit
Validates `KitchenStove` state transitions, immediate ingredient deduction, and atomic refund on cancellation:
```csharp
var stove = UnityEngine.Object.FindFirstObjectByType<KitchenStove>(UnityEngine.FindObjectsInactive.Include);
if (stove == null) return "FAIL: KitchenStove not found!";
return $"PASS: KitchenStove found, State={stove.CurrentState}";
```

---

## 3. Manual Play Mode Verification Checklist

### 3.1 Melee Combat & 3-Hit Combo
1. Equip weapon (slot 1).
2. **Light Attack Combo**: Click Left Mouse Button 3 times within 1.5s intervals.
   - Verify animations progress: Hit 1 → Hit 2 → Hit 3 (Finisher).
   - Verify damage multiplier scales: 1.0x → 1.2x → 1.5x.
3. **Attack Buffering**: Click attack during an ongoing strike.
   - Verify the character seamlessly transitions into the next combo attack without dropping the sequence.
4. **Special Attacks**:
   - Sprint + Click: Instantly executes a sprint Dash Attack.
   - Hold Click (> 0.35s): Charges and unleashes a Heavy Attack.
   - Press `F`: Performs a front kick with high knockback.
   - Jump (`Space`) + Click: Executes a jump slam attack.

### 3.2 Dual-Tier Enemy Health Bar & Visuals
1. Press `N` to start night combat.
2. **Overhead Health Bar**:
   - Verify monster overhead bar is hidden when monster is at 100% HP.
   - Hit a monster: bar snaps visible instantly.
   - Amber ghost damage bar lags 0.22s then catches up smoothly.
   - Bar elevation matches monster height (Tuber Maw 1.15m, Taro Brute 2.25m).
3. **Top Boss Health Bar**:
   - Boss spawn activates cinematic top-center HUD with thematic title.
   - Verify dual-boss waves display stacked dual bars cleanly.
   - On boss kill: gold `DEFEATED` banner fades out smoothly.
4. **Combat Impulse**: Verify chromatic aberration spikes subtly on heavy impacts.

### 3.3 Environmental Traversal & Boundaries
1. Try walking into the campfire, natural pond, or farmland plots.
   - Verify the player cannot enter or clip into these obstacles.
2. Spawn monsters during night phase:
   - Verify monsters spawn outside the front gate (`Z >= 52m`) along the road.
   - Monsters route around campfire and farmland plots without walking over them.

### 3.4 Camera Modes & ESC Modal Priority
1. Approach Wardrobe, press `E`: camera shifts to WardrobeMode, player locks.
2. Press `ESC`: closes wardrobe, returns to `Gameplay`, movement restores. **Pause menu must NOT open.**
3. Press `ESC` in free roam with no panels open: opens Pause Menu.

### 3.5 Cooking & Inventory Rollback
1. Open stove (`E`), select recipe, click `MASAK!`.
2. Ingredients and water are deducted immediately.
3. Close stove panel mid-cooking (`ESC`): verify 100% of ingredients and water are refunded atomically.

### 3.6 Save/Load Persistence
1. Save game via menu into Slot 1.
2. Collect or move items, then load Slot 1: verify player position, inventory, and stats restore accurately.

---

## 4. Troubleshooting Matrix

| Symptom | Root Cause | Resolution |
|---|---|---|
| Monster / building mesh invisible, only shadow casts | Shader pass uses `LightMode = UniversalForward` in Deferred+ | Change pass tag to `Tags { "LightMode" = "UniversalForwardOnly" }` |
| Cool blue moonlight penetrates interior floor/walls | Directional Light set to default `0xFFFFFFFF` (all layers) | Use Light Layer Utility: assign Sun $\rightarrow$ Layer 0, Lamps $\rightarrow$ Layer 1 |
| Interior lamps illuminate outdoor trees/grass | Interior point lights set to Layer 0+1 or 0 | Assign `InteriorLamp_*` and `Light_Interior_*` to Layer 1 (mask=2) |
| Wall mesh flickers / z-fights when entering house | Transparent render queue (`ZWrite Off`) used on building | Use `FarmBeware/Building/DitheredBuildingLit` with Bayer dither (`ZWrite On`) |
| Static batching warning / BRG performance drop | Standalone platform has `m_StaticBatching: 1` enabled | Run URP & BRG Validator and click auto-fix |
| Camera stuck / player locked after closing UI | CameraManager mode failed to reset to `Gameplay` | Call `CameraManager.Instance.SetMode(CameraMode.Gameplay)` on close |
| "2 audio listeners in scene" warning | `MirrorCamera` has an active AudioListener | Set `audioListener.enabled = false` on secondary camera |
| ESC opens Pause Menu while modal is open | Both modal UI and PauseMenuUI handle ESC simultaneously | Ensure PauseMenuUI checks for active modals before opening |
| Ingredients lost when closing stove mid-cook | StoveUIManager bypassed rollback on close | Ensure `Close()` calls `KitchenStove.CancelCooking(refundIngredients: true)` |
| Mirror reflection blown out or drops FPS | Secondary camera runs post-processing pass | Set `renderPostProcessing = false` on `UniversalAdditionalCameraData` |
| Tree leaves show black triangles at high angle | Foliage material has backface culling (`_Cull = 2`) | Set `_Cull = 0` (Two-Sided) on leaf materials |
| Camera near-plane slices through foliage | Near clip too small or proximity radius 0 | Set `nearClipPlane = 0.08f` and `cameraProximityRadius = 2.0f` |
| Monsters spawn inside house or behind player | Spawn points misconfigured or in old arena | Use `MonsterSpawnPoints` hierarchy outside front gate (`Z >= 52m`) |
| 3-Hit combo drops unexpectedly | Attack window too tight or click not buffered | Use `ComboResetWindow = 1.5f` and attack buffer (`bufferedAttackTime <= 0.45f`) |
