using UnityEngine;

namespace FeaturesFarming.Data
{
    /// <summary>
    /// ScriptableObject defining the lifecycle, stages, and yield of a harvestable crop.
    /// Supports multi-speed growth cycles (e.g. Sweet Potato = 2 days, Taro = 4 days).
    /// </summary>
    [CreateAssetMenu(fileName = "Crop_New", menuName = "Farm-Beware/Farming/Crop Data")]
    public class CropData : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier matching the seed or crop name.")]
        public string cropId;

        [Tooltip("Display name of the crop.")]
        public string cropName;

        [Header("Growth Timings (Days)")]
        [Tooltip("Total watered days required for the crop to reach full maturity.")]
        [Min(1)]
        public int totalDaysToHarvest = 2;

        [Tooltip("Array of growth stages and their visual thresholds.")]
        public CropStageVisualDefinition[] growthStages;

        [Header("Harvest Economics")]
        [Tooltip("Clean harvested item added to inventory.")]
        public ItemData cleanYieldItem;

        [Tooltip("Dirty harvested item added to inventory requiring sink washing.")]
        public ItemData dirtyYieldItem;

        [Tooltip("Minimum quantity yielded per harvest.")]
        [Min(1)]
        public int minYield = 1;

        [Tooltip("Maximum quantity yielded per harvest.")]
        [Min(1)]
        public int maxYield = 3;

        [Header("Seed Return Mechanics")]
        [Range(0f, 1f)]
        [Tooltip("Probability of receiving extra seeds upon harvest.")]
        public float seedReturnChance = 0.5f;

        [Tooltip("Seed item returned when extra seed drop triggers.")]
        public ItemData returnedSeedItem;

        /// <summary>
        /// Calculates the current visual stage index based on elapsed watered days.
        /// </summary>
        public int GetStageIndex(int daysGrown)
        {
            if (growthStages == null || growthStages.Length == 0) return 0;
            int stageIndex = 0;
            for (int i = 0; i < growthStages.Length; i++)
            {
                if (daysGrown >= growthStages[i].dayThreshold)
                {
                    stageIndex = i;
                }
            }
            return stageIndex;
        }

        /// <summary>
        /// Checks if the crop has reached full harvestable maturity.
        /// </summary>
        public bool IsMature(int daysGrown) => daysGrown >= totalDaysToHarvest;
    }
}
