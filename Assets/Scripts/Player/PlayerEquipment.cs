using UnityEngine;

// Listener visual murni (Data-Driven) untuk sistem equip senjata 3D.
// Tidak memanipulasi data inventori; hanya bereaksi terhadap event yang
// dikirim oleh InventoryComponent untuk memperbarui model senjata di tangan.
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private Transform handSocket;
    
    [Header("Pengaturan Animasi Equip & Attack")]
    [Tooltip("Nama Trigger parameter di Animator Controller (misal: \"Equip\", \"DrawItem\").")]
    [SerializeField] private string equipTriggerName = "Equip";

    [Tooltip("Nama Trigger parameter di Animator Controller untuk serangan pedang (misal: \"Attack\", \"Slash\").")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("Jumlah konsumsi stamina saat melakukan serangan.")]
    [SerializeField] private float attackStaminaCost = 15f;

    [Tooltip("Jika dicentang, animasi serang tetap berjalan meskipun pemain sedang tidak memegang senjata (tangan kosong).")]
    [SerializeField] private bool allowBareHandsAttack = false;

    private GameObject currentWeaponModel;
    private InventoryComponent inventory;
    private Animator animator;
    private PlayerStats playerStats;
    private PlayerBuffManager buffManager;

    public float AttackDamageMultiplier => buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f;
    public float AttackSpeedMultiplier => buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;

    public ItemData CurrentEquippedItem
    {
        get
        {
            if (inventory == null) return null;
            int idx = inventory.selectedHotbarIndex;
            if (idx >= 0 && idx < inventory.slots.Count)
            {
                var slot = inventory.slots[idx];
                if (slot != null && !slot.IsEmpty)
                    return slot.item;
            }
            return null;
        }
    }

    public bool TryPerformAttack()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Cegah spam klik saat animasi serang sebelumnya masih aktif
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if ((state.IsName("attack") || state.IsName(attackTriggerName)) && state.normalizedTime < 0.75f)
            {
                return false;
            }
        }

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null && (playerStats.currentStamina < attackStaminaCost || playerStats.IsExhausted))
        {
            Debug.Log("[PlayerEquipment] Stamina tidak cukup untuk menyerang!");
            return false;
        }

        ItemData item = CurrentEquippedItem;

        // 1. Jika tangan kosong (tidak ada item di slot hotbar aktif)
        if (item == null)
        {
            if (!allowBareHandsAttack)
            {
                Debug.Log("[PlayerEquipment] Tidak bisa menyerang: Harus memegang senjata/pedang (slot hotbar kosong).");
                return false;
            }
        }
        // 2. Jika ada item, cek apakah item tersebut adalah senjata (isWeapon == true)
        else
        {
            bool isWeapon = (item is ToolItemData tool && tool.isWeapon);

            if (!isWeapon && !allowBareHandsAttack)
            {
                Debug.Log($"[PlayerEquipment] Item '{item.itemName}' bukan senjata (isWeapon = false), tidak bisa menyerang.");
                return false;
            }
        }

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);

            // Kurangi stamina saat serangan berhasil dilakukan
            if (playerStats != null)
            {
                playerStats.UseStamina(attackStaminaCost);
            }

            return true;
        }

        return false;
    }

    private void Awake()
    {
        inventory = GetComponent<InventoryComponent>();
        animator = GetComponentInChildren<Animator>();
        playerStats = GetComponent<PlayerStats>();
        buffManager = GetComponent<PlayerBuffManager>();
        FindHandSocketIfNeeded();
    }

    private ItemData lastEquippedItem;
    private bool isInitialized = false;

    private void Start()
    {
        // Refresh visual saat awal mulai game
        if (inventory != null)
        {
            lastEquippedItem = CurrentEquippedItem;
            UpdateEquipmentVisual(inventory.selectedHotbarIndex);
        }
        isInitialized = true;
    }

    private void OnEnable()
    {
        if (inventory == null)
            inventory = GetComponent<InventoryComponent>();

        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshCurrentEquipment;
            inventory.OnHotbarSelected += OnHotbarSlotChanged;
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshCurrentEquipment;
            inventory.OnHotbarSelected -= OnHotbarSlotChanged;
        }
    }

    /// <summary>
    /// Dipanggil saat isi inventori berubah (mengambil/menaruh item di chest, memindahkan item di tas, pickup).
    /// Memicu animasi equip jika item di tangan pemain berubah.
    /// </summary>
    public void RefreshCurrentEquipment()
    {
        if (inventory == null) return;

        ItemData currentItem = CurrentEquippedItem;
        bool itemChanged = (currentItem != lastEquippedItem);

        UpdateEquipmentVisual(inventory.selectedHotbarIndex);

        if (isInitialized && itemChanged && animator != null && !string.IsNullOrEmpty(equipTriggerName))
        {
            if (currentItem != null)
            {
                animator.SetTrigger(equipTriggerName);
            }
        }

        lastEquippedItem = currentItem;
    }

    /// <summary>
    /// Dipanggil saat pemain mengganti slot hotbar (1-4, mouse scroll).
    /// Mengganti model seketika TANPA memicu animasi equip agar tidak mengganggu gerakan jalan/lari.
    /// </summary>
    public void OnHotbarSlotChanged(int hotbarIndex)
    {
        UpdateEquipmentVisual(hotbarIndex);
        if (inventory != null)
        {
            lastEquippedItem = CurrentEquippedItem;
        }
    }

    public void UpdateEquipmentVisual(int hotbarIndex)
    {
        DestroyCurrentWeapon();

        if (inventory == null)
            return;

        if (hotbarIndex < 0 || hotbarIndex >= inventory.slots.Count)
            return;

        InventorySlot slot = inventory.slots[hotbarIndex];


        if (slot == null || slot.item == null)
            return;

        FindHandSocketIfNeeded();
        if (handSocket == null)
            return;

        // Jika item bukan Tool atau belum punya prefab 3D (equipPrefab null), lewati spawning model
        if (slot.item is not ToolItemData tool || tool.equipPrefab == null)
            return;

        GameObject spawned = Instantiate(tool.equipPrefab, handSocket);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        currentWeaponModel = spawned;
        currentWeaponModel.transform.localScale = tool.equipPrefab.transform.localScale;
    }

    private void FindHandSocketIfNeeded()
    {
        if (handSocket != null) return;

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Equals("HandSocket", System.StringComparison.OrdinalIgnoreCase))
            {
                handSocket = t;
                return;
            }
        }
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.EndsWith("RightHand", System.StringComparison.OrdinalIgnoreCase))
            {
                handSocket = t;
                return;
            }
        }
    }

    public void DestroyCurrentWeapon()
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
            currentWeaponModel = null;
        }
    }
}