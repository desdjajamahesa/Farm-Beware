using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FeaturesWardrobe;
using FeaturesInteraction;

/// <summary>
/// Editor utility idempoten — wiring scene Wardrobe + Mirror:
/// 1) WardrobeRoot (parent container).
/// 2) Re-parent Lemari (existing) ke WardrobeRoot.
/// 3) Buat/validasi 'Cermin' (MirrorCamera + RenderTexture + material).
/// 4) Buat 'WardrobeCamera' (dedicated, disabled default, local offset/rot relatif root).
/// 5) WardrobeManager (singleton) + wire semua ref (kamera, player, outfit, UI).
/// 6) PlayerOutfit di Player + outfitRoot child.
/// 7) WardrobeUI_Panel di UI_Canvas (RawImage mirror + grid + tombol + CanvasGroup fade).
///    Field WardrobeUI.mirrorCamera & mirrorRenderTexture di-wire ke 'Cermin'/RT (fix layar putih).
/// 8) WardrobeInteractable sudah terpasang di Lemari.
/// 9) Sample OutfitData assets + dummy full-body prefabs.
/// Menu: Farm Beware > Wardrobe > Wire Scene
/// </summary>
public static class WardrobeSetup
{
    private const string RootName = "WardrobeRoot";
    private const string MirrorName = "Cermin";
    private const string WardrobeCameraName = "WardrobeCamera";
    private const string ManagerName = "WardrobeManager";
    private const string LemariName = "Lemari";
    private const string PlayerName = "Player";
    private const string CanvasName = "UI_Canvas";
    private const string OutlineName = "OutfitRoot";
    private const string OutfitDataDir = "Assets/Scripts/Features/Wardrobe/Data";
    private const string OutfitPrefabDir = "Assets/Prefabs/Wardrobe";
    private const int MirrorTextureSize = 1024;

    private static readonly Vector3 DefaultCameraLocalOffset = new Vector3(-4.4f, 1.9f, 7.8f);
    private static readonly Vector3 DefaultCameraLocalRotation = new Vector3(-5f, 180f, 0f);
    private static readonly Vector3 DefaultMirrorLocalPosition = new Vector3(-4.4f, 0.9f, 3f);
    private static readonly Vector3 DefaultMirrorLocalScale = new Vector3(0.7f, 1.8f, 1f);

    [MenuItem("Farm Beware/Wardrobe/Wire Scene")]
    public static void WireWardrobeScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[WardrobeSetup] Wiring tidak dijalankan saat Play Mode.");
            return;
        }

        bool changed = false;

        // --- 0. WardrobeRoot ---
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create WardrobeRoot");
            changed = true;
        }

        // --- 0b. Mirror GameObject ---
        GameObject mirrorGO = GameObject.Find(MirrorName);
        if (mirrorGO == null)
        {
            mirrorGO = new GameObject(MirrorName);
            Undo.RegisterCreatedObjectUndo(mirrorGO, "Create Mirror");
            changed = true;
        }
        if (mirrorGO.transform.parent != root.transform)
        {
            Undo.SetTransformParent(mirrorGO.transform, root.transform, "Parent Mirror to WardrobeRoot");
            changed = true;
        }
        mirrorGO.transform.localPosition = DefaultMirrorLocalPosition;
        mirrorGO.transform.localScale = DefaultMirrorLocalScale;

        // Mirror mesh: quad CENTER-PIVOT (kamera pemain berdiri di sisi +forward cermin,
        // jadi normal quad mengarah +z lokal / +z dunia mengikuti rotasi identitas).
        MeshFilter mf = mirrorGO.GetComponent<MeshFilter>();
        if (mf == null) mf = mirrorGO.AddComponent<MeshFilter>();
        mf.sharedMesh = MirrorQuadMesh();
        mirrorGO.transform.localRotation = Quaternion.identity;
        MeshRenderer mr = mirrorGO.GetComponent<MeshRenderer>();
        if (mr == null) mr = mirrorGO.AddComponent<MeshRenderer>();
        MR(mr);

        // Mirror material.
        Material mirrorMat = LoadOrCreateMaterial("Assets/Materials/Wardrobe/Mat_Cermin.mat",
            new Color(0.85f, 0.9f, 1f, 1f));
        mr.sharedMaterial = mirrorMat;

        // Mirror child camera for RenderTexture.
        Transform mirrorCamT = mirrorGO.transform.Find("MirrorInnerCam");
        Camera mirrorInner = mirrorCamT != null ? mirrorCamT.GetComponent<Camera>() : null;
        if (mirrorCamT == null)
        {
            GameObject inner = new GameObject("MirrorInnerCam");
            inner.transform.SetParent(mirrorGO.transform, false);
            mirrorCamT = inner.transform;
        }
        if (mirrorInner == null)
            mirrorInner = mirrorCamT.gameObject.AddComponent<Camera>();
        mirrorInner.enabled = true;
        mirrorInner.gameObject.tag = "Untagged";
        mirrorInner.depth = -1;
        mirrorInner.clearFlags = CameraClearFlags.SolidColor;
        mirrorInner.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 1f);
        mirrorInner.nearClipPlane = 0.1f;
        mirrorInner.farClipPlane = 50f;
        mirrorInner.fieldOfView = 65f;
        var uacdI = mirrorCamT.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (uacdI == null)
            mirrorCamT.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        mirrorCamT.localPosition = new Vector3(0f, 0f, -0.15f);

        // RenderTexture asset.
        RenderTexture rt = LoadOrCreateRenderTexture("Assets/Materials/Wardrobe/MirrorTexture.renderTexture");

        // MirrorCamera component.
        MirrorCamera mirrorCam = mirrorGO.GetComponent<MirrorCamera>();
        if (mirrorCam == null)
        {
            mirrorCam = mirrorGO.AddComponent<MirrorCamera>();
            changed = true;
        }
        SerializedObject mirrorSO = new SerializedObject(mirrorCam);
        mirrorSO.FindProperty("mirrorCamera").objectReferenceValue = mirrorInner;
        mirrorSO.FindProperty("mirrorTexture").objectReferenceValue = rt;
        mirrorSO.FindProperty("surfaceRenderer").objectReferenceValue = mr;
        mirrorSO.FindProperty("textureSize").intValue = MirrorTextureSize;
        mirrorSO.FindProperty("distanceFromMirror").floatValue = 1.8f;
        mirrorSO.FindProperty("aimHeightOffset").floatValue = 1.25f;
        mirrorSO.ApplyModifiedProperties();

        // --- 0c. WardrobeCamera (dedicated) ---
        GameObject wcGO = GameObject.Find(WardrobeCameraName);
        if (wcGO == null)
        {
            wcGO = new GameObject(WardrobeCameraName);
            Undo.RegisterCreatedObjectUndo(wcGO, "Create WardrobeCamera");
            changed = true;
        }
        if (wcGO.transform.parent != root.transform)
        {
            Undo.SetTransformParent(wcGO.transform, root.transform, "Parent WardrobeCamera");
            changed = true;
        }
        wcGO.transform.localPosition = DefaultCameraLocalOffset;
        wcGO.transform.localRotation = Quaternion.Euler(DefaultCameraLocalRotation);
        Camera wcCam = wcGO.GetComponent<Camera>();
        if (wcCam == null) wcCam = wcGO.AddComponent<Camera>();
        wcCam.depth = 1f;
        wcCam.clearFlags = CameraClearFlags.Skybox;
        wcCam.nearClipPlane = 0.1f;
        wcCam.farClipPlane = 100f;
        wcCam.fieldOfView = 60f;
        wcCam.enabled = false;
        var uacdW = wcGO.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (uacdW == null)
            wcGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        // --- 1. Re-parent Lemari ---
        GameObject lemari = GameObject.Find(LemariName);
        if (lemari != null && lemari.transform.parent != root.transform)
        {
            Undo.SetTransformParent(lemari.transform, root.transform, "Parent Lemari to WardrobeRoot");
            changed = true;
        }
        // WardrobeInteractable harus sudah ada (HoverLabelSetup menambahkannya).
        EnsureComponent<WardrobeInteractable>(lemari);

        // --- 2. WardrobeManager ---
        GameObject mgrGO = GameObject.Find(ManagerName);
        if (mgrGO == null)
        {
            mgrGO = new GameObject(ManagerName);
            Undo.RegisterCreatedObjectUndo(mgrGO, "Create WardrobeManager");
            changed = true;
        }
        if (mgrGO.transform.parent != root.transform)
        {
            Undo.SetTransformParent(mgrGO.transform, root.transform, "Parent WardrobeManager");
            changed = true;
        }
        WardrobeManager mgr = EnsureComponent<WardrobeManager>(mgrGO);

        Camera mainCam = Camera.main;
        SerializedObject mSO = new SerializedObject(mgr);
        mSO.FindProperty("mainPlayerCamera").objectReferenceValue = mainCam;
        mSO.FindProperty("wardrobeCamera").objectReferenceValue = wcCam;
        mSO.FindProperty("mirrorCamera").objectReferenceValue = mirrorCam;
        mSO.FindProperty("wardrobeRoot").objectReferenceValue = root.transform;
        mSO.FindProperty("cameraLocalOffset").vector3Value = DefaultCameraLocalOffset;
        mSO.FindProperty("cameraLocalRotation").vector3Value = DefaultCameraLocalRotation;
        mSO.FindProperty("cameraBlendDuration").floatValue = 0.6f;
        mSO.FindProperty("uiFadeDuration").floatValue = 0.3f;

        // --- 3. Player + PlayerOutfit + outfitRoot ---
        GameObject player = GameObject.Find(PlayerName);
        if (player != null)
        {
            PlayerControl pc = player.GetComponent<PlayerControl>();
            if (pc != null) mSO.FindProperty("playerControl").objectReferenceValue = pc;

            PlayerOutfit outfit = player.GetComponent<PlayerOutfit>();
            if (outfit == null) outfit = player.AddComponent<PlayerOutfit>();
            mSO.FindProperty("playerOutfit").objectReferenceValue = outfit;

            Transform head = player.transform.Find("Head") ?? player.transform.Find("Body") ?? player.transform;
            mSO.FindProperty("playerHead").objectReferenceValue = head;

            Transform outfitRoot = player.transform.Find(OutlineName);
            if (outfitRoot == null)
            {
                GameObject orGO = new GameObject(OutlineName);
                outfitRoot = orGO.transform;
                outfitRoot.SetParent(player.transform, false);
                Undo.RegisterCreatedObjectUndo(orGO, "Create OutfitRoot");
                changed = true;
            }
            SerializedObject oSO = new SerializedObject(outfit);
            oSO.FindProperty("outfitRoot").objectReferenceValue = outfitRoot;
            oSO.ApplyModifiedProperties();
        }
        mSO.ApplyModifiedProperties();

        // --- 4. UI Panel ---
        WardrobeUI wiredUI = WireWardrobeUI(mirrorCam, rt, out bool uiChanged);
        changed |= uiChanged;

        // Wire referensi WardrobeUI ke manager (supports ForceRefreshMirror saat Enter).
        if (wiredUI != null)
        {
            SerializedObject mUI = new SerializedObject(mgr);
            SerializedProperty pUI = mUI.FindProperty("wardrobeUI");
            if (pUI != null && pUI.objectReferenceValue != wiredUI)
            {
                pUI.objectReferenceValue = wiredUI;
                mUI.ApplyModifiedProperties();
                changed = true;
            }
        }

        // --- 5. Sample Outfit assets + prefab ---
        CreateSampleOutfits();

        // --- 6. Save ---
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Debug.Log("[WardrobeSetup] Selesai: WardrobeRoot + Cermin + WardrobeCamera + Manager + PlayerOutfit + UI + sample outfits. Scene disimpan.");
        }
        else
        {
            Debug.Log("[WardrobeSetup] Tidak ada perubahan (sudah ter-wire / idempoten).");
        }
    }

    private static void MR(MeshRenderer mr)
    {
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private static Material LoadOrCreateMaterial(string path, Color c)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Wardrobe");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static RenderTexture LoadOrCreateRenderTexture(string path)
    {
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (rt != null) return rt;
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Wardrobe");
        rt = new RenderTexture(MirrorTextureSize, MirrorTextureSize, 24, RenderTextureFormat.ARGB32);
        rt.name = "MirrorTexture";
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        AssetDatabase.CreateAsset(rt, path);
        AssetDatabase.SaveAssets();
        return rt;
    }

    /// <summary>
    /// Buat/validasi panel WardrobeUI di UI_Canvas dan wire semua ref (mirrorCamera + mirrorRenderTexture + manager).
    /// Mengembalikan komponen WardrobeUI; changedOut = true bila ada perubahan (untuk menandai scene dirty + save).
    /// </summary>
    private static WardrobeUI WireWardrobeUI(MirrorCamera mirrorCam, RenderTexture mirrorRT, out bool changedOut)
    {
        changedOut = false;
        if (mirrorCam == null)
        {
            GameObject mirrorGO = GameObject.Find(MirrorName);
            if (mirrorGO != null) mirrorCam = mirrorGO.GetComponent<MirrorCamera>();
        }
        if (mirrorRT == null)
            mirrorRT = LoadOrCreateRenderTexture("Assets/Materials/Wardrobe/MirrorTexture.renderTexture");

        GameObject canvasGO = GameObject.Find(CanvasName);
        if (canvasGO == null)
        {
            Debug.LogWarning("[WardrobeSetup] UI_Canvas tidak ditemukan -> UI wardrobe dilewatkan.");
            return null;
        }

        Transform existing = canvasGO.transform.Find("WardrobeUI_Panel");
        if (existing != null && existing.GetComponent<WardrobeUI>() != null)
        {
            WardrobeUI existingUI = existing.GetComponent<WardrobeUI>();
            // Panel sudah ada: cukup wire field baru (mirrorCamera + mirrorRenderTexture) bila perlu (idempoten).
            changedOut = WireUIExisting(existingUI, mirrorCam, mirrorRT);
            return existingUI;
        }
        // Panel lama yang tidak lengkap (gagal di tengah) -> hapus lalu buat ulang.
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject panel = new GameObject("WardrobeUI_Panel", typeof(RectTransform), typeof(CanvasGroup));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Full-screen mirror background.
        GameObject mirrorImgGO = new GameObject("MirrorRawImage", typeof(RawImage));
        mirrorImgGO.transform.SetParent(panel.transform, false);
        RectTransform mrt = mirrorImgGO.GetComponent<RectTransform>();
        mrt.anchorMin = Vector2.zero;
        mrt.anchorMax = Vector2.one;
        mrt.offsetMin = Vector2.zero;
        mrt.offsetMax = Vector2.zero;
        RawImage rawImg = mirrorImgGO.GetComponent<RawImage>();
        rawImg.color = Color.white;
        rawImg.raycastTarget = false;

        // Outfit grid: TENGAH-KANAN, 6 baris x 3 kolom (18 cell; slot pertama = Default).
        GameObject gridGO = new GameObject("OutfitGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGO.transform.SetParent(panel.transform, false);
        RectTransform gridRT = gridGO.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.pivot = new Vector2(0.5f, 0.5f);
        gridRT.anchoredPosition = new Vector2(255f, 40f);
        gridRT.sizeDelta = new Vector2(360f, 696f);
        GridLayoutGroup grid = gridGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(108f, 108f);
        grid.spacing = new Vector2(12f, 12f);
        grid.childAlignment = TextAnchor.MiddleLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        // Outfit button prefab (hidden).
        GameObject btnPrefab = CreateOutfitButtonPrefab(gridRT);

        // Action buttons.
        GameObject saveGO = CreateButton("SaveButton", "Simpan", panel.transform,
            new Vector2(-110f, 60f));
        GameObject cancelGO = CreateButton("CancelButton", "Batal", panel.transform,
            new Vector2(110f, 60f));

        // WardrobeUI component.
        WardrobeUI ui = panel.AddComponent<WardrobeUI>();
        SerializedObject uiSO = new SerializedObject(ui);
        uiSO.FindProperty("mirrorRawImage").objectReferenceValue = rawImg;
        uiSO.FindProperty("mirrorCamera").objectReferenceValue = mirrorCam;
        uiSO.FindProperty("mirrorRenderTexture").objectReferenceValue = mirrorRT;
        uiSO.FindProperty("outfitGrid").objectReferenceValue = gridRT;
        uiSO.FindProperty("outfitButtonPrefab").objectReferenceValue = btnPrefab;
        uiSO.FindProperty("saveButton").objectReferenceValue = saveGO.GetComponent<Button>();
        uiSO.FindProperty("cancelButton").objectReferenceValue = cancelGO.GetComponent<Button>();
        uiSO.ApplyModifiedProperties();

        // Wire manager UI refs.
        GameObject mgrGO = GameObject.Find(ManagerName);
        if (mgrGO != null)
        {
            WardrobeManager mgr = mgrGO.GetComponent<WardrobeManager>();
            if (mgr != null)
            {
                SerializedObject mSO = new SerializedObject(mgr);
                mSO.FindProperty("wardrobeUIPanel").objectReferenceValue = panel;
                mSO.FindProperty("uiCanvasGroup").objectReferenceValue = cg;
                mSO.ApplyModifiedProperties();
            }
        }

        panel.SetActive(false);
        changedOut = true;
        return ui;
    }

    /// <summary>Wire field WardrobeUI (mirrorCamera + mirrorRenderTexture) secara idempoten. Returns true bila ada referensi berubah.</summary>
    private static bool WireUIExisting(WardrobeUI ui, MirrorCamera mirrorCam, RenderTexture mirrorRT)
    {
        if (ui == null) return false;

        bool changed = false;
        SerializedObject so = new SerializedObject(ui);

        SerializedProperty pCam = so.FindProperty("mirrorCamera");
        if (pCam != null && pCam.objectReferenceValue != mirrorCam)
        {
            pCam.objectReferenceValue = mirrorCam;
            changed = true;
        }

        SerializedProperty pRT = so.FindProperty("mirrorRenderTexture");
        if (pRT != null && pRT.objectReferenceValue != mirrorRT)
        {
            pRT.objectReferenceValue = mirrorRT;
            changed = true;
        }

        if (changed) so.ApplyModifiedProperties();

        // Layout grid 6 baris x 3 kolom (tengah-kanan) untuk panel yang sudah ada.
        if (ApplyGridLayout(ui.transform.Find("OutfitGrid")))
            changed = true;

        return changed;
    }

    /// <summary>Terapkan layout grid 6 baris x 3 kolom (tengah-kanan). Returns true bila ada yang berubah.</summary>
    private static bool ApplyGridLayout(Transform gridT)
    {
        if (gridT == null) return false;

        RectTransform rt = gridT as RectTransform;
        if (rt == null) rt = gridT.GetComponent<RectTransform>();
        GridLayoutGroup g = gridT.GetComponent<GridLayoutGroup>();
        if (rt == null || g == null) return false;

        bool dirty = false;
        Vector2 anchorCenter = new Vector2(0.5f, 0.5f);
        if (rt.anchorMin != anchorCenter || rt.anchorMax != anchorCenter)
        {
            rt.anchorMin = anchorCenter;
            rt.anchorMax = anchorCenter;
            rt.pivot = anchorCenter;
            dirty = true;
        }
        Vector2 pos = new Vector2(255f, 40f);
        Vector2 size = new Vector2(360f, 696f);
        Vector2 cell = new Vector2(108f, 108f);
        Vector2 spacing = new Vector2(12f, 12f);
        if (rt.anchoredPosition != pos) { rt.anchoredPosition = pos; dirty = true; }
        if (rt.sizeDelta != size) { rt.sizeDelta = size; dirty = true; }
        if (g.constraint != GridLayoutGroup.Constraint.FixedColumnCount) { g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; dirty = true; }
        if (g.constraintCount != 3) { g.constraintCount = 3; dirty = true; }
        if (g.cellSize != cell) { g.cellSize = cell; dirty = true; }
        if (g.spacing != spacing) { g.spacing = spacing; dirty = true; }
        if (g.childAlignment != TextAnchor.MiddleLeft) { g.childAlignment = TextAnchor.MiddleLeft; dirty = true; }
        return dirty;
    }

    private static GameObject CreateOutfitButtonPrefab(Transform parent)
    {
        GameObject go = new GameObject("OutfitButtonPrefab", typeof(RectTransform), typeof(Button), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(108f, 108f);
        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.92f);
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = bg;

        GameObject iconGO = new GameObject("Icon", typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.95f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = Vector2.zero;
        iconRT.sizeDelta = new Vector2(96f, 96f);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        GameObject nameGO = new GameObject("Name", typeof(Text));
        nameGO.transform.SetParent(go.transform, false);
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0f);
        nameRT.anchorMax = new Vector2(1f, 0.4f);
        nameRT.pivot = new Vector2(0.5f, 0.5f);
        nameRT.offsetMin = new Vector2(8f, 6f);
        nameRT.offsetMax = new Vector2(-8f, -6f);
        Text nameText = nameGO.GetComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 13;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        nameText.raycastTarget = false;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.enabled = false;

        go.SetActive(false);
        return go;
    }

    private static GameObject CreateButton(string name, string label, Transform parent, Vector2 pos)
    {
        // Pattern §5.11: Image & Text TIDAK boleh pada GameObject yang sama (NRE).
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180f, 48f);
        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.45f, 0.8f, 0.95f);
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = bg;

        // Text sebagai ANAK terpisah (hindari Image+Text di GameObject yang sama).
        GameObject txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        Text txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        txt.raycastTarget = false;
        return go;
    }

    /// <summary>Buat 4 sample OutfitData + dummy full-body prefab, lalu isi PlayerOutfit.unlockedOutfits.</summary>
    private static void CreateSampleOutfits()
    {
        EnsureFolder("Assets/Scripts/Features/Wardrobe", "Data");
        EnsureFolder("Assets/Prefabs", "Wardrobe");

        string[] names = { "Casual", "Formal", "Sleepwear", "Workwear" };
        Color[] colors = {
            new Color(0.35f, 0.65f, 0.9f),
            new Color(0.18f, 0.18f, 0.28f),
            new Color(0.92f, 0.7f, 0.82f),
            new Color(0.45f, 0.55f, 0.35f),
        };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject prefab = CreateDummyOutfitPrefab(names[i], colors[i]);
            OutfitData data = LoadOrCreate(OutfitDataDir + "/" + names[i] + ".asset", names[i]);
            data.fullBodyPrefab = prefab;
            EditorUtility.SetDirty(data);
        }

        // Wire unlockedOutfits ke Player.
        GameObject player = GameObject.Find(PlayerName);
        if (player != null)
        {
            PlayerOutfit outfit = player.GetComponent<PlayerOutfit>();
            if (outfit != null)
            {
                SerializedObject so = new SerializedObject(outfit);
                SerializedProperty list = so.FindProperty("unlockedOutfits");
                list.arraySize = names.Length;
                for (int i = 0; i < names.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<OutfitData>(OutfitDataDir + "/" + names[i] + ".asset");
                so.ApplyModifiedProperties();
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static OutfitData LoadOrCreate(string path, string name)
    {
        OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<OutfitData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.outfitName = name;
        return data;
    }

    /// <summary>Buat dummy full-body outfit prefab (capsule torso + sphere head, tanpa collider).</summary>
    private static GameObject CreateDummyOutfitPrefab(string name, Color color)
    {
        string path = OutfitPrefabDir + "/Outfit_" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        Material mat = LoadOrCreateMaterial("Assets/Materials/Wardrobe/Mat_Outfit_" + name + ".mat", color);

        GameObject root = new GameObject("Outfit_" + name);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 2.15f, 0f);
        head.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        Object.DestroyImmediate(head.GetComponent<Collider>());
        head.GetComponent<MeshRenderer>().sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    /// <summary>Quad 1x1 dengan pivot TEPAT di tengah (normal +z), untuk permukaan cermin yang konsisten.</summary>
    private static Mesh MirrorQuadMesh()
    {
        Mesh m = new Mesh();
        m.name = "MirrorQuad_Center";
        m.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        m.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        m.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go == null) return null;
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }
}