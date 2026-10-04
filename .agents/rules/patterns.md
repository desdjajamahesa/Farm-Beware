# Patterns.md

Coding standards, recurring design patterns, and strict anti-patterns for the **Farm-Beware** project.

---

## 1. Recurring Architectural Patterns

### 1.1 Awake-Safe Singleton with Fallback Resolver
Used across primary managers (`CameraManager`, `DayNightTimeManager`, `SaveLoadService`):
```csharp
private static T _instance;
public static T Instance
{
    get
    {
        if (_instance == null)
        {
            T[] found = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found != null && found.Length > 0) _instance = found[0];
        }
        return _instance;
    }
    private set => _instance = value;
}
```

### 1.2 Central Camera Delegation Pattern
Feature managers are strictly forbidden from directly enabling, disabling, or transforming cameras. All transitions delegate through `CameraManager`:
```csharp
CameraManager.Instance.SetMode(CameraMode.WardrobeMode, wardrobeRootTransform);
CameraManager.Instance.SetMode(CameraMode.Gameplay);
```

### 1.3 Gameplay Camera Mode Guard
Camera controllers must include a guard condition at the start of their update loop:
```csharp
void LateUpdate()
{
    if (CameraManager.Instance != null && CameraManager.Instance.CurrentMode != CameraMode.Gameplay)
        return;
    // Follow, orbit, and zoom execution
}
```

### 1.4 Event-Driven UI (Stateless Renderers)
Backend components own state and emit events. UI scripts listen to events and re-render without maintaining duplicate state:
```csharp
private void OnEnable() => targetInventory.OnInventoryChanged += RefreshUI;
private void OnDisable() => targetInventory.OnInventoryChanged -= RefreshUI;
```

### 1.5 Decoupled Backend Controller & View/Presenter
Never mix state management, countdown routines, or inventory mutations inside UI managers:
- **Backend Controller (`KitchenStove`)**: Owns state machine (`CookingState`), runs timers, executes transactions, and broadcasts C# events (`OnCookingStateChanged`, `OnCookingProgress`).
- **View / Presenter (`StoveUIManager`)**: Subscribes to events, updates UI, forwards user actions, and unsubscribes cleanly on close.

### 1.6 Safe Transaction & Rollback Pattern
Production stations that consume resources over time must implement atomic snapshot and rollback safety:
```csharp
// 1. Snapshot and consume immediately on start
_consumedSnapshots.Clear();
foreach (var ing in recipe.ingredients)
{
    inventory.RemoveItem(ing.item, ing.quantity);
    _consumedSnapshots.Add(new ConsumedIngredientSnapshot { item = ing.item, quantity = ing.quantity });
}
// 2. Rollback immediately if cancelled or interrupted
public void CancelCooking(bool refundIngredients = true)
{
    if (CurrentState != CookingState.Cooking) return;
    if (_cookingCoroutine != null) StopCoroutine(_cookingCoroutine);
    if (refundIngredients && _activeInventory != null)
    {
        foreach (var snap in _consumedSnapshots) _activeInventory.AddItem(snap.item, snap.quantity);
        if (_consumedWater > 0f && PlayerWaterBottle.Instance != null)
            PlayerWaterBottle.Instance.RefillWater(_consumedWater);
    }
    _consumedSnapshots.Clear();
    SetState(CookingState.Idle);
}
```

### 1.7 Combat Input Buffering & Combo State Transition
Melee attacks allow smooth input queues during active animations:
- Cache input click (`hasBufferedAttack = true`, `bufferedAttackTime = Time.time`).
- Inside animation coroutine, consume buffer when reaching combo branch window (`timer > minLock` or normalized time > 0.35).
- Use generous combo reset window (`ComboResetWindow = 1.5f`) so players can naturally complete 3-hit sequences.

### 1.8 Zero-GC Object Pooling (Combat UI & Entities)
Do not instantiate/destroy floating UI or monsters during runtime gameplay. Pre-allocate pool instances, reset state upon spawn, and return to pool on death/fadeout:
```csharp
// Example: EnemyHealthBarManager prewarms 20 EnemyOverheadBarUI instances in Awake
```

### 1.9 MaterialPropertyBlock for Dynamic Parameter Fading
Never instantiate material copies via `renderer.material` for dynamic parameters (dither fading, hit flashes). Use `MaterialPropertyBlock` to preserve GPU Resident Drawer (BRG) instancing:
```csharp
targetRenderer.GetPropertyBlock(propertyBlock);
propertyBlock.SetFloat(ShaderPropertyID, targetValue);
targetRenderer.SetPropertyBlock(propertyBlock);
```

### 1.10 Item Stack Size Invariant (Max Stack = 20)
All stackable items enforce a strict maximum ceiling of 20 units per slot:
- `ItemData.maxStack` is clamped to `[Range(1, 20)]`. Any value > 20 is clamped in `OnValidate()`.
- Equipment, weapons, and trophies have `maxStack = 1`.

### 1.11 Linked Additional Renderers & Dynamic Texture Sync
Wall-mounted accessories (mirrors, frames, lanterns) must fade synchronously with parent walls. When applying transparent materials, dynamic textures (`RenderTexture`) and UV transforms (`_BaseMap_ST`) must be explicitly synchronized.

### 1.12 Dedicated Secondary Camera Configuration
Auxiliary cameras rendering to `RenderTexture` (e.g., wardrobe mirror) must follow:
- `depth = -100` (renders before main camera passes).
- `renderType = Base` and `renderPostProcessing = false` (zero recursive post-processing).
- `AudioListener.enabled = false` (avoids duplicate listener warnings).
- `camera.enabled = false` by default; activated strictly during active interaction.

---

## 2. Coding Standards & Conventions

### 2.1 Namespaces & Naming
- Modular namespaces under `Features<ModuleName>`: `FeaturesCamera`, `FeaturesCombat`, `FeaturesInventory`, `FeaturesKitchen`, `FeaturesPersistence`, `FeaturesTime`, `FeaturesWardrobe`.
- Classes, Methods, Properties, Enums: **`PascalCase`**
- Private Fields: **`camelCase`** or **`_camelCase`**
- Local Variables & Parameters: **`camelCase`**

### 2.2 TextMeshPro Exclusivity
- ❌ Do not use legacy `UnityEngine.UI.Text`.
- ✅ Always use `TMPro.TextMeshProUGUI`.

### 2.3 New Input System Standard
- ❌ Do not use legacy `Input.GetKeyDown()`.
- ✅ Use `UnityEngine.InputSystem`: `Keyboard.current.escapeKey.wasPressedThisFrame`, `Mouse.current.leftButton`.

### 2.4 Defensive Dependency Resolution in `Awake()`
```csharp
private void Awake()
{
    if (rb == null) rb = GetComponent<Rigidbody>();
    if (col == null) col = GetComponent<Collider>();
}
```

### 2.5 100% English Mandate
All code identifiers, comments, Inspector attributes (`Header`, `Tooltip`), UI text, log messages, and ScriptableObject fields must be written exclusively in English.

---

## 3. Strict Anti-Patterns (PROHIBITED)

1. ❌ **Direct Camera State Mutating**: Never call `camera.enabled` or alter camera transforms outside `CameraManager.Instance.SetMode()`.
2. ❌ **Manual Input Locking**: Never toggle `PlayerControl.isInputLocked` manually; `CameraManager` owns this state.
3. ❌ **UI Polling**: Never poll backend inventories or station timers inside UI `Update()` methods; rely on events.
4. ❌ **Event Leaks**: Never subscribe to events without unsubscribing in `OnDisable()`.
5. ❌ **Hardcoded Layer Indices**: Never hardcode integers for physics layers; use `LayerMask.NameToLayer("LayerName")`.
6. ❌ **Disabling Cameras via `SetActive(false)`**: Disable the `Camera` component instead (`camera.enabled = false`) to avoid AudioListener conflicts.
7. ❌ **Blind Editor Automation Scripts**: Never create ad-hoc `Assets/Editor/*Setup*.cs` menu scripts that blindly alter scene hierarchy. Use targeted MCP commands.
8. ❌ **Alpha Blended Building Materials**: Never use `Transparent` render queue (`ZWrite Off`) for buildings, roofs, or walls. Always use Bayer $4 \times 4$ Dithered Alpha Clipping (`RenderType = Opaque`, `ZWrite On`) to preserve depth prepass and physical shadow maps.
9. ❌ **`UniversalForward` in Deferred+ Pipelines**: Never tag custom opaque forward shaders with `LightMode = UniversalForward` in Deferred+. Use `Tags { "LightMode" = "UniversalForwardOnly" }`.
10. ❌ **Static Batching in GPU Resident Drawer (BRG) Pipelines**: Never enable Unity Static Batching when using BRG (`gpuResidentDrawerMode: InstancedDrawing`). Static batching duplicates vertex data into CPU RAM and fractures instanced batches.
11. ❌ **UI Managers Mutating Inventories or Running Logic Timers**: UI components must never invoke inventory mutations or run backend countdown coroutines.
12. ❌ **Secondary Cameras Executing Post-Processing**: Mirror or auxiliary render texture cameras must set `renderPostProcessing = false` on `UniversalAdditionalCameraData`.
13. ❌ **Sub-Millimeter Camera Near Clip Planes**: Never set `Camera.main.nearClipPlane < 0.03f` in Deferred+. Use `0.08f` combined with proximity occlusion fading instead.
14. ❌ **One-Sided Alpha-Tested Canopy Materials**: Never leave top-down foliage materials with `_Cull = 2` (Back). Always set `_Cull = 0` (Two-Sided) to avoid black or invisible leaf artifacts.
15. ❌ **Blind Material Swapping on Dynamic Texture Surfaces**: Never swap materials on objects with dynamic textures without copying the active `RenderTexture` and `_BaseMap_ST` scale and offset.
