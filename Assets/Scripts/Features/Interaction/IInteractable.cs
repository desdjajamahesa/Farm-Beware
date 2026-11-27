using UnityEngine;

namespace FeaturesInteraction
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);

        /// <summary>
        /// Determines whether this object can currently be interacted with.
        /// Default: true. Override to disable interaction under specific conditions
        /// (e.g. decorative furniture, objects on cooldown, etc.).
        /// </summary>
        bool CanInteract(GameObject interactor) => true;
    }
}