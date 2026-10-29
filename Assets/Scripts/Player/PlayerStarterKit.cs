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

        [Header("Target Storage Chest (Auto-found if null)")]
        [SerializeField] private InventoryComponent storageChestInventory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachToPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null && player.GetComponent<PlayerStarterKit>() == null)
            {
                player.AddComponent<PlayerStarterKit>();
            }
        }

        private void OnEnable()
        {
            SubscribeTimeEvents();
        }

        private void Start()
        {
            SubscribeTimeEvents();
            EnsureStarterKit();
        }

        private void OnDisable()
        {
            UnsubscribeTimeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeTimeEvents();
        }

        private void SubscribeTimeEvents()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                timeService.OnDayChanged -= HandleDayChanged;
                timeService.OnDayChanged += HandleDayChanged;
            }
            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                FeaturesTime.DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
                FeaturesTime.DayNightTimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
        }

        private void UnsubscribeTimeEvents()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                timeService.OnDayChanged -= HandleDayChanged;
            }
            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                FeaturesTime.DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
        }

        private void HandleDayChanged(int newDay)
        {
            if (newDay >= 1)
            {
                EnsureStarterKit(newDay);
            }
        }

        public void SetTargetStorageChest(InventoryComponent chestInv)
        {
            storageChestInventory = chestInv;
        }

        public InventoryComponent FindTargetStorageChest()
        {
            if (storageChestInventory != null) return storageChestInventory;

            var storageInteractables = UnityEngine.Object.FindObjectsByType<FeaturesInteraction.StorageInteractable>(
                UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);

            if (storageInteractables != null)
            {
                foreach (var storage in storageInteractables)
                {
                    if (storage != null && storage.name.Contains("StorageChest"))
                    {
                        var inv = storage.GetComponent<InventoryComponent>();
                        if (inv != null)
                        {
                            storageChestInventory = inv;
                            return storageChestInventory;
                        }
                    }
                }

                if (storageInteractables.Length > 0 && storageInteractables[0] != null)
                {
                    var inv = storageInteractables[0].GetComponent<InventoryComponent>();
                    if (inv != null)
                    {
                        storageChestInventory = inv;
                        return storageChestInventory;
                    }
                }
            }

            var chestGo = UnityEngine.GameObject.Find("StorageChest");
            if (chestGo != null)
            {
                var inv = chestGo.GetComponent<InventoryComponent>();
                if (inv != null)
                {
                    storageChestInventory = inv;
                    return storageChestInventory;
                }
            }

            return null;
        }

        public void EnsureStarterKit()
        {
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

            EnsureStarterKit(currentDay);
        }

        public void EnsureStarterKit(int currentDay)
        {
            var inv = GetComponent<InventoryComponent>();
            if (inv == null) return;

            ResolveItemReferences();

            if (inv.maxCapacity < 16) inv.maxCapacity = 16;
            inv.EnsureSlotsAllocated();

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
                inv.selectedHotbarIndex = 0;
                var equip = GetComponent<PlayerEquipment>();
                if (equip != null)
                {
                    equip.DestroyCurrentWeapon();
                    equip.UpdateEquipmentVisual(0);
                }
                inv.OnInventoryChanged?.Invoke();
                return;
            }

            // On Day 1+: Player wakes up with empty hands/pockets.
            // Starter tools are stored in the Storage Chest per design to be retrieved via Side Quest.
            var targetChest = FindTargetStorageChest();
            if (targetChest != null)
            {
                PopulateStorageChest(targetChest);
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

            // Ensure water tool states
            if (PlantWaterer.Instance != null && PlantWaterer.Instance.CurrentWater <= 0f)
            {
                PlantWaterer.Instance.RefillWater(-1f);
            }
            if (PlayerWaterBottle.Instance != null && PlayerWaterBottle.Instance.CurrentWater <= 0f)
            {
                PlayerWaterBottle.Instance.RefillWater(-1f);
            }

            // Ensure player hotbar slot 0 is selected and weapon visual is cleared (player holds nothing upon waking)
            inv.selectedHotbarIndex = 0;
            var playerEquip = GetComponent<PlayerEquipment>();
            if (playerEquip != null)
            {
                playerEquip.DestroyCurrentWeapon();
                playerEquip.UpdateEquipmentVisual(0);
            }

            inv.OnInventoryChanged?.Invoke();
        }

        public void PopulateStorageChest(InventoryComponent chestInv)
        {
            if (chestInv == null) return;
            ResolveItemReferences();

            if (chestInv.maxCapacity < 16) chestInv.maxCapacity = 16;
            chestInv.EnsureSlotsAllocated();

            var playerInv = GetComponent<InventoryComponent>();

            // Ensure starter items in chest only if neither the chest nor the player currently possesses them
            if (hoeItem != null && chestInv.CountItem(hoeItem) == 0 && (playerInv == null || playerInv.CountItem(hoeItem) == 0))
            {
                chestInv.AddItem(hoeItem, 1);
            }
            if (plantWatererItem != null && chestInv.CountItem(plantWatererItem) == 0 && (playerInv == null || playerInv.CountItem(plantWatererItem) == 0))
            {
                chestInv.AddItem(plantWatererItem, 1);
            }
            if (waterBottleItem != null && chestInv.CountItem(waterBottleItem) == 0 && (playerInv == null || playerInv.CountItem(waterBottleItem) == 0))
            {
                chestInv.AddItem(waterBottleItem, 1);
            }
            if (sweetPotatoSeedItem != null && chestInv.CountItem(sweetPotatoSeedItem) == 0 && (playerInv == null || playerInv.CountItem(sweetPotatoSeedItem) == 0))
            {
                chestInv.AddItem(sweetPotatoSeedItem, starterSeedCount);
            }

            chestInv.OnInventoryChanged?.Invoke();
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
