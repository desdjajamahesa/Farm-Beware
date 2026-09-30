using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// UI View/Presenter untuk panel memasak kompor (Kitchen Stove).
/// Bertindak murni sebagai layer presentasi: menampilkan daftar resep, detail bahan,
/// dan memancarkan aksi pengguna ke backend controller (KitchenStove).
/// Berlangganan event OnCookingStateChanged, OnCookingStarted, OnCookingProgress,
/// OnCookingCompleted, dan OnCookingCancelled dari backend tanpa mengelola timer atau mutasi inventaris langsung.
/// </summary>
public class StoveUIManager : MonoBehaviour
{
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

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Colors")]
    [SerializeField] private Color canCookColor = new Color(0.2f, 0.7f, 0.3f, 1f);
    [SerializeField] private Color cannotCookColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color haveEnoughColor = new Color(0.3f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color notEnoughColor = new Color(0.9f, 0.3f, 0.3f, 1f);

    private List<KitchenRecipe> allRecipes = new List<KitchenRecipe>();
    private InventoryComponent playerInventory;
    private KitchenRecipe selectedRecipe;
    private KitchenStove currentStove;
    private readonly List<GameObject> spawnedRecipeButtons = new List<GameObject>();
    private readonly List<GameObject> spawnedIngredientRows = new List<GameObject>();

    public static StoveUIManager Instance { get; private set; }
    public bool IsPanelOpen => panelStove != null && panelStove.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (cookButton != null)
            cookButton.onClick.AddListener(OnCookClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    /// <summary>Buka panel stove dengan referensi controller KitchenStove dan inventory pemain.</summary>
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

    /// <summary>Overload legacy: buka panel stove dengan daftar resep langsung.</summary>
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
        playerInventory = playerInv;
        selectedRecipe = null;

        if (panelStove != null)
            panelStove.SetActive(true);

        PopulateRecipeList();
        ClearDetail();

        // Unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Lock player input
        var playerControl = FindFirstObjectByType<PlayerControl>();
        if (playerControl != null)
            playerControl.isInputLocked = true;
    }

    private void Update()
    {
        if (panelStove != null && panelStove.activeSelf &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            MainMenuController.LastFrameUIPanelClosed = Time.frameCount;
            Close();
        }
    }

    /// <summary>Tutup panel stove.</summary>
    public void Close()
    {
        if (currentStove != null && currentStove.CurrentState == CookingState.Cooking)
        {
            currentStove.CancelCooking(refundIngredients: true);
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
        var playerControl = FindFirstObjectByType<PlayerControl>();
        if (playerControl != null)
            playerControl.isInputLocked = false;
    }

    private void SubscribeToStove(KitchenStove stove)
    {
        if (stove == null) return;
        stove.OnCookingStateChanged += HandleCookingStateChanged;
        stove.OnCookingStarted += HandleCookingStarted;
        stove.OnCookingProgress += HandleCookingProgress;
        stove.OnCookingCompleted += HandleCookingCompleted;
        stove.OnCookingCancelled += HandleCookingCancelled;
    }

    private void UnsubscribeFromStove(KitchenStove stove)
    {
        if (stove == null) return;
        stove.OnCookingStateChanged -= HandleCookingStateChanged;
        stove.OnCookingStarted -= HandleCookingStarted;
        stove.OnCookingProgress -= HandleCookingProgress;
        stove.OnCookingCompleted -= HandleCookingCompleted;
        stove.OnCookingCancelled -= HandleCookingCancelled;
    }

    private void HandleCookingStateChanged(CookingState state)
    {
        UpdateCookButton();
    }

    private void HandleCookingStarted(KitchenRecipe recipe, float duration)
    {
        if (cookingProgressBar != null)
        {
            cookingProgressBar.gameObject.SetActive(true);
            cookingProgressBar.value = 0f;
        }

        if (cookButton != null)
            cookButton.interactable = false;

        if (cookButtonText != null)
            cookButtonText.text = $"Cooking... ({Mathf.CeilToInt(duration)}s)";

        UpdateIngredientDisplay(recipe);
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
            cookButtonText.text = $"Cooking... ({Mathf.CeilToInt(remainingSeconds)}s)";
        }
    }

    private void HandleCookingCompleted(KitchenRecipe recipe)
    {
        if (cookingProgressBar != null)
        {
            cookingProgressBar.value = 1f;
            cookingProgressBar.gameObject.SetActive(false);
        }

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

        if (selectedRecipe != null)
            SelectRecipe(selectedRecipe);
        else
            UpdateCookButton();
    }

    private void PopulateRecipeList()
    {
        // Clear old buttons
        foreach (var btn in spawnedRecipeButtons)
            if (btn != null) Destroy(btn);
        spawnedRecipeButtons.Clear();

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

            // Set click handler
            var button = btnGO.GetComponent<Button>();
            if (button != null)
            {
                KitchenRecipe capturedRecipe = recipe;
                button.onClick.AddListener(() => SelectRecipe(capturedRecipe));
            }
        }
    }

    private void SelectRecipe(KitchenRecipe recipe)
    {
        if (currentStove != null && currentStove.CurrentState == CookingState.Cooking) return;
        selectedRecipe = recipe;

        if (emptyStatePlaceholder != null)
            emptyStatePlaceholder.SetActive(false);

        // Update detail panel
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
            resultDescription.text = recipe != null && !string.IsNullOrEmpty(recipe.description)
                ? recipe.description
                : (recipe != null && recipe.output != null ? recipe.output.description : "");

        if (processTimeText != null)
            processTimeText.text = recipe != null ? $"Waktu: {recipe.processTime:F0} detik" : "";

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
            if (row != null) Destroy(row);
        spawnedIngredientRows.Clear();

        if (ingredientContainer == null || ingredientRowPrefab == null || recipe == null) return;

        var ingredients = recipe.GetAllIngredients();
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null) continue;

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

        // Tampilkan baris kebutuhan air bila resep memerlukan air
        if (recipe.waterRequired > 0f)
        {
            GameObject waterRowGO = Instantiate(ingredientRowPrefab, ingredientContainer, false);
            waterRowGO.transform.localScale = Vector3.one;
            waterRowGO.SetActive(true);
            spawnedIngredientRows.Add(waterRowGO);

            var icon = waterRowGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
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

    private void UpdateCookButton()
    {
        bool isCookingNow = currentStove != null && currentStove.CurrentState == CookingState.Cooking;
        bool canCook = !isCookingNow && currentStove != null && selectedRecipe != null && playerInventory != null &&
                       currentStove.CanCook(selectedRecipe, playerInventory, out _);

        if (cookButton != null)
            cookButton.interactable = canCook;

        if (cookButtonImage != null)
            cookButtonImage.color = canCook ? canCookColor : cannotCookColor;

        if (cookButtonText != null && !isCookingNow)
            cookButtonText.text = canCook ? "Cook!" : "Missing Ingredients";
    }

    private void UpdateIngredientDisplay(KitchenRecipe recipe)
    {
        PopulateIngredientRows(recipe);
    }

    private void OnCookClicked()
    {
        if (currentStove == null || selectedRecipe == null || playerInventory == null) return;
        if (currentStove.CurrentState == CookingState.Cooking) return;

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
            if (row != null) Destroy(row);
        spawnedIngredientRows.Clear();
    }

    private void OnDestroy()
    {
        UnsubscribeFromStove(currentStove);

        if (Instance == this)
            Instance = null;

        if (cookButton != null)
            cookButton.onClick.RemoveListener(OnCookClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);
    }
}
