using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Inventory UI system that appears when the player opens a chest.
/// Creates a full inventory grid panel with item slots, close button, and chest title.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int rows = 4;
    public int columns = 6;
    public float slotSize = 64f;
    public float slotPadding = 6f;

    [Header("Styling")]
    public Color panelColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    public Color slotColor = new Color(0.22f, 0.25f, 0.30f, 1.0f);
    public Color slotHighlightColor = new Color(0.35f, 0.55f, 0.85f, 1.0f);
    public Color titleColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);
    public Color closeButtonColor = new Color(0.85f, 0.25f, 0.25f, 1.0f);

    private GameObject inventoryPanel;
    private Canvas inventoryCanvas;
    private bool isOpen = false;

    // Singleton
    private static InventoryUI _instance;
    public static InventoryUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InventoryUI>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("InventoryUI_Manager");
                    _instance = obj.AddComponent<InventoryUI>();
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }

        CreateInventoryUI();
    }

    void Update()
    {
        if (!isOpen) return;

        // Close inventory with E or Escape
        bool closePressed = false;

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            closePressed = Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (!closePressed)
        {
            try
            {
                closePressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape);
            }
            catch { }
        }
        #endif

        if (closePressed)
        {
            CloseInventory();
        }
    }

    private void CreateInventoryUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("InventoryCanvas");
        canvasObj.transform.SetParent(transform);
        inventoryCanvas = canvasObj.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Main Panel (dark background overlay)
        GameObject overlayObj = new GameObject("DarkOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        RectTransform overlayRT = overlayObj.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        // Inventory Panel Container
        float panelWidth = columns * (slotSize + slotPadding) + slotPadding + 40f;
        float panelHeight = rows * (slotSize + slotPadding) + slotPadding + 100f;

        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvasObj.transform, false);
        Image panelImg = inventoryPanel.AddComponent<Image>();
        panelImg.color = panelColor;

        // Rounded appearance via Outline
        Outline panelOutline = inventoryPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.4f, 0.5f, 0.7f, 0.5f);
        panelOutline.effectDistance = new Vector2(2, 2);

        RectTransform panelRT = inventoryPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Title: "Chest Inventory"
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inventoryPanel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "\u2B50 CHEST INVENTORY";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = titleColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.offsetMin = new Vector2(10, -50);
        titleRT.offsetMax = new Vector2(-10, -8);

        // Close Button [X]
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(inventoryPanel.transform, false);
        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = closeButtonColor;
        Button closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseInventory);
        RectTransform closeRT = closeObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-8, -8);
        closeRT.sizeDelta = new Vector2(36, 36);

        GameObject closeLabel = new GameObject("CloseLabel");
        closeLabel.transform.SetParent(closeObj.transform, false);
        Text closeTxt = closeLabel.AddComponent<Text>();
        closeTxt.text = "X";
        closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (closeTxt.font == null) closeTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        closeTxt.fontSize = 20;
        closeTxt.fontStyle = FontStyle.Bold;
        closeTxt.color = Color.white;
        closeTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform closeLabelRT = closeLabel.GetComponent<RectTransform>();
        closeLabelRT.anchorMin = Vector2.zero;
        closeLabelRT.anchorMax = Vector2.one;
        closeLabelRT.offsetMin = Vector2.zero;
        closeLabelRT.offsetMax = Vector2.zero;

        // Inventory Slots Grid
        string[] sampleItems = { "Sword", "Shield", "Potion", "Key", "Gem", "Scroll", "Ring", "Helmet" };
        Color[] itemColors = {
            new Color(0.75f, 0.35f, 0.35f),
            new Color(0.35f, 0.55f, 0.75f),
            new Color(0.35f, 0.75f, 0.45f),
            new Color(0.85f, 0.75f, 0.30f),
            new Color(0.70f, 0.35f, 0.80f),
            new Color(0.80f, 0.65f, 0.40f),
            new Color(0.90f, 0.80f, 0.20f),
            new Color(0.50f, 0.65f, 0.80f)
        };

        float gridStartX = -(panelWidth / 2f) + slotPadding + 20f + slotSize / 2f;
        float gridStartY = (panelHeight / 2f) - 60f - slotPadding - slotSize / 2f;

        int itemIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = gridStartX + c * (slotSize + slotPadding);
                float y = gridStartY - r * (slotSize + slotPadding);

                // Slot Background
                GameObject slotObj = new GameObject($"Slot_{r}_{c}");
                slotObj.transform.SetParent(inventoryPanel.transform, false);
                Image slotImg = slotObj.AddComponent<Image>();
                slotImg.color = slotColor;
                Outline slotOutline = slotObj.AddComponent<Outline>();
                slotOutline.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.4f);
                slotOutline.effectDistance = new Vector2(1, 1);

                RectTransform slotRT = slotObj.GetComponent<RectTransform>();
                slotRT.anchorMin = new Vector2(0.5f, 0.5f);
                slotRT.anchorMax = new Vector2(0.5f, 0.5f);
                slotRT.pivot = new Vector2(0.5f, 0.5f);
                slotRT.anchoredPosition = new Vector2(x, y);
                slotRT.sizeDelta = new Vector2(slotSize, slotSize);

                // Place sample items in first few slots
                if (itemIndex < sampleItems.Length)
                {
                    // Item Icon (colored square)
                    GameObject iconObj = new GameObject("ItemIcon");
                    iconObj.transform.SetParent(slotObj.transform, false);
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.color = itemColors[itemIndex];
                    RectTransform iconRT = iconObj.GetComponent<RectTransform>();
                    iconRT.anchorMin = new Vector2(0.15f, 0.3f);
                    iconRT.anchorMax = new Vector2(0.85f, 0.95f);
                    iconRT.offsetMin = Vector2.zero;
                    iconRT.offsetMax = Vector2.zero;

                    // Item Name Label
                    GameObject labelObj = new GameObject("ItemLabel");
                    labelObj.transform.SetParent(slotObj.transform, false);
                    Text labelTxt = labelObj.AddComponent<Text>();
                    labelTxt.text = sampleItems[itemIndex];
                    labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (labelTxt.font == null) labelTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    labelTxt.fontSize = 10;
                    labelTxt.color = new Color(0.8f, 0.8f, 0.8f);
                    labelTxt.alignment = TextAnchor.LowerCenter;
                    RectTransform labelRT = labelObj.GetComponent<RectTransform>();
                    labelRT.anchorMin = new Vector2(0, 0);
                    labelRT.anchorMax = new Vector2(1, 0.3f);
                    labelRT.offsetMin = Vector2.zero;
                    labelRT.offsetMax = Vector2.zero;

                    itemIndex++;
                }
            }
        }

        // Prompt Text at bottom
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(inventoryPanel.transform, false);
        Text promptTxt = promptObj.AddComponent<Text>();
        promptTxt.text = "Press [E] or [ESC] to close";
        promptTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (promptTxt.font == null) promptTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        promptTxt.fontSize = 14;
        promptTxt.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        promptTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform promptRT = promptObj.GetComponent<RectTransform>();
        promptRT.anchorMin = new Vector2(0, 0);
        promptRT.anchorMax = new Vector2(1, 0);
        promptRT.pivot = new Vector2(0.5f, 0);
        promptRT.offsetMin = new Vector2(10, 6);
        promptRT.offsetMax = new Vector2(-10, 30);

        // Start hidden
        canvasObj.SetActive(false);
    }

    public void OpenInventory()
    {
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(true);
            isOpen = true;
        }
    }

    public void CloseInventory()
    {
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(false);
            isOpen = false;
        }
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public bool IsOpen => isOpen;
}
