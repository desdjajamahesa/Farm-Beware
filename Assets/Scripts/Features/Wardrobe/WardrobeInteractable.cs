using FeaturesWardrobe;
using UnityEngine;

namespace FeaturesInteraction
{
    public class WardrobeInteractable : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject interactor)
        {
            if (WardrobeManager.Instance != null)
                WardrobeManager.Instance.EnterWardrobeMode();
            else
                Debug.LogWarning("[WardrobeInteractable] WardrobeManager.Instance not found!");
        }
    }
}