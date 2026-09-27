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

    [Header("Pengaturan Kombo Serangan (Mixamo)")]
    [Tooltip("Waktu maksimal (detik) antar klik untuk melanjutkan ke pukulan kombo berikutnya sebelum reset.")]
    [SerializeField] private float comboResetWindow = 0.9f;

    [Tooltip("Pengali damage untuk masing-masing pukulan kombo: Hit 1, Hit 2, Hit 3 (Finisher).")]
    [SerializeField] private float[] comboDamageMultipliers = new float[] { 1.0f, 1.2f, 1.5f };

    [Tooltip("Biaya stamina per pukulan kombo: Hit 1, Hit 2, Hit 3.")]
    [SerializeField] private float[] comboStaminaCosts = new float[] { 10f, 10f, 15f };

    [Header("Pengaturan Skill Tendangan (Spartan Kick)")]
    [Tooltip("Konsumsi stamina saat melancarkan tendangan (Kick).")]
    public float kickStaminaCost = 15f;
    [Tooltip("Damage dari tendangan (Kick).")]
    public int kickDamage = 22;
    [Tooltip("Kekuatan dorongan knockback tendangan menjauhkan musuh.")]
    public float kickKnockback = 10.5f;
    [Tooltip("Jangkauan jarak tendangan (meter).")]
    public float kickHitRange = 2.0f;
    [Tooltip("Waktu cooldown skill tendangan (detik).")]
    public float kickCooldown = 1.8f;
    private float lastKickTime = -10f;

    [Header("Pengaturan Jurus Spesial (Leap Strike)")]
    [Tooltip("Nama Trigger parameter di Animator untuk jurus spesial.")]
    [SerializeField] private string skillTriggerName = "SkillAttack";

    [Tooltip("Biaya stamina untuk melancarkan jurus spesial.")]
    [SerializeField] private float skillStaminaCost = 25f;

    [Tooltip("Pengali damage untuk jurus spesial (relatif terhadap base damage).")]
    [SerializeField] private float skillDamageMultiplier = 2.0f;

    [Tooltip("Jangkauan/radius hantaman jurus spesial.")]
    [SerializeField] private float skillHitRange = 2.5f;

    [Tooltip("Kekuatan dorongan knockback jurus spesial.")]
    [SerializeField] private float skillKnockback = 8.0f;

    private int currentComboIndex = 0;
    private float lastAttackTime = -999f;
    private bool isExecutingSkill = false;

    public int CurrentComboIndex => currentComboIndex;
    public bool IsExecutingSkill => isExecutingSkill;
    public bool IsHoldingWeapon => currentWeaponModel != null;

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

    public bool IsAttackState(AnimatorStateInfo state)
    {
        return state.IsName("attack") ||
               state.IsName("Attack_Combo1") ||
               state.IsName("Attack_Combo2") ||
               state.IsName("Attack_Combo3") ||
               state.IsName("Attack_Skill");
    }

    public bool TryPerformAttack()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (isExecutingSkill) return false;

        // Cegah spam klik jika animasi serang saat ini baru dimulai (izinkan combo window di tengah/akhir animasi)
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (IsAttackState(state) && state.normalizedTime < 0.35f)
            {
                return false;
            }
        }

        // Tentukan combo index berikutnya
        float timeSinceLast = Time.time - lastAttackTime;
        if (timeSinceLast > comboResetWindow || currentComboIndex >= 2)
        {
            currentComboIndex = 0;
        }
        else
        {
            currentComboIndex++;
        }

        float staminaCost = (comboStaminaCosts != null && currentComboIndex < comboStaminaCosts.Length)
            ? comboStaminaCosts[currentComboIndex]
            : attackStaminaCost;

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null && (playerStats.currentStamina < staminaCost || playerStats.IsExhausted))
        {
            Debug.Log("[PlayerEquipment] Stamina tidak cukup untuk menyerang!");
            currentComboIndex = 0;
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

            atkSpdMultiplier *= attackAnimationSpeed;

            animator.speed = atkSpdMultiplier;
            animator.SetInteger("ComboIndex", currentComboIndex);
            animator.SetTrigger(attackTriggerName);

            lastAttackTime = Time.time;

            if (playerStats != null)
            {
                playerStats.UseStamina(staminaCost);
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
                baseDmg = 8;
                knockback = 3.5f;
            }

            float comboMul = (comboDamageMultipliers != null && currentComboIndex < comboDamageMultipliers.Length)
                ? comboDamageMultipliers[currentComboIndex]
                : 1f;

            float dmgMultiplier = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * comboMul;
            int finalDamage = Mathf.RoundToInt(baseDmg * dmgMultiplier);

            // Combo ke-3 (Finisher) adalah putaran 360 derajat
            bool is360 = (currentComboIndex == 2);
            float activeRange = is360 ? attackHitRange * 1.15f : attackHitRange;
            float finalKnockback = is360 ? knockback * 1.4f : knockback;

            if (currentSwingCoroutine != null)
            {
                StopCoroutine(currentSwingCoroutine);
                currentSwingCoroutine = null;
            }

            currentSwingCoroutine = StartCoroutine(RoutineSwingHitbox(finalDamage, finalKnockback, atkSpdMultiplier, is360, activeRange));

            return true;
        }

        return false;
    }

    public bool TryPerformKick()
    {
        if (Time.time - lastKickTime < kickCooldown)
            return false;

        if (playerStats != null && playerStats.currentStamina < kickStaminaCost)
            return false;

        if (playerStats != null)
        {
            playerStats.UseStamina(kickStaminaCost);
        }

        lastKickTime = Time.time;

        if (animator != null)
        {
            animator.ResetTrigger("Kick");
            animator.SetTrigger("Kick");
        }

        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
        }

        currentSwingCoroutine = StartCoroutine(RoutineKickHitbox());
        return true;
    }

    private System.Collections.IEnumerator RoutineKickHitbox()
    {
        float speed = Mathf.Max(0.5f, AttackAnimationSpeed);
        float impactDelay = 0.08f / speed;
        yield return new WaitForSeconds(impactDelay);

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 forwardDir = transform.forward;
        Vector3 sweepCenter = origin + forwardDir * (kickHitRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(sweepCenter, kickHitRange * 0.7f, ~0, QueryTriggerInteraction.Collide);

        var hitSet = new System.Collections.Generic.HashSet<FeaturesCombat.IDamageable>();
        foreach (var c in hits)
        {
            if (c == null || c.gameObject == gameObject || c.transform.IsChildOf(transform)) continue;

            Vector3 toTarget = c.transform.position - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.04f && Vector3.Dot(forwardDir, toTarget.normalized) < 0.20f)
                continue;

            var target = c.GetComponent<FeaturesCombat.IDamageable>() ?? c.GetComponentInParent<FeaturesCombat.IDamageable>();
            if (target != null && !target.IsDead && hitSet.Add(target))
            {
                Vector3 hitPoint = c.ClosestPoint(sweepCenter);
                target.TakeDamage(kickDamage, hitPoint, forwardDir);

                Rigidbody targetRb = c.GetComponent<Rigidbody>() ?? c.GetComponentInParent<Rigidbody>();
                if (targetRb != null && !targetRb.isKinematic)
                {
                    Vector3 kbDir = (forwardDir + Vector3.up * 0.2f).normalized;
                    targetRb.AddForce(kbDir * kickKnockback, ForceMode.Impulse);
                }

                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnEnemyDamage(
                        hitPoint + Vector3.up * 0.8f,
                        kickDamage,
                        isCrit: true,
                        isSkill: false);
                }
            }
        }

        yield return new WaitForSeconds(0.32f / speed);
        currentSwingCoroutine = null;
    }

    public bool TryPerformSkillAttack()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (isExecutingSkill) return false;

        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (IsAttackState(state) && state.normalizedTime < 0.40f)
            {
                return false;
            }
        }

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null && (playerStats.currentStamina < skillStaminaCost || playerStats.IsExhausted))
        {
            Debug.Log("[PlayerEquipment] Stamina tidak cukup untuk jurus spesial!");
            return false;
        }

        ItemData item = CurrentEquippedItem;
        if (item == null)
        {
            if (!allowBareHandsAttack)
            {
                Debug.Log("[PlayerEquipment] Tidak bisa melancarkan jurus: Tangan kosong.");
                return false;
            }
        }
        else
        {
            bool isWeapon = (item is ToolItemData tool && tool.isWeapon);
            if (!isWeapon && !allowBareHandsAttack)
            {
                Debug.Log($"[PlayerEquipment] Item '{item.itemName}' bukan senjata, tidak bisa melancarkan jurus.");
                return false;
            }
        }

        if (animator != null && !string.IsNullOrEmpty(skillTriggerName))
        {
            float atkSpdMultiplier = buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
            var upgradeState = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance ?? GetComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>();
            if (upgradeState != null && upgradeState.sweetPotatoPathUnlocked)
            {
                atkSpdMultiplier *= 1.20f;
            }

            atkSpdMultiplier *= attackAnimationSpeed;

            animator.speed = atkSpdMultiplier;
            animator.SetTrigger(skillTriggerName);

            lastAttackTime = Time.time;
            currentComboIndex = 0; // Reset combo setelah skill

            if (playerStats != null)
            {
                playerStats.UseStamina(skillStaminaCost);
            }

            int baseDmg;
            if (item is ToolItemData toolData)
            {
                baseDmg = (upgradeState != null) ? upgradeState.baseDamage : toolData.baseDamage;
            }
            else
            {
                baseDmg = 8;
            }

            float dmgMultiplier = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * skillDamageMultiplier;
            int finalDamage = Mathf.RoundToInt(baseDmg * dmgMultiplier);

            if (currentSwingCoroutine != null)
            {
                StopCoroutine(currentSwingCoroutine);
                currentSwingCoroutine = null;
            }

            currentSwingCoroutine = StartCoroutine(RoutineSkillHitbox(finalDamage, skillKnockback, atkSpdMultiplier, skillHitRange));

            return true;
        }

        return false;
    }

    private System.Collections.IEnumerator RoutineSwingHitbox(int damage, float knockback, float speedMultiplier, bool is360 = false, float rangeOverride = -1f)
    {
        float delay = 0.12f / Mathf.Max(0.5f, speedMultiplier);
        float duration = 0.28f / Mathf.Max(0.5f, speedMultiplier);

        TriggerRangeIndicatorPulse(delay + duration + 0.1f);

        yield return new WaitForSeconds(delay);

        float activeRange = rangeOverride > 0f ? rangeOverride : attackHitRange;
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

        if (hitbox != null && !is360)
        {
            hitbox.ConfigureHitRange(activeRange);
            hitbox.Activate(gameObject, damage, knockback, transform.forward);
        }
        else
        {
            // Untuk putaran 360 atau tanpa hitbox fisik, gunakan direct sweep
            PerformDirectMeleeSweep(damage, knockback, is360, activeRange);
        }

        yield return new WaitForSeconds(duration);

        if (hitbox != null)
        {
            hitbox.Deactivate();
        }

        float maxWait = 0.5f / Mathf.Max(0.5f, speedMultiplier);
        float elapsed = 0f;
        while (elapsed < maxWait && animator != null)
        {
            elapsed += Time.deltaTime;
            var curr = animator.GetCurrentAnimatorStateInfo(0);
            var next = animator.GetNextAnimatorStateInfo(0);
            if (!IsAttackState(curr) && !IsAttackState(next))
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

    private System.Collections.IEnumerator RoutineSkillHitbox(int damage, float knockback, float speedMultiplier, float range)
    {
        isExecutingSkill = true;

        // Waktu tunggu lompatan di udara sebelum mendarat dan menghantam tanah (pada t=1.14s animasi slam)
        float impactDelay = 1.14f / Mathf.Max(0.5f, speedMultiplier);
        float impactDuration = 0.25f / Mathf.Max(0.5f, speedMultiplier);

        TriggerRangeIndicatorPulse(impactDelay + impactDuration + 0.15f);

        yield return new WaitForSeconds(impactDelay);

        // Hantaman mendarat: sapuan AoE 360 derajat di sekitar titik hantaman
        PerformDirectMeleeSweep(damage, knockback, is360: true, rangeOverride: range);

        yield return new WaitForSeconds(impactDuration);

        float maxWait = 0.6f / Mathf.Max(0.5f, speedMultiplier);
        float elapsed = 0f;
        while (elapsed < maxWait && animator != null)
        {
            elapsed += Time.deltaTime;
            var curr = animator.GetCurrentAnimatorStateInfo(0);
            var next = animator.GetNextAnimatorStateInfo(0);
            if (!IsAttackState(curr) && !IsAttackState(next))
            {
                break;
            }
            yield return null;
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }

        isExecutingSkill = false;
        currentSwingCoroutine = null;
    }

    /// <summary>
    /// Sapuan melee langsung (mendukung serangan cone forward ataupun putaran 360 derajat).
    /// </summary>
    private void PerformDirectMeleeSweep(int damage, float knockback, bool is360 = false, float rangeOverride = -1f)
    {
        float activeRange = rangeOverride > 0f ? rangeOverride : attackHitRange;
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 forwardDir = transform.forward;
        Vector3 sweepCenter = is360 ? origin : origin + forwardDir * (activeRange * 0.5f);
        float sweepRadius = is360 ? activeRange : (activeRange * 0.65f);
        Collider[] hits = Physics.OverlapSphere(sweepCenter, sweepRadius, ~0, QueryTriggerInteraction.Collide);

        var hitSet = new System.Collections.Generic.HashSet<FeaturesCombat.IDamageable>();
        foreach (var c in hits)
        {
            if (c == null || c.gameObject == gameObject || c.transform.IsChildOf(transform)) continue;

            if (!is360)
            {
                Vector3 toTarget = c.transform.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.04f && Vector3.Dot(forwardDir, toTarget.normalized) < 0.2f)
                    continue;
            }

            var target = c.GetComponent<FeaturesCombat.IDamageable>() ?? c.GetComponentInParent<FeaturesCombat.IDamageable>();
            if (target != null && !target.IsDead && hitSet.Add(target))
            {
                Vector3 hitPoint = c.ClosestPoint(sweepCenter);
                Vector3 hitDir = is360 ? (c.transform.position - origin).normalized : forwardDir;
                if (hitDir.sqrMagnitude < 0.01f) hitDir = forwardDir;

                target.TakeDamage(damage, hitPoint, hitDir);

                Rigidbody targetRb = c.GetComponent<Rigidbody>() ?? c.GetComponentInParent<Rigidbody>();
                if (targetRb != null && !targetRb.isKinematic)
                {
                    targetRb.AddForce((hitDir + Vector3.up * 0.35f).normalized * knockback, ForceMode.Impulse);
                }

                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    bool isFinisher = is360 && !isExecutingSkill;
                    bool isSkill = isExecutingSkill;
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnEnemyDamage(
                        hitPoint + Vector3.up * 0.8f,
                        damage,
                        isCrit: isFinisher,
                        isSkill: isSkill);
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
