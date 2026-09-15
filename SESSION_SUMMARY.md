# SESSION SUMMARY — 2026-09-14

## STATUS: **GENSHIN-STYLE COOKING + DIRTY/CLEAN SYSTEM — OPERATIONAL WITH POSITION DRIFT ⚠️**

---

## GENSHIN COOKING SYSTEM — IMPLEMENTED

### Architecture
- **GenshinStove.cs** (`Features/Kitchen/GenshinStove.cs`): MonoBehaviour + IInteractable. NOT a KitchenStation subclass. On E-key interact → opens `StoveUIManager` panel.
- **StoveUIManager.cs** (`Features/Kitchen/UI/StoveUIManager.cs`): Panel logic — recipe grid populate, ingredient check via `CountItem()`, instant cook via `RemoveItem()`+`AddItem()`, ESC/close handler. All text refs typed `TMPro.TextMeshProUGUI`.
- **KitchenRecipe.cs** updated with `RecipeIngredient` class, `List<RecipeIngredient> ingredients`, `recipeIcon`, `description`.

---

## MIGRASI TOTAL TO TEXTMESHPRO (PANEL_STOVE)

- Migrasi 8 elemen UI pada Panel_Stove dan seluruh Prefab terkait (RecipeButtonPrefab, IngredientRowPrefab) dari `UnityEngine.UI.Text` legacy ke `TMPro.TextMeshProUGUI`.
- Penyelesaian bug rendering text-drift via YAML direct modification (`m_VerticalAlignment = 512 / Midline`, `m_HorizontalAlignment = 2 / Center`).
- Penyesuaian `StoveUIManager.cs` untuk mengikat variabel `TextMeshProUGUI` secara type-safe.
- TMP `m_VerticalAlignment` tidak bisa diubah via `TextMeshProUGUI.alignment` C# API secara langsung ke serialized YAML — harus direct YAML edit untuk memastikan nilai 512 tersimpan.

---

## ARCHITECTURE LAYOUT "SINGLE CENTRAL AXIS"

### TopBar
- Anchor: Top-Stretch (0,1)→(1,1), Height 80px
- HeaderIcon: 50×50, Top-Left
- Title: "Cook", Left-Aligned, fontSize 44
- CloseButton/X: Centered, fontSize 28, Stretch-All (0,0)→(1,1), `m_VerticalAlignment=512`

### LeftContent
- Anchor: (0,0)→(0.38,0.89)
- GridLayoutGroup: Cell Size 150×180, Spacing 15×15

### RightContent (Single Central Axis — No Parent LayoutGroup)
```
RightContent (0.4,0)→(1,0.89)
├── TopDetailZone (0, 0.45)→(1, 1) — 55% height
│   ├── ResultName: Top-Stretch, Height=50, Y=-15, fontSize=40, Center+Midline
│   ├── ResultIcon: Top-Center (0.5,1), 150×150, Y=-70
│   ├── ResultDescription: Top-Stretch, Height=50, Y=-225, fontSize=22, Center+Midline
│   └── ProcessTime: Top-Stretch, Height=25, Y=-278, fontSize=18, Center+Midline
├── BottomIngredientZone (0, 0.18)→(1, 0.45) — 27% height
│   ├── IngredientLabel: Top-Stretch, Height=25, "BAHAN:", fontSize=20, Center+Midline
│   └── IngredientContainer: Stretch-All, offsetMax.y=-30
│       └── HorizontalLayoutGroup (MiddleCenter, Spacing=20, ChildControl=false)
└── BottomCookZone (0, 0)→(1, 0.18) — 18% height
    └── CookButton: Center anchor (0.5,0.5), 220×50, "MASAK!"
```

### IngredientRowPrefab (110×140px)
```
Root: 110×140
├── Icon: Top-Center (0.5,1), Pivot (0.5,1), 64×64, Y=-10
├── Name: Top-Center (0.5,1), Pivot (0.5,0.5), 100×34, Y=-92
│   TMP: fontSize=16, wordWrap=true, overflow=Truncate, Center+Midline
└── Count: Top-Center (0.5,1), Pivot (0.5,0.5), 100×22, Y=-123
    TMP: fontSize=16, wordWrap=false, overflow=Truncate, Center+Midline
```

### RecipeButtonPrefab (150×180px)
```
Root: 150×180
├── Icon: Top-Center, 120×120, Y=-10
└── Name: Bottom-Stretch, Height=30, fontSize=22, Center+Midline
```

---

## STOVEUI MANAGER REFERENCES (verified OK)
- `panelStove` → Panel_Stove
- `recipeListContent` → Content (GridLayoutGroup)
- `resultIcon` → Icon (Image)
- `resultName` → ResultName (TMP)
- `resultDescription` → ResultDescription (TMP)
- `processTimeText` → ProcessTime (TMP)
- `ingredientContainer` → IngredientContainer (RectTransform)
- `cookButton` → CookButton (Button)
- `cookButtonText` → Label (TMP)
- `cookButtonImage` → CookButton (Image)
- `closeButton` → TopBar/CloseButton (Button)
- `emptyStatePlaceholder` → EmptyStatePlaceholder (GameObject)
- `recipeButtonPrefab` → RecipeButtonPrefab (150×180)
- `ingredientRowPrefab` → IngredientRowPrefab (110×140)

---

## DIRTY/CLEAN SYSTEM — ARCHITECTURE OVERHAUL

### Problem
Each dirty→clean mapping required a separate `KitchenRecipe` ScriptableObject (Wash_Apple, Wash_Potato, etc.). Adding a new dirty item = new asset file. Recipe bloat.

### Solution: Item-Level Dirty/Clean Data
- **FoodItemData.cs**: Added `bool isDirty` + `ItemData cleanVariant`
- **MaterialItemData.cs**: Added `bool isDirty` + `ItemData cleanVariant`
- **KitchenSinkInteractable.cs**: Rewrote — removed `washRecipes` list. Now creates virtual recipe at runtime via `ScriptableObject.CreateInstance<KitchenRecipe>()`. Checks `FoodItemData.isDirty`/`cleanVariant` or `MaterialItemData.isDirty`/`cleanVariant`.

### Data Wiring (on assets)
| Item Asset | `isDirty` | `cleanVariant` |
|---|---|---|
| Apple_Dirty | ✅ | Apple_Clean |
| Potato_Dirty | ✅ | Potato_Clean |
| Tomato_Dirty | ✅ | Tomato_Clean |
| Carrot_Dirty | ✅ | Carrot_Clean |

### Deleted Assets (5 wash recipes)
- Wash_Apple.asset, Wash_Potato.asset, Wash_Tomato.asset, Wash_Carrot.asset, Recipe_WashCarrot.asset

### Benefits
- Zero recipe assets for washing
- Adding new dirty item = 2 Inspector fields (no new asset)
- Self-documenting on the item itself
- Sink becomes data-driven from item, not external recipe lookup

---

## KEY FIXES THIS SESSION

### 1. Kitchen Transform Alignment — **NOT YET APPLIED (2026-09-14)**
- `kitchen_sink.localPosition`: Current `(31.28, 0.42, 16.98)` → Target `(0, -0.30, 0)` — 17m Z drift
- `stove.localPosition`: Current `(34.17, 0.156, 17.019)` → Target `(-1.60, 1.65, 0)` — 17m Z drift
- Root cause: Kitchen parent (`Environment/Kitchen`) at `(32.57, -0.126, -0.039)` instead of origin

### 2. Kitchen Wall Logic Injection
- `cube` and `cube_1` under `Environment/Kitchen/cube` set to Layer 12 (Wall)
- 2 BoxColliders added per wall (trigger=true for occlusion, trigger=false for physics)
- `FeaturesCamera.WallOccluder` added with `transparentAlpha=0.15`, `fadeSpeed=8`

### 3. Input System Crash Fix
- `StoveUIManager.Update()`: Replaced `Input.GetKeyDown(KeyCode.Escape)` with `Keyboard.current.escapeKey.wasPressedThisFrame` (New Input System)
- Added `using UnityEngine.InputSystem`

### 4. UI Layout Fixes
- Grey bars: Fixed `childForceExpandWidth=true→false`, `childControlHeight=true→false`
- Header collapse: Set explicit height via `offsetMax.y=-60`
- RightPanel overlap: Restructured to RightContent `(0.40→1)` with 2% gap from LeftContent
- CookButton: Anchored to BottomCookZone, 220×50
- ESC handler: Added `Update()` with `Keyboard.current.escapeKey.wasPressedThisFrame`
- Cursor restore: `Close()` now sets `Cursor.visible=false; Cursor.lockState=Locked`

### 5. TMP Vertical Alignment Bug (YAML Direct Edit)
- `m_VerticalAlignment` on Name, Count (IngredientRowPrefab) and X (CloseButton) stuck at 4608 (Bottom) despite C# API setting 512 (Midline)
- Root cause: TMP `alignment` property doesn't directly serialize to `m_VerticalAlignment` in scene/prefab YAML
- Fix: Direct YAML text replacement `4608 → 512` via PowerShell

---

## ALL RECIPE ASSETS (Final State)

### Cook Recipes (wired in GenshinStove)
| Recipe | Input | Output |
|---|---|---|
| Cook_Fish | Fish Raw | Grilled Fish |
| Cook_Meat | Meat Raw | Steak Cooked |
| Cook_Potato | Potato Clean | Baked Potato |
| Cook_Tomato | Tomato Clean | Tomato Soup |
| Cook_Rice | Rice (Raw) | Cooked Rice |
| Cook_Veggies | Carrot (Clean) | Cooked Veggies |
| Recipe_CookCarrot | Carrot (Clean) | Carrot Cooked |

### Wash Recipes
**NONE** — All deleted. Washing now uses item-level `isDirty`/`cleanVariant` data.

---

## KITCHEN COMPONENT STATE (StagingScene) — **UPDATED 2026-09-14**

### Kitchen_Stove
- Transform: `localPosition (34.17, 0.156, 17.019)` — **NEEDS RESET**
- BoxCollider, MeshRenderer, WorldLabel, Highlightable, **GenshinStove** (7 cook recipes wired)
- **NOT**: No InventoryComponent, No StoveInteractable, No KitchenStationProgressOverlay

### Kitchen_Sink
- Transform: `localPosition (31.28, 0.42, 16.98)` — **NEEDS RESET**
- KitchenSinkInteractable (virtual recipe system, no washRecipes list)
- InventoryComponent with `allowedCategories=[Vegetable, Fruit]`
- KitchenStationProgressOverlay, KitchenStationSoundFx **PRESENT** (unexpected per old summary)
- **NO wash recipes should be wired** — virtual recipe system handles dirty→clean

### Refrigerator
- RefrigeratorInteractable
- InventoryComponent with `allowedCategories=[Vegetable, Fruit, Meat, Dish]`
- 6 test items: Apel x5, Wortel x5, Carrot Cooked x5, Apple Clean x5, Apple Dirty x5

### SinkManager (NEW)
- `Assets/Scripts/Features/Kitchen/SinkManager.cs` — exists, modified 2026-09-13
- Purpose: TBD (verify if used or legacy)

---

## FILE CHANGES THIS SESSION (2026-09-09) + **DELTA 2026-09-14**

| File | Changes |
|---|---|
| `Features/Kitchen/GenshinStove.cs` | **NEW** — MonoBehaviour + IInteractable, recipe list, opens StoveUIManager |
| `Features/Kitchen/UI/StoveUIManager.cs` | **NEW** — Panel logic, TMP text refs, recipe grid, ingredient check, instant cook, ESC handler, New Input System |
| `Features/Kitchen/KitchenRecipe.cs` | Added `RecipeIngredient`, `ingredients` list, `recipeIcon`, `description`, `IsMultiIngredient`, `GetAllIngredients()` |
| `Features/Kitchen/KitchenSinkInteractable.cs` | Rewrote — removed `washRecipes`, virtual recipe from `isDirty`/`cleanVariant`, `ScriptableObject.CreateInstance` |
| `Features/Inventory/Data/FoodItemData.cs` | Added `bool isDirty`, `ItemData cleanVariant` |
| `Features/Inventory/Data/MaterialItemData.cs` | Added `bool isDirty`, `ItemData cleanVariant` |
| `Prefabs/UI/RecipeButtonPrefab.prefab` | **NEW** — 150×180 grid cell, Icon Top 120×120, Name Bottom fontSize=22 |
| `Prefabs/UI/IngredientRowPrefab.prefab` | **NEW** — 110×140 box, Icon 64×64, Name fontSize=16, Count fontSize=16 |
| `Scenes/StagingScene.unity` | Panel_Stove fully migrated to TMP, all layout zones restructured to Single Central Axis |
| `Features/Kitchen/SinkManager.cs` | **EXISTS** (modified 2026-09-13) — purpose TBD, verify usage |

---

## NEXT ROADMAP (TUGAS TERTUNDA) — **UPDATED 2026-09-14**

0. **URGENT: Reset Kitchen Transforms** — `Environment/Kitchen` parent + `Kitchen_Stove` + `Kitchen_Sink` to origin (fixes 17m Z drift)
1. **Pemanis visual saat memasak** — VFX Asap/Api, SFX Memasak, Animasi UI Success
2. **Modularisasi 3D Mesh Tembok Dapur** — Layer 12 Wall agar skrip WallOccluder bekerja per segmen
3. **Integrasi Sprite Ikon pada HeaderIcon TopBar**
4. **Test Genshin cooking in Play Mode** — Walk to stove, press E, verify panel opens, select recipe, cook
5. **Test dirty→clean in Play Mode** — Put Apple_Dirty in sink, verify it becomes Apple_Clean
6. **Clean duplicate cook recipes** — Cook_Veggies and Recipe_CookCarrot both cook Carrot Clean (remove one)
7. **Verify SinkManager purpose** — Remove if unused / document if needed
8. **Replace trophy placeholder icons** — Drag final sprites to `ItemData.itemIcon` in each `TrophyCube_*.asset`
9. **Replace placeholder cube prefabs** — Swap TrophyCube prefabs with final 3D trophy models
10. **Trophy Cabinet quantity display** — Remove quantity text for trophy slots (always 1)
