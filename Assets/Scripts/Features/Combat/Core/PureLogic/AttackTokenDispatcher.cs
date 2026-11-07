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
        private readonly float[] _meleeDistances;
        private readonly bool[] _meleeCommitted;

        private readonly int[] _rangedOwners;
        private readonly float[] _rangedExpirations;
        private readonly AttackTokenPriority[] _rangedPriorities;
        private readonly float[] _rangedDistances;
        private readonly bool[] _rangedCommitted;

        public int MaxMeleeTokens => _meleeOwners.Length;
        public int MaxRangedTokens => _rangedOwners.Length;

        public AttackTokenDispatcher(
            int maxMeleeTokens = DEFAULT_MAX_MELEE_TOKENS,
            int maxRangedTokens = DEFAULT_MAX_RANGED_TOKENS)
        {
            _meleeOwners = new int[maxMeleeTokens > 0 ? maxMeleeTokens : 1];
            _meleeExpirations = new float[_meleeOwners.Length];
            _meleePriorities = new AttackTokenPriority[_meleeOwners.Length];
            _meleeDistances = new float[_meleeOwners.Length];
            _meleeCommitted = new bool[_meleeOwners.Length];

            _rangedOwners = new int[maxRangedTokens > 0 ? maxRangedTokens : 1];
            _rangedExpirations = new float[_rangedOwners.Length];
            _rangedPriorities = new AttackTokenPriority[_rangedOwners.Length];
            _rangedDistances = new float[_rangedOwners.Length];
            _rangedCommitted = new bool[_rangedOwners.Length];
        }

        /// <summary>
        /// Attempts to acquire an attack token with priority awareness and distance-based preemption.
        /// Higher-priority entities (Elite, Boss) can evict lower-priority tokens.
        /// Combatants in immediate strike range (distanceToTarget <= 2.2m) can preempt distant uncommitted holders.
        /// </summary>
        public bool TryAcquireToken(
            int entityId,
            bool isRanged,
            float currentTime,
            float durationSec = 3.0f,
            AttackTokenPriority priority = AttackTokenPriority.Minion,
            float distanceToTarget = float.MaxValue,
            bool isCommitted = false)
        {
            if (entityId == 0) return false;

            int[] owners = isRanged ? _rangedOwners : _meleeOwners;
            float[] expirations = isRanged ? _rangedExpirations : _meleeExpirations;
            AttackTokenPriority[] priorities = isRanged ? _rangedPriorities : _meleePriorities;
            float[] distances = isRanged ? _rangedDistances : _meleeDistances;
            bool[] committed = isRanged ? _rangedCommitted : _meleeCommitted;

            // 1. If entity already holds a token, refresh duration, update priority, distance, and commitment
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId)
                {
                    expirations[i] = currentTime + durationSec;
                    if (priority > priorities[i]) priorities[i] = priority;
                    distances[i] = distanceToTarget;
                    if (isCommitted) committed[i] = true;
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
                    distances[i] = distanceToTarget;
                    committed[i] = isCommitted;
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
                    distances[evictIndex] = distanceToTarget;
                    committed[evictIndex] = isCommitted;
                    return true;
                }
            }

            // 4. Distance-Based Strike Preemption (Equal Tier):
            // If applicant is in immediate striking range (distanceToTarget <= 2.2f) and an existing slot holder
            // is distant (distance >= 2.8f) and NOT actively committed to an attack swing (!committed[i]),
            // preempt the distant anticipatory token so the monster right in front of the player can strike immediately!
            if (distanceToTarget <= 2.2f)
            {
                int distantCandidate = -1;
                float maxDistantDist = 2.8f;

                for (int i = 0; i < owners.Length; i++)
                {
                    if (priorities[i] <= priority && !committed[i])
                    {
                        if (distances[i] > maxDistantDist)
                        {
                            maxDistantDist = distances[i];
                            distantCandidate = i;
                        }
                    }
                }

                if (distantCandidate != -1)
                {
                    owners[distantCandidate] = entityId;
                    expirations[distantCandidate] = currentTime + durationSec;
                    priorities[distantCandidate] = priority;
                    distances[distantCandidate] = distanceToTarget;
                    committed[distantCandidate] = isCommitted;
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
            float[] distances = isRanged ? _rangedDistances : _meleeDistances;
            bool[] committed = isRanged ? _rangedCommitted : _meleeCommitted;

            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId)
                {
                    owners[i] = 0;
                    expirations[i] = 0f;
                    priorities[i] = AttackTokenPriority.Minion;
                    distances[i] = float.MaxValue;
                    committed[i] = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Checks whether the entity currently holds an active, unexpired attack token. Zero-GC.
        /// </summary>
        public bool HasToken(int entityId, bool isRanged, float currentTime)
        {
            if (entityId == 0) return false;
            int[] owners = isRanged ? _rangedOwners : _meleeOwners;
            float[] expirations = isRanged ? _rangedExpirations : _meleeExpirations;
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId && currentTime < expirations[i])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether the entity is currently committed to an active attack swing.
        /// </summary>
        public bool IsCommitted(int entityId, bool isRanged)
        {
            if (entityId == 0) return false;
            int[] owners = isRanged ? _rangedOwners : _meleeOwners;
            bool[] committed = isRanged ? _rangedCommitted : _meleeCommitted;
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] == entityId) return committed[i];
            }
            return false;
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
                _meleeDistances[i] = float.MaxValue;
                _meleeCommitted[i] = false;
            }
            for (int i = 0; i < _rangedOwners.Length; i++)
            {
                _rangedOwners[i] = 0;
                _rangedExpirations[i] = 0f;
                _rangedPriorities[i] = AttackTokenPriority.Minion;
                _rangedDistances[i] = float.MaxValue;
                _rangedCommitted[i] = false;
            }
        }
    }
}
