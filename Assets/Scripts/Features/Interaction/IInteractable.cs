using UnityEngine;

namespace FeaturesInteraction
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);

        /// <summary>
        /// Menentukan apakah objek saat ini bisa diinteraksikan.
        /// Default: true. Override untuk menonaktifkan interaksi dalam kondisi tertentu
        /// (misalnya furniture dekoratif, objek dalam cooldown, dsb).
        /// </summary>
        bool CanInteract(GameObject interactor) => true;
    }
}