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
            if (bedroomWall != null)
            {
                groupsCreated += SetupZoneGroups(bedroomWall.transform, "Bedroom", new Dictionary<string, string[]>
                {
                    { "Bedroom_South", new string[] { "Wall_South", "Corner_SW", "Corner_SE" } },
                    { "Bedroom_West", new string[] { "Wall_West", "DoorFrame_West_2m", "BedroomDoor" } },
                    { "Bedroom_North", new string[] { "Wall_North", "Corner_NW", "Corner_NE" } },
                    { "Bedroom_East", new string[] { "Wall_East", "Window_East_2m" } }
                }, ref occludersGrouped);
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
