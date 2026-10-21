using System;
using System.Collections.Generic;

namespace FeaturesStoryline.Data
{
    /// <summary>
    /// Pure serializable DTO capturing the complete persistent state of the storyline subsystem.
    /// Can be round-tripped cleanly through GameSaveData.
    /// </summary>
    [Serializable]
    public class StorylineSavePayload
    {
        public int chapter = 1;
        public List<string> triggeredBeats = new List<string>();
        public List<string> unlockedJournals = new List<string>();
        public bool isComplete = false;

        public StorylineSavePayload()
        {
            chapter = 1;
            triggeredBeats = new List<string>();
            unlockedJournals = new List<string>();
            isComplete = false;
        }

        public StorylineSavePayload(int chapter, IEnumerable<string> beats, IEnumerable<string> journals, bool isComplete)
        {
            this.chapter = chapter;
            this.triggeredBeats = beats != null ? new List<string>(beats) : new List<string>();
            this.unlockedJournals = journals != null ? new List<string>(journals) : new List<string>();
            this.isComplete = isComplete;
        }
    }
}
