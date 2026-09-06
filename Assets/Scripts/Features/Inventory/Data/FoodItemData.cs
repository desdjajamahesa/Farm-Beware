using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodItem", menuName = "Inventory/Food Item Data")]
public class FoodItemData : ItemData
{
    public int healAmount;
    public FoodCategory foodCategory;

    void OnEnable()
    {
        type = ItemType.Consumable;
    }
}
