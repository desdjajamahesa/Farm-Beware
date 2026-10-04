using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Widget bar darah mengambang (Overhead Floating Health Bar) individual untuk satu musuh.
    /// Berada di dalam Screen-Space UI Canvas dan diposisikan di atas kepala monster via Camera.WorldToScreenPoint.
    /// Dilengkapi Ghost Damage Fill (amber catch-up), fade kontekstual, dan tekstur prosedural bersih.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class EnemyOverheadBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image ghostFillImage;
        [SerializeField] private Image mainFillImage;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Tuning & Timings")]
        [SerializeField] private float ghostDelay = 0.22f;
        [SerializeField] private float ghostShrinkSpeed = 2.4f;
        [SerializeField] private float combatVisibleDuration = 3.2f;
        [SerializeField] private float fadeDuration = 0.25f;

        [Header("Colors")]
        [SerializeField] private Color normalHealthColor = new Color(0.92f, 0.22f, 0.22f, 1f);     // Ruby Red
        [SerializeField] private Color criticalHealthColor = new Color(0.98f, 0.12f, 0.12f, 1f);   // Bright Crimson
        [SerializeField] private Color ghostColor = new Color(0.98f, 0.75f, 0.15f, 0.95f);         // Amber Gold
        [SerializeField] private Color bgColor = new Color(0.07f, 0.08f, 0.12f, 0.88f);            // Deep Obsidian

        private EnemyBase boundEnemy;
        private Coroutine ghostCoroutine;
        private Coroutine fadeCoroutine;
        private float lastDamageTime = -99f;
        private float currentFillRatio = 1f;
        private float targetFillRatio = 1f;
        private bool isBossBar = false;

        private static Sprite cachedBarSprite;

        private void Awake()
        {
            EnsureComponents();
        }

        public void EnsureComponents()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            EnsureVisualElements();
        }

        public void Bind(EnemyBase enemy)
        {
            Unbind();

            if (enemy == null) return;
            boundEnemy = enemy;
            isBossBar = enemy.isBoss;

            // Sesuaikan ukuran bar: Boss bar sedikit lebih lebar jika diaktifkan overhead
            float width = isBossBar ? 110f : 80f;
            float height = isBossBar ? 9f : 7f;
            rectTransform.sizeDelta = new Vector2(width, height);

            boundEnemy.OnHealthChanged += HandleHealthChanged;

            // Inisialisasi rasio darah
            float maxHp = Mathf.Max(1, enemy.maxHealth);
            float curHp = Mathf.Clamp(enemy.currentHealth, 0, enemy.maxHealth);
            currentFillRatio = curHp / maxHp;
            targetFillRatio = currentFillRatio;

            if (mainFillImage != null) mainFillImage.fillAmount = currentFillRatio;
            if (ghostFillImage != null) ghostFillImage.fillAmount = currentFillRatio;

            UpdateHpText(curHp, maxHp);

            // Kondisi awal: sembunyikan jika HP masih penuh (anti-clutter), atau langsung tampil jika sudah terluka
            if (curHp < maxHp)
            {
                canvasGroup.alpha = 1f;
                lastDamageTime = Time.time;
            }
            else
            {
                canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            if (boundEnemy != null)
            {
                boundEnemy.OnHealthChanged -= HandleHealthChanged;
                boundEnemy = null;
            }

            if (ghostCoroutine != null)
            {
                StopCoroutine(ghostCoroutine);
                ghostCoroutine = null;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void HandleHealthChanged(int currentHp, int maxHp)
        {
            if (boundEnemy == null) return;

            float newRatio = Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));

            // Jika menerima damage (rasio berkurang)
            if (newRatio < targetFillRatio)
            {
                lastDamageTime = Time.time;
                targetFillRatio = newRatio;

                if (mainFillImage != null)
                {
                    mainFillImage.fillAmount = targetFillRatio;
                    mainFillImage.color = targetFillRatio <= 0.25f ? criticalHealthColor : normalHealthColor;
                }

                // Mulai lag ghost catch-up
                if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
                ghostCoroutine = StartCoroutine(RoutineGhostDrop(targetFillRatio));

                // Pastikan bar langsung terlihat (Snap Alpha 1)
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                canvasGroup.alpha = 1f;
            }
            else
            {
                // Healing: Naikkan langsung bersamaan
                targetFillRatio = newRatio;
                currentFillRatio = newRatio;
                if (mainFillImage != null) mainFillImage.fillAmount = newRatio;
                if (ghostFillImage != null) ghostFillImage.fillAmount = newRatio;
                lastDamageTime = Time.time;
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                canvasGroup.alpha = 1f;
            }

            UpdateHpText(currentHp, maxHp);

            // Jika mati, fade out seketika
            if (currentHp <= 0)
            {
                FadeOut(0.12f);
            }
        }

        private IEnumerator RoutineGhostDrop(float target)
        {
            yield return new WaitForSeconds(ghostDelay);

            while (ghostFillImage != null && ghostFillImage.fillAmount > target)
            {
                ghostFillImage.fillAmount = Mathf.MoveTowards(ghostFillImage.fillAmount, target, Time.deltaTime * ghostShrinkSpeed);
                yield return null;
            }

            if (ghostFillImage != null) ghostFillImage.fillAmount = target;
            ghostCoroutine = null;
        }

        /// <summary>
        /// Mengecek apakah bar sudah idle di luar pertarungan dan perlu memudar.
        /// Dipanggil secara terpusat oleh EnemyHealthBarManager demi menghemat per-frame Update overhead.
        /// </summary>
        public void EvaluateVisibility(bool isVisibleOnScreen)
        {
            if (!isVisibleOnScreen || boundEnemy == null || boundEnemy.IsDead)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            // Jika HP penuh dan sudah melebihi durasi combat visible, fade out halus
            if (targetFillRatio >= 0.999f && Time.time - lastDamageTime > combatVisibleDuration)
            {
                if (canvasGroup.alpha > 0f && fadeCoroutine == null)
                {
                    FadeOut(fadeDuration);
                }
            }
            else if (targetFillRatio < 0.999f && Time.time - lastDamageTime > 5.0f)
            {
                // Jika musuh terluka tapi dianggurkan lebih dari 5 detik, fade out lembut agar layar tidak kotor
                if (canvasGroup.alpha > 0.35f && fadeCoroutine == null)
                {
                    fadeCoroutine = StartCoroutine(RoutineFadeAlpha(0.35f, fadeDuration));
                }
            }
        }

        private void FadeOut(float duration)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(RoutineFadeAlpha(0f, duration));
        }

        private IEnumerator RoutineFadeAlpha(float targetAlpha, float duration)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
            fadeCoroutine = null;
        }

        private void UpdateHpText(float cur, float max)
        {
            if (hpText != null)
            {
                hpText.text = $"{Mathf.RoundToInt(cur)}";
            }
        }

        public void SetScreenPosition(Vector2 screenPos)
        {
            rectTransform.position = screenPos;
        }

        public EnemyBase BoundEnemy => boundEnemy;

        /// <summary>
        /// Membangun struktur visual bar darah prosedural jika prefab/komponen belum terisi di inspector.
        /// Menggunakan round rectangle sprite beresolusi tinggi yang ramah GPU batching.
        /// </summary>
        private void EnsureVisualElements()
        {
            if (cachedBarSprite == null)
            {
                cachedBarSprite = CreateRoundedBarSprite();
            }

            // 1. Background Frame
            if (backgroundImage == null)
            {
                var bgObj = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(transform, false);
                backgroundImage = bgObj.GetComponent<Image>();
                backgroundImage.sprite = cachedBarSprite;
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = bgColor;
                backgroundImage.raycastTarget = false;

                var rt = bgObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // 2. Ghost Fill (Amber lag bar)
            if (ghostFillImage == null)
            {
                var ghostObj = new GameObject("GhostFill", typeof(RectTransform), typeof(Image));
                ghostObj.transform.SetParent(transform, false);
                ghostFillImage = ghostObj.GetComponent<Image>();
                ghostFillImage.sprite = cachedBarSprite;
                ghostFillImage.type = Image.Type.Filled;
                ghostFillImage.fillMethod = Image.FillMethod.Horizontal;
                ghostFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                ghostFillImage.color = ghostColor;
                ghostFillImage.raycastTarget = false;

                var rt = ghostObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(1f, 1f);
                rt.offsetMax = new Vector2(-1f, -1f);
            }

            // 3. Main Fill (Ruby Red)
            if (mainFillImage == null)
            {
                var mainObj = new GameObject("MainFill", typeof(RectTransform), typeof(Image));
                mainObj.transform.SetParent(transform, false);
                mainFillImage = mainObj.GetComponent<Image>();
                mainFillImage.sprite = cachedBarSprite;
                mainFillImage.type = Image.Type.Filled;
                mainFillImage.fillMethod = Image.FillMethod.Horizontal;
                mainFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                mainFillImage.color = normalHealthColor;
                mainFillImage.raycastTarget = false;

                var rt = mainObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(1f, 1f);
                rt.offsetMax = new Vector2(-1f, -1f);
            }

            // 4. HP Text opsional
            if (hpText == null)
            {
                var txtObj = new GameObject("HpText", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(transform, false);
                hpText = txtObj.GetComponent<TextMeshProUGUI>();
                hpText.fontSize = 9f;
                hpText.fontStyle = FontStyles.Bold;
                hpText.alignment = TextAlignmentOptions.Center;
                hpText.color = Color.white;
                hpText.outlineWidth = 0.25f;
                hpText.outlineColor = new Color32(10, 10, 15, 255);
                hpText.raycastTarget = false;

                var rt = txtObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(60f, 14f);
                rt.anchoredPosition = new Vector2(0f, 1f);
                txtObj.SetActive(false); // Sembunyikan angka default pada kroco agar ultra-minimalis
            }
        }

        private static Sprite CreateRoundedBarSprite()
        {
            int w = 32;
            int h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color solid = Color.white;
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Sudut melengkung halus 2px
                    bool isCorner = (x <= 1 && y <= 1) || (x <= 1 && y >= h - 2) || (x >= w - 2 && y <= 1) || (x >= w - 2 && y >= h - 2);
                    tex.SetPixel(x, y, isCorner ? clear : solid);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
        }

        private void OnDisable()
        {
            Unbind();
        }
    }
}
