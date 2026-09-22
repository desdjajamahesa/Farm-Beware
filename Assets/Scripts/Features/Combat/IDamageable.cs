using UnityEngine;

namespace FeaturesCombat
{
    /// <summary>
    /// Interface standar untuk semua entitas yang dapat menerima luka fisik/damage dalam combat (Pemain, Musuh, Boss).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Menerima damage dengan informasi posisi dan arah benturan untuk efek knockback/partikel.
        /// </summary>
        void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection);

        /// <summary>
        /// Status apakah entitas sudah mati/hancur.
        /// </summary>
        bool IsDead { get; }
    }
}
