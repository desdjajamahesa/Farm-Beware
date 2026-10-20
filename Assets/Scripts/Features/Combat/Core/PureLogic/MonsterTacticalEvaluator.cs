using UnityEngine;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Explicit tactical behavioral states for monster AI.
    /// </summary>
    public enum MonsterTacticalState
    {
        IdlePatrol,
        EngageDirect,
        FlankOrbit,
        TangentialKite,
        TelegraphAttack,
        RetreatHeal
    }

    /// <summary>
    /// Pure C# POCO Tactical Evaluator for monster navigation, encirclement, and combat behaviors.
    /// Zero-GC deterministic calculation decoupled from Unity lifecycle dependencies.
    /// </summary>
    public class MonsterTacticalEvaluator
    {
        public float OrbitMinRadius { get; set; } = 3.2f;
        public float OrbitMaxRadius { get; set; } = 4.8f;
        public float KiteDistanceThreshold { get; set; } = 3.8f;
        public float DefaultOrbitSpeedFactor { get; set; } = 0.82f;

        public MonsterTacticalEvaluator(
            float orbitMinRadius = 3.2f,
            float orbitMaxRadius = 4.8f,
            float kiteDistanceThreshold = 3.8f)
        {
            OrbitMinRadius = orbitMinRadius;
            OrbitMaxRadius = orbitMaxRadius;
            KiteDistanceThreshold = kiteDistanceThreshold;
        }

        /// <summary>
        /// Evaluates the tactical desired movement direction and updates the intended tactical state.
        /// Pure logic without heap allocations.
        /// </summary>
        public Vector3 EvaluateMovement(
            Vector3 enemyPos,
            Vector3 targetPos,
            float distanceToTarget,
            bool hasAttackToken,
            bool isRanged,
            int entityId,
            float attackRange,
            out MonsterTacticalState calculatedState)
        {
            return EvaluateMovement(
                enemyPos,
                targetPos,
                distanceToTarget,
                hasAttackToken,
                isRanged,
                entityId,
                attackRange,
                isAttackReady: false,
                out calculatedState);
        }

        /// <summary>
        /// Evaluates movement with explicit attack readiness. If entity is ready to strike,
        /// avoids endless FlankOrbit and drives EngageDirect to claim tokens and strike.
        /// </summary>
        public Vector3 EvaluateMovement(
            Vector3 enemyPos,
            Vector3 targetPos,
            float distanceToTarget,
            bool hasAttackToken,
            bool isRanged,
            int entityId,
            float attackRange,
            bool isAttackReady,
            out MonsterTacticalState calculatedState)
        {
            Vector3 toTarget = targetPos - enemyPos;
            toTarget.y = 0f;

            float sqrDist = toTarget.sqrMagnitude;
            if (sqrDist < 0.0001f)
            {
                calculatedState = MonsterTacticalState.EngageDirect;
                return Vector3.forward;
            }

            Vector3 directDir = toTarget / Mathf.Sqrt(sqrDist);

            // 1. Ranged Specialist Behavior (Corn Musketeer / The Ranger)
            if (isRanged)
            {
                if (distanceToTarget < KiteDistanceThreshold)
                {
                    calculatedState = MonsterTacticalState.TangentialKite;
                    return CalculateTangentialKiteDirection(directDir, entityId);
                }

                if (distanceToTarget <= attackRange)
                {
                    // Within effective firing range: hold ground or minor positioning
                    calculatedState = MonsterTacticalState.EngageDirect;
                    return Vector3.zero;
                }

                // Close the gap to enter attack range
                calculatedState = MonsterTacticalState.EngageDirect;
                return directDir;
            }

            // 2. Melee Specialist Behavior (Tuber Maw, Taro Brute, Taro Colossus)
            if (hasAttackToken || isAttackReady)
            {
                // Has attack permit or cooldown is ready to strike: Charge directly to player!
                calculatedState = MonsterTacticalState.EngageDirect;
                return directDir;
            }

            // No attack token and not attack-ready: Check distance to combat ring
            if (distanceToTarget > OrbitMaxRadius)
            {
                // Outside combat ring: Approach the orbit zone
                calculatedState = MonsterTacticalState.EngageDirect;
                return directDir;
            }

            // Inside combat ring and waiting for attack token: Flank and circle around player!
            calculatedState = MonsterTacticalState.FlankOrbit;
            return CalculateOrbitDirection(directDir, distanceToTarget, entityId, OrbitMinRadius, OrbitMaxRadius);
        }

        /// <summary>
        /// Calculates a tangential circling vector around the target to form an encirclement ring.
        /// Alternates clockwise vs counter-clockwise based on entity ID bit.
        /// </summary>
        public Vector3 CalculateOrbitDirection(Vector3 directDirToTarget, float currentDist, int entityId)
        {
            return CalculateOrbitDirection(directDirToTarget, currentDist, entityId, OrbitMinRadius, OrbitMaxRadius);
        }

        /// <summary>
        /// Overload with dynamic min and max orbit radii.
        /// </summary>
        public Vector3 CalculateOrbitDirection(Vector3 directDirToTarget, float currentDist, int entityId, float minRadius, float maxRadius)
        {
            float directionSign = ((entityId & 1) == 0) ? 1.0f : -1.0f;

            // Tangent perpendicular to line of sight (X-Z plane)
            Vector3 tangent = new Vector3(-directDirToTarget.z, 0f, directDirToTarget.x) * directionSign;

            // Radial bias to keep distance within [minRadius, maxRadius]
            float midRadius = (minRadius + maxRadius) * 0.5f;
            float radialOffset = currentDist - midRadius;
            Vector3 radialCorrection = directDirToTarget * Mathf.Clamp(radialOffset * 0.45f, -0.75f, 0.75f);

            Vector3 combined = tangent * 0.85f + radialCorrection * 0.45f;
            combined.y = 0f;
            return combined.sqrMagnitude > 0.001f ? combined.normalized : tangent;
        }

        /// <summary>
        /// Calculates a diagonal/tangential evasive retreat vector for ranged combatants.
        /// </summary>
        public Vector3 CalculateTangentialKiteDirection(Vector3 directDirToTarget, int entityId)
        {
            Vector3 retreatDir = -directDirToTarget;
            float lateralSign = ((entityId & 1) == 0) ? 1.0f : -1.0f;
            Vector3 lateral = new Vector3(-directDirToTarget.z, 0f, directDirToTarget.x) * (lateralSign * 0.65f);

            Vector3 combined = retreatDir + lateral;
            combined.y = 0f;
            return combined.sqrMagnitude > 0.001f ? combined.normalized : retreatDir;
        }

        /// <summary>
        /// Generates a local wander waypoint for ambient idle patrol around a spawn anchor.
        /// </summary>
        public static Vector3 CalculateAmbientWanderTarget(Vector3 origin, float wanderRadius, float angleDegrees, float normalizedDist)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float dist = Mathf.Clamp01(normalizedDist) * wanderRadius;
            return origin + new Vector3(Mathf.Cos(rad) * dist, 0f, Mathf.Sin(rad) * dist);
        }
    }
}
