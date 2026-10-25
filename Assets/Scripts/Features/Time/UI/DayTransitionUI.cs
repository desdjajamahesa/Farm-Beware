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
/// Orchestrates Prestige CinemaScope letterbox bars, anamorphic horizon flare beams,
/// glassmorphic obsidian titlecards, atmospheric solar dust/crimson ember motes,
/// procedural orchestral/doom bell acoustic synthesis, and organic ease-curve animations.
/// Fully decoupled and compliant with Clean Architecture, Zero-GC runtime ticks, and 100% English mandate.
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
    #region Singleton & Accessors

    private static DayTransitionUI _instance;
    public static DayTransitionUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DayTransitionUI>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    var go = new GameObject("DayTransitionUI");
                    _instance = go.AddComponent<DayTransitionUI>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public static DayTransitionUI EnsureInstance() => Instance;

    #endregion

    #region Inspector Fields & UI References

    [Header("Hierarchy Containers")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private Image vignetteOverlay;
    [SerializeField] private RectTransform bannerRoot;
    [SerializeField] private Image bannerBackgroundImage;
    [SerializeField] private Image bannerAccentLine;
    [SerializeField] private Image bannerTopAccentLine;
    [SerializeField] private Image horizonFlareBeam;
    [SerializeField] private RectTransform cinematicBarTop;
    [SerializeField] private RectTransform cinematicBarBottom;

    [Header("Typography")]
    [SerializeField] private TextMeshProUGUI categoryTagText;
    [SerializeField] private TextMeshProUGUI iconText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI statusPillText;
    [SerializeField] private Image statusPillBg;

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
    [SerializeField] private float entranceDuration = 0.55f;
    [SerializeField] private float displayDuration = 2.20f;
    [SerializeField] private float exitDuration = 0.55f;

    // State tracking
    private Coroutine transitionCoroutine;
    private Coroutine cameraShakeCoroutine;
    private int lastPlayedDay = -1;
    private TimeManager.DayPhase lastPlayedPhase = (TimeManager.DayPhase)(-1);
    private float lastPlayedRealTime = -999f;
    private int lastTransitionFrame = -1;
    private Vector2 baseBannerPosition = new Vector2(0f, -135f);

    public CanvasGroup CanvasGroupRef => transitionCanvasGroup;
    public TextMeshProUGUI TitleTextRef => titleText;
    public TextMeshProUGUI SubtitleTextRef => subtitleText;
    public bool IsPlayingTransition => transitionCoroutine != null;
    public Image VignetteOverlayRef => vignetteOverlay;

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
            horizonFlareBeam.rectTransform.sizeDelta = new Vector2(0f, 8f);
            Color fc = horizonFlareBeam.color;
            fc.a = 0f;
            horizonFlareBeam.color = fc;
        }

        if (cinematicBarTop != null)
        {
            cinematicBarTop.anchoredPosition = new Vector2(0f, 75f);
        }

        if (cinematicBarBottom != null)
        {
            cinematicBarBottom.anchoredPosition = new Vector2(0f, -75f);
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

        EnsureUIHierarchy();
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CinematicTransitionRoutine(isNight: true, dayNumber: 0));
    }

    /// <summary>
    /// Plays the AAA cinematic transition from Night to Day (Sunrise, dawn renewal, and homestead awaking).
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

        EnsureUIHierarchy();
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CinematicTransitionRoutine(isNight: false, dayNumber: dayNumber));
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
            // 1. Theme Configuration
            Color titleColor;
            Color subtitleColor;
            Color categoryColor;
            Color accentColor;
            Color flareColor;
            Color vignetteTargetColor;
            Color cardBgColor;

            if (isNight)
            {
                if (iconText != null) iconText.text = "";
                if (categoryTagText != null)
                {
                    categoryTagText.text = "✦   T H E   B L O O D   M O O N   E C L I P S E   ✦";
                    categoryTagText.color = new Color(0.98f, 0.45f, 0.45f, 0.90f);
                }

                if (titleText != null)
                {
                    titleText.text = "NIGHTFALL";
                    titleText.color = new Color(1.0f, 0.35f, 0.35f); // Deep Crimson
                }

                if (subtitleText != null)
                {
                    subtitleText.text = "The crimson shadows awaken. Defend the homestead!";
                    subtitleText.color = new Color(0.98f, 0.72f, 0.72f);
                }

                if (statusPillText != null)
                {
                    statusPillText.text = "⚔️ NIGHT BRAWL COMMENCED   •   DEFEND BARRICADES";
                    statusPillText.color = new Color(1f, 0.82f, 0.82f);
                }

                if (statusPillBg != null)
                {
                    statusPillBg.color = new Color(0.40f, 0.08f, 0.12f, 0.75f);
                }

                titleColor = new Color(1.0f, 0.35f, 0.35f);
                subtitleColor = new Color(0.98f, 0.72f, 0.72f);
                categoryColor = new Color(0.98f, 0.45f, 0.45f);
                accentColor = new Color(0.95f, 0.24f, 0.24f, 0.95f);
                flareColor = new Color(1.0f, 0.30f, 0.30f, 0.85f);
                vignetteTargetColor = new Color(0.32f, 0.02f, 0.05f, 0.46f); // Blood Vignette
                cardBgColor = new Color(0.09f, 0.03f, 0.05f, 0.95f); // Deep Plum Obsidian

                PlaySound(isNight: true);
                if (Application.isPlaying)
                {
                    cameraShakeCoroutine = StartCoroutine(RoutineCameraMicroShake(0.20f, 0.06f));
                }
            }
            else
            {
                int safeDay = Mathf.Max(1, dayNumber);
                string roman = ToRomanNumeral(safeDay);

                if (iconText != null) iconText.text = "";
                if (categoryTagText != null)
                {
                    categoryTagText.text = $"✦   D A Y   {roman}   •   A   N E W   D A W N   A W A K E N S   ✦";
                    categoryTagText.color = new Color(1.0f, 0.85f, 0.40f, 0.90f);
                }

                if (titleText != null)
                {
                    titleText.text = $"DAY {safeDay}";
                    titleText.color = new Color(1.0f, 0.82f, 0.25f); // Sunlight Amber Gold
                }

                if (subtitleText != null)
                {
                    subtitleText.text = "A gentle golden dawn breaks over the soil. Tend to your farm!";
                    subtitleText.color = new Color(0.99f, 0.92f, 0.75f);
                }

                if (statusPillText != null)
                {
                    statusPillText.text = "🌱 CROPS GROWING   •   💧 WELLS REFILLED   •   💰 SHOP OPEN";
                    statusPillText.color = new Color(1.0f, 0.95f, 0.80f);
                }

                if (statusPillBg != null)
                {
                    statusPillBg.color = new Color(0.35f, 0.25f, 0.05f, 0.70f);
                }

                titleColor = new Color(1.0f, 0.82f, 0.25f);
                subtitleColor = new Color(0.99f, 0.92f, 0.75f);
                categoryColor = new Color(1.0f, 0.85f, 0.40f);
                accentColor = new Color(1.0f, 0.82f, 0.25f, 0.95f);
                flareColor = new Color(1.0f, 0.90f, 0.45f, 0.85f);
                vignetteTargetColor = new Color(0.95f, 0.80f, 0.35f, 0.28f); // Golden Dawn Glow
                cardBgColor = new Color(0.05f, 0.07f, 0.11f, 0.94f); // Midnight Slate Obsidian

                PlaySound(isNight: false);
            }

            if (bannerBackgroundImage != null) bannerBackgroundImage.color = cardBgColor;
            if (bannerAccentLine != null) bannerAccentLine.color = accentColor;
            if (bannerTopAccentLine != null) bannerTopAccentLine.color = accentColor;
            if (horizonFlareBeam != null) horizonFlareBeam.color = new Color(flareColor.r, flareColor.g, flareColor.b, 0f);

            ConfigureMotes(isNight);

            // 2. Entrance Phase (0.55s): CinemaScope Letterbox + Horizon Flare Expansion + Titlecard Spring
            float inDuration = Mathf.Max(entranceDuration, 0.1f);
            for (float t = 0f; t < inDuration; t += Time.unscaledDeltaTime)
            {
                float norm = Mathf.Clamp01(t / inDuration);
                float easeOutCubic = 1f - Mathf.Pow(1f - norm, 3f);
                float springScale = Mathf.LerpUnclamped(0.85f, 1.0f, EaseOutBack(norm));

                // Alpha Ramp
                if (transitionCanvasGroup != null)
                    transitionCanvasGroup.alpha = norm;

                // Letterbox Bars
                if (cinematicBarTop != null)
                    cinematicBarTop.anchoredPosition = new Vector2(0f, Mathf.Lerp(75f, 0f, easeOutCubic));
                if (cinematicBarBottom != null)
                    cinematicBarBottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(-75f, 0f, easeOutCubic));

                // Vignette
                if (vignetteOverlay != null)
                {
                    Color vc = vignetteTargetColor;
                    vc.a = Mathf.Lerp(0f, vignetteTargetColor.a, easeOutCubic);
                    vignetteOverlay.color = vc;
                }

                // Anamorphic Lens Flare Expansion
                if (horizonFlareBeam != null)
                {
                    float flareWidth = Mathf.Lerp(0f, 1500f, easeOutCubic);
                    horizonFlareBeam.rectTransform.sizeDelta = new Vector2(flareWidth, 8f);
                    Color fc = flareColor;
                    fc.a = Mathf.Lerp(0f, flareColor.a, Mathf.Sin(norm * Mathf.PI * 0.5f));
                    horizonFlareBeam.color = fc;
                }

                // Banner Scale & Vertical Rise
                if (bannerRoot != null)
                {
                    bannerRoot.localScale = new Vector3(springScale, springScale, 1f);
                    float yPos = Mathf.Lerp(baseBannerPosition.y - 25f, baseBannerPosition.y, easeOutCubic);
                    bannerRoot.anchoredPosition = new Vector2(0f, yPos);
                }

                yield return null;
            }

            // Snap entrance values
            if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;
            if (cinematicBarTop != null) cinematicBarTop.anchoredPosition = Vector2.zero;
            if (cinematicBarBottom != null) cinematicBarBottom.anchoredPosition = Vector2.zero;
            if (bannerRoot != null) bannerRoot.localScale = Vector3.one;

            // 3. Hold Phase & Ken Burns Breathing (2.2s)
            float holdDuration = Mathf.Max(displayDuration, 0.5f);
            for (float t = 0f; t < holdDuration; t += Time.unscaledDeltaTime)
            {
                // Organic Floating Drift
                float floatOffset = Mathf.Sin(t * 2.8f) * 3.5f;
                if (bannerRoot != null)
                {
                    bannerRoot.anchoredPosition = baseBannerPosition + new Vector2(0f, floatOffset);
                }

                // Horizon Flare Gentle Breath
                if (horizonFlareBeam != null)
                {
                    float breathAlpha = flareColor.a * (0.80f + 0.20f * Mathf.Sin(t * 3.2f));
                    Color fc = flareColor;
                    fc.a = breathAlpha;
                    horizonFlareBeam.color = fc;
                }

                // Atmospheric Particle Motes Animation
                AnimateMotes(t, isNight);

                yield return null;
            }

            // 4. Resolve & Exit Phase (0.55s)
            float outDuration = Mathf.Max(exitDuration, 0.1f);
            for (float t = 0f; t < outDuration; t += Time.unscaledDeltaTime)
            {
                float norm = Mathf.Clamp01(t / outDuration);
                float easeInCubic = norm * norm * norm;
                float currentAlpha = 1f - norm;
                float scaleDrift = Mathf.Lerp(1.0f, 1.05f, norm);

                if (transitionCanvasGroup != null)
                    transitionCanvasGroup.alpha = currentAlpha;

                // Letterbox Bars retreat
                if (cinematicBarTop != null)
                    cinematicBarTop.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 75f, easeInCubic));
                if (cinematicBarBottom != null)
                    cinematicBarBottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -75f, easeInCubic));

                // Flare collapse
                if (horizonFlareBeam != null)
                {
                    float flareWidth = Mathf.Lerp(1500f, 0f, easeInCubic);
                    horizonFlareBeam.rectTransform.sizeDelta = new Vector2(flareWidth, 8f);
                }

                // Banner expansion dissolve
                if (bannerRoot != null)
                {
                    bannerRoot.localScale = new Vector3(scaleDrift, scaleDrift, 1f);
                }

                // Vignette fade
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
        }
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
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
            ? new Color(0.98f, 0.28f, 0.28f, 0.75f)  // Crimson Cinder
            : new Color(1.00f, 0.88f, 0.35f, 0.70f); // Golden Sun Dust

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
                ? -((elapsed * speed * 25f + phase * 40f) % 90f) // Falling embers
                : ((elapsed * speed * 25f + phase * 40f) % 90f);  // Rising sun motes

            float xSway = Mathf.Sin((elapsed + phase) * 2.5f) * 12f;

            rt.anchoredPosition = basePos + new Vector2(xSway, yOffset);

            // Shimmer alpha
            Color c = _moteImages[i].color;
            c.a = 0.30f + 0.45f * Mathf.Sin((elapsed + phase) * 4f);
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

        // 4-Layer Harmonic Awakening:
        // Warm C3 130.8Hz body + 6-note Celesta/Harp Arpeggio (C4, E4, G4, B4, D5, G5) + Bell resonance
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

        // 4-Layer Nightfall Doom Toll:
        // 50Hz sub-bass shockwave + D2 73.4Hz Cathedral Bell with tritone partials + dread drone rumble
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
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normDist - 0.35f) / 0.65f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedVignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _cachedVignetteSprite;
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
                float alpha = Mathf.Pow(nx, 2.4f) * Mathf.Pow(ny, 1.4f);
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedFlareSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _cachedFlareSprite;
    }

    private static Sprite _cachedRoundedCardSprite;
    private static Sprite GetOrCreateRoundedCardSprite()
    {
        if (_cachedRoundedCardSprite != null) return _cachedRoundedCardSprite;

        int size = 128;
        int radius = 22;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cx = x < radius ? radius : (x > size - 1 - radius ? size - 1 - radius : x);
                int cy = y < radius ? radius : (y > size - 1 - radius ? size - 1 - radius : y);
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float alpha = 1f - Mathf.Clamp01(dist - radius + 0.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        _cachedRoundedCardSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return _cachedRoundedCardSprite;
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
    /// Builds the AAA luxury UI hierarchy if not already configured in the Inspector.
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
            canvas.sortingOrder = 92;

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
            if (raycaster != null)
            {
                if (Application.isPlaying) Destroy(raycaster);
                else DestroyImmediate(raycaster);
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                if (Application.isPlaying) Destroy(scaler);
                else DestroyImmediate(scaler);
            }

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                if (Application.isPlaying) Destroy(canvas);
                else DestroyImmediate(canvas);
            }

            transform.SetAsLastSibling();
        }

        // Clean up legacy unused DayText child if present
        var legacyDayText = transform.Find("DayText");
        if (legacyDayText != null)
        {
            if (Application.isPlaying) Destroy(legacyDayText.gameObject);
            else DestroyImmediate(legacyDayText.gameObject);
        }

        // CanvasGroup
        transitionCanvasGroup = GetComponent<CanvasGroup>();
        if (transitionCanvasGroup == null)
            transitionCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        transitionCanvasGroup.alpha = 0f;
        transitionCanvasGroup.blocksRaycasts = false;
        transitionCanvasGroup.interactable = false;

        // AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // 2. Fullscreen Vignette Overlay
        var vignetteGO = transform.Find("VignetteOverlay")?.gameObject;
        if (vignetteGO == null)
        {
            vignetteGO = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
            vignetteGO.transform.SetParent(transform, false);
        }
        var vignetteRect = vignetteGO.GetComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;
        vignetteOverlay = vignetteGO.GetComponent<Image>();
        vignetteOverlay.sprite = GetOrCreateVignetteSprite();
        vignetteOverlay.color = new Color(0f, 0f, 0f, 0f);
        vignetteOverlay.raycastTarget = false;

        // 3. CinemaScope Letterbox Bars
        var barTopGO = transform.Find("CinematicBar_Top")?.gameObject;
        if (barTopGO == null)
        {
            barTopGO = new GameObject("CinematicBar_Top", typeof(RectTransform), typeof(Image));
            barTopGO.transform.SetParent(transform, false);
        }
        cinematicBarTop = barTopGO.GetComponent<RectTransform>();
        cinematicBarTop.anchorMin = new Vector2(0f, 1f);
        cinematicBarTop.anchorMax = new Vector2(1f, 1f);
        cinematicBarTop.pivot = new Vector2(0.5f, 1f);
        cinematicBarTop.sizeDelta = new Vector2(0f, 75f);
        cinematicBarTop.anchoredPosition = new Vector2(0f, 75f); // Initially off-screen
        var barTopImg = barTopGO.GetComponent<Image>();
        barTopImg.color = new Color(0.02f, 0.03f, 0.05f, 1f);
        barTopImg.raycastTarget = false;

        var barBottomGO = transform.Find("CinematicBar_Bottom")?.gameObject;
        if (barBottomGO == null)
        {
            barBottomGO = new GameObject("CinematicBar_Bottom", typeof(RectTransform), typeof(Image));
            barBottomGO.transform.SetParent(transform, false);
        }
        cinematicBarBottom = barBottomGO.GetComponent<RectTransform>();
        cinematicBarBottom.anchorMin = new Vector2(0f, 0f);
        cinematicBarBottom.anchorMax = new Vector2(1f, 0f);
        cinematicBarBottom.pivot = new Vector2(0.5f, 0f);
        cinematicBarBottom.sizeDelta = new Vector2(0f, 75f);
        cinematicBarBottom.anchoredPosition = new Vector2(0f, -75f); // Initially off-screen
        var barBottomImg = barBottomGO.GetComponent<Image>();
        barBottomImg.color = new Color(0.02f, 0.03f, 0.05f, 1f);
        barBottomImg.raycastTarget = false;

        // 4. Horizon Flare Beam
        var flareGO = transform.Find("HorizonFlareBeam")?.gameObject;
        if (flareGO == null)
        {
            flareGO = new GameObject("HorizonFlareBeam", typeof(RectTransform), typeof(Image));
            flareGO.transform.SetParent(transform, false);
        }
        var flareRt = flareGO.GetComponent<RectTransform>();
        flareRt.anchorMin = new Vector2(0.5f, 1f);
        flareRt.anchorMax = new Vector2(0.5f, 1f);
        flareRt.pivot = new Vector2(0.5f, 0.5f);
        flareRt.anchoredPosition = new Vector2(0f, -135f);
        flareRt.sizeDelta = new Vector2(0f, 8f);
        horizonFlareBeam = flareGO.GetComponent<Image>();
        horizonFlareBeam.sprite = GetOrCreateFlareSprite();
        horizonFlareBeam.color = new Color(1f, 1f, 1f, 0f);
        horizonFlareBeam.raycastTarget = false;

        // 5. Banner Root Container
        var bannerGO = transform.Find("BannerRoot")?.gameObject;
        if (bannerGO == null)
        {
            bannerGO = new GameObject("BannerRoot", typeof(RectTransform), typeof(Image));
            bannerGO.transform.SetParent(transform, false);
        }
        bannerRoot = bannerGO.GetComponent<RectTransform>();
        bannerRoot.anchorMin = new Vector2(0.5f, 1f);
        bannerRoot.anchorMax = new Vector2(0.5f, 1f);
        bannerRoot.pivot = new Vector2(0.5f, 1f);
        bannerRoot.anchoredPosition = baseBannerPosition;
        bannerRoot.sizeDelta = new Vector2(680f, 130f);

        bannerBackgroundImage = bannerGO.GetComponent<Image>();
        bannerBackgroundImage.sprite = GetOrCreateRoundedCardSprite();
        bannerBackgroundImage.type = Image.Type.Sliced;
        bannerBackgroundImage.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);
        bannerBackgroundImage.raycastTarget = false;

        // 6. Accent Lines (Top & Bottom)
        var topAccentGO = bannerRoot.Find("TopAccentLine")?.gameObject;
        if (topAccentGO == null)
        {
            topAccentGO = new GameObject("TopAccentLine", typeof(RectTransform), typeof(Image));
            topAccentGO.transform.SetParent(bannerRoot, false);
        }
        var topAccentRect = topAccentGO.GetComponent<RectTransform>();
        topAccentRect.anchorMin = new Vector2(0.04f, 1f);
        topAccentRect.anchorMax = new Vector2(0.96f, 1f);
        topAccentRect.pivot = new Vector2(0.5f, 1f);
        topAccentRect.anchoredPosition = Vector2.zero;
        topAccentRect.sizeDelta = new Vector2(0f, 2f);
        bannerTopAccentLine = topAccentGO.GetComponent<Image>();
        bannerTopAccentLine.color = new Color(1f, 0.82f, 0.25f, 0.95f);
        bannerTopAccentLine.raycastTarget = false;

        var accentGO = bannerRoot.Find("AccentLine")?.gameObject;
        if (accentGO == null)
        {
            accentGO = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
            accentGO.transform.SetParent(bannerRoot, false);
        }
        var accentRect = accentGO.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.04f, 0f);
        accentRect.anchorMax = new Vector2(0.96f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 2.5f);
        bannerAccentLine = accentGO.GetComponent<Image>();
        bannerAccentLine.color = new Color(1f, 0.82f, 0.25f, 0.95f);
        bannerAccentLine.raycastTarget = false;

        var defaultFont = GetFontAsset();

        // 7. Category Tag Text (Pre-Header)
        var catGO = bannerRoot.Find("CategoryTagText")?.gameObject;
        if (catGO == null)
        {
            catGO = new GameObject("CategoryTagText", typeof(RectTransform), typeof(TextMeshProUGUI));
            catGO.transform.SetParent(bannerRoot, false);
        }
        var catRect = catGO.GetComponent<RectTransform>();
        catRect.anchorMin = new Vector2(0.05f, 0.76f);
        catRect.anchorMax = new Vector2(0.95f, 0.96f);
        catRect.offsetMin = Vector2.zero;
        catRect.offsetMax = Vector2.zero;
        categoryTagText = catGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) categoryTagText.font = defaultFont;
        categoryTagText.fontSize = 11f;
        categoryTagText.fontStyle = FontStyles.Bold;
        categoryTagText.characterSpacing = 3.5f;
        categoryTagText.alignment = TextAlignmentOptions.Center;
        categoryTagText.raycastTarget = false;

        // 8. Main Title Text (DAY X / NIGHTFALL)
        var titleGO = bannerRoot.Find("TitleText")?.gameObject;
        if (titleGO == null)
        {
            titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(bannerRoot, false);
        }
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.44f);
        titleRect.anchorMax = new Vector2(0.95f, 0.78f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleText.font = defaultFont;
        titleText.fontSize = 28f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 4f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        // 9. Subtitle Text
        var subGO = bannerRoot.Find("SubtitleText")?.gameObject;
        if (subGO == null)
        {
            subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(bannerRoot, false);
        }
        var subRect = subGO.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.05f, 0.22f);
        subRect.anchorMax = new Vector2(0.95f, 0.44f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) subtitleText.font = defaultFont;
        subtitleText.fontSize = 12.5f;
        subtitleText.fontStyle = FontStyles.Normal;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.raycastTarget = false;

        // 10. Status Pill Container & Text
        var pillGO = bannerRoot.Find("StatusPill")?.gameObject;
        if (pillGO == null)
        {
            pillGO = new GameObject("StatusPill", typeof(RectTransform), typeof(Image));
            pillGO.transform.SetParent(bannerRoot, false);
        }
        var pillRt = pillGO.GetComponent<RectTransform>();
        pillRt.anchorMin = new Vector2(0.5f, 0.05f);
        pillRt.anchorMax = new Vector2(0.5f, 0.05f);
        pillRt.pivot = new Vector2(0.5f, 0f);
        pillRt.sizeDelta = new Vector2(480f, 18f);
        statusPillBg = pillGO.GetComponent<Image>();
        statusPillBg.sprite = GetOrCreateRoundedCardSprite();
        statusPillBg.type = Image.Type.Sliced;
        statusPillBg.color = new Color(0f, 0f, 0f, 0.50f);
        statusPillBg.raycastTarget = false;

        var pillTextGO = pillGO.transform.Find("Text")?.gameObject;
        if (pillTextGO == null)
        {
            pillTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            pillTextGO.transform.SetParent(pillGO.transform, false);
        }
        var pillTextRt = pillTextGO.GetComponent<RectTransform>();
        pillTextRt.anchorMin = Vector2.zero;
        pillTextRt.anchorMax = Vector2.one;
        pillTextRt.offsetMin = Vector2.zero;
        pillTextRt.offsetMax = Vector2.zero;
        statusPillText = pillTextGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statusPillText.font = defaultFont;
        statusPillText.fontSize = 9f;
        statusPillText.fontStyle = FontStyles.Bold;
        statusPillText.characterSpacing = 1.2f;
        statusPillText.alignment = TextAlignmentOptions.Center;
        statusPillText.raycastTarget = false;

        // 11. Legacy IconText compatibility (hidden/clear)
        var iconGO = bannerRoot.Find("IconText")?.gameObject;
        if (iconGO == null)
        {
            iconGO = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));
            iconGO.transform.SetParent(bannerRoot, false);
        }
        iconText = iconGO.GetComponent<TextMeshProUGUI>();
        iconText.raycastTarget = false;
        iconText.text = "";

        // 12. Atmospheric Motes Container
        var motesGO = transform.Find("AtmosphereMotes")?.gameObject;
        if (motesGO == null)
        {
            motesGO = new GameObject("AtmosphereMotes", typeof(RectTransform));
            motesGO.transform.SetParent(transform, false);
        }
        motesContainer = motesGO.GetComponent<RectTransform>();
        motesContainer.anchorMin = new Vector2(0.5f, 1f);
        motesContainer.anchorMax = new Vector2(0.5f, 1f);
        motesContainer.pivot = new Vector2(0.5f, 1f);
        motesContainer.anchoredPosition = baseBannerPosition;
        motesContainer.sizeDelta = new Vector2(700f, 130f);

        BuildParticleMotes(motesContainer);
    }

    private void BuildParticleMotes(RectTransform container)
    {
        const int count = 10;
        _moteImages = new Image[count];
        _moteBasePositions = new Vector2[count];
        _moteSpeeds = new float[count];
        _motePhaseOffsets = new float[count];

        Sprite moteSprite = GetOrCreateSparkleMoteSprite();

        for (int i = 0; i < count; i++)
        {
            var moteName = $"Mote_{i}";
            var moteChild = container.Find(moteName)?.gameObject;
            if (moteChild == null)
            {
                moteChild = new GameObject(moteName, typeof(RectTransform), typeof(Image));
                moteChild.transform.SetParent(container, false);
            }

            var rt = moteChild.GetComponent<RectTransform>();
            float size = UnityEngine.Random.Range(4f, 7.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            Vector2 rndPos = new Vector2(
                UnityEngine.Random.Range(-320f, 320f),
                UnityEngine.Random.Range(-55f, 55f)
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