using UnityEngine;

namespace FeaturesInteraction
{
    /// <summary>
    /// Interaksi minimal untuk furniture (Kursi, Peti, Meja, dll).
    /// Hanya log interaksi; cocok untuk furniture yang hanya butuh hover glow + label.
    /// Bisa di-extend untuk fungsionalitas khusus (duduk, buka tutup, taruh item).
    /// Furniture tanpa fungsi nyata (hasUsableFunction = false) tidak akan di-highlight.
    /// </summary>
    public class GenericFurnitureInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Jenis furniture untuk logging (Kursi, Peti, Meja, dll).")]
        [SerializeField] private string furnitureType = "Furniture";

        [Tooltip("Pesan custom saat di-interact (kosong = default).")]
        [SerializeField] private string customInteractMessage;

        [Tooltip("Apakah furniture ini memiliki fungsi interaksi nyata (selain dekoratif). "
                 + "Jika false, objek tidak akan muncul highlight dan tidak bisa diinteraksikan.")]
        [SerializeField] private bool hasUsableFunction = false;

        /// <summary>
        /// Hanya izinkan interaksi dan highlight jika furniture punya fungsi nyata
        /// (misal Meja Pedagang), atau jika ada komponen interaktif lain terpasang (MerchantTableInteractable, dll).
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            if (hasUsableFunction) return true;

            // Izinkan jika ada interactable lain yang lebih spesifik pada objek ini
            var others = GetComponents<IInteractable>();
            foreach (var other in others)
            {
                if (other != (IInteractable)this)
                    return true;
            }

            return false;
        }

        public void Interact(GameObject interactor)
        {
            string message = !string.IsNullOrEmpty(customInteractMessage)
                ? customInteractMessage
                : $"Berinteraksi dengan {furnitureType} ({gameObject.name})";

            if (furnitureType == "Table" || furnitureType == "Merchant Table")
            {
                var merchant = GetComponent<FeaturesEconomy.MerchantTableInteractable>();
                if (merchant == null)
                    merchant = gameObject.AddComponent<FeaturesEconomy.MerchantTableInteractable>();
                merchant.Interact(interactor);
                return;
            }
        }

        // Helper untuk setup cepat via Inspector
        public void SetFurnitureType(string type)
        {
            furnitureType = type;
        }

        public void SetCustomMessage(string message)
        {
            customInteractMessage = message;
        }
    }
}