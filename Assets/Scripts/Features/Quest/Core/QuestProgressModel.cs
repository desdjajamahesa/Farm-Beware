using System;
using System.Collections.Generic;

namespace FarmBeware.Logic.Quests
{
    public enum QuestStage
    {
        GatheringSupplies = 0,
        PlantingCrop = 1,
        WateringCrop = 2,
        Completed = 3
    }

    /// <summary>
    /// Pure C# POCO model representing the First Steps onboarding side quest.
    /// Manages requirement evaluation with zero Unity lifecycle dependencies and zero heap allocations during evaluation.
    /// </summary>
    public class QuestProgressModel
    {
        public const string QuestId = "sidequest_first_steps";
        public const string QuestTitle = "First Steps";

        public const string ItemIdHoe = "tool_hoe";
        public const string ItemIdPlantWaterer = "tool_plant_waterer";
        public const string ItemIdWaterBottle = "food_bottle_water";
        public const string ItemIdSweetPotatoSeed = "seed_sweet_potato";

        public QuestStage CurrentStage { get; private set; } = QuestStage.GatheringSupplies;

        public bool HasHoe { get; private set; }
        public bool HasPlantWaterer { get; private set; }
        public bool HasWaterBottle { get; private set; }
        public bool HasSweetPotatoSeed { get; private set; }

        public bool HasPlantedCrop { get; private set; }
        public bool HasWateredCrop { get; private set; }

        public int PlantedCount { get; private set; } = 0;
        public int WateredCount { get; private set; } = 0;

        public bool IsAllSuppliesGathered => HasHoe && HasPlantWaterer && HasWaterBottle && HasSweetPotatoSeed;
        public bool IsCompleted => CurrentStage == QuestStage.Completed;

        public event Action OnStateChanged;

        /// <summary>
        /// Evaluates the player's current item collection against the required starter tools.
        /// </summary>
        public bool EvaluateInventory(IEnumerable<string> itemIds)
        {
            if (CurrentStage == QuestStage.Completed) return false;

            bool hadHoe = HasHoe;
            bool hadWaterer = HasPlantWaterer;
            bool hadBottle = HasWaterBottle;
            bool hadSeed = HasSweetPotatoSeed;
            QuestStage previousStage = CurrentStage;

            HasHoe = false;
            HasPlantWaterer = false;
            HasWaterBottle = false;
            HasSweetPotatoSeed = false;

            if (itemIds != null)
            {
                foreach (var rawId in itemIds)
                {
                    if (string.IsNullOrEmpty(rawId)) continue;
                    string id = rawId.Trim();

                    if (IsHoeMatch(id))
                    {
                        HasHoe = true;
                    }
                    else if (IsPlantWatererMatch(id))
                    {
                        HasPlantWaterer = true;
                    }
                    else if (IsWaterBottleMatch(id))
                    {
                        HasWaterBottle = true;
                    }
                    else if (IsSweetPotatoSeedMatch(id))
                    {
                        HasSweetPotatoSeed = true;
                    }
                }
            }

            if (IsAllSuppliesGathered && CurrentStage == QuestStage.GatheringSupplies)
            {
                CurrentStage = QuestStage.PlantingCrop;
            }

            bool changed = (hadHoe != HasHoe) || (hadWaterer != HasPlantWaterer) ||
                           (hadBottle != HasWaterBottle) || (hadSeed != HasSweetPotatoSeed) ||
                           (previousStage != CurrentStage);

            if (changed)
            {
                OnStateChanged?.Invoke();
            }

            return changed;
        }

        /// <summary>
        /// Called when a crop has been sown.
        /// </summary>
        public bool OnCropPlanted(string seedItemId)
        {
            if (CurrentStage != QuestStage.PlantingCrop && CurrentStage != QuestStage.WateringCrop) return false;

            if (IsSweetPotatoSeedMatch(seedItemId))
            {
                PlantedCount++;
                HasPlantedCrop = true;
                CurrentStage = QuestStage.WateringCrop;
                OnStateChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when a farmland crop has been watered.
        /// </summary>
        public bool OnCropWatered(string seedItemId = null)
        {
            if (CurrentStage != QuestStage.WateringCrop) return false;

            if (string.IsNullOrEmpty(seedItemId) || IsSweetPotatoSeedMatch(seedItemId))
            {
                WateredCount++;
                if (PlantedCount > 0 && WateredCount >= PlantedCount)
                {
                    HasWateredCrop = true;
                    CurrentStage = QuestStage.Completed;
                }
                OnStateChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Synchronizes the crop counts directly from active FarmlandTiles in the scene.
        /// Only completes when all planted crops are watered.
        /// </summary>
        public bool SyncCropCounts(int totalPlanted, int watered)
        {
            if (CurrentStage == QuestStage.GatheringSupplies || CurrentStage == QuestStage.Completed) return false;

            int newPlanted = totalPlanted > PlantedCount ? totalPlanted : PlantedCount;
            int newWatered = watered > newPlanted ? newPlanted : watered;

            bool changed = (newPlanted != PlantedCount) || (newWatered != WateredCount);
            PlantedCount = newPlanted;
            WateredCount = newWatered;

            if (PlantedCount > 0)
            {
                HasPlantedCrop = true;
                if (WateredCount >= PlantedCount)
                {
                    HasWateredCrop = true;
                    if (CurrentStage != QuestStage.Completed)
                    {
                        CurrentStage = QuestStage.Completed;
                        changed = true;
                    }
                }
                else
                {
                    if (CurrentStage != QuestStage.WateringCrop)
                    {
                        CurrentStage = QuestStage.WateringCrop;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                OnStateChanged?.Invoke();
            }

            return changed;
        }

        /// <summary>
        /// Formats the current quest objectives into display text for the HUD.
        /// </summary>
        public string GetFormattedObjectiveDetails()
        {
            switch (CurrentStage)
            {
                case QuestStage.GatheringSupplies:
                    string hoeMark = HasHoe ? "<color=#80FF80>[OK]</color>" : "<color=#FFA0A0>[  ]</color>";
                    string watererMark = HasPlantWaterer ? "<color=#80FF80>[OK]</color>" : "<color=#FFA0A0>[  ]</color>";
                    string bottleMark = HasWaterBottle ? "<color=#80FF80>[OK]</color>" : "<color=#FFA0A0>[  ]</color>";
                    string seedMark = HasSweetPotatoSeed ? "<color=#80FF80>[OK]</color>" : "<color=#FFA0A0>[  ]</color>";

                    return $"Gather farming supplies:\n" +
                           $"{hoeMark} Hoe  {watererMark} Waterer\n" +
                           $"{bottleMark} Bottle  {seedMark} Sweet Potato Seed";

                case QuestStage.PlantingCrop:
                    return "Till soil with Hoe & plant seeds:\n" +
                           "<color=#FFA0A0>[  ]</color> Plant Sweet Potato Seed (0 planted)\n" +
                           "<color=#FFA0A0>[  ]</color> Water planted crops (0/0)";

                case QuestStage.WateringCrop:
                    string plantedCropStr = PlantedCount == 1 ? "1 Sweet Potato" : $"{PlantedCount} Sweet Potatoes";
                    return $"Till soil, plant & water all crops:\n" +
                           $"<color=#FFA0A0>[  ]</color> Planted: {plantedCropStr}\n" +
                           $"<color=#FFA0A0>[  ]</color> Watered: {WateredCount}/{PlantedCount} crops";

                case QuestStage.Completed:
                    string finalCountStr = PlantedCount <= 1 ? "Sweet Potato" : $"{PlantedCount} Sweet Potatoes";
                    return $"<color=#FFD700>All objectives completed!</color>\n" +
                           $"<color=#80FF80>[OK]</color> Planted & Watered {finalCountStr}!";

                default:
                    return string.Empty;
            }
        }

        private static bool IsHoeMatch(string id)
        {
            return string.Equals(id, ItemIdHoe, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Tool_Hoe", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Hoe", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPlantWatererMatch(string id)
        {
            return string.Equals(id, ItemIdPlantWaterer, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Tool_PlantWaterer", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Plant Waterer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWaterBottleMatch(string id)
        {
            return string.Equals(id, ItemIdWaterBottle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Food_BottleOfWater", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "food_bottleofwater", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Bottle of Water", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Water Bottle", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSweetPotatoSeedMatch(string id)
        {
            return string.Equals(id, ItemIdSweetPotatoSeed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Seed_SweetPotato", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "seed_sweetpotato", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Sweet Potato Seed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
