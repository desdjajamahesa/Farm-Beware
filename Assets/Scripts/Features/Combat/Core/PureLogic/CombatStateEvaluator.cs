namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# POCO Combat State Evaluator governing deterministic combo branching matrices,
    /// dynamic animation cancellation windows, and special attack transitions without MonoBehaviour overhead.
    /// </summary>
    public class CombatStateEvaluator
    {
        private CombatActionID _currentAction = CombatActionID.Idle;
        private float _lastActionTimestamp = -999f;
        private float _comboResetWindow = 1.5f;

        public CombatActionID CurrentAction => _currentAction;
        public float LastActionTimestamp => _lastActionTimestamp;
        public float ComboResetWindow
        {
            get => _comboResetWindow;
            set => _comboResetWindow = value > 0f ? value : 1.5f;
        }

        public AttackDefinition CurrentDefinition => GetDefinitionForAction(_currentAction);

        public CombatStateEvaluator(float comboResetWindow = 1.5f)
        {
            _comboResetWindow = comboResetWindow;
            _currentAction = CombatActionID.Idle;
        }

        /// <summary>
        /// Attempts to transition to the next action in the combo matrix given a command and animation progress.
        /// Returns true if a valid transition occurred.
        /// </summary>
        public bool TryEvaluateNextAction(
            CombatCommand command,
            float normalizedAnimTime,
            float currentTime,
            out CombatActionID nextAction)
        {
            nextAction = _currentAction;

            // Check if combo reset window expired while in non-idle state
            if (_currentAction != CombatActionID.Idle && (currentTime - _lastActionTimestamp > _comboResetWindow))
            {
                _currentAction = CombatActionID.Idle;
            }

            AttackDefinition currentDef = CurrentDefinition;

            // Dynamic early cancel evaluation: if currently performing an attack, verify normalized time threshold
            if (_currentAction != CombatActionID.Idle)
            {
                if (normalizedAnimTime < currentDef.MinCancelNormalizedTime)
                {
                    return false;
                }
            }

            switch (_currentAction)
            {
                case CombatActionID.Idle:
                    switch (command)
                    {
                        case CombatCommand.LightAttack:
                            nextAction = CombatActionID.Light1;
                            break;
                        case CombatCommand.HeavyAttack:
                        case CombatCommand.ChargedThrust:
                            nextAction = CombatActionID.ChargedThrust;
                            break;
                        case CombatCommand.DashSweep:
                            nextAction = CombatActionID.DashSweep;
                            break;
                        case CombatCommand.FrontKick:
                            nextAction = CombatActionID.FrontKick;
                            break;
                    }
                    break;

                case CombatActionID.Light1:
                    switch (command)
                    {
                        case CombatCommand.LightAttack:
                            nextAction = CombatActionID.Light2; // L -> L
                            break;
                        case CombatCommand.HeavyAttack:
                            nextAction = CombatActionID.HeavyFinisher1; // L -> H
                            break;
                        case CombatCommand.FrontKick:
                            nextAction = CombatActionID.FrontKick;
                            break;
                        case CombatCommand.DashSweep:
                            nextAction = CombatActionID.DashSweep;
                            break;
                    }
                    break;

                case CombatActionID.Light2:
                    switch (command)
                    {
                        case CombatCommand.LightAttack:
                            nextAction = CombatActionID.Light3; // L -> L -> L
                            break;
                        case CombatCommand.HeavyAttack:
                            nextAction = CombatActionID.HeavyFinisher2; // L -> L -> H
                            break;
                        case CombatCommand.FrontKick:
                            nextAction = CombatActionID.FrontKick;
                            break;
                        case CombatCommand.DashSweep:
                            nextAction = CombatActionID.DashSweep;
                            break;
                    }
                    break;

                case CombatActionID.Light3:
                case CombatActionID.HeavyFinisher1:
                case CombatActionID.HeavyFinisher2:
                case CombatActionID.ChargedThrust:
                case CombatActionID.DashSweep:
                case CombatActionID.FrontKick:
                    // Branch into a fresh sequence once early cancel threshold is satisfied
                    switch (command)
                    {
                        case CombatCommand.LightAttack:
                            nextAction = CombatActionID.Light1;
                            break;
                        case CombatCommand.HeavyAttack:
                        case CombatCommand.ChargedThrust:
                            nextAction = CombatActionID.ChargedThrust;
                            break;
                        case CombatCommand.DashSweep:
                            nextAction = CombatActionID.DashSweep;
                            break;
                        case CombatCommand.FrontKick:
                            nextAction = CombatActionID.FrontKick;
                            break;
                    }
                    break;
            }

            if (nextAction != _currentAction)
            {
                _currentAction = nextAction;
                _lastActionTimestamp = currentTime;
                return true;
            }

            return false;
        }

        public void ForceSetAction(CombatActionID actionId, float currentTime)
        {
            _currentAction = actionId;
            _lastActionTimestamp = currentTime;
        }

        public void ResetToIdle()
        {
            _currentAction = CombatActionID.Idle;
        }

        public void Update(float currentTime)
        {
            if (_currentAction != CombatActionID.Idle)
            {
                if (currentTime - _lastActionTimestamp > _comboResetWindow)
                {
                    _currentAction = CombatActionID.Idle;
                }
            }
        }

        public static AttackDefinition GetDefinitionForAction(CombatActionID actionId)
        {
            switch (actionId)
            {
                case CombatActionID.Light1: return AttackDefinition.DefaultLight1;
                case CombatActionID.Light2: return AttackDefinition.DefaultLight2;
                case CombatActionID.Light3: return AttackDefinition.DefaultLight3;
                case CombatActionID.HeavyFinisher1: return AttackDefinition.DefaultHeavyFinisher1;
                case CombatActionID.HeavyFinisher2: return AttackDefinition.DefaultHeavyFinisher2;
                case CombatActionID.ChargedThrust: return AttackDefinition.DefaultChargedThrust;
                case CombatActionID.DashSweep: return AttackDefinition.DefaultDashSweep;
                case CombatActionID.FrontKick: return AttackDefinition.DefaultFrontKick;
                default: return AttackDefinition.DefaultIdle;
            }
        }
    }
}
