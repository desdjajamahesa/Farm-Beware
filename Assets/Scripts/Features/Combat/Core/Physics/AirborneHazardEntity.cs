using UnityEngine;
using FeaturesCombat.Core.PureLogic;

namespace FeaturesCombat.Core.Physics
{
    /// <summary>
    /// Governs airborne hazard state for knocked-down entities.
    /// Employs kinetic threshold gating (speed >= 4.5 m/s) and temporal subsampling (20 Hz)
    /// with Physics.OverlapSphereNonAlloc to detect collisions with other enemies,
    /// plus environmental Wall-Splat impacts when launched enemies slam into static obstacles.
    /// Utilizes a zero-GC struct ring cache to prevent multihit collision damage loops.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class AirborneHazardEntity : MonoBehaviour
    {
        private static readonly Collider[] SharedBuffer = new Collider[16];
        private static int EnemyLayerMask = -1;
        private static int BoundaryLayerMask = -1;

        [Tooltip("Minimum velocity magnitude required to maintain airborne hazard status.")]
        [SerializeField] private float minHazardSpeed = 4.5f;

        [Tooltip("Radius of the collision overlap sphere.")]
        [SerializeField] private float scanRadius = 0.85f;

        [Tooltip("Interval between overlap sphere scans in seconds (20 Hz = 0.05s).")]
        [SerializeField] private float scanInterval = 0.05f;

        [Tooltip("Secondary damage inflicted upon other enemies struck by this entity.")]
        [SerializeField] private int secondaryCollisionDamage = 18;

        [Tooltip("Base bonus damage inflicted upon this entity when slamming into a solid wall or fence at high speed.")]
        [SerializeField] private int wallSplatBonusDamage = 22;

        private struct HitRecord
        {
            public int TargetId;
            public float ExpireTime;
        }

        private readonly HitRecord[] _hitHistory = new HitRecord[8];
        private int _historyIndex = 0;
        private Rigidbody _rb;
        private UnityEngine.AI.NavMeshAgent _agent;
        private bool _wasAgentEnabledBeforeLaunch;
        private float _scanTimer;
        private bool _isHazardActive;
        private bool _hasWallSplattedThisLaunch;

        public bool IsHazardActive => _isHazardActive;
        public int LastWallSplatDamage { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (EnemyLayerMask == -1)
            {
                EnemyLayerMask = LayerMask.GetMask("Enemy");
                if (EnemyLayerMask == 0) EnemyLayerMask = ~0; // Fallback
            }

            if (BoundaryLayerMask == -1)
            {
                BoundaryLayerMask = LayerMask.GetMask("Wall", "Default", "Obstacle", "Environment");
                if (BoundaryLayerMask == 0)
                {
                    BoundaryLayerMask = ~LayerMask.GetMask("Enemy", "Player", "Ignore Raycast");
                }
            }
        }

        /// <summary>
        /// Activates airborne hazard state, decouples NavMeshAgent, sets CCD mode, and applies launching knockback force.
        /// </summary>
        public void LaunchAsHazard(Vector3 launchForce)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb == null) return;

            if (_agent == null) _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (_agent != null && _agent.enabled)
            {
                _wasAgentEnabledBeforeLaunch = true;
                _agent.enabled = false;
            }
            else
            {
                _wasAgentEnabledBeforeLaunch = false;
            }

            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.AddForce(launchForce, ForceMode.Impulse);
            _isHazardActive = true;
            _hasWallSplattedThisLaunch = false;
            LastWallSplatDamage = 0;
            _scanTimer = 0f;

            // Clear hit history ring buffer
            for (int i = 0; i < _hitHistory.Length; i++)
            {
                _hitHistory[i].TargetId = 0;
                _hitHistory[i].ExpireTime = 0f;
            }
        }

        /// <summary>
        /// Evaluates and applies environmental Wall-Splat impact damage, trauma shake, and floating text
        /// when a launched enemy slams into a solid boundary (fence, house wall, boulder).
        /// </summary>
        public int TriggerWallSplatImpact(Vector3 wallPoint, Vector3 wallNormal, float impactSpeed)
        {
            if (_hasWallSplattedThisLaunch) return 0;

            int wallDmg = ImpactKinematicsProfile.EvaluateWallSplatDamage(impactSpeed, minHazardSpeed, wallSplatBonusDamage);
            if (wallDmg <= 0) return 0;

            _hasWallSplattedThisLaunch = true;
            LastWallSplatDamage = wallDmg;

            var selfDamageable = GetComponent<IDamageable>() ?? GetComponentInParent<IDamageable>();
            if (selfDamageable != null && !selfDamageable.IsDead)
            {
                selfDamageable.TakeDamage(wallDmg, wallPoint, wallNormal);
            }

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    wallPoint + Vector3.up * 1.4f,
                    $"💥 WALL SPLAT! ({wallDmg})",
                    new Color(1f, 0.55f, 0.15f));
            }

            if (FeaturesCombat.Adapters.CombatHitVfxEngine.Instance != null)
            {
                FeaturesCombat.Adapters.CombatHitVfxEngine.Instance.TriggerHit(
                    wallPoint,
                    wallNormal,
                    gameObject,
                    null,
                    true,
                    AttackImpactTier.PoiseCrushOrWallSplat);
            }

            return wallDmg;
        }

        private void FixedUpdate()
        {
            if (!_isHazardActive || _rb == null) return;

            float sqrSpeed = _rb.linearVelocity.sqrMagnitude;
            // Kinetic threshold gating: stop scanning if velocity drops below threshold
            if (sqrSpeed < minHazardSpeed * minHazardSpeed)
            {
                EndHazardState();
                return;
            }

            // Boundary safeguard & Wall-Splat evaluation: Predictive raycast to detect fences / static boundaries
            float currentSpeed = Mathf.Sqrt(sqrSpeed);
            Vector3 moveDir = _rb.linearVelocity / currentSpeed;
            float checkDist = currentSpeed * Time.fixedDeltaTime + scanRadius * 0.5f;

            if (UnityEngine.Physics.Raycast(transform.position, moveDir, out RaycastHit wallHit, checkDist, BoundaryLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (!wallHit.collider.isTrigger && wallHit.collider.gameObject != gameObject)
                {
                    Vector3 normal = wallHit.normal;
                    normal.y = 0f;

                    // Trigger Wall-Splat bonus damage on high-speed boundary slam
                    TriggerWallSplatImpact(wallHit.point, normal.sqrMagnitude > 0.01f ? normal.normalized : -moveDir, currentSpeed);

                    if (normal.sqrMagnitude > 0.01f)
                    {
                        normal.Normalize();
                        Vector3 defl = Vector3.ProjectOnPlane(_rb.linearVelocity, normal);
                        _rb.linearVelocity = defl * 0.5f;
                    }
                    else
                    {
                        _rb.linearVelocity = Vector3.zero;
                    }

                    if (_rb.linearVelocity.sqrMagnitude < minHazardSpeed * minHazardSpeed)
                    {
                        EndHazardState();
                        return;
                    }
                }
            }

            _scanTimer += Time.fixedDeltaTime;
            if (_scanTimer < scanInterval) return;
            _scanTimer = 0f;

            int selfId = gameObject.GetInstanceID();
            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, scanRadius, SharedBuffer, EnemyLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                var col = SharedBuffer[i];
                if (col == null) continue;

                int targetId = col.gameObject.GetInstanceID();
                if (targetId == selfId) continue;

                // Validate zero-GC cooldown cache
                if (IsUnderCooldown(targetId)) continue;

                // Register hit with 0.4s cooldown
                RegisterCooldown(targetId, 0.40f);

                // Transmit secondary damage and micro stagger
                var targetDamageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (targetDamageable != null && !targetDamageable.IsDead)
                {
                    Vector3 impactDir = (col.transform.position - transform.position).normalized;
                    if (impactDir.sqrMagnitude < 0.01f) impactDir = transform.forward;

                    targetDamageable.TakeDamage(secondaryCollisionDamage, col.transform.position, impactDir);

                    // Deflect hazard momentum slightly
                    _rb.linearVelocity *= 0.75f;
                }
            }
        }

        private void EndHazardState()
        {
            if (!_isHazardActive) return;
            _isHazardActive = false;

            if (_rb != null)
            {
                _rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            // Boundary ground snap & safe restoration of NavMeshAgent
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit navHit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                transform.position = navHit.position;
                if (_rb != null)
                {
                    _rb.position = navHit.position;
                }
            }

            if (_agent != null && _wasAgentEnabledBeforeLaunch)
            {
                _agent.enabled = true;
                if (_agent.isOnNavMesh)
                {
                    _agent.Warp(transform.position);
                }
                _wasAgentEnabledBeforeLaunch = false;
            }
        }

        private bool IsUnderCooldown(int targetId)
        {
            float now = Time.time;
            for (int i = 0; i < _hitHistory.Length; i++)
            {
                if (_hitHistory[i].TargetId == targetId && now < _hitHistory[i].ExpireTime)
                {
                    return true;
                }
            }
            return false;
        }

        private void RegisterCooldown(int targetId, float duration)
        {
            _hitHistory[_historyIndex] = new HitRecord
            {
                TargetId = targetId,
                ExpireTime = Time.time + duration
            };
            _historyIndex = (_historyIndex + 1) % _hitHistory.Length;
        }

        public void DeactivateHazard()
        {
            EndHazardState();
        }

        private void OnDisable()
        {
            EndHazardState();
        }
    }
}
