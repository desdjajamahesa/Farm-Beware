using UnityEngine;

/// <summary>
/// Resep proses dapur (mencuci / memasak). Data murni, di-edit di Inspector.
/// State makanan direpresentasikan sebagai ItemData BERBEDA (input -> output).
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "FarmBeware/Kitchen Recipe")]
public class KitchenRecipe : ScriptableObject
{
    [Tooltip("Bahan yang dimasukkan ke slot (harus item ini).")]
    public ItemData input;

    [Tooltip("Hasil setelah proses selesai (state baru).")]
    public ItemData output;

    [Tooltip("Jumlah hasil yang diproduksi.")]
    public int outputCount = 1;

    [Tooltip("Durasi proses dalam detik.")]
    public float processTime = 3f;
}