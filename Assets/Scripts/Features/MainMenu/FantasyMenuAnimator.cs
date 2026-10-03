using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using FeaturesCommon;

/// <summary>
/// FantasyMenuAnimator brings the Farm Beware dual-realm main menu to life:
/// - Smooth 2.5D mouse parallax with multi-plane depth (Background, Portal, Signpost/Buttons, Title)
/// - Ethereal pulse and gentle rotation on the central rift vortex and cosmic galaxy
/// - Graceful floating bob on the 'FARM BEWARE' title plaque
/// - Gentle organic breeze sway for individual wooden signboards
/// - Interactive hover glows and responsive button micro-animations
/// - Punchy & cinematic portal dive transition when clicking 'PLAY' to enter the game
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
    [SerializeField] private float titleFloatAmplitude = 6f;
    [SerializeField] private float titleFloatSpeed = 0.85f;
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

    [Header("Game Enter Transition")]
    [Tooltip("Total duration of the enter-game portal rush animation.")]
    [SerializeField] private float enterTransitionDuration = 1.2f;
    [Tooltip("Rapid rotation boost applied to the vortex portal when entering game.")]
    [SerializeField] private float vortexSpinBoost = 240f;
    [Tooltip("Scale zoom factor for the background when diving into the portal.")]
    [SerializeField] private float bgZoomScale = 1.35f;
    [Tooltip("Screen energy flash overlay during portal entry.")]
    [SerializeField] private Image lightningFlashOverlay;

    // Internal State
    private Vector2 bgInitialPos;
    private Vector2 signpostInitialPos;
    private Vector2 titleInitialPos;
    private Vector3 titleInitialScale;

    private Vector2 targetParallaxOffset;
    private Vector2 currentParallaxOffset;

    private MainMenuController menuController;
    private bool isTransitioningToGame = false;

    public bool IsTransitioningToGame => isTransitioningToGame;

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

        // Ensure yellow glow aura on "farm beware" is completely disabled
        if (titleRect != null)
        {
            var tg = titleRect.Find("TitleGlow");
            if (tg != null)
            {
                tg.gameObject.SetActive(false);
            }
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

        if (lightningFlashOverlay == null)
        {
            var l = transform.Find("BackgroundContainer/LightningFlashOverlay");
            if (l != null) lightningFlashOverlay = l.GetComponent<Image>();
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

        // Double check title yellow glow stays disabled
        if (titleRect != null)
        {
            var tg = titleRect.Find("TitleGlow");
            if (tg != null) tg.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isTransitioningToGame)
            return;

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

    #region Title Plaque Animation (Clean & Floating, Yellow Glow Removed)
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

    #region Enter Game Transition Animation
    /// <summary>
    /// Triggers the cinematic portal rush transition when user clicks 'PLAY'.
    /// </summary>
    public void PlayEnterGameTransition(string targetSceneName, System.Action onComplete = null)
    {
        if (isTransitioningToGame) return;
        StartCoroutine(EnterGameTransitionRoutine(targetSceneName, onComplete));
    }

    private IEnumerator EnterGameTransitionRoutine(string targetSceneName, System.Action onComplete)
    {
        isTransitioningToGame = true;

        // 1. Play crisp tactile game-start audio chime
        PlayGameStartAudio();

        // 2. Prevent further clicks
        if (signpostPanel != null)
        {
            var cg = signpostPanel.GetComponent<CanvasGroup>() ?? signpostPanel.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
        }

        // Disable UIAnimationHandler on buttons to prevent scale/color fighting
        if (playButtonRect != null)
        {
            var uih = playButtonRect.GetComponent<UIAnimationHandler>();
            if (uih != null) uih.enabled = false;
            var ind = playButtonRect.Find("IndicatorIcon");
            if (ind != null) ind.gameObject.SetActive(false);
            var glow = playButtonRect.Find("SelectionGlow");
            if (glow != null) glow.gameObject.SetActive(false);
        }
        if (settingsButtonRect != null)
        {
            var uih = settingsButtonRect.GetComponent<UIAnimationHandler>();
            if (uih != null) uih.enabled = false;
            var ind = settingsButtonRect.Find("IndicatorIcon");
            if (ind != null) ind.gameObject.SetActive(false);
            var glow = settingsButtonRect.Find("SelectionGlow");
            if (glow != null) glow.gameObject.SetActive(false);
        }
        if (exitButtonRect != null)
        {
            var uih = exitButtonRect.GetComponent<UIAnimationHandler>();
            if (uih != null) uih.enabled = false;
            var ind = exitButtonRect.Find("IndicatorIcon");
            if (ind != null) ind.gameObject.SetActive(false);
            var glow = exitButtonRect.Find("SelectionGlow");
            if (glow != null) glow.gameObject.SetActive(false);
        }

        // Cache initial positions and scales
        Vector3 playBtnStartScale = playButtonRect != null ? playButtonRect.localScale : Vector3.one;
        Vector2 playBtnStartPos = playButtonRect != null ? playButtonRect.anchoredPosition : Vector2.zero;
        Vector2 settingsStartPos = settingsButtonRect != null ? settingsButtonRect.anchoredPosition : Vector2.zero;
        Vector2 exitStartPos = exitButtonRect != null ? exitButtonRect.anchoredPosition : Vector2.zero;
        Vector2 titleStartPos = titleRect != null ? titleRect.anchoredPosition : Vector2.zero;
        Vector3 bgStartScale = backgroundRect != null ? backgroundRect.localScale : Vector3.one;
        Vector3 vortexStartScale = vortexRect != null ? vortexRect.localScale : Vector3.one;

        var playCG = playButtonRect != null ? (playButtonRect.GetComponent<CanvasGroup>() ?? playButtonRect.gameObject.AddComponent<CanvasGroup>()) : null;
        var settingsCG = settingsButtonRect != null ? (settingsButtonRect.GetComponent<CanvasGroup>() ?? settingsButtonRect.gameObject.AddComponent<CanvasGroup>()) : null;
        var exitCG = exitButtonRect != null ? (exitButtonRect.GetComponent<CanvasGroup>() ?? exitButtonRect.gameObject.AddComponent<CanvasGroup>()) : null;
        var titleCG = titleRect != null ? (titleRect.GetComponent<CanvasGroup>() ?? titleRect.gameObject.AddComponent<CanvasGroup>()) : null;

        // Fade in screen dark transition
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeIn(enterTransitionDuration * 0.95f);
        }

        float elapsed = 0f;
        float duration = enterTransitionDuration;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float easeInCubic = t * t * t;
            float easeOutQuad = 1f - (1f - t) * (1f - t);

            // --- A. Play Button Impact Punch & Forward Dive ---
            if (playButtonRect != null)
            {
                if (t < 0.22f)
                {
                    // Snappy punch bounce scale up to 1.25x
                    float punchT = t / 0.22f;
                    float punchScale = 1f + Mathf.Sin(punchT * Mathf.PI) * 0.26f;
                    playButtonRect.localScale = playBtnStartScale * punchScale;
                    playButtonRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(punchT * Mathf.PI * 2f) * 2.5f);
                }
                else
                {
                    // Fly towards camera / portal center and fade away
                    float expandT = (t - 0.22f) / (1f - 0.22f);
                    float scaleMultiplier = Mathf.Lerp(1.15f, 1.55f, Mathf.Pow(expandT, 2f));
                    playButtonRect.localScale = playBtnStartScale * scaleMultiplier;

                    if (playCG != null)
                    {
                        playCG.alpha = Mathf.Clamp01(Mathf.Lerp(1f, 0f, expandT * 1.7f));
                    }
                }
            }

            // --- B. Settings, Exit, and Title Dispersal ---
            float disperseT = Mathf.Clamp01(t / 0.32f);
            float disperseEase = 1f - Mathf.Pow(1f - disperseT, 3f);

            if (settingsButtonRect != null)
            {
                settingsButtonRect.anchoredPosition = settingsStartPos + new Vector2(0f, -75f * disperseEase);
                if (settingsCG != null) settingsCG.alpha = 1f - disperseEase;
            }

            if (exitButtonRect != null)
            {
                exitButtonRect.anchoredPosition = exitStartPos + new Vector2(0f, -100f * disperseEase);
                if (exitCG != null) exitCG.alpha = 1f - disperseEase;
            }

            if (titleRect != null)
            {
                titleRect.anchoredPosition = titleStartPos + new Vector2(0f, 70f * disperseEase);
                if (titleCG != null) titleCG.alpha = 1f - disperseEase;
            }

            // --- C. Vortex Portal Acceleration & Energy Flare ---
            if (vortexRect != null)
            {
                // Rapidly accelerating vortex spin
                float spinRate = Mathf.Lerp(vortexRotateSpeed, vortexSpinBoost, easeInCubic);
                vortexRect.Rotate(0f, 0f, spinRate * Time.unscaledDeltaTime);

                // Vortex expands and pulses into view
                float vortexExpansion = Mathf.Lerp(1f, 1.45f, smoothT);
                vortexRect.localScale = vortexStartScale * vortexExpansion;
            }

            if (vortexCanvasGroup != null)
            {
                vortexCanvasGroup.alpha = Mathf.Lerp(vortexCanvasGroup.alpha, 1f, easeOutQuad);
            }

            if (galaxyCanvasGroup != null)
            {
                galaxyCanvasGroup.alpha = Mathf.Lerp(galaxyCanvasGroup.alpha, 1f, easeOutQuad);
            }

            // --- D. Background Zoom Into the Portal Rift ---
            if (backgroundRect != null)
            {
                float bgScale = Mathf.Lerp(1f, bgZoomScale, smoothT);
                backgroundRect.localScale = bgStartScale * bgScale;
            }

            // --- E. Soft Energy Bloom Flash Overlay ---
            if (lightningFlashOverlay != null)
            {
                if (t > 0.40f && t < 0.85f)
                {
                    float flashT = (t - 0.40f) / 0.45f;
                    float flashAlpha = Mathf.Sin(flashT * Mathf.PI) * 0.35f;
                    Color c = lightningFlashOverlay.color;
                    c.a = flashAlpha;
                    lightningFlashOverlay.color = c;
                }
                else
                {
                    Color c = lightningFlashOverlay.color;
                    c.a = 0f;
                    lightningFlashOverlay.color = c;
                }
            }

            yield return null;
        }

        // Clean up overlay
        if (lightningFlashOverlay != null)
        {
            Color c = lightningFlashOverlay.color;
            c.a = 0f;
            lightningFlashOverlay.color = c;
        }

        onComplete?.Invoke();

        // Load scene asynchronously for 0 hitching
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }

    private void PlayGameStartAudio()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D UI audio

        AudioClip startClip = CreateGameStartClip();
        if (startClip != null)
        {
            audioSource.PlayOneShot(startClip, 0.7f);
        }
    }

    private static AudioClip cachedStartClip;
    private static AudioClip CreateGameStartClip()
    {
        if (cachedStartClip != null) return cachedStartClip;

        int sampleRate = 44100;
        float duration = 0.55f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Pleasant magical rising chord: C5 (523Hz) -> E5 (659Hz) -> G5 (784Hz) -> C6 (1046Hz)
        float f1 = 523.25f;
        float f2 = 659.25f;
        float f3 = 783.99f;
        float f4 = 1046.50f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 6.5f);

            float s1 = Mathf.Sin(2f * Mathf.PI * f1 * t);
            float s2 = Mathf.Sin(2f * Mathf.PI * f2 * t + 0.1f);
            float s3 = Mathf.Sin(2f * Mathf.PI * f3 * t + 0.2f);
            float s4 = Mathf.Sin(2f * Mathf.PI * f4 * t + 0.3f);
            float sparkle = Mathf.Sin(2f * Mathf.PI * f4 * 2f * t) * 0.12f;

            samples[i] = (s1 * 0.35f + s2 * 0.30f + s3 * 0.25f + s4 * 0.20f + sparkle) * env * 0.45f;
        }

        cachedStartClip = AudioClip.Create("PlayGameStartChime", sampleCount, 1, sampleRate, false);
        cachedStartClip.SetData(samples, 0);
        return cachedStartClip;
    }
    #endregion
}
