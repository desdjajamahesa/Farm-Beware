using UnityEngine;

namespace FeaturesCombat
{
    /// <summary>
    /// Standard interface for all entities that can take physical damage in combat (Player, Enemy, Boss).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies damage with hit point and impact direction for knockback/particle effects.
        /// </summary>
        void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection);

        /// <summary>
        /// Whether the entity is dead or destroyed.
        /// </summary>
        bool IsDead { get; }
    }
}
