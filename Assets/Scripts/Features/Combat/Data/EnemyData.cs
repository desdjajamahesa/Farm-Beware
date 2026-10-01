using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCombat.Data
{
    /// <summary>
    /// ScriptableObject defining combat attributes, boss evolution hierarchy, and loot drop tables for an enemy variant.
    /// Supports modular balancing and Zero-GC data injection into EnemyBase runtime instances.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_New", menuName = "Farm-Beware/Combat/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity & Evolution")]
        [Tooltip("Unique enemy variant identifier matching the evolution system.")]
        public EnemyType enemyType = EnemyType.TuberMaw;

        [Tooltip("User-facing display name of the enemy.")]
        public string displayName = "Tuber Maw";

        [Tooltip("Flag indicating if this enemy is a major boss.")]
        public bool isBoss = false;

        [Tooltip("Evolved boss form of this monster (e.g. Tuber Maw -> Cyclops Tuber Maw).")]
        public EnemyData evolvedBossVariant;

        [Header("Combat Attributes")]
        [Min(1)] public int maxHealth = 100;
        [Min(0)] public int armor = 20;
        [Min(0.1f)] public float moveSpeed = 3.0f;
        [Min(1)] public int contactDamage = 15;
        [Min(0.1f)] public float attackRate = 0.4f;
        [Min(0.5f)] public float attackRange = 1.5f;
        [Min(1.0f)] public float aggroRange = 8.0f;
        [Range(0f, 1f)] public float knockbackResistance = 0.2f;

        [Header("Loot Drop Configuration")]
        [Tooltip("Modular loot table defining potential material and trophy drops.")]
        public List<LootDropEntry> lootDrops = new List<LootDropEntry>();

        [Header("Gold Economy")]
        [Min(0)] public int minGold = 100;
        [Min(0)] public int maxGold = 300;
        [Range(0f, 1f)] public float goldChance = 0.5f;

        /// <summary>
        /// Evaluates all configured loot drop entries into the provided output list.
        /// Strictly respects maxStack = 20.
        /// </summary>
        public void EvaluateLootDrops(List<(ItemData item, int quantity)> outDrops)
        {
            if (outDrops == null) return;
            outDrops.Clear();

            if (lootDrops != null)
            {
                for (int i = 0; i < lootDrops.Count; i++)
                {
                    var entry = lootDrops[i];
                    if (entry.item != null && UnityEngine.Random.value <= entry.dropChance)
                    {
                        int qty = entry.RollQuantity();
                        if (qty > 0)
                        {
                            outDrops.Add((entry.item, qty));
                        }
                    }
                }
            }
        }

        public int RollGold()
        {
            if (UnityEngine.Random.value <= goldChance)
            {
                return UnityEngine.Random.Range(minGold, maxGold + 1);
            }
            return 0;
        }
    }
}
