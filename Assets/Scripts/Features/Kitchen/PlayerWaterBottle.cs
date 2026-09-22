using System;
using UnityEngine;

namespace FeaturesKitchen
{
    /// <summary>
    /// Manages the player's refillable 100L water bottle system as specified in the MVP design guidelines.
    /// Tracks current water volume (0-100L) used for drinking, cooking recipes, and irrigating farmland tiles.
    /// </summary>
    public class PlayerWaterBottle : MonoBehaviour
    {
        public static PlayerWaterBottle Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerWaterBottle>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var player = GameObject.FindWithTag("Player");
                        if (player != null)
                        {
                            _instance = player.AddComponent<PlayerWaterBottle>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        private static PlayerWaterBottle _instance;

        [Header("Water Bottle Capacity")]
        [SerializeField] private float maxWater = 100f;
        [SerializeField] private float currentWater = 100f;

        public float MaxWater => maxWater;
        public float CurrentWater => currentWater;

        public event Action<float, float> OnWaterChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            OnWaterChanged?.Invoke(currentWater, maxWater);
            EnsureBottleInInventory();
        }

        private void EnsureBottleInInventory()
        {
            var inv = GetComponent<InventoryComponent>() ?? FindFirstObjectByType<PlayerControl>()?.GetComponent<InventoryComponent>();
            if (inv != null)
            {
                var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
                if (db != null)
                {
                    var bottleItem = db.GetItem("food_bottle_water");
                    if (bottleItem != null && inv.CountItem(bottleItem) == 0)
                    {
                        inv.AddItem(bottleItem, 1);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the bottle has at least the specified amount of water.
        /// </summary>
        public bool HasWater(float amount)
        {
            return currentWater >= amount;
        }

        /// <summary>
        /// Consumes water from the bottle. Returns true if successful, false if insufficient water.
        /// </summary>
        public bool ConsumeWater(float amount)
        {
            if (amount <= 0f) return true;

            if (currentWater >= amount)
            {
                currentWater -= amount;
                OnWaterChanged?.Invoke(currentWater, maxWater);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refills water into the bottle. If amount <= 0, refills to full capacity (100L).
        /// </summary>
        public void RefillWater(float amount = -1f)
        {
            if (amount <= 0f)
            {
                currentWater = maxWater;
            }
            else
            {
                currentWater = Mathf.Clamp(currentWater + amount, 0f, maxWater);
            }

            OnWaterChanged?.Invoke(currentWater, maxWater);
        }

        /// <summary>
        /// Sips water from the bottle to restore player hydration.
        /// </summary>
        public bool DrinkSip(float amount = 25f)
        {
            if (currentWater <= 0.01f)
            {
                return false;
            }

            float sipAmount = Mathf.Min(amount, currentWater);
            currentWater -= sipAmount;
            OnWaterChanged?.Invoke(currentWater, maxWater);

            var stats = GetComponent<PlayerStats>() ?? FindFirstObjectByType<PlayerStats>();
            if (stats != null)
            {
                stats.Drink(sipAmount);
            }

            return true;
        }
    }
}
