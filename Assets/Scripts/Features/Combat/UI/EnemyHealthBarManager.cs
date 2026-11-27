using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Centralized manager for pooling and screen-space projection of Overhead Floating Health Bars
    /// for all active regular monsters during Night Brawl.
    /// Includes Zero-GC object pooling, Camera Frustum Culling, and Dynamic Height Offset.
    /// </summary>
    public class EnemyHealthBarManager : MonoBehaviour
    {
        private static EnemyHealthBarManager _instance;
        public static EnemyHealthBarManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    EnemyHealthBarManager[] found = FindObjectsByType<EnemyHealthBarManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                    {
                        _instance = found[0];
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private bool showOverheadBarForBosses = true;
        [SerializeField] private bool hideBarAtFullHealth = false;

        public bool HideBarAtFullHealth
        {
            get => hideBarAtFullHealth;
            set => hideBarAtFullHealth = value;
        }

        [Header("Hierarchy")]
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private GameObject overheadBarPrefab;

        private readonly Queue<EnemyOverheadBarUI> pool = new Queue<EnemyOverheadBarUI>();
        private readonly Dictionary<EnemyBase, EnemyOverheadBarUI> activeBars = new Dictionary<EnemyBase, EnemyOverheadBarUI>();
        private readonly List<EnemyBase> deadBuffer = new List<EnemyBase>();

        private Camera targetCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureContainer();
            PrewarmPool();
        }

        private void OnEnable()
        {
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned += HandleEnemySpawned;
                NightBrawlManager.Instance.OnEnemyDied += HandleEnemyDied;

                // Bind previously active monsters
                if (NightBrawlManager.Instance.ActiveEnemies != null)
                {
                    foreach (var enemy in NightBrawlManager.Instance.ActiveEnemies)
                    {
                        HandleEnemySpawned(enemy);
                    }
                }
            }

            EnemyBase.OnAnyEnemyDied += HandleAnyEnemyDied;
        }

        private void OnDisable()
        {
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned -= HandleEnemySpawned;
                NightBrawlManager.Instance.OnEnemyDied -= HandleEnemyDied;
            }

            EnemyBase.OnAnyEnemyDied -= HandleAnyEnemyDied;
            ReleaseAllBars();
        }

        private void EnsureContainer()
        {
            var targetCanvas = CanvasHelper.ResolveMainUICanvas();
            if (targetCanvas == null) return;

            // Ensure manager GameObject itself is parented under the target canvas
            if (transform.parent != targetCanvas.transform)
            {
                transform.SetParent(targetCanvas.transform, false);
            }

            if (containerRect == null)
            {
                var existingContainer = targetCanvas.transform.Find("EnemyHealthBarContainer");
                if (existingContainer != null)
                {
                    containerRect = existingContainer.GetComponent<RectTransform>();
                }
                else
                {
                    var containerObj = new GameObject("EnemyHealthBarContainer", typeof(RectTransform));
                    containerObj.transform.SetParent(targetCanvas.transform, false);
                    containerRect = containerObj.GetComponent<RectTransform>();
                    containerRect.anchorMin = Vector2.zero;
                    containerRect.anchorMax = Vector2.one;
                    containerRect.offsetMin = Vector2.zero;
                    containerRect.offsetMax = Vector2.zero;
                }
            }
            else if (containerRect.parent != targetCanvas.transform)
            {
                containerRect.SetParent(targetCanvas.transform, false);
            }
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                EnemyOverheadBarUI bar = CreateNewBarInstance();
                bar.gameObject.SetActive(false);
                pool.Enqueue(bar);
            }
        }

        private EnemyOverheadBarUI CreateNewBarInstance()
        {
            GameObject obj;
            if (overheadBarPrefab != null)
            {
                obj = Instantiate(overheadBarPrefab, containerRect);
            }
            else
            {
                obj = new GameObject("OverheadHealthBar_Pooled", typeof(RectTransform), typeof(CanvasGroup), typeof(EnemyOverheadBarUI));
                obj.transform.SetParent(containerRect, false);
            }

            var bar = obj.GetComponent<EnemyOverheadBarUI>();
            bar.EnsureComponents();
            return bar;
        }

        public void HandleEnemySpawned(EnemyBase enemy)
        {
            if (enemy == null || activeBars.ContainsKey(enemy)) return;

            // If boss and overhead boss bar setting is disabled, ignore (handled by BossHealthBarManager)
            if (enemy.isBoss && !showOverheadBarForBosses) return;

            EnemyOverheadBarUI bar = pool.Count > 0 ? pool.Dequeue() : CreateNewBarInstance();
            bar.Bind(enemy, hideBarAtFullHealth);
            activeBars[enemy] = bar;
        }

        public void HandleEnemyDied(EnemyBase enemy)
        {
            if (enemy == null) return;

            if (activeBars.TryGetValue(enemy, out var bar))
            {
                activeBars.Remove(enemy);
                bar.Unbind();
                bar.gameObject.SetActive(false);
                pool.Enqueue(bar);
            }
        }

        private void HandleAnyEnemyDied(EnemyBase enemy)
        {
            HandleEnemyDied(enemy);
        }

        private void ResolveCamera()
        {
            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                var cameraService = ServiceLocator.Resolve<ICameraService>();
                if (cameraService != null && cameraService.MainCamera != null)
                {
                    targetCamera = cameraService.MainCamera;
                }
                else
                {
                    targetCamera = Camera.main;
                }
            }
        }

        private void LateUpdate()
        {
            if (activeBars.Count == 0) return;

            ResolveCamera();
            if (targetCamera == null) return;

            deadBuffer.Clear();

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            foreach (var kvp in activeBars)
            {
                EnemyBase enemy = kvp.Key;
                EnemyOverheadBarUI bar = kvp.Value;

                // Lifecycle validation
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                {
                    deadBuffer.Add(enemy);
                    continue;
                }

                // Compute 3D world head position based on monster physical scale
                float heightOffset = GetHeadHeightOffset(enemy.enemyType, enemy.isBoss);
                Vector3 headWorldPos = enemy.transform.position + Vector3.up * heightOffset;

                // Project 3D world position to 2D screen coordinates
                Vector3 screenPoint = targetCamera.WorldToScreenPoint(headWorldPos);

                // Frustum Culling: update & show only when in front of and within camera viewport bounds
                bool isVisible = screenPoint.z > 0f &&
                                 screenPoint.x >= -40f && screenPoint.x <= screenWidth + 40f &&
                                 screenPoint.y >= -40f && screenPoint.y <= screenHeight + 40f;

                if (isVisible)
                {
                    bar.SetScreenPosition(new Vector2(screenPoint.x, screenPoint.y));
                }

                bar.EvaluateVisibility(isVisible);
            }

            // Purge dead or pooled entities
            if (deadBuffer.Count > 0)
            {
                for (int i = 0; i < deadBuffer.Count; i++)
                {
                    HandleEnemyDied(deadBuffer[i]);
                }
                deadBuffer.Clear();
            }
        }

        public void ReleaseAllBars()
        {
            foreach (var kvp in activeBars)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Unbind();
                    kvp.Value.gameObject.SetActive(false);
                    pool.Enqueue(kvp.Value);
                }
            }
            activeBars.Clear();
        }

        /// <summary>
        /// Overhead height offset based on each monster's type and physical size.
        /// </summary>
        public static float GetHeadHeightOffset(EnemyType type, bool isBoss)
        {
            if (isBoss)
            {
                return type switch
                {
                    EnemyType.CyclopsTuberMaw => 2.75f,
                    EnemyType.TaroColossus => 3.45f,
                    EnemyType.TheRanger => 2.35f,
                    _ => 2.6f
                };
            }

            return type switch
            {
                EnemyType.TuberMaw => 1.15f,
                EnemyType.CornMusketeer => 1.65f,
                EnemyType.TaroBrute => 2.25f,
                _ => 1.4f
            };
        }
    }
}
