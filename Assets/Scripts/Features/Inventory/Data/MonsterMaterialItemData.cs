using UnityEngine;

public enum MaterialTier
{
    Normal,
    Boss
}

public enum UpgradePath
{
    SpeedMobility,          // Speed / Mobility path (Tuber Maw - Mutated Root)
    PrecisionTracking,      // Precision / Tracking path (Cyclops Tuber Maw - Cyclops Eye)
    PowerDefense,           // Power / Defense / Knockback path (Taro Brute - Hardened Root)
    HeavyImpactArmor        // Heavy Impact / Armor path (Taro Colossus - Colossus Core)
}

[CreateAssetMenu(fileName = "NewMonsterMaterial", menuName = "Inventory/Monster Material Item Data")]
public class MonsterMaterialItemData : ItemData
{
    [Header("Drop Classification")]
    [Tooltip("Tingkatan material (Normal dari creep biasa, Boss dari monster elit/boss).")]
    public MaterialTier materialTier = MaterialTier.Normal;

    [Tooltip("Jalur pohon upgrade senjata yang dibuka oleh material ini.")]
    public UpgradePath upgradePath = UpgradePath.SpeedMobility;

    [Tooltip("Musuh asal yang menjatuhkan material ini.")]
    public string sourceEnemy;

    private void OnEnable()
    {
        type = ItemType.Material;
        category = ItemCategory.MonsterDrop;
    }
}
