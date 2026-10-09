using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Manager UI untuk menampilkan indikator panah tepi layar (off-screen indicators)
    /// yang mengarahkan pandangan pemain ke posisi monster saat bertarung di malam hari (Night Brawl).
    /// </summary>
    public class EnemyOffscreenIndicatorUI : MonoBehaviour
    {
        public static EnemyOffscreenIndicatorUI Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float edgeMargin = 55f;
        [SerializeField] private GameObject arrowPrefab;

        private readonly Dictionary<EnemyBase, EnemyIndicatorArrow> activeIndicators = new Dictionary<EnemyBase, EnemyIndicatorArrow>();
        private readonly List<EnemyIndicatorArrow> freeArrows = new List<EnemyIndicatorArrow>();
        private readonly List<EnemyBase> toRemoveList = new List<EnemyBase>();
        private readonly HashSet<EnemyBase> currentOffscreenSet = new HashSet<EnemyBase>();
        private readonly HashSet<int> pulsingEnemies = new HashSet<int>();
        private Camera targetCamera;
        private Transform playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsurePlayerReference();
        }

        private void EnsurePlayerReference()
        {
            if (playerTransform == null)
            {
                var playerContext = ServiceLocator.Resolve<IPlayerContext>();
                if (playerContext != null && playerContext.Transform != null)
                {
                    playerTransform = playerContext.Transform;
                }
                else
                {
                    var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player") ?? GameObject.Find("PlayerCapsule");
                    if (playerObj != null)
                    {
                        playerTransform = playerObj.transform;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned += HandleEnemySpawned;
            }
            EnsurePlayerReference();
        }

        private void OnDisable()
        {
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned -= HandleEnemySpawned;
            }
            HideAllIndicators();
        }

        private void HandleEnemySpawned(EnemyBase enemy)
        {
            if (enemy != null)
            {
                pulsingEnemies.Add(enemy.GetInstanceID());
            }
        }

        private void LateUpdate()
        {
            // Ambil kamera aktif
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

            if (targetCamera == null)
            {
                HideAllIndicators();
                return;
            }

            // Pastikan referensi karakter pemain tersedia secara ketat (tidak pernah fallback ke posisi kamera)
            EnsurePlayerReference();
            if (playerTransform == null)
            {
                HideAllIndicators();
                return;
            }

            // Jika NightBrawlManager belum aktif atau sedang tidak ada pertarungan malam, sembunyikan semua
            if (NightBrawlManager.Instance == null || !NightBrawlManager.Instance.IsNightBrawlActive)
            {
                HideAllIndicators();
                return;
            }

            var activeEnemies = NightBrawlManager.Instance.ActiveEnemies;
            if (activeEnemies == null || activeEnemies.Count == 0)
            {
                HideAllIndicators();
                return;
            }

            currentOffscreenSet.Clear();
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float halfWidth = (Screen.width * 0.5f) - edgeMargin;
            float halfHeight = (Screen.height * 0.5f) - edgeMargin;
            Vector3 playerPos = playerTransform.position;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var enemy = activeEnemies[i];
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

                Vector3 worldPos = enemy.transform.position + Vector3.up * 0.8f;
                Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

                // Koreksi jika target berada di belakang frustum kamera
                bool isBehind = !targetCamera.orthographic && screenPos.z < 0;
                if (isBehind)
                {
                    screenPos.x = Screen.width - screenPos.x;
                    screenPos.y = Screen.height - screenPos.y;
                }

                // Validasi kedalaman pandangan kamera
                bool isWithinDepth = screenPos.z >= targetCamera.nearClipPlane && screenPos.z <= targetCamera.farClipPlane;

                // Cek apakah monster sudah terlihat jelas di dalam batas layar
                bool isOnScreen = isWithinDepth && !isBehind &&
                                  screenPos.x >= edgeMargin && screenPos.x <= Screen.width - edgeMargin &&
                                  screenPos.y >= edgeMargin && screenPos.y <= Screen.height - edgeMargin;

                if (isOnScreen)
                {
                    pulsingEnemies.Remove(enemy.GetInstanceID());
                    continue;
                }

                currentOffscreenSet.Add(enemy);

                // Hitung arah dari pusat layar ke target
                Vector2 fromCenter = new Vector2(screenPos.x, screenPos.y) - screenCenter;
                if (fromCenter.sqrMagnitude < 0.001f)
                {
                    fromCenter = Vector2.up;
                }

                // Interseksi vektor dengan kotak batas layar
                float scaleX = Mathf.Abs(fromCenter.x) > 0.0001f ? (halfWidth / Mathf.Abs(fromCenter.x)) : 9999f;
                float scaleY = Mathf.Abs(fromCenter.y) > 0.0001f ? (halfHeight / Mathf.Abs(fromCenter.y)) : 9999f;
                float scale = Mathf.Min(scaleX, scaleY);
                Vector2 clampedEdgePos = screenCenter + fromCenter * scale;

                float angleDegrees = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;

                // Hitung jarak real-time murni di bidang datar XZ antara pemain dan musuh
                float distance = Vector2.Distance(
                    new Vector2(playerPos.x, playerPos.z),
                    new Vector2(worldPos.x, worldPos.z));

                bool isBoss = (enemy.enemyType == EnemyType.CyclopsTuberMaw ||
                               enemy.enemyType == EnemyType.TaroColossus ||
                               enemy.enemyType == EnemyType.TheRanger);

                bool shouldPulse = pulsingEnemies.Contains(enemy.GetInstanceID());

                // Dapatkan atau buat indikator yang terpetakan khusus ke instans musuh ini
                if (!activeIndicators.TryGetValue(enemy, out var arrow))
                {
                    arrow = GetOrCreateFreeArrow();
                    activeIndicators[enemy] = arrow;
                }

                arrow.SetData(clampedEdgePos, angleDegrees, distance, isBoss, shouldPulse);
            }

            // Kembalikan panah dari musuh yang sudah tidak off-screen / sudah mati ke pool bebas
            toRemoveList.Clear();
            foreach (var kvp in activeIndicators)
            {
                if (kvp.Key == null || kvp.Key.IsDead || !currentOffscreenSet.Contains(kvp.Key))
                {
                    toRemoveList.Add(kvp.Key);
                    if (kvp.Key != null)
                    {
                        pulsingEnemies.Remove(kvp.Key.GetInstanceID());
                    }
                    if (kvp.Value != null)
                    {
                        kvp.Value.Hide();
                        freeArrows.Add(kvp.Value);
                    }
                }
            }

            for (int r = 0; r < toRemoveList.Count; r++)
            {
                activeIndicators.Remove(toRemoveList[r]);
            }
        }

        private EnemyIndicatorArrow GetOrCreateFreeArrow()
        {
            if (freeArrows.Count > 0)
            {
                int lastIdx = freeArrows.Count - 1;
                var pooled = freeArrows[lastIdx];
                freeArrows.RemoveAt(lastIdx);
                if (pooled != null) return pooled;
            }

            GameObject obj;
            if (arrowPrefab != null)
            {
                obj = Instantiate(arrowPrefab, transform);
            }
            else
            {
                int id = activeIndicators.Count + freeArrows.Count;
                obj = new GameObject($"EnemyIndicator_{id}", typeof(RectTransform), typeof(CanvasGroup), typeof(EnemyIndicatorArrow));
                obj.transform.SetParent(transform, false);
            }

            return obj.GetComponent<EnemyIndicatorArrow>();
        }

        private void HideAllIndicators()
        {
            foreach (var kvp in activeIndicators)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Hide();
                    freeArrows.Add(kvp.Value);
                }
            }
            activeIndicators.Clear();

            for (int i = 0; i < freeArrows.Count; i++)
            {
                if (freeArrows[i] != null)
                {
                    freeArrows[i].Hide();
                }
            }
            pulsingEnemies.Clear();
        }
    }
}
