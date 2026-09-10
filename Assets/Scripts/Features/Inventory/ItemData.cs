using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public enum ItemType { Material, Consumable, Tool, Trophy }

    public enum FoodCategory { None, Vegetable, Fruit, Meat, Ingredient, Dish }

    public string itemName;
    public Sprite itemIcon;
    public int maxStack;
    public string description;
    public ItemType type;
}