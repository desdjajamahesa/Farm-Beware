using System.Collections;
using UnityEngine;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Modular Grapple Slam ability for Taro Colossus (Task 2.2).
    /// Checks proximity to the player (radius <= 2.2m).
    /// Temporarily locks player movement (SetControlLock), deals slam damage,
    /// applies stun (ApplyStun), and guarantees a safety unlock so player controls never remain stuck.
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
            lastGrappleTime = Time.time - cooldown + 8f; // Initial delay
        }

        private void Update()
        {
            if (bossEnemy == null || bossEnemy.IsDead || isGrappling) return;
            if (bossEnemy.IsPerformingSkill) return;
            if (Time.time - lastGrappleTime < cooldown) return;
            if (bossEnemy.PlayerTarget == null) return;

            // Check if player is within close grapple radius
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

            // 1. Grapple telegraph wind-up (0.4s)
            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2.2f,
                    "⚠️ GRAPPLE REACH!",
                    new Color(1f, 0.4f, 0.1f));
            }

            // Briefly halt boss momentum
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            yield return new WaitForSeconds(0.4f);

            // 2. Verify boss is still alive and target remains within range
            if (bossEnemy != null && !bossEnemy.IsDead && bossEnemy.PlayerTarget != null && !NightBrawlManager.IsInsideHouse(bossEnemy.PlayerTarget.position))
            {
                float currentDist = Vector3.Distance(transform.position, bossEnemy.PlayerTarget.position);
                if (currentDist <= grabRange + 0.6f && playerCtrl != null)
                {
                    // Grapple succeeded: apply named lock to player controls
                    lockedPlayerContext = playerCtrl;
                    playerCtrl.AddInputLock("ColossusGrapple");

                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            playerCtrl.Transform.position + Vector3.up * 1.8f,
                            "💥 GRAPPLED!",
                            new Color(1f, 0.1f, 0.1f));
                    }

                    // Hold player (hold phase)
                    yield return new WaitForSeconds(holdDuration);

                    if (bossEnemy != null && !bossEnemy.IsDead)
                    {
                        // Slam phase
                        var playerDmg = playerCtrl.GetPlayerComponent<IDamageable>() ?? bossEnemy.PlayerTarget.GetComponent<IDamageable>();
                        playerDmg?.TakeDamage(grappleDamage, playerCtrl.Transform.position, Vector3.down);

                        // Apply stun and measured knockback within PlayerControl bounds
                        playerCtrl.ApplyStun(stunDuration);
                        playerCtrl.ApplyKnockback(-transform.forward + Vector3.up * 0.2f, 7.5f);
                    }

                    // Release control lock (safety unlock)
                    ReleaseGrappleLock();
                }
                else
                {
                    // Player successfully dodged / leaped away
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

            // Additional safety guarantee in case of unexpected interruption
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
