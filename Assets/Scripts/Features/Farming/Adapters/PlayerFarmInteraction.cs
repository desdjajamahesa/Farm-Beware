using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FeaturesFarming.Core;
using FeaturesFarming.Data;
using FeaturesFarming;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;

namespace FeaturesFarming.Adapters
{
    /// <summary>
    /// Thin adapter handling player input and raycasting for farm tile interactions:
    /// - Hoe: Tills empty soil into arable plots.
    /// - Water Bottle: Irrigates tilled or planted crops (-10L water).
    /// - Seed: Plants seed from active hotbar into tilled soil.
    /// - Harvest: Collects mature crops with strict inventory capacity validation (maxStack = 20).
    /// 
    /// Enforces strict constraints:
    /// 1. Zero farming actions allowed during the Night phase (TimeManager).
    /// 2. Aborts harvest if InventoryComponent cannot accept items or has insufficient space.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerFarmInteraction : MonoBehaviour
    {
        public static PlayerFarmInteraction Instance { get; private set; }

        [Header("Manager References")]
        [SerializeField] private FarmGridManager gridManager;
        [SerializeField] private InventoryComponent playerInventory;
        private IPlayerContext playerControl;
        [SerializeField] private Camera playerCamera;

        [Header("Raycast & Range Settings")]
        [Tooltip("Maximum distance from player to farmland tile to allow interaction.")]
        [SerializeField] private float maxInteractionDistance = 3.5f;

        [Tooltip("LayerMask used to raycast for soil and farmland colliders.")]
        [SerializeField] private LayerMask raycastMask = ~0;

        [Header("Irrigation Settings")]
        [Tooltip("Litre of water consumed from PlayerWaterBottle per irrigation.")]
        [SerializeField] private float waterAmountPerIrrigation = 10f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            ResolveDependencies();
        }

        private void Start()
        {
            ResolveDependencies();
        }

        public FarmGridManager GridManager
        {
            get
            {
                if (gridManager == null) ResolveDependencies();
                return gridManager;
            }
            set => gridManager = value;
        }

        public InventoryComponent PlayerInventory
        {
            get
            {
                if (playerInventory == null) ResolveDependencies();
                return playerInventory;
            }
            set => playerInventory = value;
        }

        public void ResolveDependencies()
        {
            if (gridManager == null)
            {
                gridManager = FarmGridManager.Instance ?? GetComponent<FarmGridManager>() ?? FindFirstObjectByType<FarmGridManager>();
            }

            if (playerControl == null)
            {
                playerControl = GetComponent<IPlayerContext>() ?? ServiceLocator.Resolve<IPlayerContext>();
            }

            if (playerInventory == null)
            {
                if (playerControl != null)
                {
                    playerInventory = playerControl.GetPlayerComponent<InventoryComponent>();
                }
                if (playerInventory == null)
                {
                    playerInventory = GetComponent<InventoryComponent>() ?? FindFirstObjectByType<InventoryComponent>();
                }
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            var well = GameObject.Find("WaterWell");
            if (well != null && well.GetComponent<WaterWellInteractable>() == null)
            {
                well.AddComponent<WaterWellInteractable>();
            }
        }

        private void Update()
        {
            // Do not process interactions if player movement/interaction is locked (e.g. Wardrobe or Menu open)
            if (playerControl != null && playerControl.IsInputLocked) return;

            bool isLeftClick = WasLeftClickPressed();
            bool isInteractKey = WasInteractKeyPressed();
            bool isActionKey = WasActionKeyPressed();

            if (!isLeftClick && !isInteractKey && !isActionKey) return;

            if (!TryGetTargetCoordinate(out Vector2Int targetCoord, out Vector3 hitPoint))
            {
                return;
            }

            // Execute contextual interaction
            HandleFarmAction(targetCoord, hitPoint, isInteractKey);
        }

        private bool WasLeftClickPressed()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.wasPressedThisFrame;
            }
            return Input.GetMouseButtonDown(0);
        }

        private bool WasInteractKeyPressed()
        {
            if (Keyboard.current != null)
            {
                return Keyboard.current.eKey.wasPressedThisFrame;
            }
            return Input.GetKeyDown(KeyCode.E);
        }

        private bool WasActionKeyPressed()
        {
            if (Keyboard.current != null)
            {
                return Keyboard.current.fKey.wasPressedThisFrame;
            }
            return Input.GetKeyDown(KeyCode.F);
        }

        private Vector2 GetPointerScreenPosition()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            return Input.mousePosition;
        }

        /// <summary>
        /// Raycasts from the camera through the mouse pointer or falls back to player forward direction.
        /// </summary>
        public bool TryGetTargetCoordinate(out Vector2Int coord, out Vector3 worldHitPoint)
        {
            coord = Vector2Int.zero;
            worldHitPoint = Vector3.zero;

            if (gridManager == null) return false;

            Transform playerTrans = playerControl != null ? playerControl.Transform : transform;
            Vector3 playerPos = playerTrans.position;

            // 1. Raycast through camera under mouse pointer
            if (playerCamera != null)
            {
                Ray ray = playerCamera.ScreenPointToRay(GetPointerScreenPosition());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, raycastMask, QueryTriggerInteraction.Ignore))
                {
                    float distanceToPlayer = Vector3.Distance(playerPos, hit.point);
                    if (distanceToPlayer <= maxInteractionDistance)
                    {
                        worldHitPoint = hit.point;
                        coord = gridManager.WorldToGrid(hit.point);
                        return true;
                    }
                }
            }

            // 2. Fallback: Check ground point directly in front of the player
            Vector3 forwardPoint = playerPos + playerTrans.forward * 1.5f;
            Ray downRay = new Ray(forwardPoint + Vector3.up * 1.5f, Vector3.down);
            if (Physics.Raycast(downRay, out RaycastHit downHit, 4.0f, raycastMask, QueryTriggerInteraction.Ignore))
            {
                worldHitPoint = downHit.point;
                coord = gridManager.WorldToGrid(downHit.point);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Contextually executes the appropriate farm action on the target tile coordinate.
        /// </summary>
        public bool HandleFarmAction(Vector2Int coord, Vector3 worldPos, bool isInteractOnly = false)
        {
            // CONSTRAINT: Prevent all farming actions if Night phase
            if (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
            {
                Debug.LogWarning("[PlayerFarmInteraction] Farming is only allowed during the day!");
                ShowFeedbackText(worldPos, "Farming is only allowed during the day!", new Color(1f, 0.4f, 0.4f));
                return false;
            }

            if (GridManager == null || GridManager.Grid == null) return false;

            var grid = GridManager.Grid;
            SoilTile tile = grid.GetTile(coord);

            // 1. HARVEST ACTION (Priority when tile is ready to harvest)
            if (tile != null && tile.State == SoilState.ReadyToHarvest)
            {
                return ExecuteHarvest(coord, tile, worldPos);
            }

            // If user only pressed interact ('E') and tile is not ready to harvest, avoid auto-tilling
            if (isInteractOnly && tile != null && tile.State != SoilState.Empty)
            {
                if (tile.State == SoilState.PlantedWatered)
                {
                    ShowFeedbackText(worldPos, "Crop is growing...", new Color(0.4f, 0.8f, 1f));
                    return true;
                }
            }

            // 2. TILL SOIL (Empty soil)
            if (tile == null || tile.State == SoilState.Empty)
            {
                if (IsHoldingHoe())
                {
                    if (tile == null)
                    {
                        grid.RegisterTile(coord, SoilState.Empty);
                    }

                    if (grid.TryTill(coord))
                    {
                        ShowFeedbackText(worldPos, "Soil Tilled!", new Color(0.85f, 0.65f, 0.35f));
                        return true;
                    }
                }
                else
                {
                    ShowFeedbackText(worldPos, "Requires Hoe to till soil!", new Color(1f, 0.6f, 0.2f));
                    return false;
                }
            }

            // 3. PLANT SEED (Tilled soil)
            if (tile.State == SoilState.Tilled)
            {
                InventorySlot activeSlot = GetActiveHotbarSlot();
                if (activeSlot != null && !activeSlot.IsEmpty && IsSeedItem(activeSlot.item))
                {
                    return ExecutePlant(coord, activeSlot.item, worldPos);
                }

                // If holding plant waterer, attempt watering
                if (IsHoldingPlantWaterer())
                {
                    return ExecuteWater(coord, worldPos);
                }

                ShowFeedbackText(worldPos, "Select seed or plant waterer in hotbar!", new Color(1f, 0.7f, 0.3f));
                return false;
            }

            // 4. WATER CROP (PlantedDry)
            if (tile.State == SoilState.PlantedDry)
            {
                if (IsHoldingPlantWaterer())
                {
                    return ExecuteWater(coord, worldPos);
                }

                ShowFeedbackText(worldPos, "Requires Plant Waterer in hotbar to water crop!", new Color(1f, 0.6f, 0.2f));
                return false;
            }

            // 5. Already watered crop info
            if (tile.State == SoilState.PlantedWatered)
            {
                ShowFeedbackText(worldPos, "Already watered today!", new Color(0.35f, 0.75f, 1f));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to till the tile at the given coordinate.
        /// </summary>
        public bool ExecuteTill(Vector2Int coord, Vector3 worldPos)
        {
            if (!ValidateDaytime(worldPos)) return false;

            if (GridManager == null) return false;
            var grid = GridManager.Grid;
            if (grid == null) return false;

            if (!grid.HasTile(coord))
            {
                grid.RegisterTile(coord, SoilState.Empty);
            }

            if (!IsHoldingHoe())
            {
                ShowFeedbackText(worldPos, "Requires Hoe to till soil!", new Color(1f, 0.6f, 0.2f));
                return false;
            }

            if (grid.TryTill(coord))
            {
                ShowFeedbackText(worldPos, "Soil Tilled!", new Color(0.85f, 0.65f, 0.35f));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Irrigates the tile at the given coordinate, consuming water from PlayerWaterBottle.
        /// </summary>
        public bool ExecuteWater(Vector2Int coord, Vector3 worldPos)
        {
            if (!ValidateDaytime(worldPos)) return false;

            if (!IsHoldingPlantWaterer())
            {
                ShowFeedbackText(worldPos, "Requires Plant Waterer in hotbar!", new Color(1f, 0.6f, 0.2f));
                return false;
            }

            if (GridManager == null) return false;
            var grid = GridManager.Grid;
            if (grid == null) return false;

            var waterer = PlantWaterer.Instance;
            if (waterer == null || !waterer.HasWater(waterAmountPerIrrigation))
            {
                Debug.LogWarning("[PlayerFarmInteraction] Insufficient water in plant waterer! Refill at Garden Well.");
                ShowFeedbackText(worldPos, $"Need {waterAmountPerIrrigation:F0}L Water! Refill at Garden Well.", new Color(1f, 0.5f, 0.2f));
                return false;
            }

            if (waterer.ConsumeWater(waterAmountPerIrrigation))
            {
                if (grid.TryWater(coord))
                {
                    ShowFeedbackText(worldPos, $"💧 Watered (-{waterAmountPerIrrigation:F0}L Water | {Mathf.FloorToInt(waterer.CurrentWater)}/100L)", new Color(0.35f, 0.75f, 1f));
                    return true;
                }
                else
                {
                    // Refund water if tile could not accept watering
                    waterer.RefillWater(waterAmountPerIrrigation);
                }
            }

            return false;
        }

        /// <summary>
        /// Plants the specified seed item into tilled soil at the given coordinate.
        /// </summary>
        public bool ExecutePlant(Vector2Int coord, ItemData seedItem, Vector3 worldPos)
        {
            if (!ValidateDaytime(worldPos)) return false;

            if (GridManager == null) return false;
            var grid = GridManager.Grid;
            if (grid == null || seedItem == null) return false;

            string cropId = ResolveCropIdFromSeed(seedItem);
            if (string.IsNullOrEmpty(cropId))
            {
                Debug.LogWarning($"[PlayerFarmInteraction] Could not resolve Crop ID from seed '{seedItem.itemName}'!");
                return false;
            }

            if (grid.TryPlant(coord, cropId))
            {
                if (PlayerInventory != null)
                {
                    PlayerInventory.RemoveItem(seedItem, 1);
                }

                if (playerControl != null)
                {
                    playerControl.TriggerPlantAnimation();
                }

                ShowFeedbackText(worldPos, $"Planted {seedItem.itemName}!", new Color(0.4f, 0.9f, 0.5f));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finalizes harvest of a mature crop with strict inventory validation (maxStack = 20 invariant).
        /// If the inventory cannot accept the item or lacks space, harvest is aborted to prevent item loss.
        /// </summary>
        public bool ExecuteHarvest(Vector2Int coord, SoilTile tile, Vector3 worldPos)
        {
            if (!ValidateDaytime(worldPos)) return false;

            if (GridManager == null) return false;
            var grid = GridManager.Grid;
            if (grid == null || tile == null) return false;

            if (tile.State != SoilState.ReadyToHarvest)
            {
                return false;
            }

            CropData cropData = GridManager.GetCropData(tile.PlantedCropId);

            // Determine harvest yield item and count
            ItemData yieldItem = null;
            int yieldCount = 1;

            if (cropData != null)
            {
                yieldItem = cropData.dirtyYieldItem != null ? cropData.dirtyYieldItem : cropData.cleanYieldItem;
                yieldCount = UnityEngine.Random.Range(cropData.minYield, cropData.maxYield + 1);
            }

            if (yieldItem == null)
            {
                yieldItem = Resources.Load<ItemData>("Data/Items/Crops/Crop_SweetPotato_Dirty")
                         ?? Resources.Load<ItemData>("Data/Items/Crops/Crop_SweetPotato");
            }

            if (yieldCount < 1) yieldCount = 1;

            // Seed return bonus check
            bool willReturnSeed = false;
            ItemData returnSeedItem = null;
            if (cropData != null && cropData.returnedSeedItem != null && (UnityEngine.Random.value <= cropData.seedReturnChance))
            {
                willReturnSeed = true;
                returnSeedItem = cropData.returnedSeedItem;
            }

            // STRICT INVENTORY CHECK (maxStack = 20 invariant)
            if (PlayerInventory != null && yieldItem != null)
            {
                if (!PlayerInventory.CanAcceptItem(yieldItem))
                {
                    Debug.LogWarning($"[PlayerFarmInteraction] Inventory cannot accept '{yieldItem.itemName}'! Harvest aborted.");
                    ShowFeedbackText(worldPos, "Cannot accept item! Harvest Aborted", new Color(1f, 0.4f, 0.4f));
                    return false;
                }

                if (!PlayerInventory.HasSpaceFor(yieldItem, yieldCount))
                {
                    Debug.LogWarning($"[PlayerFarmInteraction] Inventory full for '{yieldItem.itemName}' (need {yieldCount}, maxStack = {yieldItem.maxStack})! Harvest aborted.");
                    ShowFeedbackText(worldPos, "Inventory Full! Harvest Aborted", new Color(1f, 0.4f, 0.4f));
                    return false; // ABORT HARVEST - STRICT INVARIANT ENFORCEMENT
                }

                // If seed return triggered, ensure space for seed too
                if (willReturnSeed && returnSeedItem != null)
                {
                    if (!PlayerInventory.CanAcceptItem(returnSeedItem) || !PlayerInventory.HasSpaceFor(returnSeedItem, 1))
                    {
                        // Omit bonus seed drop rather than losing crop if seed slot is full
                        willReturnSeed = false;
                    }
                }
            }

            // Backend POCO mutation (only after passing all inventory checks)
            if (!grid.TryHarvest(coord, out string harvestedCropId))
            {
                return false;
            }

            // Safely deposit harvest into player inventory
            if (PlayerInventory != null && yieldItem != null)
            {
                PlayerInventory.AddItem(yieldItem, yieldCount);
                if (willReturnSeed && returnSeedItem != null)
                {
                    PlayerInventory.AddItem(returnSeedItem, 1);
                }
            }

            // Economy logging
            if (yieldItem != null)
            {
                ServiceLocator.Resolve<IDailyEconomyService>()?.RecordCropHarvested(yieldItem, yieldCount);
            }

            // Player harvest animation
            if (playerControl != null)
            {
                playerControl.TriggerHarvestAnimation();
            }

            string feedback = (willReturnSeed && returnSeedItem != null)
                ? $"+{yieldCount} {yieldItem.itemName}, +1 {returnSeedItem.itemName}"
                : $"+{yieldCount} {yieldItem.itemName}";

            ShowFeedbackText(worldPos, feedback, new Color(0.25f, 0.90f, 0.35f));
            return true;
        }

        private bool ValidateDaytime(Vector3 worldPos)
        {
            if (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
            {
                Debug.LogWarning("[PlayerFarmInteraction] Farming is blocked during the Night!");
                ShowFeedbackText(worldPos, "Farming is only allowed during the day!", new Color(1f, 0.4f, 0.4f));
                return false;
            }
            return true;
        }

        private InventorySlot GetActiveHotbarSlot()
        {
            if (playerInventory == null) return null;
            int idx = playerInventory.selectedHotbarIndex;
            if (idx >= 0 && idx < playerInventory.slots.Count)
            {
                return playerInventory.slots[idx];
            }
            return null;
        }

        private bool IsHoldingHoe()
        {
            InventorySlot slot = GetActiveHotbarSlot();
            if (slot == null || slot.IsEmpty || slot.item == null) return false;

            if (slot.item is ToolItemData tool && tool.isHoe) return true;

            string id = (slot.item.itemId ?? "").ToLower();
            string name = (slot.item.itemName ?? "").ToLower();
            return id.Contains("hoe") || name.Contains("hoe") || name.Contains("cangkul");
        }

        private bool IsHoldingPlantWaterer()
        {
            InventorySlot slot = GetActiveHotbarSlot();
            if (slot == null || slot.IsEmpty || slot.item == null) return false;

            string id = (slot.item.itemId ?? "").ToLower();
            string name = (slot.item.itemName ?? slot.item.name ?? "").ToLower();
            return id == "tool_plant_waterer" || name.Contains("plant waterer") || name.Contains("waterer") || name.Contains("watering can");
        }

        private bool IsSeedItem(ItemData item)
        {
            if (item == null) return false;
            if (item is SeedItemData) return true;
            if (item.category == ItemCategory.Seed) return true;

            string id = (item.itemId ?? "").ToLower();
            string name = (item.itemName ?? "").ToLower();
            return id.Contains("seed") || name.Contains("seed") || name.Contains("benih") || name.Contains("bibit");
        }

        private string ResolveCropIdFromSeed(ItemData seed)
        {
            if (seed == null) return null;

            string lowerId = (seed.itemId ?? "").ToLower();
            string lowerName = (seed.itemName ?? "").ToLower();

            if (lowerId.Contains("sweet_potato") || lowerId.Contains("sweetpotato") || lowerName.Contains("sweet potato"))
            {
                return "Crop_SweetPotato";
            }

            if (lowerId.Contains("taro") || lowerName.Contains("taro"))
            {
                return "Crop_Taro";
            }

            if (lowerId.Contains("corn") || lowerName.Contains("corn"))
            {
                return "Crop_Corn";
            }

            // Direct check in manager registered crop lookup
            if (gridManager != null && gridManager.GetCropData(seed.itemId) != null)
            {
                return seed.itemId;
            }

            return "Crop_SweetPotato";
        }

        private void ShowFeedbackText(Vector3 worldPos, string message, Color color)
        {
            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(worldPos + Vector3.up * 1.2f, message, color);
        }
    }

    /// <summary>
    /// Proximity interactable bridge component attached to FarmlandPlot instances.
    /// Routes standard player interaction ('E' key via PlayerInteractor) to PlayerFarmInteraction.
    /// </summary>
    public class FarmlandTileInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private FarmlandTilePresenter presenter;

        private void Awake()
        {
            if (presenter == null)
            {
                presenter = GetComponent<FarmlandTilePresenter>();
            }
        }

        public void Interact(GameObject interactor)
        {
            if (FarmGridManager.Instance == null || PlayerFarmInteraction.Instance == null) return;

            Vector2Int coord = FarmGridManager.Instance.WorldToGrid(transform.position);
            PlayerFarmInteraction.Instance.HandleFarmAction(coord, transform.position, isInteractOnly: true);
        }

        public bool CanInteract(GameObject interactor)
        {
            if (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
            {
                return false;
            }
            return true;
        }
    }
}
