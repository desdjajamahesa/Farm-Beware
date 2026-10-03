using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Lightweight service locator bridge allowing combat classes to dispatch floating text
    /// without coupling to the concrete PlayerUI FloatingCombatTextManager.
    /// </summary>
    public static class FloatingCombatTextManager
    {
        public static IFloatingTextService Instance => ServiceLocator.Resolve<IFloatingTextService>();
    }
}
