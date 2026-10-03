using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining an object in the world that can receive player interaction.
    /// </summary>
    public interface IInteractable
    {
        void Interact(GameObject interactor);
        bool CanInteract(GameObject interactor);
    }
}
