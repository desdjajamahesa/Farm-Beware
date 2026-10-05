namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# POCO Defense Evaluator governing invulnerability frames (i-Frames) during Dodge Roll,
    /// Precision Deflect (Parry) timing windows, and whiff recovery lockout without MonoBehaviour overhead.
    /// </summary>
    public class DefenseEvaluator
    {
        public const float DEFAULT_PARRY_WINDOW = 0.35f; // 350ms generous precision deflect window
        public const float DEFAULT_DODGE_IFRAME_DURATION = 0.30f; // 300ms invulnerability frames (18 frames at 60 Hz)
        public const float DEFAULT_WHIFF_DURATION = 0.35f; // 350ms recovery lockout upon unintercepted parry

        private float _parryTimer = 0f;
        private float _dodgeIFrameTimer = 0f;
        private float _whiffTimer = 0f;

        private float _baseParryWindow = DEFAULT_PARRY_WINDOW;
        private float _baseDodgeDuration = DEFAULT_DODGE_IFRAME_DURATION;
        private float _baseWhiffDuration = DEFAULT_WHIFF_DURATION;

        public bool IsParryActive => _parryTimer > 0f;
        public bool IsDodgeInvulnerable => _dodgeIFrameTimer > 0f;
        public bool IsInWhiffLockout => _whiffTimer > 0f;
        public bool CanInitiateParry => _parryTimer <= 0f && _whiffTimer <= 0f;
        public float RemainingWhiffTime => _whiffTimer;

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

        public float WhiffDuration
        {
            get => _baseWhiffDuration;
            set => _baseWhiffDuration = value > 0f ? value : DEFAULT_WHIFF_DURATION;
        }

        public DefenseEvaluator(
            float parryWindow = DEFAULT_PARRY_WINDOW,
            float dodgeIFrameDuration = DEFAULT_DODGE_IFRAME_DURATION,
            float whiffDuration = DEFAULT_WHIFF_DURATION)
        {
            _baseParryWindow = parryWindow;
            _baseDodgeDuration = dodgeIFrameDuration;
            _baseWhiffDuration = whiffDuration;
        }

        /// <summary>
        /// Initiates a precision parry window with optional bonus extension from food buffs or upgrades.
        /// Returns false if in whiff lockout or parry is already active.
        /// </summary>
        public bool TriggerParry(float bonusWindowSeconds = 0f)
        {
            if (!CanInitiateParry) return false;

            _parryTimer = _baseParryWindow + (bonusWindowSeconds > 0f ? bonusWindowSeconds : 0f);
            _whiffTimer = 0f;
            return true;
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
                    _whiffTimer = 0f; // Successful deflect cancels any whiff penalty
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
        /// Advances defensive timers and transitions expired parries into whiff recovery lockout.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_parryTimer > 0f)
            {
                _parryTimer -= deltaTime;
                if (_parryTimer <= 0f)
                {
                    _parryTimer = 0f;
                    // Natural expiration without deflecting an attack triggers whiff recovery lockout
                    _whiffTimer = _baseWhiffDuration;
                }
            }
            else if (_whiffTimer > 0f)
            {
                _whiffTimer -= deltaTime;
                if (_whiffTimer < 0f) _whiffTimer = 0f;
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
            _whiffTimer = 0f;
        }
    }
}
