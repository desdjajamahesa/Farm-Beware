using System.Collections.Generic;
using UnityEngine;

public enum FoodTier
{
    Small,
    Medium,
    Large
}

[CreateAssetMenu(fileName = "NewFoodItem", menuName = "Inventory/Food Item Data")]
public class FoodItemData : ItemData
{
    [Header("Food Classification")]
    public FoodTier foodTier = FoodTier.Small;
    public FoodCategory foodCategory;

    [Header("Nutritional Value")]
    [Tooltip("Jumlah pemulihan HP langsung saat dikonsumsi.")]
    public int healAmount = 0;

    [Tooltip("Jumlah pemulihan rasa lapar (Hunger).")]
    public float hungerRestore = 0f;

    [Tooltip("Jumlah pemulihan rasa haus / hidrasi (Thirst/Hydration). Bisa bernilai negatif jika makanan mengeringkan.")]
    public float hydrationRestore = 0f;

    [Header("Buff Effects")]
    [Tooltip("Daftar efek buff status yang diberikan saat memakan item ini.")]
    public List<BuffEffectData> buffEffects = new List<BuffEffectData>();

    private void OnEnable()
    {
        type = ItemType.Consumable;
        category = ItemCategory.Food;
    }
}
