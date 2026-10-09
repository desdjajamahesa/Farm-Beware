using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Manager terpusat untuk mengelola pool dan proyeksi layar bar darah melayang (Overhead Floating Health Bars)
    /// bagi seluruh monster biasa yang aktif di Night Brawl.
    /// Dilengkapi Zero-GC object pooling, Camera Frustum Culling, dan Dynamic Height Offset.
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

                // Bind monster yang sudah aktif sebelumnya
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
            if (containerRect == null)
            {
                containerRect = GetComponent<RectTransform>();
                if (containerRect == null)
                {
                    // Cari canvas utama UI_Canvas
                    var canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        var containerObj = new GameObject("EnemyHealthBarContainer", typeof(RectTransform));
                        containerObj.transform.SetParent(canvas.transform, false);
                        containerRect = containerObj.GetComponent<RectTransform>();
                        containerRect.anchorMin = Vector2.zero;
                        containerRect.anchorMax = Vector2.one;
                        containerRect.offsetMin = Vector2.zero;
                        containerRect.offsetMax = Vector2.zero;
                    }
                }
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

            // Jika boss dan setting overhead boss dimatikan, abaikan (karena sudah ditangani BossHealthBarManager)
            if (enemy.isBoss && !showOverheadBarForBosses) return;

            EnemyOverheadBarUI bar = pool.Count > 0 ? pool.Dequeue() : CreateNewBarInstance();
            bar.Bind(enemy);
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

                // Validasi siklus hidup
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                {
                    deadBuffer.Add(enemy);
                    continue;
                }

                // Hitung posisi kepala di dunia 3D berdasarkan skala fisik monster
                float heightOffset = GetHeadHeightOffset(enemy.enemyType, enemy.isBoss);
                Vector3 headWorldPos = enemy.transform.position + Vector3.up * heightOffset;

                // Proyeksikan posisi 3D dunia ke 2D koordinat layar
                Vector3 screenPoint = targetCamera.WorldToScreenPoint(headWorldPos);

                // Frustum Culling: hanya update & tampilkan jika berada di depan dan dalam batas pandang kamera
                bool isVisible = screenPoint.z > 0f &&
                                 screenPoint.x >= -40f && screenPoint.x <= screenWidth + 40f &&
                                 screenPoint.y >= -40f && screenPoint.y <= screenHeight + 40f;

                if (isVisible)
                {
                    bar.SetScreenPosition(new Vector2(screenPoint.x, screenPoint.y));
                }

                bar.EvaluateVisibility(isVisible);
            }

            // Bersihkan entitas mati atau dikembalikan ke pool
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
        /// Ketinggian offset di atas kepala berdasarkan tipe dan ukuran fisik masing-masing monster.
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
