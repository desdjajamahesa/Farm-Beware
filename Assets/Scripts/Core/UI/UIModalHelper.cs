using System.Collections.Generic;
using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Global modal tracking helper and scoped pause-lock coordinator to synchronize
    /// modal dismissal, prevent ESC frame-overlap, and prevent premature Time.timeScale unpauses.
    /// </summary>
    public static class UIModalHelper
    {
        public static int LastFrameUIPanelClosed = -1;
        public static bool IsSaveUIOpen = false;

        private static readonly HashSet<string> _pauseLockSources = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            LastFrameUIPanelClosed = -1;
            IsSaveUIOpen = false;
            _pauseLockSources.Clear();
        }

        /// <summary>
        /// True when at least one system holds an active pause lock on Time.timeScale.
        /// </summary>
        public static bool HasActivePauseLock => _pauseLockSources.Count > 0;

        /// <summary>
        /// Number of distinct systems currently holding a pause lock.
        /// </summary>
        public static int ActivePauseLockCount => _pauseLockSources.Count;

        /// <summary>
        /// Acquires a named pause lock and sets Time.timeScale = 0f.
        /// </summary>
        public static void AddPauseLock(string source)
        {
            if (string.IsNullOrEmpty(source)) source = "GenericPause";
            _pauseLockSources.Add(source);
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Releases a named pause lock and restores Time.timeScale = 1f only when no other pause locks remain.
        /// </summary>
        public static void RemovePauseLock(string source)
        {
            if (string.IsNullOrEmpty(source)) source = "GenericPause";
            _pauseLockSources.Remove(source);
            if (_pauseLockSources.Count == 0)
            {
                Time.timeScale = 1f;
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// Checks whether a specific source currently holds a pause lock.
        /// </summary>
        public static bool HasPauseLock(string source)
        {
            if (string.IsNullOrEmpty(source)) return _pauseLockSources.Count > 0;
            return _pauseLockSources.Contains(source);
        }

        /// <summary>
        /// Clears all active pause locks and restores Time.timeScale = 1f (e.g. on scene transition or new game).
        /// </summary>
        public static void ClearAllPauseLocks()
        {
            _pauseLockSources.Clear();
            Time.timeScale = 1f;
        }
    }
}
