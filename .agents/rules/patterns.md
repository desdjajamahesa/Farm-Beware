# Patterns.md

Coding standards, recurring design patterns, and strict anti-patterns for **Farm-Beware**.

---

## 1. Recurring Architectural Patterns

### 1.1 Pure Logic (POCO) vs Thin Engine Adapters
- Decouple all state machines and math into plain C# objects (`CombatStateEvaluator`, `PoiseTracker`, `AttackTokenDispatcher`, `DefenseEvaluator`).
- Zero Unity API calls in POCO logic; 0 byte heap allocations in runtime combat loops (Zero-GC).
- `MonoBehaviour` scripts serve exclusively as thin adapters (pumping `Tick(deltaTime)`, inputs, Unity events).

### 1.2 Central Camera Delegation
- Subsystems never manipulate camera components or transforms directly.
- Transitions delegate through `CameraManager.Instance.SetMode(CameraMode.Gameplay | TrophyMode | WardrobeMode)`.
- Camera controllers check `CurrentMode == CameraMode.Gameplay` before updating follow loops.

### 1.3 Event-Driven Stateless UI
- Backend systems own state and broadcast C# events.
- UI managers subscribe in `OnEnable()`, unsubscribe in `OnDisable()`, and re-render without duplicate state.
- UI components must never mutate inventory directly or run backend countdown coroutines.

### 1.4 Safe Station Transactions & Rollbacks
- Stations consuming resources over time (`KitchenStove`) snapshot consumed items/liquids at start.
- Cancellation performs atomic rollback (`CancelCooking(refundIngredients: true)`), restoring items and water. Output is granted only on timer completion.

### 1.5 Combat Input Buffering & Interruption
- Cache attack inputs pressed during active animations (`bufferedAttackTime <= 0.45f`).
- Consume buffer at combo branch windows (`normalizedTime >= minLock`, `ComboResetWindow = 1.5f`).
- Interrupt active combos on dodge or parry via `InterruptCombatSequence()` to reset timestamps (`-999f`) for immediate responsiveness.

### 1.6 MaterialPropertyBlock & Zero-GC Pooling
- Never call `renderer.material` for dynamic visuals. Use `MaterialPropertyBlock` to preserve GPU Resident Drawer (BRG) instancing.
- Pre-allocate pools for floating UI and combat entities; zero runtime instantiation during combat.

### 1.7 Max Stack Invariant (Ceiling = 20)
- Stackable items (`ItemData`) enforce `maxStack = 20` clamped via `[Range(1, 20)]`.
- Tools, weapons, armor, and trophies enforce `maxStack = 1`.

---

## 2. Coding Standards

- **Namespaces**: Modular `Features<Module>` (e.g., `FeaturesCombat`, `FeaturesCamera`).
- **Naming**: `PascalCase` for classes/methods/enums; `camelCase` or `_camelCase` for fields.
- **Components**: Always use `TMPro.TextMeshProUGUI`. Use New Input System (`UnityEngine.InputSystem`).
- **100% English Mandate**: All identifiers, comments, logs, Inspector attributes (`Header`, `Tooltip`), and UI labels must be written exclusively in English.

---

## 3. Strict Anti-Patterns (PROHIBITED)

1. ❌ **Direct Camera Mutation**: Never alter camera transforms outside `CameraManager`.
2. ❌ **Manual Input Locking**: Never toggle `PlayerControl.isInputLocked` manually; `CameraManager` owns this state.
3. ❌ **UI Polling**: Never poll backend states inside UI `Update()`; rely on events.
4. ❌ **Event Leaks**: Never subscribe to events without unsubscribing in `OnDisable()`.
5. ❌ **Temporary Setup Scripts**: Never create ad-hoc `Assets/Editor/*Setup*.cs` scripts. Use targeted MCP tools.
6. ❌ **Transparent Buildings**: Always use Bayer 4x4 Dithered Alpha Clipping (`RenderType = Opaque`, `ZWrite On`) to preserve depth prepass and shadows.
7. ❌ **`UniversalForward` in Deferred+**: Custom opaque forward passes must use `Tags { "LightMode" = "UniversalForwardOnly" }`.
8. ❌ **Static Batching in BRG**: Never enable Static Batching with GPU Resident Drawer (`InstancedDrawing`).
9. ❌ **Secondary Camera Post-Processing**: Mirror/auxiliary cameras must set `renderPostProcessing = false`.
10. ❌ **Dangling References in Pools**: Always unregister despawned enemies from `HitstopCoordinator` and `AttackTokenDispatcher` in `OnDisable()`.
