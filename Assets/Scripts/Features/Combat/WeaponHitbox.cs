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

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || other == null) return;

            // Abaikan penyerang sendiri
            if (attacker != null && (other.gameObject == attacker || other.transform.IsChildOf(attacker.transform)))
                return;

            IDamageable target = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead || hitTargets.Contains(target))
                return;

            hitTargets.Add(target);

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            target.TakeDamage(currentDamage, hitPoint, attackDirection);

            // Terapkan knockback pada Rigidbody musuh jika ada
            Rigidbody targetRb = other.GetComponent<Rigidbody>() ?? other.GetComponentInParent<Rigidbody>();
            if (targetRb != null && !targetRb.isKinematic)
            {
                Vector3 knockbackDir = (attackDirection + Vector3.up * 0.25f).normalized;
                targetRb.AddForce(knockbackDir * currentKnockback, ForceMode.Impulse);
            }

            // Munculkan floating combat text
            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    hitPoint + Vector3.up * 0.8f,
                    $"-{currentDamage}",
                    new Color(1f, 0.25f, 0.2f));
            }
        }
    }
}
