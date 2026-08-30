using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using FarmBeware.Logic;

namespace FeaturesWardrobe
{
    /// <summary>
    /// Manages the 3D avatar preview in the Wardrobe UI.
    /// Renders a cloned player avatar to a RenderTexture displayed via RawImage.
    /// Supports drag-to-rotate view and dynamic mesh/material swapping when items are selected.
    /// </summary>
    public class PreviewController : MonoBehaviour
    {
        [Header("Camera & RenderTarget Setup")]
        [SerializeField] private RawImage previewRawImage;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private RenderTexture previewRenderTexture;
        [SerializeField] private Transform avatarRoot;

        [Header("Preview Control")]
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private Vector2 rotateSensitivity = new Vector2(1f, 1f);

        [Header("Avatar Setup")]
        [SerializeField] private GameObject defaultAvatarPrefab;
        [SerializeField] private LayerMask previewLayer = ~0; // Default: collide with everything except UI

        [Header("Interaction Settings")]
        [SerializeField] private bool allowDragRotate = true;
        [SerializeField] private bool autoRotate = false; // Disabled by default to prevent auto-spin

        private GameObject avatarClone;
        private Quaternion originalLocalRotation;
        private Vector2 lastMousePos;
        private bool isRotating = false;

        // Cached components
        private SkinnedMeshRenderer avatarRenderer;
        private Transform cachedTransform;

        #region Properties

        public RenderTexture PreviewRenderTexture => previewRenderTexture;
        public Camera PreviewCamera => previewCamera;
        public Transform AvatarRoot => avatarRoot;
        public GameObject AvatarClone => avatarClone;

        #endregion

        private void Awake()
        {
            // Idempotent: if already initialized, skip
            if (previewCamera != null && previewRenderTexture != null && previewRenderTexture.IsCreated() && avatarClone != null)
                return;

            InitializeInternal();
        }

        /// <summary>
        /// Public initialization method - can be called manually if Awake didn't run.
        /// </summary>
        public void Initialize()
        {
            InitializeInternal();
        }

        private void InitializeInternal()
        {
            // Ensure we have a render texture if not assigned
            if (previewRenderTexture == null)
            {
                previewRenderTexture = new RenderTexture(512, 512, 24);
                previewRenderTexture.name = "WardrobePreviewRT";
            }
            if (!previewRenderTexture.IsCreated())
                previewRenderTexture.Create();

            // Ensure we have a preview camera if not assigned
            if (previewCamera == null)
            {
                GameObject camObj = new GameObject("WardrobePreviewCamera", typeof(Camera));
                previewCamera = camObj.GetComponent<Camera>();
                camObj.transform.SetParent(transform, false);
                camObj.isStatic = false;
            }

            // Configure preview camera
            ConfigurePreviewCamera();

            // Initialize avatar
            InitializeAvatar();
        }

        private void OnValidate()
        {
            // Editor-only: warn if refs missing
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (defaultAvatarPrefab == null)
                    Debug.LogWarning($"[PreviewController] {name}: defaultAvatarPrefab not assigned in Inspector");
            }
#endif
        }

        private void OnEnable()
        {
            // Apply render texture to RawImage
            if (previewRawImage != null && previewRenderTexture != null)
                previewRawImage.texture = previewRenderTexture;
        }

        private void OnDisable()
        {
            if (autoRotate)
                StopAllCoroutines();
        }

        private void ConfigurePreviewCamera()
        {
            if (previewCamera == null) return;

            previewCamera.targetTexture = previewRenderTexture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark gray
            previewCamera.farClipPlane = 50f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.cullingMask = previewLayer; // Only render preview layer
            previewCamera.depth = -10f; // Lower priority than main camera
            previewCamera.useOcclusionCulling = false;

            // Disable AudioListener to avoid "2 audio listeners" warning
            var audioListener = previewCamera.GetComponent<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = false;

            // Position camera
            previewCamera.transform.localPosition = new Vector3(0f, 3f, 8f);
            previewCamera.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        }

        private void InitializeAvatar()
        {
            if (avatarRoot == null) return;

            // Remove any existing clone
            if (avatarClone != null)
            {
                Destroy(avatarClone);
                avatarClone = null;
            }

            // Instantiate default avatar if we have a prefab
            if (defaultAvatarPrefab != null)
            {
                avatarClone = Instantiate(defaultAvatarPrefab, avatarRoot);
                avatarClone.name = "WardrobePreviewAvatar";
                avatarClone.transform.localPosition = new Vector3(0f, 1.5f, 3f);
                avatarClone.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                // Try to find existing player/character in scene
                avatarClone = GameObject.Find("Player/character");
                if (avatarClone != null)
                {
                    avatarClone.transform.SetParent(avatarRoot, false);
                    avatarClone.transform.localPosition = new Vector3(0f, 1.5f, 3f);
                }
            }

            // Cache renderer
            if (avatarClone != null)
            {
                avatarRenderer = avatarClone.GetComponentInChildren<SkinnedMeshRenderer>(true);
                cachedTransform = avatarClone.transform;
            }
        }

        private void LateUpdate()
        {
            if (previewCamera == null || avatarClone == null || avatarRenderer == null) return;

            // Apply render texture to RawImage
            if (previewRawImage != null)
                previewRawImage.texture = previewRenderTexture;

            // Handle drag-to-rotate
            if (allowDragRotate)
            {
                HandleDragRotate();
            }
        }

        private void HandleDragRotate()
        {
            // Check if mouse is over the RawImage
            var mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                previewRawImage.rectTransform, mousePos))
            {
                isRotating = false;
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isRotating = true;
                lastMousePos = mousePos;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isRotating = false;
            }

            if (!isRotating) return;

            Vector2 currentMousePos = mousePos;
            Vector2 delta = currentMousePos - lastMousePos;
            lastMousePos = currentMousePos;

            // Rotate avatar root around Y axis (orbit)
            if (avatarRoot != null)
            {
                float rotX = delta.x * rotateSensitivity.x * rotationSpeed * Time.unscaledDeltaTime;
                avatarRoot.Rotate(Vector3.up, -rotX, Space.World);
            }
        }

        /// <summary>
        /// Swaps the avatar's appearance based on the given WardrobeItemData.
        /// </summary>
        /// <param itemData>The item data defining which mesh/prefab to show</param>
        public void SetAvatarAppearance(WardrobeItemData itemData)
        {
            if (itemData == null || avatarRenderer == null || avatarClone == null) return;

            // If the item has a specific preview prefab, use it
            if (itemData.previewPrefab != null)
            {
                // Replace the avatar clone with the item's prefab
                Destroy(avatarClone);
                avatarClone = Instantiate(itemData.previewPrefab, avatarRoot);
                avatarClone.name = $"WardrobePreview_{itemData.displayName}";
                avatarClone.transform.localPosition = new Vector3(0f, 1.5f, 3f);
                avatarClone.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

                // Update cached renderer
                avatarRenderer = avatarClone.GetComponentInChildren<SkinnedMeshRenderer>(true);
                cachedTransform = avatarClone.transform;
            }
            else
            {
                // Just reset rotation, keep base appearance
                if (cachedTransform != null)
                {
                    cachedTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                }
            }
        }

        /// <summary>
        /// Centers the camera view on the avatar.
        /// </summary>
        public void CenterView()
        {
            if (avatarRoot == null || avatarClone == null) return;

            // Position camera behind and above the avatar
            if (previewCamera != null)
            {
                previewCamera.transform.localPosition = new Vector3(0f, 3f, 8f);
                previewCamera.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            }

            if (avatarRoot != null)
            {
                avatarRoot.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        /// <summary>
        /// Rotates the avatar to a specific Y angle (e.g., when selecting from grid).
        /// </summary>
        public void SetAvatarRotation(float yAngle)
        {
            if (avatarRoot != null)
            {
                avatarRoot.localRotation = Quaternion.Euler(0f, yAngle, 0f);
            }
        }

        /// <summary>
        /// Updates the avatar's SkinnedMeshRenderer to match a specific outfit configuration.
        /// Used when applying a complete OutfitData rather than a single item.
        /// </summary>
        public void ApplyOutfitToPreview(OutfitData outfit)
        {
            if (outfit == null || avatarClone == null) return;

            var renderers = avatarClone.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers)
                r.enabled = false; // disable all first

            // Enable body + hair always
            foreach (var r in renderers)
            {
                if (r.name == "body" || r.name == "hair1" || r.name == "hair2")
                    r.enabled = true;
            }

            // Apply outfit variants using OutfitData's logic
            outfit.ApplyToCharacter(avatarClone);

            // Update cached renderer
            avatarRenderer = avatarClone.GetComponentInChildren<SkinnedMeshRenderer>(true);
        }

        /// <summary>
        /// Creates a fresh avatar from the default prefab or scene reference.
        /// </summary>
        public void ResetAvatar()
        {
            InitializeAvatar();
            CenterView();
        }

        /// <summary>
        /// Called when the PreviewController is attached to a new RawImage.
        /// </summary>
        public void BindToRawImage(RawImage rawImage)
        {
            previewRawImage = rawImage;
            if (previewRawImage != null && previewRenderTexture != null)
                previewRawImage.texture = previewRenderTexture;
        }

        private void OnDestroy()
        {
            // Cleanup render texture
            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }

            // Cleanup camera
            if (previewCamera != null)
                Destroy(previewCamera.gameObject);

            // Cleanup avatar
            if (avatarClone != null)
                Destroy(avatarClone);
        }
    }
}