using UnityEngine;

namespace Core
{
    /// <summary>
    /// Global performance and frame pacing manager.
    /// Locks target frame rate to 60 FPS and enables VSync to eliminate micro-stutter,
    /// wild FPS spikes, and thermal GPU throttling during gameplay.
    /// </summary>
    public static class PerformanceManager
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializePerformanceSettings()
        {
            // Lock frame rate to a smooth, consistent 60 FPS
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;

            // Set fixed delta time consistent with 60 Hz physics
            Time.fixedDeltaTime = 0.02f;

            Debug.Log("[PerformanceManager] TargetFrameRate locked to 60 FPS with VSync=1.");
        }
    }
}
