using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmBeware.Core.Runtime;

public partial class PlayerControl
{
    // --- ACTION LOGIC ---

    // Left Mouse / F: 3-hit Light Combo (1-3-4) or Dash Attack while sprinting
    // Right Mouse: Heavy Attack (4-hit chain 1-2-3-4 & branching Heavy Finisher L->H / L->L->H / L->L->L->H)
    private void HandleAttackInput()
    {
        if (isInputLocked || isPlanting)
        {
            isChargingAttack = false;
            hasBufferedAttack = false;
            hasBufferedHeavyAttack = false;
            return;
        }

        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // 1. Light Attack (Left Click / F) & Heavy Attack (Right Click)
        bool leftPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isPointerOverUI;
        bool fPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        bool lightPressed = leftPressed || fPressed;

        bool heavyPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !isPointerOverUI;

        if (playerEquipment == null)
            playerEquipment = GetComponent<PlayerEquipment>() ?? gameObject.AddComponent<PlayerEquipment>();

        if (playerEquipment != null && playerEquipment.IsRangedWeaponEquipped)
        {
            HandleRangedSlingshotInput(lightPressed, heavyPressed);
            return;
        }

        // Allow chaining Heavy_Attack1 -> Heavy_Attack2 -> Heavy_Attack3 -> Heavy_Attack4 via Right-Click
        if (isAttacking && !isLightAttacking)
        {
            isChargingAttack = false;

            if (_currentCombatSubState == PlayerActionState.HeavyAttack && playerEquipment != null)
            {
                if (playerEquipment.CurrentHeavyComboIndex >= 3)
                {
                    hasBufferedHeavyAttack = false;
                    hasBufferedAttack = false;
                    return;
                }

                if (heavyPressed)
                {
                    if (playerEquipment.TryPerformHeavyChainAttack())
                    {
                        if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                        attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                        hasBufferedHeavyAttack = false;
                        hasBufferedAttack = false;
                    }
                    else
                    {
                        hasBufferedHeavyAttack = true;
                        bufferedAttackTime = Time.time;
                    }
                }
                else if (hasBufferedHeavyAttack)
                {
                    if (Time.time - bufferedAttackTime > 0.60f)
                    {
                        hasBufferedHeavyAttack = false;
                    }
                    else if (playerEquipment.TryPerformHeavyChainAttack())
                    {
                        if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                        attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                        hasBufferedHeavyAttack = false;
                        hasBufferedAttack = false;
                    }
                }
                return;
            }

            hasBufferedAttack = false;
            hasBufferedHeavyAttack = false;
            return;
        }

        // --- COMBO ADVANCEMENT / BUFFERING / BRANCHING HEAVY FINISHER DURING LIGHT ATTACK ---
        if (isLightAttacking)
        {
            if (heavyPressed)
            {
                hasBufferedAttack = false;
                if (playerEquipment != null && playerEquipment.TryPerformBranchingHeavyFinisher())
                {
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                    attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                    hasBufferedHeavyAttack = false;
                    return;
                }
                hasBufferedHeavyAttack = true;
                bufferedAttackTime = Time.time;
                return;
            }

            if (hasBufferedHeavyAttack)
            {
                if (Time.time - bufferedAttackTime > 0.60f)
                {
                    hasBufferedHeavyAttack = false;
                }
                else if (playerEquipment != null && playerEquipment.TryPerformBranchingHeavyFinisher())
                {
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                    attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                    hasBufferedHeavyAttack = false;
                    hasBufferedAttack = false;
                    return;
                }
            }

            // Do not wrap or buffer an extra hit 1 while the 3rd light finisher (attack4) is playing
            if (playerEquipment != null && playerEquipment.CurrentComboIndex >= 2)
            {
                hasBufferedAttack = false;
                return;
            }

            if (lightPressed)
            {
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
                if (Time.time - bufferedAttackTime > 0.60f)
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

        // Clear any leftover buffers once returned to Idle so combo never fires an extra hit unprompted
        hasBufferedAttack = false;
        hasBufferedHeavyAttack = false;

        // --- Standard Melee Attack Processing (Right-Click Heavy / Sprint Dash Attack / Left-Click Light Combo) ---
        if (heavyPressed)
        {
            isChargingAttack = false;
            hasBufferedAttack = false;
            hasBufferedHeavyAttack = false;
            if (playerEquipment != null && playerEquipment.TryPerformHeavyChainAttack())
            {
                if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                attackCoroutine = StartCoroutine(RoutineHeavyAttack());
            }
            return;
        }

        if (lightPressed)
        {
            // If running/sprinting at high speed, trigger instantaneous Dash Attack!
            if (isRunning && inputVector.sqrMagnitude >= 0.01f)
            {
                if (playerEquipment != null && playerEquipment.TryPerformDashAttack())
                {
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); attackCoroutine = null; }
                    attackCoroutine = StartCoroutine(RoutineDashAttack());
                    isChargingAttack = false;
                    hasBufferedAttack = false;
                    hasBufferedHeavyAttack = false;
                    return;
                }
            }

            if (playerEquipment != null && playerEquipment.TryPerformAttack())
            {
                StartLightAttack();
            }
        }
    }

    private void HandleRangedSlingshotInput(bool lightPressed, bool heavyPressed)
    {
        if (isAttacking)
        {
            if (heavyPressed)
            {
                hasBufferedHeavyAttack = true;
                bufferedAttackTime = Time.time;
            }
            else if (lightPressed)
            {
                hasBufferedAttack = true;
                bufferedAttackTime = Time.time;
            }
            return;
        }

        if (hasBufferedHeavyAttack)
        {
            hasBufferedHeavyAttack = false;
            if (Time.time - bufferedAttackTime <= 0.35f && playerEquipment != null && playerEquipment.TryPerformHeavyAttack(1.0f))
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(RoutineRangedShotRecovery(0.18f));
                return;
            }
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

        if (heavyPressed && playerEquipment != null)
        {
            playerEquipment.ResolveCursorAimDirection(out _);
            if (playerEquipment.TryPerformHeavyAttack(1.0f))
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(RoutineRangedShotRecovery(0.18f));
            }
            return;
        }

        if (lightPressed && playerEquipment != null)
        {
            playerEquipment.ResolveCursorAimDirection(out _);
            if (playerEquipment.TryPerformAttack())
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(RoutineRangedShotRecovery(0.14f));
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

    private void SetAnimatorRecoveryBool(bool value)
    {
        isInAttackRecovery = value;
        if (animator != null && HasAnimatorParameter(animator, "InAttackRecovery", AnimatorControllerParameterType.Bool))
        {
            animator.SetBool("InAttackRecovery", value);
        }
    }

    private void SyncAttackRecoveryState(ref bool enteredExpectedState, out bool inRecoveryHold)
    {
        inRecoveryHold = false;
        if (animator == null) return;

        var curr = animator.GetCurrentAnimatorStateInfo(0);
        bool inTrans = animator.IsInTransition(0);
        var next = inTrans ? animator.GetNextAnimatorStateInfo(0) : default;

        bool expectedNow = playerEquipment != null && playerEquipment.IsAnimatorInExpectedCombatState(curr);
        bool expectedNext = inTrans && playerEquipment != null && playerEquipment.IsAnimatorInExpectedCombatState(next);

        if (!enteredExpectedState)
        {
            if (expectedNow || expectedNext)
            {
                enteredExpectedState = true;
                if (expectedNow && !inTrans && curr.normalizedTime >= 0.94f)
                {
                    SetAnimatorRecoveryBool(true);
                    inRecoveryHold = true;
                }
                else
                {
                    SetAnimatorRecoveryBool(false);
                }
            }
            else if (IsAttackState(curr) && curr.normalizedTime >= 0.92f)
            {
                SetAnimatorRecoveryBool(true);
            }
            else if (!IsAttackState(curr))
            {
                SetAnimatorRecoveryBool(false);
            }
            return;
        }

        if (expectedNow)
        {
            if (curr.normalizedTime >= 0.94f || (inTrans && !IsAttackState(next)))
            {
                SetAnimatorRecoveryBool(true);
                inRecoveryHold = true;
            }
            else
            {
                SetAnimatorRecoveryBool(false);
            }
        }
        else if (expectedNext)
        {
            SetAnimatorRecoveryBool(false);
        }
        else if (!IsAttackState(curr) && !IsAttackState(next))
        {
            SetAnimatorRecoveryBool(false);
        }
    }

    private bool TryCancelRecoveryIntoMovement(bool inRecoveryHold)
    {
        if (!inRecoveryHold || inputVector.magnitude < 0.1f || hasBufferedAttack || hasBufferedHeavyAttack)
        {
            return false;
        }

        playerEquipment?.EndActiveSwingTrailAndHitbox();
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("HeavyAttack");
            SetAnimatorRecoveryBool(false);
            animator.SetBool("IsAttacking", false);
            animator.SetBool("Idle", false);

            bool wantsSprint = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            bool hasStamina = playerStats == null || (!playerStats.IsExhausted && playerStats.currentStamina > 0.5f);
            animator.SetBool("Sprinting", wantsSprint && hasStamina && isGroundedRecently);
        }
        else
        {
            isInAttackRecovery = false;
        }
        return true;
    }

    private IEnumerator RoutineDashAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        _currentCombatSubState = PlayerActionState.DashAttack;
        bool enteredExpectedState = false;
        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            if (!IsAttackState(animator.GetCurrentAnimatorStateInfo(0)))
                SetAnimatorRecoveryBool(false);
        }
        else
        {
            isInAttackRecovery = false;
        }

        if (rb != null)
        {
            Vector3 forwardDir = transform.forward;
            rb.linearVelocity = forwardDir * 7.5f + Vector3.up * 0.1f;
        }

        yield return null;

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1.6f;
        float minLock = 0.25f / atkSpeed;
        float maxLock = 1.40f / atkSpeed;
        float timer = 0f;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;
            SyncAttackRecoveryState(ref enteredExpectedState, out bool inRecoveryHold);
            if (TryCancelRecoveryIntoMovement(inRecoveryHold))
            {
                break;
            }

            if ((enteredExpectedState || timer > minLock) && animator != null)
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
        SetAnimatorRecoveryBool(false);
        _currentCombatSubState = PlayerActionState.Locomotion;
        attackCoroutine = null;
        playerEquipment?.CombatStateMachine?.ResetToIdle();
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("HeavyAttack");
            animator.SetBool("IsAttacking", false);
        }
    }

    private IEnumerator RoutineHeavyAttack()
    {
        isAttacking = true;
        isLightAttacking = false;
        hasBufferedAttack = false;
        _currentCombatSubState = PlayerActionState.HeavyAttack;
        bool enteredExpectedState = false;
        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            if (!IsAttackState(animator.GetCurrentAnimatorStateInfo(0)))
                SetAnimatorRecoveryBool(false);
        }
        else
        {
            isInAttackRecovery = false;
        }

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f;

        // Windup anticipation delay matching the active heavy step's kinematic apex
        int heavyIdx = playerEquipment != null ? playerEquipment.CurrentHeavyComboIndex : 0;
        float baseWindup = heavyIdx == 1 ? 0.14f : (heavyIdx >= 2 ? 0.40f : 0.28f);
        float windupWait = baseWindup / atkSpeed;
        float elapsedWindup = 0f;
        bool canceledByMovement = false;
        while (elapsedWindup < windupWait)
        {
            elapsedWindup += Time.deltaTime;
            SyncAttackRecoveryState(ref enteredExpectedState, out bool inRecoveryHold);
            if (TryCancelRecoveryIntoMovement(inRecoveryHold))
            {
                canceledByMovement = true;
                break;
            }
            yield return null;
        }

        if (!canceledByMovement)
        {
            // Forward impulse burst synchronized with active combo blade release
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * 4.2f + Vector3.up * 0.1f;
            }

            float maxLock = 2.00f / atkSpeed;
            float minLock = 0.25f / atkSpeed;
            float timer = elapsedWindup;
            while (timer < maxLock)
            {
                timer += Time.deltaTime;
                SyncAttackRecoveryState(ref enteredExpectedState, out bool inRecoveryHold);
                if (TryCancelRecoveryIntoMovement(inRecoveryHold))
                {
                    break;
                }

                if ((enteredExpectedState || timer > minLock) && animator != null)
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
        }

        isAttacking = false;
        SetAnimatorRecoveryBool(false);
        hasBufferedAttack = false;
        hasBufferedHeavyAttack = false;
        _currentCombatSubState = PlayerActionState.Locomotion;
        attackCoroutine = null;
        if (playerEquipment != null && playerEquipment.CurrentHeavyComboIndex >= 3)
        {
            playerEquipment.CombatStateMachine.ResetToIdle();
        }
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("HeavyAttack");
            animator.SetBool("IsAttacking", false);
        }
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
        bool enteredExpectedState = false;
        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            if (!IsAttackState(animator.GetCurrentAnimatorStateInfo(0)))
                SetAnimatorRecoveryBool(false);
        }
        else
        {
            isInAttackRecovery = false;
        }

        // Wait one frame for the Animator transition into the attack state to begin
        yield return null;

        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f;
        float maxLock = Mathf.Max(attackLockDuration, 2.10f) / atkSpeed;
        float minLock = 0.20f / atkSpeed;

        float timer = 0f;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;
            SyncAttackRecoveryState(ref enteredExpectedState, out bool inRecoveryHold);
            if (TryCancelRecoveryIntoMovement(inRecoveryHold))
            {
                break;
            }

            if ((enteredExpectedState || timer > minLock) && animator != null)
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
        SetAnimatorRecoveryBool(false);
        hasBufferedAttack = false;
        hasBufferedHeavyAttack = false;
        attackCoroutine = null;
        if (playerEquipment != null && playerEquipment.CurrentComboIndex >= 2)
        {
            playerEquipment.CombatStateMachine.ResetToIdle();
        }
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("HeavyAttack");
            animator.SetBool("IsAttacking", false);
        }
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

        // Phase 1: Leap windup (~0.25s / atkSpeed)
        float windupDuration = 0.25f / atkSpeed;
        float elapsed = 0f;
        while (elapsed < windupDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Apply initial leap impulse into the air
        if (rb != null)
        {
            rb.linearVelocity = forwardDir * leapForwardImpulse + Vector3.up * leapUpwardImpulse;
        }

        // Phase 2: Airborne forward glide until ground slam impact (~1.14s total / atkSpeed)
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

        // Phase 3: Landing & ground slam (halt horizontal velocity to plant firmly at impact point)
        isSkillLeaping = false;
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // Phase 4: Post-slam recovery and transition back to Idle/Moving (~1.35s / atkSpeed)
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
        SetAnimatorRecoveryBool(false);
        hasBufferedAttack = false;
        hasBufferedHeavyAttack = false;
        _currentCombatSubState = PlayerActionState.Locomotion;
        playerEquipment?.CancelActiveAttackSwing();
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("HeavyAttack");
            if (!animator.GetBool("Driving"))
            {
                animator.SetBool("IsAttacking", false);
            }
        }
    }

    private bool IsAttackState(AnimatorStateInfo info)
    {
        return info.IsName("attack") ||
               info.IsName("Attack_Combo1") ||
               info.IsName("Attack_Combo2") ||
               info.IsName("Attack_Combo3") ||
               info.IsName("Attack_Combo4") ||
               info.IsName("Attack_Heavy") ||
               info.IsName("Heavy_Attack1") ||
               info.IsName("Heavy_Attack2") ||
               info.IsName("Heavy_Attack3") ||
               info.IsName("Heavy_Attack4") ||
               info.IsName("Attack_Skill");
    }
}
