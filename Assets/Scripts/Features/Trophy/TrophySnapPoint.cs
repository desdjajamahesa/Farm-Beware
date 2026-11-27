using UnityEngine;

/// <summary>
/// 3D Snap Point anchor for the trophy rack.
/// Pure data: only maps this physical position to a single slot belonging to
/// Inventory 2 (Trophy Rack). Holds no state and instantiates nothing —
/// all visuals are managed by TrophyRackVisuals (Visual Listener)
/// reacting to OnInventoryChanged from rackInventory.
/// </summary>
public class TrophySnapPoint : MonoBehaviour
{
    /// <summary>
    /// Slot index on the Rack InventoryComponent mapped by this point.
    /// Configured in Inspector (0..3 for a 4-slot rack); -1 = unwired / inactive.
    /// </summary>
    public int slotIndex = -1;
}
