using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterialItem", menuName = "Inventory/Material Item Data")]
public class MaterialItemData : ItemData
{
    [Header("Dirty/Clean System")]
    [Tooltip("True = item ini kotor, bisa dicuci di Wastafel.")]
    public bool isDirty;

    [Tooltip("Item hasil cucian. Isi hanya pada item kotor.")]
    public ItemData cleanVariant;

    void OnEnable()
    {
        type = ItemType.Material;
    }
}
