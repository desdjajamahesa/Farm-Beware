using System;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Categorizes combat impacts into 5 visceral tiers for hitstop, camera trauma, and audio-visual scaling.
    /// </summary>
    public enum AttackImpactTier
    {
        LightSlash = 0,
        LightHit1 = 0,
        ComboFinisher = 1,
        HeavyCleave = 2,
        PoiseCrushOrWallSplat = 3,
        ParryRiposte = 4,
        RiposteStrike = 4
    }

    /// <summary>
    /// Pure C# immutable struct defining hitstop freeze durations, camera trauma intensity,
    /// spark burst count, and audio pitch envelope for a specific combat impact tier.
    /// Zero-GC allocation.
    /// </summary>
    public readonly struct ImpactKinematicsProfile
    {
        public readonly float AttackerHitstopSec;
        public readonly float VictimHitstopSec;
        public readonly float CameraTrauma;
        public readonly int SparkBurstCount;
        public readonly float AudioPitchMin;
        public readonly float AudioPitchMax;

        public ImpactKinematicsProfile(
            float attackerHitstopSec,
            float victimHitstopSec,
            float cameraTrauma,
            int sparkBurstCount,
            float audioPitchMin,
            float audioPitchMax)
        {
            AttackerHitstopSec = attackerHitstopSec;
            VictimHitstopSec = victimHitstopSec;
            CameraTrauma = cameraTrauma;
            SparkBurstCount = sparkBurstCount;
            AudioPitchMin = audioPitchMin;
            AudioPitchMax = audioPitchMax;
        }

        /// <summary>
        /// Evaluates the deterministic kinesthetic feedback profile for the given impact tier and critical state.
        /// </summary>
        public static ImpactKinematicsProfile Evaluate(AttackImpactTier tier, bool isCrit)
        {
            float critMult = isCrit ? 1.25f : 1.0f;

            switch (tier)
            {
                case AttackImpactTier.ParryRiposte:
                    return new ImpactKinematicsProfile(
                        attackerHitstopSec: 0.095f,
                        victimHitstopSec: 0.220f,
                        cameraTrauma: 0.60f,
                        sparkBurstCount: 42,
                        audioPitchMin: 0.85f,
                        audioPitchMax: 0.92f);

                case AttackImpactTier.PoiseCrushOrWallSplat:
                    return new ImpactKinematicsProfile(
                        attackerHitstopSec: 0.085f,
                        victimHitstopSec: 0.180f,
                        cameraTrauma: 0.52f,
                        sparkBurstCount: 35,
                        audioPitchMin: 0.80f,
                        audioPitchMax: 0.88f);

                case AttackImpactTier.HeavyCleave:
                    return new ImpactKinematicsProfile(
                        attackerHitstopSec: 0.075f * critMult,
                        victimHitstopSec: 0.140f * critMult,
                        cameraTrauma: Math.Min(0.65f, 0.42f * critMult),
                        sparkBurstCount: isCrit ? 34 : 28,
                        audioPitchMin: 0.82f,
                        audioPitchMax: 0.88f);

                case AttackImpactTier.ComboFinisher:
                    return new ImpactKinematicsProfile(
                        attackerHitstopSec: 0.055f * critMult,
                        victimHitstopSec: 0.105f * critMult,
                        cameraTrauma: Math.Min(0.55f, 0.30f * critMult),
                        sparkBurstCount: isCrit ? 26 : 20,
                        audioPitchMin: 0.90f,
                        audioPitchMax: 0.96f);

                case AttackImpactTier.LightSlash:
                default:
                    return new ImpactKinematicsProfile(
                        attackerHitstopSec: 0.035f * critMult,
                        victimHitstopSec: 0.065f * critMult,
                        cameraTrauma: Math.Min(0.45f, 0.18f * critMult),
                        sparkBurstCount: isCrit ? 22 : 12,
                        audioPitchMin: 0.96f,
                        audioPitchMax: 1.06f);
            }
        }

        /// <summary>
        /// Calculates deterministic wall-splat collision damage when a launched enemy slams into a solid boundary.
        /// </summary>
        public static int EvaluateWallSplatDamage(float impactSpeed, float minHazardSpeed = 4.5f, int baseWallDamage = 22)
        {
            if (impactSpeed < minHazardSpeed || baseWallDamage <= 0)
            {
                return 0;
            }

            float excessSpeedRatio = Math.Clamp((impactSpeed - minHazardSpeed) / 8.0f, 0f, 0.75f);
            return (int)MathF.Round(baseWallDamage * (1.0f + excessSpeedRatio));
        }
    }
}
