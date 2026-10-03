using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining dynamic world-space combat and notification floating text rendering.
    /// </summary>
    public interface IFloatingTextService
    {
        void SpawnText(Vector3 position, string message, Color color);
        void SpawnEnemyDamage(Vector3 position, float damage, bool isCrit = false, bool isSkill = false);
    }
}
