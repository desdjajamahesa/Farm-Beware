using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesFarming;
using FeaturesKitchen;
using FeaturesEconomy;

namespace Player
{
    /// <summary>
    /// Guarantees that the player starts with the official MVP starter kit:
    /// - Slot 0: Hoe (Tool_Hoe)
    /// - Slot 1: Plant Waterer (Tool_PlantWaterer 100L)
    /// - Slot 2: Bottle of Water (Food_BottleOfWater 4 charges)
    /// - Slot 3: 6x Sweet Potato Seeds (Seed_SweetPotato)
    /// - Dummy Sword is NOT in starter kit; must be bought from merchant table.
    /// - Full capacity for PlantWaterer and PlayerWaterBottle.
    /// </summary>
    public class PlayerStarterKit : MonoBehaviour
    {
        [Header("Starter Kit Items (Auto-loaded if null)")]
        [SerializeField] private ItemData hoeItem;
        [SerializeField] private ItemData plantWatererItem;
        [SerializeField] private ItemData waterBottleItem;
        [SerializeField] private ItemData sweetPotatoSeedItem;
        [SerializeField] private int starterSeedCount = 6;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachToPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null && player.GetComponent<PlayerStarterKit>() == null)
            {
                player.AddComponent<PlayerStarterKit>();
            }
        }

        private void Start()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                timeService.OnDayChanged += HandleDayChanged;
            }
            EnsureStarterKit();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                timeService.OnDayChanged -= HandleDayChanged;
            }
        }

        private void HandleDayChanged(int newDay)
        {
            if (newDay >= 1)
            {
                EnsureStarterKit();
            }
        }

        public void EnsureStarterKit()
        {
            var inv = GetComponent<InventoryComponent>();
            if (inv == null) return;

            ResolveItemReferences();

            if (inv.maxCapacity < 16) inv.maxCapacity = 16;
            inv.EnsureSlotsAllocated();

            int currentDay = 0;
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                currentDay = timeService.CurrentDay;
            }
            else if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                currentDay = FeaturesTime.DayNightTimeManager.Instance.CurrentDay;
            }
            else if (TimeManager.Instance != null)
            {
                currentDay = TimeManager.Instance.currentDay;
            }

            // On Day 0: Player arrives with completely empty pockets per narrative design.
            if (currentDay == 0)
            {
                if (inv.slots != null)
                {
                    for (int i = 0; i < inv.slots.Count; i++)
                    {
                        inv.slots[i] = new InventorySlot();
                    }
                }
                inv.OnInventoryChanged?.Invoke();
                return;
            }

            // Cleanse any accidental Mat_MutatedRoot
            var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db != null)
            {
                var mutatedRoot = db.GetItem("mat_mutated_root");
                if (mutatedRoot != null && inv.CountItem(mutatedRoot) > 0)
                {
                    inv.RemoveItem(mutatedRoot, inv.CountItem(mutatedRoot));
                }
            }

            // 1. Check if hotbar is completely uninitialized or empty
            bool isEmptyInventory = true;
            if (inv.slots != null)
            {
                foreach (var slot in inv.slots)
                {
                    if (slot != null && !slot.IsEmpty)
                    {
                        isEmptyInventory = false;
                        break;
                    }
                }
            }

            if (isEmptyInventory)
            {
                PopulateFreshSlots(inv);
            }
            else
            {
                // Fallback guarantee: if individual key tools are completely missing, add them safely
                if (hoeItem != null && inv.CountItem(hoeItem) == 0) inv.AddItem(hoeItem, 1);
                if (plantWatererItem != null && inv.CountItem(plantWatererItem) == 0) inv.AddItem(plantWatererItem, 1);
                if (waterBottleItem != null && inv.CountItem(waterBottleItem) == 0) inv.AddItem(waterBottleItem, 1);
                if (sweetPotatoSeedItem != null && inv.CountItem(sweetPotatoSeedItem) == 0) inv.AddItem(sweetPotatoSeedItem, starterSeedCount);
            }

            // 2. Ensure water tool states
            if (PlantWaterer.Instance != null && PlantWaterer.Instance.CurrentWater <= 0f)
            {
                PlantWaterer.Instance.RefillWater(-1f);
            }
            if (PlayerWaterBottle.Instance != null && PlayerWaterBottle.Instance.CurrentWater <= 0f)
            {
                PlayerWaterBottle.Instance.RefillWater(-1f);
            }

            inv.OnInventoryChanged?.Invoke();
        }

        private void PopulateFreshSlots(InventoryComponent inv)
        {
            if (inv.slots == null || inv.slots.Count < 4) return;

            if (hoeItem != null) inv.slots[0] = new InventorySlot(hoeItem, 1);
            if (plantWatererItem != null) inv.slots[1] = new InventorySlot(plantWatererItem, 1);
            if (waterBottleItem != null) inv.slots[2] = new InventorySlot(waterBottleItem, 1);
            if (sweetPotatoSeedItem != null) inv.slots[3] = new InventorySlot(sweetPotatoSeedItem, starterSeedCount);
        }

        private void ResolveItemReferences()
        {
            var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db != null)
            {
                if (hoeItem == null) hoeItem = db.GetItem("tool_hoe");
                if (plantWatererItem == null) plantWatererItem = db.GetItem("tool_plant_waterer");
                if (waterBottleItem == null) waterBottleItem = db.GetItem("food_bottle_water");
                if (sweetPotatoSeedItem == null) sweetPotatoSeedItem = db.GetItem("seed_sweet_potato");
            }
        }
    }
}
