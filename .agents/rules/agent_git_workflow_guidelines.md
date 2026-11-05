# Git Workflow & Commit Guidelines for AI Agents

Standard Operating Procedure (SOP) for Git commits, branching constraints, and synchronization in **Farm-Beware**.

---

## 1. Repository Branch Structure (3-Layer Architecture)

The repository enforces a 3-Layer authority model. **Agents operate strictly in Layer 3.**

1. **Layer 1 (Tech Lead)**: `main` (Production release) and `staging` (Experimental candidate).
2. **Layer 2 (Reiya's Team)**: `development` or `dev` (Primary feature integration target).
3. **Layer 3 (Programmer/Agent)**: `<programmer_name>` (Active branch: `Rafi-branch`). **Exclusive agent workspace.**

---

## 2. Commit Message Conventions

Commit history is the single source of truth for tracking task progression. All commit messages must be atomic, descriptive, and structured.

**Format**: `<type>(<scope>): <clear, concise description>` or `<type> : <description>`

### Permitted Commit Types:
- **`feat`**: Fully completed, verified feature ready for review.
- **`fix`**: Bug, error, assertion, or null reference fix.
- **`progress`**: Work-in-progress checkpoint backup.
- **`refactor`**: Code restructuring or optimization with zero functional change.
- **`chore`**: Maintenance, package dependencies, documentation, rules, or gitignore updates.
- **`assets`**: Visual/audio resource modifications (3D models, textures, audio, materials, scenes).

---

## 3. Agent Execution Workflow

Follow this sequence whenever writing, modifying, and committing code:

1. **Step 1: Synchronize (Pull)**:
   ```bash
   git checkout Rafi-branch
   git pull origin development
   ```
2. **Step 2: Isolated Execution & Anti-Regression Verification**:
   Implement changes with minimal blast radius. Before staging, run the full audit suite:
   `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()` (All 79 tests must pass).
   Verify that existing systems (Combat, Economy, Farming, Cutscenes, Time) remain unaffected.
3. **Step 3: Collateral Diff Audit & Stage Specific Files**:
   Inspect modified files with `git status` and `git diff --name-only`. Ensure no unintended `.unity` or `.prefab` edits occurred:
   ```bash
   git add <path/to/modified_file>
   ```
   *(Never use blanket `git add .`)*.
4. **Step 4: Atomic Commit**:
   Commit using the mandatory convention:
   ```bash
   git commit -m "<type>(<scope>): <description>"
   ```
5. **Step 5: Push to Remote Workspace**:
   ```bash
   git push origin Rafi-branch
   ```

---

## 4. Strict Constraints & Prohibitions

1. ❌ **No Ad-Hoc Feature Branches**: Never create new branches (`git checkout -b feature/...`). Work occurs strictly on assigned Layer 3 branch (`Rafi-branch`).
2. ❌ **No Direct Layer 1 & 2 Pushes**: Never run `git push origin main`, `staging`, or `development`. All pushes target `origin/Rafi-branch`.
3. ❌ **Mandatory Atomic Commits**: Never combine unrelated concerns in a single commit.
4. ❌ **Mandatory Verification Invariant**: Never commit code with failing tests or unhandled console errors.
5. ❌ **No Bundled Fix/Feat Commits**: Never bundle regression bug fixes with new feature commits; commit `fix` first, then `feat`.
6. ❌ **No Unintended Scene Dirtying**: Never commit modified `.unity` scene files unless the task explicitly mandated scene modifications.
