using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target; // Karakter yang akan diikuti
    public Vector3 offset = new Vector3(-10f, 10f, -10f); // Jarak default kamera isometrik

    [Header("Pengaturan Kehalusan")]
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Tentukan posisi tujuan kamera
        Vector3 desiredPosition = target.position + offset;

        // Buat transisi mulus dari posisi sekarang ke posisi tujuan (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Terapkan posisi baru ke kamera
        transform.position = smoothedPosition;
    }
}