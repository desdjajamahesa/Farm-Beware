using System;
using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FarmBeware.Logic.Quests;
using FeaturesFarming;
using FeaturesKitchen;

namespace FarmBeware.Features.Quest
{
    /// <summary>
    /// Thin MonoBehaviour adapter managing quest progression and game event listening.
    /// Bridges runtime Unity events (inventory changes, farming actions) to pure C# POCO models.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        private static QuestManager _instance;
        public static bool HasInstance => _instance != null;
        private static bool _isQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isQuitting = false;
        }

        public static QuestManager Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    QuestManager[] found = FindObjectsByType<QuestManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                    {
                        _instance = found[0];
                    }
                    else if (Application.isPlaying)
                    {
                        var go = new GameObject("QuestManager");
                        _instance = go.AddComponent<QuestManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private readonly QuestProgressModel _model = new QuestProgressModel();
        public QuestProgressModel Model => _model;

        public event Action<QuestProgressModel> OnQuestUpdated;

        private InventoryComponent _playerInventory;
        private bool _isInventorySubscribed = false;
        private QuestStage _lastAnnouncedStage = QuestStage.GatheringSupplies;
        private readonly List<string> _itemIdsCache = new List<string>(16);
        private FarmlandTile[] _cachedFarmlandTiles;
        private Transform _playerTransform;
        private float _firstStepsCompletedTimer = 0f;
        private const float TransitionToCookingQuestDelay = 3.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (_isQuitting) return;
            if (_instance == null)
            {
                var existing = FindFirstObjectByType<QuestManager>();
                if (existing == null && Application.isPlaying)
                {
                    var go = new GameObject("QuestManager");
                    go.AddComponent<QuestManager>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            _model.OnStateChanged += HandleModelStateChanged;
        }

        private void OnEnable()
        {
            FarmlandTile.OnCropPlanted += HandleCropPlanted;
            FarmlandTile.OnCropWatered += HandleCropWatered;
            InventoryComponent.OnItemConsumed += HandleItemConsumed;
            RefrigeratorInteractable.OnRefrigeratorOpened += HandleRefrigeratorOpened;
            KitchenStove.OnAnyCookingCompleted += HandleCookingCompleted;
            SubscribeTimeEvents();
            TryBindPlayerInventory();
        }

        private void OnDisable()
        {
            FarmlandTile.OnCropPlanted -= HandleCropPlanted;
            FarmlandTile.OnCropWatered -= HandleCropWatered;
            InventoryComponent.OnItemConsumed -= HandleItemConsumed;
            RefrigeratorInteractable.OnRefrigeratorOpened -= HandleRefrigeratorOpened;
            KitchenStove.OnAnyCookingCompleted -= HandleCookingCompleted;
            UnsubscribeTimeEvents();
            UnbindPlayerInventory();
        }

        private void SubscribeTimeEvents()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                timeService.OnDayChanged -= HandleDayChanged;
                timeService.OnDayChanged += HandleDayChanged;
            }
            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                FeaturesTime.DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
                FeaturesTime.DayNightTimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
        }

        private void UnsubscribeTimeEvents()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                timeService.OnDayChanged -= HandleDayChanged;
            }
            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                FeaturesTime.DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
        }

        private void HandleDayChanged(int day)
        {
            if (day >= 1 && _model.CurrentStage == QuestStage.GatheringSupplies)
            {
                NotifyPlayerOfStageProgress(QuestStage.GatheringSupplies);
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            _model.OnStateChanged -= HandleModelStateChanged;
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Start()
        {
            CacheFarmlandTiles();
            TryBindPlayerInventory();
            RefreshInventoryEvaluation();
            RefreshFarmingProgress();
        }

        private void Update()
        {
            if (!_isInventorySubscribed || _playerInventory == null)
            {
                TryBindPlayerInventory();
            }

            if (_model.CurrentStage == QuestStage.GatheringSupplies)
            {
                RefreshInventoryEvaluation();
            }
            else if (_model.CurrentStage == QuestStage.PlantingCrop || _model.CurrentStage == QuestStage.WateringCrop)
            {
                RefreshFarmingProgress();
            }
            else if (_model.CurrentStage == QuestStage.Completed)
            {
                _firstStepsCompletedTimer += Time.deltaTime;
                if (_firstStepsCompletedTimer >= TransitionToCookingQuestDelay)
                {
                    _model.AdvanceToCookingQuest();
                }
            }
            else if (_model.CurrentStage == QuestStage.EnterKitchen)
            {
                CheckPlayerInKitchen();
            }
            else if (_model.CurrentStage == QuestStage.RetrieveIngredients || _model.CurrentStage == QuestStage.CookStew)
            {
                RefreshInventoryEvaluation();
            }
        }

        private void CheckPlayerInKitchen()
        {
            if (_playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _playerTransform = player.transform;
                }
                else
                {
                    var playerContext = ServiceLocator.Resolve<IPlayerContext>();
                    if (playerContext != null && playerContext.Transform != null)
                    {
                        _playerTransform = playerContext.Transform;
                    }
                }
            }

            if (_playerTransform != null)
            {
                Vector3 pos = _playerTransform.position;
                // Kitchen room bounds: X: 9.0 to 20.5, Z: 4.5 to 13.5
                bool inKitchenArea = (pos.x >= 9.0f && pos.x <= 20.5f && pos.z >= 4.5f && pos.z <= 13.5f);
                if (inKitchenArea)
                {
                    _model.OnEnteredKitchen();
                }
                else if (KitchenStove.Instance != null && Vector3.Distance(pos, KitchenStove.Instance.transform.position) <= 5.5f)
                {
                    _model.OnEnteredKitchen();
                }
            }
        }

        public void CacheFarmlandTiles()
        {
            _cachedFarmlandTiles = UnityEngine.Object.FindObjectsByType<FarmlandTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public void TryBindPlayerInventory()
        {
            if (_playerInventory == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _playerInventory = player.GetComponent<InventoryComponent>();
                }
                else if (InventoryManagerUI.Instance != null && InventoryManagerUI.Instance.playerInventory != null)
                {
                    _playerInventory = InventoryManagerUI.Instance.playerInventory;
                }
                else
                {
                    var playerContext = ServiceLocator.Resolve<IPlayerContext>();
                    if (playerContext != null && playerContext.Transform != null)
                    {
                        _playerInventory = playerContext.Transform.GetComponent<InventoryComponent>();
                    }
                }
            }

            if (_playerInventory != null && !_isInventorySubscribed)
            {
                _playerInventory.OnInventoryChanged += HandleInventoryChanged;
                _isInventorySubscribed = true;
                RefreshInventoryEvaluation();
            }
        }

        private void UnbindPlayerInventory()
        {
            if (_playerInventory != null && _isInventorySubscribed)
            {
                _playerInventory.OnInventoryChanged -= HandleInventoryChanged;
                _isInventorySubscribed = false;
            }
        }

        private void HandleInventoryChanged()
        {
            RefreshInventoryEvaluation();
        }

        public void RefreshInventoryEvaluation()
        {
            if (_playerInventory == null)
            {
                TryBindPlayerInventory();
                if (_playerInventory == null) return;
            }

            _itemIdsCache.Clear();
            if (_playerInventory.slots != null)
            {
                for (int i = 0; i < _playerInventory.slots.Count; i++)
                {
                    var slot = _playerInventory.slots[i];
                    if (slot != null && !slot.IsEmpty && slot.item != null)
                    {
                        if (!string.IsNullOrEmpty(slot.item.itemId)) _itemIdsCache.Add(slot.item.itemId);
                        if (!string.IsNullOrEmpty(slot.item.name)) _itemIdsCache.Add(slot.item.name);
                        if (!string.IsNullOrEmpty(slot.item.itemName)) _itemIdsCache.Add(slot.item.itemName);
                    }
                }
            }

            _model.EvaluateInventory(_itemIdsCache);
        }

        public void RefreshFarmingProgress()
        {
            if (_model.CurrentStage == QuestStage.GatheringSupplies || _model.CurrentStage == QuestStage.Completed)
                return;

            if (_cachedFarmlandTiles == null || _cachedFarmlandTiles.Length == 0)
            {
                CacheFarmlandTiles();
                if (_cachedFarmlandTiles == null || _cachedFarmlandTiles.Length == 0) return;
            }

            int dryCount = 0;
            int wateredCount = 0;

            for (int i = 0; i < _cachedFarmlandTiles.Length; i++)
            {
                var tile = _cachedFarmlandTiles[i];
                if (tile == null) continue;

                if (tile.CurrentState == TileState.PlantedDry)
                {
                    dryCount++;
                }
                else if (tile.CurrentState == TileState.PlantedWatered || tile.CurrentState == TileState.ReadyToHarvest)
                {
                    wateredCount++;
                }
            }

            int totalPlanted = dryCount + wateredCount;
            if (totalPlanted > 0)
            {
                _model.SyncCropCounts(totalPlanted, wateredCount);
            }
        }

        private void HandleCropPlanted(SeedItemData seed)
        {
            if (seed != null && !string.IsNullOrEmpty(seed.itemId))
            {
                _model.OnCropPlanted(seed.itemId);
            }
            RefreshFarmingProgress();
        }

        private void HandleCropWatered(FarmlandTile tile)
        {
            if (tile != null)
            {
                string seedId = tile.PlantedSeed != null ? tile.PlantedSeed.itemId : null;
                _model.OnCropWatered(seedId);
            }
            RefreshFarmingProgress();
        }

        private void HandleRefrigeratorOpened()
        {
            _model.OnEnteredKitchen();
        }

        private void HandleCookingCompleted(KitchenRecipe recipe)
        {
            if (recipe != null)
            {
                string outId = recipe.output != null ? recipe.output.itemId : null;
                if (string.Equals(outId, QuestProgressModel.ItemIdTaroCoconutStew, StringComparison.OrdinalIgnoreCase) ||
                    (recipe.recipeName != null && recipe.recipeName.IndexOf("Taro Coconut Stew", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _model.OnStewCooked();
                }
            }
            RefreshInventoryEvaluation();
        }

        private void HandleItemConsumed(ItemData item)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId))
            {
                _model.OnDishConsumed(item.itemId);
            }
            RefreshInventoryEvaluation();
        }

        private void HandleModelStateChanged()
        {
            if (_model.CurrentStage != _lastAnnouncedStage)
            {
                _lastAnnouncedStage = _model.CurrentStage;
                NotifyPlayerOfStageProgress(_model.CurrentStage);
            }

            OnQuestUpdated?.Invoke(_model);
        }

        private void NotifyPlayerOfStageProgress(QuestStage stage)
        {
            string message = string.Empty;
            Color textColor = Color.white;

            switch (stage)
            {
                case QuestStage.GatheringSupplies:
                    message = "📦 Side Quest: Take your farming tools from the Storage Chest!";
                    textColor = new Color(0.40f, 0.85f, 1f);
                    break;

                case QuestStage.PlantingCrop:
                    message = "📋 Quest Updated: Till soil with Hoe & plant Sweet Potato!";
                    textColor = new Color(0.35f, 0.95f, 0.45f);
                    break;

                case QuestStage.WateringCrop:
                    message = "💧 Quest Updated: Water the planted Sweet Potato!";
                    textColor = new Color(0.35f, 0.75f, 1f);
                    break;

                case QuestStage.Completed:
                    message = "🎉 Side Quest Completed: First Steps!";
                    textColor = new Color(1f, 0.84f, 0.25f);
                    break;

                case QuestStage.EnterKitchen:
                    message = "🍳 Side Quest: Enter the Kitchen to prepare a meal!";
                    textColor = new Color(1f, 0.70f, 0.25f);
                    break;

                case QuestStage.RetrieveIngredients:
                    message = "🧊 Quest Updated: Take Taro & Coconut from the Refrigerator!";
                    textColor = new Color(0.35f, 0.85f, 1f);
                    break;

                case QuestStage.CookStew:
                    message = "🍲 Quest Updated: Cook Taro Coconut Stew Bowl at the Kitchen Stove!";
                    textColor = new Color(0.40f, 0.95f, 0.45f);
                    break;

                case QuestStage.ConsumeDish:
                    message = "😋 Dish Ready! Double-click Taro Coconut Stew Bowl in inventory to Eat & Drink!";
                    textColor = new Color(1f, 0.85f, 0.25f);
                    break;

                case QuestStage.CookingQuestCompleted:
                    message = "🎉 Side Quest Completed: Home Cooking! Fully nourished & buffed!";
                    textColor = new Color(1f, 0.84f, 0.25f);
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                var floatingText = ServiceLocator.Resolve<IFloatingTextService>();
                var player = ServiceLocator.Resolve<IPlayerContext>();
                if (floatingText != null && player != null && player.Transform != null)
                {
                    floatingText.SpawnText(player.Transform.position + Vector3.up * 2f, message, textColor);
                }
                Debug.Log($"[QuestManager] {message}");
            }
        }
    }
}
