namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# POCO Defense Evaluator governing invulnerability frames (i-Frames) during Dodge Roll
    /// and Precision Deflect (Parry) timing windows without MonoBehaviour overhead.
    /// </summary>
    public class DefenseEvaluator
    {
        public const float DEFAULT_PARRY_WINDOW = 0.35f; // 350ms generous precision deflect window
        public const float DEFAULT_DODGE_IFRAME_DURATION = 0.30f; // 300ms invulnerability frames (18 frames at 60 Hz)

        private float _parryTimer = 0f;
        private float _dodgeIFrameTimer = 0f;
        private float _baseParryWindow = DEFAULT_PARRY_WINDOW;
        private float _baseDodgeDuration = DEFAULT_DODGE_IFRAME_DURATION;

        public bool IsParryActive => _parryTimer > 0f;
        public bool IsDodgeInvulnerable => _dodgeIFrameTimer > 0f;

        public float ParryWindow
        {
            get => _baseParryWindow;
            set => _baseParryWindow = value > 0f ? value : DEFAULT_PARRY_WINDOW;
        }

        public float DodgeIFrameDuration
        {
            get => _baseDodgeDuration;
            set => _baseDodgeDuration = value > 0f ? value : DEFAULT_DODGE_IFRAME_DURATION;
        }

        public DefenseEvaluator(
            float parryWindow = DEFAULT_PARRY_WINDOW,
            float dodgeIFrameDuration = DEFAULT_DODGE_IFRAME_DURATION)
        {
            _baseParryWindow = parryWindow;
            _baseDodgeDuration = dodgeIFrameDuration;
        }

        /// <summary>
        /// Initiates a precision parry window with optional bonus extension from food buffs or upgrades.
        /// </summary>
        public void TriggerParry(float bonusWindowSeconds = 0f)
        {
            _parryTimer = _baseParryWindow + (bonusWindowSeconds > 0f ? bonusWindowSeconds : 0f);
        }

        /// <summary>
        /// Initiates dodge roll invulnerability frames with optional bonus duration.
        /// </summary>
        public void TriggerDodge(float bonusDurationSeconds = 0f)
        {
            _dodgeIFrameTimer = _baseDodgeDuration + (bonusDurationSeconds > 0f ? bonusDurationSeconds : 0f);
        }

        /// <summary>
        /// Evaluates incoming damage against active defense states.
        /// Returns true if damage should be negated (via parry or dodge).
        /// </summary>
        public bool EvaluateIncomingDamage(
            int incomingDamage,
            bool isUnblockable,
            out bool parrySuccessful,
            out bool dodgeSuccessful)
        {
            parrySuccessful = false;
            dodgeSuccessful = false;

            // 1. Precision Deflect / Parry Check
            if (_parryTimer > 0f)
            {
                if (!isUnblockable)
                {
                    parrySuccessful = true;
                    _parryTimer = 0f; // Consume parry window on success
                    return true;      // 100% damage negated
                }
            }

            // 2. Dodge Roll i-Frames Check
            if (_dodgeIFrameTimer > 0f)
            {
                dodgeSuccessful = true;
                return true; // 100% damage negated
            }

            return false; // Take full damage
        }

        /// <summary>
        /// Advances defensive timers.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_parryTimer > 0f)
            {
                _parryTimer -= deltaTime;
                if (_parryTimer < 0f) _parryTimer = 0f;
            }

            if (_dodgeIFrameTimer > 0f)
            {
                _dodgeIFrameTimer -= deltaTime;
                if (_dodgeIFrameTimer < 0f) _dodgeIFrameTimer = 0f;
            }
        }

        public void Reset()
        {
            _parryTimer = 0f;
            _dodgeIFrameTimer = 0f;
        }
    }
}
