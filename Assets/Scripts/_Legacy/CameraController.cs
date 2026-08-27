using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Isometric camera follow script with Orthographic projection,
/// 100% straight parallel wall rendering (no narrowing distortion),
/// wall occlusion fading, and mouse orbit/zoom controls.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target & Isometric Follow")]
    public Transform target;

    [Tooltip("Camera Pitch Angle")]
    [Range(10f, 85f)]
    public float pitch = 35.264f;

    [Tooltip("Camera Yaw Angle")]
    public float yaw = 0.0f;

    [Tooltip("Distance from capsule target")]
    public float distance = 34.0f;

    [Tooltip("Smooth follow speed")]
    public float smoothSpeed = 10.0f;

    [Header("Projection Mode")]
    [Tooltip("Set camera to Orthographic Isometric view (eliminates perspective narrowing)")]
    public bool isOrthographic = true;
    public float orthographicSize = 19.5f;

    [Header("Orbit & Zoom")]
    public bool allowMouseOrbit = true;
    public float zoomSpeed = 4.0f;
    public float minSize = 6.0f;
    public float maxSize = 45.0f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (target == null)
        {
            GameObject player = GameObject.Find("PlayerCapsule");
            if (player != null) target = player.transform;
        }

        // Apply Orthographic Camera Projection to keep left and right walls 100% straight
        if (cam != null)
        {
            cam.orthographic = isOrthographic;
            cam.orthographicSize = orthographicSize;
        }

        if (GetComponent<WallOcclusionFader>() == null)
        {
            WallOcclusionFader fader = gameObject.AddComponent<WallOcclusionFader>();
            fader.target = target;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

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
                    deltaX = Input.GetAxis("Mouse X") * 4.0f;
                    deltaY = Input.GetAxis("Mouse Y") * 4.0f;
                }
                scrollDelta = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            }
            catch
            {
                // Ignore legacy input exception
            }
        }
        #endif

        if (allowMouseOrbit && isOrbiting)
        {
            yaw += deltaX;
            pitch -= deltaY;
            pitch = Mathf.Clamp(pitch, 10.0f, 85.0f);
        }

        if (Mathf.Abs(scrollDelta) > 0.001f && cam != null)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scrollDelta * 2.0f, minSize, maxSize);
            }
            else
            {
                distance = Mathf.Clamp(distance - scrollDelta, 6.0f, 30.0f);
            }
        }

        // Calculate Camera Position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetCenter = target.position;
        Vector3 targetPosition = targetCenter - (rotation * Vector3.forward * distance);

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * smoothSpeed);
    }
}
