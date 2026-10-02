using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FantasyMenuAnimator brings the Farm Beware dual-realm main menu to life:
/// - Smooth 2.5D mouse parallax with multi-plane depth (Background, Portal, Signpost/Buttons, Title)
/// - Ethereal pulse and gentle rotation on the central rift vortex and cosmic galaxy
/// - Graceful floating bob and golden aura breathing on the 'FARM BEWARE' title plaque
/// - Gentle organic breeze sway for individual wooden signboards
/// - Interactive hover glows and responsive button micro-animations
/// </summary>
public class FantasyMenuAnimator : MonoBehaviour
{
    [Header("Parallax Depth Planes")]
    [Tooltip("Main background RectTransform.")]
    [SerializeField] private RectTransform backgroundRect;
    [Tooltip("Signpost & buttons panel RectTransform.")]
    [SerializeField] private RectTransform signpostPanel;
    [Tooltip("Title banner RectTransform.")]
    [SerializeField] private RectTransform titleRect;
    [Tooltip("Background parallax strength (max pixel offset).")]
    [SerializeField] private float bgParallaxStrength = 16f;
    [Tooltip("Foreground signpost parallax strength.")]
    [SerializeField] private float fgParallaxStrength = 8f;
    [Tooltip("Title banner parallax strength.")]
    [SerializeField] private float titleParallaxStrength = 6f;
    [Tooltip("Parallax smoothing speed.")]
    [SerializeField] private float parallaxSmoothSpeed = 5f;

    [Header("Title Plaque Animation")]
    [SerializeField] private CanvasGroup titleGlowGroup;
    [SerializeField] private float titleFloatAmplitude = 6f;
    [SerializeField] private float titleFloatSpeed = 0.85f;
    [SerializeField] private float titleGlowPulseSpeed = 1.6f;
    [SerializeField] private float titleGlowMin = 0.20f;
    [SerializeField] private float titleGlowMax = 0.60f;
    [SerializeField] private float titleScaleBreathAmount = 0.015f;
    [SerializeField] private float titleScaleBreathSpeed = 0.9f;

    [Header("Magical Portal & Cosmic Galaxy")]
    [SerializeField] private RectTransform vortexRect;
    [SerializeField] private CanvasGroup vortexCanvasGroup;
    [SerializeField] private float vortexPulseSpeed = 1.4f;
    [SerializeField] private float vortexPulseMin = 0.35f;
    [SerializeField] private float vortexPulseMax = 0.85f;
    [SerializeField] private float vortexRotateSpeed = 2.0f; // subtle slow rotation

    [SerializeField] private RectTransform galaxyRect;
    [SerializeField] private CanvasGroup galaxyCanvasGroup;
    [SerializeField] private float galaxyPulseSpeed = 1.0f;
    [SerializeField] private float galaxyPulseMin = 0.30f;
    [SerializeField] private float galaxyPulseMax = 0.75f;

    [Header("Wooden Signs Breeze Sway")]
    [SerializeField] private RectTransform playButtonRect;
    [SerializeField] private RectTransform settingsButtonRect;
    [SerializeField] private RectTransform exitButtonRect;
    [SerializeField] private float swayAngleMax = 1.2f;
    [SerializeField] private float swaySpeed = 1.3f;

    // Internal State
    private Vector2 bgInitialPos;
    private Vector2 signpostInitialPos;
    private Vector2 titleInitialPos;
    private Vector3 titleInitialScale;

    private Vector2 targetParallaxOffset;
    private Vector2 currentParallaxOffset;

    private MainMenuController menuController;

    private void Awake()
    {
        menuController = GetComponent<MainMenuController>();

        if (backgroundRect != null) bgInitialPos = backgroundRect.anchoredPosition;
        if (signpostPanel != null) signpostInitialPos = signpostPanel.anchoredPosition;
        if (titleRect != null)
        {
            titleInitialPos = titleRect.anchoredPosition;
            titleInitialScale = titleRect.localScale;
        }

        ResolveReferencesIfNull();
    }

    private void ResolveReferencesIfNull()
    {
        if (backgroundRect == null)
        {
            var bg = transform.Find("BackgroundContainer/BackgroundImage");
            if (bg != null) backgroundRect = bg.GetComponent<RectTransform>();
        }

        if (signpostPanel == null)
        {
            var sp = transform.Find("MainMenuPanel");
            if (sp != null) signpostPanel = sp.GetComponent<RectTransform>();
        }

        if (titleRect == null)
        {
            var t = transform.Find("GameTitle");
            if (t != null) titleRect = t.GetComponent<RectTransform>();
        }

        if (titleGlowGroup == null && titleRect != null)
        {
            var tg = titleRect.Find("TitleGlow");
            if (tg != null) titleGlowGroup = tg.GetComponent<CanvasGroup>();
        }

        if (vortexRect == null)
        {
            var v = transform.Find("BackgroundContainer/VortexGlow");
            if (v != null)
            {
                vortexRect = v.GetComponent<RectTransform>();
                vortexCanvasGroup = v.GetComponent<CanvasGroup>();
            }
        }

        if (galaxyRect == null)
        {
            var g = transform.Find("BackgroundContainer/GalaxyGlow");
            if (g != null)
            {
                galaxyRect = g.GetComponent<RectTransform>();
                galaxyCanvasGroup = g.GetComponent<CanvasGroup>();
            }
        }

        if (playButtonRect == null && signpostPanel != null)
        {
            var b = signpostPanel.Find("StartButton");
            if (b != null) playButtonRect = b.GetComponent<RectTransform>();
        }

        if (settingsButtonRect == null && signpostPanel != null)
        {
            var b = signpostPanel.Find("SettingsButton");
            if (b != null) settingsButtonRect = b.GetComponent<RectTransform>();
        }

        if (exitButtonRect == null && signpostPanel != null)
        {
            var b = signpostPanel.Find("QuitButton");
            if (b != null) exitButtonRect = b.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        if (backgroundRect != null && bgInitialPos == Vector2.zero) bgInitialPos = backgroundRect.anchoredPosition;
        if (signpostPanel != null && signpostInitialPos == Vector2.zero) signpostInitialPos = signpostPanel.anchoredPosition;
        if (titleRect != null && titleInitialPos == Vector2.zero)
        {
            titleInitialPos = titleRect.anchoredPosition;
            titleInitialScale = titleRect.localScale != Vector3.zero ? titleRect.localScale : Vector3.one;
        }
    }

    private void Update()
    {
        if (menuController != null && !menuController.IsMenuActive)
            return;

        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        UpdateMouseParallax(dt);
        UpdateTitle(time);
        UpdateVortexAndGalaxy(time, dt);
        UpdateBreezeSway(time);
    }

    #region 2.5D Mouse Parallax
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

        targetParallaxOffset = new Vector2(-normX, -normY * 0.6f);
        currentParallaxOffset = Vector2.Lerp(currentParallaxOffset, targetParallaxOffset, dt * parallaxSmoothSpeed);

        if (backgroundRect != null)
            backgroundRect.anchoredPosition = bgInitialPos + currentParallaxOffset * bgParallaxStrength;

        if (signpostPanel != null)
            signpostPanel.anchoredPosition = signpostInitialPos + currentParallaxOffset * fgParallaxStrength;

        if (titleRect != null)
            titleRect.anchoredPosition = titleInitialPos + currentParallaxOffset * titleParallaxStrength;
    }
    #endregion

    #region Title Plaque Animation
    private void UpdateTitle(float time)
    {
        if (titleRect == null) return;

        // Gentle floating bob
        float yFloat = Mathf.Sin(time * titleFloatSpeed * Mathf.PI * 2f) * titleFloatAmplitude;
        Vector2 currentBase = titleInitialPos + currentParallaxOffset * titleParallaxStrength;
        titleRect.anchoredPosition = currentBase + new Vector2(0f, yFloat);

        // Subtle scale breathing
        float scaleBreath = 1f + Mathf.Sin(time * titleScaleBreathSpeed * Mathf.PI * 2f) * titleScaleBreathAmount;
        titleRect.localScale = titleInitialScale * scaleBreath;

        // Radiant golden aura pulse
        if (titleGlowGroup != null)
        {
            float t = (Mathf.Sin(time * titleGlowPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            titleGlowGroup.alpha = Mathf.Lerp(titleGlowMin, titleGlowMax, t);
        }
    }
    #endregion

    #region Magical Vortex & Cosmic Galaxy
    private void UpdateVortexAndGalaxy(float time, float dt)
    {
        // Vortex pulse & subtle swirl
        if (vortexCanvasGroup != null)
        {
            float t = (Mathf.Sin(time * vortexPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            vortexCanvasGroup.alpha = Mathf.Lerp(vortexPulseMin, vortexPulseMax, t);
        }

        if (vortexRect != null && vortexRotateSpeed != 0f)
        {
            vortexRect.Rotate(0f, 0f, vortexRotateSpeed * dt);
        }

        // Galaxy cosmic glow breathing
        if (galaxyCanvasGroup != null)
        {
            float t = (Mathf.Sin(time * galaxyPulseSpeed * Mathf.PI * 2f + 1.2f) + 1f) * 0.5f;
            galaxyCanvasGroup.alpha = Mathf.Lerp(galaxyPulseMin, galaxyPulseMax, t);
        }
    }
    #endregion

    #region Wooden Signs Breeze Sway
    private void UpdateBreezeSway(float time)
    {
        // Each wooden signboard sways gently with natural phase offsets
        if (playButtonRect != null)
        {
            float sway = Mathf.Sin(time * swaySpeed) * swayAngleMax;
            playButtonRect.localRotation = Quaternion.Euler(0f, 0f, sway);
        }

        if (settingsButtonRect != null)
        {
            float sway = Mathf.Sin(time * (swaySpeed * 0.95f) + 1.2f) * (swayAngleMax * 0.85f);
            settingsButtonRect.localRotation = Quaternion.Euler(0f, 0f, sway);
        }

        if (exitButtonRect != null)
        {
            float sway = Mathf.Sin(time * (swaySpeed * 1.08f) + 2.5f) * (swayAngleMax * 1.1f);
            exitButtonRect.localRotation = Quaternion.Euler(0f, 0f, sway);
        }
    }
    #endregion
}
