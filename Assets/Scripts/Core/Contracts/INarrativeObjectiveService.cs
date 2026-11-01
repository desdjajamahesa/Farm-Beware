using System;
using System.Collections.Generic;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Narrative objective category classifying story objectives on the HUD.
    /// </summary>
    public enum NarrativeObjectiveCategory : byte
    {
        MainStory = 0,
        SideQuest = 1,
        SurvivalPrep = 2
    }

    /// <summary>
    /// Pure C# POCO model representing an active storyline-driven gameplay objective.
    /// Manages objective tracking and completion with zero Unity lifecycle dependencies.
    /// </summary>
    [Serializable]
    public sealed class NarrativeObjective
    {
        public string objectiveId;
        public NarrativeObjectiveCategory category;
        public string title;
        public string description;
        public int targetCount;
        public int currentCount;
        public bool isCompleted;
        public NarrativeFactKey trackingFactKey;

        public NarrativeObjective() { }

        public NarrativeObjective(
            string objectiveId,
            string title,
            string description,
            int targetCount,
            NarrativeObjectiveCategory category = NarrativeObjectiveCategory.MainStory,
            NarrativeFactKey trackingFactKey = NarrativeFactKey.CurrentDay)
        {
            this.objectiveId = objectiveId;
            this.title = title;
            this.description = description;
            this.targetCount = targetCount;
            this.currentCount = 0;
            this.isCompleted = false;
            this.category = category;
            this.trackingFactKey = trackingFactKey;
        }
    }

    /// <summary>
    /// Serializable data contract storing narrative objective progress in save game files.
    /// Preserves complete objective criteria, current progression, and completion status.
    /// </summary>
    [Serializable]
    public sealed class NarrativeObjectiveSaveData
    {
        public string objectiveId;
        public NarrativeObjectiveCategory category;
        public string title;
        public string description;
        public int targetCount;
        public int currentCount;
        public bool isCompleted;
        public NarrativeFactKey trackingFactKey;
    }

    /// <summary>
    /// Serializable payload preserving quest and narrative objective states across sessions.
    /// </summary>
    [Serializable]
    public sealed class QuestSavePayload
    {
        public string activeQuestId;
        public int currentStage;
        public int plantedCount;
        public int wateredCount;
        public bool isCompleted;
        public List<NarrativeObjectiveSaveData> activeStoryObjectives = new List<NarrativeObjectiveSaveData>();
    }

    /// <summary>
    /// Decoupled narrative objective service interface bridging Storyline and Quest systems.
    /// Enables narrative beats to dynamically dispatch and track daily objectives on the player HUD.
    /// </summary>
    public interface INarrativeObjectiveService
    {
        /// <summary>Collection of all currently registered narrative objectives.</summary>
        IReadOnlyList<NarrativeObjective> ActiveObjectives { get; }

        /// <summary>Retrieves an objective by its unique identifier.</summary>
        NarrativeObjective GetObjective(string objectiveId);

        /// <summary>Assigns or updates a story objective with standard MainStory classification.</summary>
        void AssignStoryObjective(string objectiveId, string title, string description, int targetCount);

        /// <summary>Assigns or updates a story objective with explicit category and tracking fact key.</summary>
        void AssignStoryObjective(string objectiveId, NarrativeObjectiveCategory category, string title, string description, int targetCount, NarrativeFactKey trackingKey);

        /// <summary>Marks a story objective as completed.</summary>
        void CompleteStoryObjective(string objectiveId);

        /// <summary>Sets the numerical progress of a story objective.</summary>
        void UpdateObjectiveProgress(string objectiveId, int currentCount);

        /// <summary>Reports numerical delta for any objective actively tracking the specified fact key.</summary>
        void ReportProgress(NarrativeFactKey factKey, int delta);

        /// <summary>Event raised whenever an objective is assigned, updated, or completed.</summary>
        event Action<string, bool> OnObjectiveStateChanged;

        /// <summary>Exports the complete quest and story objective state for disk serialization.</summary>
        QuestSavePayload ExportSavePayload();

        /// <summary>Imports and applies serialized quest and story objective state from disk.</summary>
        void ImportSavePayload(QuestSavePayload payload);
    }

    /// <summary>
    /// Decoupled event bus for publishing cross-subsystem narrative telemetry hooks.
    /// Preserves acyclic assembly boundaries without direct module coupling.
    /// </summary>
    public static class NarrativeTelemetryHooks
    {
        /// <summary>Fired when the merchant shop panel is opened.</summary>
        public static event Action OnMerchantOpened;

        /// <summary>Fired when the trophy cabinet is inspected.</summary>
        public static event Action OnTrophyInspected;

        /// <summary>Fired when a cooking recipe completes at the kitchen stove.</summary>
        public static event Action OnCookingCompleted;

        /// <summary>Fired when a crop withers or dies on a farmland tile.</summary>
        public static event Action OnCropWithered;

        /// <summary>Dispatches notification that the merchant shop opened.</summary>
        public static void ReportMerchantOpened()
        {
            OnMerchantOpened?.Invoke();
        }

        /// <summary>Dispatches notification that the trophy cabinet was inspected.</summary>
        public static void ReportTrophyInspected()
        {
            OnTrophyInspected?.Invoke();
        }

        /// <summary>Dispatches notification that cooking succeeded at the stove.</summary>
        public static void ReportCookingCompleted()
        {
            OnCookingCompleted?.Invoke();
        }

        /// <summary>Dispatches notification that a crop withered or died.</summary>
        public static void ReportCropWithered()
        {
            OnCropWithered?.Invoke();
        }
    }
}
