using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using System.Linq;
using FarmBeware.Core.Runtime;

public class InventoryManagerUI : MonoBehaviour, IModalWindow
{
    bool IModalWindow.IsOpen => IsAnyInventoryUIRelatedOpen();

    private static InventoryManagerUI _instance;
    public static InventoryManagerUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<InventoryManagerUI>();
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Data Inventori")]
    public InventoryComponent playerInventory;

    // Tampilan KANAN (panel storage). Di mode Trophy Cabinet, ini adalah Inventory Rak.
    [FormerlySerializedAs("currentChestInventory")]
    public InventoryComponent currentStorageInventory;

    [Header("Auto-Close Storage")]
    // Diperbesar agar panel storage tetap terbuka saat pemain berdiri di depan
    // Kabinet (jarak player -> Kabinet kini ~6.6 unit).
    public float maxInteractDistance = 7f;

    private Transform playerTransform;

    [Header("Referensi UI")]
    public InventorySlotUI slotPrefab;
    public RectTransform playerSlotsContainer;

    [FormerlySerializedAs("chestSlotsContainer")]
    public RectTransform storageSlotsContainer;
    public GameObject playerPanel;

    [FormerlySerializedAs("chestPanel")]
    public GameObject storagePanel;
    public Transform playerHotbarContainer;
    public RectTransform playerViewport;

    [Header("Modular Storage Panels")]
    public GameObject refrigeratorPanel;
    public GameObject trophyPanel;

    [Header("Modular Storage Slot Containers")]
    [SerializeField] private RectTransform refrigeratorSlotsContainer;
    [SerializeField] private RectTransform trophySlotsContainer;
    [Header("Item Details Toggle")]
    public GameObject itemDetailsContainer;

    [Header("Character Sheet (Tab 3-Column UI)")]
    public GameObject characterSheetRoot;
    public GameObject playerStatsPanel;
    public GameObject equipmentPanel;
    public Vector2 defaultPlayerPanelPos = new Vector2(120f, 0f);
    public Vector2 characterSheetPlayerPanelPos = new Vector2(505f, 0f);

    [Header("Panel Titles")]
    public Text leftPanelTitle;
    public Text rightPanelTitle;

    [Header("Item Details (Player Panel)")]
    public Image detailItemIcon;
    public Text detailItemName;
    public Text detailItemDesc;

    private readonly List<InventorySlotUI> playerSlotUIs = new List<InventorySlotUI>();
    private readonly List<InventorySlotUI> storageSlotUIs = new List<InventorySlotUI>();
    private readonly List<InventorySlotUI> refrigeratorSlotUIs = new List<InventorySlotUI>();
    private readonly List<InventorySlotUI> trophySlotUIs = new List<InventorySlotUI>();
    private bool isPlayerOpen;

    // Inventori yang sebenarnya dirender di panel KIRI (default = Player).
    private InventoryComponent displayLeftInventory;

    // Modus khusus: interact Kabinet -> kiri = Inventory Kabinet, kanan = Inventory Rak.
    private bool isTrophyCabinetMode;
    private InventoryComponent cabinetInventory; // kiri saat modus trophy (untuk unsubscribe)
    private int frameInventoryOpened = -1;

    void Awake()
    {
        Instance = this;

        if (playerInventory == null)
        {
            var player = ServiceLocator.Resolve<IPlayerContext>();
            if (player != null)
                playerInventory = player.GetPlayerComponent<InventoryComponent>();

            if (playerInventory == null)
                playerInventory = GetComponent<InventoryComponent>();
        }

        displayLeftInventory = playerInventory;

        if (playerPanel != null)
        {
            var rt = playerPanel.GetComponent<RectTransform>();
            if (rt != null) defaultPlayerPanelPos = rt.anchoredPosition;
        }

        // Sembunyikan semua UI paling awal + kunci kursor untuk game action.
        CloseAllUI();
    }

    /// <summary>
    /// Recursively finds a child Transform by name in the hierarchy.
    /// Unity's Transform.Find only searches direct children, so this provides recursive search.
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        
        // Check direct children first
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
            
            // Recursively search deeper
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    void Start()
    {
        if (playerInventory == null)
        {
            var player = ServiceLocator.Resolve<IPlayerContext>();
            if (player != null)
                playerInventory = player.GetPlayerComponent<InventoryComponent>();

            if (playerInventory == null)
                playerInventory = GetComponent<InventoryComponent>();
        }

        if (playerInventory != null)
        {
            playerInventory.HasHotbar = true;
            playerInventory.OnInventoryChanged += OnInventoryChanged;
            playerInventory.OnHotbarSelected += OnHotbarSelected;
            playerTransform = playerInventory.transform;

            // Ensure player inventory slots list is allocated without wiping existing items
            playerInventory.EnsureSlotsAllocated();
        }

        // Ensure hotbar is visible at start
        if (playerHotbarContainer != null)
            playerHotbarContainer.gameObject.SetActive(true);

        displayLeftInventory = playerInventory;
        BuildPlayerSlots();
        BuildSlots(storageSlotsContainer, storageSlotUIs, null);
        UpdateUI();

        // Tampilkan highlight awal di hotbar slot 0.
        if (playerInventory != null)
            playerInventory.SelectHotbarSlot(0);
    }

    /// <summary>
    /// Returns true if any inventory, storage, refrigerator, trophy, or kitchen sink panel is open.
    /// </summary>
    public bool IsAnyInventoryUIRelatedOpen()
    {
        return isPlayerOpen ||
               (playerPanel != null && playerPanel.activeSelf) ||
               (storagePanel != null && storagePanel.activeSelf) ||
               (refrigeratorPanel != null && refrigeratorPanel.activeSelf) ||
               (trophyPanel != null && trophyPanel.activeSelf) ||
               isTrophyCabinetMode ||
               IsKitchenSinkOpen();
    }

    public static System.Func<bool> IsSinkOpenCheck;
    public static System.Func<int, bool> SinkTransferInputHandler;

    public bool IsKitchenSinkOpen()
    {
        return IsSinkOpenCheck != null && IsSinkOpenCheck();
    }

    void Update()
    {
        // Auto-close storage ketika pemain menjauh (anchor = storage / rak).
        Transform anchor = currentStorageInventory != null ? currentStorageInventory.transform : null;
        if (storagePanel != null && storagePanel.activeSelf && anchor != null)
        {
            if (playerTransform == null)
                return;

            float distance = Vector3.Distance(playerTransform.position, anchor.position);
            if (distance > maxInteractDistance)
                CloseAllUI();
        }

        // Auto-close trophy cabinet jika jauh dari anchor
        if (isTrophyCabinetMode && currentStorageInventory != null)
        {
            if (playerTransform == null) return;
            float distance = Vector3.Distance(playerTransform.position, currentStorageInventory.transform.position);
            if (distance > maxInteractDistance)
                CloseAllUI();
        }

        // ESC key closes any open inventory/storage/trophy UI
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsAnyInventoryUIRelatedOpen())
            {
                UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
                CloseAllUI();
            }
        }

        // TAB / I key closes player inventory if open
        if (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame))
        {
            if (Time.frameCount != frameInventoryOpened && (isPlayerOpen || IsAnyInventoryUIRelatedOpen()))
            {
                if (currentStorageInventory != null || isTrophyCabinetMode)
                {
                    CloseAllUI();
                }
                else
                {
                    TogglePlayerInventory();
                }
            }
        }
    }

    void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
            playerInventory.OnHotbarSelected -= OnHotbarSelected;
        }
        UnsubscribeRight();
        UnsubscribeCabinet();

        if (Instance == this)
            Instance = null;
    }

    private void OnHotbarSelected(int index)
    {
        // Saat Trophy Cabinet Mode, hotbar Player tidak relevan (dan tersembunyi).
        if (isTrophyCabinetMode)
            return;

        int count = Mathf.Min(playerSlotUIs.Count, 4);
        for (int i = 0; i < count; i++)
            playerSlotUIs[i].SetHighlight(i == index);

        if (playerInventory != null && index >= 0 && index < playerInventory.slots.Count)
        {
            InventorySlot slot = playerInventory.slots[index];
            if (slot != null && !slot.IsEmpty && ItemDisplayUI.Instance != null)
                ItemDisplayUI.Instance.ShowHotbarPopup(slot.item.itemName);
        }
    }

    private void OnInventoryChanged()
    {
        UpdateUI();
    }

    // Hanya membuka/menutup panel pemain. Jika storage/Kabinet sedang terbuka, tutup semua.
    public void TogglePlayerInventory()
    {
        // Guard: jangan buka inventory jika sedang mode Trophy atau Wardrobe
        var camService = ServiceLocator.Resolve<ICameraService>();
        if (camService != null && (camService.CurrentMode == CameraMode.TrophyMode || camService.CurrentMode == CameraMode.WardrobeMode))
            return;

        if (currentStorageInventory != null)
        {
            CloseAllUI();
            return;
        }

        isPlayerOpen = !isPlayerOpen;
        if (isPlayerOpen)
        {
            frameInventoryOpened = Time.frameCount;
            ModalStackManager.Instance?.Push(this);
        }
        else
        {
            ModalStackManager.Instance?.PopSpecific(this);
        }

        if (playerPanel != null)
        {
            playerPanel.SetActive(isPlayerOpen);

            var rt = playerPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (isPlayerOpen && (playerStatsPanel != null || characterSheetRoot != null))
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = characterSheetPlayerPanelPos;
                }
                else
                {
                    rt.anchorMin = new Vector2(0.0f, 0.5f);
                    rt.anchorMax = new Vector2(0.0f, 0.5f);
                    rt.pivot = new Vector2(0.0f, 0.5f);
                    rt.anchoredPosition = defaultPlayerPanelPos;
                }
            }
        }

        if (characterSheetRoot != null) characterSheetRoot.SetActive(isPlayerOpen);
        if (playerStatsPanel != null) playerStatsPanel.SetActive(isPlayerOpen);
        if (equipmentPanel != null) equipmentPanel.SetActive(isPlayerOpen);


        if (isPlayerOpen && playerStatsPanel != null)
        {
            playerStatsPanel.SendMessage("UpdateAllStats", SendMessageOptions.DontRequireReceiver);
        }

        // Ensure hotbar is visible when player inventory is open
        if (isPlayerOpen && playerHotbarContainer != null)
            playerHotbarContainer.gameObject.SetActive(true);

        // Show/hide item details container
        if (itemDetailsContainer != null)
            itemDetailsContainer.SetActive(isPlayerOpen);

        // Adjust viewport for item details area
        if (playerViewport != null)
        {
            if (isPlayerOpen)
                playerViewport.offsetMin = new Vector2(playerViewport.offsetMin.x, 205);
            else
                playerViewport.offsetMin = new Vector2(playerViewport.offsetMin.x, 0);
        }

        // Refresh player slots data when opening
        if (isPlayerOpen)
        {
            BuildPlayerSlots();
            UpdateUI();
        }

        // Set title when opening player-only inventory
        if (isPlayerOpen && leftPanelTitle != null)
            leftPanelTitle.text = "Inventory";

        var player = ServiceLocator.Resolve<IPlayerContext>();
        if (player != null)
            player.IsInputLocked = isPlayerOpen;

        SetCursorFree(isPlayerOpen);
    }

    /// <summary>
    /// Membuka UI storage generik: panel Player (kiri) + storage (kanan). Biar list Player tetap tampil.
    /// </summary>
    public void OpenStorageUI(InventoryComponent storageInv, string storageTitle = "Storage", GameObject customPanel = null)
    {
        if (storageInv == null) return;

        // Kalau sedang mode trophy, kembalikan dulu ke tampilan normal.
        if (isTrophyCabinetMode)
            CloseAllUI();

        // Mode Storage: Sembunyikan Stats dan Equipment, kembalikan posisi Player Panel ke kiri
        if (characterSheetRoot != null) characterSheetRoot.SetActive(false);
        if (playerStatsPanel != null) playerStatsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);


        if (playerPanel != null)
        {
            var rt = playerPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.0f, 0.5f);
                rt.anchorMax = new Vector2(0.0f, 0.5f);
                rt.pivot = new Vector2(0.0f, 0.5f);
                rt.anchoredPosition = defaultPlayerPanelPos;
            }
        }

        UnsubscribeRight();
        currentStorageInventory = storageInv;
        currentStorageInventory.OnInventoryChanged += OnInventoryChanged;

        displayLeftInventory = playerInventory;

        GameObject activeStoragePanel;
        RectTransform activeSlotsContainer;
        List<InventorySlotUI> activeSlotList = storageSlotUIs; // Default

if (customPanel != null)
        {
            activeStoragePanel = customPanel;
            RectTransform resolvedContainer = null;

            if (customPanel == refrigeratorPanel && refrigeratorSlotsContainer != null)
                resolvedContainer = refrigeratorSlotsContainer;
            else if (customPanel == trophyPanel && trophySlotsContainer != null)
                resolvedContainer = trophySlotsContainer;
            else
            {
                var found = FindDeepChild(customPanel.transform, "INV_StorageSlotsContainer");
                resolvedContainer = found?.GetComponent<RectTransform>();
            }

            activeSlotsContainer = resolvedContainer;

            if (customPanel == refrigeratorPanel)
                activeSlotList = refrigeratorSlotUIs;
            else if (customPanel == trophyPanel)
                activeSlotList = trophySlotUIs;
            else
                activeSlotList = storageSlotUIs;

            if (storagePanel != null) storagePanel.SetActive(false);
            if (refrigeratorPanel != null && refrigeratorPanel != customPanel) refrigeratorPanel.SetActive(false);
            if (trophyPanel != null && trophyPanel != customPanel) trophyPanel.SetActive(false);
        }
        else
        {
            // Use default panel
            activeStoragePanel = storagePanel;
            activeSlotsContainer = storageSlotsContainer;
            activeSlotList = storageSlotUIs;
            
            if (refrigeratorPanel != null) refrigeratorPanel.SetActive(false);
            if (trophyPanel != null) trophyPanel.SetActive(false);
        }

        if (activeSlotsContainer != null)
            {
                BuildSlots(activeSlotsContainer, activeSlotList, storageInv);
            }

        isPlayerOpen = true;
        if (playerPanel != null) playerPanel.SetActive(true);
        if (activeStoragePanel != null) activeStoragePanel.SetActive(true);

        if (activeSlotsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(activeSlotsContainer);
                if (activeSlotsContainer.rect.width < 1f)
                {
                    GridLayoutGroup glg = activeSlotsContainer.GetComponent<GridLayoutGroup>();
                    int cols = glg != null && glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? glg.constraintCount : 4;
                    float cellW = glg != null ? glg.cellSize.x : 100f;
                    float spacingX = glg != null ? glg.spacing.x : 12f;
                    float padL = glg != null ? glg.padding.left : 0;
                    float padR = glg != null ? glg.padding.right : 0;
                    int rows = Mathf.CeilToInt((float)activeSlotList.Count / cols);
                    float cellH = glg != null ? glg.cellSize.y : 100f;
                    float spacingY = glg != null ? glg.spacing.y : 12f;
                    float padT = glg != null ? glg.padding.top : 0;
                    float padB = glg != null ? glg.padding.bottom : 0;
                    float w = cols * cellW + (cols - 1) * spacingX + padL + padR;
                    float h = rows * cellH + (rows - 1) * spacingY + padT + padB;
                    activeSlotsContainer.sizeDelta = new Vector2(w, h);
                    Debug.LogWarning($"[OpenStorageUI] Container width was ~0 after rebuild, forced sizeDelta=({w},{h}) for {activeSlotList.Count} slots");
                }
            }

        if (playerHotbarContainer != null)
            playerHotbarContainer.gameObject.SetActive(true);

        if (itemDetailsContainer != null)
            itemDetailsContainer.SetActive(true);

        if (playerViewport != null)
            playerViewport.offsetMin = new Vector2(playerViewport.offsetMin.x, 205);

        // Set panel titles dynamically - find HeaderTitle in active panels
        if (leftPanelTitle != null) leftPanelTitle.text = "Inventory";
        
        GameObject activeRightPanel = customPanel != null ? customPanel : storagePanel;
        var rightTitleText = activeRightPanel?.transform.Find("HeaderTitle")?.GetComponent<Text>();
        if (rightTitleText != null) rightTitleText.text = storageTitle;

        var pc = ServiceLocator.Resolve<IPlayerContext>();
        if (pc != null) pc.IsInputLocked = true;

        SetCursorFree(true);
        ModalStackManager.Instance?.Push(this);
        UpdateUI();
    }

    /// <summary>
    /// Buka UI KHUSUS Trophy Cabinet:
    /// KIRI  = Inventory Kabinet (berisi Trophy, siap di-drag keluar),
    /// KANAN = Inventory Rak (4 slot utama SnapPoint).
    /// MENGGUNAKAN PANEL TROPHY SPESIFIK (trophyPanel).
    /// </summary>
    public void OpenTrophyCabinetUI(InventoryComponent cabinetInv, InventoryComponent rackInv)
    {
        if (cabinetInv == null)
        {
            Debug.LogWarning("OpenTrophyCabinetUI: cabinetInv null.");
            return;
        }

        // Bersihkan state tampilan sebelumnya (storage lama / player-open).
        if (isTrophyCabinetMode)
            CloseAllUI();

        UnsubscribeRight();
        UnsubscribeCabinet();

        // Kabinet only — Rak placed via 3D SnapPoints, no UI panel.
        if (rackInv != null)
        {
            currentStorageInventory = rackInv;
            currentStorageInventory.OnInventoryChanged += OnInventoryChanged;
        }

        cabinetInventory = cabinetInv;
        if (cabinetInventory != null)
            cabinetInventory.OnInventoryChanged += OnInventoryChanged;

        displayLeftInventory = cabinetInv;
        isTrophyCabinetMode = true;

        // Build cabinet panel on STANDARD player panel (left).
        BuildSlots(playerSlotsContainer, playerSlotUIs, cabinetInv);

        // Build rack panel on TROPHY panel (right).
        GameObject rackPanel = trophyPanel != null ? trophyPanel : storagePanel;
        RectTransform rackContainer = null;

        if (trophySlotsContainer != null)
            rackContainer = trophySlotsContainer;
        else if (rackPanel != null)
        {
            var found = FindDeepChild(rackPanel.transform, "INV_StorageSlotsContainer");
            rackContainer = found?.GetComponent<RectTransform>();
        }

        List<InventorySlotUI> rackList = trophyPanel != null ? new List<InventorySlotUI>() : storageSlotUIs;

        if (rackInv != null && rackContainer != null)
            BuildSlots(rackContainer, rackList, rackInv);

        // Sembunyikan hotbar Player selama trophy mode.
        if (playerHotbarContainer != null)
            playerHotbarContainer.gameObject.SetActive(false);

        isPlayerOpen = true;

        // Mode Trophy Cabinet: Sembunyikan Stats dan Equipment, kembalikan posisi Player Panel ke KIRI
        if (characterSheetRoot != null) characterSheetRoot.SetActive(false);
        if (playerStatsPanel != null) playerStatsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);

        if (playerPanel != null)
        {
            var rt = playerPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.0f, 0.5f);
                rt.anchorMax = new Vector2(0.0f, 0.5f);
                rt.pivot = new Vector2(0.0f, 0.5f);
                rt.anchoredPosition = defaultPlayerPanelPos;
            }
        }

        // Activate panels - LEFT panel only for Trophy mode
        if (playerPanel != null) playerPanel.SetActive(true);
        
        // TROPHY MODE: Right panel HIDDEN (uses 3D Snap Points for drag-drop)
        if (trophyPanel != null) trophyPanel.SetActive(false);
        if (storagePanel != null) storagePanel.SetActive(false);
        if (refrigeratorPanel != null) refrigeratorPanel.SetActive(false);

        // HIDE item details container for Trophy mode (no detail area)
        if (itemDetailsContainer != null)
            itemDetailsContainer.SetActive(false);

        // Expand viewport to full height (no detail area for Trophy)
        if (playerViewport != null)
            playerViewport.offsetMin = new Vector2(playerViewport.offsetMin.x, 0);

        // Set panel titles for Trophy Cabinet mode in English
        if (leftPanelTitle != null) leftPanelTitle.text = "Trophy Cabinet";
        
        var rightTitleText = trophyPanel?.transform.Find("HeaderTitle")?.GetComponent<Text>();
        if (rightTitleText != null) rightTitleText.text = "Trophy Rack";

        var pcCabinet = ServiceLocator.Resolve<IPlayerContext>();
        if (pcCabinet != null) pcCabinet.IsInputLocked = true;

        SetCursorFree(true);
        ModalStackManager.Instance?.Push(this);
        UpdateUI();
    }

    /// <summary>
    /// Menutup semua panel dan mengunci kursor kembali. Saat berasal dari mode Trophy
    /// Cabinet, panel KIRI di-set kembali ke inventory Player + hotbar dimunculkan.
    /// </summary>
    public void CloseAllUI()
    {
        isPlayerOpen = false;
        ModalStackManager.Instance?.PopSpecific(this);

        // Close all panels
        if (playerPanel != null) playerPanel.SetActive(false);
        if (storagePanel != null) storagePanel.SetActive(false);
        if (refrigeratorPanel != null) refrigeratorPanel.SetActive(false);
        if (trophyPanel != null) trophyPanel.SetActive(false);
        if (characterSheetRoot != null) characterSheetRoot.SetActive(false);
        if (playerStatsPanel != null) playerStatsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);


        // ALWAYS ensure hotbar is visible when closing all UI
        if (playerHotbarContainer != null)
            playerHotbarContainer.gameObject.SetActive(true);

        UnsubscribeRight();
        UnsubscribeCabinet();

        // Pulihkan tampilan kiri ke Player bila tershop dari mode Trophy Cabinet.
        if (isTrophyCabinetMode)
        {
            isTrophyCabinetMode = false;

            if (playerHotbarContainer != null)
                playerHotbarContainer.gameObject.SetActive(true);

            displayLeftInventory = playerInventory;
            BuildPlayerSlots();
            UpdateUI();
        }

        // Restore player input (unlock movement/interaction)
        var pcClose = ServiceLocator.Resolve<IPlayerContext>();
        if (pcClose != null)
        {
            pcClose.IsInputLocked = false;
        }

        // Restore time scale in case it was paused
        Time.timeScale = 1f;

        SetCursorFree(false);
    }

    void IModalWindow.OpenModal()
    {
        if (!IsAnyInventoryUIRelatedOpen())
            TogglePlayerInventory();
    }

    void IModalWindow.CloseModal() => CloseAllUI();

    /// <summary>
    /// Update the item detail panel (bottom of player inventory).
    /// </summary>
    public void UpdateItemDetails(ItemData item)
    {
        if (item == null)
        {
            if (detailItemIcon != null)
            {
                detailItemIcon.sprite = null;
                detailItemIcon.enabled = false;
            }
            if (detailItemName != null) detailItemName.text = "";
            if (detailItemDesc != null) detailItemDesc.text = "";
            return;
        }

        if (detailItemIcon != null)
        {
            detailItemIcon.sprite = item.itemIcon;
            detailItemIcon.enabled = item.itemIcon != null;
        }
        if (detailItemName != null) detailItemName.text = item.itemName;
        if (detailItemDesc != null)
        {
            string desc = item.description ?? "";
            if (item is FoodItemData food)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(desc);
                List<string> nutrition = new List<string>();
                if (food.hungerRestore != 0) nutrition.Add($"<b>Hunger:</b> {(food.hungerRestore > 0 ? "+" : "")}{food.hungerRestore:F0}");
                if (food.hydrationRestore != 0) nutrition.Add($"<b>Hydration:</b> {(food.hydrationRestore > 0 ? "+" : "")}{food.hydrationRestore:F0}");
                if (food.healAmount != 0) nutrition.Add($"<b>HP:</b> {(food.healAmount > 0 ? "+" : "")}{food.healAmount}");

                if (nutrition.Count > 0)
                {
                    sb.AppendLine();
                    sb.Append($"<color=#38BDF8>{string.Join("  |  ", nutrition)}</color>");
                }

                if (food.buffEffects != null && food.buffEffects.Count > 0)
                {
                    foreach (var buff in food.buffEffects)
                    {
                        string buffDesc = !string.IsNullOrEmpty(buff.description) ? buff.description : buff.buffName;
                        sb.AppendLine();
                        sb.Append($"<color=#FBBF24>✦ {buffDesc} ({buff.duration}s)</color>");
                    }
                }
                detailItemDesc.text = sb.ToString();
            }
            else
            {
                detailItemDesc.text = desc;
            }
        }
    }

    private void SetCursorFree(bool free)
    {
        // Keep cursor always visible and unlocked (user preference)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        _ = free;
    }

    private void BuildPlayerSlots()
    {
        ClearContainer(playerHotbarContainer);
        ClearContainer(playerSlotsContainer);
        playerSlotUIs.Clear();

        if (playerInventory == null) return;

        int count = Mathf.Max(0, playerInventory.maxCapacity);
        for (int i = 0; i < count; i++)
        {
            // 4 slot pertama (Hotbar) tampil di HUD; sisanya di panel inventory utama.
            Transform parent = i < 4 ? playerHotbarContainer : (Transform)playerSlotsContainer;
            if (parent == null) continue;

            InventorySlotUI slot = Instantiate(slotPrefab, parent);
            slot.Init(this, i, playerInventory);
            InventorySlot slotData = i < playerInventory.slots.Count ? playerInventory.slots[i] : new InventorySlot();
            slot.BoundSlot = slotData;
            slot.SetSlotVisual(slotData);
            playerSlotUIs.Add(slot);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            DestroyImmediate(container.GetChild(i).gameObject);
    }

    private void BuildSlots(RectTransform container, List<InventorySlotUI> list, InventoryComponent inventory)
    {
        if (container == null) return;

        // Bersihkan SELURUH anak container (termasuk sisa scaffold lama) agar tak ada duplikat.
        for (int i = container.childCount - 1; i >= 0; i--)
            DestroyImmediate(container.GetChild(i).gameObject);
        list.Clear();

        if (inventory == null) return;

        int count = Mathf.Max(0, inventory.maxCapacity);
        for (int i = 0; i < count; i++)
        {
            InventorySlotUI slot = Instantiate(slotPrefab, container, false);
            slot.transform.localScale = Vector3.one;
            slot.transform.SetAsLastSibling();
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect != null)
                slotRect.anchoredPosition3D = Vector3.zero;
            slot.Init(this, i, inventory);
            InventorySlot slotData = i < inventory.slots.Count ? inventory.slots[i] : new InventorySlot();
            slot.BoundSlot = slotData;
            slot.SetSlotVisual(slotData);
            list.Add(slot);
        }

        Debug.Log($"[BuildSlots] container={container.name}, count={list.Count}, containerSize={container.rect}, containerActive={container.gameObject.activeInHierarchy}");
    }

    public void SwapSlots(InventoryComponent owner, int sourceIndex, int destinationIndex)
    {
        if (owner == null) return;
        owner.SwapSlots(sourceIndex, destinationIndex);
    }

    /// <summary>
    /// Single-click quick transfer: moves the entire stack from the clicked slot
    /// to the "other" inventory. If clicked from storage → player, or player → storage.
    /// Returns true if transfer happened.
    /// </summary>
    public bool TryQuickTransfer(InventoryComponent slotOwner, int slotIndex)
    {
        if (slotOwner == null || playerInventory == null)
            return false;

        // Determine which inventory to transfer TO.
        InventoryComponent targetInventory = null;

        if (slotOwner == playerInventory)
        {
            // Jika UI Kitchen Sink sedang terbuka, transfer langsung ke slot input wastafel
            if (IsKitchenSinkOpen() && SinkTransferInputHandler != null)
            {
                return SinkTransferInputHandler(slotIndex);
            }

            // Clicked a player slot → transfer to storage (if open).
            targetInventory = currentStorageInventory;

            // Trophy cabinet mode: player panel shows cabinet inventory, not player.
            // In that mode, left panel owner == cabinetInventory, not playerInventory.
            // So this branch means we truly clicked Player inventory → send to storage.
        }
        else if (slotOwner == currentStorageInventory)
        {
            // Clicked a storage slot → transfer to player.
            targetInventory = playerInventory;

            // In trophy cabinet mode, left is cabinetInventory. If rack is storage,
            // clicking rack slot sends to cabinetInventory (displayed on left).
            if (isTrophyCabinetMode && cabinetInventory != null)
                targetInventory = cabinetInventory;
        }
        else if (isTrophyCabinetMode && slotOwner == cabinetInventory)
        {
            // Clicked cabinet (left panel) → transfer to rack (storage, right panel).
            targetInventory = currentStorageInventory;
        }

        if (targetInventory == null)
            return false;

        // Validate the slot has an item.
        if (slotIndex < 0 || slotIndex >= slotOwner.slots.Count)
            return false;

        InventorySlot slot = slotOwner.slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
            return false;

        // Use TransferItemTo (index overload) which moves the entire stack.
        slotOwner.TransferItemTo(targetInventory, slotIndex);
        return true;
    }

    public void UpdateUI()
    {
        if (displayLeftInventory == null && playerInventory != null)
            displayLeftInventory = playerInventory;

        RefreshPanel(playerSlotUIs, displayLeftInventory);
        RefreshPanel(storageSlotUIs, currentStorageInventory);
        RefreshPanel(refrigeratorSlotUIs, currentStorageInventory);
        RefreshPanel(trophySlotUIs, currentStorageInventory);
    }

    private void RefreshPanel(List<InventorySlotUI> list, InventoryComponent inventory)
    {
        if (inventory == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            InventorySlot data = i < inventory.slots.Count ? inventory.slots[i] : null;
            list[i].SetSlotVisual(data);
        }
    }

    private void UnsubscribeRight()
    {
        if (currentStorageInventory != null)
        {
            currentStorageInventory.OnInventoryChanged -= OnInventoryChanged;
            currentStorageInventory = null;
        }
    }

    private void UnsubscribeCabinet()
    {
        if (cabinetInventory != null)
        {
            cabinetInventory.OnInventoryChanged -= OnInventoryChanged;
            cabinetInventory = null;
        }
    }
}