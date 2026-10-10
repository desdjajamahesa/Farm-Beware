using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PauseMenuAnimator brings the Farm Beware pause menu to life:
/// - Smooth modal pop-in (scale + fade) when pausing
/// - Subtle 2.5D mouse parallax on the parchment card
/// - Gentle floating bob and golden aura breathing on the 'FARM BEWARE' title plaque
/// - Operates smoothly at Time.timeScale = 0 using unscaledDeltaTime
/// </summary>
public class PauseMenuAnimator : MonoBehaviour
{
    [Header("Modal Card & Parallax")]
    [Tooltip("The central parchment card RectTransform.")]
    [SerializeField] private RectTransform cardRect;
    [Tooltip("Background container / overlay.")]
    [SerializeField] private RectTransform backgroundOverlayRect;
    [Tooltip("Parallax offset strength in pixels.")]
    [SerializeField] private float parallaxStrength = 8f;
    [SerializeField] private float parallaxSmoothSpeed = 5f;

    [Header("Title Plaque Animation")]
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private CanvasGroup titleGlowGroup;
    [SerializeField] private float titleFloatAmplitude = 4f;
    [SerializeField] private float titleFloatSpeed = 0.8f;
    [SerializeField] private float titleGlowMinAlpha = 0.25f;
    [SerializeField] private float titleGlowMaxAlpha = 0.75f;
    [SerializeField] private float titleGlowPulseSpeed = 1.4f;

    [Header("Open / Close Animation")]
    [SerializeField] private float popInDuration = 0.25f;
    [SerializeField] private Vector3 startScale = new Vector3(0.92f, 0.92f, 1f);

    private Vector2 cardInitialPos;
    private Vector3 cardInitialScale = Vector3.one;
    private Vector2 titleInitialPos;
    private Vector3 titleInitialScale = Vector3.one;
    private Vector2 targetParallaxOffset;
    private Vector2 currentParallaxOffset;

    private MainMenuController menuController;
    private Coroutine popInCoroutine;

    private void Awake()
    {
        menuController = GetComponent<MainMenuController>();
        ResolveReferences();

        if (cardRect != null)
        {
            cardInitialPos = cardRect.anchoredPosition;
            cardInitialScale = cardRect.localScale != Vector3.zero ? cardRect.localScale : Vector3.one;
        }

        if (titleRect != null)
        {
            titleInitialPos = titleRect.anchoredPosition;
            titleInitialScale = titleRect.localScale != Vector3.zero ? titleRect.localScale : Vector3.one;
        }
    }

    private void ResolveReferences()
    {
        if (cardRect == null)
        {
            var p = transform.Find("SignpostPanel");
            if (p != null) cardRect = p.GetComponent<RectTransform>();
        }

        if (backgroundOverlayRect == null)
        {
            var bg = transform.Find("BackgroundContainer");
            if (bg != null) backgroundOverlayRect = bg.GetComponent<RectTransform>();
        }

        if (titleRect == null)
        {
            var t = transform.Find("GameTitle");
            if (t == null && cardRect != null) t = cardRect.Find("GameTitle");
            if (t != null) titleRect = t.GetComponent<RectTransform>();
        }

        if (titleGlowGroup == null && titleRect != null)
        {
            var tg = titleRect.Find("TitleAuraGlow") ?? titleRect.Find("TitleBloodGlow") ?? titleRect.Find("TitleGlow");
            if (tg != null) titleGlowGroup = tg.GetComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        TriggerPopIn();
    }

    public void TriggerPopIn()
    {
        if (cardRect == null)
        {
            ResolveReferences();
            if (cardRect != null)
            {
                cardInitialPos = cardRect.anchoredPosition;
                cardInitialScale = cardRect.localScale != Vector3.zero ? cardRect.localScale : Vector3.one;
            }
        }

        if (cardRect != null)
        {
            if (popInCoroutine != null) StopCoroutine(popInCoroutine);
            popInCoroutine = StartCoroutine(AnimatePopIn());
        }
    }

    private IEnumerator AnimatePopIn()
    {
        float elapsed = 0f;
        cardRect.localScale = Vector3.Scale(cardInitialScale, startScale);

        while (elapsed < popInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popInDuration);
            // EaseOutBack
            float c1 = 1.4f;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            cardRect.localScale = Vector3.LerpUnclamped(Vector3.Scale(cardInitialScale, startScale), cardInitialScale, ease);
            yield return null;
        }

        cardRect.localScale = cardInitialScale;
        popInCoroutine = null;
    }

    private void Update()
    {
        if (menuController != null && !menuController.IsMenuActive)
            return;

        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        UpdateMouseParallax(dt);
        UpdateTitle(time);
    }

    private void UpdateMouseParallax(float dt)
    {
        if (parallaxStrength <= 0f || cardRect == null) return;

        Vector2 mouseScreen = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        else
            mouseScreen = Input.mousePosition;
#else
        mouseScreen = Input.mousePosition;
#endif

        float normX = Mathf.Clamp((mouseScreen.x / Screen.width - 0.5f) * 2f, -1f, 1f);
        float normY = Mathf.Clamp((mouseScreen.y / Screen.height - 0.5f) * 2f, -1f, 1f);

        targetParallaxOffset = new Vector2(normX * parallaxStrength, normY * parallaxStrength);
        currentParallaxOffset = Vector2.Lerp(currentParallaxOffset, targetParallaxOffset, dt * parallaxSmoothSpeed);

        cardRect.anchoredPosition = cardInitialPos + currentParallaxOffset;
    }

    private void UpdateTitle(float time)
    {
        if (titleRect != null)
        {
            float yOffset = Mathf.Sin(time * titleFloatSpeed * Mathf.PI * 2f) * titleFloatAmplitude;
            titleRect.anchoredPosition = titleInitialPos + new Vector2(0f, yOffset);
        }

        if (titleGlowGroup != null)
        {
            float pulse = (Mathf.Sin(time * titleGlowPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            titleGlowGroup.alpha = Mathf.Lerp(titleGlowMinAlpha, titleGlowMaxAlpha, pulse);
        }
    }
}
