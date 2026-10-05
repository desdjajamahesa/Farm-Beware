# Agent Guidelines & Operational SOP

Primary operating guidelines, behavioral constraints, and Git workflow rules for autonomous AI agents in **Farm-Beware**.

---

## 1. Identity & System Context

- **Project**: Farm-Beware
- **Engine**: Unity 6000.3.20f1 (Unity 6), Universal Render Pipeline (URP Deferred+)
- **Tooling**: Unity MCP (Model Context Protocol). Inspect scene hierarchy, run in-memory Roslyn C# (`execute_code`), monitor logs (`read_console`), and compile (`refresh_unity`).
- **Standard**: Senior Engineer standard. Strict OOP principles, data-driven architecture, zero-leak event lifecycles, and zero-GC runtime combat loops.

---

## 2. Agent Operational Laws

### 2.1 Rule File Character Limits (Hard Constraint)
- All markdown rule files under `.agents/rules/*.md` MUST strictly remain under **4,000 characters** (`MAX_CONTENT_LENGTH = 4,000`).
- Any frontmatter description MUST stay under **250 characters** (`MAX_DESCRIPTION_LENGTH = 250`).
- Keep rule content dense, technical, and free of redundant boilerplate to prevent IDE Custom Rule Editor truncation.

### 2.2 Token Discipline & Code References
- Provide dense, structured technical responses without conversational filler.
- Always reference files and symbols with clickable markdown links: `[File.cs](file:///path/to/File.cs#L10)`.
- Apply single-concern edits: avoid unrequested drive-by refactorings.

### 2.3 Prohibited Editor Setup Scripts
- ❌ **NEVER** write temporary editor setup scripts (`Assets/Editor/*Setup*.cs` or `[MenuItem("...")]`).
- ✅ **Use Direct MCP Tools**: Inspect and mutate scenes exclusively via MCP tools (`execute_code`, `manage_scene`, `manage_gameobject`, `manage_components`).

### 2.4 Self-Healing Protocol (MCP-First)
- Autonomous error resolution without asking the user to paste console logs:
  1. Pull active console logs via `read_console` (filter: `error`).
  2. Identify root cause and line number.
  3. Apply targeted, minimal patches.
  4. Trigger domain compilation via `refresh_unity`.
  5. Verify that console logs reach 0 errors and 0 exceptions.

### 2.5 Pure Logic vs. Thin Adapter Separation
- Decouple pure computation from `MonoBehaviour`.
- Grid math, buff formulas, combat state machines, and poise tracking reside in POCO C# classes with zero Unity lifecycle dependencies and zero heap allocations.
- `MonoBehaviour` instances act strictly as **Thin Adapters**: handling input events, pumping `Tick(deltaTime)`, and routing Unity engine APIs.

### 2.6 File Hygiene & Cache Boundaries
- ❌ **NEVER** search, index, or parse engine cache folders: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`.
- ❌ Do not touch `.meta` files manually without GUID synchronization.

### 2.7 100% English Mandate (Strict)
- ❌ **PROHIBITED**: Writing code identifiers, comments, logs, Inspector attributes (`Header`, `Tooltip`), or UI text in Indonesian or non-English languages.
- ✅ **ALL English Everywhere**: Code identifiers, Inspector attributes, UI text, logs, ScriptableObjects, and technical documentation.

---

## 3. Git Workflow & 3-Layer Repository SOP

1. **Layer 1 (Tech Lead)**: `main` (Production) & `staging` (Testing)
2. **Layer 2 (Lead Dev)**: `development` / `dev` (Primary Integration)
3. **Layer 3 (Programmer/Agent)**: `<programmer_name>` (e.g., `Rafi-branch` / `Sprint-branch`). **Exclusive agent workspace.**

### Branching Constraints
- Agents are **STRICTLY PROHIBITED** from creating new feature branches (`git checkout -b feature/...`).
- Agents are **STRICTLY PROHIBITED** from pushing directly to `main`, `staging`, or `development`.
- All operations and pushes target only Layer 3 branches (`origin/<programmer_name>`).
- Commit messages must be atomic: `<type> : <description>` (`feat`, `progress`, `fix`, `refactor`, `chore`, `assets`).
