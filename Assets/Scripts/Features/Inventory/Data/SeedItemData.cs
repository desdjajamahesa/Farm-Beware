using UnityEngine;

[CreateAssetMenu(fileName = "NewSeedItem", menuName = "Inventory/Seed Item Data")]
public class SeedItemData : ItemData
{
    [Header("Crop Yield & Growth")]
    [Tooltip("Harvested crop item yielded when this crop reaches maturity.")]
    public ItemData cropYield;

    [Tooltip("Growth duration from planted seed to mature crop (in seconds).")]
    public float growthDuration = 30f;

    [Tooltip("Minimum crop yield quantity upon harvest.")]
    public int minYield = 1;

    [Tooltip("Maximum crop yield quantity upon harvest.")]
    public int maxYield = 3;

    [Header("Ecology & Brawl")]
    [Tooltip("Mutant monster species associated with this crop at night (e.g. Tuber Maw, Taro Brute, Corn Musketeer).")]
    public string associatedMonster;

    private void OnEnable()
    {
        type = ItemType.Material;
        category = ItemCategory.Seed;
    }
}
