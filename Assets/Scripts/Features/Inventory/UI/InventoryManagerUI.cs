using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class InventoryManagerUI : MonoBehaviour
{
    public static InventoryManagerUI Instance { get; private set; }

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

    private readonly List<InventorySlotUI> playerSlotUIs = new List<InventorySlotUI>();
    private readonly List<InventorySlotUI> storageSlotUIs = new List<InventorySlotUI>();
    private bool isPlayerOpen;

    // Inventori yang sebenarnya dirender di panel KIRI (default = Player).
    private InventoryComponent displayLeftInventory;

    // Modus khusus: interact Kabinet -> kiri = Inventory Kabinet, kanan = Inventory Rak.
    private bool isTrophyCabinetMode;
    private InventoryComponent cabinetInventory; // kiri saat modus trophy (untuk unsubscribe)

    void Awake()
    {
        Instance = this;

        // Sembunyikan semua UI paling awal + kunci kursor untuk game action.
        CloseAllUI();
    }

    void Start()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<InventoryComponent>();

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += OnInventoryChanged;
            playerInventory.OnHotbarSelected += OnHotbarSelected;
            playerTransform = playerInventory.transform;
        }

        displayLeftInventory = playerInventory;
        BuildPlayerSlots();
        BuildSlots(storageSlotsContainer, storageSlotUIs, null);
        UpdateUI();

        // Tampilkan highlight awal di hotbar slot 0.
        if (playerInventory != null)
            playerInventory.SelectHotbarSlot(0);
    }

    void Update()
    {
        // Guard: Trophy Mode = panel Kabinet<Rak modal, jangan auto-close oleh jarak
        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
            return;

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
        // Guard: jangan buka inventory jika sedang Trophy Mode
        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
            return;

        if (currentStorageInventory != null)
        {
            CloseAllUI();
            return;
        }

        isPlayerOpen = !isPlayerOpen;

        if (playerPanel != null)
            playerPanel.SetActive(isPlayerOpen);

        SetCursorFree(isPlayerOpen);
    }

    /// <summary>
    /// Membuka UI storage generik: panel Player (kiri) + storage (kanan). Biar list Player tetap tampil.
    /// </summary>
    public void OpenStorageUI(InventoryComponent storageInv)
    {
        if (storageInv == null) return;

        // Kalau sedang mode trophy, kembalikan dulu ke tampilan normal.
        if (isTrophyCabinetMode)
            CloseAllUI();

        UnsubscribeRight();
        currentStorageInventory = storageInv;
        currentStorageInventory.OnInventoryChanged += OnInventoryChanged;

        displayLeftInventory = playerInventory;
        BuildSlots(storageSlotsContainer, storageSlotUIs, storageInv);

        isPlayerOpen = true;
        if (playerPanel != null) playerPanel.SetActive(true);
        if (storagePanel != null) storagePanel.SetActive(true);

        SetCursorFree(true);
        UpdateUI();
    }

    /// <summary>
    /// Buka UI KHUSUS Trophy Cabinet:
    /// KIRI  = Inventory Kabinet (berisi Trophy, siap di-drag keluar),
    /// KANAN = Inventory Rak (4 slot utama SnapPoint).
    /// Panel Player TIDAK ikut terbuka — Trophy hanya boleh di Kabinet/Rak.
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

        // Kiri = Kabinet, kanan = Rak.
        currentStorageInventory = rackInv;
        if (currentStorageInventory != null)
            currentStorageInventory.OnInventoryChanged += OnInventoryChanged;

        cabinetInventory = cabinetInv;
        if (cabinetInventory != null)
            cabinetInventory.OnInventoryChanged += OnInventoryChanged;

        displayLeftInventory = cabinetInv;
        isTrophyCabinetMode = true;

        // Bangun ulang kedua panel sesuai data backend (data-driven murni).
        BuildSlots(playerSlotsContainer, playerSlotUIs, cabinetInv);
        BuildSlots(storageSlotsContainer, storageSlotUIs, rackInv);

        // Sembunyikan hotbar Player selama trophy mode (tidak relevan).
        if (playerHotbarContainer != null)
            playerHotbarContainer.gameObject.SetActive(false);

        isPlayerOpen = true;
        if (playerPanel != null) playerPanel.SetActive(true);
        if (storagePanel != null) storagePanel.SetActive(true);

        SetCursorFree(true);
        UpdateUI();
    }

    /// <summary>
    /// Menutup semua panel dan mengunci kursor kembali. Saat berasal dari mode Trophy
    /// Cabinet, panel KIRI di-set kembali ke inventory Player + hotbar dimunculkan.
    /// </summary>
    public void CloseAllUI()
    {
        isPlayerOpen = false;

        if (playerPanel != null) playerPanel.SetActive(false);
        if (storagePanel != null) storagePanel.SetActive(false);

        UnsubscribeRight();
        UnsubscribeCabinet();

        // Pulihkan tampilan kiri ke Player bila tershop dari mode Trophy Cabinet.
        if (isTrophyCabinetMode)
        {
            isTrophyCabinetMode = false;

            if (playerHotbarContainer != null)
                playerHotbarContainer.gameObject.SetActive(true);

            BuildPlayerSlots();
            displayLeftInventory = playerInventory;
        }

        SetCursorFree(false);
    }

    private void SetCursorFree(bool free)
    {
        Cursor.visible = free;
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
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
            slot.BoundSlot = i < playerInventory.slots.Count ? playerInventory.slots[i] : new InventorySlot();
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
            InventorySlotUI slot = Instantiate(slotPrefab, container);
            slot.Init(this, i, inventory);
            slot.BoundSlot = i < inventory.slots.Count ? inventory.slots[i] : new InventorySlot();
            list.Add(slot);
        }
    }

    public void SwapSlots(InventoryComponent owner, int sourceIndex, int destinationIndex)
    {
        if (owner == null) return;
        owner.SwapSlots(sourceIndex, destinationIndex);
    }

    public void UpdateUI()
    {
        RefreshPanel(playerSlotUIs, displayLeftInventory);
        RefreshPanel(storageSlotUIs, currentStorageInventory);
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