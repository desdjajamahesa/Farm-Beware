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
public partial class DayTransitionUI : MonoBehaviour
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
    public TimeManager.DayPhase LastPlayedPhase => lastPlayedPhase;
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

    private void Start()
    {
        EnsureUIHierarchy();
        SubscribeTimeEvents();
    }

    private void OnEnable()
    {
        SubscribeTimeEvents();
    }

    private void SubscribeTimeEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            TimeManager.Instance.OnDayChanged += HandleDayChanged;
            TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }

        if (DayNightTimeManager.Instance != null)
        {
            DayNightTimeManager.Instance.OnTimePhaseChanged -= HandleTimePhaseChanged;
            DayNightTimeManager.Instance.OnTimePhaseChanged += HandleTimePhaseChanged;
            DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
            DayNightTimeManager.Instance.OnDayChanged += HandleDayChanged;
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
            DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
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
            int d1 = DayNightTimeManager.Instance != null ? DayNightTimeManager.Instance.CurrentDay : 0;
            int d2 = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0;
            int currentDay = Mathf.Max(d1, d2);
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
            int d1 = DayNightTimeManager.Instance != null ? DayNightTimeManager.Instance.CurrentDay : 0;
            int d2 = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0;
            int currentDay = Mathf.Max(d1, d2);
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

        lastPlayedPhase = (TimeManager.DayPhase)(-1);
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
            if (mb is IDialogueDismissible dismissible)
            {
                dismissible.DismissDialogue();
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
                        "<color=#FF5252>•</color>  <b>STANCE: COMBAT BRAWL</b>     <color=#5A3A42>|</color>     " +
                        "<color=#FFB74D>•</color>  <b>FARM TOOLS LOCKED</b>     <color=#5A3A42>|</color>     " +
                        "<color=#FF8A80>•</color>  <b>DEFEND THE BARRICADES</b>";
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
                        "<color=#FFD54F>•</color>  <b>STANCE: AGRICULTURAL</b>     <color=#4A5568>|</color>     " +
                        "<color=#66BB6A>•</color>  <b>CROPS GROWING</b>     <color=#4A5568>|</color>     " +
                        "<color=#4FC3F7>•</color>  <b>WELL & MARKET OPEN</b>";
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
}
