using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility setup Kitchen Room (Farm Beware).
/// Menu 1: buat ItemData + Recipe + Prefab furnitur dapur.
/// Menu 2: wire scene (bangun area dapur, komponen, wiring, seed item, save scene).
/// Semua IDEMPOTEN (aman dijalankan ulang).
/// </summary>
public static class KitchenSetup
{
    private const string FurnitureDir = "Assets/Prefabs/Furniture/Kitchen";
    private const string KitchenDataDir = "Assets/Scripts/Features/Kitchen/Data";
    private const string ItemDataDir = "Assets/Scripts/Features/Inventory/Data";
    private const string IconSourcePath = "Assets/Scripts/Features/Inventory/Data/DummySword.asset";

    #region Menu 1: Data & Prefabs

    [MenuItem("Farm Beware/Kitchen/Create Kitchen Data & Prefabs")]
    public static void CreateKitchenDataAndPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Furniture");
        EnsureFolder("Assets/Prefabs/Furniture", "Kitchen");

        // --- ItemData food states ---
        ItemData carrotDirty = CreateItemData(ItemDataDir + "/Carrot_Dirty.asset", "Carrot (Dirty)", ItemData.ItemType.Material,
            ItemData.FoodCategory.Vegetable, 20);
        ItemData carrotClean = CreateItemData(ItemDataDir + "/Carrot_Clean.asset", "Carrot (Clean)", ItemData.ItemType.Material,
            ItemData.FoodCategory.Vegetable, 20);
        ItemData riceRaw = CreateItemData(ItemDataDir + "/Rice_Raw.asset", "Rice (Raw)", ItemData.ItemType.Material,
            ItemData.FoodCategory.Ingredient, 20);
        CreateItemData(ItemDataDir + "/Cooked_Veggies.asset", "Cooked Veggies", ItemData.ItemType.Consumable,
            ItemData.FoodCategory.Dish, 20);
        CreateItemData(ItemDataDir + "/Cooked_Rice.asset", "Cooked Rice", ItemData.ItemType.Consumable,
            ItemData.FoodCategory.Dish, 20);

        // --- Kitchen Recipes ---
        EnsureFolder("Assets/Scripts/Features/Kitchen", "Data");
        KitchenRecipe washCarrot = CreateRecipe(KitchenDataDir + "/Wash_Carrot.asset", carrotDirty, carrotClean, 1, 3f);
        KitchenRecipe cookVeg = CreateRecipe(KitchenDataDir + "/Cook_Veggies.asset", carrotClean,
            AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataDir + "/Cooked_Veggies.asset"), 1, 5f);
        KitchenRecipe cookRice = CreateRecipe(KitchenDataDir + "/Cook_Rice.asset", riceRaw,
            AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataDir + "/Cooked_Rice.asset"), 1, 4f);

        // --- Prefabs kosmetik & furnitur ---
        CreateFurniturePrefab(PrimitiveType.Cube, "Fridge", new Vector3(1.1f, 2f, 1f), new Color(0.75f, 0.85f, 0.9f));
        CreateFurniturePrefab(PrimitiveType.Cube, "Sink", new Vector3(1.4f, 0.85f, 0.75f), new Color(0.5f, 0.55f, 0.6f));
        CreateFurniturePrefab(PrimitiveType.Cube, "Stove", new Vector3(1.4f, 0.5f, 0.8f), new Color(0.2f, 0.22f, 0.25f));
        CreateFurniturePrefab(PrimitiveType.Cube, "Table", new Vector3(2f, 0.1f, 1f), new Color(0.55f, 0.4f, 0.28f));
        CreateFurniturePrefab(PrimitiveType.Cube, "Chair", new Vector3(0.45f, 0.55f, 0.45f), new Color(0.6f, 0.45f, 0.32f));
        CreateFurniturePrefab(PrimitiveType.Cube, "FoodPrepArea", new Vector3(1.3f, 0.12f, 0.8f), new Color(0.42f, 0.38f, 0.34f));
        CreateFurniturePrefab(PrimitiveType.Cube, "Window", new Vector3(1.6f, 1.2f, 0.1f), new Color(0.4f, 0.62f, 0.85f));
        CreateFurniturePrefab(PrimitiveType.Cube, "Door", new Vector3(1f, 2f, 0.2f), new Color(0.5f, 0.35f, 0.25f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[KitchenSetup] ItemData, Recipe, dan 8 prefab furnitur dapur berhasil dibuat. (Gunakan menu Wire untuk merakit scene.)");
    }

    private static ItemData CreateItemData(string path, string itemName, ItemData.ItemType type,
        ItemData.FoodCategory category, int maxStack)
    {
        ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.itemName = itemName;
        data.type = type;
        data.foodCategory = category;
        data.maxStack = maxStack;
        data.healAmount = 0;
        data.equipPrefab = null;
        data.placeablePrefab = null;

        if (data.itemIcon == null)
        {
            ItemData iconSource = AssetDatabase.LoadAssetAtPath<ItemData>(IconSourcePath);
            if (iconSource != null && iconSource.itemIcon != null)
                data.itemIcon = iconSource.itemIcon;
        }

        EditorUtility.SetDirty(data);
        return data;
    }

    private static KitchenRecipe CreateRecipe(string path, ItemData input, ItemData output, int outputCount, float time)
    {
        KitchenRecipe recipe = AssetDatabase.LoadAssetAtPath<KitchenRecipe>(path);
        if (recipe == null)
        {
            recipe = ScriptableObject.CreateInstance<KitchenRecipe>();
            AssetDatabase.CreateAsset(recipe, path);
        }

        recipe.input = input;
        recipe.output = output;
        recipe.outputCount = outputCount;
        recipe.processTime = time;

        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static void CreateFurniturePrefab(PrimitiveType shape, string name, Vector3 scale, Color color)
    {
        string path = FurnitureDir + "/" + name + ".prefab";
        string matPath = FurnitureDir + "/Kitchen_" + name + "_mat.mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;

            mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;

        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.transform.localScale = scale;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    #endregion

    #region Menu 2: Wire Scene

    [MenuItem("Farm Beware/Kitchen/Wire Kitchen Scene")]
    public static void WireKitchenScene()
    {
        GameObject playerGO = GameObject.Find("Player");
        if (playerGO == null)
        {
            Debug.LogError("[KitchenSetup] 'Player' tidak ditemukan di scene. Abort.");
            return;
        }

        InventoryComponent playerInv = playerGO.GetComponent<InventoryComponent>();
        GameObject root = EnsureSceneObject("KitchenRoot", null, new Vector3(14f, 0f, 2f));

        // --- Lantai dapur ---
        GameObject floor = CreatePrimitiveInScene(PrimitiveType.Plane, "Kitchen_Floor", root.transform, new Vector3(0f, 0f, 0f), new Vector3(4f, 1f, 3.5f));
        DestroyColliderIfAny(floor);

        // --- Kulkas ---
        GameObject fridge = CreatePrimitiveInScene(PrimitiveType.Cube, "Fridge", root.transform, new Vector3(-1.6f, 1f, 2.4f), new Vector3(1.1f, 2f, 1f));
        ResetColliderBounds(fridge);
        InventoryComponent fridgeInv = GetOrAdd<InventoryComponent>(fridge);
        if (fridgeInv.slots == null || fridgeInv.slots.Count != 8)
            fridgeInv.ResetInventory(8);
        GetOrAdd<RefrigeratorInteractable>(fridge);

        // --- Sink ---
        GameObject sink = CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Sink", root.transform, new Vector3(0.2f, 0.43f, 2.4f), new Vector3(1.4f, 0.85f, 0.75f));
        ResetColliderBounds(sink);
        InventoryComponent sinkInv = GetOrAdd<InventoryComponent>(sink);
        if (sinkInv.slots == null || sinkInv.slots.Count != 1)
            sinkInv.ResetInventory(1);
        KitchenSinkInteractable sinkStation = GetOrAdd<KitchenSinkInteractable>(sink);

        // --- Kompor ---
        GameObject stove = CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Stove", root.transform, new Vector3(1.4f, 0.25f, 2.4f), new Vector3(1.4f, 0.5f, 0.8f));
        ResetColliderBounds(stove);
        InventoryComponent stoveInv = GetOrAdd<InventoryComponent>(stove);
        if (stoveInv.slots == null || stoveInv.slots.Count != 2)
            stoveInv.ResetInventory(2);
        StoveInteractable stoveStation = GetOrAdd<StoveInteractable>(stove);

        // --- Kosmetik ---
        CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Table", root.transform, new Vector3(0f, 0.05f, -0.1f), new Vector3(2f, 0.1f, 1f));
        CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Chair", root.transform, new Vector3(0f, 0.3f, -1f), new Vector3(0.45f, 0.6f, 0.45f));
        CreatePrimitiveInScene(PrimitiveType.Cube, "FoodPrepArea", root.transform, new Vector3(-0.75f, 0.06f, -0.1f), new Vector3(1.3f, 0.12f, 0.8f));
        GameObject window = CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Window", root.transform, new Vector3(1.8f, 1.2f, 0.5f), new Vector3(1.6f, 1.2f, 0.1f));
        DestroyColliderIfAny(window);

        // --- Backyard placeholder & pintu ---
        GameObject backyard = CreatePrimitiveInScene(PrimitiveType.Plane, "Backyard_Floor", root.transform, new Vector3(0f, 0f, -9f), new Vector3(6f, 1f, 6f));
        DestroyColliderIfAny(backyard);
        GameObject spawn = CreatePrimitiveInScene(PrimitiveType.Sphere, "Spawn_Backyard", root.transform, new Vector3(0f, 0.4f, -7f), new Vector3(0.4f, 0.4f, 0.4f));
        DestroyColliderIfAny(spawn);

        GameObject door = CreatePrimitiveInScene(PrimitiveType.Cube, "Back_Door", root.transform, new Vector3(-0f, 1f, 0.75f), new Vector3(1f, 2f, 0.2f));
        ResetColliderBounds(door);
        DoorInteractable doorInteractable = GetOrAdd<DoorInteractable>(door);
        SerializedObject doorSO = new SerializedObject(doorInteractable);
        doorSO.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
        doorSO.ApplyModifiedProperties();

        // --- Wiring stasiun ---
        SerializedObject sinkSO = new SerializedObject(sinkStation);
        sinkSO.FindProperty("resultTarget").objectReferenceValue = playerInv;

        SerializedProperty washList = sinkSO.FindProperty("washRecipes");
        washList.arraySize = 1;
        washList.GetArrayElementAtIndex(0).objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<KitchenRecipe>(KitchenDataDir + "/Wash_Carrot.asset");
        sinkSO.ApplyModifiedProperties();

        SerializedObject stoveSO = new SerializedObject(stoveStation);
        SerializedProperty stoveList = stoveSO.FindProperty("recipes");
        stoveList.arraySize = 2;
        stoveList.GetArrayElementAtIndex(0).objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<KitchenRecipe>(KitchenDataDir + "/Cook_Veggies.asset");
        stoveList.GetArrayElementAtIndex(1).objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<KitchenRecipe>(KitchenDataDir + "/Cook_Rice.asset");
        stoveSO.ApplyModifiedProperties();

        // --- UI panel progress stasiun ---
        WireStationUI(stoveStation);

        // --- Seed demo ke player ---
        ItemData carrotDirty = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataDir + "/Carrot_Dirty.asset");
        ItemData carrotClean = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataDir + "/Carrot_Clean.asset");
        ItemData riceRaw = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataDir + "/Rice_Raw.asset");
        if (carrotDirty != null && playerInv.CountItem(carrotDirty) == 0) playerInv.AddItem(carrotDirty, 2);
        if (carrotClean != null && playerInv.CountItem(carrotClean) == 0) playerInv.AddItem(carrotClean, 1);
        if (riceRaw != null && playerInv.CountItem(riceRaw) == 0) playerInv.AddItem(riceRaw, 1);

        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Debug.Log("[KitchenSetup] Wire selesai: Dapur + Kulkas + Sink + Kompor + kosmetik + pintu backyard + seed player. Scene tersimpan.");
    }

    private static void WireStationUI(StoveInteractable stoveStation)
    {
        GameObject canvasGO = GameObject.Find("UI_Canvas");
        if (canvasGO == null)
        {
            Debug.LogWarning("[KitchenSetup] 'UI_Canvas' tidak ditemukan -> panel progress dapur dilewatkan.");
            return;
        }

        Transform existing = canvasGO.transform.Find("KitchenStationUI_Panel");
        if (existing != null)
            return;

        GameObject panel = new GameObject("KitchenStationUI_Panel", typeof(RectTransform));
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.SetParent(canvasGO.transform, false);
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 110f);
        panelRT.sizeDelta = new Vector2(320f, 44f);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform fillRT = CreateRectChild("Fill", panelRT);
        Image fill = fillRT.gameObject.AddComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.color = new Color(0.2f, 0.8f, 0.3f);

        RectTransform statusRT = CreateRectChild("Status", panelRT);
        Text status = statusRT.gameObject.AddComponent<Text>();
        status.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        status.fontSize = 14;
        status.color = Color.white;
        status.alignment = TextAnchor.MiddleCenter;
        status.text = "";
        status.raycastTarget = false;

        KitchenStationUI ui = panel.AddComponent<KitchenStationUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("station").objectReferenceValue = stoveStation;
        so.FindProperty("progressFill").objectReferenceValue = fill;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.ApplyModifiedProperties();
    }

    private static RectTransform CreateRectChild(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4, 4);
        rt.offsetMax = new Vector2(-4, -4);
        return rt;
    }

    private static GameObject CreatePrimitiveInScene(PrimitiveType shape, string name, Transform parent,
        Vector3 localPos, Vector3 localScale)
    {
        // Idempoten: bila anak dengan nama sama sudah ada, jangan duplikat.
        if (parent != null)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;
        }

        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        return go;
    }

    private static GameObject EnsureSceneObject(string name, Transform parent, Vector3 worldPos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
        }
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }

    private static void ResetColliderBounds(GameObject go)
    {
        // Primitive memiliki collider; pastikan aktif & non-trigger.
        Collider col = go.GetComponent<Collider>();
        if (col != null && col.isTrigger)
            col.isTrigger = false;
    }

    private static void DestroyColliderIfAny(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Object.DestroyImmediate(col);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(full))
            return;
        AssetDatabase.CreateFolder(parent, child);
    }

    #endregion
}