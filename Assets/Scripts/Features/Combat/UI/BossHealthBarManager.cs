using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Manager terpusat untuk menampilkan bar darah Boss sinematik di layar atas (Top-Center HUD).
    /// Mendukung 1 Boss tunggal (Hari 2-4) ataupun Multi-Boss sekaligus (Hari 5 Dual Boss: Cyclops Tuber Maw + The Ranger).
    /// Otomatis mengatur layout (Single vs Dual Stacked), fade transisi saat muncul/kalah, dan Zero-GC runtime.
    /// </summary>
    public class BossHealthBarManager : MonoBehaviour
    {
        private static BossHealthBarManager _instance;
        public static BossHealthBarManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    BossHealthBarManager[] found = FindObjectsByType<BossHealthBarManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                    {
                        _instance = found[0];
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Hierarchy & Containers")]
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private GameObject slotPrefab;

        private readonly List<BossHealthBarSlotUI> slotPool = new List<BossHealthBarSlotUI>();
        private readonly Dictionary<EnemyBase, BossHealthBarSlotUI> activeBosses = new Dictionary<EnemyBase, BossHealthBarSlotUI>();

        private Coroutine fadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureContainer();
            PrewarmSlots(3); // Support up to 3 bosses simultaneously
        }

        private void OnEnable()
        {
            if (NightBrawlManager.Instance != null)
            {
                NightBrawlManager.Instance.OnEnemySpawned += HandleEnemySpawned;
                NightBrawlManager.Instance.OnEnemyDied += HandleEnemyDied;

                // Bind jika ada boss yang sudah spawn
                if (NightBrawlManager.Instance.ActiveEnemies != null)
                {
                    foreach (var enemy in NightBrawlManager.Instance.ActiveEnemies)
                    {
                        if (enemy != null && enemy.isBoss)
                        {
                            HandleEnemySpawned(enemy);
                        }
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
            ClearAllBosses();
        }

        private void EnsureContainer()
        {
            if (containerRect == null)
            {
                containerRect = GetComponent<RectTransform>();
                if (containerRect == null)
                {
                    var canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        var bossPanel = new GameObject("BossHealthBarPanel", typeof(RectTransform), typeof(CanvasGroup));
                        bossPanel.transform.SetParent(canvas.transform, false);
                        containerRect = bossPanel.GetComponent<RectTransform>();
                        mainCanvasGroup = bossPanel.GetComponent<CanvasGroup>();

                        // Posisikan di layar atas tengah, sedikit di bawah indikator wave tracker
                        containerRect.anchorMin = new Vector2(0.5f, 1f);
                        containerRect.anchorMax = new Vector2(0.5f, 1f);
                        containerRect.pivot = new Vector2(0.5f, 1f);
                        containerRect.anchoredPosition = new Vector2(0f, -55f);
                        containerRect.sizeDelta = new Vector2(600f, 130f);
                    }
                }
            }

            if (mainCanvasGroup == null && containerRect != null)
            {
                mainCanvasGroup = containerRect.GetComponent<CanvasGroup>() ?? containerRect.gameObject.AddComponent<CanvasGroup>();
            }

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.blocksRaycasts = false;
            }
        }

        private void PrewarmSlots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                BossHealthBarSlotUI slot = CreateSlotInstance();
                slot.gameObject.SetActive(false);
                slotPool.Add(slot);
            }
        }

        private BossHealthBarSlotUI CreateSlotInstance()
        {
            GameObject obj;
            if (slotPrefab != null)
            {
                obj = Instantiate(slotPrefab, containerRect);
            }
            else
            {
                obj = new GameObject("BossSlot", typeof(RectTransform), typeof(CanvasGroup), typeof(BossHealthBarSlotUI));
                obj.transform.SetParent(containerRect, false);
            }

            var slot = obj.GetComponent<BossHealthBarSlotUI>();
            slot.EnsureComponents();
            return slot;
        }

        public void HandleEnemySpawned(EnemyBase enemy)
        {
            if (enemy == null || !enemy.isBoss || activeBosses.ContainsKey(enemy)) return;

            // Ambil slot bebas dari pool
            BossHealthBarSlotUI slot = null;
            for (int i = 0; i < slotPool.Count; i++)
            {
                if (!slotPool[i].gameObject.activeSelf)
                {
                    slot = slotPool[i];
                    break;
                }
            }

            if (slot == null)
            {
                slot = CreateSlotInstance();
                slotPool.Add(slot);
            }

            activeBosses[enemy] = slot;
            ReorderAndLayoutSlots();

            // Fade in seluruh boss HUD
            if (mainCanvasGroup != null && mainCanvasGroup.alpha < 0.99f)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(RoutineFadeAlpha(1f, 0.4f));
            }
        }

        public void HandleEnemyDied(EnemyBase enemy)
        {
            if (enemy == null || !activeBosses.TryGetValue(enemy, out var slot)) return;

            activeBosses.Remove(enemy);

            slot.PlayDefeatedAnimation(() =>
            {
                slot.Unbind();
                slot.gameObject.SetActive(false);
                ReorderAndLayoutSlots();

                // Jika tidak ada lagi boss aktif, fade out seluruh HUD boss
                if (activeBosses.Count == 0 && mainCanvasGroup != null)
                {
                    if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                    fadeCoroutine = StartCoroutine(RoutineFadeAlpha(0f, 0.5f));
                }
            });
        }

        private void HandleAnyEnemyDied(EnemyBase enemy)
        {
            HandleEnemyDied(enemy);
        }

        private void ReorderAndLayoutSlots()
        {
            int count = activeBosses.Count;
            bool isDualBoss = count >= 2;

            int index = 0;
            float verticalSpacing = isDualBoss ? 56f : 64f;

            foreach (var kvp in activeBosses)
            {
                EnemyBase boss = kvp.Key;
                BossHealthBarSlotUI slot = kvp.Value;

                slot.Bind(boss, isDualBoss);

                var rt = slot.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -index * verticalSpacing);

                index++;
            }
        }

        private IEnumerator RoutineFadeAlpha(float target, float duration)
        {
            if (mainCanvasGroup == null) yield break;

            float start = mainCanvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                mainCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            mainCanvasGroup.alpha = target;
            fadeCoroutine = null;
        }

        private void ClearAllBosses()
        {
            foreach (var kvp in activeBosses)
            {
                kvp.Value.Unbind();
                kvp.Value.gameObject.SetActive(false);
            }
            activeBosses.Clear();

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
            }
        }
    }
}
