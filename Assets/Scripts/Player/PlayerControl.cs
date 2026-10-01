using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem; // Pastikan ini tetap ada

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
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

    // Wall slide helpers
    private Vector3 contactWallNormal = Vector3.zero;
    private bool isTouchingWall = false;
    private readonly System.Collections.Generic.HashSet<Collider> activeWallColliders = new System.Collections.Generic.HashSet<Collider>();

    // Kunci input global: saat true, pemain tidak bisa bergerak, membuka
    // inventori, melompat, dash, atau berinteraksi (dipakai mode Trophy, dst).
    public bool isInputLocked = false;

    void Awake()
    {
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
        inputActions.Player.Jump.performed += ctx => ExecuteJump();
        inputActions.Player.Interact.performed += OnInteractPressed;
    }

    void OnDisable()
    {
        // Guard null agar OnDisable aman saat OnEnable gagal/urutan tidak menentu.
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        // Mencabut pendaftaran event untuk mencegah memory leak
        inputActions.Player.Jump.performed -= ctx => ExecuteJump();

        inputActions.Player.Disable();

        inputActions.Player.Interact.performed -= OnInteractPressed;
    }

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

        // 1. Cek apakah karakter menginjak tanah
        CheckGrounded();

        // 2. Membaca Input Pergerakan (Deadzone check agar micro-drift tidak menormalkan sudut acak)
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
        {
            inputVector = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
        else
        {
            inputVector = Vector3.zero;
        }

        // 3. Cek apakah pemain menahan tombol Shift untuk Lari (Sprint)
        bool isMoving = inputVector.magnitude >= 0.1f;
        bool wantsToRun = !isAttacking && Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        // Karakter hanya berlari jika bergerak, menekan shift, dan memiliki stamina
        isRunning = isMoving && wantsToRun && (playerStats == null || !playerStats.IsExhausted);

        // 4. Konsumsi Stamina HANYA saat Berlari (Sprint)
        if (isRunning && playerStats != null)
        {
            playerStats.UseStamina(playerStats.staminaDrainRate * Time.deltaTime);
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
            Vector3 moveDirection = Quaternion.Euler(0, 45f, 0) * inputVector;
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
                if (dot < 0f) // Bergerak menabrak tembok
                {
                    Vector3 slide = Vector3.ProjectOnPlane(desiredMove, contactWallNormal);
                    slide.y = 0f;
                    if (slide.sqrMagnitude > 0.001f)
                        desiredMove = slide.normalized;
                }
            }

            // 2. Wall Sliding prediktif via CapsuleCast ke depan (mencegah tersendat sebelum kontak)
            if (playerCollider != null)
            {
                float halfHeight = Mathf.Max(playerCollider.height * 0.5f - playerCollider.radius, 0f);
                Vector3 p1 = transform.position + playerCollider.center + Vector3.up * halfHeight;
                Vector3 p2 = transform.position + playerCollider.center - Vector3.up * Mathf.Max(halfHeight - 0.1f, 0f);
                float radius = playerCollider.radius;
                float castDist = maxSpeed * Time.fixedDeltaTime + 0.15f;

                // Pass 1: Deteksi dan defleksikan arah ke dinding pertama
                if (Physics.CapsuleCast(p1, p2, radius * 0.95f, desiredMove, out RaycastHit hit, castDist, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
                {
                    float wallAngle = Vector3.Angle(hit.normal, Vector3.up);
                    if (wallAngle > 50f && wallAngle < 130f)
                    {
                        Vector3 hitNormal = hit.normal;
                        hitNormal.y = 0f;
                        hitNormal.Normalize();

                        float dot = Vector3.Dot(desiredMove, hitNormal);
                        if (dot < 0f)
                        {
                            Vector3 slide = Vector3.ProjectOnPlane(desiredMove, hitNormal);
                            slide.y = 0f;
                            if (slide.sqrMagnitude > 0.001f)
                                desiredMove = slide.normalized;

                            // Pass 2: Jika berada di sudut (dua dinding bertemu), cek dinding kedua
                            if (Physics.CapsuleCast(p1, p2, radius * 0.95f, desiredMove, out RaycastHit hitCorner, castDist * 0.5f, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
                            {
                                float cornerAngle = Vector3.Angle(hitCorner.normal, Vector3.up);
                                if (cornerAngle > 50f && cornerAngle < 130f)
                                {
                                    Vector3 cornerNormal = hitCorner.normal;
                                    cornerNormal.y = 0f;
                                    cornerNormal.Normalize();

                                    float dotCorner = Vector3.Dot(desiredMove, cornerNormal);
                                    if (dotCorner < 0f)
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

    void OnCollisionStay(Collision collision)
    {
        bool foundWall = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float angle = Vector3.Angle(normal, Vector3.up);
            if (angle > 50f && angle < 130f)
            {
                normal.y = 0f;
                contactWallNormal = normal.normalized;
                foundWall = true;
                break;
            }
        }

        if (foundWall)
        {
            activeWallColliders.Add(collision.collider);
            isTouchingWall = true;
        }
        else
        {
            activeWallColliders.Remove(collision.collider);
            if (activeWallColliders.Count == 0)
            {
                isTouchingWall = false;
                contactWallNormal = Vector3.zero;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        activeWallColliders.Remove(collision.collider);
        if (activeWallColliders.Count == 0)
        {
            isTouchingWall = false;
            contactWallNormal = Vector3.zero;
        }
    }

    // --- LOGIKA AKSI ---

    // Klik Kiri Mouse / Tombol F: Serangan Kombo Biasa (3-Hit Combo)
    // Klik Kanan Mouse / Tombol R: Jurus Spesial (Leap Strike)
    private void HandleAttackInput()
    {
        if (isInputLocked || isPlanting || isAttacking) return;

        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // 1. Serangan Normal (3-Hit Combo): Left Click atau Tombol F
        bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isPointerOverUI;
        bool fKey = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;

        // 2. Jurus Spesial (Leap Strike): Right Click atau Tombol R
        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !isPointerOverUI;
        bool rKey = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        // 3. Skill Tendangan Spartan (Knockback Kick): Tombol Q
        bool qKey = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        // 4. Selebrasi / Battlecry Emote: Tombol T
        bool tKey = Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;

        if (playerEquipment == null)
            playerEquipment = GetComponent<PlayerEquipment>() ?? gameObject.AddComponent<PlayerEquipment>();

        if (qKey)
        {
            if (playerEquipment != null && playerEquipment.TryPerformKick())
            {
                StartCoroutine(RoutineKick());
            }
        }
        else if (tKey)
        {
            if (animator != null && isGrounded)
            {
                animator.ResetTrigger("Taunt");
                animator.SetTrigger("Taunt");
            }
        }
        else if (rightClick || rKey)
        {
            if (playerEquipment != null && playerEquipment.TryPerformSkillAttack())
            {
                StartCoroutine(RoutineSkillAttack());
            }
        }
        else if (leftClick || fKey)
        {
            if (playerEquipment != null && playerEquipment.TryPerformAttack())
            {
                StartCoroutine(RoutineAttack());
            }
        }
    }

    private IEnumerator RoutineAttack()
    {
        isAttacking = true;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        // Tunggu satu frame agar transisi animator ke state attack dimulai
        yield return null;

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f;
        float maxLock = attackLockDuration / atkSpeed;
        float minLock = 0.30f / atkSpeed;

        float timer = 0f;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;

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
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private IEnumerator RoutineKick()
    {
        isAttacking = true;
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
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private IEnumerator RoutineSkillAttack()
    {
        isAttacking = true;
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

    #endregion
}