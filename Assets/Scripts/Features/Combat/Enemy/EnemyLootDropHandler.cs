using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Handler statis modular untuk memproses drop ekonomi & item saat musuh tereliminasi (Task 4.1 Refactor).
    /// Mengurangi beban tanggung jawab God Class dari EnemyBase tanpa merusak kompatibilitas data.
    /// </summary>
    public static class EnemyLootDropHandler
    {
        /// <summary>
        /// Determines mutually exclusive material vs gold drop outcome for non-boss enemies.
        /// True = drop Monster Material; False = drop Gold.
        /// </summary>
        public static bool RollMaterialInsteadOfGold(float materialChance, float randomRoll)
        {
            return randomRoll < Mathf.Clamp01(materialChance);
        }

        private static bool IsSeedDrop(ItemData item)
        {
            if (item == null) return false;
            if (item is SeedItemData || item.category == ItemCategory.Seed) return true;
            return !string.IsNullOrEmpty(item.itemId) && item.itemId.StartsWith("seed_", System.StringComparison.OrdinalIgnoreCase);
        }

        public static void ProcessDeathDrops(EnemyBase enemy)
        {
            if (enemy == null) return;

            bool isOverdrive = NightBrawlManager.Instance != null && NightBrawlManager.Instance.IsOverdriveNightActive;
            Vector3 tossDir = CalculateTossDirection(enemy);

            if (enemy.isBoss)
            {
                if (Random.value <= enemy.goldChance)
                {
                    AwardCombatGold(enemy, enemy.minGold, enemy.maxGold, isOverdrive);
                }

                if (enemy.EnemyData != null && enemy.EnemyData.lootDrops != null && enemy.EnemyData.lootDrops.Count > 0)
                {
                    for (int i = 0; i < enemy.EnemyData.lootDrops.Count; i++)
                    {
                        var entry = enemy.EnemyData.lootDrops[i];
                        if (entry.item == null || IsSeedDrop(entry.item)) continue;

                        float effectiveChance = Core.PureLogic.NightOverdriveEvaluator.CalculateOverdriveDropChance(entry.dropChance, isOverdrive);
                        if (Random.value <= effectiveChance)
                        {
                            SpawnSpreadDrops(enemy.transform.position, entry.item, Mathf.Clamp(entry.RollQuantity(), 1, 20), tossDir);
                        }
                    }
                }
                else if (enemy.dropMaterial != null && Random.value <= Core.PureLogic.NightOverdriveEvaluator.CalculateOverdriveDropChance(enemy.dropChance, isOverdrive))
                {
                    int dropCount = Mathf.Clamp(Random.Range(enemy.minDropCount, enemy.maxDropCount + 1), 1, 20);
                    SpawnSpreadDrops(enemy.transform.position, enemy.dropMaterial, dropCount, tossDir);
                }
            }
            else
            {
                // Regular enemies: mutually exclusive 50% Monster Material OR 50% Gold (prevents BUG 021 double-drop & BUG 022 zero-drop)
                ItemData matItem = null;
                int matMin = enemy.minDropCount > 0 ? enemy.minDropCount : 1;
                int matMax = enemy.maxDropCount >= matMin ? enemy.maxDropCount : 3;
                float baseMatChance = enemy.dropChance > 0f ? enemy.dropChance : 0.5f;

                if (enemy.EnemyData != null && enemy.EnemyData.lootDrops != null)
                {
                    for (int i = 0; i < enemy.EnemyData.lootDrops.Count; i++)
                    {
                        var entry = enemy.EnemyData.lootDrops[i];
                        if (entry.item != null && !IsSeedDrop(entry.item))
                        {
                            matItem = entry.item;
                            matMin = entry.minQuantity > 0 ? entry.minQuantity : 1;
                            matMax = entry.maxQuantity >= matMin ? entry.maxQuantity : matMin;
                            baseMatChance = entry.dropChance > 0f ? entry.dropChance : 0.5f;
                            break;
                        }
                    }
                }

                if (matItem == null)
                {
                    matItem = enemy.dropMaterial ?? ResolveFallbackMaterial(enemy.enemyType);
                }

                float effectiveMatChance = Core.PureLogic.NightOverdriveEvaluator.CalculateOverdriveDropChance(baseMatChance, isOverdrive);
                if (matItem != null && RollMaterialInsteadOfGold(effectiveMatChance, Random.value))
                {
                    int dropCount = Mathf.Clamp(Random.Range(matMin, matMax + 1), 1, 20);
                    SpawnSpreadDrops(enemy.transform.position, matItem, dropCount, tossDir);
                }
                else
                {
                    int minG = enemy.minGold > 0 ? enemy.minGold : 200;
                    int maxG = enemy.maxGold >= minG ? enemy.maxGold : 500;
                    AwardCombatGold(enemy, minG, maxG, isOverdrive);
                }
            }

            // Drop Seeds independently according to MVP specification (ProcessSeedDrops handles seed quantities cleanly)
            ProcessSeedDrops(enemy, tossDir);
        }

        private static ItemData ResolveFallbackMaterial(EnemyType type)
        {
            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db == null) return null;

            return type switch
            {
                EnemyType.TuberMaw => db.GetItem("mat_mutated_root"),
                EnemyType.TaroBrute => db.GetItem("mat_hardened_root"),
                EnemyType.CornMusketeer => db.GetItem("mat_kernel_shrapnel"),
                EnemyType.CyclopsTuberMaw => db.GetItem("mat_cyclops_eye"),
                EnemyType.TaroColossus => db.GetItem("mat_colossus_core"),
                EnemyType.TheRanger => db.GetItem("mat_cob_core"),
                _ => null
            };
        }

        private static void AwardCombatGold(EnemyBase enemy, int minGold, int maxGold, bool isOverdrive)
        {
            int baseGold = Random.Range(minGold, maxGold + 1);
            int goldAmount = Core.PureLogic.NightOverdriveEvaluator.CalculateOverdriveGold(baseGold, isOverdrive);
            var wallet = ServiceLocator.Resolve<IWalletService>();
            if (wallet != null && goldAmount > 0)
            {
                wallet.AddGold(goldAmount);
                ServiceLocator.Resolve<IDailyEconomyService>()?.RecordCombatGold(goldAmount);

                if (FloatingCombatTextManager.Instance != null)
                {
                    FloatingCombatTextManager.Instance.SpawnText(
                        enemy.transform.position + Vector3.up * 1.5f,
                        $"+{goldAmount} Gold",
                        new Color(1f, 0.85f, 0.2f));
                }
            }
        }

        private static void SpawnSpreadDrops(Vector3 origin, ItemData item, int count, Vector3 tossDir)
        {
            for (int d = 0; d < count; d++)
            {
                float angle = Random.Range(-35f, 35f);
                Vector3 spreadDir = Quaternion.Euler(0f, angle, 0f) * tossDir;
                WorldItemPickup.Spawn(origin, item, 1, spreadDir);
            }
        }

        private static void ProcessSeedDrops(EnemyBase enemy, Vector3 tossDir)
        {
            string seedId = null;
            int seedCount = 0;

            switch (enemy.enemyType)
            {
                // Sweet Potato Line (Guideline: Tuber Maw drops 1-4 SP seeds: 60/20/10/10%)
                case EnemyType.TuberMaw:
                {
                    seedId = "seed_sweet_potato";
                    float r = Random.value;
                    if (r < 0.60f) seedCount = 1;
                    else if (r < 0.80f) seedCount = 2;
                    else if (r < 0.90f) seedCount = 3;
                    else seedCount = 4;
                    break;
                }
                case EnemyType.CyclopsTuberMaw:
                {
                    seedId = "seed_sweet_potato";
                    seedCount = Random.Range(2, 6); // 2-5 boss drop
                    break;
                }

                // Taro Line (Guideline: Taro Brute drops 1-3 Taro seeds: 70/20/10%)
                case EnemyType.TaroBrute:
                {
                    seedId = "seed_taro";
                    float r = Random.value;
                    if (r < 0.70f) seedCount = 1;
                    else if (r < 0.90f) seedCount = 2;
                    else seedCount = 3;
                    break;
                }
                case EnemyType.TaroColossus:
                {
                    seedId = "seed_taro";
                    seedCount = Random.Range(2, 6); // 2-5 boss drop
                    break;
                }

                // Corn Line (Guideline: Corn Musketeer drops 1-3 Corn seeds: 70/20/10%)
                case EnemyType.CornMusketeer:
                {
                    seedId = "seed_corn";
                    float r = Random.value;
                    if (r < 0.70f) seedCount = 1;
                    else if (r < 0.90f) seedCount = 2;
                    else seedCount = 3;
                    break;
                }
                case EnemyType.TheRanger:
                {
                    seedId = "seed_corn";
                    seedCount = Random.Range(2, 6); // 2-5 boss drop
                    break;
                }
            }

            if (!string.IsNullOrEmpty(seedId) && seedCount > 0)
            {
                var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
                var seedItem = db != null ? db.GetItem(seedId) : null;
                if (seedItem != null)
                {
                    for (int s = 0; s < seedCount; s++)
                    {
                        float angle = Random.Range(-35f, 35f);
                        Vector3 spreadDir = Quaternion.Euler(0f, angle, 0f) * tossDir;
                        WorldItemPickup.Spawn(enemy.transform.position, seedItem, 1, spreadDir);
                    }
                }
            }
        }

        private static Vector3 CalculateTossDirection(EnemyBase enemy)
        {
            Transform playerTarget = enemy.PlayerTarget;
            Vector3 tossDir;
            if (playerTarget != null)
            {
                tossDir = (enemy.transform.position - playerTarget.position);
            }
            else
            {
                tossDir = -enemy.transform.forward;
            }
            tossDir.y = 0f;
            if (tossDir.sqrMagnitude < 0.001f) tossDir = -enemy.transform.forward;
            return tossDir.normalized;
        }
    }
}
