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

            // Buka UI KHUSUS Kabinet (tanpa panel Rak):
            // Rak ditempatkan/dikoleksi via interaksi 3D SnapPoints.
            if (InventoryManagerUI.Instance != null)
            {
                InventoryComponent cabinetInv = GetComponent<InventoryComponent>();
                InventoryManagerUI.Instance.OpenTrophyCabinetUI(cabinetInv, null);
            }
        }
    }
}