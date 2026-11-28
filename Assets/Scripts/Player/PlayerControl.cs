using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem; // Keep this import
using FarmBeware.Core.Runtime;

[RequireComponent(typeof(Rigidbody))]
public partial class PlayerControl : MonoBehaviour, IPlayerContext
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float turnSpeed = 18f;

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

    [Header("Attack Movement Settings")]
    [Tooltip("Movement speed multiplier while swinging (e.g. 0.4 = 40% of walkSpeed).")]
    [Range(0.1f, 1.0f)]
    public float attackMoveSpeedMultiplier = 0.4f;

    [Tooltip("Character rotation speed during non-active attack startup/recovery.")]
    [Range(5f, 30f)]
    public float attackTurnSpeed = 14f;

    [Header("Special Skill Momentum (Leap)")]
    [Tooltip("Forward impulse speed during leap strike.")]
    public float leapForwardImpulse = 6.5f;

    [Tooltip("Upward impulse speed during leap strike.")]
    public float leapUpwardImpulse = 2.5f;

    [Header("Dodge & Deflect Settings")]
    [Tooltip("Dash velocity during dodge roll.")]
    public float dodgeSpeed = 6.5f;
    [Tooltip("Total physical duration of dodge roll (seconds).")]
    public float dodgeDuration = 0.62f;
    [Tooltip("Invulnerability frames (i-frames) duration during dodge roll (seconds).")]
    public float dodgeIFrameDuration = 0.32f;
    [Tooltip("Precision parry active window (seconds).")]
    public float parryWindow = 0.35f;
    [Tooltip("Stamina cost per dodge roll.")]
    public float dodgeStaminaCost = 15f;
    [Tooltip("Stamina cost per precision parry.")]
    public float parryStaminaCost = 10f;
    private bool isDodging = false;
    private Vector3 dodgeRollDirection = Vector3.forward;
    private float dodgeElapsed = 0f;
    public bool IsDodging => isDodging;
    public float DodgeProgress => (isDodging && dodgeDuration > 0.01f) ? Mathf.Clamp01(dodgeElapsed / dodgeDuration) : 0f;

    // Attack micro-lunge kinematics state driven deterministically in FixedUpdate
    private Vector3 _lungeDirection = Vector3.forward;
    private float _lungeSpeed = 0f;
    private float _lungeTimer = 0f;
    private float _lungeDuration = 0f;
    public float ActiveLungeSpeed => (_lungeTimer > 0f) ? _lungeSpeed : 0f;

    /// <summary>
    /// Applies a short directional micro-lunge synced with attack active frames (Zero-GC, resolved in FixedUpdate).
    /// </summary>
    public void ApplyAttackMicroLunge(Vector3 direction, float speed, float duration)
    {
        if (speed <= 0.01f || duration <= 0.005f) return;
        direction.y = 0f;
        _lungeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        _lungeSpeed = speed;
        _lungeDuration = duration;
        _lungeTimer = duration;
    }

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
    [SerializeField] private float acceleration = 42f;
    [SerializeField] private float deceleration = 40f;
    [SerializeField] private float airControl = 14f;

    // Internal state
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

        // Apply frictionless material for smooth movement and wall sliding
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
        // Guard: ensure inputActions exists even if OnEnable runs before Awake.
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        inputActions.Player.Enable();

        // Register input action callbacks
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Interact.performed += OnInteractPressed;
    }

    void OnDisable()
    {
        // Null guard in case OnEnable failed or ran out of order.
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        // Unregister input callbacks to prevent memory leaks
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
        // Input lock: halt inventory, hotbar, movement, and animator while locked.
        if (isInputLocked)
        {
            if (isAttacking || isSkillLeaping || isChargingAttack || hasBufferedAttack)
            {
                CancelActiveCombatAction();
            }

            // Allow Tab / I to close the inventory UI while it is open
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

            // Ensure velocity is zeroed and animator stays idle while UI is open
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

        // Hold position while planting or watering (blocks movement/sprint/jump/interact)
        // EXCEPT when movement input (WASD) is pressed to immediately cancel the animation.
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

        // 1. Check grounded state
        CheckGrounded();
        if (isGrounded)
        {
            groundedGraceTimer = 0.1f;
        }
        else if (groundedGraceTimer > 0f)
        {
            groundedGraceTimer -= Time.deltaTime;
        }

        // 2. Read movement input (deadzone check prevents micro-drift from normalizing random angles)
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

        // 3. Defensive actions (Dodge Roll & Parry) - usable while moving, idle, or canceling attacks
        HandleDefensiveInput();

        // 4. Attack input (supports combo buffering, early cancel, and branching heavy finishers mid-swing)
        HandleAttackInput();

        // Block non-combat actions while executing an attack
        if (!isAttacking)
        {
            HandleInventoryInput();
            HandleHotbarInput();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TriggerInteract();
            }
        }

        // 3. Check Shift key for sprinting
        bool isMoving = inputVector.magnitude >= 0.1f;
        bool wantsToRun = !isAttacking && Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        // Sprint only when moving, holding Shift, having stamina, and grounded (grace buffer prevents slope stutter)
        bool hasStamina = playerStats == null || (!playerStats.IsExhausted && playerStats.currentStamina > 0.5f);
        isRunning = isMoving && wantsToRun && hasStamina && isGroundedRecently;

        // 4. Drain stamina ONLY while sprinting and physically moving
        if (isRunning)
        {
            sprintDuration += Time.deltaTime;
            bool hasPhysicalMovement = rb != null && Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).sqrMagnitude > 0.04f;

            // ~0.15s sprint grace period keeps initial takeoff from standstill responsive;
            // after 0.15s, require physical movement so stamina does not drain when blocked by walls/colliders.
            if ((sprintDuration <= 0.15f || hasPhysicalMovement) && playerStats != null)
            {
                playerStats.UseStamina(playerStats.staminaDrainRate * Time.deltaTime);
            }
        }
        else
        {
            sprintDuration = 0f;
        }

        // 5. Synchronize Animator parameters naturally
        if (animator != null)
        {
            animator.SetBool("IsAttacking", isAttacking);
            if (isAttacking)
            {
                animator.SetBool("Sprinting", false);
                animator.SetBool("Grounded", true); // Keep Grounded true while attacking so AirBorn does not interrupt the swing
                animator.SetFloat("Vel", isMoving ? 0.3f : 0f);
            }
            else
            {
                isInAttackRecovery = false;
                if (HasAnimatorParameter(animator, "InAttackRecovery", AnimatorControllerParameterType.Bool))
                    animator.SetBool("InAttackRecovery", false);

                // Use actual horizontal speed so footsteps stay synchronized without sliding
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

                // Use 0.12f dampTime for smooth speed and footstep transitions
                animator.SetFloat("Vel", targetSpeed, 0.12f, Time.deltaTime);
                animator.SetBool("Grounded", isGrounded);
                // Idle active when there is no movement input and body velocity has slowed down
                bool isIdleNow = !isMoving && currentSpeed < 0.25f;
                animator.SetBool("Idle", isIdleNow);

                // Sync weapon-held state (optional, only if parameter exists in Animator Controller)
                bool hasWeapon = (playerEquipment != null && playerEquipment.IsHoldingWeapon);
                if (HasAnimatorParameter(animator, "HasWeapon", AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool("HasWeapon", hasWeapon);
                }

                // Idle fidget: ONLY when holding a weapon and idle for > 9 seconds
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

    // Step-Up: smoothly lift the character over low stairs without invisible walls
    private void HandleStepUp(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f || playerCollider == null || rb == null) return;
        if (!isGrounded) return;

        float radius = playerCollider.radius;
        float checkDist = radius + 0.2f;

        // 1. Check for a low obstacle ahead near feet (< stepUpHeight)
        Vector3 lowOrigin = transform.position + Vector3.up * 0.05f;
        if (!Physics.Raycast(lowOrigin, moveDir, out RaycastHit lowHit, checkDist,
            ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            return;

        // Ensure obstacle is low enough (not a tall wall)
        float obstacleTopY = lowHit.collider.bounds.max.y;
        float stepDelta = obstacleTopY - transform.position.y;
        if (stepDelta <= 0f || stepDelta > stepUpHeight) return;

        // 2. Check for sufficient headroom above the step
        Vector3 aboveStepOrigin = transform.position + Vector3.up * (stepDelta + 0.05f) + moveDir * checkDist;
        if (Physics.CheckSphere(aboveStepOrigin, radius * 0.9f,
            ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            return;

        // 3. Smoothly lift player onto the step
        float targetY = transform.position.y + stepDelta;
        float newY = Mathf.MoveTowards(rb.position.y, targetY, stepUpSpeed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector3(rb.position.x, newY, rb.position.z));
        // Reset downward velocity during step-up to prevent bouncing
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

        void FixedUpdate()
    {
        // Input lock: halt movement physics when locked or planting seeds
        if (isInputLocked || isPlanting)
        {
            if (rb != null && !rb.isKinematic)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // During dodge roll, drive rigidbody along dodgeRollDirection with smooth deceleration in the final 30%
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

        // During skill leap, let physics momentum carry the character forward
        if (isSkillLeaping)
        {
            return;
        }

        // During attack micro-lunge, drive smooth tapering forward momentum
        if (_lungeTimer > 0f && rb != null)
        {
            _lungeTimer = Mathf.Max(0f, _lungeTimer - Time.fixedDeltaTime);
            float ratio = (_lungeDuration > 0.005f) ? (_lungeTimer / _lungeDuration) : 0f;
            float speedScale = Mathf.Lerp(0.25f, 1.0f, ratio);
            Vector3 lungeVel = _lungeDirection * (_lungeSpeed * speedScale);
            rb.linearVelocity = new Vector3(lungeVel.x, rb.linearVelocity.y, lungeVel.z);
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // Preserve vertical velocity component (gravity/jump)
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

            // 1. Wall sliding via active physics contact
            if (isTouchingWall && contactWallNormal.sqrMagnitude > 0.01f)
            {
                float dot = Vector3.Dot(desiredMove, contactWallNormal);
                if (dot < -0.01f) // Moving into a wall
                {
                    Vector3 slide = Vector3.ProjectOnPlane(desiredMove, contactWallNormal);
                    slide.y = 0f;
                    if (slide.sqrMagnitude > 0.001f)
                        desiredMove = slide.normalized;
                }
            }

            // 2. Predictive wall sliding via forward CapsuleCast (prevents snagging before static wall contact)
            if (playerCollider != null)
            {
                float halfHeight = Mathf.Max(playerCollider.height * 0.5f - playerCollider.radius, 0f);
                Vector3 p1 = transform.position + playerCollider.center + Vector3.up * halfHeight;
                Vector3 p2 = transform.position + playerCollider.center - Vector3.up * Mathf.Max(halfHeight - 0.1f, 0f);
                float radius = playerCollider.radius;
                float castDist = maxSpeed * Time.fixedDeltaTime + 0.15f;
                int wallLayerMask = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("Default"));

                // Pass 1: Detect and deflect movement along the first wall
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

                                // Pass 2: Check for a second wall when wedged in a corner
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

            // Dampen speed on sharp turns (>90 degrees) for grounded rotational inertia
            float angleDiff = Vector3.Angle(transform.forward, desiredMove);
            float turnSpeedFactor = 1f;
            if (angleDiff > 90f)
            {
                turnSpeedFactor = Mathf.Lerp(0.65f, 1f, (180f - angleDiff) / 90f);
            }

            targetHorizontalVel = desiredMove * (maxSpeed * turnSpeedFactor);

            // Step-Up: automatically climb low steps without invisible walls
            HandleStepUp(desiredMove);

            // Rotate character toward movement or wall-slide direction (restrict turning during active swing frames for commitment)
            if (desiredMove.sqrMagnitude > 0.001f)
            {
                Vector3 lookEuler = Quaternion.LookRotation(desiredMove).eulerAngles;
                Quaternion targetRotation = Quaternion.Euler(0f, lookEuler.y, 0f);
                bool inActiveSwing = isAttacking && playerEquipment != null && playerEquipment.IsInActiveSwingWindow;
                float activeTurnSpeed = inActiveSwing ? 3f : (isAttacking ? attackTurnSpeed : turnSpeed);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, activeTurnSpeed * Time.fixedDeltaTime));
            }
        }

        // Apply smooth inertial acceleration / deceleration
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

        // Dampen residual angular velocity
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
        // Ignore dynamic objects (enemies, projectiles, loot) - wall sliding applies only to static walls/environment geometry
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