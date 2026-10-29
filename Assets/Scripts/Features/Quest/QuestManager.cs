using System;
using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FarmBeware.Logic.Quests;
using FeaturesFarming;

namespace FarmBeware.Features.Quest
{
    /// <summary>
    /// Thin MonoBehaviour adapter managing quest progression and game event listening.
    /// Bridges runtime Unity events (inventory changes, farming actions) to pure C# POCO models.
    /// </summary>
    public class QuestManager : MonoBehaviour, INarrativeObjectiveService
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
        public event Action<string, bool> OnObjectiveStateChanged;

        private readonly List<NarrativeObjective> _storyObjectives = new List<NarrativeObjective>();
        public IReadOnlyList<NarrativeObjective> ActiveObjectives => _storyObjectives;

        private InventoryComponent _playerInventory;
        private bool _isInventorySubscribed = false;
        private QuestStage _lastAnnouncedStage = QuestStage.GatheringSupplies;
        private readonly List<string> _itemIdsCache = new List<string>(16);
        private FarmlandTile[] _cachedFarmlandTiles;

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
            ServiceLocator.Register<INarrativeObjectiveService>(this);
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
            TryBindPlayerInventory();
        }

        private void OnDisable()
        {
            FarmlandTile.OnCropPlanted -= HandleCropPlanted;
            FarmlandTile.OnCropWatered -= HandleCropWatered;
            UnbindPlayerInventory();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            _model.OnStateChanged -= HandleModelStateChanged;
            ServiceLocator.Unregister<INarrativeObjectiveService>();
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

        #region INarrativeObjectiveService Implementation

        public NarrativeObjective GetObjective(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId)) return null;
            for (int i = 0; i < _storyObjectives.Count; i++)
            {
                if (string.Equals(_storyObjectives[i].objectiveId, objectiveId, StringComparison.OrdinalIgnoreCase))
                {
                    return _storyObjectives[i];
                }
            }
            return null;
        }

        public NarrativeObjective GetObject(string objectiveId) => GetObjective(objectiveId);

        public void AssignStoryObjective(string objectiveId, string title, string description, int targetCount)
        {
            AssignStoryObjective(objectiveId, NarrativeObjectiveCategory.MainStory, title, description, targetCount, NarrativeFactKey.CurrentDay);
        }

        public void AssignStoryObjective(string objectiveId, NarrativeObjectiveCategory category, string title, string description, int targetCount, NarrativeFactKey trackingKey)
        {
            if (string.IsNullOrEmpty(objectiveId)) return;

            var existing = GetObjective(objectiveId);
            if (existing != null)
            {
                existing.title = title;
                existing.description = description;
                existing.targetCount = targetCount;
                existing.category = category;
                existing.trackingFactKey = trackingKey;
            }
            else
            {
                var newObj = new NarrativeObjective(objectiveId, title, description, targetCount, category, trackingKey);
                _storyObjectives.Add(newObj);
            }

            OnObjectiveStateChanged?.Invoke(objectiveId, false);
            OnQuestUpdated?.Invoke(_model);
        }

        public void CompleteStoryObjective(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId)) return;

            var obj = GetObjective(objectiveId);
            if (obj != null && !obj.isCompleted)
            {
                obj.isCompleted = true;
                obj.currentCount = obj.targetCount;
                OnObjectiveStateChanged?.Invoke(objectiveId, true);
                OnQuestUpdated?.Invoke(_model);

                var floatingText = ServiceLocator.Resolve<IFloatingTextService>();
                var player = ServiceLocator.Resolve<IPlayerContext>();
                if (floatingText != null && player != null && player.Transform != null)
                {
                    floatingText.SpawnText(player.Transform.position + Vector3.up * 2.2f, $"✨ Objective Complete: {obj.title}!", new Color(1f, 0.85f, 0.25f));
                }
                Debug.Log($"[QuestManager] ✨ Story objective completed: '{obj.title}' ({objectiveId})");
            }
        }

        public void UpdateObjectiveProgress(string objectiveId, int currentCount)
        {
            if (string.IsNullOrEmpty(objectiveId)) return;

            var obj = GetObjective(objectiveId);
            if (obj != null)
            {
                obj.currentCount = currentCount;
                if (obj.currentCount >= obj.targetCount && !obj.isCompleted)
                {
                    CompleteStoryObjective(objectiveId);
                }
                else
                {
                    OnObjectiveStateChanged?.Invoke(objectiveId, false);
                    OnQuestUpdated?.Invoke(_model);
                }
            }
        }

        public void ReportProgress(NarrativeFactKey factKey, int delta)
        {
            for (int i = 0; i < _storyObjectives.Count; i++)
            {
                var obj = _storyObjectives[i];
                if (obj != null && !obj.isCompleted && obj.trackingFactKey == factKey)
                {
                    UpdateObjectiveProgress(obj.objectiveId, obj.currentCount + delta);
                }
            }
        }

        public QuestSavePayload ExportSavePayload()
        {
            var payload = new QuestSavePayload
            {
                activeQuestId = QuestProgressModel.QuestId,
                currentStage = (int)_model.CurrentStage,
                plantedCount = _model.PlantedCount,
                wateredCount = _model.WateredCount,
                isCompleted = _model.IsCompleted
            };

            for (int i = 0; i < _storyObjectives.Count; i++)
            {
                var obj = _storyObjectives[i];
                if (obj != null)
                {
                    payload.activeStoryObjectives.Add(new NarrativeObjectiveSaveData
                    {
                        objectiveId = obj.objectiveId,
                        category = obj.category,
                        title = obj.title,
                        description = obj.description,
                        targetCount = obj.targetCount,
                        currentCount = obj.currentCount,
                        isCompleted = obj.isCompleted,
                        trackingFactKey = obj.trackingFactKey
                    });
                }
            }

            return payload;
        }

        public void ImportSavePayload(QuestSavePayload payload)
        {
            if (payload == null) return;

            // Zero-reflection typed model state restoration
            _model.RestoreState(
                (QuestStage)payload.currentStage,
                payload.plantedCount,
                payload.wateredCount,
                payload.plantedCount > 0,
                payload.isCompleted
            );

            _storyObjectives.Clear();
            if (payload.activeStoryObjectives != null)
            {
                for (int i = 0; i < payload.activeStoryObjectives.Count; i++)
                {
                    var saved = payload.activeStoryObjectives[i];
                    if (saved == null || string.IsNullOrEmpty(saved.objectiveId)) continue;

                    int resolvedTarget = saved.targetCount > 0 ? saved.targetCount : 1;
                    string resolvedTitle = !string.IsNullOrEmpty(saved.title) ? saved.title : saved.objectiveId;
                    string resolvedDescription = saved.description ?? string.Empty;

                    var restored = new NarrativeObjective(
                        saved.objectiveId,
                        resolvedTitle,
                        resolvedDescription,
                        resolvedTarget,
                        saved.category,
                        saved.trackingFactKey)
                    {
                        currentCount = saved.currentCount,
                        isCompleted = saved.isCompleted
                    };
                    _storyObjectives.Add(restored);
                }
            }

            OnQuestUpdated?.Invoke(_model);
            OnObjectiveStateChanged?.Invoke(string.Empty, false);
        }

        #endregion
    }
}
