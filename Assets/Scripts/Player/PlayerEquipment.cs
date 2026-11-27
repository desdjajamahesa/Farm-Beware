using UnityEngine;

// Pure data-driven visual listener for the 3D weapon equip system.
// Does not mutate inventory data; reacts to InventoryComponent events
// to update the held weapon model in the player's hand.
public partial class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private Transform handSocket;
    
    [Header("Pengaturan Animasi Equip & Attack")]
    [Tooltip("Nama Trigger parameter di Animator Controller untuk serangan pedang (misal: \"Attack\", \"Slash\").")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("Jumlah konsumsi stamina saat melakukan serangan.")]
    [SerializeField] private float attackStaminaCost = 15f;

    [Tooltip("Jika dicentang, animasi serang tetap berjalan meskipun pemain sedang tidak memegang senjata (tangan kosong).")]
    [SerializeField] private bool allowBareHandsAttack = true;

    [Header("Pengaturan Animasi Serangan")]
    [Tooltip("Kecepatan pemutaran animasi serangan di Animator (1 = normal baseline). Nilai 1.0 memberikan ritme serangan yang terbaca, natural, dan terkalibrasi.")]
    [Range(0.5f, 3.5f)]
    [SerializeField] private float attackAnimationSpeed = 1.0f;

    public float AttackAnimationSpeed => attackAnimationSpeed;

    [Header("Pengaturan Kombo Serangan (Mixamo)")]
    [Tooltip("Waktu maksimal (detik) antar klik untuk melanjutkan ke pukulan kombo berikutnya sebelum reset.")]
    [SerializeField] private float comboResetWindow = 1.8f;
    public float ComboResetWindow
    {
        get => comboResetWindow;
        set
        {
            comboResetWindow = value;
            if (combatStateMachine != null) combatStateMachine.ComboResetWindow = value;
        }
    }

    [Tooltip("Pengali damage untuk masing-masing pukulan kombo: Hit 1, Hit 2, Hit 3 (Finisher).")]
    [SerializeField] private float[] comboDamageMultipliers = new float[] { 1.0f, 1.2f, 1.5f };

    [Tooltip("Biaya stamina per pukulan kombo: Hit 1, Hit 2, Hit 3.")]
    [SerializeField] private float[] comboStaminaCosts = new float[] { 10f, 10f, 15f };

    [Header("Pengaturan Timing Ayunan & Trail Kombo")]
    [Tooltip("Delay windup (detik) untuk ayunan Hit 1 (attack1, 0.30s) pada base speed.")]
    [SerializeField] private float combo1SwingDelay = 0.14f;
    [Tooltip("Durasi aktif ayunan (detik) untuk Hit 1.")]
    [SerializeField] private float combo1SwingDuration = 0.16f;

    [Tooltip("Delay windup (detik) untuk ayunan Hit 2 (attack3, 0.70s) pada base speed.")]
    [SerializeField] private float combo2SwingDelay = 0.24f;
    [Tooltip("Durasi aktif ayunan (detik) untuk Hit 2.")]
    [SerializeField] private float combo2SwingDuration = 0.26f;

    [Tooltip("Delay windup (detik) untuk putaran Hit 3 (attack4, 1.00s) pada base speed.")]
    [SerializeField] private float combo3SwingDelay = 0.18f;
    [Tooltip("Durasi aktif putaran (detik) untuk Hit 3.")]
    [SerializeField] private float combo3SwingDuration = 0.55f;

    [Tooltip("Delay windup (detik) untuk ayunan Hit 4 (4-Hit Finisher) pada base speed.")]
    [SerializeField] private float combo4SwingDelay = 0.22f;
    [Tooltip("Durasi aktif ayunan (detik) untuk Hit 4.")]
    [SerializeField] private float combo4SwingDuration = 0.45f;

    [Tooltip("Windup delay (seconds) for Heavy Attack baseline at base speed.")]
    [SerializeField] private float heavySwingDelay = 0.25f;
    [Tooltip("Active strike sweep duration (seconds) for Heavy Attack baseline.")]
    [SerializeField] private float heavySwingDuration = 0.70f;

    [Tooltip("Per-step windup delays (seconds) matching Heavy_Attack1..4 kinematic peaks.")]
    [SerializeField] private float[] heavyStepSwingDelays = new float[] { 0.28f, 0.12f, 0.42f, 0.42f };
    [Tooltip("Per-step active strike durations (seconds) matching Heavy_Attack1..4 clips.")]
    [SerializeField] private float[] heavyStepSwingDurations = new float[] { 0.30f, 0.26f, 0.28f, 0.28f };

    /// <summary>
    /// Resolves windup delay and active swing duration for the given heavy combo step (0..3),
    /// matching each heavy clip's kinematic apex.
    /// </summary>
    public void ResolveHeavyStepTiming(int heavyStepIndex, out float delay, out float duration)
    {
        delay = (heavyStepSwingDelays != null && heavyStepSwingDelays.Length > 0)
            ? heavyStepSwingDelays[Mathf.Clamp(heavyStepIndex, 0, heavyStepSwingDelays.Length - 1)]
            : heavySwingDelay;

        duration = (heavyStepSwingDurations != null && heavyStepSwingDurations.Length > 0)
            ? heavyStepSwingDurations[Mathf.Clamp(heavyStepIndex, 0, heavyStepSwingDurations.Length - 1)]
            : heavySwingDuration;
    }

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

    [Header("Pengaturan Audio Serangan Senjata")]
    [Tooltip("AudioSource khusus untuk memutar efek ayunan senjata pemain. Jika null, otomatis mengambil/membuat AudioSource di objek ini.")]
    [SerializeField] private AudioSource weaponAudioSource;
    [SerializeField] private AudioClip swingLightClip;
    [SerializeField] private AudioClip swingMediumClip;
    [SerializeField] private AudioClip swingHeavyClip;
    [SerializeField] private AudioClip swingDashClip;
    [SerializeField] private AudioClip swingSkillClip;
    [SerializeField] private AudioClip swingPunchClip;
    [Range(0f, 1f)]
    [SerializeField] private float swingAudioVolume = 0.85f;

    private int currentComboIndex = 0;
    private int currentHeavyComboIndex = 0;
    private float lastAttackTime = -999f;
    private bool isExecutingSkill = false;

    // Melee Combat FSM & Zero-GC Non-Alloc Hit Buffers
    private FeaturesCombat.Melee.MeleeCombatStateMachine combatStateMachine = new FeaturesCombat.Melee.MeleeCombatStateMachine();
    public FeaturesCombat.Melee.MeleeCombatStateMachine CombatStateMachine => combatStateMachine;

    private readonly Collider[] _hitBuffer = new Collider[64];
    private readonly FeaturesCombat.IDamageable[] _damagedTargetsBuffer = new FeaturesCombat.IDamageable[64];
    private int _damagedTargetsCount = 0;

    private bool HasTargetBeenHit(FeaturesCombat.IDamageable target)
    {
        for (int i = 0; i < _damagedTargetsCount; i++)
        {
            if (_damagedTargetsBuffer[i] == target) return true;
        }
        return false;
    }

    private void RecordTargetHit(FeaturesCombat.IDamageable target)
    {
        if (_damagedTargetsCount < _damagedTargetsBuffer.Length)
        {
            _damagedTargetsBuffer[_damagedTargetsCount++] = target;
        }
    }

    public int CurrentComboIndex => currentComboIndex;
    public int CurrentHeavyComboIndex => currentHeavyComboIndex;
    public bool IsExecutingSkill => isExecutingSkill;
    public bool IsHoldingWeapon => currentWeaponModel != null;

    [Header("Combat Occlusion / Line-of-Sight")]
    [Tooltip("LayerMask for solid obstacles (walls, fences, structures) that block melee attacks.")]
    [SerializeField] private LayerMask obstacleLayerMask;

    public LayerMask ObstacleLayerMask
    {
        get
        {
            if (obstacleLayerMask.value == 0)
            {
                obstacleLayerMask = FarmBeware.Core.Runtime.NonAllocPhysics.GetDefaultObstacleLayerMask();
            }
            return obstacleLayerMask;
        }
        set => obstacleLayerMask = value;
    }

    public void SetObstacleLayerMask(LayerMask mask) => ObstacleLayerMask = mask;

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
    private FeaturesCombat.Adapters.WeaponTrailController currentWeaponTrail;
    private FeaturesCombat.Adapters.WeaponTrailController bareHandsTrail;
    private InventoryComponent inventory;
    private Animator animator;
    private PlayerStats playerStats;
    private PlayerBuffManager buffManager;
    private Coroutine currentSwingCoroutine;

    // Visual attack-range indicator components (ground circle beneath player feet)
    private GameObject rangeIndicatorObj;
    private LineRenderer rangeLineRenderer;
    private Material indicatorMaterial;
    private Coroutine pulseCoroutine;
    private bool isCurrentlyPulsing = false;

    public float AttackDamageMultiplier => buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f;
    public float AttackSpeedMultiplier => buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
    public float AttackHitRange => attackHitRange;

    public bool IsRangedWeaponEquipped => CurrentEquippedItem is ToolItemData t && t.isWeapon && t.isRangedWeapon;

    public ItemData CurrentEquippedItem
    {
        get
        {
            var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
            bool isNight = (timeService != null && timeService.IsNight) ||
                           (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                           (FeaturesTime.DayNightTimeManager.Instance != null && FeaturesTime.DayNightTimeManager.Instance.IsNight);

            var stance = GetComponent<PlayerStanceManager>() ?? PlayerStanceManager.Instance;
            if (isNight || (stance != null && stance.IsInCombatStance))
            {
                var armorState = GetComponent<FeaturesWorkbench.PlayerArmorUpgradeState>() ?? FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
                if (armorState != null)
                {
                    var equippedWep = armorState.GetEquippedItem(FarmBeware.Core.Runtime.EquipmentSlotType.MainWeapon);
                    if (equippedWep != null && FeaturesWorkbench.PlayerWeaponUpgradeState.IsValidCombatWeapon(equippedWep))
                    {
                        return equippedWep;
                    }
                }

                var combatWep = stance != null ? stance.GetActiveCombatWeapon() : null;
                if (combatWep != null) return combatWep;
            }
            if (inventory == null) inventory = GetComponent<InventoryComponent>();
            if (inventory != null)
            {
                int idx = inventory.selectedHotbarIndex;
                if (idx >= 0 && idx < inventory.slots.Count)
                {
                    var slot = inventory.slots[idx];
                    if (slot != null && !slot.IsEmpty)
                        return slot.item;
                }
            }

            // Fallback when hotbar slot is empty: if a ranged weapon (Slingshot) is equipped in MainWeapon slot,
            // return it so daytime/dusk attacks fire the Slingshot instead of bare-hands melee punching.
            var armorStateFallback = GetComponent<FeaturesWorkbench.PlayerArmorUpgradeState>() ?? FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
            if (armorStateFallback != null)
            {
                var mainWep = armorStateFallback.GetEquippedItem(FarmBeware.Core.Runtime.EquipmentSlotType.MainWeapon);
                if (mainWep is ToolItemData rangedMain && rangedMain.isWeapon && rangedMain.isRangedWeapon)
                {
                    return mainWep;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Resolves the horizontal world-space direction from the player toward the mouse cursor,
    /// and rotates the player character horizontally to face the cursor target.
    /// </summary>
    public Vector3 ResolveCursorAimDirection(out Vector3 targetWorldPoint)
    {
        Vector3 fallbackForward = transform.forward.sqrMagnitude > 0.001f ? transform.forward.normalized : Vector3.forward;
        targetWorldPoint = transform.position + fallbackForward * 6f;

        Camera cam = Camera.main;
        if (cam == null)
        {
            return fallbackForward;
        }

        Vector2 screenPos = Vector2.zero;
        bool hasScreenPos = false;

        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            hasScreenPos = true;
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        else
        {
            screenPos = Input.mousePosition;
            hasScreenPos = true;
        }
#endif

        if (!hasScreenPos)
        {
            return fallbackForward;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y + 0.55f, 0f));

        Vector3 hitPoint = targetWorldPoint;
        bool foundHit = false;

        if (Physics.Raycast(ray, out RaycastHit hitInfo, 250f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hitInfo.transform != transform && !hitInfo.transform.IsChildOf(transform))
            {
                var enemy = hitInfo.collider.GetComponentInParent<FeaturesCombat.EnemyBase>();
                if (enemy != null)
                {
                    hitPoint = hitInfo.collider.bounds.center;
                    foundHit = true;
                }
            }
        }

        if (!foundHit && aimPlane.Raycast(ray, out float enterDist) && enterDist > 0f)
        {
            hitPoint = ray.GetPoint(enterDist);
            foundHit = true;
        }

        if (!foundHit)
        {
            return fallbackForward;
        }

        targetWorldPoint = hitPoint;
        Vector3 flatDelta = hitPoint - transform.position;
        flatDelta.y = 0f;

        if (flatDelta.sqrMagnitude < 0.005f)
        {
            return fallbackForward;
        }

        Vector3 aimDir = flatDelta.normalized;
        transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
        return aimDir;
    }

    public bool IsAttackState(AnimatorStateInfo state)
    {
        return state.IsName("attack") ||
               state.IsName("Attack_Combo1") ||
               state.IsName("Attack_Combo2") ||
               state.IsName("Attack_Combo3") ||
               state.IsName("Attack_Combo4") ||
               state.IsName("Attack_Heavy") ||
               state.IsName("Heavy_Attack1") ||
               state.IsName("Heavy_Attack2") ||
               state.IsName("Heavy_Attack3") ||
               state.IsName("Heavy_Attack4") ||
               state.IsName("Attack_Skill");
    }

    public bool IsAnimatorInExpectedCombatState(AnimatorStateInfo state)
    {
        switch (combatStateMachine.CurrentState)
        {
            case FeaturesCombat.Melee.MeleeCombatState.Light1:
                return state.IsName("Attack_Combo1");
            case FeaturesCombat.Melee.MeleeCombatState.Light2:
                return state.IsName("Attack_Combo2");
            case FeaturesCombat.Melee.MeleeCombatState.Light3_Finisher:
                return state.IsName("Attack_Combo3");
            case FeaturesCombat.Melee.MeleeCombatState.Light4_Finisher:
                return state.IsName("Attack_Combo4");
            case FeaturesCombat.Melee.MeleeCombatState.DashAttack:
                return state.IsName("Attack_Combo2");
            case FeaturesCombat.Melee.MeleeCombatState.HeavyRelease:
                return state.IsName("Heavy_Attack1") || state.IsName("Attack_Heavy");
            case FeaturesCombat.Melee.MeleeCombatState.HeavyFinisher1:
            case FeaturesCombat.Melee.MeleeCombatState.HeavyChain2:
                return state.IsName("Heavy_Attack2") || state.IsName("Attack_Heavy");
            case FeaturesCombat.Melee.MeleeCombatState.HeavyFinisher2:
            case FeaturesCombat.Melee.MeleeCombatState.HeavyChain3:
                return state.IsName("Heavy_Attack3") || state.IsName("Attack_Heavy");
            case FeaturesCombat.Melee.MeleeCombatState.HeavyChain4:
                return state.IsName("Heavy_Attack4") || state.IsName("Attack_Heavy");
            default:
                return true;
        }
    }

    private bool ResolveCombatStateProgress(out float normTime)
    {
        normTime = 1.0f;
        if (animator == null || animator.runtimeAnimatorController == null)
            return true;

        var curr = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (combatStateMachine.CurrentState != FeaturesCombat.Melee.MeleeCombatState.Idle)
            {
                if (IsAnimatorInExpectedCombatState(next))
                {
                    normTime = next.normalizedTime;
                    return true;
                }
                if (IsAnimatorInExpectedCombatState(curr))
                {
                    normTime = curr.normalizedTime;
                    return true;
                }
                if (IsAttackState(curr) || IsAttackState(next))
                    return false;
            }
            if (IsAttackState(next)) normTime = next.normalizedTime;
            return true;
        }

        if (combatStateMachine.CurrentState != FeaturesCombat.Melee.MeleeCombatState.Idle)
        {
            if (IsAnimatorInExpectedCombatState(curr))
            {
                normTime = curr.normalizedTime;
                return true;
            }
            if (IsAttackState(curr))
                return false;
        }
        else if (IsAttackState(curr))
        {
            normTime = curr.normalizedTime;
        }
        return true;
    }

    public bool TryPerformAttack()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (isExecutingSkill) return false;

        if (!ResolveCombatStateProgress(out float normTime))
        {
            return false;
        }

        if (!combatStateMachine.TryTriggerLight(Time.time, normTime, out var nextState, out int comboStep))
        {
            return false;
        }

        currentComboIndex = comboStep;

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        bool isRiposte = playerStats != null && playerStats.DefenseEvaluator != null && playerStats.DefenseEvaluator.TryConsumeRiposte();

        float staminaCost = isRiposte ? 0f : ((comboStaminaCosts != null && currentComboIndex < comboStaminaCosts.Length)
            ? comboStaminaCosts[currentComboIndex]
            : attackStaminaCost);

        if (!isRiposte && playerStats != null && (playerStats.currentStamina < staminaCost || playerStats.IsExhausted))
        {
            Debug.Log("[PlayerEquipment] Stamina tidak cukup untuk menyerang!");
            combatStateMachine.ResetToIdle();
            currentComboIndex = 0;
            return false;
        }

        ItemData item = CurrentEquippedItem;

        // Validate weapon or bare hands
        if (item == null)
        {
            if (!allowBareHandsAttack)
            {
                Debug.Log("[PlayerEquipment] Tidak bisa menyerang: Tangan kosong.");
                combatStateMachine.ResetToIdle();
                return false;
            }
        }
        else
        {
            bool isWeapon = (item is ToolItemData tool && tool.isWeapon);
            if (!isWeapon && !allowBareHandsAttack)
            {
                Debug.Log($"[PlayerEquipment] Item '{item.itemName}' bukan senjata, tidak bisa menyerang.");
                combatStateMachine.ResetToIdle();
                return false;
            }
        }

        // Ranged Weapon Execution Path (e.g. Corn Cob Repeater)
        if (item is ToolItemData rangedTool && rangedTool.isRangedWeapon)
        {
            if (staminaCost > 0f && playerStats != null)
            {
                playerStats.UseStamina(staminaCost);
            }
            ExecuteRangedWeaponVolley(rangedTool, isHeavyVolley: false, isSkillBarrage: false, isRiposte: isRiposte);
            return true;
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
            animator.ResetTrigger(attackTriggerName);
            animator.SetInteger("ComboIndex", currentComboIndex);
            animator.SetTrigger(attackTriggerName);

            lastAttackTime = Time.time;

            if (staminaCost > 0f && playerStats != null)
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

            var statMods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
            float comboMul = combatStateMachine.GetCurrentDamageMultiplier();
            float dmgMultiplier = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * comboMul * statMods.DamageMultiplier;
            if (currentComboIndex >= 2)
            {
                dmgMultiplier *= statMods.FinisherDamageMultiplier;
            }
            if (isRiposte)
            {
                dmgMultiplier *= 2.5f;
                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 2.1f,
                        "⚡ RIPOSTE CLEAVE!",
                        new Color(1f, 0.9f, 0.2f));
                }
            }
            int finalDamage = Mathf.RoundToInt(baseDmg * dmgMultiplier);

            // 3rd/4th combo hit (Finisher) or Riposte performs a wide 360-degree sweep
            bool is360 = (currentComboIndex >= 2) || isRiposte;
            float activeRange = is360 ? attackHitRange * 1.15f : attackHitRange;
            float finalKnockback = knockback * combatStateMachine.GetCurrentKnockbackMultiplier() * statMods.KnockbackMultiplier * (isRiposte ? 2.0f : 1.0f);

            if (currentSwingCoroutine != null)
            {
                StopCoroutine(currentSwingCoroutine);
                currentSwingCoroutine = null;
            }

            currentSwingCoroutine = StartCoroutine(RoutineSwingHitbox(finalDamage, finalKnockback, atkSpdMultiplier, is360, activeRange, isFinisher: is360, isHeavy: isRiposte, isDash: false, isRiposte: isRiposte));

            return true;
        }

        return false;
    }

    /// <summary>
    /// Stops any active swing coroutine, weapon trail, and hitbox and restores animator speed without resetting combo state.
    /// Used when movement cancels post-attack recovery hold while keeping the combo window open.
    /// </summary>
    public void EndActiveSwingTrailAndHitbox()
    {
        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
            currentSwingCoroutine = null;
        }

        isExecutingSkill = false;

        if (bareHandsTrail != null)
        {
            bareHandsTrail.EndTrail();
        }

        if (currentWeaponTrail != null)
        {
            currentWeaponTrail.EndTrail();
        }

        if (currentWeaponModel != null)
        {
            var hitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
            if (hitbox != null)
            {
                hitbox.Deactivate();
            }
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    /// <summary>
    /// Immediately cancels any active melee swing or skill coroutine, deactivates weapon hitboxes and trails,
    /// restores animator playback speed, and resets the underlying combat state machine.
    /// Ensures zero ghost hitboxes when an attack is interrupted by dodge, parry, damage, or modal input locks.
    /// </summary>
    public void CancelActiveAttackSwing()
    {
        EndActiveSwingTrailAndHitbox();
        combatStateMachine?.InterruptCombatSequence();
    }

    public bool TryPerformBranchingHeavyFinisher()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (isExecutingSkill) return false;

        if (!ResolveCombatStateProgress(out float normTime))
        {
            return false;
        }

        if (!combatStateMachine.TryTriggerHeavyFinisher(Time.time, normTime, out var nextState))
        {
            return false;
        }

        float staminaCost = attackStaminaCost * 1.4f;
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null && (playerStats.currentStamina < staminaCost || playerStats.IsExhausted))
        {
            combatStateMachine.ResetToIdle();
            return false;
        }

        ItemData item = CurrentEquippedItem;
        if (item is ToolItemData rangedTool && rangedTool.isRangedWeapon)
        {
            if (playerStats != null) playerStats.UseStamina(staminaCost);
            ExecuteRangedWeaponVolley(rangedTool, isHeavyVolley: true, isSkillBarrage: false, isRiposte: false);
            return true;
        }

        float atkSpdMultiplier = buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
        var upgradeState = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance ?? GetComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>();
        if (upgradeState != null && upgradeState.sweetPotatoPathUnlocked) atkSpdMultiplier *= 1.20f;
        atkSpdMultiplier *= attackAnimationSpeed;

        currentComboIndex = 3;
        currentHeavyComboIndex = combatStateMachine.HeavyComboIndex;

        if (animator != null)
        {
            animator.speed = atkSpdMultiplier;
            animator.ResetTrigger(attackTriggerName);
            animator.ResetTrigger("HeavyAttack");
            animator.SetInteger("ComboIndex", 3);
            animator.SetInteger("HeavyComboIndex", currentHeavyComboIndex);
            animator.SetTrigger("HeavyAttack");
        }

        lastAttackTime = Time.time;
        if (playerStats != null) playerStats.UseStamina(staminaCost);

        int baseDmg = 8;
        float knockback = 4.0f;
        if (item is ToolItemData toolData)
        {
            baseDmg = (upgradeState != null) ? upgradeState.baseDamage : toolData.baseDamage;
            knockback = (upgradeState != null) ? upgradeState.baseKnockback : toolData.knockbackForce;
        }

        var statMods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
        float dmgMult = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * combatStateMachine.GetCurrentDamageMultiplier() * statMods.DamageMultiplier * statMods.FinisherDamageMultiplier;
        int finalDamage = Mathf.RoundToInt(baseDmg * dmgMult);
        float finalKnockback = knockback * combatStateMachine.GetCurrentKnockbackMultiplier() * statMods.KnockbackMultiplier;

        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
            currentSwingCoroutine = null;
        }

        currentSwingCoroutine = StartCoroutine(RoutineSwingHitbox(finalDamage, finalKnockback, atkSpdMultiplier, is360: true, rangeOverride: attackHitRange * 1.25f, isFinisher: true, isHeavy: true));
        return true;
    }

    public void ExecuteRangedWeaponVolley(ToolItemData rangedTool, bool isHeavyVolley, bool isSkillBarrage, bool isRiposte = false)
    {
        if (rangedTool == null) return;

        // Guarantee zero melee hitbox or melee swing coroutine overlap
        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
            currentSwingCoroutine = null;
        }
        if (bareHandsTrail != null)
        {
            bareHandsTrail.EndTrail();
        }
        if (currentWeaponTrail != null)
        {
            currentWeaponTrail.EndTrail();
        }
        if (currentWeaponModel != null)
        {
            var accidentalHitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
            if (accidentalHitbox != null)
            {
                accidentalHitbox.Deactivate();
            }
        }

        // Prevent full-body melee sword slash animations from playing when firing the Slingshot
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(attackTriggerName))
            {
                animator.ResetTrigger(attackTriggerName);
            }
            animator.ResetTrigger("HeavyAttack");
            if (!string.IsNullOrEmpty(skillTriggerName) &&
                PlayerControl.HasAnimatorParameter(animator, skillTriggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger(skillTriggerName);
            }
            animator.SetBool("IsAttacking", false);
        }

        lastAttackTime = Time.time;

        var upgradeState = GetComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>() ?? FeaturesWorkbench.PlayerWeaponUpgradeState.Instance;
        var statMods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
        float playerDmgMult = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * statMods.DamageMultiplier * (isRiposte ? 2.5f : 1.0f);

        int weaponLevel = upgradeState != null ? upgradeState.weaponLevel : 1;
        bool activePath = upgradeState != null && upgradeState.activeSpecialization == FeaturesWorkbench.Core.SpecializationPath.Corn;
        int specNodeLevel = upgradeState != null ? upgradeState.specializationNodeLevel : 0;

        var profile = FeaturesCombat.Core.PureLogic.RangedWeaponEvaluator.EvaluateShot(
            rangedTool.baseDamage,
            weaponLevel,
            playerDmgMult,
            rangedTool.projectileSpeed,
            rangedTool.knockbackForce,
            activePath,
            specNodeLevel,
            currentComboIndex,
            isHeavyVolley,
            isSkillBarrage);

        // Aim toward cursor and rotate player to face target
        Vector3 baseForward = ResolveCursorAimDirection(out _);
        Vector3 muzzlePos = transform.position + Vector3.up * 1.0f + baseForward * 0.72f;

        int count = Mathf.Max(1, profile.ProjectileCount);
        for (int i = 0; i < count; i++)
        {
            float angleOffset = 0f;
            if (count > 1 && profile.SpreadAngleDeg > 0.01f)
            {
                float t = (float)i / (count - 1) - 0.5f;
                angleOffset = t * profile.SpreadAngleDeg;
            }

            Vector3 shotDir = Quaternion.Euler(0f, angleOffset, 0f) * baseForward;
            bool isCrit = isRiposte || (Random.value <= profile.CritChance);
            int projDmg = isCrit ? Mathf.RoundToInt(profile.DamagePerProjectile * 1.5f) : profile.DamagePerProjectile;

            var proj = FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                gameObject,
                muzzlePos,
                shotDir,
                projDmg,
                profile.ProjectileSpeed,
                rangedTool.projectileColor);

            if (proj != null)
            {
                proj.ConfigureRangedModifiers(
                    profile.MaxPierceCount,
                    profile.KnockbackForce,
                    profile.HasCritExplosion,
                    profile.ExplosionRadius,
                    isCrit);
            }
        }

        if (currentWeaponModel != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(RoutineSlingshotRecoil());
        }

        PlayAttackSwingSound(false, isHeavyVolley, isHeavyVolley, isSkillBarrage);
        TriggerRangeIndicatorPulse(0.20f);
    }

    private System.Collections.IEnumerator RoutineSlingshotRecoil()
    {
        if (currentWeaponModel == null) yield break;
        Transform modelTr = currentWeaponModel.transform;
        Quaternion baseRot = Quaternion.identity;
        Quaternion recoilRot = Quaternion.Euler(-18f, 0f, 0f);

        float elapsed = 0f;
        const float snapDuration = 0.12f;
        while (elapsed < snapDuration && modelTr != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapDuration);
            modelTr.localRotation = Quaternion.Slerp(recoilRot, baseRot, t);
            yield return null;
        }
        if (modelTr != null)
        {
            modelTr.localRotation = baseRot;
        }
    }

    public bool TryPerformHeavyAttack(float chargeRatio = 1.0f)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (isExecutingSkill) return false;

        float staminaCost = attackStaminaCost * 1.6f;
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null && (playerStats.currentStamina < staminaCost || playerStats.IsExhausted))
        {
            Debug.Log("[PlayerEquipment] Stamina not enough for heavy strike!");
            combatStateMachine.ResetToIdle();
            return false;
        }

        ItemData item = CurrentEquippedItem;
        if (item == null && !allowBareHandsAttack) return false;
        if (item != null && !(item is ToolItemData tool && tool.isWeapon) && !allowBareHandsAttack) return false;

        combatStateMachine.ReleaseHeavy(Time.time, out _);

        if (item is ToolItemData rangedHeavyTool && rangedHeavyTool.isRangedWeapon)
        {
            lastAttackTime = Time.time;
            currentComboIndex = 3;
            if (playerStats != null) playerStats.UseStamina(staminaCost);
            ExecuteRangedWeaponVolley(rangedHeavyTool, isHeavyVolley: true, isSkillBarrage: false, isRiposte: false);
            return true;
        }

        float atkSpdMultiplier = buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
        var upgradeState = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance ?? GetComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>();
        if (upgradeState != null && upgradeState.sweetPotatoPathUnlocked) atkSpdMultiplier *= 1.20f;
        atkSpdMultiplier *= attackAnimationSpeed;

        currentComboIndex = 3;
        currentHeavyComboIndex = combatStateMachine.HeavyComboIndex;

        if (animator != null)
        {
            animator.speed = atkSpdMultiplier;
            animator.ResetTrigger(attackTriggerName);
            animator.ResetTrigger("HeavyAttack");
            animator.SetInteger("ComboIndex", 3);
            animator.SetInteger("HeavyComboIndex", currentHeavyComboIndex);
            animator.SetTrigger("HeavyAttack");
        }

        lastAttackTime = Time.time;
        if (playerStats != null) playerStats.UseStamina(staminaCost);

        int baseDmg = 8;
        float knockback = 4.0f;
        if (item is ToolItemData toolData)
        {
            baseDmg = (upgradeState != null) ? upgradeState.baseDamage : toolData.baseDamage;
            knockback = (upgradeState != null) ? upgradeState.baseKnockback : toolData.knockbackForce;
        }

        float dmgMult = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * combatStateMachine.GetCurrentDamageMultiplier();
        int finalDamage = Mathf.RoundToInt(baseDmg * dmgMult);
        float finalKnockback = knockback * combatStateMachine.GetCurrentKnockbackMultiplier();

        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
            currentSwingCoroutine = null;
        }

        currentSwingCoroutine = StartCoroutine(RoutineSwingHitbox(finalDamage, finalKnockback, atkSpdMultiplier, is360: true, rangeOverride: attackHitRange * 1.3f, isHeavy: true));
        return true;
    }

    /// <summary>
    /// Advances the 4-hit heavy attack chain (Heavy_Attack1 -> Heavy_Attack2 -> Heavy_Attack3 -> Heavy_Attack4).
    /// </summary>
    public bool TryPerformHeavyChainAttack()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (isExecutingSkill) return false;

        if (!ResolveCombatStateProgress(out float normTime))
        {
            return false;
        }

        if (!combatStateMachine.TryTriggerHeavyChain(Time.time, normTime, out _, out int heavyStep))
        {
            return false;
        }

        float staminaCost = attackStaminaCost * 1.4f;
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null && (playerStats.currentStamina < staminaCost || playerStats.IsExhausted))
        {
            combatStateMachine.ResetToIdle();
            return false;
        }

        ItemData item = CurrentEquippedItem;
        if (item == null && !allowBareHandsAttack)
        {
            combatStateMachine.ResetToIdle();
            return false;
        }
        if (item != null && !(item is ToolItemData tool && tool.isWeapon) && !allowBareHandsAttack)
        {
            combatStateMachine.ResetToIdle();
            return false;
        }
        if (item is ToolItemData rangedTool && rangedTool.isRangedWeapon)
        {
            if (playerStats != null) playerStats.UseStamina(staminaCost);
            ExecuteRangedWeaponVolley(rangedTool, isHeavyVolley: true, isSkillBarrage: false, isRiposte: false);
            return true;
        }

        float atkSpdMultiplier = buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
        var upgradeState = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance ?? GetComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>();
        if (upgradeState != null && upgradeState.sweetPotatoPathUnlocked) atkSpdMultiplier *= 1.20f;
        atkSpdMultiplier *= attackAnimationSpeed;

        currentComboIndex = 3;
        currentHeavyComboIndex = heavyStep;

        if (animator != null)
        {
            animator.speed = atkSpdMultiplier;
            animator.ResetTrigger(attackTriggerName);
            animator.ResetTrigger("HeavyAttack");
            animator.SetInteger("ComboIndex", 3);
            animator.SetInteger("HeavyComboIndex", currentHeavyComboIndex);
            animator.SetTrigger("HeavyAttack");
        }

        lastAttackTime = Time.time;
        if (playerStats != null) playerStats.UseStamina(staminaCost);

        int baseDmg = 8;
        float knockback = 4.0f;
        if (item is ToolItemData toolData)
        {
            baseDmg = (upgradeState != null) ? upgradeState.baseDamage : toolData.baseDamage;
            knockback = (upgradeState != null) ? upgradeState.baseKnockback : toolData.knockbackForce;
        }

        var statMods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
        float dmgMult = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * combatStateMachine.GetCurrentDamageMultiplier() * statMods.DamageMultiplier;
        int finalDamage = Mathf.RoundToInt(baseDmg * dmgMult);
        float finalKnockback = knockback * combatStateMachine.GetCurrentKnockbackMultiplier() * statMods.KnockbackMultiplier;

        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
            currentSwingCoroutine = null;
        }

        currentSwingCoroutine = StartCoroutine(RoutineSwingHitbox(finalDamage, finalKnockback, atkSpdMultiplier, is360: true, rangeOverride: attackHitRange * 1.3f, isFinisher: currentHeavyComboIndex >= 3, isHeavy: true));
        return true;
    }

    public bool TryPerformDashAttack()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (isExecutingSkill) return false;

        float staminaCost = attackStaminaCost * 1.2f;
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null && (playerStats.currentStamina < staminaCost || playerStats.IsExhausted))
        {
            Debug.Log("[PlayerEquipment] Stamina not enough for dash attack!");
            combatStateMachine.ResetToIdle();
            return false;
        }

        ItemData item = CurrentEquippedItem;
        if (item == null && !allowBareHandsAttack) return false;
        if (item != null && !(item is ToolItemData tool && tool.isWeapon) && !allowBareHandsAttack) return false;

        if (!combatStateMachine.TryTriggerDashAttack(Time.time)) return false;

        if (item is ToolItemData rangedDashTool && rangedDashTool.isRangedWeapon)
        {
            lastAttackTime = Time.time;
            currentComboIndex = 1;
            if (playerStats != null) playerStats.UseStamina(staminaCost);
            ExecuteRangedWeaponVolley(rangedDashTool, isHeavyVolley: false, isSkillBarrage: false, isRiposte: false);
            return true;
        }

        float atkSpdMultiplier = buffManager != null ? buffManager.GetAttackSpeedMultiplier() : 1f;
        var upgradeState = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance ?? GetComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>();
        if (upgradeState != null && upgradeState.sweetPotatoPathUnlocked) atkSpdMultiplier *= 1.20f;
        atkSpdMultiplier *= attackAnimationSpeed;

        if (animator != null)
        {
            animator.speed = atkSpdMultiplier;
            animator.SetInteger("ComboIndex", 1);
            animator.SetTrigger(attackTriggerName);
        }

        lastAttackTime = Time.time;
        if (playerStats != null) playerStats.UseStamina(staminaCost);

        int baseDmg = 8;
        float knockback = 4.0f;
        if (item is ToolItemData toolData)
        {
            baseDmg = (upgradeState != null) ? upgradeState.baseDamage : toolData.baseDamage;
            knockback = (upgradeState != null) ? upgradeState.baseKnockback : toolData.knockbackForce;
        }

        float dmgMult = (buffManager != null ? buffManager.GetAttackDamageMultiplier() : 1f) * combatStateMachine.GetCurrentDamageMultiplier();
        int finalDamage = Mathf.RoundToInt(baseDmg * dmgMult);
        float finalKnockback = knockback * combatStateMachine.GetCurrentKnockbackMultiplier();

        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
            currentSwingCoroutine = null;
        }

        currentSwingCoroutine = StartCoroutine(RoutineSwingHitbox(finalDamage, finalKnockback, atkSpdMultiplier, is360: false, rangeOverride: attackHitRange * 1.25f, isDash: true));
        return true;
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

        if (item is ToolItemData rangedSkillTool && rangedSkillTool.isRangedWeapon)
        {
            lastAttackTime = Time.time;
            currentComboIndex = 0;
            if (playerStats != null) playerStats.UseStamina(skillStaminaCost);
            ExecuteRangedWeaponVolley(rangedSkillTool, isHeavyVolley: false, isSkillBarrage: true, isRiposte: false);
            return true;
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
            if (PlayerControl.HasAnimatorParameter(animator, skillTriggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(skillTriggerName);
            }

            lastAttackTime = Time.time;
            currentComboIndex = 0; // Reset combo after skill

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

    private System.Collections.IEnumerator RoutineSwingHitbox(int damage, float knockback, float speedMultiplier, bool is360 = false, float rangeOverride = -1f, bool isFinisher = false, bool isHeavy = false, bool isDash = false, bool isRiposte = false)
    {
        float speedRatio = speedMultiplier / Mathf.Max(0.1f, attackAnimationSpeed);
        float delay;
        float duration;

        if (isDash)
        {
            delay = 0.08f / speedRatio;
            duration = 0.18f / speedRatio;
        }
        else if (isHeavy)
        {
            ResolveHeavyStepTiming(currentHeavyComboIndex, out float baseHeavyDelay, out float baseHeavyDur);
            delay = baseHeavyDelay / speedRatio;
            duration = baseHeavyDur / speedRatio;
        }
        else if (currentComboIndex == 3)
        {
            delay = combo4SwingDelay / speedRatio;
            duration = combo4SwingDuration / speedRatio;
        }
        else if (is360 || isFinisher || currentComboIndex == 2)
        {
            delay = combo3SwingDelay / speedRatio;
            duration = combo3SwingDuration / speedRatio;
        }
        else if (currentComboIndex == 1)
        {
            delay = combo2SwingDelay / speedRatio;
            duration = combo2SwingDuration / speedRatio;
        }
        else
        {
            delay = combo1SwingDelay / speedRatio;
            duration = combo1SwingDuration / speedRatio;
        }

        TriggerRangeIndicatorPulse(delay + duration + 0.1f);

        // Wait until direct state-to-state combo transition enters the expected state so hitbox/trail align with the clip
        if (animator != null && animator.runtimeAnimatorController != null && !isDash && combatStateMachine.CurrentState != FeaturesCombat.Melee.MeleeCombatState.Idle)
        {
            float syncWait = 0f;
            while (syncWait < 0.45f)
            {
                var curr = animator.GetCurrentAnimatorStateInfo(0);
                var next = animator.GetNextAnimatorStateInfo(0);
                if (IsAnimatorInExpectedCombatState(curr) || IsAnimatorInExpectedCombatState(next))
                {
                    break;
                }
                syncWait += Time.deltaTime;
                yield return null;
            }
        }

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

        var activeTrail = GetActiveTrail();
        if (activeTrail != null)
        {
            activeTrail.BeginTrail(currentComboIndex, isFinisher || is360, isHeavy);
        }

        PlayAttackSwingSound(is360, isFinisher, isHeavy, isDash);

        if (hitbox != null && !is360 && !isHeavy && !isDash && !isRiposte)
        {
            hitbox.ConfigureHitRange(activeRange);
            hitbox.Activate(gameObject, damage, knockback, transform.forward);
        }
        else
        {
            // Direct zero-GC non-alloc sweep for 360, heavy strikes, ripostes, dash attacks, or bare hands
            PerformDirectMeleeSweep(damage, knockback, is360, activeRange, isFinisher, isHeavy, isDash, isRiposte);
        }

        yield return new WaitForSeconds(duration);

        if (activeTrail != null)
        {
            activeTrail.EndTrail();
        }

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

        float speedRatio = speedMultiplier / Mathf.Max(0.1f, attackAnimationSpeed);
        float launchDelay = 0.25f / speedRatio;
        float impactDelay = 1.14f / speedRatio;
        float postImpactLinger = 0.20f / speedRatio;

        TriggerRangeIndicatorPulse(impactDelay + 0.35f);

        // Wait for leap windup to finish as the character launches airborne
        yield return new WaitForSeconds(launchDelay);

        PlaySwingAudio(swingSkillClip != null ? swingSkillClip : swingHeavyClip);

        var activeSkillTrail = GetActiveTrail();
        if (activeSkillTrail != null)
        {
            activeSkillTrail.BeginTrail(2, isFinisher: true, isHeavy: true);
        }

        // Keep weapon trail active across the airborne arc until ground impact
        float flightDuration = Mathf.Max(0.05f, impactDelay - launchDelay);
        yield return new WaitForSeconds(flightDuration);

        // Ground slam impact: 360 AoE zero-GC non-alloc sweep with heavy impulse
        PerformDirectMeleeSweep(damage, knockback, is360: true, rangeOverride: range, isHeavy: true);

        yield return new WaitForSeconds(postImpactLinger);

        if (activeSkillTrail != null)
        {
            activeSkillTrail.EndTrail();
        }

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
    /// Direct melee sweep query (zero-GC, non-alloc, supporting frontal cone or 360 spins with visual impulse juice).
    /// </summary>
    public void PerformDirectMeleeSweep(int damage, float knockback, bool is360 = false, float rangeOverride = -1f, bool isFinisher = false, bool isHeavy = false, bool isDash = false, bool isRiposte = false)
    {
        float activeRange = rangeOverride > 0f ? rangeOverride : attackHitRange;
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 forwardDir = transform.forward;
        Vector3 sweepCenter = is360 ? origin : origin + forwardDir * (activeRange * 0.5f);
        float sweepRadius = is360 ? activeRange : (activeRange * 0.65f);

        _damagedTargetsCount = 0;
        int hitCount = Physics.OverlapSphereNonAlloc(sweepCenter, sweepRadius, _hitBuffer, ~0, QueryTriggerInteraction.Collide);

        bool landedHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = _hitBuffer[i];
            if (c == null || c.gameObject == gameObject || c.transform.IsChildOf(transform)) continue;

            if (!is360)
            {
                Vector3 toTarget = c.transform.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.04f && Vector3.Dot(forwardDir, toTarget.normalized) < 0.2f)
                    continue;
            }

            var target = c.GetComponent<FeaturesCombat.IDamageable>() ?? c.GetComponentInParent<FeaturesCombat.IDamageable>();
            if (target != null && !target.IsDead && !HasTargetBeenHit(target))
            {
                // Line-of-sight occlusion check: verify attack is not obstructed by solid walls or fences
                Vector3 targetPoint;
                try { targetPoint = c.ClosestPoint(origin); }
                catch { targetPoint = c.bounds.center; }

                if (FarmBeware.Core.Runtime.NonAllocPhysics.IsLineOfSightBlocked(origin, targetPoint, c, gameObject, ObstacleLayerMask.value))
                {
                    continue; // Blocked by wall/obstacle!
                }

                RecordTargetHit(target);
                landedHit = true;

                Vector3 hitPoint = c.ClosestPoint(sweepCenter);
                Vector3 hitDir = is360 ? (c.transform.position - origin).normalized : forwardDir;
                if (hitDir.sqrMagnitude < 0.01f) hitDir = forwardDir;

                bool isGodMode = (playerStats != null && playerStats.isGodMode);
                int effectiveDamage = isGodMode ? 99999 : damage;

                target.TakeDamage(effectiveDamage, hitPoint, hitDir);

                if (FeaturesCombat.Adapters.CombatHitVfxEngine.Instance != null)
                {
                    FeaturesCombat.Core.PureLogic.AttackImpactTier impactTier;
                    if (isRiposte || isExecutingSkill)
                    {
                        impactTier = FeaturesCombat.Core.PureLogic.AttackImpactTier.ParryRiposte;
                    }
                    else if (isHeavy)
                    {
                        impactTier = FeaturesCombat.Core.PureLogic.AttackImpactTier.HeavyCleave;
                    }
                    else if (isFinisher || is360 || isDash)
                    {
                        impactTier = FeaturesCombat.Core.PureLogic.AttackImpactTier.ComboFinisher;
                    }
                    else
                    {
                        impactTier = FeaturesCombat.Core.PureLogic.AttackImpactTier.LightSlash;
                    }

                    FeaturesCombat.Adapters.CombatHitVfxEngine.Instance.TriggerHit(
                        hitPoint,
                        hitDir,
                        c.gameObject,
                        gameObject,
                        isCrit: isRiposte || isFinisher || isHeavy || isExecutingSkill,
                        impactTier: impactTier);
                }

                Rigidbody targetRb = c.GetComponent<Rigidbody>() ?? c.GetComponentInParent<Rigidbody>();
                if (targetRb != null && !targetRb.isKinematic)
                {
                    targetRb.AddForce((hitDir + Vector3.up * 0.35f).normalized * knockback, ForceMode.Impulse);
                }

                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnEnemyDamage(
                        hitPoint + Vector3.up * 0.8f,
                        effectiveDamage,
                        isCrit: isFinisher || isHeavy,
                        isSkill: isExecutingSkill || isHeavy);
                }
            }
        }

        // Post-processing tactile screen juice on impactful hits
        if (landedHit && (isFinisher || isHeavy || isDash || isExecutingSkill))
        {
            if (FeaturesTime.Atmosphere.DayNightVolumeController.Instance != null)
            {
                float impulseIntensity = isHeavy ? 0.95f : (isFinisher ? 0.85f : 0.65f);
                float impulseDuration = isHeavy ? 0.30f : 0.22f;
                FeaturesTime.Atmosphere.DayNightVolumeController.Instance.TriggerCombatImpulse(impulseIntensity, impulseDuration);
            }
        }
    }

}
