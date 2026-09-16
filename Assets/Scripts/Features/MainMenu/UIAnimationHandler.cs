using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAnimationHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.15f;
    [SerializeField] private Color hoverTintColor = new Color(1.15f, 1.15f, 1.15f, 1f);

    [Header("Press")]
    [SerializeField] private float pressScale = 0.92f;
    [SerializeField] private float pressDuration = 0.08f;

    private Vector3 originalScale;
    private Color originalColor;
    private Coroutine currentAnim;
    private Coroutine colorAnim;
    private bool isHovered = false;
    private bool isPressed = false;
    private Image targetImage;

    void Awake()
    {
        originalScale = transform.localScale;
        targetImage = GetComponent<Image>();
        if (targetImage != null) originalColor = targetImage.color;
    }

    void OnEnable()
    {
        originalScale = transform.localScale;
        if (targetImage != null) originalColor = targetImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPressed) return;
        isHovered = true;
        AnimateToScale(originalScale * hoverScale, hoverDuration);
        if (targetImage != null)
            AnimateColor(originalColor * hoverTintColor, hoverDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            AnimateToScale(originalScale, hoverDuration);
            if (targetImage != null)
                AnimateColor(originalColor, hoverDuration);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        AnimateToScale(originalScale * pressScale, pressDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        Vector3 target = isHovered ? originalScale * hoverScale : originalScale;
        AnimateToScale(target, pressDuration);
    }

    private void AnimateToScale(Vector3 target, float duration)
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateScaleRoutine(target, duration));
    }

    private void AnimateColor(Color target, float duration)
    {
        if (colorAnim != null) StopCoroutine(colorAnim);
        colorAnim = StartCoroutine(AnimateColorRoutine(target, duration));
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
        currentAnim = null;
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
