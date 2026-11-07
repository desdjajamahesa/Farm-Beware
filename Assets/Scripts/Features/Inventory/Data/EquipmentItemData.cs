using UnityEngine;
using FarmBeware.Core.Runtime;

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Inventory/Equipment Item Data")]
public class EquipmentItemData : ItemData
{
    [Header("Equipment Slot")]
    public EquipmentSlotType slotType = EquipmentSlotType.Chest;

    [Header("Set Identity")]
    public string setName = "Sweet Potato"; // e.g. "Sweet Potato"

    [Header("Equipment Bonus Stats")]
    public int bonusHealth = 0;
    public int bonusDefense = 0;
    public float bonusAttackSpeed = 0f;
    public float bonusMoveSpeed = 0f;
    public float bonusCriticalChance = 0f;

    void OnEnable()
    {
        type = ItemType.Tool;
        maxStack = 1;
    }
}

