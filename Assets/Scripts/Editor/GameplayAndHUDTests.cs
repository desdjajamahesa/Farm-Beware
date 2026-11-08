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
                typeof(FarmBeware.Features.Quest.QuestManager)
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
            int total = 108;

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
        #endregion
    }
}
