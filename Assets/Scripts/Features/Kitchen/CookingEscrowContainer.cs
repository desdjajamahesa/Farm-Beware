using System;
using System.Collections.Generic;
using UnityEngine;
using FeaturesInventory;

namespace FeaturesKitchen
{
    /// <summary>
    /// Isolated escrow container holding ingredients and fluids locked during active cooking.
    /// Manages two-phase commit & abort rollback lifecycle for KitchenStove.
    /// </summary>
    [Serializable]
    public class CookingEscrowContainer
    {
        [SerializeField] private List<InventorySlotSnapshot> _heldIngredients = new List<InventorySlotSnapshot>();
        [SerializeField] private float _heldWater = 0f;

        public IReadOnlyList<InventorySlotSnapshot> HeldIngredients => _heldIngredients;
        public float HeldWater => _heldWater;
        public bool IsEmpty => _heldIngredients.Count == 0 && _heldWater <= 0f;

        public void HoldIngredient(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            _heldIngredients.Add(new InventorySlotSnapshot(item, quantity));
        }

        public void HoldWater(float amount)
        {
            if (amount > 0f) _heldWater += amount;
        }

        public void ReleaseHold(InventoryComponent targetInventory, Action<ItemData, int> onOverflow = null)
        {
            // Refund water to player water bottle
            if (_heldWater > 0f && PlayerWaterBottle.Instance != null)
            {
                PlayerWaterBottle.Instance.RefillWater(_heldWater);
            }

            // Refund ingredients to player inventory
            if (targetInventory != null)
            {
                foreach (var snap in _heldIngredients)
                {
                    if (snap.item != null && snap.quantity > 0)
                    {
                        int added = targetInventory.AddItemAmount(snap.item, snap.quantity);
                        int leftover = snap.quantity - added;
                        if (leftover > 0)
                        {
                            onOverflow?.Invoke(snap.item, leftover);
                        }
                    }
                }
            }

            Clear();
        }

        public void Clear()
        {
            _heldIngredients.Clear();
            _heldWater = 0f;
        }
    }
}
