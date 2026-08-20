using UnityEngine;

/// <summary>
/// Makes a UI element always face the main camera (billboard effect).
/// </summary>
public class FaceCamera : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        // Billboard: face towards the camera
        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                          mainCam.transform.rotation * Vector3.up);
    }
}
