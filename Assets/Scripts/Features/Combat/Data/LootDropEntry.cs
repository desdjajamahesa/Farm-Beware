using System;
using UnityEngine;

namespace FeaturesCombat.Data
{
    /// <summary>
    /// Configuration entry defining an individual item drop chance and quantity range.
    /// Strictly enforces the maxStack = 20 invariant.
    /// </summary>
    [Serializable]
    public struct LootDropEntry
    {
        [Tooltip("The item asset dropped upon defeat.")]
        public ItemData item;

        [Tooltip("Minimum quantity dropped per roll.")]
        [Range(1, 20)]
        public int minQuantity;

        [Tooltip("Maximum quantity dropped per roll.")]
        [Range(1, 20)]
        public int maxQuantity;

        [Tooltip("Drop probability between 0.0 (0%) and 1.0 (100%).")]
        [Range(0f, 1f)]
        public float dropChance;

        public LootDropEntry(ItemData item, int minQuantity, int maxQuantity, float dropChance)
        {
            this.item = item;
            this.minQuantity = Mathf.Clamp(minQuantity, 1, 20);
            this.maxQuantity = Mathf.Clamp(maxQuantity, minQuantity, 20);
            this.dropChance = Mathf.Clamp01(dropChance);
        }

        public int RollQuantity()
        {
            if (minQuantity <= 0) minQuantity = 1;
            if (maxQuantity < minQuantity) maxQuantity = minQuantity;
            int roll = UnityEngine.Random.Range(minQuantity, maxQuantity + 1);
            return Mathf.Clamp(roll, 1, 20);
        }
    }
}
