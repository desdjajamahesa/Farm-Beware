# Git Workflow & Commit Guidelines for AI Agents

Standard Operating Procedure (SOP) for Git commits, branching constraints, and synchronization in **Farm-Beware**.

---

## 1. Repository Branch Structure (3-Layer Architecture)

The repository enforces a 3-Layer authority model. **Agents operate strictly in Layer 3.**

1. **Layer 1 (Tech Lead)**: `main` (Production release) and `staging` (QA release candidate).
2. **Layer 2 (Lead Dev)**: `development` / `dev` (Primary feature integration target).
3. **Layer 3 (Programmer/Agent)**: `<programmer_name>` (Active branch: `Rafi-branch`). **Exclusive agent workspace.**

---

## 2. Commit Message Conventions

All commits must be atomic, descriptive, and structured using Conventional Commits:

**Format**: `<type>(<scope>): <clear, concise description>`

### Permitted Commit Types:
- **`feat`**: Completed, verified feature ready for review.
- **`fix`**: Bug, error, assertion, or null reference fix.
- **`test`**: Automated audit test additions or NUnit CI harness updates.
- **`progress`**: Work-in-progress checkpoint backup.
- **`refactor`**: Code restructuring or optimization with zero functional change.
- **`chore`**: Maintenance, package dependencies, `.gitattributes`, or cleanup.
- **`docs`**: Documentation, rules, GDD, and changelog updates.
- **`assets`**: Visual/audio resource modifications (models, textures, audio, scenes, prefabs).

---

## 3. Agent Execution Workflow

1. **Synchronize**: Work strictly on `Rafi-branch` (`git pull origin development` when integrating approved updates).
2. **Isolated Execution & Verification**:
   Implement edits with minimal blast radius, then verify **117/117 Passed** via:
   - `FarmBeware.EditorTests.GameplayAndHUDTests.RunAllTests()`
   - `run_tests(mode: "EditMode", assembly_names: ["FarmBeware.Editor.Tests"])`
3. **Collateral Diff Audit & Selective Stage**:
   Inspect `git status -s` and `git diff --name-only` to ensure zero unintended `.unity`, `.prefab`, or `.asset` changes:
   ```bash
   git add <path/to/modified_file>
   ```
   *(Prefer explicit paths over blanket `git add .`)*.
4. **Atomic Commit & Push**:
   ```bash
   git commit -m "<type>(<scope>): <description>"
   git push origin Rafi-branch
   ```

---

## 4. Strict Constraints & Prohibitions

1. **No Ad-Hoc Feature Branches**: Never run `git checkout -b ...`; stay on `Rafi-branch`.
2. **Never Touch Protected Branches**: Never checkout, merge into, reset, or push to `main`, `staging`, or `development`.
3. **Mandatory Verification Gate**: Never commit code with compilation errors, warnings, or failing tests (< 117/117).
4. **No Bundled Fix/Feat Commits**: Commit `fix` separately from new `feat` or `refactor` changes.
5. **No Unintended Scene Dirtying**: Never stage modified `.unity` scenes unless explicitly required and verified.
