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
                // Canonical Dossier 1: Cheap Land Deed
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_01_WAIVER",
                    1,
                    "Cheap Land Deed",
                    "Municipal Land Registry / The Veteran",
                    "PARCEL ACQUISITION RECORD — LOT 417-B\n\n" +
                    "I used my military severance to buy this land for next to nothing. The contract had an environmental waiver mentioning an old factory from 1989 that was shut down and buried.\n\n" +
                    "Clause 14 basically said: 'Buy at your own risk. Municipality is not responsible for whatever is left underneath.' I didn't think much of it at the time. Land is land.",
                    1
                ),
                // Canonical Dossier 2: Recovered Warning Tag
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_02_REAGENT7",
                    2,
                    "Recovered Warning Tag",
                    "Scraped Metal Plaque",
                    "STAMPED STEEL TAG — DUST & RUST\n\n" +
                    "I took a wire brush to the metal sign I hit with my hoe. The stamped letters read:\n" +
                    "'WARNING: EXPERIMENTAL AGRO-CHEMICAL REAGENT 7. DO NOT DISTURB SUB-SURFACE CONDUITS.'\n\n" +
                    "The corner of the metal was sheared off when my hoe pulled it. That's right where the green fluid started bubbling up.",
                    2
                ),
                // Canonical Dossier 3: Incident 042: Buried Tanks
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_03_INCIDENT042",
                    3,
                    "Incident 042: Buried Tanks",
                    "Old Factory Evacuation Sheet",
                    "FACILITY LOG ARCHIVE — 1989\n\n" +
                    "A pressurized containment tank ruptured beneath this soil over thirty years ago. Instead of cleaning it up, management poured quick-drying concrete over the conduits and abandoned the facility.\n\n" +
                    "The chemicals stayed trapped in the dark... until my hoe cracked the outer casing yesterday morning.",
                    3
                ),
                // Canonical Dossier 4: Mutant Combat Dossier
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_04_APEX_BIO",
                    4,
                    "Mutant Combat Dossier",
                    "Veteran's Field Observations",
                    "TACTICAL LOG — DEFENSE ANALYSIS\n\n" +
                    "1. Tuber Maws: Fast and erratic, but soft skin. Easy to bring down with quick strikes.\n" +
                    "2. Taro Brutes: Armored in petrified plant bark. Need heavy weapons and timed dodges to crack their plating.\n" +
                    "3. The Giant Sentinel: A massive mutant root system controlling the flow of chemicals through the broken factory pipes.",
                    4
                ),
                // Canonical Dossier 5: Emergency Clean-Up Valve Blueprint
                JournalEntryData.CreateRuntimeInstance(
                    "JOURNAL_05_FAILSAFE",
                    5,
                    "Emergency Clean-Up Valve Blueprint",
                    "Waterproof Conduit Schematic",
                    "DRAINAGE SYSTEM BLUEPRINT — VALVE #09\n\n" +
                    "Found inside a sealed conduit pipe: There's an emergency neutralization valve located right beneath the central field.\n\n" +
                    "When opened, it floods the underground pipes with neutralizing gas that renders the chemical harmless. But the main valve wheel is strangled by the giant root boss.",
                    5
                ),
                // Additional canonical dossiers
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_06", 4, "Heavy Armor Observations", "Veteran's Weapon Bench Notes",
                    "The taro roots pulled from the ground don't rot. They harden into composite armor within hours of being exposed to well water. Wrapping these dried fibers around my weapons adds serious kinetic weight.", 6
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_07", 5, "Neutralizing Gas Specs", "Maintenance Memo",
                    "The emergency relief tank contains high-pressure neutralizing vapor. One burst is enough to neutralize the entire subterranean basin. All I need to do is clear the roots off the release lever.", 7
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_NEXAPHYTO_08", 5, "The Root of the Problem", "Veteran's Threat Assessment",
                    "The monster guarding the central valve isn't just an overgrown plant—it's plugged directly into the ruptured chemical conduit. It won't let go of the valve willingly. Time to treat this like a combat breach.", 8
                ),
                JournalEntryData.CreateRuntimeInstance(
                    "LOG_FARMER_PURGE", 5, "Peace at Last: Veteran's Journal", "The Veteran",
                    "The big monster is down. The emergency valve popped open with a thunderous hiss, and white neutralizing gas swept through the cracked pipes. The green slime dissolved into clean, rich soil.\n\n" +
                    "No more monsters. No more war. Tomorrow morning, I can finally be just a farmer.", 9
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
                "A Quiet Life",
                "After years in the military, this remote farm is finally yours. Head inside the cabin and get some rest. Tomorrow, the real farm work begins.",
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
                StoryTriggerType.Dawn,
                StoryDeliveryType.DialogueBox,
                "New Plot to Clear",
                "Morning already. The main garden beds are ready, but there's still a patch of untouched dirt by the edge of the field. Time to grab the hoe and plant some seeds.",
                day: 1,
                criteria: new[] {
                    new NarrativeFactCondition(NarrativeFactKey.CurrentDay, FactOperator.GreaterOrEqual, 1, 10),
                    new NarrativeFactCondition(NarrativeFactKey.IntroCutsceneCompleted, FactOperator.Equal, 1, 10)
                },
                journal: GetJournal("JOURNAL_01_WAIVER"),
                saliencePriority: 2,
                speaker: "Veteran"
            ));

            // Reactive Storylet: First well water draw
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_WELL_WATER_100",
                1,
                StoryTriggerType.WellWaterRefilled,
                StoryDeliveryType.DialogueBox,
                "Weird Smell in the Well",
                "Wait a second... the well water feels colder than usual today, and it has a strange metallic smell. Did my digging earlier shake something loose underground?",
                day: 1,
                criteria: new[] { new NarrativeFactCondition(NarrativeFactKey.TotalWellWaterDrawnLiters, FactOperator.GreaterOrEqual, 100, 20) },
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Max, 2) },
                saliencePriority: 2,
                speaker: "Veteran"
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_FIRST_PLANT",
                1,
                StoryTriggerType.FirstPlant,
                StoryDeliveryType.DialogueBox,
                "Something Under the Dirt",
                "CLANG! My hoe just hit something metal buried deep in the dirt. I pulled it up—it's a bent, rusty warning sign from an old factory. The letters are almost completely faded. Probably just old junk. I tossed it to the side of the field and got back to work... unaware that pulling it out cracked a sealed underground pipe right below my farm.",
                day: 1,
                speaker: "Veteran",
                shake: true,
                shakeIntensity: 0.35f
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_DUSK_WARNING",
                1,
                StoryTriggerType.Dusk,
                StoryDeliveryType.DialogueBox,
                "Growing Way Too Fast",
                "That's crazy. The seeds I planted earlier today are already growing huge. At first I thought the soil was just rich, but now the vines are actually twitching in the dirt...",
                day: 1,
                speaker: "Veteran",
                shake: true,
                shakeIntensity: 0.25f
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_NIGHT_START",
                1,
                StoryTriggerType.WaveStart,
                StoryDeliveryType.BossIntro,
                "WHAT IS THAT?!",
                "The sweet potatoes just ripped themselves out of the ground! They turned into monsters! Good thing my army reflexes are still sharp—defend the farm!",
                day: 1,
                wave: 1
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D1_NIGHT_CLEARED",
                1,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Not Just Normal Soil",
                "They melted into glowing green slime. Normal plants don't grow teeth. This all started right after my hoe hit that buried metal sign today. Something toxic is leaking under my land.",
                day: 1,
                journal: GetJournal("JOURNAL_02_REAGENT7"),
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 3) },
                saliencePriority: 3
            ));

            // Dream Sequence 1: Well Roots Nightmare
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_01_WELL",
                1,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Echo of the Hoe",
                "You dream of your old army days, hearing the metallic clink of a shovel hitting a buried landmine—except when you look down, it's your farm hoe hitting that rusty factory sign, and glowing green water is seeping up around your boots.",
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
                StoryDeliveryType.DialogueBox,
                "Checking the Old Sign",
                "I went back to the edge of the field and scraped the rust off that metal sign I dug up yesterday. Beneath the dirt, I can still read part of it: 'NEXAPHYTO FACTORY — DANGER: CHEMICAL TANK 04'. So this land wasn't just an ordinary factory...",
                day: 2,
                speaker: "Veteran"
            ));

            // Reactive Storylet: Washing Taro in Sink
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_SINK_WASH_TARO",
                2,
                StoryTriggerType.CropWashedInSink,
                StoryDeliveryType.DialogueBox,
                "Hard as Armor",
                "When I washed the dirt off this taro in the sink, the skin turned hard as steel. It actually clinked against the metal basin. If these things mutate tonight, they're going to have heavy armor.",
                day: 0,
                criteria: new[] {
                    new NarrativeFactCondition(NarrativeFactKey.CurrentDay, FactOperator.GreaterOrEqual, 2, 10),
                    new NarrativeFactCondition(NarrativeFactKey.DirtyCropsWashedCount, FactOperator.GreaterOrEqual, 1, 10),
                    new NarrativeFactCondition(NarrativeFactKey.WashedCount_Taro, FactOperator.GreaterOrEqual, 1, 20)
                },
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Max, 4) },
                saliencePriority: 3,
                speaker: "Veteran"
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_FIRST_HARVEST",
                2,
                StoryTriggerType.FirstHarvest,
                StoryDeliveryType.DialogueBox,
                "Glowing Veins",
                "Look at the roots on this harvest. They grew twice as fast overnight, and there are weird glowing veins inside them. Whatever leaked from that crack is feeding the plants.",
                day: 2,
                speaker: "Veteran"
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_NIGHT_START",
                2,
                StoryTriggerType.WaveStart,
                StoryDeliveryType.HudBanner,
                "Armored Monsters",
                "Just as I feared—the taro crops mutated into heavy brutes with rock-hard skin! Watch their swings and dodge!",
                day: 2,
                wave: 1
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D2_NIGHT_CLEARED",
                2,
                StoryTriggerType.WaveCleared,
                StoryDeliveryType.JournalPage,
                "Cracked Concrete Below",
                "Where the brutes burst out of the ground, the dirt caved in a little. I found an old waterproof tube near a cracked pipe. This whole farm is sitting right on top of a buried chemical dump.",
                day: 2,
                journal: GetJournal("JOURNAL_03_INCIDENT042")
            ));

            // Dream Sequence 2: Petrified Skin
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_02_SINK",
                2,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Spreading Cracks",
                "In your sleep, you hear the old metal sign creaking at the edge of the field. Underneath the soil, cracks are spreading like tree roots, drinking the water from your well.",
                day: 2,
                saliencePriority: 3
            ));

            // ==========================================
            // DAY 3: THE TOXIC NODE & CORN CROSSFIRE
            // ==========================================
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_DAWN_PULSE",
                3,
                StoryTriggerType.Dawn,
                StoryDeliveryType.HudBanner,
                "The Leak is Spreading",
                "The ground is shaking around the water well. That small crack I opened on Day 1 is getting wider, pushing more factory chemicals into the groundwater.",
                day: 3,
                shake: true,
                shakeIntensity: 0.4f
            ));

            // Reactive Storylet: Workbench Weapon Upgrade
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_WORKBENCH_UPGRADE",
                3,
                StoryTriggerType.WeaponUpgradedAtBench,
                StoryDeliveryType.DialogueBox,
                "Veteran Ingenuity",
                "The roots left behind by those monsters don't rot—they dry harder than iron. Back in the army, we used whatever we had to survive. Wrapping these fibers around my weapon makes it hit twice as hard.",
                day: 3,
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Max, 6) },
                saliencePriority: 2,
                speaker: "Veteran"
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D3_BOSS_INTRO",
                3,
                StoryTriggerType.BossSpawn,
                StoryDeliveryType.BossIntro,
                "CYCLOPS TUBER MAW",
                "A dozen mutated roots under the field just fused together into one massive one-eyed monster! Keep moving and watch its ground slam!",
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
                "What Lies Beneath",
                "That giant thing tore open a deep hole when it surfaced. Inside the rubble, I found an old inspection folder. I finally know what happened here in 1989.",
                day: 3,
                journal: GetJournal("JOURNAL_04_APEX_BIO"),
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 7) }
            ));

            // Dream Sequence 3: Conduits Under the Bedroom
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_03_PIPES",
                3,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Beneath the Floorboards",
                "You dream you can see right through the cabin floorboards. Deep below your house, giant rusted factory pipes are wrapped in thick, breathing plant roots.",
                day: 3,
                saliencePriority: 3
            ));

            // ==========================================
            // DAY 4: GROUNDWATER SATURATION & COLOSSUS
            // ==========================================
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D4_DAWN_TREMORS",
                4,
                StoryTriggerType.Dawn,
                StoryDeliveryType.HudBanner,
                "Pipes Under Pressure",
                "Heavy tremors across the field. Green smoke is hissing out of the dirt where my hoe hit that sign days ago. The underground tank is about to blow.",
                day: 4,
                shake: true,
                shakeIntensity: 0.5f
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D4_MERCHANT_FLAVOR",
                4,
                StoryTriggerType.MerchantOpen,
                StoryDeliveryType.DialogueBox,
                "Nervous Trader",
                "Hey, soldier... your crops are huge, sure, but why are the roots glowing in the dark? Whatever you dug up on this farm, watch your back tonight.",
                day: 4,
                speaker: "Merchant"
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D4_BOSS_INTRO",
                4,
                StoryTriggerType.BossSpawn,
                StoryDeliveryType.BossIntro,
                "THE TARO COLOSSUS",
                "A massive beast covered in stone-hard bark just broke through the old factory pipes! Don't let it crush you!",
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
                "A Way to Stop the Leak",
                "In the broken pipe beneath the Colossus, I found the old factory blueprint! There's an emergency clean-up valve underground that can neutralize all the chemicals—if I can clear the giant roots blocking it.",
                day: 4,
                journal: GetJournal("JOURNAL_05_FAILSAFE"),
                mutations: new[] { new NarrativeFactMutation(NarrativeFactKey.NexaPhytoTruthLevel, FactMutationOp.Set, 9) }
            ));

            // Dream Sequence 4: The Ocular Reflection
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_04_OCULAR",
                4,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "The Blocked Valve",
                "A muffled alarm echoes in your dream. Down in the dark beneath the farm, a massive tangle of roots is squeezing a heavy metal valve shut, waiting for tomorrow night.",
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
                StoryDeliveryType.DialogueBox,
                "The Final Stand",
                "Red chemical gas is pouring out of the vents, turning the whole sky dark red. If I run away now, I lose everything I worked for. One more night. I finish this tonight.",
                day: 5,
                speaker: "Veteran"
            );
            d5Dawn.triggersAtmosphericChange = true;
            beats.Add(d5Dawn);

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D5_DUSK_DISTORTED",
                5,
                StoryTriggerType.Dusk,
                StoryDeliveryType.HudBanner,
                "Old Factory Siren",
                "An old emergency siren is echoing from deep underground! Lock and load—the biggest mutation is coming up right now!",
                day: 5,
                journal: GetJournal("LOG_NEXAPHYTO_07")
            ));

            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "D5_BOSS1_INTRO",
                5,
                StoryTriggerType.BossSpawn,
                StoryDeliveryType.BossIntro,
                "COLOSSUS RETURNS",
                "Another giant brute is climbing out of the main trench! Put it down fast!",
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
                "THE RANGER — ROOT OF THE LEAK",
                "There it is! That massive corn monster has its roots wrapped right around the underground clean-up valve! Kill it to release the valve and save the farm!",
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
                "The Leak is Sealed",
                "With the giant monster dead, its roots finally let go of the underground valve!\nA loud hiss echoes beneath the farm as clean neutralizing gas floods the broken pipes, turning all the toxic slime into harmless dirt.\nThe air clears. The fight is finally over.",
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
                "Peace Earned",
                "I came here to leave the battlefield behind, only to accidentally wake up a buried nightmare under my own soil.\nFive days of hard work and five nights of survival—now this farm is truly safe.",
                day: 5
            ));

            // Dream Sequence 5: Purified Soil
            beats.Add(StoryBeatData.CreateRuntimeInstance(
                "DREAM_05_DAWN",
                5,
                StoryTriggerType.Sleep,
                StoryDeliveryType.DreamSequence,
                "Quiet Morning Rain",
                "No more gunfire, no more underground rumbling. You dream of quiet morning rain falling over clean, normal green fields. You are finally home.",
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
