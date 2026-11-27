using FarmBeware.Core.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// First-Person Trophy Arrangement mode manager (singleton).
// Delegates camera switching to CameraManager. ESC to exit mode.
public class TrophySystemManager : MonoBehaviour, FarmBeware.Core.Runtime.ITrophyService
{
    private static TrophySystemManager _instance;

    // Singleton + lazy resolver (same pattern as TimeManager) so Instance
    // still resolves even if Awake has not been called yet (edit mode/dll).
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

    // Maximum raycast distance when collecting a trophy from the rack.
    private const float RaycastDistance = 10f;

    [Header("Dual-Inventory Trophy Cabinet")]
    [Tooltip("Inventory 1 (Kabinet): tempat item piala disimpan; target saat piala diambil dari rak.")]
    [SerializeField] private InventoryComponent currentCabinetInventory;

    [Tooltip("Inventory 2 (Rack): sumber kebenaran visual piala yang terpasang di rak.")]
    [SerializeField] private InventoryComponent currentRackInventory;

    // Public read access to the trophy camera via CameraManager.
    public Camera TrophyFirstPersonCamera
    {
        get
        {
            if (trophyCamera != null)
                return trophyCamera;

            var cameraService = ServiceLocator.Resolve<ICameraService>();
            if (cameraService != null && cameraService.TrophyCamera != null)
                return cameraService.TrophyCamera;

            return null;
        }
    }

    // Public read access to both inventories (used by the rack drag flow).
    public InventoryComponent CabinetInventory { get { return currentCabinetInventory; } }
    public InventoryComponent RackInventory { get { return currentRackInventory; } }

    private bool isInTrophyMode = false;

    // Public read access for other listeners (e.g., TrophyDragController).
    public bool IsInTrophyMode { get { return isInTrophyMode; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITrophyService>(this);

        // Auto-resolve trophy camera from the scene hierarchy (child of trophySystemRoot).
        // Must include inactive objects so camera can be found when disabled by default.
        if (trophyCamera == null && trophySystemRoot != null)
            trophyCamera = trophySystemRoot.GetComponentInChildren<Camera>(true);
    }

    private void OnEnable()
    {
        DraggableItem.HybridWorldDropHandler = HandleTrophyWorldDrop;
    }

    private void OnDisable()
    {
        if (DraggableItem.HybridWorldDropHandler == HandleTrophyWorldDrop)
            DraggableItem.HybridWorldDropHandler = null;
    }

    private bool HandleTrophyWorldDrop(InventorySlotUI originSlot)
    {
        if (!isInTrophyMode || originSlot == null)
            return false;

        Camera cam = TrophyFirstPersonCamera;
        if (cam == null || !cam.enabled)
            cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return false;

        ItemData item = originSlot.BoundSlot != null ? originSlot.BoundSlot.item : null;
        if (item == null || item is not TrophyItemData)
            return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("SnapPoint")))
            return false;

        TrophySnapPoint snap = hit.collider != null ? hit.collider.GetComponent<TrophySnapPoint>() : null;
        if (snap == null || snap.slotIndex < 0)
            return false;

        InventoryComponent rack = currentRackInventory;
        if (rack == null || originSlot.ownerInventory == null)
            return false;

        originSlot.ownerInventory.MoveItemToSlot(originSlot.SlotIndex, rack, snap.slotIndex);
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            FarmBeware.Core.Runtime.ServiceLocator.Unregister<FarmBeware.Core.Runtime.ITrophyService>();
            Instance = null;
        }
    }

    private void Update()
    {
        // Trophy mode INACTIVE: ignore all input below.
        if (!isInTrophyMode)
            return;

        // ESC to exit trophy mode.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            FarmBeware.Core.Runtime.UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
            ExitTrophyMode();
            return;
        }

        // Left-click: attempt to collect a trophy currently placed on the rack (SnapPoint).
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryCollectTrophy();
    }

    /// <summary>
    /// Left-click (New Input System) in trophy mode: cast a ray from the trophy camera
    /// onto the SnapPoint layer (layer 10), then move the corresponding rack slot's contents
    /// back to the Cabinet via backend TransferItemTo.
    /// </summary>
    private void TryCollectTrophy()
    {
        if (currentRackInventory == null || currentCabinetInventory == null)
            return;

        // Clicks over UI are handled by EventSystem (slots, buttons, etc.) — skip world raycast
        // so trophies behind the panel are not accidentally collected.
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

        // Backend command: move that rack slot's contents back to the Cabinet.
        // Visual (3D model) is automatically removed by TrophyRackVisuals via OnInventoryChanged.
        currentRackInventory.TransferItemTo(currentCabinetInventory, snap.slotIndex);
    }

    public void EnterTrophyMode()
    {
        if (isInTrophyMode)
            return;

        isInTrophyMode = true;

        // Delegate camera switching to ICameraService
        var cameraService = ServiceLocator.Resolve<ICameraService>();
        if (cameraService != null)
        {
            cameraService.SetMode(CameraMode.TrophyMode, trophySystemRoot);
            cameraService.PositionPlayerBehindTrophyCamera();
        }
        else
        {
            Debug.LogError("[TrophySystemManager] ICameraService not found!");
        }

        Debug.Log("Entered First-Person Trophy Mode");
    }


    public void ExitTrophyMode()
    {
        if (!isInTrophyMode)
            return;

        isInTrophyMode = false;

        // Delegate camera switching to ICameraService
        var camService = ServiceLocator.Resolve<ICameraService>();
        if (camService != null)
        {
            camService.SetMode(CameraMode.Gameplay);
        }

        // Close storage/inventory panel opened when entering trophy mode
        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.CloseAllUI();

        Debug.Log("Exited Trophy Mode");
    }
}