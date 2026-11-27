using UnityEngine;

namespace FeaturesInteraction
{
    // Cabinet-specific interaction: enters First-Person Trophy mode
    // while opening the Cabinet <-> Rack UI (Cabinet inventory serves as the source
    // of trophies that can be dragged onto the rack / 3D Snap Points).
    [RequireComponent(typeof(InventoryComponent))]
    public class TrophyCabinetInteractable : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject interactor)
        {
            // Report narrative telemetry for trophy inspection
            FarmBeware.Core.Runtime.NarrativeTelemetryHooks.ReportTrophyInspected();

            // Enter trophy mode (lock player input + switch camera to rack).
            if (TrophySystemManager.Instance != null)
                TrophySystemManager.Instance.EnterTrophyMode();

            // Open Cabinet-ONLY UI (without Rack panel):
            // Rack items are placed/collected via 3D SnapPoints interaction.
            if (InventoryManagerUI.Instance != null)
            {
                InventoryComponent cabinetInv = GetComponent<InventoryComponent>();
                InventoryManagerUI.Instance.OpenTrophyCabinetUI(cabinetInv, null);
            }
        }
    }
}