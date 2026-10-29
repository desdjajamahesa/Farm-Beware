using System;
using System.Collections.Generic;

namespace FarmBeware.Logic.Quests
{
    public enum QuestStage
    {
        // Side Quest 1: First Steps (Planting Quest)
        GatheringSupplies = 0,
        PlantingCrop = 1,
        WateringCrop = 2,
        Completed = 3,

        // Side Quest 2: Home Cooking (Cooking Quest)
        EnterKitchen = 4,
        RetrieveIngredients = 5,
        CookStew = 6,
        ConsumeDish = 7,
        CookingQuestCompleted = 8
    }

    /// <summary>
    /// Pure C# POCO model representing onboarding side quests:
    /// 1. First Steps: Gathering tools, tilling soil, planting & watering sweet potatoes.
    /// 2. Home Cooking: Entering kitchen, taking Taro & Coconut from refrigerator, cooking Taro Coconut Stew, and eating/drinking.
    /// Manages requirement evaluation with zero Unity lifecycle dependencies and zero heap allocations during evaluation.
    /// </summary>
    public class QuestProgressModel
    {
        public const string QuestIdFirstSteps = "sidequest_first_steps";
        public const string QuestTitleFirstSteps = "First Steps";

        public const string QuestIdHomeCooking = "sidequest_home_cooking";
        public const string QuestTitleHomeCooking = "Home Cooking";

        public const string QuestId = QuestIdFirstSteps;
        public const string QuestTitle = QuestTitleFirstSteps;

        public const string ItemIdHoe = "tool_hoe";
        public const string ItemIdPlantWaterer = "tool_plant_waterer";
        public const string ItemIdWaterBottle = "food_bottle_water";
        public const string ItemIdSweetPotatoSeed = "seed_sweet_potato";

        public const string ItemIdTaro = "crop_taro";
        public const string ItemIdCoconut = "mat_coconut";
        public const string ItemIdTaroCoconutStew = "food_taro_coconut_stew";

        public QuestStage CurrentStage { get; private set; } = QuestStage.GatheringSupplies;

        // Quest 1: First Steps State
        public bool HasHoe { get; private set; }
        public bool HasPlantWaterer { get; private set; }
        public bool HasWaterBottle { get; private set; }
        public bool HasSweetPotatoSeed { get; private set; }

        public bool HasPlantedCrop { get; private set; }
        public bool HasWateredCrop { get; private set; }

        public int PlantedCount { get; private set; } = 0;
        public int WateredCount { get; private set; } = 0;

        public bool IsAllSuppliesGathered => HasHoe && HasPlantWaterer && HasWaterBottle && HasSweetPotatoSeed;
        public bool IsCompleted => CurrentStage == QuestStage.Completed || CurrentStage == QuestStage.CookingQuestCompleted;

        // Quest 2: Home Cooking State
        public bool HasEnteredKitchen { get; private set; }
        public bool HasTaro { get; private set; }
        public bool HasCoconut { get; private set; }
        public bool HasCookedStew { get; private set; }
        public bool HasConsumedStew { get; private set; }

        public bool IsAllCookingIngredientsGathered => HasTaro && HasCoconut;
        public bool IsCookingQuest => CurrentStage >= QuestStage.EnterKitchen;

        public event Action OnStateChanged;

        /// <summary>
        /// Transitions from First Steps to Home Cooking side quest.
        /// </summary>
        public bool AdvanceToCookingQuest()
        {
            if (CurrentStage == QuestStage.Completed)
            {
                CurrentStage = QuestStage.EnterKitchen;
                OnStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called when the player steps inside the kitchen area.
        /// </summary>
        public bool OnEnteredKitchen()
        {
            if (CurrentStage == QuestStage.EnterKitchen)
            {
                HasEnteredKitchen = true;
                CurrentStage = QuestStage.RetrieveIngredients;
                OnStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called when the player cooks or collects Taro Coconut Stew Bowl.
        /// </summary>
        public bool OnStewCooked()
        {
            if (CurrentStage == QuestStage.CookStew)
            {
                HasCookedStew = true;
                CurrentStage = QuestStage.ConsumeDish;
                OnStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called when the player double-clicks the dish to eat and drink.
        /// </summary>
        public bool OnDishConsumed(string itemId)
        {
            if (CurrentStage == QuestStage.ConsumeDish && IsStewMatch(itemId))
            {
                HasConsumedStew = true;
                CurrentStage = QuestStage.CookingQuestCompleted;
                OnStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates the player's current item collection against quest requirements.
        /// </summary>
        public bool EvaluateInventory(IEnumerable<string> itemIds)
        {
            if (CurrentStage == QuestStage.CookingQuestCompleted) return false;

            bool hadHoe = HasHoe;
            bool hadWaterer = HasPlantWaterer;
            bool hadBottle = HasWaterBottle;
            bool hadSeed = HasSweetPotatoSeed;
            bool hadTaro = HasTaro;
            bool hadCoconut = HasCoconut;
            bool hadCookedStew = HasCookedStew;
            QuestStage previousStage = CurrentStage;

            HasHoe = false;
            HasPlantWaterer = false;
            HasWaterBottle = false;
            HasSweetPotatoSeed = false;
            HasTaro = false;
            HasCoconut = false;
            HasCookedStew = false;

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
                    else if (IsTaroMatch(id))
                    {
                        HasTaro = true;
                    }
                    else if (IsCoconutMatch(id))
                    {
                        HasCoconut = true;
                    }
                    else if (IsStewMatch(id))
                    {
                        HasCookedStew = true;
                    }
                }
            }

            // Quest 1: First Steps progression
            if (IsAllSuppliesGathered && CurrentStage == QuestStage.GatheringSupplies)
            {
                CurrentStage = QuestStage.PlantingCrop;
            }

            // Quest 2: Home Cooking progression
            if (CurrentStage == QuestStage.RetrieveIngredients && IsAllCookingIngredientsGathered)
            {
                CurrentStage = QuestStage.CookStew;
            }
            else if (CurrentStage == QuestStage.CookStew && HasCookedStew)
            {
                CurrentStage = QuestStage.ConsumeDish;
            }

            bool changed = (hadHoe != HasHoe) || (hadWaterer != HasPlantWaterer) ||
                           (hadBottle != HasWaterBottle) || (hadSeed != HasSweetPotatoSeed) ||
                           (hadTaro != HasTaro) || (hadCoconut != HasCoconut) ||
                           (hadCookedStew != HasCookedStew) ||
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
            if (CurrentStage == QuestStage.GatheringSupplies || CurrentStage == QuestStage.Completed || CurrentStage >= QuestStage.EnterKitchen)
                return false;

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

                    return $"Take farming tools from Storage Chest:\n" +
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

                case QuestStage.EnterKitchen:
                    return "Enter the Kitchen to prepare a meal:\n" +
                           "<color=#FFA0A0>[  ]</color> Go inside the Kitchen\n" +
                           "<color=#888888>Head inside the farmhouse kitchen</color>";

                case QuestStage.RetrieveIngredients:
                    string taroMark = HasTaro ? "<color=#80FF80>[OK]</color>" : "<color=#FFA0A0>[  ]</color>";
                    string coconutMark = HasCoconut ? "<color=#80FF80>[OK]</color>" : "<color=#FFA0A0>[  ]</color>";
                    return "Take ingredients from Refrigerator:\n" +
                           $"{taroMark} Taro  {coconutMark} Coconut\n" +
                           "<color=#888888>Retrieve both from the kitchen fridge</color>";

                case QuestStage.CookStew:
                    return "Cook at the Kitchen Stove:\n" +
                           "<color=#FFA0A0>[  ]</color> Make Taro Coconut Stew Bowl\n" +
                           "<color=#888888>Interact with stove & cook the recipe</color>";

                case QuestStage.ConsumeDish:
                    return "Enjoy your hearty cooked meal:\n" +
                           "<color=#FFA0A0>[  ]</color> Double-click stew to eat & drink!\n" +
                           "<color=#FFCC00>Tip: Double-click in inventory to eat & drink</color>";

                case QuestStage.CookingQuestCompleted:
                    return "<color=#FFD700>All objectives completed!</color>\n" +
                           "<color=#80FF80>[OK]</color> Cooked & enjoyed Taro Coconut Stew Bowl!";

                default:
                    return string.Empty;
            }
        }

        private static bool IsHoeMatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return string.Equals(id, ItemIdHoe, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Tool_Hoe", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Hoe", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPlantWatererMatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return string.Equals(id, ItemIdPlantWaterer, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Tool_PlantWaterer", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Plant Waterer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWaterBottleMatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return string.Equals(id, ItemIdWaterBottle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Food_BottleOfWater", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "food_bottleofwater", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Bottle of Water", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Water Bottle", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSweetPotatoSeedMatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return string.Equals(id, ItemIdSweetPotatoSeed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Seed_SweetPotato", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "seed_sweetpotato", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Sweet Potato Seed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTaroMatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return string.Equals(id, ItemIdTaro, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Crop_Taro", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Taro", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCoconutMatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return string.Equals(id, ItemIdCoconut, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Mat_Coconut", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Coconut", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStewMatch(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return string.Equals(id, ItemIdTaroCoconutStew, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Food_TaroCoconutStew", StringComparison.OrdinalIgnoreCase) ||
                   id.IndexOf("TaroCoconutStew", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("Taro Coconut Stew", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
