using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Satu bahan dalam resep: item + jumlah yang dibutuhkan.
/// Digunakan oleh GenshinStove untuk mengecek inventory pemain.
/// </summary>
[System.Serializable]
public class RecipeIngredient
{
    public ItemData item;
    public int quantity = 1;
}

/// <summary>
/// Resep proses dapur (mencuci / memasak). Data murni, di-edit di Inspector.
/// Mendukung dua mode:
/// - Sink: pakai field `input` tunggal (backward compat, auto-start).
/// - Genshin Stove: pakai field `ingredients` list (manual cook, cek dari inventory pemain).
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "FarmBeware/Kitchen Recipe")]
public class KitchenRecipe : ScriptableObject
{
    [Header("Legacy (Sink)")]
    [Tooltip("Bahan tunggal untuk Sink (backward compat). Abaikan jika pakai ingredients list.")]
    public ItemData input;

    [Header("Genshin Cooking")]
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

    [Tooltip("Durasi proses dalam detik (untuk Sink; Genshin Stove = instan).")]
    public float processTime = 3f;

    // === ACCESSORS ===

    /// <summary>True jika resep pakai multi-bahan (Genshin mode).</summary>
    public bool IsMultiIngredient => ingredients != null && ingredients.Count > 0;

    /// <summary>Dapatkan semua bahan (gabungkan field lama + baru).</summary>
    public IReadOnlyList<RecipeIngredient> GetAllIngredients()
    {
        if (IsMultiIngredient)
            return ingredients;
        if (input != null)
            return new[] { new RecipeIngredient { item = input, quantity = 1 } };
        return System.Array.Empty<RecipeIngredient>();
    }
}
