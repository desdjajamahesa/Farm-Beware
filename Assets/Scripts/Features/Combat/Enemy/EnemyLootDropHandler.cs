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

            // 2. Drop Monster Material
            if (enemy.dropMaterial != null && Random.value <= enemy.dropChance)
            {
                int dropCount = Random.Range(enemy.minDropCount, enemy.maxDropCount + 1);
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
                tossDir.Normalize();

                for (int i = 0; i < dropCount; i++)
                {
                    float angle = Random.Range(-35f, 35f);
                    Vector3 spreadDir = Quaternion.Euler(0f, angle, 0f) * tossDir;
                    WorldItemPickup.Spawn(enemy.transform.position, enemy.dropMaterial, 1, spreadDir);
                }
            }
        }
    }
}
