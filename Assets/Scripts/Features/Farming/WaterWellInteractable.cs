using UnityEngine;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;

namespace FeaturesFarming
{
    /// <summary>
    /// Garden Well Interactable:
    /// Enables the Water Well in the garden to act as the player's refill station for Plant Waterer.
    /// Can ONLY refill the Plant Waterer tool (100L).
    /// Does NOT refill the player's drinking water bottle (which is refillable only at Kitchen Sink).
    /// </summary>
    public class WaterWellInteractable : MonoBehaviour, IInteractable, IObstructionExempt
    {
        [Header("Interaction Prompt")]
        [Tooltip("The label shown on screen when player approaches (e.g. 'Refill Plant Waterer')")]
        [SerializeField] private string promptLabel = "Refill Plant Waterer";

        private WorldLabel worldLabel;

        private void Awake()
        {
            worldLabel = GetComponent<WorldLabel>();
            if (worldLabel == null)
                worldLabel = GetComponentInChildren<WorldLabel>();
            if (worldLabel == null)
                worldLabel = gameObject.AddComponent<WorldLabel>();

            worldLabel.displayName = promptLabel;

            // Ensure collider is active so PlayerInteractor raycast/overlap detects the well
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(2.5f, 2.5f, 2.5f);
                box.center = new Vector3(0f, 1.25f, 0f);
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
            var textService = ServiceLocator.Resolve<IFloatingTextService>();
            Vector3 textPos = transform.position + Vector3.up * 2f;

            // Validate that the player has the Plant Waterer tool in inventory
            bool hasPlantWaterer = false;
            var inv = interactor != null ? interactor.GetComponent<InventoryComponent>() : null;
            if (inv == null)
            {
                var player = ServiceLocator.Resolve<IPlayerContext>();
                if (player != null)
                    inv = player.GetPlayerComponent<InventoryComponent>();
            }

            if (inv != null && inv.slots != null)
            {
                foreach (var slot in inv.slots)
                {
                    if (slot != null && !slot.IsEmpty && slot.item != null)
                    {
                        string id = (slot.item.itemId ?? "").ToLower();
                        string name = (slot.item.itemName ?? "").ToLower();
                        if (id == "tool_plant_waterer" || name.Contains("plant waterer") || name.Contains("waterer") || name.Contains("watering can"))
                        {
                            hasPlantWaterer = true;
                            break;
                        }
                    }
                }
            }

            if (!hasPlantWaterer)
            {
                if (textService != null)
                {
                    textService.SpawnText(
                        textPos,
                        "Requires Plant Waterer to refill!",
                        new Color(1f, 0.6f, 0.2f));
                }
                return;
            }

            // Refill Plant Waterer to 100L
            if (PlantWaterer.Instance != null)
            {
                PlantWaterer.Instance.RefillWater(100f);
                if (textService != null)
                {
                    textService.SpawnText(
                        textPos,
                        "💧 Plant Waterer Refilled (100/100 L)!",
                        new Color(0.25f, 0.85f, 1f));
                }
            }
        }
    }
}
