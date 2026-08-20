using UnityEngine;

namespace FeaturesInteraction
{
    // Interaksi spesifik untuk Kabinet: masuk ke mode First-Person Trophy
    // sekaligus membuka UI Kabinet <-> Rak (inventori Kabinet sebagai sumber
    // trophy yang bisa di-drag ke rak/Snap Point 3D).
    [RequireComponent(typeof(InventoryComponent))]
    public class TrophyCabinetInteractable : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject interactor)
        {
            // Masuk mode trophy (kunci input pemain + pindah kamera ke rak).
            if (TrophySystemManager.Instance != null)
                TrophySystemManager.Instance.EnterTrophyMode();

            // Buka UI KHUSUS Kabinet <-> Rak:
            //   KIRI  = Inventory Kabinet (berisi trophy, siap di-drag keluar),
            //   KANAN = Inventory Rak (4 slot).
            // Panel Inventory Player TIDAK dibuka — trophy hanya boleh di Kabinet/Rak.
            if (InventoryManagerUI.Instance != null)
            {
                InventoryComponent cabinetInv = GetComponent<InventoryComponent>();
                InventoryComponent rackInv = TrophySystemManager.Instance != null
                    ? TrophySystemManager.Instance.RackInventory
                    : null;
                InventoryManagerUI.Instance.OpenTrophyCabinetUI(cabinetInv, rackInv);
            }
        }
    }
}