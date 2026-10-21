using System.Collections.Generic;
using UnityEngine;
using FeaturesStoryline.Data;
using FeaturesCombat;

namespace FeaturesStoryline
{
    /// <summary>
    /// Narrative database and factory providing complete story beats and archival research logs
    /// for the "NexaPhyto Bio-Hazard" campaign.
    /// Fully aligned with the official canon: abandoned research facility, chemical leakage in soil,
    /// mutated crop flora, daytime farming & defense preparation, and nighttime survival defense.
    /// Easily editable and extensible without modifying underlying systems.
    /// </summary>
    public static class StoryDatabase
    {
        public static List<JournalEntryData> CreateDefaultJournalEntries()
        {
            var entries = new List<JournalEntryData>
            {
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_01",
                    1,
                    "Deed of Sale & Environmental Waiver",
                    "Municipal Land Registry",
                    "PARCEL ACQUISITION RECORD — LOT 417-B\n\n" +
                    "Buyer acknowledges that this remote property was acquired 'as-is' following the abrupt liquidation " +
                    "of the NexaPhyto Bio-Agrochemicals Research Facility in 1989.\n\n" +
                    "Clause 14: The seller and municipality bear zero liability for any lingering subterranean chemical runoff, " +
                    "unsealed drainage conduits, or ecological anomalies. The buyer assumes sole responsibility for soil remediation.",
                    1
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_02",
                    1,
                    "Incident Report #042: Tank Rupture",
                    "Chief Safety Inspector Dr. V. Aris",
                    "URGENT MEMORANDUM — FACILITY EVACUATION\n\n" +
                    "Subterranean Storage Tank 04 has suffered catastrophic structural compromise. Approximately 10,000 liters " +
                    "of experimental mutagenic growth catalyst 'Phyto-Toxin Reagent 7' have escaped into the secondary drainage basin.\n\n" +
                    "The compound binds aggressively to organic plant matter, accelerating cellular division tenfold. Immediate quarantine ordered.",
                    2
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_03",
                    2,
                    "Bio-Study: Specimen T-Maw & Taro",
                    "Senior Bio-Engineer Dr. K. Vance",
                    "FIELD RESEARCH REPORT: REAGENT 7 ON VEGETATION\n\n" +
                    "1. Sweet Potato: High carbohydrate content rapidly metabolizes the mutagen into predatory jaws and subterranean burrowing musculature (Specimen: Tuber Maw).\n" +
                    "2. Taro: Thick fibrous root jackets absorb mineralized leachate, producing impenetrable armored bark (Specimen: Taro Brute). Avoid melee trades without dodge stamina.",
                    3
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_04",
                    3,
                    "Specimen C-1: The Cyclopean Neural Node",
                    "Dr. K. Vance",
                    "BIO-OBSERVATION LOG #109\n\n" +
                    "As chemical concentration peaks in the central drainage basin, adjacent root clusters fuse into a singular apex entity.\n\n" +
                    "Specimen C-1 (The Cyclops) develops a centralized sensory organ that coordinates nearby roots. Its ground-pound shockwaves " +
                    "threaten our perimeter barriers. Heavy poise damage is mandatory to stagger the subject.",
                    4
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_05",
                    4,
                    "Facility Sub-Floor Drainage Schematics",
                    "Facility Engineering Team",
                    "INFRASTRUCTURE ANALYSIS\n\n" +
                    "The old homestead was constructed directly above the facility's main chemical effluent junction. " +
                    "Every time the farmer tills and waters the topsoil, moisture carries dormant chemical crystals into the corroded pipes below, " +
                    "triggering a violent biochemical reaction in the roots.",
                    5
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_06",
                    4,
                    "Specimen T-Colossus: Structural Chitin",
                    "Dr. K. Vance",
                    "CRITICAL HAZARD CLASSIFICATION\n\n" +
                    "Taro roots exposed to prolonged chemical sludge form massive multi-ton subterranean anchors. " +
                    "The resulting abomination — The Colossus — possesses bone-crushing strength and near-impenetrable fibrous armor. " +
                    "Only high-tier upgraded weaponry can pierce its outer bark.",
                    6
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_07",
                    5,
                    "Emergency Neutralization Protocol",
                    "Facility Director M. Sterling",
                    "CONTINGENCY PROCEDURES\n\n" +
                    "In the event of total bio-containment failure, the automated emergency flush system can be triggered at the main drainage vent.\n\n" +
                    "Neutralizing reagent will flood the underground conduits, crystallizing all mutagenic sludge into inert fertilizer. " +
                    "However, the facility vent is fiercely guarded by our runaway genetic sentinel. The sentinel must be eliminated first.",
                    7
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_08",
                    5,
                    "Specimen Apex: The Ranger",
                    "Classified NexaPhyto Dossier",
                    "APEX BIO-SENTINEL DOSSIER\n\n" +
                    "The Ranger: An arboreal bio-engineered crop hybrid combining flexible corn stalk sinew with deadly toxic thorn projectiles. " +
                    "It defends the chemical breach with relentless speed and ranged suppression. Defeating it will trigger the automated purge valve.",
                    8
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_FARMER_PURGE",
                    5,
                    "The Soil Purified: Farmer's Diary",
                    "The Farmer",
                    "FINAL DIARY ENTRY\n\n" +
                    "The Ranger has fallen. As it collapsed across the fence, the automated emergency valve engaged with a deafening hiss.\n\n" +
                    "Golden neutralizing vapor surged through the corroded pipes, neutralizing the chemical seepage. The mutated roots withered away into rich, dark loam.\n\n" +
                    "The morning air is crisp and pure. The nightmare is over. This land is finally my true home.",
                    9
                )
            };

            return entries;
        }

        public static List<StoryBeatData> CreateDefaultStoryBeats(List<JournalEntryData> journals = null)
        {
            if (journals == null || journals.Count == 0)
            {
                journals = CreateDefaultJournalEntries();
            }

            var journalDict = new Dictionary<string, JournalEntryData>();
            foreach (var j in journals)
            {
                if (j != null && !string.IsNullOrEmpty(j.entryId))
                    journalDict[j.entryId] = j;
            }

            JournalEntryData GetJournal(string id) => journalDict.TryGetValue(id, out var j) ? j : null;

            var beats = new List<StoryBeatData>();

            // ==========================================
            // DAY 1: A CHEAP PARCEL OF LAND
            // ==========================================
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "PROLOGUE",
                1,
                StoryTriggerType.GameStart,
                StoryDeliveryType.FullScreenOverlay,
                "A Fresh Start",
                "You pooled your savings to buy a remote plot of land far from the city...\n" +
                "Yet the air smells faintly of sulfur and rusted iron. A weathered sign hangs on the front gate:\n" +
                "'NEXAPHYTO BIO-AGROCHEMICALS — RESTRICTED RESEARCH FACILITY'.",
                day: 1
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_MANIFEST_FOUND",
                1,
                StoryTriggerType.GameStart,
                StoryDeliveryType.JournalPage,
                "Purchase Deed & Waiver",
                "You find the old municipal transfer deed. NexaPhyto abandoned this site after a catastrophic incident.",
                day: 1,
                journal: GetJournal("LOG_NEXAPHYTO_01")
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_FIRST_PLANT",
                1,
                StoryTriggerType.FirstPlant,
                StoryDeliveryType.FloatingWorldText,
                "Chemical Sheen",
                "An iridescent chemical sheen glimmers upon the moist soil as you plant the seed...",
                day: 1
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_DUSK_WARNING",
                1,
                StoryTriggerType.Dusk,
                StoryDeliveryType.HudBanner,
                "Chemical Awakening",
                "As twilight falls, subterranean root clusters twitch and pulsate beneath your fields...",
                day: 1
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_NIGHT_START",
                1,
                StoryTriggerType.WaveStart,
                StoryDeliveryType.BossIntro,
                "The Mutants Emerge",
                "They came from your crops! Sweet potatoes have mutated into predatory Tuber Maws! Defend the farm!",
                day: 1,
                wave: 1
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_NIGHT_CLEARED",
                1,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Chemical Residue",
                "The creatures dissolved into dark chemical sludge. An old incident report is unearthed from the mud.",
                day: 1,
                journal: GetJournal("LOG_NEXAPHYTO_02")
            ));

            // ==========================================
            // DAY 2: CORRODED RUNOFF
            // ==========================================
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_DAWN_PIPES",
                2,
                StoryTriggerType.Dawn,
                StoryDeliveryType.HudBanner,
                "Corroded Drainage",
                "Rusted industrial drainage pipes protrude from the ditch near your farmland.",
                day: 2
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_FIRST_HARVEST",
                2,
                StoryTriggerType.FirstHarvest,
                StoryDeliveryType.FloatingWorldText,
                "Mutated Roots",
                "Dark fibrous chemical veins run through the harvested crops. The roots thrive on the toxic leachate.",
                day: 2
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_NIGHT_START",
                2,
                StoryTriggerType.WaveStart,
                StoryDeliveryType.HudBanner,
                "Reinforced Abominations",
                "Taro crops have evolved into armored Taro Brutes! Keep your stamina ready to dodge!",
                day: 2,
                wave: 1
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_NIGHT_CLEARED",
                2,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Specimen Notes Unlocked",
                "You find Dr. Vance's lab notes explaining how Reagent 7 hardens root cell walls into armor.",
                day: 2,
                journal: GetJournal("LOG_NEXAPHYTO_03")
            ));

            // ==========================================
            // DAY 3: THE TOXIC NODE
            // ==========================================
            var d3Dawn = StoryBeatData.CreateRuntimeInstance(
                "D3_DAWN_PULSE",
                3,
                StoryTriggerType.Dawn,
                StoryDeliveryType.HudBanner,
                "Subterranean Pressure",
                "A low rumble vibrates through the water well. Chemical sludge accumulates in the drainage basin.",
                day: 3
            );
            d3Dawn.triggersScreenShake = true;
            d3Dawn.screenShakeIntensity = 0.4f;
            beats.Add(d3Dawn);

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_SLEEP_DREAM",
                3,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Toxic Vision",
                "In fitful sleep, you see ruptured chemical storage vats and a massive single eye opening in the subterranean sludge.",
                day: 3
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_BOSS_INTRO",
                3,
                StoryTriggerType.BossSpawn,
                StoryDeliveryType.BossIntro,
                "CYCLOPS TUBER MAW",
                "Concentrated chemical mutagen has merged a dozen root clusters into a single-eyed predator!",
                day: 3,
                wave: 3,
                bossType: EnemyType.CyclopsTuberMaw
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_NIGHT_CLEARED",
                3,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Apex Neural Node",
                "The Cyclops core shatters into crystallized chemical residue. Facility report C-1 recovered.",
                day: 3,
                journal: GetJournal("LOG_NEXAPHYTO_04")
            ));

            // ==========================================
            // DAY 4: GROUNDWATER SATURATION
            // ==========================================
            var d4Dawn = StoryBeatData.CreateRuntimeInstance(
                "D4_DAWN_TREMORS",
                4,
                StoryTriggerType.Dawn,
                StoryDeliveryType.HudBanner,
                "Groundwater Tremors",
                "Severe tremors spider across the farmland. Chemical fumes hiss from subterranean fissures.",
                day: 4
            );
            d4Dawn.triggersScreenShake = true;
            d4Dawn.screenShakeIntensity = 0.5f;
            beats.Add(d4Dawn);

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D4_MERCHANT_FLAVOR",
                4,
                StoryTriggerType.MerchantOpen,
                StoryDeliveryType.DialogueBox,
                "Wary Merchant",
                "Merchant: 'Hey... these vegetables are glowing under the lamp. Where did you dig these up? Watch your back tonight.'",
                day: 4
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D4_BOSS_INTRO",
                4,
                StoryTriggerType.BossSpawn,
                StoryDeliveryType.BossIntro,
                "THE TARO COLOSSUS",
                "An ancient subterranean growth, bloated with decades of chemical sludge, erupts from the earth!",
                day: 4,
                wave: 4,
                bossType: EnemyType.TaroColossus
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D4_NIGHT_CLEARED",
                4,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Drainage Blueprint Found",
                "You discover the facility drainage schematics and Colossus hazard dossier.",
                day: 4,
                journal: GetJournal("LOG_NEXAPHYTO_05")
            ));

            // ==========================================
            // DAY 5: THE FINAL CONTAINMENT
            // ==========================================
            var d5Dawn = StoryBeatData.CreateRuntimeInstance(
                "D5_DAWN_REDSKY",
                5,
                StoryTriggerType.Dawn,
                StoryDeliveryType.HudBanner,
                "Critical Chemical Surge",
                "The sky bleeds dark crimson with toxic smog. Ruined facility sirens wail in the distance. The final breach is imminent.",
                day: 5
            );
            d5Dawn.triggersAtmosphericChange = true;
            beats.Add(d5Dawn);

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D5_DUSK_DISTORTED",
                5,
                StoryTriggerType.Dusk,
                StoryDeliveryType.HudBanner,
                "Emergency Alarm",
                "The facility siren distorts into a continuous alarm. Maximum containment breach in progress!",
                day: 5
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D5_BOSS1_INTRO",
                5,
                StoryTriggerType.BossSpawn,
                StoryDeliveryType.BossIntro,
                "COLOSSUS RE-EMERGENCE",
                "The Colossus rises anew, empowered by the facility's core chemical discharge!",
                day: 5,
                wave: 4,
                bossType: EnemyType.TaroColossus
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D5_BOSS2_INTRO",
                5,
                StoryTriggerType.BossSpawn,
                StoryDeliveryType.BossIntro,
                "THE RANGER — APEX BIO-SENTINEL",
                "NexaPhyto's runaway genetic sentinel manifests! Slay it to trigger the automated neutralization purge!",
                day: 5,
                wave: 5,
                bossType: EnemyType.TheRanger
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D5_VICTORY",
                5,
                StoryTriggerType.BossDeath,
                StoryDeliveryType.FullScreenOverlay,
                "Containment Neutralized",
                "With the Ranger slain, the automated purge valve opens with a deafening roar!\n" +
                "Neutralizing vapor floods the subterranean pipes, crystallizing the toxic sludge into inert soil.\n" +
                "The air clears. The farm is safe at last.",
                day: 5,
                bossType: EnemyType.TheRanger,
                journal: GetJournal("LOG_FARMER_PURGE")
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "EPILOGUE_STATS",
                5,
                StoryTriggerType.Custom,
                StoryDeliveryType.FullScreenOverlay,
                "The Reclaimed Homestead",
                "You have survived the toxic siege of NexaPhyto Facility.\n" +
                "Through resilience, farming, and fierce defense, you have reclaimed your new life.",
                day: 5,
                journal: GetJournal("LOG_NEXAPHYTO_07")
            ));

            return beats;
        }
    }
}
