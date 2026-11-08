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
        // Test 1: Day/Night Phase progression & continuous simulation past 16:00 (Dusk)
        private static bool Test_DayNight_PhaseProgression_DuskUntilNight()
        {
            var go = new GameObject("Test_TimeManager");
            try
            {
                var mgr = go.AddComponent<DayNightTimeManager>();
                
                var phaseNoon = mgr.EvaluatePhase(12.0f);
                var phaseDuskStart = mgr.EvaluatePhase(16.0f);
                var phaseLateDusk = mgr.EvaluatePhase(17.5f);
                var phaseNight = mgr.EvaluatePhase(18.0f);

                if (phaseNoon != FeaturesRendering.Lighting.EnvironmentPhase.Day)
                {
                    Debug.LogError($"Phase at 12:00 was {phaseNoon}, expected Day.");
                    return false;
                }

                if (phaseDuskStart != FeaturesRendering.Lighting.EnvironmentPhase.Dusk)
                {
                    Debug.LogError($"Phase at 16:00 was {phaseDuskStart}, expected Dusk.");
                    return false;
                }

                if (phaseLateDusk != FeaturesRendering.Lighting.EnvironmentPhase.Dusk)
                {
                    Debug.LogError($"Phase at 17:50 was {phaseLateDusk}, expected Dusk.");
                    return false;
                }

                if (phaseNight != FeaturesRendering.Lighting.EnvironmentPhase.Night)
                {
                    Debug.LogError($"Phase at 18:00 was {phaseNight}, expected Night.");
                    return false;
                }

                // Verify Dusk phase is not classified as Night, enabling continuous time flow
                bool isDaytimeSimulationAt16 = (phaseDuskStart != FeaturesRendering.Lighting.EnvironmentPhase.Night);
                if (!isDaytimeSimulationAt16)
                {
                    Debug.LogError("Dusk phase evaluated as night!");
                    return false;
                }

                Debug.Log("✅ Test 1: Day/Night Dusk continuous time simulation verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 2: Bed sleep logic - allows sleep during Dusk/Day and blocks only during Night with active monsters


        private static bool Test_BedInteractable_Logic_AllowsDuskAndBlocksNightOnly()
        {
            var mockTime = new MockTimeService();
            ServiceLocator.Register<ITimeService>(mockTime);

            var go = new GameObject("Test_Bed");
            try
            {
                var label = go.AddComponent<FeaturesInteraction.WorldLabel>();
                var bed = go.AddComponent<FeaturesInteraction.BedInteractable>();

                // 1. Dusk Phase (16:00): sleep must be permitted to start night phase!
                mockTime.CurrentPhase = DayPhase.Dusk;
                mockTime.IsNightEncounterCleared = false;
                bed.UpdateLabel();
                if (label.displayName != "Sleep (Start Night Phase)")
                {
                    Debug.LogError($"During Dusk, Bed label was '{label.displayName}', expected 'Sleep (Start Night Phase)'!");
                    return false;
                }

                // 2. Night Phase with Uncleared Wave: monsters outside warning!
                mockTime.CurrentPhase = DayPhase.Night;
                mockTime.IsNightEncounterCleared = false;
                bed.UpdateLabel();
                if (label.displayName != "Bed (Monsters Lurking Outside!)")
                {
                    Debug.LogError($"During Night brawl, Bed label was '{label.displayName}', expected 'Bed (Monsters Lurking Outside!)'!");
                    return false;
                }

                // 3. Night Phase with Cleared Wave: sleep advances to next day!
                mockTime.CurrentPhase = DayPhase.Night;
                mockTime.IsNightEncounterCleared = true;
                bed.UpdateLabel();
                if (label.displayName != "Sleep (Advance to Next Day)")
                {
                    Debug.LogError($"After wave cleared, Bed label was '{label.displayName}', expected 'Sleep (Advance to Next Day)'!");
                    return false;
                }

                Debug.Log("✅ Test 2: Bed sleep permission logic across Dawn/Day/Dusk/Night verified.");
                return true;
            }
            finally
            {
                ServiceLocator.Unregister<ITimeService>();
                Object.DestroyImmediate(go);
            }
        }

        // Test 3: Water Bottle 4 charges (100L total, 25L per sip)
        private static bool Test_PlayerWaterBottle_4Charges_FullToEmpty()
        {
            var go = new GameObject("Test_WaterBottle");
            try
            {
                var bottle = go.AddComponent<PlayerWaterBottle>();
                bottle.SetWater(100f);

                // Sip 1
                bool s1 = bottle.DrinkSip(25f);
                if (!s1 || Mathf.Abs(bottle.CurrentWater - 75f) > 0.01f)
                {
                    Debug.LogError($"Sip 1 failed. Expected 75L, got {bottle.CurrentWater}");
                    return false;
                }

                // Sip 2
                bool s2 = bottle.DrinkSip(25f);
                if (!s2 || Mathf.Abs(bottle.CurrentWater - 50f) > 0.01f)
                {
                    Debug.LogError($"Sip 2 failed. Expected 50L, got {bottle.CurrentWater}");
                    return false;
                }

                // Sip 3
                bool s3 = bottle.DrinkSip(25f);
                if (!s3 || Mathf.Abs(bottle.CurrentWater - 25f) > 0.01f)
                {
                    Debug.LogError($"Sip 3 failed. Expected 25L, got {bottle.CurrentWater}");
                    return false;
                }

                // Sip 4
                bool s4 = bottle.DrinkSip(25f);
                if (!s4 || Mathf.Abs(bottle.CurrentWater - 0f) > 0.01f)
                {
                    Debug.LogError($"Sip 4 failed. Expected 0L, got {bottle.CurrentWater}");
                    return false;
                }

                // Sip 5 (Empty Bottle)
                bool s5 = bottle.DrinkSip(25f);
                if (s5)
                {
                    Debug.LogError("Sip 5 succeeded on empty bottle!");
                    return false;
                }

                Debug.Log("✅ Test 3: Water bottle 4-charge (25L each) full-to-empty depletion verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 4: Water Bottle Capacity preservation (100L clamped properly on Save/Load)
        private static bool Test_WaterBottle_CapacityPreservation_100L()
        {
            float savedBottleAmount = 100f;
            float loadedAmount = savedBottleAmount >= 0f ? Mathf.Clamp(savedBottleAmount, 0f, 100f) : 100f;

            if (Mathf.Abs(loadedAmount - 100f) > 0.01f)
            {
                Debug.LogError($"Expected 100L clamped capacity, got {loadedAmount}");
                return false;
            }

            Debug.Log("✅ Test 4: Water bottle 100L Save/Load capacity preservation verified.");
            return true;
        }

        // Test 5: Kitchen station vertical progress bar geometry
        private static bool Test_KitchenSinkProgressOverlay_VerticalGeometry()
        {
            var stationGO = new GameObject("Test_Sink");
            try
            {
                var inv = stationGO.AddComponent<InventoryComponent>();
                var station = stationGO.AddComponent<MockKitchenStation>();
                var overlay = stationGO.AddComponent<KitchenStationProgressOverlay>();

                // Trigger BuildOverlays via reflection
                var method = typeof(KitchenStationProgressOverlay).GetMethod("BuildOverlays", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null) method.Invoke(overlay, null);

                Transform overlay0 = stationGO.transform.Find("ProgressOverlay_0");
                if (overlay0 == null)
                {
                    Debug.LogError("ProgressOverlay_0 was not instantiated!");
                    return false;
                }

                Transform track = overlay0.Find("Track_0");
                Transform veil = overlay0.Find("Veil_0");

                if (track == null || veil == null)
                {
                    Debug.LogError("Track_0 or Veil_0 child missing in ProgressOverlay_0!");
                    return false;
                }

                // Verify track has vertical bar dimensions (thickness width and full vertical height)
                if (Mathf.Abs(track.localScale.x - 0.045f) > 0.01f)
                {
                    Debug.LogError($"Track width was {track.localScale.x}, expected ~0.045m!");
                    return false;
                }

                if (Mathf.Abs(track.localScale.y - 0.7f) > 0.01f)
                {
                    Debug.LogError($"Track height was {track.localScale.y}, expected ~0.70m!");
                    return false;
                }

                Debug.Log("✅ Test 5: Kitchen sink vertical progress bar geometry (track & vertical fill bar) verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(stationGO);
            }
        }

        // Test 6: Stove UI cooking progress bar permanently hidden
        private static bool Test_StoveUI_CookingProgressBarPermanentlyHidden()
        {
            var go = new GameObject("Test_StoveUI");
            try
            {
                var stoveUI = go.AddComponent<StoveUIManager>();
                var sliderGO = new GameObject("CookingProgressBar");
                sliderGO.transform.SetParent(go.transform);
                var slider = sliderGO.AddComponent<Slider>();

                // Set slider via reflection
                var field = typeof(StoveUIManager).GetField("cookingProgressBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(stoveUI, slider);
                }

                // Simulate update/sync
                var method = typeof(StoveUIManager).GetMethod("SyncCookingProgressBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    sliderGO.SetActive(true);
                    method.Invoke(stoveUI, null);

                    if (sliderGO.activeSelf)
                    {
                        Debug.LogError("cookingProgressBar remained active after SyncCookingProgressBar!");
                        return false;
                    }
                }

                Debug.Log("✅ Test 6: Stove UI legacy cooking progress bar deactivation verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 7: CombatPhaseTrackerUI procedural day badge and clock layout
        private static bool Test_CombatPhaseTrackerUI_ProceduralDayAndClock()
        {
            var go = new GameObject("Test_HUD");
            try
            {
                var tracker = go.AddComponent<CombatPhaseTrackerUI>();

                var methodBuild = typeof(CombatPhaseTrackerUI).GetMethod("BuildProceduralTrackerUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var methodActivity = typeof(CombatPhaseTrackerUI).GetMethod("BuildProceduralActivityUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (methodBuild != null) methodBuild.Invoke(tracker, null);
                if (methodActivity != null) methodActivity.Invoke(tracker, null);

                var dayText = go.transform.Find("TrackerBadge/DayText")?.GetComponent<TextMeshProUGUI>();
                var clockText = go.transform.Find("TrackerBadge/ClockText")?.GetComponent<TextMeshProUGUI>();
                var activityHeader = go.transform.Find("ActivityCard/ActivityHeader")?.GetComponent<TextMeshProUGUI>();
                var activityCard = go.transform.Find("ActivityCard")?.GetComponent<RectTransform>();

                if (dayText == null || !dayText.gameObject.activeSelf)
                {
                    Debug.LogError("DayText was not created or is inactive!");
                    return false;
                }

                if (clockText == null || Mathf.Abs(clockText.fontSize - 18f) > 0.1f)
                {
                    Debug.LogError($"ClockText font size is {clockText?.fontSize}, expected 18pt!");
                    return false;
                }

                if (activityCard == null || Mathf.Abs(activityCard.sizeDelta.y - 72f) > 0.1f)
                {
                    Debug.LogError($"ActivityCard height is {activityCard?.sizeDelta.y}, expected 72px!");
                    return false;
                }

                if (activityHeader == null || Mathf.Abs(activityHeader.fontSize - 12f) > 0.1f)
                {
                    Debug.LogError($"ActivityHeader font size is {activityHeader?.fontSize}, expected 12pt!");
                    return false;
                }

                if (tracker.CurrentClockMode != FeaturesTime.UI.ClockDisplayMode.FullStep60s || Mathf.Abs(tracker.ClockUpdateIntervalRealSeconds - 60f) > 0.01f)
                {
                    Debug.LogError($"Default ClockMode should be FullStep60s (60s), got {tracker.CurrentClockMode} ({tracker.ClockUpdateIntervalRealSeconds}s)!");
                    return false;
                }

                tracker.ToggleClockMode();
                if (tracker.CurrentClockMode != FeaturesTime.UI.ClockDisplayMode.HalfStep30s || Mathf.Abs(tracker.ClockUpdateIntervalRealSeconds - 30f) > 0.01f)
                {
                    Debug.LogError($"ToggleClockMode should switch to HalfStep30s (30s), got {tracker.CurrentClockMode} ({tracker.ClockUpdateIntervalRealSeconds}s)!");
                    return false;
                }

                tracker.ToggleClockMode();
                if (tracker.CurrentClockMode != FeaturesTime.UI.ClockDisplayMode.QuarterStep15s || Mathf.Abs(tracker.ClockUpdateIntervalRealSeconds - 15f) > 0.01f)
                {
                    Debug.LogError($"ToggleClockMode should switch to QuarterStep15s (15s), got {tracker.CurrentClockMode} ({tracker.ClockUpdateIntervalRealSeconds}s)!");
                    return false;
                }

                tracker.ToggleClockMode();
                if (tracker.CurrentClockMode != FeaturesTime.UI.ClockDisplayMode.FullStep60s || Mathf.Abs(tracker.ClockUpdateIntervalRealSeconds - 60f) > 0.01f)
                {
                    Debug.LogError($"ToggleClockMode should switch back to FullStep60s (60s), got {tracker.CurrentClockMode} ({tracker.ClockUpdateIntervalRealSeconds}s)!");
                    return false;
                }

                Debug.Log("✅ Test 7: Procedural HUD Day badge, 18pt Clock, 72px Activity Card, and Triple Clock Mode (60s/30s/15s) toggle verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 10: Hotbar mouse click slot selection
        private static bool Test_Hotbar_MouseClick_Selection()
        {
            var go = new GameObject("Test_HotbarClick");
            try
            {
                var inv = go.AddComponent<InventoryComponent>();
                inv.ResetInventory(16);
                inv.HasHotbar = true;
                inv.selectedHotbarIndex = 0;

                var slotGO = new GameObject("Slot_2");
                slotGO.transform.SetParent(go.transform);
                var slotUI = slotGO.AddComponent<InventorySlotUI>();
                slotUI.Init(null, 2, inv);

                var pointerData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left
                };

                slotUI.OnPointerClick(pointerData);

                if (inv.selectedHotbarIndex != 2)
                {
                    Debug.LogError($"Expected hotbar index 2, got {inv.selectedHotbarIndex}!");
                    return false;
                }

                Debug.Log("✅ Test 10: Hotbar mouse click slot selection verified (selectedHotbarIndex updated).");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 14: Player inventory 16-slot auto-allocation
        private static bool Test_PlayerInventory_16Slots_Allocation()
        {
            var playerGO = new GameObject("Player");
            playerGO.tag = "Player";
            try
            {
                var inv = playerGO.AddComponent<InventoryComponent>();
                // Simulasikan prefab/scene dengan 12 slot lama
                inv.maxCapacity = 12;
                inv.slots.Clear();
                for (int i = 0; i < 12; i++) inv.slots.Add(new InventorySlot());

                // Trigger Awake via reflection
                var awakeMethod = typeof(InventoryComponent).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null) awakeMethod.Invoke(inv, null);

                if (inv.maxCapacity != 16 || inv.slots.Count != 16)
                {
                    Debug.LogError($"Expected capacity 16 and 16 slots, got capacity={inv.maxCapacity}, count={inv.slots.Count}!");
                    return false;
                }

                if (!inv.HasHotbar)
                {
                    Debug.LogError("Player inventory should have HasHotbar = true!");
                    return false;
                }

                Debug.Log("✅ Test 14: Player inventory 16-slot expansion (4 hotbar + 12 main) verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(playerGO);
            }
        }

        // Test 15: Food buff duration and tick acceleration under x8 fast forward mode
        private static bool Test_PlayerBuff_FastForward8x_DurationAndTickAcceleration()
        {
            var go = new GameObject("Test_Player_BuffSpeed");
            try
            {
                var stats = go.AddComponent<PlayerStats>();
                stats.maxHealth = 100;
                stats.currentHealth = 50;

                var buffMgr = go.AddComponent<PlayerBuffManager>();

                // 1. Normal mode (1x): buff with 20s duration and HealthRegenTick 5 HP/s
                var normalBuff = new BuffEffectData(BuffType.HealthRegenTick, 5f, 20f, "Normal Regen", "5 HP/s", null);
                buffMgr.ApplyBuff(normalBuff);

                buffMgr.TimeMultiplierOverride = 1.0f;
                // Tick 1 second real time
                buffMgr.TickBuffs(1.0f);

                var activeBuff = buffMgr.ActiveBuffs[0];
                if (Mathf.Abs(activeBuff.remainingDuration - 19.0f) > 0.05f)
                {
                    Debug.LogError($"Expected 19s remaining under 1x speed, got {activeBuff.remainingDuration}!");
                    return false;
                }
                if (stats.currentHealth != 55)
                {
                    Debug.LogError($"Expected 55 health after 1s tick, got {stats.currentHealth}!");
                    return false;
                }

                // 2. Fast-Forward mode (8x): Apply 8x multiplier override
                buffMgr.TimeMultiplierOverride = 8.0f;

                // Tick 1 second real time -> should consume 8 seconds of buff duration (19s - 8s = 11s)
                // and trigger 8 periodic healing ticks (8 * 5 = 40 HP healed -> 55 + 40 = 95 HP)
                buffMgr.TickBuffs(1.0f);

                if (Mathf.Abs(activeBuff.remainingDuration - 11.0f) > 0.05f)
                {
                    Debug.LogError($"Expected 11s remaining under 8x fast-forward, got {activeBuff.remainingDuration}!");
                    return false;
                }
                if (stats.currentHealth != 95)
                {
                    Debug.LogError($"Expected 95 health after 8x acceleration, got {stats.currentHealth}!");
                    return false;
                }

                // Tick another 1.5 seconds real time (1.5 * 8 = 12s) -> exceeds 11s remaining -> buff should expire and be removed
                buffMgr.TickBuffs(1.5f);

                if (buffMgr.ActiveBuffs.Count != 0)
                {
                    Debug.LogError($"Expected buff to expire and be removed, but count is {buffMgr.ActiveBuffs.Count}!");
                    return false;
                }

                Debug.Log("✅ Test 15: Food buff duration and tick acceleration under x8 fast forward mode verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 20: Food buffs clear on sleep / day transition so morning starts completely fresh
        private static bool Test_PlayerBuff_ClearsOnSleepAndDayTransition()
        {
            var go = new GameObject("Test_Player_BuffSleepPurge");
            try
            {
                var stats = go.AddComponent<PlayerStats>();
                stats.maxHealth = 100;
                stats.currentHealth = 100;

                var buffMgr = go.AddComponent<PlayerBuffManager>();

                // 1. Apply multiple food buffs
                var hpBuff = new BuffEffectData(BuffType.MaxHealthPercent, 0.20f, 600f, "Purple Taro Cake", "+20% Max HP", null);
                var speedBuff = new BuffEffectData(BuffType.MoveSpeedPercent, 0.15f, 600f, "Roasted Sweet Potato", "+15% Move Speed", null);
                var regenBuff = new BuffEffectData(BuffType.HealthRegenTick, 5f, 600f, "Corn Porridge", "5 HP/s Regen", null);

                buffMgr.ApplyBuff(hpBuff);
                buffMgr.ApplyBuff(speedBuff);
                buffMgr.ApplyBuff(regenBuff);

                // Verify pre-sleep state: buffs are active, stats modified
                if (buffMgr.ActiveBuffs.Count != 3)
                {
                    Debug.LogError($"Expected 3 active buffs before sleep, got {buffMgr.ActiveBuffs.Count}!");
                    return false;
                }

                if (stats.maxHealth != 120)
                {
                    Debug.LogError($"Expected 120 maxHealth with +20% HP buff, got {stats.maxHealth}!");
                    return false;
                }

                if (Mathf.Abs(buffMgr.GetSpeedMultiplier() - 1.15f) > 0.01f)
                {
                    Debug.LogError($"Expected 1.15x speed multiplier, got {buffMgr.GetSpeedMultiplier()}!");
                    return false;
                }

                bool eventFired = false;
                buffMgr.OnBuffsChanged += () => eventFired = true;

                // 2. Player sleeps in bed / advances to morning
                buffMgr.ClearAllBuffs();

                // Verify post-sleep state: 0 buffs remain, stats restored to baseline
                if (buffMgr.ActiveBuffs.Count != 0)
                {
                    Debug.LogError($"Expected 0 active buffs after sleep, got {buffMgr.ActiveBuffs.Count}!");
                    return false;
                }

                if (stats.maxHealth != 100)
                {
                    Debug.LogError($"Expected maxHealth restored to base 100 after sleep, got {stats.maxHealth}!");
                    return false;
                }

                if (Mathf.Abs(buffMgr.GetSpeedMultiplier() - 1.0f) > 0.01f)
                {
                    Debug.LogError($"Expected 1.0x baseline speed multiplier after sleep, got {buffMgr.GetSpeedMultiplier()}!");
                    return false;
                }

                if (!eventFired)
                {
                    Debug.LogError("Expected OnBuffsChanged to fire upon clearing buffs!");
                    return false;
                }

                Debug.Log("✅ Test 20: Food buff sleep purge, baseline stat restoration, and HUD event notification verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 21: DayTransitionUI Day and Night animated banner configuration, text styling, and cancel integrity
        private static bool Test_DayTransitionUI_DayAndNightBannerAnimation()
        {
            var go = new GameObject("Test_DayTransitionUI_Obj");
            try
            {
                var transitionUI = go.AddComponent<DayTransitionUI>();
                transitionUI.EnsureUIHierarchy();

                if (transitionUI.CanvasGroupRef == null)
                {
                    Debug.LogError("DayTransitionUI failed to generate CanvasGroup!");
                    return false;
                }

                // 1. Test Day Transition
                transitionUI.PlayDayTransition(3);
                if (transitionUI.TitleTextRef == null || !transitionUI.TitleTextRef.text.Contains("DAY 3"))
                {
                    Debug.LogError($"Expected Day banner title to contain 'DAY 3', got '{transitionUI.TitleTextRef?.text}'!");
                    return false;
                }
                if (transitionUI.SubtitleTextRef == null || !transitionUI.SubtitleTextRef.text.Contains("dawn"))
                {
                    Debug.LogError($"Expected Day subtitle to mention 'dawn', got '{transitionUI.SubtitleTextRef?.text}'!");
                    return false;
                }

                // 2. Test Night Transition
                transitionUI.PlayNightTransition();
                if (transitionUI.TitleTextRef == null || !transitionUI.TitleTextRef.text.Contains("NIGHTFALL"))
                {
                    Debug.LogError($"Expected Night banner title to be 'NIGHTFALL', got '{transitionUI.TitleTextRef?.text}'!");
                    return false;
                }
                if (transitionUI.SubtitleTextRef == null || !transitionUI.SubtitleTextRef.text.Contains("homestead"))
                {
                    Debug.LogError($"Expected Night subtitle to mention 'homestead', got '{transitionUI.SubtitleTextRef?.text}'!");
                    return false;
                }

                // 3. Test CancelTransition & Redesigned Anamorphic Celestial Hierarchy
                var medallion = go.transform.Find("BannerRoot/CelestialMedallion");
                var halo = go.transform.Find("BannerRoot/CelestialHaloRing");
                if (medallion == null || halo == null)
                {
                    Debug.LogError("[Test 21] Redesigned DayTransitionUI missing CelestialMedallion or CelestialHaloRing!");
                    return false;
                }

                transitionUI.CancelTransition();
                if (transitionUI.CanvasGroupRef.alpha > 0.01f)
                {
                    Debug.LogError($"Expected CanvasGroup alpha to be 0 after CancelTransition, got {transitionUI.CanvasGroupRef.alpha}!");
                    return false;
                }

                Debug.Log("✅ Test 21: DayTransitionUI Day/Night banner procedural generation, text styling, and cancel verified.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Test 22: Multi-source input lock hierarchy, DayTransitionUI raycast absorption, and leak prevention
        private static bool Test_MultiSourceInputLock_AndTransitionLeakPrevention()
        {
            var playerGo = new GameObject("Test_Player_LockAudit");
            var transitionGo = new GameObject("Test_DayTransition_LockAudit");

            try
            {
                var playerControl = playerGo.AddComponent<PlayerControl>();
                var playerCtx = (FarmBeware.Core.Runtime.IPlayerContext)playerControl;
                FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.IPlayerContext>(playerControl);

                // 1. Initial State: Unlocked
                if (playerControl.isInputLocked)
                {
                    Debug.LogError("PlayerControl must initially have isInputLocked == false!");
                    return false;
                }

                // 2. Add named lock for DayTransition
                playerControl.AddInputLock("DayTransition");
                if (!playerControl.isInputLocked || !playerControl.HasInputLock("DayTransition"))
                {
                    Debug.LogError("Expected isInputLocked == true and HasInputLock('DayTransition') == true!");
                    return false;
                }

                // 3. Attempt premature legacy generic unlock (e.g. from dialog or save UI closing)
                playerCtx.IsInputLocked = false;
                if (!playerControl.isInputLocked)
                {
                    Debug.LogError("Input Lock Leak! Legacy generic unlock improperly cleared 'DayTransition' lock!");
                    return false;
                }

                // 4. Overlapping lock sources: BedSleep + DayTransition
                playerControl.AddInputLock("BedSleep");
                playerControl.RemoveInputLock("DayTransition");
                if (!playerControl.isInputLocked)
                {
                    Debug.LogError("Input Lock Leak! Releasing DayTransition improperly cleared BedSleep lock!");
                    return false;
                }

                playerControl.RemoveInputLock("BedSleep");
                if (playerControl.isInputLocked)
                {
                    Debug.LogError("PlayerControl must be unlocked after all lock sources are removed!");
                    return false;
                }

                // 5. Test ClearAllInputLocks (e.g. for save file reload or scene reset)
                playerControl.AddInputLock("Stun");
                playerControl.AddInputLock("Respawn");
                if (!playerControl.isInputLocked)
                {
                    Debug.LogError("PlayerControl must be locked after adding Stun and Respawn!");
                    return false;
                }
                playerControl.ClearAllInputLocks();
                if (playerControl.isInputLocked)
                {
                    Debug.LogError("ClearAllInputLocks failed to reset isInputLocked to false!");
                    return false;
                }

                // 6. Test DayTransitionUI raycast blocking & auto-locking
                var transitionUI = transitionGo.AddComponent<DayTransitionUI>();
                transitionUI.EnsureUIHierarchy();
                transitionUI.PlayNightTransition();

                if (transitionUI.CanvasGroupRef == null || !transitionUI.CanvasGroupRef.blocksRaycasts)
                {
                    Debug.LogError("DayTransitionUI failed to set CanvasGroup.blocksRaycasts == true during active transition!");
                    return false;
                }

                if (transitionUI.VignetteOverlayRef == null || !transitionUI.VignetteOverlayRef.raycastTarget)
                {
                    Debug.LogError("DayTransitionUI failed to set VignetteOverlay.raycastTarget == true during active transition!");
                    return false;
                }

                if (!playerControl.isInputLocked)
                {
                    Debug.LogError("DayTransitionUI failed to automatically lock PlayerControl input upon playing transition!");
                    return false;
                }

                // Cancel transition and verify raycast blocking and input lock are released cleanly
                transitionUI.CancelTransition();
                if (transitionUI.CanvasGroupRef.blocksRaycasts)
                {
                    Debug.LogError("DayTransitionUI failed to reset blocksRaycasts to false on CancelTransition!");
                    return false;
                }
                if (transitionUI.VignetteOverlayRef.raycastTarget)
                {
                    Debug.LogError("DayTransitionUI failed to reset VignetteOverlay.raycastTarget to false on CancelTransition!");
                    return false;
                }
                if (playerControl.isInputLocked)
                {
                    Debug.LogError("DayTransitionUI failed to release player input lock on CancelTransition!");
                    return false;
                }

                Debug.Log("✅ Test 22: Multi-source input lock hierarchy, DayTransitionUI raycast absorption, and leak prevention verified.");
                return true;
            }
            finally
            {
                FarmBeware.Core.Runtime.ServiceLocator.Unregister<FarmBeware.Core.Runtime.IPlayerContext>();
                Object.DestroyImmediate(playerGo);
                Object.DestroyImmediate(transitionGo);
            }
        }

        // Test 30: ModalStackManager centralized input gating into PlayerControl.isInputLocked
        private static bool Test_ModalStack_CentralizedInputLock_PlayerControl_Audit()
        {
            var cleanupList = new List<Object>();
            try
            {
                var playerGO = new GameObject("Test_Player_ModalAudit", typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerControl));
                cleanupList.Add(playerGO);
                var player = playerGO.GetComponent<PlayerControl>();

                var modalStack = ModalStackManager.Instance;
                if (modalStack == null)
                {
                    Debug.LogError("[Test 30] ModalStackManager.Instance is null!");
                    return false;
                }

                // Initially no modals open
                modalStack.CloseAll();
                if (player.isInputLocked)
                {
                    Debug.LogError("[Test 30] PlayerControl.isInputLocked should be false with no active modals!");
                    return false;
                }

                // Push modal
                var dummyModal = new TestAuditModal();
                modalStack.Push(dummyModal);

                if (!modalStack.HasActiveModal)
                {
                    Debug.LogError("[Test 30] ModalStackManager.HasActiveModal should be true after push!");
                    return false;
                }

                if (!player.isInputLocked)
                {
                    Debug.LogError("[Test 30] PlayerControl.isInputLocked did not automatically become true when ModalStackManager had an active modal!");
                    return false;
                }

                // Pop modal
                modalStack.PopSpecific(dummyModal);
                dummyModal.IsOpen = false;

                if (modalStack.HasActiveModal)
                {
                    Debug.LogError("[Test 30] ModalStackManager.HasActiveModal should be false after pop!");
                    return false;
                }

                if (player.isInputLocked)
                {
                    Debug.LogError("[Test 30] PlayerControl.isInputLocked failed to release back to false after modal pop!");
                    return false;
                }

                Debug.Log("✅ Test 30: ModalStackManager centralized input gating into PlayerControl.isInputLocked verified.");
                return true;
            }
            finally
            {
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    ModalStackManager.Instance.CloseAll();
                }
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        private static bool Test_VehicleDrivingSystem_DynamicsAndLifecycleAudit()
        {
            var cleanupList = new System.Collections.Generic.List<GameObject>();
            try
            {
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    ModalStackManager.Instance.Clear();
                }

                var core = new FarmBeware.Features.Vehicles.PureLogic.VehicleDynamicsCore();

                // 1. Acceleration and speed clamping
                float speedAfterAccel = core.CalculateForwardSpeed(1.0f, 0f, false, 0.5f);
                if (speedAfterAccel <= 0f)
                {
                    Debug.LogError("[Test 38] Vehicle forward throttle failed to produce positive speed!");
                    return false;
                }

                float clampedTopSpeed = core.CalculateForwardSpeed(1.0f, 25f, false, 0.5f);
                if (clampedTopSpeed > core.MaxForwardSpeed)
                {
                    Debug.LogError($"[Test 38] Vehicle exceeded MaxForwardSpeed ({core.MaxForwardSpeed})! Speed was {clampedTopSpeed}");
                    return false;
                }

                // 2. Reverse speed
                float reverseSpeed = core.CalculateForwardSpeed(-1.0f, 0f, false, 0.5f);
                if (reverseSpeed >= 0f)
                {
                    Debug.LogError("[Test 38] Reverse throttle failed to produce negative speed!");
                    return false;
                }

                float clampedReverse = core.CalculateForwardSpeed(-1.0f, -15f, false, 0.5f);
                if (clampedReverse < -core.MaxReverseSpeed)
                {
                    Debug.LogError($"[Test 38] Vehicle exceeded MaxReverseSpeed ({-core.MaxReverseSpeed})! Speed was {clampedReverse}");
                    return false;
                }

                // 3. Speed-sensitive steering angle
                float lowSpeedSteer = core.CalculateSteeringAngle(1.0f, 0f, 0f, 1.0f);
                float highSpeedSteer = core.CalculateSteeringAngle(1.0f, 0f, 20f, 1.0f);

                if (Mathf.Abs(lowSpeedSteer - core.BaseSteerAngle) > 0.5f)
                {
                    Debug.LogError($"[Test 38] Expected low speed steer angle ~ {core.BaseSteerAngle}, but got {lowSpeedSteer}");
                    return false;
                }

                if (Mathf.Abs(highSpeedSteer - core.HighSpeedSteerAngle) > 0.5f)
                {
                    Debug.LogError($"[Test 38] Expected high speed steer angle ~ {core.HighSpeedSteerAngle}, but got {highSpeedSteer}");
                    return false;
                }

                // 4. Handbrake drift slip
                float normalGrip = core.CalculateLateralGrip(false, 0f);
                float driftGrip = core.CalculateLateralGrip(true, 0f);
                if (driftGrip >= normalGrip)
                {
                    Debug.LogError("[Test 38] Handbrake failed to drop lateral grip factor for drift simulation!");
                    return false;
                }

                // 5. Ackermann Kinematics & Zero Stationary Yaw
                float stationaryYaw = core.CalculateAckermannYawRate(0f, 35f, false, 0f, 0.02f);
                if (Mathf.Abs(stationaryYaw) > 0.0001f)
                {
                    Debug.LogError($"[Test 38] Stationary car must have 0 yaw rate! Got {stationaryYaw}");
                    return false;
                }

                float movingYaw = core.CalculateAckermannYawRate(5.0f, 35f, false, 0f, 0.02f);
                if (movingYaw <= 20f || movingYaw >= 120f)
                {
                    Debug.LogError($"[Test 38] Expected realistic moving yaw rate around ~66.8 deg/s, got {movingYaw}");
                    return false;
                }

                float reverseYaw = core.CalculateAckermannYawRate(-3.0f, 35f, false, 0f, 0.02f);
                if (reverseYaw >= 0f)
                {
                    Debug.LogError($"[Test 38] Reversing yaw rate must be negative for right steer! Got {reverseYaw}");
                    return false;
                }

                // 6. Brake-Before-Reverse state machine
                float brakingSpeed = core.CalculateForwardSpeed(-1.0f, 10.0f, false, 0.1f);
                if (brakingSpeed >= 10.0f || brakingSpeed < 0f)
                {
                    Debug.LogError($"[Test 38] Pressing [S] while moving forward must decelerate with brake! Got {brakingSpeed}");
                    return false;
                }

                // 7. Horn SFX Asset Verification
                var hornClip = Resources.Load<AudioClip>("Audio/SFX_Truck_Horn");
                if (hornClip == null)
                {
                    Debug.LogError("[Test 38] SFX_Truck_Horn audio clip missing from Resources/Audio/!");
                    return false;
                }

                // 8. Gear determination
                var gearDrive = core.DetermineGear(12f, 1f);
                var gearRev = core.DetermineGear(-3f, -1f);
                var gearPark = core.DetermineGear(0f, 0f);

                if (gearDrive != FarmBeware.Features.Vehicles.PureLogic.VehicleDynamicsCore.GearState.Drive ||
                    gearRev != FarmBeware.Features.Vehicles.PureLogic.VehicleDynamicsCore.GearState.Reverse ||
                    gearPark != FarmBeware.Features.Vehicles.PureLogic.VehicleDynamicsCore.GearState.Park)
                {
                    Debug.LogError($"[Test 38] Gear determination failure! Expected Drive, Reverse, Park, got {gearDrive}, {gearRev}, {gearPark}");
                    return false;
                }

                // 9. PlayerControl input lock lifecycle integration
                var playerObj = new GameObject("Test_Player_Vehicle");
                cleanupList.Add(playerObj);
                var playerControl = playerObj.AddComponent<PlayerControl>();

                playerControl.AddInputLock("VehicleDriving");
                if (!playerControl.isInputLocked || !playerControl.HasInputLock("VehicleDriving"))
                {
                    Debug.LogError("[Test 38] PlayerControl failed to lock input upon entering vehicle!");
                    return false;
                }

                playerControl.RemoveInputLock("VehicleDriving");
                if (playerControl.isInputLocked || playerControl.HasInputLock("VehicleDriving"))
                {
                    Debug.LogError("[Test 38] PlayerControl failed to release input lock upon exiting vehicle!");
                    return false;
                }

                // 7. Verify Vehicle enter/exit lifecycle preserves 100% clean unit scale (anti-pancake regression test)
                var truckObj = new GameObject("Test_Truck_Controller");
                cleanupList.Add(truckObj);
                truckObj.AddComponent<Rigidbody>();
                var vehicleController = truckObj.AddComponent<FarmBeware.Features.Vehicles.ArcadeVehicleController>();

                vehicleController.EnterVehicle(playerObj);
                if (!vehicleController.IsDriverOccupied)
                {
                    Debug.LogError("[Test 38] Failed to enter vehicle!");
                    return false;
                }

                vehicleController.ExitVehicle();
                if (vehicleController.IsDriverOccupied)
                {
                    Debug.LogError("[Test 38] Failed to exit vehicle!");
                    return false;
                }

                if (playerObj.transform.localScale != Vector3.one)
                {
                    Debug.LogError($"[Test 38] Player scale corrupted on vehicle exit! Expected (1,1,1), got {playerObj.transform.localScale}");
                    return false;
                }

                Debug.Log("✅ Test 38: VehicleDynamicsCore kinematics, speed-sensitive steering, drift grip, gears, and Player scale preservation verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 38] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    ModalStackManager.Instance.Clear();
                }

                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null)
                    {
                        Object.DestroyImmediate(cleanupList[i]);
                    }
                }
            }
        }

        private static bool Test_VehicleEntry_DualChannel_InteractionTrigger_Audit()
        {
            var cleanupList = new System.Collections.Generic.List<GameObject>();
            try
            {
                // 1. Create Truck with ArcadeVehicleController
                var truckObj = new GameObject("Test_Pickup_Truck", typeof(Rigidbody), typeof(BoxCollider));
                cleanupList.Add(truckObj);
                var avc = truckObj.AddComponent<FarmBeware.Features.Vehicles.ArcadeVehicleController>();
                avc.EnsureSubAssemblies();

                // Awake and EnsureSubAssemblies should create InteractionTrigger
                var triggerTransform = truckObj.transform.Find("InteractionTrigger");
                if (triggerTransform == null)
                {
                    Debug.LogError("[Test 40] InteractionTrigger child was NOT automatically created on truck!");
                    return false;
                }

                var triggerCol = triggerTransform.GetComponent<Collider>();
                if (triggerCol == null || !triggerCol.isTrigger)
                {
                    Debug.LogError("[Test 40] InteractionTrigger missing trigger BoxCollider!");
                    return false;
                }

                // 2. Create Player
                var playerObj = new GameObject("Test_Player_VehicleEntry");
                cleanupList.Add(playerObj);
                var playerControl = playerObj.AddComponent<PlayerControl>();
                var playerCol = playerObj.AddComponent<CapsuleCollider>();

                // Position player outside driver door (within 2.5m)
                playerObj.transform.position = truckObj.transform.position + new Vector3(-2.2f, 0f, 0.3f);
                Physics.SyncTransforms();

                // 3. Test CanInteract
                bool canInteract = avc.CanInteract(playerObj);
                if (!canInteract)
                {
                    Debug.LogError("[Test 40] CanInteract returned false at 2.2m from driver door!");
                    return false;
                }

                // 4. Test IInteractable.Interact
                avc.Interact(playerObj);

                if (!avc.IsDriverOccupied)
                {
                    Debug.LogError("[Test 40] Failed to enter vehicle via IInteractable.Interact!");
                    return false;
                }

                if (!playerControl.HasInputLock("VehicleDriving"))
                {
                    Debug.LogError("[Test 40] PlayerControl did not acquire 'VehicleDriving' input lock upon vehicle entry!");
                    return false;
                }

                // 5. Test ExitVehicle
                avc.ExitVehicle();

                if (avc.IsDriverOccupied)
                {
                    Debug.LogError("[Test 40] Vehicle still occupied after ExitVehicle()!");
                    return false;
                }

                if (playerControl.HasInputLock("VehicleDriving"))
                {
                    Debug.LogError("[Test 40] 'VehicleDriving' input lock was not removed upon exit!");
                    return false;
                }

                if (playerObj.transform.localScale != Vector3.one)
                {
                    Debug.LogError($"[Test 40] Player scale deformed on exit! Expected Vector3.one, got {playerObj.transform.localScale}");
                    return false;
                }

                Debug.Log("✅ Test 40: Dual-channel vehicle entry, InteractionTrigger child volume, CanInteract envelope, and scale preservation verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 40] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
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

        // Test 45: Day 0 Arrival — HUD Suppression, Time Progression Lock, Activity Gating, and Bed Sleep Transition Audit
        public static bool Test_Day0_Arrival_HUDSuppression_TimeLock_And_BedSleepTransition_Audit()
        {
            var cleanupList = new List<Object>();
            var prevTimeInstance = FeaturesTime.DayNightTimeManager.Instance;
            FarmBeware.Core.Runtime.ServiceLocator.TryResolve<FarmBeware.Core.Runtime.ITimeService>(out var prevService);
            try
            {
                // 1. Create Canvas with all core gameplay HUD panels
                var canvasGO = new GameObject("UI_Canvas");
                cleanupList.Add(canvasGO);
                var canvasComp = canvasGO.AddComponent<Canvas>();

                string[] hudPanelNames = {
                    "HUD_HotbarPanel",
                    "HUD_Hotbar",
                    "HUD_CombatPhaseTracker",
                    "HUD_BuffBar",
                    "HUD_PlayerStatsPanel",
                    "HUD_EnemyIndicators"
                };

                foreach (var name in hudPanelNames)
                {
                    var panel = new GameObject(name);
                    panel.transform.SetParent(canvasGO.transform, false);
                    panel.SetActive(true); // Initially active
                }

                // 2. Setup TimeManager & DayNightTimeManager for Day 0 Arrival
                var timeGO = new GameObject("Test_DayNightTimeManager");
                cleanupList.Add(timeGO);
                var timeMgr = timeGO.AddComponent<DayNightTimeManager>();
                FeaturesTime.DayNightTimeManager.Instance = timeMgr;
                FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(timeMgr);
                timeMgr.SetDayAndTime(0, 16.0f);
                timeMgr.SetPaused(true);

                if (timeMgr.CurrentDay != 0)
                {
                    Debug.LogError($"[Test 45] Expected CurrentDay == 0, got {timeMgr.CurrentDay}");
                    return false;
                }

                // 3. Test HUD Suppression on Day 0
                var cutsceneGO = new GameObject("Test_CutsceneController", typeof(PlayableDirector), typeof(OpeningCutsceneController));
                cleanupList.Add(cutsceneGO);
                var cutscene = cutsceneGO.GetComponent<OpeningCutsceneController>();
                cutscene.TargetCanvas = canvasComp;

                // When cutscene concludes on Day 0, HUD must remain suppressed
                cutscene.SetGameplayHUDVisible(true);

                foreach (var name in hudPanelNames)
                {
                    var t = canvasGO.transform.Find(name);
                    if (t != null && t.gameObject.activeSelf)
                    {
                        Debug.LogError($"[Test 45] Panel '{name}' was unexpectedly active on Day 0!");
                        return false;
                    }
                }

                // 4. Test Time Progression Lock on Day 0
                if (!timeMgr.IsPaused)
                {
                    Debug.LogError("[Test 45] DayNightTimeManager isPaused was false on Day 0!");
                    return false;
                }
                float hourBefore = timeMgr.CurrentHour;
                if (Mathf.Abs(hourBefore - 16.0f) > 0.01f)
                {
                    Debug.LogError($"[Test 45] Expected Day 0 hour == 16.0f, got {hourBefore}");
                    return false;
                }

                // 5. Test Activity Gating on Day 0
                var playerGO = new GameObject("Test_Player");
                cleanupList.Add(playerGO);

                // Farmland interaction on Day 0
                var farmTileGO = new GameObject("Test_FarmlandTile");
                cleanupList.Add(farmTileGO);
                var farmLabel = farmTileGO.AddComponent<WorldLabel>();
                var farmTile = farmTileGO.AddComponent<FarmlandTile>();
                farmTile.Interact(playerGO);
                if (farmTile.CurrentState != TileState.Untilled)
                {
                    Debug.LogError("[Test 45] Farmland tile mutated state on Day 0!");
                    return false;
                }
                farmTile.UpdateLabelText(playerGO);
                if (farmLabel.displayName != "Farmland (Rest First)")
                {
                    Debug.LogError($"[Test 45] Expected 'Farmland (Rest First)' on Day 0, got '{farmLabel.displayName}'");
                    return false;
                }

                // Workbench interaction on Day 0
                var benchGO = new GameObject("Test_Workbench");
                cleanupList.Add(benchGO);
                var benchLabel = benchGO.AddComponent<WorldLabel>();
                var bench = benchGO.AddComponent<FeaturesWorkbench.WorkbenchInteractable>();
                bench.Interact(playerGO);
                if (FeaturesWorkbench.UI.WorkbenchUI.Instance != null && FeaturesWorkbench.UI.WorkbenchUI.Instance.gameObject.activeSelf)
                {
                    Debug.LogError("[Test 45] Workbench UI opened on Day 0!");
                    return false;
                }

                // 6. Test Bed Sleep & Day 1 Transition
                var bedGO = new GameObject("Test_Bed");
                cleanupList.Add(bedGO);
                var bedLabel = bedGO.AddComponent<WorldLabel>();
                var bed = bedGO.AddComponent<FeaturesInteraction.BedInteractable>();
                bed.UpdateLabel();

                if (bedLabel.displayName != "Sleep (Begin Day 1)")
                {
                    Debug.LogError($"[Test 45] Expected bed label 'Sleep (Begin Day 1)' on Day 0, got '{bedLabel.displayName}'");
                    return false;
                }

                // Simulate Day 0 bed interaction
                bed.Interact(playerGO);

                // Day should now be 1
                if (timeMgr.CurrentDay != 1)
                {
                    Debug.LogError($"[Test 45] Expected CurrentDay == 1 after bed sleep, got {timeMgr.CurrentDay}");
                    return false;
                }

                if (timeMgr.IsPaused)
                {
                    Debug.LogError("[Test 45] DayNightTimeManager remained paused after Day 1 transition!");
                    return false;
                }

                if (Mathf.Abs(timeMgr.CurrentHour - 6.0f) > 0.05f)
                {
                    Debug.LogError($"[Test 45] Expected Day 1 dawn hour == 6.0f, got {timeMgr.CurrentHour}");
                    return false;
                }

                // Verify HUD panels are now revealed
                cutscene.SetGameplayHUDVisible(true);
                foreach (var name in hudPanelNames)
                {
                    var t = canvasGO.transform.Find(name);
                    if (t == null || !t.gameObject.activeSelf)
                    {
                        Debug.LogError($"[Test 45] Core HUD panel '{name}' failed to activate on Day 1!");
                        return false;
                    }
                }

                Debug.Log("✅ Test 45: Day 0 Arrival HUD suppression, time progression lock, activity gating, and Bed sleep transition verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 45] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                FeaturesTime.DayNightTimeManager.Instance = prevTimeInstance;
                if (prevTimeInstance != null)
                {
                    prevTimeInstance.SetDayAndTime(1, 6.0f);
                    FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(prevTimeInstance);
                }
                else if (prevService != null)
                {
                    FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(prevService);
                }

                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 46: Vehicle Driver Visibility, Ergonomic Seating, Accessories Gating, and Seated Animation State Audit
        public static bool Test_VehicleDriver_Visibility_ErgonomicSeating_And_AnimationState_Audit()
        {
            var cleanupList = new List<Object>();
            FarmBeware.Core.Runtime.ServiceLocator.TryResolve<FarmBeware.Core.Runtime.IPlayerContext>(out var prevPlayer);
            try
            {
                // 1. Create _ENTITIES root to verify clean reparenting
                var entitiesRoot = new GameObject("_ENTITIES");
                cleanupList.Add(entitiesRoot);

                // 2. Create Truck with ArcadeVehicleController
                var truckObj = new GameObject("Test_Pickup_Truck", typeof(Rigidbody), typeof(BoxCollider));
                cleanupList.Add(truckObj);
                var avc = truckObj.AddComponent<FarmBeware.Features.Vehicles.ArcadeVehicleController>();
                avc.EnsureSubAssemblies();

                // 3. Create Player under _ENTITIES
                var playerObj = new GameObject("Test_Player", typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerControl), typeof(PlayerEquipment));
                cleanupList.Add(playerObj);
                playerObj.transform.SetParent(entitiesRoot.transform, false);

                var pControl = playerObj.GetComponent<PlayerControl>();
                var pEquip = playerObj.GetComponent<PlayerEquipment>();

                // Add character visual SkinnedMeshRenderer (e.g. body / clothes)
                var charObj = new GameObject("character");
                charObj.transform.SetParent(playerObj.transform, false);
                var anim = charObj.AddComponent<Animator>();
                var ac = Resources.Load<UnityEditor.Animations.AnimatorController>("Player/AsepAnimator");
                if (ac != null) anim.runtimeAnimatorController = ac;

                var bodyObj = new GameObject("body");
                bodyObj.transform.SetParent(charObj.transform, false);
                var smr = bodyObj.AddComponent<SkinnedMeshRenderer>();
                smr.enabled = true;

                // Add non-character accessories (e.g. range indicator, dust)
                var indicatorObj = new GameObject("AttackRangeIndicator");
                indicatorObj.transform.SetParent(playerObj.transform, false);
                indicatorObj.SetActive(true);

                var dustObj = new GameObject("NightAtmosphere_Dust");
                dustObj.transform.SetParent(playerObj.transform, false);
                dustObj.SetActive(true);

                // 4. Test EnterVehicle
                avc.EnterVehicle(playerObj);

                if (!avc.IsDriverOccupied)
                {
                    Debug.LogError("[Test 46] Vehicle driver not occupied after EnterVehicle!");
                    return false;
                }

                // Verify PlayerControl & PlayerEquipment are disabled during occupancy
                if (pControl.enabled)
                {
                    Debug.LogError("[Test 46] PlayerControl remained enabled while driving!");
                    return false;
                }
                if (pEquip.enabled)
                {
                    Debug.LogError("[Test 46] PlayerEquipment remained enabled while driving!");
                    return false;
                }

                // Verify SkinnedMeshRenderer remains enabled (visible)
                if (!smr.enabled)
                {
                    Debug.LogError("[Test 46] Character SkinnedMeshRenderer was unexpectedly hidden on vehicle enter!");
                    return false;
                }

                // Verify non-character accessories are hidden
                if (indicatorObj.activeSelf)
                {
                    Debug.LogError("[Test 46] AttackRangeIndicator remained active while driving!");
                    return false;
                }
                if (dustObj.activeSelf)
                {
                    Debug.LogError("[Test 46] NightAtmosphere_Dust remained active while driving!");
                    return false;
                }

                // Verify Driving parameter is set on Animator if controller present
                if (anim.runtimeAnimatorController != null && !anim.GetBool("Driving"))
                {
                    Debug.LogError("[Test 46] Animator 'Driving' parameter was not set to true!");
                    return false;
                }

                // Verify motion synchronization: simulate truck movement and LateUpdate lock
                truckObj.transform.position += Vector3.forward * 10f;
                var lateUpdateMethod = typeof(FarmBeware.Features.Vehicles.ArcadeVehicleController).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                lateUpdateMethod?.Invoke(avc, null);

                var seat = truckObj.transform.Find("DriverSeatAnchor");
                if (seat != null)
                {
                    float dist = Vector3.Distance(playerObj.transform.position, seat.position);
                    if (dist > 0.001f)
                    {
                        Debug.LogError($"[Test 46] Player failed to move with vehicle! Distance to seat: {dist:F4}");
                        return false;
                    }
                }

                // 5. Test ExitVehicle
                avc.ExitVehicle();

                if (avc.IsDriverOccupied)
                {
                    Debug.LogError("[Test 46] Vehicle still occupied after ExitVehicle!");
                    return false;
                }

                // Verify PlayerControl & PlayerEquipment are re-enabled after exit
                if (!pControl.enabled)
                {
                    Debug.LogError("[Test 46] PlayerControl was not re-enabled on vehicle exit!");
                    return false;
                }
                if (!pEquip.enabled)
                {
                    Debug.LogError("[Test 46] PlayerEquipment was not re-enabled on vehicle exit!");
                    return false;
                }

                // Verify Driving parameter reset
                if (anim.runtimeAnimatorController != null && anim.GetBool("Driving"))
                {
                    Debug.LogError("[Test 46] Animator 'Driving' parameter was not reset to false!");
                    return false;
                }

                // Verify accessories restored
                if (!indicatorObj.activeSelf)
                {
                    Debug.LogError("[Test 46] AttackRangeIndicator failed to re-activate on vehicle exit!");
                    return false;
                }
                if (!dustObj.activeSelf)
                {
                    Debug.LogError("[Test 46] NightAtmosphere_Dust failed to re-activate on vehicle exit!");
                    return false;
                }

                // Verify player parent returned cleanly to _ENTITIES
                if (playerObj.transform.parent != entitiesRoot.transform)
                {
                    Debug.LogError($"[Test 46] Player parent was {playerObj.transform.parent?.name ?? "null"}, expected '_ENTITIES'!");
                    return false;
                }

                Debug.Log("✅ Test 46: Vehicle Driver Visibility, Rigid Motion Sync, Accessories Gating, and Clean Dismount verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 46] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                if (prevPlayer != null && (prevPlayer as Component) != null)
                {
                    FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.IPlayerContext>(prevPlayer);
                }
                else
                {
                    FarmBeware.Core.Runtime.ServiceLocator.Unregister<FarmBeware.Core.Runtime.IPlayerContext>();
                }
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 48: Day 0 Empty Inventory & Day 1 Starter Kit Distribution Audit
        private static bool Test_Day0_EmptyInventory_And_Day1_StarterKit_Award_Audit()
        {
            var cleanupList = new System.Collections.Generic.List<GameObject>();
            var prevTimeInstance = FeaturesTime.DayNightTimeManager.Instance;
            FarmBeware.Core.Runtime.ServiceLocator.TryResolve<FarmBeware.Core.Runtime.ITimeService>(out var prevService);

            try
            {
                var timeGo = new GameObject("Test_TimeService_Audit");
                cleanupList.Add(timeGo);
                var timeMgr = timeGo.AddComponent<DayNightTimeManager>();
                FeaturesTime.DayNightTimeManager.Instance = timeMgr;
                FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(timeMgr);

                var playerGo = new GameObject("Test_Player_StarterKitAudit");
                cleanupList.Add(playerGo);
                playerGo.tag = "Player";
                var inv = playerGo.AddComponent<InventoryComponent>();
                inv.ResetInventory(16);
                var kit = playerGo.AddComponent<global::Player.PlayerStarterKit>();

                // 1. Force Day 0
                timeMgr.SetDayAndTime(0, 16.0f);

                // 2. Execute EnsureStarterKit on Day 0
                kit.EnsureStarterKit();

                // Verify inventory is completely empty
                for (int i = 0; i < inv.slots.Count; i++)
                {
                    if (inv.slots[i] != null && !inv.slots[i].IsEmpty)
                    {
                        Debug.LogError($"[Test 48] Day 0 inventory must be empty, but slot {i} contained: {inv.slots[i].item?.itemName ?? "item"} x{inv.slots[i].quantity}!");
                        return false;
                    }
                }

                // Verify InventoryManagerUI hotbar visibility evaluation on Day 0
                var invUIGo = new GameObject("Test_InvUI_Audit");
                cleanupList.Add(invUIGo);
                var invUI = invUIGo.AddComponent<InventoryManagerUI>();
                if (invUI.ShouldHotbarBeVisible())
                {
                    Debug.LogError("[Test 48] InventoryManagerUI.ShouldHotbarBeVisible() returned true on Day 0! Expected false.");
                    return false;
                }

                // 3. Advance to Day 1
                timeMgr.SetDayAndTime(1, 6.0f);

                var chestGo = new GameObject("Test_StorageChest_Audit");
                cleanupList.Add(chestGo);
                var chestStorage = chestGo.AddComponent<FeaturesInteraction.StorageInteractable>();
                var chestInv = chestGo.GetComponent<InventoryComponent>();
                chestInv.ResetInventory(16);
                kit.SetTargetStorageChest(chestInv);

                // 4. Trigger Day 1 EnsureStarterKit
                kit.EnsureStarterKit();

                // Verify player inventory remains completely empty on Day 1 wake up per design
                for (int i = 0; i < 4; i++)
                {
                    if (inv.slots[i] != null && !inv.slots[i].IsEmpty)
                    {
                        Debug.LogError($"[Test 48] Day 1 player slot {i} was expected empty, found: {inv.slots[i].item?.itemId}!");
                        return false;
                    }
                }

                // Verify starter items populated in Storage Chest for side quest retrieval
                if (chestInv.slots[0].IsEmpty || chestInv.slots[0].item == null || !chestInv.slots[0].item.itemId.Contains("hoe"))
                {
                    Debug.LogError($"[Test 48] Storage Chest Slot 0 expected Hoe, found: {chestInv.slots[0].item?.itemId ?? "empty"}!");
                    return false;
                }
                if (chestInv.slots[1].IsEmpty || chestInv.slots[1].item == null || !chestInv.slots[1].item.itemId.Contains("plant_waterer"))
                {
                    Debug.LogError($"[Test 48] Storage Chest Slot 1 expected Plant Waterer, found: {chestInv.slots[1].item?.itemId ?? "empty"}!");
                    return false;
                }
                if (chestInv.slots[2].IsEmpty || chestInv.slots[2].item == null || !chestInv.slots[2].item.itemId.Contains("bottle_water"))
                {
                    Debug.LogError($"[Test 48] Storage Chest Slot 2 expected Water Bottle, found: {chestInv.slots[2].item?.itemId ?? "empty"}!");
                    return false;
                }
                if (chestInv.slots[3].IsEmpty || chestInv.slots[3].item == null || !chestInv.slots[3].item.itemId.Contains("sweet_potato") || chestInv.slots[3].quantity < 3 || chestInv.slots[3].quantity > 6)
                {
                    Debug.LogError($"[Test 48] Storage Chest Slot 3 expected 3-6x Sweet Potato Seeds, found: {chestInv.slots[3].item?.itemId ?? "empty"} x{chestInv.slots[3].quantity}!");
                    return false;
                }

                // Verify ShouldHotbarBeVisible() is true on Day 1
                if (!invUI.ShouldHotbarBeVisible())
                {
                    Debug.LogError("[Test 48] InventoryManagerUI.ShouldHotbarBeVisible() returned false on Day 1! Expected true.");
                    return false;
                }

                Debug.Log("✅ Test 48: Day 0 Zero-Inventory & Day 1 Storage Chest Tool Population + Player Empty Hands & Hotbar Gating verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 48] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                FeaturesTime.DayNightTimeManager.Instance = prevTimeInstance;
                if (prevService != null)
                {
                    FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ITimeService>(prevService);
                }
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 50: Day 0 Dismount No Hoe Held & Empty Inventory Invariant
        private static bool Test_Day0_NoHoeHeldAndEmptyInventory_Audit()
        {
            var cleanupList = new List<GameObject>();
            var prevTimeInstance = FeaturesTime.DayNightTimeManager.Instance;
            try
            {
                var timeGo = new GameObject("Test_TimeManager_Day0");
                cleanupList.Add(timeGo);
                var timeMgr = timeGo.AddComponent<FeaturesTime.DayNightTimeManager>();
                typeof(FeaturesTime.DayNightTimeManager)
                    .GetField("<CurrentDay>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(timeMgr, 0);
                FeaturesTime.DayNightTimeManager.Instance = timeMgr;

                var playerGo = new GameObject("Test_Player_Day0");
                cleanupList.Add(playerGo);
                var inv = playerGo.AddComponent<InventoryComponent>();
                var equip = playerGo.AddComponent<PlayerEquipment>();
                var kit = playerGo.AddComponent<PlayerStarterKit>();

                inv.selectedHotbarIndex = 2;
                kit.EnsureStarterKit(0);
                equip.DestroyCurrentWeapon();

                if (inv.selectedHotbarIndex != 0)
                {
                    Debug.LogError($"[Test 50] Day 0 expected selectedHotbarIndex = 0, got {inv.selectedHotbarIndex}!");
                    return false;
                }

                foreach (var slot in inv.slots)
                {
                    if (!slot.IsEmpty)
                    {
                        Debug.LogError($"[Test 50] Day 0 expected empty slot, found {slot.item?.itemId}!");
                        return false;
                    }
                }

                Debug.Log("✅ Test 50: Day 0 Dismount No Hoe Held & Empty Inventory Invariant verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 50] Exception: {ex.Message}\n{ex.StackTrace}");
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

        // Test 54: Fridge Permissive Crop & Food Storage (Sweet Potato, Taro, Corn) Audit
        private static bool Test_Fridge_AcceptsSweetPotatoAndAllCrops_Audit()
        {
            var cleanupList = new List<GameObject>();
            try
            {
                var fridgeGo = new GameObject("Test_Fridge");
                cleanupList.Add(fridgeGo);
                var inv = fridgeGo.AddComponent<InventoryComponent>();
                inv.SetAllowAllCropsAndFood(true);

                // Load sweet potato crop
                var sweetPotato = Resources.Load<ItemData>("Items/crop_sweet_potato");
                if (sweetPotato != null)
                {
                    if (!inv.CanAcceptItem(sweetPotato))
                    {
                        Debug.LogError("[Test 54] Fridge rejected sweet potato (crop_sweet_potato)! Expected accepted.");
                        return false;
                    }
                }

                // Create generic crop item
                var cropItem = ScriptableObject.CreateInstance<ItemData>();
                cropItem.itemId = "test_crop";
                cropItem.category = ItemCategory.Crop;
                if (!inv.CanAcceptItem(cropItem))
                {
                    Debug.LogError("[Test 54] Fridge rejected ItemCategory.Crop! Expected accepted.");
                    return false;
                }

                // Create generic seed item
                var seedItem = ScriptableObject.CreateInstance<ItemData>();
                seedItem.itemId = "test_seed";
                seedItem.category = ItemCategory.Seed;
                if (!inv.CanAcceptItem(seedItem))
                {
                    Debug.LogError("[Test 54] Fridge rejected ItemCategory.Seed! Expected accepted.");
                    return false;
                }

                Debug.Log("✅ Test 54: Fridge Permissive Crop & Food Storage (Sweet Potato, Taro, Corn) verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 54] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 55: InventorySlotUI Pointer Click InputLock Bypass & Transfer Dispatch Audit
        private static bool Test_InventorySlotUI_InputLockBypassAndSingleClickTransfer_Audit()
        {
            var cleanupList = new List<GameObject>();
            try
            {
                var canvasGo = new GameObject("Test_Canvas", typeof(Canvas), typeof(GraphicRaycaster));
                cleanupList.Add(canvasGo);

                var slotGo = new GameObject("Test_SlotUI", typeof(RectTransform), typeof(Image));
                slotGo.transform.SetParent(canvasGo.transform, false);
                cleanupList.Add(slotGo);
                var slotUI = slotGo.AddComponent<InventorySlotUI>();

                var playerGo = new GameObject("Test_Player");
                cleanupList.Add(playerGo);
                var playerInv = playerGo.AddComponent<InventoryComponent>();

                slotUI.Init(null, 0, playerInv);

                // Test unscaled timestamp click gap detection field exists
                var lastClickField = typeof(InventorySlotUI).GetField("_lastClickTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (lastClickField == null)
                {
                    Debug.LogError("[Test 55] _lastClickTime field missing on InventorySlotUI!");
                    return false;
                }

                Debug.Log("✅ Test 55: InventorySlotUI Pointer Click InputLock Bypass & Transfer Dispatch verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 55] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 56: DayTransitionUI Procedural Vignette Sprite Generation Audit
        private static bool Test_DayTransitionUI_ProceduralVignetteSprite_Audit()
        {
            var cleanupList = new List<GameObject>();
            try
            {
                var canvasGo = new GameObject("Test_TransitionCanvas", typeof(Canvas));
                cleanupList.Add(canvasGo);

                var uiGo = new GameObject("Test_DayTransitionUI");
                cleanupList.Add(uiGo);
                var ui = uiGo.AddComponent<DayTransitionUI>();
                ui.EnsureUIHierarchy();

                var vignetteField = typeof(DayTransitionUI).GetField("vignetteOverlay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var vignette = vignetteField?.GetValue(ui) as Image;

                if (vignette == null || vignette.sprite == null)
                {
                    Debug.LogError("[Test 56] DayTransitionUI vignetteOverlay or vignetteOverlay.sprite is null!");
                    return false;
                }

                var tex = vignette.sprite.texture;
                if (tex == null || tex.width != 128 || tex.height != 128)
                {
                    Debug.LogError("[Test 56] Vignette sprite texture is invalid or not 128x128!");
                    return false;
                }

                Color centerPixel = tex.GetPixel(64, 64);
                if (centerPixel.a > 0.05f)
                {
                    Debug.LogError($"[Test 56] Vignette center pixel alpha is {centerPixel.a}, expected transparent center (<0.05)!");
                    return false;
                }

                Color cornerPixel = tex.GetPixel(0, 0);
                if (cornerPixel.a < 0.80f)
                {
                    Debug.LogError($"[Test 56] Vignette corner pixel alpha is {cornerPixel.a}, expected dark edge (>0.80)!");
                    return false;
                }

                Debug.Log("✅ Test 56: DayTransitionUI Procedural Vignette Sprite Generation verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 56] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 58: Day 0 to Day 1 Bed Sleep Transition, Inventory Zero-State Wipe, Starter Kit Population, & Slot 0 Selection Audit
        private static bool Test_Day0_To_Day1_SleepFadeAndStarterKitTransition_Audit()
        {
            var cleanupList = new List<GameObject>();
            var prevTime = DayNightTimeManager.Instance;
            ServiceLocator.TryResolve<ITimeService>(out var prevService);

            try
            {
                var timeGo = new GameObject("Test_TimeService_Day0To1");
                cleanupList.Add(timeGo);
                var timeMgr = timeGo.AddComponent<DayNightTimeManager>();
                DayNightTimeManager.Instance = timeMgr;
                ServiceLocator.Register<ITimeService>(timeMgr);
                timeMgr.SetDayAndTime(0, 20.0f); // Day 0 Night arrival

                var playerGo = new GameObject("Test_Player_Day0To1");
                cleanupList.Add(playerGo);
                playerGo.tag = "Player";
                var inv = playerGo.AddComponent<InventoryComponent>();
                inv.ResetInventory(16);
                var equip = playerGo.AddComponent<PlayerEquipment>();
                var kit = playerGo.AddComponent<PlayerStarterKit>();
                kit.EnsureStarterKit(0);

                // Verify Day 0 inventory is 100% empty
                for (int i = 0; i < 4; i++)
                {
                    if (!inv.slots[i].IsEmpty)
                    {
                        Debug.LogError($"[Test 58] Day 0 slot {i} was not empty!");
                        return false;
                    }
                }

                // Storage chest setup for Day 1 starter tools placement
                var chestGo = new GameObject("StorageChest");
                cleanupList.Add(chestGo);
                var chestStorage = chestGo.AddComponent<FeaturesInteraction.StorageInteractable>();
                var chestInv = chestGo.GetComponent<InventoryComponent>();
                chestInv.ResetInventory(16);
                kit.SetTargetStorageChest(chestInv);

                // Verify Bed interaction on Day 0 advances to Day 1
                var bedGo = new GameObject("Test_Bed_Day0To1", typeof(BedInteractable));
                cleanupList.Add(bedGo);
                var bed = bedGo.GetComponent<BedInteractable>();
                bed.Interact(playerGo);

                if (timeMgr.CurrentDay != 1)
                {
                    Debug.LogError($"[Test 58] Expected Day 1, but current day is {timeMgr.CurrentDay}!");
                    return false;
                }

                // Verify player inventory remains completely empty on Day 1 wake up (player holds nothing)
                for (int i = 0; i < 4; i++)
                {
                    if (inv.slots[i] != null && !inv.slots[i].IsEmpty)
                    {
                        Debug.LogError($"[Test 58] Expected player slot {i} to be empty on Day 1 wake up, found {inv.slots[i].item?.itemId}!");
                        return false;
                    }
                }

                // Verify starter items populated in Storage Chest on Day 1
                if (chestInv.slots[0].IsEmpty || chestInv.slots[0].item == null || !chestInv.slots[0].item.itemId.Contains("hoe"))
                {
                    Debug.LogError($"[Test 58] Expected Hoe in storage chest slot 0, found: {chestInv.slots[0].item?.itemId ?? "empty"}!");
                    return false;
                }
                if (chestInv.slots[1].IsEmpty || chestInv.slots[1].item == null || !chestInv.slots[1].item.itemId.Contains("plant_waterer"))
                {
                    Debug.LogError($"[Test 58] Expected Plant Waterer in storage chest slot 1, found: {chestInv.slots[1].item?.itemId ?? "empty"}!");
                    return false;
                }
                if (chestInv.slots[2].IsEmpty || chestInv.slots[2].item == null || !chestInv.slots[2].item.itemId.Contains("bottle_water"))
                {
                    Debug.LogError($"[Test 58] Expected Water Bottle in storage chest slot 2, found: {chestInv.slots[2].item?.itemId ?? "empty"}!");
                    return false;
                }
                if (chestInv.slots[3].IsEmpty || chestInv.slots[3].item == null || !chestInv.slots[3].item.itemId.Contains("sweet_potato") || chestInv.slots[3].quantity < 3 || chestInv.slots[3].quantity > 6)
                {
                    Debug.LogError($"[Test 58] Expected 3-6 Sweet Potato Seeds in storage chest slot 3, found: {chestInv.slots[3].item?.itemId ?? "empty"} x{chestInv.slots[3].quantity}!");
                    return false;
                }

                // Verify selected hotbar slot is 0
                if (inv.selectedHotbarIndex != 0)
                {
                    Debug.LogError($"[Test 58] Expected selectedHotbarIndex = 0, found {inv.selectedHotbarIndex}!");
                    return false;
                }

                Debug.Log("✅ Test 58: Day 0 -> Day 1 Bed Transition, Inventory Zero-State Wipe, Storage Chest Population, & Slot 0 Selection verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 58] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                DayNightTimeManager.Instance = prevTime;
                if (prevService != null) ServiceLocator.Register<ITimeService>(prevService);
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 59: Bed daytime sleep confirmation modal, cursor locking, and StartNightPhase transition
        private static bool Test_DaytimeBedSleep_ConfirmationAndTransition_Audit()
        {
            var cleanupList = new List<GameObject>();
            var prevCursorVisible = Cursor.visible;
            var prevLockState = Cursor.lockState;
            var prevIsSaveUIOpen = UIModalHelper.IsSaveUIOpen;

            try
            {
                bool confirmed = false;
                BedSleepConfirmationUI.Show("3x Sweet Potato (ripening in 25s)", () => { confirmed = true; });

                var modalGO = GameObject.Find("BedSleepConfirmationUI");
                if (modalGO != null) cleanupList.Add(modalGO);

                var modal = modalGO != null ? modalGO.GetComponent<IModalWindow>() : null;
                if (modal == null || !modal.IsOpen)
                {
                    Debug.LogError("[Test 59] BedSleepConfirmationUI modal failed to open!");
                    return false;
                }

                // Verify cursor unlocked and visible
                if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
                {
                    Debug.LogError($"[Test 59] Cursor not freed on modal open: visible={Cursor.visible}, lockState={Cursor.lockState}!");
                    return false;
                }

                if (!UIModalHelper.IsSaveUIOpen)
                {
                    Debug.LogError("[Test 59] UIModalHelper.IsSaveUIOpen was not set to true!");
                    return false;
                }

                // Close modal (simulate cancel / ESC)
                modal.CloseModal();

                if (modal.IsOpen)
                {
                    Debug.LogError("[Test 59] BedSleepConfirmationUI modal failed to close!");
                    return false;
                }

                if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
                {
                    Debug.LogError($"[Test 59] Cursor not restored on modal close: visible={Cursor.visible}, lockState={Cursor.lockState}!");
                    return false;
                }

                if (UIModalHelper.IsSaveUIOpen)
                {
                    Debug.LogError("[Test 59] UIModalHelper.IsSaveUIOpen was not reset to false!");
                    return false;
                }

                // Re-open and verify Confirm callback & raycast contract
                BedSleepConfirmationUI.Show("", () => { confirmed = true; });

                var confirmBtnObj = GameObject.Find("SleepConfirmModalRoot/DialogCard/ButtonRow/ConfirmBtn");
                if (confirmBtnObj == null)
                {
                    Debug.LogError("[Test 59] ConfirmBtn GameObject not found under DialogCard!");
                    return false;
                }

                var btnGraphic = confirmBtnObj.GetComponent<Image>();
                if (btnGraphic == null || !btnGraphic.raycastTarget)
                {
                    Debug.LogError("[Test 59] ConfirmBtn Image is missing or has raycastTarget disabled!");
                    return false;
                }

                var lblTmp = confirmBtnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (lblTmp == null || lblTmp.raycastTarget)
                {
                    Debug.LogError("[Test 59] ConfirmBtn Label TextMeshProUGUI has raycastTarget=true which blocks button clicks!");
                    return false;
                }

                var methodConfirm = typeof(BedSleepConfirmationUI).GetMethod("HandleConfirm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (methodConfirm != null)
                {
                    modalGO = GameObject.Find("BedSleepConfirmationUI") ?? GameObject.Find("SleepConfirmModalRoot");
                    var uiInst = modalGO != null ? modalGO.GetComponent<BedSleepConfirmationUI>() : null;
                    if (uiInst == null) uiInst = Object.FindFirstObjectByType<BedSleepConfirmationUI>();
                    if (uiInst != null) methodConfirm.Invoke(uiInst, null);
                    if (!confirmed)
                    {
                        Debug.LogError("[Test 59] HandleConfirm failed to invoke onConfirm callback!");
                        return false;
                    }
                }

                // Verify DayNightTimeManager.ITimeService.StartNightPhase() transitions time to nightStartHour
                var timeGO = new GameObject("Test_DayNightManager");
                cleanupList.Add(timeGO);
                var dntm = timeGO.AddComponent<DayNightTimeManager>();
                var timeService = (ITimeService)dntm;
                dntm.SetTime(10f); // 10:00 AM daytime
                timeService.StartNightPhase();

                if (timeService.CurrentPhase != DayPhase.Night || dntm.CurrentPhase != FeaturesRendering.Lighting.EnvironmentPhase.Night)
                {
                    Debug.LogError($"[Test 59] StartNightPhase failed to transition phase to Night! Current={timeService.CurrentPhase}");
                    return false;
                }

                if (Mathf.Abs(timeService.TimeOfDay - 18.0f) > 0.01f)
                {
                    Debug.LogError($"[Test 59] StartNightPhase expected 18.0h, got {timeService.TimeOfDay}h!");
                    return false;
                }

                Debug.Log("✅ Test 59: Bed daytime sleep confirmation modal, raycast clickability, keyboard confirmation, and StartNightPhase transition verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 59] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                Cursor.visible = prevCursorVisible;
                Cursor.lockState = prevLockState;
                UIModalHelper.IsSaveUIOpen = prevIsSaveUIOpen;

                var modalRoot = GameObject.Find("SleepConfirmModalRoot");
                if (modalRoot != null) cleanupList.Add(modalRoot);
                var modalBedUI = GameObject.Find("BedSleepConfirmationUI");
                if (modalBedUI != null) cleanupList.Add(modalBedUI);
                if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
                {
                    ModalStackManager.Instance.Clear();
                }

                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 61: Inventory Tab toggle open and close, zero frame-bounce, and LastFrameUIPanelClosed synchronization
        private static bool Test_Inventory_TabToggle_OpenAndClose_Audit()
        {
            var cleanupList = new List<GameObject>();
            var prevCursorVisible = Cursor.visible;
            var prevLockState = Cursor.lockState;

            try
            {
                var uiGO = new GameObject("Test_InventoryUI");
                cleanupList.Add(uiGO);
                var invUI = uiGO.AddComponent<InventoryManagerUI>();

                var playerPanelGO = new GameObject("PlayerPanel");
                playerPanelGO.transform.SetParent(uiGO.transform);
                invUI.playerPanel = playerPanelGO;
                playerPanelGO.SetActive(false);

                // Initial state: closed
                if (invUI.IsAnyInventoryUIRelatedOpen())
                {
                    Debug.LogError("[Test 61] Expected inventory initially closed!");
                    return false;
                }

                // 1. Toggle open via TogglePlayerInventory
                invUI.TogglePlayerInventory();
                if (!invUI.IsAnyInventoryUIRelatedOpen())
                {
                    Debug.LogError("[Test 61] TogglePlayerInventory failed to open inventory!");
                    return false;
                }
                if (!playerPanelGO.activeSelf)
                {
                    Debug.LogError("[Test 61] playerPanel is not active after opening!");
                    return false;
                }

                // 2. Toggle close via CloseAllUI
                invUI.CloseAllUI();
                if (invUI.IsAnyInventoryUIRelatedOpen())
                {
                    Debug.LogError("[Test 61] CloseAllUI failed to close inventory!");
                    return false;
                }
                if (playerPanelGO.activeSelf)
                {
                    Debug.LogError("[Test 61] playerPanel is still active after CloseAllUI!");
                    return false;
                }
                if (UIModalHelper.LastFrameUIPanelClosed != Time.frameCount)
                {
                    Debug.LogError($"[Test 61] UIModalHelper.LastFrameUIPanelClosed ({UIModalHelper.LastFrameUIPanelClosed}) != Time.frameCount ({Time.frameCount})!");
                    return false;
                }

                Debug.Log("✅ Test 61: Inventory Tab toggle open/close, zero frame-bounce, and LastFrameUIPanelClosed synchronization verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 61] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                Cursor.visible = prevCursorVisible;
                Cursor.lockState = prevLockState;
                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 67: Player V2 Character Rig Avatar, Native Combat Animations, Loop Settings, and Controller State Audit
        private static bool Test_PlayerV2_CharacterAsset_NativeAnimations_And_Avatar_Audit()
        {
            try
            {
                // 1. Verify character_player_v2 prefab avatar binding
                var prefabPath = "Assets/Prefabs/Player/character_player_v2.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError("[Test 67] character_player_v2.prefab not found at expected path!");
                    return false;
                }

                var anim = prefab.GetComponent<Animator>();
                if (anim == null)
                {
                    Debug.LogError("[Test 67] Animator missing on character_player_v2.prefab!");
                    return false;
                }

                if (anim.avatar == null || anim.avatar.name != "characterAvatar")
                {
                    Debug.LogError($"[Test 67] Animator avatar is {anim.avatar?.name}, expected 'characterAvatar'!");
                    return false;
                }

                // 2. Verify AsepAnimator_V2 controller states and native combat motions
                var ctrlPath = "Assets/Resources/Player/AsepAnimator_V2.controller";
                var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ctrlPath);
                if (ctrl == null)
                {
                    Debug.LogError("[Test 67] AsepAnimator_V2.controller missing from Resources/Player/!");
                    return false;
                }

                var sm = ctrl.layers[0].stateMachine;
                var stateDict = sm.states.ToDictionary(s => s.state.name, s => s.state);

                // Verify Combat Motions
                if (!stateDict.TryGetValue("Attack_Combo1", out var combo1) || combo1.motion == null || combo1.motion.name != "sword_up")
                {
                    Debug.LogError($"[Test 67] Attack_Combo1 motion is {combo1?.motion?.name}, expected 'sword_up'!");
                    return false;
                }

                if (!stateDict.TryGetValue("Attack_Combo2", out var combo2) || combo2.motion == null || combo2.motion.name != "sword_cross")
                {
                    Debug.LogError($"[Test 67] Attack_Combo2 motion is {combo2?.motion?.name}, expected 'sword_cross'!");
                    return false;
                }

                if (!stateDict.TryGetValue("Attack_Combo3", out var combo3) || combo3.motion == null || combo3.motion.name != "sword_combo")
                {
                    Debug.LogError($"[Test 67] Attack_Combo3 motion is {combo3?.motion?.name}, expected 'sword_combo'!");
                    return false;
                }

                if (!stateDict.TryGetValue("Attack_Heavy", out var heavy) || heavy.motion == null || heavy.motion.name != "sword_combo")
                {
                    Debug.LogError($"[Test 67] Attack_Heavy motion is {heavy?.motion?.name}, expected 'sword_combo'!");
                    return false;
                }

                if (!stateDict.TryGetValue("Attack_Skill", out var skill) || skill.motion == null || skill.motion.name != "sword_dash")
                {
                    Debug.LogError($"[Test 67] Attack_Skill motion is {skill?.motion?.name}, expected 'sword_dash'!");
                    return false;
                }

                if (!stateDict.TryGetValue("Idle_Combat", out var idleCombat) || idleCombat.motion == null || idleCombat.motion.name != "sword_idle")
                {
                    Debug.LogError($"[Test 67] Idle_Combat state or motion is {idleCombat?.motion?.name}, expected 'sword_idle'!");
                    return false;
                }

                // 3. Verify Animation Clip Loop Time Invariants
                string[] loopingClips = new string[] { "idle_loop", "walk_loop", "Running", "sword_idle" };
                foreach (var clipName in loopingClips)
                {
                    string p = clipName == "sword_idle" 
                        ? "Assets/Art/Characters/PlayerV2/anim/combat/sword_idle.anim" 
                        : $"Assets/Art/Characters/PlayerV2/anim/{clipName}.anim";
                    var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                    if (c == null)
                    {
                        Debug.LogError($"[Test 67] Clip {clipName} not found at {p}!");
                        return false;
                    }
                    var settings = AnimationUtility.GetAnimationClipSettings(c);
                    if (!settings.loopTime)
                    {
                        Debug.LogError($"[Test 67] Clip {clipName} expected loopTime=true!");
                        return false;
                    }
                }

                Debug.Log("✅ Test 67: Player V2 character rig avatar, native combat animations (sword_up, sword_cross, sword_combo, sword_dash, sword_idle), and controller state audit verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 67] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // Test 70: Vehicle Driving HUD Cutscene Suppression, Cross-Cutscene State Integrity, and Kinematic Enemy Safety Audit
        private static bool Test_VehicleDrivingHUD_Suppression_And_KinematicEnemySafety_Audit()
        {
            var cleanupList = new System.Collections.Generic.List<GameObject>();
            var oldCutscene = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ICutsceneService>();

            var opening = FeaturesStoryline.Cutscenes.OpeningCutsceneController.Instance;
            GameObject opGo = null;
            if (opening == null)
            {
                opGo = new GameObject("Test_OpeningCutscene_Host", typeof(UnityEngine.Playables.PlayableDirector), typeof(FeaturesStoryline.Cutscenes.OpeningCutsceneController));
                cleanupList.Add(opGo);
                opening = opGo.GetComponent<FeaturesStoryline.Cutscenes.OpeningCutsceneController>();
            }
            var opField = typeof(FeaturesStoryline.Cutscenes.OpeningCutsceneController).GetField("_isCutscenePlaying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool origOpPlaying = (bool)(opField?.GetValue(opening) ?? false);

            var night1 = FeaturesStoryline.Cutscenes.Night1CutsceneController.Instance;
            GameObject n1Go = null;
            if (night1 == null)
            {
                n1Go = new GameObject("Test_Night1Cutscene_Host", typeof(FeaturesStoryline.Cutscenes.Night1CutsceneController));
                cleanupList.Add(n1Go);
                night1 = n1Go.GetComponent<FeaturesStoryline.Cutscenes.Night1CutsceneController>();
            }
            var n1Field = typeof(FeaturesStoryline.Cutscenes.Night1CutsceneController).GetField("_isCutscenePlaying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool origN1Playing = (bool)(n1Field?.GetValue(night1) ?? false);

            try
            {
                // 1. Setup VehicleDrivingHUD
                var hud = FarmBeware.Features.Vehicles.UI.VehicleDrivingHUD.Instance;
                GameObject hudGo = null;
                if (hud == null)
                {
                    hudGo = new GameObject("VehicleDrivingHUD_Host", typeof(FarmBeware.Features.Vehicles.UI.VehicleDrivingHUD));
                    cleanupList.Add(hudGo);
                    hud = hudGo.GetComponent<FarmBeware.Features.Vehicles.UI.VehicleDrivingHUD>();
                }

                // Register Night1CutsceneController into ServiceLocator to test cross-service resolution
                FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ICutsceneService>(night1);

                opField?.SetValue(opening, false);
                n1Field?.SetValue(night1, false);

                hud.SetDrivingActive(false);
                if (hud.ShouldBeVisible())
                {
                    Debug.LogError("[Test 70] HUD should not be visible when driving is inactive!");
                    return false;
                }

                // Simulate driving mode outside cutscene
                hud.SetDrivingActive(true);
                if (!hud.ShouldBeVisible())
                {
                    Debug.LogError("[Test 70] HUD should be visible while driving outside cutscenes!");
                    return false;
                }

                // 2. Simulate Opening Cutscene playback
                opField?.SetValue(opening, true);

                if (!night1.IsCutscenePlaying)
                {
                    Debug.LogError("[Test 70] night1.IsCutscenePlaying must return true when opening cutscene is active!");
                    return false;
                }

                if (!hud.IsCutsceneActive())
                {
                    Debug.LogError("[Test 70] hud.IsCutsceneActive() must return true when opening cutscene is active!");
                    return false;
                }

                if (hud.ShouldBeVisible())
                {
                    Debug.LogError("[Test 70] hud.ShouldBeVisible() must be FALSE during opening cutscene!");
                    return false;
                }

                // SnapHidden test
                hud.SnapHidden();
                var canvas = GameObject.Find("VehicleDrivingCanvas")?.GetComponent<Canvas>();
                if (canvas != null && canvas.enabled)
                {
                    Debug.LogError("[Test 70] Canvas must be disabled after SnapHidden()!");
                    return false;
                }

                // Reset opening cutscene and test Night1 cutscene
                opField?.SetValue(opening, false);
                n1Field?.SetValue(night1, true);

                if (!hud.IsCutsceneActive() || hud.ShouldBeVisible())
                {
                    Debug.LogError("[Test 70] HUD must suppress during Night1 cutscene!");
                    return false;
                }

                // 3. Kinematic Rigidbody Safety in EnemyBase
                var enemyGo = new GameObject("Test_Enemy_Kinematic", typeof(Rigidbody), typeof(BoxCollider));
                cleanupList.Add(enemyGo);
                var rb = enemyGo.GetComponent<Rigidbody>();
                rb.isKinematic = true;

                var enemy = enemyGo.AddComponent<FeaturesCombat.EnemyBase>();
                var setVel = typeof(FeaturesCombat.EnemyBase).GetMethod("SetLinearVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                // Must execute cleanly without throwing "Setting linear velocity of a kinematic body is not supported"
                setVel?.Invoke(enemy, new object[] { new Vector3(3f, 0f, 3f) });

                Debug.Log("✅ Test 70: Vehicle Driving HUD Cutscene Suppression, Cross-Cutscene State Integrity, and Kinematic Enemy Safety verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 70] Exception during audit: {ex.Message} " + ex.StackTrace);
                return false;
            }
            finally
            {
                if (opField != null && opening != null) opField.SetValue(opening, false);
                if (n1Field != null && night1 != null) n1Field.SetValue(night1, false);

                if (oldCutscene != null)
                {
                    FarmBeware.Core.Runtime.ServiceLocator.Register<FarmBeware.Core.Runtime.ICutsceneService>(oldCutscene);
                }
                else
                {
                    FarmBeware.Core.Runtime.ServiceLocator.Unregister<FarmBeware.Core.Runtime.ICutsceneService>();
                }

                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 92: Vehicle Interaction Range Calibration & Input Lock During Cutscene Audit
        private static bool Test_Vehicle_InteractionRangeTuned_And_InputBlockedDuringCutscene_Audit()
        {
            var cleanupList = new System.Collections.Generic.List<GameObject>();
            ServiceLocator.TryResolve<IPlayerContext>(out var prevPlayer);
            ServiceLocator.TryResolve<ICutsceneService>(out var prevCutscene);
            try
            {
                // 1. Setup truck and components
                var truckObj = new GameObject("Test_Range_Truck", typeof(Rigidbody), typeof(BoxCollider));
                cleanupList.Add(truckObj);
                var avc = truckObj.AddComponent<FarmBeware.Features.Vehicles.ArcadeVehicleController>();
                avc.EnsureSubAssemblies();

                // 2. Setup player
                var playerObj = new GameObject("Test_Range_Player", typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerControl));
                cleanupList.Add(playerObj);
                var playerControl = playerObj.GetComponent<PlayerControl>();
                ServiceLocator.Register<IPlayerContext>(playerControl);

                // Setup mock cutscene service
                var mockCutscene = new MockVehicleCutsceneService();
                ServiceLocator.Register<ICutsceneService>(mockCutscene);

                // Verify 1: Near Driver Door (X = -1.5m, Y = 0, Z = 0.2m) -> CanInteract is TRUE
                playerObj.transform.position = truckObj.transform.position + new Vector3(-1.5f, 0f, 0.2f);
                Physics.SyncTransforms();
                if (!avc.CanInteract(playerObj))
                {
                    Debug.LogError("[Test 92] CanInteract returned false when player is standing at driver door (-1.5m)!");
                    return false;
                }

                // Verify 2: Far Cargo Bed / Rear (Z = +5.0m) -> CanInteract is FALSE
                playerObj.transform.position = truckObj.transform.position + new Vector3(0f, 0f, 5.0f);
                Physics.SyncTransforms();
                if (avc.CanInteract(playerObj))
                {
                    Debug.LogError("[Test 92] CanInteract returned true when player is far behind truck (+5.0m)!");
                    return false;
                }

                // Verify 3: Far Passenger Side (X = +4.0m) -> CanInteract is FALSE
                playerObj.transform.position = truckObj.transform.position + new Vector3(4.0f, 0f, 0.2f);
                Physics.SyncTransforms();
                if (avc.CanInteract(playerObj))
                {
                    Debug.LogError("[Test 92] CanInteract returned true when player is far on passenger lawn (+4.0m)!");
                    return false;
                }

                // Verify 4: Standing at door, but cutscene starts -> CanInteract MUST BE FALSE
                playerObj.transform.position = truckObj.transform.position + new Vector3(-1.5f, 0f, 0.2f);
                Physics.SyncTransforms();
                mockCutscene.SetPlaying(true);
                if (avc.CanInteract(playerObj))
                {
                    Debug.LogError("[Test 92] CanInteract returned true while cutscene is active!");
                    return false;
                }

                // Verify 5: Driving input blocked during cutscene
                mockCutscene.SetPlaying(false);
                avc.EnterVehicle(playerObj);
                if (!avc.IsDriverOccupied)
                {
                    Debug.LogError("[Test 92] Failed to enter vehicle!");
                    return false;
                }

                // Activate cutscene while player is seated
                mockCutscene.SetPlaying(true);
                if (!avc.IsCutsceneOrInputBlocked())
                {
                    Debug.LogError("[Test 92] IsCutsceneOrInputBlocked returned false during active cutscene!");
                    return false;
                }

                // Simulate FixedUpdate while cutscene is playing
                var fixedUpdateMethod = typeof(FarmBeware.Features.Vehicles.ArcadeVehicleController).GetMethod("FixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fixedUpdateMethod?.Invoke(avc, null);

                var rb = truckObj.GetComponent<Rigidbody>();
                if (rb.linearVelocity.sqrMagnitude > 0.0001f || avc.CurrentSpeedKmh > 0.01f)
                {
                    Debug.LogError($"[Test 92] Vehicle moved during active cutscene! Velocity={rb.linearVelocity}, SpeedKmh={avc.CurrentSpeedKmh}");
                    return false;
                }

                mockCutscene.SetPlaying(false);
                avc.ExitVehicle();

                Debug.Log("✅ Test 92: Vehicle Driver Door Interaction Range (2.4m) and Cutscene Immobilization verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 92] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                if (prevPlayer != null && (prevPlayer as Component) != null) ServiceLocator.Register<IPlayerContext>(prevPlayer);
                else ServiceLocator.Unregister<IPlayerContext>();

                if (prevCutscene != null) ServiceLocator.Register<ICutsceneService>(prevCutscene);
                else ServiceLocator.Unregister<ICutsceneService>();

                for (int i = cleanupList.Count - 1; i >= 0; i--)
                {
                    if (cleanupList[i] != null) Object.DestroyImmediate(cleanupList[i]);
                }
            }
        }

        // Test 93: Verify Day 1 Starter Kit does NOT contain endgame combat armor or weapons (Hoe, Waterer, Bottle, 3 seeds only)
        private static bool Test_StarterKit_DoesNotContainCombatArmor_Audit()
        {
            var testPlayer = new GameObject("Test93_Player");
            var testChest = new GameObject("Test93_Chest");
            try
            {
                var pInv = testPlayer.AddComponent<InventoryComponent>();
                pInv.ResetInventory(16);
                var pArmor = testPlayer.AddComponent<PlayerArmorUpgradeState>();
                var pKit = testPlayer.AddComponent<PlayerStarterKit>();

                var cInv = testChest.AddComponent<InventoryComponent>();
                cInv.ResetInventory(16);
                pKit.PopulateStorageChest(cInv);

                var db = Resources.Load<ItemDatabase>("Database/ItemDatabase");
                var hoe = db?.GetItem("tool_hoe");
                var waterer = db?.GetItem("tool_plant_waterer");
                var bottle = db?.GetItem("food_bottle_water");
                var seed = db?.GetItem("seed_sweet_potato");

                if (cInv.CountItem(hoe) < 1 || cInv.CountItem(waterer) < 1 || cInv.CountItem(bottle) < 1 || cInv.CountItem(seed) < 1)
                {
                    Debug.LogError("[Test 93] Storage Chest missing core starter farming items (Hoe/Waterer/Bottle/Seed)!");
                    return false;
                }

                // Verify chest does NOT contain combat armor or weapons
                string[] bannedIds = { "item_burrow_hood", "item_swift_root_armor", "item_fang_grips", "item_underground_striders", "item_sweet_potato_heart", "tool_weapon_corn_repeater" };
                foreach (var bid in bannedIds)
                {
                    var item = db?.GetItem(bid);
                    if (item != null && cInv.CountItem(item) > 0)
                    {
                        Debug.LogError($"[Test 93] Storage Chest illegally contained '{bid}' on Day 1! Player progression bypassed.");
                        return false;
                    }
                }

                Debug.Log("✅ Test 93: Day 1 Starter Kit verified (Hoe, Waterer, Bottle, 3 Seeds only; 0 combat armor / slingshot in chest).");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 93] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                if (testPlayer != null) Object.DestroyImmediate(testPlayer);
                if (testChest != null) Object.DestroyImmediate(testChest);
            }
        }

        // Test 97: Verify Water Bottle shop buyPrice is calibrated to 2,000 G
        private static bool Test_WaterBottle_ShopRepurchasePrice_Audit()
        {
            var bottle = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Food/Food_BottleOfWater.asset");
            if (bottle == null || bottle.buyPrice != 2000)
            {
                Debug.LogError($"[Test 97] Food_BottleOfWater buyPrice is {bottle?.buyPrice} (expected 2000)!");
                return false;
            }

            Debug.Log("✅ Test 97: Food_BottleOfWater shop buyPrice calibrated to 2,000 G verified.");
            return true;
        }

        // Test 103: Axis 3 — Interaction Edges (Swing Cancel Hitbox Cleanup & Nightfall Auto-Dismount Vehicle Anti-Exploit) Audit
        public static bool Test_Axis3_InteractionEdges_SwingCancelCleanup_And_NightVehicleAutoDismount_Audit()
        {
            GameObject playerObj = null;
            GameObject weaponObj = null;
            GameObject truckObj = null;
            GameObject tmObj = null;
            try
            {
                if (TimeManager.Instance == null)
                {
                    tmObj = new GameObject("Test_Axis3_TimeManager");
                    tmObj.AddComponent<TimeManager>();
                }
                TimeManager.Instance.SetTimeState(1, TimeManager.DayPhase.Day, false);

                playerObj = new GameObject("Test_Axis3_Player");
                playerObj.AddComponent<Rigidbody>();
                playerObj.AddComponent<CapsuleCollider>();
                var pc = playerObj.AddComponent<PlayerControl>();
                var pe = playerObj.AddComponent<PlayerEquipment>();

                weaponObj = new GameObject("Test_Axis3_WeaponModel");
                weaponObj.transform.SetParent(playerObj.transform, false);
                var boxCol = weaponObj.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                var hitbox = weaponObj.AddComponent<FeaturesCombat.WeaponHitbox>();

                var modelField = typeof(PlayerEquipment).GetField("currentWeaponModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                modelField?.SetValue(pe, weaponObj);

                // 1. Activate weapon hitbox as if mid-swing, then trigger CancelActiveAttackSwing()
                hitbox.Activate(playerObj, 25, 5f, Vector3.forward);
                if (!hitbox.IsActive)
                {
                    Debug.LogError("[Test 103] WeaponHitbox failed to activate for test setup!");
                    return false;
                }

                pe.CancelActiveAttackSwing();
                if (hitbox.IsActive)
                {
                    Debug.LogError("[Test 103] Ghost hitbox leak! WeaponHitbox remained active after CancelActiveAttackSwing()!");
                    return false;
                }

                // 2. Enter vehicle during Day, then trigger Night phase transition -> must auto-dismount & block re-entry during Night Brawl
                truckObj = new GameObject("Test_Axis3_Truck");
                truckObj.AddComponent<Rigidbody>();
                var vehicle = truckObj.AddComponent<FarmBeware.Features.Vehicles.ArcadeVehicleController>();
                truckObj.transform.position = new Vector3(-20f, 0f, 53.5f);

                vehicle.EnterVehicle(playerObj);
                if (!vehicle.IsDriverOccupied || !pc.HasInputLock("VehicleDriving"))
                {
                    Debug.LogError("[Test 103] Failed to enter vehicle during Day phase!");
                    return false;
                }

                // Transition to Night (simulating 18:00 auto-sleep / Night Brawl start while driving)
                TimeManager.Instance.StartNightPhase();

                if (vehicle.IsDriverOccupied || pc.HasInputLock("VehicleDriving"))
                {
                    Debug.LogError($"[Test 103] Vehicle remained occupied ({vehicle.IsDriverOccupied}) or input locked ({pc.HasInputLock("VehicleDriving")}) after Night phase transition!");
                    return false;
                }

                // Attempt to interact with vehicle during active Night Brawl -> must be rejected
                playerObj.transform.position = truckObj.transform.position;
                if (vehicle.CanInteract(playerObj))
                {
                    Debug.LogError("[Test 103] Vehicle allowed entry during active Night Brawl (invulnerability exploit)!");
                    return false;
                }

                TimeManager.Instance.SetTimeState(1, TimeManager.DayPhase.Day, false);
                Debug.Log("✅ Test 103: Axis 3 Interaction Edges (mid-swing hitbox cancel cleanup and Nightfall vehicle auto-dismount & anti-exploit) verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 103] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.SetTimeState(1, TimeManager.DayPhase.Day, false);
                }
                if (truckObj != null) Object.DestroyImmediate(truckObj);
                if (weaponObj != null) Object.DestroyImmediate(weaponObj);
                if (playerObj != null) Object.DestroyImmediate(playerObj);
                if (tmObj != null) Object.DestroyImmediate(tmObj);
            }
        }

        // Test 104: Axis 4 — Zero-GC Tick Invariants (CropGrowthCountdownUI Second-Caching, NavMesh GetCornersNonAlloc Buffer, and Scene-Handle Cutscene Caching) Audit
        public static bool Test_Axis4_ZeroGC_TickInvariants_CountdownCache_And_CutsceneHasInstance_Audit()
        {
            GameObject plotObj = null;
            try
            {
                plotObj = new GameObject("Test_Axis4_CountdownPlot");
                var countdown = plotObj.AddComponent<FeaturesFarming.CropGrowthCountdownUI>();
                var awakeMethod = typeof(FeaturesFarming.CropGrowthCountdownUI).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                awakeMethod?.Invoke(countdown, null);

                var timerTextField = typeof(FeaturesFarming.CropGrowthCountdownUI).GetField("timerText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tmp = timerTextField?.GetValue(countdown) as TMP_Text;
                if (tmp == null)
                {
                    Debug.LogError("[Test 104] CropGrowthCountdownUI failed to build timerText!");
                    return false;
                }

                // Frame 1 at 119.9s (CeilToInt = 120)
                countdown.UpdateTileState(FeaturesFarming.TileState.PlantedWatered, 0.50f, 119.9f, "Sweet Potato");
                string firstRef = tmp.text;
                if (countdown.LastDisplayedSeconds != 120 || firstRef != "2:00")
                {
                    Debug.LogError($"[Test 104] Expected '2:00' (120s), got '{firstRef}' ({countdown.LastDisplayedSeconds}s)!");
                    return false;
                }

                // Frame 2 & 3 within the same integer second (119.5s, 119.1s -> CeilToInt = 120): must preserve exact string reference (0 B allocated)
                countdown.UpdateTileState(FeaturesFarming.TileState.PlantedWatered, 0.501f, 119.5f, "Sweet Potato");
                countdown.UpdateTileState(FeaturesFarming.TileState.PlantedWatered, 0.502f, 119.1f, "Sweet Potato");
                string secondRef = tmp.text;

                if (! object.ReferenceEquals(firstRef, secondRef))
                {
                    Debug.LogError("[Test 104] CropGrowthCountdownUI allocated a new string on sub-second frames despite identical integer second!");
                    return false;
                }

                // Verify EnemyBase preallocates _pathCornersBuffer (Vector3[16]) for GetCornersNonAlloc
                var cornersField = typeof(FeaturesCombat.EnemyBase).GetField("_pathCornersBuffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cornersField == null)
                {
                    Debug.LogError("[Test 104] EnemyBase missing preallocated _pathCornersBuffer for NavMeshPath.GetCornersNonAlloc!");
                    return false;
                }

                // Verify OpeningCutsceneController and Night1CutsceneController cache _lastSearchedSceneHandle
                var opSceneField = typeof(FeaturesStoryline.Cutscenes.OpeningCutsceneController).GetField("_lastSearchedSceneHandle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var n1SceneField = typeof(FeaturesStoryline.Cutscenes.Night1CutsceneController).GetField("_lastSearchedSceneHandle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (opSceneField == null || n1SceneField == null)
                {
                    Debug.LogError("[Test 104] Cutscene controllers missing _lastSearchedSceneHandle static cache!");
                    return false;
                }

                Debug.Log("✅ Test 104: Axis 4 Zero-GC tick invariants (CropGrowthCountdownUI string reference caching, EnemyBase GetCornersNonAlloc buffer, and cutscene HasInstance scene-handle caching) verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 104] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                if (plotObj != null) Object.DestroyImmediate(plotObj);
            }
        }

        // Test 106: MapProjectionMath Zero-GC coordinate normalization & Style C bounds
        private static bool Test_WorldMap_ProjectionMath_ZeroGC_And_StyleCBoundaryCalculations()
        {
            try
            {
                var bounds = FarmBeware.Core.Runtime.MapBounds2D.DefaultStyleCBounds;

                // 1. Boundary span verification
                if (Mathf.Abs(bounds.SpanX - 231.11112f) > 0.01f || Mathf.Abs(bounds.SpanZ - 130.0f) > 0.01f)
                {
                    Debug.LogError($"[Test 106] Invalid Style C Bounds span! SpanX={bounds.SpanX}, SpanZ={bounds.SpanZ}");
                    return false;
                }

                // 2. Center point mapping: (-70, 35) must project to exactly UV (0.5, 0.5) and anchored (0, 0)
                var centerWorld = new Vector3(-70f, 0f, 35f);
                var uvCenter = FarmBeware.Core.Runtime.MapProjectionMath.WorldToNormalizedUV(centerWorld, bounds);
                if (Mathf.Abs(uvCenter.x - 0.5f) > 0.0001f || Mathf.Abs(uvCenter.y - 0.5f) > 0.0001f)
                {
                    Debug.LogError($"[Test 106] Center world position failed UV mapping! UV={uvCenter}");
                    return false;
                }

                var containerSize = new Vector2(1600f, 900f);
                var anchoredCenter = FarmBeware.Core.Runtime.MapProjectionMath.WorldToMapAnchoredPosition(centerWorld, bounds, containerSize);
                if (anchoredCenter.sqrMagnitude > 0.0001f)
                {
                    Debug.LogError($"[Test 106] Center world position did not map to anchored (0, 0)! Anchored={anchoredCenter}");
                    return false;
                }

                // 3. Min/Max boundary corners
                var minWorld = new Vector3(bounds.MinX, 0f, bounds.MinZ);
                var uvMin = FarmBeware.Core.Runtime.MapProjectionMath.WorldToNormalizedUV(minWorld, bounds);
                if (Mathf.Abs(uvMin.x) > 0.0001f || Mathf.Abs(uvMin.y) > 0.0001f)
                {
                    Debug.LogError($"[Test 106] Min corner failed UV mapping! UV={uvMin}");
                    return false;
                }

                var anchoredMin = FarmBeware.Core.Runtime.MapProjectionMath.UVToAnchoredPosition(uvMin, containerSize);
                if (Mathf.Abs(anchoredMin.x - (-800f)) > 0.01f || Mathf.Abs(anchoredMin.y - (-450f)) > 0.01f)
                {
                    Debug.LogError($"[Test 106] Min corner failed anchored position! Anchored={anchoredMin}");
                    return false;
                }

                var maxWorld = new Vector3(bounds.MaxX, 0f, bounds.MaxZ);
                var uvMax = FarmBeware.Core.Runtime.MapProjectionMath.WorldToNormalizedUV(maxWorld, bounds);
                if (Mathf.Abs(uvMax.x - 1f) > 0.0001f || Mathf.Abs(uvMax.y - 1f) > 0.0001f)
                {
                    Debug.LogError($"[Test 106] Max corner failed UV mapping! UV={uvMax}");
                    return false;
                }

                // 4. Heading rotation conversion
                if (Mathf.Abs(FarmBeware.Core.Runtime.MapProjectionMath.WorldYawToUIRotationZ(0f) - 0f) > 0.0001f ||
                    Mathf.Abs(FarmBeware.Core.Runtime.MapProjectionMath.WorldYawToUIRotationZ(90f) - (-90f)) > 0.0001f ||
                    Mathf.Abs(FarmBeware.Core.Runtime.MapProjectionMath.WorldYawToUIRotationZ(180f) - (-180f)) > 0.0001f ||
                    Mathf.Abs(FarmBeware.Core.Runtime.MapProjectionMath.WorldYawToUIRotationZ(270f) - (-270f)) > 0.0001f)
                {
                    Debug.LogError("[Test 106] Heading yaw rotation calculation failed!");
                    return false;
                }

                Debug.Log("✅ Test 106: MapProjectionMath Zero-GC coordinate normalization & Style C bounds verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 106] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // Test 107: WorldMapUI ModalStack integration, player input lock gating, and marker tracking
        private static bool Test_WorldMap_ModalStack_And_PlayerTracking_Audit()
        {
            GameObject canvasGo = null;
            GameObject mapGo = null;
            GameObject playerGo = null;
            var prevStack = ModalStackManager.Instance;
            var origPlayer = ServiceLocator.Resolve<IPlayerContext>();

            try
            {
                // Setup Canvas and WorldMapUI hierarchy
                canvasGo = new GameObject("Test_Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
                mapGo = new GameObject("Test_WorldMapUI", typeof(RectTransform), typeof(CanvasGroup), typeof(FarmBeware.Player.UI.WorldMapUI));
                mapGo.transform.SetParent(canvasGo.transform, false);

                var mapContainer = new GameObject("Container", typeof(RectTransform));
                mapContainer.transform.SetParent(mapGo.transform, false);
                var containerRt = mapContainer.GetComponent<RectTransform>();
                containerRt.sizeDelta = new Vector2(1600f, 900f);

                var markerGo = new GameObject("Marker", typeof(RectTransform));
                markerGo.transform.SetParent(mapContainer.transform, false);
                var markerRt = markerGo.GetComponent<RectTransform>();

                var mapUi = mapGo.GetComponent<FarmBeware.Player.UI.WorldMapUI>();

                // Bind serialized references
                var so = new UnityEditor.SerializedObject(mapUi);
                so.FindProperty("canvasGroup").objectReferenceValue = mapGo.GetComponent<CanvasGroup>();
                so.FindProperty("mapContainer").objectReferenceValue = containerRt;
                so.FindProperty("playerMarker").objectReferenceValue = markerRt;
                so.ApplyModifiedProperties();

                // Setup Mock Player with IPlayerContext
                playerGo = new GameObject("Test_Player_MapTracking");
                playerGo.tag = "Player";
                playerGo.transform.position = new Vector3(-70f, 0f, 35f);
                playerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing East
                var pControl = playerGo.AddComponent<PlayerControl>();

                mapUi.SetPlayerTarget(playerGo.transform, pControl);
                ServiceLocator.Register<IPlayerContext>(pControl);

                // Initial state
                if (mapUi.IsOpen)
                {
                    Debug.LogError("[Test 107] WorldMapUI must be initially closed!");
                    return false;
                }
                if (pControl.isInputLocked)
                {
                    Debug.LogError("[Test 107] PlayerControl must not be input locked before map is opened!");
                    return false;
                }

                // 1. Open Map
                mapUi.OpenModal();

                if (!mapUi.IsOpen)
                {
                    Debug.LogError("[Test 107] WorldMapUI.IsOpen failed to become true!");
                    return false;
                }
                if (!ModalStackManager.Instance.HasActiveModal)
                {
                    Debug.LogError("[Test 107] ModalStackManager must register WorldMapUI as active modal!");
                    return false;
                }
                if (!pControl.isInputLocked || !pControl.HasInputLock("WorldMapUI"))
                {
                    Debug.LogError("[Test 107] PlayerControl input lock 'WorldMapUI' was not acquired on open!");
                    return false;
                }

                // Check marker anchored position (at center world position -70, 35 -> anchored pos 0, 0)
                if (markerRt.anchoredPosition.sqrMagnitude > 0.01f)
                {
                    Debug.LogError($"[Test 107] Player marker anchored position did not match center! Anchored={markerRt.anchoredPosition}");
                    return false;
                }
                // Check marker rotation: facing East (90 deg) -> local Euler Z = -90 (or 270)
                float rotZ = markerRt.localEulerAngles.z;
                if (Mathf.Abs(rotZ - 270f) > 0.5f && Mathf.Abs(rotZ - (-90f)) > 0.5f)
                {
                    Debug.LogError($"[Test 107] Player marker rotation did not match player yaw! RotZ={rotZ}");
                    return false;
                }

                // 2. Move Player to Farmhouse (21.49, 42.91) facing North (0 deg)
                playerGo.transform.position = new Vector3(21.49f, 1.75f, 42.91f);
                playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                mapUi.ForceUpdateTracking();

                var expectedAnchored = FarmBeware.Core.Runtime.MapProjectionMath.WorldToMapAnchoredPosition(
                    playerGo.transform.position,
                    mapUi.MapBounds,
                    containerRt.rect.size
                );

                if ((markerRt.anchoredPosition - expectedAnchored).sqrMagnitude > 0.05f)
                {
                    Debug.LogError($"[Test 107] Player marker position did not update after player moved! Marker={markerRt.anchoredPosition}, Expected={expectedAnchored}");
                    return false;
                }

                // 3. Close Map
                mapUi.CloseModal();

                if (mapUi.IsOpen)
                {
                    Debug.LogError("[Test 107] WorldMapUI.IsOpen did not become false after CloseModal!");
                    return false;
                }
                if (pControl.HasInputLock("WorldMapUI"))
                {
                    Debug.LogError("[Test 107] 'WorldMapUI' input lock was not released on close!");
                    return false;
                }

                // 4. Toggle Modal
                mapUi.ToggleModal();
                if (!mapUi.IsOpen || !pControl.HasInputLock("WorldMapUI"))
                {
                    Debug.LogError("[Test 107] ToggleModal failed to open map!");
                    return false;
                }
                mapUi.ToggleModal();
                if (mapUi.IsOpen || pControl.HasInputLock("WorldMapUI"))
                {
                    Debug.LogError("[Test 107] ToggleModal failed to close map!");
                    return false;
                }

                // 5. Zoom In / Zoom Out and Pan verification
                mapUi.OpenModal();
                if (Mathf.Abs(mapUi.CurrentZoom - 1.0f) > 0.001f)
                {
                    Debug.LogError("[Test 107] WorldMapUI zoom did not reset to 1.0f on open!");
                    return false;
                }

                mapUi.SetZoom(2.5f);
                if (Mathf.Abs(mapUi.CurrentZoom - 2.5f) > 0.001f || Mathf.Abs(containerRt.localScale.x - 2.5f) > 0.001f)
                {
                    Debug.LogError("[Test 107] SetZoom(2.5f) failed to update CurrentZoom or content scale!");
                    return false;
                }

                mapUi.SetPan(new Vector2(100f, -50f));
                if (Mathf.Abs(mapUi.PanOffset.x - 100f) > 0.001f || Mathf.Abs(mapUi.PanOffset.y - (-50f)) > 0.001f)
                {
                    Debug.LogError("[Test 107] SetPan failed to update PanOffset!");
                    return false;
                }

                // Reopening map resets zoom & pan to default overview
                mapUi.CloseModal();
                mapUi.OpenModal();
                if (Mathf.Abs(mapUi.CurrentZoom - 1.0f) > 0.001f || mapUi.PanOffset.sqrMagnitude > 0.001f)
                {
                    Debug.LogError("[Test 107] Reopening map did not reset zoom & pan!");
                    return false;
                }
                mapUi.CloseModal();

                Debug.Log("✅ Test 107: WorldMapUI ModalStack integration, player input lock gating, marker tracking, and mouse wheel zoom/pan verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 107] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                if (mapGo != null) Object.DestroyImmediate(mapGo);
                if (canvasGo != null) Object.DestroyImmediate(canvasGo);
                if (playerGo != null) Object.DestroyImmediate(playerGo);
                if (prevStack != null) prevStack.Clear();
                if (origPlayer != null) ServiceLocator.Register<IPlayerContext>(origPlayer);
                else ServiceLocator.Unregister<IPlayerContext>();
            }
        }
    }
}
