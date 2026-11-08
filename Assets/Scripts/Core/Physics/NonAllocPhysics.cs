using System;
using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Centralized high-performance non-allocating physics utility.
    /// Provides zero-GC OverlapSphere and Raycast queries using thread-safe or reusable static buffers.
    /// </summary>
    public static class NonAllocPhysics
    {
        private static readonly Collider[] ColliderBuffer = new Collider[64];
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[32];

        /// <summary>
        /// Non-allocating OverlapSphere that iterates matching colliders via an action callback.
        /// Immediately clears buffer slots after execution to prevent dangling memory references.
        /// </summary>
        public static int OverlapSphere(Vector3 center, float radius, int layerMask, Action<Collider> onHit)
        {
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(center, radius, ColliderBuffer, layerMask);
            for (int i = 0; i < count; i++)
            {
                if (ColliderBuffer[i] != null)
                {
                    onHit?.Invoke(ColliderBuffer[i]);
                    ColliderBuffer[i] = null;
                }
            }
            return count;
        }

        /// <summary>
        /// Non-allocating OverlapSphere returning matching colliders populated into a provided array.
        /// </summary>
        public static int OverlapSphereNonAlloc(Vector3 center, float radius, Collider[] results, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return UnityEngine.Physics.OverlapSphereNonAlloc(center, radius, results, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Non-allocating Raycast query.
        /// </summary>
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return UnityEngine.Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        private static int _defaultObstacleMask = -1;

        /// <summary>
        /// Retrieves the LayerMask containing solid physical obstacles (Wall, Obstacle, Structures, Environment).
        /// Cached dynamically to prevent per-call LayerMask lookups.
        /// </summary>
        public static int GetDefaultObstacleLayerMask()
        {
            if (_defaultObstacleMask == -1)
            {
                int mask = 0;
                int wallLayer = LayerMask.NameToLayer("Wall");
                if (wallLayer >= 0) mask |= (1 << wallLayer);
                int obsLayer = LayerMask.NameToLayer("Obstacle");
                if (obsLayer >= 0) mask |= (1 << obsLayer);
                int structLayer = LayerMask.NameToLayer("Structures");
                if (structLayer >= 0) mask |= (1 << structLayer);
                int envLayer = LayerMask.NameToLayer("Environment");
                if (envLayer >= 0) mask |= (1 << envLayer);

                // Fallback to layer 12 (Wall) if named layers could not be resolved
                if (mask == 0) mask = 1 << 12;
                _defaultObstacleMask = mask;
            }
            return _defaultObstacleMask;
        }

        /// <summary>
        /// Evaluates whether the line-of-sight between an attack origin and a target point is obstructed by a solid obstacle.
        /// Performs a zero-GC Linecast query ignoring triggers, self-hits, and target colliders.
        /// Draws visual debug lines (Green = clear, Red = blocked).
        /// </summary>
        public static bool IsLineOfSightBlocked(Vector3 origin, Vector3 targetPoint, Collider targetCollider, GameObject attacker, int obstacleLayerMask)
        {
            Vector3 toTarget = targetPoint - origin;
            float dist = toTarget.magnitude;
            if (dist < 0.05f) return false;

            if (obstacleLayerMask == 0)
            {
                obstacleLayerMask = GetDefaultObstacleLayerMask();
            }

            if (UnityEngine.Physics.Linecast(origin, targetPoint, out RaycastHit hit, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                // Verify the hit is not the target itself or a child of the target
                if (targetCollider != null && (hit.collider == targetCollider || hit.collider.transform.IsChildOf(targetCollider.transform)))
                {
                    Debug.DrawLine(origin, targetPoint, Color.green, 0.4f);
                    return false;
                }

                // Verify the hit is not the attacker itself
                if (attacker != null && (hit.collider.gameObject == attacker || hit.collider.transform.IsChildOf(attacker.transform)))
                {
                    Debug.DrawLine(origin, targetPoint, Color.green, 0.4f);
                    return false;
                }

                // Blocked by an obstacle!
                Debug.DrawLine(origin, hit.point, Color.red, 0.5f);
                Debug.DrawLine(hit.point, targetPoint, new Color(1f, 0.2f, 0.2f, 0.5f), 0.5f);
                return true;
            }

            Debug.DrawLine(origin, targetPoint, Color.green, 0.4f);
            return false;
        }
    }
}
