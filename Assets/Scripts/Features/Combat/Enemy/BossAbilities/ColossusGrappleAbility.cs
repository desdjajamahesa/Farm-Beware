using System.Collections;
using UnityEngine;
using PlayerUI;

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

        private void Awake()
        {
            bossEnemy = GetComponent<EnemyBase>();
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

        private IEnumerator RoutineGrappleSlam()
        {
            isGrappling = true;
            bossEnemy.IsPerformingSkill = true;

            var playerCtrl = bossEnemy.PlayerTarget != null ? bossEnemy.PlayerTarget.GetComponent<PlayerControl>() : null;

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

            // 2. Evaluasi apakah target masih berada di dalam jangkauan
            if (bossEnemy.PlayerTarget != null && !NightBrawlManager.IsInsideHouse(bossEnemy.PlayerTarget.position))
            {
                float currentDist = Vector3.Distance(transform.position, bossEnemy.PlayerTarget.position);
                if (currentDist <= grabRange + 0.6f && playerCtrl != null)
                {
                    // Cengkeraman sukses: Kunci kontrol pemain
                    playerCtrl.SetControlLock(true);

                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            playerCtrl.transform.position + Vector3.up * 1.8f,
                            "💥 GRAPPLED!",
                            new Color(1f, 0.1f, 0.1f));
                    }

                    // Tahan pemain (hold phase)
                    yield return new WaitForSeconds(holdDuration);

                    // Bantingan (Slam phase)
                    var playerDmg = playerCtrl.GetComponent<IDamageable>();
                    playerDmg?.TakeDamage(grappleDamage, playerCtrl.transform.position, Vector3.down);

                    // Terapkan stun dan knockback terukur sesuai batasan PlayerControl
                    playerCtrl.ApplyStun(stunDuration);
                    playerCtrl.ApplyKnockback(-transform.forward + Vector3.up * 0.2f, 7.5f);

                    // Buka kembali kunci kontrol (safety unlock)
                    playerCtrl.SetControlLock(false);
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
            if (playerCtrl != null)
            {
                playerCtrl.SetControlLock(false);
            }

            bossEnemy.IsPerformingSkill = false;
            isGrappling = false;
            lastGrappleTime = Time.time;
        }

        private void OnDisable()
        {
            if (isGrappling)
            {
                if (bossEnemy != null && bossEnemy.PlayerTarget != null)
                {
                    var playerCtrl = bossEnemy.PlayerTarget.GetComponent<PlayerControl>();
                    if (playerCtrl != null)
                    {
                        playerCtrl.SetControlLock(false);
                    }
                    bossEnemy.IsPerformingSkill = false;
                }
                isGrappling = false;
            }
        }
    }
}
