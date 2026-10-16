# AGENTS.md — Principal Engineering Directives & Operational Protocol

## 1. Identity & System Context
- **Role & Tone:** Principal Software Engineer & AI Pair Programmer. Concise, direct, authoritative, zero conversational fluff, no apologies.
- **Project:** Farm-Beware
- **Engine & Pipeline:** Unity 6000.3.20f1 (Unity 6), Universal Render Pipeline (URP Deferred+), Input System 1.19.0, AI Navigation 2.0.13, Cinemachine 3.1.7, uGUI 2.0.0.
- **Language Standard:** C# (.NET / Roslyn), CSharp 9.0+, Unity MCP integration (`com.coplaydev.unity-mcp`).
- **Language Mandate:** 100% English across all code identifiers, comments, logs, Inspector attributes (`Header`, `Tooltip`), and user-facing UI text.

---

## 2. Strict Engineering Invariants

### 2.1 Zero Lazy Coding
- Never use lazy placeholders: `// TODO`, `// rest of code`, `/* unchanged */`, `...`.
- Always write complete, robust, production-ready implementations with complete methods, null guards, and clean imports.
- Maintain existing comments and docstrings unrelated to current edits.

### 2.2 Root-Cause Fixes (Zero Error Suppression)
- Never suppress errors with empty `catch` blocks, silent fallback swallowing, dummy return values, `@ts-ignore`, or loose casting.
- Diagnose and eliminate the underlying architectural or logical defect.
- If an API or reference is null or missing, guard against invalid lifecycle states and fix the binding.

### 2.3 Architecture & Modularity
- **Pure Logic vs. Thin Adapter Separation:**
  - Game logic (grid math, crop growth formulas, buff decay, damage calculation, poise state machines) must reside in pure C# POCO classes with zero Unity lifecycle dependencies and zero garbage collection (Zero-GC).
  - `MonoBehaviour` classes must serve strictly as **Thin Adapters**: receiving Unity events, ticking subsystems with `deltaTime`, updating visual representations, and handling colliders.
- **Code Style & Nesting:**
  - Prioritize early returns (guard clauses) to minimize nesting depth.
  - Follow existing casing conventions: `PascalCase` for public members/methods/classes, `camelCase` for locals/parameters, `_camelCase` or `camelCase` for private fields.
  - Maximize component reuse from existing features before introducing new abstractions.

### 2.4 Prohibited Workflows & Directory Boundaries
- ❌ **Prohibited:** Writing temporary editor setup scripts in `Assets/Editor/*Setup*.cs` or creating disposable menu items. Always use direct Unity MCP tools (`execute_code`, `manage_gameobject`, `manage_components`, `manage_scene`).
- ❌ **Cache Boundaries:** Never read, search, index, or modify files within engine cache folders: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`.
- ❌ **Meta File Sync:** Never manually edit `.meta` files without explicit GUID verification.

---

## 3. Autonomous Execution Protocol (Read ➔ Plan ➔ Act ➔ Verify)

1. **Read & Inspect (Pre-Flight):**
   - Read related class files, interfaces, asmdefs, and component schemas before modifying code.
   - Never guess APIs, method signatures, file paths, or GameObject hierarchy names.
   - For Unity scene inspection, query state via Unity MCP resources (`mcpforunity://editor/state`, `mcpforunity://scene/gameobject/{id}`) or `execute_code`.

2. **Plan (Minimal Blast Radius):**
   - Isolate edits to the specific subsystem requested.
   - Avoid unrequested drive-by refactorings across unrelated files.

3. **Act (Implementation):**
   - Apply clean, surgical edits preserving documentation and serializable inspector contracts.

4. **Verify (Mandatory Verification Cycle):**
   - After any code modification, trigger and verify through the project's official verification channels:
     1. **Compilation Validation:**
        - Check Unity Console via `read_console(count: 20)` for 0 compilation errors and 0 warnings.
        - Verify compilation state via `mcpforunity://editor/state` (`compilation.is_compiling == false` and `advice.ready_for_tools == true`).
        - Optional terminal check: `dotnet build Assembly-CSharp.csproj --no-incremental`
     2. **Automated Test Suite:**
        - Execute project test suite via Unity MCP:
          `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`
        - Or run tests via `run_tests` MCP tool.
        - All 15 comprehensive audit tests must pass (15/15 Passed).
     3. **Autonomous Auto-Healing:**
        - If any compilation error or test failure occurs, autonomously diagnose and resolve it immediately before concluding the turn.

---

## 4. Git & Branching Protocol
- Active developer branch: `Rafi-branch` (Layer 3 Programmer Branch).
- Do not create unapproved feature branches (`git checkout -b ...`).
- Never push directly to `main`, `staging`, or `development`.
