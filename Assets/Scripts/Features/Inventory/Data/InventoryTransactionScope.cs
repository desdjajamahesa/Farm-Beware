using System;
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesInventory
{
    /// <summary>
    /// Implements Two-Phase Escrow Transaction Scope for safe inventory operations.
    /// Provides CaptureSnapshot, EscrowItems, Rollback, and Commit primitives.
    /// Implements IDisposable to guarantee safe rollback if aborted prematurely.
    /// </summary>
    public class InventoryTransactionScope : IDisposable
    {
        private readonly InventoryComponent _inventory;
        private readonly InventorySnapshot _initialSnapshot;
        private readonly List<InventorySlotSnapshot> _escrowItems = new List<InventorySlotSnapshot>();

        public bool IsCommitted => _isCommitted;
        public bool IsRolledBack => _isRolledBack;
        public IReadOnlyList<InventorySlotSnapshot> EscrowItems => _escrowItems;

        private bool _isCommitted;
        private bool _isRolledBack;

        public InventoryTransactionScope(InventoryComponent inventory)
        {
            _inventory = inventory;
            if (_inventory != null)
            {
                _initialSnapshot = InventorySnapshot.Capture(_inventory);
            }
        }

        public static InventoryTransactionScope Begin(InventoryComponent inventory)
        {
            return new InventoryTransactionScope(inventory);
        }

        public void EscrowItem(ItemData item, int quantity)
        {
            if (_isCommitted || _isRolledBack || item == null || quantity <= 0) return;
            _escrowItems.Add(new InventorySlotSnapshot(item, quantity));
        }

        public void Commit()
        {
            if (_isCommitted || _isRolledBack) return;
            _isCommitted = true;
            _escrowItems.Clear();
        }

        public void Rollback()
        {
            if (_isCommitted || _isRolledBack) return;
            _isRolledBack = true;

            if (_inventory != null)
            {
                foreach (var escrow in _escrowItems)
                {
                    if (escrow.item != null && escrow.quantity > 0)
                    {
                        int added = _inventory.AddItemAmount(escrow.item, escrow.quantity);
                        int leftover = escrow.quantity - added;
                        if (leftover > 0)
                        {
                            Debug.LogWarning($"[InventoryTransactionScope] Leftover item during rollback: {leftover}x {escrow.item.itemName}");
                        }
                    }
                }
            }
            _escrowItems.Clear();
        }

        public void Dispose()
        {
            if (!_isCommitted && !_isRolledBack)
            {
                Rollback();
            }
        }
    }
}
