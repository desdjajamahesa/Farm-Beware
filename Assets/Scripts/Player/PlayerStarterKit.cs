using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesFarming;
using FeaturesKitchen;
using FeaturesEconomy;
using FeaturesWorkbench;

namespace Player
{
    /// <summary>
    /// Guarantees that on Day 1+ the Storage Chest contains the official starter kit:
    /// - Slot 0: Hoe (Tool_Hoe)
    /// - Slot 1: Plant Waterer (Tool_PlantWaterer 100L)
    /// - Slot 2: Bottle of Water (Food_BottleOfWater 4 charges)
    /// - Slot 3: 6x Sweet Potato Seeds (Seed_SweetPotato)
    /// - Slot 4: Homestead Slingshot (tool_weapon_corn_repeater)
    /// - Slots 5..9: Sweet Potato Combat Armor Set (Burrow Hood, Swift Root Armor, Fang Grips, Underground Striders, Sweet Potato Heart)
    /// </summary>
    public class PlayerStarterKit : MonoBehaviour
    {
        [Header("Starter Kit Items (Auto-loaded if null)")]
        [SerializeField] private ItemData hoeItem;
        [SerializeField] private ItemData plantWatererItem;
        [SerializeField] private ItemData waterBottleItem;
        [SerializeField] private ItemData sweetPotatoSeedItem;
        [SerializeField] private int starterSeedCount = 6;

        [Header("Day 1 Combat & Armor Starter Pack (Auto-loaded if null)")]
        [SerializeField] private ItemData slingshotItem;
        [SerializeField] private ItemData burrowHoodItem;
        [SerializeField] private ItemData swiftRootArmorItem;
        [SerializeField] private ItemData fangGripsItem;
        [SerializeField] private ItemData undergroundStridersItem;
        [SerializeField] private ItemData sweetPotatoHeartItem;

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
            // Starter tools, Homestead Slingshot, and Sweet Potato Armor set are stored in the Storage Chest.
            var targetChest = FindTargetStorageChest();
            if (targetChest != null)
            {
                PopulateStorageChest(targetChest);
            }
            PopulateRefrigerator();

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
            var armorState = GetComponent<PlayerArmorUpgradeState>() ?? PlayerArmorUpgradeState.Instance;

            // 1. Core Starter Farming Pack (Slots 0..3)
            TryAddStarterItemToChest(chestInv, playerInv, armorState, hoeItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, plantWatererItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, waterBottleItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, sweetPotatoSeedItem, starterSeedCount);

            // 2. Homestead Slingshot & Sweet Potato Combat Armor Set (Slots 4..9)
            TryAddStarterItemToChest(chestInv, playerInv, armorState, slingshotItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, burrowHoodItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, swiftRootArmorItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, fangGripsItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, undergroundStridersItem, 1);
            TryAddStarterItemToChest(chestInv, playerInv, armorState, sweetPotatoHeartItem, 1);

            chestInv.OnInventoryChanged?.Invoke();
        }

        private static void TryAddStarterItemToChest(
            InventoryComponent chestInv,
            InventoryComponent playerInv,
            PlayerArmorUpgradeState armorState,
            ItemData item,
            int quantity)
        {
            if (item == null || chestInv == null || quantity <= 0) return;

            if (chestInv.CountItem(item) > 0) return;
            if (playerInv != null && playerInv.CountItem(item) > 0) return;
            if (IsItemCurrentlyEquipped(armorState, item)) return;

            chestInv.AddItem(item, quantity);
        }

        private static bool IsItemCurrentlyEquipped(PlayerArmorUpgradeState armorState, ItemData item)
        {
            if (armorState == null || item == null) return false;
            foreach (EquipmentSlotType st in System.Enum.GetValues(typeof(EquipmentSlotType)))
            {
                var eq = armorState.GetEquippedItem(st);
                if (eq != null && (eq == item || string.Equals(eq.itemId, item.itemId, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        public void PopulateRefrigerator(InventoryComponent fridgeInv = null)
        {
            if (fridgeInv == null)
            {
                var fridge = UnityEngine.Object.FindFirstObjectByType<RefrigeratorInteractable>(
                    UnityEngine.FindObjectsInactive.Include);
                if (fridge != null)
                {
                    fridgeInv = fridge.GetComponent<InventoryComponent>();
                }
            }

            if (fridgeInv == null) return;

            var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db == null) return;

            var taroItem = db.GetItem("crop_taro");
            var coconutItem = db.GetItem("mat_coconut");
            var playerInv = GetComponent<InventoryComponent>();

            if (taroItem != null && fridgeInv.CountItem(taroItem) == 0 && (playerInv == null || playerInv.CountItem(taroItem) == 0))
            {
                fridgeInv.AddItem(taroItem, 1);
            }
            if (coconutItem != null && fridgeInv.CountItem(coconutItem) == 0 && (playerInv == null || playerInv.CountItem(coconutItem) == 0))
            {
                fridgeInv.AddItem(coconutItem, 1);
            }

            fridgeInv.OnInventoryChanged?.Invoke();
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

                if (slingshotItem == null) slingshotItem = db.GetItem("tool_weapon_corn_repeater");
                if (burrowHoodItem == null) burrowHoodItem = db.GetItem("item_burrow_hood");
                if (swiftRootArmorItem == null) swiftRootArmorItem = db.GetItem("item_swift_root_armor");
                if (fangGripsItem == null) fangGripsItem = db.GetItem("item_fang_grips");
                if (undergroundStridersItem == null) undergroundStridersItem = db.GetItem("item_underground_striders");
                if (sweetPotatoHeartItem == null) sweetPotatoHeartItem = db.GetItem("item_sweet_potato_heart");
            }

#if UNITY_EDITOR
            if (slingshotItem == null)
                slingshotItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Equipment/Tool_CornRepeater.asset");
            if (burrowHoodItem == null)
                burrowHoodItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Equipment/item_burrow_hood.asset");
            if (swiftRootArmorItem == null)
                swiftRootArmorItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Equipment/item_swift_root_armor.asset");
            if (fangGripsItem == null)
                fangGripsItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Equipment/item_fang_grips.asset");
            if (undergroundStridersItem == null)
                undergroundStridersItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Equipment/item_underground_striders.asset");
            if (sweetPotatoHeartItem == null)
                sweetPotatoHeartItem = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Equipment/item_sweet_potato_heart.asset");
#endif
        }
    }
}
