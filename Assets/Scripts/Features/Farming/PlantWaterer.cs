using System;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesFarming
{
    /// <summary>
    /// Manages the player's dedicated agricultural Plant Waterer (Watering Can) system.
    /// Tracks current water volume (0-100L) used strictly for irrigating farmland crops.
    /// Can ONLY be refilled at the garden well.
    /// </summary>
    public class PlantWaterer : MonoBehaviour
    {
        public static PlantWaterer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlantWaterer>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var player = GameObject.FindWithTag("Player");
                        if (player != null)
                        {
                            _instance = player.AddComponent<PlantWaterer>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        private static PlantWaterer _instance;

        [Header("Plant Waterer Capacity (100L)")]
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
            ServiceLocator.Register<PlantWaterer>(this);
        }

        private void Start()
        {
            EnsureWatererInInventory();
            OnWaterChanged?.Invoke(currentWater, maxWater);
        }

        public void EnsureWatererInInventory()
        {
            var inv = GetComponent<InventoryComponent>() ?? ServiceLocator.Resolve<IPlayerContext>()?.GetPlayerComponent<InventoryComponent>();
            if (inv != null)
            {
                ItemData watererItem = null;
                var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
                if (db != null)
                {
                    watererItem = db.GetItem("tool_plant_waterer");
                }

                if (watererItem == null)
                {
                    var registry = Resources.Load<ItemRegistrySO>("ItemRegistrySO");
                    if (registry != null)
                    {
                        watererItem = registry.GetItem("tool_plant_waterer");
                    }
                }

                if (watererItem != null && inv.CountItem(watererItem) == 0)
                {
                    inv.AddItem(watererItem, 1);
                }
            }
        }

        /// <summary>
        /// Checks whether the plant waterer has at least the specified amount of water.
        /// </summary>
        public bool HasWater(float amount)
        {
            return currentWater >= amount;
        }

        /// <summary>
        /// Consumes water from the plant waterer. Returns true if successful, false if insufficient water.
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
        /// Refills water into the plant waterer. If amount <= 0, refills to full capacity (100L).
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
        /// Sets current water directly (used by Save/Load system).
        /// </summary>
        public void SetWater(float amount)
        {
            currentWater = Mathf.Clamp(amount, 0f, maxWater);
            EnsureWatererInInventory();
            OnWaterChanged?.Invoke(currentWater, maxWater);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Unregister<PlantWaterer>();
                _instance = null;
            }
        }
    }
}
