using System;
using System.Collections.Generic;
using FarmBeware.Core.Runtime;

namespace FarmBeware.Logic.Quests
{
    /// <summary>
    /// Normalized completion state displayed in the Quest Log UI.
    /// </summary>
    public enum QuestCompletionState : byte
    {
        Unfinished = 0,
        Finished = 1
    }

    /// <summary>
    /// Filter mode supported by the Quest Log browser.
    /// </summary>
    public enum QuestStateFilter : byte
    {
        All = 0,
        Unfinished = 1,
        Finished = 2
    }

    /// <summary>
    /// Immutable summary counts for all quests currently received by the player.
    /// </summary>
    public readonly struct QuestLogSummary
    {
        public readonly int TotalCount;
        public readonly int UnfinishedCount;
        public readonly int FinishedCount;

        public QuestLogSummary(int totalCount, int unfinishedCount, int finishedCount)
        {
            TotalCount = totalCount;
            UnfinishedCount = unfinishedCount;
            FinishedCount = finishedCount;
        }
    }

    /// <summary>
    /// Pure C# view-model snapshot representing a single accepted quest entry,
    /// its progress counts, step breakdown, and Finish/Unfinish state.
    /// </summary>
    public sealed class QuestEntrySnapshot
    {
        public string QuestId { get; }
        public string Title { get; }
        public string Description { get; }
        public string StepsDetail { get; }
        public NarrativeObjectiveCategory Category { get; }
        public QuestCompletionState State { get; }
        public int CurrentProgress { get; }
        public int TargetProgress { get; }

        public bool IsFinished => State == QuestCompletionState.Finished;

        public float ProgressRatio
        {
            get
            {
                if (TargetProgress <= 0) return 0f;
                float ratio = (float)CurrentProgress / TargetProgress;
                if (ratio < 0f) return 0f;
                return ratio > 1f ? 1f : ratio;
            }
        }

        public int ProgressPercent => (int)Math.Round(ProgressRatio * 100f);

        public QuestEntrySnapshot(
            string questId,
            string title,
            string description,
            string stepsDetail,
            NarrativeObjectiveCategory category,
            QuestCompletionState state,
            int currentProgress,
            int targetProgress)
        {
            QuestId = questId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            StepsDetail = stepsDetail ?? string.Empty;
            Category = category;
            State = state;
            TargetProgress = targetProgress > 0 ? targetProgress : 1;
            CurrentProgress = Math.Clamp(currentProgress, 0, TargetProgress);
        }
    }

    /// <summary>
    /// Pure C# POCO aggregator consolidating onboarding side quests and narrative objectives
    /// into normalized quest entries for UI presentation without Unity lifecycle dependencies.
    /// </summary>
    public static class QuestLogAggregator
    {
        private const string MarkDone = "<color=#52E58A>[OK]</color>";
        private const string MarkPending = "<color=#FF9F68>[  ]</color>";

        /// <summary>
        /// Builds the full ordered list of quests received by the player.
        /// Active (Unfinished) quests appear before completed (Finished) quests.
        /// </summary>
        public static void BuildEntries(
            QuestProgressModel model,
            IReadOnlyList<NarrativeObjective> storyObjectives,
            int currentDay,
            List<QuestEntrySnapshot> output)
        {
            if (output == null) return;
            output.Clear();

            if (model != null)
            {
                if (HasReceivedFirstSteps(model, currentDay))
                {
                    output.Add(BuildFirstStepsSnapshot(model));
                }

                if (model.CurrentStage >= QuestStage.EnterKitchen)
                {
                    output.Add(BuildHomeCookingSnapshot(model));
                }
            }

            if (storyObjectives != null)
            {
                for (int i = 0; i < storyObjectives.Count; i++)
                {
                    NarrativeObjective obj = storyObjectives[i];
                    if (obj == null || string.IsNullOrEmpty(obj.objectiveId)) continue;
                    output.Add(BuildObjectiveSnapshot(obj));
                }
            }

            // Keep Unfinished quests grouped first so active objectives stay immediately visible
            if (output.Count > 1)
            {
                StablePartitionByState(output);
            }
        }

        /// <summary>
        /// Filters quest snapshots into a caller-supplied list without allocating new entries.
        /// </summary>
        public static void FilterEntries(
            IReadOnlyList<QuestEntrySnapshot> source,
            QuestStateFilter filter,
            List<QuestEntrySnapshot> filteredOutput)
        {
            if (filteredOutput == null) return;
            filteredOutput.Clear();
            if (source == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                QuestEntrySnapshot entry = source[i];
                if (entry == null) continue;

                if (filter == QuestStateFilter.All ||
                    (filter == QuestStateFilter.Unfinished && !entry.IsFinished) ||
                    (filter == QuestStateFilter.Finished && entry.IsFinished))
                {
                    filteredOutput.Add(entry);
                }
            }
        }

        /// <summary>
        /// Computes total, unfinished, and finished counts across all received quests.
        /// </summary>
        public static QuestLogSummary ComputeSummary(IReadOnlyList<QuestEntrySnapshot> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return new QuestLogSummary(0, 0, 0);
            }

            int unfinished = 0;
            int finished = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                QuestEntrySnapshot entry = entries[i];
                if (entry == null) continue;
                if (entry.IsFinished) finished++;
                else unfinished++;
            }

            return new QuestLogSummary(unfinished + finished, unfinished, finished);
        }

        /// <summary>
        /// Returns display text for a quest category pill.
        /// </summary>
        public static string GetCategoryLabel(NarrativeObjectiveCategory category)
        {
            switch (category)
            {
                case NarrativeObjectiveCategory.SideQuest:
                    return "SIDE QUEST";
                case NarrativeObjectiveCategory.SurvivalPrep:
                    return "SURVIVAL";
                case NarrativeObjectiveCategory.MainStory:
                default:
                    return "MAIN STORY";
            }
        }

        /// <summary>
        /// Returns standardized Finish / Unfinish label text.
        /// </summary>
        public static string GetStateLabel(QuestCompletionState state)
        {
            return state == QuestCompletionState.Finished ? "FINISHED" : "UNFINISHED";
        }

        private static bool HasReceivedFirstSteps(QuestProgressModel model, int currentDay)
        {
            if (currentDay >= 1) return true;

            return model.CurrentStage != QuestStage.GatheringSupplies ||
                   model.HasHoe ||
                   model.HasPlantWaterer ||
                   model.HasWaterBottle ||
                   model.HasSweetPotatoSeed ||
                   model.HasPlantedCrop ||
                   model.HasWateredCrop ||
                   model.PlantedCount > 0 ||
                   model.WateredCount > 0;
        }

        private static QuestEntrySnapshot BuildFirstStepsSnapshot(QuestProgressModel model)
        {
            bool isFinished = model.CurrentStage >= QuestStage.Completed;
            bool suppliesComplete = model.IsAllSuppliesGathered || model.CurrentStage >= QuestStage.PlantingCrop;
            bool plantedComplete = model.HasPlantedCrop || model.PlantedCount > 0 || model.CurrentStage >= QuestStage.WateringCrop;
            bool wateredComplete = isFinished || (model.PlantedCount > 0 && model.WateredCount >= model.PlantedCount);

            int suppliesCount;
            if (suppliesComplete)
            {
                suppliesCount = 4;
            }
            else
            {
                suppliesCount = (model.HasHoe ? 1 : 0) +
                                (model.HasPlantWaterer ? 1 : 0) +
                                (model.HasWaterBottle ? 1 : 0) +
                                (model.HasSweetPotatoSeed ? 1 : 0);
            }

            int currentSteps = isFinished
                ? 6
                : suppliesCount + (plantedComplete ? 1 : 0) + (wateredComplete ? 1 : 0);

            int targetCrops = Math.Max(1, model.PlantedCount);
            int shownWatered = isFinished ? targetCrops : Math.Clamp(model.WateredCount, 0, targetCrops);

            string detail =
                $"{(suppliesComplete ? MarkDone : MarkPending)} Gather starter tools & seed from Storage Chest ({suppliesCount}/4)\n" +
                $"{(plantedComplete ? MarkDone : MarkPending)} Till soil & plant Sweet Potato Seed ({model.PlantedCount}/1)\n" +
                $"{(wateredComplete ? MarkDone : MarkPending)} Water planted Sweet Potatoes ({shownWatered}/{targetCrops})";

            return new QuestEntrySnapshot(
                QuestProgressModel.QuestIdFirstSteps,
                QuestProgressModel.QuestTitleFirstSteps,
                "Collect starter equipment and cultivate your first Sweet Potato plot.",
                detail,
                NarrativeObjectiveCategory.SideQuest,
                isFinished ? QuestCompletionState.Finished : QuestCompletionState.Unfinished,
                currentSteps,
                6);
        }

        private static QuestEntrySnapshot BuildHomeCookingSnapshot(QuestProgressModel model)
        {
            bool isFinished = model.CurrentStage == QuestStage.CookingQuestCompleted;
            bool enteredKitchen = model.HasEnteredKitchen || model.CurrentStage >= QuestStage.RetrieveIngredients;
            bool hasTaro = model.HasTaro || model.CurrentStage >= QuestStage.CookStew;
            bool hasCoconut = model.HasCoconut || model.CurrentStage >= QuestStage.CookStew;
            bool ingredientsReady = hasTaro && hasCoconut;
            bool cookedStew = model.HasCookedStew || model.CurrentStage >= QuestStage.ConsumeDish;
            bool consumedStew = model.HasConsumedStew || isFinished;

            int ingredientCount = (hasTaro ? 1 : 0) + (hasCoconut ? 1 : 0);
            int currentSteps = isFinished
                ? 5
                : (enteredKitchen ? 1 : 0) + ingredientCount + (cookedStew ? 1 : 0) + (consumedStew ? 1 : 0);

            string detail =
                $"{(enteredKitchen ? MarkDone : MarkPending)} Enter the Farmhouse Kitchen\n" +
                $"{(ingredientsReady ? MarkDone : MarkPending)} Take Taro & Coconut from Refrigerator ({ingredientCount}/2)\n" +
                $"{(cookedStew ? MarkDone : MarkPending)} Cook Taro Coconut Stew Bowl at Stove\n" +
                $"{(consumedStew ? MarkDone : MarkPending)} Eat & drink stew from Inventory";

            return new QuestEntrySnapshot(
                QuestProgressModel.QuestIdHomeCooking,
                QuestProgressModel.QuestTitleHomeCooking,
                "Prepare and enjoy a nourishing meal inside the farmhouse kitchen.",
                detail,
                NarrativeObjectiveCategory.SideQuest,
                isFinished ? QuestCompletionState.Finished : QuestCompletionState.Unfinished,
                currentSteps,
                5);
        }

        private static QuestEntrySnapshot BuildObjectiveSnapshot(NarrativeObjective obj)
        {
            int target = obj.targetCount > 0 ? obj.targetCount : 1;
            int current = obj.isCompleted ? target : Math.Clamp(obj.currentCount, 0, target);
            QuestCompletionState state = obj.isCompleted
                ? QuestCompletionState.Finished
                : QuestCompletionState.Unfinished;

            string desc = !string.IsNullOrEmpty(obj.description) ? obj.description : obj.title;
            string mark = obj.isCompleted ? MarkDone : MarkPending;
            string stepText = $"{mark} Progress: {current} / {target}";

            return new QuestEntrySnapshot(
                obj.objectiveId,
                !string.IsNullOrEmpty(obj.title) ? obj.title : obj.objectiveId,
                desc,
                stepText,
                obj.category,
                state,
                current,
                target);
        }

        private static void StablePartitionByState(List<QuestEntrySnapshot> list)
        {
            // Small list (typically < 20 entries); simple stable insertion maintains original relative order per state
            int insertIndex = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].IsFinished)
                {
                    if (i != insertIndex)
                    {
                        QuestEntrySnapshot item = list[i];
                        for (int j = i; j > insertIndex; j--)
                        {
                            list[j] = list[j - 1];
                        }
                        list[insertIndex] = item;
                    }
                    insertIndex++;
                }
            }
        }
    }
}
