using UnityEngine;

namespace FeaturesWardrobe
{
    [RequireComponent(typeof(Camera))]
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

        public RenderTexture MirrorTexture => mirrorTexture;
        public Camera Camera => mirrorCamera;

        private Transform mirrorTransform;
        private bool isInitialized;

        private void Awake()
        {
            mirrorTransform = transform;

            if (mirrorCamera == null)
                mirrorCamera = GetComponent<Camera>();

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
            }

            if (mirrorCamera != null)
                mirrorCamera.targetTexture = mirrorTexture;
        }

        private void ConfigureCamera()
        {
            if (mirrorCamera == null) return;

            // FIX layar putih: gunakan solid gelap, bukan Skybox (RT cermin jadi gelap
            // walau belum ada konten, bukan keputih-putihan).
            mirrorCamera.clearFlags = CameraClearFlags.SolidColor;
            mirrorCamera.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 1f);
            mirrorCamera.fieldOfView = 60f;
            mirrorCamera.nearClipPlane = 0.1f;
            mirrorCamera.farClipPlane = 100f;
            mirrorCamera.useOcclusionCulling = true;
            mirrorCamera.allowHDR = false;
            mirrorCamera.allowMSAA = true;
            mirrorCamera.enabled = true;
        }

        /// <summary>Pastikan RenderTexture + targetTexture siap (idempoten). Dipanggil dari WardrobeManager saat Enter.</summary>
        public void EnsureInitialized()
        {
            if (mirrorTexture == null)
            {
                InitializeRenderTexture();
                return;
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
            if (!isInitialized || mirrorCamera == null || playerTarget == null)
                return;

            // Position camera at mirror surface, facing player
            Vector3 mirrorForward = -mirrorTransform.forward; // Cermin face ke player
            Vector3 cameraPosition = mirrorTransform.position + mirrorForward * distanceFromMirror;

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
            if (mirrorTexture != null)
            {
                mirrorTexture.Release();
                mirrorTexture = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mirrorCamera == null)
                mirrorCamera = GetComponent<Camera>();
        }
#endif
    }
}