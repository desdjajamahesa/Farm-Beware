using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target; // Target character to follow
    public Vector3 offset = new Vector3(-10f, 10f, -10f); // Default isometric camera offset

    [Header("Pengaturan Kehalusan")]
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Compute target camera position
        Vector3 desiredPosition = target.position + offset;

        // Smoothly interpolate from current position to target position (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Apply new position to the camera
        transform.position = smoothedPosition;
    }
}