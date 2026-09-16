using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HorrorMenuAnimator menghidupkan Main Menu Farm Beware dengan serangkaian animasi
/// atmosferik: mata merah berkedip di ladang jagung, kabut merayap, efek parallax mouse 2.5D,
/// getaran seram pada judul, dan ayunan plang tombol rustic.
/// </summary>
public class HorrorMenuAnimator : MonoBehaviour
{
    [System.Serializable]
    public class GlowingEye
    {
        public RectTransform eyeRect;
        public CanvasGroup canvasGroup;
        [HideInInspector] public Vector2 initialPos;
        [HideInInspector] public float nextBlinkTime;
        [HideInInspector] public bool isBlinking;
        [HideInInspector] public float baseAlpha = 0.9f;
        [HideInInspector] public float phaseOffset;
    }

    [Header("Background & Parallax")]
    [Tooltip("RectTransform gambar background utama.")]
    [SerializeField] private RectTransform backgroundRect;
    [Tooltip("Intensitas pergerakan background mengikuti kursor mouse (2.5D Parallax).")]
    [SerializeField] private float bgParallaxStrength = 18f;
    [Tooltip("Kecepatan smoothing pergerakan kursor.")]
    [SerializeField] private float parallaxSmoothSpeed = 4f;

    [Header("Title 'FARM BEWARE'")]
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private CanvasGroup titleGlowGroup;
    [SerializeField] private float titleFloatSpeed = 0.8f;
    [SerializeField] private float titleFloatAmount = 8f;
    [SerializeField] private float titleBreathSpeed = 1.2f;
    [SerializeField] private float titleBreathScale = 0.025f;
    [SerializeField] private float twitchIntervalMin = 4f;
    [SerializeField] private float twitchIntervalMax = 9f;

    [Header("Cornfield Glowing Eyes")]
    [SerializeField] private List<GlowingEye> glowingEyes = new List<GlowingEye>();
    [SerializeField] private float eyeBlinkDuration = 0.18f;
    [SerializeField] private float eyePulseSpeed = 1.5f;

    [Header("Drifting Fog Layers")]
    [SerializeField] private RectTransform[] fogLayers;
    [SerializeField] private float[] fogDriftSpeeds = new float[] { -25f, -40f, -15f };
    [SerializeField] private float fogWaveSpeed = 0.6f;
    [SerializeField] private float fogWaveHeight = 12f;
    [SerializeField] private float fogResetThreshold = 1400f;

    [Header("Rustic Buttons Sway")]
    [SerializeField] private RectTransform[] buttonRects;
    [SerializeField] private float buttonSwaySpeed = 1.2f;
    [SerializeField] private float buttonSwayAngle = 1.2f;
    [SerializeField] private float buttonFloatAmount = 4f;

    [Header("Ambient Lightning / Moonlight Flicker")]
    [SerializeField] private Image backgroundOverlayImage;
    [SerializeField] private float minLightningInterval = 12f;
    [SerializeField] private float maxLightningInterval = 28f;

    // Internal State
    private Vector2 bgInitialPos;
    private Vector2 titleInitialPos;
    private Vector3 titleInitialScale;
    private Vector2[] buttonsInitialPos;
    private Vector2[] fogInitialPos;
    private Vector2 targetParallaxOffset;
    private Vector2 currentParallaxOffset;
    private float nextTwitchTime;
    private bool isTwitching = false;
    private float nextLightningTime;
    private Coroutine lightningCoroutine;

    private void Awake()
    {
        if (backgroundRect != null) bgInitialPos = backgroundRect.anchoredPosition;
        if (titleRect != null)
        {
            titleInitialPos = titleRect.anchoredPosition;
            titleInitialScale = titleRect.localScale;
        }

        // Cache initial positions
        if (buttonRects != null && buttonRects.Length > 0)
        {
            buttonsInitialPos = new Vector2[buttonRects.Length];
            for (int i = 0; i < buttonRects.Length; i++)
            {
                if (buttonRects[i] != null) buttonsInitialPos[i] = buttonRects[i].anchoredPosition;
            }
        }

        if (fogLayers != null && fogLayers.Length > 0)
        {
            fogInitialPos = new Vector2[fogLayers.Length];
            for (int i = 0; i < fogLayers.Length; i++)
            {
                if (fogLayers[i] != null) fogInitialPos[i] = fogLayers[i].anchoredPosition;
            }
        }

        // Initialize eyes
        foreach (var eye in glowingEyes)
        {
            if (eye.eyeRect != null)
            {
                eye.initialPos = eye.eyeRect.anchoredPosition;
                eye.nextBlinkTime = Time.time + Random.Range(1f, 5f);
                eye.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
                if (eye.canvasGroup != null) eye.baseAlpha = eye.canvasGroup.alpha;
            }
        }

        nextTwitchTime = Time.time + Random.Range(twitchIntervalMin, twitchIntervalMax);
        nextLightningTime = Time.time + Random.Range(minLightningInterval, maxLightningInterval);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        UpdateMouseParallax(dt);
        UpdateTitleAnimation(time, dt);
        UpdateCornfieldEyes(time, dt);
        UpdateDriftingFog(time, dt);
        UpdateButtonsSway(time);
        UpdateLightningCycle(time);
    }

    #region 2.5D Mouse Parallax
    private void UpdateMouseParallax(float dt)
    {
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

        targetParallaxOffset = new Vector2(-normX * bgParallaxStrength, -normY * (bgParallaxStrength * 0.6f));
        currentParallaxOffset = Vector2.Lerp(currentParallaxOffset, targetParallaxOffset, dt * parallaxSmoothSpeed);

        if (backgroundRect != null)
        {
            backgroundRect.anchoredPosition = bgInitialPos + currentParallaxOffset;
        }
    }
    #endregion

    #region Title Animation
    private void UpdateTitleAnimation(float time, float dt)
    {
        if (titleRect == null) return;

        // Subtle floating and breathing
        float floatY = Mathf.Sin(time * titleFloatSpeed * Mathf.PI * 2f) * titleFloatAmount;
        float breathScale = 1f + Mathf.Sin(time * titleBreathSpeed * Mathf.PI * 2f) * titleBreathScale;

        // Occasional horror glitch / heartbeat twitch
        if (!isTwitching && time >= nextTwitchTime)
        {
            StartCoroutine(TitleHorrorTwitchRoutine());
        }

        if (!isTwitching)
        {
            titleRect.anchoredPosition = titleInitialPos + new Vector2(currentParallaxOffset.x * 0.3f, floatY + currentParallaxOffset.y * 0.3f);
            titleRect.localScale = titleInitialScale * breathScale;
            titleRect.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 0.5f) * 0.6f);
        }

        // Title glow pulse
        if (titleGlowGroup != null)
        {
            float glowAlpha = 0.35f + Mathf.Sin(time * titleBreathSpeed * Mathf.PI * 2f) * 0.25f;
            titleGlowGroup.alpha = glowAlpha;
        }
    }

    private IEnumerator TitleHorrorTwitchRoutine()
    {
        isTwitching = true;
        Vector2 twitchPos = titleRect.anchoredPosition;

        // Quick double heartbeat spasm
        for (int i = 0; i < 2; i++)
        {
            titleRect.localScale = titleInitialScale * 1.07f;
            titleRect.anchoredPosition = twitchPos + new Vector2(Random.Range(-5f, 5f), Random.Range(-4f, 4f));
            yield return new WaitForSecondsRealtime(0.04f);

            titleRect.localScale = titleInitialScale * 0.98f;
            titleRect.anchoredPosition = twitchPos;
            yield return new WaitForSecondsRealtime(0.05f);
        }

        titleRect.localScale = titleInitialScale;
        isTwitching = false;
        nextTwitchTime = Time.unscaledTime + Random.Range(twitchIntervalMin, twitchIntervalMax);
    }
    #endregion

    #region Cornfield Glowing Eyes
    private void UpdateCornfieldEyes(float time, float dt)
    {
        foreach (var eye in glowingEyes)
        {
            if (eye.eyeRect == null || eye.canvasGroup == null) continue;

            // Parallax with corn stalks
            eye.eyeRect.anchoredPosition = eye.initialPos + currentParallaxOffset * 0.85f;

            if (!eye.isBlinking)
            {
                // Sinusoidal breathing glow
                float pulse = Mathf.Sin(time * eyePulseSpeed + eye.phaseOffset) * 0.15f;
                eye.canvasGroup.alpha = Mathf.Clamp01(eye.baseAlpha + pulse);

                // Trigger blink
                if (time >= eye.nextBlinkTime)
                {
                    StartCoroutine(BlinkEyeRoutine(eye));
                }
            }
        }
    }

    private IEnumerator BlinkEyeRoutine(GlowingEye eye)
    {
        eye.isBlinking = true;
        float elapsed = 0f;
        float halfDur = eyeBlinkDuration * 0.5f;

        // Fade out (tutup mata)
        while (elapsed < halfDur)
        {
            elapsed += Time.unscaledDeltaTime;
            eye.canvasGroup.alpha = Mathf.Lerp(eye.baseAlpha, 0f, elapsed / halfDur);
            yield return null;
        }

        eye.canvasGroup.alpha = 0f;

        // Berkedip cepat atau mengintai sebentar dari kegelapan
        float closedDelay = (Random.value < 0.25f) ? Random.Range(0.8f, 2.0f) : Random.Range(0.08f, 0.2f);
        yield return new WaitForSecondsRealtime(closedDelay);

        // Sedikit geser posisi mata (seolah mengintip ke arah lain)
        eye.eyeRect.anchoredPosition = eye.initialPos + new Vector2(Random.Range(-2f, 2f), Random.Range(-1.5f, 1.5f));

        // Fade in (buka mata)
        elapsed = 0f;
        while (elapsed < halfDur)
        {
            elapsed += Time.unscaledDeltaTime;
            eye.canvasGroup.alpha = Mathf.Lerp(0f, eye.baseAlpha, elapsed / halfDur);
            yield return null;
        }

        eye.canvasGroup.alpha = eye.baseAlpha;
        eye.isBlinking = false;
        eye.nextBlinkTime = Time.unscaledTime + Random.Range(2.5f, 7.5f);
    }
    #endregion

    #region Drifting Fog
    private void UpdateDriftingFog(float time, float dt)
    {
        if (fogLayers == null || fogLayers.Length == 0) return;

        for (int i = 0; i < fogLayers.Length; i++)
        {
            var fog = fogLayers[i];
            if (fog == null) continue;

            float speed = (i < fogDriftSpeeds.Length) ? fogDriftSpeeds[i] : -20f;
            Vector2 pos = fog.anchoredPosition;

            // Horizontal drift
            pos.x += speed * dt;

            // Vertical soft wave
            float wave = Mathf.Sin(time * fogWaveSpeed + i * 1.7f) * fogWaveHeight;
            pos.y = fogInitialPos[i].y + wave;

            // Reset loop
            if (speed < 0 && pos.x < -fogResetThreshold)
            {
                pos.x += fogResetThreshold * 2f;
            }
            else if (speed > 0 && pos.x > fogResetThreshold)
            {
                pos.x -= fogResetThreshold * 2f;
            }

            // Apply Parallax to fog
            fog.anchoredPosition = pos + new Vector2(currentParallaxOffset.x * (0.6f + i * 0.2f), currentParallaxOffset.y * 0.4f);
        }
    }
    #endregion

    #region Rustic Buttons Sway
    private void UpdateButtonsSway(float time)
    {
        if (buttonRects == null || buttonsInitialPos == null) return;

        for (int i = 0; i < buttonRects.Length; i++)
        {
            var btn = buttonRects[i];
            if (btn == null || i >= buttonsInitialPos.Length) continue;

            // Subtle pendular sign sway
            float sway = Mathf.Sin(time * buttonSwaySpeed + i * 1.3f) * buttonSwayAngle;
            btn.localRotation = Quaternion.Euler(0, 0, sway);

            // Subtle vertical float
            float floatY = Mathf.Sin(time * (buttonSwaySpeed * 1.2f) + i * 2.1f) * buttonFloatAmount;
            Vector2 targetPos = buttonsInitialPos[i] + new Vector2(currentParallaxOffset.x * 0.4f, floatY + currentParallaxOffset.y * 0.4f);
            btn.anchoredPosition = targetPos;
        }
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

        Color originalColor = backgroundOverlayImage.color;
        Color flashColor = new Color(0.85f, 0.88f, 1f, 0.45f);

        // Flash 1
        backgroundOverlayImage.color = flashColor;
        yield return new WaitForSecondsRealtime(0.06f);
        backgroundOverlayImage.color = originalColor;
        yield return new WaitForSecondsRealtime(0.08f);

        // Flash 2 (aftershock)
        backgroundOverlayImage.color = new Color(0.75f, 0.78f, 0.95f, 0.25f);
        yield return new WaitForSecondsRealtime(0.08f);
        backgroundOverlayImage.color = originalColor;

        lightningCoroutine = null;
    }
    #endregion
}
