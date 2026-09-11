using UnityEngine;

[CreateAssetMenu(fileName = "NewToolItem", menuName = "Inventory/Tool Item Data")]
public class ToolItemData : ItemData
{
    void OnEnable()
    {
        type = ItemType.Tool;
    }
}
