using System.Collections.Generic;
using UnityEngine;

namespace FeaturesFarming
{
    /// <summary>
    /// Manager sentral sistem pertanian.
    /// Mengontrol seluruh petak kebun dan sinkronisasi dengan siklus waktu (TimeManager).
    /// </summary>
    public class FarmingManager : MonoBehaviour, FarmBeware.Core.Runtime.IFarmingService
    {
        public static FarmingManager Instance { get; private set; }

        [Header("Daftar Petak Kebun")]
        [SerializeField] private List<FarmlandTile> farmTiles = new List<FarmlandTile>();
        public IReadOnlyList<FarmlandTile> FarmTiles => farmTiles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.IFarmingService>(this);

            RefreshFarmTiles();
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(TimeManager.DayPhase newPhase)
        {
            Debug.Log($"[FarmingManager] Phase changed to {newPhase}. Updating farm tiles...");
            foreach (var tile in farmTiles)
            {
                if (tile != null)
                {
                    tile.UpdateLabelText(null);
                }
            }
        }

        private void Start()
        {
            RefreshFarmTiles();
        }

        public void RefreshFarmTiles()
        {
            farmTiles.Clear();
            var found = FindObjectsByType<FarmlandTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            farmTiles.AddRange(found);
            Debug.Log($"[FarmingManager] Terdaftar {farmTiles.Count} petak kebun aktif.");
        }

        private void HandleDayChanged(int newDay)
        {
            Debug.Log($"[FarmingManager] Hari baru {newDay}! Memperbarui seluruh petak kebun...");
            foreach (var tile in farmTiles)
            {
                if (tile != null)
                {
                    tile.AdvanceDay();
                }
            }
        }

        public void RegisterTile(FarmlandTile tile)
        {
            if (tile != null && !farmTiles.Contains(tile))
            {
                farmTiles.Add(tile);
            }
        }

        public void UnregisterTile(FarmlandTile tile)
        {
            if (tile != null && farmTiles.Contains(tile))
            {
                farmTiles.Remove(tile);
            }
        }

        public bool HasCropsAboutToRipen(float thresholdSeconds, out string cropSummary)
        {
            cropSummary = string.Empty;
            int ripeningCount = 0;
            float minRemaining = float.MaxValue;
            string cropName = "Crops";

            foreach (var tile in farmTiles)
            {
                if (tile == null) continue;
                if (tile.CurrentState == TileState.ReadyToHarvest)
                {
                    ripeningCount++;
                    minRemaining = 0f;
                    if (tile.PlantedSeed != null) cropName = tile.PlantedSeed.itemName;
                }
                else if (tile.CurrentState == TileState.PlantedWatered)
                {
                    float rem = tile.GrowthDuration - tile.CurrentTimer;
                    if (rem <= thresholdSeconds && rem > 0f)
                    {
                        ripeningCount++;
                        if (rem < minRemaining)
                        {
                            minRemaining = rem;
                            if (tile.PlantedSeed != null) cropName = tile.PlantedSeed.itemName;
                        }
                    }
                }
            }

            if (ripeningCount > 0)
            {
                string timeStr = minRemaining <= 1f ? "Ready to Harvest!" : $"ripening in {Mathf.CeilToInt(minRemaining)}s";
                cropSummary = $"{ripeningCount}x {cropName} ({timeStr})";
                return true;
            }

            return false;
        }
    }
}
