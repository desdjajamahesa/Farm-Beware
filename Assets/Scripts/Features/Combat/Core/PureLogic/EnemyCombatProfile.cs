using UnityEngine;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# immutable readability, tracking lock-off, poise, and locomotion profile per enemy archetype.
    /// Eliminates shared uniform windups and climax-snap tracking so players can sidestep committed strikes.
    /// Zero-GC value type.
    /// </summary>
    public readonly struct EnemyCombatProfile
    {
        public readonly float WindupDurationSec;
        public readonly float TrackingCutoffRatio;
        public readonly float ActiveStrikeDurationSec;
        public readonly float RecoveryPunishSec;
        public readonly float MaxPoise;
        public readonly float PoiseRegenRate;
        public readonly float PoiseRegenDelay;
        public readonly bool InnateSuperArmor;
        public readonly bool WindupSuperArmor;
        public readonly float KnockbackResistance;
        public readonly float MoveAcceleration;
        public readonly float TurnSpeedDegPerSec;
        public readonly float HitboxConeDot;
        public readonly bool EvictTokenOnStagger;

        public EnemyCombatProfile(
            float windupDurationSec,
            float trackingCutoffRatio,
            float activeStrikeDurationSec,
            float recoveryPunishSec,
            float maxPoise,
            float poiseRegenRate,
            float poiseRegenDelay,
            bool innateSuperArmor,
            bool windupSuperArmor,
            float knockbackResistance,
            float moveAcceleration,
            float turnSpeedDegPerSec,
            float hitboxConeDot,
            bool evictTokenOnStagger)
        {
            WindupDurationSec = windupDurationSec;
            TrackingCutoffRatio = trackingCutoffRatio;
            ActiveStrikeDurationSec = activeStrikeDurationSec;
            RecoveryPunishSec = recoveryPunishSec;
            MaxPoise = maxPoise;
            PoiseRegenRate = poiseRegenRate;
            PoiseRegenDelay = poiseRegenDelay;
            InnateSuperArmor = innateSuperArmor;
            WindupSuperArmor = windupSuperArmor;
            KnockbackResistance = knockbackResistance;
            MoveAcceleration = moveAcceleration;
            TurnSpeedDegPerSec = turnSpeedDegPerSec;
            HitboxConeDot = hitboxConeDot;
            EvictTokenOnStagger = evictTokenOnStagger;
        }

        private static readonly EnemyCombatProfile ProfileTuberMaw = new EnemyCombatProfile(
            windupDurationSec: 0.42f, trackingCutoffRatio: 0.55f, activeStrikeDurationSec: 0.08f, recoveryPunishSec: 0.28f,
            maxPoise: 35f, poiseRegenRate: 15f, poiseRegenDelay: 2.5f, innateSuperArmor: false, windupSuperArmor: false,
            knockbackResistance: 0.20f, moveAcceleration: 18f, turnSpeedDegPerSec: 360f, hitboxConeDot: 0.25f, evictTokenOnStagger: true);

        private static readonly EnemyCombatProfile ProfileTaroBrute = new EnemyCombatProfile(
            windupDurationSec: 0.62f, trackingCutoffRatio: 0.55f, activeStrikeDurationSec: 0.10f, recoveryPunishSec: 0.48f,
            maxPoise: 85f, poiseRegenRate: 18f, poiseRegenDelay: 3.0f, innateSuperArmor: true, windupSuperArmor: true,
            knockbackResistance: 0.75f, moveAcceleration: 10f, turnSpeedDegPerSec: 220f, hitboxConeDot: 0.20f, evictTokenOnStagger: true);

        private static readonly EnemyCombatProfile ProfileCornMusketeer = new EnemyCombatProfile(
            windupDurationSec: 0.58f, trackingCutoffRatio: 0.65f, activeStrikeDurationSec: 0.08f, recoveryPunishSec: 0.34f,
            maxPoise: 28f, poiseRegenRate: 12f, poiseRegenDelay: 2.5f, innateSuperArmor: false, windupSuperArmor: false,
            knockbackResistance: 0.15f, moveAcceleration: 16f, turnSpeedDegPerSec: 340f, hitboxConeDot: 0.25f, evictTokenOnStagger: true);

        private static readonly EnemyCombatProfile ProfileCyclopsTuberMaw = new EnemyCombatProfile(
            windupDurationSec: 0.56f, trackingCutoffRatio: 0.58f, activeStrikeDurationSec: 0.10f, recoveryPunishSec: 0.52f,
            maxPoise: 160f, poiseRegenRate: 25f, poiseRegenDelay: 3.2f, innateSuperArmor: true, windupSuperArmor: true,
            knockbackResistance: 0.45f, moveAcceleration: 14f, turnSpeedDegPerSec: 260f, hitboxConeDot: 0.18f, evictTokenOnStagger: true);

        private static readonly EnemyCombatProfile ProfileTaroColossus = new EnemyCombatProfile(
            windupDurationSec: 0.78f, trackingCutoffRatio: 0.52f, activeStrikeDurationSec: 0.12f, recoveryPunishSec: 0.68f,
            maxPoise: 240f, poiseRegenRate: 28f, poiseRegenDelay: 3.5f, innateSuperArmor: true, windupSuperArmor: true,
            knockbackResistance: 0.90f, moveAcceleration: 8f, turnSpeedDegPerSec: 180f, hitboxConeDot: 0.15f, evictTokenOnStagger: true);

        private static readonly EnemyCombatProfile ProfileTheRanger = new EnemyCombatProfile(
            windupDurationSec: 0.65f, trackingCutoffRatio: 0.65f, activeStrikeDurationSec: 0.10f, recoveryPunishSec: 0.55f,
            maxPoise: 180f, poiseRegenRate: 24f, poiseRegenDelay: 3.0f, innateSuperArmor: true, windupSuperArmor: true,
            knockbackResistance: 1.00f, moveAcceleration: 0f, turnSpeedDegPerSec: 200f, hitboxConeDot: 0.20f, evictTokenOnStagger: true);

        public static EnemyCombatProfile GetProfile(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.TuberMaw: return ProfileTuberMaw;
                case EnemyType.TaroBrute: return ProfileTaroBrute;
                case EnemyType.CornMusketeer: return ProfileCornMusketeer;
                case EnemyType.CyclopsTuberMaw: return ProfileCyclopsTuberMaw;
                case EnemyType.TaroColossus: return ProfileTaroColossus;
                case EnemyType.TheRanger: return ProfileTheRanger;
                default: return ProfileTuberMaw;
            }
        }

        /// <summary>
        /// Evaluates whether the attacker is still tracking the target (returns true)
        /// or has crossed the tracking cutoff and is locked to its committed strike vector.
        /// </summary>
        public static bool EvaluateWindupTracking(
            in EnemyCombatProfile profile,
            float elapsedSec,
            out bool isTrackingLocked,
            out float normalizedProgress)
        {
            float duration = profile.WindupDurationSec > 0.05f ? profile.WindupDurationSec : 0.05f;
            float ratio = elapsedSec / duration;
            if (ratio < 0f) ratio = 0f;
            else if (ratio > 1f) ratio = 1f;

            normalizedProgress = ratio;
            isTrackingLocked = normalizedProgress >= profile.TrackingCutoffRatio;
            return !isTrackingLocked;
        }
    }
}
