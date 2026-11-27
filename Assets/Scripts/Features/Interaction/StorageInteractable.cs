using System.Collections.Generic;
using UnityEngine;

namespace FeaturesInteraction
{
    // Defines a single storage loot table entry (chest/wardrobe/cabinet).
    [System.Serializable]
    public struct LootDrop
    {
        public ItemData item;
        public int minAmount;
        public int maxAmount;
        [Range(0f, 100f)] public float dropChance;
    }

    // Generic storage: can be attached to Chest, Wardrobe, or Cabinet prefabs
    // without rewriting storage logic.
    [RequireComponent(typeof(InventoryComponent))]
    public class StorageInteractable : MonoBehaviour, IInteractable
    {
        public List<LootDrop> lootTable = new List<LootDrop>();
        public bool generateLootOnStart = false;

        private InventoryComponent inventory;

        private void Awake()
        {
            inventory = GetComponent<InventoryComponent>();

            if (generateLootOnStart)
                GenerateLoot();
        }

        private void GenerateLoot()
        {
            if (inventory == null)
                return;

            foreach (LootDrop drop in lootTable)
            {
                if (drop.item == null)
                    continue;

                // Roll a random number 0-100; add item if within drop chance.
                float roll = Random.Range(0f, 100f);
                if (roll > drop.dropChance)
                    continue;

                int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
                if (amount > 0)
                    inventory.AddItem(drop.item, amount);
            }
        }

public void Interact(GameObject interactor)
    {
        Debug.Log("Storage dibuka oleh " + interactor.name);

        // Open dual-panel UI (player + storage) without auto-transferring items.
        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.OpenStorageUI(GetComponent<InventoryComponent>(), "Storage");
    }
    }
}
