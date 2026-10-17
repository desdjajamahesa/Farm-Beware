using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FeaturesTime;
using FeaturesRendering.Lighting;
using FarmBeware.Core.Runtime;

/// <summary>
/// Mengelola animasi transisi visual pergantian waktu antara Siang (Day) dan Malam (Night).
/// Menampilkan banner glassmorphic animasi halus (Day ➔ Night: "🌙 NIGHTFALL", Night ➔ Day: "☀️ DAY X"),
/// aksen vignette atmosferik di tepi layar, dan efek audio chime harmonis tanpa membekukan kontrol pemain.
/// Mendukung auto-procedural hierarchy generation sehingga mandiri dan dapat berjalan di scene mana pun.
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
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

    [Header("UI Hierarchy References (Auto-Generated if Null)")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private Image vignetteOverlay;
    [SerializeField] private RectTransform bannerRoot;
    [SerializeField] private Image bannerBackgroundImage;
    [SerializeField] private Image bannerAccentLine;
    [SerializeField] private TextMeshProUGUI iconText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dayTransitionSfx;
    [SerializeField] private AudioClip nightTransitionSfx;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.65f;

    [Header("Animation Timings")]
    [SerializeField] private float entranceDuration = 0.45f;
    [SerializeField] private float displayDuration = 1.80f;
    [SerializeField] private float exitDuration = 0.50f;

    // State tracking
    private Coroutine transitionCoroutine;
    private int lastPlayedDay = -1;
    private TimeManager.DayPhase lastPlayedPhase = (TimeManager.DayPhase)(-1);
    private float lastPlayedRealTime = -999f;
    private Vector2 baseBannerPosition = new Vector2(0f, -140f);

    public CanvasGroup CanvasGroupRef => transitionCanvasGroup;
    public TextMeshProUGUI TitleTextRef => titleText;
    public TextMeshProUGUI SubtitleTextRef => subtitleText;
    public bool IsPlayingTransition => transitionCoroutine != null;
    public Image VignetteOverlayRef => vignetteOverlay;

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

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        EnsureUIHierarchy();
        SubscribeTimeEvents();
    }

    private void OnEnable()
    {
        EnsureUIHierarchy();
        SubscribeTimeEvents();
    }

    private void OnDisable()
    {
        CancelTransition();
        UnsubscribeTimeEvents();
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
        }
    }

    private void UnsubscribeTimeEvents()
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

    private void HandleDayChanged(int newDay)
    {
        PlayDayTransition(newDay);
    }

    private void HandlePhaseChanged(TimeManager.DayPhase phase)
    {
        if (phase == TimeManager.DayPhase.Night)
        {
            PlayNightTransition();
        }
        else if (phase == TimeManager.DayPhase.Day)
        {
            int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;
            PlayDayTransition(currentDay);
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
            int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;
            PlayDayTransition(currentDay);
        }
    }

    /// <summary>
    /// Membatalkan animasi transisi seketika dan menyembunyikan overlay (digunakan saat loading save).
    /// </summary>
    public void CancelTransition()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

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

        UnlockPlayer();
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

    /// <summary>
    /// Memutar animasi transisi visual Day ➔ Night (Nightfall).
    /// </summary>
    public void PlayNightTransition()
    {
        // Cegah pemicuan ganda dalam selang waktu < 1.0 detik
        if (Time.unscaledTime - lastPlayedRealTime < 1.0f && lastPlayedPhase == TimeManager.DayPhase.Night)
            return;

        lastPlayedPhase = TimeManager.DayPhase.Night;
        lastPlayedRealTime = Time.unscaledTime;

        EnsureUIHierarchy();
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionRoutine(isNight: true, dayNumber: 0));
    }

    /// <summary>
    /// Memutar animasi transisi visual Night ➔ Day (Sunrise / New Day).
    /// </summary>
    public void PlayDayTransition(int dayNumber)
    {
        if (Time.unscaledTime - lastPlayedRealTime < 1.0f && lastPlayedPhase == TimeManager.DayPhase.Day && lastPlayedDay == dayNumber)
            return;

        lastPlayedPhase = TimeManager.DayPhase.Day;
        lastPlayedDay = dayNumber;
        lastPlayedRealTime = Time.unscaledTime;

        EnsureUIHierarchy();
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionRoutine(isNight: false, dayNumber: dayNumber));
    }

    private IEnumerator TransitionRoutine(bool isNight, int dayNumber)
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
            // 1. Konfigurasi Visual & Konten Teks
            if (isNight)
        {
            if (iconText != null) iconText.text = "🌙";
            if (titleText != null)
            {
                titleText.text = "NIGHTFALL";
                titleText.color = new Color(0.98f, 0.44f, 0.44f); // Crimson Coral
            }
            if (subtitleText != null)
            {
                subtitleText.text = "The shadows lengthen. Defend the homestead!";
                subtitleText.color = new Color(0.99f, 0.72f, 0.72f);
            }
            if (bannerBackgroundImage != null)
            {
                bannerBackgroundImage.color = new Color(0.12f, 0.04f, 0.06f, 0.94f); // Deep Blood Plum
            }
            if (bannerAccentLine != null)
            {
                bannerAccentLine.color = new Color(0.94f, 0.27f, 0.27f, 0.90f); // Crimson Accent
            }
            if (vignetteOverlay != null)
            {
                vignetteOverlay.color = new Color(0.24f, 0.02f, 0.05f, 0f); // Crimson Edge Vignette
            }

            PlaySound(isNight: true);
        }
        else
        {
            if (iconText != null) iconText.text = "☀️";
            if (titleText != null)
            {
                titleText.text = $"DAY {Mathf.Max(1, dayNumber)}";
                titleText.color = new Color(0.98f, 0.75f, 0.14f); // Warm Sunlight Gold
            }
            if (subtitleText != null)
            {
                subtitleText.text = "A new dawn has arrived. Tend to your farm!";
                subtitleText.color = new Color(0.99f, 0.90f, 0.65f);
            }
            if (bannerBackgroundImage != null)
            {
                bannerBackgroundImage.color = new Color(0.06f, 0.09f, 0.14f, 0.94f); // Midnight Slate
            }
            if (bannerAccentLine != null)
            {
                bannerAccentLine.color = new Color(0.98f, 0.75f, 0.14f, 0.90f); // Golden Amber Accent
            }
            if (vignetteOverlay != null)
            {
                vignetteOverlay.color = new Color(1.0f, 0.85f, 0.40f, 0f); // Golden Dawn Glow
            }

            PlaySound(isNight: false);
        }

        float targetVignetteAlpha = isNight ? 0.38f : 0.22f;

        // 2. Tahap Masuk: Alpha Fade-In & Spring Scale (0.45s)
        float inDuration = Mathf.Max(entranceDuration, 0.1f);
        for (float t = 0f; t < inDuration; t += Time.unscaledDeltaTime)
        {
            float norm = Mathf.Clamp01(t / inDuration);
            // Ease-out back spring formula: f(x) = (c1 + 1)*x^3 - c1*x^2 + 1
            float scaleSpring = Mathf.LerpUnclamped(0.85f, 1.0f, EaseOutBack(norm));

            if (transitionCanvasGroup != null)
                transitionCanvasGroup.alpha = norm;

            if (bannerRoot != null)
            {
                bannerRoot.localScale = new Vector3(scaleSpring, scaleSpring, 1f);
                bannerRoot.anchoredPosition = baseBannerPosition;
            }

            if (vignetteOverlay != null)
            {
                Color vc = vignetteOverlay.color;
                vc.a = Mathf.Lerp(0f, targetVignetteAlpha, norm);
                vignetteOverlay.color = vc;
            }

            yield return null;
        }

        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;
        if (bannerRoot != null) bannerRoot.localScale = Vector3.one;

        // 3. Tahap Tahan & Floating Drift Halus (1.8s)
        float holdDuration = Mathf.Max(displayDuration, 0.5f);
        for (float t = 0f; t < holdDuration; t += Time.unscaledDeltaTime)
        {
            // Amplitudo apung lembut 3 piksel
            float floatOffset = Mathf.Sin(t * 3.5f) * 3f;
            if (bannerRoot != null)
            {
                bannerRoot.anchoredPosition = baseBannerPosition + new Vector2(0f, floatOffset);
            }
            yield return null;
        }

        // 4. Tahap Keluar: Alpha Fade-Out & Soft Scale Expansion (0.50s)
        float outDuration = Mathf.Max(exitDuration, 0.1f);
        for (float t = 0f; t < outDuration; t += Time.unscaledDeltaTime)
        {
            float norm = Mathf.Clamp01(t / outDuration);
            float currentAlpha = 1f - norm;
            float scaleDrift = Mathf.Lerp(1.0f, 1.05f, norm);

            if (transitionCanvasGroup != null)
                transitionCanvasGroup.alpha = currentAlpha;

            if (bannerRoot != null)
                bannerRoot.localScale = new Vector3(scaleDrift, scaleDrift, 1f);

            if (vignetteOverlay != null)
            {
                Color vc = vignetteOverlay.color;
                vc.a = Mathf.Lerp(targetVignetteAlpha, 0f, norm);
                vignetteOverlay.color = vc;
            }

            yield return null;
        }
        }
        finally
        {
            // Reset kondisi akhir
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 0f;
                transitionCanvasGroup.blocksRaycasts = false;
            }
            if (vignetteOverlay != null)
            {
                Color vc = vignetteOverlay.color;
                vc.a = 0f;
                vignetteOverlay.color = vc;
                vignetteOverlay.raycastTarget = false;
            }
            if (bannerRoot != null)
            {
                bannerRoot.localScale = Vector3.one;
                bannerRoot.anchoredPosition = baseBannerPosition;
            }

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

    private void PlaySound(bool isNight)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isNight ? nightTransitionSfx : dayTransitionSfx;
        if (clipToPlay == null)
        {
            clipToPlay = isNight ? GetOrCreateNightChime() : GetOrCreateDayChime();
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, sfxVolume);
        }
    }

    private static AudioClip _cachedDayChime;
    private static AudioClip _cachedNightChime;

    private static AudioClip GetOrCreateDayChime()
    {
        if (_cachedDayChime != null) return _cachedDayChime;

        int sampleRate = 44100;
        float duration = 1.2f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Melodi Fajar: C5 (523Hz) -> E5 (659Hz) -> G5 (784Hz)
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 2.8f);

            float noteFreq = (t < 0.18f) ? 523.25f : ((t < 0.36f) ? 659.25f : 783.99f);
            float tone = Mathf.Sin(2f * Mathf.PI * noteFreq * t) * 0.7f +
                         Mathf.Sin(4f * Mathf.PI * noteFreq * t) * 0.3f;

            samples[i] = tone * env * 0.5f;
        }

        _cachedDayChime = AudioClip.Create("Chime_DayTransition", sampleCount, 1, sampleRate, false);
        _cachedDayChime.SetData(samples, 0);
        return _cachedDayChime;
    }

    private static AudioClip GetOrCreateNightChime()
    {
        if (_cachedNightChime != null) return _cachedNightChime;

        int sampleRate = 44100;
        float duration = 1.5f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Lonceng Malam: Gong rendah 220Hz (A3) dengan overtones 440Hz & 660Hz decay lambat
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 1.8f);

            float tone = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.55f +
                         Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.30f +
                         Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.15f;

            samples[i] = tone * env * 0.6f;
        }

        _cachedNightChime = AudioClip.Create("Chime_NightTransition", sampleCount, 1, sampleRate, false);
        _cachedNightChime.SetData(samples, 0);
        return _cachedNightChime;
    }

    /// <summary>
    /// Membangun antarmuka visual secara prosedural jika belum dikonfigurasi di Inspector.
    /// </summary>
    public void EnsureUIHierarchy()
    {
        if (transitionCanvasGroup != null && bannerRoot != null && titleText != null)
            return;

        // 1. Pastikan Canvas overlay
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

        transitionCanvasGroup = GetComponent<CanvasGroup>();
        if (transitionCanvasGroup == null)
            transitionCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        transitionCanvasGroup.alpha = 0f;
        transitionCanvasGroup.blocksRaycasts = false;
        transitionCanvasGroup.interactable = false;

        // 2. AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D Stereo
        }

        // 3. Vignette Overlay (full screen)
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
        vignetteOverlay.color = new Color(0f, 0f, 0f, 0f);
        vignetteOverlay.raycastTarget = false;

        // 4. Banner Root Container
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
        bannerRoot.sizeDelta = new Vector2(580f, 96f);

        bannerBackgroundImage = bannerGO.GetComponent<Image>();
        bannerBackgroundImage.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        bannerBackgroundImage.raycastTarget = false;

        // 5. Accent Line di bawah banner
        var accentGO = bannerRoot.Find("AccentLine")?.gameObject;
        if (accentGO == null)
        {
            accentGO = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
            accentGO.transform.SetParent(bannerRoot, false);
        }
        var accentRect = accentGO.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(1f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 3f);
        bannerAccentLine = accentGO.GetComponent<Image>();
        bannerAccentLine.color = new Color(0.98f, 0.75f, 0.14f, 0.9f);
        bannerAccentLine.raycastTarget = false;

        // 6. Icon Text (🌙 / ☀️)
        var iconGO = bannerRoot.Find("IconText")?.gameObject;
        if (iconGO == null)
        {
            iconGO = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));
            iconGO.transform.SetParent(bannerRoot, false);
        }
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(0.18f, 1f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconText = iconGO.GetComponent<TextMeshProUGUI>();
        iconText.fontSize = 44;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.raycastTarget = false;
        iconText.text = "☀️";

        // 7. Title Text
        var titleGO = bannerRoot.Find("TitleText")?.gameObject;
        if (titleGO == null)
        {
            titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(bannerRoot, false);
        }
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.18f, 0.44f);
        titleRect.anchorMax = new Vector2(0.98f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.raycastTarget = false;
        titleText.text = "DAY 1";

        // 8. Subtitle Text
        var subGO = bannerRoot.Find("SubtitleText")?.gameObject;
        if (subGO == null)
        {
            subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(bannerRoot, false);
        }
        var subRect = subGO.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.18f, 0.10f);
        subRect.anchorMax = new Vector2(0.98f, 0.48f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 14;
        subtitleText.alignment = TextAlignmentOptions.TopLeft;
        subtitleText.raycastTarget = false;
        subtitleText.text = "A new dawn has arrived.";
    }
}