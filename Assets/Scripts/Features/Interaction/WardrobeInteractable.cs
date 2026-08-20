using UnityEngine;

namespace FeaturesInteraction
{
    // Interaksi spesifik Lemari: membuka UI kustomisasi pakaian (belum dibuat).
    public class WardrobeInteractable : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject interactor)
        {
            Debug.Log("Membuka UI Ganti Pakaian (Belum Diimplementasikan)");
        }
    }
}