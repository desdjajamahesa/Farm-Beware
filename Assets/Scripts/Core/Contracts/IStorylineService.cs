using System;
using System.Collections.Generic;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Service contract for the storyline progression system.
    /// Tracks story beat completion, journal entries, and narrative progression state.
    /// Registered in ServiceLocator for decoupled feature access.
    /// </summary>
    public interface IStorylineService
    {
        /// <summary>Current story chapter (1-5, maps to in-game day).</summary>
        int CurrentChapter { get; }

        /// <summary>Whether a specific story beat has been triggered.</summary>
        bool HasTriggered(string beatId);

        /// <summary>Trigger a story beat by ID. Returns false if already triggered or conditions unmet.</summary>
        bool TriggerBeat(string beatId);

        /// <summary>Whether the journal has a specific entry unlocked.</summary>
        bool HasJournalEntry(string entryId);

        /// <summary>Total number of journal entries unlocked.</summary>
        int UnlockedJournalCount { get; }

        /// <summary>Whether the main story has been completed (epilogue reached).</summary>
        bool IsStoryComplete { get; }

        /// <summary>Opens the journal UI browser.</summary>
        void ShowJournal();

        /// <summary>Closes the journal UI browser.</summary>
        void HideJournal();

        /// <summary>Whether the journal UI is currently visible.</summary>
        bool IsJournalOpen { get; }

        /// <summary>Returns readonly list of all triggered beat IDs.</summary>
        IReadOnlyCollection<string> TriggeredBeatIds { get; }

        /// <summary>Returns readonly list of all unlocked journal entry IDs.</summary>
        IReadOnlyCollection<string> UnlockedJournalIds { get; }

        /// <summary>Fact blackboard associated with the narrative state.</summary>
        INarrativeFactBoard FactBoard { get; }

        /// <summary>Restores story state during save game load.</summary>
        void RestoreState(int chapter, IEnumerable<string> beats, IEnumerable<string> journals, bool isComplete);

        /// <summary>Restores extended narrative state with facts and last-played maps.</summary>
        void RestoreState(int chapter, IEnumerable<string> beats, IEnumerable<string> journals, bool isComplete, int[] rawFacts, IList<string> lastPlayedKeys, IList<int> lastPlayedDays);

        /// <summary>Exports cooldown and repeat maps for persistence.</summary>
        void ExportCooldowns(out List<string> keys, out List<int> days);

        /// <summary>Fired when a story beat is triggered.</summary>
        event Action<string> OnStoryBeatTriggered;

        /// <summary>Fired when a new journal entry is unlocked.</summary>
        event Action<string> OnJournalEntryUnlocked;

        /// <summary>Fired when story chapter advances.</summary>
        event Action<int> OnChapterChanged;

        /// <summary>Fired when the main storyline is completed.</summary>
        event Action OnStoryCompleted;
    }
}
