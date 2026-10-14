using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// PauseMenuAnimator brings the Farm Beware fantasy pause menu to life:
/// - Enlarged, grand fantasy wooden board plaque (clean unified artwork, no duplicate overlapping overlays).
/// - 5 compact, fully visible carved wooden plank buttons fitting cleanly inside the inner board box.
/// - Living Fairytale Cottage: warm flickering hearth fire in the window and doorway.
/// - Magic Crystals Aurora: pulsating ethereal glow and twinkling diamond star glints.
/// - Toxic Spores FX: glowing green acid droplets drifting gracefully from the flower monster's mouth.
/// - 2.5D mouse parallax with perspective card tilt.
/// - Operates smoothly at Time.timeScale = 0 using unscaledDeltaTime.
/// </summary>
public class PauseMenuAnimator : MonoBehaviour
{
    [Header("Main Containers & Dimensions")]
    [Tooltip("The central fantasy wooden board RectTransform.")]
    [SerializeField] private RectTransform cardRect;
    [Tooltip("Dark backdrop vignette overlay.")]
    [SerializeField] private Image backgroundOverlay;
    [Tooltip("Overall menu scale multiplier (enlarges the entire pause menu).")]
    [SerializeField] private float menuScale = 1.3f;
    [SerializeField] private float parallaxStrength = 10f;
    [SerializeField] private float parallaxSmoothSpeed = 5f;
    [SerializeField] private float cardPerspectiveTilt = 2.2f;

    [Header("Button Dimensions (Sized to fit inner board box)")]
    [SerializeField] private Vector2 buttonSize = new Vector2(290f, 52f);
    [SerializeField] private float buttonVerticalSpacing = 58f;

    [Header("Fairytale Mushroom Cottage (Left)")]
    [SerializeField] private CanvasGroup hearthGlowGroup;
    [SerializeField] private float hearthFlickerSpeed = 2.8f;

    [Header("Magic Crystals & Stars (Bottom)")]
    [SerializeField] private CanvasGroup crystalsGlowGroup;
    [SerializeField] private float crystalsPulseSpeed = 1.3f;
    [SerializeField] private RectTransform sparkleContainer;

    [Header("Spore Droplets FX (From Monster Flower)")]
    [SerializeField] private RectTransform sporeContainer;
    [SerializeField] private Sprite sporeSprite;

    [Header("Wooden Buttons & Sway")]
    [SerializeField] private List<RectTransform> buttonPlanks = new List<RectTransform>();
    [SerializeField] private float plankSwayAngle = 0.45f;
    [SerializeField] private float plankSwaySpeed = 1.3f;

    [Header("Modal Pop-In Animation")]
    [SerializeField] private float popInDuration = 0.22f;

    public float MenuScale => menuScale;

    // Internal State
    private Vector2 cardInitialPos;
    private Vector2 targetParallaxOffset;
    private Vector2 currentParallaxOffset;
    private float currentTiltX = 0f;
    private float currentTiltY = 0f;

    // Button initial states
    private List<Vector2> buttonInitialPositions = new List<Vector2>();
    private List<float> buttonSwayPhases = new List<float>();

    // Spore and Sparkle particle pools
    private class SporeParticle
    {
        public RectTransform rect;
        public Image img;
        public Vector2 velocity;
        public float life;
        public float maxLife;
    }
    private List<SporeParticle> activeSpores = new List<SporeParticle>();
    private float sporeSpawnTimer = 0f;

    private class SparkleEffect
    {
        public RectTransform rect;
        public Image img;
        public float life;
        public float maxLife;
        public float peakScale;
    }
    private List<SparkleEffect> activeSparkles = new List<SparkleEffect>();
    private float sparkleSpawnTimer = 0f;

    private MainMenuController menuController;
    private Coroutine popInCoroutine;
    private Sprite sparkleSprite;
    private Sprite buttonBacklightSprite;

    private void Awake()
    {
        menuController = GetComponent<MainMenuController>();
        EnsureFantasyElements();
        CacheInitialTransforms();
    }

    private void Start()
    {
        EnsureFantasyElements();
        CacheInitialTransforms();
    }

    private void OnEnable()
    {
        TriggerPopIn();
    }

    /// <summary>
    /// Auto-resolves and builds all fantasy visual layers, cleans up duplicate overlays,
    /// sets up 5 compact opaque buttons (staying cleanly inside the inner board), and scales the menu up.
    /// </summary>
    public void EnsureFantasyElements()
    {
        // 1. Resolve or find SignpostPanel
        if (cardRect == null)
        {
            var p = transform.Find("SignpostPanel") ?? transform.Find("MainMenuPanel");
            if (p != null) cardRect = p.GetComponent<RectTransform>();
        }

        if (cardRect == null) return;

        // Clean up any duplicate ChomperCreature objects so flower is unified ("jadi satu saja")
        for (int i = cardRect.childCount - 1; i >= 0; i--)
        {
            var child = cardRect.GetChild(i);
            if (child.name.StartsWith("ChomperCreature") || child.name.StartsWith("Chomper_Head"))
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Clean up any duplicate glow overlays
        CleanDuplicateChildren("CottageHearthGlow");
        CleanDuplicateChildren("CrystalsAuroraGlow");
        CleanDuplicateChildren("SporeContainer");
        CleanDuplicateChildren("SparkleContainer");

        // Ensure board size, clean sprite, and enlarged scale
        var boardImg = cardRect.GetComponent<Image>();
        if (boardImg != null)
        {
            boardImg.color = Color.white;
            boardImg.raycastTarget = false;
#if UNITY_EDITOR
            if (boardImg.sprite == null || boardImg.sprite.name != "Fantasy_Pause_Board_Clean")
            {
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/PauseMenu/Fantasy_Pause_Board_Clean.png");
                if (s != null) boardImg.sprite = s;
            }
#endif
        }
        cardRect.sizeDelta = new Vector2(1024f, 561f);
        cardRect.localScale = Vector3.one * menuScale;

        // 2. Resolve Background Overlay
        if (backgroundOverlay == null)
        {
            var bgObj = transform.Find("BackgroundContainer") ?? transform.Find("BackgroundOverlay");
            if (bgObj != null) backgroundOverlay = bgObj.GetComponent<Image>();
        }
        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0.02f, 0.03f, 0.06f, 0.72f);
        }

        // 3. Resolve or Create Cottage Hearth Light (warm light in cottage window/door)
        if (hearthGlowGroup == null)
        {
            var hObj = cardRect.Find("CottageHearthGlow");
            if (hObj != null) hearthGlowGroup = hObj.GetComponent<CanvasGroup>();
        }
        if (hearthGlowGroup == null)
        {
            var go = new GameObject("CottageHearthGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(cardRect, false);
            go.transform.SetSiblingIndex(0);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-325f, -20f);
            rt.sizeDelta = new Vector2(140f, 140f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
#if UNITY_EDITOR
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/PauseMenu/Cottage_Hearth_Glow.png");
            if (s != null) img.sprite = s;
#endif
            hearthGlowGroup = go.GetComponent<CanvasGroup>();
            hearthGlowGroup.alpha = 0.6f;
        }

        // 4. Resolve or Create Crystals Aurora Glow
        if (crystalsGlowGroup == null)
        {
            var crObj = cardRect.Find("CrystalsAuroraGlow");
            if (crObj != null) crystalsGlowGroup = crObj.GetComponent<CanvasGroup>();
        }
        if (crystalsGlowGroup == null)
        {
            var go = new GameObject("CrystalsAuroraGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(cardRect, false);
            go.transform.SetSiblingIndex(1);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(85f, -195f);
            rt.sizeDelta = new Vector2(380f, 160f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
#if UNITY_EDITOR
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/PauseMenu/Crystals_Aura_Glow.png");
            if (s != null) img.sprite = s;
#endif
            crystalsGlowGroup = go.GetComponent<CanvasGroup>();
            crystalsGlowGroup.alpha = 0.5f;
        }

        // 5. Resolve Particle Containers
        if (sporeContainer == null)
        {
            var sc = cardRect.Find("SporeContainer");
            if (sc != null) sporeContainer = sc.GetComponent<RectTransform>();
            else
            {
                var go = new GameObject("SporeContainer", typeof(RectTransform));
                go.transform.SetParent(cardRect, false);
                sporeContainer = go.GetComponent<RectTransform>();
                sporeContainer.anchoredPosition = Vector2.zero;
                sporeContainer.sizeDelta = cardRect.sizeDelta;
            }
        }

        if (sparkleContainer == null)
        {
            var spc = cardRect.Find("SparkleContainer");
            if (spc != null) sparkleContainer = spc.GetComponent<RectTransform>();
            else
            {
                var go = new GameObject("SparkleContainer", typeof(RectTransform));
                go.transform.SetParent(cardRect, false);
                sparkleContainer = go.GetComponent<RectTransform>();
                sparkleContainer.anchoredPosition = Vector2.zero;
                sparkleContainer.sizeDelta = cardRect.sizeDelta;
            }
        }

#if UNITY_EDITOR
        if (sporeSprite == null)
            sporeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/PauseMenu/Toxic_Spore_Particle.png");
        if (sparkleSprite == null)
            sparkleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/PauseMenu/Magic_Sparkle_Star.png");
        if (buttonBacklightSprite == null)
            buttonBacklightSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/PauseMenu/Plank_Backlight_Glow.png");
#endif

        // 6. Setup & Configure 5 Wooden Buttons (compact & centered, staying inside inner board box)
        SetupButtons();
    }

    private void CleanDuplicateChildren(string childName)
    {
        if (cardRect == null) return;
        var found = new List<Transform>();
        for (int i = 0; i < cardRect.childCount; i++)
        {
            var t = cardRect.GetChild(i);
            if (t.name == childName) found.Add(t);
        }
        for (int i = 1; i < found.Count; i++)
        {
            DestroyImmediate(found[i].gameObject);
        }
    }

    private void SetupButtons()
    {
        buttonPlanks.Clear();

        var resumeBtn = cardRect.Find("ResumeButton");
        var newGameBtn = cardRect.Find("NewGameButton") ?? cardRect.Find("CheckpointButton");
        var settingsBtn = cardRect.Find("SettingsButton");
        var mainMenuBtn = cardRect.Find("MainMenuButton") ?? cardRect.Find("QuitButton");
        var quitDesktopBtn = cardRect.Find("QuitDesktopButton");

        // If NewGameButton is missing, duplicate ResumeButton
        if (newGameBtn == null && resumeBtn != null)
        {
            var go = Instantiate(resumeBtn.gameObject, cardRect);
            go.name = "NewGameButton";
            newGameBtn = go.transform;
        }

        // Configure 5 compact buttons centered at y = 0
        // Vertical spacing = 58px. Centers: +116, +58, 0, -58, -116
        // Bounds: top is +142, bottom is -142 -> well within inner wood area (+175 to -180)!
        ConfigureButtonPlank(resumeBtn, "Btn_Resume_Plank", new Vector2(0f, buttonVerticalSpacing * 2f), 0);
        ConfigureButtonPlank(newGameBtn, "Btn_NewGame_Plank", new Vector2(0f, buttonVerticalSpacing), 1);
        ConfigureButtonPlank(settingsBtn, "Btn_Setting_Plank", new Vector2(0f, 0f), 2);
        ConfigureButtonPlank(mainMenuBtn, "Btn_MainMenu_Plank", new Vector2(0f, -buttonVerticalSpacing), 3);
        ConfigureButtonPlank(quitDesktopBtn, "Btn_ExitDesktop_Plank", new Vector2(0f, -buttonVerticalSpacing * 2f), 4);

        // Hide legacy PausedTitle text so board stays clean
        var oldTitle = cardRect.Find("PausedTitle") ?? cardRect.Find("GameTitle");
        if (oldTitle != null) oldTitle.gameObject.SetActive(false);
    }

    private void ConfigureButtonPlank(Transform btnT, string spriteName, Vector2 pos, int index)
    {
        if (btnT == null) return;
        var rt = btnT.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = buttonSize;
        rt.localScale = Vector3.one;

        // Ensure Image is 100% opaque white with assigned plank sprite
        var img = btnT.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.white;
            img.raycastTarget = true;
#if UNITY_EDITOR
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Textures/PauseMenu/{spriteName}.png");
            if (s != null) img.sprite = s;
#endif
        }

        // Ensure Button colors have alpha = 1 (NOT transparent!)
        var btn = btnT.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
        }

        // Ensure CanvasGroup on button has alpha = 1
        var cg = btnT.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // Disable UIAnimationHandler tint override so it doesn't dim or hide the button
        var uiah = btnT.GetComponent<UIAnimationHandler>();
        if (uiah != null)
        {
            uiah.enabled = false;
        }

        // Disable TMP text child so clean carved sprite text displays clearly
        var txt = btnT.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (txt != null) txt.gameObject.SetActive(false);

        // Add backlight glow child for rich warm hover effect
        var backlight = btnT.Find("BacklightHalo");
        if (backlight == null && buttonBacklightSprite != null)
        {
            var bgObj = new GameObject("BacklightHalo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            bgObj.transform.SetParent(btnT, false);
            bgObj.transform.SetAsFirstSibling();
            var brt = bgObj.GetComponent<RectTransform>();
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(buttonSize.x + 35f, buttonSize.y + 24f);
            var bimg = bgObj.GetComponent<Image>();
            bimg.sprite = buttonBacklightSprite;
            bimg.raycastTarget = false;
            var bcg = bgObj.GetComponent<CanvasGroup>();
            bcg.alpha = 0f;
        }
        else if (backlight != null)
        {
            var brt = backlight.GetComponent<RectTransform>();
            if (brt != null) brt.sizeDelta = new Vector2(buttonSize.x + 35f, buttonSize.y + 24f);
        }

        // Clean up redundant EventTriggers
        var triggers = btnT.GetComponents<EventTrigger>();
        for (int i = 1; i < triggers.Length; i++) DestroyImmediate(triggers[i]);
        var trigger = btnT.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnT.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((_) => OnButtonHoverEnter(rt, index));
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((_) => OnButtonHoverExit(rt));
        trigger.triggers.Add(exitEntry);

        buttonPlanks.Add(rt);
    }

    private void CacheInitialTransforms()
    {
        if (cardRect != null)
        {
            cardInitialPos = cardRect.anchoredPosition;
            cardRect.localScale = Vector3.one * menuScale;
        }

        buttonInitialPositions.Clear();
        buttonSwayPhases.Clear();
        for (int i = 0; i < buttonPlanks.Count; i++)
        {
            if (buttonPlanks[i] != null)
            {
                buttonPlanks[i].localScale = Vector3.one;
                buttonInitialPositions.Add(buttonPlanks[i].anchoredPosition);
                buttonSwayPhases.Add(i * 0.95f);
            }
        }
    }

    public void TriggerPopIn()
    {
        EnsureFantasyElements();
        CacheInitialTransforms();

        if (popInCoroutine != null) StopCoroutine(popInCoroutine);
        popInCoroutine = StartCoroutine(AnimatePopIn());
    }

    private IEnumerator AnimatePopIn()
    {
        float elapsed = 0f;
        Vector2 startPos = cardInitialPos + new Vector2(0f, 35f);
        Vector3 targetScale = Vector3.one * menuScale;
        Vector3 startScale = targetScale * 0.88f;

        cardRect.anchoredPosition = startPos;
        cardRect.localScale = startScale;

        // Keep all buttons at scale 1 immediately so they are fully visible
        for (int i = 0; i < buttonPlanks.Count; i++)
        {
            if (buttonPlanks[i] != null) buttonPlanks[i].localScale = Vector3.one;
        }

        while (elapsed < popInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popInDuration);

            // EaseOutBack for bouncy elastic feel
            float c1 = 1.4f;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            cardRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, ease);
            cardRect.anchoredPosition = Vector2.LerpUnclamped(startPos, cardInitialPos, ease);

            yield return null;
        }

        cardRect.localScale = targetScale;
        cardRect.anchoredPosition = cardInitialPos;
        cardRect.localEulerAngles = Vector3.zero;
        popInCoroutine = null;
    }

    private void Update()
    {
        if (menuController != null && !menuController.IsMenuActive)
            return;

        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        UpdateMouseParallaxAndPerspective(dt);
        UpdateCottageHearth(time);
        UpdateMagicCrystals(time, dt);
        UpdateWoodenPlankSway(time);
        UpdateToxicSpores(dt);
        UpdateMagicSparkles(dt);
    }

    #region 2.5D Parallax & Perspective Card Tilt
    private void UpdateMouseParallaxAndPerspective(float dt)
    {
        if (cardRect == null) return;

        Vector2 mousePos = Vector2.zero;
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
        else
        {
            try { mousePos = Input.mousePosition; } catch { }
        }

        float normX = Mathf.Clamp((mousePos.x / Screen.width - 0.5f) * 2f, -1f, 1f);
        float normY = Mathf.Clamp((mousePos.y / Screen.height - 0.5f) * 2f, -1f, 1f);

        targetParallaxOffset = new Vector2(normX * parallaxStrength, normY * parallaxStrength * 0.7f);
        currentParallaxOffset = Vector2.Lerp(currentParallaxOffset, targetParallaxOffset, dt * parallaxSmoothSpeed);

        if (popInCoroutine == null)
        {
            cardRect.anchoredPosition = cardInitialPos + currentParallaxOffset;
        }

        currentTiltX = Mathf.Lerp(currentTiltX, -normY * cardPerspectiveTilt, dt * parallaxSmoothSpeed);
        currentTiltY = Mathf.Lerp(currentTiltY, normX * cardPerspectiveTilt * 1.3f, dt * parallaxSmoothSpeed);

        if (popInCoroutine == null)
        {
            cardRect.localEulerAngles = new Vector3(currentTiltX, currentTiltY, 0f);
        }
    }
    #endregion

    #region Button Interactions
    private void OnButtonHoverEnter(RectTransform btn, int index)
    {
        btn.localScale = new Vector3(1.06f, 1.06f, 1f);

        var halo = btn.Find("BacklightHalo");
        if (halo != null)
        {
            var cg = halo.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        // Spawn a burst of glowing spore droplets from monster flower mouth
        SpawnSporeBurst(2);
    }

    private void OnButtonHoverExit(RectTransform btn)
    {
        btn.localScale = Vector3.one;

        var halo = btn.Find("BacklightHalo");
        if (halo != null)
        {
            var cg = halo.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }
    }
    #endregion

    #region Fairytale Cottage Hearth Fire
    private void UpdateCottageHearth(float time)
    {
        if (hearthGlowGroup == null) return;

        float baseSine = (Mathf.Sin(time * hearthFlickerSpeed) + 1f) * 0.5f;
        float jitter = Mathf.PerlinNoise(time * 4.5f, 0f) * 0.25f;
        hearthGlowGroup.alpha = Mathf.Clamp(0.38f + baseSine * 0.45f + jitter, 0.2f, 1f);
    }
    #endregion

    #region Magic Crystals Aurora & Twinkles
    private void UpdateMagicCrystals(float time, float dt)
    {
        if (crystalsGlowGroup != null)
        {
            float pulse = (Mathf.Sin(time * crystalsPulseSpeed) + 1f) * 0.5f;
            crystalsGlowGroup.alpha = Mathf.Lerp(0.32f, 0.78f, pulse);
        }

        sparkleSpawnTimer += dt;
        if (sparkleSpawnTimer > 0.45f)
        {
            sparkleSpawnTimer = 0f;
            SpawnRandomSparkle();
        }
    }

    private void SpawnRandomSparkle()
    {
        if (sparkleContainer == null || sparkleSprite == null) return;

        Vector2 spawnPos = new Vector2(
            Random.Range(20f, 220f),
            Random.Range(-230f, -170f)
        );

        var go = new GameObject("CrystalSparkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(sparkleContainer, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = spawnPos;
        rt.sizeDelta = new Vector2(32f, 32f);

        var img = go.GetComponent<Image>();
        img.sprite = sparkleSprite;
        img.raycastTarget = false;
        img.color = new Color(0.85f, 0.95f, 1f, 0f);

        activeSparkles.Add(new SparkleEffect
        {
            rect = rt,
            img = img,
            life = 0f,
            maxLife = Random.Range(0.45f, 0.75f),
            peakScale = Random.Range(0.8f, 1.4f)
        });
    }

    private void UpdateMagicSparkles(float dt)
    {
        for (int i = activeSparkles.Count - 1; i >= 0; i--)
        {
            var sp = activeSparkles[i];
            sp.life += dt;
            float progress = sp.life / sp.maxLife;

            if (progress >= 1f)
            {
                if (sp.rect != null) Destroy(sp.rect.gameObject);
                activeSparkles.RemoveAt(i);
                continue;
            }

            float curve = Mathf.Sin(progress * Mathf.PI);
            if (sp.rect != null)
            {
                sp.rect.localScale = Vector3.one * (curve * sp.peakScale);
                sp.rect.localEulerAngles = new Vector3(0f, 0f, progress * 90f);
            }
            if (sp.img != null)
            {
                sp.img.color = new Color(0.9f, 0.98f, 1f, curve);
            }
        }
    }
    #endregion

    #region Toxic Spores FX
    private void SpawnSporeBurst(int count)
    {
        if (sporeContainer == null || sporeSprite == null) return;

        // Position near monster flower's open mouth (approx x: 285, y: 55)
        Vector2 mouthPos = new Vector2(285f, 55f);

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("SporeDroplet", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(sporeContainer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = mouthPos + new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
            float size = Random.Range(18f, 28f);
            rt.sizeDelta = new Vector2(size, size);

            var img = go.GetComponent<Image>();
            img.sprite = sporeSprite;
            img.raycastTarget = false;
            img.color = new Color(0.4f, 1f, 0.2f, 0.85f);

            Vector2 vel = new Vector2(
                Random.Range(-60f, -25f),
                Random.Range(-35f, 15f)
            );

            activeSpores.Add(new SporeParticle
            {
                rect = rt,
                img = img,
                velocity = vel,
                life = 0f,
                maxLife = Random.Range(1.2f, 1.9f)
            });
        }
    }

    private void UpdateToxicSpores(float dt)
    {
        sporeSpawnTimer += dt;
        if (sporeSpawnTimer > 0.85f)
        {
            sporeSpawnTimer = 0f;
            SpawnSporeBurst(1);
        }

        for (int i = activeSpores.Count - 1; i >= 0; i--)
        {
            var sp = activeSpores[i];
            sp.life += dt;
            float progress = sp.life / sp.maxLife;

            if (progress >= 1f)
            {
                if (sp.rect != null) Destroy(sp.rect.gameObject);
                activeSpores.RemoveAt(i);
                continue;
            }

            if (sp.rect != null)
            {
                float wobbleY = Mathf.Sin(sp.life * 4.5f) * 8f;
                sp.rect.anchoredPosition += (sp.velocity + new Vector2(0f, wobbleY)) * dt;
                float alpha = Mathf.Clamp01((1f - progress) * 1.2f);
                if (sp.img != null)
                {
                    sp.img.color = new Color(0.45f, 1f, 0.25f, alpha * 0.85f);
                }
            }
        }
    }
    #endregion

    #region Wooden Buttons Idle Breeze Sway
    private void UpdateWoodenPlankSway(float time)
    {
        for (int i = 0; i < buttonPlanks.Count; i++)
        {
            if (buttonPlanks[i] == null) continue;
            float phase = i < buttonSwayPhases.Count ? buttonSwayPhases[i] : (i * 0.8f);
            float angle = Mathf.Sin(time * plankSwaySpeed + phase) * plankSwayAngle;
            buttonPlanks[i].localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }
    #endregion
}
