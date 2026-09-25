using System;
using System.Collections.Generic;
using UnityEngine;
using FeaturesCombat;

namespace FeaturesEconomy
{
    /// <summary>
    /// Central manager for daily market rates and morning financial summary.
    /// Manages randomized daily crop prices within MVP ranges (Sweet Potato: 750-1000G, Taro: 1400-1800G, Corn: 1400-1800G),
    /// and logs operational metrics (monsters slain, crops harvested/sold, gold earned) for the Daily Morning Report.
    /// </summary>
    public class DailyEconomyManager : MonoBehaviour
    {
        public static DailyEconomyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DailyEconomyManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var go = new GameObject("DailyEconomyManager");
                        _instance = go.AddComponent<DailyEconomyManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        private static DailyEconomyManager _instance;

        [Header("Current Daily Market Prices (Gold)")]
        [SerializeField] private int sweetPotatoPrice = 850;
        [SerializeField] private int taroPrice = 1600;
        [SerializeField] private int cornPrice = 1600;

        [Header("Daily Activity Statistics (Reset on New Day)")]
        public int dailyCropsHarvested = 0;
        public int dailyCropsSold = 0;
        public int dailyGoldEarnedTrading = 0;
        public int dailyMonstersSlain = 0;
        public int dailyGoldEarnedCombat = 0;

        // Snapshot of the day just concluded (for morning report display)
        public int lastDayMonstersSlain { get; private set; }
        public int lastDayGoldEarnedCombat { get; private set; }
        public int lastDayCropsHarvested { get; private set; }
        public int lastDayCropsSold { get; private set; }
        public int lastDayGoldEarnedTrading { get; private set; }

        public event Action OnMarketPricesChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            RollDailyMarketPrices();
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            EnemyBase.OnAnyEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
            EnemyBase.OnAnyEnemyDied -= HandleEnemyDied;
        }

        private void HandleDayChanged(int newDay)
        {
            // Snapshot yesterday's stats before rolling new day
            lastDayMonstersSlain = dailyMonstersSlain;
            lastDayGoldEarnedCombat = dailyGoldEarnedCombat;
            lastDayCropsHarvested = dailyCropsHarvested;
            lastDayCropsSold = dailyCropsSold;
            lastDayGoldEarnedTrading = dailyGoldEarnedTrading;

            // Roll new market prices for the new day
            RollDailyMarketPrices();

            // Reset current daily accumulators
            dailyCropsHarvested = 0;
            dailyCropsSold = 0;
            dailyGoldEarnedTrading = 0;
            dailyMonstersSlain = 0;
            dailyGoldEarnedCombat = 0;
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            dailyMonstersSlain++;
        }

        /// <summary>
        /// Records combat gold directly gained from monster drops or wave bonuses.
        /// </summary>
        public void RecordCombatGold(int gold)
        {
            if (gold > 0)
            {
                dailyGoldEarnedCombat += gold;
            }
        }

        /// <summary>
        /// Rolls daily market prices within the official MVP Guideline ranges:
        /// - Sweet Potato: 750 – 1,000 Gold
        /// - Taro: 1,400 – 1,800 Gold
        /// - Corn: 1,400 – 1,800 Gold
        /// </summary>
        public void RollDailyMarketPrices()
        {
            sweetPotatoPrice = UnityEngine.Random.Range(750, 1001);
            taroPrice = UnityEngine.Random.Range(1400, 1801);
            cornPrice = UnityEngine.Random.Range(1400, 1801);

            Debug.Log($"[DailyEconomyManager] Market prices updated today: Sweet Potato={sweetPotatoPrice}G, Taro={taroPrice}G, Corn={cornPrice}G");
            OnMarketPricesChanged?.Invoke();
        }

        /// <summary>
        /// Resolves the current market sell price for a given item.
        /// Accounts for clean vs dirty variants (dirty variants sell for 50%).
        /// </summary>
        public int GetCropSellPrice(ItemData item)
        {
            if (item == null) return 0;

            string id = item.itemId != null ? item.itemId.ToLower() : "";
            string name = item.itemName != null ? item.itemName.ToLower() : "";

            bool isDirty = item.isDirty || id.Contains("dirty") || name.Contains("dirty");

            int basePrice;
            if (id.Contains("sweet_potato") || name.Contains("sweet potato") || name.Contains("sweetpotato"))
            {
                basePrice = sweetPotatoPrice;
            }
            else if (id.Contains("taro") || name.Contains("taro"))
            {
                basePrice = taroPrice;
            }
            else if (id.Contains("corn") || name.Contains("corn") || name.Contains("jagung"))
            {
                basePrice = cornPrice;
            }
            else
            {
                basePrice = item.sellPrice > 0 ? item.sellPrice : 50;
            }

            if (isDirty)
            {
                basePrice = Mathf.Max(1, Mathf.RoundToInt(basePrice * 0.5f));
            }

            return basePrice;
        }

        public void RecordCropHarvested(ItemData item, int count)
        {
            dailyCropsHarvested += count;
        }

        public void RecordCropSold(ItemData item, int count, int goldEarned)
        {
            dailyCropsSold += count;
            dailyGoldEarnedTrading += goldEarned;
        }
    }
}
