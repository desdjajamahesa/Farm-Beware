using UnityEngine;
using UnityEngine.Rendering.Universal;
using FeaturesCamera;

namespace FeaturesWardrobe
{
    public class MirrorCamera : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Camera mirrorCamera;
        [SerializeField] private RenderTexture mirrorTexture;
        [SerializeField] private Renderer surfaceRenderer; // MeshRenderer cermin (tampilkan RT live)
        [SerializeField] private int textureSize = 1024;
        
        [Header("Target")]
        [SerializeField] private Transform playerTarget; // Player head/body untuk face
        
        [Header("Positioning")]
        [Tooltip("Jarak kamera dari permukaan cermin (dalam satuan dunia).")]
        [SerializeField] private float distanceFromMirror = 1.8f;
        [Tooltip("Offset vertikal dari posisi player (untuk face ke wajah).")]
        [SerializeField] private float verticalOffset = 0.1f;
        [Tooltip("Tinggi bidikan minimal di atas titik target (agar tidak membidik kaki).")]
        [SerializeField] private float aimHeightOffset = 1.25f;
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

        private void Awake()
        {
            // Ambil kamera dari child (MirrorInnerCam) jika di Inspector kosong
            if (mirrorCamera == null)
            {
                mirrorCamera = GetComponentInChildren<Camera>();
            }

            // Pastikan mirrorSurface memiliki nilai agar kamera bisa dikalkulasi posisinya
            if (mirrorSurface == null)
            {
                mirrorSurface = this.transform;
            }

            // Matikan komponen kamera di objek Induk (jika tersisa dari bug sebelumnya)
            Camera parentCam = GetComponent<Camera>();
            if (parentCam != null && parentCam != mirrorCamera)
            {
                parentCam.enabled = false;
            }

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
                    if (uacd.cameraStack != null && uacd.cameraStack.Count > 0)
                    {
                        uacd.cameraStack.Clear();
                    }
                }
            }

            InitializeRenderTexture();
            ConfigureCamera();
            BindSurfaceTexture();
        }

        private void InitializeRenderTexture()
        {
            // FIX layar putih: JANGAN Release()/re-create RT bila sudah terisi (asset).
            // Identity RT harus stabil agar RawImage & kameranya selalu memakai objek yang sama.
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
                _textureCreatedByScript = false; // Inspector-assigned
            }

            if (mirrorCamera != null)
                mirrorCamera.targetTexture = mirrorTexture;
        }

        private void ConfigureCamera()
        {
            if (mirrorCamera == null) return;

            // Properties already set in Awake() for early initialization
            // DO NOT enable camera here - WardrobeManager controls when to enable
            // mirrorCamera.enabled = true; // REMOVED: causes auto-enable at startup
        }

        /// <summary>Pastikan RenderTexture + targetTexture siap (idempoten). Dipanggil dari WardrobeManager saat Enter.</summary>
        public void EnsureInitialized()
        {
            if (mirrorTexture == null)
            {
                InitializeRenderTexture();
            }
            if (mirrorCamera != null && mirrorCamera.targetTexture != mirrorTexture)
                mirrorCamera.targetTexture = mirrorTexture;
            BindSurfaceTexture();
        }

        /// <summary>Tempel RenderTexture ke permukaan cermin (refleksi dunia live).</summary>
        private void BindSurfaceTexture()
        {
            if (surfaceRenderer == null || mirrorTexture == null) return;
            surfaceRenderer.material.mainTexture = mirrorTexture;
        }

        private void LateUpdate()
        {
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
            if (CameraManager.Instance == null || CameraManager.Instance.CurrentMode != CameraManager.CameraMode.WardrobeMode)
                return;

            if (!isInitialized || mirrorCamera == null || playerTarget == null || mirrorSurface == null)
                return;

            // Position camera at mirror surface, facing player
            // mirrorSurface is the static mirror surface Transform (NOT this camera's transform)
            Vector3 mirrorForward = -mirrorSurface.forward; // Cermin face ke player
            Vector3 cameraPosition = mirrorSurface.position + mirrorForward * distanceFromMirror;

            mirrorCamera.transform.position = cameraPosition;

            // Face player (dengan tinggi bidikan minimal di atas kaki/root).
            float aimHeight = Mathf.Max(verticalOffset, aimHeightOffset);
            Vector3 lookTarget = playerTarget.position + Vector3.up * aimHeight;
            Vector3 direction = (lookTarget - cameraPosition).normalized;

            // Keep camera upright (no roll) — only yaw/pitch
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            mirrorCamera.transform.rotation = targetRotation;
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