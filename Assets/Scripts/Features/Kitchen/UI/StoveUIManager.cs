using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FarmBeware.Core.Runtime;
using FeaturesKitchen;

/// <summary>
/// UI View/Presenter for the Kitchen Stove cooking panel.
/// Pure presentation layer:
/// - Displays available recipe list with Can-Cook visual indicators (normal color vs. grayscale).
/// - Shows rich recipe details (culinary description, nutritional stats, buffs, cook time).
/// - Supports 3 Concurrent Burners (Multi-Slot Cooking) with real-time countdown, progress, and individual cancel/take.
/// - Persists background cooking when panel is closed so player can leave the stove while cooking runs.
/// - Dispatches cooking commands to the backend controller (KitchenStove).
/// </summary>
public class StoveUIManager : MonoBehaviour, IModalWindow
{
    bool IModalWindow.IsOpen => IsPanelOpen;

    #region Serialized UI References

    [Header("Panel Root")]
    [SerializeField] private GameObject panelStove;

    [Header("Recipe List (Left)")]
    [SerializeField] private RectTransform recipeListContent;
    [SerializeField] private GameObject recipeButtonPrefab;

    [Header("Recipe Detail (Right)")]
    [SerializeField] private Image resultIcon;
    [SerializeField] private TextMeshProUGUI resultName;
    [SerializeField] private TextMeshProUGUI resultDescription;
    [SerializeField] private TextMeshProUGUI processTimeText;
    [SerializeField] private RectTransform ingredientContainer;
    [SerializeField] private GameObject ingredientRowPrefab;
    [SerializeField] private GameObject emptyStatePlaceholder;

    [Header("Cook Button & Progress")]
    [SerializeField] private Button cookButton;
    [SerializeField] private TextMeshProUGUI cookButtonText;
    [SerializeField] private Image cookButtonImage;
    [SerializeField] private Slider cookingProgressBar;

    [Header("Cooked Output Slot (Legacy / Fallback)")]
    [SerializeField] private GameObject cookedSlotRoot;
    [SerializeField] private Image cookedSlotIcon;
    [SerializeField] private TextMeshProUGUI cookedSlotCountText;
    [SerializeField] private Button collectCookedButton;
    [SerializeField] private TextMeshProUGUI collectButtonText;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Panel Behavior")]
    [Tooltip("If true, closing the panel while cooking cancels the process and refunds ingredients. If false, stove continues cooking in background.")]
    [SerializeField] private bool cancelCookingOnPanelClose = false;

    [Header("Colors")]
    [SerializeField] private Color canCookColor = new Color(0.2f, 0.7f, 0.3f, 1f);
    [SerializeField] private Color cannotCookColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color haveEnoughColor = new Color(0.3f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color notEnoughColor = new Color(0.9f, 0.3f, 0.3f, 1f);

    #endregion

    #region Internal Types & State

    [Serializable]
    public class BurnerSlotUI
    {
        public int slotIndex;
        public GameObject root;
        public Image background;
        public TextMeshProUGUI titleText;
        public Image iconImage;
        public TextMeshProUGUI statusText;
        public GameObject progressBarRoot;
        public Image progressBarFill;
        public Button actionButton;
        public TextMeshProUGUI actionButtonText;
        public Image actionButtonImage;
    }

    private struct RecipeButtonEntry
    {
        public KitchenRecipe recipe;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusBadge;
        public Image border;
        public Button button;
    }

    private static Material _grayscaleMaterial;
    public static Material GrayscaleMaterial
    {
        get
        {
            if (_grayscaleMaterial == null)
            {
                var shader = Shader.Find("UI/Grayscale");
                if (shader != null)
                {
                    _grayscaleMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
            }
            return _grayscaleMaterial;
        }
    }

    private List<KitchenRecipe> allRecipes = new List<KitchenRecipe>();
    private InventoryComponent playerInventory;
    private KitchenRecipe selectedRecipe;
    private KitchenStove currentStove;
    private ItemDatabase cachedItemDatabase;

    private readonly List<GameObject> spawnedRecipeButtons = new List<GameObject>();
    private readonly List<RecipeButtonEntry> spawnedRecipeButtonEntries = new List<RecipeButtonEntry>();
    private readonly List<GameObject> spawnedIngredientRows = new List<GameObject>();

    private GameObject burnerHubRoot;
    private readonly BurnerSlotUI[] burnerSlotUIs = new BurnerSlotUI[KitchenStove.MaxCookingSlots];

    public static StoveUIManager Instance { get; private set; }
    public bool IsPanelOpen => panelStove != null && panelStove.activeSelf;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Instance = this;
        cachedItemDatabase = Resources.Load<ItemDatabase>("Database/ItemDatabase");

        if (cookButton != null)
            cookButton.onClick.AddListener(OnCookClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (collectCookedButton != null)
        {
            collectCookedButton.onClick.RemoveListener(OnCollectClicked);
            collectCookedButton.onClick.AddListener(OnCollectClicked);
        }

        var slotBoxBtn = cookedSlotRoot != null ? cookedSlotRoot.transform.Find("SlotBox")?.GetComponent<Button>() : null;
        if (slotBoxBtn != null)
        {
            slotBoxBtn.onClick.RemoveListener(OnCollectClicked);
            slotBoxBtn.onClick.AddListener(OnCollectClicked);
        }

        var backdropBtn = transform.Find("ModalBackdrop")?.GetComponent<Button>();
        if (backdropBtn != null)
            backdropBtn.onClick.AddListener(OnCloseClicked);

        if (cookingProgressBar != null)
            cookingProgressBar.gameObject.SetActive(false);

        EnsureCookedSlotUI();
        EnsureBurnerUI();
    }

    private void Update()
    {
        if (panelStove != null && panelStove.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
                Close();
                return;
            }

            // Real-time animation of burner countdowns and progress bars while panel is open
            if (currentStove != null)
            {
                if (currentStove.ActiveCookingCount > 0)
                    UpdateAllBurnerTimers();
                else
                    SyncCookingProgressBar();
            }
        }
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        UnsubscribeFromStove(currentStove);

        // Ensure player movement lock is safely released if panel is disabled externally
        var playerControl = ServiceLocator.Resolve<IPlayerContext>();
        if (playerControl != null && playerControl.IsInputLocked)
        {
            playerControl.IsInputLocked = false;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        UnsubscribeFromStove(currentStove);

        if (Instance == this)
            Instance = null;

        if (cookButton != null)
            cookButton.onClick.RemoveListener(OnCookClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);

        var backdropBtn = transform.Find("ModalBackdrop")?.GetComponent<Button>();
        if (backdropBtn != null)
            backdropBtn.onClick.RemoveListener(OnCloseClicked);

        if (collectCookedButton != null)
            collectCookedButton.onClick.RemoveListener(OnCollectClicked);
    }

    #endregion

    #region Open & Close Panel

    /// <summary>Opens stove panel with references to controller KitchenStove and player inventory.</summary>
    public void Open(KitchenStove stove, InventoryComponent playerInv)
    {
        UnsubscribeFromStove(currentStove);
        currentStove = stove != null ? stove : KitchenStove.Instance;
        SubscribeToStove(currentStove);

        var recipes = (currentStove != null && currentStove.availableRecipes != null)
            ? currentStove.availableRecipes
            : new KitchenRecipe[0];

        OpenInternal(recipes, playerInv);
    }

    /// <summary>Legacy overload: opens panel directly with recipe array.</summary>
    public void Open(KitchenRecipe[] recipes, InventoryComponent playerInv)
    {
        UnsubscribeFromStove(currentStove);
        currentStove = KitchenStove.Instance;
        SubscribeToStove(currentStove);

        OpenInternal(recipes, playerInv);
    }

    private void OpenInternal(KitchenRecipe[] recipes, InventoryComponent playerInv)
    {
        allRecipes = recipes != null ? new List<KitchenRecipe>(recipes) : new List<KitchenRecipe>();

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        }
        playerInventory = playerInv;
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += HandleInventoryChanged;
        }

        if (panelStove != null)
            panelStove.SetActive(true);

        ModalStackManager.Instance?.Push(this);

        EnsureCookedSlotUI();
        EnsureBurnerUI();
        PopulateRecipeList();
        UpdateCookedSlotUI();
        UpdateAllBurnerCards();

        selectedRecipe = null;
        ClearDetail();

        // Unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Lock player input
        var playerControl = ServiceLocator.Resolve<IPlayerContext>();
        if (playerControl != null)
            playerControl.IsInputLocked = true;
    }

    /// <summary>Closes the stove panel without cancelling background cooking.</summary>
    public void Close()
    {
        if (cancelCookingOnPanelClose && currentStove != null && currentStove.ActiveCookingCount > 0)
        {
            for (int i = 0; i < KitchenStove.MaxCookingSlots; i++)
            {
                if (currentStove.IsSlotCooking(i))
                    currentStove.CancelSlotCooking(i, refundIngredients: true);
            }
        }

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        UnsubscribeFromStove(currentStove);

        if (panelStove != null)
            panelStove.SetActive(false);

        selectedRecipe = null;
        ClearDetail();

        // Restore cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Unlock player input
        var playerClose = ServiceLocator.Resolve<IPlayerContext>();
        if (playerClose != null)
            playerClose.IsInputLocked = false;

        ModalStackManager.Instance?.PopSpecific(this);
    }

    void IModalWindow.OpenModal()
    {
        var inv = FindFirstObjectByType<InventoryComponent>();
        Open(KitchenStove.Instance, inv);
    }

    void IModalWindow.CloseModal() => Close();

    #endregion

    #region Event Handlers & Stove Sync

    private void HandleInventoryChanged()
    {
        if (selectedRecipe != null)
        {
            UpdateIngredientDisplay(selectedRecipe);
        }
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    private void SubscribeToStove(KitchenStove stove)
    {
        if (stove == null) return;
        stove.OnCookingStateChanged += HandleCookingStateChanged;
        stove.OnCookingStarted += HandleCookingStarted;
        stove.OnCookingProgress += HandleCookingProgress;
        stove.OnCookingCompleted += HandleCookingCompleted;
        stove.OnCookingCancelled += HandleCookingCancelled;
        stove.OnCookedOutputChanged += HandleCookedOutputChanged;

        // Multi-slot burner events
        stove.OnSlotCookingStarted += HandleSlotCookingStarted;
        stove.OnSlotCookingProgress += HandleSlotCookingProgress;
        stove.OnSlotCookingCompleted += HandleSlotCookingCompleted;
        stove.OnSlotCookingCancelled += HandleSlotCookingCancelled;
        stove.OnSlotOutputChanged += HandleSlotOutputChanged;
    }

    private void UnsubscribeFromStove(KitchenStove stove)
    {
        if (stove == null) return;
        stove.OnCookingStateChanged -= HandleCookingStateChanged;
        stove.OnCookingStarted -= HandleCookingStarted;
        stove.OnCookingProgress -= HandleCookingProgress;
        stove.OnCookingCompleted -= HandleCookingCompleted;
        stove.OnCookingCancelled -= HandleCookingCancelled;
        stove.OnCookedOutputChanged -= HandleCookedOutputChanged;

        // Multi-slot burner events
        stove.OnSlotCookingStarted -= HandleSlotCookingStarted;
        stove.OnSlotCookingProgress -= HandleSlotCookingProgress;
        stove.OnSlotCookingCompleted -= HandleSlotCookingCompleted;
        stove.OnSlotCookingCancelled -= HandleSlotCookingCancelled;
        stove.OnSlotOutputChanged -= HandleSlotOutputChanged;
    }

    private void HandleCookingStateChanged(CookingState state)
    {
        UpdateAllBurnerCards();
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    private void HandleCookingStarted(KitchenRecipe recipe, float duration)
    {
        UpdateAllBurnerCards();
        UpdateCookButton();
        if (selectedRecipe != null)
            UpdateIngredientDisplay(selectedRecipe);
        UpdateRecipeListVisuals();
    }

    private void HandleCookingProgress(float progress01, float remainingSeconds)
    {
        UpdateAllBurnerTimers();
    }

    private void HandleCookingCompleted(KitchenRecipe recipe)
    {
        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
        SyncCookingProgressBar();
    }

    private void HandleCookingCancelled(KitchenRecipe recipe)
    {
        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
        SyncCookingProgressBar();
    }

    private void HandleCookedOutputChanged(ItemData item, int count)
    {
        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
        SyncCookingProgressBar();
    }

    private void HandleSlotCookingStarted(int slotIdx, KitchenRecipe recipe, float duration)
    {
        UpdateAllBurnerCards();
        UpdateCookButton();
        if (selectedRecipe != null)
            UpdateIngredientDisplay(selectedRecipe);
        UpdateRecipeListVisuals();
        SyncCookingProgressBar();
    }

    private void HandleSlotCookingProgress(int slotIdx, float progress01, float remaining)
    {
        UpdateAllBurnerTimers();
    }

    private void HandleSlotCookingCompleted(int slotIdx, KitchenRecipe recipe)
    {
        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
        SyncCookingProgressBar();
    }

    private void HandleSlotCookingCancelled(int slotIdx, KitchenRecipe recipe)
    {
        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
        SyncCookingProgressBar();
    }

    private void HandleSlotOutputChanged(int slotIdx, ItemData item, int count)
    {
        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    #endregion

    #region Recipe List & Can-Cook Visual Indicator

    private void PopulateRecipeList()
    {
        foreach (var btn in spawnedRecipeButtons)
            SafeDestroy(btn);
        spawnedRecipeButtons.Clear();
        spawnedRecipeButtonEntries.Clear();

        if (recipeListContent == null || recipeButtonPrefab == null) return;

        foreach (var recipe in allRecipes)
        {
            if (recipe == null) continue;

            GameObject btnGO = Instantiate(recipeButtonPrefab, recipeListContent, false);
            btnGO.transform.localScale = Vector3.one;
            btnGO.SetActive(true);
            spawnedRecipeButtons.Add(btnGO);

            var icon = btnGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && recipe.output != null && recipe.output.itemIcon != null)
            {
                icon.sprite = recipe.output.itemIcon;
                icon.enabled = true;
            }

            var nameText = btnGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                string displayName = !string.IsNullOrEmpty(recipe.recipeName) ? recipe.recipeName : (recipe.output != null ? recipe.output.itemName : recipe.name);
                nameText.text = displayName;
            }

            var border = btnGO.transform.Find("Border")?.GetComponent<Image>();
            var statusBadge = btnGO.transform.Find("StatusBadge")?.GetComponent<TextMeshProUGUI>();

            var button = btnGO.GetComponent<Button>();
            if (button != null)
            {
                KitchenRecipe capturedRecipe = recipe;
                button.onClick.AddListener(() => SelectRecipe(capturedRecipe));
            }

            spawnedRecipeButtonEntries.Add(new RecipeButtonEntry
            {
                recipe = recipe,
                icon = icon,
                nameText = nameText,
                statusBadge = statusBadge,
                border = border,
                button = button
            });
        }

        UpdateRecipeListVisuals();
    }

    /// <summary>
    /// Evaluates each recipe against player inventory and clean water bottle.
    /// If cookable: normal color and full brightness, [Ready] badge.
    /// If missing ingredients: grayscale desaturated visual, [Missing] badge.
    /// </summary>
    private void UpdateRecipeListVisuals()
    {
        foreach (var entry in spawnedRecipeButtonEntries)
        {
            if (entry.recipe == null) continue;

            bool canCook = currentStove != null && playerInventory != null && currentStove.CanCook(entry.recipe, playerInventory, out _);

            if (entry.icon != null)
            {
                if (canCook)
                {
                    entry.icon.material = null;
                    entry.icon.color = Color.white;
                }
                else
                {
                    entry.icon.material = GrayscaleMaterial;
                    entry.icon.color = new Color(0.48f, 0.48f, 0.48f, 0.72f);
                }
            }

            if (entry.nameText != null)
            {
                entry.nameText.color = canCook ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.7f);
            }

            if (entry.statusBadge != null)
            {
                entry.statusBadge.text = canCook ? "Ready" : "Missing";
                entry.statusBadge.color = canCook ? new Color(0.2f, 0.85f, 0.45f, 1f) : new Color(0.6f, 0.65f, 0.7f, 0.7f);
            }

            if (entry.border != null)
            {
                bool isSelected = selectedRecipe != null && entry.recipe == selectedRecipe;
                entry.border.color = isSelected
                    ? new Color(0.96f, 0.62f, 0.04f, 1f)
                    : new Color(0.2f, 0.25f, 0.35f, 0.35f);
            }
        }
    }

    #endregion

    #region Recipe Detail Presentation

    private void SelectRecipe(KitchenRecipe recipe)
    {
        selectedRecipe = recipe;

        if (emptyStatePlaceholder != null)
            emptyStatePlaceholder.SetActive(false);

        if (resultIcon != null)
        {
            if (recipe != null && recipe.output != null && recipe.output.itemIcon != null)
            {
                resultIcon.sprite = recipe.output.itemIcon;
                resultIcon.enabled = true;
            }
            else
            {
                resultIcon.enabled = false;
            }
        }

        if (resultName != null)
            resultName.text = recipe != null && recipe.output != null ? recipe.output.itemName : (recipe != null ? recipe.name : "");

        if (resultDescription != null)
        {
            if (recipe == null)
            {
                resultDescription.text = "";
            }
            else
            {
                string desc = !string.IsNullOrEmpty(recipe.description)
                    ? recipe.description
                    : (recipe.output != null ? recipe.output.description : "");

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"<size=105%>{desc}</size>");

                if (recipe.output is FoodItemData food)
                {
                    List<string> nutrition = new List<string>();
                    if (food.hungerRestore != 0) nutrition.Add($"<b>Hunger:</b> {(food.hungerRestore > 0 ? "+" : "")}{food.hungerRestore:F0}");
                    if (food.hydrationRestore != 0) nutrition.Add($"<b>Hydration:</b> {(food.hydrationRestore > 0 ? "+" : "")}{food.hydrationRestore:F0}");
                    if (food.healAmount != 0) nutrition.Add($"<b>HP:</b> {(food.healAmount > 0 ? "+" : "")}{food.healAmount}");

                    if (nutrition.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"<color=#38BDF8>● {string.Join("   ● ", nutrition)}</color>");
                    }

                    if (food.buffEffects != null && food.buffEffects.Count > 0)
                    {
                        sb.AppendLine();
                        foreach (var buff in food.buffEffects)
                        {
                            string buffDesc = !string.IsNullOrEmpty(buff.description) ? buff.description : buff.buffName;
                            sb.AppendLine($"<color=#FBBF24>• {buffDesc} ({buff.duration}s)</color>");
                        }
                    }
                }

                resultDescription.text = sb.ToString().TrimEnd();
            }
        }

        if (processTimeText != null)
        {
            string tier = recipe != null && recipe.output is FoodItemData f ? $"{f.foodTier} Tier Meal" : "Cooked Dish";
            processTimeText.text = recipe != null ? $"<color=#38BDF8>{tier}</color>  •  <color=#FBBF24>Cook Time: {FormatDuration(recipe.processTime)}</color>" : "";
        }

        UpdateRecipeListVisuals();

        if (recipe != null)
            PopulateIngredientRows(recipe);

        UpdateCookButton();
    }

    private void PopulateIngredientRows(KitchenRecipe recipe)
    {
        foreach (var row in spawnedIngredientRows)
            SafeDestroy(row);
        spawnedIngredientRows.Clear();

        if (ingredientContainer == null || ingredientRowPrefab == null || recipe == null) return;

        var ingredients = recipe.GetAllIngredients();
        foreach (var ingredient in ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;

            GameObject rowGO = Instantiate(ingredientRowPrefab, ingredientContainer, false);
            rowGO.transform.localScale = Vector3.one;
            rowGO.SetActive(true);
            spawnedIngredientRows.Add(rowGO);

            var icon = rowGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && ingredient.item.itemIcon != null)
            {
                icon.sprite = ingredient.item.itemIcon;
                icon.enabled = true;
            }

            var nameText = rowGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = ingredient.item.itemName;

            int owned = playerInventory != null ? playerInventory.CountItem(ingredient.item) : 0;
            int required = ingredient.quantity;
            var countText = rowGO.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = $"{owned}/{required}";
                countText.color = owned >= required ? haveEnoughColor : notEnoughColor;
            }
        }

        // Clean water requirement row
        if (recipe.waterRequired > 0f)
        {
            GameObject waterRowGO = Instantiate(ingredientRowPrefab, ingredientContainer, false);
            waterRowGO.transform.localScale = Vector3.one;
            waterRowGO.SetActive(true);
            spawnedIngredientRows.Add(waterRowGO);

            var icon = waterRowGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                var db = cachedItemDatabase != null ? cachedItemDatabase : Resources.Load<ItemDatabase>("Database/ItemDatabase");
                var waterItem = db != null ? db.GetItem("food_bottle_water") : null;
                if (waterItem != null && waterItem.itemIcon != null)
                {
                    icon.sprite = waterItem.itemIcon;
                    icon.enabled = true;
                }
                else
                {
                    icon.color = new Color(0.3f, 0.7f, 1f, 1f);
                }
            }

            var nameText = waterRowGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = "Clean Water";

            float ownedWater = PlayerWaterBottle.Instance != null ? PlayerWaterBottle.Instance.CurrentWater : 0f;
            float reqWater = recipe.waterRequired;
            var countText = waterRowGO.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = $"{Mathf.FloorToInt(ownedWater)}/{reqWater:F0} L";
                countText.color = ownedWater >= reqWater ? haveEnoughColor : notEnoughColor;
            }
        }
    }

    private void UpdateIngredientDisplay(KitchenRecipe recipe)
    {
        PopulateIngredientRows(recipe);
    }

    #endregion

    #region 3-Burner Multi-Slot UI Hub

    /// <summary>
    /// Ensures the 3-Burner Multi-Slot UI hub exists and cards are instantiated.
    /// Replaces old single-slot output with 3 dedicated burner status cards.
    /// </summary>
    private void EnsureBurnerUI()
    {
        if (burnerHubRoot != null) return;

        Transform parentZone = null;
        if (cookedSlotRoot != null)
        {
            parentZone = cookedSlotRoot.transform.parent;
            cookedSlotRoot.SetActive(false); // Hide old single-slot HearthTray
        }
        else if (panelStove != null)
        {
            parentZone = panelStove.transform.Find("RightContent/BottomCookZone");
        }

        if (parentZone == null) return;

        Transform existing = parentZone.Find("BurnerHubZone");
        if (existing != null)
        {
            burnerHubRoot = existing.gameObject;
            BindBurnerSlotUIs(burnerHubRoot);
            return;
        }

        GameObject hubGO = new GameObject("BurnerHubZone", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hubGO.transform.SetParent(parentZone, false);
        burnerHubRoot = hubGO;

        var hubRect = hubGO.GetComponent<RectTransform>();
        hubRect.anchorMin = new Vector2(0f, 0.5f);
        hubRect.anchorMax = new Vector2(0f, 0.5f);
        hubRect.pivot = new Vector2(0f, 0.5f);
        hubRect.anchoredPosition = new Vector2(24f, 0f);
        hubRect.sizeDelta = new Vector2(380f, 96f);

        var hlg = hubGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < KitchenStove.MaxCookingSlots; i++)
        {
            burnerSlotUIs[i] = CreateBurnerCard(hubGO.transform, i);
        }
    }

    private BurnerSlotUI CreateBurnerCard(Transform parent, int slotIndex)
    {
        var cardGO = new GameObject($"BurnerCard_{slotIndex + 1}", typeof(RectTransform), typeof(Image));
        cardGO.transform.SetParent(parent, false);

        var cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(118f, 90f);

        var bg = cardGO.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);

        TMP_FontAsset fontAsset = resultName != null ? resultName.font : null;

        // Title
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(cardGO.transform, false);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);
        titleRect.sizeDelta = new Vector2(-8f, 16f);

        var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) titleTmp.font = fontAsset;
        titleTmp.text = $"Burner {slotIndex + 1}";
        titleTmp.fontSize = 11f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.7f, 0.75f, 0.85f, 1f);

        // Middle Row (Icon & Status Text)
        var midRowGO = new GameObject("MidRow", typeof(RectTransform));
        midRowGO.transform.SetParent(cardGO.transform, false);
        var midRect = midRowGO.GetComponent<RectTransform>();
        midRect.anchorMin = new Vector2(0f, 1f);
        midRect.anchorMax = new Vector2(1f, 1f);
        midRect.pivot = new Vector2(0.5f, 1f);
        midRect.anchoredPosition = new Vector2(0f, -22f);
        midRect.sizeDelta = new Vector2(-8f, 32f);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(midRowGO.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(2f, 0f);
        iconRect.sizeDelta = new Vector2(28f, 28f);

        var iconImg = iconGO.GetComponent<Image>();
        iconImg.enabled = false;

        var statusGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(midRowGO.transform, false);
        var statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.offsetMin = new Vector2(34f, 0f);
        statusRect.offsetMax = new Vector2(0f, 0f);

        var statusTmp = statusGO.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) statusTmp.font = fontAsset;
        statusTmp.text = "Empty\n<color=#22C55E>Ready</color>";
        statusTmp.fontSize = 10f;
        statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
        statusTmp.lineSpacing = -15f;
        statusTmp.overflowMode = TextOverflowModes.Ellipsis;

        // Mini Progress Bar
        var barRootGO = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
        barRootGO.transform.SetParent(cardGO.transform, false);
        var barRootRect = barRootGO.GetComponent<RectTransform>();
        barRootRect.anchorMin = new Vector2(0f, 0f);
        barRootRect.anchorMax = new Vector2(1f, 0f);
        barRootRect.pivot = new Vector2(0.5f, 0f);
        barRootRect.anchoredPosition = new Vector2(0f, 28f);
        barRootRect.sizeDelta = new Vector2(-12f, 4f);

        var barBgImg = barRootGO.GetComponent<Image>();
        barBgImg.color = new Color(0.18f, 0.22f, 0.30f, 1f);

        var barFillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        barFillGO.transform.SetParent(barRootGO.transform, false);
        var barFillRect = barFillGO.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;

        var barFillImg = barFillGO.GetComponent<Image>();
        barFillImg.sprite = GetWhiteSprite();
        barFillImg.color = new Color(0.96f, 0.62f, 0.04f, 1f);
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFillImg.fillAmount = 0f;
        barRootGO.SetActive(false);

        // Action Button
        var btnGO = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 0f);
        btnRect.anchorMax = new Vector2(1f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 4f);
        btnRect.sizeDelta = new Vector2(-12f, 22f);

        var btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.3f, 0.35f, 0.42f, 1f);

        var btn = btnGO.GetComponent<Button>();
        int capturedIdx = slotIndex;
        btn.onClick.AddListener(() => OnBurnerActionClicked(capturedIdx));

        var btnLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLabelGO.transform.SetParent(btnGO.transform, false);
        var btnLabelRect = btnLabelGO.GetComponent<RectTransform>();
        btnLabelRect.anchorMin = Vector2.zero;
        btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero;
        btnLabelRect.offsetMax = Vector2.zero;

        var btnLabelTmp = btnLabelGO.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) btnLabelTmp.font = fontAsset;
        btnLabelTmp.text = "";
        btnLabelTmp.fontSize = 10f;
        btnLabelTmp.fontStyle = FontStyles.Bold;
        btnLabelTmp.alignment = TextAlignmentOptions.Center;
        btnLabelTmp.color = Color.white;

        btnGO.SetActive(false);

        return new BurnerSlotUI
        {
            slotIndex = slotIndex,
            root = cardGO,
            background = bg,
            titleText = titleTmp,
            iconImage = iconImg,
            statusText = statusTmp,
            progressBarRoot = barRootGO,
            progressBarFill = barFillImg,
            actionButton = btn,
            actionButtonText = btnLabelTmp,
            actionButtonImage = btnImg
        };
    }

    private void BindBurnerSlotUIs(GameObject hubGO)
    {
        for (int i = 0; i < KitchenStove.MaxCookingSlots; i++)
        {
            var card = hubGO.transform.Find($"BurnerCard_{i + 1}");
            if (card != null)
            {
                var bg = card.GetComponent<Image>();
                var titleTmp = card.Find("Title")?.GetComponent<TextMeshProUGUI>();
                var iconImg = card.Find("MidRow/Icon")?.GetComponent<Image>();
                var statusTmp = card.Find("MidRow/StatusText")?.GetComponent<TextMeshProUGUI>();
                var barRoot = card.Find("ProgressBar")?.gameObject;
                var barFillImg = card.Find("ProgressBar/Fill")?.GetComponent<Image>();
                if (barFillImg != null)
                {
                    if (barFillImg.sprite == null)
                        barFillImg.sprite = GetWhiteSprite();
                    barFillImg.type = Image.Type.Filled;
                    barFillImg.fillMethod = Image.FillMethod.Horizontal;
                    barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                }
                var btn = card.Find("ActionButton")?.GetComponent<Button>();
                var btnImg = card.Find("ActionButton")?.GetComponent<Image>();
                var btnLabelTmp = card.Find("ActionButton/Label")?.GetComponent<TextMeshProUGUI>();

                if (btn != null)
                {
                    int capturedIdx = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnBurnerActionClicked(capturedIdx));
                }

                burnerSlotUIs[i] = new BurnerSlotUI
                {
                    slotIndex = i,
                    root = card.gameObject,
                    background = bg,
                    titleText = titleTmp,
                    iconImage = iconImg,
                    statusText = statusTmp,
                    progressBarRoot = barRoot,
                    progressBarFill = barFillImg,
                    actionButton = btn,
                    actionButtonText = btnLabelTmp,
                    actionButtonImage = btnImg
                };
            }
        }
    }

    /// <summary>Updates visuals of all 3 burner cards based on current stove state.</summary>
    public void UpdateAllBurnerCards()
    {
        EnsureBurnerUI();
        if (currentStove == null) return;

        for (int i = 0; i < KitchenStove.MaxCookingSlots; i++)
        {
            var ui = burnerSlotUIs[i];
            if (ui == null || ui.root == null) continue;

            var slot = currentStove.GetSlot(i);
            if (slot == null || slot.IsIdle)
            {
                ui.titleText.text = $"Burner {i + 1}";
                ui.titleText.color = new Color(0.6f, 0.65f, 0.75f, 1f);
                ui.iconImage.enabled = false;
                ui.statusText.text = "Empty\n<color=#22C55E>Ready</color>";
                ui.statusText.alignment = TextAlignmentOptions.Center;
                ui.progressBarRoot.SetActive(false);
                ui.actionButton.gameObject.SetActive(false);
                ui.background.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
            }
            else if (slot.IsCooking)
            {
                ui.titleText.text = $"🔥 Burner {i + 1}";
                ui.titleText.color = new Color(0.96f, 0.62f, 0.04f, 1f);

                string dishName = (slot.activeRecipe != null && slot.activeRecipe.output != null)
                    ? slot.activeRecipe.output.itemName
                    : "Cooking";

                if (slot.activeRecipe != null && slot.activeRecipe.output != null && slot.activeRecipe.output.itemIcon != null)
                {
                    ui.iconImage.sprite = slot.activeRecipe.output.itemIcon;
                    ui.iconImage.enabled = true;
                }
                else
                {
                    ui.iconImage.enabled = false;
                }

                ui.statusText.alignment = TextAlignmentOptions.MidlineLeft;
                float prog = Mathf.Clamp01(slot.Progress01);
                ui.statusText.text = $"<b>{dishName}</b>\n<color=#38BDF8>{FormatDuration(slot.remainingTime)} ({Mathf.RoundToInt(prog * 100f)}%)</color>";

                ui.progressBarRoot.SetActive(true);
                if (ui.progressBarFill != null)
                {
                    if (ui.progressBarFill.sprite == null)
                        ui.progressBarFill.sprite = GetWhiteSprite();
                    ui.progressBarFill.fillAmount = prog;
                }

                ui.actionButton.gameObject.SetActive(true);
                ui.actionButtonImage.color = new Color(0.85f, 0.25f, 0.25f, 1f);
                ui.actionButtonText.text = "Cancel";

                ui.background.color = new Color(0.13f, 0.16f, 0.23f, 0.98f);
            }
            else if (slot.HasOutput)
            {
                ui.titleText.text = $"🍲 Burner {i + 1}";
                ui.titleText.color = new Color(0.2f, 0.85f, 0.45f, 1f);

                var item = slot.outputSlot.item;
                int count = slot.outputSlot.quantity;
                string dishName = item != null ? item.itemName : "Dish";

                if (item != null && item.itemIcon != null)
                {
                    ui.iconImage.sprite = item.itemIcon;
                    ui.iconImage.enabled = true;
                }
                else
                {
                    ui.iconImage.enabled = false;
                }

                ui.statusText.alignment = TextAlignmentOptions.MidlineLeft;
                ui.statusText.text = $"<b>{dishName}</b>\n<color=#22C55E>Ready! x{count}</color>";

                ui.progressBarRoot.SetActive(false);

                ui.actionButton.gameObject.SetActive(true);
                ui.actionButtonImage.color = new Color(0.18f, 0.65f, 0.38f, 1f);
                ui.actionButtonText.text = "Take Dish";

                ui.background.color = new Color(0.10f, 0.18f, 0.15f, 0.98f);
            }
        }

        SyncCookingProgressBar();
    }

    private void UpdateAllBurnerTimers()
    {
        if (currentStove == null) return;

        for (int i = 0; i < KitchenStove.MaxCookingSlots; i++)
        {
            var slot = currentStove.GetSlot(i);
            var ui = burnerSlotUIs[i];
            if (slot == null || ui == null || !slot.IsCooking) continue;

            string dishName = (slot.activeRecipe != null && slot.activeRecipe.output != null)
                ? slot.activeRecipe.output.itemName
                : "Cooking";

            float prog = Mathf.Clamp01(slot.Progress01);
            ui.statusText.text = $"<b>{dishName}</b>\n<color=#38BDF8>{FormatDuration(slot.remainingTime)} ({Mathf.RoundToInt(prog * 100f)}%)</color>";
            if (ui.progressBarFill != null)
            {
                if (ui.progressBarFill.sprite == null)
                    ui.progressBarFill.sprite = GetWhiteSprite();
                ui.progressBarFill.fillAmount = prog;
            }
        }

        SyncCookingProgressBar();
    }

    private void SyncCookingProgressBar()
    {
        // Legacy global cooking progress bar above 'Select a Recipe' removed per user request.
        // Each burner slot (Burner 1, 2, 3) already has dedicated progress indicators.
        if (cookingProgressBar != null && cookingProgressBar.gameObject.activeSelf)
        {
            cookingProgressBar.gameObject.SetActive(false);
        }
    }

    private void OnBurnerActionClicked(int slotIndex)
    {
        if (currentStove == null) return;
        var slot = currentStove.GetSlot(slotIndex);
        if (slot == null) return;

        if (slot.IsCooking)
        {
            currentStove.CancelSlotCooking(slotIndex, refundIngredients: true);
            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                currentStove.transform.position + Vector3.up * 1.5f,
                $"Burner {slotIndex + 1} cooking cancelled (Refunded)",
                new Color(1f, 0.6f, 0.2f));
        }
        else if (slot.HasOutput)
        {
            if (playerInventory == null) return;
            bool ok = currentStove.CollectSlotOutput(slotIndex, playerInventory, out string failReason);
            if (ok)
            {
                ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                    currentStove.transform.position + Vector3.up * 1.5f,
                    $"Collected dish from Burner {slotIndex + 1}!",
                    new Color(0.3f, 0.9f, 0.4f));
            }
            else
            {
                ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                    currentStove.transform.position + Vector3.up * 1.5f,
                    failReason,
                    new Color(1f, 0.4f, 0.4f));
            }
        }

        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    #endregion

    #region Cooked Output Slot Presentation (Legacy Support)

    private void EnsureCookedSlotUI()
    {
        if (cookedSlotRoot != null)
        {
            if (collectCookedButton != null)
            {
                collectCookedButton.onClick.RemoveListener(OnCollectClicked);
                collectCookedButton.onClick.AddListener(OnCollectClicked);
            }
            var existingSlotBtn = cookedSlotRoot.transform.Find("SlotBox")?.GetComponent<Button>();
            if (existingSlotBtn != null)
            {
                existingSlotBtn.onClick.RemoveListener(OnCollectClicked);
                existingSlotBtn.onClick.AddListener(OnCollectClicked);
            }
            return;
        }

        Transform rightContent = panelStove != null ? panelStove.transform.Find("RightContent") : null;
        if (rightContent == null) return;

        Transform found = rightContent.Find("CookedOutputSlotZone");
        if (found != null)
        {
            cookedSlotRoot = found.gameObject;
            cookedSlotIcon = found.Find("SlotBox/Icon")?.GetComponent<Image>();
            cookedSlotCountText = found.Find("SlotBox/Count")?.GetComponent<TextMeshProUGUI>();
            collectCookedButton = found.Find("CollectButton")?.GetComponent<Button>();
            collectButtonText = collectCookedButton != null ? collectCookedButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (collectCookedButton != null)
            {
                collectCookedButton.onClick.RemoveListener(OnCollectClicked);
                collectCookedButton.onClick.AddListener(OnCollectClicked);
            }
            return;
        }
    }

    public void UpdateCookedSlotUI()
    {
        if (cookedSlotRoot == null) return;

        bool hasOutput = currentStove != null && currentStove.HasCookedOutput;
        var slot = currentStove != null ? currentStove.CookedOutputSlot : null;

        if (hasOutput && slot != null && slot.item != null)
        {
            if (cookedSlotIcon != null)
            {
                cookedSlotIcon.sprite = slot.item.itemIcon;
                cookedSlotIcon.enabled = slot.item.itemIcon != null;
                cookedSlotIcon.color = Color.white;
            }

            if (cookedSlotCountText != null)
            {
                cookedSlotCountText.text = slot.quantity > 1 ? $"x{slot.quantity}" : "";
            }

            if (collectCookedButton != null)
            {
                collectCookedButton.interactable = true;
                var img = collectCookedButton.GetComponent<Image>();
                if (img != null) img.color = new Color(0.18f, 0.65f, 0.38f, 1f);
            }

            if (collectButtonText != null)
            {
                collectButtonText.text = $"Take {slot.item.itemName}";
            }
        }
        else
        {
            if (cookedSlotIcon != null)
            {
                cookedSlotIcon.enabled = false;
            }

            if (cookedSlotCountText != null)
            {
                cookedSlotCountText.text = "";
            }

            if (collectCookedButton != null)
            {
                collectCookedButton.interactable = false;
                var img = collectCookedButton.GetComponent<Image>();
                if (img != null) img.color = new Color(0.35f, 0.4f, 0.45f, 0.5f);
            }

            if (collectButtonText != null)
            {
                collectButtonText.text = "Output Empty";
            }
        }
    }

    private void OnCollectClicked()
    {
        if (currentStove == null || playerInventory == null) return;

        if (!currentStove.HasAnyCookedOutput && !currentStove.HasCookedOutput)
            return;

        bool success = currentStove.CollectAllOutput(playerInventory, out string failReason);
        if (success)
        {
            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                currentStove.transform.position + Vector3.up * 1.5f,
                "Collected cooked dish(es)!",
                new Color(0.3f, 0.9f, 0.4f));
        }
        else
        {
            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                currentStove.transform.position + Vector3.up * 1.5f,
                failReason,
                new Color(1f, 0.4f, 0.4f));
        }

        UpdateAllBurnerCards();
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    #endregion

    #region Cooking Actions & Button Logic

    private void UpdateCookButton()
    {
        if (currentStove == null) return;

        int availableCount = currentStove.AvailableSlotCount;
        int nextSlot = currentStove.GetNextAvailableSlotIndex();

        if (selectedRecipe == null)
        {
            if (cookButton != null) cookButton.interactable = false;
            if (cookButtonImage != null) cookButtonImage.color = cannotCookColor;
            if (cookButtonText != null) cookButtonText.text = "Select a Recipe";
            return;
        }

        if (availableCount == 0)
        {
            if (cookButton != null) cookButton.interactable = false;
            if (cookButtonImage != null) cookButtonImage.color = cannotCookColor;
            if (cookButtonText != null)
            {
                if (currentStove.ActiveCookingCount == KitchenStove.MaxCookingSlots)
                    cookButtonText.text = "All 3 Burners Busy";
                else
                    cookButtonText.text = "Take Food to Cook";
            }
            return;
        }

        string failReason = string.Empty;
        bool canCook = playerInventory != null && currentStove.CanCookSlot(selectedRecipe, playerInventory, nextSlot, out failReason);

        if (cookButton != null)
            cookButton.interactable = canCook;

        if (cookButtonImage != null)
            cookButtonImage.color = canCook ? canCookColor : cannotCookColor;

        if (cookButtonText != null)
        {
            if (canCook)
            {
                cookButtonText.text = $"Cook on Burner {nextSlot + 1} ({availableCount}/3 Free)";
            }
            else
            {
                cookButtonText.text = !string.IsNullOrEmpty(failReason) ? failReason : "Missing Ingredients";
            }
        }
    }

    private void OnCookClicked()
    {
        if (currentStove == null) return;
        if (selectedRecipe == null || playerInventory == null) return;

        int targetSlot = currentStove.GetNextAvailableSlotIndex();
        if (targetSlot < 0)
        {
            Debug.LogWarning("[StoveUIManager] All 3 burners are busy or holding food!");
            return;
        }

        if (!currentStove.CanCookSlot(selectedRecipe, playerInventory, targetSlot, out string failReason))
        {
            Debug.LogWarning($"[StoveUIManager] Cannot cook: {failReason}");
            return;
        }

        bool started = currentStove.StartCookingSlot(selectedRecipe, playerInventory, targetSlot);
        if (started)
        {
            UpdateAllBurnerCards();
            UpdateCookButton();
            UpdateRecipeListVisuals();
            if (selectedRecipe != null)
                UpdateIngredientDisplay(selectedRecipe);
        }
    }

    private void OnCloseClicked()
    {
        Close();
    }

    private void ClearDetail()
    {
        if (resultIcon != null) resultIcon.enabled = false;
        if (resultName != null) resultName.text = "";
        if (resultDescription != null) resultDescription.text = "";
        if (processTimeText != null) processTimeText.text = "";
        if (emptyStatePlaceholder != null) emptyStatePlaceholder.SetActive(true);
        if (cookButton != null) cookButton.interactable = false;

        foreach (var row in spawnedIngredientRows)
            SafeDestroy(row);
        spawnedIngredientRows.Clear();
    }

    private static string FormatDuration(float seconds)
    {
        int totalSec = Mathf.Max(0, Mathf.CeilToInt(seconds));
        if (totalSec < 60) return $"{totalSec}s";
        int m = totalSec / 60;
        int s = totalSec % 60;
        return s > 0 ? $"{m}m {s}s" : $"{m}m";
    }

    private static Sprite _whiteSprite;
    public static Sprite GetWhiteSprite()
    {
        if (_whiteSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return _whiteSprite;
    }

    private static void SafeDestroy(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(go);
        else
            Destroy(go);
#else
        Destroy(go);
#endif
    }

    #endregion
}
