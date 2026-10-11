using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using FarmBeware.Core.Runtime;
using FeaturesSaveSystem;

/// <summary>
/// Death Screen Menu UI (YOU DIED).
/// Muncul saat pemain tewas / collapsed:
/// 1. Layar meredup dengan vignet gelap & tulisan "YOU DIED" muncul dengan animasi scale & fade.
/// 2. Setelah animasi teks selesai (atau di-skip), banner bergeser ke atas dan menu pilihan muncul:
///    - Checkpoint: Bangun di kasur kamar tidur pada pagi hari sebelum malam tiba dengan vital pulih.
///    - Load Game: Membuka modal Save & Load system multi-slot untuk memuat file simpanan.
///    - Main Menu: Keluar ke scene menu utama secara mulus.
/// </summary>
public class DeathScreenUI : MonoBehaviour, IModalWindow
{
    public static bool HasInstance => instance != null;
    private static bool isApplicationQuitting = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        isApplicationQuitting = false;
        instance = null;
    }

    public static DeathScreenUI Instance
    {
        get
        {
            if (isApplicationQuitting && !Application.isPlaying) return null;

            if (instance == null)
            {
                instance = FindFirstObjectByType<DeathScreenUI>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    var canvas = GameObject.Find("UI_Canvas");
                    if (canvas == null)
                    {
                        var c = FindFirstObjectByType<Canvas>();
                        if (c != null) canvas = c.gameObject;
                    }

                    if (canvas != null)
                    {
                        var panelTr = canvas.transform.Find("Panel_DeathScreen");
                        if (panelTr != null)
                        {
                            instance = panelTr.GetComponent<DeathScreenUI>() ?? panelTr.gameObject.AddComponent<DeathScreenUI>();
                        }
                    }

                    if (instance == null)
                    {
                        var go = new GameObject("DeathScreenUI");
                        instance = go.AddComponent<DeathScreenUI>();
                    }
                }
            }
            return instance;
        }
        private set => instance = value;
    }
    private static DeathScreenUI instance;

    [Header("UI Root")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private CanvasGroup modalCanvasGroup;
    [SerializeField] private Image backdropImage;
    [Tooltip("Tingkat kegelapan latar belakang layar. 0f = sepenuhnya transparan.")]
    [SerializeField] private float backdropMaxAlpha = 0f;

    [Header("Banner 'YOU DIED'")]
    [SerializeField] private RectTransform bannerContainer;
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private Image youDiedImage;
    [SerializeField] private TextMeshProUGUI youDiedFallbackText;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Menu Card & Buttons")]
    [SerializeField] private RectTransform menuCard;
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private Button checkpointButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;

    public bool IsOpen => isOpen;
    public bool CanDismissWithEscape => false;

    private bool isOpen = false;
    private bool isIntroAnimating = false;
    private bool isHandlingAction = false;
    private bool isBrowsingSaveUI = false;
    private Coroutine activeSequenceCoroutine;

    private readonly Vector2 bannerCenterPos = Vector2.zero;
    private readonly Vector2 bannerHeaderPos = new Vector2(0f, 168f);
    private readonly Vector3 bannerCenterScale = Vector3.one;
    private readonly Vector3 bannerHeaderScale = new Vector3(0.88f, 0.88f, 1f);

    private readonly Vector2 menuStartPos = new Vector2(0f, -80f);
    private readonly Vector2 menuFinalPos = new Vector2(0f, -44f);

    public void OpenModal() => Open();
    public void CloseModal() => Close();

    private void Awake()
    {
        isApplicationQuitting = false;

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
        }

        EnsureUIHierarchy();
        WireButtons();
    }

    private void WireButtons()
    {
        if (checkpointButton != null)
        {
            checkpointButton.onClick.RemoveListener(OnCheckpointClicked);
            checkpointButton.onClick.AddListener(OnCheckpointClicked);
        }
        if (loadGameButton != null)
        {
            loadGameButton.onClick.RemoveListener(OnLoadGameClicked);
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void OnEnable()
    {
        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.OnLoadCompleted += HandleLoadCompleted;
        }

        if (SaveSystemUI.Instance != null)
        {
            SaveSystemUI.Instance.OnSaveUIClosed += HandleSaveUIClosed;
        }
    }

    private void OnDisable()
    {
        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.OnLoadCompleted -= HandleLoadCompleted;
        }

        if (SaveSystemUI.HasInstance && SaveSystemUI.Instance != null)
        {
            SaveSystemUI.Instance.OnSaveUIClosed -= HandleSaveUIClosed;
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }
        if (instance == this)
        {
            instance = null;
        }
    }

    private void HandleLoadCompleted(GameSaveData saveData)
    {
        isBrowsingSaveUI = false;
        CloseInstant();
    }

    private void HandleSaveUIClosed()
    {
        if (SaveSystemUI.Instance != null)
        {
            SaveSystemUI.Instance.OnSaveUIClosed -= HandleSaveUIClosed;
        }

        if (isBrowsingSaveUI && !isHandlingAction)
        {
            isBrowsingSaveUI = false;
            var playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null || playerStats.currentHealth <= 0)
            {
                if (modalPanel != null)
                {
                    modalPanel.SetActive(true);
                }
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    private void Update()
    {
        if (!isOpen) return;

        // Skip intro jika pemain menekan sembarang tombol atau klik mouse saat animasi intro berlangsung
        if (isIntroAnimating && CheckAnyInputPressed())
        {
            SkipIntroToMenu();
            return;
        }

        // Pastikan kursor selalu aktif saat berada di Death Screen Menu
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Efek denyut halus (breathing aura) pada tulisan 'YOU DIED' setelah menu terbuka
        if (!isIntroAnimating && bannerContainer != null)
        {
            float breath = 1f + Mathf.Sin(Time.unscaledTime * 2.0f) * 0.012f;
            bannerContainer.localScale = bannerHeaderScale * breath;
        }

        // Pertahankan jeda simulasi dunia selama tidak sedang mengeksekusi respawn
        bool isRespawning = PlayerRespawnController.Instance != null && PlayerRespawnController.Instance.IsRespawning;
        if (!isRespawning && !isBrowsingSaveUI && Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
        }
    }

    private bool CheckAnyInputPressed()
    {
        try
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.anyKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            {
                return true;
            }

            var ms = UnityEngine.InputSystem.Mouse.current;
            if (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame))
            {
                return true;
            }
        }
        catch { }

        try
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Memastikan hierarki UI tersedia di canvas, membuat hierarki dinamis jika belum ada di scene.
    /// </summary>
    public void EnsureUIHierarchy()
    {
        if (modalPanel != null && bannerContainer != null && menuCard != null) return;

        var canvas = GameObject.Find("UI_Canvas");
        if (canvas == null)
        {
            var c = FindFirstObjectByType<Canvas>();
            if (c != null) canvas = c.gameObject;
        }
        if (canvas == null) return;

        var existing = canvas.transform.Find("Panel_DeathScreen");
        if (existing != null)
        {
            var oldGlow = existing.Find("Banner_YouDied/BackglowHalo");
            if (oldGlow != null) DestroyImmediate(oldGlow.gameObject);

            var bd = existing.Find("Backdrop")?.GetComponent<Image>();
            if (bd != null) bd.color = new Color(0f, 0f, 0f, backdropMaxAlpha);

            modalPanel = existing.gameObject;
            ResolveComponentReferences();
            if (bannerContainer != null && menuCard != null) return;

            // Jika ada hierarchy lama yang tidak lengkap, bangun ulang secara bersih
            DestroyImmediate(existing.gameObject);
            modalPanel = null;
        }

        BuildUI(canvas.transform);
    }

    private void ResolveComponentReferences()
    {
        if (modalPanel == null) return;

        modalCanvasGroup = modalPanel.GetComponent<CanvasGroup>();
        if (backdropImage == null)
        {
            var bd = modalPanel.transform.Find("Backdrop");
            if (bd != null) backdropImage = bd.GetComponent<Image>();
        }

        if (bannerContainer == null)
        {
            var b = modalPanel.transform.Find("Banner_YouDied");
            if (b != null)
            {
                bannerContainer = b.GetComponent<RectTransform>();
                bannerCanvasGroup = b.GetComponent<CanvasGroup>();

                var oldGlow = b.Find("BackglowHalo");
                if (oldGlow != null) DestroyImmediate(oldGlow.gameObject);

                var img = b.Find("Image_YouDied");
                if (img != null) youDiedImage = img.GetComponent<Image>();

                var txt = b.Find("Text_YouDiedFallback");
                if (txt != null) youDiedFallbackText = txt.GetComponent<TextMeshProUGUI>();

                var sub = b.Find("SubtitleText");
                if (sub != null)
                {
                    subtitleCanvasGroup = sub.GetComponent<CanvasGroup>();
                    subtitleText = sub.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        if (menuCard == null)
        {
            var mc = modalPanel.transform.Find("DeathMenuCard");
            if (mc != null)
            {
                menuCard = mc.GetComponent<RectTransform>();
                menuCanvasGroup = mc.GetComponent<CanvasGroup>();

                var cp = mc.Find("Btn_Checkpoint");
                if (cp != null) checkpointButton = cp.GetComponent<Button>();

                var ld = mc.Find("Btn_LoadGame");
                if (ld != null) loadGameButton = ld.GetComponent<Button>();

                var mm = mc.Find("Btn_MainMenu");
                if (mm != null) mainMenuButton = mm.GetComponent<Button>();

                var fb = mc.Find("FeedbackText");
                if (fb != null) feedbackText = fb.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void BuildUI(Transform canvasTr)
    {
        // 1. Root Panel
        modalPanel = new GameObject("Panel_DeathScreen", typeof(RectTransform), typeof(CanvasGroup));
        modalPanel.transform.SetParent(canvasTr, false);
        var rootRT = modalPanel.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        modalCanvasGroup = modalPanel.GetComponent<CanvasGroup>();
        modalCanvasGroup.alpha = 0f;

        // 2. Backdrop (Transparan)
        var backdropGO = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdropGO.transform.SetParent(modalPanel.transform, false);
        var bdRT = backdropGO.GetComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero;
        bdRT.anchorMax = Vector2.one;
        bdRT.sizeDelta = Vector2.zero;
        backdropImage = backdropGO.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, backdropMaxAlpha);
        backdropImage.raycastTarget = true;

        // 3. Banner 'YOU DIED' Container (Murni transparan, tanpa kotak background)
        var bannerGO = new GameObject("Banner_YouDied", typeof(RectTransform), typeof(CanvasGroup));
        bannerGO.transform.SetParent(modalPanel.transform, false);
        bannerContainer = bannerGO.GetComponent<RectTransform>();
        bannerContainer.anchorMin = new Vector2(0.5f, 0.5f);
        bannerContainer.anchorMax = new Vector2(0.5f, 0.5f);
        bannerContainer.pivot = new Vector2(0.5f, 0.5f);
        bannerContainer.sizeDelta = new Vector2(580f, 210f);
        bannerContainer.anchoredPosition = bannerCenterPos;
        bannerCanvasGroup = bannerGO.GetComponent<CanvasGroup>();
        bannerCanvasGroup.alpha = 0f;

        // Graphic Image "YOU DIED" (Transparent PNG)
        var imgGO = new GameObject("Image_YouDied", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(bannerContainer, false);
        var imgRT = imgGO.GetComponent<RectTransform>();
        imgRT.anchorMin = new Vector2(0.5f, 0.5f);
        imgRT.anchorMax = new Vector2(0.5f, 0.5f);
        imgRT.pivot = new Vector2(0.5f, 0.5f);
        imgRT.sizeDelta = new Vector2(560f, 186f);
        imgRT.anchoredPosition = new Vector2(0f, 15f);

        youDiedImage = imgGO.GetComponent<Image>();
        youDiedImage.preserveAspect = true;
        youDiedImage.raycastTarget = false;

        Sprite loadedSprite = LoadYouDiedSprite();
        if (loadedSprite != null)
        {
            youDiedImage.sprite = loadedSprite;
            youDiedImage.color = Color.white;
        }

        // 3c. Fallback TextMeshPro jika gambar sprite tidak tersedia
        var textGO = new GameObject("Text_YouDiedFallback", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(bannerContainer, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.sizeDelta = new Vector2(560f, 100f);
        textRT.anchoredPosition = new Vector2(0f, 15f);

        youDiedFallbackText = textGO.GetComponent<TextMeshProUGUI>();
        youDiedFallbackText.text = "<b>YOU DIED</b>";
        youDiedFallbackText.fontSize = 58;
        youDiedFallbackText.characterSpacing = 12f;
        youDiedFallbackText.color = new Color(0.98f, 0.78f, 0.25f, 1f);
        youDiedFallbackText.alignment = TextAlignmentOptions.Center;
        youDiedFallbackText.raycastTarget = false;

        if (loadedSprite != null)
        {
            textGO.SetActive(false);
        }

        // 3d. Subtitle Text
        var subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(bannerContainer, false);
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.sizeDelta = new Vector2(520f, 28f);
        subRT.anchoredPosition = new Vector2(0f, -76f);

        subtitleCanvasGroup = subGO.GetComponent<CanvasGroup>();
        subtitleCanvasGroup.alpha = 0f;

        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        subtitleText.text = "Exhaustion overtook you in the fields of darkness...";
        subtitleText.fontSize = 14;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.color = new Color(0.90f, 0.75f, 0.80f, 0.95f);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.raycastTarget = false;

        // 4. Central Modal Card (Menu Pilihan: Checkpoint, Load, Main Menu)
        var cardGO = new GameObject("DeathMenuCard", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        cardGO.transform.SetParent(modalPanel.transform, false);
        menuCard = cardGO.GetComponent<RectTransform>();
        menuCard.anchorMin = new Vector2(0.5f, 0.5f);
        menuCard.anchorMax = new Vector2(0.5f, 0.5f);
        menuCard.pivot = new Vector2(0.5f, 0.5f);
        menuCard.sizeDelta = new Vector2(520f, 275f);
        menuCard.anchoredPosition = menuFinalPos;

        menuCanvasGroup = cardGO.GetComponent<CanvasGroup>();
        menuCanvasGroup.alpha = 0f;
        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;

        var cardImg = cardGO.GetComponent<Image>();
        cardImg.color = new Color(0.08f, 0.05f, 0.07f, 0.95f);

        var outline = cardGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.68f, 0.18f, 0.22f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Divider Line di atas tombol
        var divGO = new GameObject("DividerLine", typeof(RectTransform), typeof(Image));
        divGO.transform.SetParent(menuCard, false);
        var dRT = divGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0.5f);
        dRT.anchorMax = new Vector2(0.5f, 0.5f);
        dRT.pivot = new Vector2(0.5f, 0.5f);
        dRT.sizeDelta = new Vector2(440f, 2f);
        dRT.anchoredPosition = new Vector2(0f, 108f);
        var divImg = divGO.GetComponent<Image>();
        divImg.color = new Color(0.60f, 0.16f, 0.20f, 0.75f);

        // Tombol 1: Checkpoint (Bed Recovery)
        checkpointButton = CreateMenuButton(
            menuCard,
            "Btn_Checkpoint",
            "<b>CHECKPOINT (BED RECOVERY)</b>",
            "Awaken in bedroom at morning with restored vitals",
            new Color(0.72f, 0.38f, 0.06f, 1f),
            new Color(0.88f, 0.50f, 0.12f, 1f),
            new Color(0.50f, 0.24f, 0.03f, 1f),
            new Vector2(450f, 52f),
            new Vector2(0f, 64f),
            OnCheckpointClicked);

        // Tombol 2: Load Game
        loadGameButton = CreateMenuButton(
            menuCard,
            "Btn_LoadGame",
            "<b>LOAD SAVED GAME</b>",
            "Select and resume from a saved file slot",
            new Color(0.12f, 0.35f, 0.78f, 1f),
            new Color(0.20f, 0.48f, 0.94f, 1f),
            new Color(0.08f, 0.22f, 0.55f, 1f),
            new Vector2(450f, 52f),
            new Vector2(0f, 2f),
            OnLoadGameClicked);

        // Tombol 3: Main Menu
        mainMenuButton = CreateMenuButton(
            menuCard,
            "Btn_MainMenu",
            "<b>RETURN TO MAIN MENU</b>",
            "Exit current session and return to title screen",
            new Color(0.22f, 0.26f, 0.34f, 1f),
            new Color(0.32f, 0.38f, 0.48f, 1f),
            new Color(0.15f, 0.18f, 0.24f, 1f),
            new Vector2(450f, 52f),
            new Vector2(0f, -60f),
            OnMainMenuClicked);

        // Footer Feedback Text
        var fbGO = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fbGO.transform.SetParent(menuCard, false);
        var fbRT = fbGO.GetComponent<RectTransform>();
        fbRT.anchorMin = new Vector2(0.5f, 0.5f);
        fbRT.anchorMax = new Vector2(0.5f, 0.5f);
        fbRT.pivot = new Vector2(0.5f, 0.5f);
        fbRT.sizeDelta = new Vector2(460f, 24f);
        fbRT.anchoredPosition = new Vector2(0f, -108f);
        feedbackText = fbGO.GetComponent<TextMeshProUGUI>();
        feedbackText.text = "Select an option above to continue.";
        feedbackText.fontSize = 12;
        feedbackText.color = new Color(0.72f, 0.72f, 0.78f, 0.85f);
        feedbackText.alignment = TextAlignmentOptions.Center;

        modalPanel.SetActive(false);
    }

    private Sprite LoadYouDiedSprite()
    {
        var sprite = Resources.Load<Sprite>("UI/YouDied");
        if (sprite != null) return sprite;

        string[] paths = new string[]
        {
            System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "YouDied.png"),
            System.IO.Path.Combine(Application.dataPath, "Art", "Textures", "YouDied.png")
        };

        foreach (var p in paths)
        {
            if (System.IO.File.Exists(p))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(p);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DeathScreenUI] Failed to load sprite fallback: {ex.Message}");
                }
            }
        }
        return null;
    }

    private Button CreateMenuButton(
        Transform parent,
        string name,
        string title,
        string subtitle,
        Color normalColor,
        Color hoverColor,
        Color pressedColor,
        Vector2 size,
        Vector2 pos,
        UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = btnGO.GetComponent<Image>();
        img.color = normalColor;

        var outline = btnGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.50f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var btn = btnGO.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = normalColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        if (onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }

        // Title TMP
        var textGO = new GameObject("Text_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(btnGO.transform, false);
        var tRT = textGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.sizeDelta = Vector2.zero;
        tRT.anchoredPosition = new Vector2(0, 8);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        // Subtitle TMP
        var subTextGO = new GameObject("Text_Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subTextGO.transform.SetParent(btnGO.transform, false);
        var sRT = subTextGO.GetComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero;
        sRT.anchorMax = Vector2.one;
        sRT.sizeDelta = Vector2.zero;
        sRT.anchoredPosition = new Vector2(0, -10);
        var subTmp = subTextGO.GetComponent<TextMeshProUGUI>();
        subTmp.text = subtitle;
        subTmp.fontSize = 11;
        subTmp.color = new Color(0.92f, 0.92f, 0.96f, 0.78f);
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;

        btnGO.AddComponent<DeathMenuButtonAnimator>();

        return btn;
    }

    /// <summary>
    /// Membuka antarmuka Death Screen:
    /// Memulai dengan animasi 'YOU DIED', lalu membuka menu Checkpoint, Load Game, dan Main Menu.
    /// </summary>
    public void Open()
    {
        if (isOpen) return;
        EnsureUIHierarchy();
        WireButtons();
        if (modalPanel == null) return;

        isOpen = true;
        isIntroAnimating = true;
        isHandlingAction = false;
        isBrowsingSaveUI = false;

        SetButtonsInteractable(false);

        if (feedbackText != null)
        {
            feedbackText.text = "Select an option above to continue.";
        }

        // Aktifkan panel dan GameObject agar coroutine berjalan
        modalPanel.SetActive(true);
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.Push(this);
        }

        // Hentikan getaran kamera pertarungan
        if (FeaturesCamera.IsometricCameraController.Instance != null)
        {
            FeaturesCamera.IsometricCameraController.Instance.StopShake();
        }
        if (FeaturesCombat.Adapters.TraumaCameraShake.Instance != null)
        {
            FeaturesCombat.Adapters.TraumaCameraShake.Instance.ResetTrauma();
        }

        // Pastikan kursor selalu aktif dan bebas
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Jeda waktu game seketika
        Time.timeScale = 0f;

        if (activeSequenceCoroutine != null) StopCoroutine(activeSequenceCoroutine);
        activeSequenceCoroutine = StartCoroutine(RoutineDeathIntroSequence());
    }

    /// <summary>
    /// Coroutine sekuens animasi kematian:
    /// 1. Vignet gelap menggelap, tulisan 'YOU DIED' muncul di tengah dengan pembesaran lembut (scale & fade).
    /// 2. Dentang nada gong kematian beresonansi.
    /// 3. Jeda dramatis saat teks membesar secara halus & subtitle memudar masuk.
    /// 4. Banner 'YOU DIED' bergeser mulus ke atas menjadi header.
    /// 5. Menu card (Checkpoint, Load, Main Menu) muncul meluncur dari bawah.
    /// </summary>
    private IEnumerator RoutineDeathIntroSequence()
    {
        if (modalCanvasGroup != null) modalCanvasGroup.alpha = 1f;

        if (backdropImage != null)
        {
            backdropImage.color = new Color(0f, 0f, 0f, backdropMaxAlpha);
        }

        if (bannerContainer != null)
        {
            bannerContainer.anchoredPosition = bannerCenterPos;
            bannerContainer.localScale = bannerCenterScale * 0.75f;
        }

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 0f;
        }

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 0f;
        }

        if (menuCard != null)
        {
            menuCard.anchoredPosition = menuStartPos;
        }

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        PlayDeathGongSound();

        // 1. Fase Kemunculan (0.75 detik) - Fade & Scale
        float elapsed = 0f;
        float appearDuration = 0.75f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);

            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, easeOut);
            }

            if (bannerContainer != null)
            {
                bannerContainer.localScale = bannerCenterScale * Mathf.Lerp(0.75f, 1.04f, easeOut);
            }

            yield return null;
        }

        // 2. Fase Tahan & Subtitle (0.95 detik)
        elapsed = 0f;
        float holdDuration = 0.95f;

        while (elapsed < holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / holdDuration);

            if (bannerContainer != null)
            {
                bannerContainer.localScale = bannerCenterScale * Mathf.Lerp(1.04f, 1.08f, t);
            }

            if (subtitleCanvasGroup != null)
            {
                subtitleCanvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
            }

            yield return null;
        }

        // 3. Fase Transisi: Banner bergeser ke atas & Menu Card meluncur masuk (0.4 detik)
        elapsed = 0f;
        float transitionDuration = 0.40f;
        Vector2 bStartPos = bannerContainer != null ? bannerContainer.anchoredPosition : bannerCenterPos;
        Vector3 bStartScale = bannerContainer != null ? bannerContainer.localScale : bannerCenterScale;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float easeOut = 1f - Mathf.Pow(1f - t, 2.5f);

            if (bannerContainer != null)
            {
                bannerContainer.anchoredPosition = Vector2.Lerp(bStartPos, bannerHeaderPos, easeOut);
                bannerContainer.localScale = Vector3.Lerp(bStartScale, bannerHeaderScale, easeOut);
            }

            if (menuCard != null)
            {
                menuCard.anchoredPosition = Vector2.Lerp(menuStartPos, menuFinalPos, easeOut);
            }

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, easeOut);
            }

            yield return null;
        }

        FinalizeMenuReveal();
    }

    /// <summary>
    /// Melewati animasi intro secara langsung jika pemain menekan tombol apapun untuk merespon cepat.
    /// </summary>
    private void SkipIntroToMenu()
    {
        if (activeSequenceCoroutine != null)
        {
            StopCoroutine(activeSequenceCoroutine);
            activeSequenceCoroutine = null;
        }

        if (backdropImage != null)
        {
            backdropImage.color = new Color(0f, 0f, 0f, backdropMaxAlpha);
        }

        if (bannerContainer != null)
        {
            bannerContainer.anchoredPosition = bannerHeaderPos;
            bannerContainer.localScale = bannerHeaderScale;
        }

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 1f;
        }

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 1f;
        }

        if (menuCard != null)
        {
            menuCard.anchoredPosition = menuFinalPos;
        }

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
        }

        FinalizeMenuReveal();
    }

    private void FinalizeMenuReveal()
    {
        isIntroAnimating = false;
        activeSequenceCoroutine = null;

        if (bannerContainer != null)
        {
            bannerContainer.anchoredPosition = bannerHeaderPos;
            bannerContainer.localScale = bannerHeaderScale;
        }

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 1f;
        }

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 1f;
        }

        if (menuCard != null)
        {
            menuCard.anchoredPosition = menuFinalPos;
        }

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }

        SetButtonsInteractable(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Menutup antarmuka Death Screen dengan animasi fade-out.
    /// </summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        isIntroAnimating = false;

        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }

        if (modalPanel != null && gameObject.activeInHierarchy)
        {
            if (activeSequenceCoroutine != null) StopCoroutine(activeSequenceCoroutine);
            activeSequenceCoroutine = StartCoroutine(RoutineFadeAlpha(0f, 0.2f, () =>
            {
                if (modalPanel != null) modalPanel.SetActive(false);
            }));
        }
        else if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Menutup panel Death Screen secara instan tanpa menunggu coroutine fade.
    /// </summary>
    public void CloseInstant()
    {
        isOpen = false;
        isIntroAnimating = false;

        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }

        if (activeSequenceCoroutine != null)
        {
            StopCoroutine(activeSequenceCoroutine);
            activeSequenceCoroutine = null;
        }

        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 0f;
        }

        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
    }

    private IEnumerator RoutineFadeAlpha(float targetAlpha, float duration, Action onComplete = null)
    {
        if (modalCanvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = modalCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            modalCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        modalCanvasGroup.alpha = targetAlpha;
        activeSequenceCoroutine = null;
        onComplete?.Invoke();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (checkpointButton != null) checkpointButton.interactable = interactable;
        if (loadGameButton != null) loadGameButton.interactable = interactable;
        if (mainMenuButton != null) mainMenuButton.interactable = interactable;
    }

    #region Procedural Audio

    private static AudioClip deathGongClip;

    private void PlayDeathGongSound()
    {
        if (deathGongClip == null)
        {
            deathGongClip = CreateProceduralGong();
        }

        if (audioSource != null && deathGongClip != null)
        {
            audioSource.PlayOneShot(deathGongClip, 0.70f);
        }
    }

    private static AudioClip CreateProceduralGong()
    {
        int sampleRate = 44100;
        float duration = 2.4f;
        int count = (int)(sampleRate * duration);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 1.8f);
            float osc = (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.45f)
                      + (Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.35f)
                      + (Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.20f);
            samples[i] = osc * env * 0.55f;
        }

        var clip = AudioClip.Create("DeathGongProcedural", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    #endregion

    #region Button Actions

    private void OnCheckpointClicked()
    {
        if (isHandlingAction) return;
        isHandlingAction = true;
        SetButtonsInteractable(false);

        if (feedbackText != null)
        {
            feedbackText.text = "Restoring vitals and awakening in bed...";
        }

        if (PlayerRespawnController.Instance != null)
        {
            PlayerRespawnController.Instance.ExecuteBedRespawn(() =>
            {
                Close();
            });
        }
        else
        {
            Close();
            Time.timeScale = 1f;
        }
    }

    private void OnLoadGameClicked()
    {
        if (isHandlingAction) return;

        isBrowsingSaveUI = true;

        if (SaveSystemUI.Instance != null)
        {
            SaveSystemUI.Instance.OnSaveUIClosed -= HandleSaveUIClosed;
            SaveSystemUI.Instance.OnSaveUIClosed += HandleSaveUIClosed;
            SaveSystemUI.Instance.Open();
        }
        else
        {
            Debug.LogWarning("[DeathScreenUI] SaveSystemUI.Instance is not available.");
            isBrowsingSaveUI = false;
        }
    }

    private void OnMainMenuClicked()
    {
        if (isHandlingAction) return;
        isHandlingAction = true;
        SetButtonsInteractable(false);

        if (feedbackText != null)
        {
            feedbackText.text = "Returning to Main Menu...";
        }

        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var fadeService = ServiceLocator.Resolve<IFadeService>();
        if (fadeService != null)
        {
            fadeService.FadeIn(0.35f, () =>
            {
                SceneManager.LoadScene("MainMenuScene");
            });
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    #endregion
}

/// <summary>
/// Helper mikro-animasi tactile untuk tombol pada Death Screen Menu.
/// Memberikan respons scale spring saat di-hover dan ditekan.
/// </summary>
public class DeathMenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private bool isHovered = false;
    private bool isPressed = false;

    private static AudioClip popClip;

    private void Awake()
    {
        baseScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
        targetScale = baseScale;
    }

    private void OnEnable()
    {
        transform.localScale = baseScale;
        targetScale = baseScale;
        isHovered = false;
        isPressed = false;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 15f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = baseScale * 1.025f;
        PlayPopSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = isPressed ? (baseScale * 0.96f) : baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetScale = baseScale * 0.96f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetScale = isHovered ? (baseScale * 1.025f) : baseScale;
    }

    private void PlayPopSound()
    {
        if (popClip == null)
        {
            int sampleRate = 44100;
            float dur = 0.08f;
            int count = (int)(sampleRate * dur);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 22f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 720f * t) * env * 0.25f;
            }
            popClip = AudioClip.Create("BtnPopSound", count, 1, sampleRate, false);
            popClip.SetData(samples, 0);
        }

        if (DeathScreenUI.Instance != null)
        {
            var src = DeathScreenUI.Instance.GetComponent<AudioSource>();
            if (src != null && popClip != null)
            {
                src.PlayOneShot(popClip, 0.45f);
            }
        }
    }
}
