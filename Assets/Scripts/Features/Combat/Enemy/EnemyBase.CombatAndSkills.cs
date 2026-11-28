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
        /// Telegraphed melee attack driven by EnemyCombatProfile.
        /// Rotates only during the tracking window; locks strike heading cleanly at TrackingCutoffRatio
        /// so player sidesteps evade fairly instead of being undone by impact snap rotation.
        /// </summary>
        private IEnumerator RoutineMeleeTelegraphedAttack()
        {
            isPerformingSkill = true;
            currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.TelegraphAttack;
            EnsureTelegraphElements();

            var profile = FeaturesCombat.Core.PureLogic.EnemyCombatProfile.GetProfile(enemyType);
            IsTrackingLocked = false;
            IsInRecoveryPunishWindow = false;
            CurrentWindupProgress = 0f;
            Vector3 lockedDir = transform.forward;
            lockedDir.y = 0f;
            LockedStrikeDirection = lockedDir.sqrMagnitude > 0.001f ? lockedDir.normalized : Vector3.forward;

            if (rb != null)
            {
                SetLinearVelocity(Vector3.zero);
            }

            if (profile.WindupSuperArmor && poiseTracker != null)
            {
                poiseTracker.IsSuperArmorActive = true;
            }

            // 1. Wind-up Phase (Archetype Duration): Tracking Phase -> Cutoff Lock-Off Phase
            float windupDuration = profile.WindupDurationSec;
            float telegraphHitRange = attackRange + 0.55f;
            if (combatTelegraph != null)
            {
                combatTelegraph.SetColors(new Color(1.8f, 0.35f, 0.1f, 0.95f), new Color(1.0f, 0.25f, 0.05f, 0.35f));
                combatTelegraph.StartCharge(windupDuration, telegraphHitRange);
            }

            if (meshRenderer != null)
            {
                SetRendererColor(new Color(1f, 0.50f, 0.18f));
            }

            float windupTimer = 0f;
            while (windupTimer < windupDuration)
            {
                windupTimer += Time.deltaTime;
                bool stillTracking = FeaturesCombat.Core.PureLogic.EnemyCombatProfile.EvaluateWindupTracking(
                    profile, windupTimer, out bool lockedNow, out float normProg);
                CurrentWindupProgress = normProg;

                if (stillTracking)
                {
                    if (playerTarget != null)
                    {
                        LookAtTarget(playerTarget.position);
                    }
                    Vector3 flatFwd = transform.forward;
                    flatFwd.y = 0f;
                    if (flatFwd.sqrMagnitude > 0.001f)
                    {
                        LockedStrikeDirection = flatFwd.normalized;
                    }
                }
                else if (!IsTrackingLocked && lockedNow)
                {
                    IsTrackingLocked = true;
                    combatTelegraph?.SetColors(new Color(2.2f, 0.12f, 0.08f, 1.0f), new Color(1.3f, 0.15f, 0.05f, 0.55f));
                    if (meshRenderer != null)
                    {
                        SetRendererColor(new Color(1.3f, 0.22f, 0.08f));
                    }
                }

                yield return null;
            }

            // Lock rotation strictly to LockedStrikeDirection (no climax snap rotation!)
            if (LockedStrikeDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(LockedStrikeDirection, Vector3.up);
            }

            if (meshRenderer != null)
            {
                SetRendererColor(originalColor);
            }

            if (combatTelegraph != null)
            {
                combatTelegraph.Hide();
            }

            // 2. Active Strike Phase: Evaluate against committed strike cone & reach
            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
            {
                Vector3 toPlayer = playerTarget.position - transform.position;
                toPlayer.y = 0f;
                float currentDist = toPlayer.magnitude;
                float dot = (currentDist > 0.01f) ? Vector3.Dot(LockedStrikeDirection, toPlayer.normalized) : 1f;

                if (currentDist <= telegraphHitRange && (dot >= profile.HitboxConeDot || currentDist <= 0.75f))
                {
                    Vector3 enemyChest = transform.position + Vector3.up * 1f;
                    Vector3 playerChest = playerTarget.position + Vector3.up * 1f;
                    Collider playerCol = playerTarget.GetComponent<Collider>() ?? playerTarget.GetComponentInChildren<Collider>();

                    bool isBlockedByWall = FarmBeware.Core.Runtime.NonAllocPhysics.IsLineOfSightBlocked(
                        enemyChest, playerChest, playerCol, gameObject, 0);

                    if (isBlockedByWall)
                    {
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
                            playerDamageable.TakeDamage(contactDamage, playerTarget.position + Vector3.up * 1f, LockedStrikeDirection);
                        }
                    }
                }
                else
                {
                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            transform.position + Vector3.up * 1.6f,
                            "Miss!",
                            new Color(0.85f, 0.85f, 0.85f, 0.8f));
                    }
                }
            }

            // 3. Archetype Recovery Punish Window
            IsInRecoveryPunishWindow = true;
            if (poiseTracker != null)
            {
                poiseTracker.IsSuperArmorActive = poiseTracker.HasInnateSuperArmor;
            }
            yield return new WaitForSeconds(profile.RecoveryPunishSec);

            IsInRecoveryPunishWindow = false;
            IsTrackingLocked = false;
            CurrentWindupProgress = 0f;
            isPerformingSkill = false;
            lastAttackTime = Time.time;
            skillCooldownTimer = Mathf.Min(skillCooldownTimer, Mathf.Max(0f, GetSkillCooldown() - 1.2f));
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), false);
        }

        /// <summary>
        /// Aimed ranged shot for Corn Musketeer.
        /// Aims at the player with a laser sight for 0.55s (0.40s dynamic tracking, 0.15s Aim Lock).
        /// Shot elevation is leveled flat (Delta Y = 0) so on-screen projectile velocity stays 100% symmetric.
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

            // Reference collider to measure ground foot height (independent of 3D mesh pivot)
            Collider myCol = GetComponent<Collider>();
            float feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
            Vector3 muzzlePos = transform.position + transform.forward * 0.6f;
            muzzlePos.y = feetY + 1.0f; // Exact chest height (1.0m above ground)

            Vector3 aimDir = transform.forward;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = Vector3.forward;
            aimDir.Normalize();

            float aimTimer = 0f;
            while (aimTimer < 0.55f)
            {
                aimTimer += Time.deltaTime;

                // Tracking Phase (0 to 0.40s): Track and follow the player's position
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
                // Aim Freeze Phase (0.40s to 0.55s): Aim direction locked! Show red warning signal
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

            // 2. Firing: Launch physical projectile from Corn chest height toward player chest
            int shotDamage = contactDamage > 0 ? contactDamage : 15;
            FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                gameObject,
                muzzlePos,
                aimDir,
                damageAmount: shotDamage,
                projSpeed: 13.5f,
                projectileColor: new Color(1f, 0.85f, 0.15f));

            // Brief backward recoil kick
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
        /// Heavy artillery projectile attack for The Ranger (Boss Stationary Turret).
        /// Aims at the player with a red warning laser (0.45s telegraph), then fires a high-damage mortar shell.
        /// </summary>
        private IEnumerator RoutineTheRangerAttack()
        {
            isPerformingSkill = true;
            currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.TelegraphAttack;
            EnsureTelegraphElements();

            var profile = FeaturesCombat.Core.PureLogic.EnemyCombatProfile.GetProfile(EnemyType.TheRanger);
            IsTrackingLocked = false;
            IsInRecoveryPunishWindow = false;
            CurrentWindupProgress = 0f;

            if (rb != null)
            {
                SetLinearVelocity(Vector3.zero);
            }

            // 1. Aiming Telegraph (Tracking -> Lock-Off at TrackingCutoffRatio)
            if (laserSightLine != null)
            {
                laserSightLine.enabled = true;
                Color aimColor = new Color(1f, 0.85f, 0.1f, 0.8f);
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
            while (aimTimer < profile.WindupDurationSec)
            {
                aimTimer += Time.deltaTime;
                bool stillTracking = FeaturesCombat.Core.PureLogic.EnemyCombatProfile.EvaluateWindupTracking(
                    profile, aimTimer, out bool lockedNow, out float normProg);
                CurrentWindupProgress = normProg;

                if (stillTracking)
                {
                    if (playerTarget != null)
                    {
                        LookAtTarget(playerTarget.position);
                        feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
                        muzzlePos = transform.position + transform.forward * 0.8f;
                        muzzlePos.y = feetY + 1.2f;

                        Collider playerCol = playerTarget.GetComponent<Collider>();
                        Vector3 targetPos = playerCol != null ? playerCol.bounds.center : (playerTarget.position + Vector3.up * 1.0f);
                        Vector3 toTarget = targetPos - muzzlePos;
                        if (toTarget.sqrMagnitude > 0.001f)
                        {
                            aimDir = toTarget.normalized;
                            LockedStrikeDirection = aimDir;
                        }

                        if (laserSightLine != null)
                        {
                            laserSightLine.SetPosition(0, muzzlePos);
                            laserSightLine.SetPosition(1, targetPos);
                        }
                    }
                }
                else if (!IsTrackingLocked && lockedNow)
                {
                    IsTrackingLocked = true;
                    if (laserSightLine != null)
                    {
                        Color lockColor = new Color(1f, 0.18f, 0.08f, 0.98f);
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

            // 2. Firing: Launch high-velocity corn mortar shell along committed aimDir
            FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                gameObject,
                muzzlePos,
                aimDir,
                damageAmount: contactDamage,
                projSpeed: 16.0f,
                projectileColor: new Color(1f, 0.35f, 0.1f));

            // 3. Recovery Punish Phase
            IsInRecoveryPunishWindow = true;
            yield return new WaitForSeconds(profile.RecoveryPunishSec);
            IsInRecoveryPunishWindow = false;
            IsTrackingLocked = false;
            CurrentWindupProgress = 0f;
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

            // 1. Retreat orderly from player (up to ~7m safe distance or 1.8s max)
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

                    // Once at a safe distance, stop retreating and begin recovery
                    if (dist >= retreatSafeDistance)
                    {
                        break;
                    }

                    Vector3 fleeDir = awayFromPlayer.normalized;
                    if (fleeDir.sqrMagnitude < 0.001f)
                    {
                        fleeDir = -transform.forward;
                    }

                    // Apply Dynamic Obstacle Avoidance to avoid hitting obstacles or getting cornered
                    Vector3 moveDir = ApplyObstacleAvoidance(fleeDir);

                    // Prevent retreating into the modular house interior
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

            // Halt retreat movement
            if (rb != null)
            {
                SetLinearVelocity(new Vector3(0f, GetLinearVelocityY(), 0f));
            }

            // 2. Heal self up to 50 current health
            if (FloatingCombatTextManager.Instance != null)
            {
                int healRecovered = Mathf.Max(0, retreatHealTargetHealth - currentHealth);
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.5f,
                    $"💚 +{healRecovered} HP (Recovered)",
                    new Color(0.2f, 1f, 0.4f)
                );
            }

            // Green recovery visual flash
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

            // 3. Healing complete; immediately resume aggressive pursuit and attacks
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

            // Submerge underground
            Vector3 startScale = transform.localScale;
            transform.localScale = new Vector3(startScale.x, 0.1f, startScale.z);

            // Phase 1: Track player position underground until close (1.2s max)
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

            // Phase 2: Halt movement and activate danger telegraph (0.55s wind-up)
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

            // Phase 3: Erupt from underground and deal area damage
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
            EnsureTelegraphElements();
            IsTrackingLocked = false;
            CurrentWindupProgress = 0f;

            Vector3 eyeOrigin = transform.position + Vector3.up * 0.5f;
            Vector3 shootDir = transform.forward;
            if (laserSightLine != null)
            {
                laserSightLine.enabled = true;
                Color trackColor = new Color(1f, 0.85f, 0.1f, 0.8f);
                if (laserSightLine.material != null)
                {
                    laserSightLine.material.color = trackColor;
                    if (laserSightLine.material.HasProperty("_BaseColor"))
                        laserSightLine.material.SetColor("_BaseColor", trackColor);
                }
            }

            // Charge beam for 1.0s: Track for 0.65s, Lock direction for final 0.35s so sidestepping dodges cleanly
            float charge = 0f;
            const float totalCharge = 1.0f;
            const float lockTime = 0.65f;
            while (charge < totalCharge)
            {
                charge += Time.deltaTime;
                CurrentWindupProgress = Mathf.Clamp01(charge / totalCharge);
                eyeOrigin = transform.position + Vector3.up * 0.5f;

                if (charge <= lockTime)
                {
                    if (playerTarget != null)
                    {
                        LookAtTarget(playerTarget.position);
                        Vector3 targetChest = playerTarget.position + Vector3.up * 1.0f;
                        Vector3 toTarget = targetChest - eyeOrigin;
                        if (toTarget.sqrMagnitude > 0.001f) shootDir = toTarget.normalized;
                        LockedStrikeDirection = shootDir;
                        if (laserSightLine != null)
                        {
                            laserSightLine.SetPosition(0, eyeOrigin);
                            laserSightLine.SetPosition(1, targetChest);
                        }
                    }
                }
                else if (!IsTrackingLocked)
                {
                    IsTrackingLocked = true;
                    if (laserSightLine != null)
                    {
                        Color lockColor = new Color(1f, 0.18f, 0.08f, 0.98f);
                        if (laserSightLine.material != null)
                        {
                            laserSightLine.material.color = lockColor;
                            if (laserSightLine.material.HasProperty("_BaseColor"))
                                laserSightLine.material.SetColor("_BaseColor", lockColor);
                        }
                        laserSightLine.SetPosition(0, eyeOrigin);
                        laserSightLine.SetPosition(1, eyeOrigin + shootDir * 16f);
                    }
                }
                yield return null;
            }

            if (laserSightLine != null) laserSightLine.enabled = false;

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

            IsInRecoveryPunishWindow = true;
            yield return new WaitForSeconds(0.5f);
            IsInRecoveryPunishWindow = false;
            IsTrackingLocked = false;
            CurrentWindupProgress = 0f;
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
            EnsureTelegraphElements();
            IsTrackingLocked = false;

            // Leap high into the air
            Vector3 groundPos = transform.position;
            transform.position = groundPos + Vector3.up * 12f;

            // Hover & track for 0.90s, then lock slam target and project ground telegraph for final 0.60s
            yield return new WaitForSeconds(0.90f);

            Vector3 slamTarget = playerTarget != null ? playerTarget.position : groundPos;
            if (NightBrawlManager.IsInsideHouse(slamTarget))
            {
                slamTarget = NightBrawlManager.GetNearestOutdoorPosition(slamTarget, 2.5f);
            }
            IsTrackingLocked = true;
            if (combatTelegraph != null)
            {
                combatTelegraph.transform.position = slamTarget + Vector3.up * 0.05f;
                combatTelegraph.SetColors(new Color(2.2f, 0.12f, 0.08f, 1.0f), new Color(1.3f, 0.15f, 0.05f, 0.50f));
                combatTelegraph.StartCharge(0.60f, 4.0f);
            }

            yield return new WaitForSeconds(0.60f);

            if (combatTelegraph != null)
            {
                combatTelegraph.Hide();
                combatTelegraph.transform.localPosition = new Vector3(0f, 0.04f, 0f);
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

            IsInRecoveryPunishWindow = true;
            yield return new WaitForSeconds(0.6f);
            IsInRecoveryPunishWindow = false;
            IsTrackingLocked = false;
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

            // Brief charge (0.4s)
            yield return new WaitForSeconds(0.4f);

            Collider myCol = GetComponent<Collider>();

            // Fire 3 consecutive corn projectiles at chest height
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
            EnsureTelegraphElements();
            IsTrackingLocked = false;

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2f,
                    "🌽 Heavens Fall!",
                    new Color(1f, 0.6f, 0.2f));
            }

            // Track for 0.65s, then lock impact zone and project danger perimeter for final 0.55s
            yield return new WaitForSeconds(0.65f);

            Vector3 impactPos = playerTarget != null ? playerTarget.position : transform.position;
            const float blastRadius = 3.0f;
            IsTrackingLocked = true;
            if (combatTelegraph != null)
            {
                combatTelegraph.transform.position = impactPos + Vector3.up * 0.05f;
                combatTelegraph.SetColors(new Color(2.2f, 0.12f, 0.08f, 1.0f), new Color(1.3f, 0.15f, 0.05f, 0.50f));
                combatTelegraph.StartCharge(0.55f, blastRadius);
            }

            yield return new WaitForSeconds(0.55f);

            if (combatTelegraph != null)
            {
                combatTelegraph.Hide();
                combatTelegraph.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            }

            // If the player is inside the house (roof protection) or rolled out of blastRadius, 0 damage
            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
            {
                Vector3 toPlayer = playerTarget.position - impactPos;
                toPlayer.y = 0f;
                if (toPlayer.magnitude <= blastRadius)
                {
                    var target = playerTarget.GetComponent<IDamageable>();
                    target?.TakeDamage(30, impactPos, Vector3.down);
                }
            }

            IsInRecoveryPunishWindow = true;
            yield return new WaitForSeconds(0.5f);
            IsInRecoveryPunishWindow = false;
            IsTrackingLocked = false;
            isPerformingSkill = false;
            lastAttackTime = Time.time + 0.8f;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), true);
        }
    }
}
