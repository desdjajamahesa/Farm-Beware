using UnityEngine;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# POCO Attack Token Dispatcher for horde crowd management.
    /// Allocates active attack permits (default: 3 Melee, 2 Ranged) to prevent simultaneous
    /// uncoordinated dogpiling, and provides automatic expiration reclamation to prevent deadlocks.
    /// </summary>
    public class AttackTokenDispatcher
    {
        public const int DEFAULT_MAX_MELEE_TOKENS = 3;
        public const int DEFAULT_MAX_RANGED_TOKENS = 2;

        private readonly int[] _meleeOwners;
        private readonly float[] _meleeExpirations;

        private readonly int[] _rangedOwners;
        private readonly float[] _rangedExpirations;

        public int MaxMeleeTokens => _meleeOwners.Length;
        public int MaxRangedTokens => _rangedOwners.Length;

        public AttackTokenDispatcher(
            int maxMeleeTokens = DEFAULT_MAX_MELEE_TOKENS,
            int maxRangedTokens = DEFAULT_MAX_RANGED_TOKENS)
        {
            _meleeOwners = new int[maxMeleeTokens > 0 ? maxMeleeTokens : 1];
            _meleeExpirations = new float[_meleeOwners.Length];

            _rangedOwners = new int[maxRangedTokens > 0 ? maxRangedTokens : 1];
            _rangedExpirations = new float[_rangedOwners.Length];
        }

        /// <summary>
        /// Attempts to acquire an attack token. If all tokens are occupied, checks for expired tokens to reclaim.
        /// </summary>
        public bool TryAcquireToken(int entityId, bool isRanged, float currentTime, float durationSec = 3.0f)
        {
            if (entityId == 0) return false;

            int[] owners = isRanged ? _rangedOwners : _meleeOwners;
            float[] expirations = isRanged ? _rangedExpirations : _meleeExpirations;

            // 1. If entity already holds a token, refresh duration
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId)
                {
                    expirations[i] = currentTime + durationSec;
                    return true;
                }
            }

            // 2. Find an empty or expired slot
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == 0 || currentTime >= expirations[i])
                {
                    owners[i] = entityId;
                    expirations[i] = currentTime + durationSec;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Releases an attack token held by the entity.
        /// </summary>
        public void ReleaseToken(int entityId, bool isRanged)
        {
            if (entityId == 0) return;

            int[] owners = isRanged ? _rangedOwners : _meleeOwners;
            float[] expirations = isRanged ? _rangedExpirations : _meleeExpirations;

            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId)
                {
                    owners[i] = 0;
                    expirations[i] = 0f;
                    break;
                }
            }
        }

        /// <summary>
        /// Releases all tokens currently assigned to the entity across both melee and ranged pools.
        /// </summary>
        public void ReleaseAllForEntity(int entityId)
        {
            if (entityId == 0) return;
            ReleaseToken(entityId, false);
            ReleaseToken(entityId, true);
        }

        /// <summary>
        /// Resets all tokens back to available.
        /// </summary>
        public void ResetAll()
        {
            for (int i = 0; i < _meleeOwners.Length; i++)
            {
                _meleeOwners[i] = 0;
                _meleeExpirations[i] = 0f;
            }
            for (int i = 0; i < _rangedOwners.Length; i++)
            {
                _rangedOwners[i] = 0;
                _rangedExpirations[i] = 0f;
            }
        }
    }
}
