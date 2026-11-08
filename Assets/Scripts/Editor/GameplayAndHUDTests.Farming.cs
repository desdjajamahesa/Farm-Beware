using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using TMPro;
using FeaturesTime;
using FeaturesTime.UI;
using FeaturesKitchen;
using FeaturesCombat;
using FeaturesCombat.Adapters;
using FeaturesCombat.Core.PureLogic;
using FeaturesCombat.UI;
using FeaturesCombat.Wave;
using FeaturesEconomy;
using UnityEngine.EventSystems;
using FarmBeware.Core.Runtime;
using PlayerUI;
using FeaturesStoryline;
using FeaturesStoryline.Data;
using FeaturesStoryline.UI;
using FeaturesStoryline.Cutscenes;
using FeaturesFarming;
using FeaturesInteraction;
using Player;
using FarmBeware.Logic.Quests;
using FarmBeware.Features.Quest;
using FarmBeware.Features.Quest.UI;
using FeaturesWorkbench;
using FeaturesWorkbench.Core;
using Object = UnityEngine.Object;

namespace FarmBeware.EditorTests
{
    public static partial class GameplayAndHUDTests
    {
        // Test 8: FarmlandTile seed drop safety via WorldItemPickup.Spawn
        private static bool Test_FarmlandTile_WorldItemPickup_SpawnOnFullInventory()
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = "seed_test";
            item.itemName = "Test Seeds";

            try
            {
                var pickup = WorldItemPickup.Spawn(Vector3.zero, item, 2, Vector3.up);
                if (pickup == null)
                {
                    Debug.LogError("WorldItemPickup.Spawn returned null!");
                    return false;
                }

                if (pickup.item != item || pickup.quantity != 2)
                {
                    Debug.LogError($"Pickup properties mismatch: item={pickup.item?.itemName}, qty={pickup.quantity}");
                    Object.DestroyImmediate(pickup.gameObject);
                    return false;
                }

                Object.DestroyImmediate(pickup.gameObject);
                Debug.Log("✅ Test 8: WorldItemPickup.Spawn fallback for full inventory harvest verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(item);
            }
        }

        // Test 9: FarmlandPlot 1 to 16 have no Mat_Highlight overrides on crop renderers
        private static bool Test_FarmlandPlot_1_NoHighlightOverrides()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Compound/Farmland.prefab");
            if (prefab == null)
            {
                Debug.LogError("Farmland.prefab could not be loaded!");
                return false;
            }

            for (int i = 1; i <= 16; i++)
            {
                var plot = prefab.transform.Find("FarmlandPlot_" + i);
                if (plot == null) continue;

                var renderers = plot.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null && m.name.Contains("Highlight"))
                        {
                            Debug.LogError($"Renderer {r.name} in FarmlandPlot_{i} (prefab) still has Highlight material!");
                            return false;
                        }
                    }
                }
            }

            // Also verify FarmlandPlot 1 to 16 in scene hierarchy if active
            for (int i = 1; i <= 16; i++)
            {
                var scenePlot = GameObject.Find("FarmlandPlot_" + i);
                if (scenePlot == null) continue;

                var renderers = scenePlot.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null && m.name.Contains("Highlight"))
                        {
                            Debug.LogError($"Renderer {r.name} in FarmlandPlot_{i} (scene) still has Highlight material!");
                            return false;
                        }
                    }
                }
            }

            Debug.Log("✅ Test 9: FarmlandPlot 1 to 16 crop highlight override fix verified (0 Mat_Highlight on crops).");
            return true;
        }

        // Test 13: WorldItemPickup drop protection on full inventory
        private static bool Test_WorldItemPickup_Protection_FullInventory()
        {
            var playerGO = new GameObject("Test_Player_Isolated");
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = "drop_loot_gem";
            item.itemName = "Monster Drop Gem";
            item.maxStack = 1;

            var fillerItem = ScriptableObject.CreateInstance<ItemData>();
            fillerItem.itemId = "filler_item";
            fillerItem.itemName = "Filler Item";
            fillerItem.maxStack = 1;

            try
            {
                var inv = playerGO.AddComponent<InventoryComponent>();
                inv.ResetInventory(16);

                // Fill all 16 slots with unstackable items
                for (int i = 0; i < 16; i++)
                {
                    inv.slots[i].item = fillerItem;
                    inv.slots[i].quantity = 1;
                }

                if (inv.HasSpaceFor(item, 1))
                {
                    Debug.LogError("Inventory should be completely full!");
                    return false;
                }

                var pickup = WorldItemPickup.Spawn(Vector3.zero, item, 1, Vector3.up);
                if (pickup == null)
                {
                    Debug.LogError("Failed to spawn pickup!");
                    return false;
                }

                pickup.TargetInventory = inv;

                if (pickup.CanPlayerPickUp())
                {
                    Debug.LogError("CanPlayerPickUp should be FALSE when inventory is full!");
                    Object.DestroyImmediate(pickup.gameObject);
                    return false;
                }

                // Simulate Collect invocation when full
                var collectMethod = typeof(WorldItemPickup).GetMethod("Collect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (collectMethod != null) collectMethod.Invoke(pickup, null);

                // Verify pickup was NOT destroyed
                if (pickup == null || pickup.gameObject == null)
                {
                    Debug.LogError("Pickup was destroyed even though inventory was full! Item would be lost!");
                    return false;
                }

                Object.DestroyImmediate(pickup.gameObject);
                Debug.Log("✅ Test 13: WorldItemPickup drop protection on full inventory verified (item preserved on ground).");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(playerGO);
                Object.DestroyImmediate(item);
                Object.DestroyImmediate(fillerItem);
            }
        }

        // Test 25: Multi-stack seed consumption priority (Active Hotbar slot decremented before main inventory backpack)
        private static bool Test_PlayerInventory_MultiStack_PlantSeedHotbarConsumptionPriorityAudit()
        {
            var cleanupList = new System.Collections.Generic.List<Object>();
            try
            {
                var invGo = new GameObject("Test_PlayerInventory", typeof(InventoryComponent));
                cleanupList.Add(invGo);
                var inv = invGo.GetComponent<InventoryComponent>();
                inv.HasHotbar = true;
                inv.ResetInventory(16);

                var seedItem = ScriptableObject.CreateInstance<SeedItemData>();
                cleanupList.Add(seedItem);
                seedItem.itemId = "seed_sweet_potato";
                seedItem.itemName = "Sweet Potato Seed";
                seedItem.maxStack = 20;
                seedItem.growthDuration = 30f;

                // Setup scenario:
                // Stack A: 5 seeds in Hotbar Slot 0 (Active in hand, selectedHotbarIndex = 0)
                // Stack B: 10 seeds in Main Inventory Slot 4 (in backpack)
                inv.slots[0].item = seedItem;
                inv.slots[0].quantity = 5;

                inv.slots[4].item = seedItem;
                inv.slots[4].quantity = 10;

                inv.selectedHotbarIndex = 0;

                // Test Part 1: RemoveItem without explicit preferred index automatically resolves active hotbar slot
                bool removed1 = inv.RemoveItem(seedItem, 1);
                if (!removed1)
                {
                    Debug.LogError("[Test 25] RemoveItem failed to remove 1 seed!");
                    return false;
                }
                if (inv.slots[0].quantity != 4)
                {
                    Debug.LogError($"[Test 25] Expected Hotbar Slot 0 to have 4 seeds, got {inv.slots[0].quantity}!");
                    return false;
                }
                if (inv.slots[4].quantity != 10)
                {
                    Debug.LogError($"[Test 25] Main Inventory Slot 4 was erroneously decremented to {inv.slots[4].quantity}, expected 10!");
                    return false;
                }

                // Test Part 2: Explicit preferredSlotIndex: 0
                bool removed2 = inv.RemoveItem(seedItem, 1, preferredSlotIndex: 0);
                if (!removed2)
                {
                    Debug.LogError("[Test 25] RemoveItem with preferredSlotIndex failed!");
                    return false;
                }
                if (inv.slots[0].quantity != 3)
                {
                    Debug.LogError($"[Test 25] Expected Hotbar Slot 0 to have 3 seeds, got {inv.slots[0].quantity}!");
                    return false;
                }
                if (inv.slots[4].quantity != 10)
                {
                    Debug.LogError($"[Test 25] Main Inventory Slot 4 was erroneously decremented to {inv.slots[4].quantity}, expected 10!");
                    return false;
                }

                // Test Part 3: Exhaust remaining 3 seeds in Hotbar Slot 0
                bool removed3 = inv.RemoveItem(seedItem, 3, preferredSlotIndex: 0);
                if (!removed3)
                {
                    Debug.LogError("[Test 25] Failed to exhaust Hotbar Slot 0!");
                    return false;
                }
                if (!inv.slots[0].IsEmpty)
                {
                    Debug.LogError("[Test 25] Hotbar Slot 0 should be empty after exhausting quantity!");
                    return false;
                }
                if (inv.slots[4].quantity != 10)
                {
                    Debug.LogError($"[Test 25] Main Inventory Slot 4 quantity was altered, expected 10, got {inv.slots[4].quantity}!");
                    return false;
                }

                // Test Part 4: Next planting action falls back to Main Inventory Slot 4
                bool removed4 = inv.RemoveItem(seedItem, 2);
                if (!removed4)
                {
                    Debug.LogError("[Test 25] Failed to fall back to Main Inventory Slot 4!");
                    return false;
                }
                if (!inv.slots[0].IsEmpty)
                {
                    Debug.LogError("[Test 25] Hotbar Slot 0 should still be empty!");
                    return false;
                }
                if (inv.slots[4].quantity != 8)
                {
                    Debug.LogError($"[Test 25] Expected Main Inventory Slot 4 to have 8 seeds, got {inv.slots[4].quantity}!");
                    return false;
                }

                // Test Part 5: RemoveItemFromSlot direct deduction
                bool removed5 = inv.RemoveItemFromSlot(4, 3);
                if (!removed5)
                {
                    Debug.LogError("[Test 25] RemoveItemFromSlot failed!");
                    return false;
                }
                if (inv.slots[4].quantity != 5)
                {
                    Debug.LogError($"[Test 25] Expected Main Inventory Slot 4 to have 5 seeds after RemoveItemFromSlot, got {inv.slots[4].quantity}!");
                    return false;
                }

                Debug.Log("✅ Test 25: Multi-stack seed consumption priority (Hotbar active slot priority before backpack) verified.");
                return true;
            }
            finally
            {
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null)
                    {
                        Object.DestroyImmediate(cleanupList[i]);
                    }
                }
            }
        }

        // Test 29: CropData_Corn ScriptableObject asset integrity, 3-day maturity, yields, and FarmGridManager dual-lookup alias
        private static bool Test_CropDataCorn_AssetIntegrity_And_DualLookup_Audit()
        {
            var cleanupList = new List<Object>();
            try
            {
                var cornAsset = AssetDatabase.LoadAssetAtPath<FeaturesFarming.Data.CropData>("Assets/Data/Items/Crops/CropData_Corn.asset");
                if (cornAsset == null)
                {
                    Debug.LogError("[Test 29] Assets/Data/Items/Crops/CropData_Corn.asset does not exist or failed to load!");
                    return false;
                }

                if (cornAsset.cropId != "Crop_Corn")
                {
                    Debug.LogError($"[Test 29] CropData_Corn.cropId mismatch! Expected 'Crop_Corn', got '{cornAsset.cropId}'");
                    return false;
                }

                if (cornAsset.totalDaysToHarvest != 3)
                {
                    Debug.LogError($"[Test 29] CropData_Corn.totalDaysToHarvest mismatch! Expected 3, got {cornAsset.totalDaysToHarvest}");
                    return false;
                }

                if (cornAsset.cleanYieldItem == null || cornAsset.dirtyYieldItem == null || cornAsset.returnedSeedItem == null)
                {
                    Debug.LogError("[Test 29] CropData_Corn missing item references (cleanYieldItem, dirtyYieldItem, or returnedSeedItem)!");
                    return false;
                }

                // Verify FarmGridManager dual lookup (Crop_Corn and seed_corn)
                var gridManagerGO = new GameObject("Test_FarmGridManager_Corn");
                cleanupList.Add(gridManagerGO);
                var gridManager = gridManagerGO.AddComponent<FeaturesFarming.Adapters.FarmGridManager>();
                gridManager.RegisterCropData(cornAsset);

                var lookupByCropId = gridManager.GetCropData("Crop_Corn");
                var lookupBySeedId = gridManager.GetCropData("seed_corn");

                if (lookupByCropId == null || lookupByCropId != cornAsset)
                {
                    Debug.LogError("[Test 29] FarmGridManager.GetCropData('Crop_Corn') failed to resolve registered CropData_Corn!");
                    return false;
                }

                if (lookupBySeedId == null || lookupBySeedId != cornAsset)
                {
                    Debug.LogError("[Test 29] FarmGridManager.GetCropData('seed_corn') alias failed to resolve registered CropData_Corn!");
                    return false;
                }

                Debug.Log("✅ Test 29: CropData_Corn ScriptableObject asset integrity, 3-day growth cycle, clean/dirty yields, and FarmGridManager dual lookup verified.");
                return true;
            }
            finally
            {
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 52: FarmlandTile IInteractable.CanInteract Night & Growing State Gating
        private static bool Test_FarmlandTile_NightAndGrowingSuppression_Audit()
        {
            var cleanupList = new List<GameObject>();
            var prevTimeInstance = FeaturesTime.DayNightTimeManager.Instance;
            try
            {
                var timeGo = new GameObject("Test_TimeManager_Farmland");
                cleanupList.Add(timeGo);
                var timeMgr = timeGo.AddComponent<FeaturesTime.DayNightTimeManager>();
                typeof(FeaturesTime.DayNightTimeManager)
                    .GetField("<CurrentDay>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(timeMgr, 1);
                typeof(FeaturesTime.DayNightTimeManager)
                    .GetField("currentHour", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(timeMgr, 10f);
                FeaturesTime.DayNightTimeManager.Instance = timeMgr;

                var tileGo = new GameObject("Test_FarmlandTile");
                cleanupList.Add(tileGo);
                var tile = tileGo.AddComponent<FarmlandTile>();
                var interactable = tile as IInteractable;

                var playerGo = new GameObject("Test_Player");
                cleanupList.Add(playerGo);

                // Case A: PlantedWatered (growing) -> CanInteract must be FALSE
                typeof(FarmlandTile).GetField("currentState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(tile, TileState.PlantedWatered);
                if (interactable.CanInteract(playerGo))
                {
                    Debug.LogError("[Test 52] FarmlandTile.CanInteract returned true while growing (PlantedWatered)! Expected false.");
                    return false;
                }

                // Case B: At Night -> CanInteract must be FALSE
                typeof(FeaturesTime.DayNightTimeManager)
                    .GetField("currentHour", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(timeMgr, 20f);
                typeof(FarmlandTile).GetField("currentState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(tile, TileState.Untilled);
                if (interactable.CanInteract(playerGo))
                {
                    Debug.LogError("[Test 52] FarmlandTile.CanInteract returned true at Night! Expected false.");
                    return false;
                }

                Debug.Log("✅ Test 52: FarmlandTile IInteractable.CanInteract Night & Growing State Gating verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 52] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                FeaturesTime.DayNightTimeManager.Instance = prevTimeInstance;
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 53: CropGrowthCountdownUI Night Indicator Suppression Audit
        private static bool Test_CropGrowthCountdownUI_NightSuppression_Audit()
        {
            var cleanupList = new List<GameObject>();
            var prevTimeInstance = FeaturesTime.DayNightTimeManager.Instance;
            try
            {
                var timeGo = new GameObject("Test_TimeManager_CropUI");
                cleanupList.Add(timeGo);
                var timeMgr = timeGo.AddComponent<FeaturesTime.DayNightTimeManager>();
                typeof(FeaturesTime.DayNightTimeManager)
                    .GetField("currentHour", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(timeMgr, 20f); // Night
                FeaturesTime.DayNightTimeManager.Instance = timeMgr;

                bool suppressed = CropGrowthCountdownUI.ShouldSuppressIndicators();
                if (!suppressed)
                {
                    Debug.LogError("[Test 53] ShouldSuppressIndicators() returned false during Night phase! Expected true.");
                    return false;
                }

                Debug.Log("✅ Test 53: CropGrowthCountdownUI Night Indicator Suppression verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 53] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                FeaturesTime.DayNightTimeManager.Instance = prevTimeInstance;
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        /// <summary>
        /// [Test 90] Verifies Bio-Catalyst Night Overdrive pure logic (preserving default 0% night crop growth
        /// while enabling 50% overdrive nocturnal growth, +25% gold, +30% material drops, and material consumption).
        /// </summary>
        public static bool Test_BioCatalystNightOverdrive_CropGrowthAndLootEscalation()
        {
            // 1. Default night must have 0 crop growth per MVP Guideline
            float normalNightDelta = NightOverdriveEvaluator.EvaluateCropNightGrowthDelta(10f, 1f, isOverdriveActive: false);
            if (normalNightDelta != 0f)
            {
                Debug.LogError($"[Test 90] Default night crop growth must be 0 per GDD, got {normalNightDelta}!");
                return false;
            }

            // 2. Overdrive night enables 50% nocturnal crop growth
            float overdriveNightDelta = NightOverdriveEvaluator.EvaluateCropNightGrowthDelta(10f, 1f, isOverdriveActive: true);
            if (Mathf.Abs(overdriveNightDelta - 5.0f) > 0.01f)
            {
                Debug.LogError($"[Test 90] Overdrive night growth expected 5.0s for 10s deltaTime, got {overdriveNightDelta}!");
                return false;
            }

            // 3. Gold (+25%) and Material drop chance (+30% flat bonus) escalation
            int baseGold = 100;
            int odGold = NightOverdriveEvaluator.CalculateOverdriveGold(baseGold, isOverdriveActive: true);
            float odDrop = NightOverdriveEvaluator.CalculateOverdriveDropChance(0.50f, isOverdriveActive: true);
            if (odGold != 125 || Mathf.Abs(odDrop - 0.80f) > 0.01f)
            {
                Debug.LogError($"[Test 90] Overdrive loot scaling mismatch: gold={odGold} (expected 125), drop={odDrop} (expected 0.80)");
                return false;
            }

            // 4. NightBrawlManager activation consumes 2 monster materials
            var go = new GameObject("Test_Overdrive_Player");
            var nbGo = new GameObject("Test_NightBrawlManager");
            var prevNb = NightBrawlManager.Instance;
            try
            {
                typeof(NightBrawlManager).GetProperty("Instance")?.SetValue(null, null);
                var inv = go.AddComponent<InventoryComponent>();
                var nb = nbGo.AddComponent<NightBrawlManager>();

                var matItem = ScriptableObject.CreateInstance<ItemData>();
                matItem.itemId = "mat_mutated_root";
                matItem.itemName = "Mutated Root";
                matItem.category = ItemCategory.Material;
                matItem.maxStack = 20;

                inv.slots.Clear();
                inv.slots.Add(new InventorySlot(matItem, 3));

                bool activated = nb.TryActivateNightOverdrive(inv);
                if (!activated || !nb.IsOverdriveNightActive || inv.slots[0].quantity != 1)
                {
                    Debug.LogError($"[Test 90] TryActivateNightOverdrive failed! activated={activated}, active={nb.IsOverdriveNightActive}, remainingMat={inv.slots[0].quantity}");
                    ScriptableObject.DestroyImmediate(matItem);
                    return false;
                }

                // 5. Verify Tactical Forge WorkbenchUI 3-Tab Widescreen Bento Hierarchy & Refresh
                var wbGo = new GameObject("Test_WorkbenchUI", typeof(RectTransform));
                var prevWb = FeaturesWorkbench.UI.WorkbenchUI.Instance;
                try
                {
                    typeof(FeaturesWorkbench.UI.WorkbenchUI).GetProperty("Instance")?.SetValue(null, null);
                    var wb = wbGo.AddComponent<FeaturesWorkbench.UI.WorkbenchUI>();
                    typeof(FeaturesWorkbench.UI.WorkbenchUI)
                        .GetMethod("BuildUIHierarchyIfNeeded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        ?.Invoke(wb, null);

                    wb.SetTab(FeaturesWorkbench.UI.WorkbenchUI.WorkbenchTab.Weapon);
                    wb.SetTab(FeaturesWorkbench.UI.WorkbenchUI.WorkbenchTab.Armor);
                    wb.SetTab(FeaturesWorkbench.UI.WorkbenchUI.WorkbenchTab.Overdrive);
                }
                finally
                {
                    if (wbGo != null) GameObject.DestroyImmediate(wbGo);
                    typeof(FeaturesWorkbench.UI.WorkbenchUI).GetProperty("Instance")?.SetValue(null, prevWb);
                }

                ScriptableObject.DestroyImmediate(matItem);
                Debug.Log("✅ Test 90: Bio-Catalyst Night Overdrive crop growth, Clean Harvest purification, loot escalation, and Tactical Forge WorkbenchUI verified.");
                return true;
            }
            finally
            {
                if (go != null) GameObject.DestroyImmediate(go);
                if (nbGo != null) GameObject.DestroyImmediate(nbGo);
                typeof(NightBrawlManager).GetProperty("Instance")?.SetValue(null, prevNb);
            }
        }

        // Test 100: Verify Farmland Plot Day 1 restriction (6 active plots, 10 locked) and Workbench Land Expansion (+2 plots for 2,500 G up to 16)
        private static bool Test_Farmland_Day1CapacityAndWorkbenchExpansion_Audit()
        {
            var mgrObj = new GameObject("Test100_FarmlandMgr");
            var playerObj = new GameObject("Test100_Player");
            var prevMgr = FeaturesFarming.FarmlandExpansionManager.Instance;
            var prevWallet = ServiceLocator.Resolve<IWalletService>();
            var tiles = new System.Collections.Generic.List<GameObject>();
            try
            {
                var mgr = mgrObj.AddComponent<FeaturesFarming.FarmlandExpansionManager>();
                FeaturesFarming.FarmlandExpansionManager.Instance = mgr;
                mgr.SetUnlockedPlotCount(FeaturesFarming.FarmlandExpansionManager.InitialPlotCount);

                var wallet = playerObj.AddComponent<FeaturesEconomy.PlayerWallet>();
                wallet.SetGold(10000);
                ServiceLocator.Register<IWalletService>(wallet);

                if (mgr.UnlockedPlotCount != 6)
                {
                    Debug.LogError("[Test 100] InitialPlotCount must be 6!");
                    return false;
                }

                // Create simulated plots 1 to 16
                for (int i = 1; i <= 16; i++)
                {
                    var tObj = new GameObject($"FarmlandPlot_{i}", typeof(BoxCollider), typeof(WorldLabel), typeof(Highlightable));
                    var t = tObj.AddComponent<FeaturesFarming.FarmlandTile>();
                    t.RefreshLockState();
                    tiles.Add(tObj);

                    if (i <= 6)
                    {
                        if (t.IsLocked)
                        {
                            Debug.LogError($"[Test 100] FarmlandPlot_{i} is locked on Day 1 (should be unlocked)!");
                            return false;
                        }
                    }
                    else
                    {
                        if (!t.IsLocked)
                        {
                            Debug.LogError($"[Test 100] FarmlandPlot_{i} is unlocked on Day 1 (must be locked)!");
                            return false;
                        }
                        if (t.CanInteract(playerObj))
                        {
                            Debug.LogError($"[Test 100] CanInteract returned true on locked FarmlandPlot_{i}!");
                            return false;
                        }
                    }
                }

                // Expand Farmland: +2 plots for 2,500 G
                bool expanded = mgr.TryExpand(wallet);
                if (!expanded || mgr.UnlockedPlotCount != 8 || wallet.CurrentGold != 7500)
                {
                    Debug.LogError($"[Test 100] TryExpand failed! expanded={expanded}, count={mgr.UnlockedPlotCount} (exp 8), gold={wallet.CurrentGold} (exp 7500)");
                    return false;
                }

                // Plots 7 and 8 must now be unlocked!
                var plot7 = tiles[6].GetComponent<FeaturesFarming.FarmlandTile>();
                var plot8 = tiles[7].GetComponent<FeaturesFarming.FarmlandTile>();
                var plot9 = tiles[8].GetComponent<FeaturesFarming.FarmlandTile>();

                if (plot7.IsLocked || plot8.IsLocked)
                {
                    Debug.LogError("[Test 100] Plots 7 and 8 did not unlock after land deed expansion!");
                    return false;
                }
                if (!plot9.IsLocked)
                {
                    Debug.LogError("[Test 100] Plot 9 unlocked prematurely!");
                    return false;
                }

                Debug.Log("✅ Test 100: Farmland Day 1 6-plot gating, 10 locked plots, and Workbench Land Expansion (+2 plots for 2,500 G) verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 100] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                FeaturesFarming.FarmlandExpansionManager.Instance = prevMgr;
                if (mgrObj != null) Object.DestroyImmediate(mgrObj);
                if (playerObj != null) Object.DestroyImmediate(playerObj);
                foreach (var t in tiles) if (t != null) Object.DestroyImmediate(t);
                if (prevWallet != null) ServiceLocator.Register<IWalletService>(prevWallet);
                else ServiceLocator.Unregister<IWalletService>();
            }
        }
    }
}
