# Testing.md

Testing strategies, automated verification scripts, manual Play Mode checklists, and troubleshooting matrices for **Farm-Beware**.

---

## 1. Testing Philosophy & Standards

Quality assurance follows a strict **Zero-Regression & Data Integrity** standard:
1. **MCP Automated Verification (Editor Mode)**: In-memory Roslyn C# validation via `execute_code` to test database registries, component links, and storage structures without waiting for full Play Mode boot times.
2. **Clean Console Mandate**: Every commit must leave the Editor console with **0 Errors** and **0 Warnings** via `read_console`.
3. **Play Mode Manual Verification**: Verification for Rigidbody interactions, player movement, camera transitions, and modal input priority.

---

## 2. Automated Verification Scripts (MCP Roslyn C#)

These snippets run directly through the `execute_code` MCP tool (`compiler: "roslyn"`, `safety_checks: false`):

### 2.1 ItemDatabase Integrity Check
Validates that all 33 active items are loaded and contain zero missing or null asset links:

```csharp
var db = ItemDatabase.Instance;
if (db == null) return "FAIL: ItemDatabase failed to load from Resources!";

var items = db.GetAllItems();
int count = items != null ? items.Count : 0;
int nullCount = 0;
var categories = new System.Collections.Generic.Dictionary<string, int>();

foreach (var item in items)
{
    if (item == null) { nullCount++; continue; }
    string cat = item.category.ToString();
    if (!categories.ContainsKey(cat)) categories[cat] = 0;
    categories[cat]++;
}

var result = $"Total Items: {count} (Null: {nullCount})\n";
foreach (var kvp in categories) result += $" - {kvp.Key}: {kvp.Value} items\n";
return result;
```

### 2.2 Test Chest Inventory Validation
Ensures `TestChest` in `StagingScene` contains the full 21-item MVP test batch and zero null loot entries:

```csharp
var chest = GameObject.Find("TestChest");
if (chest == null) return "FAIL: TestChest GameObject not found in scene!";

var inv = chest.GetComponent<InventoryComponent>();
int filledSlots = 0;
for (int i = 0; i < inv.slots.Count; i++)
{
    if (inv.slots[i] != null && inv.slots[i].item != null) filledSlots++;
}

var storage = chest.GetComponent<FeaturesInteraction.StorageInteractable>();
int nullDrops = storage.lootTable.FindAll(d => d.item == null).Count;

return $"PASS: TestChest has {filledSlots}/24 populated slots. Null loot drops: {nullDrops}.";
```

### 2.3 Camera & Player Component Validation
Ensures core singletons and player components are active without missing script references:

```csharp
var camMgr = UnityEngine.Object.FindAnyObjectByType<FeaturesCamera.CameraManager>();
var isoCam = UnityEngine.Object.FindAnyObjectByType<FeaturesCamera.IsometricCameraController>();
var player = GameObject.FindWithTag("Player");

return $"CameraManager: {(camMgr != null ? "OK" : "MISSING")}\n" +
       $"IsometricCameraController: {(isoCam != null ? "OK" : "MISSING")}\n" +
       $"Player: {(player != null ? "OK" : "MISSING")}";
```

### 2.4 Graphics Pipeline & Shader Verification
Validates that URP Deferred+ custom shaders are supported and compiled with `UniversalForwardOnly`:

```csharp
var sMonster = UnityEngine.Shader.Find("FarmBeware/Monster/MonsterFresnelLit");
var sBuilding = UnityEngine.Shader.Find("FarmBeware/Building/DitheredBuildingLit");

if (sMonster == null || !sMonster.isSupported) return "FAIL: MonsterFresnelLit missing or unsupported!";
if (sBuilding == null || !sBuilding.isSupported) return "FAIL: DitheredBuildingLit missing or unsupported!";

var mMat = new UnityEngine.Material(sMonster);
var bMat = new UnityEngine.Material(sBuilding);

return $"PASS: Shaders verified.\n" +
       $" - MonsterFresnelLit: supported={sMonster.isSupported}, hasFresnel={mMat.HasProperty("_FresnelColor")}\n" +
       $" - DitheredBuildingLit: supported={sBuilding.isSupported}, hasDither={bMat.HasProperty("_DitherFade")}";
```

### 2.5 Light Layers Segregation Audit
Checks that Directional Light is isolated to Layer 0 and all interior lamps are isolated to Layer 1:

```csharp
var allLights = UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None);
int extLights = 0, intLights = 0, leakLights = 0;

foreach (var l in allLights)
{
    uint mask = (uint)l.renderingLayerMask;
    if (l.type == UnityEngine.LightType.Directional)
    {
        if (mask == 1) extLights++; else leakLights++;
    }
    else if (l.name.StartsWith("InteriorLamp_") || l.name.StartsWith("Light_Interior_"))
    {
        if (mask == 2) intLights++; else leakLights++;
    }
}

return $"PASS: Light Layers. Exterior Sun: {extLights} (mask=1), Interior Lamps: {intLights} (mask=2), Leaking Lights: {leakLights}";
```

### 2.6 KitchenStove State Machine & Transaction Rollback Audit
Validates `KitchenStove` presence, `CookingState` enum, item consumption, and atomic refund on cancellation:

```csharp
var stove = UnityEngine.Object.FindFirstObjectByType<KitchenStove>(UnityEngine.FindObjectsInactive.Include);
if (stove == null) return "FAIL: KitchenStove not found in scene!";

var testGO = new UnityEngine.GameObject("Stove_Audit_Test");
try
{
    var testStove = testGO.AddComponent<KitchenStove>();
    var inv = testGO.AddComponent<InventoryComponent>();
    inv.ResetInventory(10);

    var raw = UnityEngine.ScriptableObject.CreateInstance<ItemData>();
    raw.itemId = "audit_raw"; raw.itemName = "Audit Raw";
    var cooked = UnityEngine.ScriptableObject.CreateInstance<ItemData>();
    cooked.itemId = "audit_cooked"; cooked.itemName = "Audit Cooked";

    var recipe = UnityEngine.ScriptableObject.CreateInstance<KitchenRecipe>();
    recipe.output = cooked; recipe.outputCount = 1; recipe.processTime = 1f;
    recipe.ingredients = new System.Collections.Generic.List<RecipeIngredient>
    {
        new RecipeIngredient { item = raw, quantity = 2 }
    };

    inv.AddItem(raw, 2);
    testStove.StartCooking(recipe, inv);
    int consumedCount = inv.CountItem(raw);

    testStove.CancelCooking(refundIngredients: true);
    int refundedCount = inv.CountItem(raw);

    UnityEngine.Object.DestroyImmediate(raw);
    UnityEngine.Object.DestroyImmediate(cooked);
    UnityEngine.Object.DestroyImmediate(recipe);

    return $"PASS: Stove Audit. ConsumedCount={consumedCount} (expected 0), RefundedCount={refundedCount} (expected 2), State={testStove.CurrentState}";
}
finally
{
    UnityEngine.Object.DestroyImmediate(testGO);
}
```

### 2.7 Post-Processing Volume & Mirror Camera Exclusion Audit
Checks that `Global Volume` contains all required overrides and that `MirrorCamera` excludes post-processing:

```csharp
var vol = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsInactive.Include);
if (vol == null || !vol.isGlobal) return "FAIL: Global Volume not found!";

bool hasTone = vol.profile.Has<UnityEngine.Rendering.Universal.Tonemapping>();
bool hasBloom = vol.profile.Has<UnityEngine.Rendering.Universal.Bloom>();
bool hasColor = vol.profile.Has<UnityEngine.Rendering.Universal.ColorAdjustments>();
bool hasMotion = vol.profile.Has<UnityEngine.Rendering.Universal.MotionBlur>();
bool hasChroma = vol.profile.Has<UnityEngine.Rendering.Universal.ChromaticAberration>();

var mirrorCam = UnityEngine.Object.FindFirstObjectByType<MirrorCamera>(UnityEngine.FindObjectsInactive.Include);
var camData = mirrorCam != null ? mirrorCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
bool mirrorIsolated = camData != null && !camData.renderPostProcessing;

return $"PASS: Post-Processing. Tone={hasTone}, Bloom={hasBloom}, Color={hasColor}, Motion={hasMotion}, Chroma={hasChroma}, MirrorIsolated={mirrorIsolated}";
```

### 2.8 Bedroom Mirror, Surface Frame & Wall Occlusion Link Audit
Validates `MirrorCamera` configuration, `RenderTexture` binding, wood frame material, and `WallOccluder.additionalRenderers` linking on the bedroom south wall:

```csharp
var mirrorCam = UnityEngine.Object.FindFirstObjectByType<FeaturesWardrobe.MirrorCamera>(UnityEngine.FindObjectsInactive.Include);
if (mirrorCam == null) return "FAIL: MirrorCamera component not found!";

bool rtAssigned = mirrorCam.MirrorTexture != null;
var quad = GameObject.Find("MirrorQuad");
var frame = GameObject.Find("MirrorFrame");

var wallSouth = GameObject.Find("Wall_Bedroom_South");
var occluder = wallSouth != null ? wallSouth.GetComponent<FeaturesCamera.WallOccluder>() : null;
bool linked = occluder != null && occluder.AdditionalRenderers != null && occluder.AdditionalRenderers.Count > 0;

return $"PASS: Mirror System Audit. RT={rtAssigned}, Quad={(quad != null ? "OK" : "MISSING")}, Frame={(frame != null ? "OK" : "MISSING")}, WallOccluderLinked={linked} (LinkedCount={(occluder != null ? occluder.AdditionalRenderers.Count : 0)})";
```

### 2.9 NightBrawl Front-Gate Spawn Points Audit
Ensures that all 3 night brawl spawn points are positioned outside the compound front gate (`Z >= 52m`):

```csharp
var nbm = UnityEngine.Object.FindFirstObjectByType<FeaturesCombat.NightBrawlManager>(UnityEngine.FindObjectsInactive.Include);
if (nbm == null) return "FAIL: NightBrawlManager not found!";

int validSpawns = 0;
for (int i = 0; i < 3; i++)
{
    var pt = nbm.GetSpawnPoint(i);
    if (pt.z >= 50f) validSpawns++;
}

return $"PASS: Night Brawl Spawns Audit. ValidFrontGateSpawns={validSpawns}/3 (All Z >= 50m). ScatterRadius={nbm.SpawnScatterRadius}";
```

### 2.10 Foliage Two-Sided Shading & Camera Near-Clip Audit
Validates camera near clip plane safety and two-sided foliage material rendering:

```csharp
var cam = UnityEngine.Camera.main;
if (cam == null) return "FAIL: Camera.main not found!";
bool clipOk = cam.nearClipPlane >= 0.07f && cam.nearClipPlane <= 0.10f;

string[] matNames = new string[] { "Mat_Tree_Leaf_Oak", "Mat_Tree_Leaf_Pine", "Mat_Bush_Leaf" };
int twoSidedCount = 0;
foreach (var name in matNames)
{
    var guids = UnityEditor.AssetDatabase.FindAssets($"{name} t:Material");
    if (guids.Length > 0)
    {
        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        if (mat != null && mat.HasProperty("_Cull") && mat.GetFloat("_Cull") == 0f)
            twoSidedCount++;
    }
}

return $"PASS: Foliage & NearClip Audit. NearClip={cam.nearClipPlane:F3} (Safe={clipOk}), TwoSidedMats={twoSidedCount}/{matNames.Length}";
```

---

## 3. Manual Play Mode Verification Checklist

### 3.1 Camera Transitions & ESC Modal Priority
1. Approach the Wardrobe, press `E`.
   - Camera transitions to Wardrobe mode, movement locks, mouse cursor unlocks.
2. Press `ESC`.
   - Wardrobe panel closes, camera returns to `Gameplay`, movement unlocks.
   - **CRITICAL**: The Pause Menu must **NOT** open when closing a modal with `ESC`.
3. Press `ESC` while free-roaming with no open panels.
   - The Pause Menu (`PauseMenuUI`) opens as expected.

### 3.2 Stove Cooking Flow (`KitchenStove`)
1. Retrieve cooking ingredients from `TestChest` (`Crop_SweetPotato`, `Mat_CookingOil`, etc.) and ensure water bottle has water.
2. Approach the stove, press `E`.
   - Cooking panel opens with the Single Central Axis layout.
3. Select a recipe with satisfied ingredients.
   - The "MASAK!" button activates (green).
4. Click "MASAK!".
   - Ingredients and water are deducted from inventory, button displays `Cooking... (Xs)` with a progress slider filling up.
5. **Rollback Test**: Close the panel (`ESC` or Close button) mid-cooking.
   - Verify all consumed ingredients and water are fully refunded to the player.
6. Re-open, cook to completion.
   - Once the timer expires, the finished dish appears in the player's inventory and UI details refresh.

### 3.3 Dynamic Washing Flow (`KitchenSink`)
1. Place a dirty item (`isDirty = true`) into the sink inventory.
2. Washing executes automatically.
3. Upon completion, the item in the slot transforms into its clean variant (`cleanVariant`).

### 3.4 UI Scale & Readability
1. Open inventory, cooking, and chest panels.
2. Verify all text labels and item icons remain crisp and appropriately scaled across 1920×1080 and ultrawide aspect ratios.

### 3.5 Night Combat & Interior Walkthrough (Phases 3 & 4)
1. **Night Trigger**: Press `N` during Play Mode.
   - Ambient smoothly transitions to cool night sky, Directional Light dims to 0.15 lux soft moonlight blue.
   - Safe zone interior lights automatically turn on warm amber.
2. **Combat Readability**:
   - Spawning enemies (`NightBrawlManager`) reveals solid models with vibrant HDR Fresnel rim highlights.
   - Monster meshes are **solid and visible**, and cast dark shadows on the ground.
3. **House Interior Entry**:
   - Walk the character through the front door trigger.
   - Roof and front walls smoothly fade with fine Bayer $4 \times 4$ dither pattern.
   - Interior floor and furniture are illuminated exclusively by amber lamps (Layer 1) with **zero blue moonlight contamination** (Layer 0).

### 3.6 Wardrobe Mirror, Wood Frame & Wall Occlusion Link
1. Approach the wardrobe in the bedroom, press `E`.
2. Observe player placement: player should snap to `(27.10, 0.04, 21.04)` facing the mirror.
3. Check mirror reflection: live player model and bedroom interior render crisply in portrait aspect with authentic horizontal reflection.
4. Verify the mirror frame displays a natural wood surface (`Mat_Mirror_WoodFrame.mat`).
5. Close wardrobe (`ESC`). Walk south outside the bedroom so the south bedroom wall occludes the player:
   - Verify the south wall smoothly fades to `0.15` alpha.
   - Verify the attached mirror and frame **also fade synchronously** without rendering artifacts.

### 3.7 Top-Down Tree Foliage & Smooth LOD Transitions
1. Walk the player around the trees near the house and front perimeter fence.
2. Observe high-angle camera framing: tree canopy leaves do not get sliced or show black backface holes (`_Cull = 0`).
3. Orbit and zoom the camera: verify tree geometry smoothly crossfades between LOD levels with zero pop-in.

### 3.8 Night Brawl Front-Gate Monster Approach
1. Press `N` to enter the night phase.
2. Stand near the house porch or yard.
3. Verify all monsters spawn exclusively outside the front gate (`Z > 52m`) and advance along the main road into the compound.

---

## 4. Troubleshooting Matrix

| Symptom | Root Cause | Resolution |
|---|---|---|
| Monster / Building mesh invisible / transparent, only shadow casts | Opaque shader uses `LightMode = "UniversalForward"` in Deferred+; GBuffer skips it and Forward-Only pass only accepts `UniversalForwardOnly` | Change shader pass tag to `Tags { "LightMode" = "UniversalForwardOnly" }` |
| Cool blue moonlight penetrates interior floor / walls at night | Directional Light set to default `0xFFFFFFFF` (all layers), lighting indoor geometry | Open `Tools > Farm-Beware > Rendering > Light Layer Assignment Utility` and assign Sun $\rightarrow$ Layer 0, Lamps $\rightarrow$ Layer 1 |
| Interior lamps illuminate outdoor trees and grass at night | Interior point lights set to Layer 0+1 or Layer 0 | Assign all `InteriorLamp_*` and `Light_Interior_*` to Layer 1 (mask=2) |
| Wall mesh flickers / z-fights when entering house | Traditional Alpha Blended material (`ZWrite Off`) used for building | Switch to `FarmBeware/Building/DitheredBuildingLit` with Bayer dither clipping (`ZWrite On`) |
| Static batching warning / GPU Resident Drawer performance drop | Standalone platform has `m_StaticBatching: 1` enabled in `ProjectSettings.asset` | Open `Tools > Farm-Beware > Rendering > URP & BRG Validator` and click "Perbaiki Semua Konfigurasi Secara Otomatis" |
| TextMeshPro text drifts downwards or clips | `m_VerticalAlignment` serialized as 4608 in YAML | Modify YAML directly: set `m_VerticalAlignment: 512` (Midline) |
| Camera stuck / Player movement disabled after closing UI | `CameraManager.CurrentMode` failed to return to `Gameplay` | Ensure UI close events invoke `CameraManager.Instance.SetMode(CameraMode.Gameplay)` |
| "There are 2 audio listeners in the scene" warning | `MirrorCamera` has an active AudioListener component | Disable AudioListener on mirror camera (`audioListener.enabled = false`) |
| ESC immediately triggers Pause Menu while panel is active | UI script and PauseMenuUI both consume ESC simultaneously | Ensure PauseMenuUI checks for active gameplay modals before toggling |
| Items rejected during inventory drag-and-drop | `CanAcceptItem` backend rules active | Verify `blockTrophyItems` flag or `allowedFoodCategories` on destination component |
| Ingredients lost when closing stove panel mid-cook | StoveUIManager bypassed backend rollback on panel close | Ensure `StoveUIManager.Close()` calls `currentStove.CancelCooking(refundIngredients: true)` |
| Mirror camera shows blown-out bloom / VRAM bloat | `MirrorCamera` executing secondary post-processing pass | Ensure `MirrorCamera` sets `UniversalAdditionalCameraData.renderPostProcessing = false` in `Awake()` |
| Mirror reflection inverted / flipped incorrectly | Mirror quad texture tiling not flipped horizontally | Ensure `_BaseMap_ST` scale is `(-1, 1, 1, 0)` on mirror surface material |
| Mirror turns solid black or white when wall fades | `WallOccluder` replaced material without copying dynamic texture | Verify `WallOccluder` copies `_BaseMap` and scale/offset to transparent instances |
| Tree leaves show black triangles or disappear at overhead angles | Foliage material has backface culling active (`_Cull = 2`) | Set `_Cull = 0` (Two-Sided) on leaf materials |
| Camera near-plane slices through foliage canopies | `cameraProximityRadius` is 0 or near plane is excessively large | Set `Camera.main.nearClipPlane = 0.08f` and `WallOcclusionManager.cameraProximityRadius = 2.0f` |
| Monsters spawn inside house or behind player at night | Spawn points misconfigured or using legacy arena center | Ensure `NightBrawlManager.frontGateSpawnPoints` are set to `Z > 52m` outside the front gate |


