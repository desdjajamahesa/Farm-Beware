using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utility: pembuatan aset dummy trophy (Prefab + Material + ItemData),
/// perbaikan kapasitas Kabinet (8 -> 4 slot, data-safe), dan seed 3 trophy ke Kabinet.
/// Semua menu IDEMPOTEN — aman bila dijalankan berkali-kali.
/// </summary>
public static class TrophyAssetFactory
{
    private const string PrefabDir = "Assets/Prefabs/Trophies";
    private const string MaterialDir = "Assets/Prefabs/Trophies/Materials";
    private const string ItemDataDir = "Assets/Scripts/Features/Inventory/Data";
    private const string IconSourcePath = "Assets/Scripts/Features/Inventory/Data/DummySword.asset";
    private const string CabinetName = "Kabinet";

    #region Menu: Create Dummy Trophies

    [MenuItem("Farm Beware/Trophy System/Create Dummy Trophies (Capsule, Cube, Sphere)")]
    public static void CreateDummyTrophies()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Trophies");
        EnsureFolder(PrefabDir, "Materials");

        CreateTrophy(PrimitiveType.Capsule, "TrophyCapsule", "Trophy Capsule", new Color(1f, 0.84f, 0.36f));
        CreateTrophy(PrimitiveType.Cube, "TrophyCube", "Trophy Cube", new Color(0.55f, 0.85f, 1f));
        CreateTrophy(PrimitiveType.Sphere, "TrophySphere", "Trophy Sphere", new Color(0.75f, 0.6f, 1f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TrophyAssetFactory] 3 dummy trophy dibuat: Prefab + Material + ItemData (placeablePrefab terisi).");
    }

    private static void CreateTrophy(PrimitiveType shape, string fileBase, string displayName, Color color)
    {
        string prefabPath = PrefabDir + "/" + fileBase + ".prefab";
        string materialPath = MaterialDir + "/" + fileBase + "_mat.mat";
        string dataPath = ItemDataDir + "/" + fileBase + ".asset";

        // --- 1) Material (buat bila belum ada) ---
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogError("[TrophyAssetFactory] Tidak ada shader URP/Lit maupun Standard. Pembuatan material '" + fileBase + "' dibatalkan.");
                return;
            }

            mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        // --- 2) Prefab piala (shape primitif + TrophyItem + material) ---
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = displayName;

            if (shape == PrimitiveType.Capsule)
                go.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            else
                go.transform.localScale = Vector3.one * 0.35f;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;

            TrophyItem trophyItem = go.AddComponent<TrophyItem>();
            trophyItem.trophyName = displayName;

            prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        // --- 3) ItemData (ScriptableObject) yang menunjuk ke prefab tsb ---
        ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.itemName = displayName;
        data.maxStack = 1;
        data.type = ItemData.ItemType.Trophy;
        data.healAmount = 0;
        data.equipPrefab = null;
        data.placeablePrefab = prefab;

        // Ikon: salin referensi builtin UISprite yang sudah valid dari asset lama
        // (GetBuiltinResource<Sprite> = null di Unity 6, jadi di-salin dari asset yang bekerja).
        if (data.itemIcon == null)
        {
            ItemData iconSource = AssetDatabase.LoadAssetAtPath<ItemData>(IconSourcePath);
            if (iconSource != null && iconSource.itemIcon != null)
                data.itemIcon = iconSource.itemIcon;
        }

        EditorUtility.SetDirty(data);
    }

    #endregion

    #region Menu: Set Cabinet to 4 Slots (Data-Safe)

    [MenuItem("Farm Beware/Trophy System/Set Cabinet to 4 Slots (Data-Safe)")]
    public static void SetCabinetToFourSlots()
    {
        GameObject kabinet = GameObject.Find(CabinetName);
        if (kabinet == null)
        {
            Debug.LogError(" [TrophySystemFactory] GameObject 'Kabinet' tidak ditemukan di scene.");
            return;
        }

        InventoryComponent inv = kabinet.GetComponent<InventoryComponent>();
        if (inv == null)
        {
            Debug.LogError(" [TrophySystemFactory] 'Kabinet' tidak punya InventoryComponent.");
            return;
        }

        // Sukses cepat bila sudah 4 slot.
        if (inv.slots != null && inv.slots.Count == 4 && inv.maxCapacity == 4)
        {
            Debug.Log(" [TrophySystemFactory] Kabinet sudah 4 slot. Tidak ada perubahan.");
            return;
        }

        // Keamanan: hanya menyusut bila slot ekstra (index >= 4) semuanya kosong.
        if (inv.slots != null)
        {
            for (int i = 4; i < inv.slots.Count; i++)
            {
                if (inv.slots[i] != null && !inv.slots[i].IsEmpty)
                {
                    Debug.LogError(" [TrophySystemFactory] Tidak bisa mengecilkan Kabinet: slot " + i + " masih terisi item. Kosongkan dulu slot itu.");
                    return;
                }
            }

            // Pertahankan isi slot 0..3; hanya buang slot ekstra yang kosong.
            if (inv.slots.Count > 4)
                inv.slots.RemoveRange(4, inv.slots.Count - 4);
        }

        inv.maxCapacity = 4;

        EditorSceneManager.MarkSceneDirty(kabinet.scene);
        EditorSceneManager.SaveScene(kabinet.scene);
        Debug.Log(" [TrophySystemFactory] Kabinet kini 4 slot (isi slot 0..3 tetap dipertahankan). Scene tersimpan.");
    }

    #endregion

    #region Menu: Seed Two Dummy Trophies into Cabinet

    [MenuItem("Farm Beware/Trophy System/Seed 3 Dummy Trophies into Cabinet (slot 0-2)")]
    public static void SeedDummyTrophies()
    {
        GameObject kabinet = GameObject.Find(CabinetName);
        if (kabinet == null)
        {
            Debug.LogError(" [TrophySystemFactory] GameObject 'Kabinet' tidak ditemukan di scene active.");
            return;
        }

        InventoryComponent inv = kabinet.GetComponent<InventoryComponent>();
        if (inv == null)
        {
            Debug.LogError(" [TrophySystemFactory] 'Kabinet' tidak punya InventoryComponent.");
            return;
        }

        if (inv.slots == null || inv.slots.Count < 4)
            inv.ResetInventory(4);

        string[] dataPaths =
        {
            ItemDataDir + "/TrophyCapsule.asset",   // slot 0
            ItemDataDir + "/TrophyCube.asset",      // slot 1
            ItemDataDir + "/TrophySphere.asset",    // slot 2
        };

        bool changed = false;
        for (int i = 0; i < dataPaths.Length && i < inv.slots.Count; i++)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(dataPaths[i]);
            if (item == null)
            {
                Debug.LogWarning(" [TrophySystemFactory] ItemData belum dibuat (" + dataPaths[i] + "). Jalankan 'Create Dummy Trophies' dulu.");
                continue;
            }

            if (inv.slots[i] != null && inv.slots[i].IsEmpty)
            {
                inv.slots[i].item = item;
                inv.slots[i].quantity = 1;
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(kabinet.scene);
            EditorSceneManager.SaveScene(kabinet.scene);
            Debug.Log(" [TrophySystemFactory] Slot 0-2 Kabinet diisi Trophy Capsule, Trophy Cube, dan Trophy Sphere. Slot 3 kosong.");
        }
        else
        {
            Debug.Log(" [TrophySystemFactory] Tidak ada penambahan (slot tujuan mungkin sudah terisi).");
        }
    }

    #endregion

    #region Helpers

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(full))
            return;
        AssetDatabase.CreateFolder(parent, child);
    }

    #endregion
}