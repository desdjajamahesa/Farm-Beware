using System.Collections.Generic;
using UnityEngine;
using FeaturesStoryline.Data;
using FeaturesCombat;
using FarmBeware.Core.Runtime;

namespace FeaturesStoryline
{
    /// <summary>
    /// Narrative database and factory providing complete story beats, archival research logs,
    /// and pre-allocated trigger bucket caches for the "NexaPhyto Bio-Hazard" campaign.
    /// Fully aligned with the official canon: abandoned research facility, chemical leakage in soil,
    /// mutated crop flora, daytime farming & defense preparation, and nighttime survival defense.
    /// Supports Quality-Based Narrative (QBN) criteria, specificity scoring, and Zero-GC pre-baked lookups.
    /// </summary>
    public static class StoryDatabase
    {
        private static Dictionary<StoryTriggerType, List<StoryBeatData>> _triggerBuckets;
        private static List<StoryBeatData> _cachedBeats;
        private static List<JournalEntryData> _cachedJournals;

        public static List<JournalEntryData> CreateDefaultJournalEntries()
        {
            var entries = new List<JournalEntryData>
            {
                // Canonical Dossier 1: Purchase Deed & Environmental Waiver Clause 14
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_01_WAIVER",
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
                // Canonical Dossier 2: Chemical Reagent 7 Research Memo
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_02_REAGENT7",
                    2,
                    "Memo: Phyto-Toxin Reagent 7 Properties",
                    "Dr. Elena Vance, Lead Biochemist",
                    "INTERNAL RESEARCH MEMO — CLASSIFIED\n\n" +
                    "Compound: Phyto-Toxin Reagent 7 (Experimental Growth Catalyst).\n\n" +
                    "Mechanism: Binds aggressively to organic vascular plant tissue, accelerating cell division 400-fold upon darkness exposure.\n" +
                    "Side Effects: Severe mineral absorption in root systems, producing chitinous bark armor and aggressive territorial reflex.",
                    2
                ),
                // Canonical Dossier 3: Tank 04 Rupture Emergency Report
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_03_INCIDENT042",
                    3,
                    "Incident Report #042: Tank 04 Rupture",
                    "Chief Safety Inspector Dr. V. Aris",
                    "URGENT MEMORANDUM — FACILITY EVACUATION\n\n" +
                    "Subterranean Storage Tank 04 has suffered catastrophic structural failure. Over 10,000 liters " +
                    "of experimental mutagenic growth catalyst 'Phyto-Toxin Reagent 7' have escaped into the secondary drainage basin.\n\n" +
                    "The spill sits directly beneath Lot 417-B. Management ordered concrete bulkhead sealing instead of decontamination.",
                    3
                ),
                // Canonical Dossier 4: Military Apex Bio-Sentinel Dossier
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_04_APEX_BIO",
                    4,
                    "Military Specimen Dossier: C-1 & Apex Sentinel",
                    "Col. R. Darmawan, Bio-Defense Project",
                    "SPECIMEN CLASSIFICATION REPORT\n\n" +
                    "1. Specimen C-1 (Cyclops Tuber Maw): Multi-root colony fusion forming a centralized photoreceptive ocular neural node.\n" +
                    "2. Specimen T-Colossus: Multi-ton subterranean root anchor absorbing bedrock silicate for seismic slam resilience.\n" +
                    "3. Specimen Apex (The Ranger): Stationary arboreal bio-sentinel with pressurized ballistic seed chambers guarding the effluent conduits.",
                    4
                ),
                // Canonical Dossier 5: Automated Emergency Relief Valve Blueprint
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_05_FAILSAFE",
                    5,
                    "Automated Emergency Relief Valve Blueprint",
                    "Dr. Elena Vance, Final Evacuation Note",
                    "FACILITY COUNTERMEASURE BLUEPRINT\n\n" +
                    "The automated emergency relief valve contains pressurized phosphate-alkali neutralizing vapor.\n\n" +
                    "If hydrostatic pressure peaks, the valve will automatically discharge, neutralizing all Reagent 7 into inert agricultural fertilizer.\n" +
                    "However, the valve is physically obstructed by the root core of The Ranger. Eliminating the Sentinel releases the release latch.",
                    5
                ),
                // Retained backward-compatible legacy aliases
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_01", 1, "Deed of Sale & Environmental Waiver", "Municipal Land Registry",
                    "PARCEL ACQUISITION RECORD — LOT 417-B\nAcquired 'as-is' following 1989 NexaPhyto liquidation. Clause 14 environmental waiver active.", 1
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_02", 1, "Incident Report #042: Tank Rupture", "Dr. V. Aris",
                    "Over 10,000 liters of Phyto-Toxin Reagent 7 escaped into the secondary drainage basin directly beneath Lot 417-B.", 2
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_03", 2, "Bio-Study: Specimen T-Maw & Taro", "Dr. K. Vance",
                    "Sweet potatoes metabolize mutagen into burrowing Tuber Maws. Taro roots absorb leachate into armored bark.", 3
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_04", 3, "Specimen C-1: The Cyclopean Neural Node", "Dr. K. Vance",
                    "Adjacent roots fuse into Specimen C-1, coordinating horde swarms with ground-pound shockwaves.", 4
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_05", 4, "Facility Sub-Floor Drainage Schematics", "Engineering Team",
                    "The homestead was built over the main chemical effluent pipe. Watering crops leaches dormant crystals into roots below.", 5
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_06", 4, "Specimen T-Colossus: Structural Chitin", "Dr. K. Vance",
                    "Prolonged chemical exposure forms multi-ton anchors. The Colossus possesses bone-crushing strength.", 6
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_07", 5, "Emergency Neutralization Protocol", "Facility Director M. Sterling",
                    "Neutralizing reagent will flood conduits upon defeating the runaway Sentinel guarding the vent.", 7
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_08", 5, "Specimen Apex: The Ranger", "Classified Dossier",
                    "The Ranger: Arboreal hybrid combining flexible corn stalk sinew with deadly thorn projectiles.", 8
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_FARMER_PURGE", 5, "The Soil Purified: Farmer's Diary", "The Farmer",
                    "The Ranger has fallen. The automated purge valve engaged, neutralizing the soil into rich loam. The farm is saved.", 9
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
                StoryDeliveryType.TimelineCutscene,
                "A Fresh Start",
                "You pooled your savings to buy a remote plot of land far from the city...\n" +
                "Yet the air smells faintly of sulfur and rusted iron. A weathered sign hangs on the front gate:\n" +
                "'NEXAPHYTO BIO-AGROCHEMICALS — RESTRICTED RESEARCH FACILITY'.",
                day: 0,
                mutations: new[] {
                    new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 1),
                    new NarrativeFactMutation(NarrativeFactKey.IntroCutsceneCompleted, FactMutationOp.Set, 1)
                },
                saliencePriority: 5
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_MANIFEST_FOUND",
                1,
                StoryTriggerType.GameStart,
                StoryDeliveryType.JournalPage,
                "Purchase Deed & Waiver",
                "You find the old municipal transfer deed. NexaPhyto abandoned this site after a catastrophic incident in 1989.",
                day: 1,
                criteria: new[] {
                    new NarrativeFactCondition(NarrativeFactKey.CurrentDay, FactOperator.GreaterOrEqual, 1, 10),
                    new NarrativeFactCondition(NarrativeFactKey.IntroCutsceneCompleted, FactOperator.Equal, 1, 10)
                },
                journal: GetJournal("JOURNAL_01_WAIVER") ?? GetJournal("LOG_NEXAPHYTO_01"),
                saliencePriority: 2
            ));

            // Reactive Storylet: First well water draw
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_WELL_WATER_100",
                1,
                StoryTriggerType.WellWaterRefilled,
                StoryDeliveryType.HudBanner,
                "Chilled Well Water",
                "The well water is ice-cold, carrying a faint sweet chemical scent like ancient agricultural pesticide.",
                day: 1,
                criteria: new[] { new NarrativeFactCondition(NarrativeFactKey.TotalWellWaterDrawnLiters, FactOperator.GreaterOrEqual, 100, 20) },
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Max, 2) },
                saliencePriority: 2
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
                journal: GetJournal("JOURNAL_02_REAGENT7") ?? GetJournal("LOG_NEXAPHYTO_02"),
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 3) },
                saliencePriority: 3
            ));

            // Dream Sequence 1: Well Roots Nightmare
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_01_WELL",
                1,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Nightmare of the Well",
                "In your sleep, you pull the heavy rope from the stone well. As the bucket surfaces, " +
                "it overflows not with water, but with thousands of whispering sweet potato vines mimicking the creak of the pulley...",
                day: 1,
                saliencePriority: 3
            ));

            // ==========================================
            // DAY 2: CORRODED RUNOFF & TARO HARDENING
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

            // Reactive Storylet: Washing Taro in Sink
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_SINK_WASH_TARO",
                2,
                StoryTriggerType.CropWashedInSink,
                StoryDeliveryType.HudBanner,
                "Mineralized Taro Bark",
                "As tap water washes over the taro roots, the skins harden like riverstone... Dull clinks echo in the sink basin.",
                day: 2,
                criteria: new[] {
                    new NarrativeFactCondition(NarrativeFactKey.CurrentDay, FactOperator.Equal, 2, 10),
                    new NarrativeFactCondition(NarrativeFactKey.PlantedCount_Taro, FactOperator.GreaterOrEqual, 1, 20)
                },
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Max, 4) },
                saliencePriority: 3
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
                journal: GetJournal("JOURNAL_03_INCIDENT042") ?? GetJournal("LOG_NEXAPHYTO_03")
            ));

            // Dream Sequence 2: Petrified Skin
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_02_SINK",
                2,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Nightmare of the Sink",
                "You dream you are washing soil from your hands at the kitchen sink, but your forearms harden into thick taro bark, " +
                "while the faucet pours thick, iridescent cyan runoff...",
                day: 2,
                saliencePriority: 3
            ));

            // ==========================================
            // DAY 3: THE TOXIC NODE & CORN CROSSFIRE
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

            // Reactive Storylet: Workbench Weapon Upgrade
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_WORKBENCH_UPGRADE",
                3,
                StoryTriggerType.WeaponUpgradedAtBench,
                StoryDeliveryType.HudBanner,
                "Bio-Mechanical Weaponry",
                "Mutated fibrous sinew bonds into the weapon hilt, pulsing with bio-luminescent chemical energy.",
                day: 3,
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Max, 6) },
                saliencePriority: 2
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
                bossType: EnemyType.CyclopsTuberMaw,
                saliencePriority: 4
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_NIGHT_CLEARED",
                3,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Apex Neural Node",
                "The Cyclops core shatters into crystallized chemical residue. Facility report C-1 recovered.",
                day: 3,
                journal: GetJournal("JOURNAL_04_APEX_BIO") ?? GetJournal("LOG_NEXAPHYTO_04"),
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 7) }
            ));

            // Dream Sequence 3: Conduits Under the Bedroom
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_03_PIPES",
                3,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Vision of the Catacombs",
                "Your perspective sinks through the bedroom floorboards, revealing the corroded drainage pipes of Tank 04 " +
                "coiled around the house foundations like petrified serpents...",
                day: 3,
                saliencePriority: 3
            ));

            // ==========================================
            // DAY 4: GROUNDWATER SATURATION & COLOSSUS
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
                bossType: EnemyType.TaroColossus,
                journal: GetJournal("LOG_NEXAPHYTO_06"),
                saliencePriority: 4
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D4_NIGHT_CLEARED",
                4,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Drainage Blueprint Found",
                "You discover the facility drainage schematics and Colossus hazard dossier.",
                day: 4,
                journal: GetJournal("JOURNAL_05_FAILSAFE") ?? GetJournal("LOG_NEXAPHYTO_05"),
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 9) }
            ));

            // Dream Sequence 4: The Ocular Reflection
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_04_OCULAR",
                4,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "The Mirror's Gaze",
                "You stand before the wardrobe mirror in your bedroom. In place of your reflection, " +
                "a massive bio-luminescent ocular stalk stares back, bathing the room in pulsing crimson...",
                day: 4,
                saliencePriority: 3
            ));

            // ==========================================
            // DAY 5: THE FINAL CONTAINMENT & RESOLUTION
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
                day: 5,
                journal: GetJournal("LOG_NEXAPHYTO_07")
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
                bossType: EnemyType.TaroColossus,
                saliencePriority: 4
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
                bossType: EnemyType.TheRanger,
                journal: GetJournal("LOG_NEXAPHYTO_08"),
                saliencePriority: 5
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
                journal: GetJournal("LOG_FARMER_PURGE"),
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 10) },
                saliencePriority: 6
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "EPILOGUE_STATS",
                5,
                StoryTriggerType.Custom,
                StoryDeliveryType.FullScreenOverlay,
                "The Reclaimed Homestead",
                "You have survived the toxic siege of NexaPhyto Facility.\n" +
                "Through resilience, farming, and fierce defense, you have reclaimed your new life.",
                day: 5
            ));

            // Dream Sequence 5: Purified Soil
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_05_DAWN",
                5,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Dawn of the Purified Soil",
                "The low rumble in the pipes ceases forever. Pure morning dew gathers on peaceful sweet potato and taro leaves. " +
                "Lot 417-B is finally your true home.",
                day: 5,
                saliencePriority: 3
            ));

            // Populate pre-baked trigger buckets cache
            BakeTriggerBuckets(beats);

            return beats;
        }

        public static void BakeTriggerBuckets(List<StoryBeatData> beats)
        {
            _cachedBeats = beats;
            _triggerBuckets = new Dictionary<StoryTriggerType, List<StoryBeatData>>();

            if (beats == null) return;

            for (int i = 0; i < beats.Count; i++)
            {
                var b = beats[i];
                if (b == null) continue;

                if (!_triggerBuckets.TryGetValue(b.triggerType, out var list))
                {
                    list = new List<StoryBeatData>();
                    _triggerBuckets[b.triggerType] = list;
                }
                list.Add(b);
            }
        }

        public static IReadOnlyList<StoryBeatData> GetBeatsForTrigger(StoryTriggerType trigger)
        {
            if (_triggerBuckets == null)
            {
                CreateDefaultStoryBeats();
            }

            if (_triggerBuckets.TryGetValue(trigger, out var list))
            {
                return list;
            }

            return System.Array.Empty<StoryBeatData>();
        }
    }
}
