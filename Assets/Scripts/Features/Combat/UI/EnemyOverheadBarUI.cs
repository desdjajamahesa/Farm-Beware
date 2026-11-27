using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Individual Overhead Floating Health Bar widget for a single enemy.
    /// Lives inside Screen-Space UI Canvas and is positioned above the monster's head via Camera.WorldToScreenPoint.
    /// Includes Ghost Damage Fill (amber catch-up), contextual fading, and clean procedural textures.
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
        private bool hideAtFullHealth = false;

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

        public void Bind(EnemyBase enemy, bool hideAtFullHealth = false)
        {
            Unbind();

            if (enemy == null) return;
            boundEnemy = enemy;
            isBossBar = enemy.isBoss;
            this.hideAtFullHealth = hideAtFullHealth;

            // Adjust bar dimensions: Boss bar is slightly wider when overhead display is enabled
            float width = isBossBar ? 110f : 80f;
            float height = isBossBar ? 9f : 7f;
            rectTransform.sizeDelta = new Vector2(width, height);

            boundEnemy.OnHealthChanged += HandleHealthChanged;

            // Initialize health ratio
            float maxHp = Mathf.Max(1, enemy.maxHealth);
            float curHp = Mathf.Clamp(enemy.currentHealth, 0, enemy.maxHealth);
            currentFillRatio = curHp / maxHp;
            targetFillRatio = currentFillRatio;

            if (mainFillImage != null) mainFillImage.fillAmount = currentFillRatio;
            if (ghostFillImage != null) ghostFillImage.fillAmount = currentFillRatio;

            UpdateHpText(curHp, maxHp);

            // Initial state: show if hideAtFullHealth is false or already damaged
            if (!this.hideAtFullHealth || curHp < maxHp)
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
            hideAtFullHealth = false;
        }

        private void HandleHealthChanged(int currentHp, int maxHp)
        {
            if (boundEnemy == null) return;

            float newRatio = Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));

            // If taking damage (ratio decreased)
            if (newRatio < targetFillRatio)
            {
                lastDamageTime = Time.time;
                targetFillRatio = newRatio;

                if (mainFillImage != null)
                {
                    mainFillImage.fillAmount = targetFillRatio;
                    mainFillImage.color = targetFillRatio <= 0.25f ? criticalHealthColor : normalHealthColor;
                }

                // Start ghost catch-up lag
                if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
                ghostCoroutine = StartCoroutine(RoutineGhostDrop(targetFillRatio));

                // Ensure bar is immediately visible (Snap Alpha 1)
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                canvasGroup.alpha = 1f;
            }
            else
            {
                // Healing: Raise both bars immediately together
                targetFillRatio = newRatio;
                currentFillRatio = newRatio;
                if (mainFillImage != null) mainFillImage.fillAmount = newRatio;
                if (ghostFillImage != null) ghostFillImage.fillAmount = newRatio;
                lastDamageTime = Time.time;
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                canvasGroup.alpha = 1f;
            }

            UpdateHpText(currentHp, maxHp);

            // If dead, fade out immediately
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
        /// Checks whether the bar has been idle out of combat and should fade out.
        /// Invoked centrally by EnemyHealthBarManager to avoid per-frame Update overhead.
        /// </summary>
        public void EvaluateVisibility(bool isVisibleOnScreen)
        {
            if (!isVisibleOnScreen || boundEnemy == null || boundEnemy.IsDead)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            // If hideAtFullHealth is enabled: hide when at full HP and past combatVisibleDuration
            if (hideAtFullHealth && targetFillRatio >= 0.999f && Time.time - lastDamageTime > combatVisibleDuration)
            {
                if (canvasGroup.alpha > 0f && fadeCoroutine == null)
                {
                    FadeOut(fadeDuration);
                }
            }
            else if (hideAtFullHealth && targetFillRatio < 0.999f && Time.time - lastDamageTime > 5.0f)
            {
                // If enemy is damaged but left idle for over 5s, softly dim bar to avoid screen clutter
                if (canvasGroup.alpha > 0.35f && fadeCoroutine == null)
                {
                    fadeCoroutine = StartCoroutine(RoutineFadeAlpha(0.35f, fadeDuration));
                }
            }
            else if (!hideAtFullHealth)
            {
                // If not hiding at full health, keep bar visible while enemy is active & on-screen
                if (canvasGroup.alpha < 1f && fadeCoroutine == null)
                {
                    canvasGroup.alpha = 1f;
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
        /// Procedurally builds the health bar visual hierarchy if prefab/components are unassigned in the Inspector.
        /// Uses a high-resolution rounded-rectangle sprite friendly to GPU batching.
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

            // 4. Optional HP Text
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
                txtObj.SetActive(false); // Hide default numbers on regular enemies for ultra-minimalist look
            }
        }

        private static Sprite CreateRoundedBarSprite()
        {
            return FarmBeware.Core.Runtime.ProceduralUIAssetCache.GetRoundedBarSprite();
        }

        private void OnDisable()
        {
            Unbind();
        }
    }
}
