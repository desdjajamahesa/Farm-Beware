using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PlayerUI
{
    /// <summary>
    /// Singleton manager untuk memunculkan teks pertarungan melayang (Floating Damage/Heal/Notification Text).
    /// Mendukung auto-binding ke PlayerStats untuk memunculkan -Damage dan +Heal secara otomatis.
    /// Menyediakan metode publik SpawnText untuk memunculkan damage musuh di posisi dunia 3D dengan
    /// pelacakan dinamis kamera, font outline kontras tinggi, dan object pooling efisien (0 GC).
    /// </summary>
    public class FloatingCombatTextManager : MonoBehaviour
    {
        private static FloatingCombatTextManager _instance;
        public static FloatingCombatTextManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    FloatingCombatTextManager[] found = FindObjectsByType<FloatingCombatTextManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set { _instance = value; }
        }

        [Header("Target References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private RectTransform containerCanvas;
        [SerializeField] private GameObject floatingTextPrefab;

        [Header("Colors")]
        [SerializeField] private Color damageColor = new Color(1f, 0.28f, 0.22f);      // Merah Terang
        [SerializeField] private Color criticalDamageColor = new Color(1f, 0.70f, 0.15f); // Emas Jingga
        [SerializeField] private Color healColor = new Color(0.25f, 0.95f, 0.40f);        // Hijau Terang
        [SerializeField] private Color noticeColor = new Color(0.95f, 0.88f, 0.35f);      // Kuning Lembut

        private readonly Queue<FloatingTextItem> pool = new Queue<FloatingTextItem>();
        private Camera targetCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (containerCanvas == null)
            {
                containerCanvas = GetComponent<RectTransform>();
            }

            // Daftarkan dan nonaktifkan semua child pra-eksisting ke dalam pool
            if (containerCanvas != null)
            {
                for (int i = 0; i < containerCanvas.childCount; i++)
                {
                    var child = containerCanvas.GetChild(i);
                    var item = child.GetComponent<FloatingTextItem>();
                    if (item != null)
                    {
                        child.gameObject.SetActive(false);
                        pool.Enqueue(item);
                    }
                }
            }
        }

        private void Start()
        {
            EnsurePlayerStatsBound();
            ResolveCamera();
        }

        private void OnEnable()
        {
            EnsurePlayerStatsBound();
            if (playerStats != null)
            {
                playerStats.OnDamageTaken += HandleDamageTaken;
                playerStats.OnHealed += HandleHealed;
            }
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.OnDamageTaken -= HandleDamageTaken;
                playerStats.OnHealed -= HandleHealed;
            }
        }

        private void EnsurePlayerStatsBound()
        {
            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>();
                if (playerStats != null && isEnabled)
                {
                    playerStats.OnDamageTaken -= HandleDamageTaken;
                    playerStats.OnHealed -= HandleHealed;
                    playerStats.OnDamageTaken += HandleDamageTaken;
                    playerStats.OnHealed += HandleHealed;
                }
            }
        }

        private bool isEnabled => isActiveAndEnabled;

        private void ResolveCamera()
        {
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
        }

        private void HandleDamageTaken(int amount)
        {
            if (playerStats == null) return;
            Vector3 worldPos = playerStats.transform.position + Vector3.up * 1.8f;
            Color col = amount >= 30 ? criticalDamageColor : damageColor;
            SpawnText(worldPos, $"-{amount}", col);
        }

        private void HandleHealed(int amount)
        {
            if (playerStats == null) return;
            Vector3 worldPos = playerStats.transform.position + Vector3.up * 1.8f;
            SpawnText(worldPos, $"+{amount} HP", healColor);
        }

        public void SpawnText(Vector3 worldPos, string text)
        {
            SpawnText(worldPos, text, noticeColor);
        }

        public void SpawnText(Vector3 worldPos, string text, Color color)
        {
            ResolveCamera();
            if (targetCamera == null) return;

            // Berikan sedikit random jitter agar angka berurutan tidak menumpuk persis
            Vector3 spawnWorldPos = worldPos + new Vector3(
                Random.Range(-0.25f, 0.25f),
                Random.Range(0f, 0.2f),
                Random.Range(-0.25f, 0.25f));

            FloatingTextItem item = GetOrCreateItem();
            item.PlayWorldTracked(text, color, spawnWorldPos, targetCamera, () => pool.Enqueue(item));
        }

        private FloatingTextItem GetOrCreateItem()
        {
            while (pool.Count > 0)
            {
                FloatingTextItem item = pool.Dequeue();
                if (item != null)
                    return item;
            }

            if (floatingTextPrefab != null)
            {
                GameObject obj = Instantiate(floatingTextPrefab, containerCanvas);
                var comp = obj.GetComponent<FloatingTextItem>();
                if (comp != null) comp.EnsureComponents();
                return comp;
            }

            // Fallback prosedural berkualitas tinggi
            return BuildProceduralFloatingText(containerCanvas);
        }

        private FloatingTextItem BuildProceduralFloatingText(Transform parent)
        {
            GameObject obj = new GameObject("FloatingText_Item", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 48f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 26f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.outlineWidth = 0.28f;
            tmp.outlineColor = new Color32(15, 15, 15, 255);

            var item = obj.AddComponent<FloatingTextItem>();
            item.EnsureComponents();
            return item;
        }
    }
}
