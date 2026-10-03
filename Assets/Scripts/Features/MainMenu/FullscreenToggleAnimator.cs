using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// FullscreenToggleAnimator provides fluid, tactile animations for the Fullscreen toggle switch:
/// - Smooth spring slide transition for the switch knob (EaseOutBack)
/// - Organic glow & color shift for the track/slot (Dim wood off -> Vibrant fantasy emerald on)
/// - Knob hover & click punch micro-animations
/// - Procedural toggle click sound feedback
/// </summary>
public class FullscreenToggleAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Toggle Components")]
    [SerializeField] private Toggle targetToggle;
    [SerializeField] private RectTransform knobRect;
    [SerializeField] private Image slotImage;

    [Header("Knob Positions")]
    [SerializeField] private float offPositionX = -20f;
    [SerializeField] private float onPositionX = 20f;
    [SerializeField] private float slideDuration = 0.24f;

    [Header("Colors")]
    [SerializeField] private Color offColor = new Color(0.18f, 0.12f, 0.08f, 0.95f);
    [SerializeField] private Color onColor = new Color(0.24f, 0.78f, 0.38f, 1.0f);
    [SerializeField] private Color hoverGlowAdd = new Color(0.15f, 0.15f, 0.15f, 0f);

    [Header("Knob Dynamics")]
    [SerializeField] private float hoverScale = 1.14f;
    [SerializeField] private float pressScale = 0.92f;
    [SerializeField] private float punchScale = 1.25f;

    private Vector3 knobOriginalScale = Vector3.one;
    private Vector3 knobTargetScale = Vector3.one;
    private bool isHovered = false;
    private bool isPressed = false;
    private Coroutine slideCoroutine;
    private Coroutine punchCoroutine;
    private static AudioClip toggleOnClip;
    private static AudioClip toggleOffClip;

    private void Awake()
    {
        if (targetToggle == null) targetToggle = GetComponent<Toggle>();
        ResolveReferences();

        if (knobRect != null)
        {
            knobOriginalScale = knobRect.localScale != Vector3.zero ? knobRect.localScale : Vector3.one;
            knobTargetScale = knobOriginalScale;
        }

        if (targetToggle != null)
        {
            targetToggle.onValueChanged.AddListener(OnToggleStateChanged);
            ApplyInstantState(targetToggle.isOn);
        }
    }

    private void OnDestroy()
    {
        if (targetToggle != null)
        {
            targetToggle.onValueChanged.RemoveListener(OnToggleStateChanged);
        }
    }

    private void ResolveReferences()
    {
        if (knobRect == null)
        {
            var knobTrans = transform.Find("Knob");
            if (knobTrans != null) knobRect = knobTrans as RectTransform;
        }

        if (slotImage == null)
        {
            var bgTrans = transform.Find("Background");
            if (bgTrans != null) slotImage = bgTrans.GetComponent<Image>();
            else slotImage = GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        if (targetToggle != null)
        {
            ApplyInstantState(targetToggle.isOn);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (knobRect != null && punchCoroutine == null)
        {
            knobRect.localScale = Vector3.Lerp(knobRect.localScale, knobTargetScale, dt * 14f);
        }
    }

    public void ApplyInstantState(bool isOn)
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);

        if (knobRect != null)
        {
            Vector2 pos = knobRect.anchoredPosition;
            pos.x = isOn ? onPositionX : offPositionX;
            knobRect.anchoredPosition = pos;
        }

        if (slotImage != null)
        {
            slotImage.color = isOn ? onColor : offColor;
        }
    }

    private void OnToggleStateChanged(bool isOn)
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideKnobRoutine(isOn));

        PlayToggleSound(isOn);
        TriggerKnobPunch();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed && punchCoroutine == null)
        {
            knobTargetScale = knobOriginalScale * hoverScale;
        }
        UpdateSlotHoverVisual(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed && punchCoroutine == null)
        {
            knobTargetScale = knobOriginalScale;
        }
        UpdateSlotHoverVisual(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = true;
        knobTargetScale = knobOriginalScale * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = false;
        knobTargetScale = isHovered ? knobOriginalScale * hoverScale : knobOriginalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Toggle component handles isOn transition, which fires OnToggleStateChanged
    }

    private void UpdateSlotHoverVisual(bool hovered)
    {
        if (slotImage == null || targetToggle == null) return;
        Color baseCol = targetToggle.isOn ? onColor : offColor;
        slotImage.color = hovered ? (baseCol + hoverGlowAdd) : baseCol;
    }

    public void TriggerKnobPunch()
    {
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchBounceRoutine());
    }

    private IEnumerator SlideKnobRoutine(bool targetState)
    {
        if (knobRect == null) yield break;

        float startX = knobRect.anchoredPosition.x;
        float endX = targetState ? onPositionX : offPositionX;
        Color startCol = slotImage != null ? slotImage.color : offColor;
        Color endCol = targetState ? onColor : offColor;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            // Elastic overshoot curve
            float easeT = EaseOutBack(t);
            float curX = Mathf.LerpUnclamped(startX, endX, easeT);

            Vector2 pos = knobRect.anchoredPosition;
            pos.x = curX;
            knobRect.anchoredPosition = pos;

            if (slotImage != null)
            {
                slotImage.color = Color.Lerp(startCol, endCol, t);
            }

            yield return null;
        }

        Vector2 finalPos = knobRect.anchoredPosition;
        finalPos.x = endX;
        knobRect.anchoredPosition = finalPos;

        if (slotImage != null)
        {
            slotImage.color = endCol;
        }

        slideCoroutine = null;
    }

    private IEnumerator PunchBounceRoutine()
    {
        if (knobRect == null) yield break;
        float elapsed = 0f;
        float duration = 0.20f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float punch = 1f + Mathf.Sin(t * Mathf.PI) * (punchScale - 1f) * Mathf.Exp(-t * 2.5f);
            knobRect.localScale = knobOriginalScale * punch;
            yield return null;
        }

        knobRect.localScale = knobTargetScale;
        punchCoroutine = null;
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private void PlayToggleSound(bool isOn)
    {
        AudioSource src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        if (isOn)
        {
            if (toggleOnClip == null) toggleOnClip = CreateTone(520f, 784f, 0.16f);
            src.PlayOneShot(toggleOnClip, 0.7f);
        }
        else
        {
            if (toggleOffClip == null) toggleOffClip = CreateTone(680f, 440f, 0.14f);
            src.PlayOneShot(toggleOffClip, 0.6f);
        }
    }

    private static AudioClip CreateTone(float fStart, float fEnd, float dur)
    {
        int sampleRate = 44100;
        int count = (int)(sampleRate * dur);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float p = t / dur;
            float freq = Mathf.Lerp(fStart, fEnd, p);
            float env = Mathf.Exp(-t * 14f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.45f;
        }
        var clip = AudioClip.Create("Toggle_" + fStart, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
