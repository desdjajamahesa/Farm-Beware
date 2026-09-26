using UnityEngine;

// Listener visual murni (Data-Driven) untuk sistem equip senjata 3D.
// Tidak memanipulasi data inventori; hanya bereaksi terhadap event yang
// dikirim oleh InventoryComponent untuk memperbarui model senjata di tangan.
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private Transform handSocket;
    
    [Header("Pengaturan Animasi Equip & Attack")]
    [Tooltip("Nama Trigger parameter di Animator Controller (misal: \"Equip\", \"DrawItem\").")]
#pragma warning disable CS0414
    [SerializeField] private string equipTriggerName = "";
#pragma warning restore CS0414

    [Tooltip("Nama Trigger parameter di Animator Controller untuk serangan pedang (misal: \"Attack\", \"Slash\").")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("Jumlah konsumsi stamina saat melakukan serangan.")]
    [SerializeField] private float attackStaminaCost = 15f;

    [Tooltip("Jika dicentang, animasi serang tetap berjalan meskipun pemain sedang tidak memegang senjata (tangan kosong).")]
    [SerializeField] private bool allowBareHandsAttack = true;

    [Header("Pengaturan Animasi Serangan")]
    [Tooltip("Kecepatan pemutaran animasi serangan di Animator (1 = normal, 1.6 = 60% lebih cepat). Dapat diatur bebas di Inspector.")]
    [Range(0.5f, 3.5f)]
    [SerializeField] private float attackAnimationSpeed = 1.6f;

    public float AttackAnimationSpeed => attackAnimationSpeed;

    public enum RangeIndicatorVisibility
    {
        WhenHoldingWeapon,
        Always,
        OnlyDuringAttack,
        Disabled
    }

    [Header("Pengaturan Hit Range & Indikator Lingkaran")]
    [Tooltip("Radius jarak jangkauan hit serangan (meter).")]
    [Range(0.5f, 5.0f)]
    [SerializeField] private float attackHitRange = 1.8f;

    [Tooltip("Kapan lingkaran indikator hit range di tanah ditampilkan.")]
    [SerializeField] private RangeIndicatorVisibility rangeIndicatorMode = RangeIndicatorVisibility.WhenHoldingWeapon;

    [Tooltip("Warna garis lingkaran saat standby/siap menyerang.")]
    [SerializeField] private Color rangeIndicatorColor = new Color(0.2f, 0.8f, 1f, 0.45f);

    [Tooltip("Warna garis lingkaran saat ayunan serangan aktif (pulse / hentakan).")]
    [SerializeField] private Color attackPulseColor = new Color(1f, 0.4f, 0.15f, 0.9f);

    [Tooltip("Ketebalan garis lingkaran indikator di tanah.")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float indicatorLineWidth = 0.05f;

    [Tooltip("Offset ketinggian lingkaran dari tanah (mencegah z-fighting).")]
    [SerializeField] private float groundYOffset = 0.04f;

    [Tooltip("Tampilkan gizmo lingkaran di Scene view saat objek dipilih.")]
    [SerializeField] private bool showGizmos = true;

    private GameObject currentWeaponModel;
    private InventoryComponent inventory;
    private Animator animator;
    private PlayerStats playerStats;
    private PlayerBuffManager buffManager;
    private Coroutine currentSwingCoroutine;

    // Komponen visual indikator jangkauan serangan (lingkaran di bawah kaki karakter)
    private GameObject rangeIndicatorObj;
    private LineRenderer rangeLineRenderer;
    private Material indicatorMaterial;
    private Coroutine pulseCoroutine;
    private bool isCurrentlyPulsing = false;

    public float AttackDamageMultiplier => buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f;
    public float AttackSpeedMultiplier => buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
    public float AttackHitRange => attackHitRange;

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

            // Gabungkan dengan kecepatan animasi serangan dari Inspector
            atkSpdMultiplier *= attackAnimationSpeed;

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

        // Memicu efek pulse/highlight pada indikator lingkaran di bawah
        TriggerRangeIndicatorPulse(delay + duration + 0.1f);

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
            hitbox.ConfigureHitRange(attackHitRange);
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

        // Tunggu sisa transisi animasi attack selesai sebelum mereset animator.speed kembali ke 1.0f
        float maxWait = 0.5f / Mathf.Max(0.5f, speedMultiplier);
        float elapsed = 0f;
        while (elapsed < maxWait && animator != null)
        {
            elapsed += Time.deltaTime;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("attack") && !animator.GetNextAnimatorStateInfo(0).IsName("attack"))
            {
                break;
            }
            yield return null;
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
        Vector3 sweepCenter = origin + forwardDir * (attackHitRange * 0.5f);
        float sweepRadius = attackHitRange * 0.65f;
        Collider[] hits = Physics.OverlapSphere(sweepCenter, sweepRadius, ~0, QueryTriggerInteraction.Collide);

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
        DestroyCurrentWeapon();
        EnsureRangeIndicator();
    }

    private ItemData lastEquippedItem;
#pragma warning disable CS0414
    private bool isInitialized = false;
#pragma warning restore CS0414

    private void Start()
    {
        // Refresh visual saat awal mulai game
        if (inventory != null)
        {
            lastEquippedItem = CurrentEquippedItem;
            UpdateEquipmentVisual(inventory.selectedHotbarIndex);
        }
        isInitialized = true;
        UpdateRangeIndicatorState();
    }

    private void Update()
    {
        UpdateRangeIndicatorState();
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
        UpdateRangeIndicatorState();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshCurrentEquipment;
            inventory.OnHotbarSelected -= OnHotbarSlotChanged;
        }
        if (rangeIndicatorObj != null)
        {
            rangeIndicatorObj.SetActive(false);
        }
    }

    /// <summary>
    /// Inisialisasi dan pembaruan komponen LineRenderer untuk lingkaran jangkauan di tanah.
    /// </summary>
    private void EnsureRangeIndicator()
    {
        if (rangeIndicatorObj == null)
        {
            Transform existing = transform.Find("AttackRangeIndicator");
            if (existing != null)
            {
                rangeIndicatorObj = existing.gameObject;
            }
            else
            {
                rangeIndicatorObj = new GameObject("AttackRangeIndicator");
                rangeIndicatorObj.transform.SetParent(transform, false);
                rangeIndicatorObj.transform.localPosition = new Vector3(0f, groundYOffset, 0f);
                rangeIndicatorObj.transform.localRotation = Quaternion.identity;
            }
        }

        if (rangeLineRenderer == null && rangeIndicatorObj != null)
        {
            rangeLineRenderer = rangeIndicatorObj.GetComponent<LineRenderer>();
            if (rangeLineRenderer == null)
            {
                rangeLineRenderer = rangeIndicatorObj.AddComponent<LineRenderer>();
            }

            rangeLineRenderer.useWorldSpace = false;
            rangeLineRenderer.loop = true;
            rangeLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rangeLineRenderer.receiveShadows = false;
            rangeLineRenderer.alignment = LineAlignment.View;

            if (indicatorMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") 
                             ?? Shader.Find("Universal Render Pipeline/Unlit") 
                             ?? Shader.Find("Hidden/Internal-Colored");
                if (shader != null)
                {
                    indicatorMaterial = new Material(shader);
                }
            }
            if (indicatorMaterial != null)
            {
                rangeLineRenderer.material = indicatorMaterial;
            }
        }

        UpdateCircleGeometry();
        UpdateIndicatorVisual(isCurrentlyPulsing ? attackPulseColor : rangeIndicatorColor);
    }

    public void UpdateCircleGeometry()
    {
        if (rangeLineRenderer == null) return;
        if (rangeIndicatorObj != null)
        {
            rangeIndicatorObj.transform.localPosition = new Vector3(0f, groundYOffset, 0f);
        }

        int segments = 48;
        rangeLineRenderer.positionCount = segments;
        rangeLineRenderer.startWidth = indicatorLineWidth;
        rangeLineRenderer.endWidth = indicatorLineWidth;

        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float rad = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Sin(rad) * attackHitRange;
            float z = Mathf.Cos(rad) * attackHitRange;
            rangeLineRenderer.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    private void UpdateIndicatorVisual(Color color)
    {
        if (rangeLineRenderer != null)
        {
            rangeLineRenderer.startColor = color;
            rangeLineRenderer.endColor = color;
        }
    }

    private void UpdateRangeIndicatorState()
    {
        EnsureRangeIndicator();
        if (rangeIndicatorObj == null) return;

        bool shouldShow = false;
        switch (rangeIndicatorMode)
        {
            case RangeIndicatorVisibility.Always:
                shouldShow = true;
                break;
            case RangeIndicatorVisibility.WhenHoldingWeapon:
                shouldShow = CanPerformAttackWithCurrentItem();
                break;
            case RangeIndicatorVisibility.OnlyDuringAttack:
                shouldShow = isCurrentlyPulsing;
                break;
            case RangeIndicatorVisibility.Disabled:
                shouldShow = false;
                break;
        }

        if (rangeIndicatorObj.activeSelf != shouldShow)
        {
            rangeIndicatorObj.SetActive(shouldShow);
        }
    }

    public bool CanPerformAttackWithCurrentItem()
    {
        ItemData item = CurrentEquippedItem;
        if (item == null) return allowBareHandsAttack;
        return (item is ToolItemData tool && tool.isWeapon) || allowBareHandsAttack;
    }

    private void TriggerRangeIndicatorPulse(float duration)
    {
        if (rangeIndicatorMode == RangeIndicatorVisibility.Disabled) return;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(RoutineRangeIndicatorPulse(duration));
    }

    private System.Collections.IEnumerator RoutineRangeIndicatorPulse(float duration)
    {
        isCurrentlyPulsing = true;
        EnsureRangeIndicator();

        if (rangeIndicatorObj != null)
        {
            rangeIndicatorObj.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color currentColor = Color.Lerp(attackPulseColor, rangeIndicatorColor, t);
            UpdateIndicatorVisual(currentColor);
            yield return null;
        }

        isCurrentlyPulsing = false;
        UpdateIndicatorVisual(rangeIndicatorColor);
        UpdateRangeIndicatorState();
        pulseCoroutine = null;
    }

    private void OnValidate()
    {
        if (rangeLineRenderer != null)
        {
            UpdateCircleGeometry();
            UpdateIndicatorVisual(isCurrentlyPulsing ? attackPulseColor : rangeIndicatorColor);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Vector3 center = transform.position + Vector3.up * groundYOffset;
        int segments = 40;
        Vector3 prev = center + new Vector3(attackHitRange, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * (i * (360f / segments));
            Vector3 next = center + new Vector3(Mathf.Cos(rad) * attackHitRange, 0f, Mathf.Sin(rad) * attackHitRange);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Gambar arah hadap serangan (forward ray)
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
        Vector3 forward = transform.forward * attackHitRange;
        Gizmos.DrawRay(center, forward);
    }

    /// <summary>
    /// Dipanggil saat isi inventori berubah (mengambil/menaruh item di chest, memindahkan item di tas, pickup).
    /// </summary>
    public void RefreshCurrentEquipment()
    {
        if (inventory == null) return;

        ItemData currentItem = CurrentEquippedItem;
        bool itemChanged = (currentItem != lastEquippedItem);

        UpdateEquipmentVisual(inventory.selectedHotbarIndex);

        // Animasi equip/pegang barang dinonaktifkan

        lastEquippedItem = currentItem;
        UpdateRangeIndicatorState();
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
        UpdateRangeIndicatorState();
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

        if (hitbox != null)
        {
            hitbox.ConfigureHitRange(attackHitRange);
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

        // Safeguard mutlak: Pastikan SELURUH child di handSocket dibersihkan agar tidak ada
        // model senjata stray (seperti prefab yang tersimpan di scene) yang tertinggal atau menumpuk dobel.
        FindHandSocketIfNeeded();
        if (handSocket != null)
        {
            for (int i = handSocket.childCount - 1; i >= 0; i--)
            {
                var child = handSocket.GetChild(i).gameObject;
                var hitbox = child.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
                if (hitbox != null)
                {
                    hitbox.Deactivate();
                }

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}
