using UnityEngine;

namespace FeaturesCamera
{
    /// <summary>
    /// Centralized camera management singleton. Single source of truth for all camera state.
    /// Manages state machine (Gameplay/Trophy/Wardrobe), positioning, and component lifecycle.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        #region Singleton
        private static CameraManager _instance;
        public static CameraManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<CameraManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        #endregion

        public enum CameraMode
        {
            Gameplay,      // IsometricCameraController follows player
            TrophyMode,    // First-person trophy arrangement
            WardrobeMode   // Side view of player at mirror
        }

        [Header("Camera References")]
        [Tooltip("Main gameplay camera (isometric).")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("Unified isometric camera controller component.")]
        [SerializeField] private IsometricCameraController isometricCameraController;

        [Tooltip("Trophy first-person camera.")]
        [SerializeField] private Camera trophyCamera;

        [Tooltip("Wardrobe side-view camera.")]
        [SerializeField] private Camera wardrobeCamera;

        [Header("Transform Roots")]
        [Tooltip("Trophy system root transform (for local camera positioning).")]
        [SerializeField] private Transform trophySystemRoot;

        [Tooltip("Wardrobe root transform (for local camera positioning).")]
        [SerializeField] private Transform wardrobeRoot;

        [Header("Player Control")]
        [Tooltip("PlayerControl component for input locking.")]
        [SerializeField] private PlayerControl playerControl;

        private CameraMode _currentMode = CameraMode.Gameplay;
        public CameraMode CurrentMode => _currentMode;

        public event System.Action<CameraMode> OnCameraModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-resolve references if not wired in Inspector
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (isometricCameraController == null && mainCamera != null)
                isometricCameraController = mainCamera.GetComponent<IsometricCameraController>();

            if (playerControl == null)
                playerControl = FindFirstObjectByType<PlayerControl>();

            // Enforce initial mode: gameplay cameras on, feature cameras off.
            // Scene files can persist stale enabled-states from a previous session
            // (e.g. after an editor crash or edit-mode simulation) — never trust them.
            if (mainCamera != null)
                mainCamera.enabled = true;
            if (isometricCameraController != null)
                isometricCameraController.enabled = true;
            if (trophyCamera != null)
                trophyCamera.enabled = false;
            if (wardrobeCamera != null)
                wardrobeCamera.enabled = false;

            // Gameplay mode is the only valid starting state
            _currentMode = CameraMode.Gameplay;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (playerControl != null)
                playerControl.isInputLocked = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Set camera mode with validation. Positions cameras, manages component lifecycle,
        /// locks input, and controls cursor. Prevents invalid transitions.
        /// </summary>
        public void SetMode(CameraMode mode, Transform contextRoot = null)
        {
            if (_currentMode == mode)
            {
                Debug.LogWarning($"[CameraManager] Already in {mode} mode.");
                return;
            }

            // Validate transition
            if (!IsValidTransition(_currentMode, mode))
            {
                Debug.LogError($"[CameraManager] Invalid transition: {_currentMode} → {mode}. Must return to Gameplay first.");
                return;
            }

            Debug.Log($"[CameraManager] Transitioning: {_currentMode} → {mode}");

            CameraMode previousMode = _currentMode;
            _currentMode = mode;

            switch (mode)
            {
                case CameraMode.Gameplay:
                    SetupGameplayMode();
                    break;
                case CameraMode.TrophyMode:
                    SetupTrophyMode(contextRoot);
                    break;
                case CameraMode.WardrobeMode:
                    SetupWardrobeMode(contextRoot);
                    break;
            }

            OnCameraModeChanged?.Invoke(mode);
        }

        private bool IsValidTransition(CameraMode from, CameraMode to)
        {
            // Can always return to Gameplay
            if (to == CameraMode.Gameplay)
                return true;

            // Can only enter Trophy/Wardrobe from Gameplay
            if (from == CameraMode.Gameplay && (to == CameraMode.TrophyMode || to == CameraMode.WardrobeMode))
                return true;

            // Trophy → Wardrobe or Wardrobe → Trophy is invalid
            return false;
        }

        private void SetupGameplayMode()
        {
            // Enable main camera + isometric controller
            if (mainCamera != null)
                mainCamera.enabled = true;

            if (isometricCameraController != null)
                isometricCameraController.enabled = true;

            // Disable feature cameras
            if (trophyCamera != null)
                trophyCamera.enabled = false;

            if (wardrobeCamera != null)
                wardrobeCamera.enabled = false;

            // Unlock input, keep cursor free
            if (playerControl == null)
                playerControl = FindObjectOfType<PlayerControl>();
            if (playerControl != null)
            {
                playerControl.isInputLocked = false;
                Debug.Log("[DEBUG] CameraManager successfully unlocked PlayerControl.");
            }
            else
            {
                Debug.LogWarning("[DEBUG] CameraManager could not find PlayerControl to unlock!");
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[CameraManager] Gameplay mode active");
        }

        private void SetupTrophyMode(Transform contextRoot)
        {
            if (trophyCamera == null)
            {
                Debug.LogError("[CameraManager] Trophy camera not assigned!");
                return;
            }

            // Use provided root or fallback to stored reference
            Transform root = contextRoot != null ? contextRoot : trophySystemRoot;
            if (root == null)
            {
                Debug.LogError("[CameraManager] Trophy system root not provided!");
                return;
            }

            // Disable main camera + isometric controller
            if (mainCamera != null)
                mainCamera.enabled = false;

            if (isometricCameraController != null)
                isometricCameraController.enabled = false;

            // Camera pose is authored directly in the scene — no runtime repositioning.
            trophyCamera.enabled = true;

            // Lock input, free cursor
            if (playerControl != null)
                playerControl.isInputLocked = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[CameraManager] Trophy mode active");
        }

        private void SetupWardrobeMode(Transform contextRoot)
        {
            if (wardrobeCamera == null)
            {
                Debug.LogError("[CameraManager] Wardrobe camera not assigned!");
                return;
            }

            // Use provided root or fallback to stored reference
            Transform root = contextRoot != null ? contextRoot : wardrobeRoot;
            if (root == null)
            {
                Debug.LogError("[CameraManager] Wardrobe root not provided!");
                return;
            }

            // Disable main camera + isometric controller
            if (mainCamera != null)
                mainCamera.enabled = false;

            if (isometricCameraController != null)
                isometricCameraController.enabled = false;

            // Camera pose is authored directly in the scene — no runtime repositioning.
            wardrobeCamera.enabled = true;

            // Lock input, free cursor
            if (playerControl == null)
                playerControl = FindObjectOfType<PlayerControl>();
            if (playerControl != null)
                playerControl.isInputLocked = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[CameraManager] Wardrobe mode active");
        }

        /// <summary>
        /// Public API: Position player behind trophy camera (for teleport on mode enter).
        /// Called by TrophySystemManager after SetMode(TrophyMode).
        /// </summary>
        public void PositionPlayerBehindTrophyCamera()
        {
            if (playerControl == null || trophyCamera == null)
                return;

            Vector3 behind = trophyCamera.transform.position - trophyCamera.transform.forward * 0.4f;

            // Snap to ground
            if (Physics.Raycast(behind, Vector3.down, out RaycastHit hit, 5f))
                behind.y = hit.point.y + 1f;

            Rigidbody rb = playerControl.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.position = behind;
            }
            else
            {
                playerControl.transform.position = behind;
            }

            Debug.Log($"[CameraManager] Player positioned behind trophy camera: {behind}");
        }
    }
}
