using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Slot visual individual untuk bar kesehatan Boss di layar atas (Top HUD).
    /// Mendukung kustomisasi nama gelar, icon boss, ghost damage fill,
    /// indikator fase amukan (Enrage Divider), getaran saat terkena damage besar, dan transisi "DEFEATED".
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BossHealthBarSlotUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI bossSubtitleText;
        [SerializeField] private Image bossIconImage;
        [SerializeField] private Image frameBorderImage;
        [SerializeField] private Image bgImage;
        [SerializeField] private Image ghostFillImage;
        [SerializeField] private Image mainFillImage;
        [SerializeField] private Image enrageMarkerImage;
        [SerializeField] private TextMeshProUGUI hpValueText;
        [SerializeField] private TextMeshProUGUI defeatedBadgeText;

        [Header("Tuning")]
        [SerializeField] private float ghostDelay = 0.3f;
        [SerializeField] private float ghostShrinkSpeed = 1.8f;
        [SerializeField] private Color normalMainColor = new Color(0.88f, 0.16f, 0.16f, 1f);     // Dark Crimson
        [SerializeField] private Color enrageMainColor = new Color(0.98f, 0.35f, 0.05f, 1f);     // Fiery Ember
        [SerializeField] private Color ghostColor = new Color(0.96f, 0.72f, 0.15f, 0.95f);       // Amber Gold

        private EnemyBase boundBoss;
        private Coroutine ghostCoroutine;
        private Coroutine shakeCoroutine;
        private Coroutine pulseCoroutine;
        private float currentFillRatio = 1f;
        private float targetFillRatio = 1f;
        private bool isEnraged = false;
        private Vector3 initialScale = Vector3.one;

        private static Sprite cachedFrameSprite;

        private void Awake()
        {
            EnsureComponents();
        }

        public void EnsureComponents()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            initialScale = transform.localScale;
            EnsureVisualElements();
        }

        public void Bind(EnemyBase boss, bool isDualBossMode = false)
        {
            Unbind();

            if (boss == null) return;
            boundBoss = boss;

            // Atur dimensi slot: Dual Boss (Hari 5) sedikit lebih ramping dari Single Boss
            float width = isDualBossMode ? 480f : 560f;
            float height = isDualBossMode ? 46f : 54f;
            rectTransform.sizeDelta = new Vector2(width, height);

            ConfigureBossIdentity(boss.enemyType);

            boss.OnHealthChanged += HandleHealthChanged;

            float maxHp = Mathf.Max(1, boss.maxHealth);
            float curHp = Mathf.Clamp(boss.currentHealth, 0, boss.maxHealth);
            currentFillRatio = curHp / maxHp;
            targetFillRatio = currentFillRatio;

            if (mainFillImage != null) mainFillImage.fillAmount = currentFillRatio;
            if (ghostFillImage != null) ghostFillImage.fillAmount = currentFillRatio;

            UpdateHpText(curHp, maxHp);

            if (defeatedBadgeText != null) defeatedBadgeText.gameObject.SetActive(false);

            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            if (boundBoss != null)
            {
                boundBoss.OnHealthChanged -= HandleHealthChanged;
                boundBoss = null;
            }

            if (ghostCoroutine != null)
            {
                StopCoroutine(ghostCoroutine);
                ghostCoroutine = null;
            }

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            transform.localScale = initialScale;
        }

        private void ConfigureBossIdentity(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.CyclopsTuberMaw:
                    if (bossNameText != null) bossNameText.text = "👁️ CYCLOPS TUBER MAW";
                    if (bossSubtitleText != null) bossSubtitleText.text = "— Abyssal Eye of the Deep Hollow —";
                    if (enrageMarkerImage != null) enrageMarkerImage.gameObject.SetActive(false);
                    break;

                case EnemyType.TaroColossus:
                    if (bossNameText != null) bossNameText.text = "👑 TARO COLOSSUS";
                    if (bossSubtitleText != null) bossSubtitleText.text = "— Titan of the Primeval Roots —";
                    if (enrageMarkerImage != null) enrageMarkerImage.gameObject.SetActive(false);
                    break;

                case EnemyType.TheRanger:
                    if (bossNameText != null) bossNameText.text = "🏹 THE RANGER";
                    if (bossSubtitleText != null) bossSubtitleText.text = "— Sentinel of the Cursed Harvest —";
                    if (enrageMarkerImage != null) enrageMarkerImage.gameObject.SetActive(true);
                    break;

                default:
                    if (bossNameText != null) bossNameText.text = "💀 CORRUPTED BEAST";
                    if (bossSubtitleText != null) bossSubtitleText.text = "— Horror of the Night —";
                    if (enrageMarkerImage != null) enrageMarkerImage.gameObject.SetActive(false);
                    break;
            }
        }

        private void HandleHealthChanged(int currentHp, int maxHp)
        {
            if (boundBoss == null) return;

            float newRatio = Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));

            if (newRatio < targetFillRatio)
            {
                targetFillRatio = newRatio;

                if (mainFillImage != null)
                {
                    mainFillImage.fillAmount = targetFillRatio;
                }

                // Shake & Punch efek saat boss terkena pukulan
                if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
                shakeCoroutine = StartCoroutine(RoutinePunchShake());

                // Ghost lag drop
                if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
                ghostCoroutine = StartCoroutine(RoutineGhostDrop(targetFillRatio));
            }
            else
            {
                targetFillRatio = newRatio;
                currentFillRatio = newRatio;
                if (mainFillImage != null) mainFillImage.fillAmount = newRatio;
                if (ghostFillImage != null) ghostFillImage.fillAmount = newRatio;
            }

            // Cek Enrage phase untuk The Ranger (50% HP)
            if (boundBoss.enemyType == EnemyType.TheRanger && newRatio <= 0.501f && !isEnraged)
            {
                isEnraged = true;
                if (pulseCoroutine == null) pulseCoroutine = StartCoroutine(RoutineEnragePulse());
            }

            UpdateHpText(currentHp, maxHp);

            if (currentHp <= 0)
            {
                PlayDefeatedAnimation();
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

        private IEnumerator RoutinePunchShake()
        {
            transform.localScale = initialScale * 1.035f;
            yield return new WaitForSeconds(0.08f);
            transform.localScale = initialScale;
            shakeCoroutine = null;
        }

        private IEnumerator RoutineEnragePulse()
        {
            while (isEnraged && boundBoss != null && !boundBoss.IsDead)
            {
                if (mainFillImage != null)
                {
                    float t = Mathf.PingPong(Time.time * 3f, 1f);
                    mainFillImage.color = Color.Lerp(normalMainColor, enrageMainColor, t);
                }
                yield return null;
            }
        }

        public void PlayDefeatedAnimation(System.Action onComplete = null)
        {
            StartCoroutine(RoutineDefeatedSequence(onComplete));
        }

        private IEnumerator RoutineDefeatedSequence(System.Action onComplete)
        {
            if (defeatedBadgeText != null)
            {
                defeatedBadgeText.gameObject.SetActive(true);
                defeatedBadgeText.text = "⚔️ DEFEATED ⚔️";
            }

            if (mainFillImage != null) mainFillImage.color = new Color(0.3f, 0.3f, 0.35f, 0.8f);

            // Tunggu 0.8 detik agar pemain puas melihat teks DEFEATED
            yield return new WaitForSeconds(0.8f);

            // Fade out halus
            float elapsed = 0f;
            float duration = 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private void UpdateHpText(float cur, float max)
        {
            if (hpValueText != null)
            {
                hpValueText.text = $"{Mathf.RoundToInt(cur)} / {Mathf.RoundToInt(max)} HP";
            }
        }

        public EnemyBase BoundBoss => boundBoss;

        private void EnsureVisualElements()
        {
            if (cachedFrameSprite == null)
            {
                cachedFrameSprite = CreateBoxSprite();
            }

            // 1. Header Row (Nama & Subtitle)
            if (bossNameText == null)
            {
                var titleObj = new GameObject("BossTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObj.transform.SetParent(transform, false);
                bossNameText = titleObj.GetComponent<TextMeshProUGUI>();
                bossNameText.fontSize = 13.5f;
                bossNameText.fontStyle = FontStyles.Bold;
                bossNameText.alignment = TextAlignmentOptions.Left;
                bossNameText.color = new Color(0.98f, 0.85f, 0.35f); // Gold Amber
                bossNameText.outlineWidth = 0.28f;
                bossNameText.outlineColor = new Color32(10, 10, 15, 255);
                bossNameText.raycastTarget = false;

                var rt = titleObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0.6f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(4f, 0f);
                rt.sizeDelta = new Vector2(0f, 18f);
            }

            if (bossSubtitleText == null)
            {
                var subObj = new GameObject("BossSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                subObj.transform.SetParent(transform, false);
                bossSubtitleText = subObj.GetComponent<TextMeshProUGUI>();
                bossSubtitleText.fontSize = 10f;
                bossSubtitleText.fontStyle = FontStyles.Italic;
                bossSubtitleText.alignment = TextAlignmentOptions.Right;
                bossSubtitleText.color = new Color(0.75f, 0.75f, 0.85f, 0.9f);
                bossSubtitleText.outlineWidth = 0.2f;
                bossSubtitleText.outlineColor = new Color32(10, 10, 15, 255);
                bossSubtitleText.raycastTarget = false;

                var rt = subObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-4f, 0f);
                rt.sizeDelta = new Vector2(0f, 18f);
            }

            // 2. Bar Frame & Track
            if (frameBorderImage == null)
            {
                var frameObj = new GameObject("BarFrame", typeof(RectTransform), typeof(Image));
                frameObj.transform.SetParent(transform, false);
                frameBorderImage = frameObj.GetComponent<Image>();
                frameBorderImage.sprite = cachedFrameSprite;
                frameBorderImage.type = Image.Type.Sliced;
                frameBorderImage.color = new Color(0.18f, 0.16f, 0.24f, 1f);
                frameBorderImage.raycastTarget = false;

                var rt = frameObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 2f);
                rt.sizeDelta = new Vector2(0f, 24f);

                // Background di dalam frame
                var bgObj = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(frameObj.transform, false);
                bgImage = bgObj.GetComponent<Image>();
                bgImage.sprite = cachedFrameSprite;
                bgImage.type = Image.Type.Sliced;
                bgImage.color = new Color(0.06f, 0.06f, 0.09f, 0.95f);
                bgImage.raycastTarget = false;

                var bgRt = bgObj.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = new Vector2(2f, 2f);
                bgRt.offsetMax = new Vector2(-2f, -2f);

                // Ghost fill
                var ghostObj = new GameObject("GhostFill", typeof(RectTransform), typeof(Image));
                ghostObj.transform.SetParent(bgObj.transform, false);
                ghostFillImage = ghostObj.GetComponent<Image>();
                ghostFillImage.sprite = cachedFrameSprite;
                ghostFillImage.type = Image.Type.Filled;
                ghostFillImage.fillMethod = Image.FillMethod.Horizontal;
                ghostFillImage.color = ghostColor;
                ghostFillImage.raycastTarget = false;

                var ghostRt = ghostObj.GetComponent<RectTransform>();
                ghostRt.anchorMin = Vector2.zero;
                ghostRt.anchorMax = Vector2.one;
                ghostRt.offsetMin = Vector2.zero;
                ghostRt.offsetMax = Vector2.zero;

                // Main fill
                var mainObj = new GameObject("MainFill", typeof(RectTransform), typeof(Image));
                mainObj.transform.SetParent(bgObj.transform, false);
                mainFillImage = mainObj.GetComponent<Image>();
                mainFillImage.sprite = cachedFrameSprite;
                mainFillImage.type = Image.Type.Filled;
                mainFillImage.fillMethod = Image.FillMethod.Horizontal;
                mainFillImage.color = normalMainColor;
                mainFillImage.raycastTarget = false;

                var mainRt = mainObj.GetComponent<RectTransform>();
                mainRt.anchorMin = Vector2.zero;
                mainRt.anchorMax = Vector2.one;
                mainRt.offsetMin = Vector2.zero;
                mainRt.offsetMax = Vector2.zero;

                // Enrage Marker (Garis tengah di 50%)
                var markerObj = new GameObject("EnrageMarker_50", typeof(RectTransform), typeof(Image));
                markerObj.transform.SetParent(bgObj.transform, false);
                enrageMarkerImage = markerObj.GetComponent<Image>();
                enrageMarkerImage.color = new Color(1f, 0.2f, 0.2f, 0.85f);
                enrageMarkerImage.raycastTarget = false;

                var markerRt = markerObj.GetComponent<RectTransform>();
                markerRt.anchorMin = new Vector2(0.5f, 0f);
                markerRt.anchorMax = new Vector2(0.5f, 1f);
                markerRt.pivot = new Vector2(0.5f, 0.5f);
                markerRt.sizeDelta = new Vector2(2.5f, 0f);
                markerRt.anchoredPosition = Vector2.zero;
                markerObj.SetActive(false);

                // Angka HP eksak
                var hpTxtObj = new GameObject("HpValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
                hpTxtObj.transform.SetParent(bgObj.transform, false);
                hpValueText = hpTxtObj.GetComponent<TextMeshProUGUI>();
                hpValueText.fontSize = 11.5f;
                hpValueText.fontStyle = FontStyles.Bold;
                hpValueText.alignment = TextAlignmentOptions.Center;
                hpValueText.color = Color.white;
                hpValueText.outlineWidth = 0.25f;
                hpValueText.outlineColor = new Color32(10, 10, 15, 255);
                hpValueText.raycastTarget = false;

                var hpRt = hpTxtObj.GetComponent<RectTransform>();
                hpRt.anchorMin = Vector2.zero;
                hpRt.anchorMax = Vector2.one;
                hpRt.offsetMin = Vector2.zero;
                hpRt.offsetMax = Vector2.zero;

                // Defeated badge
                var defObj = new GameObject("DefeatedBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                defObj.transform.SetParent(frameObj.transform, false);
                defeatedBadgeText = defObj.GetComponent<TextMeshProUGUI>();
                defeatedBadgeText.fontSize = 14f;
                defeatedBadgeText.fontStyle = FontStyles.Bold;
                defeatedBadgeText.alignment = TextAlignmentOptions.Center;
                defeatedBadgeText.color = new Color(0.98f, 0.85f, 0.2f);
                defeatedBadgeText.outlineWidth = 0.35f;
                defeatedBadgeText.outlineColor = new Color32(20, 10, 10, 255);
                defeatedBadgeText.raycastTarget = false;

                var defRt = defObj.GetComponent<RectTransform>();
                defRt.anchorMin = Vector2.zero;
                defRt.anchorMax = Vector2.one;
                defRt.offsetMin = Vector2.zero;
                defRt.offsetMax = Vector2.zero;
                defObj.SetActive(false);
            }
        }

        private static Sprite CreateBoxSprite()
        {
            int w = 16;
            int h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color solid = Color.white;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, solid);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(2, 2, 2, 2));
        }

        private void OnDisable()
        {
            Unbind();
        }
    }
}
