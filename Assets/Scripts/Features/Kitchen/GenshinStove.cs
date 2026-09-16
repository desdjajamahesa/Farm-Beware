using UnityEngine;
using FeaturesInteraction;

/// <summary>
/// Kompor gaya Genshin Impact: tidak ada slot drag-drop.
/// Saat interaksi (E key), membuka UI buku resep di tengah layar.
/// Bahan dicek dari inventory pemain, hasil langsung masuk inventory pemain.
/// </summary>
public class GenshinStove : MonoBehaviour, IInteractable
{
    [Header("Resep Masak")]
    [Tooltip("Semua resep yang tersedia di kompor ini.")]
    [SerializeField] private KitchenRecipe[] recipes;

    [Header("UI Reference")]
    [Tooltip("StoveUIManager yang mengontrol Panel_Stove. Jika kosong, cari di scene.")]
    [SerializeField] private StoveUIManager stoveUI;

    private void Awake()
    {
        if (stoveUI == null)
            stoveUI = FindFirstObjectByType<StoveUIManager>();
    }

    public void Interact(GameObject interactor)
    {
        if (stoveUI == null)
        {
            Debug.LogWarning("[GenshinStove] StoveUIManager tidak ditemukan!");
            return;
        }

        // Dapatkan inventory pemain
        InventoryComponent playerInv = interactor.GetComponent<InventoryComponent>();
        if (playerInv == null)
        {
            Debug.LogWarning("[GenshinStove] Player tidak punya InventoryComponent!");
            return;
        }

        stoveUI.Open(recipes, playerInv);
    }
}
