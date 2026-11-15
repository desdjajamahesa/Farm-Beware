using System.Collections;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Kemampuan modular Grapple Slam untuk Taro Colossus (Task 2.2).
    /// Mengecek kedekatan dengan pemain (radius <= 2.2m).
    /// Mengunci pergerakan pemain sementara (SetControlLock), memberikan damage bantingan,
    /// menerapkan stun (ApplyStun), dan menjamin safety unlock agar kontrol pemain tidak pernah terjebak.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public class ColossusGrappleAbility : MonoBehaviour
    {
        [Header("Grapple Slam Settings")]
        [SerializeField] private float cooldown = 24f;
        [SerializeField] private float grabRange = 2.2f;
        [SerializeField] private int grappleDamage = 35;
        [SerializeField] private float stunDuration = 1.2f;
        [SerializeField] private float holdDuration = 0.7f;

        private EnemyBase bossEnemy;
        private float lastGrappleTime = 0f;
        private bool isGrappling = false;
        private IPlayerContext lockedPlayerContext;

        public bool IsGrappling => isGrappling;

        private void Awake()
        {
            EnsureSubscribed();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
        }

        public void EnsureSubscribed()
        {
            if (bossEnemy == null) bossEnemy = GetComponent<EnemyBase>();
            if (bossEnemy != null)
            {
                bossEnemy.OnActionInterrupted -= HandleBossActionInterrupted;
                bossEnemy.OnActionInterrupted += HandleBossActionInterrupted;
            }
        }

        private void Start()
        {
            lastGrappleTime = Time.time - cooldown + 8f; // Delay awal
        }

        private void Update()
        {
            if (bossEnemy == null || bossEnemy.IsDead || isGrappling) return;
            if (bossEnemy.IsPerformingSkill) return;
            if (Time.time - lastGrappleTime < cooldown) return;
            if (bossEnemy.PlayerTarget == null) return;

            // Cek apakah pemain berada di dalam radius cengkeraman dekat
            float dist = Vector3.Distance(transform.position, bossEnemy.PlayerTarget.position);
            if (dist <= grabRange && !NightBrawlManager.IsInsideHouse(bossEnemy.PlayerTarget.position))
            {
                StartCoroutine(RoutineGrappleSlam());
            }
        }

        private void HandleBossActionInterrupted()
        {
            if (!isGrappling) return;

            StopAllCoroutines();
            ReleaseGrappleLock();
            if (bossEnemy != null)
            {
                bossEnemy.IsPerformingSkill = false;
            }
            isGrappling = false;
            lastGrappleTime = Time.time;
        }

        private void ReleaseGrappleLock()
        {
            if (lockedPlayerContext != null)
            {
                lockedPlayerContext.RemoveInputLock("ColossusGrapple");
                lockedPlayerContext = null;
            }
            else if (bossEnemy != null && bossEnemy.PlayerTarget != null)
            {
                var playerCtrl = bossEnemy.PlayerTarget.GetComponent<IPlayerContext>();
                playerCtrl?.RemoveInputLock("ColossusGrapple");
            }
        }

        private IEnumerator RoutineGrappleSlam()
        {
            EnsureSubscribed();
            isGrappling = true;
            bossEnemy.IsPerformingSkill = true;

            var playerCtrl = bossEnemy.PlayerTarget != null ? bossEnemy.PlayerTarget.GetComponent<IPlayerContext>() : null;

            // 1. Wind-up telegraf cengkeraman (0.4s)
            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2.2f,
                    "⚠️ GRAPPLE REACH!",
                    new Color(1f, 0.4f, 0.1f));
            }

            // Hentikan sejenak dorongan bos
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            yield return new WaitForSeconds(0.4f);

            // 2. Evaluasi apakah bos masih hidup dan target masih berada di dalam jangkauan
            if (bossEnemy != null && !bossEnemy.IsDead && bossEnemy.PlayerTarget != null && !NightBrawlManager.IsInsideHouse(bossEnemy.PlayerTarget.position))
            {
                float currentDist = Vector3.Distance(transform.position, bossEnemy.PlayerTarget.position);
                if (currentDist <= grabRange + 0.6f && playerCtrl != null)
                {
                    // Cengkeraman sukses: Kunci kontrol pemain dengan named lock
                    lockedPlayerContext = playerCtrl;
                    playerCtrl.AddInputLock("ColossusGrapple");

                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            playerCtrl.Transform.position + Vector3.up * 1.8f,
                            "💥 GRAPPLED!",
                            new Color(1f, 0.1f, 0.1f));
                    }

                    // Tahan pemain (hold phase)
                    yield return new WaitForSeconds(holdDuration);

                    if (bossEnemy != null && !bossEnemy.IsDead)
                    {
                        // Bantingan (Slam phase)
                        var playerDmg = playerCtrl.GetPlayerComponent<IDamageable>() ?? bossEnemy.PlayerTarget.GetComponent<IDamageable>();
                        playerDmg?.TakeDamage(grappleDamage, playerCtrl.Transform.position, Vector3.down);

                        // Terapkan stun dan knockback terukur sesuai batasan PlayerControl
                        playerCtrl.ApplyStun(stunDuration);
                        playerCtrl.ApplyKnockback(-transform.forward + Vector3.up * 0.2f, 7.5f);
                    }

                    // Buka kembali kunci kontrol (safety unlock)
                    ReleaseGrappleLock();
                }
                else
                {
                    // Pemain berhasil menghindar / melompat menjauh
                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            transform.position + Vector3.up * 1.8f,
                            "Grapple Evaded!",
                            new Color(0.6f, 0.9f, 0.6f));
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);

            // Jaminan keamanan tambahan jika terjadi interupsi tak terduga
            ReleaseGrappleLock();

            if (bossEnemy != null)
            {
                bossEnemy.IsPerformingSkill = false;
            }
            isGrappling = false;
            lastGrappleTime = Time.time;
        }

        private void OnDisable()
        {
            if (bossEnemy != null)
            {
                bossEnemy.OnActionInterrupted -= HandleBossActionInterrupted;
            }

            if (isGrappling)
            {
                ReleaseGrappleLock();
                if (bossEnemy != null)
                {
                    bossEnemy.IsPerformingSkill = false;
                }
                isGrappling = false;
            }
        }
    }
}
