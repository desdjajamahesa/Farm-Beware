using UnityEngine;

[CreateAssetMenu(fileName = "NewToolItem", menuName = "Inventory/Tool Item Data")]
public class ToolItemData : ItemData
{
    public GameObject equipPrefab;

    void OnEnable()
    {
        type = ItemType.Tool;
    }
}
