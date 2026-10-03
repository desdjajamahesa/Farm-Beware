using UnityEngine;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;

namespace FeaturesSaveSystem
{
    /// <summary>
    /// Bedroom Table Save Station:
    /// Enables the table in BedroomZone to act as the player's saving & loading desk.
    /// When approached, displays the interact prompt '[ E ] Load' matching door/bed interactions.
    /// Pressing E opens the Saving System modal with Save and Load capabilities.
    /// </summary>
    public class SaveStationInteractable : MonoBehaviour, IInteractable, IObstructionExempt
    {
        [Header("Interaction Prompt")]
        [Tooltip("The label shown on screen when player approaches (e.g. 'Save / Load')")]
        [SerializeField] private string promptLabel = "Save / Load";

        private WorldLabel worldLabel;

        private void Awake()
        {
            worldLabel = GetComponent<WorldLabel>();
            if (worldLabel == null)
                worldLabel = GetComponentInChildren<WorldLabel>();
            if (worldLabel == null)
                worldLabel = gameObject.AddComponent<WorldLabel>();

            worldLabel.displayName = promptLabel;

            // Ensure collider is active
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1.2f, 1f, 2.6f);
            }
        }

        private void OnEnable()
        {
            if (worldLabel != null)
                worldLabel.displayName = promptLabel;
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (SaveSystemUI.Instance != null)
            {
                SaveSystemUI.Instance.Open();
            }
            else
            {
                Debug.LogWarning("[SaveStationInteractable] SaveSystemUI instance not found in scene!");
            }
        }

        public void SetPromptLabel(string newLabel)
        {
            promptLabel = newLabel;
            if (worldLabel != null)
                worldLabel.displayName = promptLabel;
        }
    }
}
