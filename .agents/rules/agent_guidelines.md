# Agent Guidelines & Operational SOP

Operating guidelines and Git rules for autonomous AI agents in **Farm-Beware**.

---

## 1. System Context

- **Engine**: Unity `6000.3.20f1` (Unity 6), URP Deferred+
- **Active Branch**: `Rafi-branch` (Layer 3 Programmer Branch)
- **Tooling**: Unity MCP (`com.coplaydev.unity-mcp`) for `refresh_unity`, `execute_code`, `read_console`, `run_tests`, `manage_gameobject`.
- **Standard**: Principal Engineer standard. Modular OOP, zero-leak events, zero-GC combat/map loops.

---

## 2. Agent Operational Laws

### 2.1 Rule Character Limits (Hard Constraint)
- All rule files under `.agents/rules/*.md` MUST strictly remain under **4,000 characters** (`MAX_CONTENT_LENGTH = 4,000`).
- Any frontmatter description MUST stay under **250 characters**.

### 2.2 Token Discipline & Links
- Dense technical responses without conversational filler.
- Reference files with clickable links: `[File.cs](file:///path/to/File.cs#L10)`.
- Apply single-concern edits; avoid unrequested drive-by refactorings.

### 2.3 Prohibited Editor Setup Scripts
- **NEVER** create disposable setup scripts (`Assets/Editor/*Setup*.cs` or temporary `[MenuItem]`).
- **Use Direct MCP Tools**: Mutate scenes via `execute_code`, `manage_scene`, `manage_gameobject`, `manage_components`.

### 2.4 Mandatory Verification Suite (117/117 Tests)
- **Compilation Check**: Check console via `read_console` for 0 compilation errors and 0 script warnings.
- **Automated Test Suite**: Verify **117/117 Passed** on BOTH:
  - `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`
  - Unity Test Runner `EditMode` (`run_tests(mode: "EditMode", assembly_names: ["FarmBeware.Editor.Tests"])`).
- **Auto-Healing**: Fix any compilation error, warning, or test failure immediately.

### 2.5 Pure Logic vs. Thin Adapter Separation
- Decouple computation from `MonoBehaviour` into Zero-GC pure C# POCOs; keep `MonoBehaviour` classes as thin adapters.

### 2.6 Contract & Exception Safety (Zero SendMessage / Zero Empty Catch)
- **NEVER** call runtime `SendMessage` or write empty `catch {}` blocks.
- **Use Typed Contracts**: Communicate across modules via interfaces in `Assets/Scripts/Core/Contracts/` resolved cleanly via `ServiceLocator` or component interface queries.

### 2.7 100% English Mandate
- **ALL English Everywhere**: Identifiers, code comments, `[Header]`/`[Tooltip]` attributes, logs, UI text, and technical docs (except `CHANGELOG-RAFI`/`TECH`/`BHASKORO` historical notes if retained).

### 2.8 Anti-Regression Invariant & Blast Radius
- Preserve existing public API signatures (use overloads/optional params if extending).
- Restore shared state (`isInputLocked`, `timeScale`, camera priority, modal stack) deterministically on cancel or teardown.
- Check `git status` before commit; never stage unintended `.unity` or `.prefab` edits.

---

## 3. Git Workflow & 3-Layer Repository SOP

1. **Layer 1 (Tech Lead)**: `main` (Production) & `staging` (Testing)
2. **Layer 2 (Lead Dev)**: `development` / `dev` (Primary Integration)
3. **Layer 3 (Programmer/Agent)**: `<programmer_name>` (e.g., `Rafi-branch`). **Exclusive agent workspace.**

### Branching Constraints
- Never create ad-hoc feature branches (`git checkout -b ...`) or push directly to `main`, `staging`, or `development`.
- Use atomic commits: `<type>(<scope>): <description>` (`feat`, `fix`, `refactor`, `chore`, `test`, `assets`, `docs`).
