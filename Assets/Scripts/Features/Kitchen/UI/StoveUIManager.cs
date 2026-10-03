using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FarmBeware.Core.Runtime;

/// <summary>
/// UI View/Presenter for the Kitchen Stove cooking panel.
/// Pure presentation layer:
/// - Displays available recipe list with Can-Cook visual indicators (normal color vs. grayscale).
/// - Shows rich recipe details (culinary description, nutritional stats, buffs, cook time).
/// - Provides a dedicated Cooked Food Output Slot on the stove to hold finished dishes.
/// - Allows the player to collect cooked dishes into their inventory without auto-dumping on panel close.
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

    [Header("Cooked Output Slot")]
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
    }

    private void Update()
    {
        if (panelStove != null && panelStove.activeSelf &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
            Close();
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

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
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
        PopulateRecipeList();
        UpdateCookedSlotUI();

        // Check if stove is currently busy cooking
        if (currentStove != null && currentStove.CurrentState == CookingState.Cooking && currentStove.ActiveRecipe != null)
        {
            SelectRecipe(currentStove.ActiveRecipe);
        }
        else
        {
            selectedRecipe = null;
            ClearDetail();
        }

        // Unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Lock player input
        var playerControl = ServiceLocator.Resolve<IPlayerContext>();
        if (playerControl != null)
            playerControl.IsInputLocked = true;
    }

    /// <summary>Closes the stove panel.</summary>
    public void Close()
    {
        if (cancelCookingOnPanelClose && currentStove != null && currentStove.CurrentState == CookingState.Cooking)
        {
            currentStove.CancelCooking(refundIngredients: true);
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
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

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
    }

    private void HandleCookingStateChanged(CookingState state)
    {
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    private void HandleCookingStarted(KitchenRecipe recipe, float duration)
    {
        if (cookingProgressBar != null)
        {
            cookingProgressBar.gameObject.SetActive(true);
            cookingProgressBar.value = 0f;
        }

        if (cookButtonText != null)
            cookButtonText.text = $"Cooking... ({Mathf.CeilToInt(duration)}s) [Cancel]";

        UpdateCookButton();
        UpdateIngredientDisplay(recipe);
        UpdateRecipeListVisuals();
    }

    private void HandleCookingProgress(float progress01, float remainingSeconds)
    {
        if (cookingProgressBar != null)
        {
            if (!cookingProgressBar.gameObject.activeSelf)
                cookingProgressBar.gameObject.SetActive(true);
            cookingProgressBar.value = progress01;
        }

        if (cookButtonText != null)
        {
            cookButtonText.text = $"Cooking... ({Mathf.CeilToInt(remainingSeconds)}s) [Cancel]";
        }
    }

    private void HandleCookingCompleted(KitchenRecipe recipe)
    {
        if (cookingProgressBar != null)
        {
            cookingProgressBar.value = 1f;
            cookingProgressBar.gameObject.SetActive(false);
        }

        UpdateCookedSlotUI();
        UpdateRecipeListVisuals();

        if (selectedRecipe != null)
            SelectRecipe(selectedRecipe);
        else
            UpdateCookButton();
    }

    private void HandleCookingCancelled(KitchenRecipe recipe)
    {
        if (cookingProgressBar != null)
        {
            cookingProgressBar.value = 0f;
            cookingProgressBar.gameObject.SetActive(false);
        }

        UpdateCookedSlotUI();
        UpdateRecipeListVisuals();

        if (selectedRecipe != null)
            SelectRecipe(selectedRecipe);
        else
            UpdateCookButton();
    }

    private void HandleCookedOutputChanged(ItemData item, int count)
    {
        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    #endregion

    #region Recipe List & Can-Cook Visual Indicator

    private void PopulateRecipeList()
    {
        // Clear old buttons
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

            // Set icon
            var icon = btnGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && recipe.output != null && recipe.output.itemIcon != null)
            {
                icon.sprite = recipe.output.itemIcon;
                icon.enabled = true;
            }

            // Set name
            var nameText = btnGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                string displayName = !string.IsNullOrEmpty(recipe.recipeName) ? recipe.recipeName : (recipe.output != null ? recipe.output.itemName : recipe.name);
                nameText.text = displayName;
            }

            // Set border & badge
            var border = btnGO.transform.Find("Border")?.GetComponent<Image>();
            var statusBadge = btnGO.transform.Find("StatusBadge")?.GetComponent<TextMeshProUGUI>();

            // Set click handler
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

        // Update detail panel icon
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

        // Complete rich description: Lore, Nutritional Values, and Buff Effects
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
                            sb.AppendLine($"<color=#FBBF24>✦ {buffDesc} ({buff.duration}s)</color>");
                        }
                    }
                }

                resultDescription.text = sb.ToString().TrimEnd();
            }
        }

        if (processTimeText != null)
        {
            string tier = recipe != null && recipe.output is FoodItemData f ? $"{f.foodTier} Tier Meal" : "Cooked Dish";
            processTimeText.text = recipe != null ? $"<color=#38BDF8>{tier}</color>  •  <color=#FBBF24>Cook Time: {recipe.processTime:F0}s</color>" : "";
        }

        // Highlight selected recipe card in list
        UpdateRecipeListVisuals();

        // Populate ingredients
        if (recipe != null)
            PopulateIngredientRows(recipe);

        // Update cook button
        UpdateCookButton();
    }

    private void PopulateIngredientRows(KitchenRecipe recipe)
    {
        // Clear old rows
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

            // Set icon
            var icon = rowGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && ingredient.item.itemIcon != null)
            {
                icon.sprite = ingredient.item.itemIcon;
                icon.enabled = true;
            }

            // Set name
            var nameText = rowGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = ingredient.item.itemName;

            // Set count "owned / required"
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

            float ownedWater = FeaturesKitchen.PlayerWaterBottle.Instance != null ? FeaturesKitchen.PlayerWaterBottle.Instance.CurrentWater : 0f;
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

    #region Cooked Output Slot Presentation

    /// <summary>
    /// Auto-resolves or dynamically creates the Cooked Food Output Slot UI inside the detail panel.
    /// </summary>
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

        // Check if already created previously in the hierarchy
        Transform found = panelStove != null ? panelStove.transform.Find("RightContent/CookedOutputSlotZone") : null;
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

        Transform rightContent = panelStove != null ? panelStove.transform.Find("RightContent") : null;
        if (rightContent == null) return;

        // Procedural creation with modern clean theme
        GameObject zoneGO = new GameObject("CookedOutputSlotZone", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        zoneGO.transform.SetParent(rightContent, false);
        var zoneRect = zoneGO.GetComponent<RectTransform>();
        zoneRect.sizeDelta = new Vector2(0, 52);

        var hlg = zoneGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 16f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Slot Box
        GameObject slotBox = new GameObject("SlotBox", typeof(RectTransform), typeof(Image), typeof(Button));
        slotBox.transform.SetParent(zoneGO.transform, false);
        var slotRect = slotBox.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(48, 48);
        var slotBg = slotBox.GetComponent<Image>();
        slotBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
        var slotBtn = slotBox.GetComponent<Button>();
        slotBtn.onClick.AddListener(OnCollectClicked);

        // Slot Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotBox.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4, 4);
        iconRect.offsetMax = new Vector2(-4, -4);
        cookedSlotIcon = iconGO.GetComponent<Image>();
        cookedSlotIcon.enabled = false;

        // Slot Count Badge
        GameObject countGO = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        countGO.transform.SetParent(slotBox.transform, false);
        var countRect = countGO.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.4f, 0);
        countRect.anchorMax = new Vector2(1, 0.45f);
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;
        cookedSlotCountText = countGO.GetComponent<TextMeshProUGUI>();
        cookedSlotCountText.alignment = TextAlignmentOptions.BottomRight;
        cookedSlotCountText.fontSize = 14;
        cookedSlotCountText.fontStyle = FontStyles.Bold;
        cookedSlotCountText.color = Color.white;

        // Collect Button
        GameObject collectBtnGO = new GameObject("CollectButton", typeof(RectTransform), typeof(Image), typeof(Button));
        collectBtnGO.transform.SetParent(zoneGO.transform, false);
        var btnRect = collectBtnGO.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(160, 40);
        var btnImage = collectBtnGO.GetComponent<Image>();
        btnImage.color = new Color(0.18f, 0.65f, 0.38f, 1f);
        collectCookedButton = collectBtnGO.GetComponent<Button>();
        collectCookedButton.onClick.AddListener(OnCollectClicked);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(collectBtnGO.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        collectButtonText = labelGO.GetComponent<TextMeshProUGUI>();
        collectButtonText.alignment = TextAlignmentOptions.Center;
        collectButtonText.fontSize = 14;
        collectButtonText.fontStyle = FontStyles.Bold;
        collectButtonText.color = Color.white;
        collectButtonText.text = "Collect Dish";

        cookedSlotRoot = zoneGO;

        // Position it right before BottomCookZone
        Transform bottomCookZone = rightContent.Find("BottomCookZone");
        if (bottomCookZone != null)
        {
            zoneGO.transform.SetSiblingIndex(bottomCookZone.GetSiblingIndex());
        }
    }

    /// <summary>
    /// Updates the output slot presentation based on current stove output state.
    /// </summary>
    public void UpdateCookedSlotUI()
    {
        EnsureCookedSlotUI();
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

        if (!currentStove.HasCookedOutput)
            return;

        bool success = currentStove.CollectCookedOutput(playerInventory, out string failReason);
        if (success)
        {
            if (currentStove != null)
            {
                ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                    currentStove.transform.position + Vector3.up * 1.5f,
                    "Collected cooked dish!",
                    new Color(0.3f, 0.9f, 0.4f));
            }
        }
        else
        {
            if (currentStove != null)
            {
                ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                    currentStove.transform.position + Vector3.up * 1.5f,
                    failReason,
                    new Color(1f, 0.4f, 0.4f));
            }
            Debug.LogWarning($"[StoveUIManager] Failed to collect dish: {failReason}");
        }

        UpdateCookedSlotUI();
        UpdateCookButton();
        UpdateRecipeListVisuals();
    }

    #endregion

    #region Cooking Actions & Button Logic

    private void UpdateCookButton()
    {
        bool isCookingNow = currentStove != null && currentStove.CurrentState == CookingState.Cooking;

        if (isCookingNow)
        {
            if (cookButton != null)
                cookButton.interactable = true; // Can be clicked to cancel

            if (cookButtonImage != null)
                cookButtonImage.color = cannotCookColor;

            return;
        }

        bool canCook = currentStove != null && selectedRecipe != null && playerInventory != null &&
                       currentStove.CanCook(selectedRecipe, playerInventory, out string failReason);

        if (cookButton != null)
            cookButton.interactable = canCook;

        if (cookButtonImage != null)
            cookButtonImage.color = canCook ? canCookColor : cannotCookColor;

        if (cookButtonText != null)
        {
            if (canCook)
            {
                cookButtonText.text = "Cook!";
            }
            else
            {
                if (currentStove != null && currentStove.HasCookedOutput && selectedRecipe != null &&
                    currentStove.CookedOutputSlot.item != selectedRecipe.output)
                {
                    cookButtonText.text = "Clear Output Slot";
                }
                else
                {
                    cookButtonText.text = "Missing Ingredients";
                }
            }
        }
    }

    private void OnCookClicked()
    {
        if (currentStove == null) return;

        // If cooking is in progress, clicking acts as Cancel
        if (currentStove.CurrentState == CookingState.Cooking)
        {
            currentStove.CancelCooking(refundIngredients: true);
            return;
        }

        if (selectedRecipe == null || playerInventory == null) return;

        if (!currentStove.CanCook(selectedRecipe, playerInventory, out string failReason))
        {
            Debug.LogWarning($"[StoveUIManager] Cannot cook: {failReason}");
            return;
        }

        currentStove.StartCooking(selectedRecipe, playerInventory);
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
