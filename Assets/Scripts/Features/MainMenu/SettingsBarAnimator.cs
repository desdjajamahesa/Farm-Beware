using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SettingsBarAnimator brings slider bars to life:
/// - Smooth animated fill transition (interpolates fill smoothly)
/// - Organic ambient glow pulse along the active bar
/// - Tactile interactive knob animations (hover expansion, drag pulse, release spring bounce)
/// </summary>
public class SettingsBarAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Slider References")]
    [SerializeField] private Slider targetSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform handleRect;

    [Header("Knob Scale Dynamics")]
    [SerializeField] private float hoverScale = 1.20f;
    [SerializeField] private float pressScale = 1.30f;
    [SerializeField] private float animSpeed = 12f;

    [Header("Bar Glow Breathing")]
    [SerializeField] private bool enableGlowBreathing = true;
    [SerializeField] private float glowSpeed = 2.2f;
    [SerializeField] private float glowMin = 0.88f;
    [SerializeField] private float glowMax = 1.12f;

    private Vector3 handleOriginalScale = Vector3.one;
    private Vector3 targetHandleScale = Vector3.one;
    private Color originalFillColor = Color.white;
    private bool isHovered = false;
    private bool isPressed = false;

    private void Awake()
    {
        if (targetSlider == null) targetSlider = GetComponent<Slider>();
        ResolveReferences();

        if (handleRect != null)
            handleOriginalScale = handleRect.localScale != Vector3.zero ? handleRect.localScale : Vector3.one;
        targetHandleScale = handleOriginalScale;

        if (fillImage != null)
            originalFillColor = fillImage.color;
    }

    private void ResolveReferences()
    {
        if (targetSlider == null) targetSlider = GetComponent<Slider>();

        if (fillImage == null && targetSlider != null && targetSlider.fillRect != null)
        {
            fillImage = targetSlider.fillRect.GetComponent<Image>();
        }

        if (handleRect == null && targetSlider != null && targetSlider.handleRect != null)
        {
            handleRect = targetSlider.handleRect;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        // Smooth handle scale transition
        if (handleRect != null)
        {
            handleRect.localScale = Vector3.Lerp(handleRect.localScale, targetHandleScale, dt * animSpeed);
        }

        // Ambient glow breathing on the active fill bar
        if (enableGlowBreathing && fillImage != null)
        {
            float pulse = Mathf.Lerp(glowMin, glowMax, (Mathf.Sin(time * glowSpeed * Mathf.PI * 2f) + 1f) * 0.5f);
            if (isHovered || isPressed) pulse *= 1.15f;

            fillImage.color = new Color(
                Mathf.Clamp01(originalFillColor.r * pulse),
                Mathf.Clamp01(originalFillColor.g * pulse),
                Mathf.Clamp01(originalFillColor.b * pulse),
                originalFillColor.a
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            targetHandleScale = handleOriginalScale * hoverScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            targetHandleScale = handleOriginalScale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetHandleScale = handleOriginalScale * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetHandleScale = isHovered ? handleOriginalScale * hoverScale : handleOriginalScale;
    }

    /// <summary>
    /// Snappy tactile punch bounce on the knob.
    /// </summary>
    public void TriggerKnobPunch()
    {
        StopAllCoroutines();
        StartCoroutine(KnobPunchRoutine());
    }

    private IEnumerator KnobPunchRoutine()
    {
        if (handleRect == null) yield break;
        float elapsed = 0f;
        float duration = 0.18f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.28f;
            handleRect.localScale = handleOriginalScale * punch;
            yield return null;
        }

        handleRect.localScale = targetHandleScale;
    }
}
