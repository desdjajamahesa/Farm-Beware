using System;
using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesEconomy
{
    /// <summary>
    /// Central manager for daily market rates and morning financial summary.
    /// Manages randomized daily per-unit clean crop prices balanced around 1-3 drops/plot (avg 2.0):
    /// Sweet Potato: 375-500G/unit (avg 750-1000G/plot), Taro & Corn: 700-900G/unit (avg 1400-1800G/plot),
    /// and logs operational metrics (monsters slain, crops harvested/sold, gold earned) for the Daily Morning Report.
    /// </summary>
    public class DailyEconomyManager : MonoBehaviour, IDailyEconomyService
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

        [Header("Current Daily Market Prices (Gold per Clean Unit)")]
        [SerializeField] private int sweetPotatoPrice = 425;
        [SerializeField] private int taroPrice = 800;
        [SerializeField] private int cornPrice = 800;

        public int SweetPotatoPrice => sweetPotatoPrice;
        public int TaroPrice => taroPrice;
        public int CornPrice => cornPrice;

        public void RestoreEconomyState(int sweetPotato, int taro, int corn, int cropsHarvested, int cropsSold, int goldTrading, int monstersSlain, int goldCombat)
        {
            if (sweetPotato > 0) sweetPotatoPrice = sweetPotato;
            if (taro > 0) taroPrice = taro;
            if (corn > 0) cornPrice = corn;

            dailyCropsHarvested = cropsHarvested;
            dailyCropsSold = cropsSold;
            dailyGoldEarnedTrading = goldTrading;
            dailyMonstersSlain = monstersSlain;
            dailyGoldEarnedCombat = goldCombat;

            Debug.Log($"[DailyEconomyManager] Economy restored: Sweet Potato={sweetPotatoPrice}G, Taro={taroPrice}G, Corn={cornPrice}G");
            OnMarketPricesChanged?.Invoke();
        }

        /// <summary>
        /// Resets all economy states, daily statistics, and rolls fresh market prices for New Game.
        /// </summary>
        public void ResetEconomyState()
        {
            dailyCropsHarvested = 0;
            dailyCropsSold = 0;
            dailyGoldEarnedTrading = 0;
            dailyMonstersSlain = 0;
            dailyGoldEarnedCombat = 0;

            lastDayMonstersSlain = 0;
            lastDayGoldEarnedCombat = 0;
            lastDayCropsHarvested = 0;
            lastDayCropsSold = 0;
            lastDayGoldEarnedTrading = 0;

            RollDailyMarketPrices();
            Debug.Log("[DailyEconomyManager] Economy state fully reset for New Game.");
        }

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

            ServiceLocator.Register<IDailyEconomyService>(this);
            RollDailyMarketPrices();
        }

        private bool isSubscribed = false;

        private void OnEnable()
        {
            TrySubscribeTimeService();
        }

        private void Start()
        {
            if (!isSubscribed)
            {
                TrySubscribeTimeService();
            }
        }

        private void Update()
        {
            if (!isSubscribed)
            {
                TrySubscribeTimeService();
            }
        }

        private void TrySubscribeTimeService()
        {
            if (isSubscribed) return;
            var timeService = ServiceLocator.Resolve<ITimeService>();
            if (timeService != null)
            {
                timeService.OnDayChanged += HandleDayChanged;
                isSubscribed = true;
            }
        }

        private void OnDisable()
        {
            if (isSubscribed)
            {
                var timeService = ServiceLocator.Resolve<ITimeService>();
                if (timeService != null)
                {
                    timeService.OnDayChanged -= HandleDayChanged;
                }
                isSubscribed = false;
            }
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

        public void RecordMonsterSlain()
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
        /// Rolls daily per-unit clean market prices balanced around 1-3 harvest drops per plot (avg 2.0):
        /// - Sweet Potato: 375 – 500 Gold / unit (~750 – 1,000 G per plot avg)
        /// - Taro: 700 – 900 Gold / unit (~1,400 – 1,800 G per plot avg)
        /// - Corn: 700 – 900 Gold / unit (~1,400 – 1,800 G per plot avg)
        /// </summary>
        public void RollDailyMarketPrices()
        {
            sweetPotatoPrice = UnityEngine.Random.Range(375, 501);
            taroPrice = UnityEngine.Random.Range(700, 901);
            cornPrice = UnityEngine.Random.Range(700, 901);

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

        void IDailyEconomyService.RecordCropHarvested(object item, int count)
        {
            RecordCropHarvested(item as ItemData, count);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IDailyEconomyService>();
        }
    }
}
