using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using FeaturesEconomy;
using FeaturesFarming;
using FeaturesKitchen;
using FeaturesWardrobe;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;

namespace FeaturesSaveSystem
{
    public partial class SaveSystemManager
    {
        #region Game State Capture & Apply

        private GameSaveData CaptureCurrentGameState()
        {
            var data = new GameSaveData
            {
                saveVersion = 1
            };

            // 1. Player Transform & Stats & Inventory
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                data.playerPosX = player.transform.position.x;
                data.playerPosY = player.transform.position.y;
                data.playerPosZ = player.transform.position.z;
                data.playerRotY = player.transform.eulerAngles.y;

                var playerCtx = ServiceLocator.Resolve<IPlayerContext>();
                if (playerCtx != null)
                {
                    data.health = Mathf.Max(1, playerCtx.Health);
                    data.stamina = Mathf.Max(10f, playerCtx.Stamina);
                    data.hunger = Mathf.Max(10f, playerCtx.Hunger);
                    data.thirst = Mathf.Max(10f, playerCtx.Thirst);
                }
                else
                {
                    data.health = 100;
                    data.stamina = 100f;
                    data.hunger = 100f;
                    data.thirst = 100f;
                }

                var inv = player.GetComponent<InventoryComponent>();
                if (inv != null)
                {
                    data.selectedHotbarIndex = inv.selectedHotbarIndex;

                    if (inv.slots != null)
                    {
                        for (int i = 0; i < inv.slots.Count; i++)
                        {
                            var slot = inv.slots[i];
                            if (slot != null && !slot.IsEmpty && slot.item != null)
                            {
                                string id = !string.IsNullOrEmpty(slot.item.itemId) ? slot.item.itemId : slot.item.name;
                                data.inventorySlots.Add(new SavedInventorySlot
                                {
                                    slotIndex = i,
                                    itemId = id,
                                    quantity = slot.quantity
                                });
                            }
                        }
                    }
                }

                var buffService = ServiceLocator.Resolve<IBuffStateService>() ?? ServiceLocator.Resolve<IPlayerContext>()?.GetPlayerComponent<IBuffStateService>();
                if (buffService != null)
                {
                    var snapshots = new List<BuffSnapshotData>();
                    buffService.ExportBuffSnapshots(snapshots);
                    for (int i = 0; i < snapshots.Count; i++)
                    {
                        var sb = snapshots[i];
                        data.activeBuffs.Add(new SavedActiveBuff
                        {
                            buffName = sb.BuffName,
                            buffType = sb.BuffType,
                            value = sb.Value,
                            duration = sb.Duration,
                            remainingDuration = sb.RemainingDuration,
                            description = sb.Description
                        });
                    }
                }
            }

            // 2. Gold
            if (PlayerWallet.Instance != null)
            {
                data.gold = PlayerWallet.Instance.CurrentGold;
            }

            // 3. Water Bottle, Plant Waterer & Garden Well
            if (PlayerWaterBottle.Instance != null)
            {
                data.waterBottleAmount = PlayerWaterBottle.Instance.CurrentWater;
            }
            if (FeaturesFarming.PlantWaterer.Instance != null)
            {
                data.plantWatererAmount = FeaturesFarming.PlantWaterer.Instance.CurrentWater;
            }
            if (FeaturesFarming.WaterWellInteractable.Instance != null)
            {
                var well = FeaturesFarming.WaterWellInteractable.Instance;
                data.wellHasWaterer = well.HasWatererInWell;
                data.wellWatererReady = well.IsWatererReadyToCollect;
                data.wellWatererProgress = well.RefillWaterProgress;
                data.wellWatererStartAmount = well.WatererStartAmount;
                data.wellRefillsUsedToday = well.RefillsUsedToday;
            }

            // 4. Time System (24h continuous clock & phase)
            if (TimeManager.Instance != null)
            {
                data.currentDay = TimeManager.Instance.currentDay;
                data.currentPhase = (int)TimeManager.Instance.currentPhase;
                data.isNightEncounterCleared = TimeManager.Instance.isNightEncounterCleared;
            }
            else
            {
                data.currentDay = 1;
                data.currentPhase = 0;
            }

            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                data.currentHour = FeaturesTime.DayNightTimeManager.Instance.CurrentHour;
            }
            else
            {
                data.currentHour = data.currentPhase == 1 ? 18.0f : 6.0f;
            }

            // 5. Farmland Crops & Capacity
            data.unlockedFarmlandPlots = FeaturesFarming.FarmlandExpansionManager.Instance != null
                ? FeaturesFarming.FarmlandExpansionManager.Instance.UnlockedPlotCount
                : 6;
            var tiles = FindObjectsByType<FarmlandTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (tile == null) continue;
                    string seedId = "";
                    if (tile.PlantedSeed != null)
                    {
                        seedId = !string.IsNullOrEmpty(tile.PlantedSeed.itemId) ? tile.PlantedSeed.itemId : tile.PlantedSeed.name;
                    }

                    data.farmlandTiles.Add(new SavedFarmlandTile
                    {
                        tileName = tile.gameObject.name,
                        tileState = (int)tile.CurrentState,
                        seedItemId = seedId,
                        growthProgress = tile.GrowthProgress,
                        currentTimer = tile.CurrentTimer,
                        growthDuration = tile.GrowthDuration
                    });
                }
            }

            // 6. Daily Economy Market Prices & Stats
            if (DailyEconomyManager.Instance != null)
            {
                data.sweetPotatoPrice = DailyEconomyManager.Instance.SweetPotatoPrice;
                data.taroPrice = DailyEconomyManager.Instance.TaroPrice;
                data.cornPrice = DailyEconomyManager.Instance.CornPrice;
                data.dailyCropsHarvested = DailyEconomyManager.Instance.dailyCropsHarvested;
                data.dailyCropsSold = DailyEconomyManager.Instance.dailyCropsSold;
                data.dailyGoldEarnedTrading = DailyEconomyManager.Instance.dailyGoldEarnedTrading;
                data.dailyMonstersSlain = DailyEconomyManager.Instance.dailyMonstersSlain;
                data.dailyGoldEarnedCombat = DailyEconomyManager.Instance.dailyGoldEarnedCombat;
            }

            // 7. Wardrobe
            if (PlayerOutfit.Instance != null && PlayerOutfit.Instance.CurrentOutfit != null)
            {
                data.outfitName = PlayerOutfit.Instance.CurrentOutfit.name;
                data.isHatEquipped = PlayerOutfit.Instance.isHatEquipped;
            }

            // 7. Weapon & Armor Upgrades
            if (FeaturesWorkbench.PlayerWeaponUpgradeState.Instance != null)
            {
                var wu = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance;
                data.weaponLevel = wu.weaponLevel;
                data.activeSpecialization = (int)wu.activeSpecialization;
                data.specializationNodeLevel = wu.specializationNodeLevel;
                data.totalBossesDefeated = wu.totalBossesDefeated;
                data.sweetPotatoPathUnlocked = wu.sweetPotatoPathUnlocked;
                data.taroPathUnlocked = wu.taroPathUnlocked;
                data.cornPathUnlocked = wu.cornPathUnlocked;
            }
            if (FeaturesWorkbench.PlayerArmorUpgradeState.Instance != null)
            {
                var au = FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
                data.armorLevel = au.armorLevel;
                if (data.equippedSlots == null) data.equippedSlots = new List<SavedEquippedSlot>();
                data.equippedSlots.Clear();
                foreach (EquipmentSlotType slot in System.Enum.GetValues(typeof(EquipmentSlotType)))
                {
                    var item = au.GetEquippedItem(slot);
                    if (item != null && !string.IsNullOrEmpty(item.itemId))
                    {
                        data.equippedSlots.Add(new SavedEquippedSlot { slotType = (int)slot, itemId = item.itemId });
                    }
                }
            }

            // 8. Container Inventories (Storage, Refrigerator, Trophy Cabinet)
            CaptureContainerInventories(data);

            // 9. Combat & Night Brawl
            if (FeaturesCombat.NightBrawlManager.Instance != null)
            {
                data.nightCurrentWave = FeaturesCombat.NightBrawlManager.Instance.CurrentWave;
                data.nightTotalWaves = FeaturesCombat.NightBrawlManager.Instance.TotalWaves;
                data.isNightBrawlActive = FeaturesCombat.NightBrawlManager.Instance.IsNightBrawlActive;

                var enemies = FeaturesCombat.NightBrawlManager.Instance.ActiveEnemies;
                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy != null && !enemy.IsDead)
                        {
                            data.activeEnemies.Add(new SavedEnemyData
                            {
                                enemyType = (int)enemy.enemyType,
                                currentHealth = enemy.currentHealth,
                                maxHealth = enemy.maxHealth,
                                posX = enemy.transform.position.x,
                                posY = enemy.transform.position.y,
                                posZ = enemy.transform.position.z,
                                rotY = enemy.transform.eulerAngles.y
                            });
                        }
                    }
                }

                var pendingSpawns = FeaturesCombat.NightBrawlManager.Instance.PendingWaveSpawns;
                if (pendingSpawns != null)
                {
                    for (int i = 0; i < pendingSpawns.Count; i++)
                    {
                        data.nightBrawlPendingWaveEnemies.Add((int)pendingSpawns[i]);
                    }
                }
            }

            // 9b. Kitchen Sink Washing & Bottle Refill State
            var kitchenSink = KitchenSinkInteractable.Instance ?? FindFirstObjectByType<KitchenSinkInteractable>(FindObjectsInactive.Include);
            if (kitchenSink != null)
            {
                var inSlot = kitchenSink.InputSlot;
                if (inSlot != null && !inSlot.IsEmpty && inSlot.item != null)
                {
                    data.sinkInputItemId = !string.IsNullOrEmpty(inSlot.item.itemId) ? inSlot.item.itemId : inSlot.item.name;
                    data.sinkInputQuantity = inSlot.quantity;
                }

                var outSlot = kitchenSink.OutputSlot;
                if (outSlot != null && !outSlot.IsEmpty && outSlot.item != null)
                {
                    data.sinkOutputItemId = !string.IsNullOrEmpty(outSlot.item.itemId) ? outSlot.item.itemId : outSlot.item.name;
                    data.sinkOutputQuantity = outSlot.quantity;
                }

                data.sinkIsWashing = kitchenSink.IsWashing;
                data.sinkWashProgress = kitchenSink.WashProgress;
                data.sinkHasBottle = kitchenSink.HasBottleInSink;
                data.sinkIsRefillingBottle = kitchenSink.IsRefillingWater;
                data.sinkBottleReady = kitchenSink.IsBottleReadyToCollect;
                data.sinkBottleProgress = kitchenSink.RefillWaterProgress;
                data.sinkBottleRefillsUsedToday = kitchenSink.BottleRefillsUsedToday;
            }

            // 9c. Kitchen Stove Active Burners & Output State
            var kitchenStove = KitchenStove.Instance ?? FindFirstObjectByType<KitchenStove>(FindObjectsInactive.Include);
            if (kitchenStove != null && kitchenStove.CookingSlots != null)
            {
                for (int i = 0; i < kitchenStove.CookingSlots.Length; i++)
                {
                    var slot = kitchenStove.CookingSlots[i];
                    if (slot == null || (!slot.IsCooking && !slot.HasOutput)) continue;

                    string recName = slot.activeRecipe != null
                        ? (!string.IsNullOrEmpty(slot.activeRecipe.recipeName) ? slot.activeRecipe.recipeName : slot.activeRecipe.name)
                        : "";
                    ItemData outItem = slot.HasOutput ? slot.outputSlot.item : slot.activeRecipe?.output;
                    string outId = outItem != null
                        ? (!string.IsNullOrEmpty(outItem.itemId) ? outItem.itemId : outItem.name)
                        : "";
                    int outQty = slot.HasOutput
                        ? slot.outputSlot.quantity
                        : (slot.activeRecipe != null ? slot.activeRecipe.outputCount : 1);

                    data.stoveBurnerSlots.Add(new SavedStoveBurnerSlot
                    {
                        slotIndex = slot.slotIndex,
                        state = (int)slot.state,
                        recipeName = recName,
                        remainingTime = slot.remainingTime,
                        totalDuration = slot.totalDuration,
                        outputItemId = outId,
                        outputQuantity = outQty
                    });
                }
            }

            // 10. Storyline Progression
            var storyService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IStorylineService>();
            if (storyService != null)
            {
                data.storyChapter = storyService.CurrentChapter;
                data.triggeredStoryBeats = new List<string>(storyService.TriggeredBeatIds);
                data.unlockedJournalEntries = new List<string>(storyService.UnlockedJournalIds);
                data.isStoryComplete = storyService.IsStoryComplete;

                if (storyService.FactBoard != null)
                {
                    data.narrativeFacts = new int[(int)FarmBeware.Core.Runtime.NarrativeFactKey.MaxCount];
                    storyService.FactBoard.CopyRawFacts(data.narrativeFacts);
                }

                storyService.ExportCooldowns(out data.lastPlayedBeatKeys, out data.lastPlayedDays);
                storyService.ExportPendingModals(out data.pendingModalBeatIds);
            }

            // 11. Quest Progress Persistence
            var questService = ServiceLocator.Resolve<INarrativeObjectiveService>();
            if (questService != null)
            {
                data.questPayload = questService.ExportSavePayload();
            }

            return data;
        }

        private void ApplyLoadedGameState(GameSaveData data)
        {
            // 1. Player Transform & Physics (Clean Teleport with linear/angular velocity reset)
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                Vector3 targetPos = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
                // Void fallback: if coordinates are (0,0,0) or below ground
                if (targetPos.sqrMagnitude < 0.01f || targetPos.y < -20f)
                {
                    targetPos = new Vector3(27.1f, 0.5f, 21.04f);
                    var bed = FindFirstObjectByType<BedInteractable>(FindObjectsInactive.Include);
                    if (bed != null)
                    {
                        targetPos = bed.transform.position + bed.transform.forward * 0.9f + Vector3.up * 0.5f;
                    }
                }
                Quaternion targetRot = Quaternion.Euler(0f, data.playerRotY, 0f);

                var playerCtx = ServiceLocator.Resolve<IPlayerContext>();
                if (playerCtx != null)
                {
                    playerCtx.Teleport(targetPos, targetRot);
                }
                else
                {
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.position = targetPos;
                        rb.rotation = targetRot;
                    }
                    player.transform.position = targetPos;
                    player.transform.rotation = targetRot;
                    Physics.SyncTransforms();
                }

                // 2. Stats
                int restoredHealth = Mathf.Max(1, data.health > 0 ? data.health : 100);
                float restoredStamina = Mathf.Max(10f, data.stamina > 0f ? data.stamina : 100f);
                float restoredHunger = Mathf.Max(10f, data.hunger > 0f ? data.hunger : 100f);
                float restoredThirst = Mathf.Max(10f, data.thirst > 0f ? data.thirst : 100f);

                if (playerCtx != null)
                {
                    playerCtx.RestoreStats(restoredHealth, restoredStamina, restoredHunger, restoredThirst);
                    playerCtx.IsInputLocked = false;
                    playerCtx.SetControlLock(false);
                }

                // 3. Inventory & Hotbar Selection
                var inv = player.GetComponent<InventoryComponent>();
                if (inv != null && inv.slots != null)
                {
                    for (int i = 0; i < inv.slots.Count; i++)
                    {
                        if (inv.slots[i] != null)
                        {
                            inv.slots[i].item = null;
                            inv.slots[i].quantity = 0;
                        }
                    }

                    foreach (var savedSlot in data.inventorySlots)
                    {
                        if (savedSlot.slotIndex >= 0 && savedSlot.slotIndex < inv.slots.Count)
                        {
                            var item = ResolveItem(savedSlot.itemId);
                            if (item != null)
                            {
                                inv.slots[savedSlot.slotIndex].item = item;
                                inv.slots[savedSlot.slotIndex].quantity = savedSlot.quantity;
                            }
                        }
                    }

                    inv.selectedHotbarIndex = Mathf.Clamp(data.selectedHotbarIndex, 0, Mathf.Max(0, inv.slots.Count - 1));
                    inv.OnInventoryChanged?.Invoke();
                    inv.OnHotbarSelected?.Invoke(inv.selectedHotbarIndex);

                    if (playerCtx != null)
                    {
                        playerCtx.UpdateEquipmentVisual(inv.selectedHotbarIndex);
                    }
                }

                // 3b. Active Food Buffs (always clear prior buffs if save has none)
                var buffService = ServiceLocator.Resolve<IBuffStateService>() ?? playerCtx?.GetPlayerComponent<IBuffStateService>();
                if (buffService != null)
                {
                    if (data.activeBuffs == null || data.activeBuffs.Count == 0)
                    {
                        buffService.RestoreFromBuffSnapshots(null);
                    }
                    else
                    {
                        var snapshots = new List<BuffSnapshotData>(data.activeBuffs.Count);
                        for (int i = 0; i < data.activeBuffs.Count; i++)
                        {
                            var sb = data.activeBuffs[i];
                            if (sb == null || sb.remainingDuration <= 0f) continue;
                            snapshots.Add(new BuffSnapshotData(sb.buffName, sb.buffType, sb.value, sb.duration, sb.remainingDuration, sb.description));
                        }
                        buffService.RestoreFromBuffSnapshots(snapshots);
                    }
                }
            }

            // 4. Gold
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.SetGold(data.gold);
            }

            // 5. Water Bottle, Plant Waterer & Garden Well
            if (PlayerWaterBottle.Instance != null)
            {
                float bottleAmt = data.waterBottleAmount >= 0f ? Mathf.Clamp(data.waterBottleAmount, 0f, 100f) : 100f;
                PlayerWaterBottle.Instance.SetWater(bottleAmt);
            }
            if (FeaturesFarming.PlantWaterer.Instance != null)
            {
                float plantAmt = data.plantWatererAmount >= 0f ? data.plantWatererAmount : 100f;
                FeaturesFarming.PlantWaterer.Instance.SetWater(plantAmt);
            }
            if (FeaturesFarming.WaterWellInteractable.Instance != null)
            {
                FeaturesFarming.WaterWellInteractable.Instance.RestoreWellState(
                    data.wellHasWaterer,
                    data.wellWatererReady,
                    data.wellWatererProgress,
                    data.wellWatererStartAmount,
                    data.wellRefillsUsedToday
                );
            }

            // 6. Weapon & Armor Upgrades
            if (FeaturesWorkbench.PlayerWeaponUpgradeState.Instance != null)
            {
                var wu = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance;
                wu.weaponLevel = Mathf.Clamp(data.weaponLevel, 1, 10);
                wu.activeSpecialization = (FeaturesWorkbench.Core.SpecializationPath)data.activeSpecialization;
                wu.specializationNodeLevel = data.specializationNodeLevel;
                wu.totalBossesDefeated = data.totalBossesDefeated;
                if (data.activeSpecialization == 0)
                {
                    wu.sweetPotatoPathUnlocked = data.sweetPotatoPathUnlocked;
                    wu.taroPathUnlocked = data.taroPathUnlocked;
                    wu.cornPathUnlocked = data.cornPathUnlocked;
                }
                wu.RecalculateStats();
            }
            if (FeaturesWorkbench.PlayerArmorUpgradeState.Instance != null)
            {
                var au = FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
                au.armorLevel = Mathf.Clamp(data.armorLevel, 1, 10);
                if (data.equippedSlots != null)
                {
                    var db = ItemDatabase.Instance;
                    foreach (var eq in data.equippedSlots)
                    {
                        var item = db?.GetItem(eq.itemId);
                        if (item != null)
                        {
                            au.EquipItem((EquipmentSlotType)eq.slotType, item, out _);
                        }
                    }
                }
            }

            // 7. Container Inventories (Storage, Refrigerator, Trophy Cabinet)
            RestoreContainerInventories(data);

            // 7b. Kitchen Sink Washing & Bottle Refill State
            var kitchenSink = KitchenSinkInteractable.Instance ?? FindFirstObjectByType<KitchenSinkInteractable>(FindObjectsInactive.Include);
            if (kitchenSink != null)
            {
                kitchenSink.RestoreSinkState(
                    ResolveItem(data.sinkInputItemId),
                    data.sinkInputQuantity,
                    ResolveItem(data.sinkOutputItemId),
                    data.sinkOutputQuantity,
                    data.sinkIsWashing,
                    data.sinkWashProgress,
                    data.sinkHasBottle,
                    data.sinkBottleReady,
                    data.sinkBottleProgress,
                    data.sinkBottleRefillsUsedToday
                );
            }

            // 7c. Kitchen Stove Active Burners & Output State
            var kitchenStove = KitchenStove.Instance ?? FindFirstObjectByType<KitchenStove>(FindObjectsInactive.Include);
            if (kitchenStove != null)
            {
                kitchenStove.RestoreStoveState(data.stoveBurnerSlots, ResolveItem);
            }

            // 8. Snap Camera to Player immediately
            if (FeaturesCamera.IsometricCameraController.Instance != null)
            {
                FeaturesCamera.IsometricCameraController.Instance.SnapToTarget();
            }

            // Cancel any DayTransitionUI fade so no false 'Morning' banner is shown
            DayTransitionUI.Instance?.CancelTransition();

            // 9. Time & Day State
            var phase = (TimeManager.DayPhase)data.currentPhase;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetTimeState(
                    data.currentDay,
                    phase,
                    data.isNightEncounterCleared,
                    notifyPhaseChanged: false
                );
            }

            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                float targetHour = data.currentHour > 0f ? data.currentHour : (data.currentPhase == 1 ? 18.0f : 6.0f);
                FeaturesTime.DayNightTimeManager.Instance.SetDayAndTime(data.currentDay, targetHour);
            }

            // 10. Instant Atmospheric Environment Sync
            var lightingControllers = FindObjectsByType<FeaturesTime.Atmosphere.DayNightLightingController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lc in lightingControllers)
            {
                if (lc != null) lc.ApplyPresetInstant(phase);
            }

            var audioControllers = FindObjectsByType<FeaturesTime.Atmosphere.DayNightAudioController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ac in audioControllers)
            {
                if (ac != null) ac.ApplyInstant(phase);
            }

            var safeZoneLights = FindObjectsByType<FeaturesTime.Atmosphere.HouseSafeZoneLighting>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sz in safeZoneLights)
            {
                if (sz != null) sz.ApplyInstant(phase);
            }

            if (FeaturesRendering.Vision.VisionManager.Instance != null)
            {
                FeaturesRendering.Vision.VisionManager.Instance.SetNightState(phase == TimeManager.DayPhase.Night, instant: true);
            }

            if (FeaturesTime.UI.CombatPhaseTrackerUI.Instance != null)
            {
                FeaturesTime.UI.CombatPhaseTrackerUI.Instance.UpdatePhaseDisplay(phase);
            }

            // 11. Combat & Night Brawl
            FeaturesCombat.UI.EnemyHealthBarManager.Instance?.ReleaseAllBars();
            FeaturesCombat.UI.BossHealthBarManager.Instance?.ClearAllBosses();

            if (FeaturesCombat.NightBrawlManager.Instance != null)
            {
                FeaturesCombat.NightBrawlManager.Instance.RestoreNightBrawlState(
                    data.currentDay,
                    data.nightCurrentWave,
                    data.nightTotalWaves,
                    data.isNightEncounterCleared,
                    data.isNightBrawlActive,
                    data.activeEnemies,
                    data.nightBrawlPendingWaveEnemies
                );
            }
            else
            {
                var existingEnemies = FindObjectsByType<FeaturesCombat.EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var enemy in existingEnemies)
                {
                    if (enemy != null) Destroy(enemy.gameObject);
                }
            }

            // 12. Farmland Crops & Capacity
            if (FeaturesFarming.FarmlandExpansionManager.Instance != null && data.unlockedFarmlandPlots >= 6)
            {
                FeaturesFarming.FarmlandExpansionManager.Instance.SetUnlockedPlotCount(data.unlockedFarmlandPlots);
            }

            var tiles = FindObjectsByType<FarmlandTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (tiles != null && data.farmlandTiles != null)
            {
                var tileLookup = new Dictionary<string, FarmlandTile>();
                foreach (var t in tiles)
                {
                    if (t != null && !tileLookup.ContainsKey(t.gameObject.name))
                        tileLookup.Add(t.gameObject.name, t);
                }

                foreach (var savedTile in data.farmlandTiles)
                {
                    if (tileLookup.TryGetValue(savedTile.tileName, out var tile))
                    {
                        SeedItemData seed = null;
                        if (!string.IsNullOrEmpty(savedTile.seedItemId))
                        {
                            var resolved = ResolveItem(savedTile.seedItemId);
                            seed = resolved as SeedItemData;
                        }

                        tile.RestoreCropState(
                            (TileState)savedTile.tileState,
                            seed,
                            savedTile.growthProgress,
                            savedTile.currentTimer,
                            savedTile.growthDuration
                        );
                    }
                }
            }

            // 13. Bed Interactables
            var beds = FindObjectsByType<FeaturesInteraction.BedInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in beds)
            {
                b?.UpdateLabel();
            }

            // 14. Wardrobe Outfit & Hat
            if (PlayerOutfit.Instance != null)
            {
                if (!string.IsNullOrEmpty(data.outfitName))
                {
                    var outfit = Resources.Load<OutfitData>("Player/model/" + data.outfitName);
                    if (outfit != null)
                    {
                        PlayerOutfit.Instance.EquipOutfit(outfit);
                    }
                }
                PlayerOutfit.Instance.SetHatActive(data.isHatEquipped);
            }

            // 15. Daily Economy Market Prices & Stats
            if (DailyEconomyManager.Instance != null && data.sweetPotatoPrice > 0)
            {
                DailyEconomyManager.Instance.RestoreEconomyState(
                    data.sweetPotatoPrice,
                    data.taroPrice,
                    data.cornPrice,
                    data.dailyCropsHarvested,
                    data.dailyCropsSold,
                    data.dailyGoldEarnedTrading,
                    data.dailyMonstersSlain,
                    data.dailyGoldEarnedCombat
                );
            }

            // 16. Storyline Progression
            var loadedStoryService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.IStorylineService>();
            if (loadedStoryService != null && data.triggeredStoryBeats != null)
            {
                loadedStoryService.RestoreState(
                    data.storyChapter,
                    data.triggeredStoryBeats,
                    data.unlockedJournalEntries,
                    data.isStoryComplete,
                    data.narrativeFacts,
                    data.lastPlayedBeatKeys,
                    data.lastPlayedDays,
                    data.pendingModalBeatIds
                );
            }

            // 17. Quest Progress Persistence
            var loadedQuestService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.INarrativeObjectiveService>();
            if (loadedQuestService != null && data.questPayload != null)
            {
                loadedQuestService.ImportSavePayload(data.questPayload);
            }
        }

        private void CaptureContainerInventories(GameSaveData data)
        {
            var fridge = FindFirstObjectByType<RefrigeratorInteractable>(FindObjectsInactive.Include);
            if (fridge != null)
            {
                var inv = fridge.GetComponent<InventoryComponent>();
                if (inv != null && inv.slots != null)
                {
                    var saved = new SavedContainerInventory { containerId = "Refrigerator" };
                    for (int i = 0; i < inv.slots.Count; i++)
                    {
                        var s = inv.slots[i];
                        if (s != null && !s.IsEmpty && s.item != null)
                        {
                            string id = !string.IsNullOrEmpty(s.item.itemId) ? s.item.itemId : s.item.name;
                            saved.slots.Add(new SavedInventorySlot { slotIndex = i, itemId = id, quantity = s.quantity });
                        }
                    }
                    data.containerInventories.Add(saved);
                }
            }

            var sink = FindFirstObjectByType<KitchenSinkInteractable>(FindObjectsInactive.Include);
            if (sink != null)
            {
                var inv = sink.GetComponent<InventoryComponent>();
                if (inv != null && inv.slots != null)
                {
                    var saved = new SavedContainerInventory { containerId = "KitchenSink" };
                    for (int i = 0; i < inv.slots.Count; i++)
                    {
                        var s = inv.slots[i];
                        if (s != null && !s.IsEmpty && s.item != null)
                        {
                            string id = !string.IsNullOrEmpty(s.item.itemId) ? s.item.itemId : s.item.name;
                            saved.slots.Add(new SavedInventorySlot { slotIndex = i, itemId = id, quantity = s.quantity });
                        }
                    }
                    data.containerInventories.Add(saved);
                }
            }

            if (TrophySystemManager.Instance != null)
            {
                var cabInv = TrophySystemManager.Instance.CabinetInventory;
                if (cabInv != null && cabInv.slots != null)
                {
                    var saved = new SavedContainerInventory { containerId = "TrophyCabinet" };
                    for (int i = 0; i < cabInv.slots.Count; i++)
                    {
                        var s = cabInv.slots[i];
                        if (s != null && !s.IsEmpty && s.item != null)
                        {
                            string id = !string.IsNullOrEmpty(s.item.itemId) ? s.item.itemId : s.item.name;
                            saved.slots.Add(new SavedInventorySlot { slotIndex = i, itemId = id, quantity = s.quantity });
                        }
                    }
                    data.containerInventories.Add(saved);
                }

                var rackInv = TrophySystemManager.Instance.RackInventory;
                if (rackInv != null && rackInv.slots != null)
                {
                    var saved = new SavedContainerInventory { containerId = "TrophyRack" };
                    for (int i = 0; i < rackInv.slots.Count; i++)
                    {
                        var s = rackInv.slots[i];
                        if (s != null && !s.IsEmpty && s.item != null)
                        {
                            string id = !string.IsNullOrEmpty(s.item.itemId) ? s.item.itemId : s.item.name;
                            saved.slots.Add(new SavedInventorySlot { slotIndex = i, itemId = id, quantity = s.quantity });
                        }
                    }
                    data.containerInventories.Add(saved);
                }
            }
        }

        private void RestoreContainerInventories(GameSaveData data)
        {
            if (data.containerInventories == null) return;

            foreach (var container in data.containerInventories)
            {
                if (container.containerId == "Refrigerator")
                {
                    var fridge = FindFirstObjectByType<RefrigeratorInteractable>(FindObjectsInactive.Include);
                    if (fridge != null)
                    {
                        var inv = fridge.GetComponent<InventoryComponent>();
                        if (inv != null && inv.slots != null)
                        {
                            for (int i = 0; i < inv.slots.Count; i++)
                            {
                                if (inv.slots[i] != null) { inv.slots[i].item = null; inv.slots[i].quantity = 0; }
                            }
                            foreach (var s in container.slots)
                            {
                                if (s.slotIndex >= 0 && s.slotIndex < inv.slots.Count)
                                {
                                    var item = ResolveItem(s.itemId);
                                    if (item != null)
                                    {
                                        inv.slots[s.slotIndex].item = item;
                                        inv.slots[s.slotIndex].quantity = s.quantity;
                                    }
                                }
                            }
                            inv.OnInventoryChanged?.Invoke();
                        }
                    }
                }
                else if (container.containerId == "KitchenSink")
                {
                    var sink = FindFirstObjectByType<KitchenSinkInteractable>(FindObjectsInactive.Include);
                    if (sink != null)
                    {
                        var inv = sink.GetComponent<InventoryComponent>();
                        if (inv != null && inv.slots != null)
                        {
                            for (int i = 0; i < inv.slots.Count; i++)
                            {
                                if (inv.slots[i] != null) { inv.slots[i].item = null; inv.slots[i].quantity = 0; }
                            }
                            foreach (var s in container.slots)
                            {
                                if (s.slotIndex >= 0 && s.slotIndex < inv.slots.Count)
                                {
                                    var item = ResolveItem(s.itemId);
                                    if (item != null)
                                    {
                                        inv.slots[s.slotIndex].item = item;
                                        inv.slots[s.slotIndex].quantity = s.quantity;
                                    }
                                }
                            }
                            inv.OnInventoryChanged?.Invoke();
                        }
                    }
                }
                else if (container.containerId == "TrophyCabinet" && TrophySystemManager.Instance != null)
                {
                    var inv = TrophySystemManager.Instance.CabinetInventory;
                    if (inv != null && inv.slots != null)
                    {
                        for (int i = 0; i < inv.slots.Count; i++)
                        {
                            if (inv.slots[i] != null) { inv.slots[i].item = null; inv.slots[i].quantity = 0; }
                        }
                        foreach (var s in container.slots)
                        {
                            if (s.slotIndex >= 0 && s.slotIndex < inv.slots.Count)
                            {
                                var item = ResolveItem(s.itemId);
                                if (item != null)
                                {
                                    inv.slots[s.slotIndex].item = item;
                                    inv.slots[s.slotIndex].quantity = s.quantity;
                                }
                            }
                        }
                        inv.OnInventoryChanged?.Invoke();
                    }
                }
                else if (container.containerId == "TrophyRack" && TrophySystemManager.Instance != null)
                {
                    var inv = TrophySystemManager.Instance.RackInventory;
                    if (inv != null && inv.slots != null)
                    {
                        for (int i = 0; i < inv.slots.Count; i++)
                        {
                            if (inv.slots[i] != null) { inv.slots[i].item = null; inv.slots[i].quantity = 0; }
                        }
                        foreach (var s in container.slots)
                        {
                            if (s.slotIndex >= 0 && s.slotIndex < inv.slots.Count)
                            {
                                var item = ResolveItem(s.itemId);
                                if (item != null)
                                {
                                    inv.slots[s.slotIndex].item = item;
                                    inv.slots[s.slotIndex].quantity = s.quantity;
                                }
                            }
                        }
                        inv.OnInventoryChanged?.Invoke();
                    }
                }
            }
        }

        private string GetCurrentPlayerLocationName()
        {
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                var zone = player.GetComponentInChildren<InteractionZone>();
                if (zone == null)
                {
                    var zones = FindObjectsByType<InteractionZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var z in zones)
                    {
                        if (z.ContainsPoint(player.transform.position))
                            return z.gameObject.name.Replace("Zone", "");
                    }
                }
                else
                {
                    return zone.gameObject.name.Replace("Zone", "");
                }
            }
            return "Bedroom";
        }

        private ItemData ResolveItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
            if (db != null)
            {
                var item = db.GetItem(itemId);
                if (item != null) return item;

                var allItems = db.GetAllItems();
                if (allItems != null)
                {
                    string cleanTarget = itemId.Replace("_", "").Replace(" ", "");
                    foreach (var it in allItems)
                    {
                        if (it == null) continue;
                        if (string.Equals(it.itemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(it.name, itemId, StringComparison.OrdinalIgnoreCase))
                        {
                            return it;
                        }
                        if (string.Equals((it.itemId ?? "").Replace("_", "").Replace(" ", ""), cleanTarget, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(it.name.Replace("_", "").Replace(" ", ""), cleanTarget, StringComparison.OrdinalIgnoreCase))
                        {
                            return it;
                        }
                    }
                }
            }

            return null;
        }

        private void ShowNotification(string msg, Color col)
        {
            var fct = ServiceLocator.Resolve<IFloatingTextService>();
            if (fct != null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                Vector3 pos = player != null ? player.transform.position + Vector3.up * 1.8f : Vector3.zero;
                fct.SpawnText(pos, msg, col);
            }
        }

        #endregion
    }
}
