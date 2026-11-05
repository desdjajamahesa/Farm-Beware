using System;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesFarming
{
    /// <summary>
    /// Manages farmland plot capacity progression, land deeds, and locked plot gating.
    /// Default Day 1 campaign begins with 6 arable plots (FarmlandPlot_1..6).
    /// Additional plots are unlocked in pairs (+2 plots for 2,500 G) up to the 16-plot maximum.
    /// </summary>
    [DisallowMultipleComponent]
    public class FarmlandExpansionManager : MonoBehaviour
    {
        public static FarmlandExpansionManager Instance { get; set; }

        public const int InitialPlotCount = 6;
        public const int MaxPlotCount = 16;
        public const int PlotsPerExpansion = 2;
        public const int ExpansionCostGold = 2500;

        [Header("Farmland Plot Progression")]
        [Range(InitialPlotCount, MaxPlotCount)]
        [SerializeField] private int unlockedPlotCount = InitialPlotCount;

        public int UnlockedPlotCount => unlockedPlotCount;
        public bool IsFullyExpanded => unlockedPlotCount >= MaxPlotCount;

        public event Action<int> OnPlotsExpanded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = UnityEngine.Object.FindFirstObjectByType<FarmlandExpansionManager>(
                    UnityEngine.FindObjectsInactive.Include);
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    var go = new GameObject("FarmlandExpansionManager");
                    Instance = go.AddComponent<FarmlandExpansionManager>();
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Instance = this;
            RefreshAllPlots();
        }

        /// <summary>
        /// Parses plot index number from GameObject name (e.g. 'FarmlandPlot_10' -> 10).
        /// </summary>
        public static int ParsePlotIndex(string tileName)
        {
            if (string.IsNullOrEmpty(tileName)) return 1;

            int lastUnderscore = tileName.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore < tileName.Length - 1)
            {
                string suffix = tileName.Substring(lastUnderscore + 1);
                if (int.TryParse(suffix, out int idx))
                {
                    return idx;
                }
            }

            return 1;
        }

        /// <summary>
        /// Returns whether a plot index is within the currently unlocked capacity.
        /// </summary>
        public bool IsPlotUnlocked(int plotIndex)
        {
            if (plotIndex <= 0) return true;
            return plotIndex <= unlockedPlotCount;
        }

        public bool CanExpand(IWalletService wallet = null)
        {
            if (IsFullyExpanded) return false;
            if (wallet == null) wallet = ServiceLocator.Resolve<IWalletService>();
            return wallet != null && wallet.CanAfford(ExpansionCostGold);
        }

        /// <summary>
        /// Attempts to purchase the next 2-plot expansion deed.
        /// Deducts 2,500 Gold and activates the next pair of FarmlandTiles.
        /// </summary>
        public bool TryExpand(IWalletService wallet = null)
        {
            if (IsFullyExpanded)
            {
                Debug.LogWarning("[FarmlandExpansionManager] Farmland is already fully expanded to 16 plots!");
                return false;
            }

            if (wallet == null)
            {
                wallet = ServiceLocator.Resolve<IWalletService>();
            }

            if (wallet == null || !wallet.CanAfford(ExpansionCostGold))
            {
                Debug.LogWarning("[FarmlandExpansionManager] Insufficient gold to purchase Land Expansion Deed!");
                return false;
            }

            if (!wallet.SpendGold(ExpansionCostGold))
            {
                return false;
            }

            unlockedPlotCount = Mathf.Min(MaxPlotCount, unlockedPlotCount + PlotsPerExpansion);
            RefreshAllPlots();
            OnPlotsExpanded?.Invoke(unlockedPlotCount);

            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            var spawnPos = player != null ? player.transform.position + Vector3.up * 1.5f : Vector3.zero;
            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                spawnPos,
                $"🌾 Farmland Expanded! ({unlockedPlotCount}/16 Plots Active)",
                new Color(0.25f, 0.95f, 0.45f));

            Debug.Log($"[FarmlandExpansionManager] Successfully expanded farmland capacity to {unlockedPlotCount}/16 plots!");
            return true;
        }

        /// <summary>
        /// Overwrites unlocked plot count (e.g. from save data or test fixture).
        /// </summary>
        public void SetUnlockedPlotCount(int count)
        {
            unlockedPlotCount = Mathf.Clamp(count, InitialPlotCount, MaxPlotCount);
            RefreshAllPlots();
            OnPlotsExpanded?.Invoke(unlockedPlotCount);
        }

        /// <summary>
        /// Resynchronizes all FarmlandTile instances in the scene with the current unlocked count.
        /// </summary>
        public void RefreshAllPlots()
        {
            var tiles = UnityEngine.Object.FindObjectsByType<FarmlandTile>(
                UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);

            if (tiles != null)
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (tiles[i] != null)
                    {
                        tiles[i].RefreshLockState();
                    }
                }
            }
        }
    }
}
