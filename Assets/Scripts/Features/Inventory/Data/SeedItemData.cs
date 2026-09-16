using UnityEngine;

[CreateAssetMenu(fileName = "NewSeedItem", menuName = "Inventory/Seed Item Data")]
public class SeedItemData : ItemData
{
    [Header("Crop Yield & Growth")]
    [Tooltip("Hasil panen tanaman saat tanaman ini matang.")]
    public ItemData cropYield;

    [Tooltip("Durasi waktu pertumbuhan tanaman dari bibit sampai siap panen (detik).")]
    public float growthDuration = 30f;

    [Tooltip("Jumlah hasil panen minimal.")]
    public int minYield = 1;

    [Tooltip("Jumlah hasil panen maksimal.")]
    public int maxYield = 3;

    [Header("Ecology & Brawl")]
    [Tooltip("Monster yang berasosiasi dengan tanaman ini di malam hari (e.g. Tuber Maw, Taro Brute).")]
    public string associatedMonster;

    private void OnEnable()
    {
        type = ItemType.Material;
        category = ItemCategory.Seed;
    }
}
