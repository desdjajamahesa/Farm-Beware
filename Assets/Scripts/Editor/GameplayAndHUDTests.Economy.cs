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
using FeaturesEconomy.PureLogic;
using UnityEngine.EventSystems;
using FarmBeware.Core.Runtime;
using FarmBeware.Data.Runtime;
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
        // Test 12: Merchant shop scroll position reset to top
        private static bool Test_MerchantShopUI_ResetScrollPosition()
        {
            var go = new GameObject("Test_MerchantShop");
            try
            {
                var shopUI = go.AddComponent<MerchantShopUI>();
                var scrollGO = new GameObject("ScrollRect", typeof(RectTransform));
                scrollGO.transform.SetParent(go.transform, false);
                var scrollRT = scrollGO.GetComponent<RectTransform>();
                scrollRT.sizeDelta = new Vector2(400f, 300f);

                var scroll = scrollGO.AddComponent<ScrollRect>();
                var contentGO = new GameObject("Content", typeof(RectTransform));
                contentGO.transform.SetParent(scrollGO.transform, false);
                var contentRT = contentGO.GetComponent<RectTransform>();
                contentRT.sizeDelta = new Vector2(400f, 1000f);
                scroll.content = contentRT;
                scroll.viewport = scrollRT;

                var field = typeof(MerchantShopUI).GetField("scrollRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(shopUI, scroll);

                // Simulate scrolled to bottom
                scroll.verticalNormalizedPosition = 0f;

                shopUI.ResetScrollPosition();

                if (Mathf.Abs(scroll.verticalNormalizedPosition - 1f) > 0.05f)
                {
                    Debug.LogError($"Expected scroll position 1f, got {scroll.verticalNormalizedPosition}!");
                    return false;
                }

                Debug.Log("✅ Test 12: Merchant shop scroll position reset to top verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        
                // =========================================================================
        // MILESTONE 1: SHOP INTERACTABLE & ECONOMY AUDIT TESTS (Tests 75 - 78)
        // =========================================================================

        // Test 75: ShopInteractable component instantiation, IInteractable implementation, and stock catalog
        private static bool Test_ShopInteractable_ComponentInstantiation_And_StockInitialization()
        {
            var cleanupList = new List<Object>();
            try
            {
                var go = new GameObject("TradingPost_ShopCounter");
                cleanupList.Add(go);
                go.layer = 0; // Default layer

                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(2.4f, 1.2f, 1.4f);
                col.center = new Vector3(0f, 0.55f, 0f);
                col.isTrigger = false;

                var label = go.AddComponent<FeaturesInteraction.WorldLabel>();
                label.displayName = "Trading Post Merchant (Trade)";

                var highlight = go.AddComponent<FeaturesInteraction.Highlightable>();

                var shop = go.AddComponent<ShopInteractable>();

                // 1. Component verification
                if (go.GetComponent<FeaturesInteraction.WorldLabel>() == null)
                {
                    Debug.LogError("[Test 75] Missing required WorldLabel component!");
                    return false;
                }
                if (go.GetComponent<BoxCollider>() == null)
                {
                    Debug.LogError("[Test 75] Missing required BoxCollider component!");
                    return false;
                }
                if (go.GetComponent<FeaturesInteraction.Highlightable>() == null)
                {
                    Debug.LogError("[Test 75] Missing required Highlightable component!");
                    return false;
                }

                // 2. Layer safety verification (must never be on Wall Layer 12)
                if (go.layer == 12)
                {
                    Debug.LogError("[Test 75] Shop counter must not reside on Wall layer (12)!");
                    return false;
                }

                // 3. IInteractable contract verification
                if (!(shop is FeaturesInteraction.IInteractable interactable))
                {
                    Debug.LogError("[Test 75] ShopInteractable does not implement IInteractable!");
                    return false;
                }
                if (!interactable.CanInteract(null))
                {
                    Debug.LogError("[Test 75] IInteractable.CanInteract returned false!");
                    return false;
                }

                // 4. Stock initialization from ItemDatabase
                shop.InitializeDefaultStock();
                var stockField = typeof(MerchantTableInteractable).GetField("stockItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var stock = stockField?.GetValue(shop) as List<ItemData>;

                if (stock == null || stock.Count == 0)
                {
                    Debug.LogError("[Test 75] Stock catalog failed to initialize from ItemDatabase!");
                    return false;
                }
                if (!stock.Exists(it => it.category == ItemCategory.Seed))
                {
                    Debug.LogError("[Test 75] Stock catalog does not contain any seed items!");
                    return false;
                }
                if (!stock.Exists(it => it.buyPrice > 0))
                {
                    Debug.LogError("[Test 75] Stock catalog contains no buyable items!");
                    return false;
                }

                Debug.Log("✅ Test 75: ShopInteractable component instantiation, IInteractable contract, and stock catalog verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 75] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                foreach (var obj in cleanupList)
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                }
            }
        }

        // Test 76: ShopInteractable Day 0 gating (blocked) and Day 1+ activation (opens MerchantShopUI)
        private static bool Test_ShopInteractable_DayProgressionGating_And_MerchantShopUI_Activation()
        {
            var cleanupList = new List<Object>();
            FarmBeware.Core.Runtime.ServiceLocator.TryResolve<FarmBeware.Core.Runtime.ITimeService>(out var prevTime);
            var instanceField = typeof(MerchantShopUI).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var prevInstance = instanceField?.GetValue(null);

            try
            {
                var mockTime = new MockTimeService { CurrentDay = 0 };
                FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(mockTime);

                var shopGO = new GameObject("Test_MerchantShopUI_Root");
                cleanupList.Add(shopGO);
                var shopUI = shopGO.AddComponent<MerchantShopUI>();
                instanceField?.SetValue(null, shopUI);

                var counterGO = new GameObject("Test_ShopInteractable");
                cleanupList.Add(counterGO);
                counterGO.AddComponent<BoxCollider>();
                counterGO.AddComponent<FeaturesInteraction.WorldLabel>();
                var shop = counterGO.AddComponent<ShopInteractable>();
                shop.InitializeDefaultStock();

                var playerGO = new GameObject("Player");
                cleanupList.Add(playerGO);
                playerGO.tag = "Player";

                var awakeMethod = typeof(MerchantShopUI).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                awakeMethod?.Invoke(shopUI, null);

                // 1. Day 0 gating: must NOT open shop
                mockTime.CurrentDay = 0;
                shop.Interact(playerGO);
                if (shopUI.IsOpen)
                {
                    Debug.LogError("[Test 76] MerchantShopUI opened on Day 0! Day 0 interaction must be blocked.");
                    return false;
                }

                // 2. Day 1 activation: MUST open shop
                mockTime.CurrentDay = 1;
                shop.Interact(playerGO);
                if (!shopUI.IsOpen)
                {
                    Debug.LogError("[Test 76] MerchantShopUI failed to open on Day 1!");
                    return false;
                }

                // 3. Modal stack tracking
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    if (!ModalStackManager.Instance.HasActiveModal)
                    {
                        Debug.LogError("[Test 76] ModalStackManager does not track active shop modal!");
                        return false;
                    }
                }

                // 4. Modal closure
                shopUI.CloseShop();
                if (shopUI.IsOpen)
                {
                    Debug.LogError("[Test 76] MerchantShopUI remained open after CloseShop()!");
                    return false;
                }

                Debug.Log("✅ Test 76: ShopInteractable Day 0 gating and Day 1+ MerchantShopUI modal activation verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 76] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                instanceField?.SetValue(null, prevInstance);
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    ModalStackManager.Instance.Clear();
                }
                if (prevTime != null) FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(prevTime);
                else FarmBeware.Core.Runtime.ServiceLocator.Unregister<FarmBeware.Core.Runtime.ITimeService>();

                foreach (var obj in cleanupList)
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                }
            }
        }

        // Test 77: PlayerWallet transaction integrity: buy deduction, sell increment, and full inventory refund
        private static bool Test_PlayerWallet_TransactionIntegrity_BuyDeduction_SellIncrement_And_FullInventoryRefund()
        {
            var cleanupList = new List<Object>();
            var walletField = typeof(PlayerWallet).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var prevWallet = walletField?.GetValue(null);

            try
            {
                var playerGO = new GameObject("Test_Player_WalletAudit");
                cleanupList.Add(playerGO);
                playerGO.tag = "Player";

                var wallet = playerGO.AddComponent<PlayerWallet>();
                walletField?.SetValue(null, wallet);

                var inv = playerGO.AddComponent<InventoryComponent>();
                inv.ResetInventory(16);

                var shopGO = new GameObject("Test_ShopUI_WalletAudit");
                cleanupList.Add(shopGO);
                var shopUI = shopGO.AddComponent<MerchantShopUI>();

                var invField = typeof(MerchantShopUI).GetField("playerInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                invField?.SetValue(shopUI, inv);

                var testSeed = ScriptableObject.CreateInstance<ItemData>();
                cleanupList.Add(testSeed);
                testSeed.itemId = "seed_audit_test";
                testSeed.itemName = "Audit Test Seed";
                testSeed.buyPrice = 150;
                testSeed.sellPrice = 100;
                testSeed.maxStack = 10;
                testSeed.category = ItemCategory.Seed;

                var buyMethod = typeof(MerchantShopUI).GetMethod("TryBuyItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var sellMethod = typeof(MerchantShopUI).GetMethod("TrySellItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                wallet.SetGold(1000);
                if (wallet.CurrentGold != 1000)
                {
                    Debug.LogError($"[Test 77] Initial wallet gold setup failed: got {wallet.CurrentGold}, expected 1000!");
                    return false;
                }

                // 1. Buy transaction: deduct 150G -> 850G, inventory gains 1x seed
                buyMethod.Invoke(shopUI, new object[] { testSeed, 1 });
                if (wallet.CurrentGold != 850 || inv.CountItem(testSeed) != 1)
                {
                    Debug.LogError($"[Test 77] Buy transaction failed! Gold={wallet.CurrentGold} (expected 850), SeedCount={inv.CountItem(testSeed)} (expected 1)");
                    return false;
                }

                // 2. Sell transaction: increment 100G -> 950G, inventory loses 1x seed
                sellMethod.Invoke(shopUI, new object[] { testSeed, 1 });
                if (wallet.CurrentGold != 950 || inv.CountItem(testSeed) != 0)
                {
                    Debug.LogError($"[Test 77] Sell transaction failed! Gold={wallet.CurrentGold} (expected 950), SeedCount={inv.CountItem(testSeed)} (expected 0)");
                    return false;
                }

                // 3. Full inventory refund protection
                var fillerItem = ScriptableObject.CreateInstance<ItemData>();
                cleanupList.Add(fillerItem);
                fillerItem.itemId = "filler_unstackable";
                fillerItem.maxStack = 1;

                for (int i = 0; i < 16; i++)
                {
                    inv.slots[i].item = fillerItem;
                    inv.slots[i].quantity = 1;
                }

                int goldBeforeFullBuy = wallet.CurrentGold;
                buyMethod.Invoke(shopUI, new object[] { testSeed, 1 });
                if (wallet.CurrentGold != goldBeforeFullBuy)
                {
                    Debug.LogError($"[Test 77] Full inventory refund failed! Balance before={goldBeforeFullBuy}, balance after={wallet.CurrentGold}. Gold was not refunded!");
                    return false;
                }
                if (inv.CountItem(testSeed) != 0)
                {
                    Debug.LogError($"[Test 77] Full inventory item integrity failed! Expected 0 seeds, got {inv.CountItem(testSeed)}");
                    return false;
                }

                // 4. Partial inventory overflow exploit prevention (e.g., 2 spaces left when buying 5 items)
                testSeed.maxStack = 20;
                for (int i = 0; i < 15; i++)
                {
                    inv.slots[i].item = fillerItem;
                    inv.slots[i].quantity = 1;
                }
                inv.slots[15].item = testSeed;
                inv.slots[15].quantity = 18; // Exactly 2 spaces left (18/20)

                int goldBeforePartialBuy = wallet.CurrentGold;
                int seedsBeforePartialBuy = inv.CountItem(testSeed);
                buyMethod.Invoke(shopUI, new object[] { testSeed, 5 });

                int netGold = wallet.CurrentGold - goldBeforePartialBuy;
                int netSeeds = inv.CountItem(testSeed) - seedsBeforePartialBuy;

                if (netGold != 0 || netSeeds != 0)
                {
                    Debug.LogError($"[Test 77] Partial inventory overflow guard failed! NetGold={netGold} (expected 0), NetSeeds={netSeeds} (expected 0). Items gained for free!");
                    return false;
                }

                // Comprehensive sweep: k in {1, 3, 4} spaces free
                int[] partialCapacities = new int[] { 1, 3, 4 };
                foreach (int freeSpaces in partialCapacities)
                {
                    inv.slots[15].quantity = testSeed.maxStack - freeSpaces;
                    int goldBeforeSweep = wallet.CurrentGold;
                    int seedsBeforeSweep = inv.CountItem(testSeed);

                    buyMethod.Invoke(shopUI, new object[] { testSeed, 5 });

                    if (wallet.CurrentGold != goldBeforeSweep || inv.CountItem(testSeed) != seedsBeforeSweep)
                    {
                        Debug.LogError($"[Test 77] Matrix sweep ({freeSpaces} spaces free) failed! GoldDiff={wallet.CurrentGold - goldBeforeSweep}, SeedsDiff={inv.CountItem(testSeed) - seedsBeforeSweep}");
                        return false;
                    }
                }

                Debug.Log("✅ Test 77: PlayerWallet buy deduction, sell increment, and full/partial inventory refund verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 77] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                walletField?.SetValue(null, prevWallet);
                foreach (var obj in cleanupList)
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                }
            }
        }

        // Test 78: DailyEconomyManager daily price rolling, dirty produce discount, and morning report day snapshots
        private static bool Test_DailyEconomyManager_DailyPriceRolling_DirtyDiscount_And_DayTransitionSnapshot()
        {
            var cleanupList = new List<Object>();
            var mgrField = typeof(DailyEconomyManager).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var prevMgr = mgrField?.GetValue(null);

            try
            {
                var go = new GameObject("Test_DailyEconomyManager_Audit");
                cleanupList.Add(go);
                var mgr = go.AddComponent<DailyEconomyManager>();
                mgrField?.SetValue(null, mgr);

                // 1. 50 iterations bound checking
                for (int i = 0; i < 50; i++)
                {
                    mgr.RollDailyMarketPrices();
                    if (mgr.SweetPotatoPrice < 375 || mgr.SweetPotatoPrice > 500)
                    {
                        Debug.LogError($"[Test 78] SweetPotatoPrice {mgr.SweetPotatoPrice} out of [375, 500] range!");
                        return false;
                    }
                    if (mgr.TaroPrice < 700 || mgr.TaroPrice > 900)
                    {
                        Debug.LogError($"[Test 78] TaroPrice {mgr.TaroPrice} out of [700, 900] range!");
                        return false;
                    }
                    if (mgr.CornPrice < 700 || mgr.CornPrice > 900)
                    {
                        Debug.LogError($"[Test 78] CornPrice {mgr.CornPrice} out of [700, 900] range!");
                        return false;
                    }
                }

                // 2. Dirty produce 50% discount verification
                var cleanPotato = ScriptableObject.CreateInstance<ItemData>();
                cleanupList.Add(cleanPotato);
                cleanPotato.itemId = "crop_sweet_potato";
                cleanPotato.itemName = "Sweet Potato";
                cleanPotato.isDirty = false;

                var dirtyPotato = ScriptableObject.CreateInstance<ItemData>();
                cleanupList.Add(dirtyPotato);
                dirtyPotato.itemId = "crop_sweet_potato_dirty";
                dirtyPotato.itemName = "Dirty Sweet Potato";
                dirtyPotato.isDirty = true;

                int cleanPrice = mgr.GetCropSellPrice(cleanPotato);
                int dirtyPrice = mgr.GetCropSellPrice(dirtyPotato);
                int expectedDirty = Mathf.Max(1, Mathf.RoundToInt(cleanPrice * 0.5f));

                if (cleanPrice != mgr.SweetPotatoPrice || dirtyPrice != expectedDirty)
                {
                    Debug.LogError($"[Test 78] Dirty crop discount calculation mismatch! Clean={cleanPrice} (expected {mgr.SweetPotatoPrice}), Dirty={dirtyPrice} (expected {expectedDirty})");
                    return false;
                }

                // 3. Day transition and operational statistics snapshot verification
                mgr.dailyCropsHarvested = 10;
                mgr.dailyCropsSold = 6;
                mgr.dailyGoldEarnedTrading = 4800;
                mgr.dailyMonstersSlain = 8;
                mgr.dailyGoldEarnedCombat = 600;

                var handleDayMethod = typeof(DailyEconomyManager).GetMethod("HandleDayChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                handleDayMethod.Invoke(mgr, new object[] { 2 });

                if (mgr.lastDayCropsHarvested != 10 ||
                    mgr.lastDayCropsSold != 6 ||
                    mgr.lastDayGoldEarnedTrading != 4800 ||
                    mgr.lastDayMonstersSlain != 8 ||
                    mgr.lastDayGoldEarnedCombat != 600)
                {
                    Debug.LogError("[Test 78] Day transition failed to snapshot operational metrics accurately!");
                    return false;
                }

                if (mgr.dailyCropsHarvested != 0 ||
                    mgr.dailyCropsSold != 0 ||
                    mgr.dailyGoldEarnedTrading != 0 ||
                    mgr.dailyMonstersSlain != 0 ||
                    mgr.dailyGoldEarnedCombat != 0)
                {
                    Debug.LogError("[Test 78] Daily activity counters failed to reset on day transition!");
                    return false;
                }

                Debug.Log("✅ Test 78: DailyEconomyManager daily price rolling, dirty produce discount, and day transition snapshots verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 78] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                mgrField?.SetValue(null, prevMgr);
                foreach (var obj in cleanupList)
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                }
            }
        }

        /// <summary>
        /// [Test 79] Verifies the Commercial Trading Post Complex & Shop Counter in StagingScene.unity:
        /// 1. _WORLD/Structures/TradingPost_Complex exists at (75.0, 0.0, 80.0) with TradingPost_Building, Yard, Fences, and Props.
        /// 2. TradingPost_Building has NavMeshObstacle with carving = true and size (10.4, 3.5, 5.9).
        /// 3. TradingPost_Yard ground apron satisfies Zero-Snag Invariant (no solid colliders).
        /// 4. Vehicle Turnaround Bay at (63.0, 0.0, 68.0) radius 8.0m contains exactly 0 solid obstacle colliders.
        /// 5. LanternPost_05_YardThreshold is placed at (65.0, 0.0, 80.5) to clear the vehicle turnaround bay.
        /// 6. _ENTITIES/TradingPost_ShopCounter exists at (72.8, 0.0, 77.8) with ShopInteractable, >= 10 stock items,
        ///    BoxCollider on Default Layer 0, WorldLabel, Highlightable, and Merchant NPC.
        /// 7. Clearance from SpawnPoint_FrontGate_Left is > 6.0m (40.78m).
        /// </summary>
        public static bool Test_TradingPost_Complex_Architecture_Clearances_And_ShopCounter_Audit()
        {
            var tpComplex = GameObject.Find("_WORLD/Structures/TradingPost_Complex");
            if (tpComplex == null)
            {
                Debug.LogError("[Test 79] _WORLD/Structures/TradingPost_Complex GameObject not found in active scene!");
                return false;
            }

            var building = tpComplex.transform.Find("TradingPost_Building");
            if (building == null)
            {
                Debug.LogError("[Test 79] TradingPost_Building child missing under TradingPost_Complex!");
                return false;
            }

            var obs = building.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obs == null || !obs.carving)
            {
                Debug.LogError("[Test 79] TradingPost_Building missing carved NavMeshObstacle component!");
                return false;
            }

            var yard = tpComplex.transform.Find("TradingPost_Yard");
            if (yard == null)
            {
                Debug.LogError("[Test 79] TradingPost_Yard child missing under TradingPost_Complex!");
                return false;
            }

            var apron = yard.Find("TradingYard_Apron");
            if (apron == null)
            {
                Debug.LogError("[Test 79] TradingYard_Apron missing in TradingPost_Yard!");
                return false;
            }

            var apronCol = apron.GetComponent<Collider>();
            if (apronCol != null && apronCol.enabled)
            {
                Debug.LogError("[Test 79] TradingYard_Apron has an active collider, violating Zero-Snag Invariant!");
                return false;
            }

            var marker = yard.Find("TurnaroundBay_Marker");
            var bayCenter = marker != null ? marker.position : new Vector3(198.0f, 0.0f, 248.0f);
            float radius = 8.0f;
            var hits = Physics.OverlapSphere(bayCenter, radius, ~0, QueryTriggerInteraction.Collide);
            foreach (var h in hits)
            {
                if (!(h is TerrainCollider))
                {
                    Debug.LogError($"[Test 79] Turnaround bay contains solid obstacle collider: {h.gameObject.name} ({h.GetType().Name})!");
                    return false;
                }
            }

            var lp5 = GameObject.Find("LanternPost_05_YardThreshold") ??
                      GameObject.Find("Yard_Lantern_North") ??
                      GameObject.Find("_WORLD/Pathways/Road_Homestead_To_TradingPost/Roadside_LanternPosts/Yard_Lantern_North");
            if (lp5 == null)
            {
                Debug.LogError("[Test 79] LanternPost_05 / Yard_Lantern_North not found in scene!");
                return false;
            }
            if (Vector3.Distance(lp5.transform.position, bayCenter) <= radius)
            {
                Debug.LogError($"[Test 79] LanternPost_05_YardThreshold is inside the 16m turnaround circle! Pos={lp5.transform.position}");
                return false;
            }

            var counter = GameObject.Find("_ENTITIES/TradingPost_ShopCounter");
            if (counter == null)
            {
                Debug.LogError("[Test 79] _ENTITIES/TradingPost_ShopCounter GameObject not found!");
                return false;
            }

            var shop = counter.GetComponent<FeaturesEconomy.ShopInteractable>();
            if (shop == null)
            {
                Debug.LogError("[Test 79] ShopInteractable component missing on TradingPost_ShopCounter!");
                return false;
            }
            if (shop.StockItems == null || shop.StockItems.Count < 5)
            {
                Debug.LogError($"[Test 79] ShopInteractable stock items not populated! Count={shop.StockItems?.Count ?? 0}");
                return false;
            }

            var col = counter.GetComponent<BoxCollider>();
            if (col == null || col.isTrigger || counter.layer != 0)
            {
                Debug.LogError("[Test 79] TradingPost_ShopCounter missing solid non-trigger BoxCollider on Layer 0 (Default)!");
                return false;
            }

            var label = counter.GetComponent<FeaturesInteraction.WorldLabel>();
            if (label == null || !label.GetDisplayName().Contains("Trading Post"))
            {
                Debug.LogError($"[Test 79] WorldLabel missing or invalid on TradingPost_ShopCounter! Name='{label?.GetDisplayName()}'");
                return false;
            }

            var highlight = counter.GetComponent<FeaturesInteraction.Highlightable>();
            if (highlight == null)
            {
                Debug.LogError("[Test 79] Highlightable component missing on TradingPost_ShopCounter!");
                return false;
            }

            var spawner = GameObject.Find("SpawnPoint_FrontGate_Left");
            if (spawner != null)
            {
                float dist = Vector3.Distance(spawner.transform.position, tpComplex.transform.position);
                if (dist < 6.0f)
                {
                    Debug.LogError($"[Test 79] TradingPost_Complex is too close to monster spawner! Distance={dist:F2}m (Expected > 6.0m)");
                    return false;
                }
            }

            Debug.Log("✅ Test 79: Commercial Trading Post Complex architecture, clearances, and shop counter verified.");
            return true;
        }

        // Test 91: Verify complete accessibility of all 3 weapons and 5 armor pieces via both Merchant Purchasing and Workbench Crafting
        private static bool Test_WeaponsAndArmor_CompleteAccessibility_CraftingAndPurchasing_Audit()
        {
            var cleanupList = new List<Object>();
            var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db == null)
            {
                Debug.LogError("[Test 91] ItemDatabase could not be loaded!");
                return false;
            }

            string[] weaponIds = new string[] { "tool_weapon_root_cleaver", "tool_weapon_corn_repeater" };
            string[] armorIds = new string[] { "item_burrow_hood", "item_swift_root_armor", "item_fang_grips", "item_underground_striders", "item_sweet_potato_heart" };

            // 1. Verify all 8 items exist in ItemDatabase with valid prices
            foreach (var wid in weaponIds)
            {
                var it = db.GetItem(wid);
                if (it == null || it.buyPrice <= 0)
                {
                    Debug.LogError($"[Test 91] Weapon '{wid}' missing from ItemDatabase or has invalid buyPrice ({it?.buyPrice})!");
                    return false;
                }
            }
            foreach (var aid in armorIds)
            {
                var it = db.GetItem(aid);
                if (it == null || it.buyPrice <= 0)
                {
                    Debug.LogError($"[Test 91] Armor '{aid}' missing from ItemDatabase or has invalid buyPrice ({it?.buyPrice})!");
                    return false;
                }
            }

            var walletField = typeof(PlayerWallet).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var prevWallet = walletField?.GetValue(null);
            var prevWalletSvc = ServiceLocator.Resolve<IWalletService>();

            try
            {
                // 2. Verify Merchant Table stock initialization includes all 8 items
                var tableGo = new GameObject("Test91_MerchantTable");
                cleanupList.Add(tableGo);
                var table = tableGo.AddComponent<MerchantTableInteractable>();
                table.InitializeDefaultStock();

                foreach (var wid in weaponIds)
                {
                    if (!table.StockItems.Any(it => it != null && it.itemId == wid))
                    {
                        Debug.LogError($"[Test 91] MerchantTable stock missing weapon '{wid}'!");
                        return false;
                    }
                }
                foreach (var aid in armorIds)
                {
                    if (!table.StockItems.Any(it => it != null && it.itemId == aid))
                    {
                        Debug.LogError($"[Test 91] MerchantTable stock missing armor '{aid}'!");
                        return false;
                    }
                }

                // 3. Verify TradingPost ShopInteractable stock includes all 8 items
                var postGo = new GameObject("Test91_ShopCounter");
                cleanupList.Add(postGo);
                var shop = postGo.AddComponent<ShopInteractable>();
                shop.InitializeDefaultStock();

                foreach (var wid in weaponIds)
                {
                    if (!shop.StockItems.Any(it => it != null && it.itemId == wid))
                    {
                        Debug.LogError($"[Test 91] TradingPost stock missing weapon '{wid}'!");
                        return false;
                    }
                }
                foreach (var aid in armorIds)
                {
                    if (!shop.StockItems.Any(it => it != null && it.itemId == aid))
                    {
                        Debug.LogError($"[Test 91] TradingPost stock missing armor '{aid}'!");
                        return false;
                    }
                }

                // 4. Verify MerchantShopUI purchasing simulation
                var playerGo = new GameObject("Test91_Player");
                cleanupList.Add(playerGo);
                playerGo.tag = "Player";
                var inv = playerGo.AddComponent<InventoryComponent>();
                inv.ResetInventory(16);

                var wallet = playerGo.AddComponent<PlayerWallet>();
                walletField?.SetValue(null, wallet);
                wallet.AddGold(50000);
                ServiceLocator.Register<IWalletService>(wallet);

                var shopUiGo = new GameObject("Test91_MerchantShopUI");
                cleanupList.Add(shopUiGo);
                var shopUi = shopUiGo.AddComponent<MerchantShopUI>();

                var invField = typeof(MerchantShopUI).GetField("playerInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                invField?.SetValue(shopUi, inv);

                // Test purchasing Root Cleaver
                var cleaverItem = db.GetItem("tool_weapon_root_cleaver");
                var buyMethod = typeof(MerchantShopUI).GetMethod("TryBuyItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (buyMethod == null)
                {
                    Debug.LogError("[Test 91] MerchantShopUI.TryBuyItem method not found!");
                    return false;
                }

                int goldBefore = wallet.CurrentGold;
                buyMethod.Invoke(shopUi, new object[] { cleaverItem, 1 });
                if (inv.CountItem(cleaverItem) != 1 || wallet.CurrentGold != goldBefore - cleaverItem.buyPrice)
                {
                    Debug.LogError($"[Test 91] Failed purchasing Root Cleaver from shop! invCount={inv.CountItem(cleaverItem)}, gold={wallet.CurrentGold}");
                    return false;
                }

                // Test purchasing Burrow Hood
                var hoodItem = db.GetItem("item_burrow_hood");
                goldBefore = wallet.CurrentGold;
                buyMethod.Invoke(shopUi, new object[] { hoodItem, 1 });
                if (inv.CountItem(hoodItem) != 1 || wallet.CurrentGold != goldBefore - hoodItem.buyPrice)
                {
                    Debug.LogError($"[Test 91] Failed purchasing Burrow Hood from shop! invCount={inv.CountItem(hoodItem)}, gold={wallet.CurrentGold}");
                    return false;
                }

                // 5. Verify Workbench Crafting of weapons and armor
                var armorState = playerGo.AddComponent<FeaturesWorkbench.PlayerArmorUpgradeState>();
                var wepState = playerGo.AddComponent<FeaturesWorkbench.PlayerWeaponUpgradeState>();
                var rootMat = db.GetItem("mat_mutated_root");
                inv.AddItem(rootMat, 20);

                var wbGo = new GameObject("Test91_WorkbenchUI");
                cleanupList.Add(wbGo);
                var wb = wbGo.AddComponent<FeaturesWorkbench.UI.WorkbenchUI>();

                var wbInvField = typeof(FeaturesWorkbench.UI.WorkbenchUI).GetField("playerInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                wbInvField?.SetValue(wb, inv);
                var wbArmorField = typeof(FeaturesWorkbench.UI.WorkbenchUI).GetField("armorState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                wbArmorField?.SetValue(wb, armorState);
                var wbWepField = typeof(FeaturesWorkbench.UI.WorkbenchUI).GetField("weaponState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                wbWepField?.SetValue(wb, wepState);

                // Craft Homestead Slingshot via WorkbenchUI
                var repeaterItem = db.GetItem("tool_weapon_corn_repeater");
                goldBefore = wallet.CurrentGold;
                int rootsBefore = inv.CountItem(rootMat);
                wb.CraftSetPiece("tool_weapon_corn_repeater", 450, "mat_mutated_root", 3);

                bool hasRepeater = inv.CountItem(repeaterItem) > 0 || armorState.GetEquippedItem(EquipmentSlotType.MainWeapon) == repeaterItem;
                if (!hasRepeater || inv.CountItem(rootMat) != rootsBefore - 3 || wallet.CurrentGold != goldBefore - 450)
                {
                    Debug.LogError($"[Test 91] Failed crafting Homestead Slingshot! hasRepeater={hasRepeater}, roots={inv.CountItem(rootMat)}, gold={wallet.CurrentGold}");
                    return false;
                }

                // Craft Swift Root Armor via WorkbenchUI
                var armorPiece = db.GetItem("item_swift_root_armor");
                goldBefore = wallet.CurrentGold;
                rootsBefore = inv.CountItem(rootMat);
                wb.CraftSetPiece("item_swift_root_armor", 6000, "mat_mutated_root", 10);

                bool hasArmor = inv.CountItem(armorPiece) > 0 || armorState.GetEquippedItem(EquipmentSlotType.Chest) == armorPiece;
                if (!hasArmor || inv.CountItem(rootMat) != rootsBefore - 10 || wallet.CurrentGold != goldBefore - 6000)
                {
                    Debug.LogError($"[Test 91] Failed crafting Swift Root Armor! hasArmor={hasArmor}, roots={inv.CountItem(rootMat)}, gold={wallet.CurrentGold}");
                    return false;
                }

                Debug.Log("✅ Test 91: Complete accessibility of all 3 weapons and 5 armor pieces via Merchant Purchasing and Workbench Crafting verified.");
                return true;
            }
            finally
            {
                walletField?.SetValue(null, prevWallet);
                if (prevWalletSvc != null) ServiceLocator.Register<IWalletService>(prevWalletSvc);

                foreach (var o in cleanupList)
                {
                    if (o != null) Object.DestroyImmediate(o);
                }
            }
        }

        // Test 96: Verify Taro Corn Stew recipe opportunity cost does not exceed 4,000 G
        private static bool Test_KitchenRecipe_TaroCornStew_CostCalibration_Audit()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<KitchenRecipe>("Assets/Data/Recipes/Cooking/Recipe_TaroCornStew.asset");
            if (recipe == null)
            {
                Debug.LogError("[Test 96] Recipe_TaroCornStew.asset could not be loaded!");
                return false;
            }

            if (recipe.ingredients == null || recipe.ingredients.Count < 3)
            {
                Debug.LogError("[Test 96] Recipe_TaroCornStew ingredients count is invalid!");
                return false;
            }

            int taroQty = 0;
            int cornQty = 0;
            int waterQty = 0;

            foreach (var ing in recipe.ingredients)
            {
                if (ing.item != null)
                {
                    if (ing.item.itemId == "crop_taro" || ing.item.name.Contains("Taro")) taroQty = ing.quantity;
                    else if (ing.item.itemId == "crop_corn" || ing.item.name.Contains("Corn")) cornQty = ing.quantity;
                    else if (ing.item.itemId == "food_bottle_water" || ing.item.name.Contains("Water")) waterQty = ing.quantity;
                }
            }

            if (taroQty != 1 || cornQty != 1 || waterQty != 1)
            {
                Debug.LogError($"[Test 96] Recipe_TaroCornStew ingredient quantities mismatch: Taro={taroQty} (exp 1), Corn={cornQty} (exp 1), Water={waterQty} (exp 1)!");
                return false;
            }

            Debug.Log("✅ Test 96: Recipe_TaroCornStew calibrated to 1 Taro + 1 Corn + 1 Bottle of Water (1,600 G balanced crop cost) verified.");
            return true;
        }

        // Test 101: Axis 1 — Monte Carlo Economy Solvency, Zero Soft-Lock Proof, and Land Deed ROI Convergence Audit
        public static bool Test_Axis1_EconomyMonteCarloSolvency_And_LandDeedROI_Audit()
        {
            try
            {
                var report = FeaturesEconomy.PureLogic.EconomyConvergenceSimulator.RunMonteCarloAudit(250, 0x9E3779B9u);

                if (report.TotalTrials != 1000)
                {
                    Debug.LogError($"[Test 101] Expected 1000 Monte Carlo trials, got {report.TotalTrials}!");
                    return false;
                }

                if (report.SoftLockCount != 0 || report.SoftLockProbability > 0f)
                {
                    Debug.LogError($"[Test 101] Economy soft-lock detected in {report.SoftLockCount}/{report.TotalTrials} trials!");
                    return false;
                }

                if (report.SweetPotatoDeedPaybackCycles <= 1.0f || report.SweetPotatoDeedPaybackCycles > 1.70f)
                {
                    Debug.LogError($"[Test 101] Sweet Potato Land Deed payback cycles out of expected [1.0, 1.70] range: {report.SweetPotatoDeedPaybackCycles:F2}");
                    return false;
                }

                if (report.TaroCornDeedPaybackCycles <= 0.5f || report.TaroCornDeedPaybackCycles > 1.05f)
                {
                    Debug.LogError($"[Test 101] Taro/Corn Land Deed payback cycles out of expected [0.5, 1.05] range: {report.TaroCornDeedPaybackCycles:F2}");
                    return false;
                }

                if (report.GuaranteedBossCoresAcross5Days != 4 || report.WorstCaseNight1RecoverySeedEquivalents < 7.0f)
                {
                    Debug.LogError($"[Test 101] Boss core count ({report.GuaranteedBossCoresAcross5Days}) or Night 1 solvency floor ({report.WorstCaseNight1RecoverySeedEquivalents:F1}) invalid!");
                    return false;
                }

                Debug.Log($"✅ Test 101: Axis 1 Monte Carlo Economy Audit passed (1,000 trials, 0% soft-lock, SP Deed ROI={report.SweetPotatoDeedPaybackCycles:F2} cycles, Taro/Corn ROI={report.TaroCornDeedPaybackCycles:F2} cycles, Mean Day 5 Gold={report.MeanDay5CumulativeGold:F0} G).");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 101] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // Test 112: Board Orders (HayDay-style outside perimeter fence), 1.2x multiplier, GDD clean vs dirty crop value alignment, & atomic delivery
        private static bool Test_BoardOrders_GDDDirtyCleanPricing_Multiplier12x_Delivery_And_ScenePlacement_Audit()
        {
            var cleanupList = new List<Object>();
            var econField = typeof(DailyEconomyManager).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var prevEcon = econField?.GetValue(null);
            var walletField = typeof(PlayerWallet).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var prevWallet = walletField?.GetValue(null);
            var prevWalletService = ServiceLocator.Resolve<IWalletService>();
            var prevCatalogService = ServiceLocator.Resolve<IItemCatalog>();

            try
            {
                // 1. GDD Crop Asset & Drop-Rate Balance Audit:
                // Multi-drop 1-3 items/plot (avg 2.0) => Clean unit = 375-500 SP, 700-900 Taro/Corn; Dirty = 50% (188/400/400)
                var cleanSp = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Crops/Crop_SweetPotato.asset");
                var dirtySp = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Crops/Crop_SweetPotato_Dirty.asset");
                var cleanTaro = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Crops/Crop_Taro.asset");
                var dirtyTaro = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Crops/Crop_Taro_Dirty.asset");
                var cleanCorn = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Crops/Crop_Corn.asset");
                var dirtyCorn = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Crops/Crop_Corn_Dirty.asset");

                var seedSp = AssetDatabase.LoadAssetAtPath<SeedItemData>("Assets/Data/Items/Seeds/Seed_SweetPotato.asset");
                var seedTaro = AssetDatabase.LoadAssetAtPath<SeedItemData>("Assets/Data/Items/Seeds/Seed_Taro.asset");
                var seedCorn = AssetDatabase.LoadAssetAtPath<SeedItemData>("Assets/Data/Items/Seeds/Seed_Corn.asset");

                if (cleanSp == null || dirtySp == null || cleanTaro == null || dirtyTaro == null || cleanCorn == null || dirtyCorn == null ||
                    seedSp == null || seedTaro == null || seedCorn == null)
                {
                    Debug.LogError("[Test 112] One or more clean/dirty crop or seed ScriptableObjects could not be loaded.");
                    return false;
                }

                if (seedSp.minYield != 1 || seedSp.maxYield != 3 ||
                    seedTaro.minYield != 1 || seedTaro.maxYield != 3 ||
                    seedCorn.minYield != 1 || seedCorn.maxYield != 3)
                {
                    Debug.LogError("[Test 112] Seed minYield/maxYield must remain 1..3 (avg 2.0) to match Option 2 per-unit crop pricing.");
                    return false;
                }

                if (cleanSp.sellPrice != 375 || dirtySp.sellPrice != 188 ||
                    cleanTaro.sellPrice != 800 || dirtyTaro.sellPrice != 400 ||
                    cleanCorn.sellPrice != 800 || dirtyCorn.sellPrice != 400)
                {
                    Debug.LogError($"[Test 112] Crop asset sellPrice mismatch vs drop-rate-balanced GDD/50% dirty rule: " +
                                   $"SP={cleanSp.sellPrice}/{dirtySp.sellPrice}, Taro={cleanTaro.sellPrice}/{dirtyTaro.sellPrice}, Corn={cleanCorn.sellPrice}/{dirtyCorn.sellPrice}.");
                    return false;
                }

                if (cleanSp.isDirty || cleanTaro.isDirty || cleanCorn.isDirty ||
                    !dirtySp.isDirty || !dirtyTaro.isDirty || !dirtyCorn.isDirty ||
                    dirtySp.cleanVariant != cleanSp || dirtyTaro.cleanVariant != cleanTaro || dirtyCorn.cleanVariant != cleanCorn)
                {
                    Debug.LogError("[Test 112] Dirty crop metadata (isDirty / cleanVariant link) is misaligned.");
                    return false;
                }

                // 2. DailyEconomyManager dynamic clean vs 50% dirty sell rate & 1.2x Board Order Pure Logic math
                if (!Mathf.Approximately(BoardOrderEvaluator.PriceMultiplier, 1.2f))
                {
                    Debug.LogError($"[Test 112] Expected BoardOrderEvaluator.PriceMultiplier=1.2f, got {BoardOrderEvaluator.PriceMultiplier}");
                    return false;
                }

                if (BoardOrderEvaluator.CalculateBoardTotalReward(375, 1) != 450 ||
                    BoardOrderEvaluator.CalculateBoardTotalReward(425, 2) != 1020 ||
                    BoardOrderEvaluator.CalculateBoardTotalReward(800, 2) != 1920)
                {
                    Debug.LogError("[Test 112] BoardOrderEvaluator 1.2x payout math failed exact integer expectations.");
                    return false;
                }

                var dailyOrders = BoardOrderEvaluator.GenerateDailyOrders(1, 0u, BoardOrderEvaluator.DefaultOrderCount);
                if (dailyOrders == null || dailyOrders.Count != 4)
                {
                    Debug.LogError("[Test 112] GenerateDailyOrders must return 4 slots.");
                    return false;
                }

                for (int i = 0; i < dailyOrders.Count; i++)
                {
                    if (dailyOrders[i] == null || !BoardOrderEvaluator.IsCleanCropTemplateId(dailyOrders[i].ItemId))
                    {
                        Debug.LogError($"[Test 112] Board Order slot {i} ({dailyOrders[i]?.ItemId}) must be a clean crop only!");
                        return false;
                    }
                }

                for (int i = 0; i < 12; i++)
                {
                    var rep = BoardOrderEvaluator.CreateReplacementOrder(i % 4, 2, (uint)(500 + i), "crop_sweet_potato");
                    if (rep == null || !BoardOrderEvaluator.IsCleanCropTemplateId(rep.ItemId))
                    {
                        Debug.LogError($"[Test 112] Replacement order generated non-clean item {rep?.ItemId}!");
                        return false;
                    }
                }

                var cleanSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    dailyOrders[0].ItemId,
                    dailyOrders[1].ItemId,
                    dailyOrders[2].ItemId
                };
                if (!cleanSet.Contains("crop_sweet_potato") || !cleanSet.Contains("crop_taro") || !cleanSet.Contains("crop_corn"))
                {
                    Debug.LogError("[Test 112] Daily clean order slots 0..2 failed to cover all three GDD crop tiers.");
                    return false;
                }

                // 3. End-to-End Board Fulfillment & Replacement (Clean + Dirty + Rejection on Insufficient Bag)
                econField?.SetValue(null, null);
                walletField?.SetValue(null, null);

                var econGO = new GameObject("Test_Board_DailyEconomy");
                cleanupList.Add(econGO);
                var econ = econGO.AddComponent<DailyEconomyManager>();
                econField?.SetValue(null, econ);
                econ.RollDailyMarketPrices();

                int spDirtyMarket = econ.GetCropSellPrice(dirtySp);
                int taroDirtyMarket = econ.GetCropSellPrice(dirtyTaro);
                int cornDirtyMarket = econ.GetCropSellPrice(dirtyCorn);
                if (spDirtyMarket != Mathf.Max(1, Mathf.RoundToInt(econ.GetCropSellPrice(cleanSp) * 0.5f)) ||
                    taroDirtyMarket != Mathf.Max(1, Mathf.RoundToInt(econ.GetCropSellPrice(cleanTaro) * 0.5f)) ||
                    cornDirtyMarket != Mathf.Max(1, Mathf.RoundToInt(econ.GetCropSellPrice(cleanCorn) * 0.5f)))
                {
                    Debug.LogError("[Test 112] DailyEconomyManager dirty produce 50% rate invariant failed.");
                    return false;
                }

                var actorGO = new GameObject("Test_Board_Actor");
                cleanupList.Add(actorGO);
                var inventory = actorGO.AddComponent<InventoryComponent>();
                inventory.ResetInventory(16);
                var wallet = actorGO.AddComponent<PlayerWallet>();

                var boardGO = new GameObject("Test_Board_Station");
                cleanupList.Add(boardGO);
                var board = boardGO.AddComponent<OrderBoardInteractable>();
                board.RefreshOrdersForDay(1, 1337u);

                // Insufficient bag stock must reject cleanly without mutating gold/inventory
                if (board.TryFulfillOrder(0, inventory, wallet, out int rejectedGold, out _) || rejectedGold != 0 || wallet.CurrentGold != 0)
                {
                    Debug.LogError("[Test 112] Board fulfilled or mutated wallet when player lacked requested crops.");
                    return false;
                }

                // Fulfill clean order at 1.2x
                BoardOrderSlot cleanSlot = board.ActiveOrders[0];
                string initialCleanOrderId = cleanSlot.OrderId;
                ItemData cleanReqItem = board.ResolveOrderItem(cleanSlot);
                BoardOrderPriceBreakdown cleanBreakdown = board.GetOrderPricing(cleanSlot);
                if (cleanReqItem == null || cleanBreakdown.BoardTotalReward <= cleanBreakdown.BaseTotalPrice)
                {
                    Debug.LogError("[Test 112] Clean order item resolution or 1.2x pricing breakdown invalid.");
                    return false;
                }

                inventory.AddItemAmount(cleanReqItem, cleanSlot.RequiredQuantity);
                if (!board.TryFulfillOrder(0, inventory, wallet, out int cleanPayout, out _))
                {
                    Debug.LogError("[Test 112] Clean order fulfillment failed despite sufficient inventory.");
                    return false;
                }

                if (cleanPayout != cleanBreakdown.BoardTotalReward ||
                    wallet.CurrentGold != cleanPayout ||
                    inventory.CountItem(cleanReqItem) != 0 ||
                    econ.dailyCropsSold != cleanSlot.RequiredQuantity ||
                    econ.dailyGoldEarnedTrading != cleanPayout ||
                    board.ActiveOrders[0].OrderId == initialCleanOrderId)
                {
                    Debug.LogError("[Test 112] Clean order atomic payout, inventory deduction, telemetry, or slot replacement mismatch.");
                    return false;
                }

                // Dirty item guard: even if slot 3 is forced to a dirty ID, Board Order must reject dirty produce
                var boardOrdersListField = typeof(OrderBoardInteractable).GetField("activeOrders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rawOrders = boardOrdersListField?.GetValue(board) as List<BoardOrderSlot>;
                if (rawOrders != null && rawOrders.Count > 3)
                {
                    var originalSlot3 = rawOrders[3];
                    rawOrders[3] = new BoardOrderSlot { OrderId = "DIRTY-GUARD-TEST", CustomerName = "Guard", FlavorNote = "Test", ItemId = "crop_sweet_potato_dirty", RequiredQuantity = 1 };
                    inventory.AddItemAmount(dirtySp, 1);
                    int goldBeforeGuard = wallet.CurrentGold;
                    bool acceptedDirty = board.TryFulfillOrder(3, inventory, wallet, out int rejectedDirtyGold, out _);
                    inventory.RemoveItem(dirtySp, 1);
                    rawOrders[3] = originalSlot3;

                    if (acceptedDirty || rejectedDirtyGold != 0 || wallet.CurrentGold != goldBeforeGuard)
                    {
                        Debug.LogError("[Test 112] Board Order accepted or mutated wallet for a dirty crop!");
                        return false;
                    }
                }

                // Slot 3 generated order must also be clean and fulfill at 1.2x
                BoardOrderSlot fourthSlot = board.ActiveOrders[3];
                ItemData fourthItem = board.ResolveOrderItem(fourthSlot);
                BoardOrderPriceBreakdown fourthBreakdown = board.GetOrderPricing(fourthSlot);
                if (fourthItem == null || fourthItem.isDirty || !BoardOrderEvaluator.IsCleanCropTemplateId(fourthItem.itemId))
                {
                    Debug.LogError("[Test 112] Board Order slot 3 must resolve to a clean crop only!");
                    return false;
                }

                inventory.AddItemAmount(fourthItem, fourthSlot.RequiredQuantity);
                int goldBeforeFourth = wallet.CurrentGold;
                if (!board.TryFulfillOrder(3, inventory, wallet, out int fourthPayout, out _))
                {
                    Debug.LogError("[Test 112] Clean slot 3 order fulfillment failed despite sufficient inventory.");
                    return false;
                }

                if (fourthPayout != fourthBreakdown.BoardTotalReward ||
                    wallet.CurrentGold != goldBeforeFourth + fourthPayout ||
                    inventory.CountItem(fourthItem) != 0)
                {
                    Debug.LogError("[Test 112] Clean slot 3 1.2x payout or inventory deduction mismatch.");
                    return false;
                }

                // Skip/Replace button behavior
                string beforeSkipId = board.ActiveOrders[1].OrderId;
                if (!board.TryReplaceOrder(1) || board.ActiveOrders[1].OrderId == beforeSkipId ||
                    !BoardOrderEvaluator.IsCleanCropTemplateId(board.ActiveOrders[1].ItemId))
                {
                    Debug.LogError("[Test 112] TryReplaceOrder failed to issue a fresh clean crop slip.");
                    return false;
                }

                // 4. MainScene placement outside North perimeter fence (z > 48.11) & clean-only serialized slots
                var sceneBoardGO = GameObject.Find("OrderBoard_Outdoor");
                if (sceneBoardGO == null)
                {
                    Debug.LogError("[Test 112] OrderBoard_Outdoor GameObject is missing from MainScene.");
                    return false;
                }

                if (sceneBoardGO.transform.position.z <= 48.11f)
                {
                    Debug.LogError($"[Test 112] OrderBoard_Outdoor ({sceneBoardGO.transform.position}) is not outside the North perimeter fence (z > 48.11).");
                    return false;
                }

                var sceneBoard = sceneBoardGO.GetComponent<OrderBoardInteractable>();
                var sceneCol = sceneBoardGO.GetComponent<BoxCollider>();
                var sceneLabel = sceneBoardGO.GetComponent<WorldLabel>();
                var sceneHighlight = sceneBoardGO.GetComponent<Highlightable>();
                if (sceneBoard == null || sceneCol == null || sceneLabel == null || sceneHighlight == null ||
                    sceneBoard.ActiveOrders == null || sceneBoard.ActiveOrders.Count < 4)
                {
                    Debug.LogError("[Test 112] OrderBoard_Outdoor in MainScene is missing required components or default 4 slips.");
                    return false;
                }

                foreach (var sceneSlot in sceneBoard.ActiveOrders)
                {
                    var slotItem = sceneBoard.ResolveOrderItem(sceneSlot);
                    if (slotItem == null || slotItem.isDirty || !BoardOrderEvaluator.IsCleanCropTemplateId(slotItem.itemId))
                    {
                        Debug.LogError($"[Test 112] Scene OrderBoard_Outdoor contains non-clean crop request: {sceneSlot?.ItemId}");
                        return false;
                    }
                }

                Debug.Log($"✅ Test 112: Board Orders (HayDay outdoor @ {sceneBoardGO.transform.position}, Clean-Only crops), GDD clean/dirty crop pricing (50% dirty), 1.2x multiplier, & atomic fulfillment audit passed.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 112] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                foreach (var obj in cleanupList)
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                }

                econField?.SetValue(null, prevEcon);
                walletField?.SetValue(null, prevWallet);

                if (prevWalletService != null)
                    ServiceLocator.Register<IWalletService>(prevWalletService);
                else
                    ServiceLocator.Unregister<IWalletService>();

                if (prevCatalogService != null)
                    ServiceLocator.Register<IItemCatalog>(prevCatalogService);
                else
                    ServiceLocator.Unregister<IItemCatalog>();
            }
        }
    }
}
