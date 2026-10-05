using UnityEngine;

namespace FeaturesCombat.Core.Physics
{
    /// <summary>
    /// Governs airborne hazard state for knocked-down entities.
    /// Employs kinetic threshold gating (speed >= 4.5 m/s) and temporal subsampling (20 Hz)
    /// with Physics.OverlapSphereNonAlloc to detect collisions with other enemies.
    /// Utilizes a zero-GC struct ring cache to prevent multihit collision damage loops.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class AirborneHazardEntity : MonoBehaviour
    {
        private static readonly Collider[] SharedBuffer = new Collider[16];
        private static int EnemyLayerMask = -1;

        [Tooltip("Minimum velocity magnitude required to maintain airborne hazard status.")]
        [SerializeField] private float minHazardSpeed = 4.5f;

        [Tooltip("Radius of the collision overlap sphere.")]
        [SerializeField] private float scanRadius = 0.85f;

        [Tooltip("Interval between overlap sphere scans in seconds (20 Hz = 0.05s).")]
        [SerializeField] private float scanInterval = 0.05f;

        [Tooltip("Secondary damage inflicted upon other enemies struck by this entity.")]
        [SerializeField] private int secondaryCollisionDamage = 18;

        private struct HitRecord
        {
            public int TargetId;
            public float ExpireTime;
        }

        private readonly HitRecord[] _hitHistory = new HitRecord[8];
        private int _historyIndex = 0;
        private Rigidbody _rb;
        private float _scanTimer;
        private bool _isHazardActive;

        public bool IsHazardActive => _isHazardActive;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (EnemyLayerMask == -1)
            {
                EnemyLayerMask = LayerMask.GetMask("Enemy");
                if (EnemyLayerMask == 0) EnemyLayerMask = ~0; // Fallback
            }
        }

        /// <summary>
        /// Activates airborne hazard state and applies launching knockback force.
        /// </summary>
        public void LaunchAsHazard(Vector3 launchForce)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb == null) return;

            _rb.AddForce(launchForce, ForceMode.Impulse);
            _isHazardActive = true;
            _scanTimer = 0f;

            // Clear hit history ring buffer
            for (int i = 0; i < _hitHistory.Length; i++)
            {
                _hitHistory[i].TargetId = 0;
                _hitHistory[i].ExpireTime = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (!_isHazardActive || _rb == null) return;

            // Kinetic threshold gating: stop scanning if velocity drops below threshold
            if (_rb.linearVelocity.sqrMagnitude < minHazardSpeed * minHazardSpeed)
            {
                _isHazardActive = false;
                return;
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
            _isHazardActive = false;
        }

        private void OnDisable()
        {
            _isHazardActive = false;
        }
    }
}
