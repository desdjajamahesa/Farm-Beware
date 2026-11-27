using UnityEngine;
using FeaturesInteraction;
using FeaturesWorkbench.UI;

namespace FeaturesWorkbench
{
    /// <summary>
    /// Garage workbench (IInteractable) that opens the weapon upgrade UI.
    /// Allows the player to upgrade base weapon level (Damage Lv 1-3) and unlock seed specialization paths.
    /// </summary>
    [RequireComponent(typeof(WorldLabel))]
    public class WorkbenchInteractable : MonoBehaviour, IInteractable
    {
        private WorldLabel worldLabel;

        private void Awake()
        {
            worldLabel = GetComponent<WorldLabel>();
            if (worldLabel != null && string.IsNullOrEmpty(worldLabel.displayName))
            {
                worldLabel.displayName = "Workbench (Weapon Upgrade)";
            }
        }

        public void Interact(GameObject interactor)
        {
            var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
            int currentDay = timeService != null ? timeService.CurrentDay : 1;
            if (currentDay == 0)
            {
                FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IFloatingTextService>()?.SpawnText(
                    transform.position + Vector3.up * 1.2f,
                    "Rest first. The harvest begins tomorrow.",
                    new Color(1f, 0.8f, 0.4f));
                return;
            }

            if (WorkbenchUI.Instance == null)
            {
                var existing = Object.FindAnyObjectByType<WorkbenchUI>(FindObjectsInactive.Include);
                if (existing != null)
                {
                    existing.Open();
                    return;
                }

                // If not present on UI Canvas yet, build procedurally
                var canvas = GameObject.Find("UI_Canvas");
                if (canvas != null)
                {
                    var uiObj = new GameObject("Panel_Workbench");
                    uiObj.transform.SetParent(canvas.transform, false);
                    var wbUI = uiObj.AddComponent<WorkbenchUI>();
                    wbUI.Open();
                    return;
                }
            }
            else
            {
                WorkbenchUI.Instance.Open();
            }
        }
    }
}
