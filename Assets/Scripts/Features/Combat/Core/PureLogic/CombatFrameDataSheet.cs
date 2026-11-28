using System;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Discrete execution phases for a single combat swing or action.
    /// </summary>
    public enum CombatPhase
    {
        Idle = 0,
        Startup = 1,
        Active = 2,
        Recovery = 3
    }

    /// <summary>
    /// Discrete player actions evaluated against the 2D cancel and priority buffer matrix.
    /// Ordered numerically so higher-priority defensive inputs outrank offensive chains.
    /// </summary>
    public enum PlayerCancelAction
    {
        None = 0,
        Move = 1,
        Jump = 2,
        LightAttack = 3,
        DashAttack = 4,
        FrontKick = 5,
        HeavyAttack = 6,
        Dodge = 7,
        Parry = 8
    }

    /// <summary>
    /// Immutable 60 FPS calibrated frame data specification for a player combat move.
    /// Acts as the Single Source of Truth for startup, active hitbox duration, recovery,
    /// cancel thresholds, forward micro-lunge kinematics, poise damage, and impact tier.
    /// Zero-GC value type.
    /// </summary>
    public readonly struct PlayerMoveFrameData
    {
        public readonly CombatActionID ActionID;
        public readonly float ClipDurationSec;
        public readonly float StartupSec;
        public readonly float ActiveSec;
        public readonly float RecoverySec;
        public readonly float ChainCancelNormTime;
        public readonly float DodgeCancelNormTime;
        public readonly float ParryCancelNormTime;
        public readonly float MoveCancelNormTime;
        public readonly float ForwardLungeSpeed;
        public readonly float LungeDurationSec;
        public readonly float DamageMultiplier;
        public readonly float PoiseDamage;
        public readonly float KnockbackForce;
        public readonly float StaminaCost;
        public readonly float ReachMultiplier;
        public readonly bool Is360Sweep;
        public readonly AttackImpactTier ImpactTier;

        public PlayerMoveFrameData(
            CombatActionID actionId,
            float clipDurationSec,
            float startupSec,
            float activeSec,
            float recoverySec,
            float chainCancelNormTime,
            float dodgeCancelNormTime,
            float parryCancelNormTime,
            float moveCancelNormTime,
            float forwardLungeSpeed,
            float lungeDurationSec,
            float damageMultiplier,
            float poiseDamage,
            float knockbackForce,
            float staminaCost,
            float reachMultiplier,
            bool is360Sweep,
            AttackImpactTier impactTier)
        {
            ActionID = actionId;
            ClipDurationSec = clipDurationSec;
            StartupSec = startupSec;
            ActiveSec = activeSec;
            RecoverySec = recoverySec;
            ChainCancelNormTime = chainCancelNormTime;
            DodgeCancelNormTime = dodgeCancelNormTime;
            ParryCancelNormTime = parryCancelNormTime;
            MoveCancelNormTime = moveCancelNormTime;
            ForwardLungeSpeed = forwardLungeSpeed;
            LungeDurationSec = lungeDurationSec;
            DamageMultiplier = damageMultiplier;
            PoiseDamage = poiseDamage;
            KnockbackForce = knockbackForce;
            StaminaCost = staminaCost;
            ReachMultiplier = reachMultiplier;
            Is360Sweep = is360Sweep;
            ImpactTier = impactTier;
        }
    }

    /// <summary>
    /// Single Source of Truth for player combat frame data, cancel windows, and dodge-end cancel gating.
    /// Zero-GC pure C# logic.
    /// </summary>
    public static class CombatFrameDataSheet
    {
        public const float DODGE_END_CANCEL_PROGRESS = 0.78f;

        private static readonly PlayerMoveFrameData MoveIdle = new PlayerMoveFrameData(
            CombatActionID.Idle, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, false, AttackImpactTier.LightSlash);

        private static readonly PlayerMoveFrameData MoveLight1 = new PlayerMoveFrameData(
            CombatActionID.Light1, 0.333f, 0.12f, 0.16f, 0.053f, 0.50f, 0.22f, 0.18f, 0.80f, 2.8f, 0.10f, 1.0f, 18f, 1.8f, 10f, 1.00f, false, AttackImpactTier.LightSlash);

        private static readonly PlayerMoveFrameData MoveLight2 = new PlayerMoveFrameData(
            CombatActionID.Light2, 0.700f, 0.20f, 0.22f, 0.280f, 0.50f, 0.24f, 0.20f, 0.68f, 3.2f, 0.12f, 1.2f, 24f, 2.2f, 10f, 1.05f, false, AttackImpactTier.LightSlash);

        private static readonly PlayerMoveFrameData MoveLight3 = new PlayerMoveFrameData(
            CombatActionID.Light3, 1.033f, 0.18f, 0.48f, 0.373f, 0.55f, 0.28f, 0.22f, 0.72f, 3.8f, 0.15f, 1.6f, 42f, 4.8f, 15f, 1.15f, true, AttackImpactTier.ComboFinisher);

        private static readonly PlayerMoveFrameData MoveLight4 = new PlayerMoveFrameData(
            CombatActionID.Light4, 1.033f, 0.20f, 0.45f, 0.383f, 0.65f, 0.28f, 0.22f, 0.74f, 4.0f, 0.15f, 1.85f, 48f, 5.2f, 15f, 1.20f, true, AttackImpactTier.ComboFinisher);

        private static readonly PlayerMoveFrameData MoveChargedThrust = new PlayerMoveFrameData(
            CombatActionID.ChargedThrust, 0.833f, 0.28f, 0.28f, 0.273f, 0.55f, 0.28f, 0.22f, 0.74f, 4.4f, 0.14f, 2.0f, 55f, 4.8f, 21f, 1.25f, true, AttackImpactTier.HeavyCleave);

        private static readonly PlayerMoveFrameData MoveHeavyFinisher1 = new PlayerMoveFrameData(
            CombatActionID.HeavyFinisher1, 0.400f, 0.12f, 0.24f, 0.040f, 0.55f, 0.28f, 0.22f, 0.84f, 4.0f, 0.11f, 1.9f, 50f, 5.0f, 21f, 1.25f, true, AttackImpactTier.HeavyCleave);

        private static readonly PlayerMoveFrameData MoveHeavyChain2 = new PlayerMoveFrameData(
            CombatActionID.HeavyChain2, 0.400f, 0.12f, 0.24f, 0.040f, 0.60f, 0.28f, 0.22f, 0.84f, 4.0f, 0.11f, 2.2f, 55f, 5.2f, 21f, 1.25f, true, AttackImpactTier.HeavyCleave);

        private static readonly PlayerMoveFrameData MoveHeavyFinisher2 = new PlayerMoveFrameData(
            CombatActionID.HeavyFinisher2, 0.800f, 0.38f, 0.26f, 0.160f, 0.60f, 0.30f, 0.24f, 0.80f, 4.6f, 0.14f, 2.4f, 62f, 5.8f, 21f, 1.30f, true, AttackImpactTier.HeavyCleave);

        private static readonly PlayerMoveFrameData MoveHeavyChain3 = new PlayerMoveFrameData(
            CombatActionID.HeavyChain3, 0.800f, 0.38f, 0.26f, 0.160f, 0.62f, 0.30f, 0.24f, 0.80f, 4.8f, 0.14f, 2.5f, 65f, 6.0f, 21f, 1.30f, true, AttackImpactTier.HeavyCleave);

        private static readonly PlayerMoveFrameData MoveHeavyChain4 = new PlayerMoveFrameData(
            CombatActionID.HeavyChain4, 0.933f, 0.40f, 0.28f, 0.253f, 0.70f, 0.32f, 0.25f, 0.76f, 5.2f, 0.16f, 3.0f, 85f, 7.2f, 21f, 1.35f, true, AttackImpactTier.PoiseCrushOrWallSplat);

        private static readonly PlayerMoveFrameData MoveDashSweep = new PlayerMoveFrameData(
            CombatActionID.DashSweep, 0.700f, 0.08f, 0.20f, 0.420f, 0.42f, 0.22f, 0.18f, 0.62f, 7.5f, 0.16f, 1.4f, 30f, 4.2f, 18f, 1.25f, false, AttackImpactTier.ComboFinisher);

        private static readonly PlayerMoveFrameData MoveFrontKick = new PlayerMoveFrameData(
            CombatActionID.FrontKick, 0.460f, 0.10f, 0.14f, 0.220f, 0.45f, 0.20f, 0.18f, 0.65f, 3.0f, 0.10f, 0.8f, 55f, 6.5f, 12f, 1.05f, false, AttackImpactTier.ComboFinisher);

        public static PlayerMoveFrameData GetMove(CombatActionID actionId)
        {
            switch (actionId)
            {
                case CombatActionID.Light1: return MoveLight1;
                case CombatActionID.Light2: return MoveLight2;
                case CombatActionID.Light3: return MoveLight3;
                case CombatActionID.Light4: return MoveLight4;
                case CombatActionID.ChargedThrust: return MoveChargedThrust;
                case CombatActionID.HeavyFinisher1: return MoveHeavyFinisher1;
                case CombatActionID.HeavyChain2: return MoveHeavyChain2;
                case CombatActionID.HeavyFinisher2: return MoveHeavyFinisher2;
                case CombatActionID.HeavyChain3: return MoveHeavyChain3;
                case CombatActionID.HeavyChain4: return MoveHeavyChain4;
                case CombatActionID.DashSweep: return MoveDashSweep;
                case CombatActionID.FrontKick: return MoveFrontKick;
                default: return MoveIdle;
            }
        }

        public static CombatPhase EvaluatePhase(in PlayerMoveFrameData move, float elapsedSec)
        {
            if (move.ActionID == CombatActionID.Idle)
            {
                return CombatPhase.Idle;
            }

            if (elapsedSec < move.StartupSec)
            {
                return CombatPhase.Startup;
            }

            if (elapsedSec < move.StartupSec + move.ActiveSec)
            {
                return CombatPhase.Active;
            }

            return CombatPhase.Recovery;
        }

        /// <summary>
        /// Evaluates the 2D Cancel Priority Matrix for player combat and dodge states.
        /// </summary>
        public static bool CanCancel(
            CombatActionID currentAction,
            float normalizedTime,
            PlayerCancelAction requestedCancel,
            bool isInRecoveryHold,
            bool isDodging = false,
            float dodgeProgress = 0f)
        {
            if (requestedCancel == PlayerCancelAction.None)
            {
                return false;
            }

            if (isDodging)
            {
                if (dodgeProgress < DODGE_END_CANCEL_PROGRESS)
                {
                    return false;
                }

                return requestedCancel == PlayerCancelAction.Parry ||
                       requestedCancel == PlayerCancelAction.LightAttack ||
                       requestedCancel == PlayerCancelAction.HeavyAttack ||
                       requestedCancel == PlayerCancelAction.DashAttack;
            }

            if (currentAction == CombatActionID.Idle)
            {
                return true;
            }

            if (isInRecoveryHold)
            {
                return true;
            }

            PlayerMoveFrameData move = GetMove(currentAction);
            switch (requestedCancel)
            {
                case PlayerCancelAction.Parry:
                    return normalizedTime >= move.ParryCancelNormTime;

                case PlayerCancelAction.Dodge:
                    return normalizedTime >= move.DodgeCancelNormTime;

                case PlayerCancelAction.LightAttack:
                case PlayerCancelAction.HeavyAttack:
                case PlayerCancelAction.DashAttack:
                case PlayerCancelAction.FrontKick:
                    return normalizedTime >= move.ChainCancelNormTime;

                case PlayerCancelAction.Move:
                case PlayerCancelAction.Jump:
                    return normalizedTime >= move.MoveCancelNormTime;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Zero-GC prioritized combat input buffer preventing dropped presses before cancel windows open.
    /// Priority: Parry (8) > Dodge (7) > HeavyAttack (6) > FrontKick (5) > DashAttack (4) > LightAttack (3) > Jump (2).
    /// </summary>
    public sealed class CombatInputBuffer
    {
        public const float DEFAULT_DEFENSE_BUFFER_WINDOW = 0.22f;
        public const float DEFAULT_ATTACK_BUFFER_WINDOW = 0.35f;

        private float _parryExpire = -1f;
        private float _dodgeExpire = -1f;
        private float _heavyExpire = -1f;
        private float _frontKickExpire = -1f;
        private float _dashExpire = -1f;
        private float _lightExpire = -1f;
        private float _jumpExpire = -1f;

        public static int GetPriority(PlayerCancelAction action) => (int)action;

        public void Buffer(PlayerCancelAction action, float currentTime, float windowSec = -1f)
        {
            if (action == PlayerCancelAction.None || action == PlayerCancelAction.Move)
            {
                return;
            }

            float window = windowSec > 0f
                ? windowSec
                : (action == PlayerCancelAction.Dodge || action == PlayerCancelAction.Parry || action == PlayerCancelAction.Jump
                    ? DEFAULT_DEFENSE_BUFFER_WINDOW
                    : DEFAULT_ATTACK_BUFFER_WINDOW);

            float expire = currentTime + window;
            switch (action)
            {
                case PlayerCancelAction.Parry: _parryExpire = expire; break;
                case PlayerCancelAction.Dodge: _dodgeExpire = expire; break;
                case PlayerCancelAction.HeavyAttack: _heavyExpire = expire; break;
                case PlayerCancelAction.FrontKick: _frontKickExpire = expire; break;
                case PlayerCancelAction.DashAttack: _dashExpire = expire; break;
                case PlayerCancelAction.LightAttack: _lightExpire = expire; break;
                case PlayerCancelAction.Jump: _jumpExpire = expire; break;
            }
        }

        public bool HasBuffered(PlayerCancelAction action, float currentTime)
        {
            switch (action)
            {
                case PlayerCancelAction.Parry: return currentTime <= _parryExpire;
                case PlayerCancelAction.Dodge: return currentTime <= _dodgeExpire;
                case PlayerCancelAction.HeavyAttack: return currentTime <= _heavyExpire;
                case PlayerCancelAction.FrontKick: return currentTime <= _frontKickExpire;
                case PlayerCancelAction.DashAttack: return currentTime <= _dashExpire;
                case PlayerCancelAction.LightAttack: return currentTime <= _lightExpire;
                case PlayerCancelAction.Jump: return currentTime <= _jumpExpire;
                default: return false;
            }
        }

        public PlayerCancelAction PeekValid(float currentTime)
        {
            if (currentTime <= _parryExpire) return PlayerCancelAction.Parry;
            if (currentTime <= _dodgeExpire) return PlayerCancelAction.Dodge;
            if (currentTime <= _heavyExpire) return PlayerCancelAction.HeavyAttack;
            if (currentTime <= _frontKickExpire) return PlayerCancelAction.FrontKick;
            if (currentTime <= _dashExpire) return PlayerCancelAction.DashAttack;
            if (currentTime <= _lightExpire) return PlayerCancelAction.LightAttack;
            if (currentTime <= _jumpExpire) return PlayerCancelAction.Jump;
            return PlayerCancelAction.None;
        }

        public bool TryConsume(PlayerCancelAction action, float currentTime)
        {
            if (!HasBuffered(action, currentTime))
            {
                return false;
            }

            ClearAction(action);
            return true;
        }

        public bool TryConsumeHighestPriority(float currentTime, out PlayerCancelAction action)
        {
            action = PeekValid(currentTime);
            if (action == PlayerCancelAction.None)
            {
                return false;
            }

            ClearAction(action);
            return true;
        }

        private void ClearAction(PlayerCancelAction action)
        {
            switch (action)
            {
                case PlayerCancelAction.Parry: _parryExpire = -1f; break;
                case PlayerCancelAction.Dodge: _dodgeExpire = -1f; break;
                case PlayerCancelAction.HeavyAttack: _heavyExpire = -1f; break;
                case PlayerCancelAction.FrontKick: _frontKickExpire = -1f; break;
                case PlayerCancelAction.DashAttack: _dashExpire = -1f; break;
                case PlayerCancelAction.LightAttack: _lightExpire = -1f; break;
                case PlayerCancelAction.Jump: _jumpExpire = -1f; break;
            }
        }

        public void Clear()
        {
            _parryExpire = -1f;
            _dodgeExpire = -1f;
            _heavyExpire = -1f;
            _frontKickExpire = -1f;
            _dashExpire = -1f;
            _lightExpire = -1f;
            _jumpExpire = -1f;
        }
    }
}
