namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# POCO Defense Evaluator governing invulnerability frames (i-Frames) during Dodge Roll,
    /// Precision Deflect (Parry) timing windows, whiff recovery lockout,
    /// Perfect Parry Riposte counter-strike windows, and Phantom Dodge Empowerment without MonoBehaviour overhead.
    /// </summary>
    public class DefenseEvaluator
    {
        public const float DEFAULT_PARRY_WINDOW = 0.35f; // 350ms generous precision deflect window
        public const float DEFAULT_DODGE_IFRAME_DURATION = 0.30f; // 300ms invulnerability frames (18 frames at 60 Hz)
        public const float DEFAULT_WHIFF_DURATION = 0.35f; // 350ms recovery lockout upon unintercepted parry
        public const float DEFAULT_RIPOSTE_WINDOW = 1.20f; // 1.2s counter-strike window after successful parry
        public const float DEFAULT_DODGE_EMPOWER_DURATION = 3.00f; // 3.0s +15% damage empowerment after evading an attack mid-dodge

        private float _parryTimer = 0f;
        private float _dodgeIFrameTimer = 0f;
        private float _whiffTimer = 0f;
        private float _riposteTimer = 0f;
        private float _dodgeEmpowerTimer = 0f;

        private float _baseParryWindow = DEFAULT_PARRY_WINDOW;
        private float _baseDodgeDuration = DEFAULT_DODGE_IFRAME_DURATION;
        private float _baseWhiffDuration = DEFAULT_WHIFF_DURATION;

        public bool IsParryActive => _parryTimer > 0f;
        public bool IsDodgeInvulnerable => _dodgeIFrameTimer > 0f;
        public bool IsInWhiffLockout => _whiffTimer > 0f;
        public bool CanInitiateParry => _parryTimer <= 0f && _whiffTimer <= 0f;
        public float RemainingWhiffTime => _whiffTimer;

        public bool IsRiposteWindowActive => _riposteTimer > 0f;
        public bool IsDodgeEmpowermentActive => _dodgeEmpowerTimer > 0f;
        public float RemainingRiposteTime => _riposteTimer;
        public float RemainingDodgeEmpowerTime => _dodgeEmpowerTimer;

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
        /// Opens a Riposte counter-strike window on parry or Dodge Empowerment buff on i-frame evasion.
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
                    _riposteTimer = DEFAULT_RIPOSTE_WINDOW; // Open 1.2s Riposte counter window
                    return true;      // 100% damage negated
                }
            }

            // 2. Dodge Roll i-Frames Check
            if (_dodgeIFrameTimer > 0f)
            {
                dodgeSuccessful = true;
                _dodgeEmpowerTimer = DEFAULT_DODGE_EMPOWER_DURATION; // Activate 3.0s Phantom Dodge empowerment
                return true; // 100% damage negated
            }

            return false; // Take full damage
        }

        /// <summary>
        /// Attempts to consume an active Riposte counter-strike window when the player attacks.
        /// Returns true if a Riposte was available and consumed.
        /// </summary>
        public bool TryConsumeRiposte()
        {
            if (_riposteTimer > 0f)
            {
                _riposteTimer = 0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Explicitly activates or refreshes the Phantom Dodge Empowerment window.
        /// </summary>
        public void ActivateDodgeEmpowerment(float duration = DEFAULT_DODGE_EMPOWER_DURATION)
        {
            if (duration > _dodgeEmpowerTimer)
            {
                _dodgeEmpowerTimer = duration;
            }
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

            if (_riposteTimer > 0f)
            {
                _riposteTimer -= deltaTime;
                if (_riposteTimer < 0f) _riposteTimer = 0f;
            }

            if (_dodgeEmpowerTimer > 0f)
            {
                _dodgeEmpowerTimer -= deltaTime;
                if (_dodgeEmpowerTimer < 0f) _dodgeEmpowerTimer = 0f;
            }
        }

        public void Reset()
        {
            _parryTimer = 0f;
            _dodgeIFrameTimer = 0f;
            _whiffTimer = 0f;
            _riposteTimer = 0f;
            _dodgeEmpowerTimer = 0f;
        }
    }
}
