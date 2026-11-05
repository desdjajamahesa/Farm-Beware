using System;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure POCO state machine evaluating player stance transitions (Agricultural vs CombatBrawl),
    /// hotbar index persistence across night encounters, and MVP tool/consumable restrictions.
    /// Zero UnityEngine dependencies and Zero GC allocations per tick.
    /// </summary>
    public class PlayerStanceContext
    {
        public PlayerStanceMode CurrentStance { get; private set; } = PlayerStanceMode.Agricultural;
        public int SavedAgriculturalHotbarIndex { get; private set; } = 0;
        public bool HasSavedHotbarIndex { get; private set; } = false;

        public event Action<PlayerStanceMode, PlayerStanceMode> OnStanceChanged;

        /// <summary>
        /// Evaluates the target gameplay stance for a given environmental time phase.
        /// Night maps strictly to CombatBrawl; Dawn, Day, and Dusk map to Agricultural.
        /// </summary>
        public static PlayerStanceMode EvaluateStanceForPhase(DayPhase phase)
        {
            return (phase == DayPhase.Night) ? PlayerStanceMode.CombatBrawl : PlayerStanceMode.Agricultural;
        }

        /// <summary>
        /// Transitions the internal stance state based on an incoming time phase.
        /// Automatically captures the agricultural hotbar index when entering Night.
        /// </summary>
        public bool TransitionToPhase(DayPhase phase, int currentHotbarIndex)
        {
            PlayerStanceMode targetStance = EvaluateStanceForPhase(phase);
            return SetStance(targetStance, currentHotbarIndex);
        }

        /// <summary>
        /// Explicitly transitions the player stance, updating the saved hotbar memory if entering combat.
        /// </summary>
        public bool SetStance(PlayerStanceMode targetStance, int currentHotbarIndex)
        {
            if (CurrentStance == targetStance) return false;

            PlayerStanceMode previous = CurrentStance;

            if (targetStance == PlayerStanceMode.CombatBrawl)
            {
                SavedAgriculturalHotbarIndex = Math.Max(0, currentHotbarIndex);
                HasSavedHotbarIndex = true;
            }

            CurrentStance = targetStance;
            OnStanceChanged?.Invoke(previous, targetStance);
            return true;
        }

        /// <summary>
        /// Returns the saved hotbar slot index to restore upon returning to Agricultural stance.
        /// </summary>
        public int GetRestoredHotbarIndex(int defaultIndex = 0)
        {
            return HasSavedHotbarIndex ? SavedAgriculturalHotbarIndex : defaultIndex;
        }

        /// <summary>
        /// Checks whether a tool is strictly forbidden at night per GDD guidelines ("Hoe unavailable at night").
        /// </summary>
        public static bool IsToolForbiddenAtNight(string itemId, bool isHoe, bool isWateringCan)
        {
            if (isHoe || isWateringCan) return true;
            if (string.IsNullOrEmpty(itemId)) return false;

            string lower = itemId.ToLowerInvariant();
            return lower.Contains("hoe") || lower.Contains("waterer") || lower.Contains("watering");
        }

        /// <summary>
        /// Evaluates whether an item action (use/select) from the hotbar is permitted in the current stance.
        /// In Agricultural stance: all items are permitted.
        /// In CombatBrawl stance: consumables (food buffs/potions) are allowed; farming tools are rejected.
        /// </summary>
        public static bool CanUseHotbarItem(PlayerStanceMode stance, bool isConsumable, string itemId, bool isHoe, bool isWateringCan)
        {
            if (stance == PlayerStanceMode.Agricultural) return true;

            // In CombatBrawl stance at night:
            // Consumables (Food buffs, hydration sips) are strictly permitted (GDD: "Only 3 food slots into night")
            if (isConsumable) return true;

            // Farming tools are strictly prohibited during night brawl
            if (IsToolForbiddenAtNight(itemId, isHoe, isWateringCan)) return false;

            // Non-combat, non-consumable items are ignored/muted during brawl
            return false;
        }
    }
}

