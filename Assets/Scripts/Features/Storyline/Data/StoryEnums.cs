namespace FeaturesStoryline.Data
{
    /// <summary>
    /// Trigger points that can activate a story beat.
    /// </summary>
    public enum StoryTriggerType
    {
        GameStart,         // On initial game start / new day 1 load
        Dawn,              // When dawn breaks (EnvironmentPhase.Dawn)
        Dusk,              // At dusk bell / warning (EnvironmentPhase.Dusk)
        WaveStart,         // When a specific combat wave begins
        WaveCleared,       // When all waves for a night are defeated
        BossSpawn,         // When a boss enemy enters the arena
        BossDeath,         // When a boss enemy is slain
        FirstPlant,        // First crop planting of a playthrough or chapter
        FirstHarvest,      // First crop harvest
        Sleep,             // Bed sleep interaction
        MerchantOpen,      // Visiting merchant shop
        Custom             // Custom script programmatic trigger
    }

    /// <summary>
    /// Visual and narrative delivery styles for presenting story beats.
    /// </summary>
    public enum StoryDeliveryType
    {
        HudBanner,         // Non-intrusive upper-center animated HUD notification
        FloatingWorldText, // In-world floating text message
        JournalPage,       // Reveals a journal page and shows notification
        DialogueBox,       // Modal dialogue with character portrait and dialogue text
        BossIntro,         // Cinematic title card and intro camera focus
        FullScreenOverlay, // Cinematic black screen fade with centered lore text (prologue/epilogue)
        DreamSequence      // Dream vignette during night rest
    }
}
