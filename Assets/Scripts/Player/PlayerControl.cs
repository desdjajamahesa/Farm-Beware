using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem; // Pastikan ini tetap ada

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 7f;
    public float turnSpeed = 15f;

    [Header("Pengaturan Aksi")]
    public float jumpForce = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 inputVector;
    private PlayerInputActions inputActions;
    private PlayerInteractor interactor;
    private InventoryComponent playerInventory;

    // Status internal
    private bool isGrounded;
    private bool isDashing;
    private float lastDashTime = -100f;

    // Kunci input global: saat true, pemain tidak bisa bergerak, membuka
    // inventori, melompat, dash, atau berinteraksi (dipakai mode Trophy, dst).
    public bool isInputLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (inputActions == null)
            inputActions = new PlayerInputActions();

        interactor = GetComponent<PlayerInteractor>();  
        playerInventory = GetComponent<InventoryComponent>();  
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

        // 1. Cek apakah karakter menginjak tanah
        CheckGrounded();

        // Jika sedang dash, abaikan input pergerakan pemain
        if (isDashing) return;

        // 2. Membaca Input Pergerakan
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        inputVector = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // 3. Sinkronisasi Animator
        if (animator != null)
        {
            float speedValue = inputVector.magnitude;
            animator.SetFloat("Vel", speedValue);

            // Mengirimkan status tanah SEBENARNYA ke Animator
            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("Idle", speedValue < 0.1f);
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
            float moveDistance = moveSpeed * Time.fixedDeltaTime;
            
            // SWEEP TEST: Check for collisions before moving using CapsuleCast
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            float capsuleRadius = capsuleCollider != null ? capsuleCollider.radius : 0.5f;
            float capsuleHeight = capsuleCollider != null ? capsuleCollider.height : 2.0f;
            Vector3 capsuleCenter = rb.position + Vector3.up * (capsuleHeight * 0.5f);
            float capsuleHalfHeight = (capsuleHeight - capsuleRadius * 2f) * 0.5f;
            float skinWidth = 0.05f; // Small margin
            
            // CapsuleCast to check for collisions along movement path
            RaycastHit sweepHit;
            bool hasHit = Physics.CapsuleCast(
                capsuleCenter + Vector3.down * capsuleHalfHeight - moveDirection * 0.01f, // Start slightly behind
                capsuleCenter + Vector3.up * capsuleHalfHeight - moveDirection * 0.01f,
                capsuleRadius - 0.01f, // Slightly smaller radius for safety
                moveDirection,
                out sweepHit,
                moveDistance + 0.05f, // Distance + skin width
                ~0, // All layers
                QueryTriggerInteraction.Ignore);
            
            if (hasHit)
            {
                // Hit something - move only to contact point minus skin width
                moveDistance = Mathf.Max(0, sweepHit.distance - 0.05f);
            }
            
            Vector3 newPosition = rb.position + moveDirection * moveDistance;
            rb.MovePosition(newPosition);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
        else
        {
            // Pengereman alami saat tidak ada input (mempertahankan kecepatan jatuh Y)
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    // --- LOGIKA AKSI ---

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

        // Hanya bisa lompat jika menginjak tanah dan tidak sedang dash
        if (isGrounded && !isDashing)
        {
            // Reset kecepatan Y agar lompatan konsisten, lalu dorong ke atas
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
{
    if (isInputLocked) return;

    // Pastikan skrip interactor tidak hilang/error
    if (interactor != null)
        {
            // Perintahkan "Tangan" untuk menjalankan logikanya
            interactor.OnInteractInput(); 
        }
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