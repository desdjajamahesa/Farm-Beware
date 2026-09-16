# AGENT.md

Primary operating guidelines, behavioral constraints, and Git workflow rules for autonomous AI agents working in the **Farm-Beware** codebase.

---

## 1. Identity & System Context
- **Project**: Farm-Beware
- **Engine**: Unity 6000.3.20f1 (Unity 6 / 2023 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Tooling**: Unity MCP (Model Context Protocol). The agent can and must inspect scene hierarchy, run in-memory Roslyn C# code (`execute_code`), and inspect console logs (`read_console`) autonomously.
- **Audience**: Senior Engineer. Code must adhere to strict OOP principles, data-driven architecture, and zero-leak event lifecycles. Avoid explaining basic programming concepts.

---

## 2. Agent Operational Laws

### 2.1 Token Discipline & Communication
- Keep responses dense, technical, and structured without unnecessary conversational filler or apologies.
- Reference code using standard markdown links: `path/File.cs:LINE`.
- Apply single-concern edits: avoid unrequested drive-by refactorings.

### 2.2 No Editor Setup Scripts (Hard Rule)
- ❌ **NEVER** write or execute temporary editor wiring scripts (`Assets/Editor/*Setup*.cs` or `[MenuItem("...")]`).
- These scripts mutate scenes blindly and introduce duplicate controllers or broken serialized references.
- ✅ **Use Direct MCP Tools**: Inspect and modify scenes exclusively via MCP tools (`execute_code`, `manage_scene`, `manage_gameobject`, `manage_components`).

### 2.3 Self-Healing Protocol (MCP-First)
- Never ask the user to paste console logs or describe the scene:
  1. Pull active console logs via `read_console` (filter: `error`).
  2. Identify the exact root cause and file line.
  3. Apply targeted, minimal patches.
  4. Trigger domain compilation via `refresh_unity` (compile: `request`).
  5. Verify that console logs reach 0 errors.

### 2.4 Pure Logic vs. Thin Adapter Separation
- Decouple pure computation from `MonoBehaviour`.
- Grid math, buff formulas, inventory math, recipe evaluation, and sorting algorithms must reside in POCO C# classes with zero Unity lifecycle dependencies.
- `MonoBehaviour` instances act strictly as **Thin Adapters**: handling input events, pumping updates, and routing engine API calls.

### 2.5 File Hygiene & Cache Boundaries
- ❌ **NEVER** search, index, or parse engine cache folders:
  `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, `.vs/`.
- ❌ Do not touch `.meta` files manually without GUID synchronization. Always use `AssetDatabase.MoveAsset` when relocating assets.

---

## 3. Git Workflow & 3-Layer Repository SOP

The repository operates on a strict 3-Layer branch architecture:

### 3.1 Branch Layers
1. **Layer 1 (Tech Lead)**: `main` (Production) & `staging` (Testing)
2. **Layer 2 (Lead Dev)**: `development` / `dev` (Primary Integration)
3. **Layer 3 (Programmer/Agent)**: `<programmer_name>` (e.g., `rafi-branch`). **Exclusive agent workspace.**

### 3.2 Branching Constraints
- Agents are **STRICTLY PROHIBITED** from creating new feature branches (`git checkout -b feature/...`).
- Agents are **STRICTLY PROHIBITED** from pushing directly to `main`, `staging`, or `development`.
- All operations and pushes target only Layer 3 branches (`origin/<programmer_name>`).

### 3.3 Atomic Commit Conventions
All commit messages must be atomic and follow this format:
`<Commit_Type> : <Clear, Concise Description>`

| Type | When to Use | Example |
|---|---|---|
| `feat (100%)` | Feature fully implemented and verified | `feat (100%) : implement genshin cooking UI panel` |
| `progress` | Work-in-progress backup checkpoint | `progress : 50% wardrobe item reorganization` |
| `fix` | Bug or null-reference resolution | `fix : resolve ESC key pause conflict on UI panels` |
| `refactor` | Code restructuring with zero functional changes | `refactor : relocate wardrobe scripts to feature directory` |
| `chore` | Maintenance, documentation, or folder cleanup | `chore : update architecture documentation and remove dead assets` |
| `assets` | Asset modifications (models, sprites, materials) | `assets : import food icons into project resources` |
