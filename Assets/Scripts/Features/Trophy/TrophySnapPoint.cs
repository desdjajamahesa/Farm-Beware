using UnityEngine;

/// <summary>
/// Titik tempel (Snap Point) di ruang 3D untuk rak piala.
/// Murni data: hanya memetakan posisi fisik ini ke satu slot milik
/// Inventory 2 (Trophy Rack). Tidak menyimpan state, tidak meng-instantiate
/// apa pun — seluruh visual dikelola TrophyRackVisuals (Visual Listener)
/// yang bereaksi pada event OnInventoryChanged dari rackInventory.
/// </summary>
public class TrophySnapPoint : MonoBehaviour
{
    /// <summary>
    /// Indeks slot pada InventoryComponent Rack yang dipetakan oleh titik ini.
    /// Diatur dari Inspector (0..3 untuk rak 4 slot); -1 = belum diwire / nonaktif.
    /// </summary>
    public int slotIndex = -1;
}
