using System;

namespace FeaturesCombat.Melee
{
    /// <summary>
    /// Pure C# POCO Finite State Machine governing player melee combos, heavy charges, and dash attacks.
    /// Operates without MonoBehaviour dependencies or GC allocations.
    /// </summary>
    public class MeleeCombatStateMachine
    {
        private MeleeCombatState _currentState = MeleeCombatState.Idle;
        private int _comboIndex = 0;
        private float _lastAttackTime = -999f;
        private float _chargeStartTime = 0f;
        private float _currentChargeDuration = 0f;

        // Configurable Timing & Multipliers
        public float ComboResetWindow { get; set; } = 0.9f;
        public float MinChargeForHeavy { get; set; } = 0.35f;
        public float MaxChargeDuration { get; set; } = 1.0f;

        public float Light1DamageMultiplier { get; set; } = 1.0f;
        public float Light2DamageMultiplier { get; set; } = 1.2f;
        public float Light3DamageMultiplier { get; set; } = 1.6f;
        public float DashAttackDamageMultiplier { get; set; } = 1.4f;
        public float MinHeavyDamageMultiplier { get; set; } = 1.8f;
        public float MaxHeavyDamageMultiplier { get; set; } = 3.0f;

        public float Light1KnockbackMultiplier { get; set; } = 1.0f;
        public float Light2KnockbackMultiplier { get; set; } = 1.15f;
        public float Light3KnockbackMultiplier { get; set; } = 1.6f;
        public float DashAttackKnockbackMultiplier { get; set; } = 1.5f;
        public float MinHeavyKnockbackMultiplier { get; set; } = 1.8f;
        public float MaxHeavyKnockbackMultiplier { get; set; } = 2.8f;

        // State Properties
        public MeleeCombatState CurrentState => _currentState;
        public int ComboIndex => _comboIndex;
        public float LastAttackTime => _lastAttackTime;
        public bool IsCharging => _currentState == MeleeCombatState.HeavyCharging;
        public float CurrentChargeDuration => _currentChargeDuration;

        public float ChargeRatio
        {
            get
            {
                if (!IsCharging && _currentState != MeleeCombatState.HeavyRelease)
                    return 0f;

                float range = MaxChargeDuration - MinChargeForHeavy;
                if (range <= 0.001f) return 1f;

                float net = _currentChargeDuration - MinChargeForHeavy;
                if (net <= 0f) return 0f;

                float ratio = net / range;
                return ratio > 1f ? 1f : ratio;
            }
        }

        public event Action<MeleeCombatState> OnStateChanged;

        public MeleeCombatStateMachine()
        {
            ResetToIdle();
        }

        /// <summary>
        /// Attempts to advance or trigger a light combo attack.
        /// Cycles: Idle -> Light1 -> Light2 -> Light3_Finisher -> Light1.
        /// </summary>
        public bool TryTriggerLight(float currentTime, out MeleeCombatState newState, out int comboStep)
        {
            if (_currentState == MeleeCombatState.HeavyCharging)
            {
                newState = _currentState;
                comboStep = _comboIndex;
                return false;
            }

            float timeSinceLast = currentTime - _lastAttackTime;
            if (timeSinceLast > ComboResetWindow || _comboIndex >= 2 || _currentState == MeleeCombatState.Idle)
            {
                _comboIndex = 0;
                _currentState = MeleeCombatState.Light1;
            }
            else if (_comboIndex == 0)
            {
                _comboIndex = 1;
                _currentState = MeleeCombatState.Light2;
            }
            else
            {
                _comboIndex = 2;
                _currentState = MeleeCombatState.Light3_Finisher;
            }

            _lastAttackTime = currentTime;
            newState = _currentState;
            comboStep = _comboIndex;

            OnStateChanged?.Invoke(_currentState);
            return true;
        }

        /// <summary>
        /// Begins heavy attack charging.
        /// </summary>
        public bool StartHeavyCharge(float currentTime)
        {
            if (_currentState == MeleeCombatState.HeavyCharging)
                return false;

            _currentState = MeleeCombatState.HeavyCharging;
            _chargeStartTime = currentTime;
            _currentChargeDuration = 0f;

            OnStateChanged?.Invoke(_currentState);
            return true;
        }

        /// <summary>
        /// Updates charge duration during heavy attack hold.
        /// </summary>
        public void UpdateCharge(float deltaTime)
        {
            if (_currentState == MeleeCombatState.HeavyCharging)
            {
                _currentChargeDuration += deltaTime;
                if (_currentChargeDuration > MaxChargeDuration)
                {
                    _currentChargeDuration = MaxChargeDuration;
                }
            }
        }

        /// <summary>
        /// Releases a heavy attack if the minimum charge threshold has been reached.
        /// </summary>
        public bool ReleaseHeavy(float currentTime, out float finalChargeRatio)
        {
            finalChargeRatio = 0f;
            if (_currentState != MeleeCombatState.HeavyCharging)
                return false;

            if (_currentChargeDuration < MinChargeForHeavy)
            {
                // Charge didn't meet minimum threshold
                ResetToIdle();
                return false;
            }

            finalChargeRatio = ChargeRatio;
            _currentState = MeleeCombatState.HeavyRelease;
            _lastAttackTime = currentTime;
            _comboIndex = 0;

            OnStateChanged?.Invoke(_currentState);
            return true;
        }

        /// <summary>
        /// Triggers a dynamic dash attack (running lunge sweep).
        /// </summary>
        public bool TryTriggerDashAttack(float currentTime)
        {
            if (_currentState == MeleeCombatState.HeavyCharging)
                return false;

            _currentState = MeleeCombatState.DashAttack;
            _comboIndex = 0;
            _lastAttackTime = currentTime;

            OnStateChanged?.Invoke(_currentState);
            return true;
        }

        /// <summary>
        /// Resets state machine back to Idle state.
        /// </summary>
        public void ResetToIdle()
        {
            _currentState = MeleeCombatState.Idle;
            _currentChargeDuration = 0f;
            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Checks combo window expiration when not actively attacking.
        /// </summary>
        public void Update(float currentTime)
        {
            if (_currentState != MeleeCombatState.HeavyCharging && _currentState != MeleeCombatState.Idle)
            {
                if (currentTime - _lastAttackTime > ComboResetWindow)
                {
                    _comboIndex = 0;
                    _currentState = MeleeCombatState.Idle;
                }
            }
        }

        /// <summary>
        /// Calculates the effective damage multiplier for the active combat state.
        /// </summary>
        public float GetCurrentDamageMultiplier()
        {
            switch (_currentState)
            {
                case MeleeCombatState.Light1:
                    return Light1DamageMultiplier;
                case MeleeCombatState.Light2:
                    return Light2DamageMultiplier;
                case MeleeCombatState.Light3_Finisher:
                    return Light3DamageMultiplier;
                case MeleeCombatState.DashAttack:
                    return DashAttackDamageMultiplier;
                case MeleeCombatState.HeavyRelease:
                    return MinHeavyDamageMultiplier + (MaxHeavyDamageMultiplier - MinHeavyDamageMultiplier) * ChargeRatio;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Calculates the effective knockback multiplier for the active combat state.
        /// </summary>
        public float GetCurrentKnockbackMultiplier()
        {
            switch (_currentState)
            {
                case MeleeCombatState.Light1:
                    return Light1KnockbackMultiplier;
                case MeleeCombatState.Light2:
                    return Light2KnockbackMultiplier;
                case MeleeCombatState.Light3_Finisher:
                    return Light3KnockbackMultiplier;
                case MeleeCombatState.DashAttack:
                    return DashAttackKnockbackMultiplier;
                case MeleeCombatState.HeavyRelease:
                    return MinHeavyKnockbackMultiplier + (MaxHeavyKnockbackMultiplier - MinHeavyKnockbackMultiplier) * ChargeRatio;
                default:
                    return 1.0f;
            }
        }
    }
}
