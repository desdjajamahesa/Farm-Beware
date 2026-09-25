using UnityEngine;
using FeaturesInteraction;
using FeaturesWorkbench.UI;

namespace FeaturesWorkbench
{
    /// <summary>
    /// Meja kerja di garasi (IInteractable) yang membuka UI upgrade senjata.
    /// Memungkinkan pemain meningkatkan level dasar senjata (Damage Lv 1-3) dan membuka jalur spesialisasi bibit.
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
            if (WorkbenchUI.Instance == null)
            {
                var existing = Object.FindAnyObjectByType<WorkbenchUI>(FindObjectsInactive.Include);
                if (existing != null)
                {
                    existing.Open();
                    return;
                }

                // Jika belum ada di UI Canvas, bangun secara prosedural
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
