namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Explicit player gameplay stance distinguishing daytime farming/exploration
    /// from nighttime brawl combat encounters.
    /// </summary>
    public enum PlayerStanceMode
    {
        /// <summary>
        /// Daytime agricultural exploration stance: player interacts with farmland,
        /// holds tools selected from hotbar, and explores freely.
        /// </summary>
        Agricultural,

        /// <summary>
        /// Nighttime horde brawl combat stance: player auto-wields equipped weapon
        /// from the equipment sheet, mutes agricultural tools, and focuses on survival combat.
        /// </summary>
        CombatBrawl
    }
}

