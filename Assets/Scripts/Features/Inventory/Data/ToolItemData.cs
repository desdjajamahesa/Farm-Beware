using UnityEngine;

[CreateAssetMenu(fileName = "NewToolItem", menuName = "Inventory/Tool Item Data")]
public class ToolItemData : ItemData
{
    public GameObject equipPrefab;
    public bool isWeapon;
    public bool isHoe;

    void OnEnable()
    {
        type = ItemType.Tool;
        category = ItemCategory.Tool;
    }
}
