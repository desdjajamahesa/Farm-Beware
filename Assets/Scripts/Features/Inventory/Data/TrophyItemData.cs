using UnityEngine;

[CreateAssetMenu(fileName = "NewTrophyItem", menuName = "Inventory/Trophy Item Data")]
public class TrophyItemData : ItemData
{
    public GameObject placeablePrefab;

    void OnEnable()
    {
        type = ItemType.Trophy;
    }
}
