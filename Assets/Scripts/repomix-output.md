This file is a merged representation of the entire codebase, combined into a single document by Repomix.
The content has been processed where comments have been removed.

# File Summary

## Purpose
This file contains a packed representation of the entire repository's contents.
It is designed to be easily consumable by AI systems for analysis, code review,
or other automated processes.

## File Format
The content is organized as follows:
1. This summary section
2. Repository information
3. Directory structure
4. Repository files (if enabled)
5. Multiple file entries, each consisting of:
  a. A header with the file path (## File: path/to/file)
  b. The full contents of the file in a code block

## Usage Guidelines
- This file should be treated as read-only. Any changes should be made to the
  original repository files, not this packed version.
- When processing this file, use the file path to distinguish
  between different files in the repository.
- Be aware that this file may contain sensitive information. Handle it with
  the same level of security as you would the original repository.

## Notes
- Some files may have been excluded based on .gitignore rules and Repomix's configuration
- Binary files are not included in this packed representation. Please refer to the Repository Structure section for a complete list of file paths, including binary files
- Files matching patterns in .gitignore are excluded
- Files matching default ignore patterns are excluded
- Code comments have been removed from supported file types
- Files are sorted by Git change count (files with more changes are at the bottom)

# Directory Structure
```
Behaviour/
  IsometricCamera.cs
  IsometricCamera.cs.meta
Editor/
  RoomBuilderEditor.cs
  RoomBuilderEditor.cs.meta
Features/
  Camera/
    WallOccluder.cs
    WallOccluder.cs.meta
    WallOcclusionManager.cs
    WallOcclusionManager.cs.meta
  Interaction/
    BedInteractable.cs
    BedInteractable.cs.meta
    GenericFurnitureInteractable.cs
    GenericFurnitureInteractable.cs.meta
    Highlightable.cs
    Highlightable.cs.meta
    HoverLabelController.cs
    HoverLabelController.cs.meta
    IInteractable.cs
    IInteractable.cs.meta
    InteractionZone.cs
    InteractionZone.cs.meta
    PlayerInteractor.cs
    PlayerInteractor.cs.meta
    StorageInteractable.cs
    StorageInteractable.cs.meta
    TrophyCabinetInteractable.cs
    TrophyCabinetInteractable.cs.meta
    WardrobeInteractable.cs
    WardrobeInteractable.cs.meta
    WorldLabel.cs
    WorldLabel.cs.meta
  Inventory/
    Data/
      Carrot_Clean.asset
      Carrot_Clean.asset.meta
      Carrot_Dirty.asset
      Carrot_Dirty.asset.meta
      Cooked_Rice.asset
      Cooked_Rice.asset.meta
      Cooked_Veggies.asset
      Cooked_Veggies.asset.meta
      DummySword.asset
      DummySword.asset.meta
      Potion.asset
      Potion.asset.meta
      Rice_Raw.asset
      Rice_Raw.asset.meta
      Seed.asset
      Seed.asset.meta
      Stone.asset
      Stone.asset.meta
      TrophyCapsule.asset
      TrophyCapsule.asset.meta
      TrophyCube.asset
      TrophyCube.asset.meta
      TrophySphere.asset
      TrophySphere.asset.meta
      Wood.asset
      Wood.asset.meta
    UI/
      DraggableItem.cs
      DraggableItem.cs.meta
      InventoryManagerUI.cs
      InventoryManagerUI.cs.meta
      InventorySlotUI.cs
      InventorySlotUI.cs.meta
      ItemDisplayUI.cs
      ItemDisplayUI.cs.meta
    Data.meta
    InventoryComponent.cs
    InventoryComponent.cs.meta
    InventorySlot.cs
    InventorySlot.cs.meta
    ItemData.cs
    ItemData.cs.meta
    UI.meta
  Kitchen/
    Data/
      Cook_Rice.asset
      Cook_Rice.asset.meta
      Cook_Veggies.asset
      Cook_Veggies.asset.meta
      Wash_Carrot.asset
      Wash_Carrot.asset.meta
    UI/
      KitchenStationProgressOverlay.cs
      KitchenStationProgressOverlay.cs.meta
      KitchenStationSoundFx.cs
      KitchenStationSoundFx.cs.meta
      KitchenStationUI.cs
      KitchenStationUI.cs.meta
    Data.meta
    DoorInteractable.cs
    DoorInteractable.cs.meta
    KitchenRecipe.cs
    KitchenRecipe.cs.meta
    KitchenSinkInteractable.cs
    KitchenSinkInteractable.cs.meta
    KitchenStation.cs
    KitchenStation.cs.meta
    RefrigeratorInteractable.cs
    RefrigeratorInteractable.cs.meta
    StoveInteractable.cs
    StoveInteractable.cs.meta
    UI.meta
  Time/
    UI/
      DayTransitionUI.cs
      DayTransitionUI.cs.meta
    TimeManager.cs
    TimeManager.cs.meta
    UI.meta
  Trophy/
    TrophyItem.cs
    TrophyItem.cs.meta
    TrophyRackVisuals.cs
    TrophyRackVisuals.cs.meta
    TrophySnapPoint.cs
    TrophySnapPoint.cs.meta
    TrophySystemManager.cs
    TrophySystemManager.cs.meta
  Wardrobe/
    Data/
      Casual.asset
      Casual.asset.meta
      Formal.asset
      Formal.asset.meta
      Sleepwear.asset
      Sleepwear.asset.meta
      Workwear.asset
      Workwear.asset.meta
    UI/
      WardrobeUI.cs
      WardrobeUI.cs.meta
    Data.meta
    MirrorCamera.cs
    MirrorCamera.cs.meta
    OutfitData.cs
    OutfitData.cs.meta
    PlayerOutfit.cs
    PlayerOutfit.cs.meta
    UI.meta
    WardrobeManager.cs
    WardrobeManager.cs.meta
  Camera.meta
  Interaction.meta
  Inventory.meta
  Kitchen.meta
  Time.meta
  Trophy.meta
  Wardrobe.meta
Player/
  UI/
    PlayerHealthUI.cs
    PlayerHealthUI.cs.meta
  PlayerControl.cs
  PlayerControl.cs.meta
  PlayerEquipment.cs
  PlayerEquipment.cs.meta
  PlayerInputActions.cs
  PlayerInputActions.cs.meta
  PlayerInputActions.inputactions
  PlayerInputActions.inputactions.meta
  PlayerStats.cs
  PlayerStats.cs.meta
  UI.meta
AutoDoor.cs
AutoDoor.cs.meta
Behaviour.meta
CameraController.cs
CameraController.cs.meta
Editor.meta
FaceCamera.cs
FaceCamera.cs.meta
Features.meta
GameInitializer.cs
GameInitializer.cs.meta
InventoryUI.cs
InventoryUI.cs.meta
Player.meta
PlayerController.cs
PlayerController.cs.meta
RoomBuilder.cs
RoomBuilder.cs.meta
WallOcclusionFader.cs
WallOcclusionFader.cs.meta
```

# Files

## File: Behaviour/IsometricCamera.cs
```csharp
using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target;
    public Vector3 offset = new Vector3(-10f, 10f, -10f);

    [Header("Pengaturan Kehalusan")]
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;


        Vector3 desiredPosition = target.position + offset;


        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);


        transform.position = smoothedPosition;
    }
}
```

## File: Behaviour/IsometricCamera.cs.meta
```
fileFormatVersion: 2
guid: bd01c2371c9acb24392ae44125422532
```

## File: Editor/RoomBuilderEditor.cs
```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;





public class RoomBuilderEditor : EditorWindow
{
    [MenuItem("Tools/Generate 3D Room & Capsule Player")]
    public static void GenerateRoomFromMenu()
    {
        RoomBuilder builder = FindObjectOfType<RoomBuilder>();
        if (builder == null)
        {
            GameObject builderObj = new GameObject("SampleSceneManager");
            builder = builderObj.AddComponent<RoomBuilder>();
        }

        builder.BuildRoom();
        Selection.activeGameObject = builder.gameObject;


        EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        EditorSceneManager.SaveScene(builder.gameObject.scene);

        EditorUtility.DisplayDialog("3D Room Re-Generated & Saved",
            "The 3D Room layout and Capsule Player have been generated and saved successfully!\n\n" +
            "1. Denah ruangan 3D 80x80 persis seperti di gambar sampel.\n" +
            "2. BoxCollider terpasang di semua tembok.\n" +
            "3. Karakter CapsulePutih siap berjalan dengan WASD.\n" +
            "4. Scene telah berhasil DISIMPAN (SampleScene.unity).\n\n" +
            "Tekan PLAY di Unity untuk menguji gerakan capsule!", "Siap!");
    }

    public static void SaveSceneBatchMode()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        RoomBuilder builder = FindObjectOfType<RoomBuilder>();
        if (builder == null)
        {
            GameObject builderObj = new GameObject("SampleSceneManager");
            builder = builderObj.AddComponent<RoomBuilder>();
        }

        builder.BuildRoom();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Batch mode: 80x80 Room Layout successfully built and saved to SampleScene.unity");
    }
}

[CustomEditor(typeof(RoomBuilder))]
public class RoomBuilderInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomBuilder builder = (RoomBuilder)target;

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
        if (GUILayout.Button("Generate & Save 3D Room Layout", GUILayout.Height(40)))
        {
            builder.BuildRoom();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            EditorSceneManager.SaveScene(builder.gameObject.scene);
        }
        GUI.backgroundColor = Color.white;
    }
}
```

## File: Editor/RoomBuilderEditor.cs.meta
```
fileFormatVersion: 2
guid: f7c0cd26304583f4c93ac0ff16a0157a
```

## File: Features/Camera/WallOccluder.cs
```csharp
using UnityEngine;

namespace FeaturesCamera
{




    public class WallOccluder : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Target alpha when wall is occluding (0 = invisible, 1 = opaque)")]
        [Range(0f, 1f)]
        public float transparentAlpha = 0.15f;

        [Tooltip("Fade speed (higher = faster)")]
        public float fadeSpeed = 8f;

        [Header("References (auto-assigned if empty)")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material originalMaterial;
        [SerializeField] private Material transparentMaterial;


        private float currentAlpha = 1f;
        private bool isOccluding = false;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;


            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();


            if (meshRenderer != null && originalMaterial == null)
                originalMaterial = meshRenderer.sharedMaterial;


            CreateTransparentMaterial();

            currentAlpha = 1f;
            isInitialized = true;
        }

        private void CreateTransparentMaterial()
        {

            var transparentMat = Resources.Load<Material>("Materials/Walls/Mat_Wall_Transparent");


            if (transparentMat == null) {
                var guids = UnityEditor.AssetDatabase.FindAssets("Mat_Wall_Transparent t:Material");
                if (guids.Length > 0) {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    transparentMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }


            if (transparentMat == null && meshRenderer != null && meshRenderer.sharedMaterial != null) {
                transparentMat = new Material(meshRenderer.sharedMaterial);
                transparentMat.name = "Mat_Wall_Transparent_Runtime";
            }


            if (transparentMat != null) {
                transparentMat = new Material(transparentMat);
                transparentMat.name = "Mat_Wall_Transparent_Instance";


                if (transparentMat.HasProperty("_Surface")) transparentMat.SetFloat("_Surface", 1f);
                if (transparentMat.HasProperty("_Blend")) transparentMat.SetFloat("_Blend", 0f);
                if (transparentMat.HasProperty("_SrcBlend")) transparentMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (transparentMat.HasProperty("_DstBlend")) transparentMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (transparentMat.HasProperty("_ZWrite")) transparentMat.SetFloat("_ZWrite", 0f);
                if (transparentMat.HasProperty("_Cull")) transparentMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                transparentMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                transparentMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentMat.renderQueue = 3000;
            }

            this.transparentMaterial = transparentMat;
        }

        private void Update()
        {
            if (!isInitialized) Initialize();
            FadeAlpha();
        }




        public void SetOccluding(bool occluding)
        {
            if (!isInitialized) Initialize();
            isOccluding = occluding;
        }

        private void FadeAlpha()
        {
            if (meshRenderer == null || transparentMaterial == null) return;

            float targetAlpha = isOccluding ? transparentAlpha : 1f;


            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);


            Color c = transparentMaterial.color;
            c.a = currentAlpha;
            transparentMaterial.color = c;


            if (currentAlpha < 1f && meshRenderer.sharedMaterial != transparentMaterial) {
                meshRenderer.material = transparentMaterial;
            }
            else if (currentAlpha >= 1f && meshRenderer.sharedMaterial != originalMaterial) {
                meshRenderer.sharedMaterial = originalMaterial;
            }
        }


        private void OnDisable()
        {
            if (meshRenderer != null && originalMaterial != null) {
                meshRenderer.sharedMaterial = originalMaterial;
            }
        }

        private void OnValidate()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
        }
    }
}
```

## File: Features/Camera/WallOccluder.cs.meta
```
fileFormatVersion: 2
guid: ef61ab698c979384f80e6ffdbb85087e
```

## File: Features/Camera/WallOcclusionManager.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCamera
{
    public class WallOcclusionManager : MonoBehaviour
    {
        #region Singleton
        private static WallOcclusionManager _instance;
        public static WallOcclusionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<WallOcclusionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        #endregion

        [Header("References")]
        [Tooltip("Main camera (isometric)")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("Player transform (center of body)")]
        [SerializeField] private Transform player;

        [Header("Occlusion Settings")]
        [Tooltip("Layer mask for occluding walls")]
        [SerializeField] private LayerMask occluderLayerMask = -1;

        [Tooltip("How often to check for occlusion (seconds)")]
        [SerializeField] private float checkInterval = 0.05f;

        [Tooltip("Height from ground to cast ray (player center)")]
        [SerializeField] private float raycastHeight = 1.5f;

        [Tooltip("Additional distance buffer for raycast")]
        [SerializeField] private float raycastDistanceBuffer = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugDrawRays = false;

        private HashSet<WallOccluder> currentlyOccluding = new HashSet<WallOccluder>();
        private float lastCheckTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null) mainCamera = Camera.main;
            if (player == null) {
                var playerObj = GameObject.Find("Player");
                if (playerObj != null) player = playerObj.transform;
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null || player == null) return;

            if (Time.time - lastCheckTime >= checkInterval)
            {
                CheckOcclusion();
                lastCheckTime = Time.time;
            }
        }

        private void CheckOcclusion()
        {
            if (mainCamera == null || player == null) return;

            Vector3 playerCenter = player.position + Vector3.up * raycastHeight;
            Vector3 camPos = mainCamera.transform.position;

            Vector3 horizontalDir = camPos - playerCenter;
            horizontalDir.y = 0f;
            horizontalDir.Normalize();

            float playerToCamDist = Vector3.Distance(new Vector3(camPos.x, 0, camPos.z), new Vector3(playerCenter.x, 0, playerCenter.z));
            float baseDistance = playerToCamDist + raycastDistanceBuffer;

            HashSet<WallOccluder> newOccluding = new HashSet<WallOccluder>();


            int rayCount = 5;
            float maxFanAngle = 12f;

            for (int i = 0; i < rayCount; i++)
            {
                float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
                float angle = Mathf.Lerp(-maxFanAngle, maxFanAngle, t);
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 rayDir = rot * horizontalDir;

                Vector3 rayOrigin = playerCenter;

                if (debugDrawRays)
                {
                    Debug.DrawRay(rayOrigin, rayDir * baseDistance, Color.red, checkInterval);
                }

                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, baseDistance, occluderLayerMask.value, QueryTriggerInteraction.Collide);

                foreach (var hit in hits)
                {
                    var occluder = hit.collider.GetComponent<WallOccluder>();
                    if (occluder != null)
                    {

                        if (hit.distance < playerToCamDist + 0.5f)
                        {
                            newOccluding.Add(occluder);
                        }
                    }
                }
            }

            UpdateOcclusionState(newOccluding);
        }

        private void UpdateOcclusionState(HashSet<WallOccluder> newOccluding)
        {
            var toRemove = new List<WallOccluder>();
            foreach (var occluder in currentlyOccluding) {
                if (!newOccluding.Contains(occluder)) {
                    occluder.SetOccluding(false);
                    toRemove.Add(occluder);
                }
            }
            foreach (var o in toRemove) currentlyOccluding.Remove(o);

            foreach (var occluder in newOccluding) {
                if (!currentlyOccluding.Contains(occluder)) {
                    occluder.SetOccluding(true);
                    currentlyOccluding.Add(occluder);
                }
            }
        }

        public void ForceAllTransparent(bool transparent)
        {
            var allOccluders = FindObjectsByType<WallOccluder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var o in allOccluders) {
                o.SetOccluding(transparent);
            }
        }

        public void RefreshNow()
        {
            lastCheckTime = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDrawRays || mainCamera == null || player == null) return;

            Vector3 playerCenter = player.position + Vector3.up * raycastHeight;
            Vector3 camPos = mainCamera.transform.position;

            Vector3 horizontalDir = camPos - playerCenter;
            horizontalDir.y = 0f;
            horizontalDir.Normalize();

            float playerToCamDist = Vector3.Distance(new Vector3(camPos.x, 0, camPos.z), new Vector3(playerCenter.x, 0, playerCenter.z));
            float distance = playerToCamDist + raycastDistanceBuffer;


            int rayCount = 5;
            float maxFanAngle = 12f;

            for (int i = 0; i < rayCount; i++)
            {
                float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
                float angle = Mathf.Lerp(-maxFanAngle, maxFanAngle, t);
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 rayDir = rot * horizontalDir;

                Gizmos.color = Color.red;
                Gizmos.DrawRay(playerCenter, rayDir * distance);
            }
        }
    }
}
```

## File: Features/Camera/WallOcclusionManager.cs.meta
```
fileFormatVersion: 2
guid: 7ab45a23ae89e3d498099bd9e4bd8335
```

## File: Features/Interaction/BedInteractable.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{



    public class BedInteractable : MonoBehaviour, IInteractable
    {

        [SerializeField] private int sleepHealAmount = 100;

        public void Interact(GameObject interactor)
        {
            if (TimeManager.Instance == null)
            {
                Debug.LogWarning("TimeManager tidak ditemukan di scene!");
                return;
            }


            if (TimeManager.Instance.currentPhase == TimeManager.DayPhase.Day)
            {
                Debug.Log("Masih siang, belum bisa tidur!");
                return;
            }


            PlayerStats stats = interactor.GetComponent<PlayerStats>();
            if (stats != null)
                stats.Heal(sleepHealAmount);


            TimeManager.Instance.AdvanceToNextDay();
        }
    }
}
```

## File: Features/Interaction/BedInteractable.cs.meta
```
fileFormatVersion: 2
guid: 214705c60fb9b2a46b7a616670012cf1
```

## File: Features/Interaction/GenericFurnitureInteractable.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{





    public class GenericFurnitureInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Jenis furniture untuk logging (Kursi, Peti, Meja, dll).")]
        [SerializeField] private string furnitureType = "Furniture";

        [Tooltip("Pesan custom saat di-interact (kosong = default).")]
        [SerializeField] private string customInteractMessage;

        public void Interact(GameObject interactor)
        {
            string message = !string.IsNullOrEmpty(customInteractMessage)
                ? customInteractMessage
                : $"Berinteraksi dengan {furnitureType} ({gameObject.name})";

            Debug.Log(message);






        }


        public void SetFurnitureType(string type)
        {
            furnitureType = type;
        }

        public void SetCustomMessage(string message)
        {
            customInteractMessage = message;
        }
    }
}
```

## File: Features/Interaction/GenericFurnitureInteractable.cs.meta
```
fileFormatVersion: 2
guid: 8477bc2d1896a554a8d1c62ff987e08e
```

## File: Features/Interaction/Highlightable.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{





    public class Highlightable : MonoBehaviour
    {
        [Tooltip("Material highlight (emissive) yang dipakai saat objek di-hover.")]
        [SerializeField] private Material highlightMaterial;

        private Renderer[] cachedRenderers;
        private Material[][] originalMaterials;

        public void SetHighlightMaterial(Material mat)
        {
            highlightMaterial = mat;
        }

        public void SetHighlight(bool on)
        {
            if (highlightMaterial == null)
                return;

            CacheRenderers();

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer r = cachedRenderers[i];
                if (r == null)
                    continue;

                if (on)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < mats.Length; m++)
                        mats[m] = highlightMaterial;
                    r.sharedMaterials = mats;
                }
                else
                {
                    if (originalMaterials != null && i < originalMaterials.Length)
                    {
                        Material[] mats = new Material[originalMaterials[i].Length];
                        for (int m = 0; m < mats.Length; m++)
                            mats[m] = originalMaterials[i][m];
                        r.sharedMaterials = mats;
                    }
                }
            }
        }

        private void CacheRenderers()
        {
            if (cachedRenderers != null && cachedRenderers.Length > 0)
                return;

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[cachedRenderers.Length][];
            for (int i = 0; i < cachedRenderers.Length; i++)
                originalMaterials[i] = cachedRenderers[i].sharedMaterials;
        }
    }
}
```

## File: Features/Interaction/Highlightable.cs.meta
```
fileFormatVersion: 2
guid: f5c46b36a1557054c99cedf6b80a0734
```

## File: Features/Interaction/HoverLabelController.cs
```csharp
using FeaturesInteraction;
using UnityEngine;








public class HoverLabelController : MonoBehaviour
{
    [Tooltip("Interactor pemain yang menjadi sumber target hover.")]
    [SerializeField] private PlayerInteractor interactor;

    private string lastShownName;
    private bool isShowing;

    private GameObject lastTarget;
    private Highlightable lastHighlight;

    private void OnEnable()
    {
        lastShownName = null;
        isShowing = false;
        lastTarget = null;
        lastHighlight = null;
    }

    private void OnDisable()
    {
        ClearHighlight();
        HideIfShowing();
    }

    private void Update()
    {
        if (interactor == null)
        {
            ClearAll();
            return;
        }


        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
        {
            ClearAll();
            return;
        }

        GameObject target = interactor.CurrentTarget;
        if (target == null)
        {
            ClearAll();
            return;
        }


        if (target != lastTarget)
        {
            ClearHighlight();
            lastTarget = target;
            lastHighlight = target.GetComponent<Highlightable>();
            if (lastHighlight != null)
                lastHighlight.SetHighlight(true);
        }

        WorldLabel label = target.GetComponent<WorldLabel>();
        string displayName = label != null ? label.GetDisplayName() : target.name;

        if (string.IsNullOrEmpty(displayName))
        {
            ClearAll();
            return;
        }

        Show(displayName);
    }

    private void Show(string name)
    {

        if (isShowing && lastShownName == name)
            return;

        lastShownName = name;
        isShowing = true;

        if (ItemDisplayUI.Instance != null)
        {
            ItemDisplayUI.Instance.ShowWorldHover(name);
            ItemDisplayUI.Instance.ShowInteractPrompt(name);
        }
    }

    private void ClearAll()
    {
        ClearHighlight();
        HideIfShowing();
    }

    private void ClearHighlight()
    {
        if (lastHighlight != null)
        {
            lastHighlight.SetHighlight(false);
            lastHighlight = null;
        }
        lastTarget = null;
    }

    private void HideIfShowing()
    {
        if (!isShowing)
            return;

        isShowing = false;
        lastShownName = null;

        if (ItemDisplayUI.Instance != null)
        {
            ItemDisplayUI.Instance.HideWorldHover();
            ItemDisplayUI.Instance.HideInteractPrompt();
        }
    }
}
```

## File: Features/Interaction/HoverLabelController.cs.meta
```
fileFormatVersion: 2
guid: 6d06a462f254a3f47ada1fe38470c745
```

## File: Features/Interaction/IInteractable.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);
    }
}
```

## File: Features/Interaction/IInteractable.cs.meta
```
fileFormatVersion: 2
guid: df6f9613326bb7f42b133baf736b3a57
```

## File: Features/Interaction/InteractionZone.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{
    public class InteractionZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [Tooltip("Nama zona (mis. \"Bedroom\", \"Kitchen\")")]
        [SerializeField] private string zoneName = "Zone";

        [Tooltip("Collider trigger yang mendefinisikan batas zona")]
        [SerializeField] private Collider zoneCollider;

        public string ZoneName => zoneName;

        public bool ContainsPoint(Vector3 point)
        {
            if (zoneCollider == null) return false;
            return zoneCollider.bounds.Contains(point);
        }

        private void OnValidate()
        {
            if (zoneCollider == null) zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null) zoneCollider.isTrigger = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (zoneCollider == null) return;

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
            Gizmos.DrawCube(zoneCollider.bounds.center, zoneCollider.bounds.size);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
        }
    }
}
```

## File: Features/Interaction/InteractionZone.cs.meta
```
fileFormatVersion: 2
guid: 218b50136a8285a4ca5ff4487846160c
```

## File: Features/Interaction/PlayerInteractor.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Deteksi Interaksi")]

        [SerializeField] private float interactRadius = 2.5f;

        [Header("Layer Interactable")]
        public LayerMask interactableLayer = ~0;

        private IInteractable currentInteractable;

        void Update()
        {
            currentInteractable = FindClosestInteractable();
        }

        private InteractionZone currentZone;

        private IInteractable FindClosestInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, interactableLayer.value);

            IInteractable best = null;
            float bestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable == null) continue;


                if (!IsInSameZone(hit.transform))
                    continue;

                float dist = (hit.transform.position - transform.position).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
            }

            return best;
        }

        private bool IsInSameZone(Transform target)
        {

            var targetZone = target.GetComponentInParent<InteractionZone>();
            if (targetZone == null) return true;


            return currentZone == targetZone;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<InteractionZone>(out var zone))
                currentZone = zone;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<InteractionZone>(out var zone) && currentZone == zone)
                currentZone = null;
        }

        public void OnInteractInput()
        {
            if (currentInteractable != null)
                currentInteractable.Interact(gameObject);
        }


        public GameObject CurrentTarget
        {
            get
            {
                if (currentInteractable is MonoBehaviour mb)
                    return mb.gameObject;
                return null;
            }
        }
    }
}
```

## File: Features/Interaction/PlayerInteractor.cs.meta
```
fileFormatVersion: 2
guid: 25d535644e7085146969390a49aa021d
```

## File: Features/Interaction/StorageInteractable.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FeaturesInteraction
{

    [System.Serializable]
    public struct LootDrop
    {
        public ItemData item;
        public int minAmount;
        public int maxAmount;
        [Range(0f, 100f)] public float dropChance;
    }



    [RequireComponent(typeof(InventoryComponent))]
    public class StorageInteractable : MonoBehaviour, IInteractable
    {
        public List<LootDrop> lootTable = new List<LootDrop>();
        public bool generateLootOnStart = false;

        private InventoryComponent inventory;

        private void Awake()
        {
            inventory = GetComponent<InventoryComponent>();

            if (generateLootOnStart)
                GenerateLoot();
        }

        private void GenerateLoot()
        {
            if (inventory == null)
                return;

            foreach (LootDrop drop in lootTable)
            {
                if (drop.item == null)
                    continue;


                float roll = Random.Range(0f, 100f);
                if (roll > drop.dropChance)
                    continue;

                int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
                if (amount > 0)
                    inventory.AddItem(drop.item, amount);
            }
        }

        public void Interact(GameObject interactor)
        {
            Debug.Log("Storage dibuka oleh " + interactor.name);


            if (InventoryManagerUI.Instance != null)
                InventoryManagerUI.Instance.OpenStorageUI(GetComponent<InventoryComponent>());
        }
    }
}
```

## File: Features/Interaction/StorageInteractable.cs.meta
```
fileFormatVersion: 2
guid: 2c4da961f0279da448d233572387e0de
```

## File: Features/Interaction/TrophyCabinetInteractable.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{



    [RequireComponent(typeof(InventoryComponent))]
    public class TrophyCabinetInteractable : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject interactor)
        {

            if (TrophySystemManager.Instance != null)
                TrophySystemManager.Instance.EnterTrophyMode();





            if (InventoryManagerUI.Instance != null)
            {
                InventoryComponent cabinetInv = GetComponent<InventoryComponent>();
                InventoryComponent rackInv = TrophySystemManager.Instance != null
                    ? TrophySystemManager.Instance.RackInventory
                    : null;
                InventoryManagerUI.Instance.OpenTrophyCabinetUI(cabinetInv, rackInv);
            }
        }
    }
}
```

## File: Features/Interaction/TrophyCabinetInteractable.cs.meta
```
fileFormatVersion: 2
guid: 7050ca9e88d70a545b17a6d09d531129
```

## File: Features/Interaction/WardrobeInteractable.cs
```csharp
using FeaturesWardrobe;
using UnityEngine;

namespace FeaturesInteraction
{
    public class WardrobeInteractable : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject interactor)
        {
            if (WardrobeManager.Instance != null)
                WardrobeManager.Instance.EnterWardrobeMode();
            else
                Debug.LogWarning("[WardrobeInteractable] WardrobeManager.Instance not found!");
        }
    }
}
```

## File: Features/Interaction/WardrobeInteractable.cs.meta
```
fileFormatVersion: 2
guid: bfbd32c29d915a24d9c6c2a3158bc1a3
```

## File: Features/Interaction/WorldLabel.cs
```csharp
using UnityEngine;

namespace FeaturesInteraction
{





    public class WorldLabel : MonoBehaviour
    {
        [Tooltip("Nama ramah yang ditampilkan saat hover (kosong = nama GameObject).")]
        public string displayName;

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(displayName))
                return displayName;
            return gameObject != null ? gameObject.name : "";
        }
    }
}
```

## File: Features/Interaction/WorldLabel.cs.meta
```
fileFormatVersion: 2
guid: ece06dd238bd35446a7735224afd633f
```

## File: Features/Inventory/Data/Carrot_Clean.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Carrot_Clean
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Carrot (Clean)
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 20
  type: 0
  healAmount: 0
  foodCategory: 1
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Carrot_Clean.asset.meta
```
fileFormatVersion: 2
guid: 5bd3aca182d0923468b2c603d86f737a
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Carrot_Dirty.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Carrot_Dirty
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Carrot (Dirty)
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 20
  type: 0
  healAmount: 0
  foodCategory: 1
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Carrot_Dirty.asset.meta
```
fileFormatVersion: 2
guid: b3319f908a899664698f67f253f2c66d
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Cooked_Rice.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Cooked_Rice
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Cooked Rice
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 20
  type: 1
  healAmount: 0
  foodCategory: 5
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Cooked_Rice.asset.meta
```
fileFormatVersion: 2
guid: 205a11daf64e36e48b2fe81c0fadb624
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Cooked_Veggies.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Cooked_Veggies
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Cooked Veggies
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 20
  type: 1
  healAmount: 0
  foodCategory: 5
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Cooked_Veggies.asset.meta
```
fileFormatVersion: 2
guid: 1bdf5b57a2726b9418733071267703ec
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/DummySword.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: DummySword
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Dummy Sword
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 1
  type: 2
  healAmount: 0
  equipPrefab: {fileID: 8479986625188135542, guid: 6b2794aaa34bd4e40a29b00fdc58afca, type: 3}
```

## File: Features/Inventory/Data/DummySword.asset.meta
```
fileFormatVersion: 2
guid: 2da20757d972f5e4387c2eab16885a48
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Potion.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Potion
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Potion
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 10
  type: 1
  healAmount: 10
```

## File: Features/Inventory/Data/Potion.asset.meta
```
fileFormatVersion: 2
guid: 89d4583b3bdd74240a6230b70b90333c
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Rice_Raw.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Rice_Raw
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Rice (Raw)
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 20
  type: 0
  healAmount: 0
  foodCategory: 4
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Rice_Raw.asset.meta
```
fileFormatVersion: 2
guid: 81667949593bee043a9439fc507ec44b
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Seed.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Seed
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Carrot Seed
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 99
  type: 0
  healAmount: 0
  equipPrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Seed.asset.meta
```
fileFormatVersion: 2
guid: 79c80a270641dfc4dbb61b085d9fe053
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Stone.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Stone
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Stone
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 99
  type: 0
  healAmount: 0
  equipPrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Stone.asset.meta
```
fileFormatVersion: 2
guid: adf20e44344ed714c9a7a49a08fd25a1
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/TrophyCapsule.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: TrophyCapsule
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Trophy Capsule
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 1
  type: 3
  healAmount: 0
  foodCategory: 0
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 288256409107858469, guid: 57cffcbc5f9274e49b7d878cb7c71119, type: 3}
```

## File: Features/Inventory/Data/TrophyCapsule.asset.meta
```
fileFormatVersion: 2
guid: 4490e5ccbab3e5a468a7238955f93461
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/TrophyCube.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: TrophyCube
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Trophy Cube
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 1
  type: 3
  healAmount: 0
  foodCategory: 0
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 2246817352462402479, guid: 9cd3bbab02efa1540b57f38f64b8ebe8, type: 3}
```

## File: Features/Inventory/Data/TrophyCube.asset.meta
```
fileFormatVersion: 2
guid: 7d175609290fb234aa2889ea1164399b
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/TrophySphere.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: TrophySphere
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Trophy Sphere
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 1
  type: 3
  healAmount: 0
  foodCategory: 0
  equipPrefab: {fileID: 0}
  placeablePrefab: {fileID: 767020534589880245, guid: e0e65c935ee55bb4ba86c9c1fb167cea, type: 3}
```

## File: Features/Inventory/Data/TrophySphere.asset.meta
```
fileFormatVersion: 2
guid: 4ff0e5cbd3cebbd4bb9fb17d462820a4
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/Data/Wood.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ae5c125112b5be549a05d6bb0b7bba92, type: 3}
  m_Name: Wood
  m_EditorClassIdentifier: Assembly-CSharp::ItemData
  itemName: Wood
  itemIcon: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  maxStack: 99
  type: 0
  healAmount: 0
  equipPrefab: {fileID: 0}
```

## File: Features/Inventory/Data/Wood.asset.meta
```
fileFormatVersion: 2
guid: 40b6d60e0b6e7174ab45022360956140
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/UI/DraggableItem.cs
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform rectTransform;
    public Image image;
    public Transform parentAfterDrag;

    private bool dropped;

    public InventorySlotUI OriginSlot { get; private set; }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OriginSlot = GetComponentInParent<InventorySlotUI>();
        parentAfterDrag = transform.parent;
        dropped = false;


        transform.SetParent(transform.root, true);
        transform.SetAsLastSibling();


        if (image != null)
            image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (image != null)
            image.raycastTarget = true;



        if (!dropped)
        {
            if (TryHybridWorldDrop())
                dropped = true;
        }

        if (dropped)
        {

            Destroy(gameObject);
        }
        else if (parentAfterDrag != null)
        {

            transform.SetParent(parentAfterDrag, true);
            CenterInSlot(rectTransform, parentAfterDrag as RectTransform);
        }
    }






    private bool TryHybridWorldDrop()
    {
        if (TrophySystemManager.Instance == null || !TrophySystemManager.Instance.IsInTrophyMode)
            return false;

        Camera cam = TrophySystemManager.Instance.TrophyFirstPersonCamera;
        if (cam == null || Mouse.current == null || OriginSlot == null)
            return false;

        ItemData item = OriginSlot.BoundSlot != null ? OriginSlot.BoundSlot.item : null;
        if (item == null || item.placeablePrefab == null)
            return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, LayerMask.GetMask("SnapPoint")))
            return false;

        TrophySnapPoint snap = hit.collider != null ? hit.collider.GetComponent<TrophySnapPoint>() : null;
        if (snap == null || snap.slotIndex < 0)
            return false;

        InventoryComponent rack = TrophySystemManager.Instance.RackInventory;
        if (rack == null || OriginSlot.ownerInventory == null)
            return false;



        OriginSlot.ownerInventory.MoveItemToSlot(OriginSlot.SlotIndex, rack, snap.slotIndex);
        return true;
    }


    public void MarkDropped()
    {
        dropped = true;
    }

    private static void CenterInSlot(RectTransform item, RectTransform slot)
    {
        if (item == null || slot == null) return;
        item.anchorMin = Vector2.zero;
        item.anchorMax = Vector2.one;
        item.offsetMin = Vector2.zero;
        item.offsetMax = Vector2.zero;
        item.localScale = Vector3.one;
    }
}
```

## File: Features/Inventory/UI/DraggableItem.cs.meta
```
fileFormatVersion: 2
guid: 7ecafe03be06d9e4a81f95aeb2625104
```

## File: Features/Inventory/UI/InventoryManagerUI.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using FeaturesWardrobe;
using UnityEngine.Serialization;

public class InventoryManagerUI : MonoBehaviour
{
    public static InventoryManagerUI Instance { get; private set; }

    [Header("Data Inventori")]
    public InventoryComponent playerInventory;


    [FormerlySerializedAs("currentChestInventory")]
    public InventoryComponent currentStorageInventory;

    [Header("Auto-Close Storage")]


    public float maxInteractDistance = 7f;

    private Transform playerTransform;

    [Header("Referensi UI")]
    public InventorySlotUI slotPrefab;
    public RectTransform playerSlotsContainer;

    [FormerlySerializedAs("chestSlotsContainer")]
    public RectTransform storageSlotsContainer;
    public GameObject playerPanel;

    [FormerlySerializedAs("chestPanel")]
    public GameObject storagePanel;
    public Transform playerHotbarContainer;

    private readonly List<InventorySlotUI> playerSlotUIs = new List<InventorySlotUI>();
    private readonly List<InventorySlotUI> storageSlotUIs = new List<InventorySlotUI>();
    private bool isPlayerOpen;


    private InventoryComponent displayLeftInventory;


    private bool isTrophyCabinetMode;
    private InventoryComponent cabinetInventory;

    void Awake()
    {
        Instance = this;


        CloseAllUI();
    }

    void Start()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<InventoryComponent>();

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += OnInventoryChanged;
            playerInventory.OnHotbarSelected += OnHotbarSelected;
            playerTransform = playerInventory.transform;
        }

        displayLeftInventory = playerInventory;
        BuildPlayerSlots();
        BuildSlots(storageSlotsContainer, storageSlotUIs, null);
        UpdateUI();


        if (playerInventory != null)
            playerInventory.SelectHotbarSlot(0);
    }

    void Update()
    {

        Transform anchor = currentStorageInventory != null ? currentStorageInventory.transform : null;
        if (storagePanel != null && storagePanel.activeSelf && anchor != null)
        {
            if (playerTransform == null)
                return;

            float distance = Vector3.Distance(playerTransform.position, anchor.position);
            if (distance > maxInteractDistance)
                CloseAllUI();
        }
    }

    void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
            playerInventory.OnHotbarSelected -= OnHotbarSelected;
        }
        UnsubscribeRight();
        UnsubscribeCabinet();

        if (Instance == this)
            Instance = null;
    }

    private void OnHotbarSelected(int index)
    {

        if (isTrophyCabinetMode)
            return;

        int count = Mathf.Min(playerSlotUIs.Count, 4);
        for (int i = 0; i < count; i++)
            playerSlotUIs[i].SetHighlight(i == index);

        if (playerInventory != null && index >= 0 && index < playerInventory.slots.Count)
        {
            InventorySlot slot = playerInventory.slots[index];
            if (slot != null && !slot.IsEmpty && ItemDisplayUI.Instance != null)
                ItemDisplayUI.Instance.ShowHotbarPopup(slot.item.itemName);
        }
    }

    private void OnInventoryChanged()
    {
        UpdateUI();
    }


    public void TogglePlayerInventory()
    {

        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
            return;


        if (WardrobeManager.Instance != null && WardrobeManager.Instance.IsInWardrobeMode)
            return;

        if (currentStorageInventory != null)
        {
            CloseAllUI();
            return;
        }

        isPlayerOpen = !isPlayerOpen;

        if (playerPanel != null)
            playerPanel.SetActive(isPlayerOpen);

        SetCursorFree(isPlayerOpen);
    }




    public void OpenStorageUI(InventoryComponent storageInv)
    {
        if (storageInv == null) return;


        if (isTrophyCabinetMode)
            CloseAllUI();

        UnsubscribeRight();
        currentStorageInventory = storageInv;
        currentStorageInventory.OnInventoryChanged += OnInventoryChanged;

        displayLeftInventory = playerInventory;
        BuildSlots(storageSlotsContainer, storageSlotUIs, storageInv);

        isPlayerOpen = true;
        if (playerPanel != null) playerPanel.SetActive(true);
        if (storagePanel != null) storagePanel.SetActive(true);

        SetCursorFree(true);
        UpdateUI();
    }







    public void OpenTrophyCabinetUI(InventoryComponent cabinetInv, InventoryComponent rackInv)
    {
        if (cabinetInv == null)
        {
            Debug.LogWarning("OpenTrophyCabinetUI: cabinetInv null.");
            return;
        }


        if (isTrophyCabinetMode)
            CloseAllUI();

        UnsubscribeRight();
        UnsubscribeCabinet();


        currentStorageInventory = rackInv;
        if (currentStorageInventory != null)
            currentStorageInventory.OnInventoryChanged += OnInventoryChanged;

        cabinetInventory = cabinetInv;
        if (cabinetInventory != null)
            cabinetInventory.OnInventoryChanged += OnInventoryChanged;

        displayLeftInventory = cabinetInv;
        isTrophyCabinetMode = true;


        BuildSlots(playerSlotsContainer, playerSlotUIs, cabinetInv);
        BuildSlots(storageSlotsContainer, storageSlotUIs, rackInv);


        if (playerHotbarContainer != null)
            playerHotbarContainer.gameObject.SetActive(false);

        isPlayerOpen = true;
        if (playerPanel != null) playerPanel.SetActive(true);
        if (storagePanel != null) storagePanel.SetActive(true);

        SetCursorFree(true);
        UpdateUI();
    }





    public void CloseAllUI()
    {
        isPlayerOpen = false;

        if (playerPanel != null) playerPanel.SetActive(false);
        if (storagePanel != null) storagePanel.SetActive(false);

        UnsubscribeRight();
        UnsubscribeCabinet();


        if (isTrophyCabinetMode)
        {
            isTrophyCabinetMode = false;

            if (playerHotbarContainer != null)
                playerHotbarContainer.gameObject.SetActive(true);

            BuildPlayerSlots();
            displayLeftInventory = playerInventory;
        }

        SetCursorFree(false);
    }

    private void SetCursorFree(bool free)
    {
        Cursor.visible = free;
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void BuildPlayerSlots()
    {
        ClearContainer(playerHotbarContainer);
        ClearContainer(playerSlotsContainer);
        playerSlotUIs.Clear();

        if (playerInventory == null) return;

        int count = Mathf.Max(0, playerInventory.maxCapacity);
        for (int i = 0; i < count; i++)
        {

            Transform parent = i < 4 ? playerHotbarContainer : (Transform)playerSlotsContainer;
            if (parent == null) continue;

            InventorySlotUI slot = Instantiate(slotPrefab, parent);
            slot.Init(this, i, playerInventory);
            slot.BoundSlot = i < playerInventory.slots.Count ? playerInventory.slots[i] : new InventorySlot();
            playerSlotUIs.Add(slot);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            DestroyImmediate(container.GetChild(i).gameObject);
    }

    private void BuildSlots(RectTransform container, List<InventorySlotUI> list, InventoryComponent inventory)
    {
        if (container == null) return;


        for (int i = container.childCount - 1; i >= 0; i--)
            DestroyImmediate(container.GetChild(i).gameObject);
        list.Clear();

        if (inventory == null) return;

        int count = Mathf.Max(0, inventory.maxCapacity);
        for (int i = 0; i < count; i++)
        {
            InventorySlotUI slot = Instantiate(slotPrefab, container);
            slot.Init(this, i, inventory);
            slot.BoundSlot = i < inventory.slots.Count ? inventory.slots[i] : new InventorySlot();
            list.Add(slot);
        }
    }

    public void SwapSlots(InventoryComponent owner, int sourceIndex, int destinationIndex)
    {
        if (owner == null) return;
        owner.SwapSlots(sourceIndex, destinationIndex);
    }

    public void UpdateUI()
    {
        RefreshPanel(playerSlotUIs, displayLeftInventory);
        RefreshPanel(storageSlotUIs, currentStorageInventory);
    }

    private void RefreshPanel(List<InventorySlotUI> list, InventoryComponent inventory)
    {
        if (inventory == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            InventorySlot data = i < inventory.slots.Count ? inventory.slots[i] : null;
            list[i].SetSlotVisual(data);
        }
    }

    private void UnsubscribeRight()
    {
        if (currentStorageInventory != null)
        {
            currentStorageInventory.OnInventoryChanged -= OnInventoryChanged;
            currentStorageInventory = null;
        }
    }

    private void UnsubscribeCabinet()
    {
        if (cabinetInventory != null)
        {
            cabinetInventory.OnInventoryChanged -= OnInventoryChanged;
            cabinetInventory = null;
        }
    }
}
```

## File: Features/Inventory/UI/InventoryManagerUI.cs.meta
```
fileFormatVersion: 2
guid: 93386187329ed5d4385ac3498fe3a517
```

## File: Features/Inventory/UI/InventorySlotUI.cs
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public Text quantityText;
    public Image backgroundImage;

    [Tooltip("Bar progress per-slot (stasiun dapur). Naik dari bawah ke atas di pinggir slot. Dibuat otomatis bila kosong.")]
    [SerializeField] private Image progressFill;


    public static bool debugLogProgress;

    private InventoryManagerUI manager;
    private Color defaultColor;


    public InventoryComponent ownerInventory;

    public int SlotIndex { get; private set; }
    public InventorySlot BoundSlot { get; set; }


    private KitchenStation station;
    private bool stationChecked;


    private GameObject progressContainerGO;
    private Image progressTrack;
    private Text progressText;
    private Color fillDefaultColor = new Color(1f, 0.78f, 0.1f, 0.9f);
    private bool flashing;
    private float flashEndTime;
    private float lastLoggedFill = -1f;
    private RectTransform fillRT;
    private float baseFillHeight = -1f;
    private float fillWidth = 9f;

    public bool HasItemIcon
    {
        get { return iconImage != null && iconImage.gameObject.activeSelf; }
    }

    public void Init(InventoryManagerUI inventoryManager, int index, InventoryComponent owner)
    {
        manager = inventoryManager;
        SlotIndex = index;
        ownerInventory = owner;

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
            defaultColor = backgroundImage.color;


        station = owner != null ? owner.GetComponent<KitchenStation>() : null;
        stationChecked = true;

        BuildProgressIndicator();
        SetProgressVisible(false);

        if (station != null)
            station.OnProcessCompleted += OnSlotProcessCompleted;
    }

    public void SetHighlight(bool isSelected)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = isSelected ? Color.yellow : defaultColor;
    }

    public void SetSlotVisual(InventorySlot slot)
    {



        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (progressContainerGO != null && transform.GetChild(i).gameObject == progressContainerGO)
                continue;
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        iconImage = null;
        quantityText = null;

        flashing = false;
        SetProgressVisible(false);

        if (slot == null || slot.IsEmpty)
            return;


        GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(8, 8);
        iconRT.offsetMax = new Vector2(-8, -8);

        Image img = iconGO.AddComponent<Image>();
        img.sprite = slot.item.itemIcon;
        img.preserveAspect = true;
        img.raycastTarget = true;
        iconGO.AddComponent<DraggableItem>();
        iconImage = img;


        GameObject qtyGO = new GameObject("Quantity", typeof(RectTransform));
        qtyGO.transform.SetParent(transform, false);
        RectTransform qtyRT = qtyGO.GetComponent<RectTransform>();

        qtyRT.anchorMin = new Vector2(1, 0);
        qtyRT.anchorMax = new Vector2(1, 0);
        qtyRT.pivot = new Vector2(1, 1);
        qtyRT.anchoredPosition = new Vector2(-2, 2);
        qtyRT.sizeDelta = new Vector2(22, 16);

        Text qText = qtyGO.AddComponent<Text>();
        qText.font = GetFont();
        qText.fontSize = 14;
        qText.color = Color.white;
        qText.alignment = TextAnchor.MiddleRight;
        qText.raycastTarget = false;
        qText.text = slot.quantity.ToString();
        quantityText = qText;
    }

    private static Font _font;

    private static Font GetFont()
    {
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _font;
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem dragItem = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableItem>()
            : null;
        if (dragItem == null) return;

        InventorySlotUI originSlot = dragItem.OriginSlot;
        if (originSlot == null || manager == null || ownerInventory == null)
            return;


        if (originSlot == this)
            return;




        originSlot.ownerInventory.MoveItemToSlot(originSlot.SlotIndex, ownerInventory, SlotIndex);


        dragItem.MarkDropped();
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        if (eventData.button != PointerEventData.InputButton.Left || eventData.clickCount != 2)
            return;


        if (ownerInventory != null && manager != null && ownerInventory == manager.playerInventory)
        {
            ownerInventory.UseItem(SlotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ownerInventory == null)
            return;

        InventorySlot data = SlotIndex >= 0 && SlotIndex < ownerInventory.slots.Count
            ? ownerInventory.slots[SlotIndex]
            : null;
        if (data == null || data.IsEmpty || ItemDisplayUI.Instance == null)
            return;

        ItemDisplayUI.Instance.ShowHover(data.item.itemName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemDisplayUI.Instance != null)
            ItemDisplayUI.Instance.HideHover();
    }






    private void BuildProgressIndicator()
    {
        if (progressContainerGO != null)
            return;

        GameObject container = new GameObject("ProgressIndicator", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        RectTransform containerRT = container.GetComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;


        GameObject trackGO = new GameObject("Track", typeof(RectTransform));
        trackGO.transform.SetParent(container.transform, false);
        RectTransform trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(1f, 0.06f);
        trackRT.anchorMax = new Vector2(1f, 0.94f);
        trackRT.pivot = new Vector2(0.5f, 0.5f);
        trackRT.offsetMin = new Vector2(-16f, 0f);
        trackRT.offsetMax = new Vector2(-4f, 0f);
        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0.03f, 0.03f, 0.03f, 0.85f);
        trackImg.raycastTarget = false;


        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(container.transform, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(1f, 0.06f);
        fillRT.anchorMax = new Vector2(1f, 0.06f);
        fillRT.pivot = new Vector2(0.5f, 0f);
        fillRT.anchoredPosition = new Vector2(-10f, 0f);
        fillRT.sizeDelta = new Vector2(fillWidth, 0f);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.type = Image.Type.Simple;
        fillImg.color = fillDefaultColor;
        fillImg.raycastTarget = false;


        GameObject pctGO = new GameObject("Percent", typeof(RectTransform));
        pctGO.transform.SetParent(container.transform, false);
        RectTransform pctRT = pctGO.GetComponent<RectTransform>();
        pctRT.anchorMin = new Vector2(0.5f, 1.02f);
        pctRT.anchorMax = new Vector2(0.5f, 1.02f);
        pctRT.pivot = new Vector2(0.5f, 0f);
        pctRT.anchoredPosition = Vector2.zero;
        pctRT.sizeDelta = new Vector2(64f, 20f);
        Text pctText = pctGO.AddComponent<Text>();
        pctText.font = GetFont();
        pctText.fontSize = 13;
        pctText.fontStyle = FontStyle.Bold;
        pctText.color = new Color(1f, 0.95f, 0.3f, 1f);
        pctText.alignment = TextAnchor.MiddleCenter;
        pctText.raycastTarget = false;
        pctText.text = "";

        progressContainerGO = container;
        progressTrack = trackImg;
        this.fillRT = fillRT;
        if (containerRT.rect.height > 0) { baseFillHeight = containerRT.rect.height * 0.88f; }
        progressFill = fillImg;
        progressText = pctText;
    }

    private void SetProgressVisible(bool visible)
    {
        if (progressContainerGO == null)
            return;

        bool wasActive = progressContainerGO.activeSelf;
        if (wasActive != visible)
            progressContainerGO.SetActive(visible);

        // Log HANYA saat ada transisi (hindari spam per-frame).
        if (wasActive != visible && station != null && progressFill != null && ownerInventory != null)
        {
            Debug.Log("[InventorySlotUI] progress " + (visible ? "AKTIF" : "HILANG") + " slot " + SlotIndex
                + " owner " + ownerInventory.gameObject.name + " fill=" + progressFill.fillAmount.ToString("F2"));
        }
    }

    private void Update()
    {
        if (!stationChecked)
        {
            station = ownerInventory != null ? ownerInventory.GetComponent<KitchenStation>() : null;
            stationChecked = true;
        }

        if (station == null || progressFill == null || progressContainerGO == null)
            return;


        if (flashing)
        {
            if (Time.time >= flashEndTime)
            {
                flashing = false;
                progressFill.color = fillDefaultColor;
                progressFill.fillAmount = 0f;
                SetProgressText("");
                SetProgressVisible(false);
            }
            else {
                if (fillRT != null && baseFillHeight > 0) {
                    fillRT.sizeDelta = new Vector2(fillWidth, baseFillHeight);
                }
                SetProgressText("100%");
            }
            return;
        }

        if (!station.IsProcessing(SlotIndex))
        {
            SetProgressVisible(false);
            return;
        }

        SetProgressVisible(true);
        float p = Mathf.Clamp01(station.GetSlotProgress(SlotIndex));
        if (fillRT != null) {
            if (baseFillHeight <= 0 && progressContainerGO != null) {
                RectTransform crt = progressContainerGO.GetComponent<RectTransform>();
                if (crt != null && crt.rect.height > 0) baseFillHeight = crt.rect.height * 0.88f;
            }
            if (baseFillHeight > 0) {
                fillRT.sizeDelta = new Vector2(fillWidth, baseFillHeight * p);
            }
        }
        SetProgressText(Mathf.RoundToInt(p * 100f) + "%");

        // Debug: bukti proses bertahap (log saat nilai berubah >= 1%).
        if (debugLogProgress && Mathf.Abs(p - lastLoggedFill) >= 0.01f)
        {
            lastLoggedFill = p;
            Debug.Log("[InventorySlotUI] debugsink slot " + SlotIndex + " owner " + ownerInventory.gameObject.name
                + " fill=" + p.ToString("F2"));
        }
        lastLoggedFill = p;
    }

    private void SetProgressText(string text)
    {
        if (progressText != null)
            progressText.text = text;
    }

    private void OnSlotProcessCompleted(int slotIndex)
    {
        if (slotIndex != SlotIndex || progressFill == null)
            return;


        flashing = true;
        flashEndTime = Time.time + 0.35f;
        progressFill.color = new Color(0.2f, 1f, 0.35f, 0.95f);
        if (fillRT != null && baseFillHeight > 0) {
            fillRT.sizeDelta = new Vector2(fillWidth, baseFillHeight);
        }
        SetProgressVisible(true);
    }

    private void OnDestroy()
    {
        if (station != null)
            station.OnProcessCompleted -= OnSlotProcessCompleted;
    }

    private void OnDisable()
    {

        if (ItemDisplayUI.Instance != null)
            ItemDisplayUI.Instance.HideHover();

        flashing = false;
        SetProgressVisible(false);
    }
}
```

## File: Features/Inventory/UI/InventorySlotUI.cs.meta
```
fileFormatVersion: 2
guid: 43acb35e8b65e474db73c0ffa2a93dff
```

## File: Features/Inventory/UI/ItemDisplayUI.cs
```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;









public class ItemDisplayUI : MonoBehaviour
{
    public static ItemDisplayUI Instance { get; private set; }

    public Text hotbarPopupText;
    public Text mouseTooltipText;



    public Text worldHoverText;


    public Text interactPromptText;



    [SerializeField] private Vector2 tooltipOffset = new Vector2(25f, 65f);

    private Coroutine hideHotbarCoroutine;
    private RectTransform tooltipBg;

    void Awake()
    {
        Instance = this;

        SanitizeText(hotbarPopupText);
        SanitizeText(mouseTooltipText);
        SanitizeText(worldHoverText);
        SanitizeText(interactPromptText);

        if (mouseTooltipText != null)
            BuildTooltipBackground();
    }

    void Update()
    {

        if (mouseTooltipText == null || !mouseTooltipText.gameObject.activeSelf)
            return;

        if (Mouse.current == null)
            return;



        RectTransform tooltipRect = mouseTooltipText.rectTransform;
        if (hotbarPopupText != null &&
            (mouseTooltipText == hotbarPopupText || tooltipRect == hotbarPopupText.rectTransform))
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
            return;




        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Mouse.current.position.ReadValue(),
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint))
            return;

        Vector2 target = localPoint + tooltipOffset;
        tooltipRect.anchoredPosition = target;


        if (tooltipBg != null)
            tooltipBg.anchoredPosition = target;
    }

    public void ShowHover(string itemName)
    {
        if (mouseTooltipText == null)
            return;

        mouseTooltipText.text = itemName;
        mouseTooltipText.gameObject.SetActive(true);
        if (tooltipBg != null)
            tooltipBg.gameObject.SetActive(true);
    }

    public void HideHover()
    {
        if (mouseTooltipText == null)
            return;

        mouseTooltipText.gameObject.SetActive(false);
        if (tooltipBg != null)
            tooltipBg.gameObject.SetActive(false);
    }


    public void ShowWorldHover(string name)
    {
        if (worldHoverText == null)
            return;

        worldHoverText.text = name;
        worldHoverText.gameObject.SetActive(true);
    }

    public void HideWorldHover()
    {
        if (worldHoverText == null)
            return;

        worldHoverText.text = "";
        worldHoverText.gameObject.SetActive(false);
    }

    // Prompt aksi "E — Nama" saat objek interaktif sedang di-hover.
    public void ShowInteractPrompt(string displayName)
    {
        if (interactPromptText == null)
            return;

        interactPromptText.text = "E — " + displayName;
        interactPromptText.gameObject.SetActive(true);
    }

    public void HideInteractPrompt()
    {
        if (interactPromptText == null)
            return;

        interactPromptText.text = "";
        interactPromptText.gameObject.SetActive(false);
    }

    public void ShowHotbarPopup(string itemName)
    {
        if (hotbarPopupText == null)
            return;

        hotbarPopupText.text = itemName;
        hotbarPopupText.gameObject.SetActive(true);

        if (hideHotbarCoroutine != null)
            StopCoroutine(hideHotbarCoroutine);

        hideHotbarCoroutine = StartCoroutine(HideHotbarRoutine());
    }

    private IEnumerator HideHotbarRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (hotbarPopupText != null)
        {
            hotbarPopupText.text = "";
            hotbarPopupText.gameObject.SetActive(false);
        }
        hideHotbarCoroutine = null;
    }

    private static void SanitizeText(Text text)
    {
        if (text == null)
            return;

        text.text = "";
        text.gameObject.SetActive(false);
    }

    // Bangun kotak gelap untuk tooltip sebagai sibling (indeks paling awal)
    // sehingga dirender di belakang teks tooltip dan ikut bergerak setiap frame.
    private void BuildTooltipBackground()
    {
        if (tooltipBg != null)
            return;

        RectTransform textRect = mouseTooltipText.rectTransform;
        Canvas canvas = textRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
            return;

        var bgGO = new GameObject("UI_MouseTooltipBG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasRect, false);


        bgGO.transform.SetAsFirstSibling();

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.85f);
        bgImg.sprite = null;
        bgImg.raycastTarget = false;
        bgImg.type = Image.Type.Simple;

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = textRect.anchorMin;
        bgRT.anchorMax = textRect.anchorMax;
        bgRT.pivot = textRect.pivot;
        bgRT.sizeDelta = textRect.sizeDelta + new Vector2(20f, 12f);

        tooltipBg = bgRT;
        bgGO.SetActive(false);
    }
}
```

## File: Features/Inventory/UI/ItemDisplayUI.cs.meta
```
fileFormatVersion: 2
guid: 0c194b9852a66624394c2a04e83a8ef3
```

## File: Features/Inventory/Data.meta
```
fileFormatVersion: 2
guid: e315c974fae658c40aab1647f5619bf4
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory/InventoryComponent.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int maxCapacity;



    [Tooltip("Tidak mengizinkan item tipe Trophy masuk ke inventory ini (Contoh: inventory Player).")]
    [SerializeField] private bool blockTrophyItems;



    [Tooltip("Pembatas kategori makanan (FoodCategory). Kosong = semua boleh masuk.")]
    [SerializeField] private List<ItemData.FoodCategory> allowedFoodCategories;


    public bool BlocksTrophyItems { get { return blockTrophyItems; } }
    public List<ItemData.FoodCategory> AllowedFoodCategories { get { return allowedFoodCategories; } }





    public void SetAllowedFoodCategories(List<ItemData.FoodCategory> categories)
    {
        if (allowedFoodCategories == null)
            allowedFoodCategories = new List<ItemData.FoodCategory>();
        allowedFoodCategories.Clear();
        if (categories != null)
            allowedFoodCategories.AddRange(categories);
    }





    public bool CanAcceptItem(ItemData item)
    {
        if (item == null)
            return false;


        if (blockTrophyItems && item.type == ItemData.ItemType.Trophy)
            return false;


        if (allowedFoodCategories != null && allowedFoodCategories.Count > 0)
        {
            bool allowed = false;
            for (int i = 0; i < allowedFoodCategories.Count; i++)
            {
                if (allowedFoodCategories[i] == item.foodCategory)
                {
                    allowed = true;
                    break;
                }
            }
            if (!allowed)
                return false;
        }

        return true;
    }


    public System.Action OnInventoryChanged;


    public int selectedHotbarIndex = 0;
    public System.Action<int> OnHotbarSelected;

    public void SelectHotbarSlot(int index)
    {

        index = Mathf.Clamp(index, 0, 3);
        selectedHotbarIndex = index;
        OnHotbarSelected?.Invoke(selectedHotbarIndex);
    }

    public void ResetInventory(int capacity)
    {
        maxCapacity = capacity;
        slots.Clear();
        for (int i = 0; i < maxCapacity; i++)
            slots.Add(new InventorySlot());
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;


        if (!CanAcceptItem(item))
            return false;

        int remaining = amount;


        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0) break;

            if (!slot.IsEmpty && slot.item == item)
            {
                int space = item.maxStack - slot.quantity;
                int toAdd = Mathf.Min(space, remaining);
                if (toAdd > 0)
                {
                    slot.quantity += toAdd;
                    remaining -= toAdd;
                }
            }
        }


        if (remaining > 0)
        {
            foreach (InventorySlot slot in slots)
            {
                if (remaining <= 0) break;

                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(item.maxStack, remaining);
                    slot.item = item;
                    slot.quantity = toAdd;
                    remaining -= toAdd;
                }
            }
        }

        if (remaining == 0)
            OnInventoryChanged?.Invoke();

        return remaining == 0;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (CountTotalQuantity(item) < amount)
            return false;

        int remaining = amount;


        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || slot.item != item)
                continue;

            int toRemove = Mathf.Min(slot.quantity, remaining);
            slot.quantity -= toRemove;
            remaining -= toRemove;

            if (slot.quantity <= 0)
            {
                slot.item = null;
                slot.quantity = 0;
            }
        }

        if (remaining == 0)
            OnInventoryChanged?.Invoke();

        return remaining == 0;
    }

    public void TransferItemTo(InventoryComponent targetInventory, ItemData item, int amount)
    {
        if (targetInventory == null || item == null || amount <= 0)
            return;

        if (targetInventory == this)
            return;

        int available = CountTotalQuantity(item);
        int amountToMove = Mathf.Min(amount, available);
        if (amountToMove <= 0)
            return;


        if (!targetInventory.AddItem(item, amountToMove))
            return;

        RemoveItem(item, amountToMove);


        targetInventory.OnInventoryChanged?.Invoke();
        OnInventoryChanged?.Invoke();
    }









    public void TransferItemTo(InventoryComponent targetInventory, int sourceIndex)
    {
        if (targetInventory == null || targetInventory == this)
            return;

        if (sourceIndex < 0 || sourceIndex >= slots.Count)
            return;

        InventorySlot source = slots[sourceIndex];
        if (source == null || source.IsEmpty || source.item == null)
            return;


        if (!targetInventory.AddItem(source.item, source.quantity))
            return;


        source.item = null;
        source.quantity = 0;


        OnInventoryChanged?.Invoke();
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
            return;

        ItemData item = slot.item;

        if (item.type == ItemData.ItemType.Consumable)
        {

            PlayerStats playerStats = GetComponent<PlayerStats>();
            if (playerStats != null)
                playerStats.Heal(item.healAmount);

            Debug.Log("Mengonsumsi " + item.itemName + " memulihkan " + item.healAmount + " HP");


            slot.quantity -= 1;
            if (slot.quantity <= 0)
            {
                slot.item = null;
                slot.quantity = 0;
            }

            OnInventoryChanged?.Invoke();
        }
    }

    public void DropItem(int slotIndex)
    {

        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
            return;

        ItemData item = slot.item;
        Debug.Log(string.Format("Dropping {0} to the ground!", item.itemName));


        slot.quantity -= 1;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }

        OnInventoryChanged?.Invoke();



        if (slotIndex == selectedHotbarIndex)
            OnHotbarSelected?.Invoke(selectedHotbarIndex);
    }





    public void ReplaceItemAt(int index, ItemData item, int quantity)
    {
        if (index < 0 || index >= slots.Count)
            return;

        InventorySlot slot = slots[index];
        if (slot == null)
            return;

        slot.item = item;
        slot.quantity = quantity;
        OnInventoryChanged?.Invoke();
    }





    public void RemoveFromSlot(int index, int amount)
    {
        if (index < 0 || index >= slots.Count)
            return;

        InventorySlot slot = slots[index];
        if (slot == null || slot.IsEmpty || amount <= 0)
            return;

        slot.quantity -= amount;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }

        OnInventoryChanged?.Invoke();
    }



    public void MoveItemToSlot(int sourceIndex, InventoryComponent targetInventory, int targetIndex)
    {
        if (targetInventory == null)
            return;

        if (sourceIndex < 0 || sourceIndex >= slots.Count)
            return;
        if (targetIndex < 0 || targetIndex >= targetInventory.slots.Count)
            return;

        InventorySlot sourceSlot = slots[sourceIndex];
        InventorySlot targetSlot = targetInventory.slots[targetIndex];
        if (sourceSlot == null || targetSlot == null)
            return;

        if (sourceSlot.IsEmpty)
            return;


        if (sourceSlot.item != null && !targetInventory.CanAcceptItem(sourceSlot.item))
            return;

        if (targetSlot.IsEmpty)
        {

            targetSlot.item = sourceSlot.item;
            targetSlot.quantity = sourceSlot.quantity;
            sourceSlot.item = null;
            sourceSlot.quantity = 0;
        }
        else if (targetSlot.item == sourceSlot.item)
        {

            int space = targetSlot.item.maxStack - targetSlot.quantity;
            if (space > 0)
            {
                int toMove = Mathf.Min(space, sourceSlot.quantity);
                targetSlot.quantity += toMove;
                sourceSlot.quantity -= toMove;
                if (sourceSlot.quantity <= 0)
                {
                    sourceSlot.item = null;
                    sourceSlot.quantity = 0;
                }
            }
        }
        else
        {

            InventorySlot temp = new InventorySlot();
            temp.item = targetSlot.item;
            temp.quantity = targetSlot.quantity;
            targetSlot.item = sourceSlot.item;
            targetSlot.quantity = sourceSlot.quantity;
            sourceSlot.item = temp.item;
            sourceSlot.quantity = temp.quantity;
        }

        OnInventoryChanged?.Invoke();
        if (this != targetInventory)
            targetInventory.OnInventoryChanged?.Invoke();

        if (selectedHotbarIndex == sourceIndex)
            OnHotbarSelected?.Invoke(selectedHotbarIndex);
        if (targetInventory.selectedHotbarIndex == targetIndex)
            targetInventory.OnHotbarSelected?.Invoke(targetInventory.selectedHotbarIndex);
    }

    public void SwapSlots(int indexA, int indexB)
    {        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count)
            return;

        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;


        OnInventoryChanged?.Invoke();
    }

    public int CountItem(ItemData item)
    {
        return CountTotalQuantity(item);
    }

    private int CountTotalQuantity(ItemData item)
    {
        int total = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty && slot.item == item)
                total += slot.quantity;
        }
        return total;
    }
}
```

## File: Features/Inventory/InventoryComponent.cs.meta
```
fileFormatVersion: 2
guid: 70861eb2f122e0640af8abd768ee654f
```

## File: Features/Inventory/InventorySlot.cs
```csharp
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty
    {
        get { return item == null || quantity <= 0; }
    }
}
```

## File: Features/Inventory/InventorySlot.cs.meta
```
fileFormatVersion: 2
guid: 0a31e99fe7084f242a209060ae9c006c
```

## File: Features/Inventory/ItemData.cs
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public enum ItemType { Material, Consumable, Tool, Trophy }



    public enum FoodCategory { None, Vegetable, Fruit, Meat, Ingredient, Dish }

    public string itemName;
    public Sprite itemIcon;
    public int maxStack;

    public ItemType type;
    public int healAmount;


    public FoodCategory foodCategory;


    public GameObject equipPrefab;



    public GameObject placeablePrefab;
}
```

## File: Features/Inventory/ItemData.cs.meta
```
fileFormatVersion: 2
guid: ae5c125112b5be549a05d6bb0b7bba92
```

## File: Features/Inventory/UI.meta
```
fileFormatVersion: 2
guid: 783b0ef21f3148c4282b8d54720db52c
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Kitchen/Data/Cook_Rice.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5335ef365cb29a940816d2e56467f4a9, type: 3}
  m_Name: Cook_Rice
  m_EditorClassIdentifier: Assembly-CSharp::KitchenRecipe
  input: {fileID: 11400000, guid: 81667949593bee043a9439fc507ec44b, type: 2}
  output: {fileID: 11400000, guid: 205a11daf64e36e48b2fe81c0fadb624, type: 2}
  outputCount: 1
  processTime: 4
```

## File: Features/Kitchen/Data/Cook_Rice.asset.meta
```
fileFormatVersion: 2
guid: 7142947d7d113094ba877af4b06d21fc
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Kitchen/Data/Cook_Veggies.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5335ef365cb29a940816d2e56467f4a9, type: 3}
  m_Name: Cook_Veggies
  m_EditorClassIdentifier: Assembly-CSharp::KitchenRecipe
  input: {fileID: 11400000, guid: 5bd3aca182d0923468b2c603d86f737a, type: 2}
  output: {fileID: 11400000, guid: 1bdf5b57a2726b9418733071267703ec, type: 2}
  outputCount: 1
  processTime: 5
```

## File: Features/Kitchen/Data/Cook_Veggies.asset.meta
```
fileFormatVersion: 2
guid: b8e8b7cf0b2612a4f8c09fbc30dd641a
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Kitchen/Data/Wash_Carrot.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5335ef365cb29a940816d2e56467f4a9, type: 3}
  m_Name: Wash_Carrot
  m_EditorClassIdentifier: Assembly-CSharp::KitchenRecipe
  input: {fileID: 11400000, guid: b3319f908a899664698f67f253f2c66d, type: 2}
  output: {fileID: 11400000, guid: 5bd3aca182d0923468b2c603d86f737a, type: 2}
  outputCount: 1
  processTime: 3
```

## File: Features/Kitchen/Data/Wash_Carrot.asset.meta
```
fileFormatVersion: 2
guid: f9e7c509fce275842b2f97b290f664e5
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Kitchen/UI/KitchenStationProgressOverlay.cs
```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;









public class KitchenStationProgressOverlay : MonoBehaviour
{
    [Tooltip("Stasiun yang dipantau (Sink / Kompor). Jika kosong, memakai komponen sejenis pada GameObject ini.")]
    [SerializeField] private KitchenStation station;

    [Tooltip("Dasar overlay per slot (urutan = index slot). Stove: Burner_1 & Burner_2. Kosong = fallback ke atas Renderer.")]
    [SerializeField] private Transform[] slotAnchors;

    [Tooltip("Material overlay transparan (URP Unlit, alpha rendah).")]
    [SerializeField] private Material overlayMaterial;

    [Header("Tampilan")]
    [Tooltip("Warna dasar overlay.")]
    [SerializeField] private Color overlayColor = new Color(1f, 1f, 1f, 0.5f);
    [Tooltip("Lebar overlay dalam satuan dunia.")]
    [SerializeField] private float overlayWidth = 0.9f;
    [Tooltip("Tinggi maksimum overlay saat progress penuh (satuan dunia).")]
    [SerializeField] private float maxHeight = 0.7f;
    [Tooltip("Bila true, overlay menghadap kamera (billboard, terkunci sumbu Y = tetap naik ke atas).")]
    [SerializeField] private bool useBillboard = true;
    [Tooltip("Tag kamera utama untuk billboard.")]
    [SerializeField] private string billboardCameraTag = "MainCamera";

    [Header("Feedback Selesai")]
    [Tooltip("Lama panel 'Selesai!' tampil sebelum overlay disembunyikan.")]
    [SerializeField] private float hideDelayAfterComplete = 1.2f;
    [Tooltip("Durasi efek flash alpha saat selesai.")]
    [SerializeField] private float flashDuration = 0.25f;
    [Tooltip("Klip suara saat selesai (kosong = pop prosedural dibuat otomatis).")]
    [SerializeField] private AudioClip completeSound;
    [Tooltip("Volume suara selesai.")]
    [SerializeField] private float soundVolume = 0.8f;
    [Tooltip("Jeda minimum antar bunyi agar beberapa slot yang selesai bersamaan tidak bunyi dobel.")]
    [SerializeField] private float soundCooldown = 0.5f;

    private class SlotOverlay
    {
        public Transform root;
        public Transform quad;
        public MeshRenderer renderer;
        public bool started;
        public Coroutine hideRoutine;
        public Color defaultColor;
    }

    private readonly Dictionary<int, SlotOverlay> overlays = new Dictionary<int, SlotOverlay>();
    private AudioSource audioSource;
    private AudioClip generatedPop;
    private Camera cachedCamera;
    private float lastSoundTime = -100f;
    private bool warnedMissing;

    private void OnEnable()
    {
        if (station == null)
            station = GetComponent<KitchenStation>();

        if (station == null)
        {
            if (!warnedMissing)
            {
                Debug.LogWarning("[KitchenStationProgressOverlay] station tidak ditemukan di '" + gameObject.name + "'. Overlay dinonaktifkan.", this);
                warnedMissing = true;
            }
            return;
        }

        BuildOverlays();
        HideAll();

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": " + overlays.Count + " overlay dibuat. " + BuildOverlayPositionReport(), this);

        station.OnProcessStarted += OnProcessStarted;
        station.OnProcessProgress += OnProcessProgress;
        station.OnProcessCompleted += OnProcessCompleted;
        station.OnProcessCancelled += OnProcessCancelled;
    }

    private void OnDisable()
    {
        if (station == null)
            return;

        station.OnProcessStarted -= OnProcessStarted;
        station.OnProcessProgress -= OnProcessProgress;
        station.OnProcessCompleted -= OnProcessCompleted;
        station.OnProcessCancelled -= OnProcessCancelled;

        HideAll();
    }

    private void Update()
    {


        if (station == null || overlays.Count == 0)
            return;

        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null || o.quad == null)
                continue;


            if (o.hideRoutine != null)
                continue;

            int slot = pair.Key;
            if (station.IsProcessing(slot))
            {
                if (!o.root.gameObject.activeSelf)
                {
                    o.started = true;
                    o.root.gameObject.SetActive(true);
                }
                SetVeilHeight(o, maxHeight * station.GetSlotProgress(slot));
            }
            else if (o.started || o.root.gameObject.activeSelf)
            {

                o.started = false;
                o.root.gameObject.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (!useBillboard || overlays.Count == 0)
            return;

        Camera cam = FindActiveCamera();
        if (cam == null)
            return;

        bool dirty = false;
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null || o.root == null || !o.root.gameObject.activeSelf)
                continue;

            Vector3 flat = cam.transform.position - o.root.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                o.root.rotation = Quaternion.LookRotation(flat);
                dirty = true;
            }
        }
        if (dirty)
            cachedCamera = cam;
    }

    private void BuildOverlays()
    {
        if (overlays.Count > 0)
            return;

        int count = (slotAnchors != null && slotAnchors.Length > 0) ? slotAnchors.Length : 1;
        Vector3 fallbackAnchor = ComputeFallbackAnchor();

        for (int i = 0; i < count; i++)
        {
            Transform anchor = (slotAnchors != null && i < slotAnchors.Length && slotAnchors[i] != null)
                ? slotAnchors[i]
                : null;

            Vector3 worldPos = anchor != null ? anchor.position : fallbackAnchor;


            GameObject rootGO = new GameObject("ProgressOverlay_" + i);
            Transform root = rootGO.transform;
            root.SetParent(transform, false);
            Vector3 s = transform.lossyScale;
            if (s.x != 0f && s.y != 0f && s.z != 0f)
                root.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            root.position = worldPos;


            GameObject quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGO.name = "Veil_" + i;
            Collider col = quadGO.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            Transform quad = quadGO.transform;
            quad.SetParent(root, false);
            quad.localPosition = Vector3.zero;
            quad.localRotation = Quaternion.identity;
            quad.localScale = new Vector3(overlayWidth, 0.0001f, 1f);

            MeshRenderer mesh = quadGO.GetComponent<MeshRenderer>();
            mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mesh.receiveShadows = false;
            mesh.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            if (overlayMaterial != null) { mesh.material = overlayMaterial; mesh.material.renderQueue = 4000; } else if (!warnedMissing)
            {
                Debug.LogWarning("[KitchenStationProgressOverlay] overlayMaterial kosong di '" + gameObject.name + "'. Veil tampil apa adanya.", this);
                warnedMissing = true;
            }
            Color color = (overlayMaterial != null) ? overlayColor : Color.white;
            mesh.material.color = color;

            SlotOverlay slot = new SlotOverlay();
            slot.root = root;
            slot.quad = quad;
            slot.renderer = mesh;
            slot.started = false;
            slot.defaultColor = color;

            rootGO.SetActive(false);
            quadGO.SetActive(true);
            overlays[i] = slot;
        }
    }

    private Vector3 ComputeFallbackAnchor()
    {
        Vector3 pos = transform.position;
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Bounds b = r.bounds;
            pos = new Vector3(b.center.x, b.max.y, b.max.z);
        }
        return pos;
    }

    private void OnProcessStarted(int slotIndex, float duration)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null || o.quad == null)
            return;

        if (o.hideRoutine != null)
        {
            StopCoroutine(o.hideRoutine);
            o.hideRoutine = null;
        }

        o.started = true;
        o.root.gameObject.SetActive(true);
        SetVeilHeight(o, 0f);

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": proses MULAI slot " + slotIndex + " (durasi " + duration + "s)", this);
    }

    private void OnProcessProgress(int slotIndex, float progress01)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null || o.quad == null)
            return;


        if (!o.started || !o.root.gameObject.activeSelf)
        {
            o.started = true;
            o.root.gameObject.SetActive(true);
        }

        float height = maxHeight * Mathf.Clamp01(progress01);
        SetVeilHeight(o, height);
    }

    private void OnProcessCompleted(int slotIndex)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null || o.quad == null)
            return;

        o.root.gameObject.SetActive(true);
        SetVeilHeight(o, maxHeight);
        PlayCompleteSound();

        if (o.hideRoutine != null)
            StopCoroutine(o.hideRoutine);
        o.hideRoutine = StartCoroutine(FlashThenHideRoutine(o));

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": proses SELESAI slot " + slotIndex, this);
    }

    private void OnProcessCancelled(int slotIndex)
    {
        SlotOverlay o = GetOverlay(slotIndex);
        if (o == null)
            return;

        o.started = false;
        if (o.hideRoutine != null)
        {
            StopCoroutine(o.hideRoutine);
            o.hideRoutine = null;
        }
        SetVeilHeight(o, 0f);
        o.root.gameObject.SetActive(false);

        Debug.Log("[KitchenStationProgressOverlay] " + gameObject.name + ": proses DIBATALKAN slot " + slotIndex, this);
    }

    private IEnumerator FlashThenHideRoutine(SlotOverlay o)
    {

        float t = 0f;
        while (t < flashDuration && o.renderer != null)
        {
            Color c = o.defaultColor;
            c.a = Mathf.Lerp(1f, o.defaultColor.a, Mathf.PingPong(t * (2f / flashDuration), 1f));
            o.renderer.material.color = c;
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(hideDelayAfterComplete);

        if (o.renderer != null)
            o.renderer.material.color = o.defaultColor;
        SetVeilHeight(o, 0f);
        if (o.root != null)
            o.root.gameObject.SetActive(false);
        o.started = false;
        o.hideRoutine = null;
    }

    private void SetVeilHeight(SlotOverlay o, float height)
    {
        if (o.quad == null)
            return;

        float h = Mathf.Max(height, 0.0001f);
        o.quad.localScale = new Vector3(overlayWidth, h, 1f);

        o.quad.localPosition = new Vector3(0f, h * 0.5f, 0f);
    }

    private string BuildOverlayPositionReport()
    {
        string report = "";
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null || o.root == null)
                continue;
            report += " [slot " + pair.Key + "]" + o.root.position.ToString("F2");
        }
        return report;
    }

    private SlotOverlay GetOverlay(int slotIndex)
    {
        if (overlays.Count == 0)
            return null;

        if (overlays.TryGetValue(slotIndex, out SlotOverlay o))
            return o;


        int lastIndex = -1;
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            if (pair.Key > lastIndex)
                lastIndex = pair.Key;
        }

        if (lastIndex >= 0 && overlays.TryGetValue(lastIndex, out SlotOverlay fallback))
            return fallback;
        return null;
    }

    private void HideAll()
    {
        foreach (KeyValuePair<int, SlotOverlay> pair in overlays)
        {
            SlotOverlay o = pair.Value;
            if (o == null)
                continue;
            if (o.hideRoutine != null && isActiveAndEnabled)
                StopCoroutine(o.hideRoutine);
            o.hideRoutine = null;
            o.started = false;
            if (o.renderer != null)
                o.renderer.material.color = o.defaultColor;
            if (o.root != null)
                o.root.gameObject.SetActive(false);
        }
    }

    private void PlayCompleteSound()
    {
        if (Time.time - lastSoundTime < soundCooldown || soundVolume <= 0f)
            return;

        lastSoundTime = Time.time;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (completeSound != null)
        {
            audioSource.PlayOneShot(completeSound, soundVolume);
            return;
        }

        if (generatedPop == null)
            generatedPop = CreatePopClip();
        if (generatedPop != null)
            audioSource.PlayOneShot(generatedPop, soundVolume);
    }

    private static AudioClip CreatePopClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.14f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(1400f, 400f, t / duration);
            float envelope = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("ProgressPop", samples, 1, sampleRate, false);
        if (clip != null)
            clip.SetData(data, 0);
        return clip;
    }

    private Camera FindActiveCamera()
    {
        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        Camera[] all = Camera.allCameras;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].isActiveAndEnabled)
                return all[i];
        }
        return null;
    }
}
```

## File: Features/Kitchen/UI/KitchenStationProgressOverlay.cs.meta
```
fileFormatVersion: 2
guid: e79f3b27cb433e24cb5486a2484b0c39
```

## File: Features/Kitchen/UI/KitchenStationSoundFx.cs
```csharp
using UnityEngine;







public class KitchenStationSoundFx : MonoBehaviour
{
    [Tooltip("Stasiun yang dipantau (Sink / Kompor). Jika kosong, memakai komponen sendiri.")]
    [SerializeField] private KitchenStation station;

    [Tooltip("Volume suara selesai.")]
    [SerializeField] private float soundVolume = 0.8f;

    [Tooltip("Jeda minimum antar bunyi agar 2 slot yang selesai bersamaan tidak dobel.")]
    [SerializeField] private float soundCooldown = 0.5f;

    private AudioSource audioSource;
    private AudioClip generatedPop;
    private float lastSoundTime = -100f;

    private void OnEnable()
    {
        if (station == null)
            station = GetComponent<KitchenStation>();

        if (station == null)
            return;

        station.OnProcessCompleted += OnProcessCompleted;
    }

    private void OnDisable()
    {
        if (station == null)
            return;

        station.OnProcessCompleted -= OnProcessCompleted;
    }

    private void OnProcessCompleted(int slotIndex)
    {
        if (Time.time - lastSoundTime < soundCooldown || soundVolume <= 0f)
            return;

        lastSoundTime = Time.time;

        Debug.Log("[KitchenStationSoundFx] " + gameObject.name + ": slot " + slotIndex + " selesai, bunyi diputar.");

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (generatedPop == null)
            generatedPop = CreatePopClip();
        if (generatedPop != null)
            audioSource.PlayOneShot(generatedPop, soundVolume);
    }

    private static AudioClip CreatePopClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.14f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(1400f, 400f, t / duration);
            float envelope = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("ProgressPop", samples, 1, sampleRate, false);
        if (clip != null)
            clip.SetData(data, 0);
        return clip;
    }
}
```

## File: Features/Kitchen/UI/KitchenStationSoundFx.cs.meta
```
fileFormatVersion: 2
guid: 959b73b363c78864fb50e28322d13f38
```

## File: Features/Kitchen/UI/KitchenStationUI.cs
```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;






public class KitchenStationUI : MonoBehaviour
{
    [Tooltip("Stasiun yang dipantau (Sink / Kompor).")]
    [SerializeField] private KitchenStation station;

    [Tooltip("Panel root progress (CanvasGroup). Saat tidak memproses, panel disembunyikan.")]
    [SerializeField] private CanvasGroup panelGroup;

    [Tooltip("Baris progress (Image berjenis Filled).")]
    [SerializeField] private Image progressFill;

    [Tooltip("Teks status proses (opsional).")]
    [SerializeField] private Text statusText;

    [Tooltip("Lama tampil teks 'Selesai!' sebelum panel disembunyikan.")]
    [SerializeField] private float hideDelayAfterComplete = 1.2f;

    private int activeSlotIndex = -1;
    private Coroutine hideRoutine;

    private void OnEnable()
    {
        if (station != null)
        {
            station.OnProcessStarted += OnProcessStarted;
            station.OnProcessProgress += OnProcessProgress;
            station.OnProcessCompleted += OnProcessCompleted;
        }

        if (panelGroup == null)
            panelGroup = GetComponent<CanvasGroup>();


        SetPanelVisible(false);
    }

    private void OnDisable()
    {
        if (station != null)
        {
            station.OnProcessStarted -= OnProcessStarted;
            station.OnProcessProgress -= OnProcessProgress;
            station.OnProcessCompleted -= OnProcessCompleted;
        }

        CancelHideRoutine();
    }

    private void OnProcessStarted(int slotIndex, float duration)
    {
        activeSlotIndex = slotIndex;
        CancelHideRoutine();
        SetPanelVisible(true);
        SetVisual(0f, "Memproses...");
    }

    private void OnProcessProgress(int slotIndex, float progress01)
    {
        if (slotIndex != activeSlotIndex)
            return;
        SetVisual(Mathf.Clamp01(progress01), null);
    }

    private void OnProcessCompleted(int slotIndex)
    {
        if (slotIndex != activeSlotIndex)
            return;

        activeSlotIndex = -1;
        SetVisual(1f, "Selesai!");
        CancelHideRoutine();
        hideRoutine = StartCoroutine(HideAfterDelayRoutine());
    }

    private IEnumerator HideAfterDelayRoutine()
    {
        yield return new WaitForSeconds(hideDelayAfterComplete);
        SetPanelVisible(false);
        SetVisual(0f, "");
        hideRoutine = null;
    }

    private void CancelHideRoutine()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    private void SetVisual(float amount01, string status)
    {
        if (progressFill != null)
            progressFill.fillAmount = amount01;

        if (statusText != null && status != null)
            statusText.text = status;
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelGroup == null)
            return;

        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.blocksRaycasts = visible;
    }
}
```

## File: Features/Kitchen/UI/KitchenStationUI.cs.meta
```
fileFormatVersion: 2
guid: a73026acccdb0c344919a7cc59649045
```

## File: Features/Kitchen/Data.meta
```
fileFormatVersion: 2
guid: 075912a0623aea74d9eaeb1684de1b71
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Kitchen/DoorInteractable.cs
```csharp
using UnityEngine;
using FeaturesInteraction;






public enum ThresholdAxis
{
    X,
    Y,
    Z
}

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Spawn Points (Two-Way)")]
    [Tooltip("Spawn point ketika player berada di DALAM (keluar ke luar)")]
    [SerializeField] private Transform spawnPointInside;

    [Tooltip("Spawn point ketika player berada di LUAR (masuk ke dalam)")]
    [SerializeField] private Transform spawnPointOutside;

    [Header("Detection")]
    [Tooltip("Axis to use for inside/outside detection")]
    [SerializeField] private ThresholdAxis thresholdAxis = ThresholdAxis.Z;

    [Tooltip("Threshold value on selected axis: player coordinate > threshold = inside (for X/Z), player coordinate < threshold = inside (for Y)")]
    [SerializeField] private float insideThreshold = 14.0f;

    public void Interact(GameObject interactor)
    {
        if (spawnPointInside == null || spawnPointOutside == null)
        {
            Debug.LogWarning("DoorInteractable: spawnPointInside atau spawnPointOutside belum di-set.");
            return;
        }

        PlayerControl player = interactor != null ? interactor.GetComponent<PlayerControl>() : null;
        if (player == null)
            player = FindFirstObjectByType<PlayerControl>();

        if (player == null)
            return;


        float playerCoord = GetCoordinate(player.transform.position, thresholdAxis);
        bool isInside = (thresholdAxis == ThresholdAxis.X || thresholdAxis == ThresholdAxis.Z)
            ? playerCoord > insideThreshold
            : playerCoord < insideThreshold;

        Transform targetSpawn = isInside ? spawnPointOutside : spawnPointInside;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = targetSpawn.position;
        }
        else
        {
            player.transform.position = targetSpawn.position;
        }

        Debug.Log($"[DoorInteractable] Player teleported to " + (isInside ? "outside" : "inside") + " via " + gameObject.name);
    }

    private float GetCoordinate(Vector3 pos, ThresholdAxis axis)
    {
        return axis switch
        {
            ThresholdAxis.X => pos.x,
            ThresholdAxis.Y => pos.y,
            ThresholdAxis.Z => pos.z,
            _ => pos.z
        };
    }
}
```

## File: Features/Kitchen/DoorInteractable.cs.meta
```
fileFormatVersion: 2
guid: ea1c453d6faf4e4468a0a4b76440433f
```

## File: Features/Kitchen/KitchenRecipe.cs
```csharp
using UnityEngine;





[CreateAssetMenu(fileName = "NewRecipe", menuName = "FarmBeware/Kitchen Recipe")]
public class KitchenRecipe : ScriptableObject
{
    [Tooltip("Bahan yang dimasukkan ke slot (harus item ini).")]
    public ItemData input;

    [Tooltip("Hasil setelah proses selesai (state baru).")]
    public ItemData output;

    [Tooltip("Jumlah hasil yang diproduksi.")]
    public int outputCount = 1;

    [Tooltip("Durasi proses dalam detik.")]
    public float processTime = 3f;
}
```

## File: Features/Kitchen/KitchenRecipe.cs.meta
```
fileFormatVersion: 2
guid: 5335ef365cb29a940816d2e56467f4a9
```

## File: Features/Kitchen/KitchenSinkInteractable.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using FeaturesInteraction;







[RequireComponent(typeof(InventoryComponent))]
public class KitchenSinkInteractable : KitchenStation, IInteractable
{
    [Header("Recipe Pencucian (kotor -> bersih)")]
    [Tooltip("Mapping item kotor -> item bersih + durasi.")]
    [SerializeField] private List<KitchenRecipe> washRecipes = new List<KitchenRecipe>();

    [Tooltip("Bila true, hasil cuci otomatis dikembalikan ke Inventory Player. Bila false (default), hasil tetap di slot sink seperti Stove.")]
    [SerializeField] private bool returnWashedToPlayer = false;

    protected override void Awake()
    {
        base.Awake();



        if (!returnWashedToPlayer)
        {
            resultTarget = null;
            return;
        }


        if (resultTarget == null)
        {
            GameObject playerGO = GameObject.Find("Player");
            if (playerGO != null)
                resultTarget = playerGO.GetComponent<InventoryComponent>();
        }
    }

    protected override KitchenRecipe FindRecipeFor(ItemData item, int slotIndex)
    {
        if (item == null)
            return null;

        for (int i = 0; i < washRecipes.Count; i++)
        {
            if (washRecipes[i] != null && washRecipes[i].input == item)
                return washRecipes[i];
        }

        return null;
    }

    public void Interact(GameObject interactor)
    {
        if (InventoryManagerUI.Instance != null && StationInventory != null)
            InventoryManagerUI.Instance.OpenStorageUI(StationInventory);
    }
}
```

## File: Features/Kitchen/KitchenSinkInteractable.cs.meta
```
fileFormatVersion: 2
guid: 3180717363bc1b045b980128eb719bc2
```

## File: Features/Kitchen/KitchenStation.cs
```csharp
using System.Collections.Generic;
using UnityEngine;







public abstract class KitchenStation : MonoBehaviour
{
    [Tooltip("Inventori stasiun (slot tempat bahan ditaruh). Jika kosong, memakai komponen sendiri.")]
    [SerializeField] protected InventoryComponent stationInventory;

    [Tooltip("Tujuan hasil (mis. Inventory Player). Kosong = hasil tetap di slot stasiun.")]
    [SerializeField] protected InventoryComponent resultTarget;


    private readonly Dictionary<int, KitchenRecipe> activeRecipes = new Dictionary<int, KitchenRecipe>();
    private readonly Dictionary<int, float> remainingTime = new Dictionary<int, float>();
    private readonly Dictionary<int, float> totalDuration = new Dictionary<int, float>();
    private readonly Dictionary<int, int> processQuantity = new Dictionary<int, int>();

    public event System.Action<int, float> OnProcessStarted;
    public event System.Action<int, float> OnProcessProgress;
    public event System.Action<int> OnProcessCompleted;
    public event System.Action<int> OnProcessCancelled;

    public InventoryComponent StationInventory { get { return stationInventory; } }



    public int ActiveSlotCount { get { return activeRecipes.Count; } }
    public bool IsProcessing(int slot) { return activeRecipes.ContainsKey(slot); }
    public float GetSlotProgress(int slot)
    {
        if (!remainingTime.TryGetValue(slot, out float remaining))
            return 0f;
        if (!totalDuration.TryGetValue(slot, out float total) || total <= 0f)
            return 0f;
        return Mathf.Clamp01(1f - remaining / total);
    }

    protected abstract KitchenRecipe FindRecipeFor(ItemData item, int slotIndex);

    protected virtual void Awake()
    {
        if (stationInventory == null)
            stationInventory = GetComponent<InventoryComponent>();
    }

    protected virtual void OnEnable()
    {
        if (stationInventory != null)
        {
            stationInventory.OnInventoryChanged += HandleInventoryChanged;


            HandleInventoryChanged();
        }
    }

    protected virtual void OnDisable()
    {
        if (stationInventory != null)
            stationInventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (stationInventory == null || stationInventory.slots == null)
            return;

        for (int i = 0; i < stationInventory.slots.Count; i++)
        {
            InventorySlot slot = stationInventory.slots[i];
            if (slot == null)
                continue;

            bool processing = activeRecipes.ContainsKey(i);

            if (slot.IsEmpty || slot.item == null)
            {
                if (processing)
                    CancelProcessing(i);
                continue;
            }

            KitchenRecipe recipe = FindRecipeFor(slot.item, i);
            if (!processing && recipe != null)
                StartProcessing(i, recipe);
            else if (processing && (recipe == null || recipe != activeRecipes[i]))
                CancelProcessing(i);
        }
    }

    private void StartProcessing(int slotIndex, KitchenRecipe recipe)
    {
        activeRecipes[slotIndex] = recipe;
        remainingTime[slotIndex] = recipe.processTime;
        totalDuration[slotIndex] = recipe.processTime;


        InventorySlot slot = SafeSlot(slotIndex);
        if (slot != null && !slot.IsEmpty)
            processQuantity[slotIndex] = slot.quantity;
        else
            processQuantity[slotIndex] = 1;

        OnProcessStarted?.Invoke(slotIndex, recipe.processTime);
        OnProcessProgress?.Invoke(slotIndex, 0f);
    }

    private void CancelProcessing(int slotIndex)
    {
        activeRecipes.Remove(slotIndex);
        remainingTime.Remove(slotIndex);
        totalDuration.Remove(slotIndex);
        processQuantity.Remove(slotIndex);

        OnProcessCancelled?.Invoke(slotIndex);
    }

    protected virtual void Update()
    {
        if (activeRecipes.Count == 0 || stationInventory == null)
            return;



        float dt = Mathf.Min(Time.deltaTime, 1f);

        List<int> indices = new List<int>(remainingTime.Keys);
        for (int k = 0; k < indices.Count; k++)
        {
            int i = indices[k];
            if (!activeRecipes.ContainsKey(i) || !remainingTime.TryGetValue(i, out float remaining) ||
                !totalDuration.TryGetValue(i, out float total) || total <= 0f)
                continue;


            InventorySlot slot = SafeSlot(i);
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                CancelProcessing(i);
                continue;
            }

            remainingTime[i] -= dt;
            OnProcessProgress?.Invoke(i, Mathf.Clamp01(1f - remainingTime[i] / total));

            if (remainingTime[i] <= 0f)
                CompleteProcessing(i);
        }
    }

    private void CompleteProcessing(int slotIndex)
    {
        if (!activeRecipes.TryGetValue(slotIndex, out KitchenRecipe recipe))
            return;


        int qty = 1;
        if (processQuantity.TryGetValue(slotIndex, out int savedQty) && savedQty > 0)
            qty = savedQty;


        activeRecipes.Remove(slotIndex);
        remainingTime.Remove(slotIndex);
        totalDuration.Remove(slotIndex);
        processQuantity.Remove(slotIndex);


        InventorySlot slot = SafeSlot(slotIndex);
        if (slot != null && !slot.IsEmpty && slot.item == recipe.input)
            stationInventory.RemoveFromSlot(slotIndex, qty);


        bool stored = resultTarget != null && resultTarget.AddItem(recipe.output, qty * recipe.outputCount);
        if (!stored)
            stationInventory.ReplaceItemAt(slotIndex, recipe.output, qty * recipe.outputCount);

        OnProcessCompleted?.Invoke(slotIndex);
    }

    private InventorySlot SafeSlot(int index)
    {
        if (stationInventory == null || stationInventory.slots == null)
            return null;
        return (index >= 0 && index < stationInventory.slots.Count) ? stationInventory.slots[index] : null;
    }
}
```

## File: Features/Kitchen/KitchenStation.cs.meta
```
fileFormatVersion: 2
guid: 6a7e9a65af74cf34d8a74e50a12295bc
```

## File: Features/Kitchen/RefrigeratorInteractable.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using FeaturesInteraction;






[RequireComponent(typeof(InventoryComponent))]
public class RefrigeratorInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Kategori makanan yang boleh disimpan (default: sayur & buah).")]
    [SerializeField] private List<ItemData.FoodCategory> allowedCategories =
        new List<ItemData.FoodCategory>
        {
            ItemData.FoodCategory.Vegetable,
            ItemData.FoodCategory.Fruit,
        };

    private void Awake()
    {
        if (allowedCategories == null || allowedCategories.Count == 0)
        {
            allowedCategories = new List<ItemData.FoodCategory>
            {
                ItemData.FoodCategory.Vegetable,
                ItemData.FoodCategory.Fruit,
            };
        }

        ApplyRestriction(GetComponent<InventoryComponent>());
    }

    private void ApplyRestriction(InventoryComponent inventory)
    {

        inventory?.SetAllowedFoodCategories(allowedCategories);
    }

    public void Interact(GameObject interactor)
    {
        InventoryComponent inventory = GetComponent<InventoryComponent>();
        if (inventory == null)
            return;


        ApplyRestriction(inventory);

        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.OpenStorageUI(inventory);
    }
}
```

## File: Features/Kitchen/RefrigeratorInteractable.cs.meta
```
fileFormatVersion: 2
guid: 84fda6109fef4ef489c8165bac5386bd
```

## File: Features/Kitchen/StoveInteractable.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using FeaturesInteraction;






[RequireComponent(typeof(InventoryComponent))]
public class StoveInteractable : KitchenStation, IInteractable
{
    [Header("Recipe Masak (bahan -> makanan siap makan)")]
    [Tooltip("Resep bahan -> hasil + durasi.")]
    [SerializeField] private List<KitchenRecipe> recipes = new List<KitchenRecipe>();

    protected override KitchenRecipe FindRecipeFor(ItemData item, int slotIndex)
    {
        if (item == null)
            return null;

        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null && recipes[i].input == item)
                return recipes[i];
        }

        return null;
    }

    public void Interact(GameObject interactor)
    {
        if (InventoryManagerUI.Instance != null && StationInventory != null)
            InventoryManagerUI.Instance.OpenStorageUI(StationInventory);
    }
}
```

## File: Features/Kitchen/StoveInteractable.cs.meta
```
fileFormatVersion: 2
guid: 71447b0cf8b185344b6fdcc58ac916b2
```

## File: Features/Kitchen/UI.meta
```
fileFormatVersion: 2
guid: 8a42e180867ce004c9108c42ba135a0f
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Time/UI/DayTransitionUI.cs
```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;




public class DayTransitionUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private Text dayText;
    [SerializeField] private float fadeDuration = 1.5f;

    void Awake()
    {

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }
    }

    void OnEnable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged += OnDayChangedHandler;
    }

    void OnDisable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged -= OnDayChangedHandler;
    }

    private void OnDayChangedHandler(int newDay)
    {
        if (transitionCanvasGroup == null)
            return;


        StartCoroutine(TransitionRoutine(newDay));
    }

    private IEnumerator TransitionRoutine(int newDay)
    {
        if (dayText != null)
            dayText.text = "Day " + newDay;


        transitionCanvasGroup.blocksRaycasts = true;

        float half = Mathf.Max(fadeDuration / 2f, 0.05f);


        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transitionCanvasGroup.alpha = Mathf.Clamp01(t / half);
            yield return null;
        }
        transitionCanvasGroup.alpha = 1f;


        yield return new WaitForSeconds(1.5f);


        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transitionCanvasGroup.alpha = Mathf.Clamp01(1f - (t / half));
            yield return null;
        }
        transitionCanvasGroup.alpha = 0f;


        transitionCanvasGroup.blocksRaycasts = false;
    }
}
```

## File: Features/Time/UI/DayTransitionUI.cs.meta
```
fileFormatVersion: 2
guid: 4bcc1dc43c42fd24eb3ca8f6be2a08fc
```

## File: Features/Time/TimeManager.cs
```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;



public class TimeManager : MonoBehaviour
{

    public enum DayPhase { Day, Night }

    private static TimeManager _instance;




    public static TimeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                TimeManager[] found = FindObjectsByType<TimeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                    _instance = found[0];
            }
            return _instance;
        }
        private set { _instance = value; }
    }


    public int currentDay { get; private set; } = 1;
    public DayPhase currentPhase { get; private set; } = DayPhase.Day;


    public event Action<int> OnDayChanged;
    public event Action<DayPhase> OnPhaseChanged;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {

        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            SkipToNight();
    }


    public void AdvanceToNextDay()
    {
        currentDay++;
        currentPhase = DayPhase.Day;

        OnDayChanged?.Invoke(currentDay);
        OnPhaseChanged?.Invoke(currentPhase);

        Debug.Log($"Day changed to {currentDay}, Phase: {currentPhase}");
    }


    public void SkipToNight()
    {
        if (currentPhase == DayPhase.Night)
            return;

        currentPhase = DayPhase.Night;
        OnPhaseChanged?.Invoke(currentPhase);

        Debug.Log($"Phase changed to {currentPhase}");
    }
}
```

## File: Features/Time/TimeManager.cs.meta
```
fileFormatVersion: 2
guid: bb0f0df36f945cf4f86da104e510205c
```

## File: Features/Time/UI.meta
```
fileFormatVersion: 2
guid: 9c458b8235502754a96ec291a50ab25a
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Trophy/TrophyItem.cs
```csharp
using UnityEngine;



[RequireComponent(typeof(Collider))]
public class TrophyItem : MonoBehaviour
{

    public string trophyName = "Unnamed Trophy";
}
```

## File: Features/Trophy/TrophyItem.cs.meta
```
fileFormatVersion: 2
guid: 723145c5f75dbcc4e9f5d565a4c13f19
```

## File: Features/Trophy/TrophyRackVisuals.cs
```csharp
using System.Collections.Generic;
using UnityEngine;








public class TrophyRackVisuals : MonoBehaviour
{
    #region Fields & Properties

    [Tooltip("Inventori rak (Inventory 2) yang menjadi sumber data visual.")]
    [SerializeField] private InventoryComponent rackInventory;

    [Tooltip("Titik SnapPoint 3D (panjangnya harus sama dengan jumlah slot rack).")]
    [SerializeField] private Transform[] snapPoints;


    private readonly Dictionary<int, GameObject> _spawnedModels = new Dictionary<int, GameObject>();


    private readonly Dictionary<int, ItemData> _spawnedItemAt = new Dictionary<int, ItemData>();



    private bool _refreshing;

    #endregion

    #region Lifecycle

    private void OnEnable()
    {
        if (rackInventory == null)
            return;

        rackInventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (rackInventory != null)
            rackInventory.OnInventoryChanged -= Refresh;
    }

    private void OnDestroy()
    {
        DestroyAllModels();
    }

    #endregion

    #region Sync (Data-Driven Listener)




    private void Refresh()
    {
        if (_refreshing || rackInventory == null)
            return;

        _refreshing = true;
        try
        {
            if (rackInventory.slots == null)
                return;

            for (int i = 0; i < rackInventory.slots.Count; i++)
                SyncSlot(i);
        }
        finally
        {
            _refreshing = false;
        }
    }




    private void SyncSlot(int index)
    {
        InventorySlot slot = (index >= 0 && index < rackInventory.slots.Count) ? rackInventory.slots[index] : null;
        ItemData item = (slot != null && !slot.IsEmpty) ? slot.item : null;

        bool hasModel = _spawnedModels.TryGetValue(index, out GameObject model) && model != null;


        if (item != null && item.placeablePrefab != null)
        {

            if (hasModel && _spawnedItemAt.TryGetValue(index, out ItemData renderedItem) && renderedItem == item)
                return;


            if (hasModel)
                DestroyModel(index);

            if (snapPoints == null || index >= snapPoints.Length || snapPoints[index] == null)
                return;

            GameObject created = CreateModel(item.placeablePrefab, snapPoints[index]);
            _spawnedModels[index] = created;
            _spawnedItemAt[index] = item;
        }

        else if (hasModel)
        {
            DestroyModel(index);
        }
    }




    private GameObject CreateModel(GameObject prefab, Transform anchor)
    {
        GameObject model = Instantiate(prefab, anchor);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        if (prefab.transform != null)
            model.transform.localScale = prefab.transform.localScale;
        return model;
    }

    private void DestroyModel(int index)
    {
        if (_spawnedModels.TryGetValue(index, out GameObject model))
        {
            if (model != null)
                Destroy(model);
            _spawnedModels.Remove(index);
            _spawnedItemAt.Remove(index);
        }
    }

    private void DestroyAllModels()
    {
        foreach (GameObject model in _spawnedModels.Values)
        {
            if (model != null)
                Destroy(model);
        }
        _spawnedModels.Clear();
        _spawnedItemAt.Clear();
    }

    #endregion
}
```

## File: Features/Trophy/TrophyRackVisuals.cs.meta
```
fileFormatVersion: 2
guid: c21661a206fb1894b8f2d748485d38bb
```

## File: Features/Trophy/TrophySnapPoint.cs
```csharp
using UnityEngine;








public class TrophySnapPoint : MonoBehaviour
{




    public int slotIndex = -1;
}
```

## File: Features/Trophy/TrophySnapPoint.cs.meta
```
fileFormatVersion: 2
guid: 4f2969f9daa8dc4418b4bf0949954759
```

## File: Features/Trophy/TrophySystemManager.cs
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;




public class TrophySystemManager : MonoBehaviour
{
    private static TrophySystemManager _instance;



    public static TrophySystemManager Instance
    {
        get
        {
            if (_instance == null)
            {
                TrophySystemManager[] found = FindObjectsByType<TrophySystemManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                    _instance = found[0];
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [SerializeField] private Camera mainPlayerCamera;
    [SerializeField] private Camera trophyFirstPersonCamera;
    [SerializeField] private PlayerControl playerControl;


    private const float RaycastDistance = 10f;

    [Header("Pose Kamera Trophy (relatif ke TrophyCabinetSystem root)")]
    [Tooltip("Parent container (TrophyCabinetSystem). Kamera trophy harus child dari root ini.")]
    [SerializeField] private Transform trophySystemRoot;

    [Tooltip("Offset posisi kamera lokal (relative ke trophySystemRoot).")]
    [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-0.15f, 1.5f, 3f);

    [Tooltip("Offset rotasi kamera lokal (Euler, relative ke trophySystemRoot).")]
    [SerializeField] private Vector3 cameraLocalRotation = new Vector3(3f, 0f, 0f);

    [Header("Dual-Inventory Trophy Cabinet")]
    [Tooltip("Inventory 1 (Kabinet): tempat item piala disimpan; target saat piala diambil dari rak.")]
    [SerializeField] private InventoryComponent currentCabinetInventory;

    [Tooltip("Inventory 2 (Rack): sumber kebenaran visual piala yang terpasang di rak.")]
    [SerializeField] private InventoryComponent currentRackInventory;


    public Camera TrophyFirstPersonCamera { get { return trophyFirstPersonCamera; } }


    public InventoryComponent CabinetInventory { get { return currentCabinetInventory; } }
    public InventoryComponent RackInventory { get { return currentRackInventory; } }

    private bool isInTrophyMode = false;


    public bool IsInTrophyMode { get { return isInTrophyMode; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {

        if (!isInTrophyMode)
            return;


        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitTrophyMode();
            return;
        }


        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryCollectTrophy();
    }






    private void TryCollectTrophy()
    {
        if (currentRackInventory == null || currentCabinetInventory == null)
            return;



        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (trophyFirstPersonCamera == null || Mouse.current == null)
            return;

        Ray ray = trophyFirstPersonCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, RaycastDistance, LayerMask.GetMask("SnapPoint")))
            return;

        TrophySnapPoint snap = hit.collider != null ? hit.collider.GetComponent<TrophySnapPoint>() : null;
        if (snap == null || snap.slotIndex < 0)
            return;



        currentRackInventory.TransferItemTo(currentCabinetInventory, snap.slotIndex);
    }

    public void EnterTrophyMode()
    {
        if (isInTrophyMode)
            return;

        isInTrophyMode = true;

        if (playerControl != null)
            playerControl.isInputLocked = true;


        if (mainPlayerCamera != null)
            mainPlayerCamera.enabled = false;

        if (trophyFirstPersonCamera != null)
        {
            trophyFirstPersonCamera.enabled = true;
            trophyFirstPersonCamera.gameObject.SetActive(true);
        }


        AlignTrophyCamera();
        PositionPlayerToCamera();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Masuk First-Person Trophy Mode");
    }






    private void AlignTrophyCamera()
    {
        if (trophyFirstPersonCamera == null)
            return;

        if (trophySystemRoot != null && trophyFirstPersonCamera.transform.parent == trophySystemRoot)
        {

            trophyFirstPersonCamera.transform.localPosition = cameraLocalOffset;
            trophyFirstPersonCamera.transform.localRotation = Quaternion.Euler(cameraLocalRotation);
            Debug.Log($"Trophy cam pose (local): pos={cameraLocalOffset} rot={cameraLocalRotation}");
        }
        else if (trophySystemRoot != null)
        {

            Vector3 worldPos = trophySystemRoot.TransformPoint(cameraLocalOffset);
            Quaternion worldRot = trophySystemRoot.rotation * Quaternion.Euler(cameraLocalRotation);
            trophyFirstPersonCamera.transform.SetPositionAndRotation(worldPos, worldRot);
            Debug.Log($"Trophy cam pose (world from root): pos={worldPos} rot={worldRot.eulerAngles}");
        }
        else
        {

            Debug.LogWarning("TrophySystemManager: trophySystemRoot is null, camera position unchanged.");
        }
    }





    private void PositionPlayerToCamera()
    {

        if (playerControl == null)
            playerControl = FindFirstObjectByType<PlayerControl>();

        if (playerControl == null || trophyFirstPersonCamera == null)
            return;

        Vector3 behind = trophyFirstPersonCamera.transform.position
                         - trophyFirstPersonCamera.transform.forward * 0.4f;


        if (Physics.Raycast(behind, Vector3.down, out RaycastHit hit, 5f))
            behind.y = hit.point.y + 1f;

        Rigidbody rb = playerControl.GetComponent<Rigidbody>();
        if (rb != null)
        {


            rb.linearVelocity = Vector3.zero;
            rb.position = behind;
        }
        else
        {
            playerControl.transform.position = behind;
        }

        Debug.Log($"TrophyMode: kamera@{trophyFirstPersonCamera.transform.position}, player -> {behind}");
    }

    public void ExitTrophyMode()
    {
        if (!isInTrophyMode)
            return;

        isInTrophyMode = false;

        if (playerControl != null)
            playerControl.isInputLocked = false;


        if (trophyFirstPersonCamera != null)
            trophyFirstPersonCamera.enabled = false;

        if (mainPlayerCamera != null)
            mainPlayerCamera.enabled = true;


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        if (InventoryManagerUI.Instance != null)
            InventoryManagerUI.Instance.CloseAllUI();

        Debug.Log("Keluar dari Trophy Mode");
    }
}
```

## File: Features/Trophy/TrophySystemManager.cs.meta
```
fileFormatVersion: 2
guid: 14bc6f098f9030b439ab892407458f24
```

## File: Features/Wardrobe/Data/Casual.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b8764bebebf62094ba280a34b2083467, type: 3}
  m_Name: Casual
  m_EditorClassIdentifier: Assembly-CSharp::FeaturesWardrobe.OutfitData
  outfitName: Casual
  icon: {fileID: 0}
  fullBodyPrefab: {fileID: 2689986709966026323, guid: a2c886a201499a34197d540bd558c672, type: 3}
  description:
```

## File: Features/Wardrobe/Data/Casual.asset.meta
```
fileFormatVersion: 2
guid: 3b4b37d93654cd747a7bcbfcc0214e86
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Wardrobe/Data/Formal.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b8764bebebf62094ba280a34b2083467, type: 3}
  m_Name: Formal
  m_EditorClassIdentifier: Assembly-CSharp::FeaturesWardrobe.OutfitData
  outfitName: Formal
  icon: {fileID: 0}
  fullBodyPrefab: {fileID: 5388317238291384574, guid: 50571ef510719c34abfbd8591676fdbf, type: 3}
  description:
```

## File: Features/Wardrobe/Data/Formal.asset.meta
```
fileFormatVersion: 2
guid: ba80c7db04281b740a2e2d6cb07bc3c2
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Wardrobe/Data/Sleepwear.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b8764bebebf62094ba280a34b2083467, type: 3}
  m_Name: Sleepwear
  m_EditorClassIdentifier: Assembly-CSharp::FeaturesWardrobe.OutfitData
  outfitName: Sleepwear
  icon: {fileID: 0}
  fullBodyPrefab: {fileID: 9090147172062824752, guid: 5e852293450d31744aa48ab82d539f88, type: 3}
  description:
```

## File: Features/Wardrobe/Data/Sleepwear.asset.meta
```
fileFormatVersion: 2
guid: bc0ff941d45b2f8499aa2dabf8ddfcb7
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Wardrobe/Data/Workwear.asset
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b8764bebebf62094ba280a34b2083467, type: 3}
  m_Name: Workwear
  m_EditorClassIdentifier: Assembly-CSharp::FeaturesWardrobe.OutfitData
  outfitName: Workwear
  icon: {fileID: 0}
  fullBodyPrefab: {fileID: 4735670894764622563, guid: 430aa25cec2ac3f4d9a2345b6bd13dcf, type: 3}
  description:
```

## File: Features/Wardrobe/Data/Workwear.asset.meta
```
fileFormatVersion: 2
guid: 6cb1ba36c65fd29488dca802fc1046a7
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Wardrobe/UI/WardrobeUI.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesWardrobe
{
    public class WardrobeUI : MonoBehaviour
    {
        [Header("Mirror Display")]
        [SerializeField] private RawImage mirrorRawImage;
        [SerializeField] private MirrorCamera mirrorCamera;
        [SerializeField] private RenderTexture mirrorRenderTexture;

        [Header("Outfit Grid")]
        [SerializeField] private Transform outfitGrid;
        [SerializeField] private GameObject outfitButtonPrefab;

        [Header("Current Preview")]
        [SerializeField] private Image currentPreviewImage;

        [Header("Actions")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;

        [Header("Visual")]
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color normalColor = Color.white;

        private List<Button> outfitButtons = new List<Button>();
        private List<OutfitData> outfitSlotData = new List<OutfitData>();
        private OutfitData currentlySelectedOutfit;

        private void Awake()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnEnable()
        {

            RefreshMirrorTexture();
        }

        private void Start()
        {

            RefreshMirrorTexture();


            if (WardrobeManager.Instance != null && WardrobeManager.Instance.PlayerOutfitProp != null)
            {
                var outfit = WardrobeManager.Instance.PlayerOutfitProp;
                outfit.OnOutfitChanged += OnOutfitChanged;
                outfit.OnPreviewChanged += OnPreviewChanged;
            }

            BuildOutfitGrid();
            UpdateCurrentPreview();
        }



        private void RefreshMirrorTexture()
        {
            if (mirrorRawImage == null)
            {
                Debug.LogWarning("[WardrobeUI] mirrorRawImage null, tidak bisa assign texture.");
                return;
            }

            if (mirrorCamera == null)
                mirrorCamera = FindFirstObjectByType<MirrorCamera>(FindObjectsInactive.Include);

            if (mirrorCamera == null)
            {
                Debug.LogWarning("[WardrobeUI] MirrorCamera tidak ditemukan -> RawImage tetap putih. Wire field 'mirrorCamera' di scene.");
                return;
            }

            if (mirrorCamera.MirrorTexture == null)
            {
                Debug.LogWarning("[WardrobeUI] MirrorCamera.MirrorTexture masih null -> RawImage tetap putih.");
                return;
            }


            mirrorRawImage.texture = mirrorCamera.MirrorTexture;
            Debug.Log("[WardrobeUI] MirrorRawImage texture assigned: " + mirrorCamera.MirrorTexture.name);
        }



        public void ForceRefreshMirror()
        {
            RefreshMirrorTexture();
        }


        public Texture MirrorTextureSource =>
            mirrorRenderTexture != null ? mirrorRenderTexture : (mirrorCamera != null ? mirrorCamera.MirrorTexture : null);

        private void Update()
        {


            if (mirrorRawImage != null && mirrorRawImage.texture != MirrorTextureSource)
            {
                Texture current = MirrorTextureSource;
                if (current != null)
                {
                    mirrorRawImage.texture = current;
                    Debug.Log("[WardrobeUI] Update: Re-assigned mirror texture: " + current.name);
                }
            }
        }

        private void OnDestroy()
        {
            if (WardrobeManager.Instance != null && WardrobeManager.Instance.PlayerOutfitProp != null)
            {
                var outfit = WardrobeManager.Instance.PlayerOutfitProp;
                outfit.OnOutfitChanged -= OnOutfitChanged;
                outfit.OnPreviewChanged -= OnPreviewChanged;
            }
        }

        private void BuildOutfitGrid()
        {
            if (outfitGrid == null || outfitButtonPrefab == null) return;


            foreach (var btn in outfitButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            outfitButtons.Clear();
            outfitSlotData.Clear();

            var outfit = WardrobeManager.Instance?.PlayerOutfitProp;
            if (outfit == null) return;


            outfitButtons.Add(CreateOutfitButton(null, "Default", outfit));
            outfitSlotData.Add(null);

            for (int i = 0; i < outfit.unlockedOutfits.Count; i++)
            {
                var data = outfit.unlockedOutfits[i];
                if (data == null) continue;
                outfitButtons.Add(CreateOutfitButton(data, data.outfitName, outfit));
                outfitSlotData.Add(data);
            }

            SelectOutfitButton(outfit.currentOutfit);
        }

        private Button CreateOutfitButton(OutfitData data, string label, PlayerOutfit outfit)
        {
            var btnGO = Instantiate(outfitButtonPrefab, outfitGrid);


            btnGO.SetActive(true);
            var btn = btnGO.GetComponent<Button>();
            var img = btnGO.GetComponentInChildren<Image>();
            var txt = btnGO.GetComponentInChildren<Text>();

            if (img != null && data != null && data.icon != null)
                img.sprite = data.icon;

            if (txt != null)
                txt.text = label;

            OutfitData captured = data;
            btn.onClick.AddListener(() =>
            {
                if (captured == null)
                    WardrobeManager.Instance.PreviewDefault();
                else
                    WardrobeManager.Instance.TryOnOutfit(captured);
                SelectOutfitButton(captured);
            });

            return btn;
        }

        private void SelectOutfitButton(OutfitData outfit)
        {
            currentlySelectedOutfit = outfit;

            for (int i = 0; i < outfitButtons.Count; i++)
            {
                var btn = outfitButtons[i];
                var img = btn.GetComponentInChildren<Image>();
                if (img == null) continue;


                bool isSelected = i < outfitSlotData.Count && outfitSlotData[i] == outfit;
                img.color = isSelected ? selectedColor : normalColor;
            }
        }

        private void OnOutfitChanged(OutfitData outfit)
        {
            UpdateCurrentPreview();

            SelectOutfitButton(outfit);
        }

        private void OnPreviewChanged(OutfitData outfit)
        {


            if (currentPreviewImage != null && outfit != null && outfit.icon != null)
                currentPreviewImage.sprite = outfit.icon;


            SelectOutfitButton(outfit);
        }

        private void UpdateCurrentPreview()
        {
            var outfit = WardrobeManager.Instance?.PlayerOutfitProp?.currentOutfit;
            if (currentPreviewImage != null && outfit != null && outfit.icon != null)
                currentPreviewImage.sprite = outfit.icon;
        }

        private void OnSaveClicked()
        {
            WardrobeManager.Instance.CommitOutfit();
            WardrobeManager.Instance.ExitWardrobeMode();
        }

        private void OnCancelClicked()
        {
            WardrobeManager.Instance.RevertOutfit();
            WardrobeManager.Instance.ExitWardrobeMode();
        }
    }
}
```

## File: Features/Wardrobe/UI/WardrobeUI.cs.meta
```
fileFormatVersion: 2
guid: 31e7fc495cdebe24e8a12f5e6e4793b3
```

## File: Features/Wardrobe/Data.meta
```
fileFormatVersion: 2
guid: c63f36e38390b5a438cbc12ba4279c5b
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Wardrobe/MirrorCamera.cs
```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FeaturesWardrobe
{
    [RequireComponent(typeof(Camera))]
    public class MirrorCamera : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Camera mirrorCamera;
        [SerializeField] private RenderTexture mirrorTexture;
        [SerializeField] private Renderer surfaceRenderer;
        [SerializeField] private int textureSize = 1024;

        [Header("Target")]
        [SerializeField] private Transform playerTarget;

        [Header("Positioning")]
        [Tooltip("Jarak kamera dari permukaan cermin (dalam satuan dunia).")]
        [SerializeField] private float distanceFromMirror = 1.8f;
        [Tooltip("Offset vertikal dari posisi player (untuk face ke wajah).")]
        [SerializeField] private float verticalOffset = 0.1f;
        [Tooltip("Tinggi bidikan minimal di atas titik target (agar tidak membidik kaki).")]
        [SerializeField] private float aimHeightOffset = 1.25f;
        [Tooltip("Transform permukaan cermin (static) - kamera diposisikan relatif ke ini")]
        [SerializeField] private Transform mirrorSurface;

        public RenderTexture MirrorTexture => mirrorTexture;
        public Camera MirrorCameraComponent => mirrorCamera ? mirrorCamera : GetComponent<Camera>();
        public Transform MirrorSurface => mirrorSurface;

        private bool isInitialized;

        private void Awake()
        {

            mirrorCamera = GetComponent<Camera>();


            mirrorCamera.depth = -100;
            mirrorCamera.clearFlags = CameraClearFlags.SolidColor;
            mirrorCamera.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 1f);
            mirrorCamera.fieldOfView = 60f;
            mirrorCamera.nearClipPlane = 0.1f;
            mirrorCamera.farClipPlane = 100f;
            mirrorCamera.useOcclusionCulling = true;
            mirrorCamera.allowHDR = false;
            mirrorCamera.allowMSAA = true;

            var uacd = mirrorCamera.GetComponent<UniversalAdditionalCameraData>();
            if (uacd != null)
            {
                uacd.renderType = CameraRenderType.Base;
                if (uacd.cameraStack != null && uacd.cameraStack.Count > 0)
                {
                    uacd.cameraStack.Clear();
                }
            }

            InitializeRenderTexture();
            ConfigureCamera();
            BindSurfaceTexture();
        }

        private void InitializeRenderTexture()
        {


            if (mirrorTexture == null)
            {
                mirrorTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
                mirrorTexture.name = "WardrobeMirrorTexture";
                mirrorTexture.filterMode = FilterMode.Bilinear;
                mirrorTexture.wrapMode = TextureWrapMode.Clamp;
                mirrorTexture.Create();
            }

            if (mirrorCamera != null)
                mirrorCamera.targetTexture = mirrorTexture;
        }

        private void ConfigureCamera()
        {
            if (mirrorCamera == null) return;




        }


        public void EnsureInitialized()
        {
            if (mirrorTexture == null)
            {
                InitializeRenderTexture();
                return;
            }
            if (mirrorCamera != null && mirrorCamera.targetTexture != mirrorTexture)
                mirrorCamera.targetTexture = mirrorTexture;
            BindSurfaceTexture();
        }


        private void BindSurfaceTexture()
        {
            if (surfaceRenderer == null || mirrorTexture == null) return;
            surfaceRenderer.material.mainTexture = mirrorTexture;
        }

        private void LateUpdate()
        {

            if (mirrorCamera != null && mirrorCamera.enabled && mirrorCamera.targetTexture == null && mirrorTexture != null)
            {
                mirrorCamera.targetTexture = mirrorTexture;
                Debug.LogWarning("[MirrorCamera] LateUpdate: Camera was enabled without targetTexture! Re-bound and keeping enabled.");
            }


            if (mirrorCamera != null && mirrorCamera.enabled && mirrorCamera.targetTexture == null)
            {
                mirrorCamera.enabled = false;
                Debug.LogError("[MirrorCamera] LateUpdate: No targetTexture available! Disabling camera to prevent screen rendering.");
                return;
            }

            if (!isInitialized || mirrorCamera == null || playerTarget == null || mirrorSurface == null)
                return;



            Vector3 mirrorForward = -mirrorSurface.forward;
            Vector3 cameraPosition = mirrorSurface.position + mirrorForward * distanceFromMirror;

            mirrorCamera.transform.position = cameraPosition;


            float aimHeight = Mathf.Max(verticalOffset, aimHeightOffset);
            Vector3 lookTarget = playerTarget.position + Vector3.up * aimHeight;
            Vector3 direction = (lookTarget - cameraPosition).normalized;


            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            mirrorCamera.transform.rotation = targetRotation;
        }

        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
            isInitialized = true;
        }

        public void SetTextureSize(int size)
        {
            if (size == textureSize) return;
            textureSize = size;
            InitializeRenderTexture();
        }

        private void OnDestroy()
        {
            if (mirrorTexture != null)
            {
                mirrorTexture.Release();
                mirrorTexture = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mirrorCamera == null)
                mirrorCamera = GetComponent<Camera>();
        }
#endif
    }
}
```

## File: Features/Wardrobe/MirrorCamera.cs.meta
```
fileFormatVersion: 2
guid: 882c98b44d0f15649b5f431551912973
```

## File: Features/Wardrobe/OutfitData.cs
```csharp
using UnityEngine;

namespace FeaturesWardrobe
{
    [CreateAssetMenu(fileName = "NewOutfit", menuName = "Wardrobe/Outfit Data")]
    public class OutfitData : ScriptableObject
    {
        [Header("Identity")]
        public string outfitName;
        public Sprite icon;

        [Header("Visual")]
        [Tooltip("Full-body prefab (body + clothes combined). Di-spawn ke PlayerOutfit.outfitRoot.")]
        public GameObject fullBodyPrefab;

        [Header("Optional Metadata")]
        [TextArea] public string description;
    }
}
```

## File: Features/Wardrobe/OutfitData.cs.meta
```
fileFormatVersion: 2
guid: b8764bebebf62094ba280a34b2083467
```

## File: Features/Wardrobe/PlayerOutfit.cs
```csharp
using UnityEngine;
using System.Collections.Generic;

namespace FeaturesWardrobe
{
    public class PlayerOutfit : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Root transform untuk spawn model outfit (child of Player, posisi relatif ke body).")]
        [SerializeField] private Transform outfitRoot;

        [Header("Runtime State")]
        [Tooltip("Outfit yang sedang dipakai (persisted).")]
        public OutfitData currentOutfit;

        [Tooltip("Koleksi outfit yang sudah di-unlock (cosmetic wardrobe).")]
        public List<OutfitData> unlockedOutfits = new List<OutfitData>();


        private OutfitData previewOutfit;
        private GameObject previewModel;
        private GameObject currentModel;
        private bool previewingDefault;


        public event System.Action<OutfitData> OnOutfitChanged;
        public event System.Action<OutfitData> OnPreviewChanged;


        public OutfitData CurrentOutfit => currentOutfit;
        public OutfitData PreviewOutfit => previewOutfit;
        public bool IsPreviewing => previewOutfit != null && previewOutfit != currentOutfit;
        public Transform OutfitRoot => outfitRoot;

        private void Awake()
        {
            if (outfitRoot == null)
            {

                outfitRoot = new GameObject("OutfitRoot").transform;
                outfitRoot.SetParent(transform, false);
                outfitRoot.localPosition = Vector3.zero;
                outfitRoot.localRotation = Quaternion.identity;
            }



        }


        public void TryOn(OutfitData outfit)
        {
            if (outfit == null || !unlockedOutfits.Contains(outfit))
                return;

            previewingDefault = false;
            previewOutfit = outfit;
            RefreshPreview();
            OnPreviewChanged?.Invoke(outfit);
        }


        public void PreviewDefault()
        {
            DestroyPreviewModel();
            previewOutfit = null;
            previewingDefault = true;
            OnPreviewChanged?.Invoke(currentOutfit);
        }


        public void Commit()
        {
            if (previewOutfit != null && previewOutfit != currentOutfit)
            {
                currentOutfit = previewOutfit;
                ApplyOutfit(currentOutfit);
                previewOutfit = null;
                previewModel = null;
                previewingDefault = false;
                OnOutfitChanged?.Invoke(currentOutfit);
                return;
            }


            if (previewingDefault && currentOutfit != null)
            {
                currentOutfit = null;
                DestroyCurrentModel();
                previewingDefault = false;
                OnOutfitChanged?.Invoke(null);
            }
        }


        public void Revert()
        {
            if (previewOutfit == null)
                return;

            previewOutfit = null;
            DestroyPreviewModel();
            OnPreviewChanged?.Invoke(currentOutfit);
        }


        private void ApplyOutfit(OutfitData outfit)
        {
            if (outfit == null || outfit.fullBodyPrefab == null)
            {
                Debug.LogWarning($"[PlayerOutfit] OutfitData atau fullBodyPrefab null: {outfit?.outfitName}");
                return;
            }

            DestroyCurrentModel();

            GameObject spawned = Instantiate(outfit.fullBodyPrefab, outfitRoot);
            spawned.transform.localPosition = Vector3.zero;
            spawned.transform.localRotation = Quaternion.identity;


            spawned.transform.localScale = outfit.fullBodyPrefab.transform.localScale;

            currentModel = spawned;
        }

        private void RefreshPreview()
        {
            if (previewOutfit == null || previewOutfit.fullBodyPrefab == null)
            {
                DestroyPreviewModel();
                return;
            }

            DestroyPreviewModel();

            GameObject spawned = Instantiate(previewOutfit.fullBodyPrefab, outfitRoot);
            spawned.transform.localPosition = Vector3.zero;
            spawned.transform.localRotation = Quaternion.identity;
            spawned.transform.localScale = previewOutfit.fullBodyPrefab.transform.localScale;


            var renderers = spawned.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {

                    if (mats[i] == null) continue;

                    var mat = new Material(mats[i]);
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        mat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 0.9f));
                    }
                    if (mat.HasProperty("_Color"))
                    {
                        var c = mat.GetColor("_Color");
                        mat.SetColor("_Color", new Color(c.r, c.g, c.b, 0.9f));
                    }
                    mats[i] = mat;
                }
                r.materials = mats;
            }

            previewModel = spawned;
        }

        private void DestroyCurrentModel()
        {
            if (currentModel != null)
            {
                Destroy(currentModel);
                currentModel = null;
            }
        }

        private void DestroyPreviewModel()
        {
            if (previewModel != null)
            {
                Destroy(previewModel);
                previewModel = null;
            }
        }

        private void OnDestroy()
        {
            DestroyCurrentModel();
            DestroyPreviewModel();
        }
    }
}
```

## File: Features/Wardrobe/PlayerOutfit.cs.meta
```
fileFormatVersion: 2
guid: 1a857833f11c2084d86afc9c8bf97847
```

## File: Features/Wardrobe/UI.meta
```
fileFormatVersion: 2
guid: 4c3d2208a865c93429b82de77637ae12
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Wardrobe/WardrobeManager.cs
```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace FeaturesWardrobe
{
    public class WardrobeManager : MonoBehaviour
    {
        #region Singleton
        private static WardrobeManager _instance;
        public static WardrobeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var found = FindObjectsByType<WardrobeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (found != null && found.Length > 0)
                        _instance = found[0];
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        #endregion

        [Header("Cameras")]
        [Tooltip("Main player camera (isometric).")]
        [SerializeField] private Camera mainPlayerCamera;

        [Tooltip("Dedicated wardrobe camera (child of wardrobeRoot).")]
        [SerializeField] private Camera wardrobeCamera;

        [Tooltip("MirrorCamera component on the Mirror GameObject.")]
        [SerializeField] private MirrorCamera mirrorCamera;

        [Header("Positioning (Relative to WardrobeRoot)")]
        [Tooltip("Parent container (WardrobeRoot). Camera & Mirror should be children.")]
        [SerializeField] private Transform wardrobeRoot;

        [Tooltip("Local position offset of wardrobeCamera relative to wardrobeRoot.")]
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-2.5f, 1.6f, -2f);

        [Tooltip("Local rotation offset (Euler) of wardrobeCamera relative to wardrobeRoot.")]
        [SerializeField] private Vector3 cameraLocalRotation = new Vector3(8f, 85f, 0f);

        [Header("Mirror Positioning")]
        [Tooltip("Jarak player dari permukaan cermin saat buka wardrobe.")]
        [SerializeField] private float playerMirrorDistance = 5.0f;

        [Header("Animation")]
        [Tooltip("Durasi camera blend (detik).")]
        [SerializeField] private float cameraBlendDuration = 0.6f;

        [Tooltip("Durasi UI fade in/out (detik).")]
        [SerializeField] private float uiFadeDuration = 0.3f;

        [Header("Player & Outfit")]
        [SerializeField] private PlayerControl playerControl;

        [SerializeField] private PlayerOutfit playerOutfit;

        public PlayerOutfit PlayerOutfitProp => playerOutfit;
        [SerializeField] private Transform playerHead;

        [Header("UI")]
        [SerializeField] private GameObject wardrobeUIPanel;
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private WardrobeUI wardrobeUI;

        [Header("Debug")]
        [SerializeField] private bool debugCameraAudit = true;

        private bool isInWardrobeMode;
        private Coroutine blendCoroutine;
        private Vector3 playerOriginalPosition;
        private Quaternion playerOriginalRotation;
        private bool mainCameraWasEnabled;
        private bool wardrobeCameraWasEnabled;
        private bool isometricCameraWasEnabled;

        #region Public API

        public bool IsInWardrobeMode => isInWardrobeMode;

        public void EnterWardrobeMode()
        {
            if (isInWardrobeMode) return;

            isInWardrobeMode = true;

            playerOriginalPosition = playerControl.transform.position;
            playerOriginalRotation = playerControl.transform.rotation;
            mainCameraWasEnabled = mainPlayerCamera.enabled;
            wardrobeCameraWasEnabled = wardrobeCamera.enabled;


            var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
            if (isoCam != null)
            {
                isometricCameraWasEnabled = isoCam.enabled;
                isoCam.enabled = false;
                Debug.Log("[WardrobeManager] IsometricCamera disabled during wardrobe mode");
            }

            playerControl.isInputLocked = true;

            SetupWardrobeCamera();


            if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
            {
                mirrorCamera.MirrorCameraComponent.enabled = false;
                Debug.Log("[WardrobeManager] MirrorInnerCam disabled before Enter sequence");
            }



            if (mirrorCamera != null)
            {
                mirrorCamera.EnsureInitialized();
                if (playerHead != null)
                    mirrorCamera.SetPlayerTarget(playerHead);
            }

            if (blendCoroutine != null) StopCoroutine(blendCoroutine);
            blendCoroutine = StartCoroutine(BlendCamerasAndUI(true));

            PositionPlayerToMirror();


            if (debugCameraAudit)
            {
                StartCoroutine(AuditCamerasDuringBlend());
            }

            if (wardrobeUIPanel != null)
                wardrobeUIPanel.SetActive(true);

            SetUIRaycastBlocking(true);
            SetCursorFree(true);

            if (wardrobeUI != null) wardrobeUI.ForceRefreshMirror();
            LogMirrorDiagnostics();

            Debug.Log("[WardrobeManager] Entered Wardrobe Mode");
        }

        public void ExitWardrobeMode()
        {
            if (!isInWardrobeMode) return;

            if (playerOutfit != null && playerOutfit.IsPreviewing)
                playerOutfit.Revert();

            isInWardrobeMode = false;

            if (playerControl != null)
                playerControl.isInputLocked = false;

            if (blendCoroutine != null) StopCoroutine(blendCoroutine);
            blendCoroutine = StartCoroutine(BlendCamerasAndUI(false));

            if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
            if (wardrobeUIPanel != null) wardrobeUIPanel.SetActive(false);

            SetUIRaycastBlocking(false);
            SetCursorFree(false);


            var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
            if (isoCam != null)
            {
                isoCam.enabled = isometricCameraWasEnabled;
                Debug.Log($"[WardrobeManager] IsometricCamera restored: {isometricCameraWasEnabled}");
            }

            Debug.Log("[WardrobeManager] Exited Wardrobe Mode");
        }

        public void TryOnOutfit(OutfitData outfit)
        {
            if (outfit == null || playerOutfit == null) return;
            playerOutfit.TryOn(outfit);
        }

        public void PreviewDefault()
        {
            if (playerOutfit != null)
                playerOutfit.PreviewDefault();
        }

        public void CommitOutfit()
        {
            if (playerOutfit != null)
                playerOutfit.Commit();
        }

        public void RevertOutfit()
        {
            if (playerOutfit != null)
                playerOutfit.Revert();
        }

        private void SetUIRaycastBlocking(bool enabled)
        {
            if (uiCanvasGroup == null) return;
            uiCanvasGroup.interactable = enabled;
            uiCanvasGroup.blocksRaycasts = enabled;
        }

        private void SetCursorFree(bool free)
        {
            Cursor.visible = free;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void LogMirrorDiagnostics()
        {
            Texture rt = wardrobeUI != null ? wardrobeUI.MirrorTextureSource : null;
            bool mirrorReady = mirrorCamera != null && mirrorCamera.MirrorTexture != null;
            bool innerCamOn = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null && mirrorCamera.MirrorCameraComponent.enabled;
            bool targetOk = mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null &&
                            mirrorCamera.MirrorCameraComponent.targetTexture == mirrorCamera.MirrorTexture;
            Debug.Log($"[Wardrobe] diag -> RawImage.texture={(rt != null ? rt.name : "NULL")} " +
                      $"| MirrorTexture={(mirrorReady ? "OK" : "NULL")} " +
                      $"| InnerCam.enabled={innerCamOn} " +
                      $"| targetTexture==RT={targetOk}");
        }

        #endregion

        #region Camera & UI Blend

        private void SetupWardrobeCamera()
        {
            if (wardrobeCamera == null || wardrobeRoot == null) return;

            wardrobeCamera.transform.SetParent(wardrobeRoot, false);
            wardrobeCamera.transform.localPosition = cameraLocalOffset;
            wardrobeCamera.transform.localRotation = Quaternion.Euler(cameraLocalRotation);
            wardrobeCamera.enabled = false;
        }

        private IEnumerator BlendCamerasAndUI(bool entering)
        {
            float startUIAlpha = entering ? 0f : 1f;
            float targetUIAlpha = entering ? 1f : 0f;
            float elapsed = 0f;

            if (entering)
            {
                // STRICT ORDER: Disable main camera FIRST
                mainPlayerCamera.enabled = false;

                var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
                if (isoCam != null)
                {
                    isoCam.enabled = false;
                    Debug.Log("[WardrobeManager] IsometricCamera disabled (blend start)");
                }

                // Enable WardrobeCamera (screen)
                wardrobeCamera.enabled = true;
                Debug.Log("[WardrobeManager] WardrobeCamera enabled (screen)");

                // Bind targetTexture BEFORE enabling MirrorInnerCam
                if (mirrorCamera != null && mirrorCamera.MirrorTexture != null)
                {
                    mirrorCamera.MirrorCameraComponent.targetTexture = mirrorCamera.MirrorTexture;
                    Debug.Log("[WardrobeManager] MirrorInnerCam targetTexture bound: " + mirrorCamera.MirrorTexture.name);
                }

                // NOW enable MirrorInnerCam (renders to texture)
                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
                {
                    mirrorCamera.MirrorCameraComponent.enabled = true;
                    Debug.Log("[WardrobeManager] MirrorInnerCam enabled (renders to RT)");
                }

                if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;

                // Force UI RawImage update after cameras are set
                if (wardrobeUI != null) wardrobeUI.ForceRefreshMirror();
            }
            else
            {
                // EXIT ORDER: Disable MirrorInnerCam FIRST
                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null)
                {
                    mirrorCamera.MirrorCameraComponent.enabled = false;
                    Debug.Log("[WardrobeManager] MirrorInnerCam disabled before exit");
                }


                wardrobeCamera.enabled = false;


                mainPlayerCamera.enabled = true;

                var isoCam = mainPlayerCamera.GetComponent<IsometricCamera>();
                if (isoCam != null)
                {
                    isoCam.enabled = true;
                    Debug.Log("[WardrobeManager] IsometricCamera re-enabled (blend end)");
                }


                if (mirrorCamera != null && mirrorCamera.MirrorTexture != null)
                {
                    mirrorCamera.MirrorCameraComponent.targetTexture = mirrorCamera.MirrorTexture;
                    mirrorCamera.MirrorCameraComponent.enabled = true;
                    Debug.Log("[WardrobeManager] MirrorInnerCam re-enabled for mirror surface");
                }
            }


            Coroutine auditCoroutine = null;
            if (debugCameraAudit)
            {
                auditCoroutine = StartCoroutine(AuditCamerasDuringBlend());
            }

            while (elapsed < cameraBlendDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / cameraBlendDuration);
                float easedT = EaseInOutCubic(t);



                if (uiCanvasGroup != null)
                    uiCanvasGroup.alpha = Mathf.Lerp(startUIAlpha, targetUIAlpha, EaseInOutCubic(t));

                yield return null;
            }

            mainPlayerCamera.enabled = !entering;
            wardrobeCamera.enabled = entering;

            if (uiCanvasGroup != null)
                uiCanvasGroup.alpha = entering ? 1f : 0f;

            if (!entering && wardrobeUIPanel != null)
                wardrobeUIPanel.SetActive(false);

            if (entering && wardrobeUI != null)
                wardrobeUI.ForceRefreshMirror();

            blendCoroutine = null;
        }

        private IEnumerator AuditCamerasDuringBlend()
        {
            for (int i = 0; i < 20; i++)
            {
                int enabledCount = 0;
                foreach (var cam in Camera.allCameras)
                {
                    if (cam.enabled)
                    {
                        enabledCount++;
                        var uacd = cam.GetComponent<UniversalAdditionalCameraData>();
                        Debug.Log($"[AUDIT] Active Camera: {cam.name}, depth={cam.depth}, targetTexture={cam.targetTexture?.name ?? "screen"}, renderType={cam.GetComponent<UniversalAdditionalCameraData>()?.renderType}");
                    }
                }
                Debug.Log($"[AUDIT] Total enabled cameras: {enabledCount} (expected: 2 during wardrobe - WardrobeCamera + MirrorInnerCam)");
                yield return new WaitForSeconds(0.05f);
            }
        }

        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        private void PositionPlayerToMirror()
        {
            if (playerControl == null || mirrorCamera == null) return;

            // Use MirrorSurface from MirrorCamera (static mirror surface)
            Transform mirrorSurface = mirrorCamera.MirrorSurface;
            if (mirrorSurface == null)
            {
                Debug.LogError("[WardrobeManager] MirrorSurface is null on MirrorCamera!");
                return;
            }

            Vector3 faceNormal = mirrorSurface.forward;
            Vector3 target = mirrorSurface.position + faceNormal * playerMirrorDistance;

            // Raycast to find floor height
            bool hitFloor = Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f);
            if (hitFloor)
            {
                target.y = hit.point.y + 0.5f;
            }
            else
            {
                // FALLBACK: Use known bedroom floor Y (0) + offset
                // Bedroom floor is at Y=0, place player at 0.5f above
                target.y = 0.5f;
                Debug.LogWarning("[WardrobeManager] PositionPlayerToMirror: Raycast failed! Using fallback Y=0.5f");
            }

            Quaternion facingMirror = Quaternion.LookRotation(-faceNormal, Vector3.up);

            Rigidbody rb = playerControl.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.position = target;
                rb.rotation = facingMirror;
            }
            else
            {
                playerControl.transform.position = target;
                playerControl.transform.rotation = facingMirror;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!isInWardrobeMode) return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ExitWardrobeMode();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion
    }
}
```

## File: Features/Wardrobe/WardrobeManager.cs.meta
```
fileFormatVersion: 2
guid: 60da3e8797e8cd5439fe39aec35dfa24
```

## File: Features/Camera.meta
```
fileFormatVersion: 2
guid: bb82829fa83c85142a509bf9963da5fe
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Interaction.meta
```
fileFormatVersion: 2
guid: ed4aed906f66cac468591da28fe60ac6
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Inventory.meta
```
fileFormatVersion: 2
guid: 11f869df13e998249964a47170396140
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Kitchen.meta
```
fileFormatVersion: 2
guid: 6561c60db1bc9d84784b19aebd29a2ed
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Time.meta
```
fileFormatVersion: 2
guid: 1e563c04238eb8b4a9ec206277f355f3
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Trophy.meta
```
fileFormatVersion: 2
guid: 59be3cf3c110fec4384e14d943e6f919
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Features/Wardrobe.meta
```
fileFormatVersion: 2
guid: 1eafea8151c3ba049892772f1ed59c84
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Player/UI/PlayerHealthUI.cs
```csharp
using UnityEngine;
using UnityEngine.UI;




public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthSlider;

    private void OnEnable()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged += UpdateHealthVisual;
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= UpdateHealthVisual;
    }

    private void UpdateHealthVisual(int current, int max)
    {
        if (healthSlider == null)
            return;

        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}
```

## File: Player/UI/PlayerHealthUI.cs.meta
```
fileFormatVersion: 2
guid: e1f48fde28f5c8f4ea08ed2eec869fec
```

## File: Player/PlayerControl.cs
```csharp
using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 7f;
    public float turnSpeed = 15f;

    [Header("Pengaturan Aksi")]
    public float jumpForce = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 inputVector;
    private PlayerInputActions inputActions;
    private PlayerInteractor interactor;
    private InventoryComponent playerInventory;


    private bool isGrounded;
    private bool isDashing;
    private float lastDashTime = -100f;



    public bool isInputLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (inputActions == null)
            inputActions = new PlayerInputActions();

        interactor = GetComponent<PlayerInteractor>();
        playerInventory = GetComponent<InventoryComponent>();
    }

    void OnEnable()
    {

        if (inputActions == null)
            inputActions = new PlayerInputActions();

        inputActions.Player.Enable();


        inputActions.Player.Jump.performed += ctx => ExecuteJump();
        inputActions.Player.Dash.performed += ctx => StartCoroutine(ExecuteDash());
        inputActions.Player.Interact.performed += OnInteractPressed;
    }

    void OnDisable()
    {

        if (inputActions == null)
            inputActions = new PlayerInputActions();


        inputActions.Player.Jump.performed -= ctx => ExecuteJump();
        inputActions.Player.Dash.performed -= ctx => StartCoroutine(ExecuteDash());

        inputActions.Player.Disable();

        inputActions.Player.Interact.performed -= OnInteractPressed;
    }

    void Update()
    {

        if (isInputLocked) return;

        HandleInventoryInput();
        HandleHotbarInput();


        CheckGrounded();


        if (isDashing) return;


        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        inputVector = new Vector3(moveInput.x, 0f, moveInput.y).normalized;


        if (animator != null)
        {
            float speedValue = inputVector.magnitude;
            animator.SetFloat("Vel", speedValue);


            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("Idle", speedValue < 0.1f);
        }
    }

    void FixedUpdate()
    {

        if (isInputLocked) return;


        if (isDashing) return;

        if (inputVector.magnitude >= 0.1f)
        {
            Vector3 moveDirection = Quaternion.Euler(0, 45f, 0) * inputVector;

            Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
        else
        {

            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }




    private void HandleInventoryInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;


        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
            return;

        if (keyboard.tabKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
        {
            if (InventoryManagerUI.Instance != null)
                InventoryManagerUI.Instance.TogglePlayerInventory();
        }
    }


    private void HandleHotbarInput()
    {
        if (playerInventory == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(3);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f)
            {
                int idx = playerInventory.selectedHotbarIndex - 1;
                if (idx < 0) idx = 3;
                playerInventory.SelectHotbarSlot(idx);
            }
            else if (scroll < 0f)
            {
                int idx = playerInventory.selectedHotbarIndex + 1;
                if (idx > 3) idx = 0;
                playerInventory.SelectHotbarSlot(idx);
            }
        }
    }

    private void ExecuteJump()
    {
        if (isInputLocked) return;


        if (isGrounded && !isDashing)
        {

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
{
    if (isInputLocked) return;


    if (interactor != null)
        {

            interactor.OnInteractInput();
        }
    }

    private IEnumerator ExecuteDash()
    {

        if (isInputLocked || isDashing || Time.time < lastDashTime + dashCooldown || inputVector.magnitude < 0.1f)
            yield break;

        isDashing = true;
        lastDashTime = Time.time;


        if (animator != null) animator.SetBool("Sliding", true);


        Vector3 dashDirection = transform.forward;
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {

            rb.linearVelocity = dashDirection * dashSpeed;
            yield return null;
        }


        if (animator != null) animator.SetBool("Sliding", false);
        isDashing = false;
    }

    private void CheckGrounded()
    {


        Vector3 origin = transform.position + (Vector3.up * 0.1f);
        isGrounded = Physics.Raycast(origin, Vector3.down, 0.25f);
    }
}
```

## File: Player/PlayerControl.cs.meta
```
fileFormatVersion: 2
guid: 6a52ec34b9e128b43b3530475b23e0c4
```

## File: Player/PlayerEquipment.cs
```csharp
using UnityEngine;




public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private Transform handSocket;
    private GameObject currentWeaponModel;
    private InventoryComponent inventory;

    private void Awake()
    {
        inventory = GetComponent<InventoryComponent>();
    }

    private void OnEnable()
    {
        if (inventory == null)
            inventory = GetComponent<InventoryComponent>();

        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshCurrentEquipment;
            inventory.OnHotbarSelected += UpdateEquipmentVisual;
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshCurrentEquipment;
            inventory.OnHotbarSelected -= UpdateEquipmentVisual;
        }
    }

    public void RefreshCurrentEquipment()
    {
        if (inventory != null)
            UpdateEquipmentVisual(inventory.selectedHotbarIndex);
    }

    public void UpdateEquipmentVisual(int hotbarIndex)
    {
        DestroyCurrentWeapon();

        if (inventory == null || handSocket == null)
            return;

        if (hotbarIndex < 0 || hotbarIndex >= inventory.slots.Count)
            return;

        InventorySlot slot = inventory.slots[hotbarIndex];
        if (slot == null || slot.item == null || slot.item.equipPrefab == null)
            return;

        GameObject spawned = Instantiate(slot.item.equipPrefab, handSocket);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        currentWeaponModel = spawned;
        currentWeaponModel.transform.localScale = slot.item.equipPrefab.transform.localScale;
    }

    public void DestroyCurrentWeapon()
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
            currentWeaponModel = null;
        }
    }
}
```

## File: Player/PlayerEquipment.cs.meta
```
fileFormatVersion: 2
guid: b7cde301005977440a8a69289e76b180
```

## File: Player/PlayerInputActions.cs
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;


























































public partial class @PlayerInputActions: IInputActionCollection2, IDisposable
{



    public InputActionAsset asset { get; }




    public @PlayerInputActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""version"": 1,
    ""name"": ""PlayerInputActions"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""8415e618-a104-436b-8166-45e48328b62a"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""65d1d9fb-c0fe-4122-90c5-00bf98e4cf52"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""7fb47fd2-a7e3-45a4-9bbe-b4e7cb7c7b54"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Dash"",
                    ""type"": ""Button"",
                    ""id"": ""632efdee-a56d-4e44-9c2c-137c4207253d"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Interact"",
                    ""type"": ""Button"",
                    ""id"": ""7ef7a92c-eb53-44f5-8ceb-1350b0ac85f8"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""WASD"",
                    ""id"": ""eb603c9b-8355-40b7-817e-c2c215c44a8e"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""4e0746be-cccc-490b-8120-20dc9386ba6b"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""06a37e5f-d510-4ba4-913b-23d80fe90abb"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""fceb6256-e637-43f8-a379-98955d815341"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""37df917c-cbbc-431e-b6ef-3abf7505d91d"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Arrow"",
                    ""id"": ""cd4b048a-fc82-4321-a10b-cc7e562e965a"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""06ce3131-3b5f-49b5-a02c-2aba85c8127a"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""e6ad8e20-9c5d-4ed3-974e-1d2f3b213c49"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""be6ade81-bcd8-46e2-80a1-4b1530b2268f"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""89d4aa40-35dd-4a65-b0ac-4c21575532d5"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""fc054d8a-af22-4b22-a5c1-9791372ed3de"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2b3ff94c-40b6-4c27-9e99-fa3e4de0914e"",
                    ""path"": """",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Dash"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""56720b3b-dccc-4f1e-a035-def11ea7d6db"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Dash"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8f502ad5-6e6a-431c-9f33-0b6fcf2ac796"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Interact"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");

        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_Move = m_Player.FindAction("Move", throwIfNotFound: true);
        m_Player_Jump = m_Player.FindAction("Jump", throwIfNotFound: true);
        m_Player_Dash = m_Player.FindAction("Dash", throwIfNotFound: true);
        m_Player_Interact = m_Player.FindAction("Interact", throwIfNotFound: true);
    }

    ~@PlayerInputActions()
    {
        UnityEngine.Debug.Assert(!m_Player.enabled, "This will cause a leak and performance issues, PlayerInputActions.Player.Disable() has not been called.");
    }




    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }


    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }


    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }


    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;


    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }


    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    public void Enable()
    {
        asset.Enable();
    }


    public void Disable()
    {
        asset.Disable();
    }


    public IEnumerable<InputBinding> bindings => asset.bindings;


    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }


    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }


    private readonly InputActionMap m_Player;
    private List<IPlayerActions> m_PlayerActionsCallbackInterfaces = new List<IPlayerActions>();
    private readonly InputAction m_Player_Move;
    private readonly InputAction m_Player_Jump;
    private readonly InputAction m_Player_Dash;
    private readonly InputAction m_Player_Interact;



    public struct PlayerActions
    {
        private @PlayerInputActions m_Wrapper;




        public PlayerActions(@PlayerInputActions wrapper) { m_Wrapper = wrapper; }



        public InputAction @Move => m_Wrapper.m_Player_Move;



        public InputAction @Jump => m_Wrapper.m_Player_Jump;



        public InputAction @Dash => m_Wrapper.m_Player_Dash;



        public InputAction @Interact => m_Wrapper.m_Player_Interact;



        public InputActionMap Get() { return m_Wrapper.m_Player; }

        public void Enable() { Get().Enable(); }

        public void Disable() { Get().Disable(); }

        public bool enabled => Get().enabled;



        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }








        public void AddCallbacks(IPlayerActions instance)
        {
            if (instance == null || m_Wrapper.m_PlayerActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Add(instance);
            @Move.started += instance.OnMove;
            @Move.performed += instance.OnMove;
            @Move.canceled += instance.OnMove;
            @Jump.started += instance.OnJump;
            @Jump.performed += instance.OnJump;
            @Jump.canceled += instance.OnJump;
            @Dash.started += instance.OnDash;
            @Dash.performed += instance.OnDash;
            @Dash.canceled += instance.OnDash;
            @Interact.started += instance.OnInteract;
            @Interact.performed += instance.OnInteract;
            @Interact.canceled += instance.OnInteract;
        }








        private void UnregisterCallbacks(IPlayerActions instance)
        {
            @Move.started -= instance.OnMove;
            @Move.performed -= instance.OnMove;
            @Move.canceled -= instance.OnMove;
            @Jump.started -= instance.OnJump;
            @Jump.performed -= instance.OnJump;
            @Jump.canceled -= instance.OnJump;
            @Dash.started -= instance.OnDash;
            @Dash.performed -= instance.OnDash;
            @Dash.canceled -= instance.OnDash;
            @Interact.started -= instance.OnInteract;
            @Interact.performed -= instance.OnInteract;
            @Interact.canceled -= instance.OnInteract;
        }





        public void RemoveCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }










        public void SetCallbacks(IPlayerActions instance)
        {
            foreach (var item in m_Wrapper.m_PlayerActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }



    public PlayerActions @Player => new PlayerActions(this);





    public interface IPlayerActions
    {






        void OnMove(InputAction.CallbackContext context);






        void OnJump(InputAction.CallbackContext context);






        void OnDash(InputAction.CallbackContext context);






        void OnInteract(InputAction.CallbackContext context);
    }
}
```

## File: Player/PlayerInputActions.cs.meta
```
fileFormatVersion: 2
guid: efedfad8f6406c7419837d927ecc9417
```

## File: Player/PlayerInputActions.inputactions
```
{
    "version": 1,
    "name": "PlayerInputActions",
    "maps": [
        {
            "name": "Player",
            "id": "8415e618-a104-436b-8166-45e48328b62a",
            "actions": [
                {
                    "name": "Move",
                    "type": "Value",
                    "id": "65d1d9fb-c0fe-4122-90c5-00bf98e4cf52",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "Jump",
                    "type": "Button",
                    "id": "7fb47fd2-a7e3-45a4-9bbe-b4e7cb7c7b54",
                    "expectedControlType": "",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "Dash",
                    "type": "Button",
                    "id": "632efdee-a56d-4e44-9c2c-137c4207253d",
                    "expectedControlType": "",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Interact",
                    "type": "Button",
                    "id": "7ef7a92c-eb53-44f5-8ceb-1350b0ac85f8",
                    "expectedControlType": "",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
            ],
            "bindings": [
                {
                    "name": "WASD",
                    "id": "eb603c9b-8355-40b7-817e-c2c215c44a8e",
                    "path": "2DVector",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": true,
                    "isPartOfComposite": false
                },
                {
                    "name": "up",
                    "id": "4e0746be-cccc-490b-8120-20dc9386ba6b",
                    "path": "<Keyboard>/w",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "06a37e5f-d510-4ba4-913b-23d80fe90abb",
                    "path": "<Keyboard>/s",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "fceb6256-e637-43f8-a379-98955d815341",
                    "path": "<Keyboard>/a",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "37df917c-cbbc-431e-b6ef-3abf7505d91d",
                    "path": "<Keyboard>/d",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "Arrow",
                    "id": "cd4b048a-fc82-4321-a10b-cc7e562e965a",
                    "path": "2DVector",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": true,
                    "isPartOfComposite": false
                },
                {
                    "name": "up",
                    "id": "06ce3131-3b5f-49b5-a02c-2aba85c8127a",
                    "path": "<Keyboard>/upArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "e6ad8e20-9c5d-4ed3-974e-1d2f3b213c49",
                    "path": "<Keyboard>/downArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "be6ade81-bcd8-46e2-80a1-4b1530b2268f",
                    "path": "<Keyboard>/leftArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "89d4aa40-35dd-4a65-b0ac-4c21575532d5",
                    "path": "<Keyboard>/rightArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "",
                    "id": "fc054d8a-af22-4b22-a5c1-9791372ed3de",
                    "path": "<Keyboard>/space",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Jump",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "2b3ff94c-40b6-4c27-9e99-fa3e4de0914e",
                    "path": "",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Dash",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "56720b3b-dccc-4f1e-a035-def11ea7d6db",
                    "path": "<Keyboard>/leftShift",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Dash",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "8f502ad5-6e6a-431c-9f33-0b6fcf2ac796",
                    "path": "<Keyboard>/e",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Interact",
                    "isComposite": false,
                    "isPartOfComposite": false
                }
            ]
        }
    ],
    "controlSchemes": []
}
```

## File: Player/PlayerInputActions.inputactions.meta
```
fileFormatVersion: 2
guid: a79093a3f0ced224bbf163d2b976b010
ScriptedImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 2
  userData: 
  assetBundleName: 
  assetBundleVariant: 
  script: {fileID: 11500000, guid: 8404be70184654265930450def6a9037, type: 3}
  generateWrapperCode: 1
  wrapperCodePath: 
  wrapperClassName: 
  wrapperCodeNamespace:
```

## File: Player/PlayerStats.cs
```csharp
using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action<int, int> OnHealthChanged;

    public int maxHealth = 100;
    public int currentHealth;

    void Start()
    {
        currentHealth = maxHealth - 50;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("HP Pemain sekarang: " + currentHealth + "/" + maxHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log(string.Format("Pemain terkena damage {0}. HP: {1}/{2}", amount, currentHealth, maxHealth));
    }
}
```

## File: Player/PlayerStats.cs.meta
```
fileFormatVersion: 2
guid: 0309c12546640a749b49296b6a5b0f65
```

## File: Player/UI.meta
```
fileFormatVersion: 2
guid: 09bbcdf8bd07bbc4c9f3f94438876515
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: AutoDoor.cs
```csharp
using UnityEngine;







public class AutoDoor : MonoBehaviour
{
    [Header("Door Behavior")]
    public float openAngle = 90f;
    public float triggerDistance = 6f;
    public float openSpeed = 3f;
    public float closeSpeed = 4f;
    public float closeDelay = 1.5f;

    [Header("Optional")]
    public Transform playerTarget;

    private Transform _player;
    private Quaternion _closedRot;
    private bool _isOpen;
    private float _closeTimer;

    private void Start()
    {
        _closedRot = transform.localRotation;
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (playerTarget != null)
        {
            _player = playerTarget;
            return;
        }
        GameObject capsule = GameObject.Find("PlayerCapsule");
        if (capsule != null)
        {
            _player = capsule.transform;
            return;
        }
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) _player = pc.transform;
    }

    private void Update()
    {
        if (_player == null) ResolvePlayer();
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist < triggerDistance)
        {
            _isOpen = true;
            _closeTimer = 0f;
        }
        else if (_isOpen)
        {
            _closeTimer += Time.deltaTime;
            if (_closeTimer >= closeDelay) _isOpen = false;
        }

        Quaternion target = _isOpen
            ? _closedRot * Quaternion.Euler(0f, openAngle, 0f)
            : _closedRot;

        float speed = _isOpen ? openSpeed : closeSpeed;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * speed);
    }
}
```

## File: AutoDoor.cs.meta
```
fileFormatVersion: 2
guid: f4da9826b2e2f204fbc800b5fa0a8da0
```

## File: Behaviour.meta
```
fileFormatVersion: 2
guid: 49196b0425c784a4ea465436c894960c
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: CameraController.cs
```csharp
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif






public class CameraController : MonoBehaviour
{
    [Header("Target & Isometric Follow")]
    public Transform target;

    [Tooltip("Camera Pitch Angle")]
    [Range(10f, 85f)]
    public float pitch = 35.264f;

    [Tooltip("Camera Yaw Angle")]
    public float yaw = 0.0f;

    [Tooltip("Distance from capsule target")]
    public float distance = 34.0f;

    [Tooltip("Smooth follow speed")]
    public float smoothSpeed = 10.0f;

    [Header("Projection Mode")]
    [Tooltip("Set camera to Orthographic Isometric view (eliminates perspective narrowing)")]
    public bool isOrthographic = true;
    public float orthographicSize = 19.5f;

    [Header("Orbit & Zoom")]
    public bool allowMouseOrbit = true;
    public float zoomSpeed = 4.0f;
    public float minSize = 6.0f;
    public float maxSize = 45.0f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (target == null)
        {
            GameObject player = GameObject.Find("PlayerCapsule");
            if (player != null) target = player.transform;
        }


        if (cam != null)
        {
            cam.orthographic = isOrthographic;
            cam.orthographicSize = orthographicSize;
        }

        if (GetComponent<WallOcclusionFader>() == null)
        {
            WallOcclusionFader fader = gameObject.AddComponent<WallOcclusionFader>();
            fader.target = target;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isOrbiting = false;
        float deltaX = 0f;
        float deltaY = 0f;
        float scrollDelta = 0f;

        #if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            isOrbiting = Mouse.current.rightButton.isPressed;
            Vector2 delta = Mouse.current.delta.ReadValue();
            deltaX = delta.x * 0.2f;
            deltaY = delta.y * 0.2f;

            Vector2 scroll = Mouse.current.scroll.ReadValue();
            scrollDelta = scroll.y * 0.005f;
        }
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (!isOrbiting)
        {
            try
            {
                if (Input.GetMouseButton(1))
                {
                    isOrbiting = true;
                    deltaX = Input.GetAxis("Mouse X") * 4.0f;
                    deltaY = Input.GetAxis("Mouse Y") * 4.0f;
                }
                scrollDelta = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            }
            catch
            {

            }
        }
        #endif

        if (allowMouseOrbit && isOrbiting)
        {
            yaw += deltaX;
            pitch -= deltaY;
            pitch = Mathf.Clamp(pitch, 10.0f, 85.0f);
        }

        if (Mathf.Abs(scrollDelta) > 0.001f && cam != null)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scrollDelta * 2.0f, minSize, maxSize);
            }
            else
            {
                distance = Mathf.Clamp(distance - scrollDelta, 6.0f, 30.0f);
            }
        }


        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetCenter = target.position;
        Vector3 targetPosition = targetCenter - (rotation * Vector3.forward * distance);

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * smoothSpeed);
    }
}
```

## File: CameraController.cs.meta
```
fileFormatVersion: 2
guid: 147898bb630355e48b45ee690afae82f
```

## File: Editor.meta
```
fileFormatVersion: 2
guid: 32d407c43d5a9b546977f7e337888e54
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: FaceCamera.cs
```csharp
using UnityEngine;




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


        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                          mainCam.transform.rotation * Vector3.up);
    }
}
```

## File: FaceCamera.cs.meta
```
fileFormatVersion: 2
guid: 696717fa38bd97e47b3d3d7ee0acea35
```

## File: Features.meta
```
fileFormatVersion: 2
guid: 3de77554d5c128b44984fd6e781c7c2d
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: GameInitializer.cs
```csharp
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Sistem AI berhasil terhubung!");
    }
}
```

## File: GameInitializer.cs.meta
```
fileFormatVersion: 2
guid: 658d282e63bc4f146bc8fb5cf73e9175
```

## File: InventoryUI.cs
```csharp
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif





public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int rows = 4;
    public int columns = 6;
    public float slotSize = 64f;
    public float slotPadding = 6f;

    [Header("Styling")]
    public Color panelColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    public Color slotColor = new Color(0.22f, 0.25f, 0.30f, 1.0f);
    public Color slotHighlightColor = new Color(0.35f, 0.55f, 0.85f, 1.0f);
    public Color titleColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);
    public Color closeButtonColor = new Color(0.85f, 0.25f, 0.25f, 1.0f);

    private GameObject inventoryPanel;
    private Canvas inventoryCanvas;
    private bool isOpen = false;


    private static InventoryUI _instance;
    public static InventoryUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InventoryUI>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("InventoryUI_Manager");
                    _instance = obj.AddComponent<InventoryUI>();
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }

        CreateInventoryUI();
    }

    void Update()
    {
        if (!isOpen) return;


        bool closePressed = false;

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            closePressed = Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (!closePressed)
        {
            try
            {
                closePressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape);
            }
            catch { }
        }
        #endif

        if (closePressed)
        {
            CloseInventory();
        }
    }

    private void CreateInventoryUI()
    {

        GameObject canvasObj = new GameObject("InventoryCanvas");
        canvasObj.transform.SetParent(transform);
        inventoryCanvas = canvasObj.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();


        GameObject overlayObj = new GameObject("DarkOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        RectTransform overlayRT = overlayObj.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;


        float panelWidth = columns * (slotSize + slotPadding) + slotPadding + 40f;
        float panelHeight = rows * (slotSize + slotPadding) + slotPadding + 100f;

        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvasObj.transform, false);
        Image panelImg = inventoryPanel.AddComponent<Image>();
        panelImg.color = panelColor;


        Outline panelOutline = inventoryPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.4f, 0.5f, 0.7f, 0.5f);
        panelOutline.effectDistance = new Vector2(2, 2);

        RectTransform panelRT = inventoryPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);


        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inventoryPanel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "\u2B50 CHEST INVENTORY";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = titleColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.offsetMin = new Vector2(10, -50);
        titleRT.offsetMax = new Vector2(-10, -8);


        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(inventoryPanel.transform, false);
        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = closeButtonColor;
        Button closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseInventory);
        RectTransform closeRT = closeObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-8, -8);
        closeRT.sizeDelta = new Vector2(36, 36);

        GameObject closeLabel = new GameObject("CloseLabel");
        closeLabel.transform.SetParent(closeObj.transform, false);
        Text closeTxt = closeLabel.AddComponent<Text>();
        closeTxt.text = "X";
        closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (closeTxt.font == null) closeTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        closeTxt.fontSize = 20;
        closeTxt.fontStyle = FontStyle.Bold;
        closeTxt.color = Color.white;
        closeTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform closeLabelRT = closeLabel.GetComponent<RectTransform>();
        closeLabelRT.anchorMin = Vector2.zero;
        closeLabelRT.anchorMax = Vector2.one;
        closeLabelRT.offsetMin = Vector2.zero;
        closeLabelRT.offsetMax = Vector2.zero;


        string[] sampleItems = { "Sword", "Shield", "Potion", "Key", "Gem", "Scroll", "Ring", "Helmet" };
        Color[] itemColors = {
            new Color(0.75f, 0.35f, 0.35f),
            new Color(0.35f, 0.55f, 0.75f),
            new Color(0.35f, 0.75f, 0.45f),
            new Color(0.85f, 0.75f, 0.30f),
            new Color(0.70f, 0.35f, 0.80f),
            new Color(0.80f, 0.65f, 0.40f),
            new Color(0.90f, 0.80f, 0.20f),
            new Color(0.50f, 0.65f, 0.80f)
        };

        float gridStartX = -(panelWidth / 2f) + slotPadding + 20f + slotSize / 2f;
        float gridStartY = (panelHeight / 2f) - 60f - slotPadding - slotSize / 2f;

        int itemIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = gridStartX + c * (slotSize + slotPadding);
                float y = gridStartY - r * (slotSize + slotPadding);


                GameObject slotObj = new GameObject($"Slot_{r}_{c}");
                slotObj.transform.SetParent(inventoryPanel.transform, false);
                Image slotImg = slotObj.AddComponent<Image>();
                slotImg.color = slotColor;
                Outline slotOutline = slotObj.AddComponent<Outline>();
                slotOutline.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.4f);
                slotOutline.effectDistance = new Vector2(1, 1);

                RectTransform slotRT = slotObj.GetComponent<RectTransform>();
                slotRT.anchorMin = new Vector2(0.5f, 0.5f);
                slotRT.anchorMax = new Vector2(0.5f, 0.5f);
                slotRT.pivot = new Vector2(0.5f, 0.5f);
                slotRT.anchoredPosition = new Vector2(x, y);
                slotRT.sizeDelta = new Vector2(slotSize, slotSize);


                if (itemIndex < sampleItems.Length)
                {

                    GameObject iconObj = new GameObject("ItemIcon");
                    iconObj.transform.SetParent(slotObj.transform, false);
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.color = itemColors[itemIndex];
                    RectTransform iconRT = iconObj.GetComponent<RectTransform>();
                    iconRT.anchorMin = new Vector2(0.15f, 0.3f);
                    iconRT.anchorMax = new Vector2(0.85f, 0.95f);
                    iconRT.offsetMin = Vector2.zero;
                    iconRT.offsetMax = Vector2.zero;


                    GameObject labelObj = new GameObject("ItemLabel");
                    labelObj.transform.SetParent(slotObj.transform, false);
                    Text labelTxt = labelObj.AddComponent<Text>();
                    labelTxt.text = sampleItems[itemIndex];
                    labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (labelTxt.font == null) labelTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    labelTxt.fontSize = 10;
                    labelTxt.color = new Color(0.8f, 0.8f, 0.8f);
                    labelTxt.alignment = TextAnchor.LowerCenter;
                    RectTransform labelRT = labelObj.GetComponent<RectTransform>();
                    labelRT.anchorMin = new Vector2(0, 0);
                    labelRT.anchorMax = new Vector2(1, 0.3f);
                    labelRT.offsetMin = Vector2.zero;
                    labelRT.offsetMax = Vector2.zero;

                    itemIndex++;
                }
            }
        }


        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(inventoryPanel.transform, false);
        Text promptTxt = promptObj.AddComponent<Text>();
        promptTxt.text = "Press [E] or [ESC] to close";
        promptTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (promptTxt.font == null) promptTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        promptTxt.fontSize = 14;
        promptTxt.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        promptTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform promptRT = promptObj.GetComponent<RectTransform>();
        promptRT.anchorMin = new Vector2(0, 0);
        promptRT.anchorMax = new Vector2(1, 0);
        promptRT.pivot = new Vector2(0.5f, 0);
        promptRT.offsetMin = new Vector2(10, 6);
        promptRT.offsetMax = new Vector2(-10, 30);


        canvasObj.SetActive(false);
    }

    public void OpenInventory()
    {
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(true);
            isOpen = true;
        }
    }

    public void CloseInventory()
    {
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(false);
            isOpen = false;
        }
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public bool IsOpen => isOpen;
}
```

## File: InventoryUI.cs.meta
```
fileFormatVersion: 2
guid: 942c85fc180e0e94fae08f04584af294
```

## File: Player.meta
```
fileFormatVersion: 2
guid: b6ff58612e10f5940b0c7dc7ffc3d603
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: PlayerController.cs
```csharp
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif






[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 9.0f;
    public float sprintSpeed = 15.0f;
    public float rotationSpeed = 14.0f;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;


    private const float GroundY = 1.0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45.0f;
            controller.skinWidth = 0.02f;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }


        if (transform.position.y < GroundY)
        {
            Vector3 pos = transform.position;
            pos.y = GroundY;
            transform.position = pos;
        }
    }

    void Update()
    {
        if (controller == null) return;

        isGrounded = controller.isGrounded || transform.position.y <= GroundY + 0.05f;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2.0f;


            if (transform.position.y < GroundY)
            {
                Vector3 pos = transform.position;
                pos.y = GroundY;
                transform.position = pos;
            }
        }


        float horizontal = 0f;
        float vertical = 0f;
        bool isSprinting = false;
        bool jumpPressed = false;

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;

            isSprinting = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (horizontal == 0f && vertical == 0f)
        {
            try
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
                if (!isSprinting) isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (!jumpPressed) jumpPressed = Input.GetButtonDown("Jump");
            }
            catch
            {

            }
        }
        #endif

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 moveDir;

            if (cameraTransform != null)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                moveDir = camForward * inputDir.z + camRight * inputDir.x;
            }
            else
            {
                moveDir = inputDir;
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            controller.Move(moveDir * currentSpeed * Time.deltaTime);
        }

        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);


        if (transform.position.y < GroundY)
        {
            Vector3 clampedPos = transform.position;
            clampedPos.y = GroundY;
            transform.position = clampedPos;
        }
    }
}
```

## File: PlayerController.cs.meta
```
fileFormatVersion: 2
guid: d8253fadf49ff8e489ff1bb77a3ebb17
```

## File: RoomBuilder.cs
```csharp
using UnityEngine;





[ExecuteAlways]
public class RoomBuilder : MonoBehaviour
{
    [Header("Room Layout Dimensions")]
    public float roomScale = 5.0f;
    public float roomWidth = 80.0f;
    public float roomLength = 80.0f;
    public float wallHeight = 14.0f;
    public float wallThickness = 1.5f;

    [Header("Materials & Styling")]
    public Color wallColor = new Color(0.48f, 0.54f, 0.60f);
    public Color floorColor = new Color(0.55f, 0.62f, 0.68f);
    public Color capsuleColor = new Color(0.95f, 0.95f, 0.95f);
    public Color pillarColor = new Color(0.60f, 0.66f, 0.72f);

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


        roomWidth = 16.0f * roomScale;
        roomLength = 16.0f * roomScale;
        wallHeight = 3.2f * roomScale;
        wallThickness = 0.2f * roomScale;


        Material wallMat = CreateSimpleMaterial("WallMaterial", wallColor);
        Material floorMat = CreateSimpleMaterial("FloorMaterial", floorColor);
        Material capsuleMat = CreateSimpleMaterial("CapsuleMaterial", capsuleColor);
        Material pillarMat = CreateSimpleMaterial("PillarMaterial", pillarColor);


        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "FloorGrid";
        floor.transform.SetParent(roomParent.transform);
        floor.transform.position = new Vector3(0, -0.1f, 0);
        floor.transform.localScale = new Vector3(roomWidth, 0.2f, roomLength);
        floor.GetComponent<Renderer>().material = floorMat;

        if (floor.GetComponent<BoxCollider>() == null)
            floor.AddComponent<BoxCollider>();


        GameObject wallsParent = new GameObject("Walls_With_Colliders");
        wallsParent.transform.SetParent(roomParent.transform);

        float halfW = roomWidth / 2.0f;
        float halfL = roomLength / 2.0f;
        float s = roomScale;


        CreateWall(wallsParent, "Wall_Right", new Vector3(halfW, wallHeight/2f, 0), new Vector3(wallThickness, wallHeight, roomLength + wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Back", new Vector3(0, wallHeight/2f, halfL), new Vector3(roomWidth + wallThickness, wallHeight, wallThickness), wallMat);



        CreateWall(wallsParent, "Wall_Inner_Left1", new Vector3(-5.4f * s, wallHeight/2f, -5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Left1_Cap", new Vector3(-4.8f * s, wallHeight/2f, -2.0f * s), new Vector3(1.2f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Left2", new Vector3(-2.8f * s, wallHeight/2f, -5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Left2_Cap", new Vector3(-3.4f * s, wallHeight/2f, -2.0f * s), new Vector3(1.2f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_CenterBottom", new Vector3(0.2f * s, wallHeight/2f, -5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);


        CreateWall(wallsParent, "Wall_Inner_Left_Standing", new Vector3(-6.8f * s, wallHeight/2f, 0.5f * s), new Vector3(wallThickness, wallHeight, 3.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopLeft_V", new Vector3(-5.4f * s, wallHeight/2f, 4.5f * s), new Vector3(wallThickness, wallHeight, 7.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopLeft_H", new Vector3(-3.9f * s, wallHeight/2f, 1.0f * s), new Vector3(3.0f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopLeft_Divider", new Vector3(-3.0f * s, wallHeight/2f, 5.0f * s), new Vector3(wallThickness, wallHeight, 6.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_TopMiddle", new Vector3(-1.0f * s, wallHeight/2f, 4.5f * s), new Vector3(wallThickness, wallHeight, 7.0f * s), wallMat);


        CreateWall(wallsParent, "Wall_Inner_TopRight_V", new Vector3(3.2f * s, wallHeight/2f, 5.5f * s), new Vector3(wallThickness, wallHeight, 5.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_H", new Vector3(5.2f * s, wallHeight/2f, 3.0f * s), new Vector3(3.2f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_Middle_V", new Vector3(2.4f * s, wallHeight/2f, -0.5f * s), new Vector3(wallThickness, wallHeight, 5.0f * s), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_Middle_H", new Vector3(3.4f * s, wallHeight/2f, 2.0f * s), new Vector3(2.0f * s, wallHeight, wallThickness), wallMat);
        CreateWall(wallsParent, "Wall_Inner_Right_Front_V", new Vector3(2.4f * s, wallHeight/2f, -6.0f * s), new Vector3(wallThickness, wallHeight, 4.0f * s), wallMat);


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
        bedroom.transform.position = new Vector3(-6.6f * s, 0f, 4.8f * s);
        bedroom.transform.rotation = Quaternion.Euler(0, 90f, 0);

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
        charCtrl.stepOffset = 0.4f;
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
```

## File: RoomBuilder.cs.meta
```
fileFormatVersion: 2
guid: 1e9a8f346092e2e41b5b54af20820018
```

## File: WallOcclusionFader.cs
```csharp
using System.Collections.Generic;
using UnityEngine;





public class WallOcclusionFader : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Target player capsule to keep visible")]
    public Transform target;

    [Header("Fade Settings")]
    [Tooltip("Target opacity when wall is blocking player (0 = invisible, 0.25 = semi-transparent)")]
    [Range(0.05f, 0.8f)]
    public float fadeAlpha = 0.25f;

    [Tooltip("Speed of fading transition")]
    public float fadeSpeed = 10.0f;

    [Tooltip("SphereCast radius for obstacle detection")]
    public float sphereRadius = 0.5f;

    [Tooltip("Layer mask for walls")]
    public LayerMask wallLayerMask = ~0;

    private Dictionary<Renderer, float> currentAlphas = new Dictionary<Renderer, float>();
    private HashSet<Renderer> occludingWalls = new HashSet<Renderer>();

    void Start()
    {
        FindTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }


        Vector3 targetCenterPos = target.position;
        Vector3 dir = targetCenterPos - transform.position;
        float dist = dir.magnitude;

        occludingWalls.Clear();

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, sphereRadius, dir.normalized, dist, wallLayerMask);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == target || hit.transform.IsChildOf(target)) continue;

            Renderer r = hit.transform.GetComponent<Renderer>();
            if (r != null && (hit.transform.name.StartsWith("Wall") || hit.transform.name.Contains("Pillar")))
            {
                occludingWalls.Add(r);
            }
        }

        foreach (Renderer r in occludingWalls)
        {
            if (!currentAlphas.ContainsKey(r))
            {
                currentAlphas[r] = 1.0f;
            }
        }

        List<Renderer> keys = new List<Renderer>(currentAlphas.Keys);
        foreach (Renderer r in keys)
        {
            if (r == null)
            {
                currentAlphas.Remove(r);
                continue;
            }

            bool isOccluding = occludingWalls.Contains(r);
            float targetAlpha = isOccluding ? fadeAlpha : 1.0f;
            float currentAlpha = Mathf.Lerp(currentAlphas[r], targetAlpha, Time.deltaTime * fadeSpeed);
            currentAlphas[r] = currentAlpha;

            SetRendererAlpha(r, currentAlpha);

            if (!isOccluding && Mathf.Abs(currentAlpha - 1.0f) < 0.01f)
            {
                SetRendererAlpha(r, 1.0f);
                currentAlphas.Remove(r);
            }
        }
    }

    private void FindTarget()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("PlayerCapsule");
            if (player != null) target = player.transform;
        }
    }

    private void SetRendererAlpha(Renderer r, float alpha)
    {
        if (r == null) return;
        Material mat = r.material;

        Color c = mat.color;
        if (mat.HasProperty("_BaseColor")) c = mat.GetColor("_BaseColor");
        else if (mat.HasProperty("_Color")) c = mat.GetColor("_Color");

        c.a = alpha;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.color = c;

        if (alpha < 0.98f)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_Surface", 0);
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    }
}
```

## File: WallOcclusionFader.cs.meta
```
fileFormatVersion: 2
guid: 554ff7c21dadb8849a69783458915f59
```
