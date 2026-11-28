using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmBeware.Core.Runtime;
using FeaturesCombat.Core.PureLogic;

public partial class PlayerControl
{
    private readonly CombatInputBuffer _combatInputBuffer = new CombatInputBuffer();
    public PlayerCancelAction CurrentBufferedAction => _combatInputBuffer.PeekValid(Time.time);

    private void SnapFaceMovementOrAimOnAttackStart()
    {
        if (inputVector.sqrMagnitude < 0.01f) return;
        Vector3 moveDir = GetCameraRelativeDirection(inputVector);
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            transform.rotation = rot;
            if (rb != null) rb.rotation = rot;
        }
    }

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
            _combatInputBuffer.Clear();
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

        // Handle Dodge-End buffered attack cancel (>= 78% dodge completion)
        if (isDodging)
        {
            if (heavyPressed) _combatInputBuffer.Buffer(PlayerCancelAction.HeavyAttack, Time.time);
            else if (lightPressed) _combatInputBuffer.Buffer(PlayerCancelAction.LightAttack, Time.time);

            if (DodgeProgress >= CombatFrameDataSheet.DODGE_END_CANCEL_PROGRESS)
            {
                if (_combatInputBuffer.TryConsume(PlayerCancelAction.HeavyAttack, Time.time) &&
                    playerEquipment != null && playerEquipment.TryPerformHeavyChainAttack())
                {
                    CancelDodgeForCombatTransition();
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                    attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                    hasBufferedHeavyAttack = false;
                    hasBufferedAttack = false;
                    return;
                }

                if (_combatInputBuffer.TryConsume(PlayerCancelAction.LightAttack, Time.time) &&
                    playerEquipment != null && playerEquipment.TryPerformAttack())
                {
                    CancelDodgeForCombatTransition();
                    StartLightAttack();
                    hasBufferedAttack = false;
                    hasBufferedHeavyAttack = false;
                    return;
                }
            }
            return;
        }

        // Allow chaining Heavy_Attack1..4 via Right-Click OR Heavy 1-3 -> Light 1 via Left-Click/F
        if (isAttacking && !isLightAttacking)
        {
            isChargingAttack = false;

            if (_currentCombatSubState == PlayerActionState.HeavyAttack && playerEquipment != null)
            {
                if (heavyPressed)
                {
                    _combatInputBuffer.Buffer(PlayerCancelAction.HeavyAttack, Time.time);
                    hasBufferedHeavyAttack = true;
                    bufferedAttackTime = Time.time;
                }
                else if (lightPressed && playerEquipment.CurrentHeavyComboIndex < 3)
                {
                    _combatInputBuffer.Buffer(PlayerCancelAction.LightAttack, Time.time);
                    hasBufferedAttack = true;
                    bufferedAttackTime = Time.time;
                }

                if (playerEquipment.CurrentHeavyComboIndex < 3)
                {
                    if (_combatInputBuffer.HasBuffered(PlayerCancelAction.HeavyAttack, Time.time))
                    {
                        if (playerEquipment.TryPerformHeavyChainAttack())
                        {
                            _combatInputBuffer.TryConsume(PlayerCancelAction.HeavyAttack, Time.time);
                            if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                            attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                            hasBufferedHeavyAttack = false;
                            hasBufferedAttack = false;
                            return;
                        }
                    }
                    else if (_combatInputBuffer.HasBuffered(PlayerCancelAction.LightAttack, Time.time))
                    {
                        if (playerEquipment.TryPerformAttack())
                        {
                            _combatInputBuffer.TryConsume(PlayerCancelAction.LightAttack, Time.time);
                            StartLightAttack();
                            hasBufferedAttack = false;
                            hasBufferedHeavyAttack = false;
                            return;
                        }
                    }
                }
                else
                {
                    hasBufferedHeavyAttack = false;
                    hasBufferedAttack = false;
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
                _combatInputBuffer.Buffer(PlayerCancelAction.HeavyAttack, Time.time);
                hasBufferedAttack = false;
                hasBufferedHeavyAttack = true;
                bufferedAttackTime = Time.time;
            }
            else if (lightPressed)
            {
                if (playerEquipment != null && playerEquipment.CurrentComboIndex >= 2)
                {
                    hasBufferedAttack = false;
                }
                else
                {
                    _combatInputBuffer.Buffer(PlayerCancelAction.LightAttack, Time.time);
                    hasBufferedAttack = true;
                    bufferedAttackTime = Time.time;
                }
            }

            if (_combatInputBuffer.HasBuffered(PlayerCancelAction.HeavyAttack, Time.time))
            {
                if (playerEquipment != null && playerEquipment.TryPerformBranchingHeavyFinisher())
                {
                    _combatInputBuffer.TryConsume(PlayerCancelAction.HeavyAttack, Time.time);
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                    attackCoroutine = StartCoroutine(RoutineHeavyAttack());
                    hasBufferedHeavyAttack = false;
                    hasBufferedAttack = false;
                }
                return;
            }

            if (playerEquipment != null && playerEquipment.CurrentComboIndex >= 2)
            {
                hasBufferedAttack = false;
                return;
            }

            if (_combatInputBuffer.HasBuffered(PlayerCancelAction.LightAttack, Time.time))
            {
                if (playerEquipment != null && playerEquipment.TryPerformAttack())
                {
                    _combatInputBuffer.TryConsume(PlayerCancelAction.LightAttack, Time.time);
                    StartLightAttack();
                    hasBufferedAttack = false;
                }
            }
            return;
        }

        // Clear legacy flags once returned to Idle
        hasBufferedAttack = false;
        hasBufferedHeavyAttack = false;

        bool wantsHeavy = heavyPressed || _combatInputBuffer.TryConsume(PlayerCancelAction.HeavyAttack, Time.time);
        if (wantsHeavy)
        {
            isChargingAttack = false;
            _combatInputBuffer.Clear();
            if (playerEquipment != null && playerEquipment.TryPerformHeavyChainAttack())
            {
                if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
                attackCoroutine = StartCoroutine(RoutineHeavyAttack());
            }
            return;
        }

        bool wantsLight = lightPressed || _combatInputBuffer.TryConsume(PlayerCancelAction.LightAttack, Time.time);
        if (wantsLight)
        {
            _combatInputBuffer.Clear();
            // If running/sprinting at high speed, trigger instantaneous Dash Attack!
            if (isRunning && inputVector.sqrMagnitude >= 0.01f)
            {
                if (playerEquipment != null && playerEquipment.TryPerformDashAttack())
                {
                    if (attackCoroutine != null) { StopCoroutine(attackCoroutine); attackCoroutine = null; }
                    attackCoroutine = StartCoroutine(RoutineDashAttack());
                    isChargingAttack = false;
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

        CombatActionID curAction = playerEquipment != null ? playerEquipment.CurrentCombatActionID : CombatActionID.Light1;
        PlayerMoveFrameData moveData = CombatFrameDataSheet.GetMove(curAction);
        float moveCancelNorm = moveData.MoveCancelNormTime > 0f ? moveData.MoveCancelNormTime : 0.72f;
        bool inActiveWindow = playerEquipment != null && playerEquipment.IsInActiveSwingWindow;

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
                    if (expectedNow && !inTrans && !inActiveWindow && curr.normalizedTime >= moveCancelNorm)
                    {
                        inRecoveryHold = true;
                    }
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
                if (!inTrans && !inActiveWindow && curr.normalizedTime >= moveCancelNorm)
                {
                    inRecoveryHold = true;
                }
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
        SnapFaceMovementOrAimOnAttackStart();
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

        // Windup anticipation delay pulled directly from CombatFrameDataSheet (Single Source of Truth)
        CombatActionID hAction = playerEquipment != null ? playerEquipment.CurrentCombatActionID : CombatActionID.ChargedThrust;
        PlayerMoveFrameData moveData = CombatFrameDataSheet.GetMove(hAction);
        float windupWait = moveData.StartupSec / atkSpeed;
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
            // Calibrated directional micro-lunge resolved smoothly in FixedUpdate
            ApplyAttackMicroLunge(transform.forward, moveData.ForwardLungeSpeed, moveData.LungeDurationSec / atkSpeed);

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
        _combatInputBuffer.Clear();
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
        SnapFaceMovementOrAimOnAttackStart();
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

        CombatActionID lAction = playerEquipment != null ? playerEquipment.CurrentCombatActionID : CombatActionID.Light1;
        PlayerMoveFrameData moveData = CombatFrameDataSheet.GetMove(lAction);
        float atkSpeed = (playerEquipment != null) ? Mathf.Max(0.5f, playerEquipment.AttackAnimationSpeed) : 1f;
        float lungeDelay = moveData.StartupSec / atkSpeed;
        bool triggeredLunge = false;

        // Wait one frame for the Animator transition into the attack state to begin
        yield return null;

        float maxLock = Mathf.Max(attackLockDuration, 2.10f) / atkSpeed;
        float minLock = 0.20f / atkSpeed;

        float timer = 0f;
        while (timer < maxLock)
        {
            timer += Time.deltaTime;
            if (!triggeredLunge && timer >= lungeDelay)
            {
                triggeredLunge = true;
                ApplyAttackMicroLunge(transform.forward, moveData.ForwardLungeSpeed, moveData.LungeDurationSec / atkSpeed);
            }

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
        _combatInputBuffer.Clear();
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
        _lungeTimer = 0f;
        SetAnimatorRecoveryBool(false);
        hasBufferedAttack = false;
        hasBufferedHeavyAttack = false;
        _combatInputBuffer.Clear();
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
