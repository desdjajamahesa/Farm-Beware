using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmBeware.Core.Runtime;

public partial class PlayerControl
{
    // --- LOGIKA AKSI ---

    // Klik Kiri Mouse / Tombol F: Serangan Kombo Biasa (3-Hit Combo) / Dash Attack saat berlari / Heavy Strike jika ditahan
    // Klik Kanan Mouse / Tombol R: Jurus Spesial (Leap Strike)
    private void HandleAttackInput()
    {
        if (isInputLocked || isPlanting)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            return;
        }

        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // 1. Serangan Normal / Dash / Heavy
        bool leftPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isPointerOverUI;
        bool fPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        bool attackPressed = leftPressed || fPressed;

        bool leftHeld = Mouse.current != null && Mouse.current.leftButton.isPressed && !isPointerOverUI;
        bool fHeld = Keyboard.current != null && Keyboard.current.fKey.isPressed;
        bool attackHeld = leftHeld || fHeld;

        bool leftReleased = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        bool fReleased = Keyboard.current != null && Keyboard.current.fKey.wasReleasedThisFrame;
        bool attackReleased = leftReleased || fReleased;

        // 2. Jurus Spesial (Leap Strike): Right Click atau Tombol R
        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !isPointerOverUI;
        bool rKey = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        if (playerEquipment == null)
            playerEquipment = GetComponent<PlayerEquipment>() ?? gameObject.AddComponent<PlayerEquipment>();

        if (playerEquipment != null && playerEquipment.IsRangedWeaponEquipped)
        {
            HandleRangedSlingshotInput(attackPressed, attackHeld, attackReleased, rightClick || rKey);
            return;
        }

        // Priority Special Actions (Leap Strike)
        if ((rightClick || rKey) && !isSkillLeaping)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            if (playerEquipment != null && playerEquipment.TryPerformSkillAttack())
            {
                if (attackCoroutine != null) { StopCoroutine(attackCoroutine); attackCoroutine = null; }
                attackCoroutine = StartCoroutine(RoutineSkillAttack());
            }
            return;
        }

        // If performing heavy attack, kick, skill leap, or dash attack, block light attacks
        if (isAttacking && !isLightAttacking)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            return;
        }

        // --- COMBO ADVANCEMENT / BUFFERING / BRANCHING HEAVY FINISHER DURING LIGHT ATTACK ---
        if (isLightAttacking)
        {
            if (attackHeld)
            {
                attackHoldDuration += Time.deltaTime;
            }

            if (attackReleased && attackHoldDuration >= 0.30f)
            {
                attackHoldDuration = 0f;
                hasBufferedAttack = false;
                if (playerEquipment != null && playerEquipment.TryPerformBranchingHeavyFinisher())
                {
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                    attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                    return;
                }
            }

            if (attackPressed)
            {
                attackHoldDuration = 0f;
                if (playerEquipment != null && playerEquipment.TryPerformAttack())
                {
                    StartLightAttack();
                    hasBufferedAttack = false;
                }
                else
                {
                    hasBufferedAttack = true;
                    bufferedAttackTime = Time.time;
                }
            }
            else if (hasBufferedAttack)
            {
                if (Time.time - bufferedAttackTime > 0.45f)
                {
                    hasBufferedAttack = false;
                }
                else if (playerEquipment != null && playerEquipment.TryPerformAttack())
                {
                    StartLightAttack();
                    hasBufferedAttack = false;
                }
            }
            return;
        }

        // Consume buffered attack right after returning to Idle
        if (hasBufferedAttack)
        {
            hasBufferedAttack = false;
            if (Time.time - bufferedAttackTime <= 0.45f && playerEquipment != null && playerEquipment.TryPerformAttack())
            {
                StartLightAttack();
                return;
            }
        }

        // --- Standard Melee Attack Processing (Light Combo / Dash Attack / Charged Heavy) ---
        if (attackPressed)
        {
            // If running/sprinting at high speed, trigger instantaneous Dash Attack!
            if (isRunning && inputVector.sqrMagnitude >= 0.01f)
            {
                if (playerEquipment != null && playerEquipment.TryPerformDashAttack())
                {
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); attackCoroutine = null; }
                    attackCoroutine = StartCoroutine(RoutineDashAttack());
                    isChargingAttack = false;
                    attackHoldDuration = 0f;
                    hasBufferedAttack = false;
                    return;
                }
            }

            isChargingAttack = true;
            attackHoldDuration = 0f;
        }

        if (isChargingAttack)
        {
            if (attackHeld)
            {
                attackHoldDuration += Time.deltaTime;
                if (attackHoldDuration >= 0.35f && playerEquipment != null)
                {
                    playerEquipment.CombatStateMachine.StartHeavyCharge(Time.time);
                    playerEquipment.CombatStateMachine.UpdateCharge(Time.deltaTime);
                }
            }

            if (attackReleased)
            {
                isChargingAttack = false;
                if (attackHoldDuration >= 0.35f)
                {
                    // Released after charging -> Heavy Attack!
                    if (playerEquipment != null && playerEquipment.TryPerformHeavyAttack(playerEquipment.CombatStateMachine.ChargeRatio))
                    {
                        if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                        attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                    }
                }
                else
                {
                    // Released quickly -> Standard 3-Hit Combo Light Attack!
                    if (playerEquipment != null && playerEquipment.TryPerformAttack())
                    {
                        StartLightAttack();
                    }
                }
                attackHoldDuration = 0f;
            }
        }
    }

    private void HandleRangedSlingshotInput(bool attackPressed, bool attackHeld, bool attackReleased, bool skillPressed)
    {
        if (skillPressed && !isSkillLeaping && !isAttacking)
        {
            isChargingAttack = false;
            attackHoldDuration = 0f;
            hasBufferedAttack = false;
            if (playerEquipment != null && playerEquipment.TryPerformSkillAttack())
            {
                if (attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine);
                    attackCoroutine = null;
                }
                attackCoroutine = StartCoroutine(RoutineRangedShotRecovery(0.20f));
            }
            return;
        }

        if (isAttacking)
        {
            if (attackPressed)
            {
                hasBufferedAttack = true;
                bufferedAttackTime = Time.time;
            }
            return;
        }

        if (hasBufferedAttack)
        {
            hasBufferedAttack = false;
            if (Time.time - bufferedAttackTime <= 0.35f && playerEquipment != null && playerEquipment.TryPerformAttack())
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(RoutineRangedShotRecovery(0.14f));
                return;
            }
        }

        if (attackPressed)
        {
            isChargingAttack = true;
            attackHoldDuration = 0f;
            playerEquipment.ResolveCursorAimDirection(out _);
        }

        if (isChargingAttack)
        {
            if (attackHeld)
            {
                attackHoldDuration += Time.deltaTime;
                playerEquipment.ResolveCursorAimDirection(out _);
                if (attackHoldDuration >= 0.35f)
                {
                    playerEquipment.CombatStateMachine.StartHeavyCharge(Time.time);
                    playerEquipment.CombatStateMachine.UpdateCharge(Time.deltaTime);
                }
            }

            if (attackReleased)
            {
                isChargingAttack = false;
                if (attackHoldDuration >= 0.35f)
                {
                    if (playerEquipment.TryPerformHeavyAttack(playerEquipment.CombatStateMachine.ChargeRatio))
                    {
                        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                        attackCoroutine = StartCoroutine(RoutineRangedShotRecovery(0.18f));
                    }
                }
                else
                {
                    if (playerEquipment.TryPerformAttack())
                    {
                        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                        attackCoroutine = StartCoroutine(RoutineRangedShotRecovery(0.14f));
                    }
                }
                attackHoldDuration = 0f;
            }
        }
    }

    private IEnumerator RoutineRangedShotRecovery(float lockDuration)
    {
        isAttacking = true;
        isLightAttacking = false;
        _currentCombatSubState = PlayerActionState.RangedRecovery;
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }

        float elapsed = 0f;
        while (elapsed < lockDuration)
        {
            elapsed += Time.deltaTime;
            if (playerEquipment != null)
            {
                playerEquipment.ResolveCursorAimDirection(out _);
            }
            yield return null;
        }

        isAttacking = false;
        isLightAttacking = false;
        _currentCombatSubState = PlayerActionState.Locomotion;
        attackCoroutine = null;
    }

    private IEnumerator RoutineDashAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        _currentCombatSubState = PlayerActionState.DashAttack;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        if (rb != null)
        {
            Vector3 forwardDir = transform.forward;
            rb.linearVelocity = forwardDir * 7.5f + Vector3.up * 0.1f;
        }

        yield return null;

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1.6f;
        float lockDuration = 0.36f / atkSpeed;
        yield return new WaitForSeconds(lockDuration);

        isAttacking = false;
        _currentCombatSubState = PlayerActionState.Locomotion;
        attackCoroutine = null;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private IEnumerator RoutineHeavyAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        _currentCombatSubState = PlayerActionState.HeavyAttack;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f;

        // Windup anticipation delay matching the initial combo strike
        float windupWait = 0.38f / atkSpeed;
        float elapsedWindup = 0f;
        while (elapsedWindup < windupWait)
        {
            elapsedWindup += Time.deltaTime;
            yield return null;
        }

        // Forward impulse burst synchronized with active combo blade release
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * 4.2f + Vector3.up * 0.1f;
        }

        float maxLock = 1.5f / atkSpeed;
        float timer = elapsedWindup;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
        _currentCombatSubState = PlayerActionState.Locomotion;
        attackCoroutine = null;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private void StartLightAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        attackCoroutine = StartCoroutine(RoutineAttack());
    }

    private IEnumerator RoutineAttack()
    {
        isAttacking = true;
        isLightAttacking = true;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        // Tunggu satu frame agar transisi animator ke state attack dimulai
        yield return null;

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f;
        float maxLock = attackLockDuration / atkSpeed;
        float minLock = 0.20f / atkSpeed;

        float timer = 0f;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;

            if (hasBufferedAttack && playerEquipment != null)
            {
                if (Time.time - bufferedAttackTime > 0.45f)
                {
                    hasBufferedAttack = false;
                }
                else if (playerEquipment.TryPerformAttack())
                {
                    hasBufferedAttack = false;
                    StartLightAttack();
                    yield break;
                }
            }

            if (timer > minLock && animator != null)
            {
                var curr = animator.GetCurrentAnimatorStateInfo(0);
                var next = animator.GetNextAnimatorStateInfo(0);
                if (!IsAttackState(curr) && !IsAttackState(next))
                {
                    break;
                }
            }

            yield return null;
        }

        isAttacking = false;
        isLightAttacking = false;
        attackCoroutine = null;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }


    private IEnumerator RoutineSkillAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        isSkillLeaping = true;
        if (animator != null)
            animator.SetBool("IsAttacking", true);

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1.6f;

        Vector3 forwardDir = transform.forward;

        // Fase 1: Windup / ancang-ancang melompat (~0.25s / atkSpeed)
        float windupDuration = 0.25f / atkSpeed;
        float elapsed = 0f;
        while (elapsed < windupDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Berikan dorongan awal melompat ke udara
        if (rb != null)
        {
            rb.linearVelocity = forwardDir * leapForwardImpulse + Vector3.up * leapUpwardImpulse;
        }

        // Fase 2: Meluncur maju di udara hingga pedang menghantam tanah (~1.14s total / atkSpeed)
        float impactTime = 1.14f / atkSpeed;
        while (elapsed < impactTime)
        {
            elapsed += Time.deltaTime;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(forwardDir.x * leapForwardImpulse, rb.linearVelocity.y, forwardDir.z * leapForwardImpulse);
            }
            yield return new WaitForFixedUpdate();
        }

        // Fase 3: Mendarat & hantaman tanah (hentikan laju horizontal agar mendarat kokoh di titik hantaman)
        isSkillLeaping = false;
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // Fase 4: Recovery pasca hantaman dan transisi kembali ke Idle/Moving (~1.35s / atkSpeed)
        float totalLock = 1.35f / atkSpeed;
        while (elapsed < totalLock)
        {
            elapsed += Time.deltaTime;
            if (animator != null)
            {
                var curr = animator.GetCurrentAnimatorStateInfo(0);
                var next = animator.GetNextAnimatorStateInfo(0);
                if (!IsAttackState(curr) && !IsAttackState(next))
                {
                    break;
                }
            }
            yield return null;
        }

        isAttacking = false;
        isLightAttacking = false;
        isSkillLeaping = false;
        _currentCombatSubState = PlayerActionState.Locomotion;
        attackCoroutine = null;
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    /// <summary>
    /// Deterministically aborts any active combat swing, skill leap, dash attack, or charge state.
    /// </summary>
    public void CancelActiveCombatAction()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;
        isLightAttacking = false;
        isSkillLeaping = false;
        isChargingAttack = false;
        attackHoldDuration = 0f;
        hasBufferedAttack = false;
        _currentCombatSubState = PlayerActionState.Locomotion;
        playerEquipment?.CancelActiveAttackSwing();
        if (animator != null && !animator.GetBool("Driving"))
        {
            animator.SetBool("IsAttacking", false);
        }
    }

    private bool IsAttackState(AnimatorStateInfo info)
    {
        return info.IsName("attack") ||
               info.IsName("Attack_Combo1") ||
               info.IsName("Attack_Combo2") ||
               info.IsName("Attack_Combo3") ||
               info.IsName("Attack_Heavy") ||
               info.IsName("Attack_Skill");
    }
}
