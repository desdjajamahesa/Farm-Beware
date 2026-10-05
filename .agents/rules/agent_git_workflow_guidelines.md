# Git Workflow & Commit Guidelines for AI Agents

Standard Operating Procedure (SOP) for Git commits, branching constraints, and synchronization in **Farm-Beware**.

---

## 1. Repository Branch Structure (3-Layer Architecture)

The repository enforces a 3-Layer authority model. **Agents operate strictly in Layer 3.**

1. **Layer 1 (Tech Lead)**: `main` (Production release) and `staging` (Experimental candidate).
2. **Layer 2 (Reiya's Team)**: `development` or `dev` (Primary feature integration target).
3. **Layer 3 (Programmer/Agent)**: `<programmer_name>` (e.g., `Rafi-branch`, `Sprint-branch`). **Exclusive agent workspace.**

---

## 2. Commit Message Conventions

Commit history is the single source of truth for tracking task progression. All commit messages must be atomic, descriptive, and structured.

**Format**: `<type> : <clear, concise description>`

### Permitted Commit Types:
- **`feat (100%)`**: Fully completed, verified feature ready for review.
  - *Example*: `feat (100%) : implement 3-hit combo and attack buffering`
- **`progress`**: Work-in-progress checkpoint backup.
  - *Example*: `progress : 50% wardrobe item reorganization`
- **`fix`**: Bug, error, or null reference fix.
  - *Example*: `fix : resolve ESC key pause menu conflict on modal UI`
- **`refactor`**: Code restructuring or optimization with zero functional change.
  - *Example*: `refactor : decouple poise logic into pure POCO tracker`
- **`chore`**: Maintenance, package dependencies, documentation, or gitignore updates.
  - *Example*: `chore : update architecture rules and remove dead assets`
- **`assets`**: Visual/audio resource modifications (3D models, textures, audio, materials).
  - *Example*: `assets : import food icons into project resources`

---

## 3. Agent Execution Workflow

Follow this logical sequence whenever writing, modifying, and committing code:

1. **Step 1: Synchronize (Pull)**:
   Ensure local branch is aligned with upstream:
   ```bash
   git checkout <programmer_branch>
   git pull origin development
   ```
2. **Step 2: Isolated Execution**:
   Implement the task applying the *Atomic Changes* principle. Separate new features from unrelated bug fixes.
3. **Step 3: Stage Specific Files**:
   Stage only the files directly associated with the specific task:
   ```bash
   git add <path/to/modified_file>
   ```
   *(Avoid blanket `git add .` if unintended scratch files exist).*
4. **Step 4: Atomic Commit**:
   Commit using the mandatory prefix convention:
   ```bash
   git commit -m "<type> : <description>"
   ```
5. **Step 5: Push to Remote Workspace**:
   Push commits to the designated Layer 3 branch on origin:
   ```bash
   git push origin <programmer_branch>
   ```
   *(Lead Devs handle subsequent pull requests and merges into `development`).*

---

## 4. Strict Constraints & Prohibitions

1. ❌ **No Ad-Hoc Feature Branches**: Never create new branches (`git checkout -b feature/...`). Work occurs strictly on assigned Layer 3 branches.
2. ❌ **No Direct Layer 1 & 2 Pushes**: Never run `git push origin main`, `staging`, or `development`. All pushes target `origin/<programmer_name>`.
3. ❌ **Mandatory Atomic Commits**: Never combine multiple unrelated concerns in a single commit (e.g., combine feature and bugfix). Commit them separately.
