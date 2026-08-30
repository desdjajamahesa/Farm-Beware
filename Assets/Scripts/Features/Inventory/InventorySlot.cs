using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty
    {
        get { return item == null || quantity <= 0; }
    }
}