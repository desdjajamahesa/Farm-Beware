using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utility untuk wiring Dual-Inventory Trophy Cabinet secara deterministik.
/// Jalankan via menu: Farm Beware > Trophy System > Wire Scene (4 SnapPoint).
///
/// Yang dilakukan:
///  1. Buat/validasi TrophyCabinetSystem parent container.
///  2. Re-parent Kabinet, Rak_Trophy, & TrophyCamera ke TrophyCabinetSystem.
///  3. Pastikan Rak_Trophy punya InventoryComponent (Rack, 4 slot kosong).
///  4. Tambahkan TrophyRackVisuals pada Rak_Trophy dan wire rackInventory + snapPoints (Snap_1..4).
///  5. Pastikan tiap SnapPoint punya TrophySnapPoint, slotIndex 0..3, Collider, dan layer SnapPoint (10).
///  6. Setup TrophyCamera sebagai child TrophyCabinetSystem (localOffset/rotation, depth=1, inactive default).
///  7. Wire TrophySystemManager: currentCabinetInventory, currentRackInventory, trophySystemRoot, trophyFirstPersonCamera.
///  8. Aturan backend: Trophy TIDAK boleh masuk ke inventori Player (blockTrophyItems=true).
///  9. Tandai scene dirty dan simpan.
/// </summary>
public static class TrophySystemWiring
{
    private const string RootName = "TrophyCabinetSystem";
    private const string RakName = "Rak_Trophy";
    private const string ManagerName = "TrophySystemManager";
    private const string CabinetName = "Kabinet";
    private const string PlayerName = "Player";
    private const string TrophyCameraName = "TrophyCamera";
    private const int SnapCount = 4;

    // Default camera offset/rotation (sama dengan TrophySystemManager.cameraLocalOffset/Rotation)
    private static readonly Vector3 DefaultCameraLocalOffset = new Vector3(-0.15f, 1.5f, 3f);
    private static readonly Vector3 DefaultCameraLocalRotation = new Vector3(3f, 0f, 0f);

    [MenuItem("Farm Beware/Trophy System/Wire Scene (4 SnapPoint)")]
    public static void Wire()
    {
        // --- 0. Buat/validasi TrophyCabinetSystem parent container ---
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create TrophyCabinetSystem");
            Debug.Log($"[TrophySystemWiring] Created parent container '{RootName}'.");
        }

        // --- 0b. Re-parent Kabinet, Rak_Trophy, & TrophyCamera ke TrophyCabinetSystem ---
        GameObject cabinetGO = GameObject.Find(CabinetName);
        GameObject rak = GameObject.Find(RakName);
        GameObject trophyCameraGO = GameObject.Find(TrophyCameraName);

        if (cabinetGO != null && cabinetGO.transform.parent != root.transform)
        {
            Undo.SetTransformParent(cabinetGO.transform, root.transform, "Parent Kabinet to TrophyCabinetSystem");
            Debug.Log($"[TrophySystemWiring] Re-parented '{CabinetName}' to '{RootName}'.");
        }

        if (rak == null)
        {
            Debug.LogError($"[TrophySystemWiring] GameObject '{RakName}' tidak ditemukan di scene.");
            return;
        }

        if (rak.transform.parent != root.transform)
        {
            Undo.SetTransformParent(rak.transform, root.transform, "Parent Rak_Trophy to TrophyCabinetSystem");
            Debug.Log($"[TrophySystemWiring] Re-parented '{RakName}' to '{RootName}'.");
        }

        // --- 0c. Setup TrophyCamera sebagai child TrophyCabinetSystem ---
        if (trophyCameraGO == null)
        {
            trophyCameraGO = new GameObject(TrophyCameraName);
            Undo.RegisterCreatedObjectUndo(trophyCameraGO, "Create TrophyCamera");
            Debug.Log($"[TrophySystemWiring] Created '{TrophyCameraName}'.");
        }

        if (trophyCameraGO.transform.parent != root.transform)
        {
            Undo.SetTransformParent(trophyCameraGO.transform, root.transform, "Parent TrophyCamera to TrophyCabinetSystem");
            Debug.Log($"[TrophySystemWiring] Re-parented '{TrophyCameraName}' to '{RootName}'.");
        }

        // Set local transform to match cameraLocalOffset/rotation
        trophyCameraGO.transform.localPosition = DefaultCameraLocalOffset;
        trophyCameraGO.transform.localRotation = Quaternion.Euler(DefaultCameraLocalRotation);

        // Ensure Camera component exists
        Camera trophyCam = trophyCameraGO.GetComponent<Camera>();
        if (trophyCam == null)
        {
            trophyCam = trophyCameraGO.AddComponent<Camera>();
            Undo.RegisterCreatedObjectUndo(trophyCam, "Add Camera to TrophyCamera");
        }

        // Configure camera: depth=1 (render on top of Main Camera), inactive by default
        trophyCam.depth = 1f;
        trophyCam.clearFlags = CameraClearFlags.Skybox;
        trophyCameraGO.SetActive(false);

        // Ensure UniversalAdditionalCameraData exists (URP)
        var uacd = trophyCameraGO.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (uacd == null)
        {
            uacd = trophyCameraGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            Undo.RegisterCreatedObjectUndo(uacd, "Add UniversalAdditionalCameraData to TrophyCamera");
        }

        // --- 1. InventoryComponent Rack (Inventory 2) ---
        InventoryComponent rackInv = rak.GetComponent<InventoryComponent>();
        if (rackInv == null)
        {
            rackInv = rak.AddComponent<InventoryComponent>();
            Undo.RegisterCreatedObjectUndo(rackInv, "Add Rack InventoryComponent");
        }
        rackInv.maxCapacity = SnapCount;

        // Inisialisasi slot hanya bila belum sesuai (tidak menimpa data yang sudah ada).
        if (rackInv.slots == null || rackInv.slots.Count != SnapCount)
            rackInv.ResetInventory(SnapCount);

        // --- 2. TrophyRackVisuals (Visual Listener) ---
        TrophyRackVisuals visuals = rak.GetComponent<TrophyRackVisuals>();
        if (visuals == null)
        {
            visuals = rak.AddComponent<TrophyRackVisuals>();
            Undo.RegisterCreatedObjectUndo(visuals, "Add TrophyRackVisuals");
        }

        // --- 3. Snap_1..4: TrophySnapPoint + slotIndex + Collider + layer ---
        int snapLayer = LayerMask.NameToLayer("SnapPoint");
        Transform[] snapTransforms = new Transform[SnapCount];
        for (int i = 0; i < SnapCount; i++)
        {
            string name = "Snap_" + (i + 1);
            Transform snapT = rak.transform.Find(name);
            if (snapT == null)
            {
                Debug.LogError($"[TrophySystemWiring] '{name}' tidak ditemukan di bawah '{RakName}'. Buat dahulu (atau hapus baris ini bila memakai 4 titik).");
                return;
            }

            TrophySnapPoint snap = snapT.GetComponent<TrophySnapPoint>();
            if (snap == null)
            {
                snap = snapT.gameObject.AddComponent<TrophySnapPoint>();
                Undo.RegisterCreatedObjectUndo(snap, "Add TrophySnapPoint to " + name);
            }
            snap.slotIndex = i;

            if (snapT.GetComponent<Collider>() == null)
                snapT.gameObject.AddComponent<BoxCollider>();

            if (snapLayer >= 0)
                snapT.gameObject.layer = snapLayer;

            snapTransforms[i] = snapT;
        }

        // --- 4. Wire TrophyRackVisuals (serialized) ---
        SerializedObject visualsSO = new SerializedObject(visuals);
        visualsSO.FindProperty("rackInventory").objectReferenceValue = rackInv;

        SerializedProperty snaps = visualsSO.FindProperty("snapPoints");
        snaps.arraySize = SnapCount;
        for (int i = 0; i < SnapCount; i++)
        {
            if (snapTransforms[i] != null)
                snaps.GetArrayElementAtIndex(i).objectReferenceValue = snapTransforms[i];
        }
        visualsSO.ApplyModifiedProperties();

        // --- 5. Wire TrophySystemManager (serialized) ---
        GameObject managerGO = GameObject.Find(ManagerName);
        if (managerGO == null)
        {
            Debug.LogError($"[TrophySystemWiring] GameObject '{ManagerName}' tidak ditemukan di scene.");
            return;
        }

        TrophySystemManager manager = managerGO.GetComponent<TrophySystemManager>();
        if (manager == null)
        {
            Debug.LogError($"[TrophySystemWiring] Komponen '{nameof(TrophySystemManager)}' tidak ditemukan di '{ManagerName}'.");
            return;
        }

        if (cabinetGO == null)
        {
            Debug.LogError($"[TrophySystemWiring] '{CabinetName}' tidak ditemukan di scene.");
            return;
        }

        InventoryComponent cabinetInv = cabinetGO.GetComponent<InventoryComponent>();
        if (cabinetInv == null)
        {
            Debug.LogError($"[TrophySystemWiring] '{CabinetName}' tanpa InventoryComponent.");
            return;
        }

        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("currentCabinetInventory").objectReferenceValue = cabinetInv;
        managerSO.FindProperty("currentRackInventory").objectReferenceValue = rackInv;
        managerSO.FindProperty("trophySystemRoot").objectReferenceValue = root.transform;
        managerSO.FindProperty("trophyFirstPersonCamera").objectReferenceValue = trophyCam;
        managerSO.ApplyModifiedProperties();

        // --- 5.5 Aturan backend: Trophy TIDAK boleh masuk ke inventori Player ---
        GameObject playerGO = GameObject.Find(PlayerName);
        if (playerGO == null)
        {
            Debug.LogError($"[TrophySystemWiring] GameObject '{PlayerName}' tidak ditemukan di scene.");
            return;
        }

        InventoryComponent playerInv = playerGO.GetComponent<InventoryComponent>();
        if (playerInv == null)
        {
            Debug.LogError($"[TrophySystemWiring] '{PlayerName}' tidak punya InventoryComponent.");
            return;
        }

        SerializedObject playerSO = new SerializedObject(playerInv);
        playerSO.FindProperty("blockTrophyItems").boolValue = true;
        playerSO.ApplyModifiedProperties();

        // --- 6. Simpan scene ---
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Debug.Log($"[TrophySystemWiring] Wiring selesai: {RootName} created + Kabinet/Rak/Camera re-parented + Rack({rackInv.maxCapacity} slot) + TrophyRackVisuals + Snap_1..4 (slotIndex 0..3) + Manager wired (trophySystemRoot, trophyFirstPersonCamera) + Player blockTrophyItems=true + scene disimpan.");
    }
}