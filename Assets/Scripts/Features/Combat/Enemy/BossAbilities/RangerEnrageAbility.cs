using System.Collections;
using UnityEngine;
using FeaturesCombat.Projectiles;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Kemampuan modular Enrage untuk Boss The Ranger (Task 2.3).
    /// Memicu transisi fase amarah secara otomatis saat HP <= 50% (HANYA SEKALI, tidak spam).
    /// Melontarkan 20 proyektil radial burst 360 derajat (Zero-Allocation Deferred+),
    /// memberikan buff visual pendaran merah amarah, dan peningkatan kecepatan serang/gerak.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public class RangerEnrageAbility : MonoBehaviour
    {
        [Header("Enrage Trigger Settings")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float hpThresholdPercent = 0.5f;
        [SerializeField] private int projectileCount = 20;
        [SerializeField] private int burstDamage = 18;
        [SerializeField] private float projectileSpeed = 13.5f;

        private EnemyBase bossEnemy;
        private bool hasEnraged = false;
        private bool isBursting = false;

        public bool HasEnraged => hasEnraged;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (bossEnemy != null)
            {
                bossEnemy.OnHealthChanged -= HandleHealthChanged;
            }
        }

        public void EnsureInitialized()
        {
            if (bossEnemy == null)
            {
                bossEnemy = GetComponent<EnemyBase>();
            }
            if (bossEnemy != null)
            {
                bossEnemy.OnHealthChanged -= HandleHealthChanged;
                bossEnemy.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void HandleHealthChanged(int currentHp, int maxHp)
        {
            if (hasEnraged || isBursting || maxHp <= 0) return;

            float hpPercent = (float)currentHp / maxHp;
            if (hpPercent <= hpThresholdPercent && currentHp > 0)
            {
                hasEnraged = true;
                if (Application.isPlaying)
                {
                    StartCoroutine(RoutineExecuteEnrage());
                }
            }
        }

        private IEnumerator RoutineExecuteEnrage()
        {
            isBursting = true;
            bossEnemy.IsPerformingSkill = true;
            bossEnemy.IsUnstaggerable = true;

            // Berhenti sejenak untuk pose amarah (Enrage stance)
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 3.2f,
                    "🔥 THE RANGER ENRAGES! BARRAGE!",
                    new Color(1f, 0.15f, 0.1f));
            }

            // Visual: Ubah warna dan intensitas pendaran material menjadi Crimson Fury
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                Color enrageColor = new Color(1f, 0.18f, 0.12f);
                rend.material.color = enrageColor;
                if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", enrageColor);
                if (rend.material.HasProperty("_FresnelColor")) rend.material.SetColor("_FresnelColor", new Color(3f, 0.5f, 0.1f));
            }

            // Wind-up channeling sebelum peluru meledak (0.45s)
            yield return new WaitForSeconds(0.45f);

            // Tembakkan 20 butir proyektil melingkar 360 derajat (Radial 360 Burst)
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            float angleStep = 360f / Mathf.Max(1, projectileCount);

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * angleStep;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                dir.y = 0f;
                dir.Normalize();

                CombatProjectile.Spawn(
                    gameObject,
                    origin,
                    dir,
                    burstDamage,
                    projectileSpeed,
                    new Color(1f, 0.35f, 0.05f) // Molten Corn Flame
                );
            }

            // Buff stat boss setelah enrage: +25% MoveSpeed, +20% AttackRate
            bossEnemy.moveSpeed *= 1.25f;
            bossEnemy.attackRate *= 1.20f;

            yield return new WaitForSeconds(0.4f);

            bossEnemy.IsUnstaggerable = false;
            bossEnemy.IsPerformingSkill = false;
            isBursting = false;
        }
    }
}
