using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FeaturesCommon;

public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Background")]
    [SerializeField] private Image backgroundOverlay;

    [Header("Title Animation")]
    [SerializeField] private float titleFloatAmplitude = 5f;
    [SerializeField] private float titleFloatSpeed = 0.8f;
    [SerializeField] private float titleScaleBreathAmount = 0.015f;
    [SerializeField] private float titleScaleBreathSpeed = 1.0f;

    [Header("Animation Timing")]
    [SerializeField] private float bgFadeDelay = 0f;
    [SerializeField] private float titleFadeDelay = 0.2f;
    [SerializeField] private float startBtnDelay = 0.5f;
    [SerializeField] private float settingsBtnDelay = 0.65f;
    [SerializeField] private float quitBtnDelay = 0.8f;
    [SerializeField] private float elementFadeDuration = 0.35f;

    [Header("Scene Transition")]
    [Tooltip("Target gameplay scene to load when Start Game is clicked.")]
    [SerializeField] private string targetSceneName = "StagingScene";
    [Tooltip("Whether to load the target scene when the menu fades out.")]
    [SerializeField] private bool loadSceneOnStart = true;

    [Header("Legacy Style Override")]
    [Tooltip("Enable to use old programmatic layout/colors instead of custom inspector art.")]
    [SerializeField] private bool useLegacyCodeStyling = false;

    private PlayerControl playerControl;
    private bool menuActive = false;
    private Vector3 titleOriginalPos;
    private Vector3 titleOriginalScale;
    private Camera mainCamera;
    private Vector3 cameraOriginalPos;

    private enum MenuState { Hidden, FadingIn, Active, SettingsOpening, SettingsOpen, SettingsClosing, FadingOut }
    private MenuState currentState = MenuState.Hidden;
    private float stateTimer = 0f;
    private float stateDuration = 0f;

    private CanvasGroup bgOverlayCG;
    private CanvasGroup titleCG;
    private CanvasGroup mainPanelCG;
    private CanvasGroup settingsCG;
    private System.DateTime lastFrameTime;

    void Awake()
    {
        ResolveReferences();
        playerControl = FindFirstObjectByType<PlayerControl>();
        mainCamera = Camera.main;

        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
    }

    private void ResolveReferences()
    {
        if (menuCanvasGroup == null) menuCanvasGroup = GetComponent<CanvasGroup>();
        if (mainMenuPanel == null)
        { var t = transform.Find("MainMenuPanel"); if (t != null) mainMenuPanel = t.gameObject; }
        if (settingsPanel == null)
        { var t = transform.Find("SettingsPanel"); if (t != null) settingsPanel = t.gameObject; }
        if (startButton == null && mainMenuPanel != null)
        { var t = mainMenuPanel.transform.Find("StartButton"); if (t != null) startButton = t.GetComponent<Button>(); }
        if (settingsButton == null && mainMenuPanel != null)
        { var t = mainMenuPanel.transform.Find("SettingsButton"); if (t != null) settingsButton = t.GetComponent<Button>(); }
        if (quitButton == null && mainMenuPanel != null)
        { var t = mainMenuPanel.transform.Find("QuitButton"); if (t != null) quitButton = t.GetComponent<Button>(); }
        if (settingsBackButton == null && settingsPanel != null)
        { var t = settingsPanel.transform.Find("BackButton"); if (t != null) settingsBackButton = t.GetComponent<Button>(); }
        if (titleText == null)
        {
            var t = transform.Find("GameTitle");
            if (t == null) t = transform.Find("TitleText");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }
        if (backgroundOverlay == null)
        { var t = transform.Find("BackgroundOverlay"); if (t != null) backgroundOverlay = t.GetComponent<Image>(); }
    }

    void Start()
    {
        lastFrameTime = System.DateTime.UtcNow;
        if (useLegacyCodeStyling)
        {
            SetupLayout();
            ApplyVisualStyling();
            SetupAtmosphere();
        }
        ShowMenu();
    }

    void Update()
    {
        System.DateTime now = System.DateTime.UtcNow;
        float dt = (float)(now - lastFrameTime).TotalSeconds;
        lastFrameTime = now;
        if (dt > 2f) dt = 2f;
        if (dt <= 0f) dt = 0.001f;

        switch (currentState)
        {
            case MenuState.FadingIn:
                UpdateFadingIn(dt);
                break;
            case MenuState.Active:
                if (useLegacyCodeStyling)
                {
                    UpdateTitleIdle(dt);
                    UpdateCameraSway(dt);
                }
                break;
            case MenuState.SettingsOpening:
                UpdateSettingsOpening(dt);
                break;
            case MenuState.SettingsClosing:
                UpdateSettingsClosing(dt);
                break;
            case MenuState.FadingOut:
                UpdateFadingOut(dt);
                break;
        }
    }

    private void UpdateFadingIn(float dt)
    {
        stateTimer += dt;

        if (bgOverlayCG != null)
        {
            float t = Mathf.Clamp01((stateTimer - bgFadeDelay) / elementFadeDuration);
            bgOverlayCG.alpha = EaseOutCubic(t);
        }

        if (titleCG != null && titleText != null)
        {
            float t = Mathf.Clamp01((stateTimer - titleFadeDelay) / elementFadeDuration);
            titleCG.alpha = EaseOutCubic(t);
            float scaleT = EaseOutBack(Mathf.Clamp01((stateTimer - titleFadeDelay) / elementFadeDuration));
            titleText.rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, scaleT);
        }

        float[] delays = { startBtnDelay, settingsBtnDelay, quitBtnDelay };
        for (int i = 0; i < mainPanelCG.transform.childCount && i < delays.Length; i++)
        {
            var child = mainPanelCG.transform.GetChild(i);
            var cg = child.GetComponent<CanvasGroup>();
            if (cg == null) continue;

            float t = Mathf.Clamp01((stateTimer - delays[i]) / elementFadeDuration);
            cg.alpha = EaseOutCubic(t);
            Vector3 targetPos = child.localPosition;
            Vector3 startPos = targetPos + Vector3.down * 25f;
            child.localPosition = Vector3.Lerp(startPos, targetPos, EaseOutCubic(t));
        }

        if (stateTimer >= 1.2f)
        {
            currentState = MenuState.Active;
            titleOriginalPos = titleText != null ? titleText.rectTransform.localPosition : Vector3.zero;
            titleOriginalScale = titleText != null ? titleText.rectTransform.localScale : Vector3.one;
        }
    }

    private void UpdateFadingOut(float dt)
    {
        stateTimer += dt;
        float t = Mathf.Clamp01(stateTimer / stateDuration);
        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 1f - EaseOutCubic(t);

        if (t >= 1f)
        {
            gameObject.SetActive(false);
            if (FadeManager.Instance != null)
                FadeManager.Instance.FadeOut(0.3f);
            LockPlayerInput(false);
            currentState = MenuState.Hidden;

            if (loadSceneOnStart && !string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log($"[MainMenuController] Loading target scene: {targetSceneName}");
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }

    private void UpdateTitleIdle(float dt)
    {
        if (titleText == null) return;
        float time = (float)(System.DateTime.UtcNow - System.DateTime.MinValue).TotalSeconds;
        float yOffset = Mathf.Sin(time * titleFloatSpeed * Mathf.PI * 2f) * titleFloatAmplitude;
        float scaleBreath = 1f + Mathf.Sin(time * titleScaleBreathSpeed * Mathf.PI * 2f) * titleScaleBreathAmount;
        titleText.rectTransform.localPosition = titleOriginalPos + Vector3.up * yOffset;
        titleText.rectTransform.localScale = titleOriginalScale * scaleBreath;
    }

    private void UpdateCameraSway(float dt)
    {
        if (mainCamera == null) return;
        float time = (float)(System.DateTime.UtcNow - System.DateTime.MinValue).TotalSeconds;
        float x = Mathf.Sin(time * 0.15f) * 0.3f;
        float y = Mathf.Sin(time * 0.1f + 1.5f) * 0.15f;
        mainCamera.transform.position = cameraOriginalPos + new Vector3(x, y, 0f);
    }

    private float EaseOutCubic(float t) { return 1f - Mathf.Pow(1f - t, 3f); }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void SetupLayout()
    {
        if (titleText != null)
        {
            var rt = titleText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localPosition = new Vector3(0, 180, 0);
            rt.sizeDelta = new Vector2(1000, 160);
        }

        if (mainMenuPanel != null)
        {
            var rt = mainMenuPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localPosition = new Vector3(0, -30, 0);
            rt.sizeDelta = new Vector2(400, 220);

            var vlg = mainMenuPanel.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = mainMenuPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(0, 0, 5, 5);

            foreach (Transform child in mainMenuPanel.transform)
            {
                var le = child.GetComponent<LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 360;
                le.preferredHeight = 55;
            }
        }

        SetupSettingsPanel();
    }

    private void SetupSettingsPanel()
    {
        if (settingsPanel == null) return;
        var rt = settingsPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localPosition = Vector3.zero;
        rt.sizeDelta = new Vector2(560, 480);

        var vlg = settingsPanel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = settingsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.padding = new RectOffset(50, 50, 30, 30);

        var bg = settingsPanel.transform.Find("SettingsBackground");
        if (bg != null)
        {
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.localPosition = Vector3.zero;
            var bgImg = bg.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(0.06f, 0.07f, 0.10f, 0.94f);
        }

        var st = settingsPanel.transform.Find("SettingsTitle");
        if (st != null)
        {
            var stLE = st.GetComponent<LayoutElement>();
            if (stLE == null) stLE = st.gameObject.AddComponent<LayoutElement>();
            stLE.preferredHeight = 55;
            stLE.flexibleWidth = 1;
            var tmp = st.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.fontSize = 34; tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold; tmp.color = new Color(0.95f, 0.92f, 0.82f, 1f); }
        }

        string[] rowNames = { "VolumeRow", "FullscreenRow", "ResolutionRow", "QualityRow" };
        foreach (string rn in rowNames)
        {
            var row = settingsPanel.transform.Find(rn);
            if (row == null) continue;
            var rowLE = row.GetComponent<LayoutElement>();
            if (rowLE == null) rowLE = row.gameObject.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 44;
            rowLE.flexibleWidth = 1;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.padding = new RectOffset(10, 10, 0, 0);

            foreach (Transform child in row)
            {
                var le = child.GetComponent<LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                var label = child.GetComponent<TextMeshProUGUI>();
                if (label != null) { le.preferredWidth = 140; le.flexibleWidth = 0; label.fontSize = 20; label.alignment = TextAlignmentOptions.MidlineLeft; label.color = new Color(0.85f, 0.85f, 0.85f, 1f); }
                else { le.preferredWidth = 280; le.flexibleWidth = 1; }
            }
        }

        var backBtn = settingsPanel.transform.Find("BackButton");
        if (backBtn != null)
        {
            var backLE = backBtn.GetComponent<LayoutElement>();
            if (backLE == null) backLE = backBtn.gameObject.AddComponent<LayoutElement>();
            backLE.preferredHeight = 48;
            backLE.flexibleWidth = 1;
            backLE.minWidth = 240;
            var backImg = backBtn.GetComponent<Image>();
            if (backImg != null) backImg.color = new Color(0.22f, 0.22f, 0.28f, 0.9f);
            EnsureAnimationHandler(backBtn.gameObject);
        }
    }

    private void ApplyVisualStyling()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.fontSize = 52;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.95f, 0.92f, 0.82f, 1f);
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0f, 0f, 0.05f, 0.55f);
        }

        Color btnBg = new Color(0.12f, 0.11f, 0.16f, 0.85f);
        Color btnText = new Color(0.95f, 0.92f, 0.82f, 1f);
        SetupButton(startButton, "START", btnBg, btnText);
        SetupButton(settingsButton, "SETTINGS", btnBg, btnText);
        SetupButton(quitButton, "QUIT", btnBg, btnText);
    }

    private void SetupButton(Button btn, string label, Color bgColor, Color textColor)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = bgColor;
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.gameObject.SetActive(true);
            txt.text = label;
            txt.fontSize = 26;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = textColor;
        }
        EnsureAnimationHandler(btn.gameObject);
    }

    private void EnsureAnimationHandler(GameObject go)
    {
        if (go.GetComponent<UIAnimationHandler>() == null)
            go.AddComponent<UIAnimationHandler>();
    }

    private void SetupAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.55f, 0.5f, 0.45f, 1f);
        RenderSettings.ambientIntensity = 0.85f;

        var dl = GameObject.Find("Directional Light");
        if (dl != null)
        {
            var l = dl.GetComponent<Light>();
            if (l != null) { l.color = new Color(1f, 0.92f, 0.78f, 1f); l.intensity = 1.1f; }
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0f, 0f, 0.05f, 0.55f);
            backgroundOverlay.type = Image.Type.Simple;
            backgroundOverlay.raycastTarget = false;
        }
    }

    public void ShowMenu()
    {
        menuActive = true;
        gameObject.SetActive(true);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        LockPlayerInput(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.blocksRaycasts = true;
            menuCanvasGroup.interactable = true;
        }

        if (backgroundOverlay != null)
        {
            bgOverlayCG = backgroundOverlay.GetComponent<CanvasGroup>();
            if (bgOverlayCG == null) bgOverlayCG = backgroundOverlay.gameObject.AddComponent<CanvasGroup>();
            bgOverlayCG.alpha = 1f;
        }

        if (titleText != null)
        {
            titleCG = titleText.GetComponent<CanvasGroup>();
            if (titleCG == null) titleCG = titleText.gameObject.AddComponent<CanvasGroup>();
            titleCG.alpha = 1f;
        }

        if (mainMenuPanel != null)
        {
            mainPanelCG = mainMenuPanel.GetComponent<CanvasGroup>();
            if (mainPanelCG == null) mainPanelCG = mainMenuPanel.AddComponent<CanvasGroup>();
            mainPanelCG.alpha = 1f;

            foreach (Transform child in mainMenuPanel.transform)
            {
                var cg = child.GetComponent<CanvasGroup>();
                if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
            }
        }

        if (settingsPanel != null)
        {
            settingsCG = settingsPanel.GetComponent<CanvasGroup>();
            if (settingsCG == null) settingsCG = settingsPanel.AddComponent<CanvasGroup>();
            settingsCG.alpha = 1f;
        }

        stateTimer = 0f;
        currentState = MenuState.Active;

        titleOriginalPos = titleText != null ? titleText.rectTransform.localPosition : Vector3.zero;
        titleOriginalScale = titleText != null ? titleText.rectTransform.localScale : Vector3.one;

        if (mainCamera != null)
            cameraOriginalPos = mainCamera.transform.position;
    }

    private void OnStartClicked()
    {
        if (!menuActive) return;
        menuActive = false;
        currentState = MenuState.FadingOut;
        stateTimer = 0f;
        stateDuration = 0.4f;

        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeIn(0.4f);
    }

    private void OnSettingsClicked()
    {
        if (!menuActive) return;
        stateTimer = 0f;
        currentState = MenuState.SettingsOpening;
    }

    private void OnSettingsBackClicked()
    {
        stateTimer = 0f;
        currentState = MenuState.SettingsClosing;
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        Debug.Log("[MainMenu] Quit requested.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateSettingsOpening(float dt)
    {
        stateTimer += dt;
        float fadeDur = 0.15f;
        float showDur = 0.25f;

        if (stateTimer <= fadeDur)
        {
            float t = EaseOutCubic(Mathf.Clamp01(stateTimer / fadeDur));
            if (mainPanelCG != null) mainPanelCG.alpha = 1f - t;
        }
        else if (stateTimer <= fadeDur + 0.01f)
        {
            if (mainPanelCG != null) mainPanelCG.alpha = 0f;
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                if (settingsCG == null) { settingsCG = settingsPanel.GetComponent<CanvasGroup>(); if (settingsCG == null) settingsCG = settingsPanel.AddComponent<CanvasGroup>(); }
                settingsPanel.transform.localScale = Vector3.one * 0.9f;
                settingsCG.alpha = 0f;
            }
        }
        else
        {
            float elapsed = stateTimer - fadeDur - 0.01f;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / showDur));
            if (settingsCG != null) settingsCG.alpha = t;
            if (settingsPanel != null) settingsPanel.transform.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, t);

            if (t >= 1f)
            {
                currentState = MenuState.SettingsOpen;
                if (settingsPanel != null) settingsPanel.transform.localScale = Vector3.one;
            }
        }
    }

    private void UpdateSettingsClosing(float dt)
    {
        stateTimer += dt;
        float hideDur = 0.15f;
        float showDur = 0.15f;

        if (stateTimer <= hideDur)
        {
            float t = EaseOutCubic(Mathf.Clamp01(stateTimer / hideDur));
            if (settingsCG != null) settingsCG.alpha = 1f - t;
            if (settingsPanel != null) settingsPanel.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.95f, t);
        }
        else if (stateTimer <= hideDur + 0.01f)
        {
            if (settingsCG != null) settingsCG.alpha = 0f;
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
        else
        {
            float elapsed = stateTimer - hideDur - 0.01f;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / showDur));
            if (mainMenuPanel != null && !mainMenuPanel.activeSelf) mainMenuPanel.SetActive(true);
            if (mainPanelCG != null) mainPanelCG.alpha = t;

            if (t >= 1f)
            {
                currentState = MenuState.Active;
                UpdateTitleIdle(0);
                UpdateCameraSway(0);
            }
        }
    }

    public void LockPlayerInput(bool lockInput)
    {
        if (playerControl == null) playerControl = FindFirstObjectByType<PlayerControl>();
        if (playerControl != null) playerControl.isInputLocked = lockInput;
    }

    public bool IsMenuActive => menuActive;
}
