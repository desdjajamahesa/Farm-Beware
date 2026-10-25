using System;
using System.Collections.Generic;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline.Data
{
    /// <summary>
    /// Pure serializable DTO capturing the complete persistent state of the storyline subsystem,
    /// including triggered storylet history, unlocked archival dossiers, rule execution cooldowns,
    /// and the full Zero-GC Narrative Fact Blackboard array.
    /// Can be round-tripped cleanly through GameSaveData.
    /// </summary>
    [Serializable]
    public class StorylineSavePayload
    {
        public int chapter = 1;
        public List<string> triggeredBeats = new List<string>();
        public List<string> unlockedJournals = new List<string>();
        public bool isComplete = false;

        // Persistent Narrative Fact Blackboard state (128 integers)
        public int[] rawFacts = new int[(int)NarrativeFactKey.MaxCount];

        // Persistent storylet execution history for RepeatPolicy evaluation
        public List<string> lastPlayedBeatKeys = new List<string>();
        public List<int> lastPlayedDays = new List<int>();

        // Persistent deferred modal beats (Hard Safety Invariant)
        public List<string> pendingModalBeatIds = new List<string>();

        public StorylineSavePayload()
        {
            chapter = 1;
            triggeredBeats = new List<string>();
            unlockedJournals = new List<string>();
            isComplete = false;
            rawFacts = new int[(int)NarrativeFactKey.MaxCount];
            lastPlayedBeatKeys = new List<string>();
            lastPlayedDays = new List<int>();
            pendingModalBeatIds = new List<string>();
        }

        public StorylineSavePayload(
            int chapter,
            IEnumerable<string> beats,
            IEnumerable<string> journals,
            bool isComplete,
            int[] facts = null,
            Dictionary<string, int> lastPlayedMap = null,
            IEnumerable<string> pendingModals = null)
        {
            this.chapter = chapter;
            this.triggeredBeats = beats != null ? new List<string>(beats) : new List<string>();
            this.unlockedJournals = journals != null ? new List<string>(journals) : new List<string>();
            this.isComplete = isComplete;

            this.rawFacts = new int[(int)NarrativeFactKey.MaxCount];
            if (facts != null)
            {
                Array.Copy(facts, this.rawFacts, Math.Min(facts.Length, this.rawFacts.Length));
            }

            this.lastPlayedBeatKeys = new List<string>();
            this.lastPlayedDays = new List<int>();
            if (lastPlayedMap != null)
            {
                foreach (var kvp in lastPlayedMap)
                {
                    this.lastPlayedBeatKeys.Add(kvp.Key);
                    this.lastPlayedDays.Add(kvp.Value);
                }
            }

            this.pendingModalBeatIds = pendingModals != null ? new List<string>(pendingModals) : new List<string>();
        }
    }
}
