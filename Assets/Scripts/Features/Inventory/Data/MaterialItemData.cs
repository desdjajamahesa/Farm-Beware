using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterialItem", menuName = "Inventory/Material Item Data")]
public class MaterialItemData : ItemData
{
    private void OnEnable()
    {
        type = ItemType.Material;
        category = ItemCategory.Material;
    }
}
