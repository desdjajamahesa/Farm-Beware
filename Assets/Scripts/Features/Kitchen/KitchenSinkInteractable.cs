using System.Collections.Generic;
using UnityEngine;
using FeaturesInteraction;

/// <summary>
/// Kitchen Sink: mencuci sayuran/buah kotor.
/// Saat item (input recipe) diletakkan di slot stasiun, proses MENCUCI otomatis berjalan
/// (timer backend di KitchenStation) lalu hasil bersih otomatis kembali ke inventory player
/// (resultTarget). UI terbuka ala storage generik.
/// </summary>
[RequireComponent(typeof(InventoryComponent))]
public class KitchenSinkInteractable : KitchenStation, IInteractable
{
    [Header("Recipe Pencucian (kotor -> bersih)")]
    [Tooltip("Mapping item kotor -> item bersih + durasi.")]
    [SerializeField] private List<KitchenRecipe> washRecipes = new List<KitchenRecipe>();

    [Tooltip("Bila true, hasil cuci otomatis dikembalikan ke Inventory Player. Bila false (default), hasil tetap di slot sink seperti Stove.")]
    [SerializeField] private bool returnWashedToPlayer = false;

    protected override void Awake()
    {
        base.Awake();

        // Kebijakan hasil cuci: sesuai flag. Bila false, pastikan resultTarget kosong
        // (hasil tetap di slot sink) walau ada wiring lama yang menunjuk ke Player.
        if (!returnWashedToPlayer)
        {
            resultTarget = null;
            return;
        }

        // Auto-resolve target hasil ke Inventory Player (backend, bukan di UI).
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

        for (int i = 0; i < washRecipes.Count; i++)
        {
            if (washRecipes[i] != null && washRecipes[i].input == item)
                return washRecipes[i];
        }

        return null;
    }

    public void Interact(GameObject interactor)
    {
        if (InventoryManagerUI.Instance != null && StationInventory != null)
            InventoryManagerUI.Instance.OpenStorageUI(StationInventory);
    }
}