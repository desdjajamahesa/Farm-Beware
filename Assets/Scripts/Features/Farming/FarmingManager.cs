using System.Collections.Generic;
using UnityEngine;

namespace FeaturesFarming
{
    /// <summary>
    /// Manager sentral sistem pertanian.
    /// Mengontrol seluruh petak kebun dan sinkronisasi dengan siklus waktu (TimeManager).
    /// </summary>
    public class FarmingManager : MonoBehaviour
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

            RefreshFarmTiles();
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
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
    }
}
