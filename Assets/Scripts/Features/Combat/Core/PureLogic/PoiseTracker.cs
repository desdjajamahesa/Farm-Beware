namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Discrete crowd control reaction tiers resulting from poise depletion.
    /// </summary>
    public enum StaggerTier
    {
        None,
        MicroStagger, // Short flinch (0.18s)
        HeavyStagger, // Full stagger stun (0.65s)
        Knockdown     // Floor knockdown with recovery (1.20s)
    }

    /// <summary>
    /// Pure C# POCO Poise & Crowd Control tracker governing deterministic poise damage,
    /// super armor mitigation, tiered stagger thresholds, and post-stagger replenishment.
    /// </summary>
    public class PoiseTracker
    {
        private float _maxPoise = 100f;
        private float _currentPoise = 100f;
        private float _regenRate = 20f;
        private float _regenDelay = 3.0f;
        private float _timeSinceLastPoiseDamage = 0f;
        private StaggerTier _currentStagger = StaggerTier.None;
        private float _staggerTimer = 0f;
        private bool _isSuperArmorActive = false;

        public float MaxPoise => _maxPoise;
        public float CurrentPoise => _currentPoise;
        public StaggerTier CurrentStagger => _currentStagger;
        public float StaggerTimer => _staggerTimer;
        public bool IsStaggered => _currentStagger != StaggerTier.None;

        public bool IsSuperArmorActive
        {
            get => _isSuperArmorActive;
            set => _isSuperArmorActive = value;
        }

        public PoiseTracker(float maxPoise = 100f, float regenRate = 20f, float regenDelay = 3.0f)
        {
            Initialize(maxPoise, regenRate, regenDelay);
        }

        public void Initialize(float maxPoise, float regenRate, float regenDelay)
        {
            _maxPoise = maxPoise > 0f ? maxPoise : 100f;
            _currentPoise = _maxPoise;
            _regenRate = regenRate >= 0f ? regenRate : 20f;
            _regenDelay = regenDelay >= 0f ? regenDelay : 3.0f;
            _currentStagger = StaggerTier.None;
            _staggerTimer = 0f;
            _timeSinceLastPoiseDamage = 0f;
            _isSuperArmorActive = false;
        }

        /// <summary>
        /// Applies poise damage and evaluates whether a stagger tier threshold was breached.
        /// </summary>
        public StaggerTier ApplyPoiseDamage(float poiseDamage, float attackImpulse, out bool poiseBroken)
        {
            poiseBroken = false;
            _timeSinceLastPoiseDamage = 0f;

            if (_isSuperArmorActive)
            {
                // Super Armor mitigates 60% of incoming poise damage
                poiseDamage *= 0.40f;
                _currentPoise -= poiseDamage;

                if (_currentPoise <= 0f)
                {
                    _currentPoise = 0f;
                    poiseBroken = true;
                    _isSuperArmorActive = false; // Poise break strips active super armor
                    _currentStagger = StaggerTier.HeavyStagger;
                    _staggerTimer = 0.65f;
                    return _currentStagger;
                }

                // If super armor was not broken, target is completely immune to stagger
                return StaggerTier.None;
            }

            _currentPoise -= poiseDamage;

            if (_currentPoise <= 0f)
            {
                _currentPoise = 0f;
                poiseBroken = true;

                if (attackImpulse >= 2.0f)
                {
                    _currentStagger = StaggerTier.Knockdown;
                    _staggerTimer = 1.20f;
                }
                else if (attackImpulse >= 1.2f)
                {
                    _currentStagger = StaggerTier.HeavyStagger;
                    _staggerTimer = 0.65f;
                }
                else
                {
                    _currentStagger = StaggerTier.MicroStagger;
                    _staggerTimer = 0.18f;
                }

                return _currentStagger;
            }

            return StaggerTier.None;
        }

        /// <summary>
        /// Updates stagger recovery and linear poise regeneration.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_currentStagger != StaggerTier.None)
            {
                _staggerTimer -= deltaTime;
                if (_staggerTimer <= 0f)
                {
                    _staggerTimer = 0f;
                    _currentStagger = StaggerTier.None;
                    _currentPoise = _maxPoise; // Replenish full poise upon stagger recovery
                }
                return;
            }

            _timeSinceLastPoiseDamage += deltaTime;
            if (_timeSinceLastPoiseDamage >= _regenDelay && _currentPoise < _maxPoise)
            {
                _currentPoise += _regenRate * deltaTime;
                if (_currentPoise > _maxPoise)
                {
                    _currentPoise = _maxPoise;
                }
            }
        }

        public void ResetPoise()
        {
            _currentPoise = _maxPoise;
            _currentStagger = StaggerTier.None;
            _staggerTimer = 0f;
            _timeSinceLastPoiseDamage = 0f;
        }
    }
}
