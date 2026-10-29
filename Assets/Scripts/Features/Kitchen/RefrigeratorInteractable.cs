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
    public static event System.Action OnRefrigeratorOpened;

    [Tooltip("Kategori makanan yang boleh disimpan (default: sayur, buah, daging mentah, masakan jadi).")]
    [SerializeField] private List<ItemData.FoodCategory> allowedCategories =
        new List<ItemData.FoodCategory>
        {
            ItemData.FoodCategory.Vegetable,
            ItemData.FoodCategory.Fruit,
            ItemData.FoodCategory.Meat,
            ItemData.FoodCategory.Dish,
        };

    private void Awake()
    {
        if (allowedCategories == null || allowedCategories.Count == 0)
        {
            allowedCategories = new List<ItemData.FoodCategory>
            {
                ItemData.FoodCategory.Vegetable,
                ItemData.FoodCategory.Fruit,
                ItemData.FoodCategory.Meat,
                ItemData.FoodCategory.Dish,
            };
        }

        ApplyRestriction(GetComponent<InventoryComponent>());
    }

    private void Start()
    {
        EnsureQuestIngredients();
    }

    public void EnsureQuestIngredients()
    {
        var inventory = GetComponent<InventoryComponent>();
        if (inventory == null) return;

        ApplyRestriction(inventory);

        var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
        if (db == null) return;

        var taroItem = db.GetItem("crop_taro");
        var coconutItem = db.GetItem("mat_coconut");

        if (taroItem != null && inventory.CountItem(taroItem) == 0)
        {
            inventory.AddItem(taroItem, 1);
        }
        if (coconutItem != null && inventory.CountItem(coconutItem) == 0)
        {
            inventory.AddItem(coconutItem, 1);
        }
    }

    private void ApplyRestriction(InventoryComponent inventory)
    {
        // Backend: pastikan InventoryComponent milik kulkas membatasi kategori untuk menyimpan semua tanaman & makanan.
        inventory?.SetAllowedFoodCategories(allowedCategories);
        inventory?.SetAllowAllCropsAndFood(true);
    }

    public void Interact(GameObject interactor)
    {
        InventoryComponent inventory = GetComponent<InventoryComponent>();
        if (inventory == null)
            return;

        // Pastikan pembatasan kategori selalu aktif (backend, bukan UI).
        ApplyRestriction(inventory);
        EnsureQuestIngredients();

        OnRefrigeratorOpened?.Invoke();

        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.OpenStorageUI(inventory, "Refrigerator", InventoryManagerUI.Instance.refrigeratorPanel);
    }
}