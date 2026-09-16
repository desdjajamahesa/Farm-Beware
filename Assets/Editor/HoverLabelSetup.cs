using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FeaturesInteraction;

/// <summary>
/// Editor utility: wire hover label untuk objek interaktif di scene.
/// Menu: Farm Beware &gt; Tools &gt; Wire Hover Labels.
/// Idempoten (aman dijalankan berulang).
/// </summary>
public static class HoverLabelSetup
{
    private static readonly (string goName, string label)[] NameMap =
    {
        ("Fridge", "Kulkas"),
        ("Kitchen_Sink", "Kitchen Sink"),
        ("Kitchen_Stove", "Kompor"),
        ("Back_Door", "Pintu Belakang"),
        ("Kabinet", "Kabinet Piala"),
        ("TestChest", "Peti"),
        ("Lemari", "Lemari"),
        ("Objek_Kasur", "Kasur"),
        ("Rak_Trophy", "Rak Piala"),
    };

    [MenuItem("Farm Beware/Tools/Wire Hover Labels")]
    public static void Wire()
    {
        int wired = 0;
        foreach (var entry in NameMap)
        {
            GameObject go = GameObject.Find(entry.goName);
            if (go == null)
                continue;

            WorldLabel label = go.GetComponent<WorldLabel>();
            if (label == null)
                label = go.AddComponent<WorldLabel>();

            Undo.RecordObject(label, "Wire Hover Label " + entry.goName);
            label.displayName = entry.label;
            EditorUtility.SetDirty(label);
            wired++;
        }

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            HoverLabelController controller = player.GetComponent<HoverLabelController>();
            if (controller == null)
                controller = player.AddComponent<HoverLabelController>();

            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            if (interactor != null)
            {
                SerializedObject so = new SerializedObject(controller);
                so.FindProperty("interactor").objectReferenceValue = interactor;
                so.ApplyModifiedProperties();
            }
        }

        WireWorldHoverLabel();
        WireInteractPrompt();
        WireHighlights();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[HoverLabelSetup] Wiring selesai: {wired} objek berlabel + HoverLabelController(Player) + label dunia + prompt + highlight. Scene tersimpan.");
    }

    /// <summary>
    /// Buat prompt aksi "E — Nama" (UI_InteractPrompt) di atas hotbar & wire ke ItemDisplayUI.
    /// </summary>
    private static void WireInteractPrompt()
    {
        GameObject canvasGO = GameObject.Find("UI_Canvas");
        if (canvasGO == null)
            return;

        Transform existing = canvasGO.transform.Find("UI_InteractPrompt");
        Text promptText = existing != null ? existing.GetComponent<Text>() : null;

        if (existing == null)
        {
            GameObject prompt = new GameObject("UI_InteractPrompt", typeof(RectTransform));
            RectTransform rt = prompt.GetComponent<RectTransform>();
            rt.SetParent(canvasGO.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 150f); // di atas label nama dunia (y 110)
            rt.sizeDelta = new Vector2(600f, 40f);

            promptText = prompt.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 20;
            promptText.fontStyle = FontStyle.Bold;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.raycastTarget = false;
            promptText.text = "";

            Outline outline = prompt.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        promptText.gameObject.SetActive(false);

        GameObject displayGO = GameObject.Find("INV_ItemDisplayManager");
        if (displayGO == null)
            return;

        ItemDisplayUI display = displayGO.GetComponent<ItemDisplayUI>();
        if (display == null)
            return;

        Undo.RecordObject(display, "Wire InteractPromptText");
        display.interactPromptText = promptText;
        EditorUtility.SetDirty(display);
    }

    /// <summary>
    /// Tambahkan Highlightable (material emissive) ke semua objek interaktif.
    /// </summary>
    private static void WireHighlights()
    {
        Material highlight = CreateHighlightMaterial();

        foreach (var entry in NameMap)
        {
            GameObject go = GameObject.Find(entry.goName);
            if (go == null)
                continue;

            Highlightable hl = go.GetComponent<Highlightable>();
            if (hl == null)
                hl = go.AddComponent<Highlightable>();

            SerializedObject so = new SerializedObject(hl);
            so.FindProperty("highlightMaterial").objectReferenceValue = highlight;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hl);
        }
    }

    private static Material CreateHighlightMaterial()
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Kitchen");

        string path = "Assets/Materials/Kitchen/Mat_Highlight.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.5f));
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.3f) * 1.5f);
            mat.EnableKeyword("_EMISSION");
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(full)) return;
        AssetDatabase.CreateFolder(parent, child);
    }

    /// <summary>
    /// Buat label nama objek dunia (UI_WorldHoverLabel) yang tampil TETAP di atas hotbar
    /// dan wire ke ItemDisplayUI.worldHoverText. Idempoten.
    /// </summary>
    private static void WireWorldHoverLabel()
    {
        GameObject canvasGO = GameObject.Find("UI_Canvas");
        if (canvasGO == null)
        {
            Debug.LogWarning("[HoverLabelSetup] 'UI_Canvas' tidak ditemukan -> label dunia dilewatkan.");
            return;
        }

        Transform existing = canvasGO.transform.Find("UI_WorldHoverLabel");
        Text labelText = existing != null ? existing.GetComponent<Text>() : null;

        if (existing == null)
        {
            GameObject label = new GameObject("UI_WorldHoverLabel", typeof(RectTransform));
            RectTransform lrt = label.GetComponent<RectTransform>();
            lrt.SetParent(canvasGO.transform, false);
            lrt.anchorMin = new Vector2(0.5f, 0f);
            lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 110f); // di atas hotbar (top hotbar ~ y 94)
            lrt.sizeDelta = new Vector2(600f, 40f);

            labelText = label.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 20;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;
            labelText.text = "";

            Outline outline = label.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        labelText.gameObject.SetActive(false);

        GameObject displayGO = GameObject.Find("INV_ItemDisplayManager");
        if (displayGO == null)
            return;

        ItemDisplayUI display = displayGO.GetComponent<ItemDisplayUI>();
        if (display == null)
            return;

        Undo.RecordObject(display, "Wire WorldHoverText");
        display.worldHoverText = labelText;
        EditorUtility.SetDirty(display);
    }
}