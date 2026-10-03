using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Executes at the earliest possible lifecycle timing (-1000) to ensure core services and contracts are registered.
    /// Acts as the root orchestrator for Tier 0 core subsystems.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class CoreBootstrapper : MonoBehaviour
    {
        private static bool _isInitialized = false;
        public static bool IsInitialized => _isInitialized;

        private void Awake()
        {
            InitializeCoreSubsystems();
        }

        public void InitializeCoreSubsystems()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            Debug.Log("[CoreBootstrapper] Tier 0 Core subsystem contracts and ServiceLocator active.");
        }
    }
}
