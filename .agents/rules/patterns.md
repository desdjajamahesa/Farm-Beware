# Patterns.md

Coding standards, recurring design patterns, and strict anti-patterns for **Farm-Beware**.

---

## 1. Recurring Architectural Patterns

### 1.1 Pure Logic (POCO) vs Thin Engine Adapters
- Decouple state machines, grid math, and crop formulas into plain C# objects (`CombatStateEvaluator`, `PoiseTracker`, `AttackTokenDispatcher`, `DailyEconomyManager`).
- Zero Unity API calls in POCO logic; 0 byte heap allocations in runtime loops (Zero-GC).
- `MonoBehaviour` scripts act strictly as thin adapters (pumping `Tick(deltaTime)`, inputs, Unity events).

### 1.2 Central Camera & Modal Delegation
- Subsystems never manipulate camera transforms directly; delegate via `CameraManager`.
- Centralize input gating and modal panels via `IModalStackService` and `ModalStackManager`.

### 1.3 Safe Reflection Invocation over SendMessage
- Never call raw `SendMessage` on inactive objects or during EditMode tests to avoid `Assertion failed on expression: 'ShouldRunBehaviour()'`. Use type-safe methods or reflection (`MethodInfo.Invoke`).

### 1.4 Zero-Snag Road & Yard Invariant
- Mesh colliders on decorative road ribbons, gravel shoulders, and courtyard aprons must be disabled.
- Vehicles (`ArcadeVehicleController`) and player ride smoothly on native `TerrainCollider`.

### 1.5 Isometric Depth Staging
- SW to NE camera (`pitch: 30°`, `yaw: 45°`). Background structures at $+Z$; interaction zones face South ($-Z$).

### 1.6 Safe Station Transactions & Rollbacks
- Stations consuming items (`KitchenStove`) snapshot inputs at start. Cancellation performs atomic rollback (`refundIngredients: true`). Output granted only on completion.

### 1.7 Max Stack Invariant (Ceiling = 20)
- Stackable items (`ItemData`) enforce `maxStack = 20` clamped via `[Range(1, 20)]`. Tools/weapons enforce `maxStack = 1`.

### 1.8 Anti-Regression API Contracts & Decoupling
- **Preserve Public APIs**: Never alter or delete public method/property signatures used by existing modules. Use overloads or optional params.
- **Subsystem Decoupling**: Inter-feature communication must use domain events (`Action<T>`). Never directly invoke mutating methods on another feature's singleton.
- **Scoped State & Token Safety**: Never flip flat boolean flags (`isInputLocked`, `timeScale`) without guaranteed cleanup in `try...finally` or scoped token services.

---

## 2. Coding Standards

- **Namespaces**: Modular `Features<Module>` (e.g., `FeaturesCombat`, `FeaturesEconomy`, `FeaturesStoryline`).
- **Naming**: `PascalCase` for classes/methods/enums; `camelCase` or `_camelCase` for fields.
- **100% English Mandate**: All identifiers, comments, logs, Inspector attributes, and UI text must be in English.

---

## 3. Strict Anti-Patterns (PROHIBITED)

1. ❌ **Direct Camera Mutation**: Never alter camera transforms outside `CameraManager`.
2. ❌ **Manual Input Locking**: Never toggle `PlayerControl.isInputLocked` manually; use `CameraManager` or `ModalStackManager`.
3. ❌ **Unsafe SendMessage**: Never call `SendMessage` without verifying `Application.isPlaying && activeInHierarchy`.
4. ❌ **Road Collider Snags**: Never add active colliders to road ribbons, shoulders, or yard aprons.
5. ❌ **UI Polling**: Never poll backend states inside UI `Update()`; rely on events.
6. ❌ **Event Leaks**: Never subscribe to events without unsubscribing in `OnDisable()` / `OnDestroy()`.
7. ❌ **Temporary Setup Scripts**: Never create ad-hoc `Assets/Editor/*Setup*.cs` scripts. Use MCP tools.
8. ❌ **Breaking Public Contracts**: Never delete/modify existing method signatures consumed by legacy features.
9. ❌ **Silent Inspector Breakage**: Never rename serialized fields without `[FormerlySerializedAs("oldName")]`.
10. ❌ **Unscoped State Leaks**: Never mutate global state without exit cleanup (causes ghost bugs in old features).
