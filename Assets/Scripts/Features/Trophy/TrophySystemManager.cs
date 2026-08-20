using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Manager mode First-Person Trophy Arrangement (singleton).
// Mengunci input pemain, memindahkan kamera utama ke kamera trophy, dan
// membebaskan kursor saat mode aktif. ESC untuk keluar dari mode.
public class TrophySystemManager : MonoBehaviour
{
    private static TrophySystemManager _instance;

    // Singleton + lazy resolver (pola sama dengan TimeManager) agar Instance
    // tetap ter-resolve bahkan bila Awake belum terpanggil (edit mode/dll).
    public static TrophySystemManager Instance
    {
        get
        {
            if (_instance == null)
            {
                TrophySystemManager[] found = FindObjectsByType<TrophySystemManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                    _instance = found[0];
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [SerializeField] private Camera mainPlayerCamera;
    [SerializeField] private Camera trophyFirstPersonCamera;
    [SerializeField] private PlayerControl playerControl;

    // Jarak maksimum raycast saat mengambil piala dari rak.
    private const float RaycastDistance = 10f;

    [Header("Pose Kamera Trophy (relatif ke TrophyCabinetSystem root)")]
    [Tooltip("Parent container (TrophyCabinetSystem). Kamera trophy harus child dari root ini.")]
    [SerializeField] private Transform trophySystemRoot;

    [Tooltip("Offset posisi kamera lokal (relative ke trophySystemRoot).")]
    [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-0.15f, 1.5f, 3f);

    [Tooltip("Offset rotasi kamera lokal (Euler, relative ke trophySystemRoot).")]
    [SerializeField] private Vector3 cameraLocalRotation = new Vector3(3f, 0f, 0f);

    [Header("Dual-Inventory Trophy Cabinet")]
    [Tooltip("Inventory 1 (Kabinet): tempat item piala disimpan; target saat piala diambil dari rak.")]
    [SerializeField] private InventoryComponent currentCabinetInventory;

    [Tooltip("Inventory 2 (Rack): sumber kebenaran visual piala yang terpasang di rak.")]
    [SerializeField] private InventoryComponent currentRackInventory;

    // Akses baca publik ke kamera trophy (dipakai hybrid drop drag ke-3D).
    public Camera TrophyFirstPersonCamera { get { return trophyFirstPersonCamera; } }

    // Akses baca publik ke kedua inventory (dipakai alur drag ke rak).
    public InventoryComponent CabinetInventory { get { return currentCabinetInventory; } }
    public InventoryComponent RackInventory { get { return currentRackInventory; } }

    private bool isInTrophyMode = false;

    // Pembacaan publik untuk listener lain (mis. TrophyDragController).
    public bool IsInTrophyMode { get { return isInTrophyMode; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // Mode trophy TIDAK aktif: abaikan seluruh input di bawah ini.
        if (!isInTrophyMode)
            return;

        // ESC untuk keluar dari mode trophy.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitTrophyMode();
            return;
        }

        // Klik kiri: coba ambil piala yang sedang terpasang di rak (SnapPoint).
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryCollectTrophy();
    }

    /// <summary>
    /// Klik kiri (New Input System) saat mode trophy: tembakkan raycast dari kamera
    /// trophy ke layer SnapPoint (layer 10) lalu pindahkan isi slot rak yang bersangkutan
    /// kembali ke Kabinet via backend TransferItemTo.
    /// </summary>
    private void TryCollectTrophy()
    {
        if (currentRackInventory == null || currentCabinetInventory == null)
            return;

        // Klik di atas UI dikelola EventSystem (slot, tombol, dsb) — jangan ikut memproses
        // world raycast agar piala di belakang panel tidak ikut terambil.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (trophyFirstPersonCamera == null || Mouse.current == null)
            return;

        Ray ray = trophyFirstPersonCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, RaycastDistance, LayerMask.GetMask("SnapPoint")))
            return;

        TrophySnapPoint snap = hit.collider != null ? hit.collider.GetComponent<TrophySnapPoint>() : null;
        if (snap == null || snap.slotIndex < 0)
            return;

        // Backend command: pindahkan isi slot rak tersebut kembali ke Kabinet.
        // Visual (model 3D) otomatis dihapus oleh TrophyRackVisuals via OnInventoryChanged.
        currentRackInventory.TransferItemTo(currentCabinetInventory, snap.slotIndex);
    }

    public void EnterTrophyMode()
    {
        if (isInTrophyMode)
            return;

        isInTrophyMode = true;

        if (playerControl != null)
            playerControl.isInputLocked = true;

        // Use camera.enabled instead of SetActive to avoid AudioListener conflicts
        if (mainPlayerCamera != null)
            mainPlayerCamera.enabled = false;

        if (trophyFirstPersonCamera != null)
        {
            trophyFirstPersonCamera.enabled = true;
            trophyFirstPersonCamera.gameObject.SetActive(true); // Ensure GameObject is active
        }

        // Terapkan pose kamera & posisikan player di belakang kamera
        AlignTrophyCamera();
        PositionPlayerToCamera();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Masuk First-Person Trophy Mode");
    }

    /// <summary>
    /// Pose kamera trophy ditetapkan LOKAL ke trophySystemRoot.
    /// TrophyCamera sekarang child of TrophyCabinetSystem → cukup set localPosition/localRotation.
    /// Fallback: jika trophySystemRoot null, gunakan world position absolut (legacy).
    /// </summary>
    private void AlignTrophyCamera()
    {
        if (trophyFirstPersonCamera == null)
            return;

        if (trophySystemRoot != null && trophyFirstPersonCamera.transform.parent == trophySystemRoot)
        {
            // Camera is child of root → use local transform
            trophyFirstPersonCamera.transform.localPosition = cameraLocalOffset;
            trophyFirstPersonCamera.transform.localRotation = Quaternion.Euler(cameraLocalRotation);
            Debug.Log($"Trophy cam pose (local): pos={cameraLocalOffset} rot={cameraLocalRotation}");
        }
        else if (trophySystemRoot != null)
        {
            // Camera not child of root but root exists → use world transform (backward compat)
            Vector3 worldPos = trophySystemRoot.TransformPoint(cameraLocalOffset);
            Quaternion worldRot = trophySystemRoot.rotation * Quaternion.Euler(cameraLocalRotation);
            trophyFirstPersonCamera.transform.SetPositionAndRotation(worldPos, worldRot);
            Debug.Log($"Trophy cam pose (world from root): pos={worldPos} rot={worldRot.eulerAngles}");
        }
        else
        {
            // No root → keep current position (should not happen with new wiring)
            Debug.LogWarning("TrophySystemManager: trophySystemRoot is null, camera position unchanged.");
        }
    }

    /// <summary>
    /// Teleport player tepat di belakang kamera trophy (view dari belakang karakter).
    /// Menjaga player tetap di tanah via raycast ke bawah + reset kecepatan fisik.
    /// </summary>
    private void PositionPlayerToCamera()
    {
        // Auto-resolve player bila screen-wiring belum terisi (anti-gagal diam-diam).
        if (playerControl == null)
            playerControl = FindFirstObjectByType<PlayerControl>();

        if (playerControl == null || trophyFirstPersonCamera == null)
            return;

        Vector3 behind = trophyFirstPersonCamera.transform.position
                         - trophyFirstPersonCamera.transform.forward * 0.4f;

        // Snap ke tanah agar player tidak "mengambang" ketika di-teleport.
        if (Physics.Raycast(behind, Vector3.down, out RaycastHit hit, 5f))
            behind.y = hit.point.y + 1f;

        Rigidbody rb = playerControl.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Posisi pada RIGIDBODY, bukan Transform — jika ditulis ke transform.position,
            // engine fisika akan menimpa kembali di frame berikutnya (teleport tidak terlihat).
            rb.linearVelocity = Vector3.zero;
            rb.position = behind;
        }
        else
        {
            playerControl.transform.position = behind;
        }

        Debug.Log($"TrophyMode: kamera@{trophyFirstPersonCamera.transform.position}, player -> {behind}");
    }

    public void ExitTrophyMode()
    {
        if (!isInTrophyMode)
            return;

        isInTrophyMode = false;

        if (playerControl != null)
            playerControl.isInputLocked = false;

        // Use camera.enabled instead of SetActive to avoid AudioListener conflicts
        if (trophyFirstPersonCamera != null)
            trophyFirstPersonCamera.enabled = false;

        if (mainPlayerCamera != null)
            mainPlayerCamera.enabled = true;

        // Pulihkan kursor ke pengaturan gameplay default (terkunci & tersembunyi).
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Tutup panel storage/inventori yang dibuka saat masuk mode trophy.
        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.CloseAllUI();

        Debug.Log("Keluar dari Trophy Mode");
    }
}