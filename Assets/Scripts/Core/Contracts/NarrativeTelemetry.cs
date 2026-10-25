using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Decoupled narrative telemetry event bus in Core.Runtime.
    /// Preserves acyclic assembly boundaries by allowing external subsystems
    /// (Workbench, Environment, House triggers) to publish gameplay telemetry
    /// without direct asmdef coupling to the Storyline module.
    /// </summary>
    public static class NarrativeTelemetry
    {
        /// <summary>Fired when the player enters or exits a safe house interior.</summary>
        public static event Action<bool> OnPlayerSafeZoneChanged;

        /// <summary>Fired when weapon upgrades or special biochemical paths change.</summary>
        public static event Action<int, bool, bool, bool> OnWeaponUpgraded;

        /// <summary>Reports change in safe house interior state.</summary>
        public static void ReportSafeZoneChanged(bool isInside)
        {
            OnPlayerSafeZoneChanged?.Invoke(isInside);
        }

        /// <summary>Reports weapon upgrade and path unlocks.</summary>
        public static void ReportWeaponUpgraded(int weaponLevel, bool sweetPotato, bool taro, bool corn)
        {
            OnWeaponUpgraded?.Invoke(weaponLevel, sweetPotato, taro, corn);
        }

        /// <summary>Fired when a crop is washed in the kitchen sink.</summary>
        public static event Action<string, int> OnCropWashed;

        /// <summary>Reports crop washing at the kitchen sink.</summary>
        public static void ReportCropWashed(string cropName, int count)
        {
            OnCropWashed?.Invoke(cropName, count);
        }
    }
}
