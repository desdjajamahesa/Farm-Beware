using System;
using UnityEngine;
using FeaturesCombat;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline.Data
{
    /// <summary>
    /// ScriptableObject defining an atomic narrative beat or storylet rule.
    /// Operates as a discrete card in the Salience-Based Quality-Based Narrative (QBN) engine,
    /// defining activation preconditions against the Zero-GC Narrative Fact Blackboard,
    /// delivery presentation style, world state mutations, and archival unlocks.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryBeat_New", menuName = "Farm-Beware/Storyline/Story Beat")]
    public class StoryBeatData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique string identifier for this story beat (e.g. D1_PROLOGUE).")]
        public string beatId;

        [Tooltip("Story chapter (maps to in-game day or narrative phase, 1+).")]
        [Min(1)]
        public int chapter = 1;

        [Tooltip("Optional tags for narrative categorization and filtering.")]
        public string[] tags = Array.Empty<string>();

        [Header("Trigger & Contextual Conditions")]
        [Tooltip("Lifecycle or gameplay event that fires this beat.")]
        public StoryTriggerType triggerType = StoryTriggerType.Dawn;

        [Tooltip("Specific day number required (0 = any day in matching chapter).")]
        public int triggerDay = 0;

        [Tooltip("Specific wave index required (0 = any wave, 1-5).")]
        public int triggerWave = 0;

        [Tooltip("Target boss archetype (only evaluated for BossSpawn / BossDeath triggers).")]
        public EnemyType triggerBossType = EnemyType.TuberMaw;

        [Tooltip("Optional list of prerequisite beat IDs that must already be triggered.")]
        public string[] prerequisiteBeatIds = Array.Empty<string>();

        [Header("Zero-GC Blackboard Preconditions & Scoring")]
        [Tooltip("Preconditions evaluated against the INarrativeFactBoard. Specificity scores sum up matching condition weights.")]
        public NarrativeFactCondition[] criteria = Array.Empty<NarrativeFactCondition>();

        [Tooltip("World state mutations applied to the INarrativeFactBoard once this storylet executes.")]
        public NarrativeFactMutation[] mutations = Array.Empty<NarrativeFactMutation>();

        [Tooltip("Consumption policy regulating repeatable execution.")]
        public RepeatPolicy repeatPolicy = RepeatPolicy.MatchOnce;

        [Tooltip("Base priority multiplier for salience scoring: S(R_i) = saliencePriority * 100 + specificityScore.")]
        [Range(1, 10)]
        public int saliencePriority = 1;

        [Tooltip("Minimum number of in-game days that must elapse before re-triggering if repeatPolicy is CooldownDays.")]
        public int cooldownDays = 0;

        [Header("Delivery & Content")]
        [Tooltip("How this story beat is visually delivered to the player.")]
        public StoryDeliveryType deliveryType = StoryDeliveryType.HudBanner;

        [Tooltip("Heading or title displayed in banners, dialogue headers, or overlays.")]
        public string title;

        [Tooltip("Speaker name for dialogue boxes (e.g. Grandfather, Unknown Voice, The Land).")]
        public string speakerName;

        [TextArea(3, 8)]
        [Tooltip("Main narrative prose or dialogue text. Supports tokens: {PlayerName}, {WaterLiters}, {TruthLevel}.")]
        public string narrativeText;

        [TextArea(1, 3)]
        [Tooltip("Short concise text used when displayed as a top-screen HUD banner.")]
        public string hudBannerText;

        [Header("Journal Unlock")]
        [Tooltip("Whether triggering this beat unlocks a journal entry.")]
        public bool unlocksJournalEntry = false;

        [Tooltip("The journal entry to unlock if unlocksJournalEntry is true.")]
        public JournalEntryData journalEntry;

        [Header("Presentation & Timing")]
        [Tooltip("Duration in seconds for banner or floating text auto-dismissal.")]
        public float displayDuration = 5f;

        [Tooltip("Whether game time and player movement should pause during this beat.")]
        public bool pausesGameplay = false;

        [Tooltip("Optional SFX or ambient musical sting played when this beat triggers.")]
        public AudioClip narrativeAudioCue;

        [Header("Cinematic & Environmental Effects")]
        [Tooltip("Whether this beat causes the camera to shake.")]
        public bool triggersScreenShake = false;

        [Tooltip("Intensity of the camera shake (0.1 - 1.0).")]
        [Range(0.05f, 1.5f)]
        public float screenShakeIntensity = 0.3f;

        [Tooltip("Duration of the camera shake in seconds.")]
        public float screenShakeDuration = 0.5f;

        [Tooltip("Whether this beat signals an atmospheric change (e.g. red sky on Day 5).")]
        public bool triggersAtmosphericChange = false;

        [Header("Timeline Cinematic")]
        [Tooltip("Unity Timeline PlayableAsset played if deliveryType is TimelineCutscene.")]
        public UnityEngine.Playables.PlayableAsset timelineAsset;

        /// <summary>
        /// Formats dynamic token replacements into the narrative text without heap allocation when tokens are absent.
        /// </summary>
        public string FormatNarrativeText(INarrativeFactBoard facts, string playerName = "Ucup")
        {
            return FormatTokenString(narrativeText, facts, playerName);
        }

        /// <summary>
        /// Formats dynamic token replacements into the HUD banner text without heap allocation when tokens are absent.
        /// </summary>
        public string FormatHudBannerText(INarrativeFactBoard facts, string playerName = "Ucup")
        {
            string source = !string.IsNullOrEmpty(hudBannerText) ? hudBannerText : narrativeText;
            return FormatTokenString(source, facts, playerName);
        }

        private static string FormatTokenString(string text, INarrativeFactBoard facts, string playerName)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (!text.Contains("{")) return text;

            string formatted = text.Replace("{PlayerName}", playerName);
            if (facts != null)
            {
                formatted = formatted.Replace("{DayCount}", facts.GetFact(NarrativeFactKey.CurrentDay).ToString());
                formatted = formatted.Replace("{CorruptionLevel}", facts.GetFact(NarrativeFactKey.NexaPhytoCorruptionLevel).ToString());
                formatted = formatted.Replace("{TruthLevel}", facts.GetFact(NarrativeFactKey.NexaPhytoTruthLevel).ToString());
                formatted = formatted.Replace("{DeadCropCount}", facts.GetFact(NarrativeFactKey.DeadCropsCount).ToString());
                formatted = formatted.Replace("{WaterLiters}", facts.GetFact(NarrativeFactKey.TotalWellWaterDrawnLiters).ToString());
            }
            return formatted;
        }

        /// <summary>
        /// Factory method for creating runtime or mock story beats. Preserves backward compatibility.
        /// </summary>
        public static StoryBeatData CreateRuntimeInstance(
            string id,
            int chapter,
            StoryTriggerType trigger,
            StoryDeliveryType delivery,
            string title,
            string text,
            int day = 0,
            int wave = 0,
            EnemyType bossType = EnemyType.TuberMaw,
            JournalEntryData journal = null,
            NarrativeFactCondition[] criteria = null,
            NarrativeFactMutation[] mutations = null,
            RepeatPolicy repeatPolicy = RepeatPolicy.MatchOnce,
            int saliencePriority = 1,
            UnityEngine.Playables.PlayableAsset timeline = null,
            string speaker = null,
            bool shake = false,
            float shakeIntensity = 0.3f,
            float shakeDuration = 0.5f)
        {
            var beat = CreateInstance<StoryBeatData>();
            beat.beatId = id;
            beat.chapter = chapter;
            beat.triggerType = trigger;
            beat.deliveryType = delivery;
            beat.title = title;
            beat.narrativeText = text;
            beat.hudBannerText = text;
            beat.speakerName = speaker;
            beat.triggersScreenShake = shake;
            beat.screenShakeIntensity = shakeIntensity;
            beat.screenShakeDuration = shakeDuration;
            beat.triggerDay = day;
            beat.triggerWave = wave;
            beat.triggerBossType = bossType;
            beat.unlocksJournalEntry = journal != null;
            beat.journalEntry = journal;
            beat.criteria = criteria ?? Array.Empty<NarrativeFactCondition>();
            beat.mutations = mutations ?? Array.Empty<NarrativeFactMutation>();
            beat.repeatPolicy = repeatPolicy;
            beat.saliencePriority = Math.Max(1, saliencePriority);
            beat.timelineAsset = timeline;
            return beat;
        }
    }
}
