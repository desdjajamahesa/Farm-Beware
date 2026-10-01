using UnityEngine;
using PlayerUI;
using FeaturesEconomy;

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
                if (PlayerWallet.Instance != null)
                {
                    PlayerWallet.Instance.AddGold(goldAmount);
                    if (DailyEconomyManager.Instance != null)
                    {
                        DailyEconomyManager.Instance.RecordCombatGold(goldAmount);
                    }

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
