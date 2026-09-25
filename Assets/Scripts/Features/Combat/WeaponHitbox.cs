using System.Collections.Generic;
using UnityEngine;
using PlayerUI;

namespace FeaturesCombat
{
    /// <summary>
    /// Komponen deteksi tabrakan senjata (hitbox).
    /// Dipasang pada bilah senjata (DummySword, Cangkul Tempur, dll.) untuk mendeteksi IDamageable,
    /// memberikan damage terhitung, menerapkan efek knockback, dan memunculkan floating text.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour
    {
        [Header("Hitbox References")]
        [SerializeField] private Collider hitboxCollider;

        [Header("Audio / Hit FX (Optional)")]
        [SerializeField] private AudioClip hitSound;

        [Header("Sweep Detection Settings")]
        [SerializeField] private float sweepForwardOffset = 1.0f;
        [SerializeField] private float sweepRadius = 1.6f;
        [SerializeField] private float minForwardDot = 0.2f; // Minimum dot product to ensure frontal arc (rejects behind player)

        private GameObject attacker;
        private int currentDamage;
        private float currentKnockback;
        private Vector3 attackDirection;
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        private bool isActive = false;

        private void Awake()
        {
            if (hitboxCollider == null)
                hitboxCollider = GetComponent<Collider>();

            if (hitboxCollider != null)
            {
                hitboxCollider.isTrigger = true;
                hitboxCollider.enabled = false;
            }

            // Pastikan memiliki Kinematic Rigidbody agar PhysX merouting OnTriggerEnter langsung ke GameObject ini
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void Update()
        {
            if (isActive)
            {
                PerformSweepCheck();
            }
        }

        /// <summary>
        /// Mengaktifkan hitbox selama jendela ayunan aktif.
        /// </summary>
        public void Activate(GameObject attackerOwner, int damage, float knockback, Vector3 direction)
        {
            attacker = attackerOwner;
            currentDamage = damage;
            currentKnockback = knockback;
            attackDirection = direction.normalized;
            hitTargets.Clear();
            isActive = true;

            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = true;
            }

            // Lakukan sweep proaktif seketika saat ayunan dimulai
            PerformSweepCheck();
        }

        /// <summary>
        /// Menonaktifkan hitbox setelah ayunan selesai.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            hitTargets.Clear();

            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
            }
        }

        /// <summary>
        /// Pemeriksaan sapuan bola (OverlapSphere) di depan penyerang untuk menjamin registrasi pukulan
        /// terlepas dari rotasi bone animasi atau tipisnya collider fisik.
        /// Memvalidasi sudut hadap (cone check) agar musuh di belakang penyerang tidak terkena hit.
        /// </summary>
        public void PerformSweepCheck()
        {
            if (!isActive) return;

            Vector3 origin = (attacker != null) ? attacker.transform.position + Vector3.up * 0.8f : transform.position;
            Vector3 forwardDir = (attacker != null) ? attacker.transform.forward : (attackDirection != Vector3.zero ? attackDirection : transform.forward);
            Vector3 sweepCenter = origin + forwardDir * sweepForwardOffset;

            // Eksplisit LayerMask: mencakup semua collider kecuali trigger non-damageable
            Collider[] overlaps = Physics.OverlapSphere(sweepCenter, sweepRadius, ~0, QueryTriggerInteraction.Collide);

            foreach (var col in overlaps)
            {
                if (col == null) continue;

                // Abaikan penyerang sendiri
                if (attacker != null && (col.gameObject == attacker || col.transform.IsChildOf(attacker.transform)))
                    continue;

                // Cone Check: Pastikan target berada di depan penyerang (sudut hadap frontal)
                Vector3 toTarget = col.transform.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.04f)
                {
                    Vector3 normDir = toTarget.normalized;
                    float dot = Vector3.Dot(forwardDir, normDir);
                    if (dot < minForwardDot)
                    {
                        // Target berada di belakang atau samping luar jangkauan sudut serang
                        continue;
                    }
                }

                IDamageable target = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead) continue;

                if (hitTargets.Contains(target)) continue;

                Vector3 hitPoint = col.ClosestPoint(sweepCenter);
                ProcessHit(target, col.gameObject, hitPoint);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || other == null) return;

            // Abaikan penyerang sendiri
            if (attacker != null && (other.gameObject == attacker || other.transform.IsChildOf(attacker.transform)))
                return;

            // Validasi arah hadap juga pada trigger fisik
            Vector3 origin = (attacker != null) ? attacker.transform.position : transform.position;
            Vector3 forwardDir = (attacker != null) ? attacker.transform.forward : attackDirection;
            Vector3 toTarget = other.transform.position - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.04f)
            {
                if (Vector3.Dot(forwardDir, toTarget.normalized) < minForwardDot)
                    return;
            }

            IDamageable target = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead || hitTargets.Contains(target))
                return;

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            ProcessHit(target, other.gameObject, hitPoint);
        }

        private void ProcessHit(IDamageable target, GameObject targetObj, Vector3 hitPoint)
        {
            if (!hitTargets.Add(target)) return;

            int effectiveDamage = currentDamage;
            if (attacker != null)
            {
                var stats = attacker.GetComponent<PlayerStats>() ?? attacker.GetComponentInParent<PlayerStats>();
                if (stats != null && stats.isGodMode)
                {
                    effectiveDamage = 9999;
                }
            }

            target.TakeDamage(effectiveDamage, hitPoint, attackDirection);

            // Terapkan knockback pada Rigidbody musuh jika ada
            Rigidbody targetRb = targetObj.GetComponent<Rigidbody>() ?? targetObj.GetComponentInParent<Rigidbody>();
            if (targetRb != null && !targetRb.isKinematic)
            {
                Vector3 knockbackDir = (attackDirection + Vector3.up * 0.25f).normalized;
                targetRb.AddForce(knockbackDir * currentKnockback, ForceMode.Impulse);
            }

            // Munculkan floating combat text
            if (FloatingCombatTextManager.Instance != null)
            {
                Color textColor = effectiveDamage >= 9999 ? new Color(1f, 0.85f, 0.15f) : new Color(1f, 0.25f, 0.2f);
                string text = effectiveDamage >= 9999 ? "💥 9999" : $"-{effectiveDamage}";
                FloatingCombatTextManager.Instance.SpawnText(
                    hitPoint + Vector3.up * 0.8f,
                    text,
                    textColor);
            }
        }
    }
}
