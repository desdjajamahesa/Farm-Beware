using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FeaturesCamera
{
    /// <summary>
    /// Unified isometric camera controller (merges IsometricCamera + CameraController).
    /// Features: smooth follow, mouse orbit (right-drag), zoom (scroll), orthographic projection.
    /// ONLY active in Gameplay mode (CameraManager enforces).
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Target & Follow")]
        [Tooltip("Player transform to follow.")]
        public Transform target;

        [Tooltip("Camera offset from target (isometric default).")]
        public Vector3 offset = new Vector3(-10f, 10f, -10f);

        [Tooltip("Smooth follow speed.")]
        public float smoothSpeed = 5f;

        [Header("Orbit Controls")]
        [Tooltip("Camera pitch angle (vertical).")]
        [Range(10f, 85f)]
        public float pitch = 35.264f;

        [Tooltip("Camera yaw angle (horizontal).")]
        public float yaw = 0f;

        [Tooltip("Distance from target.")]
        public float distance = 34f;

        [Tooltip("Enable mouse right-drag orbit.")]
        public bool allowMouseOrbit = true;

        [Header("Zoom")]
        [Tooltip("Scroll wheel zoom speed.")]
        public float zoomSpeed = 4f;

        [Tooltip("Minimum orthographic size.")]
        public float minSize = 6f;

        [Tooltip("Maximum orthographic size.")]
        public float maxSize = 45f;

        [Header("Projection")]
        [Tooltip("Use orthographic projection (eliminates perspective narrowing).")]
        public bool isOrthographic = true;

        [Tooltip("Orthographic camera size.")]
        public float orthographicSize = 19.5f;

        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                cam = Camera.main;

            if (target == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player == null)
                    player = GameObject.Find("PlayerCapsule");
                if (player != null)
                    target = player.transform;
            }

            // Apply orthographic projection
            if (cam != null)
            {
                cam.orthographic = isOrthographic;
                cam.orthographicSize = orthographicSize;
            }

            // Ensure WallOcclusionFader is present (from CameraController)
            if (GetComponent<WallOcclusionFader>() == null)
            {
                WallOcclusionFader fader = gameObject.AddComponent<WallOcclusionFader>();
                fader.target = target;
            }
        }

        private void LateUpdate()
        {
            // Guard: Only run in Gameplay mode
            if (CameraManager.Instance != null && CameraManager.Instance.CurrentMode != CameraManager.CameraMode.Gameplay)
                return;

            if (target == null)
                return;

            // Handle orbit input
            bool isOrbiting = false;
            float deltaX = 0f;
            float deltaY = 0f;
            float scrollDelta = 0f;

            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                isOrbiting = Mouse.current.rightButton.isPressed;
                Vector2 delta = Mouse.current.delta.ReadValue();
                deltaX = delta.x * 0.2f;
                deltaY = delta.y * 0.2f;

                Vector2 scroll = Mouse.current.scroll.ReadValue();
                scrollDelta = scroll.y * 0.005f;
            }
            #endif

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (!isOrbiting)
            {
                try
                {
                    if (Input.GetMouseButton(1))
                    {
                        isOrbiting = true;
                        deltaX = Input.GetAxis("Mouse X") * 4f;
                        deltaY = Input.GetAxis("Mouse Y") * 4f;
                    }
                    scrollDelta = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
                }
                catch
                {
                    // Ignore legacy input exception
                }
            }
            #endif

            // Apply orbit
            if (allowMouseOrbit && isOrbiting)
            {
                yaw += deltaX;
                pitch -= deltaY;
                pitch = Mathf.Clamp(pitch, 10f, 85f);
            }

            // Apply zoom
            if (Mathf.Abs(scrollDelta) > 0.001f && cam != null)
            {
                if (cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scrollDelta * 2f, minSize, maxSize);
                }
                else
                {
                    distance = Mathf.Clamp(distance - scrollDelta, 6f, 30f);
                }
            }

            // Calculate camera position (orbit-based)
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 targetCenter = target.position;
            Vector3 targetPosition = targetCenter - (rotation * Vector3.forward * distance);

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * smoothSpeed);
        }
    }
}
