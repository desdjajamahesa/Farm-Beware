using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem; // Pastikan ini tetap ada
using FarmBeware.Core.Runtime;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour, IPlayerContext
{
    [Header("Pengaturan Pergerakan")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float turnSpeed = 15f;

    [Header("Pengaturan Aksi")]
    public float jumpForce = 5f;
    [Tooltip("Durasi penguncian pergerakan saat menanam benih (detik).")]
    public float plantDuration = 1.56f;
    private bool isPlanting = false;

    [Tooltip("Durasi karakter diam di tempat saat mengayunkan serangan pedang (detik).")]
    public float attackLockDuration = 1.1f;
    private bool isAttacking = false;
    private bool isSkillLeaping = false;
    private bool isLightAttacking = false;
    private bool hasBufferedAttack = false;
    private float bufferedAttackTime = 0f;
    private Coroutine attackCoroutine = null;
    private float idleFidgetTimer = 0f;
    public bool IsAttacking => isAttacking;

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
    public float dodgeSpeed = 10f;
    [Tooltip("Durasi gerakan dodge roll (detik).")]
    public float dodgeDuration = 0.35f;
    [Tooltip("Durasi frame kekebalan (i-frames) selama dodge roll (detik).")]
    public float dodgeIFrameDuration = 0.30f;
    [Tooltip("Durasi window tangkisan parry (detik). Standar 0.35s = 350ms agar timing terasa pas dan adil.")]
    public float parryWindow = 0.35f;
    [Tooltip("Konsumsi stamina saat dodge roll.")]
    public float dodgeStaminaCost = 15f;
    [Tooltip("Konsumsi stamina saat melakukan precision parry.")]
    public float parryStaminaCost = 10f;
    private bool isDodging = false;
    private Vector3 dodgeRollDirection = Vector3.forward;
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
    private float attackHoldDuration = 0f;
    private bool isChargingAttack = false;

    // Wall slide helpers
    private Vector3 contactWallNormal = Vector3.zero;
    private bool isTouchingWall = false;
    private float lastWallContactTime = -10f;
    private float sprintDuration = 0f;

    // Kunci input global: saat true, pemain tidak bisa bergerak, membuka
    // inventori, melompat, dash, atau berinteraksi (dipakai mode Trophy, dst).
    public bool isInputLocked = false;

    #region IPlayerContext Implementation
    public Transform Transform => transform;
    bool IPlayerContext.IsInputLocked { get => isInputLocked; set => isInputLocked = value; }
    public bool IsGodMode => playerStats != null && playerStats.isGodMode;
    public void PlayAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
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

    private void OnJumpPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => ExecuteJump();

    void Update()
    {
        // Kunci input: hentikan inventory/hotbar/gerak/animator saat terkunci.
        if (isInputLocked)
        {
            // Pastikan velocity benar-benar nol dan animasi idle saat UI terbuka
            inputVector = Vector3.zero;
            isRunning = false;
            if (rb != null)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (animator != null)
            {
                animator.SetFloat("Vel", 0f, 0.05f, Time.deltaTime);
                animator.SetBool("Idle", true);
                animator.SetBool("Sprinting", false);
            }
            return;
        }

        // Saat menanam benih, karakter diam di tempat (tidak bisa bergerak/berlari/lompat/interact)
        if (isPlanting)
        {
            inputVector = Vector3.zero;
            isRunning = false;
            if (animator != null)
            {
                animator.SetFloat("Vel", 0f, 0.1f, Time.deltaTime);
                animator.SetBool("Idle", true);
                animator.SetBool("Sprinting", false);
            }
            return;
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

        // Input aksi lain diblokir saat sedang mengeksekusi serangan
        if (!isAttacking)
        {
            HandleInventoryInput();
            HandleHotbarInput();
            HandleAttackInput();

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

                // Sinkronisasi status memegang senjata
                bool hasWeapon = (playerEquipment != null && playerEquipment.IsHoldingWeapon);
                animator.SetBool("HasWeapon", hasWeapon);

                // Idle fidget: HANYA jika sedang memegang senjata dan diam lebih dari 9 detik
                if (isIdleNow && hasWeapon)
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
            if (rb != null)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // Saat dodge roll aktif, dorong rigidbody ke arah dodgeRollDirection secara konsisten
        if (isDodging)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(dodgeRollDirection.x * dodgeSpeed, rb.linearVelocity.y, dodgeRollDirection.z * dodgeSpeed);
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

    // --- LOGIKA AKSI ---

    // Klik Kiri Mouse / Tombol F: Serangan Kombo Biasa (3-Hit Combo) / Dash Attack saat berlari / Heavy Strike jika ditahan
    // Klik Kanan Mouse / Tombol R: Jurus Spesial (Leap Strike)
    private void HandleAttackInput()
    {
        if (isInputLocked || isPlanting)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            return;
        }

        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // 1. Serangan Normal / Dash / Heavy
        bool leftPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isPointerOverUI;
        bool fPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        bool attackPressed = leftPressed || fPressed;

        bool leftHeld = Mouse.current != null && Mouse.current.leftButton.isPressed && !isPointerOverUI;
        bool fHeld = Keyboard.current != null && Keyboard.current.fKey.isPressed;
        bool attackHeld = leftHeld || fHeld;

        bool leftReleased = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        bool fReleased = Keyboard.current != null && Keyboard.current.fKey.wasReleasedThisFrame;
        bool attackReleased = leftReleased || fReleased;

        // 2. Jurus Spesial (Leap Strike): Right Click atau Tombol R
        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !isPointerOverUI;
        bool rKey = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        // 3. Skill Tendangan Spartan (Knockback Kick): Tombol Q
        bool qKey = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        // 4. Selebrasi / Battlecry Emote: Tombol T
        bool tKey = Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;

        if (playerEquipment == null)
            playerEquipment = GetComponent<PlayerEquipment>() ?? gameObject.AddComponent<PlayerEquipment>();

        // Priority Special Actions (Kick, Leap, Taunt)
        if (qKey && !isSkillLeaping)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            if (playerEquipment != null && playerEquipment.TryPerformKick())
            {
                if (attackCoroutine != null) { StopCoroutine(attackCoroutine); attackCoroutine = null; }
                StartCoroutine(RoutineKick());
            }
            return;
        }
        else if (tKey && !isAttacking)
        {
            if (animator != null && isGrounded)
            {
                animator.ResetTrigger("Taunt");
                animator.SetTrigger("Taunt");
            }
            return;
        }
        else if ((rightClick || rKey) && !isSkillLeaping)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            if (playerEquipment != null && playerEquipment.TryPerformSkillAttack())
            {
                if (attackCoroutine != null) { StopCoroutine(attackCoroutine); attackCoroutine = null; }
                StartCoroutine(RoutineSkillAttack());
            }
            return;
        }

        // If performing heavy attack, kick, skill leap, or dash attack, block light attacks
        if (isAttacking && !isLightAttacking)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            return;
        }

        // --- COMBO ADVANCEMENT / BUFFERING DURING LIGHT ATTACK ---
        if (isLightAttacking)
        {
            if (attackPressed || attackHeld)
            {
                if (playerEquipment != null && playerEquipment.TryPerformAttack())
                {
                    StartLightAttack();
                    hasBufferedAttack = false;
                }
                else if (attackPressed)
                {
                    hasBufferedAttack = true;
                    bufferedAttackTime = Time.time;
                }
            }
            else if (hasBufferedAttack)
            {
                if (Time.time - bufferedAttackTime > 0.45f)
                {
                    hasBufferedAttack = false;
                }
                else if (playerEquipment != null && playerEquipment.TryPerformAttack())
                {
                    StartLightAttack();
                    hasBufferedAttack = false;
                }
            }
            return;
        }

        // Consume buffered attack right after returning to Idle
        if (hasBufferedAttack)
        {
            hasBufferedAttack = false;
            if (Time.time - bufferedAttackTime <= 0.45f && playerEquipment != null && playerEquipment.TryPerformAttack())
            {
                StartLightAttack();
                return;
            }
        }

        // --- Standard Melee Attack Processing (Light Combo / Dash Attack / Charged Heavy) ---
        if (attackPressed || (attackHeld && !isChargingAttack && !isAttacking))
        {
            // If running/sprinting at high speed, trigger instantaneous Dash Attack!
            if (attackPressed && isRunning && inputVector.sqrMagnitude >= 0.01f)
            {
                if (playerEquipment != null && playerEquipment.TryPerformDashAttack())
                {
                    StartCoroutine(RoutineDashAttack());
                    isChargingAttack = false;
                    attackHoldDuration = 0f;
                    hasBufferedAttack = false;
                    return;
                }
            }

            isChargingAttack = true;
            attackHoldDuration = 0f;
            if (playerEquipment != null)
            {
                playerEquipment.CombatStateMachine.StartHeavyCharge(Time.time);
            }
        }

        if (isChargingAttack)
        {
            if (attackHeld)
            {
                attackHoldDuration += Time.deltaTime;
                if (playerEquipment != null)
                {
                    playerEquipment.CombatStateMachine.UpdateCharge(Time.deltaTime);
                }
            }

            if (attackReleased)
            {
                isChargingAttack = false;
                if (attackHoldDuration >= 0.35f)
                {
                    // Released after charging -> Heavy Attack!
                    float chargeRatio = (playerEquipment != null) ? playerEquipment.CombatStateMachine.ChargeRatio : 1f;
                    if (playerEquipment != null && playerEquipment.TryPerformHeavyAttack(chargeRatio))
                    {
                        StartCoroutine(RoutineHeavyAttack());
                    }
                }
                else
                {
                    // Released quickly -> Standard 3-Hit Combo Light Attack!
                    if (playerEquipment != null)
                    {
                        playerEquipment.CombatStateMachine.ResetToIdle();
                    }
                    if (playerEquipment != null && playerEquipment.TryPerformAttack())
                    {
                        StartLightAttack();
                    }
                }
                attackHoldDuration = 0f;
            }
        }
    }

    private IEnumerator RoutineDashAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        if (rb != null)
        {
            Vector3 forwardDir = transform.forward;
            rb.linearVelocity = forwardDir * 7.5f + Vector3.up * 0.1f;
        }

        yield return null;

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1.6f;
        float lockDuration = 0.36f / atkSpeed;
        yield return new WaitForSeconds(lockDuration);

        isAttacking = false;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private IEnumerator RoutineHeavyAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        float atkSpeed = (animator != null && animator.speed > 0.1f) ? animator.speed : ((playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f);

        // Windup anticipation delay matching the 360 upward blade lift
        float windupWait = 0.42f / atkSpeed;
        float elapsedWindup = 0f;
        while (elapsedWindup < windupWait)
        {
            elapsedWindup += Time.deltaTime;
            yield return null;
        }

        // Forward impulse burst synchronized with active 360 blade release
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * 4.2f + Vector3.up * 0.1f;
        }

        float maxLock = 0.82f / atkSpeed;
        float timer = elapsedWindup;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private void StartLightAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        attackCoroutine = StartCoroutine(RoutineAttack());
    }

    private IEnumerator RoutineAttack()
    {
        isAttacking = true;
        isLightAttacking = true;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        // Tunggu satu frame agar transisi animator ke state attack dimulai
        yield return null;

        float atkSpeed = (animator != null && animator.speed > 0.1f) ? animator.speed : ((playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f);
        float maxLock = attackLockDuration / atkSpeed;
        float minLock = 0.20f / atkSpeed;

        float timer = 0f;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;

            if (hasBufferedAttack && playerEquipment != null)
            {
                if (Time.time - bufferedAttackTime > 0.45f)
                {
                    hasBufferedAttack = false;
                }
                else if (playerEquipment.TryPerformAttack())
                {
                    hasBufferedAttack = false;
                    StartLightAttack();
                    yield break;
                }
            }

            if (timer > minLock && animator != null)
            {
                var curr = animator.GetCurrentAnimatorStateInfo(0);
                var next = animator.GetNextAnimatorStateInfo(0);
                if (!IsAttackState(curr) && !IsAttackState(next))
                {
                    break;
                }
            }

            yield return null;
        }

        isAttacking = false;
        isLightAttacking = false;
        attackCoroutine = null;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private IEnumerator RoutineKick()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1.6f;

        Vector3 forwardDir = transform.forward;
        if (rb != null)
        {
            // Hentakan lunge maju seketika bersamaan dengan lesatan tendangan
            rb.linearVelocity = forwardDir * 4.2f + Vector3.up * 0.1f;
        }

        float lockDuration = 0.38f / atkSpeed;
        float elapsed = 0f;
        while (elapsed < lockDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        isAttacking = false;
        isLightAttacking = false;
        hasBufferedAttack = false;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private IEnumerator RoutineSkillAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        isSkillLeaping = true;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1.6f;

        Vector3 forwardDir = transform.forward;

        // Fase 1: Windup / ancang-ancang melompat (~0.25s / atkSpeed)
        float windupDuration = 0.25f / atkSpeed;
        float elapsed = 0f;
        while (elapsed < windupDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Berikan dorongan awal melompat ke udara
        if (rb != null)
        {
            rb.linearVelocity = forwardDir * leapForwardImpulse + Vector3.up * leapUpwardImpulse;
        }

        // Fase 2: Meluncur maju di udara hingga pedang menghantam tanah (~1.14s total / atkSpeed)
        float impactTime = 1.14f / atkSpeed;
        while (elapsed < impactTime)
        {
            elapsed += Time.deltaTime;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(forwardDir.x * leapForwardImpulse, rb.linearVelocity.y, forwardDir.z * leapForwardImpulse);
            }
            yield return new WaitForFixedUpdate();
        }

        // Fase 3: Mendarat & hantaman tanah (hentikan laju horizontal agar mendarat kokoh di titik hantaman)
        isSkillLeaping = false;
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // Fase 4: Recovery pasca hantaman dan transisi kembali ke Idle/Moving (~1.35s / atkSpeed)
        float totalLock = 1.35f / atkSpeed;
        while (elapsed < totalLock)
        {
            elapsed += Time.deltaTime;
            if (animator != null)
            {
                var curr = animator.GetCurrentAnimatorStateInfo(0);
                var next = animator.GetNextAnimatorStateInfo(0);
                if (!IsAttackState(curr) && !IsAttackState(next))
                {
                    break;
                }
            }
            yield return null;
        }

        isAttacking = false;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private bool IsAttackState(AnimatorStateInfo info)
    {
        return info.IsName("attack") ||
               info.IsName("Attack_Combo1") ||
               info.IsName("Attack_Combo2") ||
               info.IsName("Attack_Combo3") ||
               info.IsName("Attack_Skill") ||
               info.IsName("Attack_Kick");
    }


    /// <summary>
    /// Memanggil animasi menanam secara kontekstual (mis. saat berinteraksi dengan FarmlandTile).
    /// </summary>
    public void TriggerPlantAnimation()
    {
        if (isPlanting || isAttacking) return;
        StartCoroutine(RoutinePlantSeed());
    }

    /// <summary>
    /// Memanggil animasi memanen/mengambil tanaman dari tanah.
    /// </summary>
    public void TriggerHarvestAnimation()
    {
        if (isPlanting || isAttacking) return;
        StartCoroutine(RoutinePlantSeed());
    }

    private IEnumerator RoutinePlantSeed()
    {
        isPlanting = true;
        inputVector = Vector3.zero;

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetTrigger("PlantSeed");
            animator.SetFloat("Vel", 0f);
            animator.SetBool("Idle", true);
            animator.SetBool("Sprinting", false);
        }

        // Tunggu frame berikutnya agar transisi animator ke PlantSeed dimulai
        yield return null;

        float timer = 0f;
        while (timer < plantDuration)
        {
            timer += Time.deltaTime;

            // Jika animasi sudah selesai dan bertransisi kembali ke Idle/Moving setelah minimal 0.5 detik
            if (timer > 0.5f && animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName("PlantSeed") && !animator.GetNextAnimatorStateInfo(0).IsName("PlantSeed"))
                {
                    break;
                }
            }

            yield return null;
        }

        isPlanting = false;
    }

    // Tombol Tab / I membuka-menutup panel pemain. Jika storage terbuka, tutup semua.
    private void HandleInventoryInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Guard: jangan buka inventory jika sedang Trophy Mode
        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
            return;

        if (keyboard.tabKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
        {
            if (InventoryManagerUI.Instance != null)
                InventoryManagerUI.Instance.TogglePlayerInventory();
        }
    }

    // Seleksi Hotbar: angka 1-4 + scroll mouse (wrapping).
    private void HandleHotbarInput()
    {
        if (playerInventory == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(3);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f)
            {
                int idx = playerInventory.selectedHotbarIndex - 1;
                if (idx < 0) idx = 3;
                playerInventory.SelectHotbarSlot(idx);
            }
            else if (scroll < 0f)
            {
                int idx = playerInventory.selectedHotbarIndex + 1;
                if (idx > 3) idx = 0;
                playerInventory.SelectHotbarSlot(idx);
            }
        }
    }

    private void ExecuteJump()
    {
        if (isInputLocked || isPlanting || isAttacking) return;

        // Hanya bisa lompat jika menginjak tanah
        if (isGrounded)
        {
            isGrounded = false;
            isJumping = true;
            groundBufferTimer = 0f;
            if (animator != null) animator.SetBool("Grounded", false);

            // Reset kecepatan Y agar lompatan konsisten, lalu dorong ke atas
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Translates raw 2D input (X = horizontal, Z = vertical) into isometric camera-relative world direction.
    /// Preserves analog stick magnitude instead of blanket normalization.
    /// </summary>
    public static Vector3 GetCameraRelativeDirection(Vector3 rawInput)
    {
        float inputMag = Mathf.Clamp01(rawInput.magnitude);
        if (inputMag < 0.001f) return Vector3.zero;

        Vector3 rawDir = rawInput / inputMag;

        if (Camera.main != null)
        {
            Vector3 camFwd = Camera.main.transform.forward;
            camFwd.y = 0f;
            camFwd.Normalize();

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 worldDir = camFwd * rawDir.z + camRight * rawDir.x;
            if (worldDir.sqrMagnitude > 0.001f)
            {
                return worldDir.normalized * inputMag;
            }
            return Vector3.zero;
        }

        // Standard 45-degree isometric projection fallback
        return (Quaternion.Euler(0f, 45f, 0f) * rawDir).normalized * inputMag;
    }

    private void HandleDefensiveInput()
    {
        if (isInputLocked || isPlanting || isDodging) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // 1. Dodge Roll: Left Alt, C key
        bool dodgeTriggered = kb.leftAltKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame;
        if (dodgeTriggered)
        {
            PerformDodgeRoll();
            return;
        }

        // 2. Precision Deflect / Parry: V key or Left Ctrl key
        bool parryTriggered = kb.vKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame;
        if (parryTriggered)
        {
            PerformParry();
            return;
        }
    }

    public void PerformDodgeRoll()
    {
        if (isDodging || isPlanting || isInputLocked) return;

        // Allow dodge-canceling if attacking and reached cancelable window (normalizedTime >= 0.25f)
        if (isAttacking)
        {
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.normalizedTime >= 0.25f)
                {
                    if (attackCoroutine != null)
                    {
                        StopCoroutine(attackCoroutine);
                        attackCoroutine = null;
                    }
                    isAttacking = false;
                    isLightAttacking = false;
                    isChargingAttack = false;
                    playerEquipment?.CombatStateMachine?.InterruptCombatSequence();
                    if (animator != null) animator.SetBool("IsAttacking", false);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (playerStats != null && !playerStats.UseStamina(dodgeStaminaCost))
        {
            return;
        }

        // Read live movement input
        Vector2 move = inputActions != null ? inputActions.Player.Move.ReadValue<Vector2>() : Vector2.zero;
        Vector3 rawMove = move.sqrMagnitude > 0.01f ? new Vector3(move.x, 0f, move.y).normalized : inputVector;

        // Resolve true camera-relative direction in world space
        Vector3 rollDir;
        if (rawMove.sqrMagnitude > 0.01f)
        {
            rollDir = GetCameraRelativeDirection(rawMove);
        }
        else
        {
            rollDir = transform.forward;
        }

        rollDir.y = 0f;
        if (rollDir.sqrMagnitude < 0.001f)
        {
            rollDir = transform.forward;
        }
        else
        {
            rollDir.Normalize();
        }

        StartCoroutine(RoutineDodgeRoll(rollDir));
    }

    private IEnumerator RoutineDodgeRoll(Vector3 rollDir)
    {
        isDodging = true;
        dodgeRollDirection = rollDir;

        // Instantly align character rotation with roll direction
        Quaternion targetRot = Quaternion.LookRotation(rollDir, Vector3.up);
        transform.rotation = targetRot;
        if (rb != null)
        {
            rb.rotation = targetRot;
            rb.linearVelocity = new Vector3(rollDir.x * dodgeSpeed, rb.linearVelocity.y, rollDir.z * dodgeSpeed);
        }

        // Trigger i-Frames on DefenseEvaluator
        var mods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
        if (playerStats != null)
        {
            playerStats.DefenseEvaluator.DodgeIFrameDuration = dodgeIFrameDuration;
            playerStats.DefenseEvaluator.TriggerDodge(mods.ExtraDodgeDuration);
        }

        if (animator != null)
        {
            animator.SetBool("Sliding", true);
        }

        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            animator.SetBool("Sliding", false);
        }

        isDodging = false;
    }

    public void PerformParry()
    {
        if (isDodging || isPlanting || isInputLocked) return;

        // Allow parry-canceling if attacking and reached cancelable window
        if (isAttacking)
        {
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.normalizedTime >= 0.20f)
                {
                    if (attackCoroutine != null)
                    {
                        StopCoroutine(attackCoroutine);
                        attackCoroutine = null;
                    }
                    isAttacking = false;
                    isLightAttacking = false;
                    isChargingAttack = false;
                    playerEquipment?.CombatStateMachine?.InterruptCombatSequence();
                    if (animator != null) animator.SetBool("IsAttacking", false);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        // Reject parry if already parrying or currently in whiff lockout
        if (playerStats != null && !playerStats.DefenseEvaluator.CanInitiateParry)
        {
            return;
        }

        float effectiveStaminaCost = parryStaminaCost;
        if (playerStats != null && playerStats.currentStamina < 20f)
        {
            // Low stamina penalty: costs 1.5x to prevent low-stamina parry cheese
            effectiveStaminaCost *= 1.5f;
        }

        if (playerStats != null && !playerStats.UseStamina(effectiveStaminaCost))
        {
            return;
        }

        var mods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
        if (playerStats != null)
        {
            playerStats.DefenseEvaluator.ParryWindow = parryWindow;
            playerStats.DefenseEvaluator.TriggerParry(mods.ExtraParryWindow);
        }

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }

        if (PlayerUI.FloatingCombatTextManager.Instance != null)
        {
            PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                transform.position + Vector3.up * 1.5f,
                "GUARD",
                new Color(0.8f, 0.8f, 1f));
        }
    }

    private int lastInteractFrame = -1;

    /// <summary>
    /// Menghentikan gerakan pemain secara paksa.
    /// Dipanggil saat memulai interaksi agar pemain tidak sliding/bergerak.
    /// </summary>
    public void StopMovement()
    {
        inputVector = Vector3.zero;
        isRunning = false;
        if (rb != null)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (animator != null)
        {
            animator.SetFloat("Vel", 0f);
            animator.SetBool("Idle", true);
            animator.SetBool("Sprinting", false);
        }
    }

    public void TriggerInteract()
    {
        if (Time.frameCount == lastInteractFrame || isPlanting || isAttacking) return;
        lastInteractFrame = Time.frameCount;

        if (isInputLocked)
        {
            Debug.LogWarning("[PlayerControl] Tombol E ditekan tetapi isInputLocked = true!");
            return;
        }

        // Pastikan skrip interactor tidak hilang/error
        if (interactor == null)
            interactor = GetComponent<PlayerInteractor>();

        if (interactor != null)
        {
            // Hentikan gerakan pemain sebelum memulai interaksi
            StopMovement();
            // Perintahkan "Tangan" untuk menjalankan logikanya
            interactor.OnInteractInput(); 
        }
        else
        {
            Debug.LogError("[PlayerControl] PlayerInteractor tidak ditemukan pada Player!");
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        TriggerInteract();
    }


    private void CheckGrounded()
    {
        // Jika sedang fase awal lompat (bergerak ke atas karena dorongan lompat), abaikan ground check
        if (isJumping)
        {
            if (rb != null && rb.linearVelocity.y <= 0f)
            {
                // Sudah mencapai puncak loncatan dan mulai jatuh
                isJumping = false;
            }
            else
            {
                isGrounded = false;
                groundBufferTimer = 0f;
                return;
            }
        }

        // SphereCast ke bawah untuk mendeteksi tanah secara akurat di tanjakan/tangga
        // Mulai sedikit di atas kaki karakter (y = 0.25f), radius 0.2f, jarak cast 0.25f
        float sphereRadius = 0.2f;
        Vector3 origin = transform.position + Vector3.up * (sphereRadius + 0.05f);
        float castDistance = 0.25f;

        bool hitGround = Physics.SphereCast(
            origin,
            sphereRadius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            ~LayerMask.GetMask("Ignore Raycast"),
            QueryTriggerInteraction.Ignore
        );

        // Abaikan jika collider yang terkena adalah collider diri sendiri
        if (hitGround && playerCollider != null && hit.collider == playerCollider)
        {
            hitGround = false;
        }

        if (hitGround)
        {
            isGrounded = true;
            groundBufferTimer = GroundBufferDuration;
        }
        else
        {
            if (groundBufferTimer > 0f)
            {
                groundBufferTimer -= Time.deltaTime;
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
    }

    #region Boss Attack Impact Helpers (Stun & Control Lock)

    private Coroutine stunCoroutine;

    /// <summary>
    /// Memberikan efek stun sementara pada pemain (misal: akibat bantingan grapple bos).
    /// Dilengkapi safety timeout coroutine agar pemain tidak pernah terkunci selamanya.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }
        stunCoroutine = StartCoroutine(RoutineStun(duration));
    }

    private System.Collections.IEnumerator RoutineStun(float duration)
    {
        StopMovement();
        isInputLocked = true;

        if (PlayerUI.FloatingCombatTextManager.Instance != null)
        {
            PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                transform.position + Vector3.up * 1.8f,
                "⚡ STUNNED!",
                new Color(1f, 0.85f, 0.2f));
        }

        yield return new WaitForSeconds(duration);

        isInputLocked = false;
        stunCoroutine = null;
    }

    /// <summary>
    /// Menerapkan dorongan knockback fisika pada pemain.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (rb != null)
        {
            direction.y = 0.2f;
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Mengunci / membuka input kontrol pemain secara eksplisit (misal: saat diangkat bos).
    /// </summary>
    public void SetControlLock(bool locked)
    {
        if (locked)
        {
            StopMovement();
            isInputLocked = true;
        }
        else
        {
            isInputLocked = false;
            if (stunCoroutine != null)
            {
                StopCoroutine(stunCoroutine);
                stunCoroutine = null;
            }
        }
    }

    /// <summary>
    /// Helper eksplisit untuk mengaktifkan / menonaktifkan input kontrol pemain.
    /// </summary>
    public void SetInputActive(bool active) => SetControlLock(!active);

    #endregion

    /// <summary>
    /// Teleports the player to a target position and rotation, resetting physics velocity and syncing transforms.
    /// Used by the Save/Load system and scene transitions.
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        StopMovement();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
        }
        transform.position = position;
        transform.rotation = rotation;
        Physics.SyncTransforms();
    }

    int IPlayerContext.Health => playerStats != null ? playerStats.currentHealth : 100;
    float IPlayerContext.Stamina => playerStats != null ? playerStats.currentStamina : 100f;
    float IPlayerContext.Hunger => playerStats != null ? playerStats.currentHunger : 100f;
    float IPlayerContext.Thirst => playerStats != null ? playerStats.currentThirst : 100f;

    void IPlayerContext.RestoreStats(int health, float stamina, float hunger, float thirst)
    {
        if (playerStats != null)
        {
            playerStats.RestoreStats(health, stamina, hunger, thirst);
        }
    }

    void IPlayerContext.UpdateEquipmentVisual(int slotIndex)
    {
        var equip = GetComponent<PlayerEquipment>();
        if (equip != null)
        {
            equip.UpdateEquipmentVisual(slotIndex);
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IPlayerContext>();
    }
}