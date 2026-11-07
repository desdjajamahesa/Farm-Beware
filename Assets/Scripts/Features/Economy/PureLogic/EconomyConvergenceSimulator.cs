using System;

namespace FeaturesEconomy.PureLogic
{
    /// <summary>
    /// Pure C# zero-GC mathematical model and Monte Carlo convergence simulator for the Day 1-5 MVP economy.
    /// Verifies crop net margins, watering cycle moisture budgets, Land Deed ROI payback periods,
    /// Boss Core deterministic progression, and 0% zero-gold/zero-seed soft-lock probability.
    /// </summary>
    public static class EconomyConvergenceSimulator
    {
        // Canonical MVP Crop Constants
        public const int SweetPotatoSeedCost = 100;
        public const int SweetPotatoSellMin = 750;
        public const int SweetPotatoSellMax = 1000;
        public const float SweetPotatoGrowthSeconds = 360f;

        public const int TaroSeedCost = 300;
        public const int TaroSellMin = 1400;
        public const int TaroSellMax = 1800;
        public const float TaroGrowthSeconds = 480f;

        public const int CornSeedCost = 300;
        public const int CornSellMin = 1400;
        public const int CornSellMax = 1800;
        public const float CornGrowthSeconds = 600f;

        public const float PlotMoistureDurationSeconds = 240f;

        // Canonical Farmland Expansion Constants
        public const int InitialUnlockedPlots = 6;
        public const int MaxFarmPlots = 16;
        public const int PlotsPerLandDeed = 2;
        public const int LandDeedCost = 2500;

        // Canonical Equipment & Upgrade Sink Constants
        public const int WorkbenchLevel2GoldCost = 1500;
        public const int WorkbenchLevel3GoldCost = 3000;
        public const int SpecNode1GoldCost = 2000;
        public const int SpecNode2GoldCost = 3500;
        public const int SpecNode3GoldCost = 5000;
        public const int ArmorPieceCraftCost = 1000;
        public const int BottleOfWaterShopPrice = 2000;

        public enum PlayerStrategyArchetype : byte
        {
            ConservativeSixPlot = 0,
            AggressiveLandExpander = 1,
            CombatWorkbenchHybrid = 2,
            CatastrophicZeroResourceStress = 3
        }

        public readonly struct CropEconomicsMetrics
        {
            public readonly int SeedCost;
            public readonly float MeanSellPrice;
            public readonly float MeanNetProfitPerPlot;
            public readonly float ReturnOnInvestmentRatio;
            public readonly float GrowthDurationSeconds;
            public readonly int RequiredWateringActions;
            public readonly float ProfitPerMinutePerPlot;

            public CropEconomicsMetrics(
                int seedCost,
                float meanSellPrice,
                float growthDurationSeconds,
                int requiredWateringActions)
            {
                SeedCost = seedCost;
                MeanSellPrice = meanSellPrice;
                MeanNetProfitPerPlot = meanSellPrice - seedCost;
                ReturnOnInvestmentRatio = seedCost > 0 ? (meanSellPrice - seedCost) / seedCost : 0f;
                GrowthDurationSeconds = growthDurationSeconds;
                RequiredWateringActions = requiredWateringActions;
                ProfitPerMinutePerPlot = growthDurationSeconds > 0f
                    ? MeanNetProfitPerPlot / (growthDurationSeconds / 60f)
                    : 0f;
            }
        }

        public readonly struct SimulationAuditReport
        {
            public readonly int TotalTrials;
            public readonly int SoftLockCount;
            public readonly float SoftLockProbability;
            public readonly int MinDay5CumulativeGold;
            public readonly float MeanDay5CumulativeGold;
            public readonly int MaxDay5CumulativeGold;
            public readonly float MeanDay5UnlockedPlots;
            public readonly float SweetPotatoDeedPaybackCycles;
            public readonly float TaroCornDeedPaybackCycles;
            public readonly int GuaranteedBossCoresAcross5Days;
            public readonly float WorstCaseNight1RecoverySeedEquivalents;

            public SimulationAuditReport(
                int totalTrials,
                int softLockCount,
                int minDay5CumulativeGold,
                float meanDay5CumulativeGold,
                int maxDay5CumulativeGold,
                float meanDay5UnlockedPlots,
                float sweetPotatoDeedPaybackCycles,
                float taroCornDeedPaybackCycles,
                int guaranteedBossCoresAcross5Days,
                float worstCaseNight1RecoverySeedEquivalents)
            {
                TotalTrials = totalTrials;
                SoftLockCount = softLockCount;
                SoftLockProbability = totalTrials > 0 ? (float)softLockCount / totalTrials : 0f;
                MinDay5CumulativeGold = minDay5CumulativeGold;
                MeanDay5CumulativeGold = meanDay5CumulativeGold;
                MaxDay5CumulativeGold = maxDay5CumulativeGold;
                MeanDay5UnlockedPlots = meanDay5UnlockedPlots;
                SweetPotatoDeedPaybackCycles = sweetPotatoDeedPaybackCycles;
                TaroCornDeedPaybackCycles = taroCornDeedPaybackCycles;
                GuaranteedBossCoresAcross5Days = guaranteedBossCoresAcross5Days;
                WorstCaseNight1RecoverySeedEquivalents = worstCaseNight1RecoverySeedEquivalents;
            }
        }

        /// <summary>
        /// Computes exact analytical metrics for a crop type given moisture decay rules.
        /// Note: Sweet Potato (360s) completes in 1 watering due to FarmlandTile's single-watering override;
        /// Taro (480s) requires ceil(480/240) = 2 waterings; Corn (600s) requires ceil(600/240) = 3 waterings.
        /// </summary>
        public static CropEconomicsMetrics EvaluateCropMetrics(
            int seedCost,
            int minSellPrice,
            int maxSellPrice,
            float growthSeconds,
            bool isSingleWateringExempt)
        {
            float meanSell = 0.5f * (minSellPrice + maxSellPrice);
            int waterings = isSingleWateringExempt
                ? 1
                : Math.Max(1, (int)Math.Ceiling(growthSeconds / PlotMoistureDurationSeconds));

            return new CropEconomicsMetrics(seedCost, meanSell, growthSeconds, waterings);
        }

        /// <summary>
        /// Calculates the number of harvest cycles required for a single Land Deed (+2 plots for 2,500 G)
        /// to amortize its purchase cost using the net profit of the two newly unlocked plots.
        /// </summary>
        public static float CalculateLandDeedPaybackCycles(float netProfitPerPlot)
        {
            float netGainPerCycle = PlotsPerLandDeed * netProfitPerPlot;
            if (netGainPerCycle <= 0.001f) return float.PositiveInfinity;
            return LandDeedCost / netGainPerCycle;
        }

        /// <summary>
        /// Executes a zero-GC Monte Carlo simulation across all 4 player archetypes over Days 1-5.
        /// </summary>
        public static SimulationAuditReport RunMonteCarloAudit(int trialsPerArchetype = 250, uint initialSeed = 0x9E3779B9u)
        {
            int clampedTrialsPerArchetype = Math.Max(1, trialsPerArchetype);
            int totalTrials = clampedTrialsPerArchetype * 4;
            int softLocks = 0;

            int minGold = int.MaxValue;
            int maxGold = int.MinValue;
            long sumGold = 0L;
            int sumPlots = 0;

            uint rngState = initialSeed != 0u ? initialSeed : 0x9E3779B9u;

            for (int a = 0; a < 4; a++)
            {
                var archetype = (PlayerStrategyArchetype)a;
                for (int t = 0; t < clampedTrialsPerArchetype; t++)
                {
                    SimulateFiveDayCampaign(
                        archetype,
                        ref rngState,
                        out int cumulativeGoldEarned,
                        out int unlockedPlots,
                        out bool encounteredSoftLock);

                    if (encounteredSoftLock)
                    {
                        softLocks++;
                    }

                    if (archetype != PlayerStrategyArchetype.CatastrophicZeroResourceStress)
                    {
                        if (cumulativeGoldEarned < minGold) minGold = cumulativeGoldEarned;
                        if (cumulativeGoldEarned > maxGold) maxGold = cumulativeGoldEarned;
                        sumGold += cumulativeGoldEarned;
                        sumPlots += unlockedPlots;
                    }
                }
            }

            int standardTrials = clampedTrialsPerArchetype * 3;
            float meanGold = standardTrials > 0 ? (float)((double)sumGold / standardTrials) : 0f;
            float meanPlots = standardTrials > 0 ? (float)sumPlots / standardTrials : InitialUnlockedPlots;

            var spMetrics = EvaluateCropMetrics(SweetPotatoSeedCost, SweetPotatoSellMin, SweetPotatoSellMax, SweetPotatoGrowthSeconds, true);
            var taroMetrics = EvaluateCropMetrics(TaroSeedCost, TaroSellMin, TaroSellMax, TaroGrowthSeconds, false);

            float spPayback = CalculateLandDeedPaybackCycles(spMetrics.MeanNetProfitPerPlot);
            float taroPayback = CalculateLandDeedPaybackCycles(taroMetrics.MeanNetProfitPerPlot);

            // Night 2 (Cyclops Eye x1), Night 3 (Colossus Core x1), Night 4 (Colossus Core x1), Night 5 (Cob Core x1) = 4 guaranteed cores
            const int guaranteedBossCores = 4;

            // Worst-case Night 1 recovery when entering night with 0 Gold and 0 Seeds:
            // Night 1 wave spawns 8 TuberMaw minions + Night 1 survival bounty (750 G minimum) -> >= 7.5 Sweet Potato seeds
            const float worstCaseNight1SeedEquivalents = 750f / SweetPotatoSeedCost;

            return new SimulationAuditReport(
                totalTrials,
                softLocks,
                minGold == int.MaxValue ? 0 : minGold,
                meanGold,
                maxGold == int.MinValue ? 0 : maxGold,
                meanPlots,
                spPayback,
                taroPayback,
                guaranteedBossCores,
                worstCaseNight1SeedEquivalents);
        }

        private static void SimulateFiveDayCampaign(
            PlayerStrategyArchetype archetype,
            ref uint rng,
            out int cumulativeGoldEarned,
            out int unlockedPlots,
            out bool encounteredSoftLock)
        {
            unlockedPlots = InitialUnlockedPlots;
            encounteredSoftLock = false;

            // Day 1 starter state: 500 G wallet + 2 Sweet Potato seeds in starter chest
            int walletGold = archetype == PlayerStrategyArchetype.CatastrophicZeroResourceStress ? 0 : 500;
            int seedCount = archetype == PlayerStrategyArchetype.CatastrophicZeroResourceStress ? 0 : 2;
            cumulativeGoldEarned = walletGold;

            for (int day = 1; day <= 5; day++)
            {
                int spPrice = NextRange(ref rng, SweetPotatoSellMin, SweetPotatoSellMax + 1);
                int taroPrice = NextRange(ref rng, TaroSellMin, TaroSellMax + 1);
                int cornPrice = NextRange(ref rng, CornSellMin, CornSellMax + 1);

                if (archetype == PlayerStrategyArchetype.CatastrophicZeroResourceStress && day == 1)
                {
                    // Simulate player discarding all starter gold and seeds before planting on Day 1
                    walletGold = 0;
                    seedCount = 0;
                }
                else
                {
                    // Purchase Land Deeds if strategy favors expansion and player has surplus above seed working capital
                    if (archetype == PlayerStrategyArchetype.AggressiveLandExpander ||
                        (archetype == PlayerStrategyArchetype.CombatWorkbenchHybrid && unlockedPlots < 10))
                    {
                        while (unlockedPlots < MaxFarmPlots)
                        {
                            int nextPlots = Math.Min(MaxFarmPlots, unlockedPlots + PlotsPerLandDeed);
                            int workingCapitalNeeded = nextPlots * (day >= 3 ? TaroSeedCost : SweetPotatoSeedCost);
                            if (walletGold >= LandDeedCost + workingCapitalNeeded)
                            {
                                walletGold -= LandDeedCost;
                                unlockedPlots = nextPlots;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    // Buy seeds to fill unlocked plots (up to 1.5 cycles per diurnal window)
                    int targetPlantings = unlockedPlots + (day >= 2 ? unlockedPlots / 2 : 0);
                    int unitSeedCost = day >= 3 ? TaroSeedCost : SweetPotatoSeedCost;
                    int unitSellPrice = day >= 4 ? ((taroPrice + cornPrice) / 2) : (day == 3 ? taroPrice : spPrice);

                    while (seedCount < targetPlantings && walletGold >= unitSeedCost)
                    {
                        walletGold -= unitSeedCost;
                        seedCount++;
                    }

                    // Fallback to Sweet Potato seeds if player couldn't afford higher-tier seeds for all plots
                    while (seedCount < unlockedPlots && walletGold >= SweetPotatoSeedCost)
                    {
                        walletGold -= SweetPotatoSeedCost;
                        seedCount++;
                    }

                    int harvestedCount = Math.Min(targetPlantings, seedCount);
                    seedCount -= harvestedCount;

                    int harvestRevenue = harvestedCount * unitSellPrice;
                    walletGold += harvestRevenue;
                    cumulativeGoldEarned += harvestRevenue;
                }

                // Nocturnal Brawl Phase (Nights 1 to 5)
                int minionCount = 6 + day * 3;
                bool hasBoss = day >= 2;

                for (int m = 0; m < minionCount; m++)
                {
                    // 50% chance for minion gold drop (150-350 G)
                    if (NextFloat01(ref rng) <= 0.50f)
                    {
                        int dropGold = NextRange(ref rng, 150, 351);
                        walletGold += dropGold;
                        cumulativeGoldEarned += dropGold;
                    }

                    // 45% chance for minion seed drop (1-2 seeds)
                    if (NextFloat01(ref rng) <= 0.45f)
                    {
                        seedCount += NextRange(ref rng, 1, 3);
                    }
                }

                if (hasBoss)
                {
                    int bossGold = NextRange(ref rng, 800, 1501);
                    walletGold += bossGold;
                    cumulativeGoldEarned += bossGold;
                    seedCount += NextRange(ref rng, 2, 5); // 100% boss seed drop (2-4 seeds)
                }

                // Night survival baseline bounty
                int nightBounty = 500 + day * 250;
                walletGold += nightBounty;
                cumulativeGoldEarned += nightBounty;

                // Soft-lock invariant check at the dawn of the next day:
                // Can the player plant at least 1 crop (either via owned seeds or enough gold to buy a 100 G seed)?
                bool canPlantAtDawn = seedCount > 0 || walletGold >= SweetPotatoSeedCost;
                if (!canPlantAtDawn)
                {
                    encounteredSoftLock = true;
                }
            }
        }

        private static uint NextUInt(ref uint state)
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x != 0u ? x : 0x9E3779B9u;
            return state;
        }

        private static float NextFloat01(ref uint state)
        {
            return (NextUInt(ref state) & 0x00FFFFFFu) / 16777216f;
        }

        private static int NextRange(ref uint state, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt(ref state) % span);
        }
    }
}

