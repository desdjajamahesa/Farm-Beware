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
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-2.76f, 1.655f, -2.62f);
        
        [Tooltip("Local rotation offset (Euler) of wardrobeCamera relative to wardrobeRoot.")]
        [SerializeField] private Vector3 cameraLocalRotation = new Vector3(8f, 60f, 0f);

        [Header("Mirror Positioning")]
        [Tooltip("Jarak player dari permukaan cermin saat buka wardrobe.")]
        [SerializeField] private float playerMirrorDistance = 5.0f;

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
            
            var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
            if (isoCam != null)
            {
                isometricCameraWasEnabled = isoCam.enabled;
                isoCam.enabled = false;
                Debug.Log("[WardrobeManager] IsometricCamera disabled during wardrobe mode");
            }
            
            playerControl.isInputLocked = true;
            
            SetupWardrobeCamera();
            
            if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
            {
                mirrorCamera.MirrorCameraComponent.enabled = false;
                Debug.Log("[WardrobeManager] MirrorInnerCam disabled before Enter sequence");
            }
            
            if (mirrorCamera != null)
            {
                mirrorCamera.EnsureInitialized();
                if (playerHead != null)
                    mirrorCamera.SetPlayerTarget(playerHead);
            }
            
            if (blendCoroutine != null) StopCoroutine(blendCoroutine);
            blendCoroutine = StartCoroutine(BlendCamerasAndUI(true));
            
            PositionPlayerToMirror();
            
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
            bool innerCamOn = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null && mirrorCamera.MirrorCameraComponent.enabled;
            bool targetOk = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null &&
                            mirrorCamera.MirrorCameraComponent.targetTexture == mirrorCamera.MirrorTexture;
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

            if (entering)
            {
                if (mirrorCamera != null && mirrorCamera.MirrorTexture != null)
                {
                    mirrorCamera.MirrorCameraComponent.targetTexture = mirrorCamera.MirrorTexture;
                    Debug.Log("[WardrobeManager] MirrorInnerCam targetTexture bound: " + mirrorCamera.MirrorTexture.name);
                }

                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null && mirrorCamera.MirrorCameraComponent.targetTexture == null)
                {
                    Debug.LogError("[WardrobeManager] Mirror camera has no targetTexture! Aborting blend.");
                    yield break;
                }

                mainPlayerCamera.enabled = false;
                
                var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
                if (isoCam != null)
                {
                    isoCam.enabled = false;
                    Debug.Log("[WardrobeManager] IsometricCamera disabled (blend start)");
                }
                
                wardrobeCamera.enabled = true;
                Debug.Log("[WardrobeManager] WardrobeCamera enabled (screen)");
                
                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
                {
                    mirrorCamera.MirrorCameraComponent.enabled = true;
                    Debug.Log("[WardrobeManager] MirrorInnerCam enabled (renders to RT)");
                }
                
                if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
                
                if (wardrobeUI != null) wardrobeUI.ForceRefreshMirror();
            }
            else
            {
                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
                {
                    mirrorCamera.MirrorCameraComponent.enabled = false;
                    Debug.Log("[WardrobeManager] MirrorInnerCam disabled before exit");
                }
                
                wardrobeCamera.enabled = false;
                mainPlayerCamera.enabled = true;
                
                var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
                if (isoCam != null)
                {
                    isoCam.enabled = true;
                    Debug.Log("[WardrobeManager] IsometricCamera re-enabled (blend end)");
                }
                
                if (mirrorCamera != null && mirrorCamera.MirrorTexture != null)
                {
                    mirrorCamera.MirrorCameraComponent.targetTexture = mirrorCamera.MirrorTexture;
                    mirrorCamera.MirrorCameraComponent.enabled = true;
                    Debug.Log("[WardrobeManager] MirrorInnerCam re-enabled for mirror surface");
                }
            }

            Coroutine auditCoroutine = null;
            if (debugCameraAudit)
            {
                auditCoroutine = StartCoroutine(AuditCamerasDuringBlend());
            }

            while (elapsed < cameraBlendDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / cameraBlendDuration);

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
            for (int i = 0; i < 20; i++)
            {
                int enabledCount = 0;
                foreach (var cam in Camera.allCameras)
                {
                    if (cam.enabled)
                    {
                        enabledCount++;
                        Debug.Log($"[AUDIT] Active Camera: {cam.name}, depth={cam.depth}, targetTexture={cam.targetTexture?.name ?? "screen"}");
                    }
                }
                Debug.Log($"[AUDIT] Total enabled cameras: {enabledCount}");
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

            Transform mirrorSurface = mirrorCamera.MirrorSurface;
            if (mirrorSurface == null)
            {
                Debug.LogError("[WardrobeManager] MirrorSurface is null on MirrorCamera!");
                return;
            }
            
            Vector3 faceNormal = mirrorSurface.forward;
            Vector3 target = mirrorSurface.position + faceNormal * playerMirrorDistance;

            Vector3 rayOrigin = target + Vector3.up * 3f;
            bool hitFloor = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f);
            
            if (hitFloor)
            {
                target.y = hit.point.y + 0.5f;
                Debug.Log($"[WardrobeManager] PositionPlayerToMirror: Floor hit at Y={hit.point.y}, placing player at Y={target.y}");
            }
            else
            {
                target.y = 0.5f;
                Debug.LogWarning("[WardrobeManager] PositionPlayerToMirror: Raycast failed! Using fallback Y=0.5f.");
            }

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

            // 1. Update posisi/rotasi kamera wardrobe
            if (wardrobeCamera != null && wardrobeRoot != null)
            {
                wardrobeCamera.transform.localPosition = cameraLocalOffset;
                wardrobeCamera.transform.localRotation = Quaternion.Euler(cameraLocalRotation);
            }

            // 2. Lock/Unlock cursor & handle input tombol Escape
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