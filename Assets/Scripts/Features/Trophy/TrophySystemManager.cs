using FeaturesCamera;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Manager mode First-Person Trophy Arrangement (singleton).
// Delegates camera switching to CameraManager. ESC untuk keluar dari mode.
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

    [Header("Trophy System")]
    [Tooltip("Parent container (TrophyCabinetSystem root).")]
    [SerializeField] private Transform trophySystemRoot;

    [Tooltip("Trophy first-person camera (child of TrophyCabinetSystem).")]
    [SerializeField] private Camera trophyCamera;

    // Jarak maksimum raycast saat mengambil piala dari rak.
    private const float RaycastDistance = 10f;

    [Header("Dual-Inventory Trophy Cabinet")]
    [Tooltip("Inventory 1 (Kabinet): tempat item piala disimpan; target saat piala diambil dari rak.")]
    [SerializeField] private InventoryComponent currentCabinetInventory;

    [Tooltip("Inventory 2 (Rack): sumber kebenaran visual piala yang terpasang di rak.")]
    [SerializeField] private InventoryComponent currentRackInventory;

    // Akses baca publik ke kamera trophy via CameraManager.
    // Direct reference only — no FindObjectsByType/GetComponentInChildren fallback.
    // A disabled feature camera in the scene makes those searches return the wrong Camera.
    public Camera TrophyFirstPersonCamera
    {
        get
        {
            if (trophyCamera != null)
                return trophyCamera;

            if (CameraManager.Instance != null)
                return CameraManager.Instance.GetComponentInChildren<Camera>();

            return null;
        }
    }

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

        // Auto-resolve trophy camera from the scene hierarchy (child of trophySystemRoot).
        // Must be a direct reference — FindObjectsByType would return the wrong Camera
        // when the feature camera is disabled in the scene.
        if (trophyCamera == null && trophySystemRoot != null)
            trophyCamera = trophySystemRoot.GetComponentInChildren<Camera>();
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

        Camera trophyCam = TrophyFirstPersonCamera;
        if (trophyCam == null || Mouse.current == null)
            return;

        Ray ray = trophyCam.ScreenPointToRay(Mouse.current.position.ReadValue());
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

        // Delegate camera switching to CameraManager
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetMode(CameraManager.CameraMode.TrophyMode, trophySystemRoot);
            CameraManager.Instance.PositionPlayerBehindTrophyCamera();
        }
        else
        {
            Debug.LogError("[TrophySystemManager] CameraManager.Instance not found!");
        }

        Debug.Log("Masuk First-Person Trophy Mode");
    }


    public void ExitTrophyMode()
    {
        if (!isInTrophyMode)
            return;

        isInTrophyMode = false;

        // Delegate camera switching to CameraManager
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetMode(CameraManager.CameraMode.Gameplay);
        }

        // Tutup panel storage/inventori yang dibuka saat masuk mode trophy.
        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.CloseAllUI();

        Debug.Log("Keluar dari Trophy Mode");
    }
}