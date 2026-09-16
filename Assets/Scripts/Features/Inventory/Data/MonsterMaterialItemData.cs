using UnityEngine;

public enum MaterialTier
{
    Normal,
    Boss
}

public enum UpgradePath
{
    SpeedMobility,          // Jalur Speed / Mobility (Tuber Maw - Mutated Root)
    PrecisionTracking,      // Jalur Precision / Tracking (Cyclops Tuber Maw - Cyclops Eye)
    PowerDefense,           // Jalur Power / Defense / Knockback (Taro Brute - Hardened Root)
    HeavyImpactArmor        // Jalur Heavy Impact / Armor (Taro Colossus - Colossus Core)
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
