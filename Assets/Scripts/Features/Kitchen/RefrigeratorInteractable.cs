using System.Collections.Generic;
using UnityEngine;
using FeaturesInteraction;

/// <summary>
/// Refrigerator (Kulkas): menyimpan/mengawetkan sayuran & buah.
/// Membuka UI storage generik dengan inventory kulkas di kanan.
/// Inventory kulkas diprogram membatasi FoodCategory (default: Vegetable & Fruit).
/// </summary>
[RequireComponent(typeof(InventoryComponent))]
public class RefrigeratorInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Kategori makanan yang boleh disimpan (default: sayur & buah).")]
    [SerializeField] private List<ItemData.FoodCategory> allowedCategories =
        new List<ItemData.FoodCategory>
        {
            ItemData.FoodCategory.Vegetable,
            ItemData.FoodCategory.Fruit,
        };

    private void Awake()
    {
        if (allowedCategories == null || allowedCategories.Count == 0)
        {
            allowedCategories = new List<ItemData.FoodCategory>
            {
                ItemData.FoodCategory.Vegetable,
                ItemData.FoodCategory.Fruit,
            };
        }

        ApplyRestriction(GetComponent<InventoryComponent>());
    }

    private void ApplyRestriction(InventoryComponent inventory)
    {
        // Backend: pastikan InventoryComponent milik kulkas membatasi kategori.
        inventory?.SetAllowedFoodCategories(allowedCategories);
    }

    public void Interact(GameObject interactor)
    {
        InventoryComponent inventory = GetComponent<InventoryComponent>();
        if (inventory == null)
            return;

        // Pastikan pembatasan kategori selalu aktif (backend, bukan UI).
        ApplyRestriction(inventory);

        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.OpenStorageUI(inventory);
    }
}