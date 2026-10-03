using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SettingsButtonAnimator provides juicy tactile micro-animations for settings buttons:
/// - Snappy spring punch bounce on click
/// - Smooth hover scale and glow highlight
/// - Procedural audio click / chime feedback
/// - Active vs Muted state tinting
/// </summary>
public class SettingsButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public enum AudioFeedbackType { None, MusicChime, SoundTone, SFXPop, ButtonClick }

    [Header("Audio Feedback")]
    [SerializeField] private AudioFeedbackType soundType = AudioFeedbackType.ButtonClick;

    [Header("Hover & Press Dynamics")]
    [SerializeField] private float hoverScale = 1.10f;
    [SerializeField] private float pressScale = 0.88f;
    [SerializeField] private float punchScale = 1.20f;
    [SerializeField] private float animSpeed = 14f;

    [Header("Active / Muted State")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color mutedColor = new Color(0.6f, 0.6f, 0.6f, 0.85f);

    [Header("Hover Highlight Option")]
    [SerializeField] private bool fadeOnHover = false;
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.95f, 0.7f, 0.85f);

    private Vector3 originalScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private Color targetColor = Color.white;
    private bool isHovered = false;
    private bool isPressed = false;
    private bool isMuted = false;
    private Coroutine punchCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
        targetScale = originalScale;

        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetImage != null && !fadeOnHover && activeColor == Color.white)
            activeColor = targetImage.color;

        targetColor = fadeOnHover ? idleColor : (isMuted ? mutedColor : activeColor);
        if (fadeOnHover && targetImage != null) targetImage.color = idleColor;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
        if (fadeOnHover && targetImage != null)
        {
            targetColor = idleColor;
            targetImage.color = idleColor;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (punchCoroutine == null)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, dt * animSpeed);
        }

        if (fadeOnHover && targetImage != null)
        {
            targetImage.color = Color.Lerp(targetImage.color, targetColor, dt * animSpeed);
        }
    }

    public void SetFadeOnHover(bool enable, Color idle, Color hover)
    {
        fadeOnHover = enable;
        idleColor = idle;
        hoverColor = hover;
        targetColor = isHovered ? hoverColor : idleColor;
        if (targetImage != null) targetImage.color = targetColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed && punchCoroutine == null)
        {
            targetScale = originalScale * hoverScale;
        }
        if (fadeOnHover) targetColor = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed && punchCoroutine == null)
        {
            targetScale = originalScale;
        }
        if (fadeOnHover) targetColor = idleColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = true;
        targetScale = originalScale * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = false;
        targetScale = isHovered ? originalScale * hoverScale : originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        PlayAudioFeedback();
        TriggerPunchBounce();
    }

    public void TriggerPunchBounce()
    {
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchBounceRoutine());
    }

    public void SetMutedState(bool muted)
    {
        isMuted = muted;
        if (targetImage != null)
        {
            targetImage.color = muted ? mutedColor : activeColor;
        }
    }

    public void SetAudioFeedbackType(AudioFeedbackType type)
    {
        soundType = type;
    }

    private IEnumerator PunchBounceRoutine()
    {
        float elapsed = 0f;
        float duration = 0.22f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Elastic overshoot bounce
            float scaleMult = 1f + Mathf.Sin(t * Mathf.PI) * (punchScale - 1f) * Mathf.Exp(-t * 2.5f);
            transform.localScale = originalScale * scaleMult;
            yield return null;
        }

        transform.localScale = targetScale;
        punchCoroutine = null;
    }

    private void PlayAudioFeedback()
    {
        if (soundType == AudioFeedbackType.None) return;

        AudioSource source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        AudioClip clip = GetAudioClip(soundType);
        if (clip != null)
        {
            source.PlayOneShot(clip, 0.65f);
        }
    }

    private static AudioClip musicClip;
    private static AudioClip soundClip;
    private static AudioClip sfxClip;
    private static AudioClip clickClip;

    private static AudioClip GetAudioClip(AudioFeedbackType type)
    {
        switch (type)
        {
            case AudioFeedbackType.MusicChime:
                return musicClip ?? (musicClip = CreateProceduralTone(587.33f, 880f, 0.25f));
            case AudioFeedbackType.SoundTone:
                return soundClip ?? (soundClip = CreateProceduralTone(523.25f, 659.25f, 0.22f));
            case AudioFeedbackType.SFXPop:
                return sfxClip ?? (sfxClip = CreateProceduralPop());
            case AudioFeedbackType.ButtonClick:
            default:
                return clickClip ?? (clickClip = CreateProceduralPop(800f, 0.08f));
        }
    }

    private static AudioClip CreateProceduralTone(float f1, float f2, float dur)
    {
        int sampleRate = 44100;
        int count = (int)(sampleRate * dur);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 8f);
            samples[i] = (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.4f) * env * 0.4f;
        }
        var clip = AudioClip.Create("BtnTone_" + f1, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateProceduralPop(float freq = 650f, float dur = 0.12f)
    {
        int sampleRate = 44100;
        int count = (int)(sampleRate * dur);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float pitch = freq * (1f - t / dur * 0.6f);
            float env = Mathf.Exp(-t * 18f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * pitch * t) * env * 0.5f;
        }
        var clip = AudioClip.Create("BtnPop_" + freq, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
