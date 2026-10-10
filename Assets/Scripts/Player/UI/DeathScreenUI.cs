using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FarmBeware.Core.Runtime;
using FeaturesSaveSystem;

/// <summary>
/// Death Screen Menu UI (Layar Kematian / You Collapsed).
/// Tampil saat HP pemain mencapai 0, memberikan pilihan strategis:
/// 1. Checkpoint: Pulih di kasur kamar tidur pada pagi hari sebelum night terpicu (50% HP).
/// 2. Load Game: Membuka modal multi-slot Save & Load system untuk memuat file simpanan.
/// 3. Main Menu: Keluar ke scene menu utama secara mulus.
/// </summary>
public class DeathScreenUI : MonoBehaviour, IModalWindow
{
    public static DeathScreenUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DeathScreenUI>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    var go = new GameObject("DeathScreenUI");
                    instance = go.AddComponent<DeathScreenUI>();
                }
            }
            return instance;
        }
        private set => instance = value;
    }
    private static DeathScreenUI instance;

    [Header("UI Root")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button checkpointButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    public bool IsOpen => isOpen;
    public bool CanDismissWithEscape => false;
    private bool isOpen = false;
    private Coroutine fadeCoroutine;
    private bool isHandlingAction = false;
    private bool isBrowsingSaveUI = false;

    public void OpenModal() => Open();
    public void CloseModal() => Close();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        EnsureUIHierarchy();
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

    private void OnDestroy()
    {
        if (ModalStackManager.Instance != null)
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
            // Jika pemain membatalkan menu save dan HP masih 0 (belum pulih), buka kembali death screen
            var playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null || playerStats.currentHealth <= 0)
            {
                if (modalPanel != null)
                {
                    modalPanel.SetActive(true);
                }
            }
        }
    }

    private void Update()
    {
        if (!isOpen) return;

        // Pastikan kursor selalu terlihat dan tidak terkunci saat berada di Death Screen
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Pertahankan jeda simulasi dunia selama tidak sedang mengeksekusi respawn
        bool isRespawning = PlayerRespawnController.Instance != null && PlayerRespawnController.Instance.IsRespawning;
        if (!isRespawning && !isBrowsingSaveUI && Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Memastikan hierarki UI tersedia di canvas, membuat hierarki dinamis jika belum ada di scene.
    /// </summary>
    public void EnsureUIHierarchy()
    {
        if (modalPanel != null) return;

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
            DestroyImmediate(existing.gameObject);
        }

        BuildUI(canvas.transform);
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

        canvasGroup = modalPanel.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // 2. Backdrop Vignette Gelap (Dark Crimson Onyx)
        var backdropGO = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdropGO.transform.SetParent(modalPanel.transform, false);
        var bdRT = backdropGO.GetComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero;
        bdRT.anchorMax = Vector2.one;
        bdRT.sizeDelta = Vector2.zero;
        var bdImg = backdropGO.GetComponent<Image>();
        bdImg.color = new Color(0.06f, 0.02f, 0.03f, 0.90f);
        bdImg.raycastTarget = true;

        // 3. Central Modal Card
        var cardGO = new GameObject("DeathModalCard", typeof(RectTransform), typeof(Image));
        cardGO.transform.SetParent(modalPanel.transform, false);
        var cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(580, 480);
        cardRT.anchoredPosition = Vector2.zero;

        var cardImg = cardGO.GetComponent<Image>();
        cardImg.color = new Color(0.10f, 0.07f, 0.09f, 0.98f);

        var outline = cardGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.18f, 0.22f, 0.90f); // Gothic Blood Crimson
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // 4. Header Section
        // Skull Emblem
        var skullGO = new GameObject("SkullIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        skullGO.transform.SetParent(cardGO.transform, false);
        var skRT = skullGO.GetComponent<RectTransform>();
        skRT.anchorMin = new Vector2(0.5f, 0.5f);
        skRT.anchorMax = new Vector2(0.5f, 0.5f);
        skRT.pivot = new Vector2(0.5f, 0.5f);
        skRT.sizeDelta = new Vector2(100, 50);
        skRT.anchoredPosition = new Vector2(0, 180);
        var skTmp = skullGO.GetComponent<TextMeshProUGUI>();
        skTmp.text = "☠";
        skTmp.fontSize = 44;
        skTmp.color = new Color(0.95f, 0.25f, 0.25f, 1f);
        skTmp.alignment = TextAlignmentOptions.Center;

        // Title Text
        var titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(cardGO.transform, false);
        var tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 0.5f);
        tRT.anchorMax = new Vector2(0.5f, 0.5f);
        tRT.pivot = new Vector2(0.5f, 0.5f);
        tRT.sizeDelta = new Vector2(520, 46);
        tRT.anchoredPosition = new Vector2(0, 134);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.text = "<b>YOU COLLAPSED</b>";
        titleText.fontSize = 32;
        titleText.color = new Color(0.98f, 0.86f, 0.86f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        // Subtitle Text
        var subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(cardGO.transform, false);
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.sizeDelta = new Vector2(520, 32);
        subRT.anchoredPosition = new Vector2(0, 96);
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        subtitleText.text = "Exhaustion overtook you in the fields of darkness...";
        subtitleText.fontSize = 14;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.color = new Color(0.85f, 0.70f, 0.75f, 0.95f);
        subtitleText.alignment = TextAlignmentOptions.Center;

        // Divider Line
        var divGO = new GameObject("DividerLine", typeof(RectTransform), typeof(Image));
        divGO.transform.SetParent(cardGO.transform, false);
        var dRT = divGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0.5f);
        dRT.anchorMax = new Vector2(0.5f, 0.5f);
        dRT.pivot = new Vector2(0.5f, 0.5f);
        dRT.sizeDelta = new Vector2(460, 2);
        dRT.anchoredPosition = new Vector2(0, 68);
        var divImg = divGO.GetComponent<Image>();
        divImg.color = new Color(0.55f, 0.12f, 0.15f, 0.80f);

        // 5. Interactive Action Buttons
        // Button 1: Checkpoint (Bed Recovery - Same Day Morning)
        checkpointButton = CreateMenuButton(
            cardGO.transform,
            "Btn_Checkpoint",
            "<b>CHECKPOINT (BED RECOVERY)</b>",
            "Return to morning before nightfall with full vitals",
            new Color(0.72f, 0.38f, 0.05f, 1f), // Warm Amber Bronze
            new Color(0.88f, 0.50f, 0.10f, 1f),
            new Color(0.50f, 0.24f, 0.03f, 1f),
            new Vector2(460, 56),
            new Vector2(0, 18),
            OnCheckpointClicked);

        // Button 2: Load Saved Game
        loadGameButton = CreateMenuButton(
            cardGO.transform,
            "Btn_LoadGame",
            "<b>LOAD SAVED GAME</b>",
            "Select and resume from a saved file slot",
            new Color(0.12f, 0.35f, 0.80f, 1f), // Sapphire Blue
            new Color(0.20f, 0.48f, 0.95f, 1f),
            new Color(0.08f, 0.22f, 0.55f, 1f),
            new Vector2(460, 56),
            new Vector2(0, -50),
            OnLoadGameClicked);

        // Button 3: Return to Main Menu
        mainMenuButton = CreateMenuButton(
            cardGO.transform,
            "Btn_MainMenu",
            "<b>RETURN TO MAIN MENU</b>",
            "Exit current session and return to title screen",
            new Color(0.24f, 0.28f, 0.36f, 1f), // Slate Charcoal
            new Color(0.34f, 0.40f, 0.50f, 1f),
            new Color(0.15f, 0.18f, 0.24f, 1f),
            new Vector2(460, 56),
            new Vector2(0, -118),
            OnMainMenuClicked);

        // Feedback / Status text at footer
        var fbGO = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fbGO.transform.SetParent(cardGO.transform, false);
        var fbRT = fbGO.GetComponent<RectTransform>();
        fbRT.anchorMin = new Vector2(0.5f, 0.5f);
        fbRT.anchorMax = new Vector2(0.5f, 0.5f);
        fbRT.pivot = new Vector2(0.5f, 0.5f);
        fbRT.sizeDelta = new Vector2(480, 26);
        fbRT.anchoredPosition = new Vector2(0, -178);
        feedbackText = fbGO.GetComponent<TextMeshProUGUI>();
        feedbackText.text = "Select an option above to continue.";
        feedbackText.fontSize = 13;
        feedbackText.color = new Color(0.68f, 0.68f, 0.74f, 0.85f);
        feedbackText.alignment = TextAlignmentOptions.Center;

        modalPanel.SetActive(false);
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
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(1, -1);

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
        tmp.fontSize = 15;
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
        sRT.anchoredPosition = new Vector2(0, -11);
        var subTmp = subTextGO.GetComponent<TextMeshProUGUI>();
        subTmp.text = subtitle;
        subTmp.fontSize = 11;
        subTmp.color = new Color(0.9f, 0.9f, 0.95f, 0.75f);
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;

        return btn;
    }

    /// <summary>
    /// Membuka antarmuka Death Screen dengan efek transisi halus.
    /// </summary>
    public void Open()
    {
        if (isOpen) return;
        EnsureUIHierarchy();
        if (modalPanel == null) return;

        isOpen = true;
        isHandlingAction = false;
        isBrowsingSaveUI = false;
        SetButtonsInteractable(true);

        if (feedbackText != null)
        {
            feedbackText.text = "Select an option above to continue.";
        }

        modalPanel.SetActive(true);
        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.Push(this);
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(RoutineFadeAlpha(1f, 0.25f));

        Time.timeScale = 0f;
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

        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }

        if (modalPanel != null && gameObject.activeInHierarchy)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(RoutineFadeAlpha(0f, 0.2f, () =>
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
        if (ModalStackManager.Instance != null)
        {
            ModalStackManager.Instance.PopSpecific(this);
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
    }

    private IEnumerator RoutineFadeAlpha(float targetAlpha, float duration, Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (checkpointButton != null) checkpointButton.interactable = interactable;
        if (loadGameButton != null) loadGameButton.interactable = interactable;
        if (mainMenuButton != null) mainMenuButton.interactable = interactable;
    }

    #region Button Actions

    private void OnCheckpointClicked()
    {
        if (isHandlingAction) return;
        isHandlingAction = true;
        SetButtonsInteractable(false);

        if (feedbackText != null)
        {
            feedbackText.text = "Restoring vitals and preparing bed recovery...";
        }

        // Panggil orchestrator respawn di kasur melalui PlayerRespawnController
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

        // Hide Death Screen modal panel while viewing save slots
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }

        // Buka modal SaveSystemUI (pemain dapat memilih slot simpanan untuk dimuat)
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
            if (modalPanel != null) modalPanel.SetActive(true);
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

        // Pulihkan kecepatan waktu dan kursor sebelum memuat scene
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
