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
        public static void ProcessDeathDrops(EnemyBase enemy)
        {
            if (enemy == null) return;

            // 1. Drop Gold
            if (Random.value <= enemy.goldChance)
            {
                int goldAmount = Random.Range(enemy.minGold, enemy.maxGold + 1);
                var wallet = ServiceLocator.Resolve<IWalletService>();
                if (wallet != null)
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

            // 2. Drop Monster Materials
            Vector3 tossDir = CalculateTossDirection(enemy);

            if (enemy.EnemyData != null && enemy.EnemyData.lootDrops != null && enemy.EnemyData.lootDrops.Count > 0)
            {
                // Modular ScriptableObject loot table
                for (int i = 0; i < enemy.EnemyData.lootDrops.Count; i++)
                {
                    var entry = enemy.EnemyData.lootDrops[i];
                    if (entry.item != null && Random.value <= entry.dropChance)
                    {
                        int dropCount = Mathf.Clamp(entry.RollQuantity(), 1, 20); // Strict maxStack = 20 invariant
                        for (int d = 0; d < dropCount; d++)
                        {
                            float angle = Random.Range(-35f, 35f);
                            Vector3 spreadDir = Quaternion.Euler(0f, angle, 0f) * tossDir;
                            WorldItemPickup.Spawn(enemy.transform.position, entry.item, 1, spreadDir);
                        }
                    }
                }
            }
            else if (enemy.dropMaterial != null && Random.value <= enemy.dropChance)
            {
                // Fallback legacy drop path
                int dropCount = Mathf.Clamp(Random.Range(enemy.minDropCount, enemy.maxDropCount + 1), 1, 20);
                for (int i = 0; i < dropCount; i++)
                {
                    float angle = Random.Range(-35f, 35f);
                    Vector3 spreadDir = Quaternion.Euler(0f, angle, 0f) * tossDir;
                    WorldItemPickup.Spawn(enemy.transform.position, enemy.dropMaterial, 1, spreadDir);
                }
            }

            // 3. Drop Seeds (MVP Guideline V0.1 Gap 1)
            ProcessSeedDrops(enemy, tossDir);
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
