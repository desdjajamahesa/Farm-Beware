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
    }
}
