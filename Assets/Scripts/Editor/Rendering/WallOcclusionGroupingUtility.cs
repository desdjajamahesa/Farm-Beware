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
                    { "Bedroom_South", new string[] { "Wall_South", "Corner_SW", "Corner_SE" } },
                    { "Bedroom_West", new string[] { "Wall_West", "DoorFrame_West_2m", "BedroomDoor" } },
                    { "Bedroom_North", new string[] { "Wall_North", "Corner_NW", "Corner_NE" } },
                    { "Bedroom_East", new string[] { "Wall_East", "Window_East_2m" } }
                }, ref occludersGrouped);

                // 1a. Link Bedroom foreground walls (South and West) so both fade together seamlessly
                var southGroupTrans = bedroomWall.transform.Find("Group_Bedroom_South");
                var westGroupTrans = bedroomWall.transform.Find("Group_Bedroom_West");
                southGroup = southGroupTrans != null ? southGroupTrans.GetComponent<WallOcclusionGroup>() : null;
                westGroup = westGroupTrans != null ? westGroupTrans.GetComponent<WallOcclusionGroup>() : null;

                if (southGroup != null && westGroup != null)
                {
                    if (!southGroup.linkedGroups.Contains(westGroup)) southGroup.linkedGroups.Add(westGroup);
                    if (!westGroup.linkedGroups.Contains(southGroup)) westGroup.linkedGroups.Add(southGroup);
                    EditorUtility.SetDirty(southGroup);
                    EditorUtility.SetDirty(westGroup);
                }

                // 1b. Configure Mirror as child renderer of Wall_West_2m_Mid (eliminating dual-controller conflicts)
                var mirrorObj = GameObject.Find("_WORLD/Zones/Bedroom/Wardrobe/Mirror") ?? GameObject.Find("Mirror");
                if (mirrorObj != null)
                {
                    // Mirror should be on Default layer (0) so camera rays pass directly through to Wall_West_2m_Mid
                    mirrorObj.layer = 0;

                    // Remove standalone WallOccluder on Mirror to prevent dual-fade conflicts
                    var mirrorOcc = mirrorObj.GetComponent<WallOccluder>();
                    if (mirrorOcc != null)
                    {
                        Object.DestroyImmediate(mirrorOcc);
                    }

                    // Remove mirror from group occluders list if previously registered
                    if (westGroup != null)
                    {
                        westGroup.occluders.RemoveAll(o => o == null || o.gameObject == mirrorObj);
                        EditorUtility.SetDirty(westGroup);
                    }
                    if (southGroup != null)
                    {
                        southGroup.occluders.RemoveAll(o => o == null || o.gameObject == mirrorObj);
                        EditorUtility.SetDirty(southGroup);
                    }

                    // Attach Mirror renderer to Wall_West_2m_Mid's WallOccluder
                    Transform midWallTrans = null;
                    for (int i = 0; i < bedroomWall.transform.childCount; i++)
                    {
                        var c = bedroomWall.transform.GetChild(i);
                        if (c.name.Contains("Wall_West_2m_Mid"))
                        {
                            midWallTrans = c;
                            break;
                        }
                    }

                    if (midWallTrans != null)
                    {
                        var midOcc = midWallTrans.GetComponent<WallOccluder>();
                        if (midOcc != null)
                        {
                            var mirrorRenderer = mirrorObj.GetComponent<Renderer>();
                            midOcc.AdditionalRenderers.RemoveAll(r => r == null);
                            if (mirrorRenderer != null && !midOcc.AdditionalRenderers.Contains(mirrorRenderer))
                            {
                                midOcc.AdditionalRenderers.Add(mirrorRenderer);
                            }
                            midOcc.HideAdditionalRenderersOnFade = true;
                            EditorUtility.SetDirty(midOcc);
                        }
                    }

                    EditorUtility.SetDirty(mirrorObj);
                }

                // 1c. Configure BedroomZone Collider Bounds
                var bedroomZoneObj = GameObject.Find("_WORLD/Zones/Bedroom/BedroomZone") ?? GameObject.Find("BedroomZone");
                BoxCollider bedroomZoneCol = null;
                if (bedroomZoneObj != null)
                {
                    bedroomZoneCol = bedroomZoneObj.GetComponent<BoxCollider>();
                    if (bedroomZoneCol != null)
                    {
                        bedroomZoneCol.isTrigger = true;
                        bedroomZoneCol.size = new Vector3(7.0f, 4.0f, 8.0f);
                        bedroomZoneCol.center = new Vector3(0f, 1f, 0f);
                        EditorUtility.SetDirty(bedroomZoneCol);
                        EditorUtility.SetDirty(bedroomZoneObj);
                    }
                }

                // 1d. Configure WallOcclusionManager room triggers
                var occlusionManager = Object.FindAnyObjectByType<WallOcclusionManager>();
                if (occlusionManager != null && bedroomZoneCol != null)
                {
                    var triggers = occlusionManager.RoomTriggers;
                    RoomOcclusionTrigger bedroomTrigger = triggers.Find(t => t != null && t.roomName == "Bedroom");
                    if (bedroomTrigger == null)
                    {
                        bedroomTrigger = new RoomOcclusionTrigger { roomName = "Bedroom" };
                        triggers.Add(bedroomTrigger);
                    }

                    bedroomTrigger.triggerCollider = bedroomZoneCol;
                    bedroomTrigger.foregroundGroups.Clear();
                    if (westGroup != null && !bedroomTrigger.foregroundGroups.Contains(westGroup))
                        bedroomTrigger.foregroundGroups.Add(westGroup);
                    if (southGroup != null && !bedroomTrigger.foregroundGroups.Contains(southGroup))
                        bedroomTrigger.foregroundGroups.Add(southGroup);

                    EditorUtility.SetDirty(occlusionManager);
                }
            }

            // 2. Kitchen
            var kitchenWall = GameObject.Find("_WORLD/Zones/Kitchen/Wall");
            if (kitchenWall != null)
            {
                groupsCreated += SetupZoneGroups(kitchenWall.transform, "Kitchen", new Dictionary<string, string[]>
                {
                    { "Kitchen_South", new string[] { "Wall_South", "Corner_SW" } },
                    { "Kitchen_West", new string[] { "Wall_West", "Window_West_2m", "Corner_NW" } },
                    { "Kitchen_North", new string[] { "Wall_North", "DoorFrame_North", "KitchenDoor" } },
                    { "Kitchen_East", new string[] { "Wall_East" } }
                }, ref occludersGrouped);

                // Link Kitchen foreground walls (South and West)
                var kSouthTrans = kitchenWall.transform.Find("Group_Kitchen_South");
                var kWestTrans = kitchenWall.transform.Find("Group_Kitchen_West");
                var kSouth = kSouthTrans != null ? kSouthTrans.GetComponent<WallOcclusionGroup>() : null;
                var kWest = kWestTrans != null ? kWestTrans.GetComponent<WallOcclusionGroup>() : null;

                if (kSouth != null && kWest != null)
                {
                    if (!kSouth.linkedGroups.Contains(kWest)) kSouth.linkedGroups.Add(kWest);
                    if (!kWest.linkedGroups.Contains(kSouth)) kWest.linkedGroups.Add(kSouth);
                    EditorUtility.SetDirty(kSouth);
                    EditorUtility.SetDirty(kWest);
                }
            }

            // 3. Warehouse
            var warehouseWall = GameObject.Find("_WORLD/Zones/Warehouse/Wall");
            if (warehouseWall != null)
            {
                groupsCreated += SetupZoneGroups(warehouseWall.transform, "Warehouse", new Dictionary<string, string[]>
                {
                    { "Warehouse_South", new string[] { "Wall_South", "Window_South_2m", "Corner_SE" } },
                    { "Warehouse_East", new string[] { "Wall_East", "Window_East_2m", "Corner_NE" } },
                    { "Warehouse_North", new string[] { "Wall_North", "DoorFrame_North", "WarehouseDoor" } }
                }, ref occludersGrouped);
            }

            // Mark dirty and save
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && !Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[WallOcclusionGroupingUtility] Sukses! Dibuat {groupsCreated} WallOcclusionGroup dan mengelompokkan {occludersGrouped} modular wall occluders.");
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
