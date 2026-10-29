# Farm-Beware: Storyline Base System Upgrade Plan & Implementation Blueprint

**Document ID:** DOC-STORY-002  
**Author:** Storyline Documentation Engineer  
**Engine & Pipeline:** Unity 6000.3.20f1 (Unity 6 LTS) | Universal Render Pipeline (URP Deferred+) | Input System 1.19.0  
**Target Subsystems:** `FarmBeware.Features.Storyline`, `FarmBeware.Features.Quest`, `FarmBeware.Core.Runtime`, `FarmBeware.Features.SaveSystem`  
**Authoritative Reference:** `Docs/STORYLINE_BASE_SYSTEM_ANALYSIS.md`, `Docs/MVP Version Guideline V0.1`, `PROJECT.md`, `AGENTS.md`  
**Status:** Approved Implementation Blueprint  

---

## 1. Executive Summary & Objective

This document establishes the production-grade architectural upgrade plan for the **Storyline Base System** in *Farm-Beware*. Following the deep diagnosis documented in `Docs/STORYLINE_BASE_SYSTEM_ANALYSIS.md`, this blueprint provides concrete technical specifications to:
1. Eliminate the critical per-frame heap allocation leak in `QuestHUDUI.cs` and establish a strict Zero-GC event-driven UI pattern.
2. Bridge the disconnected Storyline and Quest systems through a unified pure POCO `INarrativeObjectiveService`.
3. Upgrade `StorylineUI.cs` with an allocation-free TextMeshPro typewriter effect, procedural audio blips, and interactive branching player response choices.
4. Wire all dangling telemetry hooks across economy, cooking, farming, and combat into `NarrativeTelemetry` and `NarrativeFactBoard`.
5. Integrate quest progress persistence into `SaveSystemManager.cs` and `GameSaveData`.
6. Architect the dedicated 60m×60m `StorylinePrototypeSandbox.unity` scene equipped with the `StorylineSandboxDebugger` QA control terminal.
7. Establish the 5-pillar 25-point "fun-tested" evaluation rubric and author automated test specifications (Tests 65 & 66 in `GameplayAndHUDTests.cs`).

---

## 2. Strict Invariants & Boundary Compliance (`MVP Version Guideline V0.1`)

To ensure **ZERO contradiction** with game design mandates, all upgrades must strictly satisfy the following immutable boundaries:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                           IMMUTABLE BOUNDARIES MATRIX (MVP V0.1)                            │
├───────────────────────────────┬─────────────────────────────────────────────────────────────┤
│ Core Crops (Exactly 3)        │ Sweet Potato (6 min), Taro (15 min), Corn (15 min).         │
│                               │ NO additional seed types may be introduced.                 │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Monster Forms (Exactly 6)     │ 3 Standard: Tuber Maw, Taro Brute, Corn Musketeer.          │
│                               │ 3 Apex Bosses: Cyclops Tuber Maw, Taro Colossus, Ranger.    │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Day Phase Length              │ Exactly 18 real minutes (06:00 to 18:00 in-game).           │
│                               │ 1 real minute = 40 in-game minutes.                         │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Dusk Bell Warning             │ Minute 15:00 real time (16:00 in-game). 3 min remaining.   │
│                               │ Audio sting ("DING DONG") + amber atmosphere shift.         │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Night Combat Duration         │ UNTIMED. Night terminates strictly when all wave monsters   │
│                               │ are eliminated. NEVER attach a countdown timer to night.    │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Nocturnal Crop Growth         │ STRICTLY FROZEN. Growth ticks pause completely between      │
│                               │ 18:00 and 06:00. Unharvested crops retain current progress. │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Water Resource Limits         │ 100-liter water bottle capacity. Water well refills in 10s, │
│                               │ restricted to maximum 2 refills per day (200L daily cap).   │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Food Combat Limit             │ Exactly 3 food items can be carried into the night phase.   │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Weapon Upgrade Progression    │ 1 base weapon with 3 material paths (Speed, Defense, Area). │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Modal Combat Safety Gate      │ ZERO modal dialogs or input-locking UI during active combat │
│                               │ (ActiveCombatThreatCount > 0). All modals must be deferred. │
├───────────────────────────────┼─────────────────────────────────────────────────────────────┤
│ Runtime Update Loop Memory    │ ZERO Garbage Collection allocations (0 B GC alloc) on hot  │
│                               │ update loops. All string formatting must be event-driven.   │
└───────────────────────────────┴─────────────────────────────────────────────────────────────┘
```

---

## 3. Subsystem Technical Architecture & Upgrades

### 3.1 Upgrade A: Zero-GC Event-Driven `QuestHUDUI` Refactor

#### Defect Addressed
`QuestHUDUI.cs` currently executes string interpolation (`$"• SIDE QUEST {statusTag}"`) and invokes `model.GetFormattedObjectiveDetails()` inside `Update()` every single frame at 60 FPS, creating 600–1,200 ephemeral string allocations per second.

#### Technical Solution
1. **Remove `Update()` Polling:** Eliminate the `Update()` method entirely from `QuestHUDUI.cs`.
2. **Event-Driven Observer:** Subscribe to `INarrativeObjectiveService.OnObjectiveStateChanged` and `QuestManager.OnQuestUpdated`.
3. **Cached String Buffers:** Formats UI text only when objective state, stage, or progress count transitions.
4. **Lifecycle Subscription Safety:** Register listeners in `OnEnable()` and cleanly unregister in `OnDisable()`.

```csharp
namespace FarmBeware.Features.Quest.UI
{
    using UnityEngine;
    using TMPro;
    using FarmBeware.Core.Runtime;

    public sealed class QuestHUDUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI questHeaderBadgeText;
        [SerializeField] private TextMeshProUGUI questTitleText;
        [SerializeField] private TextMeshProUGUI questDetailText;
        [SerializeField] private CanvasGroup rootCanvasGroup;

        private INarrativeObjectiveService _objectiveService;
        private string _cachedDetailsString;
        private int _cachedProgress = -1;
        private int _cachedStage = -1;

        private void OnEnable()
        {
            _objectiveService = ServiceLocator.Resolve<INarrativeObjectiveService>();
            if (_objectiveService != null)
            {
                _objectiveService.OnObjectiveStateChanged += HandleObjectiveStateChanged;
            }
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
            }
            RefreshDisplayImmediate();
        }

        private void OnDisable()
        {
            if (_objectiveService != null)
            {
                _objectiveService.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
            }
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            }
        }

        private void HandleObjectiveStateChanged(string objectiveId, bool isCompleted)
        {
            RefreshDisplayImmediate();
        }

        private void HandleQuestUpdated(QuestProgressModel model)
        {
            if (model == null) return;
            if (model.CurrentStage == _cachedStage && model.ProgressHash == _cachedProgress)
                return; // Zero work if state has not mutated

            _cachedStage = model.CurrentStage;
            _cachedProgress = model.ProgressHash;
            UpdateDisplayFromModel(model);
        }

        // String formatting executed ONLY on state transition
        public void UpdateDisplayFromModel(QuestProgressModel model)
        {
            if (questHeaderBadgeText != null)
                questHeaderBadgeText.text = model.IsCompleted ? "• SIDE QUEST [DONE]" : "• SIDE QUEST [ACTIVE]";

            if (questDetailText != null)
                questDetailText.text = model.GetFormattedObjectiveDetails();
        }
    }
}
```

---

### 3.2 Upgrade B: `INarrativeObjectiveService` (Storyline & Quest Unification)

#### Defect Addressed
`StorylineEngine` and `QuestManager` exist in isolated silos. Narrative beats cannot issue or track gameplay objectives on the player's HUD.

#### Technical Solution
Introduce a lightweight, decoupled pure C# service contract registered in `ServiceLocator`:

```csharp
namespace FarmBeware.Core.Runtime
{
    using System;
    using System.Collections.Generic;

    public enum NarrativeObjectiveCategory : byte
    {
        MainStory = 0,
        SideQuest = 1,
        SurvivalPrep = 2
    }

    public sealed class NarrativeObjective
    {
        public string ObjectiveId { get; set; }
        public NarrativeObjectiveCategory Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TargetCount { get; set; }
        public int CurrentCount { get; set; }
        public bool IsCompleted { get; set; }
        public NarrativeFactKey TrackingFactKey { get; set; }
    }

    public interface INarrativeObjectiveService
    {
        IReadOnlyList<NarrativeObjective> ActiveObjectives { get; }
        void AssignStoryObjective(string id, NarrativeObjectiveCategory category, string title, string description, int targetCount, NarrativeFactKey trackingKey);
        void ReportProgress(NarrativeFactKey factKey, int delta);
        void CompleteObjective(string id);
        event Action<string, bool> OnObjectiveStateChanged;
    }
}
```

#### Daily 5-Day Storyline Objectives Sequence
The service drives the 5-day campaign arc by assigning daily narrative objectives synchronized with story beats:

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   5-DAY NARRATIVE OBJECTIVE SEQUENCE                             │
├───────┬──────────────────────┬───────────────────────────────┬───────────────────────────────────┤
│ Day   │ Objective ID         │ HUD Title & Description       │ Completion Target & Success Fact  │
├───────┼──────────────────────┼───────────────────────────────┼───────────────────────────────────┤
│ **1** │ `OBJ_D1_FIRST_STEPS` │ "First Steps: Till & Plant"   │ Till 1 plot, draw well water,     │
│       │                      │ Uncover buried tag & plant SP │ Plant Sweet Potato (Key: 25)      │
├───────┼──────────────────────┼───────────────────────────────┼───────────────────────────────────┤
│ **2** │ `OBJ_D2_ARMORED_TARO`│ "Armored Harvest: Clean Taro" │ Harvest Taro & wash 3 roots at    │
│       │                      │ Prepare defense against brute │ Kitchen Sink (Key: 26)            │
├───────┼──────────────────────┼───────────────────────────────┼───────────────────────────────────┤
│ **3** │ `OBJ_D3_WORKBENCH`   │ "Workbench: Harden Weapon"    │ Upgrade weapon at Workbench using │
│       │                      │ Prep for Cyclops Tuber Maw    │ monster root material (Key: 30)   │
├───────┼──────────────────────┼───────────────────────────────┼───────────────────────────────────┤
│ **4** │ `OBJ_D4_BATTLE_PREP` │ "Siege Prep: Cook Battle Food"│ Cook 3 combat buff dishes at the  │
│       │                      │ Prepare for Taro Colossus     │ Kitchen Stove (Key: 10)           │
├───────┼──────────────────────┼───────────────────────────────┼───────────────────────────────────┤
│ **5** │ `OBJ_D5_PURGE_VALVE` │ "Final Stand: Purge Conduit"  │ Defeat The Ranger and purge       │
│       │                      │ Release Relief Valve #09      │ subterranean Reagent 7 (Key: 20)  │
└───────┴──────────────────────┴───────────────────────────────┴───────────────────────────────────┘
```

---

### 3.3 Upgrade C: Zero-GC TextMeshPro Typewriter Effect & Branching Choice UI

#### Defect Addressed
`StorylineUI.cs` displays dialogue instantaneously in full text, lacks procedural audio chits, and offers no branching player choices.

#### Technical Solution
1. **Zero-GC Typewriter:** Assign full string once to `dialogueBodyText.text`, set `dialogueBodyText.maxVisibleCharacters = 0`, and increment `maxVisibleCharacters` over time in `Update()`. Substrings are NEVER allocated.
2. **Fast-Forward:** Pressing `Space`, `Enter`, or primary click while typing instantly reveals all characters (`maxVisibleCharacters = totalCharacters`). Subsequent press dismisses the modal.
3. **Procedural Audio Chits:** Trigger a light pitch-modulated audio blip (frequency 800–1200 Hz, duration 15 ms) on character reveal, skipping whitespace.
4. **Interactive Dialogue Choices:** Add choice container with 2–3 response buttons. Selecting a response applies specific `NarrativeFactMutation` operations to the `NarrativeFactBoard`.

```csharp
namespace FarmBeware.Features.Storyline.UI
{
    using UnityEngine;
    using TMPro;
    using System;
    using System.Collections;

    public sealed class TypewriterPresenter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private float charactersPerSecond = 35f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typewriterChitSound;

        private int _totalCharacters;
        private float _visibleCount;
        private bool _isTyping;
        private Action _onComplete;

        public bool IsTyping => _isTyping;

        public void StartTyping(string fullText, Action onComplete)
        {
            if (targetText == null) return;

            targetText.text = fullText;
            targetText.maxVisibleCharacters = 0;
            _totalCharacters = fullText.Length;
            _visibleCount = 0f;
            _isTyping = true;
            _onComplete = onComplete;
        }

        public void SkipToFullText()
        {
            if (!_isTyping) return;
            _isTyping = false;
            targetText.maxVisibleCharacters = _totalCharacters;
            _onComplete?.Invoke();
        }

        private void Update()
        {
            if (!_isTyping) return;

            int prevCharIndex = (int)_visibleCount;
            _visibleCount += charactersPerSecond * Time.unscaledDeltaTime;
            int newCharIndex = Mathf.Min((int)_visibleCount, _totalCharacters);

            if (newCharIndex > prevCharIndex)
            {
                targetText.maxVisibleCharacters = newCharIndex;

                // Audio feedback (skip spaces)
                if (audioSource != null && typewriterChitSound != null && newCharIndex < _totalCharacters)
                {
                    char c = targetText.text[newCharIndex];
                    if (!char.IsWhiteSpace(c))
                    {
                        audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                        audioSource.PlayOneShot(typewriterChitSound, 0.4f);
                    }
                }
            }

            if (newCharIndex >= _totalCharacters)
            {
                _isTyping = false;
                _onComplete?.Invoke();
            }
        }
    }
}
```

---

### 3.4 Upgrade D: Complete Gameplay Telemetry Hook Wiring

#### Defect Addressed
Uninvoked trigger enums and unwired `NarrativeFactKey` entries cause storylet conditions to fail silently.

#### Implementation Wiring Plan
| Telemetry Target | Source Class & Method | Trigger / Fact Mutation Wired |
|---|---|---|
| **Merchant Shop** | `MerchantShopUI.OpenModal()` | Dispatches `NarrativeTelemetry.ReportMerchantOpened()`; evaluates `StoryTriggerType.MerchantOpen`. |
| **Trophy Cabinet** | `TrophyCabinetInteractable.Interact()` | Dispatches `NarrativeTelemetry.ReportTrophyInspected()`; evaluates `StoryTriggerType.TrophyInspected`. |
| **Kitchen Cooking** | `StoveUIManager.OnDishCooked()` | Updates `NarrativeFactKey.MealsCookedCount += 1`; evaluates `StoryTriggerType.CookingCompleted`. |
| **Farmland Wither** | `FarmlandTile.OnCropWithered()` | Updates `NarrativeFactKey.DeadCropsCount += 1`. |
| **Player Damage** | `PlayerStats.TakeDamage()` | If HP < 25%, updates `NarrativeFactKey.PlayerLowHealthTriggered = 1`. |
| **Dusk Approach** | `DayNightTimeManager.Update()` | At 15:00 in-game time, dispatches `StoryTriggerType.DuskApproaching`. |
| **The Ranger Death** | `StorylineManager.HandleAnyEnemyDied()` | When `enemy.EnemyType == EnemyType.TheRanger`, sets `NarrativeFactKey.TheRangerSlayed = 1`. |

---

### 3.5 Upgrade E: Quest Persistence Serialization in `SaveSystemManager`

#### Defect Addressed
Quest stages and progress counts are discarded on game reload because `QuestManager` is omitted from `GameSaveData`.

#### Data Structure & Integration
Add `QuestSavePayload` to `GameSaveData.cs` and wire serialize/restore in `SaveSystemManager.cs`:

```csharp
[System.Serializable]
public sealed class QuestSavePayload
{
    public string activeQuestId;
    public int currentStage;
    public int plantedCount;
    public int wateredCount;
    public bool isCompleted;
    public List<NarrativeObjectiveSaveData> activeStoryObjectives = new List<NarrativeObjectiveSaveData>();
}

[System.Serializable]
public sealed class NarrativeObjectiveSaveData
{
    public string objectiveId;
    public int currentCount;
    public bool isCompleted;
}
```

In `SaveSystemManager.cs`:
- **Save Path:** `saveData.questPayload = QuestManager.Instance.ExportSavePayload();`
- **Load Path:** `QuestManager.Instance.ImportSavePayload(saveData.questPayload);`

---

## 4. Dedicated Playable Prototype Sandbox Scene Architecture

### 4.1 Scene Specification & Footprint
- **File Path:** `Assets/Scenes/Prototypes/StorylinePrototypeSandbox.unity`
- **Dimensions:** 60m × 60m enclosed homestead perimeter.
- **Render Pipeline:** URP Deferred+ (`PC_Renderer.asset`), GPU Resident Drawer enabled.
- **Budget Targets:**
  - Framerate: Stable 60 FPS (16.66 ms).
  - Draw Calls: < 90 batches.
  - Geometry: < 250,000 vertices.
  - Hot Loop Memory: **0 B GC allocation per frame**.

```
┌────────────────────────────────────────────────────────────────────────┐
│               60m × 60m STORYLINE PROTOTYPE SANDBOX LAYOUT             │
│                                                                        │
│   [North Perimeter Fence] ──────────────────────────────────────────┐  │
│   │                                                                │  │
│   │   ┌─────────────────────┐             ┌────────────────────┐   │  │
│   │   │   HOMESTEAD CABIN   │             │   STONE WELL       │   │  │
│   │   │   - Bed (Sleep)     │             │   - 100L Refill    │   │  │
│   │   │   - Sink (Wash)     │             │   - Scent Clue     │   │  │
│   │   │   - Stove (Cook)    │             └────────────────────┘   │  │
│   │   │   - Workbench       │                                      │  │
│   │   └─────────────────────┘             ┌────────────────────┐   │  │
│   │                                       │  FARMLAND (6 PLOTS)│   │  │
│   │                                       │  - Starter Plots   │   │  │
│   │                                       │  - SP, Taro, Corn  │   │  │
│   │                                       └────────────────────┘   │  │
│   │   ┌─────────────────────┐                                      │  │
│   │   │   QA DEBUG TERMINAL │                                      │  │
│   │   │   - Time Scrubbing  │             ┌────────────────────┐   │  │
│   │   │   - Beat Invocator  │             │   MERCHANT TABLE   │   │  │
│   │   └─────────────────────┘             └────────────────────┘   │  │
│   │                                                                │  │
│   [South Perimeter Gate] ───[BREACH SPAWN ZONE]────────────────────┘  │
└────────────────────────────────────────────────────────────────────────┘
```

### 4.2 7-Layer Modular Hierarchy Compliance
The sandbox scene is constructed using the project's standard 7-layer hierarchy:
1. `_SYSTEMS`: `GameManager`, `CameraManager`, `EventSystem`, `ModalStackManager`, `StorylineManager`, `QuestManager`, `NightBrawlManager`, `SaveSystemManager`.
2. `_LIGHTING_ENVIRONMENT`: `LightingAndAtmosphereDirector`, `Directional Light` (UniversalAdditionalLightData), `HouseSafeZoneLights`, `OutdoorGardenLamps`, `DefaultVolumeProfile`.
3. `_CAMERAS`: `Main Camera` (UniversalAdditionalCameraData, IsometricCameraController).
4. `_ENTITIES`: `Player` (`character_player_v2.prefab`, `PlayerControl`, `PlayerStats`, `PlayerWaterBottle`, `InventoryComponent`).
5. `_WORLD`: 60m×60m ground plane, `HomesteadPerimeter.prefab` with dynamic NavMesh carving, cabin structure, stone well.
6. `_GAMEPLAY`: 6 `FarmlandPlot.prefab` units, interactive stations (Bed, Sink, Stove, Workbench, Merchant Table), monster breach spawn points.
7. `_UI`: `UI_Canvas` containing `HUD_CombatPhaseTracker`, `QuestHUDUI`, `StorylineUI`, `CutsceneLetterboxUI`, and `StorylineSandboxDebugger`.

### 4.3 `StorylineSandboxDebugger` QA Control Terminal
An in-scene interactive controller (`StorylineSandboxDebugger.cs`) providing rapid testing controls:
- **Time Scrubbing:** Buttons to jump instantly to Dawn (06:00), Noon (12:00), Dusk (16:00), and Night (18:00).
- **Day Progression:** Jump between Day 1, Day 2, Day 3, Day 4, and Day 5.
- **Wave & Boss Spawner:** Spawn specific waves (Tuber Maws, Taro Brutes, Cyclops C-1, Taro Colossus, The Ranger).
- **Threat Simulation:** Toggle `ActiveCombatThreatCount = 3` to verify the modal combat safety invariant.
- **State Reset:** One-click restoration of clean Day 1 state without reloading the scene.

---

## 5. "Fun-Tested" 5-Pillar Evaluation Rubric (25-Point Standard)

To quantitatively evaluate and verify that the storyline base system loop is enjoyable, responsive, and balanced according to `Docs/MVP Version Guideline V0.1`, the following 25-point rubric must be used during test-agent and human playtests:

```
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                         5-PILLAR 25-POINT "FUN-TESTED" RUBRIC                                         │
├─────────┬──────────────────────────┬───────────────────────────────────────────────────────────┬──────────────┬───────┤
│ Pillar  │ Metric Focus             │ Evaluation Criteria                                       │ Scoring Range│ Pass  │
├─────────┼──────────────────────────┼───────────────────────────────────────────────────────────┼──────────────┼───────┤
│ **P1**  │ Narrative Pacing & Timing│ Story beats never interrupt active melee/dodge combat.    │ 1: Bad popups│ ≥ 4/5 │
│         │                          │ Modal dialogues appear only during calm exploration;      │ 3: Friction  │       │
│         │                          │ non-modal banners provide immediate flavor during movement│ 5: Flawless  │       │
├─────────┼──────────────────────────┼───────────────────────────────────────────────────────────┼──────────────┼───────┤
│ **P2**  │ Player Agency & Objective│ The player always knows what to do next without external  │ 1: Ambiguous │ ≥ 4/5 │
│         │ Clarity                  │ documentation. The Quest HUD and Activity Card update     │ 3: Delayed   │       │
│         │                          │ instantaneously with clear visual checkmarks on completion│ 5: Instant   │       │
├─────────┼──────────────────────────┼───────────────────────────────────────────────────────────┼──────────────┼───────┤
│ **P3**  │ Horror-Farming Tension   │ Clear psychological contrast between peaceful sunny day   │ 1: Flat/static│ ≥ 5/5 │
│         │ Cohesion                 │ agriculture (golden light) and eerie dusk/night dread     │ 3: Weak shift│       │
│         │                          │ (amber sky, dusk bell warning, twitching crops, monsters).│ 5: Visceral  │       │
├─────────┼──────────────────────────┼───────────────────────────────────────────────────────────┼──────────────┼───────┤
│ **P4**  │ Economy & Survival       │ Narrative justifies the core dilemma: *"Harvest and sell  │ 1: Siloed    │ ≥ 4/5 │
│         │ Dilemma                  │ for Gold vs. Cook food buffs for night survival."         │ 3: Generic   │       │
│         │                          │ Dialogues explain why Taro gives armor and SP gives speed.│ 5: Masterful │       │
├─────────┼──────────────────────────┼───────────────────────────────────────────────────────────┼──────────────┼───────┤
│ **P5**  │ Polish, Sound & Visual   │ High polish in story feedback: metallic screen shake on   │ 1: Rough/dry │ ≥ 5/5 │
│         │ Juice                    │ pipe strike, audio stingers on discovery, clean letterbox │ 3: Functional│       │
│         │                          │ cutscenes, procedural sleep vignettes, zero UI overlap.   │ 5: High juice│       │
└─────────┴──────────────────────────┴───────────────────────────────────────────────────────────┴──────────────┴───────┤
│                                        OVERALL PASS THRESHOLD: ≥ 22 / 25 POINTS                                       │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

**Rejection Policy:** Any score of 1 or 2 on any individual pillar triggers an automatic rejection and requires a redesign pass.

---

## 6. Automated Verification & Quality Assurance Suite

To maintain the project's zero-regression mandate and verify both performance and narrative loop mechanics autonomously, two new automated tests are added to `Assets/Scripts/Editor/GameplayAndHUDTests.cs`:

### Test 65: Automated Update-Loop Zero-GC Profiling Test
- **Method Name:** `Test_Storyline_UpdateLoop_ZeroGC_Profiling_Audit()`
- **Execution Target:**
  1. Instantiates `QuestHUDUI`, `StorylineUI`, and mock `QuestProgressModel`.
  2. Executes 300 continuous simulated frames of `QuestHUDUI.Update()` and `StorylineUI.Update()`.
  3. Measures heap allocation via `System.GC.GetAllocatedBytesForCurrentThread()`.
  4. Asserts that total allocated memory == **0 Bytes**.

```csharp
[Test]
public static void Test_Storyline_UpdateLoop_ZeroGC_Profiling_Audit()
{
    var go = new GameObject("Test_QuestHUD_Host");
    var hud = go.AddComponent<QuestHUDUI>();
    
    // Warm-up JIT & initial allocations
    for (int i = 0; i < 10; i++) hud.SimulateFrame(0.016f);
    
    long startBytes = System.GC.GetAllocatedBytesForCurrentThread();
    for (int frame = 0; frame < 300; frame++)
    {
        hud.SimulateFrame(0.016f);
    }
    long allocated = System.GC.GetAllocatedBytesForCurrentThread() - startBytes;
    
    Object.DestroyImmediate(go);
    Assert.AreEqual(0, allocated, $"Zero-GC violation! Allocated {allocated} bytes during 300 update frames.");
}
```

### Test 66: End-to-End Playable Sandbox Loop Audit
- **Method Name:** `Test_Storyline_SandboxPlayableLoop_E2E_Audit()`
- **Execution Target:**
  1. Spawns isolated sandbox environment with Player, Plot, Well, Bed, and StorylineManager.
  2. Simulates Hoe till $\to$ verifies `D1_FIRST_TILL` triggered, dialogue modal displayed, and player input locked.
  3. Simulates Space/Enter $\to$ verifies modal dismissed and player input restored.
  4. Simulates Well water refill $\to$ verifies `D1_WELL_WATER_100` banner delivered.
  5. Advances time to 16:00 Dusk $\to$ verifies Dusk Bell tolls and `D1_DUSK_WARNING` triggered.
  6. Advances time to 18:00 Night with `ActiveCombatThreatCount = 3` $\to$ verifies modal queue holds safely without mid-combat interruption.
  7. Clears threats to 0 $\to$ verifies `D1_NIGHT_CLEARED` and Journal entry unlocked.
  8. Interacts with Bed $\to$ verifies sleep transition, dream sequence, and Day 2 Dawn advance.

---

## 7. Implementation Roadmap & Milestones

```
┌────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   STORYLINE UPGRADE IMPLEMENTATION ROADMAP                             │
├────────────┬───────────────────────────────────────┬───────────────────────────────────┬───────────────┤
│ Milestone  │ Core Deliverables                     │ Subsystem Scope                   │ Status        │
├────────────┼───────────────────────────────────────┼───────────────────────────────────┼───────────────┤
│ **M1**     │ Storyline Deep Analysis & Plan        │ `Docs/STORYLINE_BASE_SYSTEM_...`  │ COMPLETED     │
│            │ Technical specification publishing.   │ Complete architecture audit.      │               │
├────────────┼───────────────────────────────────────┼───────────────────────────────────┼───────────────┤
│ **M2**     │ Core Architectural Upgrades & Zero-GC │ `QuestHUDUI.cs` Zero-GC refactor, │ NEXT UP       │
│            │ Refactor. Unify Storyline & Quests.   │ `INarrativeObjectiveService.cs`,  │               │
│            │ Wire telemetry & quest persistence.   │ Typewriter & dialogue choice UI.  │               │
├────────────┼───────────────────────────────────────┼───────────────────────────────────┼───────────────┤
│ **M3**     │ Dedicated Playable Prototype Sandbox  │ Assemble `StorylinePrototype-     │ PLANNED       │
│            │ 60m×60m isolated testing scene.       │ Sandbox.unity` + QA Debugger.     │               │
├────────────┼───────────────────────────────────────┼───────────────────────────────────┼───────────────┤
│ **M4**     │ Automated Verification & Tests 65/66  │ Add Tests 65 & 66 to test suite.  │ PLANNED       │
│            │ Verify 66/66 comprehensive pass.      │ 100% test suite passing.          │               │
├────────────┼───────────────────────────────────────┼───────────────────────────────────┼───────────────┤
│ **M5**     │ Fun-Testing, Profiling & Forensic     │ Playtest 25-point rubric (≥22/25),│ PLANNED       │
│            │ Audit. Verify 60 FPS & Zero-GC.       │ Independent forensic verification.│               │
└────────────┴───────────────────────────────────────┴───────────────────────────────────┴───────────────┘
```

---

## 8. Conclusion

This upgrade plan provides an exact, production-ready engineering blueprint that directly resolves every architectural defect, memory vulnerability, and gameplay silo identified in `Docs/STORYLINE_BASE_SYSTEM_ANALYSIS.md`.

By executing this blueprint, *Farm-Beware* will achieve a unified, high-performance, and deeply immersive narrative loop that satisfies every constraint of `Docs/MVP Version Guideline V0.1`, preserves the 60 FPS / Zero-GC performance budget, and delivers a fun-tested gameplay experience.
