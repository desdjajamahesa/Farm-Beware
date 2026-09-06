using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterialItem", menuName = "Inventory/Material Item Data")]
public class MaterialItemData : ItemData
{
    void OnEnable()
    {
        type = ItemType.Material;
    }
}
