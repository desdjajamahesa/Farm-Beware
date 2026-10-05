using System;
using UnityEngine;
using UnityEngine.AI;

namespace FeaturesCombat.Adapters
{
    /// <summary>
    /// Centralized Zero-GC Hitstop Coordinator.
    /// Manages non-allocating hit freeze frames across player and monster entities
    /// using a fixed-size ring/slot registry and coordinating Animator speed with NavMeshAgent stops.
    /// </summary>
    public class HitstopCoordinator : MonoBehaviour
    {
        public static HitstopCoordinator Instance { get; private set; }

        private struct HitstopEntry
        {
            public int InstanceId;
            public Animator TargetAnimator;
            public NavMeshAgent TargetAgent;
            public Rigidbody TargetRigidbody;
            public float RemainingTime;
            public float OriginalAnimatorSpeed;
            public float OriginalLinearDamping;
            public bool OriginalAgentStoppedState;
            public bool InUse;
        }

        private const int MAX_CONCURRENT_HITSTOPS = 64;
        private readonly HitstopEntry[] _registry = new HitstopEntry[MAX_CONCURRENT_HITSTOPS];
        private int _activeCount = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Registers or refreshes a hitstop freeze for the target entity without allocating heap memory.
        /// </summary>
        public void RegisterHitstop(GameObject target, float durationSec, float freezeSpeedScale = 0.0f)
        {
            if (target == null || durationSec <= 0.001f) return;

            int targetId = target.GetInstanceID();

            // 1. Check if entity is already registered in active slots (prevents origSpeed overwrite race condition)
            for (int i = 0; i < MAX_CONCURRENT_HITSTOPS; i++)
            {
                if (_registry[i].InUse && _registry[i].InstanceId == targetId)
                {
                    // Extend duration to the maximum remaining without overwriting original speed
                    if (durationSec > _registry[i].RemainingTime)
                    {
                        _registry[i].RemainingTime = durationSec;
                    }
                    return;
                }
            }

            // 2. Allocate into an available slot
            for (int i = 0; i < MAX_CONCURRENT_HITSTOPS; i++)
            {
                if (!_registry[i].InUse)
                {
                    var anim = target.GetComponentInChildren<Animator>();
                    var agent = target.GetComponent<NavMeshAgent>();
                    var rb = target.GetComponent<Rigidbody>();

                    float origSpeed = (anim != null && anim.speed > 0.01f) ? anim.speed : 1.0f;
                    float origDamping = (rb != null) ? rb.linearDamping : 0f;
                    bool origAgentStopped = (agent != null && agent.isOnNavMesh) ? agent.isStopped : false;

                    if (anim != null)
                    {
                        anim.speed = freezeSpeedScale;
                    }

                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.updatePosition = false;
                    }

                    if (rb != null)
                    {
                        rb.linearDamping = 12f; // Increased friction to arrest sliding during hit freeze
                    }

                    _registry[i] = new HitstopEntry
                    {
                        InstanceId = targetId,
                        TargetAnimator = anim,
                        TargetAgent = agent,
                        TargetRigidbody = rb,
                        RemainingTime = durationSec,
                        OriginalAnimatorSpeed = origSpeed,
                        OriginalLinearDamping = origDamping,
                        OriginalAgentStoppedState = origAgentStopped,
                        InUse = true
                    };

                    _activeCount++;
                    break;
                }
            }
        }

        private void Update()
        {
            if (_activeCount == 0) return;

            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < MAX_CONCURRENT_HITSTOPS; i++)
            {
                if (!_registry[i].InUse) continue;

                _registry[i].RemainingTime -= dt;

                if (_registry[i].RemainingTime <= 0f)
                {
                    RestoreSlot(i);
                }
            }
        }

        private void RestoreSlot(int index)
        {
            ref HitstopEntry entry = ref _registry[index];

            // Restore Animator
            if (entry.TargetAnimator != null)
            {
                entry.TargetAnimator.speed = entry.OriginalAnimatorSpeed > 0.01f ? entry.OriginalAnimatorSpeed : 1.0f;
            }

            // Restore Rigidbody damping
            if (entry.TargetRigidbody != null)
            {
                entry.TargetRigidbody.linearDamping = entry.OriginalLinearDamping;
            }

            // Restore NavMeshAgent
            if (entry.TargetAgent != null && entry.TargetAgent.isOnNavMesh)
            {
                entry.TargetAgent.Warp(entry.TargetAgent.transform.position);
                entry.TargetAgent.updatePosition = true;
                entry.TargetAgent.isStopped = entry.OriginalAgentStoppedState;
            }

            entry.InUse = false;
            entry.InstanceId = 0;
            entry.TargetAnimator = null;
            entry.TargetAgent = null;
            entry.TargetRigidbody = null;

            _activeCount--;
            if (_activeCount < 0) _activeCount = 0;
        }

        /// <summary>
        /// Explicitly unregisters an entity if it dies or is returned to an object pool.
        /// </summary>
        public void Unregister(GameObject target)
        {
            if (target == null || _activeCount == 0) return;
            int targetId = target.GetInstanceID();

            for (int i = 0; i < MAX_CONCURRENT_HITSTOPS; i++)
            {
                if (_registry[i].InUse && _registry[i].InstanceId == targetId)
                {
                    RestoreSlot(i);
                    break;
                }
            }
        }
    }
}
