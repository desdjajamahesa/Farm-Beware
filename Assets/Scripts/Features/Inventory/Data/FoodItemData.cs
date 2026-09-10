using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodItem", menuName = "Inventory/Food Item Data")]
public class FoodItemData : ItemData
{
    public int healAmount;
    public FoodCategory foodCategory;

    [Header("Dirty/Clean System")]
    [Tooltip("True = item ini kotor, bisa dicuci di Wastafel.")]
    public bool isDirty;

    [Tooltip("Item hasil cucian. Isi hanya pada item kotor.")]
    public ItemData cleanVariant;

    void OnEnable()
    {
        type = ItemType.Consumable;
    }
}
