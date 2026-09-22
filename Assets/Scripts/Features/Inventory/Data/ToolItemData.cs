using UnityEngine;

[CreateAssetMenu(fileName = "NewToolItem", menuName = "Inventory/Tool Item Data")]
public class ToolItemData : ItemData
{
    [Header("Visual Equipment")]
    public GameObject equipPrefab;
    public bool isWeapon;
    public bool isHoe;

    [Header("Combat Stats")]
    public int baseDamage = 25;
    public float knockbackForce = 6f;

    void OnEnable()
    {
        type = ItemType.Tool;
        category = ItemCategory.Tool;
    }
}
