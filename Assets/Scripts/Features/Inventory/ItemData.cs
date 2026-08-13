using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public enum ItemType { Material, Consumable, Tool, Trophy }

    // Klasifikasi makanan untuk filter stasiun dapur (Kulkas/Sink/Kompor).
    // State makanan direpresentasikan sebagai ItemData BERBEDA (mis. Carrot_Dirty -> Carrot_Clean).
    public enum FoodCategory { None, Vegetable, Fruit, Meat, Ingredient, Dish }

    public string itemName;
    public Sprite itemIcon;
    public int maxStack;

    public ItemType type;
    public int healAmount;

    // Kategori makanan (dipakai filter: Kulkas hanya sayur/buah, Sink untuk yang bisa dicuci, dst.).
    public FoodCategory foodCategory;

    // Referensi model 3D senjata/obyek yang di-spawn ke tangan pemain saat item di-equip.
    public GameObject equipPrefab;

    // Referensi model 3D yang di-instantiate ke dunia saat item di-drop ke Snap Point
    // (mis. model piala saat disimpan ke rak trophy).
    public GameObject placeablePrefab;
}