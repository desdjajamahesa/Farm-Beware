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
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Pengaturan Crouch")]
    public float crouchSpeed = 2.5f;
    public float crouchColliderHeight = 1.2f;
    public float crouchColliderCenterY = 0.6f;
    private float originalColliderHeight = 2.0f;
    private float originalColliderCenterY = 1.0f;
    private CapsuleCollider playerCollider;
    private bool isCrouching = false;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 inputVector;
    private PlayerInputActions inputActions;
    private PlayerInteractor interactor;
    private InventoryComponent playerInventory;
    private PlayerStats playerStats;
    private PlayerEquipment playerEquipment;

    // Status internal
    private bool isGrounded;
    private bool isDashing;
    private bool isRunning;
    private float lastDashTime = -100f;

    // Kunci input global: saat true, pemain tidak bisa bergerak, membuka
    // inventori, melompat, dash, atau berinteraksi (dipakai mode Trophy, dst).
    public bool isInputLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();
        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenterY = playerCollider.center.y;
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
        playerStats = GetComponent<PlayerStats>();
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
        inputActions.Player.Dash.performed += ctx => StartCoroutine(ExecuteDash());
        inputActions.Player.Interact.performed += OnInteractPressed;
    }

    void OnDisable()
    {
        // Guard null agar OnDisable aman saat OnEnable gagal/urutan tidak menentu.
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        // Mencabut pendaftaran event untuk mencegah memory leak
        inputActions.Player.Jump.performed -= ctx => ExecuteJump();
        inputActions.Player.Dash.performed -= ctx => StartCoroutine(ExecuteDash());

        inputActions.Player.Disable();

        inputActions.Player.Interact.performed -= OnInteractPressed;
    }

    void Update()
    {
        // Kunci input: hentikan inventory/hotbar/gerak/animator saat terkunci.
        if (isInputLocked) return;

        HandleInventoryInput();
        HandleHotbarInput();
        HandleAttackInput();
        HandlePlantSeedInput();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TriggerInteract();
        }

        // 1. Cek apakah karakter menginjak tanah
        CheckGrounded();

        // Jika sedang dash, abaikan input pergerakan pemain
        if (isDashing) return;

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

        // 3. Cek apakah pemain menahan tombol Ctrl untuk Jongkok (Crouch)
        bool wantsToCrouch = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
        isCrouching = isGrounded && wantsToCrouch;

        // 4. Cek apakah pemain menahan tombol Shift untuk Lari (Sprint)
        bool isMoving = inputVector.magnitude >= 0.1f;
        bool wantsToRun = !isCrouching && Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        // Karakter hanya berlari jika bergerak, menekan shift, tidak sedang jongkok, dan memiliki stamina
        isRunning = isMoving && wantsToRun && (playerStats == null || !playerStats.IsExhausted);

        // Update ketinggian collider secara mulus saat jongkok vs berdiri
        if (playerCollider != null)
        {
            float targetHeight = isCrouching ? crouchColliderHeight : originalColliderHeight;
            float targetCenterY = isCrouching ? crouchColliderCenterY : originalColliderCenterY;
            playerCollider.height = Mathf.MoveTowards(playerCollider.height, targetHeight, 6f * Time.deltaTime);
            Vector3 center = playerCollider.center;
            center.y = Mathf.MoveTowards(center.y, targetCenterY, 3f * Time.deltaTime);
            playerCollider.center = center;
        }

        // 5. Konsumsi Stamina HANYA saat Berlari (Sprint)
        if (isRunning && playerStats != null)
        {
            playerStats.UseStamina(playerStats.staminaDrainRate * Time.deltaTime);
        }

        // 6. Sinkronisasi Animator
        if (animator != null)
        {
            float targetSpeed = 0f;
            if (isMoving)
            {
                targetSpeed = isRunning ? 1.0f : (isCrouching ? 0.3f : 0.5f);
            }

            // Gunakan dampTime (0.1f) agar perubahan kecepatan dan langkah kaki bertransisi mulus
            animator.SetFloat("Vel", targetSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("Idle", !isMoving);
            animator.SetBool("IsCrouching", isCrouching);
            animator.SetBool("Sprinting", isRunning);
        }
    }

    void FixedUpdate()
    {
        // Kunci input: hentikan fisika pergerakan saat terkunci.
        if (isInputLocked) return;

        // Jika sedang dash, fisika dikendalikan oleh Coroutine
        if (isDashing) return;

        if (inputVector.magnitude >= 0.1f)
        {
            Vector3 moveDirection = Quaternion.Euler(0, 45f, 0) * inputVector;
            
            // Kecepatan: crouchSpeed (2.5) saat jongkok, runSpeed (8) saat lari, walkSpeed (5) saat jalan
            float currentSpeed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

            // Gerakkan karakter murni dengan linearVelocity (kecepatan akurat, responsif, dan tidak ngedrift)
            Vector3 targetVelocity = moveDirection * currentSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

            // Rotasi karakter menghadap arah pergerakan
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Vector3 lookEuler = Quaternion.LookRotation(moveDirection).eulerAngles;
                Quaternion targetRotation = Quaternion.Euler(0f, lookEuler.y, 0f);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
            }
        }
        else
        {
            // Pengereman alami saat tidak ada input (mempertahankan kecepatan jatuh Y)
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        // Redam sisa angular velocity fisik
        rb.angularVelocity = Vector3.zero;
    }

    // --- LOGIKA AKSI ---

    // Klik Kiri Mouse / Serang: Panggil animasi serangan jika item yang dipegang adalah senjata.
    private void HandleAttackInput()
    {
        if (isInputLocked) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (playerEquipment == null)
                playerEquipment = GetComponent<PlayerEquipment>();

            if (playerEquipment == null)
                playerEquipment = gameObject.AddComponent<PlayerEquipment>();

            if (playerEquipment != null)
            {
                playerEquipment.TryPerformAttack();
            }
        }
    }

    // Tombol Q: Memainkan animasi menanam benih (PlantSeed).
    private void HandlePlantSeedInput()
    {
        if (isInputLocked) return;

        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (animator != null)
            {
                animator.SetTrigger("PlantSeed");
            }
        }
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
        if (isInputLocked) return;

        // Hanya bisa lompat jika menginjak tanah, tidak sedang dash, dan tidak sedang jongkok
        if (isGrounded && !isDashing && !isCrouching)
        {
            // Reset kecepatan Y agar lompatan konsisten, lalu dorong ke atas
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private int lastInteractFrame = -1;

    public void TriggerInteract()
    {
        if (Time.frameCount == lastInteractFrame) return;
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

    private IEnumerator ExecuteDash()
    {
        // Kunci input / cooldown / syarat dash
        if (isInputLocked || isDashing || Time.time < lastDashTime + dashCooldown || inputVector.magnitude < 0.1f)
            yield break;

        isDashing = true;
        lastDashTime = Time.time;

        // Pemicu animasi dash (Misalnya menggunakan parameter "Sliding" di template Anda)
        if (animator != null) animator.SetBool("Sliding", true);

        // Arah dash berdasarkan orientasi karakter saat ini
        Vector3 dashDirection = transform.forward;
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            // Mendorong karakter ke depan dengan kecepatan dash
            rb.linearVelocity = dashDirection * dashSpeed;
            rb.angularVelocity = Vector3.zero;
            yield return null; // Tunggu ke frame berikutnya
        }

        // Akhiri dash
        if (animator != null) animator.SetBool("Sliding", false);
        isDashing = false;
    }

    private void CheckGrounded()
    {
        // Menembakkan sinar ke bawah (sedikit dari atas kaki) untuk mengecek tanah
        // Jarak sinar 0.2f. Sesuaikan jika kapsul Anda lebih tinggi/rendah.
        Vector3 origin = transform.position + (Vector3.up * 0.1f);
        isGrounded = Physics.Raycast(origin, Vector3.down, 0.25f);
    }
}