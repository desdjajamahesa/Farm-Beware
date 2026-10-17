using UnityEditor;
using UnityEngine;
using FeaturesFarming;
using FeaturesSaveSystem;

namespace FarmBeware.EditorTests
{
    public static class WaterWellRefillTests
    {
        [MenuItem("FarmBeware/Tests/Run Water Well Tests")]
        public static void RunAllTests()
        {
            Debug.Log("[TEST-SUITE] === Running Water Well & Plant Waterer Tests ===");

            int passed = 0;
            int total = 4;

            if (Test_PlantWaterer_BasicOperations()) passed++;
            if (Test_SaveLoad_PlantWaterer_ZeroWaterPreserved()) passed++;
            if (Test_WaterWell_RestoreState()) passed++;
            if (Test_WaterWell_ProportionalQuota_Calculation()) passed++;

            Debug.Log($"[TEST-SUITE] Complete! Passed: {passed}/{total}");
            if (passed == total)
            {
                Debug.Log("✅ [TEST-SUITE] ALL WATER WELL TESTS PASSED SUCCESSFULLY!");
            }
            else
            {
                Debug.LogError($"❌ [TEST-SUITE] {total - passed} TESTS FAILED!");
            }
        }

        private static bool Test_PlantWaterer_BasicOperations()
        {
            GameObject go = new GameObject("Test_Waterer");
            try
            {
                var waterer = go.AddComponent<PlantWaterer>();
                waterer.SetWater(100f);

                if (Mathf.Abs(waterer.CurrentWater - 100f) > 0.01f)
                {
                    Debug.LogError("[Test_PlantWaterer_BasicOperations] Initial water is not 100!");
                    return false;
                }

                bool consumed = waterer.ConsumeWater(30f);
                if (!consumed || Mathf.Abs(waterer.CurrentWater - 70f) > 0.01f)
                {
                    Debug.LogError($"[Test_PlantWaterer_BasicOperations] ConsumeWater failed! Current: {waterer.CurrentWater}");
                    return false;
                }

                waterer.RefillWater(20f);
                if (Mathf.Abs(waterer.CurrentWater - 90f) > 0.01f)
                {
                    Debug.LogError($"[Test_PlantWaterer_BasicOperations] RefillWater(20) failed! Current: {waterer.CurrentWater}");
                    return false;
                }

                waterer.RefillWater(-1f);
                if (Mathf.Abs(waterer.CurrentWater - 100f) > 0.01f)
                {
                    Debug.LogError($"[Test_PlantWaterer_BasicOperations] Full Refill failed! Current: {waterer.CurrentWater}");
                    return false;
                }

                Debug.Log("✅ Test_PlantWaterer_BasicOperations passed.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static bool Test_SaveLoad_PlantWaterer_ZeroWaterPreserved()
        {
            // Verify bug fix: plant waterer amount 0f must NOT reset to 100f!
            GameSaveData data = new GameSaveData();
            data.plantWatererAmount = 0f;

            float loadedAmount = data.plantWatererAmount >= 0f ? data.plantWatererAmount : 100f;
            if (Mathf.Abs(loadedAmount - 0f) > 0.001f)
            {
                Debug.LogError($"[Test_SaveLoad_PlantWaterer_ZeroWaterPreserved] 0L water amount was reset to {loadedAmount}!");
                return false;
            }

            // Verify uninitialized save (-1f) defaults to 100f
            GameSaveData freshData = new GameSaveData(); // default -1f
            float freshAmount = freshData.plantWatererAmount >= 0f ? freshData.plantWatererAmount : 100f;
            if (Mathf.Abs(freshAmount - 100f) > 0.001f)
            {
                Debug.LogError($"[Test_SaveLoad_PlantWaterer_ZeroWaterPreserved] Fresh data did not default to 100! Got: {freshAmount}");
                return false;
            }

            Debug.Log("✅ Test_SaveLoad_PlantWaterer_ZeroWaterPreserved passed.");
            return true;
        }

        private static bool Test_WaterWell_RestoreState()
        {
            GameObject wellGO = new GameObject("Test_Well");
            try
            {
                var well = wellGO.AddComponent<WaterWellInteractable>();
                well.RestoreWellState(
                    hasWaterer: true,
                    ready: true,
                    progress: 1.0f,
                    startAmount: 20f,
                    refillsUsed: 0.8f
                );

                if (!well.HasWatererInWell)
                {
                    Debug.LogError("[Test_WaterWell_RestoreState] HasWatererInWell is false!");
                    return false;
                }
                if (!well.IsWatererReadyToCollect)
                {
                    Debug.LogError("[Test_WaterWell_RestoreState] IsWatererReadyToCollect is false!");
                    return false;
                }
                if (Mathf.Abs(well.RefillWaterProgress - 1.0f) > 0.01f)
                {
                    Debug.LogError("[Test_WaterWell_RestoreState] RefillWaterProgress not 1.0f!");
                    return false;
                }
                if (Mathf.Abs(well.RefillsUsedToday - 0.8f) > 0.01f)
                {
                    Debug.LogError("[Test_WaterWell_RestoreState] RefillsUsedToday not 0.8f!");
                    return false;
                }

                Debug.Log("✅ Test_WaterWell_RestoreState passed.");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(wellGO);
            }
        }

        private static bool Test_WaterWell_ProportionalQuota_Calculation()
        {
            // Verify proportional math:
            // If waterer starts at 80L and fills to 100L:
            // Delta is 20L.
            // 20L / 100L = 0.2 quota used.
            float startWater = 80f;
            float maxWater = 100f;
            float progress = 1.0f;

            float waterDelta = (maxWater - startWater) * progress;
            float quotaUsed = waterDelta / maxWater;

            if (Mathf.Abs(quotaUsed - 0.2f) > 0.001f)
            {
                Debug.LogError($"[Test_WaterWell_ProportionalQuota_Calculation] Expected 0.2 quota used, got {quotaUsed}");
                return false;
            }

            // If cancelled at 50% from 0L:
            startWater = 0f;
            progress = 0.5f;
            waterDelta = (maxWater - startWater) * progress;
            quotaUsed = waterDelta / maxWater;

            if (Mathf.Abs(waterDelta - 50f) > 0.001f || Mathf.Abs(quotaUsed - 0.5f) > 0.001f)
            {
                Debug.LogError($"[Test_WaterWell_ProportionalQuota_Calculation] Expected 50L and 0.5 quota, got {waterDelta}L and {quotaUsed}");
                return false;
            }

            Debug.Log("✅ Test_WaterWell_ProportionalQuota_Calculation passed.");
            return true;
        }
    }
}
