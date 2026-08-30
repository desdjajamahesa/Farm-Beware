using UnityEngine;

/// <summary>
/// Automatically builds the 3D room floor layout with perfectly straight, aligned outer walls
/// matching the exact screenshot layout without any geometric or perspective distortion.
/// </summary>
[ExecuteAlways]
public class RoomBuilder : MonoBehaviour
{
    [Header("Room Layout Dimensions")]
    public float roomScale = 5.0f; // Scale factor for 80x80 grand arena room size
    public float roomWidth = 80.0f;
    public float roomLength = 80.0f;
    public float wallHeight = 14.0f;
    public float wallThickness = 1.5f;

    [Header("Materials & Styling")]
    public Color wallColor = new Color(0.48f, 0.54f, 0.60f); // Slate gray from screenshot
    public Color floorColor = new Color(0.55f, 0.62f, 0.68f); // Slightly lighter slate gray
    public Color capsuleColor = new Color(0.95f, 0.95f, 0.95f); // Clean white capsule
    public Color pillarColor = new Color(0.60f, 0.66f, 0.72f); // Pillar cube color

    [Header("Player Settings")]
    public Vector3 playerSpawnPos = new Vector3(0f, 1.0f, -20.0f);

    [ContextMenu("Generate Room & Capsule Player")]
    public void BuildRoom()
    {
        Transform existing = transform.Find("RoomEnvironment");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        GameObject roomParent = new GameObject("RoomEnvironment");
        roomParent.transform.SetParent(transform);

        // Calculate dynamic dimensions based on scale
        roomWidth = 16.0f * roomScale;
        roomLength = 16.0f * roomScale;
        wallHeight = 3.2f * roomScale;
        wallThickness = 0.2f * roomScale;

        // URP & Built-in compatible materials
        Material wallMat = CreateSimpleMaterial("WallMaterial", wallColor);
        Material floorMat = CreateSimpleMaterial("FloorMaterial", floorColor);
        Material capsuleMat = CreateSimpleMaterial("CapsuleMaterial", capsuleColor);
        Material pillarMat = CreateSimpleMaterial("PillarMaterial", pillarColor);

        // Create Floor Plane (Matches room footprint)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "FloorGrid";
        floor.transform.SetParent(roomParent.transform);
        floor.transform.position = new Vector3(0, -0.1f, 0);
        floor.transform.localScale = new Vector3(roomWidth, 0.2f, roomLength);
        floor.GetComponent<Renderer>().material = floorMat;

        if (floor.GetComponent<BoxCollider>() == null)
            floor.AddComponent<BoxCollider>();

        // Create Walls Parent
        GameObject wallsParent = new GameObject("Walls_With_Colliders");
        wallsParent.transform.SetParent(roomParent.transform);

        float halfW = roomWidth / 2.0f;
        float halfL = roomLength / 2.0f;
        float s = roomScale;

        // 1. OUTER WALLS (Back and Right outer boundary walls matching reference screenshot)
        CreateWall(wallsParent, "Wall_Right", new Vector3(halfW, wallHeight/2f, 0), new Vector3(wallThickness, wallHeight, roomLength + wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Back", new Vector3(0, wallHeight/2f, halfL), new Vector3(roomWidth + wallThickness, wallHeight, wallThickness), wallMat);

        // 2. INNER PARTITION WALLS (Exact matching layout coordinates scaled for 80x80)
        // Parallel front-left vertical slabs:
        CreateWall(wallsParent, "Wall_Inner_Left1", new Vector3(-5.4f * s, wallHeight/2f, -5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Left1_Cap", new Vector3(-4.8f * s, wallHeight/2f, -2.0f * s), new Vector3(1.2f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Left2", new Vector3(-2.8f * s, wallHeight/2f, -5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Left2_Cap", new Vector3(-3.4f * s, wallHeight/2f, -2.0f * s), new Vector3(1.2f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_CenterBottom", new Vector3(0.2f * s, wallHeight/2f, -5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);

        // Top-Left Standing Wall & Nook Chamber:
        CreateWall(wallsParent, "Wall_Inner_Left_Standing", new Vector3(-6.8f * s, wallHeight/2f, 0.5f * s), new Vector3(wallThickness, wallHeight, 3.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopLeft_V", new Vector3(-5.4f * s, wallHeight/2f, 4.5f * s), new Vector3(wallThickness, wallHeight, 7.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopLeft_H", new Vector3(-3.9f * s, wallHeight/2f, 1.0f * s), new Vector3(3.0f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopLeft_Divider", new Vector3(-3.0f * s, wallHeight/2f, 5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopMiddle", new Vector3(-1.0f * s, wallHeight/2f, 4.5f * s), new Vector3(wallThickness, wallHeight, 7.0f * s), wallMat);

        // Top-Right Chamber & Middle-Right Partitioning:
        CreateWall(wallsParent, "Wall_Inner_TopRight_V", new Vector3(3.2f * s, wallHeight/2f, 5.5f * s), new Vector3(wallThickness, wallHeight, 5.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_H", new Vector3(5.2f * s, wallHeight/2f, 3.0f * s), new Vector3(3.2f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_Middle_V", new Vector3(2.4f * s, wallHeight/2f, -0.5f * s), new Vector3(wallThickness, wallHeight, 5.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_Middle_H", new Vector3(3.4f * s, wallHeight/2f, 2.0f * s), new Vector3(2.0f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_Front_V", new Vector3(2.4f * s, wallHeight/2f, -6.0f * s), new Vector3(wallThickness, wallHeight, 4.0f * s), wallMat);

        // Standalone Block / Pillar (Placed at bottom-right corner outside wall as in reference image)
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "Pillar_Block";
        pillar.transform.SetParent(roomParent.transform);
        pillar.transform.position = new Vector3(halfW + 1.2f * s, wallHeight * 0.2f, -halfL + 1.2f * s);
        pillar.transform.localScale = new Vector3(1.2f * s, wallHeight * 0.4f, 1.2f * s);
        pillar.GetComponent<Renderer>().material = pillarMat;
        if (pillar.GetComponent<BoxCollider>() == null) pillar.AddComponent<BoxCollider>();

        playerSpawnPos = new Vector3(0f, 1.0f, -4.0f * s);
        SetupPlayerCapsule(roomParent, capsuleMat);
        SetupCameraAndLighting();
        PlaceBedroomModel(roomParent);

        Debug.Log("<color=green>SUCCESS:</color> 80x80 3D Room layout generated matching Blender reference model exactly!");
    }

    private void PlaceBedroomModel(GameObject roomParent)
    {
        GameObject existingBedroom = GameObject.Find("Bedroom");
        if (existingBedroom != null)
        {
            if (Application.isPlaying) Destroy(existingBedroom);
            else DestroyImmediate(existingBedroom);
        }

        GameObject bedroomPrefab = LoadBedroomPrefab();
        if (bedroomPrefab == null)
        {
            Debug.LogWarning("<color=yellow>BEDROOM:</color> Asset 'bedrooom.fbx' tidak ditemukan. Pastikan ada di Assets/Models/bedrooom.fbx.");
            return;
        }

        float s = roomScale;
        GameObject bedroom = Instantiate(bedroomPrefab, roomParent.transform);
        bedroom.name = "Bedroom";
        bedroom.transform.position = new Vector3(-6.6f * s, 0f, 4.8f * s); // Center of top-left chamber
        bedroom.transform.rotation = Quaternion.Euler(0, 90f, 0); // Face back wall

        Debug.Log($"<color=cyan>BEDROOM:</color> FBX bedroom placed in top-left chamber at ({-6.6f * s}, 0, {4.8f * s})!");
    }

    private GameObject LoadBedroomPrefab()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/bedrooom.fbx");
#else
        return Resources.Load<GameObject>("Models/bedrooom");
#endif
    }

    private void CreateWall(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent.transform);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = mat;

        BoxCollider boxCol = wall.GetComponent<BoxCollider>();
        if (boxCol == null)
        {
            boxCol = wall.AddComponent<BoxCollider>();
        }
        boxCol.isTrigger = false;
        boxCol.enabled = true;
        wall.isStatic = true;
    }

    private void SetupPlayerCapsule(GameObject roomParent, Material capsuleMat)
    {
        GameObject player = GameObject.Find("PlayerCapsule");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PlayerCapsule";
        }

        player.transform.position = new Vector3(playerSpawnPos.x, 1.0f, playerSpawnPos.z);
        player.transform.localScale = new Vector3(1f, 1f, 1f);
        player.GetComponent<Renderer>().material = capsuleMat;

        CharacterController charCtrl = player.GetComponent<CharacterController>();
        if (charCtrl == null)
        {
            charCtrl = player.AddComponent<CharacterController>();
        }
        charCtrl.height = 2.0f;
        charCtrl.radius = 0.5f;
        charCtrl.center = Vector3.zero;
        charCtrl.stepOffset = 0.4f; // Enable walking up stair steps smoothly
        charCtrl.slopeLimit = 45.0f;
        charCtrl.skinWidth = 0.02f;

        PlayerController pController = player.GetComponent<PlayerController>();
        if (pController == null)
        {
            pController = player.AddComponent<PlayerController>();
        }
    }

    private void SetupCameraAndLighting()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        // Configure Camera for Orthographic Projection to eliminate perspective narrowing distortion (Hades 2 style wide view)
        float orthoSize = 19.5f * (roomScale / 2.5f);
        float camDist = 34.0f * (roomScale / 2.5f);

        mainCam.orthographic = true;
        mainCam.orthographicSize = orthoSize;

        CameraController camCtrl = mainCam.GetComponent<CameraController>();
        if (camCtrl == null)
        {
            camCtrl = mainCam.gameObject.AddComponent<CameraController>();
        }
        camCtrl.orthographicSize = orthoSize;
        camCtrl.distance = camDist;

        GameObject player = GameObject.Find("PlayerCapsule");
        if (player != null)
        {
            camCtrl.target = player.transform;
        }

        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.12f, 0.15f, 0.20f);

        Light mainLight = FindObjectOfType<Light>();
        if (mainLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            mainLight = lightObj.AddComponent<Light>();
            mainLight.type = LightType.Directional;
        }
        mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        mainLight.color = Color.white;
        mainLight.intensity = 1.2f;
    }

    private Material CreateSimpleMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.name = name;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.color = color;

        return mat;
    }

    private void Start()
    {
        if (transform.Find("RoomEnvironment") == null)
        {
            BuildRoom();
        }
    }
}
