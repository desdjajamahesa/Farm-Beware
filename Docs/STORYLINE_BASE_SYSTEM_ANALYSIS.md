# Farm-Beware: Storyline Base System Deep Analysis & Technical Audit

**Document ID:** DOC-STORY-001  
**Author:** Storyline Documentation Engineer  
**Engine & Pipeline:** Unity 6000.3.20f1 (Unity 6 LTS) | Universal Render Pipeline (URP Deferred+) | Input System 1.19.0  
**Target Subsystems:** `FarmBeware.Features.Storyline`, `FarmBeware.Features.Quest`, `FarmBeware.Core.Runtime`  
**Reference Directives:** `Docs/MVP Version Guideline V0.1`, `Docs/PROJECT_ARCHITECTURE_AND_LORE.md`, `AGENTS.md`, `PROJECT.md`  
**Status:** Official Architectural Baseline  

---

## 1. Executive Summary & Purpose

The purpose of this document is to establish an exhaustive, authoritative technical analysis of the existing **Storyline Base System** within *Farm-Beware*. The game fuses a pastoral agricultural simulation loop with bio-chemical mutagenic horror and high-stakes siege combat across a 5-day escalating campaign.

This analysis evaluates the current implementation across four critical axes:
1. **Architectural Purity & Modularity:** Adherence to the **Pure Logic POCO vs. Thin MonoBehaviour Adapter** paradigm mandated in `AGENTS.md`.
2. **Runtime Performance & Memory Discipline:** Compliance with the **Zero-GC runtime update loop** invariant and the **60 FPS target budget (16.66 ms)** under Unity 6 Deferred+ URP with GPU Resident Drawer.
3. **Narrative & Gameplay Synchronization:** Alignment of story triggers, world state facts, and modal presentation with the canonical parameters in `Docs/MVP Version Guideline V0.1` (3 crops, 6 monster forms, 18-minute day, dusk bell warning at 15:00 / 16:00 in-game, untimed nighttime wave defense).
4. **Subsystem Defect & Gap Identification:** Pinpointing systemic silos, memory leaks, unwired telemetry events, presentation limitations, and persistence gaps.

---

## 2. World Lore Foundation & Narrative Grounding

### 2.1 The 1989 NexaPhyto Bio-Agrochemical Catastrophe
The events of *Farm-Beware* take place on agricultural parcel **Lot 417-B**. In the late 1980s, the land was leased under classified commercial contracts to the **NexaPhyto Bio-Agrochemicals Research Facility**. NexaPhyto engineered experimental agricultural compounds designed to hyper-accelerate organic yield cycles and synthesize military-grade bio-defensive crops.

In the winter of 1989, facility personnel logged **Incident Report #042: Subterranean Tank Rupture**. Tank 04 suffered catastrophic structural failure, releasing more than 10,000 liters of concentrated **Phyto-Toxin Reagent 7** directly into the secondary drainage aquifer beneath the farmland. Rather than undertaking environmental remediation, NexaPhyto executives executed an emergency cover-up: valves were sealed, access shafts were capped with rapid-curing industrial concrete, conduits were buried beneath topsoil, and the property was surrendered to municipal receivership.

### 2.2 Protagonist Profile & The Contractual Trap
The protagonist, **Asep / Ucup ("The Farmer")**, is an honorably discharged military veteran who invested his entire severance savings to purchase Lot 417-B at a municipal land auction, seeking an honest, peaceful agrarian living.

During the purchase, the farmer signed the standard **Environmental Waiver Clause 14**, forfeiting municipal liability regarding:
- Unmapped subterranean conduit networks or buried industrial structural elements.
- Soil, chemical, or biological anomalies stemming from historical commercial leases.

#### Character Voice & Behavioral Pillars
- **Vocabulary:** Terse, grounded, rural phrasing mixed with subtle military tactical terminology (*"perimeter"*, *"breach"*, *"recon"*, *"secure"*).
- **Rhythm:** Staccato, direct statements. Free of melodrama or philosophical monologues.
- **Emotional Stance:** Stoic perseverance under life-threatening adversity. A refusal to surrender what he worked and bled to buy.
- **Negative Invariants:** The farmer never complains about manual labor, never indulges in panic-stricken screaming, and never delivers high-level scientific exposition.

### 2.3 The Catalytic Bio-Chemical Feedback Loop
The central narrative-gameplay dynamic is built upon an ironic mechanical feedback loop: **the player's standard farming chores act as the direct physical catalyst for nighttime mutagenic siege assaults**.

```
┌────────────────────────────────────────────────────────────────────────┐
│                        DAYTIME COZY FARMING CHORES                     │
│                                                                        │
│   [1. Till Arable Plot]  ──► Striking soil cracks corroded pipe lines  │
│              │                                                         │
│              ▼                                                         │
│   [2. Draw Well Water]   ──► Groundwater saturated with chemical scent │
│              │                                                         │
│              ▼                                                         │
│   [3. Water Crops]       ──► Moisture seeps into subterranean bedrock  │
└──────────────────────────────────────┬─────────────────────────────────┘
                                       │
                                       ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   SUBTERRANEAN MUTAGENIC BINDING                       │
│                                                                        │
│   - Soil saturation dissolves dormant Phyto-Toxin Reagent 7 crystals   │
│   - Solubilized reagent binds aggressively to vegetable root systems   │
│   - Uncontrolled cellular hypertrophy and predatory mutation triggered │
└──────────────────────────────────────┬─────────────────────────────────┘
                                       │
                                       ▼
┌────────────────────────────────────────────────────────────────────────┐
│                    DUSK TRANSITION & NIGHTTIME SIEGE                   │
│                                                                        │
│   [16:00 Dusk Bell]      ──► Vines twitch violently in arable topsoil  │
│              │                                                         │
│              ▼                                                         │
│   [18:00 Nightfall]      ──► Mutated crops erupt from ground as beasts │
│              │                                                         │
│              ▼                                                         │
│   [Perimeter Defense]    ──► Sieges converge on cabin until eliminated │
└────────────────────────────────────────────────────────────────────────┘
```

### 2.4 Crop & Enemy Evolution Taxonomy (3 Crops $\rightarrow$ 6 Forms)
In strict alignment with `Docs/MVP Version Guideline V0.1`, the narrative and combat systems enforce a 1:1 correspondence between agricultural crops and mutated nocturnal enemies:

| Crop Seed | Standard Nocturnal Form | Apex Boss Evolution | Biological Mutation Trait | Economic & Combat Function | Monster Material & Upgrade Path |
|---|---|---|---|---|---|
| **Sweet Potato** *(Fast: 6 min)* | **Tuber Maw** | **Cyclops Tuber Maw** *(Specimen C-1)* | Rapid carbohydrate metabolism; high mobility; subterranean burrowing mechanics. | High cycle rate cash crop; food dishes boost stamina and movement velocity. | *Mutated Root*, *Cyclops Eye* $\rightarrow$ **Speed / Mobility Path** (Dash attack, attack speed). |
| **Taro** *(Medium: 15 min)* | **Taro Brute** | **Taro Colossus** *(Specimen T-Colossus)* | Fibrous root cortex absorbs mineral leachate; creates petrified bark armor. | High baseline gold; food dishes boost max HP, defense, and damage mitigation. | *Hardened Root*, *Colossus Core* $\rightarrow$ **Power / Defense Path** (Heavy impact, knockback, armor). |
| **Corn** *(Medium: 15 min)* | **Corn Musketeer** | **The Ranger** *(Specimen Apex)* | Tall stalk sinew and pressurized seed pods form natural ballistic firing chambers. | High-tier recovery & utility food dishes; tactical ranged hazard. | *Kernel Shrapnel*, *Cob Core* $\rightarrow$ **Area / Ballistic Path** (Splash damage, chain hit, crowd control). |

---

## 3. 5-Day Episodic Narrative Progression Architecture

The narrative unfolds across a structured 5-day campaign arc that scales in intensity from pastoral isolation to total bio-containment breach:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                            5-DAY NARRATIVE ESCALATION TIMELINE                                      │
├───────────────┬───────────────────────────────┬───────────────────────────────┬─────────────────────────────────────┤
│ Campaign Day  │ Daytime Narrative Beats       │ Dusk Warning Trigger (16:00)  │ Night Combat Waves & Climax         │
├───────────────┼───────────────────────────────┼───────────────────────────────┼─────────────────────────────────────┤
│ **Day 0**     │ Arrival at Lot 417-B via      │ N/A                           │ Empty night; bed sleep required     │
│ *(Prologue)*  │ truck; deed inspection.       │                               │ to initiate Day 1 dawn.             │
├───────────────┼───────────────────────────────┼───────────────────────────────┼─────────────────────────────────────┤
│ **Day 1**     │ Hoe hits buried steel tag;    │ `D1_DUSK_WARNING`             │ Wave 1: 5 Tuber Maws erupt.         │
│ *(Breakout)*  │ well water smells metallic.   │ Vines twitching in soil plots │ Clear: Dossier 02 (Reagent 7) drop. │
├───────────────┼───────────────────────────────┼───────────────────────────────┼─────────────────────────────────────┤
│ **Day 2**     │ NexaPhyto pipe markings;      │ `D2_DUSK_WARNING`             │ Wave 1-2: Armored Taro Brutes.      │
│ *(Armor)*     │ washing taro clinks like steel│ Armored roots harden in plots │ Clear: Dossier 03 (Incident #042).  │
├───────────────┼───────────────────────────────┼───────────────────────────────┼─────────────────────────────────────┤
│ **Day 3**     │ Seismic ground tremors;       │ `D3_DUSK_WARNING`             │ Wave 1-3: Boss Cyclops Tuber Maw.   │
│ *(Apex C-1)*  │ weapon upgrade at workbench.  │ Well water foams with gas     │ Clear: Dossier 04 (Apex Biology).   │
├───────────────┼───────────────────────────────┼───────────────────────────────┼─────────────────────────────────────┤
│ **Day 4**     │ Toxic vapor hissing from soil;│ `D4_DUSK_WARNING`             │ Wave 1-4: Boss Taro Colossus.       │
│ *(Colossus)*  │ traveling merchant warning.   │ Deep subterranean groaning    │ Clear: Dossier 05 (Valve Blueprint).│
├───────────────┼───────────────────────────────┼───────────────────────────────┼─────────────────────────────────────┤
│ **Day 5**     │ Crimson sky atmosphere shift; │ `D5_DUSK_DISTORTED`           │ Wave 1-5: Dual Boss Climax (Taro    │
│ *(Climax)*    │ final defense preparation.    │ Subterranean sirens wail      │ Colossus + The Ranger). Clear Purge.│
└───────────────┴───────────────────────────────┴───────────────────────────────┴─────────────────────────────────────┘
```

### Detailed Story Beat Specification
1. **Day 0 (Prologue):**
   - Beat: `PROLOGUE` / `CUTSCENE_DAY0`. Delivered via Timeline cutscene.
   - Narrative: Pickup truck arrives at the homestead gate. The farmer surveys the desolate fields and enters the cabin.
   - Gameplay Contract: Starter inventory cleared; tools locked; sleep in bed advances time to Day 1 Dawn.
2. **Day 1 (Arrival & The First Till):**
   - Dawn Beat: `D1_MANIFEST_FOUND`. The farmer wakes up, examines the land deed, and discovers Clause 14.
   - Exploration Beat: `D1_FIRST_TILL`. Striking the soil with a hoe produces a loud metallic clang, camera shake, and reveals a corroded tag: *"NexaPhyto Tank 04 - REAGENT 7"*.
   - Well Beat: `D1_WELL_WATER_100`. Drawing 100L of well water reveals a strange chemical stench.
   - Dusk Beat: `D1_DUSK_WARNING`. At 16:00 in-game time (Minute 15:00 real time), the Dusk Bell tolls (`DING DONG`), amber lighting blankets the farm, and root vines twitch violently.
   - Night Combat: 1 Wave (5 Tuber Maws). Defeating them triggers `D1_NIGHT_CLEARED`, unlocks `JOURNAL_02_REAGENT7`, and elevates `NexaPhytoTruthLevel` to 3.
   - Sleep Transition: `DREAM_01_WELL`. Vignette of a military shovel hitting a landmine, dissolving into a hoe striking factory pipework.
3. **Day 2 (Armored Incursion):**
   - Dawn Beat: `D2_DAWN_PIPES`. Exposed bedrock reveals NexaPhyto drainage conduits.
   - Sink Washing: `D2_SINK_WASH_TARO`. Washing harvested taro in the kitchen sink clinks with the resonance of hardened armor plate.
   - Night Combat: 2 Waves (4 + 5 enemies) introducing heavy armored Taro Brutes. Defeating them reveals `JOURNAL_03_INCIDENT042`.
4. **Day 3 (Crossfire & First Apex Specimen):**
   - Dawn Beat: `D3_DAWN_PULSE`. Ground tremors pulse beneath the stone well. Corn seeds are unlocked.
   - Workbench Crafting: `D3_WORKBENCH_UPGRADE`. The farmer utilizes monster materials to harden weapon components.
   - Night Combat: 3 Waves (4 + 5 + 6 enemies). Wave 3 introduces **Cyclops Tuber Maw (Specimen C-1)** with cinematic letterbox intro and tracking beam. Clearing unlocks `JOURNAL_04_APEX_BIO`.
5. **Day 4 (Subterranean Rupture):**
   - Dawn Beat: `D4_DAWN_TREMORS`. Farmland fissures hiss green chemical gas.
   - Merchant Beat: `D4_MERCHANT_FLAVOR`. The traveling merchant warns of glowing soil and offers combat preparation supplies.
   - Night Combat: 4 Waves culminating in **Taro Colossus (Specimen T-Colossus)**. Clearing exposes `JOURNAL_05_FAILSAFE`, revealing the existence of Subterranean Emergency Relief Valve #09.
6. **Day 5 (The Climax & Chemical Purge):**
   - Dawn Beat: `D5_DAWN_REDSKY`. Escaping chemical vapor turns `RenderSettings.ambientLight` dark crimson.
   - Dusk Beat: `D5_DUSK_DISTORTED`. Distorted industrial sirens echo from underground.
   - Night Combat: 5 Waves. Wave 4 unleashes a second Taro Colossus; Wave 5 summons the ultimate bio-sentinel **The Ranger (Specimen Apex)** choking Valve #09.
   - Climax Resolution: `D5_VICTORY`. The Ranger falls; roots wither and release Emergency Valve #09. Neutralizing vapor purges through the underground pipes, crystallizing the toxic slime into inert agricultural fertilizer.
   - Epilogue: `EPILOGUE_STATS`. Fullscreen overlay summarizes final farm statistics, victory tallies, and reflections on earning peace.

---

## 4. Current Base System Architecture & Implementation

The storyline architecture spans three primary assemblies:
- `FarmBeware.Core.Runtime` (Interfaces, Blackboard, Telemetry)
- `FarmBeware.Features.Storyline` (Engine, Database, Adapters, UI)
- `FarmBeware.Features.Quest` (Onboarding Quest Model, Quest HUD)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      PURE LOGIC LAYER (Zero-GC, POCO)                       │
│                                                                             │
│  ┌────────────────────────┐                   ┌──────────────────────────┐  │
│  │    StorylineEngine     │                   │    QuestProgressModel    │  │
│  │  - QBN Salience Rule   │                   │  - First Steps Logic     │  │
│  │    Evaluator           │                   │  - Stage Progression     │  │
│  │  - RepeatPolicy Logic  │                   │  - Rich Text Formatter   │  │
│  └───────────┬────────────┘                   └────────────┬─────────────┘  │
│              │                                             │                │
│              ▼                                             │                │
│  ┌───────────────────────────────────────────────────────┐ │                │
│  │      NarrativeFactBoard (int[128] Contiguous Array)   │ │                │
│  │  - Zero-GC Condition Checking (ReadOnlySpan<T>)       │ │                │
│  │  - Atomic Fact Mutations (Set, Add, Max)              │ │                │
│  └───────────────────────────────────────────────────────┘ │                │
└──────────────────────────────────────┬─────────────────────┼────────────────┘
                                       │                     │
                                       ▼                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                     THIN ADAPTER MONOBEHAVIOUR LAYER                        │
│                                                                             │
│  ┌────────────────────────┐                   ┌──────────────────────────┐  │
│  │    StorylineManager    │                   │       QuestManager       │  │
│  │  - Subscribes to events│                   │  - Subscribes to farm/inv│  │
│  │  - Enforces Safety Gate│                   │  - Updates Model stages  │  │
│  │  - Delivers Beat UI    │                   │  - [Defect: Polling]     │  │
│  └───────────┬────────────┘                   └────────────┬─────────────┘  │
└──────────────┼─────────────────────────────────────────────┼────────────────┘
               │                                             │
               ▼                                             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION & UI LAYER                             │
│                                                                             │
│  ┌────────────────────────┐                   ┌──────────────────────────┐  │
│  │      StorylineUI       │                   │        QuestHUDUI        │  │
│  │  - HudBanner           │                   │  - Side Quest Badge      │  │
│  │  - DialogueBox (Static)│                   │  - Objective Checklist   │  │
│  │  - FullScreenOverlay   │                   │  - [CRITICAL DEFECT:     │  │
│  │  - DreamSequence       │                   │     Allocates in Update] │  │
│  └────────────────────────┘                   └──────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.1 Pure Logic Engine: `StorylineEngine.cs`
The core narrative evaluator is a pure C# POCO class with zero `UnityEngine` lifecycle dependencies.
- **Salience Scoring Formula:** Evaluates candidate story beats using Emily Short's Quality-Based Narrative (QBN) card model:
  $$\text{Score}(R_i) = (P_{\text{base}} \times 100) + \text{specificityScore}$$
  Where $P_{\text{base}}$ is `saliencePriority` (0 to 10) and `specificityScore` is the count of matching criteria satisfied on the blackboard.
- **Repeat Policies:**
  - `MatchOnce`: Executes exactly once across the entire playthrough.
  - `OncePerDay`: Can trigger at most once per in-game day.
  - `CooldownDays`: Requires $N$ in-game days to elapse before re-triggering.
  - `AlwaysRepeatable`: Fires whenever criteria match.
- **Zero-GC Candidate Filtering:** Iterates over pre-allocated candidate lists without LINQ or heap allocations.

### 4.2 Narrative Blackboard: `NarrativeFactBoard.cs`
- Allocates a single, contiguous array: `private readonly int[] _facts = new int[128]`.
- Indexed by `NarrativeFactKey` (byte enum).
- Evaluates conditions via `ReadOnlySpan<NarrativeFactCondition>`, performing direct primitive integer comparisons in CPU registers.
- Zero boxing, zero dictionary overhead, zero runtime heap allocations.

### 4.3 Thin Adapter: `StorylineManager.cs`
- Component with `[DefaultExecutionOrder(-800)]`.
- Subscribes to engine events: `DayNightTimeManager`, `NightBrawlManager`, `EnemyBase`, `FarmlandTile`, `WaterWellInteractable`, `KitchenSink`, and `NarrativeTelemetry`.
- **The Hard Modal Safety Invariant:**
  ```csharp
  // StorylineManager.cs line 324
  if (beat.deliveryType == StoryDeliveryType.DialogueBox || 
      beat.deliveryType == StoryDeliveryType.JournalPage ||
      beat.deliveryType == StoryDeliveryType.FullScreenOverlay)
  {
      if (_factBoard.GetFact(NarrativeFactKey.ActiveCombatThreatCount) > 0)
      {
          // HARD INVARIANT: Defer modal during active combat!
          _pendingModalBeats.Enqueue(beat.beatId);
          _storylineUI.ShowBanner("Archival Log Discovered", "Read safely after combat.");
          return;
      }
  }
  ```
  Ensures the player's movement, dodge, and attack capabilities are never locked while enemies are threatening or attacking. When all threats reach zero (`ActiveCombatThreatCount == 0`), pending modals are safely flushed one by one.

### 4.4 Quest Subsystem: `QuestProgressModel.cs` & `QuestManager.cs`
- `QuestProgressModel`: Evaluates inventory for starter tools (Hoe, Plant Waterer, Water Bottle, Sweet Potato Seed) and tracks planting and watering stages for the "First Steps" tutorial.
- `QuestManager`: Thin adapter that updates the model based on player actions.

---

## 5. Exhaustive Defect & Gap Analysis

Despite its strong architectural foundation, a deep audit of the codebase reveals six major defects and architectural gaps:

### Defect 1: Critical Heap Allocation Leak in `QuestHUDUI.cs`
- **Location:** `Assets/Scripts/Features/Quest/UI/QuestHUDUI.cs` (lines 81–97, 143, 151)
- **Code Observation:**
  ```csharp
  private void Update()
  {
      if (QuestManager.Instance != null && QuestManager.Instance.Model != null)
      {
          UpdateDisplayFromModel(QuestManager.Instance.Model);
      }
  }

  public void UpdateDisplayFromModel(QuestProgressModel model)
  {
      // Line 143: String interpolation executed EVERY frame
      questHeaderBadgeText.text = $"• SIDE QUEST {statusTag}";
      // Line 151: Multi-line rich text concatenation executed EVERY frame
      questDetailText.text = model.GetFormattedObjectiveDetails();
  }
  ```
- **Technical Impact:**
  - `model.GetFormattedObjectiveDetails()` performs 4 string concatenations and 4 ternary formatting operations on every call.
  - At 60 FPS, this generates **10 to 20 string allocations per frame**, totaling **600 to 1,200 heap allocations per second** (~720 KB of ephemeral memory per minute).
  - This constant allocation stream triggers frequent Garbage Collector (GC) collections, producing micro-stutters and violating `AGENTS.md` Invariant 2.3 & 3.4.
- **Root Cause:** Polling-based UI update pattern instead of an event-driven observer pattern.

### Defect 2: Disconnected Storyline and Quest Systems (Siloed Architecture)
- **Observation:**
  - `FarmBeware.Features.Quest` and `FarmBeware.Features.Storyline` reside in completely separate assemblies.
  - `QuestProgressModel` is hardcoded to a single quest (`sidequest_first_steps`).
  - `StorylineEngine` has no mechanism to assign, track, or complete gameplay objectives.
- **Technical Impact:**
  - Designers cannot author daily storyline objectives that appear on the HUD (e.g. Day 2: *"Till 4 plots and plant Taro"*, Day 3: *"Craft a weapon upgrade at Workbench before Dusk"*, Day 4: *"Cook 3 Battle Dishes for Night Siege"*).
  - The storyline feels like an external broadcast of text popups rather than an active driver of player gameplay objectives.

### Defect 3: Static Dialogue Presentation (No Typewriter, No Choices, No Audio Chits)
- **Observation:** In `StorylineUI.cs` (lines 327–330):
  ```csharp
  if (dialogueSpeakerText != null)
      dialogueSpeakerText.text = string.IsNullOrEmpty(speaker) ? "Ucup" : speaker;
  if (dialogueBodyText != null)
      dialogueBodyText.text = message;
  ```
- **Technical Impact:**
  - Full dialogue paragraphs appear instantly in a single frame.
  - Zero progressive character reveal (typewriter effect).
  - Zero branching dialogue options or player response choices.
  - Zero procedural audio blips during dialogue reveal.
  - Dialogues feel like debug notifications rather than an immersive narrative experience.

### Defect 4: Dangling Telemetry & Unwired Blackboard Facts
- **Observation:** Several triggers defined in `StoryEnums.cs` and keys in `NarrativeFactKey` are completely uninvoked in gameplay code:
  - `StoryTriggerType.MerchantOpen` is checked in beat `D4_MERCHANT_FLAVOR`, but `MerchantShopUI` never reports when the shop opens.
  - `StoryTriggerType.TrophyInspected` is defined, but `TrophyCabinetInteractable` never invokes it.
  - `StoryTriggerType.DuskApproaching` is defined, but `DayNightTimeManager` never reports it.
  - `NarrativeFactKey.MealsCookedCount` (key 10) is never updated when cooking completes in `StoveUIManager.cs`.
  - `NarrativeFactKey.DeadCropsCount` (key 19) is never updated when crops wither.
  - `NarrativeFactKey.PlayerLowHealthTriggered` (key 15) is never updated by `PlayerStats.cs`.
  - `NarrativeFactKey.TheRangerSlayed` (key 20) is never set when The Ranger is defeated in `StorylineManager.cs:406`.
- **Technical Impact:** Any story rule relying on these triggers or facts fails silently during gameplay.

### Defect 5: Quest Progress Omitted from Save Game Persistence
- **Observation:** `SaveSystemManager.cs` serializes player inventory, stats, farmland tiles, chest items, time, and storyline facts. However, `QuestManager` and `QuestProgressModel` are completely absent from `GameSaveData`.
- **Technical Impact:** If the player saves on Day 1 while midway through the "First Steps" quest, quest stage data and step progress are discarded upon reloading.

### Defect 6: Scene Polling & Hierarchy Traversal Fallbacks
- **Observation:**
  - `QuestManager.Update()` polls `RefreshInventoryEvaluation()` every single frame while on stage `GatheringSupplies`.
  - `StorylineUI.EnsureUIHierarchy()` and `QuestHUDUI.EnsureUIHierarchy()` call `FindFirstObjectByType<Canvas>()` and `GameObject.Find("UI_Canvas")`.
- **Technical Impact:** Introduces unnecessary CPU overhead and violates the event-driven architecture directive.

### Defect 7: Absence of Dedicated Playable Prototype Sandbox Scene
- **Observation:** The project only contains `MainMenuScene.unity` and the monolithic `StagingScene.unity`.
- **Technical Impact:** Validating storyline pacing, dialogue, and night waves requires loading the full 16-plot production scene with heavy lighting and full compound colliders, severely hindering rapid iteration and isolated performance profiling.

---

## 6. Performance Budget & Memory Invariants

### 6.1 Target 60 FPS Frame Budget Breakdown (16.66 ms)
Under Unity 6 Universal Render Pipeline with Deferred+ lighting and GPU Resident Drawer:

```
Total Frame Budget: 16.66 ms (60 FPS)
┌──────────────────────┬──────────────────────┬────────────────────────────┬────────────────────────────┐
│ CPU Logic: ≤ 3.50 ms │ CPU Submission: ≤ 2ms │ GPU Deferred: ≤ 8.00 ms   │ Post & UI: ≤ 3.00 ms       │
├──────────────────────┼──────────────────────┼────────────────────────────┼────────────────────────────┤
│ • Player locomotion  │ • BRG draw batches   │ • G-Buffer depth/normals   │ • Color Grading & Vignette │
│ • AI Navigation      │ • Clustered light    │ • Clustered point lights   │ • Canvas mesh draw (uGUI)  │
│ • Storyline events   │   culling            │ • Screen-space shadows     │ • Typewriter maxChars      │
│ • Soil growth ticks  │ • Shadow pass prep   │ • Terrain & prop geometry  │ • Quest HUD badge          │
└──────────────────────┴──────────────────────┴────────────────────────────┴────────────────────────────┘
```

### 6.2 Zero-GC Memory Standard
- **Blackboard Footprint:** `NarrativeFactBoard` maintains exactly $128 \times 4\text{ bytes} = 512\text{ bytes}$ of pre-allocated contiguous heap memory for the entire life of the application.
- **Rule Evaluation:** `StorylineEngine.EvaluateTrigger` evaluates candidate rules using stack-allocated `ReadOnlySpan<NarrativeFactCondition>`. Total heap allocation per evaluation = **0 Bytes**.
- **Typewriter Effect Requirement:** Must utilize `TextMeshProUGUI.maxVisibleCharacters` property manipulation rather than substring generation, ensuring **0 Bytes** heap allocation per character reveal.

---

## 7. Architectural Invariants & Boundary Constraints

Any subsequent upgrade or implementation must strictly preserve the following architectural boundaries:

1. **Pure POCO Decoupling:** Narrative evaluation logic, specificity math, fact array management, and quest models must remain in pure C# classes with zero `MonoBehaviour` inheritance.
2. **Hard Modal Combat Safety Invariant:** Under no circumstances may a modal dialogue box, journal document, or input-locking UI window open while `ActiveCombatThreatCount > 0`.
3. **No Mid-Combat Popups:** Story beats triggered during night combat must be enqueued into `_pendingModalBeats` and displayed only after all threats are neutralized.
4. **Zero Contradiction with `Docs/MVP Version Guideline V0.1`:**
   - Exactly 3 crops: Sweet Potato, Taro, Corn.
   - Exactly 6 monster forms: 3 base + 3 apex boss evolutions.
   - 1 base weapon with 3 material-driven upgrade paths.
   - 18-minute day phase; Dusk Bell warning at 15:00 real time (16:00 in-game).
   - Untimed night waves (survival by elimination).
   - Nocturnal crop growth strictly frozen.
   - 100L water bottle; well refills max 2 times per day.
   - Max 3 food slots carried into night combat.

---

## 8. Conclusion

The existing Storyline Base System in *Farm-Beware* provides a mathematically sound, high-performance foundation built on pure POCO state machines and Zero-GC blackboard facts. 

However, critical runtime memory leaks in `QuestHUDUI.cs`, isolation between storyline and quest tracking, static dialogue presentation, unwired gameplay telemetry, and the lack of a dedicated sandbox scene prevent it from delivering an engaging, fully playable, and fun-tested experience.

The accompanying document, `Docs/STORYLINE_BASE_SYSTEM_UPGRADE_PLAN.md`, specifies the comprehensive, production-grade engineering blueprint to resolve these defects and deliver the complete playable storyline loop.
