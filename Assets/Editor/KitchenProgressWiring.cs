using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool idempoten — Wiring progress untuk stasiun dapur:
/// 1) World overlay (KitchenStationProgressOverlay) di Stove & Sink:
///    - station wired, slotAnchors (Burner_1/Burner_2 / ProgressAnchor), renderQueue=4000
/// 2) In-UI strip (InventorySlotUI) dibuat otomatis saat panel dibuka — tidak perlu wiring scene.
/// 3) KitchenStationSoundFx (bunyi "Selesai!").
/// 4) Hapus panel lama KitchenStationUI_Panel.
/// Menu: Farm Beware > Kitchen > Wire World + UI Progress (Stove & Sink)
/// Menu debug: Farm Beware > Kitchen > Debug Progress (Seed Recipe Inputs)
/// Menu debug log: Farm Beware > Kitchen > Toggle Progress Debug Log
/// </summary>
public static class KitchenProgressWiring
{
    private const string MaterialPath = "Assets/Materials/Kitchen/Mat_Progress_Overlay.mat";
    private const string AnchorName = "ProgressAnchor";

    [MenuItem("Farm Beware/Kitchen/Wire World + UI Progress (Stove & Sink)")]
    public static void WireWorldAndUiProgress()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[KitchenProgressWiring] Wiring tidak dijalankan saat Play Mode. Keluar Play dulu.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;

        GameObject stove = GameObject.Find("Kitchen_Stove");
        GameObject sink = GameObject.Find("Kitchen_Sink");

        if (stove != null)
        {
            Transform[] stoveAnchors = new Transform[] {
                stove.transform.Find("Burner_1"),
                stove.transform.Find("Burner_2")
            };
            changed |= WireWorldOverlay(stove, stove.GetComponent<StoveInteractable>(), stoveAnchors);
            changed |= WireSoundFx(stove, stove.GetComponent<StoveInteractable>());
        }
        else
        {
            Debug.LogWarning("[KitchenProgressWiring] Kitchen_Stove tidak ditemukan di scene.");
        }

        if (sink != null)
        {
            Transform anchor = EnsureProgressAnchor(sink, true);
            changed |= WireWorldOverlay(sink, sink.GetComponent<KitchenSinkInteractable>(), anchor != null ? new[] { anchor } : null);
            changed |= WireSoundFx(sink, sink.GetComponent<KitchenSinkInteractable>());
        }
        else
        {
            Debug.LogWarning("[KitchenProgressWiring] Kitchen_Sink tidak ditemukan di scene.");
        }

        changed |= DeleteLegacyPanel();

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[KitchenProgressWiring] Selesai. Scene disimpan.");
        }
        else
        {
            Debug.Log("[KitchenProgressWiring] Tidak ada perubahan (sudah ter-wire / idempoten).");
        }
    }

    [MenuItem("Farm Beware/Kitchen/Debug Progress (Seed Recipe Inputs)")]
    public static void DebugSeedProgress()
    {
        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;

        GameObject stove = GameObject.Find("Kitchen_Stove");
        GameObject sink = GameObject.Find("Kitchen_Sink");

        if (stove != null)
            changed |= SeedKitchenStation(stove, "Stove");
        else
            Debug.LogWarning("[KitchenProgressWiring] Kitchen_Stove tidak ditemukan di scene.");

        if (sink != null)
            changed |= SeedKitchenStation(sink, "Sink");
        else
            Debug.LogWarning("[KitchenProgressWiring] Kitchen_Sink tidak ditemukan di scene.");

        if (changed)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[KitchenProgressWiring] Debug seed: item masuk slot; proses langsung berjalan (Play mode, tanpa simpan scene).");
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[KitchenProgressWiring] Debug seed selesai. item sudah masuk slot stasiun; saat Play proses akan auto-mulai.");
            }
        }
        else
        {
            Debug.Log("[KitchenProgressWiring] Debug seed: tidak ada slot kosong / tanpa recipe / sudah ada item.");
        }
    }

    [MenuItem("Farm Beware/Kitchen/Toggle Progress Debug Log")]
    public static void ToggleProgressDebugLog()
    {
        InventorySlotUI.debugLogProgress = !InventorySlotUI.debugLogProgress;
        Debug.Log("[KitchenProgressWiring] debugLogProgress = " + InventorySlotUI.debugLogProgress
            + " (bila ON, log fill per-slot saat berubah >=1% untuk membuktikan proses bertahap).");
    }

    private static bool WireWorldOverlay(GameObject target, KitchenStation stationScript, Transform[] anchors)
    {
        if (stationScript == null)
        {
            Debug.LogWarning("[KitchenProgressWiring] " + target.name + " tidak memiliki KitchenStation.");
            return false;
        }

        KitchenStationProgressOverlay overlay = target.GetComponent<KitchenStationProgressOverlay>();
        if (overlay == null)
            overlay = target.AddComponent<KitchenStationProgressOverlay>();

        SerializedObject so = new SerializedObject(overlay);

        so.FindProperty("station").objectReferenceValue = stationScript;

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:Material Mat_Progress_Overlay");
            if (guids.Length > 0)
                mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        so.FindProperty("overlayMaterial").objectReferenceValue = mat;

        SerializedProperty anchorsProp = so.FindProperty("slotAnchors");
        if (anchors == null || anchors.Length == 0)
        {
            anchorsProp.arraySize = 0;
        }
        else
        {
            anchorsProp.arraySize = anchors.Length;
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i] != null)
                    anchorsProp.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
                else
                    Debug.LogWarning("[KitchenProgressWiring] Anchor index " + i + " (null) di " + target.name);
            }
        }

        SerializedProperty colorProp = so.FindProperty("overlayColor");
        colorProp.colorValue = new Color(1f, 1f, 1f, 0.5f);
        so.FindProperty("maxHeight").floatValue = 0.7f;

        so.ApplyModifiedProperties();

        if (overlay != null)
        {
            var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name == "Mat_Progress_Overlay")
                {
                    r.sharedMaterial.renderQueue = 4000;
                }
            }
        }

        Debug.Log("[KitchenProgressWiring] " + target.name + ": World Overlay ter-wire (anchors=" + (anchors == null ? 0 : anchors.Length) + ").");
        return true;
    }

    private static bool WireSoundFx(GameObject target, KitchenStation stationScript)
    {
        bool created = false;

        if (stationScript == null)
        {
            Debug.LogWarning("[KitchenProgressWiring] " + target.name + " tidak memiliki KitchenStation.");
            return false;
        }

        KitchenStationSoundFx fx = target.GetComponent<KitchenStationSoundFx>();
        if (fx == null)
        {
            fx = target.AddComponent<KitchenStationSoundFx>();
            created = true;
        }

        SerializedObject so = new SerializedObject(fx);
        so.FindProperty("station").objectReferenceValue = stationScript;
        so.ApplyModifiedProperties();

        if (created)
            Debug.Log("[KitchenProgressWiring] " + target.name + ": KitchenStationSoundFx ditambahkan.");
        return created;
    }

    private static Transform EnsureProgressAnchor(GameObject go, bool placeAtTopFront)
    {
        Transform anchor = go.transform.Find("ProgressAnchor");
        if (anchor == null)
        {
            GameObject a = new GameObject("ProgressAnchor");
            anchor = a.transform;
            anchor.SetParent(go.transform, false);
        }

        if (placeAtTopFront)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                Bounds b = r.bounds;
                anchor.position = new Vector3(b.center.x, b.max.y, b.max.z);
            }
        }

        return anchor;
    }

    private static bool DeleteLegacyPanel()
    {
        GameObject panel = GameObject.Find("KitchenStationUI_Panel");
        if (panel == null)
            return false;

        Object.DestroyImmediate(panel);
        Debug.Log("[KitchenProgressWiring] KitchenStationUI_Panel dihapus dari UI_Canvas (sudah bukan dipakai).");
        return true;
    }

    private static bool SeedKitchenStation(GameObject go, string label)
    {
        KitchenStation station = go.GetComponent<KitchenStation>();
        InventoryComponent inv = go.GetComponent<InventoryComponent>();
        if (station == null || inv == null)
        {
            Debug.LogWarning("[KitchenProgressWiring] " + label + ": komponen KitchenStation/Inventory tidak lengkap.");
            return false;
        }

        KitchenRecipe recipe = GetFirstRecipe(go);
        if (recipe == null || recipe.input == null)
        {
            Debug.LogWarning("[KitchenProgressWiring] " + label + ": tidak ada recipe yang valid.");
            return false;
        }

        for (int i = 0; i < inv.slots.Count; i++)
        {
            if (inv.slots[i] == null || inv.slots[i].IsEmpty)
            {
                if (inv.AddItem(recipe.input, 1))
                {
                    Debug.Log("[KitchenProgressWiring] " + label + ": slot " + i + " di-seed input recipe '"
                        + recipe.input.itemName + "' (proses auto-mulai saat Play).");
                    return true;
                }
            }
        }

        Debug.LogWarning("[KitchenProgressWiring] " + label + ": tidak ada slot kosong untuk seed.");
        return false;
    }

    private static KitchenRecipe GetFirstRecipe(GameObject go)
    {
        string[] fieldNames = { "recipes", "washRecipes" };

        KitchenStation station = go.GetComponent<KitchenStation>();
        if (station == null)
            return null;

        SerializedObject so = new SerializedObject(station);
        for (int f = 0; f < fieldNames.Length; f++)
        {
            SerializedProperty p = so.FindProperty(fieldNames[f]);
            if (p == null || !p.isArray || p.arraySize == 0)
                continue;
            KitchenRecipe r = p.GetArrayElementAtIndex(0).objectReferenceValue as KitchenRecipe;
            if (r != null)
                return r;
        }
        return null;
    }
}