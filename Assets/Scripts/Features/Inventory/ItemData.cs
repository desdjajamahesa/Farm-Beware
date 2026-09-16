using UnityEngine;

public enum ItemCategory
{
    Seed,
    Crop,
    Food,
    Material,
    MonsterDrop,
    Weapon,
    Tool,
    Trophy
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public enum ItemType { Material, Consumable, Tool, Trophy }

    public enum FoodCategory { None, Vegetable, Fruit, Meat, Ingredient, Dish }

    [Header("Identity")]
    public string itemId;
    public string itemName;
    public ItemCategory category = ItemCategory.Material;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Visual & Description")]
    public Sprite itemIcon;
    [TextArea(2, 4)]
    public string description;
    public int maxStack = 99;
    public ItemType type;

    [Header("Economy")]
    public int buyPrice = 0;
    public int sellPrice = 0;

    [Header("Dirty / Clean System")]
    [Tooltip("True jika item ini dalam keadaan kotor dan bisa dicuci di wastafel.")]
    public bool isDirty = false;
    [Tooltip("Item bersih hasil cucian (diisi jika item ini kotor).")]
    public ItemData cleanVariant;
}