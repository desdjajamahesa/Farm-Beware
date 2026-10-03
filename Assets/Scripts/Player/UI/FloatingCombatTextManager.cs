using System.Collections.Generic;
using TMPro;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace PlayerUI
{
    /// <summary>
    /// Singleton manager untuk memunculkan teks pertarungan melayang (Floating Damage/Heal/Notification Text).
    /// Membedakan secara visual dan dramatis antara:
    /// 1. Outgoing Damage (Pemain memukul monster): Warna Emas/Amber/Api cerah tanpa minus, simbol petir/ledakan.
    /// 2. Incoming Damage (Monster memukul pemain): Warna Merah Darah pekat dengan tanda minus dan label HP (-X HP).
    /// Menggunakan dynamic camera tracking, high-contrast outlines, dan object pooling 0-GC.
    /// </summary>
    public class FloatingCombatTextManager : MonoBehaviour, IFloatingTextService
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

        [Header("Colors - Outgoing (Pemain Memukul Monster)")]
        [Tooltip("Warna damage normal tebasan kombo pemain (Kuning Emas).")]
        [SerializeField] private Color enemyNormalDamageColor = new Color(1f, 0.84f, 0.15f);

        [Tooltip("Warna damage finisher kombo ke-3 putaran 360 (Jingga Petir / Amber).")]
        [SerializeField] private Color enemyFinisherDamageColor = new Color(1f, 0.52f, 0.05f);

        [Tooltip("Warna damage jurus spesial hantaman tanah (Merah-Jingga Api Ledakan).")]
        [SerializeField] private Color enemySkillDamageColor = new Color(1f, 0.28f, 0.05f);

        [Tooltip("Warna damage god mode cheat.")]
        [SerializeField] private Color godModeDamageColor = new Color(1f, 0.95f, 0.35f);

        [Header("Colors - Incoming (Monster Memukul Pemain)")]
        [Tooltip("Warna luka saat pemain terkena pukulan monster (Merah Darah Pekat).")]
        [SerializeField] private Color playerDamageColor = new Color(1f, 0.15f, 0.15f);

        [Tooltip("Warna luka kritis saat pemain terkena damage besar (Merah Tua Bahaya).")]
        [SerializeField] private Color playerCriticalDamageColor = new Color(0.92f, 0.04f, 0.04f);

        [Header("Colors - General")]
        [SerializeField] private Color healColor = new Color(0.25f, 0.95f, 0.40f);        // Hijau Terang
        [SerializeField] private Color noticeColor = new Color(0.95f, 0.88f, 0.35f);      // Kuning Lembut

        private static readonly Color32 DefaultEnemyOutline = new Color32(20, 20, 20, 255);
        private static readonly Color32 PlayerHurtOutline = new Color32(50, 0, 0, 255);

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
            ServiceLocator.Register<IFloatingTextService>(this);

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

        /// <summary>
        /// Memunculkan angka damage saat pemain memukul monster (Outgoing Hit).
        /// Format: Angka emas/jingga cerah tanpa tanda minus, melayang naik dengan punch scale memuaskan.
        /// </summary>
        public void SpawnEnemyDamage(Vector3 worldPos, int damage, bool isCrit = false, bool isSkill = false)
        {
            ResolveCamera();
            if (targetCamera == null) return;

            string text;
            Color textColor;
            float scaleMultiplier;

            if (damage >= 9999)
            {
                text = "💥 9999";
                textColor = godModeDamageColor;
                scaleMultiplier = 1.55f;
            }
            else if (isSkill)
            {
                text = $"💥 {damage}";
                textColor = enemySkillDamageColor;
                scaleMultiplier = 1.45f;
            }
            else if (isCrit)
            {
                text = $"⚡ {damage}";
                textColor = enemyFinisherDamageColor;
                scaleMultiplier = 1.25f;
            }
            else
            {
                text = $"{damage}";
                textColor = enemyNormalDamageColor;
                scaleMultiplier = 1.0f;
            }

            Vector3 spawnWorldPos = worldPos + new Vector3(
                Random.Range(-0.25f, 0.25f),
                Random.Range(0f, 0.25f),
                Random.Range(-0.25f, 0.25f));

            FloatingTextItem item = GetOrCreateItem();
            item.PlayWorldTracked(text, textColor, spawnWorldPos, targetCamera, scaleMultiplier, DefaultEnemyOutline, () => pool.Enqueue(item));
        }

        /// <summary>
        /// Memunculkan angka luka saat monster memukul pemain (Incoming Hit / Hurt).
        /// Format: Merah darah pekat dengan tanda minus jelas (-X HP), outline marun gelap, melayang di atas pemain.
        /// </summary>
        public void SpawnPlayerDamage(Vector3 worldPos, int amount)
        {
            ResolveCamera();
            if (targetCamera == null) return;

            bool isCritical = amount >= 30;
            string text = isCritical ? $"-{amount} HP!" : $"-{amount} HP";
            Color textColor = isCritical ? playerCriticalDamageColor : playerDamageColor;
            float scaleMultiplier = isCritical ? 1.35f : 1.15f;

            Vector3 spawnWorldPos = worldPos + new Vector3(
                Random.Range(-0.2f, 0.2f),
                Random.Range(0.05f, 0.2f),
                Random.Range(-0.2f, 0.2f));

            FloatingTextItem item = GetOrCreateItem();
            item.PlayWorldTracked(text, textColor, spawnWorldPos, targetCamera, scaleMultiplier, PlayerHurtOutline, () => pool.Enqueue(item));
        }

        private void HandleDamageTaken(int amount)
        {
            if (playerStats == null) return;
            Vector3 worldPos = playerStats.transform.position + Vector3.up * 1.9f;
            SpawnPlayerDamage(worldPos, amount);
        }

        private void HandleHealed(int amount)
        {
            if (playerStats == null) return;
            Vector3 worldPos = playerStats.transform.position + Vector3.up * 1.9f;
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

            Vector3 spawnWorldPos = worldPos + new Vector3(
                Random.Range(-0.25f, 0.25f),
                Random.Range(0f, 0.2f),
                Random.Range(-0.25f, 0.25f));

            FloatingTextItem item = GetOrCreateItem();
            item.PlayWorldTracked(text, color, spawnWorldPos, targetCamera, 1.0f, null, () => pool.Enqueue(item));
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

        void IFloatingTextService.SpawnEnemyDamage(Vector3 position, float damage, bool isCritical)
        {
            SpawnEnemyDamage(position, Mathf.RoundToInt(damage), isCritical);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<IFloatingTextService>();
                Instance = null;
            }
        }
    }
}
