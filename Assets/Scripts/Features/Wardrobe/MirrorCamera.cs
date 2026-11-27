using UnityEngine;
using UnityEngine.Rendering.Universal;
using FarmBeware.Core.Runtime;

namespace FeaturesWardrobe
{
    public class MirrorCamera : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Camera mirrorCamera;
        [SerializeField] private RenderTexture mirrorTexture;
        [SerializeField] private Renderer surfaceRenderer; // Mirror MeshRenderer (displays live RT)
        [SerializeField] private int textureSize = 1024;
        
        [Header("Target")]
        [SerializeField] private Transform playerTarget; // Player head/body to face
        
        [Header("Positioning")]
        [Tooltip("Transform permukaan cermin (static) - kamera diposisikan relatif ke ini")]
        [SerializeField] private Transform mirrorSurface;

        [Header("Manual Control")]
        [Tooltip("If true, LateUpdate will NOT auto-position camera. Use this to manually set camera transform in Scene view. Default TRUE for full manual control.")]
        [SerializeField] private bool manualPositioning = true;

        public RenderTexture MirrorTexture => mirrorTexture;
        public Camera MirrorCameraComponent => mirrorCamera ? mirrorCamera : GetComponent<Camera>();
        public Transform MirrorSurface => mirrorSurface;

        private bool isInitialized;
        private bool _textureCreatedByScript = false;
        private AudioListener _mirrorAudioListener;

        public void EnableMirrorCamera(bool enable)
        {
            if (mirrorCamera != null)
            {
                mirrorCamera.gameObject.SetActive(enable);
                mirrorCamera.enabled = enable;
            }
        }

        private void Awake()
        {
            // Cache AudioListener
            if (mirrorCamera != null)
                _mirrorAudioListener = mirrorCamera.GetComponent<AudioListener>();

            // Fetch camera from child (MirrorCamera) if unassigned in Inspector (including inactive)
            if (mirrorCamera == null)
            {
                mirrorCamera = GetComponentInChildren<Camera>(true);
                if (mirrorCamera != null)
                    _mirrorAudioListener = mirrorCamera.GetComponent<AudioListener>();
            }

            // Ensure mirrorSurface is assigned so camera positioning can be calculated
            if (mirrorSurface == null)
            {
                mirrorSurface = this.transform;
            }

            // Disable Camera component on the parent object (if leftover from a previous bug)
            Camera parentCam = GetComponent<Camera>();
            if (parentCam != null && parentCam != mirrorCamera)
            {
                parentCam.enabled = false;
            }

            // Disable AudioListener by default to avoid "2 audio listeners" warning
            if (_mirrorAudioListener != null)
                _mirrorAudioListener.enabled = false;

            if (mirrorCamera != null)
            {
                mirrorCamera.depth = -100;                    // Always render last
                mirrorCamera.clearFlags = CameraClearFlags.SolidColor;
                mirrorCamera.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 1f);
                mirrorCamera.fieldOfView = 60f;
                mirrorCamera.nearClipPlane = 0.1f;
                mirrorCamera.farClipPlane = 100f;
                mirrorCamera.useOcclusionCulling = true;
                mirrorCamera.allowHDR = false;
                mirrorCamera.allowMSAA = true;

                var uacd = mirrorCamera.GetComponent<UniversalAdditionalCameraData>();
                if (uacd != null)
                {
                    uacd.renderType = CameraRenderType.Base;  // Never overlay
                    uacd.renderPostProcessing = false;        // Hardening: exclude mirror reflection camera from post-processing VRAM passes
                    if (uacd.cameraStack != null && uacd.cameraStack.Count > 0)
                    {
                        uacd.cameraStack.Clear();
                    }
                }

                // Default state: DISABLED for performance until player interacts with wardrobe
                mirrorCamera.enabled = false;
                mirrorCamera.gameObject.SetActive(false);
            }

            InitializeRenderTexture();
            ConfigureCamera();
            BindSurfaceTexture();
        }

        private void InitializeRenderTexture()
        {
            // White screen FIX: DO NOT Release()/re-create RT if already assigned (asset).
            // RT identity must remain stable so RawImage & its camera always reference the same object.
            if (mirrorTexture == null)
            {
                mirrorTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
                mirrorTexture.name = "WardrobeMirrorTexture";
                mirrorTexture.filterMode = FilterMode.Bilinear;
                mirrorTexture.wrapMode = TextureWrapMode.Clamp;
                mirrorTexture.Create();
                _textureCreatedByScript = true;  // We created it
            }
            else
            {
                if (!mirrorTexture.IsCreated())
                    mirrorTexture.Create();
                _textureCreatedByScript = false; // Inspector-assigned
            }

            if (mirrorCamera != null)
            {
                mirrorCamera.targetTexture = mirrorTexture;
                mirrorCamera.aspect = 0.5f;
            }
        }

        private void ConfigureCamera()
        {
            if (mirrorCamera == null) return;
            mirrorCamera.aspect = 0.5f;
        }

        /// <summary>Ensures RenderTexture + targetTexture are ready (idempotent). Called from WardrobeManager on Enter.</summary>
        public void EnsureInitialized()
        {
            InitializeRenderTexture();
            if (mirrorCamera != null && mirrorCamera.targetTexture != mirrorTexture)
                mirrorCamera.targetTexture = mirrorTexture;
            if (mirrorCamera != null)
                mirrorCamera.aspect = 0.5f;
            BindSurfaceTexture();
        }

        /// <summary>Binds RenderTexture to the mirror surface (live world reflection).</summary>
        private void BindSurfaceTexture()
        {
            if (surfaceRenderer == null || mirrorTexture == null) return;
            if (surfaceRenderer.sharedMaterial != null)
            {
                surfaceRenderer.sharedMaterial.mainTexture = mirrorTexture;
                if (surfaceRenderer.sharedMaterial.HasProperty("_BaseMap"))
                    surfaceRenderer.sharedMaterial.SetTexture("_BaseMap", mirrorTexture);
            }
        }

        private void LateUpdate()
        {
            // If mirror camera is inactive/disabled, skip all LateUpdate calculations
            if (mirrorCamera == null || !mirrorCamera.enabled || !mirrorCamera.gameObject.activeInHierarchy)
                return;
            // SAFETY: If camera is enabled but has no targetTexture, disable it IMMEDIATELY
            if (mirrorCamera != null && mirrorCamera.enabled && mirrorCamera.targetTexture == null)
            {
                if (mirrorTexture != null)
                {
                    mirrorCamera.targetTexture = mirrorTexture;
                    Debug.LogWarning("[MirrorCamera] LateUpdate: Camera was enabled without targetTexture! Re-bound and keeping enabled.");
                }
                else
                {
                    // No texture available at all - HARD DISABLE
                    mirrorCamera.enabled = false;
                    Debug.LogError("[MirrorCamera] LateUpdate: No targetTexture available! Disabling camera to prevent screen rendering.");
                    return;
                }
            }
            
            // Additional safety: if somehow targetTexture was set to null externally
            if (mirrorCamera != null && mirrorCamera.enabled && mirrorCamera.targetTexture == null)
            {
                mirrorCamera.enabled = false;
                Debug.LogError("[MirrorCamera] LateUpdate: targetTexture became null! Disabling camera.");
                return;
            }

            // Skip auto-positioning if manual mode enabled
            if (manualPositioning)
                return;

            // Only auto-position when CameraManager exists AND we're in WardrobeMode
            var cameraService = ServiceLocator.Resolve<ICameraService>();
            if (cameraService == null || cameraService.CurrentMode != CameraMode.WardrobeMode)
                return;

            if (!isInitialized || mirrorCamera == null || playerTarget == null || mirrorSurface == null)
                return;

            // Position camera directly on mirror surface looking into the room
            Vector3 cameraPosition = mirrorSurface.position + mirrorSurface.forward * 0.02f;
            mirrorCamera.transform.position = cameraPosition;

            // Aim towards player center/torso
            Vector3 lookTarget = playerTarget.position;
            if (playerTarget.name.ToLower().Contains("head"))
            {
                lookTarget += Vector3.down * 0.55f;
            }
            else
            {
                lookTarget += Vector3.up * 0.85f;
            }

            Vector3 direction = (lookTarget - cameraPosition).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                mirrorCamera.transform.rotation = targetRotation;
            }
        }

        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
            isInitialized = true;
        }

        public void SetTextureSize(int size)
        {
            if (size == textureSize) return;
            textureSize = size;
            InitializeRenderTexture();
        }

        private void OnDestroy()
        {
            if (mirrorTexture != null && _textureCreatedByScript)
            {
                mirrorTexture.Release();
                mirrorTexture = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mirrorCamera == null)
                mirrorCamera = GetComponentInChildren<Camera>();
        }
#endif
    }
}