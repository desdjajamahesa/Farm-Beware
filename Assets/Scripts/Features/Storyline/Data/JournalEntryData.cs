using UnityEngine;

namespace FeaturesStoryline.Data
{
    /// <summary>
    /// ScriptableObject storing a single lore journal page or archival entry.
    /// Can be displayed in the Journal UI and collected by the player.
    /// </summary>
    [CreateAssetMenu(fileName = "Journal_New", menuName = "Farm-Beware/Storyline/Journal Entry")]
    public class JournalEntryData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique string identifier for this journal entry (e.g. JOURNAL_GRANDFATHER_01).")]
        public string entryId;

        [Tooltip("Story chapter (1-5) to which this entry belongs.")]
        public int chapter = 1;

        [Header("Metadata")]
        [Tooltip("Title of this journal page (e.g. The Weathered Journal).")]
        public string title;

        [Tooltip("Author of this page (e.g. Grandfather, The Old Farmer, The Ancient Seal).")]
        public string author = "Grandfather";

        [Tooltip("Order within the journal catalog.")]
        public int sortOrder = 0;

        [Header("Narrative Text")]
        [TextArea(6, 16)]
        [Tooltip("Full text content of this journal entry.")]
        public string content;

        [Header("Visuals")]
        [Tooltip("Optional illustration or sketch on the journal page.")]
        public Sprite pageIllustration;

        /// <summary>
        /// Factory method for creating runtime or mock journal entries.
        /// </summary>
        public static JournalEntryData CreateRuntimeInstance(string id, int chapter, string title, string author, string text, int order = 0)
        {
            var entry = CreateInstance<JournalEntryData>();
            entry.entryId = id;
            entry.chapter = chapter;
            entry.title = title;
            entry.author = author;
            entry.content = text;
            entry.sortOrder = order;
            return entry;
        }
    }
}
