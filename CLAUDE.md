# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 0. IDENTITY & CONTEXT
- You are an autonomous coding agent running via Claude Code CLI.
- Backend = OmniRoute free-tier proxy. Token budget is SCARCE. Every emitted token is a cost.
- Project: **Farm-Beware** — Unity 3D Isometric farming game.
- Bridge: Unity MCP. You CAN and MUST observe scene hierarchy, execute/validate scripts, and pull Editor console logs yourself.
- The operator is a **Senior Engineer** with strong OOP + data-structure fundamentals (including array-rendering bugs). Never explain basics. Never lecture. Talk like a peer reviewer, not a tutor.

## 1. TOKEN DISCIPLINE (HARD RULES)
- Max ~15 lines of prose per reply unless explicitly asked for detail.
- NO preamble, NO postamble, NO apologies, NO restating the task, NO summary of work already shown.
- Prefer diffs/patches over quoting whole files. Reference code as `path/File.cs:LINE` instead of pasting it.
- Ask questions ONLY when ambiguity changes implementation direction. Bundle into ONE question, max.
- One concern per edit. No drive-by refactors. No unsolicited comments in code.

## 2. EXECUTION LOOP (THINK → FIRE)
1. Think briefly inside exactly one `<thought>` tag: intent + target tool/file only (≤ 3 sentences).
2. Immediately trigger the CLI/bash command or MCP tool call. NEVER wait for permission on reversible actions (reads, edits, compile checks).
3. Batch independent tool calls in parallel.
4. Verify (compile status / console logs) BEFORE reporting done. Report format: `DONE` or `BLOCKED: <root cause> <one-line next step>`.

## 2.5 NO EDITOR SETUP SCRIPTS (HARD RULE, USER-MANDATED)
- ❌ NEVER create or run Editor automation scripts (`Assets/Editor/*Setup*.cs`, `MenuItem("Farm Beware/...")`) to wire/fix scenes.
- These scripts are unreliable: they mutate scene state blindly (e.g., added `IsometricCameraController` next to working `IsometricCamera` → broke gameplay camera).
- ✅ ALL scene inspection and modification via MCP tools only: `manage_scene`, `manage_gameobject`, `manage_components`, `manage_prefabs`, `execute_code`.
- Workflow: read-only audit first (see `SCENE_AUDIT.md` for the format) → minimal targeted change → verify.

## 3. SELF-HEALING PROTOCOL (MCP-FIRST, ASK-NEVER)
- NEVER ask the user to paste errors, screenshots, or scene descriptions. Observe directly via MCP:
  - Scene state → `manage_scene get_hierarchy` (paged, page_size ≤ 50).
  - Errors/warnings → `read_console` filtered to Error+Warning.
  - Component data → paged component reads, `include_properties=false` by default.
- Failure loop: pull console → locate root cause → minimal patch → refresh/recompile → re-pull console.
  Repeat max 5 iterations; if still failing, report `BLOCKED:` with root cause hypothesis only.
- After ANY script create/edit: check compilation status (`editor/state`, console) before using the new type.

## 4. ARCHITECTURE LAW (NON-NEGOTIABLE)
- STRICT SEPARATION of pure logic from MonoBehaviour. Pure logic MUST live in plain C# classes (static/POCO), zero Unity lifecycle dependencies:
  - Isometric grid math (world↔grid conversion, tile indexing, neighbor lookups)
  - Array/collection manipulation and render-order logic (z-order/sorting computation)
  - Inventory, economy, save-data serialization
- MonoBehaviours are THIN ADAPTERS ONLY: input capture, Update/coroutine pumping, Unity API calls. They delegate; they never compute.
- Pure logic classes must be testable without Play Mode.
- File layout: pure logic → `Assets/Scripts/Logic/*.cs`; adapters → `Assets/Scripts/Mono/*.cs`.
- When fixing rendering/array bugs: fix the pure logic class first, adapter second. Never inline math into Mono hooks.

## 5. FILE & PAYLOAD HYGIENE (UNITY CACHE)
- NEVER read, list, grep, glob, or index these paths:
  `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, `.vs/`
  and file patterns: `*.csproj`, `*.sln`, `*.unityproj`, `*.pidb`, `*.user`, `*.booproj`
- NEVER dump full scene/prefab/asset YAML into context. Query structure via MCP reads instead (paged, summary-first).
- `.meta` files: touch only when a GUID change is explicitly required; never paste contents.
- Edits: targeted Edit over full-file rewrite, always.

## 6. SIDE-EFFECT GUARDS
- No git commits, no config/package changes, no builds unless explicitly ordered.
- Reversible actions (read/edit/compile/test) require no confirmation. Irreversible ones require one-line confirmation.

---

## PROJECT ARCHITECTURE

### Unity Version
Unity 6000.3.20f1 (2023 LTS equivalent)

### Core Systems (Singleton Managers)

#### CameraManager (`Features/Camera/CameraManager.cs`) **NEW**
- **Centralized camera state machine**: Single source of truth for all camera modes
- **Camera modes**: `Gameplay`, `TrophyMode`, `WardrobeMode`
- **Responsibilities**:
  - Manages all camera enable/disable lifecycle
  - Positions Trophy and Wardrobe cameras (local to their roots)
  - Coordinates input locking via `PlayerControl.isInputLocked`
  - Controls cursor state (locked/visible) per mode
  - Validates state transitions (prevents Trophy ↔ Wardrobe direct transitions)
- **Events**: `OnCameraModeChanged(CameraMode)` for feature managers
- **Public API**:
  - `SetMode(CameraMode, Transform contextRoot)` - switches camera mode
  - `PositionPlayerBehindTrophyCamera()` - teleports player behind trophy camera

#### TimeManager (`Features/Time/TimeManager.cs`)
- Singleton managing Day/Night phase transitions (static phases, not real-time clock).
- Events: `OnDayChanged`, `OnPhaseChanged`.
- Commands: `AdvanceToNextDay()`, `SkipToNight()`.
- DEBUG: `N` key skips to night phase for bed testing.

#### TrophySystemManager (`Features/Trophy/TrophySystemManager.cs`)
- Singleton managing trophy arrangement logic (NO camera control - delegates to CameraManager).
- Dual-inventory: `CabinetInventory` (storage) + `RackInventory` (visual source-of-truth for 3D-placed trophies).
- Raycast on SnapPoint layer (layer 10) to collect trophies back to cabinet.
- ESC exits trophy mode via `CameraManager.SetMode(Gameplay)`.

### Feature-Based Structure
Code organized under `Assets/Scripts/Features/<FeatureName>/`:
- **Interaction**: IInteractable interface, PlayerInteractor, Highlightable, InteractionZone, specific interactables (Bed, Storage, TrophyCabinet, Wardrobe, GenericFurniture).
- **Inventory**: InventoryComponent (core slot/stack logic), InventorySlot, ItemData, UI (drag/drop, slot rendering).
- **Kitchen**: KitchenStation (abstract base), StoveInteractable, KitchenSinkInteractable, RefrigeratorInteractable, KitchenRecipe, UI overlays (progress, station panels).
- **Trophy**: TrophySystemManager, TrophyItem, TrophySnapPoint, TrophyRackVisuals, TrophyCabinetInteractable.
- **Wardrobe**: WardrobeManager, PlayerOutfit, OutfitData, WardrobeUI, MirrorCamera.
- **Time**: TimeManager, DayTransitionUI.
- **Camera**: WallOcclusionManager, WallOccluder (fade walls blocking player view).
- **Common**: FadeManager.

### Player System
- **PlayerControl** (`Player/PlayerControl.cs`): Rigidbody-based movement with New Input System. Handles WASD movement, jump, dash, hotbar selection (1-4, scroll), inventory toggle (Tab/I). `isInputLocked` flag disables all input (set by CameraManager).
- **PlayerController** (`PlayerController.cs`): **DEPRECATED** - Legacy capsule-based controller. Replaced by unified IsometricCameraController.
- **PlayerInteractor** (`Features/Interaction/PlayerInteractor.cs`): Interaction raycast handler. Queries IInteractable on E press.
- **PlayerStats** (`Player/PlayerStats.cs`): Health/stamina.
- **PlayerEquipment** (`Player/PlayerEquipment.cs`): Hotbar item 3D model spawning/parenting.
- **InventoryComponent**: Player inventory with 4-slot hotbar, trophy-blocking flag, food-category filtering.

### Inventory System Architecture
**InventoryComponent** is the core backend (not UI):
- **Slot-based**: List of `InventorySlot` (item + quantity).
- **Stack logic**: `AddItem()`, `RemoveItem()`, `TransferItemTo()`, `MoveItemToSlot()` (slot-precise drag/drop with stacking/swapping).
- **Rule enforcement**: `CanAcceptItem()` checks `blockTrophyItems` flag and `allowedFoodCategories` list (used by Refrigerator, Sink).
- **Events**: `OnInventoryChanged`, `OnHotbarSelected` (UI listens, backend triggers).
- **Hotbar**: First 4 slots, `selectedHotbarIndex`, scroll/number-key selection.

### Kitchen System (Data-Driven Processing)
**KitchenStation** abstract base (`Features/Kitchen/KitchenStation.cs`):
- State machine per slot: idle → processing → complete.
- Auto-starts when item lands in slot (via `OnInventoryChanged` listener).
- Per-slot timers (clamped deltaTime to prevent editor-pause exploits).
- Events: `OnProcessStarted`, `OnProcessProgress`, `OnProcessCompleted`, `OnProcessCancelled`.
- UI overlay polls `GetSlotProgress(slot)` for read-only state (event-independent).
- Subclasses: `StoveInteractable` (cooking), `KitchenSinkInteractable` (washing), `RefrigeratorInteractable` (storage with category filter).

### Interaction Protocol
1. **IInteractable** interface: `Interact(PlayerControl)`, `GetInteractionPrompt()`.
2. **PlayerInteractor**: Sphere overlap on Interactable layer → highlights closest IInteractable → E key triggers `Interact()`.
3. **Highlightable**: Outline shader via material swap on highlight enter/exit.
4. **InteractionZone**: Trigger collider wrapper for furniture without direct interaction logic.

### Input System
- New Input System (`UnityEngine.InputSystem`) throughout.
- **PlayerInputActions** asset defines action maps.
- Keyboard fallback + legacy Input Manager dual support in some scripts for compatibility.

### Editor Automation Scripts
Under `Assets/Editor/`:
- **GeminiAutomation.cs**: AI-assisted scene setup.
- **KitchenSetup.cs**, **TrophySystemWiring.cs**, **WardrobeSetup.cs**: Automated prefab wiring/injection.
- **TrophyAssetFactory.cs**: Trophy item ScriptableObject generator.
- **VisualPolishSetup.cs**, **HoverLabelSetup.cs**: Batch visual component injection.

### Camera System
- **CameraManager** (`Features/Camera/CameraManager.cs`): Centralized singleton managing all camera modes. State machine with validation. Positions Trophy/Wardrobe cameras. Coordinates input locking and cursor state.
- **IsometricCameraController** (`Features/Camera/IsometricCameraController.cs`): Unified gameplay camera component (merges old IsometricCamera + CameraController). Features: smooth follow, orbit (right-drag), zoom (scroll), orthographic projection. Guard: only runs when `CameraManager.CurrentMode == Gameplay`.
- **Trophy FP Camera**: Positioned locally to `TrophyCabinetSystem` root by CameraManager on Trophy mode enter.
- **Wardrobe Camera**: Positioned locally to `WardrobeRoot` by CameraManager on Wardrobe mode enter.
- **MirrorCamera**: RenderTexture camera (always renders to `mirrorTexture`, never screen). Used in gameplay (mirror surface) and wardrobe mode (UI RawImage). Not controlled by CameraManager.
- **WallOcclusionManager**: Raycasts between camera and player; fades walls in-between via `WallOccluder` component.

### Wardrobe System (NEW — Per-Part Outfit + Live 3D Preview)
- **WardrobeItemData** (`Logic/WardrobeItemData.cs`): ScriptableObject for individual items (ID, Name, Icon, Category, 3D Prefab).
- **ItemSlot** (`Features/Wardrobe/UI/ItemSlot.cs`): UI slot prefab with icon, selection highlight, click handling.
- **PreviewController** (`Features/Wardrobe/PreviewController.cs`): Off-screen camera + RenderTexture for live 3D avatar preview. Drag-to-rotate, dynamic mesh swap on item select.
- **WardrobeUI** (`Features/Wardrobe/UI/WardrobeUI.cs`): 4-panel layout — Left (Category tabs), Center-Left (Item Grid), Center-Right (RawImage preview), Right (Save/Exit).
- **WardrobeManager** (`Features/Wardrobe/WardrobeManager.cs`): Integrates UI, populates items from `WardrobeItemData` assets or `PlayerOutfit.unlockedOutfits`, binds preview.
- **OutfitPartResolver** (`Logic/OutfitPartResolver.cs`): Pure logic for category/variant ↔ renderer name mapping (zero UnityEngine deps).
- **PlayerOutfit** (`Features/Wardrobe/PlayerOutfit.cs`): Thin adapter — applies `OutfitData` to character SkinnedMeshRenderers via `OutfitData.ApplyToCharacter()`.

---

## COMMON COMMANDS

### Unity MCP Tools (via Unity Bridge)
- **Scene inspection**: `manage_scene get_hierarchy --page_size 50`
- **Console logs**: `read_console --filter Error,Warning`
- **Compilation check**: `editor/state` (check `isCompiling`, `hasCompileErrors`).
- **Component data**: Query GameObject components with paging (set `include_properties=false` for summary).

### Git Workflow
- **Current branch**: `scene-integration` (staging ongoing scene work).
- **Main branch**: `main`.
- Always create feature branches off `main`; merge via PR.

### Build & Test
- Unity Editor only (no CLI build commands configured yet).
- Play Mode testing required for Rigidbody/physics/input systems.
- Visual testing: Trophy mode (ESC to exit), Kitchen station progress overlays, Wall occlusion fade.

---

## CRITICAL PATTERNS

### Critical Patterns

#### Camera State Machine (CameraManager)
```csharp
// Feature managers delegate ALL camera control to CameraManager
CameraManager.Instance.SetMode(CameraManager.CameraMode.TrophyMode, trophySystemRoot);

// CameraManager handles:
// - Camera enable/disable
// - Camera positioning (Trophy/Wardrobe local to roots)
// - Input locking (PlayerControl.isInputLocked)
// - Cursor state (locked/visible)
```

**Valid Transitions**:
- `Gameplay` → `TrophyMode` ✓
- `Gameplay` → `WardrobeMode` ✓
- `TrophyMode` → `Gameplay` ✓
- `WardrobeMode` → `Gameplay` ✓
- `TrophyMode` → `WardrobeMode` ✗ (must return to Gameplay first)
- `WardrobeMode` → `TrophyMode` ✗ (must return to Gameplay first)

#### IsometricCameraController Mode Guard
```csharp
void LateUpdate()
{
    // Guard: Only run in Gameplay mode
    if (CameraManager.Instance != null && 
        CameraManager.Instance.CurrentMode != CameraManager.CameraMode.Gameplay)
        return;
    
    // ... camera follow/orbit/zoom logic
}
```

### Singleton Pattern (Lazy Resolver)
Used by TimeManager, TrophySystemManager:
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
Fallback resolver ensures Instance works even if Awake hasn't run yet (edit mode / lifecycle edge cases).

### Event-Driven UI Updates
- Backend (InventoryComponent, KitchenStation) fires events: `OnInventoryChanged`, `OnProcessProgress`.
- UI subscribes in `OnEnable`, unsubscribes in `OnDisable`.
- UI is stateless renderer; backend owns all data/timers.

### Input Locking (Global State)
`PlayerControl.isInputLocked` flag set by CameraManager:
- `true` in Trophy/Wardrobe modes → blocks movement, jump, dash, inventory toggle, interaction
- `false` in Gameplay mode → normal input processing
- **DO NOT manually set this flag** — CameraManager owns it

### Trophy Dual-Inventory Pattern
- **CabinetInventory**: Storage backend (item list).
- **RackInventory**: Visual source-of-truth (each slot = one 3D-placed trophy on SnapPoint).
- Drag from Cabinet → Rack spawns 3D model at SnapPoint.
- Click on 3D trophy (raycast layer 10) → `TransferItemTo(CabinetInventory, slotIndex)` → model destroyed via `OnInventoryChanged` listener.

### Item State Transformation (Kitchen)
Recipes define input → output (e.g., `DirtyCarrot` → `CleanCarrot`).
- KitchenStation tracks per-slot recipe + timer.
- On complete: `RemoveFromSlot(input)` + `ReplaceItemAt(slot, output)`.
- Batch processing: processes entire slot quantity at once.

---

## DEBUGGING WORKFLOW

1. **Compilation errors**: Pull console via MCP (`read_console`), fix, verify compile status.
2. **Runtime errors**: Check console in Play Mode. Common: null reference (missing Inspector wiring), layer mismatch (Interactable layer not set), event listener leaks (forgot OnDisable unsub).
3. **Camera mode stuck**: Check `CameraManager.CurrentMode` in console. ESC should return to Gameplay from Trophy/Wardrobe. If stuck, verify CameraManager exists in scene.
4. **Wardrobe camera wrong position (THE BUG)**: FIXED by CameraManager. Wardrobe camera now positioned by CameraManager, not overridden by old IsometricCamera/CameraController.
5. **Trophy mode stuck**: ESC exits via `CameraManager.SetMode(Gameplay)`. If camera not switching: check CameraManager has trophy camera reference.
6. **Kitchen station not processing**: Check `stationInventory` assigned in Inspector, item has valid KitchenRecipe, `OnInventoryChanged` listener registered.
7. **Wall occlusion not working**: Ensure walls have `WallOccluder` component, WallOcclusionManager.raycastMask includes wall layer.

---

## ANTI-PATTERNS (DO NOT)

- ❌ **Direct camera enable/disable in feature managers** → Use `CameraManager.SetMode()`.
- ❌ **Manual input locking in feature managers** → CameraManager handles it.
- ❌ **Camera positioning logic in feature managers** → CameraManager owns all positioning.
- ❌ **Inline math in MonoBehaviour Update loops** → Extract to static utility class.
- ❌ **UI polling backend every frame** → Use events (`OnInventoryChanged`, `OnProcessProgress`).
- ❌ **Hardcoded layer indices** → Use `LayerMask.GetMask("LayerName")`.
- ❌ **SetActive(false) for camera switching** → Use `camera.enabled = false` (AudioListener conflict).
- ❌ **AddItem() without CanAcceptItem() pre-check** → Rule enforcement happens IN AddItem/MoveItemToSlot.
- ❌ **Event subscription without OnDisable cleanup** → Memory leaks + double-invocation bugs.
- ❌ **Prefab edits without Editor automation scripts** → Use TrophySystemWiring.cs, KitchenSetup.cs, etc.
