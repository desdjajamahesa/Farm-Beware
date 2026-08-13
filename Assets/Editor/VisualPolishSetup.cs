using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Fase 1 polish "Cozy Farm": layout kompak dapur, material URP hangat, dinding ruangan,
/// burner kompor, serta lighting (skybox/ambient/fog) + tuning post-processing.
/// Menu: Farm Beware &gt; Polish &gt; Apply Cozy Farm &amp; Room (Phase 1).
/// Idempoten (aman dijalankan ulang).
/// </summary>
public static class VisualPolishSetup
{
    private const string MatDir = "Assets/Materials/Kitchen";
    private const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";

    private static readonly Vector3 RoomCenter = new Vector3(14f, 0f, 5f);
    private const float RoomW = 12f;   // x
    private const float RoomD = 8f;    // z
    private const float WallH = 3f;
    private const float WallT = 0.3f;

    [MenuItem("Farm Beware/Polish/Apply Cozy Farm & Room (Phase 1)")]
    public static void Apply()
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Kitchen");

        // 1) Material library
        Material wood = CreateMat("Mat_Wood", new Color(0.58f, 0.43f, 0.30f), 0.25f, 0f);
        Material wallCream = CreateMat("Mat_Wall_Cream", new Color(0.93f, 0.89f, 0.79f), 0.05f, 0f);
        Material tile = CreateMat("Mat_Floor_Tile", new Color(0.85f, 0.78f, 0.62f), 0.3f, 0f);
        Material metal = CreateMat("Mat_Metal", new Color(0.72f, 0.75f, 0.78f), 0.6f, 1f);
        Material metalDark = CreateMat("Mat_Metal_Dark", new Color(0.20f, 0.21f, 0.23f), 0.55f, 1f);
        Material glass = CreateGlassMat("Mat_Glass", new Color(0.55f, 0.75f, 0.85f, 0.5f));
        Material grass = CreateMat("Mat_Grass", new Color(0.45f, 0.62f, 0.35f), 0.15f, 0f);

        // 2) Re-layout kompak
        SetPosition("Fridge", new Vector3(10f, 1f, 8.2f));
        SetPosition("Kitchen_Sink", new Vector3(14f, 0.43f, 8.2f));
        SetPosition("Kitchen_Stove", new Vector3(18f, 0.25f, 8.2f));
        SetPosition("FoodPrepArea", new Vector3(10.2f, 0.06f, 4.8f));
        SetPosition("Kitchen_Table", new Vector3(12.8f, 0.05f, 4.8f));
        SetPosition("Kitchen_Chair", new Vector3(15.2f, 0.3f, 4.8f));
        SetPosition("Kitchen_Window", new Vector3(19.95f, 1.2f, 5f));
        SetPosition("Back_Door", new Vector3(14f, 1f, 1.1f));
        SetPosition("Spawn_Backyard", new Vector3(14f, 0.4f, -5f));

        SetFloor("Kitchen_Floor", new Vector3(14f, 0.05f, 5f), new Vector3(RoomW / 10f, 1f, RoomD / 10f), tile);
        SetFloor("Backyard_Floor", new Vector3(14f, 0.05f, -5f), new Vector3(1.6f, 1f, 1.4f), grass);

        // 3) Material aplikasi ke objek
        ApplyMat("Fridge", metal);
        ApplyMat("Kitchen_Sink", metal);
        ApplyMat("Kitchen_Stove", metalDark);
        ApplyMat("FoodPrepArea", wood);
        ApplyMat("Kitchen_Table", wood);
        ApplyMat("Kitchen_Chair", wood);
        ApplyMat("Back_Door", wood);
        ApplyMat("Kabinet", wood);
        ApplyMat("Rak_Trophy", wood);
        ApplyMat("Lemari", wood);
        ApplyMat("Objek_Kasur", wood);
        ApplyMat("TestChest", wood);

        // 4) Burner kompor (2 disc)
        GameObject stove = GameObject.Find("Kitchen_Stove");
        if (stove != null)
        {
            AddBurner(stove.transform, "Burner_1", new Vector3(0.38f, 0.27f, 0.12f), metalDark);
            AddBurner(stove.transform, "Burner_2", new Vector3(-0.38f, 0.27f, 0.12f), metalDark);
        }

        // 5) Jendela: frame + glass child
        GameObject window = GameObject.Find("Kitchen_Window");
        if (window != null)
        {
            ApplyMat(window, wood);
            AddGlass(window.transform, "Glass", glass);
        }

        // 6) Dinding ruangan
        BuildWalls(wallCream);

        // 7) Lighting & post-processing
        SetupLighting();
        SetupPostProcessing();

        // 8) Refresh prefab furnitur agar style konsisten
        RefreshPrefabMaterials("Fridge", metal);
        RefreshPrefabMaterials("Sink", metal);
        RefreshPrefabMaterials("Stove", metalDark);
        RefreshPrefabMaterials("Table", wood);
        RefreshPrefabMaterials("Chair", wood);
        RefreshPrefabMaterials("FoodPrepArea", wood);
        RefreshPrefabMaterials("Window", wood);
        RefreshPrefabMaterials("Door", wood);

        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[VisualPolishSetup] Fase 1 Cozy Farm selesai: layout, material, dinding, burner, lighting, PP. Scene tersimpan.");
    }

    #region Helpers

    private static Material CreateMat(string name, Color color, float smooth, float metallic)
    {
        string path = MatDir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateGlassMat(string name, Color color)
    {
        string path = MatDir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = 3000;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SetPosition(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) go.transform.position = pos;
    }

    private static void SetFloor(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) return;
        go.transform.position = pos;
        go.transform.localScale = scale;
        ApplyMat(go, mat);
    }

    private static void ApplyMat(string name, Material mat)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) ApplyMat(go, mat);
    }

    private static void ApplyMat(GameObject go, Material mat)
    {
        if (go == null || mat == null) return;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = mat;
    }

    private static void AddBurner(Transform parent, string name, Vector3 localPos, Material mat)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return;

        GameObject burner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        burner.name = name;
        burner.transform.SetParent(parent, true);
        burner.transform.localPosition = localPos;
        burner.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);
        Collider col = burner.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        ApplyMat(burner, mat);
    }

    private static void AddGlass(Transform parent, string name, Material mat)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return;

        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glass.name = name;
        glass.transform.SetParent(parent, true);
        glass.transform.localPosition = Vector3.zero;
        glass.transform.localScale = new Vector3(1.05f, 0.85f, 0.5f);
        Collider col = glass.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        ApplyMat(glass, mat);
    }

    private static void BuildWalls(Material wallMat)
    {
        // Back wall (z = RoomCenter.z + RoomD/2 + WallT/2)
        CreateWall("Wall_Back", new Vector3(RoomCenter.x, WallH / 2f, RoomCenter.z + RoomD / 2f + WallT / 2f),
            new Vector3(RoomW + WallT * 2f, WallH, WallT), wallMat);
        // Left wall
        CreateWall("Wall_Left", new Vector3(RoomCenter.x - RoomW / 2f - WallT / 2f, WallH / 2f, RoomCenter.z),
            new Vector3(WallT, WallH, RoomD), wallMat);
        // Right wall
        CreateWall("Wall_Right", new Vector3(RoomCenter.x + RoomW / 2f + WallT / 2f, WallH / 2f, RoomCenter.z),
            new Vector3(WallT, WallH, RoomD), wallMat);
        // Front wall - two segments dengan gap pintu (x 12.2..15.8)
        float frontZ = RoomCenter.z - RoomD / 2f - WallT / 2f;
        CreateWall("Wall_Front_L", new Vector3(11f, WallH / 2f, frontZ), new Vector3(3.6f, WallH, WallT), wallMat);
        CreateWall("Wall_Front_R", new Vector3(17f, WallH / 2f, frontZ), new Vector3(3.6f, WallH, WallT), wallMat);
    }

    private static void CreateWall(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.Find(name);
        if (wall != null)
        {
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            ApplyMat(wall, mat);
            return;
        }

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        ApplyMat(go, mat);
    }

    private static void RefreshPrefabMaterials(string prefabName, Material mat)
    {
        string path = "Assets/Prefabs/Furniture/Kitchen/" + prefabName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;
        MeshRenderer mr = prefab.GetComponent<MeshRenderer>();
        if (mr == null) return;
        mr.sharedMaterial = mat;
        EditorUtility.SetDirty(prefab);
    }

    private static void SetupLighting()
    {
        // Skybox procedural hangat
        string skyPath = "Assets/Materials/Kitchen/Skybox_Cozy.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                sky = new Material(skyShader);
                AssetDatabase.CreateAsset(sky, skyPath);
            }
        }
        if (sky != null)
        {
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.75f, 0.85f, 1f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.55f, 0.5f, 0.4f));
            if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.08f);
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.1f);
            RenderSettings.skybox = sky;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.82f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.55f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.4f, 0.35f, 0.28f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.88f, 0.84f, 0.76f);
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 220f;
    }

    private static void SetupPostProcessing()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null) return;

        VolumeComponent tonemap = profile.components.Find(c => c is UnityEngine.Rendering.Universal.Tonemapping);
        if (tonemap != null)
        {
            var t = tonemap as UnityEngine.Rendering.Universal.Tonemapping;
            t.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
        }

        VolumeComponent bloom = profile.components.Find(c => c is UnityEngine.Rendering.Universal.Bloom);
        if (bloom != null)
        {
            var b = bloom as UnityEngine.Rendering.Universal.Bloom;
            b.threshold.Override(1.1f);
            b.intensity.Override(0.4f);
        }

        VolumeComponent vignette = profile.components.Find(c => c is UnityEngine.Rendering.Universal.Vignette);
        if (vignette != null)
        {
            var v = vignette as UnityEngine.Rendering.Universal.Vignette;
            v.intensity.Override(0.25f);
        }

        VolumeComponent colorAdj = profile.components.Find(c => c is UnityEngine.Rendering.Universal.ColorAdjustments);
        if (colorAdj == null)
        {
            colorAdj = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        }
        var ca = colorAdj as UnityEngine.Rendering.Universal.ColorAdjustments;
        ca.contrast.Override(5f);
        ca.saturation.Override(5f);
        ca.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(full)) return;
        AssetDatabase.CreateFolder(parent, child);
    }

    #endregion
}