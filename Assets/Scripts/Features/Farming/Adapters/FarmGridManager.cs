using System;
using System.Collections.Generic;
using UnityEngine;
using FeaturesFarming.Core;
using FeaturesFarming.Data;
using FeaturesTime;

namespace FeaturesFarming.Adapters
{
    /// <summary>
    /// Thin Unity adapter hosting the pure C# FarmGrid engine.
    /// Manages world space <-> grid coordinate conversions and subscribes to TimeManager events.
    /// Strictly maintains zero-polling and delegates state evaluation to the POCO engine.
    /// </summary>
    [DisallowMultipleComponent]
    public class FarmGridManager : MonoBehaviour
    {
        public static FarmGridManager Instance { get; private set; }

        [Header("Grid Spatial Settings")]
        [Tooltip("Origin position in world space representing grid coordinate (0, 0).")]
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        [Tooltip("Distance in world units between adjacent grid tiles.")]
        [Min(0.5f)]
        [SerializeField] private float tileSpacing = 1.5f;

        [Header("Crop Database")]
        [Tooltip("Registered CropData definitions for lifecycle progression lookups.")]
        [SerializeField] private List<CropData> registeredCrops = new List<CropData>();

        private readonly Dictionary<string, CropData> _cropLookup = new Dictionary<string, CropData>(StringComparer.OrdinalIgnoreCase);
        private FarmGrid _grid;
        private int _lastProcessedDay = -1;

        public FarmGrid Grid => _grid ??= new FarmGrid();
        public float TileSpacing => tileSpacing;
        public Vector3 GridOrigin => gridOrigin;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_grid == null)
            {
                _grid = new FarmGrid();
            }
            PopulateCropLookup();
        }

        public bool RegisterTile(Vector2Int coord, SoilState initialState = SoilState.Empty)
        {
            return Grid.RegisterTile(coord, initialState);
        }

        public bool HasTile(Vector2Int coord)
        {
            return Grid.HasTile(coord);
        }

        public SoilTile GetTile(Vector2Int coord)
        {
            return Grid.GetTile(coord);
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
        }

        public void RegisterCropData(CropData crop)
        {
            if (crop != null && !string.IsNullOrEmpty(crop.cropId))
            {
                _cropLookup[crop.cropId] = crop;
                if (!registeredCrops.Contains(crop))
                {
                    registeredCrops.Add(crop);
                }
            }
        }

        private void PopulateCropLookup()
        {
            _cropLookup.Clear();
            if (registeredCrops != null)
            {
                foreach (var crop in registeredCrops)
                {
                    if (crop != null && !string.IsNullOrEmpty(crop.cropId))
                    {
                        _cropLookup[crop.cropId] = crop;
                    }
                }
            }

            // Fallback: auto-load all CropData in Resources or project if list was unassigned
            if (_cropLookup.Count == 0)
            {
                var crops = Resources.LoadAll<CropData>("Crops");
                foreach (var c in crops)
                {
                    if (c != null && !string.IsNullOrEmpty(c.cropId))
                    {
                        _cropLookup[c.cropId] = c;
                    }
                }
            }
        }

        private void HandleDayChanged(int newDay)
        {
            if (_lastProcessedDay == newDay) return;
            _lastProcessedDay = newDay;

            AdvanceDay();
        }

        private void HandlePhaseChanged(TimeManager.DayPhase phase)
        {
            if (phase == TimeManager.DayPhase.Day && TimeManager.Instance != null)
            {
                int currentDay = TimeManager.Instance.currentDay;
                if (_lastProcessedDay != currentDay)
                {
                    _lastProcessedDay = currentDay;
                    AdvanceDay();
                }
            }
        }

        /// <summary>
        /// Explicitly triggers a daily lifecycle progression step on the pure FarmGrid.
        /// </summary>
        public void AdvanceDay()
        {
            if (_grid == null) return;
            _grid.AdvanceDay(GetCropData);
        }

        /// <summary>
        /// Retrieves the CropData ScriptableObject matching the given ID.
        /// </summary>
        public CropData GetCropData(string cropId)
        {
            if (string.IsNullOrEmpty(cropId)) return null;
            _cropLookup.TryGetValue(cropId, out CropData crop);
            return crop;
        }

        /// <summary>
        /// Converts a world-space position to the closest grid coordinate.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 local = worldPos - gridOrigin;
            int x = Mathf.RoundToInt(local.x / tileSpacing);
            int y = Mathf.RoundToInt(local.z / tileSpacing);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Converts a grid coordinate to its world-space center position.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridCoord)
        {
            return gridOrigin + new Vector3(gridCoord.x * tileSpacing, 0f, gridCoord.y * tileSpacing);
        }
    }
}
