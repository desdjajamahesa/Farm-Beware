using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem; // Pastikan ini tetap ada
using FarmBeware.Core.Runtime;

[RequireComponent(typeof(Rigidbody))]
public partial class PlayerControl : MonoBehaviour, IPlayerContext
{
    [Header("Pengaturan Pergerakan")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float turnSpeed = 15f;

    [Header("Pengaturan Aksi")]
    public float jumpForce = 5f;
    [Tooltip("Durasi penguncian pergerakan saat menanam benih (detik).")]
    public float plantDuration = 2.90f;
    [Tooltip("Durasi penguncian pergerakan saat menyiram tanaman (detik).")]
    public float waterDuration = 3.95f;
    private bool isPlanting = false;
    private Coroutine farmActionRoutine = null;

    public enum PlayerActionState
    {
        Locomotion,
        ChargingAttack,
        LightAttack,
        HeavyAttack,
        DashAttack,
        SkillLeap,
        RangedRecovery,
        Dodging,
        FarmingAction,
        InputLocked
    }

    [Tooltip("Durasi karakter diam di tempat saat mengayunkan serangan pedang (detik).")]
    public float attackLockDuration = 1.1f;
    private bool isAttacking = false;
    private bool isSkillLeaping = false;
    private bool isLightAttacking = false;
    private bool isInAttackRecovery = false;
    private bool hasBufferedAttack = false;
    private bool hasBufferedHeavyAttack = false;
    private float bufferedAttackTime = 0f;
    private Coroutine attackCoroutine = null;
    private Coroutine dodgeCoroutine = null;
    private PlayerActionState _currentCombatSubState = PlayerActionState.Locomotion;
    private float idleFidgetTimer = 0f;
    public bool IsAttacking => isAttacking;
    public bool IsSkillLeaping => isSkillLeaping;
    public bool IsPlanting => isPlanting;
    public bool IsInAttackRecovery => isInAttackRecovery;

    public PlayerActionState CurrentActionState
    {
        get
        {
            if (isInputLocked) return PlayerActionState.InputLocked;
            if (isDodging) return PlayerActionState.Dodging;
            if (isPlanting) return PlayerActionState.FarmingAction;
            if (isSkillLeaping) return PlayerActionState.SkillLeap;
            if (isAttacking) return isLightAttacking ? PlayerActionState.LightAttack : (_currentCombatSubState != PlayerActionState.Locomotion ? _currentCombatSubState : PlayerActionState.HeavyAttack);
            if (isChargingAttack) return PlayerActionState.ChargingAttack;
            return PlayerActionState.Locomotion;
        }
    }

    [Header("Pengaturan Pergerakan Saat Menyerang")]
    [Tooltip("Pengali kecepatan jalan saat menyerang (misal: 0.4 = 40% dari walkSpeed). Karakter tetap bisa bergerak pelan.")]
    [Range(0.1f, 1.0f)]
    public float attackMoveSpeedMultiplier = 0.4f;

    [Tooltip("Kecepatan rotasi karakter saat menyerang sehingga bisa berputar/membalik arah (bulak-balik).")]
    [Range(5f, 30f)]
    public float attackTurnSpeed = 18f;

    [Header("Pengaturan Jurus Spesial (Leap Momentum)")]
    [Tooltip("Dorongan momentum maju saat melompat menerjang.")]
    public float leapForwardImpulse = 6.5f;

    [Tooltip("Dorongan momentum ke atas saat melompat menerjang.")]
    public float leapUpwardImpulse = 2.5f;

    [Header("Pengaturan Menghindar & Tangkisan (Dodge & Deflect)")]
    [Tooltip("Kecepatan meluncur saat melakukan dodge roll.")]
    public float dodgeSpeed = 6.0f;
    [Tooltip("Durasi gerakan dodge roll (detik).")]
    public float dodgeDuration = 0.85f;
    [Tooltip("Durasi frame kekebalan (i-frames) selama dodge roll (detik).")]
    public float dodgeIFrameDuration = 0.45f;
    [Tooltip("Durasi window tangkisan parry (detik). Standar 0.35s = 350ms agar timing terasa pas dan adil.")]
    public float parryWindow = 0.35f;
    [Tooltip("Konsumsi stamina saat dodge roll.")]
    public float dodgeStaminaCost = 15f;
    [Tooltip("Konsumsi stamina saat melakukan precision parry.")]
    public float parryStaminaCost = 10f;
    private bool isDodging = false;
    private Vector3 dodgeRollDirection = Vector3.forward;
    private float dodgeElapsed = 0f;
    public bool IsDodging => isDodging;

    [Header("Pengaturan Tangga (Step-Up)")]
    [Tooltip("Tinggi maksimum anak tangga yang bisa dinaiki otomatis (meter).")]
    [Range(0.05f, 0.6f)]
    public float stepUpHeight = 0.35f;
    [Tooltip("Kecepatan smooth naik anak tangga.")]
    [Range(5f, 30f)]
    public float stepUpSpeed = 15f;

    private CapsuleCollider playerCollider;
    private Rigidbody rb;
    private Animator animator;
    private Vector3 inputVector;
    private PlayerInputActions inputActions;
    private PlayerInteractor interactor;
    private InventoryComponent playerInventory;
    private PlayerStats playerStats;
    private PlayerEquipment playerEquipment;
    private PlayerBuffManager buffManager;

    [Header("Physics Movement")]
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float airControl = 12f;

    // Status internal
    private bool isGrounded;
    private bool isRunning;
    private bool isJumping;
    private float groundBufferTimer;
    private const float GroundBufferDuration = 0.12f;
    private float groundedGraceTimer = 0f;
    private bool isGroundedRecently => isGrounded || groundedGraceTimer > 0f;
    private bool isChargingAttack = false;

    // Wall slide helpers
    private Vector3 contactWallNormal = Vector3.zero;
    private bool isTouchingWall = false;
    private float lastWallContactTime = -10f;
    private float sprintDuration = 0f;

    // Global input lock: when true, player cannot move, open inventory, jump, dash, or attack.
    // Centrally queries ModalStackManager so that any open modal window halts player input automatically.
    private bool _isInputLocked = false;
    public bool isInputLocked
    {
        get => _isInputLocked || (FarmBeware.Core.Runtime.ModalStackManager.HasInstance && FarmBeware.Core.Runtime.ModalStackManager.Instance.HasActiveModal);
        set => SetControlLock(value);
    }
    private readonly System.Collections.Generic.HashSet<string> _inputLockSources = new System.Collections.Generic.HashSet<string>();

    #region IPlayerContext Implementation
    public Transform Transform => this != null ? transform : null;
    bool IPlayerContext.IsInputLocked { get => isInputLocked; set => SetControlLock(value); }
    void IPlayerContext.AddInputLock(string source) => AddInputLock(source);
    void IPlayerContext.RemoveInputLock(string source) => RemoveInputLock(source);
    void IPlayerContext.ClearAllInputLocks() => ClearAllInputLocks();
    bool IPlayerContext.HasInputLock(string source) => HasInputLock(source);
    bool IPlayerContext.HasLockOtherThan(string excludedSource) => HasLockOtherThan(excludedSource);
    public bool IsGodMode => playerStats != null && playerStats.isGodMode;
    public void PlayAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
    }

    internal static bool HasAnimatorParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(paramName))
            return false;

        var parameters = anim.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == type && parameters[i].name == paramName)
                return true;
        }
        return false;
    }
    public T GetPlayerComponent<T>() where T : class => GetComponent<T>();
    #endregion

    void Awake()
    {
        ServiceLocator.Register<IPlayerContext>(this);
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();

        // Berikan material tanpa friksi agar pergerakan dan sliding di tembok sangat mulus
        if (playerCollider != null)
        {
            PhysicsMaterial frictionless = new PhysicsMaterial("PlayerFrictionless")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            playerCollider.material = frictionless;
        }

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (inputActions == null)
            inputActions = new PlayerInputActions();

        interactor = GetComponent<PlayerInteractor>();  
        playerInventory = GetComponent<InventoryComponent>();  
        if (playerInventory != null)
            playerInventory.HasHotbar = true;
        playerStats = GetComponent<PlayerStats>();
        buffManager = GetComponent<PlayerBuffManager>();
        if (GetComponent<FeaturesEconomy.PlayerWallet>() == null)
            gameObject.AddComponent<FeaturesEconomy.PlayerWallet>();
        if (GetComponent<FeaturesFarming.PlantWaterer>() == null)
            gameObject.AddComponent<FeaturesFarming.PlantWaterer>();
        if (GetComponent<FeaturesKitchen.PlayerWaterBottle>() == null)
            gameObject.AddComponent<FeaturesKitchen.PlayerWaterBottle>();
        playerEquipment = GetComponent<PlayerEquipment>();
        if (playerEquipment == null)
            playerEquipment = gameObject.AddComponent<PlayerEquipment>();
    }

    void OnEnable()
    {
        // Guard: pastikan inputActions selalu ada meski OnEnable berjalan sebelum Awake.
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        inputActions.Player.Enable();

        // Mendaftarkan event: Saat tombol ditekan, panggil fungsi yang sesuai
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Interact.performed += OnInteractPressed;
    }

    void OnDisable()
    {
        // Guard null agar OnDisable aman saat OnEnable gagal/urutan tidak menentu.
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        // Mencabut pendaftaran event untuk mencegah memory leak
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Interact.performed -= OnInteractPressed;

        inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IPlayerContext>();
    }

    private void OnJumpPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => ExecuteJump();

    void Update()
    {
        // Kunci input: hentikan inventory/hotbar/gerak/animator saat terkunci.
        if (isInputLocked)
        {
            if (isAttacking || isSkillLeaping || isChargingAttack || hasBufferedAttack)
            {
                CancelActiveCombatAction();
            }

            // Izinkan Tab / I menutup inventori saat UI inventori sedang terbuka
            if (InventoryManagerUI.Instance != null && InventoryManagerUI.Instance.IsAnyInventoryUIRelatedOpen())
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && (kb.tabKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame))
                {
                    if (Time.frameCount != FarmBeware.Core.Runtime.UIModalHelper.LastFrameUIPanelClosed)
                    {
                        InventoryManagerUI.Instance.CloseAllUI();
                    }
                }
            }

            // Pastikan velocity benar-benar nol dan animasi idle saat UI terbuka
            CheckGrounded();
            inputVector = Vector3.zero;
            isRunning = false;
            if (rb != null && !rb.isKinematic)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (animator != null && !animator.GetBool("Driving"))
            {
                animator.SetBool("Grounded", isGrounded);
                animator.SetFloat("Vel", 0f, 0.05f, Time.deltaTime);
                animator.SetBool("Idle", true);
                animator.SetBool("Sprinting", false);
            }
            return;
        }

        // Saat menanam benih / menyiram, karakter diam di tempat (tidak bisa bergerak/berlari/lompat/interact)
        // KECUALI pemain memberikan input gerak (WASD) untuk membatalkan animasi menyiram/menanam secara instan!
        if (isPlanting)
        {
            Vector2 moveVal = (inputActions != null)
                ? inputActions.Player.Move.ReadValue<Vector2>()
                : Vector2.zero;

            if (moveVal.sqrMagnitude > 0.04f)
            {
                CancelFarmAction();
            }
            else
            {
                inputVector = Vector3.zero;
                isRunning = false;
                if (animator != null && !animator.GetBool("Driving"))
                {
                    animator.SetFloat("Vel", 0f, 0.1f, Time.deltaTime);
                    animator.SetBool("Idle", true);
                    animator.SetBool("Sprinting", false);
                }
                return;
            }
        }

        // 1. Cek apakah karakter menginjak tanah
        CheckGrounded();
        if (isGrounded)
        {
            groundedGraceTimer = 0.1f;
        }
        else if (groundedGraceTimer > 0f)
        {
            groundedGraceTimer -= Time.deltaTime;
        }

        // 2. Membaca Input Pergerakan (Deadzone check agar micro-drift tidak menormalkan sudut acak)
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        float inputMag = moveInput.magnitude;
        if (inputMag > 0.05f)
        {
            // Preserve analog stick sensitivity (clamped to 1.0) instead of blanket normalization
            Vector2 dir = moveInput / inputMag;
            float clampedMag = Mathf.Clamp01(inputMag);
            inputVector = new Vector3(dir.x, 0f, dir.y) * clampedMag;
        }
        else
        {
            inputVector = Vector3.zero;
        }

        // 3. Aksi Defensif (Dodge Roll & Parry) - bisa dieksekusi saat bergerak, idle, atau cancel serangan
        HandleDefensiveInput();

        // 4. Input Serangan (mendukung combo buffering, early cancel, dan branching heavy finisher saat sedang mengayun)
        HandleAttackInput();

        // Input aksi non-kombat diblokir saat sedang mengeksekusi serangan
        if (!isAttacking)
        {
            HandleInventoryInput();
            HandleHotbarInput();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TriggerInteract();
            }
        }

        // 3. Cek apakah pemain menahan tombol Shift untuk Lari (Sprint)
        bool isMoving = inputVector.magnitude >= 0.1f;
        bool wantsToRun = !isAttacking && Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        // Karakter hanya berlari jika bergerak, menekan shift, memiliki stamina, dan menginjak tanah (dengan grace buffer mencegah stutter di tanjakan/turunan)
        bool hasStamina = playerStats == null || (!playerStats.IsExhausted && playerStats.currentStamina > 0.5f);
        isRunning = isMoving && wantsToRun && hasStamina && isGroundedRecently;

        // 4. Konsumsi Stamina HANYA saat Berlari (Sprint) dan benar-benar bergerak secara fisik
        if (isRunning)
        {
            sprintDuration += Time.deltaTime;
            bool hasPhysicalMovement = rb != null && Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).sqrMagnitude > 0.04f;

            // Sprint grace period ~0.15s agar respon awal dari posisi diam tetap responsif;
            // setelah 0.15s, butuh pergerakan fisik nyata agar stamina tidak terkuras saat menabrak dinding/kolider.
            if ((sprintDuration <= 0.15f || hasPhysicalMovement) && playerStats != null)
            {
                playerStats.UseStamina(playerStats.staminaDrainRate * Time.deltaTime);
            }
        }
        else
        {
            sprintDuration = 0f;
        }

        // 5. Sinkronisasi Animator secara natural
        if (animator != null)
        {
            animator.SetBool("IsAttacking", isAttacking);
            if (isAttacking)
            {
                animator.SetBool("Sprinting", false);
                animator.SetBool("Grounded", true); // Kunci Grounded tetap true saat menyerang agar AirBorn tidak memotong animasi
                animator.SetFloat("Vel", isMoving ? 0.3f : 0f);
            }
            else
            {
                isInAttackRecovery = false;
                if (HasAnimatorParameter(animator, "InAttackRecovery", AnimatorControllerParameterType.Bool))
                    animator.SetBool("InAttackRecovery", false);

                // Kecepatan horizontal fisik aktual agar langkah kaki sinkron (mencegah efek kaki selip)
                Vector3 horizVel = rb != null ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) : Vector3.zero;
                float currentSpeed = horizVel.magnitude;

                float targetSpeed = 0f;
                if (isMoving && currentSpeed > 0.1f)
                {
                    if (isRunning)
                    {
                        targetSpeed = Mathf.Clamp(currentSpeed / runSpeed, 0.5f, 1.0f);
                    }
                    else
                    {
                        targetSpeed = Mathf.Clamp((currentSpeed / walkSpeed) * 0.5f, 0.1f, 0.5f);
                    }
                }

                // Gunakan dampTime 0.12f agar perubahan kecepatan dan langkah kaki bertransisi mulus
                animator.SetFloat("Vel", targetSpeed, 0.12f, Time.deltaTime);
                animator.SetBool("Grounded", isGrounded);
                // Idle aktif jika pemain tidak memberi input dan kecepatan tubuh sudah melambat
                bool isIdleNow = !isMoving && currentSpeed < 0.25f;
                animator.SetBool("Idle", isIdleNow);

                // Sinkronisasi status memegang senjata (opsional, hanya jika parameter ada di Animator Controller)
                bool hasWeapon = (playerEquipment != null && playerEquipment.IsHoldingWeapon);
                if (HasAnimatorParameter(animator, "HasWeapon", AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool("HasWeapon", hasWeapon);
                }

                // Idle fidget: HANYA jika sedang memegang senjata dan diam lebih dari 9 detik
                if (isIdleNow && hasWeapon && HasAnimatorParameter(animator, "LookAround", AnimatorControllerParameterType.Trigger))
                {
                    idleFidgetTimer += Time.fixedDeltaTime;
                    if (idleFidgetTimer > 9.0f)
                    {
                        idleFidgetTimer = 0f;
                        if (isGrounded && !isAttacking)
                        {
                            animator.SetTrigger("LookAround");
                        }
                    }
                }
                else
                {
                    idleFidgetTimer = 0f;
                }
                animator.SetBool("Sprinting", isRunning && currentSpeed > walkSpeed * 0.8f);
            }
        }
    }

    // Step-Up: Angkat karakter secara smooth melewati anak tangga rendah (tanpa invisible wall)
    private void HandleStepUp(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f || playerCollider == null || rb == null) return;
        if (!isGrounded) return;

        float radius = playerCollider.radius;
        float checkDist = radius + 0.2f;

        // 1. Cek ada rintangan pendek di depan bawah (setinggi < stepUpHeight)
        Vector3 lowOrigin = transform.position + Vector3.up * 0.05f;
        if (!Physics.Raycast(lowOrigin, moveDir, out RaycastHit lowHit, checkDist,
            ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            return;

        // Pastikan rintangan cukup rendah (bukan dinding tinggi)
        float obstacleTopY = lowHit.collider.bounds.max.y;
        float stepDelta = obstacleTopY - transform.position.y;
        if (stepDelta <= 0f || stepDelta > stepUpHeight) return;

        // 2. Cek apakah di atas step ada ruang yang cukup (tidak ada atap rendah)
        Vector3 aboveStepOrigin = transform.position + Vector3.up * (stepDelta + 0.05f) + moveDir * checkDist;
        if (Physics.CheckSphere(aboveStepOrigin, radius * 0.9f,
            ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            return;

        // 3. Angkat player ke atas anak tangga secara smooth
        float targetY = transform.position.y + stepDelta;
        float newY = Mathf.MoveTowards(rb.position.y, targetY, stepUpSpeed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector3(rb.position.x, newY, rb.position.z));
        // Reset vertical velocity saat step-up agar tidak memantul
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

        void FixedUpdate()
    {
        // Kunci input: hentikan fisika pergerakan saat terkunci atau sedang menanam benih
        if (isInputLocked || isPlanting)
        {
            if (rb != null && !rb.isKinematic)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // Saat dodge roll aktif, dorong rigidbody ke arah dodgeRollDirection dengan pelambatan halus di 30% akhir animasi
        if (isDodging)
        {
            if (rb != null)
            {
                float progress = dodgeDuration > 0.01f ? Mathf.Clamp01(dodgeElapsed / dodgeDuration) : 1f;
                float speedScale = progress < 0.70f ? 1f : Mathf.Lerp(1f, 0.25f, (progress - 0.70f) / 0.30f);
                rb.linearVelocity = new Vector3(dodgeRollDirection.x * dodgeSpeed * speedScale, rb.linearVelocity.y, dodgeRollDirection.z * dodgeSpeed * speedScale);
            }
            return;
        }

        // Auto-expire stale wall contact (guarantees zero-leak if colliders despawn or pooling occurs)
        if (isTouchingWall && (Time.fixedTime - lastWallContactTime > Time.fixedDeltaTime * 1.5f))
        {
            isTouchingWall = false;
            contactWallNormal = Vector3.zero;
        }

        // Saat jurus lompat menerjang (skill leap), biarkan momentum fisika menggerakkan tubuh maju
        if (isSkillLeaping)
        {
            return;
        }

        // Pertahankan komponen kecepatan vertikal (gravitasi/lompatan)
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 targetHorizontalVel = Vector3.zero;

        if (inputVector.magnitude >= 0.1f)
        {
            Vector3 moveDirection = GetCameraRelativeDirection(inputVector);
            float speedMultiplier = buffManager != null ? buffManager.GetSpeedMultiplier() : 1f;
            var weaponUpgrade = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance;
            if (weaponUpgrade != null && weaponUpgrade.sweetPotatoPathUnlocked)
            {
                speedMultiplier *= 1.10f; // Sweet Potato Path: +10% Movement Speed
            }

            float baseSpeed = isRunning ? runSpeed : walkSpeed;
            if (isAttacking)
            {
                baseSpeed = walkSpeed * attackMoveSpeedMultiplier;
            }
            float maxSpeed = baseSpeed * speedMultiplier;
            Vector3 desiredMove = moveDirection;

            // 1. Wall Sliding via kontak fisika aktif
            if (isTouchingWall && contactWallNormal.sqrMagnitude > 0.01f)
            {
                float dot = Vector3.Dot(desiredMove, contactWallNormal);
                if (dot < -0.01f) // Bergerak menabrak tembok
                {
                    Vector3 slide = Vector3.ProjectOnPlane(desiredMove, contactWallNormal);
                    slide.y = 0f;
                    if (slide.sqrMagnitude > 0.001f)
                        desiredMove = slide.normalized;
                }
            }

            // 2. Wall Sliding prediktif via CapsuleCast ke depan (mencegah tersendat sebelum kontak dengan tembok statis)
            if (playerCollider != null)
            {
                float halfHeight = Mathf.Max(playerCollider.height * 0.5f - playerCollider.radius, 0f);
                Vector3 p1 = transform.position + playerCollider.center + Vector3.up * halfHeight;
                Vector3 p2 = transform.position + playerCollider.center - Vector3.up * Mathf.Max(halfHeight - 0.1f, 0f);
                float radius = playerCollider.radius;
                float castDist = maxSpeed * Time.fixedDeltaTime + 0.15f;
                int wallLayerMask = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("Default"));

                // Pass 1: Deteksi dan defleksikan arah ke dinding pertama
                if (Physics.CapsuleCast(p1, p2, radius * 0.90f, desiredMove, out RaycastHit hit, castDist, wallLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != playerCollider && !hit.transform.IsChildOf(transform) && (hit.rigidbody == null || hit.rigidbody.isKinematic) && hit.distance > 0.01f)
                    {
                        float wallAngle = Vector3.Angle(hit.normal, Vector3.up);
                        if (wallAngle > 50f && wallAngle < 130f)
                        {
                            Vector3 hitNormal = hit.normal;
                            hitNormal.y = 0f;
                            hitNormal.Normalize();

                            float dot = Vector3.Dot(desiredMove, hitNormal);
                            if (dot < -0.01f)
                            {
                                Vector3 slide = Vector3.ProjectOnPlane(desiredMove, hitNormal);
                                slide.y = 0f;
                                if (slide.sqrMagnitude > 0.001f)
                                    desiredMove = slide.normalized;

                                // Pass 2: Jika berada di sudut (dua dinding bertemu), cek dinding kedua
                                if (Physics.CapsuleCast(p1, p2, radius * 0.90f, desiredMove, out RaycastHit hitCorner, castDist * 0.5f, wallLayerMask, QueryTriggerInteraction.Ignore))
                                {
                                    if (hitCorner.collider != playerCollider && !hitCorner.transform.IsChildOf(transform) && (hitCorner.rigidbody == null || hitCorner.rigidbody.isKinematic) && hitCorner.distance > 0.01f)
                                    {
                                        float cornerAngle = Vector3.Angle(hitCorner.normal, Vector3.up);
                                        if (cornerAngle > 50f && cornerAngle < 130f)
                                        {
                                            Vector3 cornerNormal = hitCorner.normal;
                                            cornerNormal.y = 0f;
                                            cornerNormal.Normalize();

                                            float dotCorner = Vector3.Dot(desiredMove, cornerNormal);
                                            if (dotCorner < -0.01f)
                                            {
                                                Vector3 cornerSlide = Vector3.ProjectOnPlane(desiredMove, cornerNormal);
                                                cornerSlide.y = 0f;
                                                desiredMove = cornerSlide.sqrMagnitude > 0.001f ? cornerSlide.normalized : Vector3.zero;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Penyesuaian kecepatan saat berputar tajam (>90 derajat) memberi kesan bobot inersia tubuh
            float angleDiff = Vector3.Angle(transform.forward, desiredMove);
            float turnSpeedFactor = 1f;
            if (angleDiff > 90f)
            {
                turnSpeedFactor = Mathf.Lerp(0.65f, 1f, (180f - angleDiff) / 90f);
            }

            targetHorizontalVel = desiredMove * (maxSpeed * turnSpeedFactor);

            // Step-Up: naik anak tangga otomatis tanpa invisible wall
            HandleStepUp(desiredMove);

            // Rotasi karakter menghadap arah pergerakan / sliding
            if (desiredMove.sqrMagnitude > 0.001f)
            {
                Vector3 lookEuler = Quaternion.LookRotation(desiredMove).eulerAngles;
                Quaternion targetRotation = Quaternion.Euler(0f, lookEuler.y, 0f);
                float activeTurnSpeed = isAttacking ? attackTurnSpeed : turnSpeed;
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, activeTurnSpeed * Time.fixedDeltaTime));
            }
        }

        // Terapkan akselerasi / deselerasi inersia yang halus
        float accelRate;
        if (isGrounded)
        {
            accelRate = (targetHorizontalVel.sqrMagnitude > 0.001f) ? acceleration : deceleration;
        }
        else
        {
            accelRate = airControl;
        }

        Vector3 newHorizontalVel = Vector3.MoveTowards(currentHorizontalVel, targetHorizontalVel, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newHorizontalVel.x, rb.linearVelocity.y, newHorizontalVel.z);

        // Redam sisa angular velocity fisik
        rb.angularVelocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        ProcessWallCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        ProcessWallCollision(collision);
    }

    private void ProcessWallCollision(Collision collision)
    {
        // Abaikan objek dinamis (musuh, proyektil, item loot) - sliding hanya untuk struktur tembok/lingkungan statis
        if (collision.collider == null || collision.collider.isTrigger) return;
        if (collision.rigidbody != null && !collision.rigidbody.isKinematic) return;
        if (collision.gameObject.GetComponent<FeaturesCombat.EnemyBase>() != null) return;

        Vector3 combinedNormal = Vector3.zero;
        int count = 0;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float angle = Vector3.Angle(normal, Vector3.up);
            if (angle > 50f && angle < 130f)
            {
                normal.y = 0f;
                combinedNormal += normal.normalized;
                count++;
            }
        }

        if (count > 0)
        {
            contactWallNormal = (combinedNormal / count).normalized;
            isTouchingWall = true;
            lastWallContactTime = Time.fixedTime;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isTouchingWall = false;
        contactWallNormal = Vector3.zero;
    }

}