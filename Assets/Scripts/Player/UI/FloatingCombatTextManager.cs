using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PlayerUI
{
    /// <summary>
    /// Singleton manager untuk memunculkan teks pertarungan melayang (Floating Damage/Heal Text).
    /// Mendukung auto-binding ke PlayerStats untuk memunculkan -Damage dan +Heal secara otomatis.
    /// Menyediakan metode publik SpawnText untuk memunculkan damage musuh di posisi dunia 3D.
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
        [SerializeField] private Color damageColor = new Color(0.95f, 0.22f, 0.22f); // Merah Terang
        [SerializeField] private Color healColor = new Color(0.25f, 0.90f, 0.35f);   // Hijau Terang
        [SerializeField] private Color buffNoticeColor = new Color(0.95f, 0.85f, 0.25f); // Emas

        private readonly Queue<FloatingTextItem> pool = new Queue<FloatingTextItem>();

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
        }

        private void Start()
        {
            EnsurePlayerStatsBound();
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

        private void HandleDamageTaken(int amount)
        {
            if (playerStats == null) return;
            Vector3 worldPos = playerStats.transform.position + Vector3.up * 1.8f;
            SpawnText(worldPos, $"-{amount}", damageColor);
        }

        private void HandleHealed(int amount)
        {
            if (playerStats == null) return;
            Vector3 worldPos = playerStats.transform.position + Vector3.up * 1.8f;
            SpawnText(worldPos, $"+{amount} HP", healColor);
        }

        public void SpawnText(Vector3 worldPos, string text, Color color)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // Jika di belakang kamera, jangan munculkan
            if (screenPos.z < 0) return;

            // Beri sedikit random offset horizontal agar teks tidak menumpuk persis
            screenPos.x += Random.Range(-25f, 25f);
            screenPos.y += Random.Range(-10f, 15f);

            FloatingTextItem item = GetOrCreateItem();
            item.Play(text, color, screenPos, () => pool.Enqueue(item));
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
                return obj.GetComponent<FloatingTextItem>();
            }

            // Fallback prosedural
            return BuildProceduralFloatingText(containerCanvas);
        }

        private FloatingTextItem BuildProceduralFloatingText(Transform parent)
        {
            GameObject obj = new GameObject("FloatingText_Item", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 40f);

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;

            return obj.AddComponent<FloatingTextItem>();
        }
    }
}
