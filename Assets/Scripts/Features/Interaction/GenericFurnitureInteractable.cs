using UnityEngine;

namespace FeaturesInteraction
{
    /// <summary>
    /// Minimal interaction handler for generic furniture (Chairs, Tables, Chests, etc.).
    /// Suitable for furniture that only needs hover glow + label tooltip.
    /// Decorative furniture without function (hasUsableFunction = false) will not be highlighted.
    /// </summary>
    public class GenericFurnitureInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Furniture category name for interaction logging (Chair, Table, Chest, etc.).")]
        [SerializeField] private string furnitureType = "Furniture";

        [Tooltip("Custom interaction message (empty = default).")]
        [SerializeField] private string customInteractMessage;

        [Tooltip("Whether this furniture has functional gameplay interaction. If false, hover highlight is suppressed.")]
        [SerializeField] private bool hasUsableFunction = false;

        public bool CanInteract(GameObject interactor)
        {
            if (hasUsableFunction) return true;

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
                : $"Interacted with {furnitureType} ({gameObject.name})";

            var otherInteractables = GetComponents<IInteractable>();
            foreach (var other in otherInteractables)
            {
                if (other != (IInteractable)this)
                {
                    other.Interact(interactor);
                    return;
                }
            }

            Debug.Log($"[GenericFurnitureInteractable] {message}");
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