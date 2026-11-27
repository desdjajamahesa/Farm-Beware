using System.Collections;
using UnityEngine;
using FeaturesCombat.Projectiles;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Modular Enrage ability for The Ranger boss (Task 2.3).
    /// Automatically triggers enrage phase transition when HP <= 50% (ONCE ONLY, non-repeating).
    /// Fires 20 projectiles in a 360-degree radial burst (Zero-Allocation Deferred+),
    /// applies a crimson fury emissive visual buff, and boosts movement/attack speed.
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
        private MaterialPropertyBlock _mpb;

        public bool HasEnraged => hasEnraged;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            hasEnraged = false;
            isBursting = false;
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

            // Pause briefly for the enrage stance
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

            // Visual: Set material color and emission intensity to Crimson Fury via MaterialPropertyBlock
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                Color enrageColor = new Color(1f, 0.18f, 0.12f);
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", enrageColor);
                _mpb.SetColor("_Color", enrageColor);
                _mpb.SetColor("_FresnelColor", new Color(3f, 0.5f, 0.1f));
                rend.SetPropertyBlock(_mpb);
            }

            // Channeling wind-up before projectile burst (0.45s)
            yield return new WaitForSeconds(0.45f);

            // Fire 20 projectiles in a 360-degree radial burst
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

            // Boss stat buff after enrage: +25% MoveSpeed, +20% AttackRate
            bossEnemy.moveSpeed *= 1.25f;
            bossEnemy.attackRate *= 1.20f;

            yield return new WaitForSeconds(0.4f);

            bossEnemy.IsUnstaggerable = false;
            bossEnemy.IsPerformingSkill = false;
            isBursting = false;
        }
    }
}
