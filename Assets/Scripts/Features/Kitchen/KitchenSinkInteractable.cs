using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FeaturesInteraction;

/// <summary>
/// Kitchen Sink: mencuci item kotor.
/// Sistem dirty/clean sekarang berbasis data item itu sendiri (FoodItemData.isDirty + cleanVariant),
/// bukan dari aset resep terpisah.
/// Membuka Panel_Sink (Furnace-style) saat interaksi E key.
/// </summary>
[RequireComponent(typeof(InventoryComponent))]
public class KitchenSinkInteractable : KitchenStation, IInteractable
{
    [Header("Wastafel Settings")]
    [Tooltip("Durasi cucian default (detik).")]
    [SerializeField] private float washDuration = 2f;

    [Tooltip("Kategori makanan yang boleh dicuci di wastafel.")]
    [SerializeField] private List<ItemData.FoodCategory> allowedCategories =
        new List<ItemData.FoodCategory>
        {
            ItemData.FoodCategory.Vegetable,
            ItemData.FoodCategory.Fruit,
        };

    [Tooltip("Bila true, hasil cuci otomatis dikembalikan ke Inventory Player.")]
    [SerializeField] private bool returnWashedToPlayer = false;

    [Header("Panel Reference")]
    [Tooltip("Panel_Sink GameObject (Furnace-style UI).")]
    [SerializeField] private GameObject panelSink;

    // Virtual recipe template — reusable instance, tidak perlu asset.
    private KitchenRecipe virtualRecipe;

    protected override void Awake()
    {
        base.Awake();

        // Buat reusable virtual recipe template (runtime-only, bukan asset).
        virtualRecipe = ScriptableObject.CreateInstance<KitchenRecipe>();
        virtualRecipe.name = "VirtualWashRecipe";

        if (allowedCategories != null && allowedCategories.Count > 0)
            stationInventory?.SetAllowedFoodCategories(allowedCategories);

        if (!returnWashedToPlayer)
        {
            resultTarget = null;
            return;
        }

        if (resultTarget == null)
        {
            GameObject playerGO = GameObject.Find("Player");
            if (playerGO != null)
                resultTarget = playerGO.GetComponent<InventoryComponent>();
        }
    }

    protected override KitchenRecipe FindRecipeFor(ItemData item, int slotIndex)
    {
        if (item == null)
            return null;

        // Check FoodItemData (consumables)
        if (item is FoodItemData food && food.isDirty && food.cleanVariant != null)
        {
            virtualRecipe.input = item;
            virtualRecipe.output = food.cleanVariant;
            virtualRecipe.processTime = washDuration;
            virtualRecipe.outputCount = 1;
            return virtualRecipe;
        }

        // Check MaterialItemData (materials like Carrot_Dirty)
        if (item is MaterialItemData mat && mat.isDirty && mat.cleanVariant != null)
        {
            virtualRecipe.input = item;
            virtualRecipe.output = mat.cleanVariant;
            virtualRecipe.processTime = washDuration;
            virtualRecipe.outputCount = 1;
            return virtualRecipe;
        }

        return null;
    }

    public void Interact(GameObject interactor)
    {
        if (panelSink == null)
        {
            Debug.LogWarning("[KitchenSinkInteractable] Panel_Sink tidak ditemukan!");
            return;
        }

        panelSink.SetActive(true);

        // Unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Lock player input
        var playerControl = interactor.GetComponent<PlayerControl>();
        if (playerControl != null)
            playerControl.isInputLocked = true;
    }

    private void Update()
    {
        if (panelSink != null && panelSink.activeSelf &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        // Stop wash coroutine BEFORE hiding panel
        if (panelSink != null)
        {
            var sinkMgr = panelSink.GetComponent<SinkManager>();
            if (sinkMgr != null)
                sinkMgr.ClosePanel();
        }

        if (panelSink != null)
            panelSink.SetActive(false);

        // Restore cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Unlock player input
        var player = FindFirstObjectByType<PlayerControl>();
        if (player != null)
            player.isInputLocked = false;
    }

    private void OnDestroy()
    {
        if (virtualRecipe != null)
            Destroy(virtualRecipe);
    }
}
