using System;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# POCO evaluator governing the voluntary "Bio-Catalyst Night Overdrive" push-your-luck mechanic.
    /// Preserves all default MVP Version Guideline V0.1 rules when inactive, while unlocking nocturnal crop growth,
    /// increased monster speed/wave density, and elevated loot/gold payouts when activated.
    /// Zero-GC allocation.
    /// </summary>
    public static class NightOverdriveEvaluator
    {
        public const int RequiredCatalystMaterialCount = 2;
        public const string DefaultCatalystMaterialId = "mat_mutated_root";
        public const float OverdriveEnemySpeedMultiplier = 1.15f;
        public const float OverdriveLootDropBonusChance = 0.30f;
        public const float OverdriveGoldMultiplier = 1.25f;
        public const float NocturnalCropGrowthMultiplier = 0.50f;

        /// <summary>
        /// Checks whether Night Overdrive can be activated given the current state and held catalyst materials.
        /// </summary>
        public static bool CanActivateOverdrive(bool isAlreadyActive, int availableMaterialCount)
        {
            if (isAlreadyActive) return false;
            return availableMaterialCount >= RequiredCatalystMaterialCount;
        }

        /// <summary>
        /// Evaluates crop growth timer progression during Night Phase.
        /// Returns 0f on standard nights (honoring MVP Guideline "Crop growth: Only counts during the Day phase"),
        /// and 50% growth speed during an active Overdrive night.
        /// </summary>
        public static float EvaluateCropNightGrowthDelta(float deltaTime, float fastForwardMultiplier, bool isOverdriveActive)
        {
            if (!isOverdriveActive || deltaTime <= 0f)
            {
                return 0f;
            }

            float mult = fastForwardMultiplier > 0f ? fastForwardMultiplier : 1.0f;
            return deltaTime * mult * NocturnalCropGrowthMultiplier;
        }

        /// <summary>
        /// Calculates the gold drop amount from defeated monsters, applying +25% bonus during Overdrive.
        /// </summary>
        public static int CalculateOverdriveGold(int baseGold, bool isOverdriveActive)
        {
            if (baseGold <= 0) return 0;
            if (!isOverdriveActive) return baseGold;
            return (int)MathF.Round(baseGold * OverdriveGoldMultiplier);
        }

        /// <summary>
        /// Calculates the effective material drop chance, adding +30% flat bonus during Overdrive.
        /// </summary>
        public static float CalculateOverdriveDropChance(float baseChance, bool isOverdriveActive)
        {
            if (!isOverdriveActive) return Math.Clamp(baseChance, 0f, 1f);
            return Math.Clamp(baseChance + OverdriveLootDropBonusChance, 0f, 1f);
        }

        /// <summary>
        /// Returns the enemy movement speed multiplier for the current night state.
        /// </summary>
        public static float GetEnemySpeedMultiplier(bool isOverdriveActive)
        {
            return isOverdriveActive ? OverdriveEnemySpeedMultiplier : 1.0f;
        }
    }
}

