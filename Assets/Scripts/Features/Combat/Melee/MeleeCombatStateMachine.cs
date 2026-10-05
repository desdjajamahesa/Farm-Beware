using System;
using FeaturesCombat.Core.PureLogic;

namespace FeaturesCombat.Melee
{
    /// <summary>
    /// Pure C# POCO Finite State Machine governing player melee combos, heavy charges, dash attacks,
    /// dynamic branching heavy finishers (L -> H vs L -> L -> H), and melee kicks.
    /// Operates without MonoBehaviour dependencies or GC allocations.
    /// </summary>
    public class MeleeCombatStateMachine
    {
        private MeleeCombatState _currentState = MeleeCombatState.Idle;
        private int _comboIndex = 0;
        private float _lastAttackTime = -999f;
        private float _chargeStartTime = 0f;
        private float _currentChargeDuration = 0f;

        private readonly CombatStateEvaluator _evaluator;

        // Configurable Timing & Multipliers
        public float ComboResetWindow
        {
            get => _evaluator.ComboResetWindow;
            set => _evaluator.ComboResetWindow = value;
        }

        public float MinChargeForHeavy { get; set; } = 0.35f;
        public float MaxChargeDuration { get; set; } = 1.0f;

        public float Light1DamageMultiplier { get; set; } = 1.0f;
        public float Light2DamageMultiplier { get; set; } = 1.2f;
        public float Light3DamageMultiplier { get; set; } = 1.6f;
        public float HeavyFinisher1DamageMultiplier { get; set; } = 1.9f;
        public float HeavyFinisher2DamageMultiplier { get; set; } = 2.4f;
        public float DashAttackDamageMultiplier { get; set; } = 1.4f;
        public float FrontKickDamageMultiplier { get; set; } = 0.8f;
        public float MinHeavyDamageMultiplier { get; set; } = 1.8f;
        public float MaxHeavyDamageMultiplier { get; set; } = 3.0f;

        public float Light1KnockbackMultiplier { get; set; } = 1.0f;
        public float Light2KnockbackMultiplier { get; set; } = 1.15f;
        public float Light3KnockbackMultiplier { get; set; } = 1.6f;
        public float HeavyFinisher1KnockbackMultiplier { get; set; } = 2.2f;
        public float HeavyFinisher2KnockbackMultiplier { get; set; } = 2.8f;
        public float DashAttackKnockbackMultiplier { get; set; } = 1.5f;
        public float FrontKickKnockbackMultiplier { get; set; } = 3.0f;
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
            _evaluator = new CombatStateEvaluator(1.5f);
            ResetToIdle();
        }

        /// <summary>
        /// Attempts to advance or trigger a light combo attack evaluated with dynamic cancellation.
        /// </summary>
        public bool TryTriggerLight(float currentTime, float normalizedAnimTime, out MeleeCombatState newState, out int comboStep)
        {
            if (_currentState == MeleeCombatState.HeavyCharging)
            {
                newState = _currentState;
                comboStep = _comboIndex;
                return false;
            }

            if (_evaluator.TryEvaluateNextAction(CombatCommand.LightAttack, normalizedAnimTime, currentTime, out CombatActionID nextAction))
            {
                _currentState = MapActionToState(nextAction);
                _comboIndex = MapStateToComboIndex(_currentState);
                _lastAttackTime = currentTime;

                newState = _currentState;
                comboStep = _comboIndex;
                OnStateChanged?.Invoke(_currentState);
                return true;
            }

            newState = _currentState;
            comboStep = _comboIndex;
            return false;
        }

        /// <summary>
        /// Backwards-compatible overload treating animation progress as completed (normalizedTime = 1.0f).
        /// </summary>
        public bool TryTriggerLight(float currentTime, out MeleeCombatState newState, out int comboStep)
        {
            return TryTriggerLight(currentTime, 1.0f, out newState, out comboStep);
        }

        /// <summary>
        /// Attempts to trigger a heavy finisher branching off of a light attack sequence (L -> H or L -> L -> H).
        /// </summary>
        public bool TryTriggerHeavyFinisher(float currentTime, float normalizedAnimTime, out MeleeCombatState newState)
        {
            if (_evaluator.TryEvaluateNextAction(CombatCommand.HeavyAttack, normalizedAnimTime, currentTime, out CombatActionID nextAction))
            {
                _currentState = MapActionToState(nextAction);
                _comboIndex = MapStateToComboIndex(_currentState);
                _lastAttackTime = currentTime;

                newState = _currentState;
                OnStateChanged?.Invoke(_currentState);
                return true;
            }

            newState = _currentState;
            return false;
        }

        /// <summary>
        /// Attempts to trigger a front kick crowd control attack.
        /// </summary>
        public bool TryTriggerFrontKick(float currentTime, float normalizedAnimTime, out MeleeCombatState newState)
        {
            if (_evaluator.TryEvaluateNextAction(CombatCommand.FrontKick, normalizedAnimTime, currentTime, out CombatActionID nextAction))
            {
                _currentState = MapActionToState(nextAction);
                _comboIndex = 0;
                _lastAttackTime = currentTime;

                newState = _currentState;
                OnStateChanged?.Invoke(_currentState);
                return true;
            }

            newState = _currentState;
            return false;
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
            _evaluator.ForceSetAction(CombatActionID.ChargedThrust, currentTime);

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
                ResetToIdle();
                return false;
            }

            finalChargeRatio = ChargeRatio;
            _currentState = MeleeCombatState.HeavyRelease;
            _lastAttackTime = currentTime;
            _comboIndex = 0;
            _evaluator.ForceSetAction(CombatActionID.ChargedThrust, currentTime);

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
            _evaluator.ForceSetAction(CombatActionID.DashSweep, currentTime);

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
            _evaluator.ResetToIdle();
            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Checks combo window expiration when not actively attacking.
        /// </summary>
        public void Update(float currentTime)
        {
            if (_currentState != MeleeCombatState.HeavyCharging && _currentState != MeleeCombatState.Idle)
            {
                _evaluator.Update(currentTime);
                if (_evaluator.CurrentAction == CombatActionID.Idle)
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
                case MeleeCombatState.HeavyFinisher1:
                    return HeavyFinisher1DamageMultiplier;
                case MeleeCombatState.HeavyFinisher2:
                    return HeavyFinisher2DamageMultiplier;
                case MeleeCombatState.DashAttack:
                    return DashAttackDamageMultiplier;
                case MeleeCombatState.FrontKick:
                    return FrontKickDamageMultiplier;
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
                case MeleeCombatState.HeavyFinisher1:
                    return HeavyFinisher1KnockbackMultiplier;
                case MeleeCombatState.HeavyFinisher2:
                    return HeavyFinisher2KnockbackMultiplier;
                case MeleeCombatState.DashAttack:
                    return DashAttackKnockbackMultiplier;
                case MeleeCombatState.FrontKick:
                    return FrontKickKnockbackMultiplier;
                case MeleeCombatState.HeavyRelease:
                    return MinHeavyKnockbackMultiplier + (MaxHeavyKnockbackMultiplier - MinHeavyKnockbackMultiplier) * ChargeRatio;
                default:
                    return 1.0f;
            }
        }

        private static MeleeCombatState MapActionToState(CombatActionID actionId)
        {
            switch (actionId)
            {
                case CombatActionID.Light1: return MeleeCombatState.Light1;
                case CombatActionID.Light2: return MeleeCombatState.Light2;
                case CombatActionID.Light3: return MeleeCombatState.Light3_Finisher;
                case CombatActionID.HeavyFinisher1: return MeleeCombatState.HeavyFinisher1;
                case CombatActionID.HeavyFinisher2: return MeleeCombatState.HeavyFinisher2;
                case CombatActionID.ChargedThrust: return MeleeCombatState.HeavyRelease;
                case CombatActionID.DashSweep: return MeleeCombatState.DashAttack;
                case CombatActionID.FrontKick: return MeleeCombatState.FrontKick;
                default: return MeleeCombatState.Idle;
            }
        }

        private static int MapStateToComboIndex(MeleeCombatState state)
        {
            switch (state)
            {
                case MeleeCombatState.Light1: return 0;
                case MeleeCombatState.Light2: return 1;
                case MeleeCombatState.Light3_Finisher: return 2;
                case MeleeCombatState.HeavyFinisher1: return 1;
                case MeleeCombatState.HeavyFinisher2: return 2;
                default: return 0;
            }
        }
    }
}
