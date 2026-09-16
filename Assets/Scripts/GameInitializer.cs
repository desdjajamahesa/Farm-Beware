using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private InventoryComponent playerInventory;
    [SerializeField] private ItemData starterSword;

    void Awake()
    {
        if (playerInventory == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                playerInventory = player.GetComponent<InventoryComponent>();
        }

        if (playerInventory != null && starterSword != null)
        {
            if (playerInventory.CountItem(starterSword) == 0)
            {
                playerInventory.AddItem(starterSword, 1);
                Debug.Log("[GameInitializer] Starter sword added to inventory!");
            }
        }
    }
}
