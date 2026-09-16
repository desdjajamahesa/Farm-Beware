# Patterns.md

Coding standards, recurring design patterns, and strict anti-patterns for the **Farm-Beware** project.

---

## 1. Recurring Architectural Patterns

### 1.1 Awake-Safe Singleton with Fallback Resolver
Used across primary managers (`TimeManager`, `CameraManager`, `TrophySystemManager`) to ensure access never returns null due to out-of-order execution:

```csharp
private static T _instance;
public static T Instance
{
    get
    {
        if (_instance == null)
        {
            T[] found = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found != null && found.Length > 0)
                _instance = found[0];
        }
        return _instance;
    }
    private set { _instance = value; }
}
```

### 1.2 Central Camera Delegation Pattern
Feature managers are **strictly forbidden** from directly enabling, disabling, or transforming cameras. All transitions must delegate through `CameraManager`:

```csharp
// Enter an interactive feature mode
CameraManager.Instance.SetMode(CameraManager.CameraMode.WardrobeMode, wardrobeRootTransform);

// Exit back to standard gameplay
CameraManager.Instance.SetMode(CameraManager.CameraMode.Gameplay);
```

`CameraManager` handles:
- Transition validation.
- Camera activation/deactivation.
- Positioning relative to context roots.
- Input locking (`PlayerControl.isInputLocked`).
- Cursor lock and visibility states (`Cursor.lockState`, `Cursor.visible`).

### 1.3 Gameplay Camera Mode Guard
Camera controllers must include a guard condition at the start of their update loop to prevent conflicting with interactive modes:

```csharp
void LateUpdate()
{
    if (CameraManager.Instance != null && 
        CameraManager.Instance.CurrentMode != CameraManager.CameraMode.Gameplay)
    {
        return;
    }

    // Follow, orbit, and zoom execution
}
```

### 1.4 Event-Driven UI (Stateless Renderers)
Backend components own state and emit events. UI scripts listen to events and re-render without maintaining duplicate state:

```csharp
private void OnEnable()
{
    if (targetInventory != null)
        targetInventory.OnInventoryChanged += RefreshUI;
}

private void OnDisable()
{
    if (targetInventory != null)
        targetInventory.OnInventoryChanged -= RefreshUI;
}
```

### 1.5 Virtual Recipe Pattern (Item-Level Transformation)
To prevent recipe asset explosion for simple transformations, transformation data is embedded directly in the source item data:

```csharp
// In FoodItemData or MaterialItemData
public bool isDirty;
public ItemData cleanVariant;

// In cleaning station (KitchenSinkInteractable)
if (foodItem.isDirty && foodItem.cleanVariant != null)
{
    var virtualRecipe = ScriptableObject.CreateInstance<KitchenRecipe>();
    virtualRecipe.SetProcess(foodItem, foodItem.cleanVariant, processTime: 2.0f);
    StartProcessing(virtualRecipe);
}
```

### 1.6 Dual-Inventory Pattern (Trophy Rack)
Decouples logical storage from in-world 3D visual anchors:
- **`CabinetInventory`**: Physical storage data list.
- **`RackInventory`**: Visual source of truth where each slot corresponds to a 3D `SnapPoint`.
- Drag-and-drop or raycast clicks transfer items between these inventories seamlessly.

### 1.7 Item Stack Size Invariant (Max Stack = 20)
All stackable items enforce a hard ceiling of 20 units per inventory slot:
- **`ItemData.maxStack`**: Clamped to `[Range(1, 20)]`. Any value above 20 is strictly prohibited and automatically clamped in `OnValidate()`.
- Stackable items (crops, food, ingredients, materials, seeds, monster drops): `maxStack = 20`.
- Non-stackable equipment (weapons, tools, trophies): `maxStack = 1`.

---

## 2. Coding Standards & Conventions

### 2.1 Namespaces & Naming
- Modular namespaces under `Features<ModuleName>`:
  - `FeaturesCamera`, `FeaturesInteraction`, `FeaturesInventory`, `FeaturesKitchen`, `FeaturesTrophy`, `FeaturesWardrobe`.
- Public Classes, Methods, Properties, Enums: **`PascalCase`**
- Private Fields: **`camelCase`** or **`_camelCase`**
- Local Variables & Parameters: **`camelCase`**

### 2.2 TextMeshPro Exclusivity
- ❌ Do not use legacy `UnityEngine.UI.Text`.
- ✅ Always use `TMPro.TextMeshProUGUI`.
- Set explicit vertical and horizontal alignment; truncate overflowing text with ellipsis where appropriate.

### 2.3 New Input System Standard
- ❌ Avoid legacy `Input.GetKeyDown(KeyCode.Escape)`.
- ✅ Use the New Input System API:
  ```csharp
  if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
  {
      ClosePanel();
  }
  ```

### 2.4 Defensive Dependency Resolution in `Awake()`
Do not rely solely on Inspector assignments for components residing on the same GameObject:
```csharp
private void Awake()
{
    if (rb == null) rb = GetComponent<Rigidbody>();
    if (col == null) col = GetComponent<Collider>();
}
```

---

## 3. Strict Anti-Patterns (PROHIBITED)

1. ❌ **Direct Camera State Mutating**: Never call `camera.enabled` or change camera transforms outside `CameraManager.Instance.SetMode()`.
2. ❌ **Manual Input Locking**: Never toggle `PlayerControl.isInputLocked` manually from feature scripts; `CameraManager` owns this state.
3. ❌ **UI Polling**: Never poll backend inventories or station timers inside UI `Update()` methods; rely on events.
4. ❌ **Event Leaks**: Never subscribe to events without unsubscribing in `OnDisable()`.
5. ❌ **Hardcoded Layer Indices**: Never hardcode integers for physics layers; use `LayerMask.NameToLayer("LayerName")` or `LayerMask.GetMask("LayerName")`.
6. ❌ **Disabling Cameras via `SetActive(false)`**: Disable the `Camera` component instead (`camera.enabled = false`) to avoid AudioListener conflicts and hierarchy churn.
7. ❌ **Blind Editor Automation Scripts**: Never create ad-hoc `Assets/Editor/*Setup*.cs` menu scripts that blindly alter scene hierarchy. Use targeted MCP commands.
