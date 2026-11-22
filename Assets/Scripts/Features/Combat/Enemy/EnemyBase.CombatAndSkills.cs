using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    public partial class EnemyBase
    {
        private void TryPerformAttack(float distToPlayer = -1f)
        {
            if (Time.time - lastAttackTime < (1f / Mathf.Max(0.1f, attackRate))) return;
            if (isPerformingSkill) return;

            if (distToPlayer < 0f && playerTarget != null)
            {
                Vector3 toTarget = playerTarget.position - transform.position;
                toTarget.y = 0f;
                distToPlayer = toTarget.magnitude;
            }

            // Attack token check: prevent uncoordinated horde dogpiling
            bool isRanged = (enemyType == EnemyType.CornMusketeer || enemyType == EnemyType.TheRanger);
            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.TokenDispatcher != null)
            {
                var priority = isBoss ? FeaturesCombat.Core.PureLogic.AttackTokenPriority.Boss : FeaturesCombat.Core.PureLogic.AttackTokenPriority.Minion;
                if (!NightBrawlManager.Instance.TokenDispatcher.TryAcquireToken(
                    gameObject.GetInstanceID(), isRanged, Time.time, 2.8f, priority, distToPlayer, isCommitted: true))
                {
                    return; // Denied, wait for token
                }
            }

            // Note: lastAttackTime is stamped upon completion of the attack routine to ensure full cooldown duration
            if (enemyType == EnemyType.CornMusketeer)
            {
                StartCoroutine(RoutineCornMusketeerAttack());
            }
            else if (enemyType == EnemyType.TheRanger)
            {
                StartCoroutine(RoutineTheRangerAttack());
            }
            else
            {
                StartCoroutine(RoutineMeleeTelegraphedAttack());
            }
        }

        /// <summary>
        /// Serangan melee bertelegraf.
        /// Monster berhenti sejenak, memunculkan indikator lingkaran merah di tanah (wind-up 0.35s).
        /// Jika pemain menghindar keluar dari lingkaran, serangan akan luput ("Miss!").
        /// </summary>
        private IEnumerator RoutineMeleeTelegraphedAttack()
        {
            isPerformingSkill = true;
            currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.TelegraphAttack;
            EnsureTelegraphElements();

            // 1. Wind-up Phase (0.35s): Berhenti sejenak, aktifkan indikator telegraf tanah, kedip oranye
            if (rb != null)
            {
                SetLinearVelocity(Vector3.zero);
            }

            float windupDuration = 0.35f;
            float telegraphHitRange = attackRange + 0.55f;
            if (combatTelegraph != null)
            {
                combatTelegraph.StartCharge(windupDuration, telegraphHitRange);
            }

            if (meshRenderer != null)
            {
                SetRendererColor(new Color(1f, 0.45f, 0.15f)); // Wind-up warning color
            }

            float windupTimer = 0f;
            while (windupTimer < windupDuration)
            {
                windupTimer += Time.deltaTime;
                if (playerTarget != null)
                {
                    LookAtTarget(playerTarget.position);
                }
                yield return null;
            }

            // Climax Snap Rotation: firmly align to target vector before triggering hit calculation
            if (playerTarget != null)
            {
                Vector3 snapDir = playerTarget.position - transform.position;
                snapDir.y = 0f;
                if (snapDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(snapDir.normalized);
                }
            }

            if (meshRenderer != null)
            {
                SetRendererColor(originalColor);
            }

            if (combatTelegraph != null)
            {
                combatTelegraph.Hide();
            }

            // 2. Active Strike Phase: Cek apakah pemain masih berada di dalam area serang (planar horizontal XZ)
            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
            {
                Vector3 toPlayer = playerTarget.position - transform.position;
                toPlayer.y = 0f;
                float currentDist = toPlayer.magnitude;
                Vector3 forwardFlat = transform.forward;
                forwardFlat.y = 0f;
                forwardFlat.Normalize();
                float dot = (currentDist > 0.01f) ? Vector3.Dot(forwardFlat, toPlayer.normalized) : 1f;

                // Pemain terkena serangan jika berada di dalam jangkauan melee dan berada di depan atau menempel sangat dekat dengan monster
                if (currentDist <= telegraphHitRange && (dot >= 0.10f || currentDist <= 1.2f))
                {
                    Vector3 enemyChest = transform.position + Vector3.up * 1f;
                    Vector3 playerChest = playerTarget.position + Vector3.up * 1f;
                    Collider playerCol = playerTarget.GetComponent<Collider>() ?? playerTarget.GetComponentInChildren<Collider>();

                    bool isBlockedByWall = FarmBeware.Core.Runtime.NonAllocPhysics.IsLineOfSightBlocked(
                        enemyChest, playerChest, playerCol, gameObject, 0);

                    if (isBlockedByWall)
                    {
                        // Serangan terhalang oleh dinding/tembok solid! Mencegah damage dari balik tembok
                        if (FloatingCombatTextManager.Instance != null)
                        {
                            FloatingCombatTextManager.Instance.SpawnText(
                                transform.position + Vector3.up * 1.6f,
                                "Blocked!",
                                new Color(0.7f, 0.7f, 0.7f, 0.85f));
                        }
                    }
                    else
                    {
                        IDamageable playerDamageable = playerTarget.GetComponent<IDamageable>() ?? playerTarget.GetComponentInParent<IDamageable>();
                        if (playerDamageable != null && !playerDamageable.IsDead)
                        {
                            playerDamageable.TakeDamage(contactDamage, playerTarget.position + Vector3.up * 1f, transform.forward);
                        }
                    }
                }
                else
                {
                    // Pemain berhasil menghindar tepat waktu!
                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            transform.position + Vector3.up * 1.6f,
                            "Miss!",
                            new Color(0.85f, 0.85f, 0.85f, 0.8f));
                    }
                }
            }

            // 3. Recovery Phase (0.18s)
            yield return new WaitForSeconds(0.18f);
            isPerformingSkill = false;
            lastAttackTime = Time.time;
            skillCooldownTimer = Mathf.Min(skillCooldownTimer, Mathf.Max(0f, GetSkillCooldown() - 1.2f));
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), false);
        }

        /// <summary>
        /// Serangan tembakan terarah Corn Musketeer.
        /// Membidik pemain dengan laser sight selama 0.55 detik (0.40s tracking dinamis, 0.15s Aim Lock).
        /// Elevasi tembakan diselaraskan datar (Delta Y = 0) agar kecepatan proyektil di layar simetris 100%.
        /// </summary>
        private IEnumerator RoutineCornMusketeerAttack()
        {
            isPerformingSkill = true;
            currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.TelegraphAttack;
            EnsureTelegraphElements();

            if (rb != null)
            {
                SetLinearVelocity(Vector3.zero);
            }

            // 1. Aiming Telegraph (0.55s total)
            if (laserSightLine != null)
            {
                laserSightLine.enabled = true;
                Color trackingColor = new Color(1f, 0.85f, 0.1f, 0.75f);
                if (laserSightLine.material != null)
                {
                    laserSightLine.material.color = trackingColor;
                    if (laserSightLine.material.HasProperty("_BaseColor"))
                        laserSightLine.material.SetColor("_BaseColor", trackingColor);
                }
            }

            // Ambil referensi collider untuk mengukur tinggi dasar kaki di atas tanah (terlepas dari posisi pivot 3D mesh)
            Collider myCol = GetComponent<Collider>();
            float feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
            Vector3 muzzlePos = transform.position + transform.forward * 0.6f;
            muzzlePos.y = feetY + 1.0f; // Tepat setinggi dada (1.0m di atas tanah)

            Vector3 aimDir = transform.forward;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = Vector3.forward;
            aimDir.Normalize();

            float aimTimer = 0f;
            while (aimTimer < 0.55f)
            {
                aimTimer += Time.deltaTime;

                // Tracking Phase (0 s.d. 0.40s): Membidik dan mengikuti posisi pemain
                if (aimTimer <= 0.40f)
                {
                    if (playerTarget != null)
                    {
                        LookAtTarget(playerTarget.position);

                        feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
                        muzzlePos = transform.position + transform.forward * 0.6f;
                        muzzlePos.y = feetY + 1.0f;

                        Collider playerCol = playerTarget.GetComponent<Collider>();
                        Vector3 targetPos = playerCol != null ? playerCol.bounds.center : (playerTarget.position + Vector3.up * 1.0f);

                        Vector3 toTarget = targetPos - muzzlePos;
                        if (toTarget.sqrMagnitude > 0.001f)
                        {
                            aimDir = toTarget.normalized;
                        }

                        if (laserSightLine != null)
                        {
                            laserSightLine.SetPosition(0, muzzlePos);
                            laserSightLine.SetPosition(1, targetPos);
                        }
                    }
                }
                // Aim Freeze Phase (0.40s s.d. 0.55s): Arah tembakan terkunci! Berikan sinyal merah peringatan
                else
                {
                    if (laserSightLine != null)
                    {
                        Color lockColor = new Color(1f, 0.25f, 0.1f, 0.95f);
                        if (laserSightLine.material != null)
                        {
                            laserSightLine.material.color = lockColor;
                            if (laserSightLine.material.HasProperty("_BaseColor"))
                                laserSightLine.material.SetColor("_BaseColor", lockColor);
                        }
                        laserSightLine.SetPosition(0, muzzlePos);
                        laserSightLine.SetPosition(1, muzzlePos + aimDir * attackRange);
                    }
                }

                yield return null;
            }

            if (laserSightLine != null)
            {
                laserSightLine.enabled = false;
            }

            // 2. Firing: Tembakkan proyektil fisik tepat dari ketinggian dada Corn ke dada pemain
            int shotDamage = contactDamage > 0 ? contactDamage : 15;
            FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                gameObject,
                muzzlePos,
                aimDir,
                damageAmount: shotDamage,
                projSpeed: 13.5f,
                projectileColor: new Color(1f, 0.85f, 0.15f));

            // Recoil kick mundur sejenak
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(-aimDir * 2.5f, ForceMode.Impulse);
            }

            // 3. Recovery (0.2s)
            yield return new WaitForSeconds(0.2f);
            isPerformingSkill = false;
            lastAttackTime = Time.time;
            skillCooldownTimer = Mathf.Min(skillCooldownTimer, Mathf.Max(0f, GetSkillCooldown() - 1.2f));
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), true);
        }

        /// <summary>
        /// Serangan proyektil artileri berat The Ranger (Boss Stationary Turret).
        /// Membidik pemain dengan laser merah peringatan (0.45s telegraph) lalu menembakkan proyektil artileri mortar berdaya hancur tinggi.
        /// </summary>
        private IEnumerator RoutineTheRangerAttack()
        {
            isPerformingSkill = true;
            currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.TelegraphAttack;
            EnsureTelegraphElements();

            if (rb != null)
            {
                SetLinearVelocity(Vector3.zero);
            }

            // 1. Aiming Telegraph (0.45s)
            if (laserSightLine != null)
            {
                laserSightLine.enabled = true;
                Color aimColor = new Color(1f, 0.2f, 0.1f, 0.85f);
                if (laserSightLine.material != null)
                {
                    laserSightLine.material.color = aimColor;
                    if (laserSightLine.material.HasProperty("_BaseColor"))
                        laserSightLine.material.SetColor("_BaseColor", aimColor);
                }
            }

            Collider myCol = GetComponent<Collider>();
            float feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
            Vector3 muzzlePos = transform.position + transform.forward * 0.8f;
            muzzlePos.y = feetY + 1.2f;

            Vector3 aimDir = transform.forward;
            float aimTimer = 0f;
            while (aimTimer < 0.45f)
            {
                aimTimer += Time.deltaTime;
                if (playerTarget != null)
                {
                    LookAtTarget(playerTarget.position);

                    Collider playerCol = playerTarget.GetComponent<Collider>();
                    Vector3 targetPos = playerCol != null ? playerCol.bounds.center : (playerTarget.position + Vector3.up * 1.0f);
                    Vector3 toTarget = targetPos - muzzlePos;
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        aimDir = toTarget.normalized;
                    }

                    if (laserSightLine != null)
                    {
                        laserSightLine.SetPosition(0, muzzlePos);
                        laserSightLine.SetPosition(1, targetPos);
                    }
                }
                yield return null;
            }

            if (laserSightLine != null)
            {
                laserSightLine.enabled = false;
            }

            // 2. Firing: Tembakkan artileri mortar jagung berkecepatan tinggi
            FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                gameObject,
                muzzlePos,
                aimDir,
                damageAmount: contactDamage,
                projSpeed: 16.0f,
                projectileColor: new Color(1f, 0.35f, 0.1f));

            // 3. Recovery Phase (0.25s)
            yield return new WaitForSeconds(0.25f);
            isPerformingSkill = false;
            lastAttackTime = Time.time;
            skillCooldownTimer = Mathf.Min(skillCooldownTimer, Mathf.Max(0f, GetSkillCooldown() - 1.2f));
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), true);
        }

        private float GetSkillCooldown()
        {
            switch (enemyType)
            {
                case EnemyType.TuberMaw: return 5f;
                case EnemyType.CyclopsTuberMaw: return 15f;
                case EnemyType.TaroBrute: return 12f;
                case EnemyType.TaroColossus: return 18f;
                case EnemyType.CornMusketeer: return 10f;
                case EnemyType.TheRanger: return 14f;
                default: return 10f;
            }
        }

        private void TryExecuteSkill(float distToPlayer)
        {
            // Offensive skills must respect AttackTokenDispatcher concurrency limits to prevent synchronized horde dogpiling
            bool isDefensiveSkill = (enemyType == EnemyType.TaroBrute);
            bool isRanged = (enemyType == EnemyType.CornMusketeer || enemyType == EnemyType.TheRanger);

            if (!isDefensiveSkill && NightBrawlManager.Instance != null && NightBrawlManager.Instance.TokenDispatcher != null)
            {
                var priority = isBoss ? FeaturesCombat.Core.PureLogic.AttackTokenPriority.Boss : FeaturesCombat.Core.PureLogic.AttackTokenPriority.Minion;
                if (!NightBrawlManager.Instance.TokenDispatcher.TryAcquireToken(
                    gameObject.GetInstanceID(), isRanged, Time.time, 3.2f, priority, distToPlayer, isCommitted: true))
                {
                    return;
                }
            }

            skillCooldownTimer = 0f;

            switch (enemyType)
            {
                case EnemyType.TuberMaw:
                    StartCoroutine(RoutineBurrowStrike());
                    break;

                case EnemyType.CyclopsTuberMaw:
                    StartCoroutine(RoutineTrackingBeam());
                    break;

                case EnemyType.TaroBrute:
                    StartCoroutine(RoutineRootGuard());
                    break;

                case EnemyType.TaroColossus:
                    if (GetComponent<ColossusAirborneAbility>() == null)
                    {
                        StartCoroutine(RoutineAirborneSlam());
                    }
                    else
                    {
                        NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), false);
                    }
                    break;

                case EnemyType.CornMusketeer:
                    StartCoroutine(RoutineKernelShot());
                    break;

                case EnemyType.TheRanger:
                    StartCoroutine(RoutineCornHeavensFall());
                    break;
            }
        }

        // --- LOW HP RETREAT & HEAL ROUTINE ---

        private IEnumerator RoutineRetreatAndHeal()
        {
            isRetreatingAndHealing = true;
            isPerformingSkill = true;
            lastRetreatHealTime = Time.time;

            float retreatTimer = 0f;

            // 1. Sedikit menjauh dari pemain (lari mundur teratur sampai jarak aman ~7m atau maksimal 1.8s)
            while (retreatTimer < maxRetreatDuration && !IsDead)
            {
                retreatTimer += Time.deltaTime;

                if (playerTarget == null)
                {
                    FindPlayerTarget();
                }

                if (playerTarget != null)
                {
                    Vector3 awayFromPlayer = transform.position - playerTarget.position;
                    awayFromPlayer.y = 0f;
                    float dist = awayFromPlayer.magnitude;

                    // Jika sudah sedikit menjauh dan berada di jarak aman, hentikan lari mundur dan mulai proses pemulihan
                    if (dist >= retreatSafeDistance)
                    {
                        break;
                    }

                    Vector3 fleeDir = awayFromPlayer.normalized;
                    if (fleeDir.sqrMagnitude < 0.001f)
                    {
                        fleeDir = -transform.forward;
                    }

                    // Terapkan Dynamic Obstacle Avoidance agar tidak menabrak rintangan atau terjebak di sudut
                    Vector3 moveDir = ApplyObstacleAvoidance(fleeDir);

                    // Cegah mundur menembus ke dalam rumah modular
                    if (NightBrawlManager.IsInsideHouse(transform.position + moveDir * 0.5f))
                    {
                        Vector3 safeOutdoor = NightBrawlManager.GetNearestOutdoorPosition(transform.position, 1.5f);
                        moveDir = (safeOutdoor - transform.position).normalized;
                    }

                    if (rb != null)
                    {
                        SetLinearVelocity(new Vector3(moveDir.x * (moveSpeed * 1.35f), GetLinearVelocityY(), moveDir.z * (moveSpeed * 1.35f)));
                    }

                    if (moveDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 12f);
                    }
                }

                yield return null;
            }

            if (IsDead)
            {
                isRetreatingAndHealing = false;
                isPerformingSkill = false;
                yield break;
            }

            // Hentikan gerak mundur
            if (rb != null)
            {
                SetLinearVelocity(new Vector3(0f, GetLinearVelocityY(), 0f));
            }

            // 2. Heal diri sendiri sampai 50 current health
            if (FloatingCombatTextManager.Instance != null)
            {
                int healRecovered = Mathf.Max(0, retreatHealTargetHealth - currentHealth);
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.5f,
                    $"💚 +{healRecovered} HP (Recovered)",
                    new Color(0.2f, 1f, 0.4f)
                );
            }

            // Visual flash warna hijau pemulihan
            if (meshRenderer != null)
            {
                SetRendererColor(new Color(0.25f, 1f, 0.45f));
            }

            yield return new WaitForSeconds(0.7f);

            if (meshRenderer != null)
            {
                SetRendererColor(originalColor);
            }

            if (!IsDead)
            {
                currentHealth = Mathf.Min(maxHealth, retreatHealTargetHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }

            yield return new WaitForSeconds(0.2f);

            // 3. Selesai heal, langsung kembali agresif mengejar dan menyerang player lagi
            isRetreatingAndHealing = false;
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.5f;
        }

        // --- SKILL ROUTINES ---

        private IEnumerator RoutineBurrowStrike()
        {
            isPerformingSkill = true;
            isBurrowed = true;
            EnsureTelegraphElements();

            // Sembunyi / masuk tanah
            Vector3 startScale = transform.localScale;
            transform.localScale = new Vector3(startScale.x, 0.1f, startScale.z);

            // Phase 1: Track posisi pemain di bawah tanah hingga mendekat (maksimal 1.2 detik)
            float timer = 0f;
            while (timer < 1.2f)
            {
                timer += Time.deltaTime;
                if (playerTarget != null)
                {
                    Vector3 burrowDest = playerTarget.position;
                    if (NightBrawlManager.IsInsideHouse(burrowDest))
                    {
                        burrowDest = NightBrawlManager.GetNearestOutdoorPosition(burrowDest, 2.0f);
                    }
                    transform.position = Vector3.MoveTowards(transform.position, burrowDest, (moveSpeed * 1.8f) * Time.deltaTime);

                    Vector3 diff = burrowDest - transform.position;
                    diff.y = 0f;
                    if (diff.magnitude <= 1.2f)
                    {
                        break;
                    }
                }
                yield return null;
            }

            // Phase 2: Hentikan gerak dan aktifkan telegraf bahaya (0.55s wind-up)
            if (rb != null)
            {
                rb.position = transform.position;
                SetLinearVelocity(Vector3.zero);
            }

            float telegraphDuration = 0.55f;
            float eruptionRadius = 2.4f;
            if (combatTelegraph != null)
            {
                combatTelegraph.StartCharge(telegraphDuration, eruptionRadius);
            }

            if (FloatingCombatTextManager.Instance != null && playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= 4.0f)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 0.5f,
                    "⚠️ Erupting!",
                    new Color(1f, 0.35f, 0.1f));
            }

            float chargeTimer = 0f;
            while (chargeTimer < telegraphDuration)
            {
                chargeTimer += Time.deltaTime;
                yield return null;
            }

            // Phase 3: Erupsi muncul dan berikan damage area
            transform.localScale = startScale;
            isBurrowed = false;

            if (combatTelegraph != null)
            {
                combatTelegraph.Hide();
            }

            if (rb != null)
            {
                rb.position = transform.position;
                SetLinearVelocity(Vector3.zero);
            }
            Physics.SyncTransforms();

            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position) && Vector3.Distance(transform.position, playerTarget.position) <= eruptionRadius)
            {
                Vector3 enemyChest = transform.position + Vector3.up * 1f;
                Vector3 playerChest = playerTarget.position + Vector3.up * 1f;
                Collider playerCol = playerTarget.GetComponent<Collider>() ?? playerTarget.GetComponentInChildren<Collider>();
                if (!FarmBeware.Core.Runtime.NonAllocPhysics.IsLineOfSightBlocked(enemyChest, playerChest, playerCol, gameObject, 0))
                {
                    var target = playerTarget.GetComponent<IDamageable>();
                    target?.TakeDamage(contactDamage + 10, playerTarget.position, Vector3.up);
                }
            }

            yield return new WaitForSeconds(0.35f);
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.6f;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), false);
        }

        private IEnumerator RoutineTrackingBeam()
        {
            isPerformingSkill = true;

            // Charge beam selama 1 detik
            float charge = 0f;
            while (charge < 1.0f)
            {
                charge += Time.deltaTime;
                if (playerTarget != null) LookAtTarget(playerTarget.position);
                yield return null;
            }

            // Tembakkan laser membidik ke arah dada/tengah tubuh pemain
            Vector3 eyeOrigin = transform.position + Vector3.up * 0.5f;
            Vector3 targetChest = (playerTarget != null) ? playerTarget.position + Vector3.up * 1.0f : (eyeOrigin + transform.forward * 10f);
            Vector3 shootDir = (targetChest - eyeOrigin).normalized;

            int hitCount = Physics.RaycastNonAlloc(eyeOrigin, shootDir, _beamHitsBuffer, 20f, ~0, QueryTriggerInteraction.Ignore);
            int closestIdx = -1;
            float closestDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                var col = _beamHitsBuffer[i].collider;
                if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
                if (_beamHitsBuffer[i].distance < closestDist)
                {
                    closestDist = _beamHitsBuffer[i].distance;
                    closestIdx = i;
                }
            }

            if (closestIdx >= 0)
            {
                var hit = _beamHitsBuffer[closestIdx];
                var target = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && hit.collider.CompareTag("Player") && !NightBrawlManager.IsInsideHouse(hit.point))
                {
                    target.TakeDamage(25, hit.point, shootDir);
                }
            }

            yield return new WaitForSeconds(0.5f);
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.6f;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), false);
        }

        private IEnumerator RoutineRootGuard()
        {
            isPerformingSkill = true;
            isRootGuarded = true;
            armor = Mathf.RoundToInt(originalArmor * 1.5f);

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.8f,
                    "🛡️ Root Guard (+50% Armor)",
                    new Color(0.4f, 0.9f, 0.4f));
            }

            yield return new WaitForSeconds(6f);

            armor = originalArmor;
            isRootGuarded = false;
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.5f;
        }

        private IEnumerator RoutineAirborneSlam()
        {
            isPerformingSkill = true;

            // Loncat tinggi ke udara
            Vector3 groundPos = transform.position;
            transform.position = groundPos + Vector3.up * 12f;

            yield return new WaitForSeconds(1.5f);

            // Turun menghantam tanah di dekat pemain (atau di batas luar jika pemain di dalam rumah)
            Vector3 slamTarget = playerTarget != null ? playerTarget.position : groundPos;
            if (NightBrawlManager.IsInsideHouse(slamTarget))
            {
                slamTarget = NightBrawlManager.GetNearestOutdoorPosition(slamTarget, 2.5f);
            }
            transform.position = slamTarget;
            if (rb != null)
            {
                rb.position = slamTarget;
                SetLinearVelocity(Vector3.zero);
            }
            Physics.SyncTransforms();

            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position) && Vector3.Distance(transform.position, playerTarget.position) <= 4f)
            {
                Vector3 enemyChest = transform.position + Vector3.up * 1f;
                Vector3 playerChest = playerTarget.position + Vector3.up * 1f;
                Collider playerCol = playerTarget.GetComponent<Collider>() ?? playerTarget.GetComponentInChildren<Collider>();
                if (!FarmBeware.Core.Runtime.NonAllocPhysics.IsLineOfSightBlocked(enemyChest, playerChest, playerCol, gameObject, 0))
                {
                    var target = playerTarget.GetComponent<IDamageable>();
                    target?.TakeDamage(35, slamTarget, Vector3.up);
                }
            }

            yield return new WaitForSeconds(0.6f);
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.8f;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), false);
        }

        private IEnumerator RoutineKernelShot()
        {
            isPerformingSkill = true;
            EnsureTelegraphElements();

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2f,
                    "🌽 Kernel Burst!",
                    new Color(1f, 0.8f, 0.2f));
            }

            // Charge sejenak (0.4s)
            yield return new WaitForSeconds(0.4f);

            Collider myCol = GetComponent<Collider>();

            // Tembakkan 3 butir proyektil jagung beruntun tepat setinggi dada
            for (int i = 0; i < 3; i++)
            {
                if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
                {
                    LookAtTarget(playerTarget.position);

                    float feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
                    Vector3 muzzlePos = transform.position + transform.forward * 0.6f;
                    muzzlePos.y = feetY + 1.0f;

                    Collider playerCol = playerTarget.GetComponent<Collider>();
                    Vector3 targetPos = playerCol != null ? playerCol.bounds.center : (playerTarget.position + Vector3.up * 1.0f);

                    Vector3 toTarget = targetPos - muzzlePos;
                    Vector3 aimDir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;

                    int burstDamage = contactDamage > 0 ? contactDamage : 15;
                    FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                        gameObject,
                        muzzlePos,
                        aimDir,
                        damageAmount: burstDamage,
                        projSpeed: 14.5f,
                        projectileColor: new Color(1f, 0.65f, 0.1f));

                    if (rb != null && !rb.isKinematic)
                    {
                        rb.AddForce(-aimDir * 1.8f, ForceMode.Impulse);
                    }
                }
                yield return new WaitForSeconds(0.14f);
            }

            yield return new WaitForSeconds(0.3f);
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.6f;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), true);
        }

        private IEnumerator RoutineCornHeavensFall()
        {
            isPerformingSkill = true;

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2f,
                    "🌽 Heavens Fall!",
                    new Color(1f, 0.6f, 0.2f));
            }

            yield return new WaitForSeconds(1.2f);

            // Jika pemain berada di dalam rumah, atap melindungi sepenuhnya (0 damage)
            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
            {
                var target = playerTarget.GetComponent<IDamageable>();
                target?.TakeDamage(30, playerTarget.position, Vector3.down);
            }

            yield return new WaitForSeconds(0.5f);
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.8f;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), true);
        }
    }
}
