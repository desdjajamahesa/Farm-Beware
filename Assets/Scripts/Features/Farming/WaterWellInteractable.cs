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
    [RequireComponent(typeof(Highlightable))]
    public class WaterWellInteractable : MonoBehaviour, IInteractable, IObstructionExempt
    {
        [Header("Interaction Prompt")]
        [Tooltip("The label shown on screen when player approaches (e.g. 'Refill Plant Waterer')")]
        [SerializeField] private string promptLabel = "Refill Plant Waterer";

        private WorldLabel worldLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSetupWaterWells()
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in transforms)
            {
                if (t != null && t.name.Equals("WaterWell", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (t.GetComponent<Highlightable>() == null)
                    {
                        var h = t.gameObject.AddComponent<Highlightable>();
#if UNITY_EDITOR
                        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Kitchen/Mat_Highlight.mat");
                        if (mat != null) h.SetHighlightMaterial(mat);
#endif
                    }

                    if (t.GetComponent<WaterWellInteractable>() == null)
                    {
                        t.gameObject.AddComponent<WaterWellInteractable>();
                    }
                    var col = t.GetComponent<Collider>();
                    if (col == null)
                    {
                        var box = t.gameObject.AddComponent<BoxCollider>();
                        box.size = new Vector3(2.6f, 2.4f, 2.6f);
                        box.center = new Vector3(0f, 1.2f, 0f);
                    }
                }
            }
        }

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
                box.size = new Vector3(2.6f, 2.4f, 2.6f);
                box.center = new Vector3(0f, 1.2f, 0f);
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
            Vector3 textPos = interactor != null ? interactor.transform.position + Vector3.up * 1.5f : transform.position + Vector3.up * 2f;

            var inv = interactor != null ? interactor.GetComponent<InventoryComponent>() : null;
            if (inv == null)
            {
                var player = ServiceLocator.Resolve<IPlayerContext>();
                if (player != null)
                    inv = player.GetPlayerComponent<InventoryComponent>();
            }

            // Check if player is holding drinking water bottle in active hotbar
            if (inv != null && inv.slots != null)
            {
                int hotbarIdx = inv.selectedHotbarIndex;
                if (hotbarIdx >= 0 && hotbarIdx < inv.slots.Count)
                {
                    var activeSlot = inv.slots[hotbarIdx];
                    if (activeSlot != null && !activeSlot.IsEmpty && activeSlot.item != null)
                    {
                        string activeId = (activeSlot.item.itemId ?? "").ToLower();
                        if (activeId == "food_bottle_water" || activeId.Contains("bottle"))
                        {
                            textService?.SpawnText(
                                textPos,
                                "⚠️ Drinking water bottle can only be refilled at Kitchen Sink!",
                                new Color(1f, 0.6f, 0.2f));
                            return;
                        }
                    }
                }
            }

            // Validate that the player has the Plant Waterer tool in inventory
            bool hasPlantWaterer = CheckHasPlantWaterer(inv);

            // Auto-grant if missing
            if (!hasPlantWaterer && PlantWaterer.Instance != null)
            {
                PlantWaterer.Instance.EnsureWatererInInventory();
                hasPlantWaterer = CheckHasPlantWaterer(inv);
            }

            if (!hasPlantWaterer)
            {
                textService?.SpawnText(
                    textPos,
                    "Requires Plant Waterer to refill farm water!",
                    new Color(1f, 0.6f, 0.2f));
                return;
            }

            // Refill Plant Waterer to 100L
            if (PlantWaterer.Instance != null)
            {
                if (PlantWaterer.Instance.CurrentWater >= PlantWaterer.Instance.MaxWater)
                {
                    textService?.SpawnText(
                        textPos,
                        "💧 Plant Waterer is already full (100/100 L)!",
                        new Color(0.3f, 0.85f, 1f));
                    return;
                }

                PlantWaterer.Instance.RefillWater(100f);
                textService?.SpawnText(
                    textPos,
                    "💧 Plant Waterer Refilled (100/100 L)!",
                    new Color(0.25f, 0.85f, 1f));
            }
        }

        private bool CheckHasPlantWaterer(InventoryComponent inv)
        {
            if (inv == null || inv.slots == null) return false;

            foreach (var slot in inv.slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item != null)
                {
                    string id = (slot.item.itemId ?? "").ToLower();
                    string name = (slot.item.itemName ?? "").ToLower();
                    if (id == "tool_plant_waterer" || name.Contains("plant waterer") || name.Contains("waterer") || name.Contains("watering can"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
