# Project: Farm-Beware — Storyline Base System Loop Upgrade & Playable Sandbox

## Architecture
- **Engine & Pipeline**: Unity 6000.3.20f1, Universal Render Pipeline (URP Deferred+), GPU Resident Drawer, Input System 1.19.0.
- **Pure Logic POCO Core**:
  - `StorylineEngine.cs`: Salience-based Quality-Based Narrative (QBN) engine evaluating rules over `INarrativeFactBoard`. Zero heap allocations on evaluation ($S(R_i) = P_{base} \times 100 + \text{specificityScore}$).
  - `NarrativeFactBoard.cs`: Contiguous `int[128]` state array storing story telemetry and game facts with stack-allocated Span condition evaluation.
  - `QuestProgressModel.cs`: Pure POCO tracking quest stages, prerequisites, and completion conditions.
- **Thin Adapter MonoBehaviour Layer**:
  - `StorylineManager.cs`: Subscribes to game domain events (time phases, farming, combat threats, well interaction) and updates `NarrativeFactBoard`. Implements hard modal safety gate (`ActiveCombatThreatCount == 0`) deferring intrusive popups during combat.
  - `StorylineUI.cs`: uGUI/TextMeshPro dialogue modal and non-modal banner presenter. Upgraded with allocation-free typewriter effect (`TMP.maxVisibleCharacters`) and optional player response choices.
  - `QuestManager.cs` / `QuestHUDUI.cs`: Manages active storyline-driven objectives and renders HUD badges. Upgraded to event-driven updates (`OnQuestUpdated`) to ensure strict Zero-GC on the runtime update loop.
- **Dedicated Sandbox Scene**:
  - `Assets/Scenes/Prototypes/StorylinePrototypeSandbox.unity`: Lightweight 60m×60m testing ground featuring 6 farmland plots, water well, cabin with bed/sink/stove, player controls, and `StorylineSandboxDebugger` for rapid interactive playtesting and pacing tuning.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Storyline Base System Deep Analysis | Comprehensive analysis of existing storyline, dialogue, and quest architecture | M1 | Survey |
| 2 | Storyline Upgrade Plan Document | Detailed architectural upgrade plan aligned with Docs/MVP Version Guideline V0.1 | M1 | Survey |
| 3 | Zero-GC QuestHUDUI Refactor | Event-driven quest HUD updating, eliminating per-frame string allocations in Update() | M2 | Survey |
| 4 | Storyline-Quest Unification | Dynamic narrative objective assignment bridging StorylineEngine and QuestManager | M2 | Survey |
| 5 | Telemetry Hook Integration | Complete wiring for MerchantOpen, TrophyInspected, MealsCookedCount, DeadCropsCount | M2 | Survey |
| 6 | Quest Persistence Serialization | Add quest state and objective progress to GameSaveData and SaveSystemManager | M2 | Survey |
| 7 | Zero-GC Typewriter Dialogue | Implement character-by-character typewriter effect via TMP maxVisibleCharacters | M2 | Survey |
| 8 | Dialogue Player Choices | Support interactive dialogue choices with fact board mutation triggers | M2 | Survey |
| 9 | Modal Combat Safety Invariant | Enforce 0.0s input lock during active night combat (ActiveCombatThreatCount > 0) | M2 | Survey |
| 10 | Dedicated Sandbox Scene Creation | Pruned, lightweight 60m×60m StorylinePrototypeSandbox.unity with 6 plots | M3 | Survey |
| 11 | Sandbox QA Debugger Terminal | In-game tool for time-scrubbing, day transitions, beat triggering, and metrics display | M3 | Survey |
| 12 | 5-Day Episodic Narrative Loop | Day 1 to 5 narrative beats mirroring farming chores and night mutagen threat escalation | M3 | Survey |
| 13 | Interactive 6-Plot Farming Loop | Seed -> Plant -> Water -> Harvest -> Cook -> Combat Buff loop integrated with story | M3 | Survey |
| 14 | Dusk Bell 16:00 Warning Trigger | Narrative alert and audio sting at Minute 15:00 (16:00 in-game) warning player of night | M3 | Survey |
| 15 | Zero-GC Update Loop Audit Test | Automated test profiling GC allocations during active storyline and quest HUD updates | M4 | Survey |
| 16 | Playable Sandbox Loop Audit Test | Automated test executing Day 1–5 narrative loop in the sandbox scene | M4 | Survey |
| 17 | Comprehensive Test Suite 100% Pass | Validate all existing 64 tests + new storyline tests pass (66/66) | M4 | Survey |
| 18 | Fun-Tested 5-Pillar Evaluation | Interactive playtest evaluation against 25-point rubric (Pacing, Agency, Tension, Economy, Juice) | M5 | Survey |
| 19 | 60 FPS Performance Verification | Profiler validation under URP Deferred+ ensuring stable 60 FPS target | M5 | Survey |
| 20 | Forensic Integrity Audit | Independent verification of zero hardcoding, zero facade implementations, and clean code | M5 | Survey |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Analysis & Upgrade Plan Publishing | Author and publish official Docs/STORYLINE_BASE_SYSTEM_ANALYSIS.md and Docs/STORYLINE_BASE_SYSTEM_UPGRADE_PLAN.md | None | DONE |
| M2 | Core Architectural Upgrades & Zero-GC Refactor | Refactor QuestHUDUI for Zero-GC, unify Storyline and Quests, wire telemetry hooks, add quest persistence, add typewriter/choice UI | M1 | DONE |
| M3 | Dedicated Playable Prototype Sandbox | Create StorylinePrototypeSandbox.unity, configure 6 plots, well, cabin, player, and StorylineSandboxDebugger | M2 | DONE |
| M4 | Automated Verification & Zero-GC Tests | Implement Tests 65 & 66 in GameplayAndHUDTests.cs, verify 66/66 tests pass | M3 | DONE |
| M5 | Fun-Testing, Profiling & Forensic Audit | Playtest sandbox against 25-point rubric, verify 60 FPS & 0 GC, run Challenger & Forensic Auditor | M4 | DONE |

## Interface Contracts
### StorylineEngine ↔ QuestManager (`INarrativeObjectiveService`)
- `void AssignStoryObjective(string objectiveId, string title, string description, int targetCount);`
- `void CompleteStoryObjective(string objectiveId);`
- `event Action<string, bool> OnObjectiveStateChanged;`

### QuestHUDUI ↔ QuestManager (Zero-GC Contract)
- `void OnEnable()`: Subscribes to `QuestManager.OnQuestUpdated`. Zero work in `Update()`.
- `void RefreshUI(QuestProgressModel model)`: Formats strings only when quest state transitions.
- `void OnDisable()`: Unsubscribes from events.

### StorylineUI ↔ Typewriter Effect
- Uses `TextMeshProUGUI.maxVisibleCharacters` incremented by timer delta in `Update()`.
- Zero string slicing or sub-string allocation during typewriter animation.

## Code Layout
- `Docs/STORYLINE_BASE_SYSTEM_ANALYSIS.md` — Deep analysis document
- `Docs/STORYLINE_BASE_SYSTEM_UPGRADE_PLAN.md` — Upgrade plan document
- `Assets/Scripts/Core/Contracts/INarrativeObjectiveService.cs` — Interface for narrative quest bridge
- `Assets/Scripts/Features/Storyline/` — Storyline engine, database, and manager
- `Assets/Scripts/Features/Storyline/UI/StorylineUI.cs` — Dialogue presentation with typewriter
- `Assets/Scripts/Features/Quest/UI/QuestHUDUI.cs` — Event-driven zero-GC quest HUD
- `Assets/Scripts/Features/Quest/` — Quest management and models
- `Assets/Scripts/Features/Storyline/Sandbox/StorylineSandboxDebugger.cs` — Sandbox QA controller
- `Assets/Scenes/Prototypes/StorylinePrototypeSandbox.unity` — Dedicated prototype sandbox scene
- `Assets/Scripts/Editor/GameplayAndHUDTests.cs` — Automated test suite
