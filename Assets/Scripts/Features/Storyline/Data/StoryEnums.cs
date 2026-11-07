namespace FeaturesStoryline.Data
{
    /// <summary>
    /// Trigger points that can activate a story beat or storylet rule evaluation.
    /// </summary>
    public enum StoryTriggerType
    {
        GameStart,                 // On initial game start / new day 1 load
        Dawn,                      // When dawn breaks (EnvironmentPhase.Dawn)
        Dusk,                      // At dusk bell / warning (EnvironmentPhase.Dusk)
        DuskApproaching,           // Warning phase before dusk
        WaveStart,                 // When a specific combat wave begins
        WaveCleared,               // When all waves for a night are defeated
        BossSpawn,                 // When a boss enemy enters the arena
        BossDeath,                 // When a boss enemy is slain
        FirstPlant,                // First crop planting of a playthrough or chapter
        FirstHarvest,              // First crop harvest
        Sleep,                     // Bed sleep interaction
        MerchantOpen,              // Visiting merchant shop
        WellWaterRefilled,         // Drawing water from the stone well
        CropHarvested,             // Harvesting mature crop from tile
        CropWashedInSink,          // Washing dirty crop in the kitchen sink
        WeaponUpgradedAtBench,     // Forging a weapon upgrade at workbench
        TrophyInspected,           // Inspecting boss trophy at living room cabinet
        Custom,                    // Custom script programmatic trigger
        FirstTill                  // First soil tilled with a hoe into arable farmland
    }

    /// <summary>
    /// Visual and narrative delivery styles for presenting story beats.
    /// </summary>
    public enum StoryDeliveryType
    {
        HudBanner,         // Non-intrusive upper-center animated HUD notification (Zero input lock)
        FloatingWorldText, // In-world floating text message (Zero input lock)
        JournalPage,       // Reveals a journal page and shows notification
        DialogueBox,       // Modal dialogue with character portrait and dialogue text (ModalStack managed)
        BossIntro,         // Cinematic title card and intro camera focus (Cutscene mode)
        FullScreenOverlay, // Cinematic black screen fade with centered lore text (prologue/epilogue)
        DreamSequence,     // Dream vignette during night rest transition
        TimelineCutscene   // Unity Timeline PlayableDirector cinematic cutscene
    }

    /// <summary>
    /// Life-cycle consumption and repeating policy for storylet rules.
    /// Aligned with Emily Short's Quality-Based Narrative model.
    /// </summary>
    public enum RepeatPolicy : byte
    {
        MatchOnce = 0,         // Vanishing card: executes once per campaign run
        OncePerDay = 1,        // Repeating card: can execute at most once per in-game day
        CooldownDays = 2,      // Cooling-down card: requires N days before re-eligibility
        AlwaysRepeatable = 3   // Standard card: continuously eligible whenever criteria are met
    }
}
