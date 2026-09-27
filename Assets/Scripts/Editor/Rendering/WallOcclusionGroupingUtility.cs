using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FeaturesCamera;

namespace FarmBeware.Editor.Rendering
{
    /// <summary>
    /// Editor utility to configure WallOcclusionGroup components on modular walls.
    /// Groups modular segments per room wall side so that camera occlusion transparency
    /// triggers per entire wall rather than per individual 2-meter modular piece.
    /// </summary>
    public static class WallOcclusionGroupingUtility
    {
        private const string MENU_PATH = "Tools/Farm-Beware/Rendering/🧱 Setup Modular Wall Occlusion Groups";

        [MenuItem(MENU_PATH, priority = 150)]
        public static void SetupAllWallGroups()
        {
            int groupsCreated = 0;
            int occludersGrouped = 0;

            // 1. Bedroom
            var bedroomWall = GameObject.Find("_WORLD/Zones/Bedroom/Wall");
            WallOcclusionGroup southGroup = null;
            WallOcclusionGroup westGroup = null;

            if (bedroomWall != null)
            {
                groupsCreated += SetupZoneGroups(bedroomWall.transform, "Bedroom", new Dictionary<string, string[]>
                {
                    { "Bedroom_South", new string[] { "Wall_South" } },
                    { "Bedroom_West", new string[] { "Wall_West", "DoorFrame_West_2m", "BedroomDoor" } },
                    { "Bedroom_North", new string[] { "Wall_North" } },
                    { "Bedroom_East", new string[] { "Wall_East", "Window_East_2m" } }
                }, ref occludersGrouped);

                var southGroupTrans = bedroomWall.transform.Find("Group_Bedroom_South");
                var westGroupTrans = bedroomWall.transform.Find("Group_Bedroom_West");
                southGroup = southGroupTrans != null ? southGroupTrans.GetComponent<WallOcclusionGroup>() : null;
                westGroup = westGroupTrans != null ? westGroupTrans.GetComponent<WallOcclusionGroup>() : null;
            }

            // Fallback/Direct lookup for modular house hierarchy
            if (southGroup == null)
            {
                var sg = GameObject.Find("Group_Bedroom_South");
                if (sg != null) southGroup = sg.GetComponent<WallOcclusionGroup>();
            }
            if (westGroup == null)
            {
                var wg = GameObject.Find("Group_Bedroom_West");
                if (wg != null) westGroup = wg.GetComponent<WallOcclusionGroup>();
            }

            // 1a. Configure Bedroom Mirror and Frame as additionalRenderers of the wall behind them
            var mirrorObj = GameObject.Find("_WORLD/Zones/BedroomZone/MirrorRoot/Mirror") ?? GameObject.Find("Mirror");
            var frameObj = GameObject.Find("_WORLD/Zones/BedroomZone/MirrorRoot/Frame") ?? GameObject.Find("Frame");

            if (mirrorObj != null)
            {
                mirrorObj.layer = 0; // Default layer: camera rays pass through to wall
                var mirrorOcc = mirrorObj.GetComponent<WallOccluder>();
                if (mirrorOcc != null) Object.DestroyImmediate(mirrorOcc);
                EditorUtility.SetDirty(mirrorObj);
            }
            if (frameObj != null)
            {
                frameObj.layer = 0;
                var frameOcc = frameObj.GetComponent<WallOccluder>();
                if (frameOcc != null) Object.DestroyImmediate(frameOcc);
                EditorUtility.SetDirty(frameObj);
            }

            // Remove mirror/frame from group occluder lists if mistakenly added
            if (westGroup != null)
            {
                westGroup.occluders.RemoveAll(o => o == null || o.gameObject == mirrorObj || o.gameObject == frameObj);
                EditorUtility.SetDirty(westGroup);
            }
            if (southGroup != null)
            {
                southGroup.occluders.RemoveAll(o => o == null || o.gameObject == mirrorObj || o.gameObject == frameObj);
                EditorUtility.SetDirty(southGroup);
            }

            // Attach Mirror & Frame renderers to wall_2m_bedroom.002's WallOccluder (South wall behind mirror)
            var wallBehindMirror = GameObject.Find("wall_2m_bedroom.002") ?? GameObject.Find("wall_2m_bedroom.001");
            if (wallBehindMirror != null)
            {
                var wallOcc = wallBehindMirror.GetComponent<WallOccluder>();
                if (wallOcc != null)
                {
                    wallOcc.AdditionalRenderers.RemoveAll(r => r == null);
                    if (mirrorObj != null)
                    {
                        var mRend = mirrorObj.GetComponent<Renderer>();
                        if (mRend != null && !wallOcc.AdditionalRenderers.Contains(mRend))
                            wallOcc.AdditionalRenderers.Add(mRend);
                    }
                    if (frameObj != null)
                    {
                        var fRend = frameObj.GetComponent<Renderer>();
                        if (fRend != null && !wallOcc.AdditionalRenderers.Contains(fRend))
                            wallOcc.AdditionalRenderers.Add(fRend);
                    }
                    wallOcc.HideAdditionalRenderersOnFade = true;
                    EditorUtility.SetDirty(wallOcc);
                }
            }

            // 2. Kitchen
            var kitchenWall = GameObject.Find("_WORLD/Zones/Kitchen/Wall");
            if (kitchenWall != null)
            {
                groupsCreated += SetupZoneGroups(kitchenWall.transform, "Kitchen", new Dictionary<string, string[]>
                {
                    { "Kitchen_South", new string[] { "Wall_South" } },
                    { "Kitchen_West", new string[] { "Wall_West", "Window_West_2m" } },
                    { "Kitchen_North", new string[] { "Wall_North", "DoorFrame_North" } },
                    { "Kitchen_East", new string[] { "Wall_East" } }
                }, ref occludersGrouped);
            }

            // 3. Warehouse
            var warehouseWall = GameObject.Find("_WORLD/Zones/Warehouse/Wall");
            if (warehouseWall != null)
            {
                groupsCreated += SetupZoneGroups(warehouseWall.transform, "Warehouse", new Dictionary<string, string[]>
                {
                    { "Warehouse_South", new string[] { "Wall_South", "Window_South_2m" } },
                    { "Warehouse_East", new string[] { "Wall_East", "Window_East_2m" } },
                    { "Warehouse_North", new string[] { "Wall_North", "DoorFrame_North" } }
                }, ref occludersGrouped);
            }

            // 4. Decouple ALL linked groups across the entire house (strictly modular)
            var allGroups = Object.FindObjectsByType<WallOcclusionGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var g in allGroups)
            {
                g.linkedGroups.Clear();
                // Ensure no corners are inside groups
                g.occluders.RemoveAll(o => o == null || o.name.ToLower().Contains("corner"));
                EditorUtility.SetDirty(g);
            }

            // Ensure door_warehouse.001 is placed in Group_Warehouse_North (at Z=12.46), NOT Group_Warehouse_South (at Z=5.26)
            var whSouth = GameObject.Find("Group_Warehouse_South")?.GetComponent<WallOcclusionGroup>();
            var whNorth = GameObject.Find("Group_Warehouse_North")?.GetComponent<WallOcclusionGroup>();
            var whDoor = GameObject.Find("door_warehouse.001");
            if (whDoor != null)
            {
                var dOcc = whDoor.GetComponent<WallOccluder>();
                if (dOcc != null)
                {
                    if (whSouth != null)
                    {
                        whSouth.occluders.Remove(dOcc);
                        EditorUtility.SetDirty(whSouth);
                    }
                    if (whNorth != null)
                    {
                        if (!whNorth.occluders.Contains(dOcc)) whNorth.occluders.Add(dOcc);
                        dOcc.Group = whNorth;
                        EditorUtility.SetDirty(whNorth);
                        EditorUtility.SetDirty(dOcc);
                    }
                }
            }

            // 5. Ensure all corner objects are 100% individual (Group = null)
            var allOccluders = Object.FindObjectsByType<WallOccluder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var occ in allOccluders)
            {
                if (occ.name.ToLower().Contains("corner"))
                {
                    occ.Group = null;
                    EditorUtility.SetDirty(occ);
                }
            }

            // 6. Clear RoomTrigger whole-room bypasses so occlusion is 100% precision Line-of-Sight
            var occlusionManager = Object.FindAnyObjectByType<WallOcclusionManager>();
            if (occlusionManager != null)
            {
                occlusionManager.RoomTriggers.Clear();
                EditorUtility.SetDirty(occlusionManager);
            }

            // 7. Setup Wall Lamps as AdditionalRenderers on the modular walls they are mounted on
            int lampsLinked = SetupWallLamps();

            // Mark dirty and save
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && !Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[WallOcclusionGroupingUtility] Sukses! Dibuat {groupsCreated} WallOcclusionGroup, {occludersGrouped} wall occluders, dan {lampsLinked} wall lamps terhubung.");
        }

        public static int SetupWallLamps()
        {
            int configuredCount = 0;
            var allOccs = Object.FindObjectsByType<WallOccluder>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Clean up any previously registered lamp renderers from all occluders first (except Mirror and Frame)
            foreach (var occ in allOccs)
            {
                occ.AdditionalRenderers.RemoveAll(r => r == null || r.gameObject.name.StartsWith("Lantern") ||
                    r.gameObject.name.StartsWith("Mount") || r.gameObject.name.StartsWith("Arm") || r.gameObject.name.StartsWith("Post_"));
            }

            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var lampRoots = new List<Transform>();
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith("WallLamp_") && t.parent != null && t.parent.name.StartsWith("Room_"))
                {
                    lampRoots.Add(t);
                }
            }

            foreach (var lamp in lampRoots)
            {
                var mountPlate = lamp.Find("MountPlate");
                Vector3 checkPos = mountPlate != null ? mountPlate.position : lamp.position;

                WallOccluder bestWall = null;
                float bestDist = float.MaxValue;

                foreach (var occ in allOccs)
                {
                    var rend = occ.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        float d = Vector3.Distance(checkPos, rend.bounds.ClosestPoint(checkPos));
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestWall = occ;
                        }
                    }
                }

                if (bestWall != null && bestDist < 0.20f)
                {
                    var childRenderers = lamp.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in childRenderers)
                    {
                        if (!bestWall.AdditionalRenderers.Contains(r))
                        {
                            bestWall.AdditionalRenderers.Add(r);
                        }
                    }
                    // Wall lamps fade transparently with the wall (transparentAlpha)
                    bestWall.HideAdditionalRenderersOnFade = false;
                    bestWall.Reinitialize();
                    EditorUtility.SetDirty(bestWall);
                    configuredCount++;
                    Debug.Log($"[WallOcclusionGroupingUtility] Linked {lamp.name} ({childRenderers.Length} renderers) to wall {bestWall.name} (dist: {bestDist:F3}m, group: {(bestWall.Group != null ? bestWall.Group.groupName : "none")})");
                }
                else
                {
                    Debug.LogWarning($"[WallOcclusionGroupingUtility] Could not find close wall for {lamp.name} (best dist: {bestDist:F3}m)");
                }
            }

            return configuredCount;
        }

        private static int SetupZoneGroups(Transform parent, string zoneName, Dictionary<string, string[]> groupRules, ref int totalOccluders)
        {
            int created = 0;

            foreach (var kvp in groupRules)
            {
                string groupName = kvp.Key;
                string[] matchKeywords = kvp.Value;

                // Find or create group container under parent
                string containerName = "Group_" + groupName;
                Transform groupObj = parent.Find(containerName);
                if (groupObj == null)
                {
                    var go = new GameObject(containerName);
                    go.transform.SetParent(parent, false);
                    groupObj = go.transform;
                }

                var groupComp = groupObj.GetComponent<WallOcclusionGroup>();
                if (groupComp == null)
                {
                    groupComp = groupObj.gameObject.AddComponent<WallOcclusionGroup>();
                }

                groupComp.groupName = groupName;
                groupComp.occluders.Clear();

                // Find all matching children under parent (direct children)
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child.name.StartsWith("Group_")) continue;

                    bool matches = false;
                    foreach (var kw in matchKeywords)
                    {
                        if (child.name.Contains(kw))
                        {
                            matches = true;
                            break;
                        }
                    }

                    if (matches)
                    {
                        int wallLayer = LayerMask.NameToLayer("Wall");
                        if (wallLayer != -1 && child.gameObject.layer != wallLayer)
                        {
                            child.gameObject.layer = wallLayer;
                            EditorUtility.SetDirty(child.gameObject);
                        }

                        var occluder = child.GetComponent<WallOccluder>();
                        if (occluder == null)
                        {
                            occluder = child.gameObject.AddComponent<WallOccluder>();
                        }

                        if (!groupComp.occluders.Contains(occluder))
                        {
                            groupComp.occluders.Add(occluder);
                            occluder.Group = groupComp;
                            totalOccluders++;
                        }
                    }
                }

                groupComp.RegisterMembers();
                EditorUtility.SetDirty(groupComp);
                created++;
            }

            return created;
        }
    }
}
