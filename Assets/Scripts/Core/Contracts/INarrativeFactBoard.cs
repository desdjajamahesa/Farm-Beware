using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Enumeration of numerical fact keys tracked in the Zero-GC Narrative Fact Blackboard.
    /// Values are stored as integer or fixed-point representations in a contiguous int[128] array.
    /// Enables cross-assembly narrative state tracking without coupling or garbage collection.
    /// </summary>
    public enum NarrativeFactKey : byte
    {
        CurrentDay = 0,
        CurrentDayPhase = 1,                 // 0 = Dawn, 1 = Day, 2 = Dusk, 3 = Night
        PlayerInHouseSafeZone = 2,           // 0 = Outdoor, 1 = Inside Safe House
        ActiveCombatThreatCount = 3,         // Active monster count in arena
        TotalWellWaterDrawnLiters = 4,       // Cumulative liters drawn from stone well
        WaterBottleCurrentLiters = 5,        // Current water bottle volume (0-100L)
        PlantedCount_SweetPotato = 6,        // Active sweet potato crops in ground
        PlantedCount_Taro = 7,               // Active taro crops in ground
        PlantedCount_Corn = 8,               // Active corn crops in ground
        DirtyCropsWashedCount = 9,           // Cumulative dirty crops cleansed in sink
        MealsCookedCount = 10,               // Cumulative dishes cooked at stove
        WeaponUpgradeTier_Speed = 11,        // Sweet Potato / Mobility weapon upgrade tier (0-3)
        WeaponUpgradeTier_Power = 12,        // Taro / Heavy Armor weapon upgrade tier (0-3)
        WeaponUpgradeTier_Ballistic = 13,    // Corn / Area weapon upgrade tier (0-3)
        TrophyUnlockedBitmask = 14,          // Bitmask of slain bosses (1=C-1, 2=Colossus, 4=Ranger)
        PlayerLowHealthTriggered = 15,       // Adrenaline trigger state (0 or 1)
        NexaPhytoTruthLevel = 16,            // Narrative knowledge progression (0-10)
        IntroCutsceneCompleted = 17,         // Whether the opening timeline cutscene was completed/skipped (0 or 1)
        NexaPhytoCorruptionLevel = 18,       // Environmental corruption percentage (0-100)
        DeadCropsCount = 19,                 // Cumulative withered / dead crops
        TheRangerSlayed = 20,                // Final boss defeat state (0 or 1)
        MaxCount = 128
    }

    /// <summary>
    /// Relational operator for testing a fact key against a target value.
    /// </summary>
    public enum FactOperator : byte
    {
        Equal = 0,
        GreaterOrEqual = 1,
        LessOrEqual = 2,
        NotEqual = 3
    }

    /// <summary>
    /// Mutation operation applied to a fact key upon storylet execution.
    /// </summary>
    public enum FactMutationOp : byte
    {
        Set = 0,
        Add = 1,
        Max = 2
    }

    /// <summary>
    /// Blittable condition criteria struct evaluating a specific fact key against a target value.
    /// </summary>
    [Serializable]
    public struct NarrativeFactCondition
    {
        public NarrativeFactKey key;
        public FactOperator op;
        public int targetValue;
        public int scoreWeight;
        public bool isRequired;

        public NarrativeFactCondition(NarrativeFactKey key, FactOperator op, int targetValue, int scoreWeight = 10, bool isRequired = true)
        {
            this.key = key;
            this.op = op;
            this.targetValue = targetValue;
            this.scoreWeight = scoreWeight;
            this.isRequired = isRequired;
        }
    }

    /// <summary>
    /// Blittable mutation struct specifying a state change to apply to a fact key.
    /// </summary>
    [Serializable]
    public struct NarrativeFactMutation
    {
        public NarrativeFactKey key;
        public FactMutationOp op;
        public int operandValue;

        public NarrativeFactMutation(NarrativeFactKey key, FactMutationOp op, int operandValue)
        {
            this.key = key;
            this.op = op;
            this.operandValue = operandValue;
        }
    }

    /// <summary>
    /// Service contract for the Zero-GC Narrative Fact Blackboard.
    /// Stores world state in a pre-allocated contiguous array without heap allocations or boxing.
    /// </summary>
    public interface INarrativeFactBoard
    {
        /// <summary>Gets the integer value for a given fact key.</summary>
        int GetFact(NarrativeFactKey key);

        /// <summary>Sets the integer value for a given fact key.</summary>
        void SetFact(NarrativeFactKey key, int value);

        /// <summary>Adds a signed delta to a given fact key.</summary>
        void AddFact(NarrativeFactKey key, int delta);

        /// <summary>Sets the fact key to the maximum of its current value and candidate.</summary>
        void MaxFact(NarrativeFactKey key, int candidate);

        /// <summary>Evaluates an array or span of conditions, outputting specificity score. Zero-GC.</summary>
        bool EvaluateConditions(ReadOnlySpan<NarrativeFactCondition> conditions, out int specificityScore);

        /// <summary>Applies a state mutation to a fact key.</summary>
        void ApplyMutation(NarrativeFactMutation mutation);

        /// <summary>Copies raw facts into a destination array for serialization.</summary>
        void CopyRawFacts(int[] destination);

        /// <summary>Loads raw facts from a source array during save loading.</summary>
        void LoadRawFacts(int[] source);

        /// <summary>Resets all fact keys back to zero for fresh session initialization.</summary>
        void ResetFacts();

        /// <summary>Event raised when any fact key changes value.</summary>
        event Action<NarrativeFactKey, int> OnFactChanged;
    }
}
