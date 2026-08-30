using UnityEngine;

namespace FeaturesInteraction
{
    /// <summary>
    /// Interaksi minimal untuk furniture (Kursi, Peti, Meja, dll).
    /// Hanya log interaksi; cocok untuk furniture yang hanya butuh hover glow + label.
    /// Bisa di-extend untuk fungsionalitas khusus (duduk, buka tutup, taruh item).
    /// </summary>
    public class GenericFurnitureInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Jenis furniture untuk logging (Kursi, Peti, Meja, dll).")]
        [SerializeField] private string furnitureType = "Furniture";

        [Tooltip("Pesan custom saat di-interact (kosong = default).")]
        [SerializeField] private string customInteractMessage;

        public void Interact(GameObject interactor)
        {
            string message = !string.IsNullOrEmpty(customInteractMessage)
                ? customInteractMessage
                : $"Berinteraksi dengan {furnitureType} ({gameObject.name})";

            Debug.Log(message);

            // TODO: Tambahkan logic khusus per furniture type di sini
            // Contoh:
            // if (furnitureType == "Chair") SitDown(interactor);
            // if (furnitureType == "Chest") OpenChest();
            // if (furnitureType == "Table") PlaceItem(interactor);
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