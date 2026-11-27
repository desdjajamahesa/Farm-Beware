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
        internal static void PrepareSingleTestHarness()
        {
            ResetTestAuditHarness();
            CanvasHelper.ClearCache();
        }

        internal static void CleanupTransientFloatingText()
        {
            var fctManager = Object.FindFirstObjectByType<PlayerUI.FloatingCombatTextManager>();
            if (fctManager != null)
            {
                var rt = fctManager.GetComponent<RectTransform>();
                if (rt != null)
                {
                    while (rt.childCount > 10)
                    {
                        Object.DestroyImmediate(rt.GetChild(rt.childCount - 1).gameObject);
                    }
                }
            }
        }

        private static void ResetTestAuditHarness()
        {
            if (ModalStackManager.HasInstance && ModalStackManager.Instance != null)
            {
                ModalStackManager.Instance.Clear();
            }

            FarmBeware.Core.Runtime.GameSessionState.IsPendingNewGameIntroCutscene = false;

            if (FeaturesStoryline.Cutscenes.Night1CutsceneController.HasInstance &&
                FeaturesStoryline.Cutscenes.Night1CutsceneController.Instance != null)
            {
                var n1 = FeaturesStoryline.Cutscenes.Night1CutsceneController.Instance;
                var f = typeof(FeaturesStoryline.Cutscenes.Night1CutsceneController).GetField("_isCutscenePlaying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(n1, false);
            }

            if (FeaturesStoryline.Cutscenes.OpeningCutsceneController.HasInstance &&
                FeaturesStoryline.Cutscenes.OpeningCutsceneController.Instance != null)
            {
                var op = FeaturesStoryline.Cutscenes.OpeningCutsceneController.Instance;
                var f = typeof(FeaturesStoryline.Cutscenes.OpeningCutsceneController).GetField("_isCutscenePlaying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(op, false);
            }

            var singletonTypes = new System.Type[]
            {
                typeof(FeaturesStoryline.Cutscenes.CutsceneLetterboxUI),
                typeof(FeaturesStoryline.Cutscenes.Night1CutsceneController),
                typeof(FeaturesStoryline.Cutscenes.OpeningCutsceneController),
                typeof(FeaturesStoryline.UI.StorylineUI),
                typeof(FeaturesStoryline.UI.BossIntroSequence),
                typeof(FeaturesStoryline.UI.JournalUI),
                typeof(DayTransitionUI),
                typeof(FarmBeware.Features.Quest.QuestManager),
                typeof(FarmBeware.Features.Quest.UI.QuestLogUI)
            };

            foreach (var t in singletonTypes)
            {
                var qf = t.GetField("_isQuitting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                qf?.SetValue(null, false);
            }

            var dayChangedField = typeof(TimeManager).GetField("OnAnyDayChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            dayChangedField?.SetValue(null, null);
            var phaseChangedField = typeof(TimeManager).GetField("OnAnyPhaseChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            phaseChangedField?.SetValue(null, null);
        }

        [MenuItem("FarmBeware/Tests/Run Comprehensive Audit Tests")]
        public static void RunAllTests()
        {
            Debug.Log("[AUDIT-SUITE] ========================================================");
            Debug.Log("[AUDIT-SUITE] === STARTING DEEP AUDIT & AUTOMATED TEST VERIFICATION ===");
            Debug.Log("[AUDIT-SUITE] ========================================================");

            ResetTestAuditHarness();

            int passed = 0;
            int total = 117;

            if (Test_DayNight_PhaseProgression_DuskUntilNight()) passed++;
            if (Test_BedInteractable_Logic_AllowsDuskAndBlocksNightOnly()) passed++;
            if (Test_PlayerWaterBottle_4Charges_FullToEmpty()) passed++;
            if (Test_WaterBottle_CapacityPreservation_100L()) passed++;
            if (Test_KitchenSinkProgressOverlay_VerticalGeometry()) passed++;
            if (Test_StoveUI_CookingProgressBarPermanentlyHidden()) passed++;
            if (Test_CombatPhaseTrackerUI_ProceduralDayAndClock()) passed++;
            if (Test_FarmlandTile_WorldItemPickup_SpawnOnFullInventory()) passed++;
            if (Test_FarmlandPlot_1_NoHighlightOverrides()) passed++;
            if (Test_Hotbar_MouseClick_Selection()) passed++;
            if (Test_AttackTrail_NightOnly()) passed++;
            if (Test_MerchantShopUI_ResetScrollPosition()) passed++;
            if (Test_WorldItemPickup_Protection_FullInventory()) passed++;
            if (Test_PlayerInventory_16Slots_Allocation()) passed++;
            if (Test_PlayerBuff_FastForward8x_DurationAndTickAcceleration()) passed++;
            if (Test_MonsterTacticalEvaluator_EncirclementAndKiting()) passed++;
            if (Test_AttackTokenDispatcher_AnticipatoryAcquisitionAndEviction()) passed++;
            if (Test_NavMesh_ObstaclePathfinding_RoutesAroundFences()) passed++;
            if (Test_BuildFixes_TelegraphDecal_FloorCollider_AndDoorSafety()) passed++;
            if (Test_PlayerBuff_ClearsOnSleepAndDayTransition()) passed++;
            if (Test_DayTransitionUI_DayAndNightBannerAnimation()) passed++;
            if (Test_MultiSourceInputLock_AndTransitionLeakPrevention()) passed++;
            if (Test_EnemyAndBossHealthBar_CanvasResolutionAndVisibilityAudit()) passed++;
            if (Test_NightBrawl_WaveSpawnLeakPrevention_MinionDecoupling_AndBossOrderAudit()) passed++;
            if (Test_PlayerInventory_MultiStack_PlantSeedHotbarConsumptionPriorityAudit()) passed++;
            if (Test_MonsterAttackConsistency_DistanceTokenPriority_TheRangerAndCooldownDecouplingAudit()) passed++;
            if (Test_PlantWatererVisualMount_AndMeleeCombatReachabilityAudit()) passed++;
            if (Test_Storyline_Subsystem_Engine_Progression_And_Persistence()) passed++;
            if (Test_CropDataCorn_AssetIntegrity_And_DualLookup_Audit()) passed++;
            if (Test_ModalStack_CentralizedInputLock_PlayerControl_Audit()) passed++;
            if (Test_Storyline_RuleSpecificity_Determinism_Audit()) passed++;
            if (Test_Storyline_ZeroGC_Allocation_Invariant_Audit()) passed++;
            if (Test_Storyline_ModalSafetyGate_NightBrawl_Audit()) passed++;
            if (Test_Storyline_NonModalBanner_PreservesLocomotion_Audit()) passed++;
            if (Test_Storyline_SavePayload_Persistence_Audit()) passed++;
            if (Test_Storyline_MonteCarlo_Reachability_Audit()) passed++;
            if (Test_OpeningCutscene_Day0_GateArrival_HoldToSkip_And_Day1Sync_Audit()) passed++;
            if (Test_VehicleDrivingSystem_DynamicsAndLifecycleAudit()) passed++;
            if (Test_CutsceneHoldToSkip_InstantTerminationAndCameraSnap_Audit()) passed++;
            if (Test_VehicleEntry_DualChannel_InteractionTrigger_Audit()) passed++;
            if (Test_Storyline_ModalQueue_SerializationAndRestore_Audit()) passed++;
            if (Test_Storyline_NarrativeFactBoard_ResetAndTokenInterpolation_Audit()) passed++;
            if (Test_Storyline_Day5VictoryAndEpilogue_DecoupledModalChain_Audit()) passed++;
            if (Test_OpeningCutscene_GameplayHUD_GuaranteedReactivation_Audit()) passed++;
            if (Test_Day0_Arrival_HUDSuppression_TimeLock_And_BedSleepTransition_Audit()) passed++;
            if (Test_VehicleDriver_Visibility_ErgonomicSeating_And_AnimationState_Audit()) passed++;
            if (Test_OpeningCutscene_VehicleArrival_And_Dismount_Audit()) passed++;
            if (Test_Day0_EmptyInventory_And_Day1_StarterKit_Award_Audit()) passed++;
            if (Test_IntroStoryTimeline_And_ZeroJitterSeating_Audit()) passed++;
            if (Test_Day0_NoHoeHeldAndEmptyInventory_Audit()) passed++;
            if (Test_StorylineUI_DialogueSpeakerAndBodyResolution_Audit()) passed++;
            if (Test_FarmlandTile_NightAndGrowingSuppression_Audit()) passed++;
            if (Test_CropGrowthCountdownUI_NightSuppression_Audit()) passed++;
            if (Test_Fridge_AcceptsSweetPotatoAndAllCrops_Audit()) passed++;
            if (Test_InventorySlotUI_InputLockBypassAndSingleClickTransfer_Audit()) passed++;
            if (Test_DayTransitionUI_ProceduralVignetteSprite_Audit()) passed++;
            if (Test_JournalUI_DocsPanelPersistentButtonAndEntries_Audit()) passed++;
            if (Test_Day0_To_Day1_SleepFadeAndStarterKitTransition_Audit()) passed++;
            if (Test_DaytimeBedSleep_ConfirmationAndTransition_Audit()) passed++;
            if (Test_HUD_NightBrawlWaveTracker_ActiveAndRestored_Audit()) passed++;
            if (Test_Inventory_TabToggle_OpenAndClose_Audit()) passed++;
            if (Test_SideQuest_FirstSteps_Progression_GatherAndPlant()) passed++;
            if (Test_MeleeHitBuffer_Expansion_And_QuestHUD_Lifecycle_Audit()) passed++;
            if (Test_CutsceneTypography_And_Day1PipeDiscoveryTrigger_Audit()) passed++;
            if (Test_Storyline_UpdateLoop_ZeroGC_Profiling_Audit()) passed++;
            if (Test_Storyline_SandboxPlayableLoop_E2E_Audit()) passed++;
            if (Test_PlayerV2_CharacterAsset_NativeAnimations_And_Avatar_Audit()) passed++;
            if (Test_DummySword_Redesign_And_HandSocket_Alignment_Audit()) passed++;
            if (Test_AttackAnimationPacing_ControllerSpeeds_And_HitboxTiming_Audit()) passed++;
            if (Test_VehicleDrivingHUD_Suppression_And_KinematicEnemySafety_Audit()) passed++;
            if (Test_Day1_DayToNight_CutsceneAndStorylineDeconfliction_Audit()) passed++;
            if (Test_OpeningCutscene_DialogueSuppression_And_Night1CamClearance_Audit()) passed++;
            if (Test_Day1_StorageChestStarterKit_And_SideQuestRetrieve_Audit()) passed++;
            if (Test_CookingQuest_Progression_And_Double_Click_Audit()) passed++;
            if (Test_ShopInteractable_ComponentInstantiation_And_StockInitialization()) passed++;
            if (Test_ShopInteractable_DayProgressionGating_And_MerchantShopUI_Activation()) passed++;
            if (Test_PlayerWallet_TransactionIntegrity_BuyDeduction_SellIncrement_And_FullInventoryRefund()) passed++;
            if (Test_DailyEconomyManager_DailyPriceRolling_DirtyDiscount_And_DayTransitionSnapshot()) passed++;
            if (Test_TradingPost_Complex_Architecture_Clearances_And_ShopCounter_Audit()) passed++;
            if (Test_WeaponUpgrade_Level1To10_DamageScalingAndCost()) passed++;
            if (Test_ArmorUpgrade_Level1To10_MitigationProgression()) passed++;
            if (Test_SpecializationTree_SinglePathExclusivity_And_Progression()) passed++;
            if (Test_SweetPotatoCombatSet_BonusEvaluation()) passed++;
            if (Test_EquipmentSlotUI_DragAndDropEquip_And_Unequip()) passed++;
            if (Test_Workbench_SaveLoadHydration_FullStatePreservation()) passed++;

            if (Test_ContextualCombatStance_And_NightHotbarRestrictions()) passed++;
            if (Test_CombatKinesthetics_ImpactTierScaling_ParryRiposte_And_DodgeStrike()) passed++;
            if (Test_EnvironmentalWallSplat_And_BranchingHeavyFinishers()) passed++;
            if (Test_CornCobRepeater_RangedWeaponAsset_And_CornSpecScaling()) passed++;
            if (Test_BioCatalystNightOverdrive_CropGrowthAndLootEscalation()) passed++;
            if (Test_WeaponsAndArmor_CompleteAccessibility_CraftingAndPurchasing_Audit()) passed++;
            if (Test_Vehicle_InteractionRangeTuned_And_InputBlockedDuringCutscene_Audit()) passed++;
            if (Test_StarterKit_DoesNotContainCombatArmor_Audit()) passed++;
            if (Test_BossCore_DropRate100Percent_Guaranteed_Audit()) passed++;
            if (Test_MonsterLootTables_ContainSeedDrops_Audit()) passed++;
            if (Test_KitchenRecipe_TaroCornStew_CostCalibration_Audit()) passed++;
            if (Test_WaterBottle_ShopRepurchasePrice_Audit()) passed++;
            if (Test_EquipmentOwnershipGating_BeforeUpgrade_Audit()) passed++;
            if (Test_CornSpecialization_Node1_AwardsHomesteadSlingshot_And_SetBonus6Piece_Audit()) passed++;
            if (Test_Farmland_Day1CapacityAndWorkbenchExpansion_Audit()) passed++;
            if (Test_Axis1_EconomyMonteCarloSolvency_And_LandDeedROI_Audit()) passed++;
            if (Test_Axis2_CombatKinematics_ComboBranching_PoiseImpulse_And_SkillTokenGating_Audit()) passed++;
            if (Test_Axis3_InteractionEdges_SwingCancelCleanup_And_NightVehicleAutoDismount_Audit()) passed++;
            if (Test_Axis4_ZeroGC_TickInvariants_CountdownCache_And_CutsceneHasInstance_Audit()) passed++;
            if (Test_PlaytestAudit_11Bugs_RegressionSuite()) passed++;
            if (Test_WorldMap_ProjectionMath_ZeroGC_And_StyleCBoundaryCalculations()) passed++;
            if (Test_WorldMap_ModalStack_And_PlayerTracking_Audit()) passed++;
            if (Test_MeleeAttack_LineOfSight_ObstacleOcclusion_Audit()) passed++;
            if (Test_PlaytestFeedback_11CriticalFixes_Audit()) passed++;
            if (Test_ExplicitStateMachines_PauseLock_RefillFSM_And_ActionHygiene_Audit()) passed++;
            if (Test_SystemArchitecture_AssetDeduplication_SharedProceduralCache_And_CatalogUnification_Audit()) passed++;
            if (Test_BoardOrders_GDDDirtyCleanPricing_Multiplier12x_Delivery_And_ScenePlacement_Audit()) passed++;
            if (Test_QuestLogUI_Aggregation_Progress_FinishUnfinishFilters_And_ZeroGC_Audit()) passed++;
            if (Test_QAHub_ComprehensiveBugFixes_Audit()) passed++;
            if (Test_PlayerModular_CombatOverhaul_HeavyChain_RecoveryHold_ActionCancels_And_ZeroWarnings_Audit()) passed++;
            if (Test_WoodenBoardPanels_TwoColumnThemeAndHierarchy_Audit()) passed++;
            if (Test_ArchitectureHardening_ZeroSendMessage_ZeroEmptyCatch_And_Contracts_Audit()) passed++;

            Debug.Log("[AUDIT-SUITE] ========================================================");
            Debug.Log($"[AUDIT-SUITE] Complete! Passed: {passed}/{total}");
            if (passed == total)
            {
                Debug.Log($"✅ [AUDIT-SUITE] ALL {total} COMPREHENSIVE AUDIT TESTS PASSED SUCCESSFULLY!");
            }
            else
            {
                Debug.LogError($"❌ [AUDIT-SUITE] {total - passed} AUDIT TESTS FAILED!");
            }
            Debug.Log("[AUDIT-SUITE] ========================================================");

            // Post-run scene cleanup: trim transient floating combat texts spawned during testing
            CleanupTransientFloatingText();
        }

        #region Shared Test Mocks
        private class MockTimeService : ITimeService
        {
            public int CurrentDay { get; set; } = 1;
            public float TimeOfDay { get; set; } = 16.0f;
            public float CurrentHour { get; set; } = 16.0f;
            public DayPhase CurrentPhase { get; set; } = DayPhase.Dusk;
            public bool IsNight => CurrentPhase == DayPhase.Night;
            public bool IsNightEncounterCleared { get; set; } = false;
            public bool UseContinuousTime => true;
#pragma warning disable CS0067
            public event System.Action<int> OnMinuteChanged;
            public event System.Action<int, DayPhase> OnPhaseChanged;
            public event System.Action<int> OnDayChanged;
#pragma warning restore CS0067
            public void StartNightPhase() { CurrentPhase = DayPhase.Night; }
            public void AdvanceToNextDay() { CurrentDay++; CurrentPhase = DayPhase.Day; }
        }

        private class MockKitchenStation : KitchenStation
        {
            protected override KitchenRecipe FindRecipeFor(ItemData item, int slotIndex) => null;
        }

        private class TestAuditModal : IModalWindow
        {
            public bool IsOpen { get; set; } = true;
            public void OpenModal() { IsOpen = true; }
            public void CloseModal() { IsOpen = false; }
        }

        private class MockCameraService : ICameraService
        {
            public CameraMode CurrentMode { get; private set; } = CameraMode.Gameplay;
            public Camera MainCamera => null;
            public Camera TrophyCamera => null;
            public Camera WardrobeCamera => null;
            public void SetMode(CameraMode mode, Transform contextRoot = null)
            {
                CurrentMode = mode;
            }
            public void PositionPlayerBehindTrophyCamera() { }
        }

        private class MockVehicleCutsceneService : ICutsceneService
        {
            private bool _isPlaying = false;
            public bool IsCutscenePlaying => _isPlaying;
            public event System.Action OnCutsceneStarted;
            public event System.Action OnCutsceneEnded;

            public void SetPlaying(bool playing)
            {
                _isPlaying = playing;
                if (playing) OnCutsceneStarted?.Invoke();
                else OnCutsceneEnded?.Invoke();
            }

            public void PlayOpeningCutscene(System.Action onComplete)
            {
                SetPlaying(true);
                onComplete?.Invoke();
                SetPlaying(false);
            }

            public void PlayNight1Cutscene(System.Action onComplete)
            {
                SetPlaying(true);
                onComplete?.Invoke();
                SetPlaying(false);
            }

            public void SkipCurrentCutscene()
            {
                SetPlaying(false);
            }
        }

        // Test 116: WoodenBoard_TwoColumn sprite, normalized layout regions, and Tier 1 + Tier 2 panel theme contracts
        internal static bool Test_WoodenBoardPanels_TwoColumnThemeAndHierarchy_Audit()
        {
            var cleanup = new List<GameObject>();
            var renamedOriginals = new List<(Transform tr, string origName)>();
            GameObject uiCanvasGO = null;
            bool hadCanvas = false;
            bool canvasWasActive = true;
            var prevJournalInst = typeof(JournalUI).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);
            var prevSaveInst = typeof(FeaturesSaveSystem.SaveSystemUI).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);

            try
            {
                // 1. Verify sprite resolution & non-overlapping two-column layout regions
                Sprite boardSprite = WoodenBoardStyle.GetTwoColumnBoardSprite();
                if (boardSprite == null)
                {
                    Debug.LogError("[Test 116] WoodenBoardStyle.GetTwoColumnBoardSprite() returned null!");
                    return false;
                }

                if (WoodenBoardStyle.LeftPaneMax.x >= WoodenBoardStyle.RightPaneMin.x)
                {
                    Debug.LogError($"[Test 116] Left/Right panes overlap! LeftMaxX={WoodenBoardStyle.LeftPaneMax.x}, RightMinX={WoodenBoardStyle.RightPaneMin.x}");
                    return false;
                }

                // 2. Verify Canvas host & preserve any pre-existing scene panels
                var resolvedCanvas = CanvasHelper.ResolveMainUICanvas();
                uiCanvasGO = resolvedCanvas != null ? resolvedCanvas.gameObject : GameObject.Find("UI_Canvas");
                if (uiCanvasGO == null)
                {
                    uiCanvasGO = new GameObject("UI_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    uiCanvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    cleanup.Add(uiCanvasGO);
                }
                else
                {
                    hadCanvas = true;
                    canvasWasActive = uiCanvasGO.activeSelf;
                    if (!canvasWasActive) uiCanvasGO.SetActive(true);
                }

                string[] panelNames = { "JournalPanel", "Panel_QuestLog", "Panel_MerchantShop", "Panel_OrderBoard", "Panel_SavingSystem" };
                foreach (var pName in panelNames)
                {
                    var pre = uiCanvasGO.transform.Find(pName);
                    if (pre != null)
                    {
                        renamedOriginals.Add((pre, pName));
                        pre.name = "__PreExisting_" + pName;
                    }
                }

                // A. StoveUIManager.ApplyWoodenBoardTheme()
                var stoveHost = new GameObject("Test_StoveHost_116", typeof(RectTransform));
                cleanup.Add(stoveHost);
                var stovePanel = new GameObject("Test_StovePanel_116", typeof(RectTransform), typeof(Image));
                stovePanel.transform.SetParent(stoveHost.transform, false);
                var stoveUI = stoveHost.AddComponent<StoveUIManager>();
                typeof(StoveUIManager).GetField("panelStove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(stoveUI, stovePanel);
                stoveUI.ApplyWoodenBoardTheme();
                if (stovePanel.GetComponent<Image>().sprite != boardSprite || stovePanel.transform.Find("PlaqueTitleText") == null)
                {
                    Debug.LogError("[Test 116] StoveUIManager did not apply WoodenBoard_TwoColumn sprite & PlaqueTitleText!");
                    return false;
                }

                // B. WorkbenchUI
                var wbHost = new GameObject("Test_Workbench_116", typeof(RectTransform));
                cleanup.Add(wbHost);
                var prevWb = FeaturesWorkbench.UI.WorkbenchUI.Instance;
                try
                {
                    typeof(FeaturesWorkbench.UI.WorkbenchUI).GetProperty("Instance")?.SetValue(null, null);
                    var wbComp = wbHost.AddComponent<FeaturesWorkbench.UI.WorkbenchUI>();
                    typeof(FeaturesWorkbench.UI.WorkbenchUI)
                        .GetMethod("BuildUIHierarchyIfNeeded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        ?.Invoke(wbComp, null);
                    var wbDialogImg = wbHost.transform.Find("WorkbenchRoot/DialogBorder/Dialog")?.GetComponent<Image>();
                    if (wbDialogImg == null || wbDialogImg.sprite != boardSprite || wbDialogImg.transform.Find("PlaqueTitleText") == null)
                    {
                        Debug.LogError("[Test 116] WorkbenchUI did not apply WoodenBoard_TwoColumn sprite & PlaqueTitleText!");
                        return false;
                    }
                }
                finally
                {
                    typeof(FeaturesWorkbench.UI.WorkbenchUI).GetProperty("Instance")?.SetValue(null, prevWb);
                }

                // C. JournalUI
                typeof(JournalUI).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);
                var journalHost = new GameObject("Test_Journal_116");
                cleanup.Add(journalHost);
                var journalUI = journalHost.AddComponent<JournalUI>();
                journalUI.EnsureUIHierarchy();
                var journalPanelTr = uiCanvasGO.transform.Find("JournalPanel");
                if (journalPanelTr != null) cleanup.Add(journalPanelTr.gameObject);
                var journalImg = journalPanelTr?.GetComponent<Image>();
                if (journalImg == null || journalImg.sprite != boardSprite || journalPanelTr.Find("EntryListPane/EntryScrollView/Viewport/EntryListContent") == null)
                {
                    Debug.LogError($"[Test 116] JournalUI failed: panel={(journalPanelTr != null)}, spriteMatch={(journalImg != null && journalImg.sprite == boardSprite)}, list={(journalPanelTr != null && journalPanelTr.Find("EntryListPane/EntryScrollView/Viewport/EntryListContent") != null)}");
                    return false;
                }

                // D. QuestLogUI
                var qlHost = new GameObject("Test_QuestLog_116");
                cleanup.Add(qlHost);
                var qlUI = qlHost.AddComponent<QuestLogUI>();
                qlUI.EnsureUIHierarchy();
                var qlRoot = uiCanvasGO.transform.Find("Panel_QuestLog");
                if (qlRoot != null) cleanup.Add(qlRoot.gameObject);
                var qlCardImg = qlRoot?.Find("QuestLogModalCard")?.GetComponent<Image>();
                if (qlCardImg == null || qlCardImg.sprite != boardSprite || qlRoot.Find("QuestLogModalCard/QuestScrollView/Viewport/QuestsContent") == null)
                {
                    Debug.LogError("[Test 116] QuestLogUI did not preserve QuestScrollView path with wooden board sprite!");
                    return false;
                }

                // E. MerchantShopUI
                var shopHost = new GameObject("Test_Shop_116");
                cleanup.Add(shopHost);
                var shopUI = shopHost.AddComponent<MerchantShopUI>();
                shopUI.EnsureUIHierarchy();
                var shopRoot = uiCanvasGO.transform.Find("Panel_MerchantShop");
                if (shopRoot != null) cleanup.Add(shopRoot.gameObject);
                var shopCardImg = shopRoot?.Find("ShopModalCard")?.GetComponent<Image>();
                if (shopCardImg == null || shopCardImg.sprite != boardSprite || shopRoot.Find("ShopModalCard/CategoryFilterBar") == null)
                {
                    Debug.LogError("[Test 116] MerchantShopUI did not construct wooden two-column board!");
                    return false;
                }

                // F. OrderBoardUI
                var obHost = new GameObject("Test_OrderBoard_116");
                cleanup.Add(obHost);
                var obUI = obHost.AddComponent<OrderBoardUI>();
                obUI.EnsureUIHierarchy();
                var obRoot = uiCanvasGO.transform.Find("Panel_OrderBoard");
                if (obRoot != null) cleanup.Add(obRoot.gameObject);
                var obCardImg = obRoot?.Find("OrderBoardModalCard")?.GetComponent<Image>();
                if (obCardImg == null || obCardImg.sprite != boardSprite || obRoot.Find("OrderBoardModalCard/OrderScrollView/Viewport/OrdersContent") == null)
                {
                    Debug.LogError("[Test 116] OrderBoardUI did not construct wooden two-column board!");
                    return false;
                }

                // G. SaveSystemUI
                typeof(FeaturesSaveSystem.SaveSystemUI).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);
                var saveHost = new GameObject("Test_SaveSystem_116");
                cleanup.Add(saveHost);
                var saveUI = saveHost.AddComponent<FeaturesSaveSystem.SaveSystemUI>();
                typeof(FeaturesSaveSystem.SaveSystemUI)
                    .GetMethod("EnsureUIHierarchy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(saveUI, null);
                var saveRoot = uiCanvasGO.transform.Find("Panel_SavingSystem");
                if (saveRoot != null) cleanup.Add(saveRoot.gameObject);
                var saveCardImg = saveRoot?.Find("SaveModalCard")?.GetComponent<Image>();
                if (saveCardImg == null || saveCardImg.sprite != boardSprite || saveRoot.Find("SaveModalCard/NewSaveSection") == null)
                {
                    Debug.LogError($"[Test 116] SaveSystemUI failed: root={(saveRoot != null)}, spriteMatch={(saveCardImg != null && saveCardImg.sprite == boardSprite)}, newSave={(saveRoot != null && saveRoot.Find("SaveModalCard/NewSaveSection") != null)}");
                    return false;
                }

                Debug.Log("✅ Test 116: WoodenBoard_TwoColumn sprite, normalized layout regions, and all 7 UI panel themes verified.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Test 116] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                for (int i = cleanup.Count - 1; i >= 0; i--)
                {
                    if (cleanup[i] != null) Object.DestroyImmediate(cleanup[i]);
                }
                for (int i = 0; i < renamedOriginals.Count; i++)
                {
                    if (renamedOriginals[i].tr != null) renamedOriginals[i].tr.name = renamedOriginals[i].origName;
                }
                if (hadCanvas && uiCanvasGO != null && !canvasWasActive) uiCanvasGO.SetActive(false);
                typeof(JournalUI).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, prevJournalInst);
                typeof(FeaturesSaveSystem.SaveSystemUI).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, prevSaveInst);
            }
        }
        #endregion
    }
}
