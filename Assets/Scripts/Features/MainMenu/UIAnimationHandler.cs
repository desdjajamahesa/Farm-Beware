using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles rich UI animations for buttons:
/// - Smooth amber candlelight flame breathing glow on hover (SelectionGlow)
/// - Sliding in, fading, and breathing indicator icon on hover (IndicatorIcon)
/// - Responsive tactile press and click punch
/// - Works flawlessly at Time.timeScale = 0 (unscaledDeltaTime)
/// </summary>
public class UIAnimationHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Hover Scale & Tint")]
    [SerializeField] private float hoverScale = 1.02f;
    [SerializeField] private float hoverDuration = 0.12f;
    [SerializeField] private Color hoverTintColor = new Color(1.15f, 1.15f, 1.15f, 1f);
    [SerializeField] private bool enableScaleAnimation = false;

    [Header("Hover Indicator & Glow")]
    [SerializeField] private CanvasGroup glowCanvasGroup;
    [SerializeField] private GameObject hoverIndicator;
    [SerializeField] private float glowFadeDuration = 0.14f;

    [Header("Press Animation")]
    [SerializeField] private bool enablePressAnimation = true;
    [SerializeField] private float pressScale = 0.97f;
    [SerializeField] private float pressDuration = 0.06f;

    private Vector3 originalScale;
    private Color originalColor;
    private Coroutine currentScaleAnim;
    private Coroutine colorAnim;
    private Coroutine glowAnim;
    private Coroutine indicatorAnim;
    private bool isHovered = false;
    private bool isPressed = false;
    private Image targetImage;

    // Indicator animation state
    private RectTransform indicatorRect;
    private CanvasGroup indicatorCanvasGroup;
    private Vector3 indicatorBasePos;
    private Vector3 indicatorOriginalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetImage = GetComponent<Image>();
        if (targetImage != null) originalColor = targetImage.color;
        
        InitGlow();
        InitIndicator();
    }

    void OnEnable()
    {
        originalScale = Vector3.one;
        transform.localScale = Vector3.one;
        if (targetImage != null) originalColor = targetImage.color;
        
        InitGlow();
        InitIndicator();
    }

    private void InitGlow()
    {
        if (glowCanvasGroup == null)
        {
            var glowT = transform.Find("SelectionGlow");
            if (glowT != null)
                glowCanvasGroup = glowT.GetComponent<CanvasGroup>();
        }

        if (glowCanvasGroup != null)
        {
            glowCanvasGroup.alpha = 0f;
            glowCanvasGroup.blocksRaycasts = false;
            glowCanvasGroup.interactable = false;
        }
    }

    private void InitIndicator()
    {
        if (hoverIndicator == null)
        {
            var iconT = transform.Find("IndicatorIcon");
            if (iconT != null)
                hoverIndicator = iconT.gameObject;
        }

        if (hoverIndicator != null)
        {
            indicatorRect = hoverIndicator.GetComponent<RectTransform>();
            if (indicatorRect != null)
            {
                indicatorBasePos = indicatorRect.localPosition;
                indicatorOriginalScale = indicatorRect.localScale;
            }

            indicatorCanvasGroup = hoverIndicator.GetComponent<CanvasGroup>();
            if (indicatorCanvasGroup == null)
                indicatorCanvasGroup = hoverIndicator.AddComponent<CanvasGroup>();

            indicatorCanvasGroup.alpha = 0f;
            indicatorCanvasGroup.blocksRaycasts = false;
            indicatorCanvasGroup.interactable = false;
            hoverIndicator.SetActive(false);
        }
    }

    void Update()
    {
        float time = Time.unscaledTime;

        // Subtle amber flame flicker while hovered
        if (isHovered && glowCanvasGroup != null)
        {
            float flameNoise = Mathf.PerlinNoise(time * 6.5f, 0.4f);
            float flamePulse = Mathf.Sin(time * 3.2f) * 0.08f;
            glowCanvasGroup.alpha = Mathf.Clamp01(0.82f + flameNoise * 0.18f + flamePulse);
        }

        // Indicator subtle breathing & floating while hovered
        if (isHovered && hoverIndicator != null && hoverIndicator.activeSelf && indicatorRect != null)
        {
            float floatX = Mathf.Sin(time * 3.5f) * 2.5f;
            indicatorRect.localPosition = indicatorBasePos + new Vector3(floatX, 0f, 0f);

            float scalePulse = 1f + Mathf.Sin(time * 4.0f) * 0.04f;
            indicatorRect.localScale = indicatorOriginalScale * scalePulse;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPressed) return;
        isHovered = true;

        if (enableScaleAnimation)
            AnimateToScale(originalScale * hoverScale, hoverDuration);

        if (targetImage != null)
            AnimateColor(originalColor * hoverTintColor, hoverDuration);

        if (glowCanvasGroup != null)
            AnimateGlow(0.95f, glowFadeDuration);

        AnimateIndicatorShow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            if (enableScaleAnimation)
                AnimateToScale(originalScale, hoverDuration);

            if (targetImage != null)
                AnimateColor(originalColor, hoverDuration);

            if (glowCanvasGroup != null)
                AnimateGlow(0f, glowFadeDuration);

            AnimateIndicatorHide();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = true;

        if (enablePressAnimation)
            AnimateToScale(originalScale * pressScale, pressDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = false;

        Vector3 target = (isHovered && enableScaleAnimation) ? originalScale * hoverScale : originalScale;
        if (enablePressAnimation)
            AnimateToScale(target, pressDuration);

        if (!isHovered)
        {
            if (glowCanvasGroup != null)
                AnimateGlow(0f, glowFadeDuration);
            AnimateIndicatorHide();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Snappy click punch bounce
        StartCoroutine(ClickPunchRoutine());
    }

    private IEnumerator ClickPunchRoutine()
    {
        float duration = 0.08f;
        float elapsed = 0f;
        Vector3 start = originalScale * pressScale;
        Vector3 target = originalScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            transform.localScale = Vector3.Lerp(start, target, eased);
            yield return null;
        }
        transform.localScale = target;
    }

    private void AnimateIndicatorShow()
    {
        if (hoverIndicator == null) return;
        hoverIndicator.SetActive(true);

        if (indicatorAnim != null) StopCoroutine(indicatorAnim);
        indicatorAnim = StartCoroutine(IndicatorShowRoutine());
    }

    private void AnimateIndicatorHide()
    {
        if (hoverIndicator == null || !hoverIndicator.activeSelf) return;

        if (indicatorAnim != null) StopCoroutine(indicatorAnim);
        indicatorAnim = StartCoroutine(IndicatorHideRoutine());
    }

    private IEnumerator IndicatorShowRoutine()
    {
        float duration = 0.16f;
        float elapsed = 0f;
        Vector3 startPos = indicatorBasePos + Vector3.right * 16f;

        if (indicatorRect != null) indicatorRect.localPosition = startPos;
        if (indicatorCanvasGroup != null) indicatorCanvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (indicatorRect != null)
                indicatorRect.localPosition = Vector3.Lerp(startPos, indicatorBasePos, eased);

            if (indicatorCanvasGroup != null)
                indicatorCanvasGroup.alpha = eased;

            yield return null;
        }

        if (indicatorRect != null) indicatorRect.localPosition = indicatorBasePos;
        if (indicatorCanvasGroup != null) indicatorCanvasGroup.alpha = 1f;
        indicatorAnim = null;
    }

    private IEnumerator IndicatorHideRoutine()
    {
        float duration = 0.12f;
        float elapsed = 0f;
        float startAlpha = indicatorCanvasGroup != null ? indicatorCanvasGroup.alpha : 1f;
        Vector3 startPos = indicatorRect != null ? indicatorRect.localPosition : indicatorBasePos;
        Vector3 endPos = indicatorBasePos + Vector3.right * 10f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (indicatorCanvasGroup != null)
                indicatorCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            if (indicatorRect != null)
                indicatorRect.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        if (indicatorCanvasGroup != null) indicatorCanvasGroup.alpha = 0f;
        if (indicatorRect != null)
        {
            indicatorRect.localPosition = indicatorBasePos;
            indicatorRect.localScale = indicatorOriginalScale;
        }
        hoverIndicator.SetActive(false);
        indicatorAnim = null;
    }

    private void AnimateGlow(float targetAlpha, float duration)
    {
        if (glowAnim != null) StopCoroutine(glowAnim);
        glowAnim = StartCoroutine(AnimateGlowRoutine(targetAlpha, duration));
    }

    private IEnumerator AnimateGlowRoutine(float targetAlpha, float duration)
    {
        if (glowCanvasGroup == null) yield break;
        float startAlpha = glowCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            glowCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            yield return null;
        }
        glowCanvasGroup.alpha = targetAlpha;
        glowAnim = null;
    }

    private void AnimateToScale(Vector3 target, float duration)
    {
        if (currentScaleAnim != null) StopCoroutine(currentScaleAnim);
        currentScaleAnim = StartCoroutine(AnimateScaleRoutine(target, duration));
    }

    private IEnumerator AnimateScaleRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.Lerp(start, target, eased);
            yield return null;
        }
        transform.localScale = target;
        currentScaleAnim = null;
    }

    private void AnimateColor(Color target, float duration)
    {
        if (colorAnim != null) StopCoroutine(colorAnim);
        colorAnim = StartCoroutine(AnimateColorRoutine(target, duration));
    }

    private IEnumerator AnimateColorRoutine(Color target, float duration)
    {
        if (targetImage == null) yield break;
        Color start = targetImage.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            targetImage.color = Color.Lerp(start, target, eased);
            yield return null;
        }
        targetImage.color = target;
        colorAnim = null;
    }
}
