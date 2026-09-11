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

    [Tooltip("Jika dicentang, animasi serang tetap berjalan meskipun pemain sedang tidak memegang senjata (tangan kosong).")]
    [SerializeField] private bool allowBareHandsAttack = false;

    private GameObject currentWeaponModel;
    private InventoryComponent inventory;
    private Animator animator;

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

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
            return true;
        }

        return false;
    }

    private void Awake()
    {
        inventory = GetComponent<InventoryComponent>();
        animator = GetComponentInChildren<Animator>();
        FindHandSocketIfNeeded();
    }

    private void Start()
    {
        // Refresh visual saat awal mulai game
        RefreshCurrentEquipment();
    }

    private void OnEnable()
    {
        if (inventory == null)
            inventory = GetComponent<InventoryComponent>();

        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshCurrentEquipment;
            inventory.OnHotbarSelected += UpdateEquipmentVisual;
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshCurrentEquipment;
            inventory.OnHotbarSelected -= UpdateEquipmentVisual;
        }
    }

    public void RefreshCurrentEquipment()
    {
        if (inventory != null)
            UpdateEquipmentVisual(inventory.selectedHotbarIndex);
    }

    public void UpdateEquipmentVisual(int hotbarIndex)
    {
        DestroyCurrentWeapon();

        if (inventory == null)
            return;

        if (hotbarIndex < 0 || hotbarIndex >= inventory.slots.Count)
            return;

        InventorySlot slot = inventory.slots[hotbarIndex];

        // Panggil animasi equip pada Animator selama hotbar dipilih (atau slot terisi)
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrEmpty(equipTriggerName))
        {
            animator.SetTrigger(equipTriggerName);
        }

        if (slot == null || slot.item == null)
            return;

        FindHandSocketIfNeeded();
        if (handSocket == null)
            return;

        // Jika item bukan Tool atau belum punya prefab 3D (equipPrefab null), lewati spawning model
        if (slot.item is not ToolItemData tool || tool.equipPrefab == null)
            return;

        GameObject spawned = Instantiate(slot.item.equipPrefab, handSocket);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        currentWeaponModel = spawned;
        currentWeaponModel.transform.localScale = slot.item.equipPrefab.transform.localScale;
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