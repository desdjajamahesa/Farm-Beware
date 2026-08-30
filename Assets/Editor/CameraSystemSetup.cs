using UnityEngine;
using UnityEditor;
using FeaturesCamera;
using FeaturesWardrobe;

/// <summary>
/// Automated setup for CameraManager + Wardrobe camera system.
/// Ensures proper separation between screen camera (WardrobeCamera) and texture camera (MirrorInnerCam).
/// Menu: Farm Beware > Camera > Wire Camera System
/// </summary>
public static class CameraSystemSetup
{
    [MenuItem("Farm Beware/Camera/Wire Camera System")]
    public static void WireCameraSystem()
    {
        if (!EditorUtility.DisplayDialog("Wire Camera System",
            "This will create CameraManager, WardrobeCamera, and wire references.\n\n" +
            "Requirements:\n" +
            "- Main Camera exists\n" +
            "- Mirror GameObject with MirrorCamera component exists\n" +
            "- TrophyCabinetSystem exists (for trophy camera)\n\n" +
            "Continue?", "Yes", "Cancel"))
            return;

        // 1. Find or create CameraManager
        CameraManager cm = Object.FindObjectOfType<CameraManager>();
        if (cm == null)
        {
            GameObject cmGO = new GameObject("CameraManager");
            cm = cmGO.AddComponent<CameraManager>();
            Debug.Log("[Setup] Created CameraManager");
        }

        // 2. Find Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[Setup] Main Camera not found!");
            return;
        }

        // 3. Find or add IsometricCameraController to Main Camera
        IsometricCameraController isoController = mainCamera.GetComponent<IsometricCameraController>();
        if (isoController == null)
        {
            isoController = mainCamera.gameObject.AddComponent<IsometricCameraController>();
            Debug.Log("[Setup] Added IsometricCameraController to Main Camera");
        }

        // 4. Find or create WardrobeRoot
        Transform wardrobeRoot = GameObject.Find("WardrobeRoot")?.transform;
        if (wardrobeRoot == null)
        {
            GameObject wrGO = new GameObject("WardrobeRoot");
            wardrobeRoot = wrGO.transform;
            wardrobeRoot.position = new Vector3(14f, 0f, 0f); // Default bedroom position
            Debug.Log("[Setup] Created WardrobeRoot");
        }

        // 5. Find or create WardrobeCamera (SCREEN camera, separate from MirrorCamera)
        Transform wardrobeCameraT = wardrobeRoot.Find("WardrobeCamera");
        Camera wardrobeCamera = null;
        if (wardrobeCameraT == null)
        {
            GameObject wcGO = new GameObject("WardrobeCamera");
            wcGO.transform.SetParent(wardrobeRoot);
            wcGO.transform.localPosition = new Vector3(-2.76f, 1.655f, -2.62f);
            wcGO.transform.localRotation = Quaternion.Euler(8f, 60f, 0f);

            wardrobeCamera = wcGO.AddComponent<Camera>();
            wardrobeCamera.clearFlags = CameraClearFlags.Skybox;
            wardrobeCamera.fieldOfView = 60f;
            wardrobeCamera.nearClipPlane = 0.3f;
            wardrobeCamera.farClipPlane = 1000f;
            wardrobeCamera.enabled = false; // CameraManager controls this

            // CRITICAL: No targetTexture (renders to screen)
            wardrobeCamera.targetTexture = null;

            Debug.Log("[Setup] Created WardrobeCamera (screen camera)");
        }
        else
        {
            wardrobeCamera = wardrobeCameraT.GetComponent<Camera>();
            if (wardrobeCamera == null)
            {
                wardrobeCamera = wardrobeCameraT.gameObject.AddComponent<Camera>();
                wardrobeCamera.enabled = false;
            }

            // Ensure no targetTexture
            wardrobeCamera.targetTexture = null;
            Debug.Log("[Setup] Found existing WardrobeCamera");
        }

        // 6. Find MirrorCamera component (for RenderTexture camera)
        MirrorCamera mirrorCam = Object.FindObjectOfType<MirrorCamera>();
        if (mirrorCam == null)
        {
            Debug.LogWarning("[Setup] MirrorCamera component not found! Wardrobe mirror won't work.");
        }

        // 7. Find Trophy system
        Transform trophySystemRoot = GameObject.Find("TrophyCabinetSystem")?.transform;
        if (trophySystemRoot == null)
        {
            Debug.LogWarning("[Setup] TrophyCabinetSystem not found! Trophy mode won't work.");
        }

        Camera trophyCamera = trophySystemRoot?.Find("TrophyCamera")?.GetComponent<Camera>();
        if (trophyCamera == null && trophySystemRoot != null)
        {
            // Create trophy camera if missing
            GameObject tcGO = new GameObject("TrophyCamera");
            tcGO.transform.SetParent(trophySystemRoot);
            tcGO.transform.localPosition = new Vector3(-0.15f, 1.5f, 3f);
            tcGO.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);

            trophyCamera = tcGO.AddComponent<Camera>();
            trophyCamera.fieldOfView = 60f;
            trophyCamera.nearClipPlane = 0.1f;
            trophyCamera.farClipPlane = 100f;
            trophyCamera.enabled = false;

            Debug.Log("[Setup] Created TrophyCamera");
        }

        // 8. Find PlayerControl
        PlayerControl playerControl = Object.FindObjectOfType<PlayerControl>();
        if (playerControl == null)
        {
            Debug.LogWarning("[Setup] PlayerControl not found! Input locking won't work.");
        }

        // 9. Wire CameraManager references
        SerializedObject cmSO = new SerializedObject(cm);
        cmSO.FindProperty("mainCamera").objectReferenceValue = mainCamera;
        cmSO.FindProperty("isometricCameraController").objectReferenceValue = isoController;
        cmSO.FindProperty("trophyCamera").objectReferenceValue = trophyCamera;
        cmSO.FindProperty("wardrobeCamera").objectReferenceValue = wardrobeCamera;
        cmSO.FindProperty("trophySystemRoot").objectReferenceValue = trophySystemRoot;
        cmSO.FindProperty("wardrobeRoot").objectReferenceValue = wardrobeRoot;
        cmSO.FindProperty("playerControl").objectReferenceValue = playerControl;
        cmSO.ApplyModifiedProperties();

        Debug.Log("[Setup] Wired CameraManager references");

        // 10. Find and wire WardrobeManager
        WardrobeManager wm = Object.FindObjectOfType<WardrobeManager>();
        if (wm != null)
        {
            SerializedObject wmSO = new SerializedObject(wm);
            wmSO.FindProperty("wardrobeRoot").objectReferenceValue = wardrobeRoot;
            wmSO.FindProperty("mirrorCamera").objectReferenceValue = mirrorCam;
            wmSO.FindProperty("mainCamera").objectReferenceValue = mainCamera;
            wmSO.FindProperty("wardrobeCamera").objectReferenceValue = wardrobeCamera;
            wmSO.FindProperty("playerControl").objectReferenceValue = playerControl;
            wmSO.ApplyModifiedProperties();

            Debug.Log("[Setup] Wired WardrobeManager references");
        }
        else
        {
            Debug.LogWarning("[Setup] WardrobeManager not found in scene!");
        }

        EditorUtility.SetDirty(cm.gameObject);
        if (wm != null) EditorUtility.SetDirty(wm.gameObject);

        EditorUtility.DisplayDialog("Setup Complete",
            "Camera system wired!\n\n" +
            "Next steps:\n" +
            "1. Verify MirrorCamera component exists on Mirror GameObject\n" +
            "2. Test Play Mode → E on Wardrobe\n" +
            "3. Camera should stay at side view (not snap to mirror surface)\n\n" +
            "Check console for any warnings.", "OK");
    }

    [MenuItem("Farm Beware/Camera/Validate Camera Setup")]
    public static void ValidateCameraSetup()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Camera System Validation ===\n");

        // Check CameraManager
        var cm = Object.FindObjectOfType<CameraManager>();
        report.AppendLine($"CameraManager: {(cm != null ? "✓ Found" : "✗ MISSING")}");
        if (cm != null)
        {
            var so = new SerializedObject(cm);
            var mainCam = so.FindProperty("mainCamera").objectReferenceValue as Camera;
            var isoCont = so.FindProperty("isometricCameraController").objectReferenceValue;
            var tropCam = so.FindProperty("trophyCamera").objectReferenceValue as Camera;
            var wardCam = so.FindProperty("wardrobeCamera").objectReferenceValue as Camera;
            var tropRoot = so.FindProperty("trophySystemRoot").objectReferenceValue;
            var wardRoot = so.FindProperty("wardrobeRoot").objectReferenceValue;
            var playerCtrl = so.FindProperty("playerControl").objectReferenceValue;

            report.AppendLine($"  mainCamera: {(mainCam != null ? "✓" : "✗")}");
            report.AppendLine($"  isometricCameraController: {(isoCont != null ? "✓" : "✗")}");
            report.AppendLine($"  trophyCamera: {(tropCam != null ? "✓" : "✗")}");
            report.AppendLine($"  wardrobeCamera: {(wardCam != null ? "✓" : "✗")}");

            if (wardCam != null && wardCam.targetTexture != null)
            {
                report.AppendLine($"  ⚠ wardrobeCamera has targetTexture! Should be NULL (screen camera)");
            }

            report.AppendLine($"  trophySystemRoot: {(tropRoot != null ? "✓" : "✗")}");
            report.AppendLine($"  wardrobeRoot: {(wardRoot != null ? "✓" : "✗")}");
            report.AppendLine($"  playerControl: {(playerCtrl != null ? "✓" : "✗")}");
        }

        // Check MirrorCamera
        var mirrorCam = Object.FindObjectOfType<MirrorCamera>();
        report.AppendLine($"\nMirrorCamera component: {(mirrorCam != null ? "✓ Found" : "✗ MISSING")}");
        if (mirrorCam != null)
        {
            var innerCam = mirrorCam.MirrorCameraComponent;
            report.AppendLine($"  mirrorCamera (inner): {(innerCam != null ? "✓" : "✗")}");
            if (innerCam != null)
            {
                report.AppendLine($"  targetTexture: {(innerCam.targetTexture != null ? "✓ Has RenderTexture" : "✗ NULL")}");
            }
        }

        // Check WardrobeManager
        var wm = Object.FindObjectOfType<WardrobeManager>();
        report.AppendLine($"\nWardrobeManager: {(wm != null ? "✓ Found" : "✗ MISSING")}");

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Validation Report", report.ToString(), "OK");
    }
}
