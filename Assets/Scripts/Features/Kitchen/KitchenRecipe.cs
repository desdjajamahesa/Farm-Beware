using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Single recipe ingredient: item + required quantity.
/// Used by KitchenStove to check the player's inventory.
/// </summary>
[System.Serializable]
public class RecipeIngredient
{
    public ItemData item;
    public int quantity = 1;
}

/// <summary>
/// Kitchen processing recipe (washing / cooking). Pure data, edited in Inspector.
/// Supports two modes:
/// - Sink: uses single `input` field (backward compat, auto-start).
/// - Stove: uses `ingredients` list (manual cook, checked from player inventory).
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "FarmBeware/Kitchen Recipe")]
public class KitchenRecipe : ScriptableObject
{
    [Header("Legacy (Sink)")]
    [Tooltip("Bahan tunggal untuk Sink (backward compat). Abaikan jika pakai ingredients list.")]
    public ItemData input;

    [Header("Stove Cooking")]
    [Tooltip("Nama unik resep untuk ditampilkan di tombol buku resep.")]
    public string recipeName;

    [Tooltip("Daftar bahan yang diperlukan (dicek dari inventory pemain saat masak).")]
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

    [Tooltip("Icon resep untuk tampilan buku resep.")]
    public Sprite recipeIcon;

    [Tooltip("Deskripsi resep untuk UI detail.")]
    [TextArea(2, 4)]
    public string description;

    [Header("Output")]
    [Tooltip("Hasil setelah proses selesai.")]
    public ItemData output;

    [Tooltip("Jumlah hasil yang diproduksi.")]
    public int outputCount = 1;

    [Tooltip("Durasi proses dalam detik.")]
    public float processTime = 3f;

    [Header("Water Requirements")]
    [Tooltip("Jumlah air yang dibutuhkan untuk memasak resep ini (Liter). Contoh: Dumpling = 15, Porridge = 10, Stew = 20.")]
    public float waterRequired = 0f;

    // === ACCESSORS ===

    /// <summary>True if recipe uses multiple ingredients (Stove mode).</summary>
    public bool IsMultiIngredient => ingredients != null && ingredients.Count > 0;

    /// <summary>Get all ingredients (combines legacy + new fields).</summary>
    public IReadOnlyList<RecipeIngredient> GetAllIngredients()
    {
        if (IsMultiIngredient)
            return ingredients;
        if (input != null)
            return new[] { new RecipeIngredient { item = input, quantity = 1 } };
        return System.Array.Empty<RecipeIngredient>();
    }
}
