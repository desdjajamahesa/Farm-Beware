using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HorrorMenuAnimator brings the Farm Beware pause menu to life with atmospheric horror effects:
/// - Cohesive 2.5D mouse parallax where background, pumpkin, and scarecrow move in unified lock-step
/// - Realistic multi-frequency candle flame flicker inside the pumpkin scarecrow's carved eyes and mouth
/// - Living organic scarecrow breath (subtle whole-scarecrow pulse)
/// - Sinister title horror heartbeat and cursed twitch
/// - Ambient distant lightning flashes
/// </summary>
public class HorrorMenuAnimator : MonoBehaviour
{
    [Header("Background & Parallax")]
    [Tooltip("Main background RectTransform.")]
    [SerializeField] private RectTransform backgroundRect;
    [Tooltip("Signpost RectTransform holding all plank buttons.")]
    [SerializeField] private RectTransform signpostPanel;
    [Tooltip("Intensity of mouse parallax movement.")]
    [SerializeField] private float bgParallaxStrength = 14f;
    [Tooltip("Smoothing speed for parallax.")]
    [SerializeField] private float parallaxSmoothSpeed = 4.5f;

    [Header("Title 'HAVE YOU GIVEN UP?'")]
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private CanvasGroup titleGlowGroup;
    [SerializeField] private float titleBreathSpeed = 0.8f;
    [SerializeField] private float titleBreathScale = 0.015f;
    [SerializeField] private float twitchIntervalMin = 5f;
    [SerializeField] private float twitchIntervalMax = 11f;

    [Header("Pumpkin Scarecrow Head")]
    [SerializeField] private RectTransform pumpkinRect;
    [SerializeField] private CanvasGroup pumpkinGlowGroup;
    [SerializeField] private float pumpkinFlameFlickerSpeed = 7f;

    [Header("Cornfield Lurker Eyes")]
    [SerializeField] private RectTransform cornfieldEyesRect;
    [SerializeField] private CanvasGroup cornfieldEyesGroup;
    [SerializeField] private float eyePulseSpeed = 1.8f;
    [SerializeField] private float eyeBlinkIntervalMin = 3.5f;
    [SerializeField] private float eyeBlinkIntervalMax = 7.5f;

    [Header("Scarecrow Organic Breath")]
    [SerializeField] private float scarecrowBreathSpeed = 0.7f;
    [SerializeField] private float scarecrowBreathAmount = 0.008f;

    [Header("Ambient Lightning / Moonlight Flicker")]
    [SerializeField] private Image backgroundOverlayImage;
    [SerializeField] private float minLightningInterval = 14f;
    [SerializeField] private float maxLightningInterval = 26f;

    // Internal State
    private Vector2 bgInitialPos;
    private Vector2 titleInitialPos;
    private Vector3 titleInitialScale;
    private Vector2 pumpkinInitialPos;
    private Vector2 signpostInitialPos;
    private Vector3 signpostInitialScale;
    private Vector2 cornfieldEyesInitialPos;
    private Vector2 targetParallaxOffset;
    private Vector2 currentParallaxOffset;
    private float nextTwitchTime;
    private bool isTwitching = false;
    private float nextLightningTime;
    private Coroutine lightningCoroutine;
    private float nextEyeBlinkTime;
    private bool isEyeBlinking = false;
    private MainMenuController menuController;

    private void Awake()
    {
        menuController = GetComponent<MainMenuController>();
        if (backgroundRect != null) bgInitialPos = backgroundRect.anchoredPosition;
        if (titleRect != null)
        {
            titleInitialPos = titleRect.anchoredPosition;
            titleInitialScale = titleRect.localScale;
            if (titleGlowGroup == null)
            {
                var tg = titleRect.GetComponentInChildren<CanvasGroup>();
                if (tg != null) titleGlowGroup = tg;
            }
        }
        if (pumpkinRect != null)
        {
            pumpkinInitialPos = pumpkinRect.anchoredPosition;
            if (pumpkinGlowGroup == null)
            {
                var pg = pumpkinRect.GetComponentInChildren<CanvasGroup>();
                if (pg != null) pumpkinGlowGroup = pg;
            }
        }
        if (signpostPanel == null)
        {
            var sp = transform.Find("SignpostPanel");
            if (sp != null) signpostPanel = sp.GetComponent<RectTransform>();
        }
        if (signpostPanel != null)
        {
            signpostInitialPos = signpostPanel.anchoredPosition;
            signpostInitialScale = signpostPanel.localScale;
        }

        if (cornfieldEyesRect == null)
        {
            var eyes = transform.Find("CornfieldLurkerEyes");
            if (eyes == null) eyes = transform.Find("BackgroundContainer/CornfieldLurkerEyes");
            if (eyes != null) cornfieldEyesRect = eyes.GetComponent<RectTransform>();
        }
        if (cornfieldEyesRect != null)
        {
            cornfieldEyesInitialPos = cornfieldEyesRect.anchoredPosition;
            if (cornfieldEyesGroup == null)
                cornfieldEyesGroup = cornfieldEyesRect.GetComponent<CanvasGroup>();
        }

        nextTwitchTime = Time.unscaledTime + Random.Range(twitchIntervalMin, twitchIntervalMax);
        nextLightningTime = Time.unscaledTime + Random.Range(minLightningInterval, maxLightningInterval);
        nextEyeBlinkTime = Time.unscaledTime + Random.Range(eyeBlinkIntervalMin, eyeBlinkIntervalMax);
    }

    private void Update()
    {
        if (menuController != null && !menuController.IsMenuActive)
            return;

        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        UpdateMouseParallax(dt);
        UpdateTitleAnimation(time);
        UpdatePumpkinAnimation(time);
        UpdateCornfieldEyes(time, dt);
        UpdateScarecrowBreath(time);
        UpdateLightningCycle(time);
    }

    #region Unified 2.5D Mouse Parallax
    private void UpdateMouseParallax(float dt)
    {
        if (bgParallaxStrength <= 0f) return;
        Vector2 mouseScreen = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
        else
        {
            mouseScreen = Input.mousePosition;
        }
#else
        mouseScreen = Input.mousePosition;
#endif

        float normX = (mouseScreen.x / Screen.width - 0.5f) * 2f;
        float normY = (mouseScreen.y / Screen.height - 0.5f) * 2f;
        normX = Mathf.Clamp(normX, -1f, 1f);
        normY = Mathf.Clamp(normY, -1f, 1f);

        targetParallaxOffset = new Vector2(-normX * bgParallaxStrength, -normY * (bgParallaxStrength * 0.55f));
        currentParallaxOffset = Vector2.Lerp(currentParallaxOffset, targetParallaxOffset, dt * parallaxSmoothSpeed);

        // Move background, scarecrow signpost, pumpkin head, and title in unified 1:1 synchronization
        // This ensures the button hitboxes NEVER desync from the painted planks on the background!
        if (backgroundRect != null)
            backgroundRect.anchoredPosition = bgInitialPos + currentParallaxOffset;

        if (signpostPanel != null)
            signpostPanel.anchoredPosition = signpostInitialPos + currentParallaxOffset;

        if (pumpkinRect != null)
            pumpkinRect.anchoredPosition = pumpkinInitialPos + currentParallaxOffset;

        if (titleRect != null && !isTwitching)
            titleRect.anchoredPosition = titleInitialPos + currentParallaxOffset * 0.85f;

        if (cornfieldEyesRect != null)
            cornfieldEyesRect.anchoredPosition = cornfieldEyesInitialPos + currentParallaxOffset;
    }
    #endregion

    #region Cornfield Lurker Eyes
    private void UpdateCornfieldEyes(float time, float dt)
    {
        if (cornfieldEyesGroup == null) return;

        if (!isEyeBlinking)
        {
            // Sinusoidal eerie breathing pulse
            float pulse = Mathf.Sin(time * eyePulseSpeed * Mathf.PI * 2f) * 0.22f;
            cornfieldEyesGroup.alpha = Mathf.Clamp01(0.75f + pulse);

            if (time >= nextEyeBlinkTime)
            {
                StartCoroutine(EyeBlinkRoutine());
            }
        }
    }

    private IEnumerator EyeBlinkRoutine()
    {
        isEyeBlinking = true;
        if (cornfieldEyesGroup == null) yield break;

        // Quick eerie blink
        float duration = 0.08f;
        float elapsed = 0f;
        float startAlpha = cornfieldEyesGroup.alpha;

        // Close eyes
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cornfieldEyesGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }
        cornfieldEyesGroup.alpha = 0f;

        // Brief delay while closed or watching
        float stayClosed = (Random.value < 0.3f) ? Random.Range(0.4f, 1.2f) : Random.Range(0.06f, 0.15f);
        yield return new WaitForSecondsRealtime(stayClosed);

        // Open eyes
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cornfieldEyesGroup.alpha = Mathf.Lerp(0f, 0.9f, elapsed / duration);
            yield return null;
        }
        cornfieldEyesGroup.alpha = 0.9f;

        isEyeBlinking = false;
        nextEyeBlinkTime = Time.unscaledTime + Random.Range(eyeBlinkIntervalMin, eyeBlinkIntervalMax);
    }
    #endregion

    #region Pumpkin Flame Flicker
    private void UpdatePumpkinAnimation(float time)
    {
        if (pumpkinGlowGroup != null)
        {
            // Organic multi-frequency candlelight Perlin noise
            float flameNoise1 = Mathf.PerlinNoise(time * pumpkinFlameFlickerSpeed, 0.4f);
            float flameNoise2 = Mathf.PerlinNoise(time * 19f, 2.1f) * 0.22f;
            float flamePulse = Mathf.Sin(time * 2.3f) * 0.10f;
            pumpkinGlowGroup.alpha = Mathf.Clamp01(0.62f + flameNoise1 * 0.35f + flameNoise2 + flamePulse);
        }
    }
    #endregion

    #region Scarecrow Organic Living Breath
    private void UpdateScarecrowBreath(float time)
    {
        if (signpostPanel == null) return;

        // Slow, organic subtle breathing of the scarecrow entity as a whole
        float breath = 1f + Mathf.Sin(time * scarecrowBreathSpeed * Mathf.PI * 2f) * scarecrowBreathAmount;
        signpostPanel.localScale = new Vector3(signpostInitialScale.x * breath, signpostInitialScale.y * breath, 1f);
    }
    #endregion

    #region Title Horror Pulse & Twitch
    private void UpdateTitleAnimation(float time)
    {
        // Sinister slow breathing on title
        if (titleRect != null && !isTwitching)
        {
            float breath = 1f + Mathf.Sin(time * titleBreathSpeed * Mathf.PI * 2f) * titleBreathScale;
            titleRect.localScale = titleInitialScale * breath;
        }

        // Title blood aura pulse
        if (titleGlowGroup != null && !isTwitching)
        {
            float glowAlpha = 0.45f + Mathf.Sin(time * titleBreathSpeed * Mathf.PI * 2f) * 0.25f;
            titleGlowGroup.alpha = glowAlpha;
        }

        // Occasional creepy twitch / shudder
        if (!isTwitching && time >= nextTwitchTime)
        {
            StartCoroutine(TitleHorrorTwitchRoutine());
        }
    }

    private IEnumerator TitleHorrorTwitchRoutine()
    {
        isTwitching = true;
        Vector2 twitchBasePos = titleInitialPos + currentParallaxOffset * 0.85f;

        if (titleRect != null)
            titleRect.anchoredPosition = twitchBasePos + new Vector2(Random.Range(-3f, 3f), Random.Range(-2f, 2f));
        if (titleGlowGroup != null)
            titleGlowGroup.alpha = 1.0f;

        yield return new WaitForSecondsRealtime(0.04f);

        if (titleRect != null)
            titleRect.anchoredPosition = twitchBasePos + new Vector2(Random.Range(-2f, 2f), Random.Range(-1.5f, 1.5f));
        if (titleGlowGroup != null)
            titleGlowGroup.alpha = 0.25f;

        yield return new WaitForSecondsRealtime(0.04f);

        if (titleRect != null)
            titleRect.anchoredPosition = twitchBasePos;
        if (titleGlowGroup != null)
            titleGlowGroup.alpha = 0.7f;

        yield return new WaitForSecondsRealtime(0.04f);

        isTwitching = false;
        nextTwitchTime = Time.unscaledTime + Random.Range(twitchIntervalMin, twitchIntervalMax);
    }
    #endregion

    #region Ambient Lightning
    private void UpdateLightningCycle(float time)
    {
        if (time >= nextLightningTime && lightningCoroutine == null)
        {
            lightningCoroutine = StartCoroutine(LightningFlickerRoutine());
            nextLightningTime = Time.unscaledTime + Random.Range(minLightningInterval, maxLightningInterval);
        }
    }

    private IEnumerator LightningFlickerRoutine()
    {
        if (backgroundOverlayImage == null) yield break;

        Color originalColor = new Color(0f, 0f, 0.05f, 0.55f);
        Color flashColor = new Color(0.82f, 0.85f, 1f, 0.40f);

        // First sharp strike
        backgroundOverlayImage.color = flashColor;
        yield return new WaitForSecondsRealtime(0.05f);
        backgroundOverlayImage.color = originalColor;
        yield return new WaitForSecondsRealtime(0.07f);

        // Second rolling rumble strike
        backgroundOverlayImage.color = new Color(0.72f, 0.76f, 0.95f, 0.22f);
        yield return new WaitForSecondsRealtime(0.09f);
        backgroundOverlayImage.color = originalColor;

        lightningCoroutine = null;
    }
    #endregion
}
