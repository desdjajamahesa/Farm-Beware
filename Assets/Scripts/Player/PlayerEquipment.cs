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
    [SerializeField] private bool allowBareHandsAttack = true;

    private GameObject currentWeaponModel;
    private InventoryComponent inventory;
    private Animator animator;
    private PlayerStats playerStats;
    private PlayerBuffManager buffManager;
    private Coroutine currentSwingCoroutine;

    public float AttackDamageMultiplier => buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f;
    public float AttackSpeedMultiplier => buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;

    public ItemData CurrentEquippedItem
    {
        get
        {
            if (inventory == null) inventory = GetComponent<InventoryComponent>();
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
            if ((state.IsName("attack") || state.IsName(attackTriggerName)) && state.normalizedTime < 0.70f)
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

        // Validasi senjata atau bare hands
        if (item == null)
        {
            if (!allowBareHandsAttack)
            {
                Debug.Log("[PlayerEquipment] Tidak bisa menyerang: Tangan kosong.");
                return false;
            }
        }
        else
        {
            bool isWeapon = (item is ToolItemData tool && tool.isWeapon);
            if (!isWeapon && !allowBareHandsAttack)
            {
                Debug.Log($"[PlayerEquipment] Item '{item.itemName}' bukan senjata, tidak bisa menyerang.");
                return false;
            }
        }

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            float atkSpdMultiplier = buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
            var upgradeState = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance ?? GetComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>();
            if (upgradeState != null && upgradeState.sweetPotatoPathUnlocked)
            {
                atkSpdMultiplier *= 1.20f;
            }

            animator.speed = atkSpdMultiplier;
            animator.SetTrigger(attackTriggerName);

            // Kurangi stamina saat serangan berhasil dilakukan
            if (playerStats != null)
            {
                playerStats.UseStamina(attackStaminaCost);
            }

            int baseDmg;
            float knockback;

            if (item is ToolItemData toolData)
            {
                baseDmg = (upgradeState != null) ? upgradeState.baseDamage : toolData.baseDamage;
                knockback = (upgradeState != null) ? upgradeState.baseKnockback : toolData.knockbackForce;
            }
            else
            {
                // Tangan kosong atau item umum: damage dasar
                baseDmg = 8;
                knockback = 3.5f;
            }

            float dmgMultiplier = buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f;
            int finalDamage = Mathf.RoundToInt(baseDmg * dmgMultiplier);

            if (currentSwingCoroutine != null)
            {
                StopCoroutine(currentSwingCoroutine);
                currentSwingCoroutine = null;
            }

            currentSwingCoroutine = StartCoroutine(RoutineSwingHitbox(finalDamage, knockback, atkSpdMultiplier));

            return true;
        }

        return false;
    }

    private System.Collections.IEnumerator RoutineSwingHitbox(int damage, float knockback, float speedMultiplier)
    {
        float delay = 0.12f / Mathf.Max(0.5f, speedMultiplier);
        float duration = 0.28f / Mathf.Max(0.5f, speedMultiplier);

        yield return new WaitForSeconds(delay);

        FeaturesCombat.WeaponHitbox hitbox = null;
        if (currentWeaponModel != null)
        {
            hitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
            if (hitbox == null)
            {
                var col = currentWeaponModel.GetComponent<Collider>() ?? currentWeaponModel.AddComponent<BoxCollider>();
                col.isTrigger = true;
                hitbox = currentWeaponModel.AddComponent<FeaturesCombat.WeaponHitbox>();
            }
        }

        if (hitbox != null)
        {
            hitbox.Activate(gameObject, damage, knockback, transform.forward);
        }
        else
        {
            // Fallback bare-hands / no-model melee sweep
            PerformDirectMeleeSweep(damage, knockback);
        }

        yield return new WaitForSeconds(duration);

        if (hitbox != null)
        {
            hitbox.Deactivate();
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }

        currentSwingCoroutine = null;
    }

    /// <summary>
    /// Sapuan langsung tanpa model senjata fisik (mis. saat bertarung tangan kosong).
    /// </summary>
    private void PerformDirectMeleeSweep(int damage, float knockback)
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 forwardDir = transform.forward;
        Vector3 sweepCenter = origin + forwardDir * 1.0f;
        Collider[] hits = Physics.OverlapSphere(sweepCenter, 1.5f, ~0, QueryTriggerInteraction.Collide);

        var hitSet = new System.Collections.Generic.HashSet<FeaturesCombat.IDamageable>();
        foreach (var c in hits)
        {
            if (c == null || c.gameObject == gameObject || c.transform.IsChildOf(transform)) continue;

            Vector3 toTarget = c.transform.position - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.04f && Vector3.Dot(forwardDir, toTarget.normalized) < 0.2f)
                continue;

            var target = c.GetComponent<FeaturesCombat.IDamageable>() ?? c.GetComponentInParent<FeaturesCombat.IDamageable>();
            if (target != null && !target.IsDead && hitSet.Add(target))
            {
                Vector3 hitPoint = c.ClosestPoint(sweepCenter);
                target.TakeDamage(damage, hitPoint, forwardDir);

                Rigidbody targetRb = c.GetComponent<Rigidbody>() ?? c.GetComponentInParent<Rigidbody>();
                if (targetRb != null && !targetRb.isKinematic)
                {
                    targetRb.AddForce((forwardDir + Vector3.up * 0.25f).normalized * knockback, ForceMode.Impulse);
                }

                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(hitPoint + Vector3.up * 0.8f, $"-{damage}", new Color(1f, 0.25f, 0.2f));
                }
            }
        }
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
            inventory = GetComponent<InventoryComponent>();

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

        // Pastikan model senjata yang di-spawn memiliki WeaponHitbox, trigger collider, dan kinematic Rigidbody
        var hitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
        if (hitbox == null)
        {
            var col = currentWeaponModel.GetComponent<Collider>() ?? currentWeaponModel.AddComponent<BoxCollider>();
            col.isTrigger = true;
            hitbox = currentWeaponModel.AddComponent<FeaturesCombat.WeaponHitbox>();
        }

        var rb = currentWeaponModel.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentWeaponModel.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
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
            var hitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
            if (hitbox != null)
            {
                hitbox.Deactivate();
            }

            if (Application.isPlaying)
            {
                Destroy(currentWeaponModel);
            }
            else
            {
                DestroyImmediate(currentWeaponModel);
            }
            currentWeaponModel = null;
        }
    }
}