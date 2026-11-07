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

        /// <summary>
        /// Checks whether the player owns or has equipped at least one armor piece.
        /// Strict equipment ownership gating: plating cannot be upgraded without owning armor.
        /// </summary>
        public bool HasAnyArmorPiece(InventoryComponent inv = null)
        {
            // Check equipped armor slots (excluding weapon)
            foreach (var kvp in _equippedMap)
            {
                if (kvp.Key != EquipmentSlotType.MainWeapon && kvp.Value != null)
                {
                    return true;
                }
            }

            if (inv == null)
            {
                inv = GetComponent<InventoryComponent>();
            }
            if (inv == null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null)
                {
                    inv = player.GetComponent<InventoryComponent>();
                }
            }

            if (inv != null && inv.slots != null)
            {
                for (int i = 0; i < inv.slots.Count; i++)
                {
                    var slot = inv.slots[i];
                    if (slot != null && !slot.IsEmpty && slot.item != null)
                    {
                        if (slot.item is EquipmentItemData ||
                            string.Equals(slot.item.itemId, "item_burrow_hood", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(slot.item.itemId, "item_swift_root_armor", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(slot.item.itemId, "item_fang_grips", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(slot.item.itemId, "item_underground_striders", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(slot.item.itemId, "item_sweet_potato_heart", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool UpgradeArmor(IWalletService wallet = null, InventoryComponent inv = null)
        {
            if (armorLevel >= EquipmentUpgradeEvaluator.MaxArmorLevel) return false;

            if (!HasAnyArmorPiece(inv))
            {
                Debug.LogWarning("[PlayerArmorUpgradeState] Cannot upgrade armor plating: Player does not possess any armor pieces!");
                return false;
            }

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
                else if (string.Equals(targetSetName, "Sweet Potato", StringComparison.OrdinalIgnoreCase) &&
                         kvp.Key == EquipmentSlotType.MainWeapon && kvp.Value != null)
                {
                    // Root Cleaver counts toward the Sweet Potato combat set
                    if (string.Equals(kvp.Value.itemId, "tool_weapon_root_cleaver", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(kvp.Value.itemId, "dummysword", StringComparison.OrdinalIgnoreCase))
                    {
                        matchCount++;
                    }
                }
            }

            return EquipmentUpgradeEvaluator.EvaluateSweetPotatoSetBonus(matchCount);
        }
    }
}
