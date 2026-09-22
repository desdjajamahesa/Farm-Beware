using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// UI Manager untuk Genshin-style cooking panel.
/// Menampilkan daftar resep di kiri, detail + bahan + tombol "Masak" di kanan.
/// Masak instan: klik = langsung konsumsi bahan + tambah hasil ke inventory pemain.
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

    [Header("Cook Button")]
    [SerializeField] private Button cookButton;
    [SerializeField] private TextMeshProUGUI cookButtonText;
    [SerializeField] private Image cookButtonImage;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Colors")]
    [SerializeField] private Color canCookColor = new Color(0.2f, 0.7f, 0.3f, 1f);
    [SerializeField] private Color cannotCookColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color haveEnoughColor = new Color(0.3f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color notEnoughColor = new Color(0.9f, 0.3f, 0.3f, 1f);

    private List<KitchenRecipe> allRecipes;
    private InventoryComponent playerInventory;
    private KitchenRecipe selectedRecipe;
    private bool isCooking = false;
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

    /// <summary>Buka panel stove dengan resep dan inventory pemain.</summary>
    public void Open(KitchenRecipe[] recipes, InventoryComponent playerInv)
    {
        allRecipes = new List<KitchenRecipe>(recipes);
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
        if (isCooking)
        {
            StopAllCoroutines();
            isCooking = false;
        }

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
        if (isCooking) return;
        selectedRecipe = recipe;

        if (emptyStatePlaceholder != null)
            emptyStatePlaceholder.SetActive(false);

        // Update detail panel
        if (resultIcon != null)
        {
            if (recipe.output != null && recipe.output.itemIcon != null)
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
            resultName.text = recipe.output != null ? recipe.output.itemName : recipe.name;

        if (resultDescription != null)
            resultDescription.text = !string.IsNullOrEmpty(recipe.description)
                ? recipe.description
                : (recipe.output != null ? recipe.output.description : "");

        if (processTimeText != null)
            processTimeText.text = $"Waktu: {recipe.processTime:F0} detik";

        // Populate ingredients
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

        if (ingredientContainer == null || ingredientRowPrefab == null) return;

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
        bool canCook = CanCook();

        if (cookButton != null)
            cookButton.interactable = isCooking ? false : canCook;

        if (cookButtonImage != null)
            cookButtonImage.color = canCook ? canCookColor : cannotCookColor;

        if (cookButtonText != null && !isCooking)
            cookButtonText.text = canCook ? "Cook!" : "Missing Ingredients";
    }

    private void UpdateIngredientDisplay(KitchenRecipe recipe)
    {
        PopulateIngredientRows(recipe);
    }

    private bool CanCook()
    {
        if (selectedRecipe == null || playerInventory == null) return false;

        // Validasi ketersediaan air di botol
        if (selectedRecipe.waterRequired > 0f)
        {
            var bottle = FeaturesKitchen.PlayerWaterBottle.Instance;
            if (bottle == null || !bottle.HasWater(selectedRecipe.waterRequired))
                return false;
        }

        var ingredients = selectedRecipe.GetAllIngredients();
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null) continue;

            // Jika item adalah bottle_water dan resep menggunakan sistem waterRequired, abaikan pengecekan slot biasa
            if (ingredient.item.itemId == "food_bottle_water" && selectedRecipe.waterRequired > 0f)
                continue;

            if (playerInventory.CountItem(ingredient.item) < ingredient.quantity)
                return false;
        }
        return true;
    }

    private void OnCookClicked()
    {
        if (isCooking || !CanCook() || selectedRecipe == null || playerInventory == null) return;
        StartCoroutine(CookProcessRoutine(selectedRecipe));
    }

    private System.Collections.IEnumerator CookProcessRoutine(KitchenRecipe recipe)
    {
        isCooking = true;

        // 1. Konsumsi bahan dan air di awal
        if (recipe.waterRequired > 0f && FeaturesKitchen.PlayerWaterBottle.Instance != null)
        {
            FeaturesKitchen.PlayerWaterBottle.Instance.ConsumeWater(recipe.waterRequired);
        }

        var ingredients = recipe.GetAllIngredients();
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item != null)
            {
                if (ingredient.item.itemId == "food_bottle_water" && recipe.waterRequired > 0f)
                    continue;

                playerInventory.RemoveItem(ingredient.item, ingredient.quantity);
            }
        }
        UpdateIngredientDisplay(recipe);

        // 2. Countdown Timer berdasarkan recipe.processTime
        float duration = recipe.processTime > 0 ? recipe.processTime : 1f;
        float elapsed = 0f;

        if (cookButton != null) cookButton.interactable = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Ceil(duration - elapsed);
            if (cookButtonText != null) cookButtonText.text = $"Cooking... ({remaining}s)";
            yield return null;
        }

        // 3. Tambahkan hasil ke inventory
        playerInventory.AddItem(recipe.output, recipe.outputCount);

        // 4. Selesai
        isCooking = false;
        if (cookButton != null) cookButton.interactable = true;
        SelectRecipe(recipe);
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
        if (Instance == this)
            Instance = null;

        if (cookButton != null)
            cookButton.onClick.RemoveListener(OnCookClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);
    }
}
