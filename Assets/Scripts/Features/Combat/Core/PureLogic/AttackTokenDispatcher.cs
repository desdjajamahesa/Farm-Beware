using UnityEngine;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Priority tier for attack token allocation, ensuring Bosses and Elites are never starved by minions.
    /// </summary>
    public enum AttackTokenPriority
    {
        Minion = 0,
        Elite = 1,
        Boss = 2
    }

    /// <summary>
    /// Pure C# POCO Attack Token Dispatcher for horde crowd management.
    /// Allocates active attack permits (default: 3 Melee, 2 Ranged) to prevent simultaneous
    /// uncoordinated dogpiling, and provides automatic expiration reclamation and priority eviction
    /// to prevent deadlocks and boss starvation.
    /// </summary>
    public class AttackTokenDispatcher
    {
        public const int DEFAULT_MAX_MELEE_TOKENS = 3;
        public const int DEFAULT_MAX_RANGED_TOKENS = 2;

        private readonly int[] _meleeOwners;
        private readonly float[] _meleeExpirations;
        private readonly AttackTokenPriority[] _meleePriorities;

        private readonly int[] _rangedOwners;
        private readonly float[] _rangedExpirations;
        private readonly AttackTokenPriority[] _rangedPriorities;

        public int MaxMeleeTokens => _meleeOwners.Length;
        public int MaxRangedTokens => _rangedOwners.Length;

        public AttackTokenDispatcher(
            int maxMeleeTokens = DEFAULT_MAX_MELEE_TOKENS,
            int maxRangedTokens = DEFAULT_MAX_RANGED_TOKENS)
        {
            _meleeOwners = new int[maxMeleeTokens > 0 ? maxMeleeTokens : 1];
            _meleeExpirations = new float[_meleeOwners.Length];
            _meleePriorities = new AttackTokenPriority[_meleeOwners.Length];

            _rangedOwners = new int[maxRangedTokens > 0 ? maxRangedTokens : 1];
            _rangedExpirations = new float[_rangedOwners.Length];
            _rangedPriorities = new AttackTokenPriority[_rangedOwners.Length];
        }

        /// <summary>
        /// Attempts to acquire an attack token with priority awareness.
        /// Higher-priority entities (Elite, Boss) can evict lower-priority tokens if all slots are occupied.
        /// </summary>
        public bool TryAcquireToken(
            int entityId,
            bool isRanged,
            float currentTime,
            float durationSec = 3.0f,
            AttackTokenPriority priority = AttackTokenPriority.Minion)
        {
            if (entityId == 0) return false;

            int[] owners = isRanged ? _rangedOwners : _meleeOwners;
            float[] expirations = isRanged ? _rangedExpirations : _meleeExpirations;
            AttackTokenPriority[] priorities = isRanged ? _rangedPriorities : _meleePriorities;

            // 1. If entity already holds a token, refresh duration and upgrade priority if applicable
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId)
                {
                    expirations[i] = currentTime + durationSec;
                    if (priority > priorities[i]) priorities[i] = priority;
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
                    priorities[i] = priority;
                    return true;
                }
            }

            // 3. Priority Eviction: If all slots are occupied, higher priority entities (Boss/Elite)
            // can evict an active token held by a lower priority entity (e.g., Minion).
            if (priority > AttackTokenPriority.Minion)
            {
                int evictIndex = -1;
                AttackTokenPriority lowestPriority = priority;
                float oldestRemainingTime = float.MaxValue;

                for (int i = 0; i < owners.Length; i++)
                {
                    if (priorities[i] < lowestPriority)
                    {
                        lowestPriority = priorities[i];
                        evictIndex = i;
                        oldestRemainingTime = expirations[i] - currentTime;
                    }
                    else if (priorities[i] == lowestPriority && evictIndex != -1)
                    {
                        // Among candidates with lowest priority, pick the one closest to expiry
                        float remaining = expirations[i] - currentTime;
                        if (remaining < oldestRemainingTime)
                        {
                            oldestRemainingTime = remaining;
                            evictIndex = i;
                        }
                    }
                }

                if (evictIndex != -1)
                {
                    owners[evictIndex] = entityId;
                    expirations[evictIndex] = currentTime + durationSec;
                    priorities[evictIndex] = priority;
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
            AttackTokenPriority[] priorities = isRanged ? _rangedPriorities : _meleePriorities;

            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId)
                {
                    owners[i] = 0;
                    expirations[i] = 0f;
                    priorities[i] = AttackTokenPriority.Minion;
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
                _meleePriorities[i] = AttackTokenPriority.Minion;
            }
            for (int i = 0; i < _rangedOwners.Length; i++)
            {
                _rangedOwners[i] = 0;
                _rangedExpirations[i] = 0f;
                _rangedPriorities[i] = AttackTokenPriority.Minion;
            }
        }
    }
}
