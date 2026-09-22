using System.Collections.Generic;
using UnityEngine;

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

        private readonly List<EnemyIndicatorArrow> arrowPool = new List<EnemyIndicatorArrow>();
        private readonly HashSet<EnemyBase> pulsingEnemies = new HashSet<EnemyBase>();
        private Camera targetCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned += HandleEnemySpawned;
            }
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
                pulsingEnemies.Add(enemy);
            }
        }

        private void LateUpdate()
        {
            // Ambil kamera aktif
            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                if (FeaturesCamera.CameraManager.Instance != null && FeaturesCamera.CameraManager.Instance.MainCamera != null)
                {
                    targetCamera = FeaturesCamera.CameraManager.Instance.MainCamera;
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

            int arrowIndex = 0;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float halfWidth = (Screen.width * 0.5f) - edgeMargin;
            float halfHeight = (Screen.height * 0.5f) - edgeMargin;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var enemy = activeEnemies[i];
                if (enemy == null || enemy.IsDead) continue;

                Vector3 worldPos = enemy.transform.position + Vector3.up * 0.8f;
                Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

                // Koreksi jika target berada di belakang frustum kamera (screenPos.z < 0)
                bool isBehind = screenPos.z < 0;
                if (isBehind)
                {
                    screenPos.x = Screen.width - screenPos.x;
                    screenPos.y = Screen.height - screenPos.y;
                }

                // Cek apakah monster sudah terlihat jelas di dalam layar
                bool isOnScreen = !isBehind &&
                                  screenPos.x >= edgeMargin && screenPos.x <= Screen.width - edgeMargin &&
                                  screenPos.y >= edgeMargin && screenPos.y <= Screen.height - edgeMargin;

                if (isOnScreen)
                {
                    // Target ada di dalam layar, tidak perlu panah off-screen
                    pulsingEnemies.Remove(enemy);
                    continue;
                }

                // Hitung arah dari pusat layar ke target
                Vector2 fromCenter = new Vector2(screenPos.x, screenPos.y) - screenCenter;
                if (fromCenter.sqrMagnitude < 0.001f)
                {
                    fromCenter = Vector2.up;
                }

                // Interseksi vektor dengan kotak batas layar (clamping to screen edges)
                float scaleX = Mathf.Abs(fromCenter.x) > 0.0001f ? (halfWidth / Mathf.Abs(fromCenter.x)) : 9999f;
                float scaleY = Mathf.Abs(fromCenter.y) > 0.0001f ? (halfHeight / Mathf.Abs(fromCenter.y)) : 9999f;
                float scale = Mathf.Min(scaleX, scaleY);
                Vector2 clampedEdgePos = screenCenter + fromCenter * scale;

                float angleDegrees = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
                float distance = Vector3.Distance(targetCamera.transform.position, worldPos);

                bool isBoss = (enemy.enemyType == EnemyType.CyclopsTuberMaw ||
                               enemy.enemyType == EnemyType.TaroColossus ||
                               enemy.enemyType == EnemyType.TheRanger);

                bool shouldPulse = pulsingEnemies.Contains(enemy);

                var arrow = GetOrCreateArrow(arrowIndex);
                arrow.SetData(clampedEdgePos, angleDegrees, distance, isBoss, shouldPulse);
                arrowIndex++;
            }

            // Sembunyikan sisa pool yang tidak terpakai
            for (int j = arrowIndex; j < arrowPool.Count; j++)
            {
                arrowPool[j].Hide();
            }
        }

        private EnemyIndicatorArrow GetOrCreateArrow(int index)
        {
            if (index < arrowPool.Count)
            {
                return arrowPool[index];
            }

            GameObject obj;
            if (arrowPrefab != null)
            {
                obj = Instantiate(arrowPrefab, transform);
            }
            else
            {
                obj = new GameObject($"EnemyIndicator_{index}", typeof(RectTransform), typeof(CanvasGroup), typeof(EnemyIndicatorArrow));
                obj.transform.SetParent(transform, false);
            }

            var arrow = obj.GetComponent<EnemyIndicatorArrow>();
            arrowPool.Add(arrow);
            return arrow;
        }

        private void HideAllIndicators()
        {
            for (int i = 0; i < arrowPool.Count; i++)
            {
                if (arrowPool[i] != null)
                {
                    arrowPool[i].Hide();
                }
            }
        }
    }
}
