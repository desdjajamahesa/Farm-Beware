using System.Collections;
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
    /// Has a 10s refill duration and maximum 2 refills per day.
    /// </summary>
    [RequireComponent(typeof(Highlightable))]
    public class WaterWellInteractable : MonoBehaviour, IInteractable, IObstructionExempt
    {
        [Header("Interaction Prompt")]
        [Tooltip("The label shown on screen when player approaches (e.g. 'Draw Water from Well')")]
        [SerializeField] private string promptLabel = "Draw Water from Well";

        [Header("Refill Settings")]
        [Tooltip("Time required to refill water in seconds.")]
        [SerializeField] private float refillDuration = 10f;

        [Tooltip("Maximum times the well can be refilled per in-game day.")]
        [SerializeField] private int maxRefillsPerDay = 2;

        private int refillsUsedToday = 0;
        private bool isRefilling = false;
        private float refillTimer = 0f;
        private Coroutine refillCoroutine;
        private WorldLabel worldLabel;
        private WaterRefillProgressUI refillProgressUI;

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
            refillProgressUI = GetComponent<WaterRefillProgressUI>() ?? gameObject.AddComponent<WaterRefillProgressUI>();
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

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            UpdatePromptLabel();
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            UpdatePromptLabel();
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;

            if (refillCoroutine != null)
            {
                StopCoroutine(refillCoroutine);
                refillCoroutine = null;
                isRefilling = false;
            }

            if (refillProgressUI != null)
                refillProgressUI.UpdateRefillState(false, 0f, 0f, false);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int newDay)
        {
            refillsUsedToday = 0;
            UpdatePromptLabel();
        }

        private void UpdatePromptLabel()
        {
            if (worldLabel == null) return;
            int remaining = Mathf.Max(0, maxRefillsPerDay - refillsUsedToday);
            if (isRefilling)
            {
                worldLabel.displayName = $"Drawing Water... ({Mathf.CeilToInt(refillTimer)}s)";
            }
            else if (remaining <= 0)
            {
                worldLabel.displayName = $"{promptLabel} (Depleted: 0/{maxRefillsPerDay})";
            }
            else
            {
                worldLabel.displayName = $"{promptLabel} ({remaining}/{maxRefillsPerDay} Left)";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            var textService = ServiceLocator.Resolve<IFloatingTextService>();
            Vector3 textPos = interactor != null ? interactor.transform.position + Vector3.up * 1.5f : transform.position + Vector3.up * 2f;

            if (isRefilling)
            {
                textService?.SpawnText(
                    textPos,
                    $"💧 Drawing water from well... ({Mathf.CeilToInt(refillTimer)}s remaining)",
                    new Color(0.3f, 0.85f, 1f));
                return;
            }

            if (refillsUsedToday >= maxRefillsPerDay)
            {
                textService?.SpawnText(
                    textPos,
                    $"⚠️ Garden Well depleted for today! (0/{maxRefillsPerDay} refills left)",
                    new Color(1f, 0.4f, 0.3f));
                return;
            }

            var waterService = ServiceLocator.Resolve<IWaterService>();
            bool watererNeedsRefill = PlantWaterer.Instance != null && PlantWaterer.Instance.CurrentWater < PlantWaterer.Instance.MaxWater;
            bool bottleNeedsRefill = waterService != null && waterService.CurrentWater < waterService.MaxWater;

            if (!watererNeedsRefill && !bottleNeedsRefill)
            {
                textService?.SpawnText(
                    textPos,
                    "💧 All water containers already full (100/100 L)!",
                    new Color(0.3f, 0.85f, 1f));
                return;
            }

            refillCoroutine = StartCoroutine(RoutineRefillWaterer(textPos));
        }

        private IEnumerator RoutineRefillWaterer(Vector3 textPos)
        {
            isRefilling = true;
            refillTimer = refillDuration;

            var textService = ServiceLocator.Resolve<IFloatingTextService>();
            textService?.SpawnText(
                textPos,
                $"💧 Drawing water from well... ({Mathf.CeilToInt(refillTimer)}s)",
                new Color(0.25f, 0.85f, 1f));

            UpdatePromptLabel();

            while (refillTimer > 0f)
            {
                float timeMultiplier = FeaturesTime.DayNightTimeManager.Instance != null
                    ? FeaturesTime.DayNightTimeManager.Instance.FastForwardMultiplier
                    : 1f;
                refillTimer -= Time.deltaTime * timeMultiplier;
                UpdatePromptLabel();

                float progress01 = 1f - (refillTimer / Mathf.Max(0.1f, refillDuration));
                if (refillProgressUI != null)
                {
                    refillProgressUI.UpdateRefillState(true, progress01, refillTimer, false, "WELL");
                }
                yield return null;
            }

            if (refillProgressUI != null)
            {
                refillProgressUI.UpdateRefillState(false, 1f, 0f, true, "WELL");
            }

            isRefilling = false;
            refillCoroutine = null;

            if (PlantWaterer.Instance != null)
            {
                PlantWaterer.Instance.RefillWater(100f);
            }

            var waterService = ServiceLocator.Resolve<IWaterService>();
            if (waterService != null)
            {
                waterService.RefillWater(100f);
            }

            refillsUsedToday++;

            int remaining = Mathf.Max(0, maxRefillsPerDay - refillsUsedToday);
            textService?.SpawnText(
                textPos,
                $"💧 Water Containers Refilled (100L)! ({remaining}/{maxRefillsPerDay} left today)",
                new Color(0.25f, 0.85f, 1f));

            UpdatePromptLabel();
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
