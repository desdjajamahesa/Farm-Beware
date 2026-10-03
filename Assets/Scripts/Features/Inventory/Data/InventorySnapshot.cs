using System;
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesInventory
{
    /// <summary>
    /// Lightweight immutable snapshot of an inventory slot at a specific moment in time.
    /// </summary>
    [Serializable]
    public struct InventorySlotSnapshot
    {
        public ItemData item;
        public int quantity;

        public InventorySlotSnapshot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public bool IsEmpty => item == null || quantity <= 0;
    }

    /// <summary>
    /// Capture and restore mechanism for InventoryComponent slots supporting atomic transaction scopes.
    /// </summary>
    [Serializable]
    public class InventorySnapshot
    {
        public List<InventorySlotSnapshot> slots = new List<InventorySlotSnapshot>();

        public static InventorySnapshot Capture(InventoryComponent inventory)
        {
            var snapshot = new InventorySnapshot();
            if (inventory != null && inventory.slots != null)
            {
                for (int i = 0; i < inventory.slots.Count; i++)
                {
                    var s = inventory.slots[i];
                    if (s != null && !s.IsEmpty)
                    {
                        snapshot.slots.Add(new InventorySlotSnapshot(s.item, s.quantity));
                    }
                    else
                    {
                        snapshot.slots.Add(new InventorySlotSnapshot(null, 0));
                    }
                }
            }
            return snapshot;
        }

        public void Restore(InventoryComponent inventory)
        {
            if (inventory == null || inventory.slots == null) return;
            inventory.slots.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                inventory.slots.Add(new InventorySlot(slots[i].item, slots[i].quantity));
            }
            inventory.OnInventoryChanged?.Invoke();
        }
    }
}
