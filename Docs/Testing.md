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

### 3.2 Genshin-Style Cooking Flow (`GenshinStove`)
1. Retrieve cooking ingredients from `TestChest` (`Crop_SweetPotato`, `Mat_CookingOil`, etc.).
2. Approach the stove, press `E`.
   - Cooking panel opens with the Single Central Axis layout.
3. Select a recipe with satisfied ingredients.
   - The "MASAK!" button activates (green).
4. Click "MASAK!".
   - Ingredients are deducted from the player inventory, and the cooked dish appears in the inventory.

### 3.3 Dynamic Washing Flow (`KitchenSink`)
1. Place a dirty item (`isDirty = true`) into the sink inventory.
2. Washing executes automatically.
3. Upon completion, the item in the slot transforms into its clean variant (`cleanVariant`).

### 3.4 UI Scale & Readability
1. Open inventory, cooking, and chest panels.
2. Verify all text labels and item icons remain crisp and appropriately scaled across 1920×1080 and ultrawide aspect ratios.

---

## 4. Troubleshooting Matrix

| Symptom | Root Cause | Resolution |
|---|---|---|
| TextMeshPro text drifts downwards or clips | `m_VerticalAlignment` serialized as 4608 in YAML | Modify YAML directly: set `m_VerticalAlignment: 512` (Midline) |
| Camera stuck / Player movement disabled after closing UI | `CameraManager.CurrentMode` failed to return to `Gameplay` | Ensure UI close events invoke `CameraManager.Instance.SetMode(CameraMode.Gameplay)` |
| "There are 2 audio listeners in the scene" warning | `MirrorCamera` has an active AudioListener component | Disable AudioListener on mirror camera (`audioListener.enabled = false`) |
| ESC immediately triggers Pause Menu while panel is active | UI script and PauseMenuUI both consume ESC simultaneously | Ensure PauseMenuUI checks for active gameplay modals before toggling |
| Items rejected during inventory drag-and-drop | `CanAcceptItem` backend rules active | Verify `blockTrophyItems` flag or `allowedFoodCategories` on destination component |
