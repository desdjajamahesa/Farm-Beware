using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FarmBeware.Editor.Rendering
{
    /// <summary>
    /// Utilitas Editor untuk Milestone 3: Probe Infrastructure & Indirect GI Setup.
    /// Mengotomasi:
    /// 1. Konfigurasi renderingLayerMask = 3 (Layer 1 | Layer 2) pada semua prefab Player & Enemy.
    /// 2. Pembentukan hierarki Reflection Probes (1 Global Exterior + 6 Local Interior ber-Box Projection).
    /// 3. Pembuatan stratified LightProbeGroup dengan proteksi clearance radius 0.25m dari collider solid.
    /// 4. Penandaan StaticEditorFlags.ContributeGI pada struktur statis scene.
    /// 5. Penguncian Directional Sun ke LightmapBakeType.Realtime untuk keamanan GI siklus 24 jam.
    /// </summary>
    public static class ProbeInfrastructureSetupUtility
    {
        private const string MENU_SETUP_M3 = "Tools/Farm-Beware/Lighting/⚡ Setup Probe Infrastructure (Milestone 3)";
        private const string MENU_VALIDATE_M3 = "Tools/Farm-Beware/Lighting/🔍 Validate Probe Setup (Milestone 3)";

        private const string ROOT_LIGHTING_ENV = "_LIGHTING_ENVIRONMENT";
        private const string GROUP_INDIRECT = "Indirect_Lighting";
        private const string GROUP_REFLECTION = "Reflection_Probes";
        private const string NAME_GLOBAL_RP = "ReflectionProbe_GlobalExterior";
        private const string NAME_LIGHT_PROBES = "World_LightProbeGroup";

        private const float CLEARANCE_RADIUS = 0.25f;

        // Definisi batas 6 ruangan interior (Center & Size) dari RoomZone BoxColliders
        private struct RoomBounds
        {
            public string Name;
            public Vector3 Center;
            public Vector3 Size;

            public RoomBounds(string name, Vector3 center, Vector3 size)
            {
                Name = name;
                Center = center;
                Size = size;
            }
        }

        private static readonly RoomBounds[] InteriorRooms = new RoomBounds[]
        {
            new RoomBounds("LivingRoom", new Vector3(15.355f, 2.0f, 22.26f), new Vector3(11.8f, 4.0f, 7.8f)),
            new RoomBounds("Foyer",      new Vector3(19.8f,   2.0f, 22.26f), new Vector3(5.0f,  4.0f, 7.0f)),
            new RoomBounds("Kitchen",    new Vector3(14.85f,  2.0f, 8.86f),  new Vector3(10.8f, 4.0f, 8.4f)),
            new RoomBounds("Warehouse",  new Vector3(25.65f,  2.0f, 8.86f),  new Vector3(11.6f, 4.0f, 8.4f)),
            new RoomBounds("Bedroom",    new Vector3(27.52f,  2.0f, 22.26f), new Vector3(9.8f,  4.0f, 7.2f)),
            new RoomBounds("Hallway",    new Vector3(22.55f,  2.0f, 15.56f), new Vector3(17.8f, 4.0f, 6.6f))
        };

        [MenuItem(MENU_SETUP_M3, priority = 200)]
        public static void ExecuteMilestone3Setup()
        {
            Debug.Log("<color=#38BDF8><b>[ProbeSetupUtility]</b> Memulai eksekusi Milestone 3: Probe Infrastructure & Indirect GI Setup...</color>");

            // 1. Entity Rendering Layer Masks
            int updatedPrefabs = SetupDynamicEntityLayerMasks();

            // 2. Reflection Probe Hierarchy
            Transform indirectRoot = EnsureIndirectLightingHierarchy();
            SetupReflectionProbes(indirectRoot);

            // 3. Light Probe Group dengan 0.25m clearance radius check
            int probeCount = SetupLightProbeGroup(indirectRoot);

            // 4. Static GI Flags
            int staticObjectsCount = FlagStaticStructuresContributeGI();

            // 5. Day/Night 24h Bake Safety
            EnsureDayNightBakeSafety();

            // 6. Mark Scene Dirty & Save
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=#4ADE80><b>[ProbeSetupUtility]</b> Milestone 3 berhasil diselesaikan!\n" +
                      $"• Prefabs Updated: {updatedPrefabs}\n" +
                      $"• Reflection Probes: 1 Global Exterior + 6 Interior (Box Projected)\n" +
                      $"• Light Probes Generated: {probeCount} probes (0.25m obstacle clearance verified)\n" +
                      $"• Static GI Flagged: {staticObjectsCount} structures/objects\n" +
                      $"• 24h Bake Safety: Sun locked to Realtime mode\n" +
                      $"• Scene tersimpan: {activeScene.path}</color>");
        }

        [MenuItem(MENU_VALIDATE_M3, priority = 201)]
        public static void ValidateMilestone3Setup()
        {
            Debug.Log("<color=#38BDF8><b>[ProbeSetupUtility]</b> Menjalankan validasi Milestone 3...</color>");

            // Validasi Prefabs
            var targetPrefabPaths = GetDynamicEntityPrefabPaths();
            int validPrefabs = 0;
            foreach (var path in targetPrefabPaths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                bool allValid = true;
                foreach (var r in renderers)
                {
                    if (r.renderingLayerMask != 3)
                    {
                        allValid = false;
                        Debug.LogWarning($"[Validate] Prefab {path} -> Renderer {r.name} renderingLayerMask = {r.renderingLayerMask} (diharapkan 3)!");
                    }
                }
                if (allValid) validPrefabs++;
            }
            Debug.Log($"[Validate] Prefabs Valid: {validPrefabs}/{targetPrefabPaths.Count}");

            // Validasi Reflection Probes
            var rps = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            Debug.Log($"[Validate] Reflection Probes ditemukan di scene: {rps.Length}");
            foreach (var rp in rps)
            {
                Debug.Log($" • {rp.name}: BoxProj={rp.boxProjection}, Size={rp.size}, Importance={rp.importance}, BlendDist={rp.blendDistance}m");
            }

            // Validasi Light Probe Group
            var lpg = UnityEngine.Object.FindFirstObjectByType<LightProbeGroup>();
            if (lpg != null)
            {
                Debug.Log($"[Validate] LightProbeGroup '{lpg.name}' aktif dengan {lpg.probePositions.Length} probes.");
            }
            else
            {
                Debug.LogError("[Validate] LightProbeGroup tidak ditemukan di scene!");
            }
        }

        #region 1. Dynamic Entity Rendering Layer Masks

        public static int SetupDynamicEntityLayerMasks()
        {
            var prefabPaths = GetDynamicEntityPrefabPaths();
            int modifiedCount = 0;

            foreach (var path in prefabPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[ProbeSetupUtility] Prefab tidak ditemukan pada path: {path}");
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                try
                {
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                    bool wasModified = false;

                    foreach (var rend in renderers)
                    {
                        // Mask 3 = Light Layer 1 (Outdoor) | Light Layer 2 (Interior)
                        if (rend.renderingLayerMask != 3)
                        {
                            rend.renderingLayerMask = 3;
                            EditorUtility.SetDirty(rend);
                            wasModified = true;
                        }
                    }

                    if (wasModified)
                    {
                        EditorUtility.SetDirty(root);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        modifiedCount++;
                        Debug.Log($"[ProbeSetupUtility] Prefab diperbarui ke renderingLayerMask = 3: {path} ({renderers.Length} renderers)");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            return modifiedCount;
        }

        private static List<string> GetDynamicEntityPrefabPaths()
        {
            var paths = new List<string>
            {
                "Assets/AssetStore/Player/prefabs/character_player.prefab",
                "Assets/AssetStore/Player/prefabs/character.prefab"
            };

            // Prefabs di Assets/Resources/Enemies/
            string resourcesEnemies = "Assets/Resources/Enemies";
            if (Directory.Exists(resourcesEnemies))
            {
                var files = Directory.GetFiles(resourcesEnemies, "*.prefab", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    paths.Add(f.Replace("\\", "/"));
                }
            }

            // Prefabs di Assets/Prefabs/Enemies/
            string prefabsEnemies = "Assets/Prefabs/Enemies";
            if (Directory.Exists(prefabsEnemies))
            {
                var files = Directory.GetFiles(prefabsEnemies, "*.prefab", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    string formatted = f.Replace("\\", "/");
                    if (!paths.Contains(formatted))
                    {
                        paths.Add(formatted);
                    }
                }
            }

            return paths;
        }

        #endregion

        #region 2. Reflection Probe Hierarchy

        private static Transform EnsureIndirectLightingHierarchy()
        {
            GameObject lightingEnv = GameObject.Find(ROOT_LIGHTING_ENV);
            if (lightingEnv == null)
            {
                lightingEnv = new GameObject(ROOT_LIGHTING_ENV);
                lightingEnv.transform.position = Vector3.zero;
            }

            Transform indirectTransform = lightingEnv.transform.Find(GROUP_INDIRECT);
            if (indirectTransform == null)
            {
                GameObject indirectGO = new GameObject(GROUP_INDIRECT);
                indirectGO.transform.SetParent(lightingEnv.transform, false);
                indirectGO.transform.localPosition = Vector3.zero;
                indirectTransform = indirectGO.transform;
            }

            return indirectTransform;
        }

        public static void SetupReflectionProbes(Transform indirectRoot)
        {
            Transform reflectionGroup = indirectRoot.Find(GROUP_REFLECTION);
            if (reflectionGroup == null)
            {
                GameObject reflGO = new GameObject(GROUP_REFLECTION);
                reflGO.transform.SetParent(indirectRoot, false);
                reflGO.transform.localPosition = Vector3.zero;
                reflectionGroup = reflGO.transform;
            }

            // Culling mask exclude Player and Enemy layers if present
            int dynamicLayers = LayerMask.GetMask("Player", "Enemy");
            int cullingMask = ~dynamicLayers;
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) cullingMask &= ~(1 << uiLayer);
            int previewLayer = LayerMask.NameToLayer("PreviewLayer");
            if (previewLayer != -1) cullingMask &= ~(1 << previewLayer);

            // 1. Global Exterior Probe
            Transform globalTransform = reflectionGroup.Find(NAME_GLOBAL_RP);
            GameObject globalGO;
            if (globalTransform == null)
            {
                globalGO = new GameObject(NAME_GLOBAL_RP);
                globalGO.transform.SetParent(reflectionGroup, false);
            }
            else
            {
                globalGO = globalTransform.gameObject;
            }

            globalGO.transform.position = new Vector3(21.0f, 4.0f, 15.5f);
            var globalProbe = globalGO.GetComponent<ReflectionProbe>();
            if (globalProbe == null) globalProbe = globalGO.AddComponent<ReflectionProbe>();

            globalProbe.mode = ReflectionProbeMode.Baked;
            globalProbe.boxProjection = false;
            globalProbe.importance = 1;
            globalProbe.resolution = 128;
            globalProbe.blendDistance = 2.0f;
            globalProbe.size = new Vector3(55.0f, 20.0f, 75.0f);
            globalProbe.center = Vector3.zero;
            globalProbe.cullingMask = cullingMask;

            // 2. 6 Interior Probes
            foreach (var room in InteriorRooms)
            {
                string probeName = $"ReflectionProbe_{room.Name}";
                Transform roomTransform = reflectionGroup.Find(probeName);
                GameObject roomGO;
                if (roomTransform == null)
                {
                    roomGO = new GameObject(probeName);
                    roomGO.transform.SetParent(reflectionGroup, false);
                }
                else
                {
                    roomGO = roomTransform.gameObject;
                }

                roomGO.transform.position = room.Center;
                var roomProbe = roomGO.GetComponent<ReflectionProbe>();
                if (roomProbe == null) roomProbe = roomGO.AddComponent<ReflectionProbe>();

                roomProbe.mode = ReflectionProbeMode.Baked;
                roomProbe.boxProjection = true;
                roomProbe.importance = 2;
                roomProbe.resolution = 128;
                roomProbe.blendDistance = 0.5f;
                roomProbe.size = room.Size;
                roomProbe.center = Vector3.zero;
                roomProbe.cullingMask = cullingMask;
            }

            Debug.Log("[ProbeSetupUtility] Hierarki Reflection Probes (1 Global + 6 Interior) berhasil dikonfigurasi.");
        }

        #endregion

        #region 3. Stratified Light Probe Group & 0.25m Clearance

        public static int SetupLightProbeGroup(Transform indirectRoot)
        {
            Transform lpgTransform = indirectRoot.Find(NAME_LIGHT_PROBES);
            GameObject lpgGO;
            if (lpgTransform == null)
            {
                lpgGO = new GameObject(NAME_LIGHT_PROBES);
                lpgGO.transform.SetParent(indirectRoot, false);
                lpgGO.transform.localPosition = Vector3.zero;
            }
            else
            {
                lpgGO = lpgTransform.gameObject;
            }

            var lpg = lpgGO.GetComponent<LightProbeGroup>();
            if (lpg == null) lpg = lpgGO.AddComponent<LightProbeGroup>();

            // Kumpulkan solid colliders dan wall layer mask untuk clearance check
            Collider[] solidColliders = CollectSolidColliders();
            int wallMask = LayerMask.GetMask("Wall", "Default");

            List<Vector3> validWorldPositions = new List<Vector3>();

            // A. Interior Safe Zone: grid 0.85m pada Y = 0.35m dan 1.6m
            float[] interiorHeights = new float[] { 0.35f, 1.6f };
            float interiorSpacing = 0.85f;

            foreach (var room in InteriorRooms)
            {
                float minX = room.Center.x - (room.Size.x * 0.5f) + 0.4f;
                float maxX = room.Center.x + (room.Size.x * 0.5f) - 0.4f;
                float minZ = room.Center.z - (room.Size.z * 0.5f) + 0.4f;
                float maxZ = room.Center.z + (room.Size.z * 0.5f) - 0.4f;

                foreach (float y in interiorHeights)
                {
                    for (float x = minX; x <= maxX; x += interiorSpacing)
                    {
                        for (float z = minZ; z <= maxZ; z += interiorSpacing)
                        {
                            Vector3 candidate = new Vector3(x, y, z);
                            if (HasSolidClearance(candidate, CLEARANCE_RADIUS, solidColliders, wallMask))
                            {
                                validWorldPositions.Add(candidate);
                            }
                        }
                    }
                }
            }

            // B. Doorway Transition Portal: 0.5m cluster around (X: 19-21, Z: 18-20)
            float doorwaySpacing = 0.5f;
            foreach (float y in interiorHeights)
            {
                for (float x = 18.8f; x <= 21.2f; x += doorwaySpacing)
                {
                    for (float z = 17.8f; z <= 20.2f; z += doorwaySpacing)
                    {
                        Vector3 candidate = new Vector3(x, y, z);
                        if (!ContainsPositionClose(validWorldPositions, candidate, 0.3f))
                        {
                            if (HasSolidClearance(candidate, CLEARANCE_RADIUS, solidColliders, wallMask))
                            {
                                validWorldPositions.Add(candidate);
                            }
                        }
                    }
                }
            }

            // C. Outdoor Farmland & Paths: grid 2.2m pada Y = 0.4m dan 1.8m
            float[] outdoorHeights = new float[] { 0.4f, 1.8f };
            float outdoorSpacing = 2.2f;
            float outMinX = 4.0f, outMaxX = 38.0f;
            float outMinZ = -8.0f, outMaxZ = 42.0f;

            // Bounding box luar House untuk dikecualikan dari grid outdoor
            Bounds houseExcludeBounds = new Bounds(new Vector3(20.5f, 2.0f, 15.5f), new Vector3(24.0f, 6.0f, 23.0f));

            foreach (float y in outdoorHeights)
            {
                for (float x = outMinX; x <= outMaxX; x += outdoorSpacing)
                {
                    for (float z = outMinZ; z <= outMaxZ; z += outdoorSpacing)
                    {
                        Vector3 candidate = new Vector3(x, y, z);
                        if (houseExcludeBounds.Contains(candidate)) continue;

                        if (!ContainsPositionClose(validWorldPositions, candidate, 1.0f))
                        {
                            if (HasSolidClearance(candidate, CLEARANCE_RADIUS, solidColliders, wallMask))
                            {
                                validWorldPositions.Add(candidate);
                            }
                        }
                    }
                }
            }

            // Konversi ke local space transform World_LightProbeGroup
            Vector3[] localPositions = new Vector3[validWorldPositions.Count];
            for (int i = 0; i < validWorldPositions.Count; i++)
            {
                localPositions[i] = lpgGO.transform.InverseTransformPoint(validWorldPositions[i]);
            }

            lpg.probePositions = localPositions;
            EditorUtility.SetDirty(lpg);

            Debug.Log($"[ProbeSetupUtility] LightProbeGroup '{NAME_LIGHT_PROBES}' dikonfigurasi dengan {localPositions.Length} probes.");
            return localPositions.Length;
        }

        private static bool HasSolidClearance(Vector3 pos, float radius, Collider[] solidColliders, int wallLayerMask)
        {
            // 1. Physics CheckSphere test
            if (Physics.CheckSphere(pos, radius, wallLayerMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // 2. Pemeriksaan geometri bounds dan closest point terhadap collider solid
            for (int i = 0; i < solidColliders.Length; i++)
            {
                Collider col = solidColliders[i];
                if (col == null || col.isTrigger) continue;

                // Expand bounds by radius for quick elimination
                Bounds expanded = col.bounds;
                expanded.Expand(radius * 2.0f);
                if (!expanded.Contains(pos)) continue;

                try
                {
                    Vector3 closest = col.ClosestPoint(pos);
                    if (Vector3.Distance(closest, pos) < radius)
                    {
                        return false;
                    }
                }
                catch
                {
                    // Non-convex mesh colliders may throw; if inside bounds, reject to be safe
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsPositionClose(List<Vector3> list, Vector3 target, float minDistance)
        {
            float sqrMin = minDistance * minDistance;
            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i] - target).sqrMagnitude < sqrMin) return true;
            }
            return false;
        }

        private static Collider[] CollectSolidColliders()
        {
            var allColliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            var solids = new List<Collider>();

            foreach (var c in allColliders)
            {
                if (c != null && !c.isTrigger)
                {
                    solids.Add(c);
                }
            }

            return solids.ToArray();
        }

        #endregion

        #region 4. Static GI Geometry Flagging

        public static int FlagStaticStructuresContributeGI()
        {
            int flaggedCount = 0;
            string[] rootNamesToFlag = new string[]
            {
                "_WORLD/Structures/House",
                "_WORLD/Pathways",
                "_WORLD/Structures/House_UnifiedFloorCollider",
                "_WORLD/Structures/WaterWell",
                "_WORLD/Structures/NaturalPond",
                "_WORLD/Terrain"
            };

            foreach (var path in rootNamesToFlag)
            {
                GameObject target = GameObject.Find(path);
                if (target != null)
                {
                    flaggedCount += FlagRecursively(target);
                }
            }

            // Cari juga struktur outdoor lainnya seperti shelter, campfire, dan perimeter
            var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go.name.Contains("Perimeter") || go.name.Contains("CraftingShelter") ||
                    go.name.Contains("Campfire") || go.name.Contains("Cobblestone"))
                {
                    flaggedCount += FlagRecursively(go);
                }
            }

            Debug.Log($"[ProbeSetupUtility] {flaggedCount} objek berhasil ditandai dengan StaticEditorFlags.ContributeGI.");
            return flaggedCount;
        }

        private static int FlagRecursively(GameObject go)
        {
            int count = 0;
            var currentFlags = GameObjectUtility.GetStaticEditorFlags(go);
            if (!currentFlags.HasFlag(StaticEditorFlags.ContributeGI))
            {
                GameObjectUtility.SetStaticEditorFlags(go, currentFlags | StaticEditorFlags.ContributeGI);
                EditorUtility.SetDirty(go);
                count++;
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                count += FlagRecursively(go.transform.GetChild(i).gameObject);
            }

            return count;
        }

        #endregion

        #region 5. 24h Day/Night Bake Safety

        public static void EnsureDayNightBakeSafety()
        {
            // Pastikan Main Directional Light berstatus LightmapBakeType.Realtime
            // Hal ini mutlak penting agar cahaya matahari kuning siang hari TIDAK terbakar ke Light Probes!
            Light mainSun = RenderSettings.sun;
            if (mainSun != null)
            {
                if (mainSun.lightmapBakeType != LightmapBakeType.Realtime)
                {
                    mainSun.lightmapBakeType = LightmapBakeType.Realtime;
                    EditorUtility.SetDirty(mainSun);
                    Debug.Log("[ProbeSetupUtility] RenderSettings.sun dikunci ke LightmapBakeType.Realtime.");
                }
            }

            var dirLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in dirLights)
            {
                if (l.type == LightType.Directional && l.lightmapBakeType != LightmapBakeType.Realtime)
                {
                    l.lightmapBakeType = LightmapBakeType.Realtime;
                    EditorUtility.SetDirty(l);
                    Debug.Log($"[ProbeSetupUtility] Directional Light '{l.name}' dikunci ke LightmapBakeType.Realtime.");
                }
            }
        }

        #endregion
    }
}
