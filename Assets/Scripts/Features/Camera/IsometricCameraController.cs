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

        [Tooltip("Offset applied to target position to center player character nicely (e.g. chest/center).")]
        public Vector3 targetOffset = new Vector3(0f, 1.0f, 0f);

        [Tooltip("Camera offset from target (isometric default).")]
        public Vector3 offset = new Vector3(-10f, 10f, -10f);

        [Tooltip("Smooth follow speed.")]
        public float smoothSpeed = 5f;

        [Header("Orbit Controls")]
        [Tooltip("Camera pitch angle (vertical).")]
        [Range(10f, 85f)]
        public float pitch = 30f;

        [Tooltip("Camera yaw angle (horizontal).")]
        public float yaw = 45f;

        [Tooltip("Distance from target.")]
        public float distance = 20f;

        [Tooltip("Enable mouse right-drag orbit.")]
        public bool allowMouseOrbit = true;

        [Header("Zoom")]
        [Tooltip("Scroll wheel zoom speed.")]
        public float zoomSpeed = 4f;

        [Tooltip("Minimum orthographic size.")]
        public float minSize = 4f;

        [Tooltip("Maximum orthographic size.")]
        public float maxSize = 12f;

        [Header("Projection")]
        [Tooltip("Use orthographic projection (eliminates perspective narrowing).")]
        public bool isOrthographic = true;

        [Tooltip("Orthographic camera size.")]
        public float orthographicSize = 6f;

        public static IsometricCameraController Instance { get; private set; }

        private Camera cam;
        private Vector3 shakeOffset = Vector3.zero;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Jaga instance utama
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Menggetarkan kamera secara halus saat pemain terkena hit (subtle impact shudder).
        /// </summary>
        public void TriggerShake(float duration = 0.14f, float intensity = 0.22f)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(RoutineShake(duration, intensity));
        }

        private System.Collections.IEnumerator RoutineShake(float duration, float intensity)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                Vector2 r = Random.insideUnitCircle;
                shakeOffset = new Vector3(r.x, r.y * 0.4f, r.x * 0.4f) * (intensity * damp);
                yield return null;
            }
            shakeOffset = Vector3.zero;
            shakeCoroutine = null;
        }

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
                if (orthographicSize > 0f)
                    cam.orthographicSize = orthographicSize;
                else if (cam.orthographicSize > 0f)
                    orthographicSize = cam.orthographicSize;
            }
        }

        private void LateUpdate()
        {
            // Guard: Only run in Gameplay mode
            if (CameraManager.Instance != null && CameraManager.Instance.CurrentMode != CameraManager.CameraMode.Gameplay)
                return;

            if (target == null)
                return;

            // Handle orbit input (new Input System only)
            bool isOrbiting = false;
            float deltaX = 0f;
            float deltaY = 0f;
            float scrollDelta = 0f;

            if (Mouse.current != null)
            {
                isOrbiting = Mouse.current.middleButton.isPressed || (Keyboard.current != null && Keyboard.current.altKey.isPressed && Mouse.current.rightButton.isPressed);
                Vector2 delta = Mouse.current.delta.ReadValue();
                deltaX = delta.x * 0.2f;
                deltaY = delta.y * 0.2f;

                Vector2 scroll = Mouse.current.scroll.ReadValue();
                scrollDelta = scroll.y * 0.005f;
            }

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
            Vector3 targetCenter = target.position + targetOffset;
            Vector3 targetPosition = targetCenter - (rotation * Vector3.forward * distance);

            // Smooth follow + Shake offset
            Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

            Vector3 managerShakePos = Vector3.zero;
            Quaternion managerShakeRot = Quaternion.identity;
            if (CameraManager.Instance != null)
            {
                managerShakePos = CameraManager.Instance.CurrentShakeOffset;
                managerShakeRot = CameraManager.Instance.CurrentShakeRotation;
            }

            transform.position = smoothedPos + shakeOffset + managerShakePos;
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation * managerShakeRot, Time.deltaTime * smoothSpeed);
        }

        /// <summary>
        /// Instantly snaps the camera to the player target position without smoothing.
        /// Essential when loading save games or teleporting between zones.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null) target = player.transform;
            }
            if (target == null) return;

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 targetCenter = target.position + targetOffset;
            Vector3 targetPosition = targetCenter - (rotation * Vector3.forward * distance);
            transform.position = targetPosition;
            transform.rotation = rotation;
        }
    }
}
