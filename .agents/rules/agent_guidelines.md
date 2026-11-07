# Agent Guidelines & Operational SOP

Operating guidelines and Git rules for autonomous AI agents in **Farm-Beware**.

---

## 1. System Context

- **Engine**: Unity 6000.3.20f1 (Unity 6), URP Deferred+
- **Active Branch**: `Rafi-branch` (Layer 3 Programmer Branch)
- **Tooling**: Unity MCP (`com.coplaydev.unity-mcp`). Hierarchy inspection, Roslyn C# (`execute_code`), console logs (`read_console`), compilation (`refresh_unity`).
- **Standard**: Senior Engineer standard. Strict OOP, modularity, zero-leak events, zero-GC combat loops.

---

## 2. Agent Operational Laws

### 2.1 Rule Character Limits (Hard Constraint)
- All rule files under `.agents/rules/*.md` MUST strictly remain under **4,000 characters** (`MAX_CONTENT_LENGTH = 4,000`).
- Any frontmatter description MUST stay under **250 characters**.
- Keep rule content dense, technical, and free of redundant boilerplate.

### 2.2 Token Discipline & Links
- Dense technical responses without conversational filler.
- Reference files with clickable markdown links: `[File.cs](file:///path/to/File.cs#L10)`.
- Apply single-concern edits: avoid unrequested drive-by refactorings.

### 2.3 Prohibited Editor Setup Scripts
- ❌ **NEVER** write temporary editor setup scripts (`Assets/Editor/*Setup*.cs` or `[MenuItem]`).
- ✅ **Use Direct MCP Tools**: Mutate scenes exclusively via MCP tools (`execute_code`, `manage_scene`, `manage_gameobject`, `manage_components`).

### 2.4 Mandatory Verification Suite (86/86 Tests)
- **Compilation Check**: Check console via `read_console` for 0 compilation errors and 0 warnings.
- **Automated Test Suite**: Run `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`. All 86 audit tests must pass (86/86).
- **Auto-Healing**: Immediately fix any error, assertion, or test failure autonomously.

### 2.5 Pure Logic vs. Thin Adapter Separation
- Decouple pure computation from `MonoBehaviour`.
- Math, crop formulas, combat FSM, and poise tracking reside in POCO C# classes with zero Unity lifecycle dependencies and zero heap allocations.
- `MonoBehaviour` scripts serve strictly as **Thin Adapters** for input, frame ticking (`deltaTime`), and Unity APIs.

### 2.6 Native Assertion Safety (Zero Unsafe SendMessage)
- ❌ **NEVER** call raw `SendMessage` on inactive objects or during EditMode tests to prevent `Assertion failed on expression: 'ShouldRunBehaviour()'`.
- ✅ **Use Safe Invocation**: Use direct type-safe methods or reflection (`MethodInfo.Invoke`).

### 2.7 100% English Mandate
- ❌ **PROHIBITED**: Non-English identifiers, comments, logs, Inspector attributes (`Header`, `Tooltip`), or UI text.
- ✅ **ALL English Everywhere**: Code identifiers, Inspector attributes, UI text, logs, ScriptableObjects, and technical docs.

### 2.8 Anti-Regression Invariant & Blast Radius
- **Pre-Flight Blast Radius Audit**: Inspect all references before modifying existing methods or types. Never alter or delete public method signatures consumed by existing modules.
- **Zero Collateral Damage**: Adding/modifying a feature must never break existing systems. Shared states (Camera, Input, Modals, Time) must guarantee clean restoration on exit or cancellation.
- **Clean Diff Gate**: Check `git status` before commit; never stage unintended scene or prefab edits.

---

## 3. Git Workflow & 3-Layer Repository SOP

1. **Layer 1 (Tech Lead)**: `main` (Production) & `staging` (Testing)
2. **Layer 2 (Lead Dev)**: `development` / `dev` (Primary Integration)
3. **Layer 3 (Programmer/Agent)**: `<programmer_name>` (e.g., `Rafi-branch`). **Exclusive agent workspace.**

### Branching Constraints
- ❌ Never create new branches (`git checkout -b feature/...`).
- ❌ Never push directly to `main`, `staging`, or `development`.
- Target only Layer 3 branches (`origin/<programmer_name>`).
- Commit messages must be atomic: `<type> : <description>` (`feat`, `progress`, `fix`, `refactor`, `chore`, `assets`).
