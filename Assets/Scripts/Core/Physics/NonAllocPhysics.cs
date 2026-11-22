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

        public static bool IsInsideHouseBounds(Vector3 pos)
        {
            return pos.x >= 9.5f && pos.x <= 31.5f && pos.z >= 4.5f && pos.z <= 26.5f;
        }

        /// <summary>
        /// Evaluates whether the line-of-sight between an attack origin and a target point is obstructed by a solid obstacle.
        /// Also blocks attacks crossing the interior/exterior house safe-zone boundary.
        /// Performs a zero-GC Linecast query ignoring triggers, self-hits, and target colliders.
        /// Draws visual debug lines (Green = clear, Red = blocked).
        /// </summary>
        public static bool IsLineOfSightBlocked(Vector3 origin, Vector3 targetPoint, Collider targetCollider, GameObject attacker, int obstacleLayerMask)
        {
            Vector3 checkTargetPos = targetCollider != null ? targetCollider.bounds.center : targetPoint;
            if (IsInsideHouseBounds(origin) != IsInsideHouseBounds(checkTargetPos))
            {
                Debug.DrawLine(origin, checkTargetPos, Color.red, 0.5f);
                return true;
            }

            Vector3 toTarget = targetPoint - origin;
            if (toTarget.magnitude < 0.05f) return false;

            if (obstacleLayerMask == 0)
            {
                obstacleLayerMask = GetDefaultObstacleLayerMask();
            }

            if (IsSegmentBlocked(origin, targetPoint, targetCollider, attacker, obstacleLayerMask))
            {
                return true;
            }

            if (targetCollider != null)
            {
                Vector3 planarCenter = new Vector3(targetCollider.bounds.center.x, origin.y, targetCollider.bounds.center.z);
                if (IsSegmentBlocked(origin, planarCenter, targetCollider, attacker, obstacleLayerMask))
                {
                    return true;
                }
            }

            Debug.DrawLine(origin, targetPoint, Color.green, 0.4f);
            return false;
        }

        private static bool IsSegmentBlocked(Vector3 origin, Vector3 endPoint, Collider targetCollider, GameObject attacker, int obstacleLayerMask)
        {
            if (UnityEngine.Physics.Linecast(origin, endPoint, out RaycastHit hit, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (targetCollider != null && (hit.collider == targetCollider || hit.collider.transform.IsChildOf(targetCollider.transform)))
                {
                    return false;
                }

                if (attacker != null && (hit.collider.gameObject == attacker || hit.collider.transform.IsChildOf(attacker.transform)))
                {
                    return false;
                }

                Debug.DrawLine(origin, hit.point, Color.red, 0.5f);
                Debug.DrawLine(hit.point, endPoint, new Color(1f, 0.2f, 0.2f, 0.5f), 0.5f);
                return true;
            }

            return false;
        }
    }
}
