using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FeaturesTime;
using FeaturesRendering.Lighting;
using FarmBeware.Core.Runtime;

/// <summary>
/// AAA Cinematic Day/Night Environmental Transition System.
/// Features a feathered widescreen anamorphic veil (zero boxy cards), procedural Celestial Crest Medallions
/// (Radiant Sunburst for Dawn, Blood-Moon Eclipse Crescent for Nightfall), rotating runic halo rings,
/// tapered metallic filigree rules, expanding editorial typography, and procedural orchestral/doom-bell synthesis.
/// Fully decoupled and compliant with Clean Architecture, Zero-GC runtime ticks, and 100% English mandate.
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
    #region Singleton & Accessors

    private static DayTransitionUI _instance;
    public static bool HasInstance
    {
        get
        {
            if (_instance != null) return true;
            _instance = FindFirstObjectByType<DayTransitionUI>(FindObjectsInactive.Include);
            return _instance != null;
        }
    }
    private static bool _isQuitting = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _instance = null;
        _isQuitting = false;
    }

    public static DayTransitionUI Instance
    {
        get
        {
            if (!Application.isPlaying) _isQuitting = false;
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DayTransitionUI>(FindObjectsInactive.Include);
                if (_instance == null && Application.isPlaying && !_isQuitting)
                {
                    var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    if (activeScene.isLoaded && activeScene.name != "MainMenuScene")
                    {
                        var go = new GameObject("DayTransitionUI");
                        _instance = go.AddComponent<DayTransitionUI>();
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return _instance;
        }
        set => _instance = value;
    }

    public static DayTransitionUI EnsureInstance() => Instance;

    #endregion

    #region Inspector Fields & UI References

    [Header("Hierarchy Containers")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private Image vignetteOverlay;
    [SerializeField] private RectTransform bannerRoot;
    [SerializeField] private Image bannerBackgroundImage;
    [SerializeField] private Image bannerInnerGlowImage;
    [SerializeField] private Image bannerAccentLine;
    [SerializeField] private Image bannerTopAccentLine;
    [SerializeField] private Image horizonFlareBeam;
    [SerializeField] private RectTransform cinematicBarTop;
    [SerializeField] private RectTransform cinematicBarBottom;
    [SerializeField] private Image topBarEdgeLine;
    [SerializeField] private Image bottomBarEdgeLine;

    [Header("Celestial Medallion & Filigree")]
    [SerializeField] private Image celestialMedallionImage;
    [SerializeField] private Image celestialHaloImage;
    [SerializeField] private Image leftTitleWingImage;
    [SerializeField] private Image rightTitleWingImage;

    [Header("Typography")]
    [SerializeField] private TextMeshProUGUI categoryTagText;
    [SerializeField] private TextMeshProUGUI iconText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI statusPillText;
    [SerializeField] private Image statusPillBg;
    [SerializeField] private Image statusPillTopRule;

    [Header("Atmospheric Particles")]
    [SerializeField] private RectTransform motesContainer;
    private Image[] _moteImages;
    private Vector2[] _moteBasePositions;
    private float[] _moteSpeeds;
    private float[] _motePhaseOffsets;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dayTransitionSfx;
    [SerializeField] private AudioClip nightTransitionSfx;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.75f;

    [Header("Cinematic Timings")]
    [SerializeField] private float entranceDuration = 0.60f;
    [SerializeField] private float displayDuration = 2.35f;
    [SerializeField] private float exitDuration = 0.60f;

    // State tracking
    private Coroutine transitionCoroutine;
    private Coroutine cameraShakeCoroutine;
    private int lastPlayedDay = -1;
    private TimeManager.DayPhase lastPlayedPhase = (TimeManager.DayPhase)(-1);
    private float lastPlayedRealTime = -999f;
    private int lastTransitionFrame = -1;
    private Vector2 baseBannerPosition = new Vector2(0f, -110f);

    public CanvasGroup CanvasGroupRef => transitionCanvasGroup;
    public TextMeshProUGUI TitleTextRef => titleText;
    public TextMeshProUGUI SubtitleTextRef => subtitleText;
    public bool IsPlayingTransition => transitionCoroutine != null;
    public Image VignetteOverlayRef => vignetteOverlay;

    /// <summary>
    /// Event dispatched when a Day/Night cinematic transition completes its entire entrance, hold, and exit cycle,
    /// or when CancelTransition() is invoked.
    /// </summary>
    public event Action OnTransitionCompleted;

    /// <summary>
    /// Global inquiry helper returning true if any DayTransitionUI instance is currently running a transition.
    /// </summary>
    public static bool IsAnyTransitionActive => Instance != null && Instance.IsPlayingTransition;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        EnsureUIHierarchy();
    }

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged += HandleDayChanged;
            TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }

        if (DayNightTimeManager.Instance != null)
        {
            DayNightTimeManager.Instance.OnTimePhaseChanged += HandleTimePhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        if (DayNightTimeManager.Instance != null)
        {
            DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleTimePhaseChanged;
        }
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion

    #region Event Listeners

    private void HandleDayChanged(int newDay)
    {
        if (newDay > 0)
        {
            PlayDayTransition(newDay);
        }
    }

    private void HandlePhaseChanged(TimeManager.DayPhase phase)
    {
        if (phase == TimeManager.DayPhase.Night)
        {
            PlayNightTransition();
        }
        else if (phase == TimeManager.DayPhase.Day)
        {
            int currentDay = DayNightTimeManager.Instance != null ? DayNightTimeManager.Instance.CurrentDay : (TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0);
            if (currentDay > 0)
            {
                PlayDayTransition(currentDay);
            }
        }
    }

    private void HandleTimePhaseChanged(EnvironmentPhase phase)
    {
        if (phase == EnvironmentPhase.Night)
        {
            PlayNightTransition();
        }
        else if (phase == EnvironmentPhase.Day || phase == EnvironmentPhase.Dawn)
        {
            int currentDay = DayNightTimeManager.Instance != null ? DayNightTimeManager.Instance.CurrentDay : (TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0);
            if (currentDay > 0)
            {
                PlayDayTransition(currentDay);
            }
        }
    }

    #endregion

    #region Transition Control & Player Locking

    public void CancelTransition()
    {
        bool wasPlaying = transitionCoroutine != null;
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (cameraShakeCoroutine != null)
        {
            StopCoroutine(cameraShakeCoroutine);
            cameraShakeCoroutine = null;
        }

        ResetVisualElementsToRest();
        UnlockPlayer();

        if (wasPlaying)
        {
            OnTransitionCompleted?.Invoke();
        }
    }

    private void ResetVisualElementsToRest()
    {
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }

        if (vignetteOverlay != null)
        {
            Color c = vignetteOverlay.color;
            c.a = 0f;
            vignetteOverlay.color = c;
            vignetteOverlay.raycastTarget = false;
        }

        if (bannerRoot != null)
        {
            bannerRoot.localScale = Vector3.one;
            bannerRoot.anchoredPosition = baseBannerPosition;
        }

        if (horizonFlareBeam != null)
        {
            horizonFlareBeam.rectTransform.sizeDelta = new Vector2(0f, 14f);
            Color fc = horizonFlareBeam.color;
            fc.a = 0f;
            horizonFlareBeam.color = fc;
        }

        if (cinematicBarTop != null)
        {
            cinematicBarTop.anchoredPosition = new Vector2(0f, 64f);
        }

        if (cinematicBarBottom != null)
        {
            cinematicBarBottom.anchoredPosition = new Vector2(0f, -64f);
        }

        HideMotes();
    }

    private void LockPlayer()
    {
        var player = ServiceLocator.Resolve<IPlayerContext>();
        if (player == null)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb is IPlayerContext ctx)
                {
                    player = ctx;
                    break;
                }
            }
        }

        if (player != null)
        {
            player.AddInputLock("DayTransition");
        }
    }

    private void UnlockPlayer()
    {
        var player = ServiceLocator.Resolve<IPlayerContext>();
        if (player == null)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb is IPlayerContext ctx)
                {
                    player = ctx;
                    break;
                }
            }
        }

        if (player != null)
        {
            player.RemoveInputLock("DayTransition");
        }
    }

    #endregion

    #region Public Transition Triggers

    /// <summary>
    /// Plays the AAA cinematic transition from Day to Night (Nightfall & Night Brawl awakening).
    /// </summary>
    public void PlayNightTransition()
    {
        if (Time.frameCount == lastTransitionFrame && lastPlayedPhase == TimeManager.DayPhase.Night)
            return;

        if (Time.unscaledTime - lastPlayedRealTime < 1.0f && lastPlayedPhase == TimeManager.DayPhase.Night)
            return;

        lastTransitionFrame = Time.frameCount;
        lastPlayedPhase = TimeManager.DayPhase.Night;
        lastPlayedRealTime = Time.unscaledTime;

        DismissActiveStoryDialogue();

        EnsureUIHierarchy();
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CinematicTransitionRoutine(isNight: true, dayNumber: 0));
    }

    /// <summary>
    /// Plays the AAA cinematic transition from Night to Day (Sunrise, dawn renewal, and homestead awakening).
    /// </summary>
    public void PlayDayTransition(int dayNumber)
    {
        if (dayNumber <= 0)
            return;

        if (Time.frameCount == lastTransitionFrame && lastPlayedPhase == TimeManager.DayPhase.Day && lastPlayedDay == dayNumber)
            return;

        if (Time.unscaledTime - lastPlayedRealTime < 1.0f && lastPlayedPhase == TimeManager.DayPhase.Day && lastPlayedDay == dayNumber)
            return;

        lastTransitionFrame = Time.frameCount;
        lastPlayedPhase = TimeManager.DayPhase.Day;
        lastPlayedDay = dayNumber;
        lastPlayedRealTime = Time.unscaledTime;

        DismissActiveStoryDialogue();

        EnsureUIHierarchy();
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        if (ServiceLocator.TryResolve<IFadeService>(out var fadeService) && fadeService != null)
        {
            fadeService.FadeOut(0.6f);
        }

        transitionCoroutine = StartCoroutine(CinematicTransitionRoutine(isNight: false, dayNumber: dayNumber));
    }

    private void DismissActiveStoryDialogue()
    {
        if (ServiceLocator.TryResolve<IModalStackService>(out var modalStack) && modalStack != null)
        {
            modalStack.CloseAll();
        }

        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb != null && mb.GetType().Name == "StorylineUI")
            {
                var method = mb.GetType().GetMethod("DismissDialogue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(mb, null);
                }
                else if (Application.isPlaying && mb.gameObject.activeInHierarchy)
                {
                    mb.SendMessage("DismissDialogue", SendMessageOptions.DontRequireReceiver);
                }
                break;
            }
        }

        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            var dlg = canvases[i].transform.Find("StoryDialoguePanel");
            if (dlg != null && dlg.gameObject.activeSelf)
            {
                dlg.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region Cinematic Sequence Routine

    private IEnumerator CinematicTransitionRoutine(bool isNight, int dayNumber)
    {
        LockPlayer();
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.blocksRaycasts = true;
        }
        if (vignetteOverlay != null)
        {
            vignetteOverlay.raycastTarget = true;
        }

        try
        {
            Color accentColor;
            Color flareColor;
            Color vignetteTargetColor;
            Color veilColor;
            Color innerGlowColor;

            if (isNight)
            {
                if (iconText != null) iconText.text = "";

                if (celestialMedallionImage != null)
                {
                    celestialMedallionImage.sprite = GetOrCreateBloodMoonCrestSprite();
                    celestialMedallionImage.color = new Color(1.0f, 0.34f, 0.36f, 1.0f);
                }
                if (celestialHaloImage != null)
                {
                    celestialHaloImage.color = new Color(0.95f, 0.22f, 0.28f, 0.55f);
                }

                if (categoryTagText != null)
                {
                    categoryTagText.text = "BLOOD MOON ECLIPSE   •   NOCTURNAL SIEGE";
                    categoryTagText.color = new Color(1.0f, 0.52f, 0.52f, 0.92f);
                }

                if (titleText != null)
                {
                    titleText.text = "NIGHTFALL";
                    titleText.color = new Color(1.0f, 0.90f, 0.90f, 1.0f);
                    titleText.characterSpacing = 7f;
                }

                if (subtitleText != null)
                {
                    subtitleText.text = "Ravenous shadows converge upon the homestead. Wield your arms and survive until dawn.";
                    subtitleText.color = new Color(0.92f, 0.74f, 0.76f, 0.95f);
                }

                if (statusPillText != null)
                {
                    statusPillText.text =
                        "<color=#FF5252>◆</color>  <b>STANCE: COMBAT BRAWL</b>     <color=#5A3A42>|</color>     " +
                        "<color=#FFB74D>◆</color>  <b>FARM TOOLS LOCKED</b>     <color=#5A3A42>|</color>     " +
                        "<color=#FF8A80>◆</color>  <b>DEFEND THE BARRICADES</b>";
                    statusPillText.color = new Color(0.96f, 0.86f, 0.88f, 1.0f);
                }

                if (statusPillBg != null)
                {
                    statusPillBg.color = new Color(0.18f, 0.03f, 0.05f, 0.82f);
                }

                accentColor = new Color(0.96f, 0.24f, 0.28f, 0.90f);
                flareColor = new Color(1.0f, 0.24f, 0.28f, 0.78f);
                vignetteTargetColor = new Color(0.28f, 0.01f, 0.04f, 0.52f);
                veilColor = new Color(0.06f, 0.015f, 0.025f, 0.92f);
                innerGlowColor = new Color(0.55f, 0.06f, 0.10f, 0.34f);

                PlaySound(isNight: true);
                if (Application.isPlaying)
                {
                    cameraShakeCoroutine = StartCoroutine(RoutineCameraMicroShake(0.22f, 0.055f));
                }
            }
            else
            {
                int safeDay = Mathf.Max(1, dayNumber);
                string roman = ToRomanNumeral(safeDay);

                if (iconText != null) iconText.text = "";

                if (celestialMedallionImage != null)
                {
                    celestialMedallionImage.sprite = GetOrCreateSunCrestSprite();
                    celestialMedallionImage.color = new Color(1.0f, 0.86f, 0.36f, 1.0f);
                }
                if (celestialHaloImage != null)
                {
                    celestialHaloImage.color = new Color(1.0f, 0.82f, 0.30f, 0.55f);
                }

                if (categoryTagText != null)
                {
                    categoryTagText.text = $"CHAPTER  {roman}   •   DAWN OF THE HOMESTEAD";
                    categoryTagText.color = new Color(1.0f, 0.84f, 0.42f, 0.92f);
                }

                if (titleText != null)
                {
                    titleText.text = $"DAY {safeDay}";
                    titleText.color = new Color(1.0f, 0.96f, 0.84f, 1.0f);
                    titleText.characterSpacing = 7f;
                }

                if (subtitleText != null)
                {
                    subtitleText.text = "A golden dawn breaks over the fertile soil. Tend your crops and prepare before dusk.";
                    subtitleText.color = new Color(0.94f, 0.89f, 0.76f, 0.95f);
                }

                if (statusPillText != null)
                {
                    statusPillText.text =
                        "<color=#FFD54F>◆</color>  <b>STANCE: AGRICULTURAL</b>     <color=#4A5568>|</color>     " +
                        "<color=#66BB6A>◆</color>  <b>CROPS GROWING</b>     <color=#4A5568>|</color>     " +
                        "<color=#4FC3F7>◆</color>  <b>WELL & MARKET OPEN</b>";
                    statusPillText.color = new Color(0.96f, 0.94f, 0.86f, 1.0f);
                }

                if (statusPillBg != null)
                {
                    statusPillBg.color = new Color(0.08f, 0.11f, 0.16f, 0.82f);
                }

                accentColor = new Color(0.98f, 0.78f, 0.28f, 0.90f);
                flareColor = new Color(1.0f, 0.86f, 0.38f, 0.75f);
                vignetteTargetColor = new Color(0.85f, 0.65f, 0.20f, 0.24f);
                veilColor = new Color(0.035f, 0.05f, 0.085f, 0.90f);
                innerGlowColor = new Color(0.55f, 0.38f, 0.08f, 0.30f);

                PlaySound(isNight: false);
            }

            if (bannerBackgroundImage != null) bannerBackgroundImage.color = veilColor;
            if (bannerInnerGlowImage != null) bannerInnerGlowImage.color = innerGlowColor;
            if (bannerAccentLine != null) bannerAccentLine.color = accentColor;
            if (bannerTopAccentLine != null) bannerTopAccentLine.color = accentColor;
            if (leftTitleWingImage != null) leftTitleWingImage.color = accentColor;
            if (rightTitleWingImage != null) rightTitleWingImage.color = accentColor;
            if (statusPillTopRule != null) statusPillTopRule.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.65f);
            if (topBarEdgeLine != null) topBarEdgeLine.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);
            if (bottomBarEdgeLine != null) bottomBarEdgeLine.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);
            if (horizonFlareBeam != null) horizonFlareBeam.color = new Color(flareColor.r, flareColor.g, flareColor.b, 0f);

            ConfigureMotes(isNight);

            // 2. Entrance Phase (0.60s): CinemaScope Letterbox + Anamorphic Light Sweep + Veil Reveal
            float inDuration = Mathf.Max(entranceDuration, 0.1f);
            for (float t = 0f; t < inDuration; t += Time.unscaledDeltaTime)
            {
                float norm = Mathf.Clamp01(t / inDuration);
                float easeOutCubic = 1f - Mathf.Pow(1f - norm, 3f);
                float easeOutQuint = 1f - Mathf.Pow(1f - norm, 5f);

                if (transitionCanvasGroup != null)
                    transitionCanvasGroup.alpha = easeOutCubic;

                if (cinematicBarTop != null)
                    cinematicBarTop.anchoredPosition = new Vector2(0f, Mathf.Lerp(64f, 0f, easeOutCubic));
                if (cinematicBarBottom != null)
                    cinematicBarBottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(-64f, 0f, easeOutCubic));

                if (vignetteOverlay != null)
                {
                    Color vc = vignetteTargetColor;
                    vc.a = Mathf.Lerp(0f, vignetteTargetColor.a, easeOutCubic);
                    vignetteOverlay.color = vc;
                }

                if (horizonFlareBeam != null)
                {
                    float flareWidth = Mathf.Lerp(120f, 1680f, easeOutQuint);
                    horizonFlareBeam.rectTransform.sizeDelta = new Vector2(flareWidth, 14f);
                    Color fc = flareColor;
                    fc.a = Mathf.Lerp(0f, flareColor.a, Mathf.Sin(norm * Mathf.PI * 0.5f));
                    horizonFlareBeam.color = fc;
                }

                if (bannerRoot != null)
                {
                    float scaleX = Mathf.Lerp(0.92f, 1.0f, easeOutQuint);
                    float scaleY = Mathf.Lerp(0.88f, 1.0f, easeOutCubic);
                    bannerRoot.localScale = new Vector3(scaleX, scaleY, 1f);
                    float yPos = Mathf.Lerp(baseBannerPosition.y + 18f, baseBannerPosition.y, easeOutCubic);
                    bannerRoot.anchoredPosition = new Vector2(0f, yPos);
                }

                if (titleText != null)
                {
                    titleText.characterSpacing = Mathf.Lerp(3f, 8.5f, easeOutCubic);
                }

                if (celestialHaloImage != null)
                {
                    celestialHaloImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 28f);
                }

                yield return null;
            }

            if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;
            if (cinematicBarTop != null) cinematicBarTop.anchoredPosition = Vector2.zero;
            if (cinematicBarBottom != null) cinematicBarBottom.anchoredPosition = Vector2.zero;
            if (bannerRoot != null) bannerRoot.localScale = Vector3.one;

            // 3. Hold Phase: Subtle Typographic Tracking Expansion & Rotating Celestial Halo
            float holdDuration = Mathf.Max(displayDuration, 0.5f);
            for (float t = 0f; t < holdDuration; t += Time.unscaledDeltaTime)
            {
                float holdNorm = Mathf.Clamp01(t / holdDuration);
                float floatOffset = Mathf.Sin(t * 2.2f) * 2.5f;
                if (bannerRoot != null)
                {
                    bannerRoot.anchoredPosition = baseBannerPosition + new Vector2(0f, floatOffset);
                }

                if (titleText != null)
                {
                    titleText.characterSpacing = Mathf.Lerp(8.5f, 11.5f, holdNorm);
                }

                if (celestialHaloImage != null)
                {
                    celestialHaloImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (inDuration + t) * 28f);
                    float haloPulse = 0.45f + 0.18f * Mathf.Sin(t * 3.5f);
                    Color hc = celestialHaloImage.color;
                    hc.a = haloPulse;
                    celestialHaloImage.color = hc;
                }

                if (horizonFlareBeam != null)
                {
                    float breathAlpha = flareColor.a * (0.72f + 0.28f * Mathf.Sin(t * 3.0f));
                    Color fc = flareColor;
                    fc.a = breathAlpha;
                    horizonFlareBeam.color = fc;
                }

                AnimateMotes(t, isNight);

                yield return null;
            }

            // 4. Resolve & Exit Phase (0.60s)
            float outDuration = Mathf.Max(exitDuration, 0.1f);
            for (float t = 0f; t < outDuration; t += Time.unscaledDeltaTime)
            {
                float norm = Mathf.Clamp01(t / outDuration);
                float easeInCubic = norm * norm * norm;
                float currentAlpha = 1f - norm;
                float scaleDrift = Mathf.Lerp(1.0f, 1.035f, norm);

                if (transitionCanvasGroup != null)
                    transitionCanvasGroup.alpha = currentAlpha;

                if (cinematicBarTop != null)
                    cinematicBarTop.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 64f, easeInCubic));
                if (cinematicBarBottom != null)
                    cinematicBarBottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -64f, easeInCubic));

                if (horizonFlareBeam != null)
                {
                    float flareWidth = Mathf.Lerp(1680f, 0f, easeInCubic);
                    horizonFlareBeam.rectTransform.sizeDelta = new Vector2(flareWidth, 14f);
                }

                if (bannerRoot != null)
                {
                    bannerRoot.localScale = new Vector3(scaleDrift, scaleDrift, 1f);
                }

                if (vignetteOverlay != null)
                {
                    Color vc = vignetteTargetColor;
                    vc.a = Mathf.Lerp(vignetteTargetColor.a, 0f, norm);
                    vignetteOverlay.color = vc;
                }

                yield return null;
            }
        }
        finally
        {
            ResetVisualElementsToRest();
            UnlockPlayer();
            transitionCoroutine = null;
            OnTransitionCompleted?.Invoke();
        }
    }

    private static string ToRomanNumeral(int number)
    {
        return number switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            6 => "VI",
            7 => "VII",
            8 => "VIII",
            9 => "IX",
            10 => "X",
            _ => number.ToString()
        };
    }

    private IEnumerator RoutineCameraMicroShake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float damper = 1f - (elapsed / duration);
            float x = (UnityEngine.Random.value * 2f - 1f) * magnitude * damper;
            float y = (UnityEngine.Random.value * 2f - 1f) * magnitude * damper;
            cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalPos;
        cameraShakeCoroutine = null;
    }

    #endregion

    #region Atmospheric Particle Motes

    private void ConfigureMotes(bool isNight)
    {
        if (_moteImages == null || _moteImages.Length == 0) return;

        Color moteColor = isNight
            ? new Color(1.00f, 0.32f, 0.34f, 0.78f)
            : new Color(1.00f, 0.88f, 0.42f, 0.74f);

        for (int i = 0; i < _moteImages.Length; i++)
        {
            if (_moteImages[i] != null)
            {
                _moteImages[i].color = moteColor;
                _moteImages[i].gameObject.SetActive(true);
            }
        }
    }

    private void AnimateMotes(float elapsed, bool isNight)
    {
        if (_moteImages == null || _moteImages.Length == 0) return;

        for (int i = 0; i < _moteImages.Length; i++)
        {
            if (_moteImages[i] == null) continue;

            var rt = _moteImages[i].rectTransform;
            Vector2 basePos = _moteBasePositions[i];
            float speed = _moteSpeeds[i];
            float phase = _motePhaseOffsets[i];

            float yOffset = isNight
                ? -((elapsed * speed * 24f + phase * 40f) % 105f)
                : ((elapsed * speed * 24f + phase * 40f) % 105f);

            float xSway = Mathf.Sin((elapsed + phase) * 2.4f) * 14f;

            rt.anchoredPosition = basePos + new Vector2(xSway, yOffset);

            Color c = _moteImages[i].color;
            c.a = 0.25f + 0.50f * Mathf.Sin((elapsed + phase) * 3.8f);
            _moteImages[i].color = c;
        }
    }

    private void HideMotes()
    {
        if (_moteImages == null) return;
        for (int i = 0; i < _moteImages.Length; i++)
        {
            if (_moteImages[i] != null)
                _moteImages[i].gameObject.SetActive(false);
        }
    }

    #endregion

    #region Procedural Audio Synthesis (AAA Orchestral & Doom Bell)

    private void PlaySound(bool isNight)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isNight ? nightTransitionSfx : dayTransitionSfx;
        if (clipToPlay == null)
        {
            clipToPlay = isNight ? GetOrCreateNightDoomToll() : GetOrCreateDayDawnAwakening();
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, sfxVolume);
        }
    }

    private static AudioClip _cachedDayChime;
    private static AudioClip _cachedNightChime;

    private static AudioClip GetOrCreateDayDawnAwakening()
    {
        if (_cachedDayChime != null) return _cachedDayChime;

        int sampleRate = 44100;
        float duration = 2.0f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] freqs = { 261.63f, 329.63f, 392.00f, 493.88f, 587.33f, 783.99f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sub = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.32f * Mathf.Exp(-t * 1.5f);

            float harp = 0f;
            for (int n = 0; n < freqs.Length; n++)
            {
                float noteStart = n * 0.075f;
                if (t >= noteStart)
                {
                    float noteT = t - noteStart;
                    float noteEnv = Mathf.Exp(-noteT * (3.2f + n * 0.3f));
                    float fundamental = Mathf.Sin(2f * Mathf.PI * freqs[n] * noteT);
                    float overtone = Mathf.Sin(4f * Mathf.PI * freqs[n] * noteT) * 0.28f;
                    harp += (fundamental + overtone) * noteEnv * 0.20f;
                }
            }

            samples[i] = Mathf.Clamp(sub + harp, -1f, 1f);
        }

        _cachedDayChime = AudioClip.Create("Chime_DayDawnAwakening", sampleCount, 1, sampleRate, false);
        _cachedDayChime.SetData(samples, 0);
        return _cachedDayChime;
    }

    private static AudioClip GetOrCreateNightDoomToll()
    {
        if (_cachedNightChime != null) return _cachedNightChime;

        int sampleRate = 44100;
        float duration = 2.4f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            float subImpact = Mathf.Sin(2f * Mathf.PI * 52f * t) * Mathf.Exp(-t * 3.6f) * 0.45f;

            float bellEnv = Mathf.Exp(-t * 1.5f);
            float bellFundamental = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.38f;
            float bellMinor3rd = Mathf.Sin(2f * Mathf.PI * 87.31f * t) * 0.22f;
            float bellTritone = Mathf.Sin(2f * Mathf.PI * 103.83f * t) * 0.18f;
            float bellOvertone = Mathf.Sin(2f * Mathf.PI * 220.0f * t) * 0.14f * Mathf.Exp(-t * 2.2f);
            float bellMetallic = Mathf.Sin(2f * Mathf.PI * 587.3f * t) * 0.08f * Mathf.Exp(-t * 3.8f);

            float gong = (bellFundamental + bellMinor3rd + bellTritone + bellOvertone + bellMetallic) * bellEnv;
            float rumble = (Mathf.PerlinNoise(t * 14f, 0.5f) - 0.5f) * 0.10f * Mathf.Exp(-t * 0.9f);

            samples[i] = Mathf.Clamp(subImpact + gong + rumble, -1f, 1f);
        }

        _cachedNightChime = AudioClip.Create("Chime_NightDoomToll", sampleCount, 1, sampleRate, false);
        _cachedNightChime.SetData(samples, 0);
        return _cachedNightChime;
    }

    #endregion

    #region Procedural Sprite Generators

    private static Sprite _cachedVignetteSprite;
    private static Sprite GetOrCreateVignetteSprite()
    {
        if (_cachedVignetteSprite != null) return _cachedVignetteSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxRadius = size * 0.5f;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normDist = Mathf.Clamp01(dist / maxRadius);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normDist - 0.32f) / 0.68f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedVignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedVignetteSprite;
    }

    private static Sprite _cachedFeatheredRibbonSprite;
    /// <summary>
    /// Generates an anamorphic widescreen ribbon sprite that feathers smoothly to 0% alpha
    /// on both horizontal edges and subtle vertical edges (eliminating ugly boxy rectangular cards).
    /// </summary>
    private static Sprite GetOrCreateFeatheredRibbonSprite()
    {
        if (_cachedFeatheredRibbonSprite != null) return _cachedFeatheredRibbonSprite;

        const int w = 256;
        const int h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float ny = (y + 0.5f) / h;
            float distY = Mathf.Abs(ny - 0.5f) * 2f;
            float verticalMask = 1f - Mathf.SmoothStep(0.76f, 1.0f, distY);

            for (int x = 0; x < w; x++)
            {
                float nx = (x + 0.5f) / w;
                float distX = Mathf.Abs(nx - 0.5f) * 2f;
                float horizontalMask = 1f - Mathf.SmoothStep(0.28f, 0.98f, distX);
                float alpha = horizontalMask * verticalMask;
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedFeatheredRibbonSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _cachedFeatheredRibbonSprite;
    }

    private static Sprite _cachedFlareSprite;
    private static Sprite GetOrCreateFlareSprite()
    {
        if (_cachedFlareSprite != null) return _cachedFlareSprite;

        int w = 256;
        int h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float ny = 1f - Mathf.Abs((y - (h * 0.5f)) / (h * 0.5f));
            for (int x = 0; x < w; x++)
            {
                float nx = 1f - Mathf.Abs((x - (w * 0.5f)) / (w * 0.5f));
                float alpha = Mathf.Pow(nx, 2.2f) * Mathf.Pow(ny, 1.5f);
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedFlareSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _cachedFlareSprite;
    }

    private static Sprite _cachedSunCrestSprite;
    /// <summary>
    /// Generates a crisp 96x96 Radiant Sunburst Crest medallion for the Daybreak transition.
    /// </summary>
    private static Sprite GetOrCreateSunCrestSprite()
    {
        if (_cachedSunCrestSprite != null) return _cachedSunCrestSprite;

        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - center;
                float r = d.magnitude / maxR;
                float angle = Mathf.Atan2(d.y, d.x);

                // Inner solar disc
                float core = 1f - Mathf.SmoothStep(0.24f, 0.30f, r);

                // Concentric inner ring
                float ring = (1f - Mathf.SmoothStep(0.0f, 0.035f, Mathf.Abs(r - 0.42f))) * 0.85f;

                // 8-point geometric sunburst rays
                float rayWave = Mathf.Max(0f, Mathf.Cos(angle * 8f));
                float rayReach = 0.32f + Mathf.Pow(rayWave, 3f) * 0.56f;
                float rays = (r > 0.28f && r < rayReach)
                    ? (1f - Mathf.SmoothStep(0.28f, rayReach, r)) * Mathf.Pow(rayWave, 2f) * 0.90f
                    : 0f;

                // Soft ambient corona
                float corona = Mathf.Pow(Mathf.Clamp01(1f - r), 2.6f) * 0.45f;

                float a = Mathf.Clamp01(core + ring + rays + corona);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedSunCrestSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedSunCrestSprite;
    }

    private static Sprite _cachedBloodMoonCrestSprite;
    /// <summary>
    /// Generates a crisp 96x96 Blood-Moon Eclipse Crescent medallion for the Nightfall transition.
    /// </summary>
    private static Sprite GetOrCreateBloodMoonCrestSprite()
    {
        if (_cachedBloodMoonCrestSprite != null) return _cachedBloodMoonCrestSprite;

        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 shadowCenter = center + new Vector2(size * 0.13f, size * 0.06f);
        float maxR = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float rMain = Vector2.Distance(p, center) / maxR;
                float rShadow = Vector2.Distance(p, shadowCenter) / maxR;

                float outerDisc = 1f - Mathf.SmoothStep(0.54f, 0.60f, rMain);
                float shadowCutout = 1f - Mathf.SmoothStep(0.44f, 0.50f, rShadow);
                float crescent = Mathf.Clamp01(outerDisc - shadowCutout * 0.96f);

                // Thin eclipse outer ring
                float eclipseRing = (1f - Mathf.SmoothStep(0f, 0.03f, Mathf.Abs(rMain - 0.68f))) * 0.65f;

                // Soft blood corona glow
                float corona = Mathf.Pow(Mathf.Clamp01(1f - rMain), 2.4f) * 0.40f;

                float a = Mathf.Clamp01(crescent + eclipseRing + corona);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedBloodMoonCrestSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedBloodMoonCrestSprite;
    }

    private static Sprite _cachedHaloRingSprite;
    /// <summary>
    /// Generates a dashed geometric halo ring that rotates slowly behind the celestial medallion.
    /// </summary>
    private static Sprite GetOrCreateHaloRingSprite()
    {
        if (_cachedHaloRingSprite != null) return _cachedHaloRingSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - center;
                float r = d.magnitude / maxR;
                float angle = Mathf.Atan2(d.y, d.x);

                float ringBand = 1f - Mathf.SmoothStep(0f, 0.025f, Mathf.Abs(r - 0.82f));
                float dashMask = Mathf.SmoothStep(-0.2f, 0.3f, Mathf.Cos(angle * 12f));
                float cardinalDiamond = (1f - Mathf.SmoothStep(0f, 0.045f, Mathf.Abs(r - 0.82f)))
                    * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 4f)), 16f);

                float a = Mathf.Clamp01(ringBand * dashMask * 0.75f + cardinalDiamond);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedHaloRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedHaloRingSprite;
    }

    private static Sprite _cachedSparkleMoteSprite;
    private static Sprite GetOrCreateSparkleMoteSprite()
    {
        if (_cachedSparkleMoteSprite != null) return _cachedSparkleMoteSprite;

        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(1f - norm, 2.2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedSparkleMoteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedSparkleMoteSprite;
    }

    private static TMP_FontAsset GetFontAsset()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        var existing = FindFirstObjectByType<TextMeshProUGUI>();
        return existing != null ? existing.font : null;
    }

    #endregion

    #region Procedural UI Hierarchy Generation

    /// <summary>
    /// Builds the redesigned widescreen anamorphic transition hierarchy.
    /// Automatically upgrades any existing scene hierarchy to the new layout.
    /// </summary>
    public void EnsureUIHierarchy()
    {
        // 1. Parent Canvas Resolution
        var parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
        if (parentCanvas == null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 750;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = gameObject.AddComponent<GraphicRaycaster>();

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                if (Application.isPlaying) Destroy(scaler);
                else DestroyImmediate(scaler);
            }

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = 750;

            transform.SetAsLastSibling();
        }

        var legacyDayText = transform.Find("DayText");
        if (legacyDayText != null)
        {
            if (Application.isPlaying) Destroy(legacyDayText.gameObject);
            else DestroyImmediate(legacyDayText.gameObject);
        }

        transitionCanvasGroup = GetComponent<CanvasGroup>();
        if (transitionCanvasGroup == null)
            transitionCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        transitionCanvasGroup.alpha = 0f;
        transitionCanvasGroup.blocksRaycasts = false;
        transitionCanvasGroup.interactable = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // 2. Fullscreen Atmospheric Vignette Overlay
        var vignetteGO = GetOrCreateChild(transform, "VignetteOverlay", typeof(Image));
        var vignetteRect = vignetteGO.GetComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;
        vignetteOverlay = vignetteGO.GetComponent<Image>();
        vignetteOverlay.sprite = GetOrCreateVignetteSprite();
        vignetteOverlay.color = new Color(0f, 0f, 0f, 0f);
        vignetteOverlay.raycastTarget = false;

        // 3. CinemaScope Letterbox Bars (64px with Feathered Metallic Edge Lines)
        var barTopGO = GetOrCreateChild(transform, "CinematicBar_Top", typeof(Image));
        cinematicBarTop = barTopGO.GetComponent<RectTransform>();
        cinematicBarTop.anchorMin = new Vector2(0f, 1f);
        cinematicBarTop.anchorMax = new Vector2(1f, 1f);
        cinematicBarTop.pivot = new Vector2(0.5f, 1f);
        cinematicBarTop.sizeDelta = new Vector2(0f, 64f);
        cinematicBarTop.anchoredPosition = new Vector2(0f, 64f);
        var barTopImg = barTopGO.GetComponent<Image>();
        barTopImg.color = new Color(0.015f, 0.02f, 0.035f, 0.98f);
        barTopImg.raycastTarget = false;

        var topEdgeGO = GetOrCreateChild(cinematicBarTop, "TopBarEdgeLine", typeof(Image));
        var topEdgeRt = topEdgeGO.GetComponent<RectTransform>();
        topEdgeRt.anchorMin = new Vector2(0f, 0f);
        topEdgeRt.anchorMax = new Vector2(1f, 0f);
        topEdgeRt.pivot = new Vector2(0.5f, 0f);
        topEdgeRt.sizeDelta = new Vector2(0f, 2f);
        topEdgeRt.anchoredPosition = Vector2.zero;
        topBarEdgeLine = topEdgeGO.GetComponent<Image>();
        topBarEdgeLine.sprite = GetOrCreateFlareSprite();
        topBarEdgeLine.raycastTarget = false;

        var barBottomGO = GetOrCreateChild(transform, "CinematicBar_Bottom", typeof(Image));
        cinematicBarBottom = barBottomGO.GetComponent<RectTransform>();
        cinematicBarBottom.anchorMin = new Vector2(0f, 0f);
        cinematicBarBottom.anchorMax = new Vector2(1f, 0f);
        cinematicBarBottom.pivot = new Vector2(0.5f, 0f);
        cinematicBarBottom.sizeDelta = new Vector2(0f, 64f);
        cinematicBarBottom.anchoredPosition = new Vector2(0f, -64f);
        var barBottomImg = barBottomGO.GetComponent<Image>();
        barBottomImg.color = new Color(0.015f, 0.02f, 0.035f, 0.98f);
        barBottomImg.raycastTarget = false;

        var botEdgeGO = GetOrCreateChild(cinematicBarBottom, "BottomBarEdgeLine", typeof(Image));
        var botEdgeRt = botEdgeGO.GetComponent<RectTransform>();
        botEdgeRt.anchorMin = new Vector2(0f, 1f);
        botEdgeRt.anchorMax = new Vector2(1f, 1f);
        botEdgeRt.pivot = new Vector2(0.5f, 1f);
        botEdgeRt.sizeDelta = new Vector2(0f, 2f);
        botEdgeRt.anchoredPosition = Vector2.zero;
        bottomBarEdgeLine = botEdgeGO.GetComponent<Image>();
        bottomBarEdgeLine.sprite = GetOrCreateFlareSprite();
        bottomBarEdgeLine.raycastTarget = false;

        // 4. Anamorphic Horizon Flare Beam (sweeps behind the title veil)
        var flareGO = GetOrCreateChild(transform, "HorizonFlareBeam", typeof(Image));
        var flareRt = flareGO.GetComponent<RectTransform>();
        flareRt.anchorMin = new Vector2(0.5f, 1f);
        flareRt.anchorMax = new Vector2(0.5f, 1f);
        flareRt.pivot = new Vector2(0.5f, 0.5f);
        flareRt.anchoredPosition = new Vector2(0f, -215f);
        flareRt.sizeDelta = new Vector2(0f, 14f);
        horizonFlareBeam = flareGO.GetComponent<Image>();
        horizonFlareBeam.sprite = GetOrCreateFlareSprite();
        horizonFlareBeam.color = new Color(1f, 1f, 1f, 0f);
        horizonFlareBeam.raycastTarget = false;

        // 5. Feathered Widescreen Banner Veil (1360x220, horizontally feathered to 0% alpha at ends)
        var bannerGO = GetOrCreateChild(transform, "BannerRoot", typeof(Image));
        bannerRoot = bannerGO.GetComponent<RectTransform>();
        bannerRoot.anchorMin = new Vector2(0.5f, 1f);
        bannerRoot.anchorMax = new Vector2(0.5f, 1f);
        bannerRoot.pivot = new Vector2(0.5f, 1f);
        bannerRoot.anchoredPosition = baseBannerPosition;
        bannerRoot.sizeDelta = new Vector2(1360f, 224f);

        bannerBackgroundImage = bannerGO.GetComponent<Image>();
        bannerBackgroundImage.sprite = GetOrCreateFeatheredRibbonSprite();
        bannerBackgroundImage.type = Image.Type.Simple;
        bannerBackgroundImage.color = new Color(0.035f, 0.05f, 0.085f, 0.90f);
        bannerBackgroundImage.raycastTarget = false;

        // Soft inner core glow behind medallion & title
        var innerGlowGO = GetOrCreateChild(bannerRoot, "InnerCoreGlow", typeof(Image));
        var innerGlowRt = innerGlowGO.GetComponent<RectTransform>();
        innerGlowRt.anchorMin = new Vector2(0.22f, 0.15f);
        innerGlowRt.anchorMax = new Vector2(0.78f, 0.88f);
        innerGlowRt.offsetMin = Vector2.zero;
        innerGlowRt.offsetMax = Vector2.zero;
        bannerInnerGlowImage = innerGlowGO.GetComponent<Image>();
        bannerInnerGlowImage.sprite = GetOrCreateFeatheredRibbonSprite();
        bannerInnerGlowImage.color = new Color(0.55f, 0.38f, 0.08f, 0.28f);
        bannerInnerGlowImage.raycastTarget = false;

        // 6. Tapered Filigree Rules (Top & Bottom of Veil)
        var topAccentGO = GetOrCreateChild(bannerRoot, "TopAccentLine", typeof(Image));
        var topAccentRect = topAccentGO.GetComponent<RectTransform>();
        topAccentRect.anchorMin = new Vector2(0.10f, 0.985f);
        topAccentRect.anchorMax = new Vector2(0.90f, 0.985f);
        topAccentRect.pivot = new Vector2(0.5f, 0.5f);
        topAccentRect.anchoredPosition = Vector2.zero;
        topAccentRect.sizeDelta = new Vector2(0f, 2.5f);
        bannerTopAccentLine = topAccentGO.GetComponent<Image>();
        bannerTopAccentLine.sprite = GetOrCreateFlareSprite();
        bannerTopAccentLine.raycastTarget = false;

        var accentGO = GetOrCreateChild(bannerRoot, "AccentLine", typeof(Image));
        var accentRect = accentGO.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.10f, 0.015f);
        accentRect.anchorMax = new Vector2(0.90f, 0.015f);
        accentRect.pivot = new Vector2(0.5f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 2.5f);
        bannerAccentLine = accentGO.GetComponent<Image>();
        bannerAccentLine.sprite = GetOrCreateFlareSprite();
        bannerAccentLine.raycastTarget = false;

        // 7. Celestial Crest Medallion & Rotating Runic Halo (Top Center of Veil)
        var haloGO = GetOrCreateChild(bannerRoot, "CelestialHaloRing", typeof(Image));
        var haloRt = haloGO.GetComponent<RectTransform>();
        haloRt.anchorMin = new Vector2(0.5f, 1f);
        haloRt.anchorMax = new Vector2(0.5f, 1f);
        haloRt.pivot = new Vector2(0.5f, 0.5f);
        haloRt.anchoredPosition = new Vector2(0f, -34f);
        haloRt.sizeDelta = new Vector2(78f, 78f);
        celestialHaloImage = haloGO.GetComponent<Image>();
        celestialHaloImage.sprite = GetOrCreateHaloRingSprite();
        celestialHaloImage.raycastTarget = false;

        var medallionGO = GetOrCreateChild(bannerRoot, "CelestialMedallion", typeof(Image));
        var medallionRt = medallionGO.GetComponent<RectTransform>();
        medallionRt.anchorMin = new Vector2(0.5f, 1f);
        medallionRt.anchorMax = new Vector2(0.5f, 1f);
        medallionRt.pivot = new Vector2(0.5f, 0.5f);
        medallionRt.anchoredPosition = new Vector2(0f, -34f);
        medallionRt.sizeDelta = new Vector2(52f, 52f);
        celestialMedallionImage = medallionGO.GetComponent<Image>();
        celestialMedallionImage.sprite = GetOrCreateSunCrestSprite();
        celestialMedallionImage.raycastTarget = false;

        var defaultFont = GetFontAsset();

        // 8. Category Tag Text (Chapter / Eclipse Kicker Overline)
        var catGO = GetOrCreateChild(bannerRoot, "CategoryTagText", typeof(TextMeshProUGUI));
        var catRect = catGO.GetComponent<RectTransform>();
        catRect.anchorMin = new Vector2(0.10f, 0.63f);
        catRect.anchorMax = new Vector2(0.90f, 0.74f);
        catRect.offsetMin = Vector2.zero;
        catRect.offsetMax = Vector2.zero;
        categoryTagText = catGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) categoryTagText.font = defaultFont;
        categoryTagText.fontSize = 11.5f;
        categoryTagText.fontStyle = FontStyles.Bold;
        categoryTagText.characterSpacing = 8.5f;
        categoryTagText.alignment = TextAlignmentOptions.Center;
        categoryTagText.raycastTarget = false;

        // 9. Main Hero Display Title ("DAY X" / "NIGHTFALL") + Left/Right Filigree Wings
        var titleGO = GetOrCreateChild(bannerRoot, "TitleText", typeof(TextMeshProUGUI));
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.15f, 0.36f);
        titleRect.anchorMax = new Vector2(0.85f, 0.64f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleText.font = defaultFont;
        titleText.fontSize = 44f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 8f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        var leftWingGO = GetOrCreateChild(bannerRoot, "LeftTitleWing", typeof(Image));
        var leftWingRt = leftWingGO.GetComponent<RectTransform>();
        leftWingRt.anchorMin = new Vector2(0.16f, 0.50f);
        leftWingRt.anchorMax = new Vector2(0.36f, 0.50f);
        leftWingRt.pivot = new Vector2(1f, 0.5f);
        leftWingRt.anchoredPosition = Vector2.zero;
        leftWingRt.sizeDelta = new Vector2(0f, 2f);
        leftTitleWingImage = leftWingGO.GetComponent<Image>();
        leftTitleWingImage.sprite = GetOrCreateFlareSprite();
        leftTitleWingImage.raycastTarget = false;

        var rightWingGO = GetOrCreateChild(bannerRoot, "RightTitleWing", typeof(Image));
        var rightWingRt = rightWingGO.GetComponent<RectTransform>();
        rightWingRt.anchorMin = new Vector2(0.64f, 0.50f);
        rightWingRt.anchorMax = new Vector2(0.84f, 0.50f);
        rightWingRt.pivot = new Vector2(0f, 0.5f);
        rightWingRt.anchoredPosition = Vector2.zero;
        rightWingRt.sizeDelta = new Vector2(0f, 2f);
        rightTitleWingImage = rightWingGO.GetComponent<Image>();
        rightTitleWingImage.sprite = GetOrCreateFlareSprite();
        rightTitleWingImage.raycastTarget = false;

        // 10. Editorial Narrative Subtitle
        var subGO = GetOrCreateChild(bannerRoot, "SubtitleText", typeof(TextMeshProUGUI));
        var subRect = subGO.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.12f, 0.21f);
        subRect.anchorMax = new Vector2(0.88f, 0.36f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) subtitleText.font = defaultFont;
        subtitleText.fontSize = 14f;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.characterSpacing = 1.2f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.raycastTarget = false;

        // 11. Sleek Tactical Phase Telemetry Ribbon (StatusPill)
        var pillGO = GetOrCreateChild(bannerRoot, "StatusPill", typeof(Image));
        var pillRt = pillGO.GetComponent<RectTransform>();
        pillRt.anchorMin = new Vector2(0.5f, 0.045f);
        pillRt.anchorMax = new Vector2(0.5f, 0.045f);
        pillRt.pivot = new Vector2(0.5f, 0f);
        pillRt.anchoredPosition = Vector2.zero;
        pillRt.sizeDelta = new Vector2(760f, 26f);
        statusPillBg = pillGO.GetComponent<Image>();
        statusPillBg.sprite = GetOrCreateFeatheredRibbonSprite();
        statusPillBg.type = Image.Type.Simple;
        statusPillBg.color = new Color(0.05f, 0.07f, 0.12f, 0.82f);
        statusPillBg.raycastTarget = false;

        var pillRuleGO = GetOrCreateChild(pillRt, "TopRule", typeof(Image));
        var pillRuleRt = pillRuleGO.GetComponent<RectTransform>();
        pillRuleRt.anchorMin = new Vector2(0.1f, 1f);
        pillRuleRt.anchorMax = new Vector2(0.9f, 1f);
        pillRuleRt.pivot = new Vector2(0.5f, 1f);
        pillRuleRt.anchoredPosition = Vector2.zero;
        pillRuleRt.sizeDelta = new Vector2(0f, 1.5f);
        statusPillTopRule = pillRuleGO.GetComponent<Image>();
        statusPillTopRule.sprite = GetOrCreateFlareSprite();
        statusPillTopRule.raycastTarget = false;

        var pillTextGO = GetOrCreateChild(pillRt, "Text", typeof(TextMeshProUGUI));
        var pillTextRt = pillTextGO.GetComponent<RectTransform>();
        pillTextRt.anchorMin = Vector2.zero;
        pillTextRt.anchorMax = Vector2.one;
        pillTextRt.offsetMin = Vector2.zero;
        pillTextRt.offsetMax = Vector2.zero;
        statusPillText = pillTextGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statusPillText.font = defaultFont;
        statusPillText.fontSize = 11f;
        statusPillText.richText = true;
        statusPillText.characterSpacing = 2.0f;
        statusPillText.alignment = TextAlignmentOptions.Center;
        statusPillText.raycastTarget = false;

        // 12. Legacy IconText compatibility (hidden/clear)
        var iconGO = GetOrCreateChild(bannerRoot, "IconText", typeof(TextMeshProUGUI));
        iconText = iconGO.GetComponent<TextMeshProUGUI>();
        iconText.raycastTarget = false;
        iconText.text = "";

        // 13. Atmospheric Motes Container
        var motesGO = GetOrCreateChild(transform, "AtmosphereMotes");
        motesContainer = motesGO.GetComponent<RectTransform>();
        motesContainer.anchorMin = new Vector2(0.5f, 1f);
        motesContainer.anchorMax = new Vector2(0.5f, 1f);
        motesContainer.pivot = new Vector2(0.5f, 1f);
        motesContainer.anchoredPosition = baseBannerPosition;
        motesContainer.sizeDelta = new Vector2(1100f, 220f);

        BuildParticleMotes(motesContainer);
    }

    private static GameObject GetOrCreateChild(Transform parent, string childName, params Type[] components)
    {
        var existing = parent.Find(childName);
        if (existing != null)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (existing.GetComponent(components[i]) == null)
                {
                    existing.gameObject.AddComponent(components[i]);
                }
            }
            return existing.gameObject;
        }

        var go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++)
        {
            if (go.GetComponent(components[i]) == null)
            {
                go.AddComponent(components[i]);
            }
        }
        return go;
    }

    private void BuildParticleMotes(RectTransform container)
    {
        const int count = 14;
        _moteImages = new Image[count];
        _moteBasePositions = new Vector2[count];
        _moteSpeeds = new float[count];
        _motePhaseOffsets = new float[count];

        Sprite moteSprite = GetOrCreateSparkleMoteSprite();

        for (int i = 0; i < count; i++)
        {
            var moteName = $"Mote_{i}";
            var moteChild = GetOrCreateChild(container, moteName, typeof(Image));

            var rt = moteChild.GetComponent<RectTransform>();
            float size = UnityEngine.Random.Range(4.5f, 8.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            Vector2 rndPos = new Vector2(
                UnityEngine.Random.Range(-460f, 460f),
                UnityEngine.Random.Range(-85f, 85f)
            );
            rt.anchoredPosition = rndPos;

            var img = moteChild.GetComponent<Image>();
            img.sprite = moteSprite;
            img.raycastTarget = false;
            moteChild.SetActive(false);

            _moteImages[i] = img;
            _moteBasePositions[i] = rndPos;
            _moteSpeeds[i] = UnityEngine.Random.Range(0.8f, 1.6f);
            _motePhaseOffsets[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }
    }

    #endregion
}
