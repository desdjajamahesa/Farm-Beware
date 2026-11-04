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
  - *Example*: `feat(environment): relocate trading post and roadway to west matching intro cutscene and isometric camera`
- **`fix`**: Bug, error, assertion, or null reference fix.
  - *Example*: `fix(ui,cutscenes): eliminate ShouldRunBehaviour assertion failures by replacing raw SendMessage with safe reflection invocation`
- **`progress`**: Work-in-progress checkpoint backup.
  - *Example*: `progress: 50% wardrobe item reorganization`
- **`refactor`**: Code restructuring or optimization with zero functional change.
  - *Example*: `refactor: decouple poise logic into pure POCO tracker`
- **`chore`**: Maintenance, package dependencies, documentation, rules, or gitignore updates.
  - *Example*: `chore: update rules with western road, trading post, and 79-test suite state`
- **`assets`**: Visual/audio resource modifications (3D models, textures, audio, materials, scenes).
  - *Example*: `assets: import produce sacks and balance scale for trading post counter`

---

## 3. Agent Execution Workflow

Follow this logical sequence whenever writing, modifying, and committing code:

1. **Step 1: Synchronize (Pull)**:
   Ensure local branch is aligned with upstream:
   ```bash
   git checkout Rafi-branch
   git pull origin development
   ```
2. **Step 2: Isolated Execution & 79-Test Verification**:
   Implement changes with minimal blast radius. Before staging or committing, execute the full audit suite:
   `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()` (All 79 tests must pass).
3. **Step 3: Stage Specific Files**:
   Stage only the files directly associated with the specific task:
   ```bash
   git add <path/to/modified_file>
   ```
   *(Avoid blanket `git add .` if unintended scratch files exist).*
4. **Step 4: Atomic Commit**:
   Commit using the mandatory convention:
   ```bash
   git commit -m "<type>(<scope>): <description>"
   ```
5. **Step 5: Push to Remote Workspace**:
   Push commits to the designated Layer 3 branch on origin:
   ```bash
   git push origin Rafi-branch
   ```

---

## 4. Strict Constraints & Prohibitions

1. ❌ **No Ad-Hoc Feature Branches**: Never create new branches (`git checkout -b feature/...`). Work occurs strictly on assigned Layer 3 branch (`Rafi-branch`).
2. ❌ **No Direct Layer 1 & 2 Pushes**: Never run `git push origin main`, `staging`, or `development`. All pushes target `origin/Rafi-branch`.
3. ❌ **Mandatory Atomic Commits**: Never combine unrelated concerns in a single commit. Commit them separately.
4. ❌ **Mandatory Verification Invariant**: Never commit code with failing tests or unhandled Unity console errors.
