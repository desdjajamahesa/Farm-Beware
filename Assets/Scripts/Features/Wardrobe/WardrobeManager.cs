using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace FeaturesWardrobe
{
    public class WardrobeManager : MonoBehaviour
    {
        #region Singleton
        private static WardrobeManager _instance;
        public static WardrobeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<WardrobeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        #endregion

        [Header("Cameras")]
        [Tooltip("Main player camera (isometric).")]
        [SerializeField] private Camera mainPlayerCamera;
        
        [Tooltip("Dedicated wardrobe camera (child of wardrobeRoot).")]
        [SerializeField] private Camera wardrobeCamera;
        
        [Tooltip("MirrorCamera component on the Mirror GameObject.")]
        [SerializeField] private MirrorCamera mirrorCamera;

        [Header("Positioning (Relative to WardrobeRoot)")]
        [Tooltip("Parent container (WardrobeRoot). Camera & Mirror should be children.")]
        [SerializeField] private Transform wardrobeRoot;
        
        [Tooltip("Local position offset of wardrobeCamera relative to wardrobeRoot.")]
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-2.5f, 1.6f, -2f);
        
        [Tooltip("Local rotation offset (Euler) of wardrobeCamera relative to wardrobeRoot.")]
        [SerializeField] private Vector3 cameraLocalRotation = new Vector3(8f, 85f, 0f);

        [Header("Animation")]
        [Tooltip("Durasi camera blend (detik).")]
        [SerializeField] private float cameraBlendDuration = 0.6f;
        
        [Tooltip("Durasi UI fade in/out (detik).")]
        [SerializeField] private float uiFadeDuration = 0.3f;

        [Header("Player & Outfit")]
        [SerializeField] private PlayerControl playerControl;
        
        [SerializeField] private PlayerOutfit playerOutfit;
        
        public PlayerOutfit PlayerOutfitProp => playerOutfit;
        [SerializeField] private Transform playerHead;

        [Header("UI")]
        [SerializeField] private GameObject wardrobeUIPanel;
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private WardrobeUI wardrobeUI;

        [Header("Debug")]
        [SerializeField] private bool debugCameraAudit = true;

        private bool isInWardrobeMode;
        private Coroutine blendCoroutine;
        private Vector3 playerOriginalPosition;
        private Quaternion playerOriginalRotation;
        private bool mainCameraWasEnabled;
        private bool wardrobeCameraWasEnabled;
        private bool isometricCameraWasEnabled;

        #region Public API

        public bool IsInWardrobeMode => isInWardrobeMode;

        public void EnterWardrobeMode()
        {
            if (isInWardrobeMode) return;
            
            isInWardrobeMode = true;
            
            playerOriginalPosition = playerControl.transform.position;
            playerOriginalRotation = playerControl.transform.rotation;
            mainCameraWasEnabled = mainPlayerCamera.enabled;
            wardrobeCameraWasEnabled = wardrobeCamera.enabled;
            
            // FIX B: Disable IsometricCamera to prevent it from re-enabling Main Camera
            var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
            if (isoCam != null)
            {
                isometricCameraWasEnabled = isoCam.enabled;
                isoCam.enabled = false;
                Debug.Log("[WardrobeManager] IsometricCamera disabled during wardrobe mode");
            }
            
            playerControl.isInputLocked = true;
            
            SetupWardrobeCamera();
            
            if (blendCoroutine != null) StopCoroutine(blendCoroutine);
            blendCoroutine = StartCoroutine(BlendCamerasAndUI(true));
            
            PositionPlayerToMirror();
            
            // FIX A: Harden MirrorTexture assignment - force reassign every Enter
            if (mirrorCamera != null)
            {
                mirrorCamera.EnsureInitialized();
                if (playerHead != null)
                    mirrorCamera.SetPlayerTarget(playerHead);
                
                // FIX A: Force reassign targetTexture every Enter
                if (mirrorCamera.Camera != null && mirrorCamera.MirrorTexture != null)
                {
                    mirrorCamera.Camera.targetTexture = mirrorCamera.MirrorTexture;
                    Debug.Log($"[WardrobeManager] MirrorInnerCam targetTexture reassigned: {mirrorCamera.MirrorTexture.name}");
                }
            }
            
            // FIX C: Harden MirrorInnerCam protection - comprehensive protection
            if (mirrorCamera != null && mirrorCamera.Camera != null)
            {
                // 1. Force targetTexture
                if (mirrorCamera.MirrorTexture != null)
                {
                    mirrorCamera.Camera.targetTexture = mirrorCamera.MirrorTexture;
                    Debug.Log($"[HARDEN] MirrorInnerCam targetTexture forced: {mirrorCamera.MirrorTexture.name}");
                }
                
                // 2. Set renderType to Base (never overlay/main)
                var uacd = mirrorCamera.Camera.GetComponent<UniversalAdditionalCameraData>();
                if (uacd != null)
                {
                    uacd.renderType = CameraRenderType.Base;
                    // cameraStack is read-only in URP, clear it properly
                    if (uacd.cameraStack != null && uacd.cameraStack.Count > 0)
                    {
                        uacd.cameraStack.Clear();
                    }
                    Debug.Log($"[HARDEN] MirrorInnerCam renderType set to Base, stack cleared");
                }
                
                // 3. Ensure very low depth so it never wins main view
                mirrorCamera.Camera.depth = -100;
                Debug.Log($"[HARDEN] MirrorInnerCam depth set to -100");
            }
            
            // FIX D: Start camera audit if debug enabled
            if (debugCameraAudit)
            {
                StartCoroutine(AuditCamerasDuringBlend());
            }
            
            if (wardrobeUIPanel != null)
                wardrobeUIPanel.SetActive(true);

            SetUIRaycastBlocking(true);
            SetCursorFree(true);

            if (wardrobeUI != null) wardrobeUI.ForceRefreshMirror();
            LogMirrorDiagnostics();

            Debug.Log("[WardrobeManager] Entered Wardrobe Mode");
        }

        public void ExitWardrobeMode()
        {
            if (!isInWardrobeMode) return;
            
            if (playerOutfit != null && playerOutfit.IsPreviewing)
                playerOutfit.Revert();
            
            isInWardrobeMode = false;
            
            if (playerControl != null)
                playerControl.isInputLocked = false;
            
            if (blendCoroutine != null) StopCoroutine(blendCoroutine);
            blendCoroutine = StartCoroutine(BlendCamerasAndUI(false));
            
            if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
            if (wardrobeUIPanel != null) wardrobeUIPanel.SetActive(false);

            SetUIRaycastBlocking(false);
            SetCursorFree(false);
            
            // Restore IsometricCamera
            var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
            if (isoCam != null)
            {
                isoCam.enabled = isometricCameraWasEnabled;
                Debug.Log($"[WardrobeManager] IsometricCamera restored: {isometricCameraWasEnabled}");
            }
            
            Debug.Log("[WardrobeManager] Exited Wardrobe Mode");
        }

        public void TryOnOutfit(OutfitData outfit)
        {
            if (outfit == null || playerOutfit == null) return;
            playerOutfit.TryOn(outfit);
        }

        public void PreviewDefault()
        {
            if (playerOutfit != null)
                playerOutfit.PreviewDefault();
        }

        public void CommitOutfit()
        {
            if (playerOutfit != null)
                playerOutfit.Commit();
        }

        public void RevertOutfit()
        {
            if (playerOutfit != null)
                playerOutfit.Revert();
        }

        private void SetUIRaycastBlocking(bool enabled)
        {
            if (uiCanvasGroup == null) return;
            uiCanvasGroup.interactable = enabled;
            uiCanvasGroup.blocksRaycasts = enabled;
        }

        private void SetCursorFree(bool free)
        {
            Cursor.visible = free;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void LogMirrorDiagnostics()
        {
            Texture rt = wardrobeUI != null ? wardrobeUI.MirrorTextureSource : null;
            bool mirrorReady = mirrorCamera != null && mirrorCamera.MirrorTexture != null;
            bool innerCamOn = mirrorCamera != null && mirrorCamera.Camera != null && mirrorCamera.Camera.enabled;
            bool targetOk = mirrorCamera != null && mirrorCamera.Camera != null &&
                            mirrorCamera.Camera.targetTexture == mirrorCamera.MirrorTexture;
            Debug.Log($"[Wardrobe] diag -> RawImage.texture={(rt != null ? rt.name : "NULL")} " +
                      $"| MirrorTexture={(mirrorReady ? "OK" : "NULL")} " +
                      $"| InnerCam.enabled={innerCamOn} " +
                      $"| targetTexture==RT={targetOk}");
        }

        #endregion

        #region Camera & UI Blend

        private void SetupWardrobeCamera()
        {
            if (wardrobeCamera == null || wardrobeRoot == null) return;
            
            wardrobeCamera.transform.SetParent(wardrobeRoot, false);
            wardrobeCamera.transform.localPosition = cameraLocalOffset;
            wardrobeCamera.transform.localRotation = Quaternion.Euler(cameraLocalRotation);
            wardrobeCamera.enabled = false;
        }

        private IEnumerator BlendCamerasAndUI(bool entering)
        {
            float startUIAlpha = entering ? 0f : 1f;
            float targetUIAlpha = entering ? 1f : 0f;
            float elapsed = 0f;

            // FIX B: IMMEDIATE camera swap + disable IsometricCamera
            if (entering)
            {
                mainPlayerCamera.enabled = false;
                
                // FIX B: Disable IsometricCamera to prevent it from re-enabling Main Camera
                var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
                if (isoCam != null)
                {
                    isoCam.enabled = false;
                    Debug.Log("[WardrobeManager] IsometricCamera disabled (blend start)");
                }
                
                wardrobeCamera.enabled = true;
                if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
            }
            else
            {
                mainPlayerCamera.enabled = true;
                wardrobeCamera.enabled = false;
                
                // Re-enable IsometricCamera on exit
                var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
                if (isoCam != null)
                {
                    isoCam.enabled = true;
                    Debug.Log("[WardrobeManager] IsometricCamera re-enabled (blend end)");
                }
            }

            // FIX D: Start camera audit if debug enabled
            Coroutine auditCoroutine = null;
            if (debugCameraAudit)
            {
                auditCoroutine = StartCoroutine(AuditCamerasDuringBlend());
            }

            while (elapsed < cameraBlendDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / cameraBlendDuration);
                float easedT = EaseInOutCubic(t);

                // Camera state stays FIXED during blend (no mid-blend toggles)
                // Only UI fades smoothly
                if (uiCanvasGroup != null)
                    uiCanvasGroup.alpha = Mathf.Lerp(startUIAlpha, targetUIAlpha, EaseInOutCubic(t));

                yield return null;
            }

            mainPlayerCamera.enabled = !entering;
            wardrobeCamera.enabled = entering;
            
            if (uiCanvasGroup != null)
                uiCanvasGroup.alpha = entering ? 1f : 0f;
            
            if (!entering && wardrobeUIPanel != null)
                wardrobeUIPanel.SetActive(false);

            if (entering && wardrobeUI != null)
                wardrobeUI.ForceRefreshMirror();

            blendCoroutine = null;
        }

        private IEnumerator AuditCamerasDuringBlend()
        {
            for (int i = 0; i < 20; i++)  // Audit for ~1 second
            {
                int enabledCount = 0;
                foreach (var cam in Camera.allCameras)
                {
                    if (cam.enabled)
                    {
                        enabledCount++;
                        var uacd = cam.GetComponent<UniversalAdditionalCameraData>();
                        Debug.Log($"[AUDIT] Active Camera: {cam.name}, depth={cam.depth}, targetTexture={cam.targetTexture?.name ?? "screen"}, renderType={cam.GetComponent<UniversalAdditionalCameraData>()?.renderType}");
                    }
                }
                Debug.Log($"[AUDIT] Total enabled cameras: {enabledCount} (expected: 2 during wardrobe - WardrobeCamera + MirrorInnerCam)");
                yield return new WaitForSeconds(0.05f);
            }
        }

        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        private void PositionPlayerToMirror()
        {
            if (playerControl == null || mirrorCamera == null) return;

            Transform mirror = mirrorCamera.transform;
            Vector3 faceNormal = mirror.forward;
            Vector3 target = mirror.position + faceNormal * 3.4f;

            if (Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f))
                target.y = hit.point.y + 0.5f;

            Quaternion facingMirror = Quaternion.LookRotation(-faceNormal, Vector3.up);

            Rigidbody rb = playerControl.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.position = target;
                rb.rotation = facingMirror;
            }
            else
            {
                playerControl.transform.position = target;
                playerControl.transform.rotation = facingMirror;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!isInWardrobeMode) return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ExitWardrobeMode();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion
    }
}