using System.Collections.Generic;
using UnityEngine;
using FeaturesInteraction;

/// <summary>
/// Stove/Kompor: memasak bahan menjadi makanan siap makan.
/// Setiap slot stasiun memproses satu recipe; hasil matang TINGGAL di slot
/// (pemain mengambilnya), atau bisa diarahkan ke resultTarget bila diinginkan.
/// </summary>
[RequireComponent(typeof(InventoryComponent))]
public class StoveInteractable : KitchenStation, IInteractable
{
    [Header("Recipe Masak (bahan -> makanan siap makan)")]
    [Tooltip("Resep bahan -> hasil + durasi.")]
    [SerializeField] private List<KitchenRecipe> recipes = new List<KitchenRecipe>();

    protected override KitchenRecipe FindRecipeFor(ItemData item, int slotIndex)
    {
        if (item == null)
            return null;

        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null && recipes[i].input == item)
                return recipes[i];
        }

        return null;
    }

    public void Interact(GameObject interactor)
    {
        if (InventoryManagerUI.Instance != null && StationInventory != null)
            InventoryManagerUI.Instance.OpenStorageUI(StationInventory);
    }
}