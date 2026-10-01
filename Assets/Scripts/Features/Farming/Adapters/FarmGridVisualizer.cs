using System;
using System.Collections.Generic;
using UnityEngine;
using FeaturesFarming.Core;
using FeaturesFarming.Data;

namespace FeaturesFarming.Adapters
{
    /// <summary>
    /// Purely stateless presentation adapter for the farming grid.
    /// Subscribes exclusively to FarmGrid.OnTileUpdated events to mutate mesh renderer materials
    /// and crop visual stages. Holds zero backend simulation state.
    /// </summary>
    [DisallowMultipleComponent]
    public class FarmGridVisualizer : MonoBehaviour
    {
        [Header("Backend Manager Reference")]
        [SerializeField] private FarmGridManager gridManager;

        [Header("Soil Materials")]
        [Tooltip("Material applied to raw, un-tilled soil (Empty).")]
        [SerializeField] private Material matRaw;

        [Tooltip("Material applied to dry tilled soil (Tilled / PlantedDry).")]
        [SerializeField] private Material matTilled;

        [Tooltip("Material applied to irrigated/watered soil (PlantedWatered / Watered Tilled).")]
        [SerializeField] private Material matWatered;

        [Header("Plot Instantiation (Optional)")]
        [Tooltip("Prefab spawned when a new grid coordinate is registered without an existing scene plot.")]
        [SerializeField] private GameObject defaultPlotPrefab;

        [Tooltip("Parent transform for dynamically spawned plot objects.")]
        [SerializeField] private Transform plotsParent;

        [Header("Fallback Crop Stage Prefabs")]
        [Tooltip("Fallback sprout visual if CropData does not specify a stagePrefab.")]
        [SerializeField] private GameObject fallbackSproutPrefab;

        [Tooltip("Fallback growing visual if CropData does not specify a stagePrefab.")]
        [SerializeField] private GameObject fallbackGrowingPrefab;

        [Tooltip("Fallback mature sweet potato visual.")]
        [SerializeField] private GameObject fallbackMatureSweetPotatoPrefab;

        [Tooltip("Fallback mature taro visual.")]
        [SerializeField] private GameObject fallbackMatureTaroPrefab;

        [Header("Juice & Feedback")]
        [SerializeField] private float matureBounceSpeed = 3f;
        [SerializeField] private float matureBounceHeight = 0.05f;

        // View-only dictionary mapping coordinates to scene presentation components (zero state held)
        private readonly Dictionary<Vector2Int, FarmlandTilePresenter> _tilePresenters = new Dictionary<Vector2Int, FarmlandTilePresenter>();

        public Material MatRaw => matRaw;
        public Material MatTilled => matTilled;
        public Material MatWatered => matWatered;

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager = FarmGridManager.Instance ?? FindFirstObjectByType<FarmGridManager>();
            }

            ResolveDefaultMaterials();
            DiscoverScenePlots();
        }

        private void OnEnable()
        {
            SubscribeToGrid();
        }

        private void OnDisable()
        {
            UnsubscribeFromGrid();
        }

        private void Start()
        {
            if (gridManager == null)
            {
                gridManager = FarmGridManager.Instance ?? FindFirstObjectByType<FarmGridManager>();
            }

            SubscribeToGrid();
            RefreshAllVisuals();
        }

        private void SubscribeToGrid()
        {
            if (gridManager != null && gridManager.Grid != null)
            {
                gridManager.Grid.OnTileUpdated -= HandleTileUpdated;
                gridManager.Grid.OnTileUpdated += HandleTileUpdated;
            }
        }

        private void UnsubscribeFromGrid()
        {
            if (gridManager != null && gridManager.Grid != null)
            {
                gridManager.Grid.OnTileUpdated -= HandleTileUpdated;
            }
        }

        /// <summary>
        /// Automatically registers any existing farmland plots found in the scene under this visualizer.
        /// </summary>
        public void DiscoverScenePlots()
        {
            if (gridManager == null) return;

            FarmlandTilePresenter[] existingPresenters = FindObjectsByType<FarmlandTilePresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var presenter in existingPresenters)
            {
                if (presenter != null)
                {
                    Vector2Int coord = gridManager.WorldToGrid(presenter.transform.position);
                    _tilePresenters[coord] = presenter;
                    presenter.Initialize(this, matureBounceSpeed, matureBounceHeight);

                    // Ensure pure backend has registered this coordinate
                    gridManager.Grid.RegisterTile(coord, SoilState.Empty);
                }
            }
        }

        /// <summary>
        /// Synchronizes all registered backend tiles with current visuals.
        /// </summary>
        public void RefreshAllVisuals()
        {
            if (gridManager == null || gridManager.Grid == null) return;

            foreach (var kvp in gridManager.Grid.Tiles)
            {
                HandleTileUpdated(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Event listener triggered strictly by FarmGrid.OnTileUpdated.
        /// Statelessly inspects the SoilTile and applies materials and crop stages.
        /// </summary>
        private void HandleTileUpdated(Vector2Int coord, SoilTile tile)
        {
            if (tile == null) return;

            FarmlandTilePresenter presenter = GetOrCreatePresenter(coord);
            if (presenter == null) return;

            CropData cropData = gridManager != null ? gridManager.GetCropData(tile.PlantedCropId) : null;
            presenter.ApplyVisuals(tile, cropData);
        }

        private FarmlandTilePresenter GetOrCreatePresenter(Vector2Int coord)
        {
            if (_tilePresenters.TryGetValue(coord, out var presenter) && presenter != null)
            {
                return presenter;
            }

            if (gridManager == null) return null;

            Vector3 worldPos = gridManager.GridToWorld(coord);
            GameObject plotObj;

            if (defaultPlotPrefab != null)
            {
                Transform parent = plotsParent != null ? plotsParent : transform;
                plotObj = Instantiate(defaultPlotPrefab, worldPos, Quaternion.identity, parent);
            }
            else
            {
                // Fallback: instantiate a simple quad/box soil plot
                plotObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plotObj.name = $"FarmlandPlot_({coord.x},{coord.y})";
                plotObj.transform.position = worldPos;
                plotObj.transform.localScale = new Vector3(1.4f, 0.1f, 1.4f);
                Transform parent = plotsParent != null ? plotsParent : transform;
                plotObj.transform.SetParent(parent, true);
            }

            presenter = plotObj.GetComponent<FarmlandTilePresenter>();
            if (presenter == null)
            {
                presenter = plotObj.AddComponent<FarmlandTilePresenter>();
            }

            presenter.Initialize(this, matureBounceSpeed, matureBounceHeight);
            _tilePresenters[coord] = presenter;
            return presenter;
        }

        public GameObject GetFallbackCropPrefab(string cropId, int stageIndex, bool isMature)
        {
            string lowerId = (cropId ?? "").ToLower();

            if (isMature)
            {
                if (lowerId.Contains("taro"))
                    return fallbackMatureTaroPrefab != null ? fallbackMatureTaroPrefab : fallbackMatureSweetPotatoPrefab;
                return fallbackMatureSweetPotatoPrefab;
            }

            if (stageIndex <= 0)
                return fallbackSproutPrefab;

            return fallbackGrowingPrefab != null ? fallbackGrowingPrefab : fallbackSproutPrefab;
        }

        private void ResolveDefaultMaterials()
        {
            // Auto-load project materials if unassigned
            if (matRaw == null)
            {
                matRaw = Resources.Load<Material>("Materials/Farming/Mat_Soil_Untilled");
            }
            if (matTilled == null)
            {
                matTilled = Resources.Load<Material>("Materials/Farming/Mat_Soil_Tilled_Dry");
            }
            if (matWatered == null)
            {
                matWatered = Resources.Load<Material>("Materials/Farming/Mat_Soil_Tilled_Wet");
            }
        }
    }

    /// <summary>
    /// Stateless visual presentation component attached to an individual plot GameObject.
    /// Receives SoilTile and CropData snapshot arguments on updates without retaining backend state.
    /// </summary>
    [DisallowMultipleComponent]
    public class FarmlandTilePresenter : MonoBehaviour
    {
        [Header("Soil Renderer")]
        [SerializeField] private MeshRenderer soilRenderer;

        [Header("Crop Container")]
        [SerializeField] private Transform cropContainer;

        [Header("Child Visual Nodes (For pre-baked prefabs)")]
        [SerializeField] private GameObject sproutVisual;
        [SerializeField] private GameObject growingVisual;
        [SerializeField] private GameObject matureSweetPotatoVisual;
        [SerializeField] private GameObject matureTaroVisual;

        private FarmGridVisualizer _visualizer;
        private GameObject _spawnedCropInstance;
        private int _currentRenderedStage = -1;
        private string _currentRenderedCropId;
        private bool _isBobbingActive = false;
        private Vector3 _initialCropLocalPos = Vector3.zero;
        private float _bounceSpeed = 3f;
        private float _bounceHeight = 0.05f;

        public void Initialize(FarmGridVisualizer visualizer, float bounceSpeed, float bounceHeight)
        {
            _visualizer = visualizer;
            _bounceSpeed = bounceSpeed;
            _bounceHeight = bounceHeight;

            if (soilRenderer == null)
                soilRenderer = GetComponentInChildren<MeshRenderer>();

            if (cropContainer == null)
            {
                Transform found = transform.Find("CropContainer");
                if (found != null)
                {
                    cropContainer = found;
                }
                else
                {
                    GameObject container = new GameObject("CropContainer");
                    container.transform.SetParent(transform, false);
                    container.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                    cropContainer = container.transform;
                }
            }

            _initialCropLocalPos = cropContainer != null ? cropContainer.localPosition : Vector3.zero;
            FindPrebakedVisualNodes();
        }

        private void FindPrebakedVisualNodes()
        {
            if (sproutVisual == null)
            {
                Transform t = transform.Find("SproutVisual") ?? transform.Find("CropContainer/SproutVisual");
                if (t != null) sproutVisual = t.gameObject;
            }
            if (growingVisual == null)
            {
                Transform t = transform.Find("GrowingVisual") ?? transform.Find("CropContainer/GrowingVisual");
                if (t != null) growingVisual = t.gameObject;
            }
            if (matureSweetPotatoVisual == null)
            {
                Transform t = transform.Find("MatureSweetPotatoVisual") ?? transform.Find("CropContainer/MatureSweetPotatoVisual");
                if (t != null) matureSweetPotatoVisual = t.gameObject;
            }
            if (matureTaroVisual == null)
            {
                Transform t = transform.Find("MatureTaroVisual") ?? transform.Find("CropContainer/MatureTaroVisual");
                if (t != null) matureTaroVisual = t.gameObject;
            }
        }

        private void Update()
        {
            if (_isBobbingActive && cropContainer != null && Application.isPlaying)
            {
                float offset = Mathf.Sin(Time.time * _bounceSpeed) * _bounceHeight;
                cropContainer.localPosition = _initialCropLocalPos + new Vector3(0f, offset, 0f);
            }
        }

        /// <summary>
        /// Applies the current snapshot from SoilTile and CropData to the scene visuals.
        /// Strictly stateless: swaps material and adjusts crop stage purely from passed parameters.
        /// </summary>
        public void ApplyVisuals(SoilTile tile, CropData cropData)
        {
            if (tile == null) return;

            // 1. Soil Material Swapping
            UpdateSoilMaterial(tile.State, tile.IsWatered);

            // 2. Crop Stage Representation
            UpdateCropVisuals(tile, cropData);
        }

        private void UpdateSoilMaterial(SoilState state, bool isWatered)
        {
            if (soilRenderer == null || _visualizer == null) return;

            Material targetMat = null;
            switch (state)
            {
                case SoilState.Empty:
                    targetMat = _visualizer.MatRaw;
                    break;
                case SoilState.Tilled:
                    targetMat = isWatered ? _visualizer.MatWatered : _visualizer.MatTilled;
                    break;
                case SoilState.PlantedDry:
                    targetMat = _visualizer.MatTilled;
                    break;
                case SoilState.PlantedWatered:
                    targetMat = _visualizer.MatWatered;
                    break;
                case SoilState.ReadyToHarvest:
                    targetMat = isWatered ? _visualizer.MatWatered : _visualizer.MatTilled;
                    break;
            }

            if (targetMat != null)
            {
                soilRenderer.sharedMaterial = targetMat;
            }
        }

        private void UpdateCropVisuals(SoilTile tile, CropData cropData)
        {
            bool isCropPresent = tile.State == SoilState.PlantedDry
                              || tile.State == SoilState.PlantedWatered
                              || tile.State == SoilState.ReadyToHarvest;

            bool isMature = tile.State == SoilState.ReadyToHarvest;
            _isBobbingActive = isMature;

            if (!isCropPresent || string.IsNullOrEmpty(tile.PlantedCropId))
            {
                HideAllCropVisuals();
                _currentRenderedStage = -1;
                _currentRenderedCropId = null;
                if (cropContainer != null) cropContainer.localPosition = _initialCropLocalPos;
                return;
            }

            int targetStage = 0;
            if (isMature)
            {
                targetStage = (cropData != null && cropData.growthStages != null && cropData.growthStages.Length > 0)
                    ? cropData.growthStages.Length - 1
                    : 2;
            }
            else if (cropData != null)
            {
                targetStage = cropData.GetStageIndex(tile.DaysGrown);
            }

            // Only re-instantiate or mutate if stage or crop ID changed
            if (_currentRenderedStage == targetStage && _currentRenderedCropId == tile.PlantedCropId)
            {
                return;
            }

            _currentRenderedStage = targetStage;
            _currentRenderedCropId = tile.PlantedCropId;

            // Attempt 1: Prefab configured in CropData.growthStages
            GameObject stagePrefab = null;
            if (cropData != null && cropData.growthStages != null && targetStage < cropData.growthStages.Length)
            {
                stagePrefab = cropData.growthStages[targetStage].stagePrefab;
            }

            // Attempt 2: Pre-baked child nodes in prefab
            if (stagePrefab == null && HasPrebakedNodes())
            {
                ApplyPrebakedStage(tile.PlantedCropId, targetStage, isMature);
                return;
            }

            // Attempt 3: Visualizer fallback prefabs
            if (stagePrefab == null && _visualizer != null)
            {
                stagePrefab = _visualizer.GetFallbackCropPrefab(tile.PlantedCropId, targetStage, isMature);
            }

            // Instantiate or swap dynamic stage prefab
            HideAllCropVisuals();
            if (stagePrefab != null && cropContainer != null)
            {
                if (_spawnedCropInstance != null)
                {
                    Destroy(_spawnedCropInstance);
                }
                _spawnedCropInstance = Instantiate(stagePrefab, cropContainer);
                _spawnedCropInstance.transform.localPosition = Vector3.zero;
                _spawnedCropInstance.transform.localRotation = Quaternion.identity;
                _spawnedCropInstance.SetActive(true);
            }
        }

        private bool HasPrebakedNodes()
        {
            return sproutVisual != null || growingVisual != null || matureSweetPotatoVisual != null || matureTaroVisual != null;
        }

        private void ApplyPrebakedStage(string cropId, int stageIndex, bool isMature)
        {
            HideAllCropVisuals();
            string lowerId = (cropId ?? "").ToLower();

            if (isMature)
            {
                if (lowerId.Contains("taro"))
                {
                    if (matureTaroVisual != null) matureTaroVisual.SetActive(true);
                    else if (matureSweetPotatoVisual != null) matureSweetPotatoVisual.SetActive(true);
                }
                else
                {
                    if (matureSweetPotatoVisual != null) matureSweetPotatoVisual.SetActive(true);
                    else if (matureTaroVisual != null) matureTaroVisual.SetActive(true);
                }
                return;
            }

            if (stageIndex <= 0)
            {
                if (sproutVisual != null) sproutVisual.SetActive(true);
            }
            else
            {
                if (growingVisual != null) growingVisual.SetActive(true);
                else if (sproutVisual != null) sproutVisual.SetActive(true);
            }
        }

        private void HideAllCropVisuals()
        {
            if (_spawnedCropInstance != null)
            {
                Destroy(_spawnedCropInstance);
                _spawnedCropInstance = null;
            }
            if (sproutVisual != null) sproutVisual.SetActive(false);
            if (growingVisual != null) growingVisual.SetActive(false);
            if (matureSweetPotatoVisual != null) matureSweetPotatoVisual.SetActive(false);
            if (matureTaroVisual != null) matureTaroVisual.SetActive(false);
        }
    }
}
