using System.Collections;
using UnityEngine;
using PlayerUI;
using FeaturesCombat.Projectiles;

namespace FeaturesCombat
{
    /// <summary>
    /// Kemampuan modular Airborne Slam untuk Taro Colossus (Task 2.2).
    /// Taro Colossus melompat ke udara, memunculkan bayangan penjejak (Tracking Shadow Decal)
    /// yang mengikuti pemain selama beberapa detik, dapat di-interrupt dengan 4 hit stagger,
    /// dan meluncurkan 3 proyektil batu (Rock Projectiles) serta AoE shockwave saat mendarat.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public class ColossusAirborneAbility : MonoBehaviour
    {
        [Header("Airborne Slam Settings")]
        [SerializeField] private float cooldown = 20f;
        [SerializeField] private float trackDuration = 3.5f;
        [SerializeField] private float airborneHeight = 9.0f;
        [SerializeField] private int aoeDamage = 40;
        [SerializeField] private float aoeRadius = 4.5f;
        [SerializeField] private int rockProjectileDamage = 20;
        [SerializeField] private int hitsToInterrupt = 4;

        private EnemyBase bossEnemy;
        private float lastSlamTime = 0f;
        private bool isExecuting = false;
        private int hitsDuringFlight = 0;
        private bool wasInterrupted = false;

        private GameObject trackingDecal;

        private void Awake()
        {
            bossEnemy = GetComponent<EnemyBase>();
        }

        private void Start()
        {
            lastSlamTime = Time.time - cooldown + 6f; // Delay awal di awal pertempuran
        }

        private void Update()
        {
            if (bossEnemy == null || bossEnemy.IsDead || isExecuting) return;
            if (bossEnemy.IsPerformingSkill) return;
            if (Time.time - lastSlamTime < cooldown) return;
            if (bossEnemy.PlayerTarget == null) return;

            StartCoroutine(RoutineAirborneSlam());
        }

        private IEnumerator RoutineAirborneSlam()
        {
            isExecuting = true;
            bossEnemy.IsPerformingSkill = true;
            hitsDuringFlight = 0;
            wasInterrupted = false;

            bossEnemy.OnHealthChanged += HandleHitDuringFlight;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2.5f,
                    "🌋 Colossus Leap!",
                    new Color(0.9f, 0.5f, 0.1f));
            }

            // 1. Naik ke udara
            Vector3 startPos = transform.position;
            Vector3 elevatedPos = startPos + Vector3.up * airborneHeight;
            float ascendTime = 0.5f;
            float t = 0f;
            while (t < ascendTime)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, elevatedPos, t / ascendTime);
                yield return null;
            }
            transform.position = elevatedPos;

            // 2. Buat / Munculkan Tracking Shadow Decal di tanah
            EnsureTrackingDecal();
            if (trackingDecal != null)
            {
                trackingDecal.SetActive(true);
            }

            // 3. Tracking Phase (Bayangan dan Colossus mengikuti pemain di udara)
            float trackTimer = 0f;
            Vector3 targetGroundPos = startPos;
            targetGroundPos.y = 0.05f;

            while (trackTimer < trackDuration)
            {
                trackTimer += Time.deltaTime;

                if (bossEnemy.PlayerTarget != null)
                {
                    Vector3 pPos = bossEnemy.PlayerTarget.position;
                    if (NightBrawlManager.IsInsideHouse(pPos))
                    {
                        pPos = NightBrawlManager.GetNearestOutdoorPosition(pPos, 2.5f);
                    }
                    targetGroundPos = pPos;
                    targetGroundPos.y = 0.05f;
                }

                // Posisikan decal di tanah tepat di bawah Colossus
                if (trackingDecal != null)
                {
                    trackingDecal.transform.position = targetGroundPos;
                }

                // Gerakkan Colossus di atas target
                Vector3 newAirPos = targetGroundPos + Vector3.up * airborneHeight;
                transform.position = Vector3.MoveTowards(transform.position, newAirPos, 8f * Time.deltaTime);

                if (wasInterrupted)
                {
                    break;
                }

                yield return null;
            }

            // Sembunyikan decal tanah
            if (trackingDecal != null)
            {
                trackingDecal.SetActive(false);
            }

            bossEnemy.OnHealthChanged -= HandleHitDuringFlight;

            // 4. Mendarat / Crash Slam
            if (wasInterrupted)
            {
                // Jatuh tak terkontrol (interrupted)
                if (FloatingCombatTextManager.Instance != null)
                {
                    FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 1.5f,
                        "💥 Slam Interrupted!",
                        new Color(0.3f, 0.8f, 1f));
                }

                // Jatuh ke tanah
                Vector3 crashPos = transform.position;
                crashPos.y = targetGroundPos.y;
                transform.position = crashPos;
                yield return new WaitForSeconds(1.0f); // Stun recovery
            }
            else
            {
                // Slam ke tanah secara cepat
                Vector3 finalGroundPos = targetGroundPos;
                float descendTime = 0.25f;
                Vector3 currentAir = transform.position;
                t = 0f;
                while (t < descendTime)
                {
                    t += Time.deltaTime;
                    transform.position = Vector3.Lerp(currentAir, finalGroundPos, t / descendTime);
                    yield return null;
                }
                transform.position = finalGroundPos;

                // AoE Slam Shockwave Damage & Knockback
                if (FloatingCombatTextManager.Instance != null)
                {
                    FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 2f,
                        "💥 EARTHQUAKE SLAM!",
                        new Color(1f, 0.2f, 0.2f));
                }

                if (bossEnemy.PlayerTarget != null && !NightBrawlManager.IsInsideHouse(bossEnemy.PlayerTarget.position))
                {
                    float dist = Vector3.Distance(transform.position, bossEnemy.PlayerTarget.position);
                    if (dist <= aoeRadius)
                    {
                        var playerDmg = bossEnemy.PlayerTarget.GetComponent<IDamageable>();
                        playerDmg?.TakeDamage(aoeDamage, bossEnemy.PlayerTarget.position, (bossEnemy.PlayerTarget.position - transform.position).normalized);

                        var playerCtrl = bossEnemy.PlayerTarget.GetComponent<PlayerControl>();
                        if (playerCtrl != null)
                        {
                            Vector3 kbDir = (bossEnemy.PlayerTarget.position - transform.position).normalized;
                            kbDir.y = 0.2f;
                            playerCtrl.ApplyKnockback(kbDir, 10f);
                        }
                    }
                }

                // 5. Tembakkan 3 Rock Projectiles radial burst
                SpawnRockProjectiles();

                yield return new WaitForSeconds(0.6f);
            }

            if (rb != null)
            {
                rb.isKinematic = false;
            }

            bossEnemy.IsPerformingSkill = false;
            isExecuting = false;
            lastSlamTime = Time.time;
        }

        private void SpawnRockProjectiles()
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            float[] angles = { -35f, 0f, 35f };

            foreach (float angleOffset in angles)
            {
                Quaternion rot = Quaternion.Euler(0f, angleOffset, 0f);
                Vector3 dir = rot * transform.forward;
                dir.y = 0f;
                dir.Normalize();

                CombatProjectile.Spawn(
                    gameObject,
                    origin,
                    dir,
                    rockProjectileDamage,
                    12.5f,
                    new Color(0.75f, 0.45f, 0.20f) // Granite Earth Amber
                );
            }
        }

        private void HandleHitDuringFlight(int currentHp, int maxHp)
        {
            hitsDuringFlight++;
            if (hitsDuringFlight >= hitsToInterrupt)
            {
                wasInterrupted = true;
            }
        }

        private void EnsureTrackingDecal()
        {
            if (trackingDecal != null) return;

            trackingDecal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trackingDecal.name = "Decal_ColossusSlam_Tracking";
            trackingDecal.transform.localScale = new Vector3(aoeRadius * 2f, 0.02f, aoeRadius * 2f);

            var col = trackingDecal.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = trackingDecal.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(0.9f, 0.1f, 0.1f, 0.4f);
                rend.material = mat;
            }

            trackingDecal.SetActive(false);
        }

        private void OnDestroy()
        {
            if (trackingDecal != null)
            {
                Destroy(trackingDecal);
            }
            if (bossEnemy != null)
            {
                bossEnemy.OnHealthChanged -= HandleHitDuringFlight;
            }
        }
    }
}
