using System.Collections.Generic;
using UnityEngine;

namespace FeaturesInteraction
{
    // Definisi satu entri loot table storage (peti/lemari/cabinet).
    [System.Serializable]
    public struct LootDrop
    {
        public ItemData item;
        public int minAmount;
        public int maxAmount;
        [Range(0f, 100f)] public float dropChance;
    }

    // Storage generik: bisa dipasang pada prefab Peti, Lemari, atau Kabinet
    // tanpa menulis ulang logika penyimpanan.
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

                // Bangkitkan angka acak 0-100; masukkan item bila lolos peluang.
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

            // Buka UI dual-panel (player + storage) tanpa auto-transfer item.
            if (InventoryManagerUI.Instance != null)
                InventoryManagerUI.Instance.OpenStorageUI(GetComponent<InventoryComponent>());
        }
    }
}
