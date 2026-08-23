This file is a merged representation of a subset of the codebase, containing specifically included files, combined into a single document by Repomix.
The content has been processed where comments have been removed.

# File Summary

## Purpose
This file contains a packed representation of a subset of the repository's contents that is considered the most important context.
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
- Only files matching these patterns are included: **/*.cs
- Files matching patterns in .gitignore are excluded
- Files matching default ignore patterns are excluded
- Code comments have been removed from supported file types
- Files are sorted by Git change count (files with more changes are at the bottom)

# Directory Structure
```
Assets/
  Editor/
    GeminiAutomation.cs
    HoverLabelSetup.cs
    ImportMainScene.cs
    KitchenProgressWiring.cs
    KitchenSetup.cs
    TrophyAssetFactory.cs
    TrophySystemWiring.cs
    VisualPolishSetup.cs
    WardrobeSetup.cs
  Scripts/
    Behaviour/
      IsometricCamera.cs
    Editor/
      RoomBuilderEditor.cs
    Features/
      Camera/
        WallOccluder.cs
        WallOcclusionManager.cs
      Common/
        FadeManager.cs
      Interaction/
        BedInteractable.cs
        GenericFurnitureInteractable.cs
        Highlightable.cs
        HoverLabelController.cs
        IInteractable.cs
        InteractionZone.cs
        PlayerInteractor.cs
        StorageInteractable.cs
        TrophyCabinetInteractable.cs
        WardrobeInteractable.cs
        WorldLabel.cs
      Inventory/
        UI/
          DraggableItem.cs
          InventoryManagerUI.cs
          InventorySlotUI.cs
          ItemDisplayUI.cs
        InventoryComponent.cs
        InventorySlot.cs
        ItemData.cs
      Kitchen/
        UI/
          KitchenStationProgressOverlay.cs
          KitchenStationSoundFx.cs
          KitchenStationUI.cs
        DoorInteractable.cs
        KitchenRecipe.cs
        KitchenSinkInteractable.cs
        KitchenStation.cs
        RefrigeratorInteractable.cs
        StoveInteractable.cs
      Time/
        UI/
          DayTransitionUI.cs
        TimeManager.cs
      Trophy/
        TrophyItem.cs
        TrophyRackVisuals.cs
        TrophySnapPoint.cs
        TrophySystemManager.cs
      Wardrobe/
        UI/
          WardrobeUI.cs
        MirrorCamera.cs
        OutfitData.cs
        PlayerOutfit.cs
        WardrobeManager.cs
    Player/
      UI/
        PlayerHealthUI.cs
      PlayerControl.cs
      PlayerEquipment.cs
      PlayerInputActions.cs
      PlayerStats.cs
    AutoDoor.cs
    CameraController.cs
    FaceCamera.cs
    GameInitializer.cs
    InventoryUI.cs
    PlayerController.cs
    RoomBuilder.cs
    WallOcclusionFader.cs
```

# Files

## File: Assets/Scripts/Features/Camera/WallOccluder.cs
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

## File: Assets/Scripts/Features/Camera/WallOcclusionManager.cs
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

## File: Assets/Scripts/Features/Common/FadeManager.cs
```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FeaturesCommon
{
    public class FadeManager : MonoBehaviour
    {
        public static FadeManager Instance { get; private set; }

        [Header("Fade Settings")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private float defaultFadeDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;

        private Coroutine currentFadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
                fadeImage.raycastTarget = false;
            }
        }

        public Coroutine FadeIn(float duration = -1f, System.Action onComplete = null)
        {
            return StartFade(0f, 1f, duration > 0 ? duration : defaultFadeDuration, onComplete);
        }

        public Coroutine FadeOut(float duration = -1f, System.Action onComplete = null)
        {
            return StartFade(1f, 0f, duration > 0 ? duration : defaultFadeDuration, onComplete);
        }

        public Coroutine FadeTo(float targetAlpha, float duration = -1f, System.Action onComplete = null)
        {
            if (fadeImage == null) return null;
            float startAlpha = fadeImage.color.a;
            return StartFade(startAlpha, targetAlpha, duration > 0 ? duration : defaultFadeDuration, onComplete);
        }

        private Coroutine StartFade(float fromAlpha, float toAlpha, float duration, System.Action onComplete)
        {
            if (fadeImage == null) return null;

            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            currentFadeCoroutine = StartCoroutine(FadeCoroutine(fromAlpha, toAlpha, duration, onComplete));
            return currentFadeCoroutine;
        }

        private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration, System.Action onComplete)
        {
            if (fadeImage == null) yield break;

            float elapsed = 0f;
            Color color = fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                color.a = alpha;
                fadeImage.color = color;
                yield return null;
            }

            color.a = toAlpha;
            fadeImage.color = color;

            currentFadeCoroutine = null;
            onComplete?.Invoke();
        }

        public void SetFadeInstant(float alpha)
        {
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;
            }
        }

        public bool IsFading => currentFadeCoroutine != null;
    }
}
```

## File: Assets/Scripts/Features/Interaction/InteractionZone.cs
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

## File: Assets/Editor/GeminiAutomation.cs
```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class GeminiCoPilot : EditorWindow
{
    private string apiKey = "AQ.Ab8RN6IEiDNm7L5rIAMNBzkpKixrU-615BpV5qDLF9jYtbZ7zg";
    private string prompt = "Buatkan lantai Plane bernama Ground, lalu buat Player bertipe Capsule di atasnya. Beri Player komponen Rigidbody, dan atur kamera isometrik melihat ke Player.";
    private string responseText = "";
    private Vector2 scrollPos; // Untuk UI scroll

    [MenuItem("Tools/Gemini AI Co-Pilot Pro")]
    public static void ShowWindow()
    {
        GetWindow<GeminiCoPilot>("AI Co-Pilot");
    }

    void OnGUI()
    {
        GUILayout.Label("Instruksi Co-Pilot (Manipulasi Scene)", EditorStyles.boldLabel);
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(80));

        if (GUILayout.Button("Eksekusi Perintah", GUILayout.Height(35)))
        {
            _ = ExecuteRequestAsync();
        }

        GUILayout.Label("Console Log (Hasil Eksekusi):", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        responseText = EditorGUILayout.TextArea(responseText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private async Task ExecuteRequestAsync()
    {
        responseText = "Menghubungi AI... (Mohon tunggu beberapa detik)";
        Repaint();


        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=" + apiKey;

        string systemContext = @"Kamu adalah AI Assistant Unity Editor.
Ubah instruksi user menjadi JSON array tindakan (commands).
WAJIB menghasilkan HANYA format JSON murni tanpa basa-basi.
Daftar action: CREATE_OBJECT, SET_POSITION, SET_ROTATION, ADD_COMPONENT, SET_CAMERA_ISOMETRIC.
Daftar type (untuk CREATE_OBJECT): Empty, Cube, Sphere, Capsule, Cylinder, Plane, Quad.
Contoh output JSON:
{
  ""commands"": [
    { ""action"": ""CREATE_OBJECT"", ""name"": ""Player"", ""type"": ""Capsule"" },
    { ""action"": ""ADD_COMPONENT"", ""name"": ""Player"", ""component"": ""Rigidbody"" },
    { ""action"": ""SET_CAMERA_ISOMETRIC"", ""targetName"": ""Player"" }
  ]
}";



        string combinedText = systemContext + "\n\nInstruksi User: " + prompt;


        string safeText = combinedText
            .Replace("\\", "\\\\") // Amankan garis miring terbalik (backslash)
            .Replace("\"", "\\\"") // Amankan tanda kutip ganda (double quotes)
            .Replace("\n", "\\n")
            .Replace("\r", "");    // Hapus karakter tersembunyi carriage return

        string jsonBody = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{safeText}\"}}]}}]}}";
        // ===========================================================

        int maxRetries = 3;
        // ... (kode ke bawahnya tetap sama persis: int currentTry = 0; dst...)
        int currentTry = 0;
        bool success = false;

        while (currentTry < maxRetries && !success)
        {
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                var operation = www.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    success = true;
                    ProcessAIResponse(www.downloadHandler.text);
                }
                else if (www.responseCode == 429)
                {
                    currentTry++;
                    responseText = $"API Sibuk (Limit 429). Otomatis menunggu dan mencoba ulang... ({currentTry}/{maxRetries})";
                    Repaint();
                    await Task.Delay(12000);
                }
                else
                {
                    responseText = "API Error: " + www.error + "\n\nDetail:\n" + www.downloadHandler.text;
                    break;
                }
            }
        }

        if (!success && currentTry >= maxRetries)
        {
            responseText = "Gagal memproses karena API terus sibuk (Mencapai batas Auto-Retry).";
        }
        Repaint();
    }

    private void ProcessAIResponse(string rawJson)
    {
        try
        {
            GeminiResponse data = JsonUtility.FromJson<GeminiResponse>(rawJson);


            if (data == null || data.candidates == null || data.candidates.Length == 0)
                throw new Exception("Struktur JSON 'candidates' kosong atau gagal diparse.");

            var parts = data.candidates[0].content?.parts;
            if (parts == null || parts.Length == 0)
                throw new Exception("Struktur JSON 'parts' kosong.");

            string rawText = parts[0].text;
            if (string.IsNullOrEmpty(rawText))
                throw new Exception("Teks konten dari Gemini kosong.");


            Match jsonMatch = Regex.Match(rawText, @"\{[\s\S]*\}");
            if (!jsonMatch.Success)
                throw new Exception("Tidak menemukan format JSON (kurung kurawal) di dalam respons AI.");

            string cleanJson = jsonMatch.Value;

            AICommandList commandList = JsonUtility.FromJson<AICommandList>(cleanJson);
            if (commandList == null || commandList.commands == null)
                throw new Exception("Berhasil menemukan JSON, namun gagal mengubahnya menjadi C# Class (AICommandList).");

            StringBuilder log = new StringBuilder();
            log.AppendLine("=== EKSEKUSI BERHASIL ===");


            foreach (var cmd in commandList.commands)
            {
                if (string.IsNullOrEmpty(cmd.action)) continue;

                string targetDisplayName = !string.IsNullOrEmpty(cmd.name) ? cmd.name : cmd.targetName;
                log.AppendLine($">> {cmd.action} | Target: {targetDisplayName}");

                ExecuteCommandSafe(cmd, log);
            }

            responseText = log.ToString();
        }
        catch (Exception ex)
        {
            responseText = $"CRITICAL PARSING ERROR:\n{ex.Message}\n\nRespons API Mentah (Debug):\n{rawJson}";
        }
    }


    private void ExecuteCommandSafe(AICommand cmd, StringBuilder log)
    {
        try
        {
            GameObject targetObj = null;
            if (!string.IsNullOrEmpty(cmd.name))
            {
                targetObj = GameObject.Find(cmd.name);
            }

            switch (cmd.action)
            {
                case "CREATE_OBJECT":
                    if (targetObj == null)
                    {
                        if (cmd.type == "Empty")
                        {
                            targetObj = new GameObject(cmd.name);
                        }

                        else if (Enum.TryParse(cmd.type, true, out PrimitiveType pt))
                        {
                            targetObj = GameObject.CreatePrimitive(pt);
                        }
                        else
                        {
                            targetObj = new GameObject(cmd.name);
                            log.AppendLine($"   (Warning: Tipe '{cmd.type}' tidak valid. Membuat objek Empty.)");
                        }

                        if (targetObj != null)
                        {
                            targetObj.name = cmd.name;
                            Undo.RegisterCreatedObjectUndo(targetObj, $"AI Create {cmd.name}");
                        }
                    }
                    break;

                case "SET_POSITION":
                    if (targetObj != null)
                    {
                        Undo.RecordObject(targetObj.transform, "AI Move");
                        targetObj.transform.position = new Vector3(cmd.x, cmd.y, cmd.z);
                    }
                    break;

                case "SET_ROTATION":
                    if (targetObj != null)
                    {
                        Undo.RecordObject(targetObj.transform, "AI Rotate");
                        targetObj.transform.rotation = Quaternion.Euler(cmd.x, cmd.y, cmd.z);
                    }
                    break;

                case "ADD_COMPONENT":
                    if (targetObj != null)
                    {
                        if (cmd.component.Contains("Rigidbody") && targetObj.GetComponent<Rigidbody>() == null)
                            Undo.AddComponent<Rigidbody>(targetObj);
                        else if (cmd.component.Contains("BoxCollider") && targetObj.GetComponent<BoxCollider>() == null)
                            Undo.AddComponent<BoxCollider>(targetObj);
                        else if (cmd.component.Contains("CapsuleCollider") && targetObj.GetComponent<CapsuleCollider>() == null)
                            Undo.AddComponent<CapsuleCollider>(targetObj);
                        else if (cmd.component.Contains("SphereCollider") && targetObj.GetComponent<SphereCollider>() == null)
                            Undo.AddComponent<SphereCollider>(targetObj);
                    }
                    break;

                case "SET_CAMERA_ISOMETRIC":
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        Undo.RecordObject(mainCam.transform, "AI Camera Rotation");
                        Undo.RecordObject(mainCam, "AI Camera Config");

                        mainCam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
                        mainCam.orthographic = true;

                        string tName = string.IsNullOrEmpty(cmd.targetName) ? cmd.name : cmd.targetName;
                        GameObject focus = GameObject.Find(tName);
                        if (focus != null)
                        {
                            mainCam.transform.position = focus.transform.position + new Vector3(-10f, 15f, -10f);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            log.AppendLine($"   [X] ERROR pada {cmd.action}: {ex.Message}");
        }
    }
}





[System.Serializable]
public class AICommandList { public AICommand[] commands; }

[System.Serializable]
public class AICommand
{
    public string action;
    public string name;
    public string type;
    public string component;
    public string targetName;
    public float x;
    public float y;
    public float z;
}

[System.Serializable] public class GeminiResponse { public Candidate[] candidates; }
[System.Serializable] public class Candidate { public Content content; }
[System.Serializable] public class Content { public Part[] parts; }
[System.Serializable] public class Part { public string text; }
```

## File: Assets/Editor/HoverLabelSetup.cs
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FeaturesInteraction;






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
            rt.anchoredPosition = new Vector2(0f, 150f);
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
            lrt.anchoredPosition = new Vector2(0f, 110f);
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
```

## File: Assets/Editor/ImportMainScene.cs
```csharp
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using System;

public class ImportMainScene
{
    [MenuItem("Farm Beware/Import/MainScene as RafiScene")]
    public static void Import()
    {
        string packagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MainSceneExport.unitypackage"
        );

        if (!File.Exists(packagePath))
        {
            Debug.LogError($"Package not found: {packagePath}");
            return;
        }

        AssetDatabase.ImportPackage(packagePath, false);
        Debug.Log("Imported MainSceneExport.unitypackage");


        RenameAndSetupScene();
    }

    private static void RenameAndSetupScene()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("MainScene t:Scene");
        if (sceneGuids.Length == 0)
        {
            Debug.LogError("MainScene not found after import");
            return;
        }

        string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
        string newScenePath = "Assets/Scenes/RafiScene.unity";

        if (File.Exists(newScenePath))
        {
            AssetDatabase.DeleteAsset(newScenePath);
            Debug.Log("Deleted existing RafiScene.unity");
        }

        string moveResult = AssetDatabase.MoveAsset(scenePath, newScenePath);
        if (!string.IsNullOrEmpty(moveResult))
        {
            Debug.LogError($"Failed to rename scene: {moveResult}");
            return;
        }
        Debug.Log($"Renamed MainScene to RafiScene at {newScenePath}");

        var scenes = EditorBuildSettings.scenes.ToList();
        int sampleIndex = scenes.FindIndex(s => s.path.Contains("SampleScene"));
        int rafiIndex = scenes.FindIndex(s => s.path.Contains("RafiScene"));

        if (sampleIndex >= 0)
        {
            scenes.RemoveAt(sampleIndex);
            Debug.Log("Removed SampleScene from build settings");
        }

        if (rafiIndex < 0)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(newScenePath, true));
            Debug.Log("Added RafiScene as scene 0 in build settings");
        }
        else
        {
            var rafiScene = scenes[rafiIndex];
            scenes.RemoveAt(rafiIndex);
            scenes.Insert(0, rafiScene);
            Debug.Log("Moved RafiScene to index 0");
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Updated EditorBuildSettings");

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Import and setup complete!");
    }
}
```

## File: Assets/Editor/KitchenProgressWiring.cs
```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;












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
```

## File: Assets/Editor/KitchenSetup.cs
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;







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


        EnsureFolder("Assets/Scripts/Features/Kitchen", "Data");
        KitchenRecipe washCarrot = CreateRecipe(KitchenDataDir + "/Wash_Carrot.asset", carrotDirty, carrotClean, 1, 3f);
        KitchenRecipe cookVeg = CreateRecipe(KitchenDataDir + "/Cook_Veggies.asset", carrotClean,
            AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataDir + "/Cooked_Veggies.asset"), 1, 5f);
        KitchenRecipe cookRice = CreateRecipe(KitchenDataDir + "/Cook_Rice.asset", riceRaw,
            AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataDir + "/Cooked_Rice.asset"), 1, 4f);


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


        GameObject floor = CreatePrimitiveInScene(PrimitiveType.Plane, "Kitchen_Floor", root.transform, new Vector3(0f, 0f, 0f), new Vector3(4f, 1f, 3.5f));
        DestroyColliderIfAny(floor);


        GameObject fridge = CreatePrimitiveInScene(PrimitiveType.Cube, "Fridge", root.transform, new Vector3(-1.6f, 1f, 2.4f), new Vector3(1.1f, 2f, 1f));
        ResetColliderBounds(fridge);
        InventoryComponent fridgeInv = GetOrAdd<InventoryComponent>(fridge);
        if (fridgeInv.slots == null || fridgeInv.slots.Count != 8)
            fridgeInv.ResetInventory(8);
        GetOrAdd<RefrigeratorInteractable>(fridge);


        GameObject sink = CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Sink", root.transform, new Vector3(0.2f, 0.43f, 2.4f), new Vector3(1.4f, 0.85f, 0.75f));
        ResetColliderBounds(sink);
        InventoryComponent sinkInv = GetOrAdd<InventoryComponent>(sink);
        if (sinkInv.slots == null || sinkInv.slots.Count != 1)
            sinkInv.ResetInventory(1);
        KitchenSinkInteractable sinkStation = GetOrAdd<KitchenSinkInteractable>(sink);


        GameObject stove = CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Stove", root.transform, new Vector3(1.4f, 0.25f, 2.4f), new Vector3(1.4f, 0.5f, 0.8f));
        ResetColliderBounds(stove);
        InventoryComponent stoveInv = GetOrAdd<InventoryComponent>(stove);
        if (stoveInv.slots == null || stoveInv.slots.Count != 2)
            stoveInv.ResetInventory(2);
        StoveInteractable stoveStation = GetOrAdd<StoveInteractable>(stove);


        CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Table", root.transform, new Vector3(0f, 0.05f, -0.1f), new Vector3(2f, 0.1f, 1f));
        CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Chair", root.transform, new Vector3(0f, 0.3f, -1f), new Vector3(0.45f, 0.6f, 0.45f));
        CreatePrimitiveInScene(PrimitiveType.Cube, "FoodPrepArea", root.transform, new Vector3(-0.75f, 0.06f, -0.1f), new Vector3(1.3f, 0.12f, 0.8f));
        GameObject window = CreatePrimitiveInScene(PrimitiveType.Cube, "Kitchen_Window", root.transform, new Vector3(1.8f, 1.2f, 0.5f), new Vector3(1.6f, 1.2f, 0.1f));
        DestroyColliderIfAny(window);


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


        WireStationUI(stoveStation);


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
```

## File: Assets/Editor/TrophyAssetFactory.cs
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;






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


        if (inv.slots != null && inv.slots.Count == 4 && inv.maxCapacity == 4)
        {
            Debug.Log(" [TrophySystemFactory] Kabinet sudah 4 slot. Tidak ada perubahan.");
            return;
        }


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
            ItemDataDir + "/TrophyCapsule.asset",
            ItemDataDir + "/TrophyCube.asset",
            ItemDataDir + "/TrophySphere.asset",
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
```

## File: Assets/Editor/TrophySystemWiring.cs
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
















public static class TrophySystemWiring
{
    private const string RootName = "TrophyCabinetSystem";
    private const string RakName = "Rak_Trophy";
    private const string ManagerName = "TrophySystemManager";
    private const string CabinetName = "Kabinet";
    private const string PlayerName = "Player";
    private const string TrophyCameraName = "TrophyCamera";
    private const int SnapCount = 4;


    private static readonly Vector3 DefaultCameraLocalOffset = new Vector3(-0.15f, 1.5f, 3f);
    private static readonly Vector3 DefaultCameraLocalRotation = new Vector3(3f, 0f, 0f);

    [MenuItem("Farm Beware/Trophy System/Wire Scene (4 SnapPoint)")]
    public static void Wire()
    {

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create TrophyCabinetSystem");
            Debug.Log($"[TrophySystemWiring] Created parent container '{RootName}'.");
        }


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


        trophyCameraGO.transform.localPosition = DefaultCameraLocalOffset;
        trophyCameraGO.transform.localRotation = Quaternion.Euler(DefaultCameraLocalRotation);


        Camera trophyCam = trophyCameraGO.GetComponent<Camera>();
        if (trophyCam == null)
        {
            trophyCam = trophyCameraGO.AddComponent<Camera>();
            Undo.RegisterCreatedObjectUndo(trophyCam, "Add Camera to TrophyCamera");
        }


        trophyCam.depth = 1f;
        trophyCam.clearFlags = CameraClearFlags.Skybox;
        trophyCameraGO.SetActive(false);


        var uacd = trophyCameraGO.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (uacd == null)
        {
            uacd = trophyCameraGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            Undo.RegisterCreatedObjectUndo(uacd, "Add UniversalAdditionalCameraData to TrophyCamera");
        }


        InventoryComponent rackInv = rak.GetComponent<InventoryComponent>();
        if (rackInv == null)
        {
            rackInv = rak.AddComponent<InventoryComponent>();
            Undo.RegisterCreatedObjectUndo(rackInv, "Add Rack InventoryComponent");
        }
        rackInv.maxCapacity = SnapCount;


        if (rackInv.slots == null || rackInv.slots.Count != SnapCount)
            rackInv.ResetInventory(SnapCount);


        TrophyRackVisuals visuals = rak.GetComponent<TrophyRackVisuals>();
        if (visuals == null)
        {
            visuals = rak.AddComponent<TrophyRackVisuals>();
            Undo.RegisterCreatedObjectUndo(visuals, "Add TrophyRackVisuals");
        }


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


        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Debug.Log($"[TrophySystemWiring] Wiring selesai: {RootName} created + Kabinet/Rak/Camera re-parented + Rack({rackInv.maxCapacity} slot) + TrophyRackVisuals + Snap_1..4 (slotIndex 0..3) + Manager wired (trophySystemRoot, trophyFirstPersonCamera) + Player blockTrophyItems=true + scene disimpan.");
    }
}
```

## File: Assets/Editor/VisualPolishSetup.cs
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;







public static class VisualPolishSetup
{
    private const string MatDir = "Assets/Materials/Kitchen";
    private const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";

    private static readonly Vector3 RoomCenter = new Vector3(14f, 0f, 5f);
    private const float RoomW = 12f;
    private const float RoomD = 8f;
    private const float WallH = 3f;
    private const float WallT = 0.3f;

    [MenuItem("Farm Beware/Polish/Apply Cozy Farm & Room (Phase 1)")]
    public static void Apply()
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Kitchen");


        Material wood = CreateMat("Mat_Wood", new Color(0.58f, 0.43f, 0.30f), 0.25f, 0f);
        Material wallCream = CreateMat("Mat_Wall_Cream", new Color(0.93f, 0.89f, 0.79f), 0.05f, 0f);
        Material tile = CreateMat("Mat_Floor_Tile", new Color(0.85f, 0.78f, 0.62f), 0.3f, 0f);
        Material metal = CreateMat("Mat_Metal", new Color(0.72f, 0.75f, 0.78f), 0.6f, 1f);
        Material metalDark = CreateMat("Mat_Metal_Dark", new Color(0.20f, 0.21f, 0.23f), 0.55f, 1f);
        Material glass = CreateGlassMat("Mat_Glass", new Color(0.55f, 0.75f, 0.85f, 0.5f));
        Material grass = CreateMat("Mat_Grass", new Color(0.45f, 0.62f, 0.35f), 0.15f, 0f);


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


        GameObject stove = GameObject.Find("Kitchen_Stove");
        if (stove != null)
        {
            AddBurner(stove.transform, "Burner_1", new Vector3(0.38f, 0.27f, 0.12f), metalDark);
            AddBurner(stove.transform, "Burner_2", new Vector3(-0.38f, 0.27f, 0.12f), metalDark);
        }


        GameObject window = GameObject.Find("Kitchen_Window");
        if (window != null)
        {
            ApplyMat(window, wood);
            AddGlass(window.transform, "Glass", glass);
        }


        BuildWalls(wallCream);


        SetupLighting();
        SetupPostProcessing();


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

        CreateWall("Wall_Back", new Vector3(RoomCenter.x, WallH / 2f, RoomCenter.z + RoomD / 2f + WallT / 2f),
            new Vector3(RoomW + WallT * 2f, WallH, WallT), wallMat);

        CreateWall("Wall_Left", new Vector3(RoomCenter.x - RoomW / 2f - WallT / 2f, WallH / 2f, RoomCenter.z),
            new Vector3(WallT, WallH, RoomD), wallMat);

        CreateWall("Wall_Right", new Vector3(RoomCenter.x + RoomW / 2f + WallT / 2f, WallH / 2f, RoomCenter.z),
            new Vector3(WallT, WallH, RoomD), wallMat);

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
```

## File: Assets/Editor/WardrobeSetup.cs
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FeaturesWardrobe;
using FeaturesInteraction;















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


        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create WardrobeRoot");
            changed = true;
        }


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



        MeshFilter mf = mirrorGO.GetComponent<MeshFilter>();
        if (mf == null) mf = mirrorGO.AddComponent<MeshFilter>();
        mf.sharedMesh = MirrorQuadMesh();
        mirrorGO.transform.localRotation = Quaternion.identity;
        MeshRenderer mr = mirrorGO.GetComponent<MeshRenderer>();
        if (mr == null) mr = mirrorGO.AddComponent<MeshRenderer>();
        MR(mr);


        Material mirrorMat = LoadOrCreateMaterial("Assets/Materials/Wardrobe/Mat_Cermin.mat",
            new Color(0.85f, 0.9f, 1f, 1f));
        mr.sharedMaterial = mirrorMat;


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


        RenderTexture rt = LoadOrCreateRenderTexture("Assets/Materials/Wardrobe/MirrorTexture.renderTexture");


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


        GameObject lemari = GameObject.Find(LemariName);
        if (lemari != null && lemari.transform.parent != root.transform)
        {
            Undo.SetTransformParent(lemari.transform, root.transform, "Parent Lemari to WardrobeRoot");
            changed = true;
        }

        EnsureComponent<WardrobeInteractable>(lemari);


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


        WardrobeUI wiredUI = WireWardrobeUI(mirrorCam, rt, out bool uiChanged);
        changed |= uiChanged;


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


        CreateSampleOutfits();


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

            changedOut = WireUIExisting(existingUI, mirrorCam, mirrorRT);
            return existingUI;
        }

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


        GameObject btnPrefab = CreateOutfitButtonPrefab(gridRT);


        GameObject saveGO = CreateButton("SaveButton", "Simpan", panel.transform,
            new Vector2(-110f, 60f));
        GameObject cancelGO = CreateButton("CancelButton", "Batal", panel.transform,
            new Vector2(110f, 60f));


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


        if (ApplyGridLayout(ui.transform.Find("OutfitGrid")))
            changed = true;

        return changed;
    }


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
```

## File: Assets/Scripts/Behaviour/IsometricCamera.cs
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

## File: Assets/Scripts/Editor/RoomBuilderEditor.cs
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

## File: Assets/Scripts/Features/Interaction/BedInteractable.cs
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

## File: Assets/Scripts/Features/Interaction/GenericFurnitureInteractable.cs
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

## File: Assets/Scripts/Features/Interaction/Highlightable.cs
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

## File: Assets/Scripts/Features/Interaction/HoverLabelController.cs
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

## File: Assets/Scripts/Features/Interaction/IInteractable.cs
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

## File: Assets/Scripts/Features/Interaction/PlayerInteractor.cs
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

## File: Assets/Scripts/Features/Interaction/StorageInteractable.cs
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

## File: Assets/Scripts/Features/Interaction/TrophyCabinetInteractable.cs
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

## File: Assets/Scripts/Features/Interaction/WorldLabel.cs
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

## File: Assets/Scripts/Features/Inventory/UI/DraggableItem.cs
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

## File: Assets/Scripts/Features/Inventory/UI/ItemDisplayUI.cs
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

## File: Assets/Scripts/Features/Inventory/InventoryComponent.cs
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

## File: Assets/Scripts/Features/Inventory/InventorySlot.cs
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

## File: Assets/Scripts/Features/Inventory/ItemData.cs
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

## File: Assets/Scripts/Features/Kitchen/UI/KitchenStationProgressOverlay.cs
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

## File: Assets/Scripts/Features/Kitchen/UI/KitchenStationSoundFx.cs
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

## File: Assets/Scripts/Features/Kitchen/UI/KitchenStationUI.cs
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

## File: Assets/Scripts/Features/Kitchen/DoorInteractable.cs
```csharp
using UnityEngine;
using FeaturesInteraction;
using FeaturesCommon;






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

    [Header("Fade Effect")]
    [Tooltip("Durasi fade in/out (detik)")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("Apakah menggunakan efek fade saat teleport")]
    [SerializeField] private bool useFadeEffect = true;

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

        if (useFadeEffect && FadeManager.Instance != null)
        {
            StartCoroutine(TeleportWithFade(player, targetSpawn.position, isInside));
        }
        else
        {
            TeleportPlayer(player, targetSpawn.position);
        }

        Debug.Log($"[DoorInteractable] Player teleported to " + (isInside ? "outside" : "inside") + " via " + gameObject.name);
    }

    private System.Collections.IEnumerator TeleportWithFade(PlayerControl player, Vector3 targetPosition, bool isInside)
    {

        yield return FadeManager.Instance.FadeIn(fadeDuration);


        TeleportPlayer(player, targetPosition);


        yield return null;


        yield return FadeManager.Instance.FadeOut(fadeDuration);
    }

    private void TeleportPlayer(PlayerControl player, Vector3 targetPosition)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = targetPosition;
        }
        else
        {
            player.transform.position = targetPosition;
        }
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

## File: Assets/Scripts/Features/Kitchen/KitchenRecipe.cs
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

## File: Assets/Scripts/Features/Kitchen/KitchenSinkInteractable.cs
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

## File: Assets/Scripts/Features/Kitchen/RefrigeratorInteractable.cs
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

## File: Assets/Scripts/Features/Kitchen/StoveInteractable.cs
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

## File: Assets/Scripts/Features/Time/UI/DayTransitionUI.cs
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

## File: Assets/Scripts/Features/Time/TimeManager.cs
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

## File: Assets/Scripts/Features/Trophy/TrophyItem.cs
```csharp
using UnityEngine;



[RequireComponent(typeof(Collider))]
public class TrophyItem : MonoBehaviour
{

    public string trophyName = "Unnamed Trophy";
}
```

## File: Assets/Scripts/Features/Trophy/TrophyRackVisuals.cs
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

## File: Assets/Scripts/Features/Trophy/TrophySnapPoint.cs
```csharp
using UnityEngine;








public class TrophySnapPoint : MonoBehaviour
{




    public int slotIndex = -1;
}
```

## File: Assets/Scripts/Features/Trophy/TrophySystemManager.cs
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

## File: Assets/Scripts/Features/Wardrobe/UI/WardrobeUI.cs
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

## File: Assets/Scripts/Features/Wardrobe/MirrorCamera.cs
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
        private bool _textureCreatedByScript = false;

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
                _textureCreatedByScript = true;
            }
            else
            {
                _textureCreatedByScript = false;
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

            if (mirrorCamera != null && mirrorCamera.enabled && mirrorCamera.targetTexture == null)
            {
                if (mirrorTexture != null)
                {
                    mirrorCamera.targetTexture = mirrorTexture;
                    Debug.LogWarning("[MirrorCamera] LateUpdate: Camera was enabled without targetTexture! Re-bound and keeping enabled.");
                }
                else
                {

                    mirrorCamera.enabled = false;
                    Debug.LogError("[MirrorCamera] LateUpdate: No targetTexture available! Disabling camera to prevent screen rendering.");
                    return;
                }
            }


            if (mirrorCamera != null && mirrorCamera.enabled && mirrorCamera.targetTexture == null)
            {
                mirrorCamera.enabled = false;
                Debug.LogError("[MirrorCamera] LateUpdate: targetTexture became null! Disabling camera.");
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
            if (mirrorTexture != null && _textureCreatedByScript)
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

## File: Assets/Scripts/Features/Wardrobe/OutfitData.cs
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

## File: Assets/Scripts/Features/Wardrobe/PlayerOutfit.cs
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

## File: Assets/Scripts/Features/Wardrobe/WardrobeManager.cs
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
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-2.76f, 1.655f, -2.62f);

        [Tooltip("Local rotation offset (Euler) of wardrobeCamera relative to wardrobeRoot.")]
        [SerializeField] private Vector3 cameraLocalRotation = new Vector3(8f, 60f, 0f);

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
                // CRITICAL FIX: Bind targetTexture FIRST before any camera switching
                if (mirrorCamera != null && mirrorCamera.MirrorTexture != null)
                {
                    mirrorCamera.MirrorCameraComponent.targetTexture = mirrorCamera.MirrorTexture;
                    Debug.Log("[WardrobeManager] MirrorInnerCam targetTexture bound: " + mirrorCamera.MirrorTexture.name);
                }

                // Verify texture is bound BEFORE proceeding
                if (mirrorCamera != null && mirrorCamera.MirrorCameraComponent != null && mirrorCamera.MirrorCameraComponent.targetTexture == null)
                {
                    Debug.LogError("[WardrobeManager] Mirror camera has no targetTexture! Aborting blend.");
                    yield break; // Abort - don't proceed without valid targetTexture
                }

                // NOW disable main camera and enable wardrobe/mirror cameras
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

                // NOW enable MirrorInnerCam (renders to texture) - texture already bound
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

            // Raycast to find floor height - cast from higher up to ensure we hit floor
            Vector3 rayOrigin = target + Vector3.up * 3f;
            bool hitFloor = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f);

            if (hitFloor)
            {
                target.y = hit.point.y + 0.5f;
                Debug.Log($"[WardrobeManager] PositionPlayerToMirror: Floor hit at Y={hit.point.y}, placing player at Y={target.y}");
            }
            else
            {
                // FALLBACK: Use known bedroom floor Y (0) + offset
                // Bedroom floor is at Y=0, place player at 0.5f above
                target.y = 0.5f;
                Debug.LogWarning("[WardrobeManager] PositionPlayerToMirror: Raycast failed! Using fallback Y=0.5f. Ray origin: " + rayOrigin);
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

## File: Assets/Scripts/Player/UI/PlayerHealthUI.cs
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

## File: Assets/Scripts/Player/PlayerControl.cs
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
            float moveDistance = moveSpeed * Time.fixedDeltaTime;


            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            float capsuleRadius = capsuleCollider != null ? capsuleCollider.radius : 0.5f;
            float capsuleHeight = capsuleCollider != null ? capsuleCollider.height : 2.0f;
            Vector3 capsuleCenter = rb.position + Vector3.up * (capsuleHeight * 0.5f);
            float capsuleHalfHeight = (capsuleHeight - capsuleRadius * 2f) * 0.5f;
            float skinWidth = 0.05f;


            RaycastHit sweepHit;
            bool hasHit = Physics.CapsuleCast(
                capsuleCenter + Vector3.down * capsuleHalfHeight - moveDirection * 0.01f,
                capsuleCenter + Vector3.up * capsuleHalfHeight - moveDirection * 0.01f,
                capsuleRadius - 0.01f,
                moveDirection,
                out sweepHit,
                moveDistance + 0.05f,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (hasHit)
            {

                moveDistance = Mathf.Max(0, sweepHit.distance - 0.05f);
            }

            Vector3 newPosition = rb.position + moveDirection * moveDistance;
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

## File: Assets/Scripts/Player/PlayerEquipment.cs
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

## File: Assets/Scripts/Player/PlayerInputActions.cs
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

## File: Assets/Scripts/Player/PlayerStats.cs
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

## File: Assets/Scripts/AutoDoor.cs
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

## File: Assets/Scripts/CameraController.cs
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

## File: Assets/Scripts/FaceCamera.cs
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

## File: Assets/Scripts/GameInitializer.cs
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

## File: Assets/Scripts/InventoryUI.cs
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

## File: Assets/Scripts/PlayerController.cs
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

## File: Assets/Scripts/RoomBuilder.cs
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

## File: Assets/Scripts/WallOcclusionFader.cs
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

## File: Assets/Scripts/Features/Interaction/WardrobeInteractable.cs
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

## File: Assets/Scripts/Features/Inventory/UI/InventorySlotUI.cs
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

## File: Assets/Scripts/Features/Kitchen/KitchenStation.cs
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

## File: Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs
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
