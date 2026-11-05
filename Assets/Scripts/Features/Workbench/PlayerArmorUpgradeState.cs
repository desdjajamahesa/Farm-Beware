using System;
using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesWorkbench.Core;

namespace FeaturesWorkbench
{
    /// <summary>
    /// Thin MonoBehaviour Adapter managing player armor progression (Lv. 1 to 10)
    /// and tracking equipped gear pieces for set bonus calculations.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerArmorUpgradeState : MonoBehaviour
    {
        public static PlayerArmorUpgradeState Instance { get; private set; }

        public event Action OnArmorUpgraded;
        public event Action<EquipmentSlotType, ItemData> OnEquipmentChanged;

        [Header("Armor Level")]
        [Range(1, 10)] public int armorLevel = 1;

        [Header("Equipped Items By Slot")]
        [SerializeField] private List<EquippedSlotEntry> equippedEntries = new List<EquippedSlotEntry>();

        [Serializable]
        public struct EquippedSlotEntry
        {
            public EquipmentSlotType slotType;
            public ItemData item;

            public EquippedSlotEntry(EquipmentSlotType slot, ItemData itemData)
            {
                slotType = slot;
                item = itemData;
            }
        }

        private readonly Dictionary<EquipmentSlotType, ItemData> _equippedMap = new Dictionary<EquipmentSlotType, ItemData>();

        public float DamageMitigationRatio => EquipmentUpgradeEvaluator.GetArmorMitigationRatio(armorLevel);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            RebuildEquippedMap();
        }

        private void RebuildEquippedMap()
        {
            _equippedMap.Clear();
            foreach (var entry in equippedEntries)
            {
                if (entry.item != null)
                {
                    _equippedMap[entry.slotType] = entry.item;
                }
            }
        }

        private void SyncEntriesList()
        {
            equippedEntries.Clear();
            foreach (var kvp in _equippedMap)
            {
                if (kvp.Value != null)
                {
                    equippedEntries.Add(new EquippedSlotEntry(kvp.Key, kvp.Value));
                }
            }
        }

        public ItemData GetEquippedItem(EquipmentSlotType slot)
        {
            _equippedMap.TryGetValue(slot, out var item);
            return item;
        }

        public bool EquipItem(EquipmentSlotType slot, ItemData item, out ItemData previousItem)
        {
            _equippedMap.TryGetValue(slot, out previousItem);
            _equippedMap[slot] = item;
            SyncEntriesList();

            OnEquipmentChanged?.Invoke(slot, item);
            return true;
        }

        public bool UnequipItem(EquipmentSlotType slot, out ItemData unequippedItem)
        {
            if (_equippedMap.TryGetValue(slot, out unequippedItem))
            {
                _equippedMap.Remove(slot);
                SyncEntriesList();
                OnEquipmentChanged?.Invoke(slot, null);
                return true;
            }

            unequippedItem = null;
            return false;
        }

        public int GetNextUpgradeCost()
        {
            return EquipmentUpgradeEvaluator.GetArmorUpgradeCost(armorLevel);
        }

        public bool UpgradeArmor(IWalletService wallet = null)
        {
            if (armorLevel >= EquipmentUpgradeEvaluator.MaxArmorLevel) return false;

            int cost = GetNextUpgradeCost();
            if (cost <= 0) return false;

            if (wallet == null)
            {
                wallet = ServiceLocator.Resolve<IWalletService>();
            }

            if (wallet != null)
            {
                if (!wallet.CanAfford(cost)) return false;
                if (!wallet.SpendGold(cost)) return false;
            }

            armorLevel++;
            OnArmorUpgraded?.Invoke();
            return true;
        }

        public SetBonusState GetCurrentSetBonus(string targetSetName = "Sweet Potato")
        {
            int matchCount = 0;
            foreach (var kvp in _equippedMap)
            {
                if (kvp.Value is EquipmentItemData eq && string.Equals(eq.setName, targetSetName, StringComparison.OrdinalIgnoreCase))
                {
                    matchCount++;
                }
            }

            return EquipmentUpgradeEvaluator.EvaluateSweetPotatoSetBonus(matchCount);
        }
    }
}
