namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Discrete input commands dispatched from player controllers or input adapters.
    /// </summary>
    public enum CombatCommand
    {
        LightAttack,
        HeavyAttack,
        DashSweep,
        FrontKick,
        ChargedThrust
    }

    /// <summary>
    /// Discrete combat action identifiers covering standard combo stages, heavy finishers, and special attacks.
    /// </summary>
    public enum CombatActionID
    {
        Idle,
        Light1,
        Light2,
        Light3,
        HeavyFinisher1, // L -> H (Wide Cleave)
        HeavyFinisher2, // L -> L -> H (Overhead Smite)
        ChargedThrust,  // Charged heavy attack
        DashSweep,      // Sprint running attack
        FrontKick,      // Melee disruptor kick
        Light4,         // L -> L -> L -> L finisher
        HeavyChain2,    // H -> H
        HeavyChain3,    // H -> H -> H
        HeavyChain4     // H -> H -> H -> H / L -> L -> L -> H
    }

    /// <summary>
    /// Pure C# immutable struct defining timing thresholds, damage/poise multipliers, and physics forces for an attack.
    /// </summary>
    public readonly struct AttackDefinition
    {
        public readonly CombatActionID ActionID;
        public readonly float MinCancelNormalizedTime;
        public readonly float DamageMultiplier;
        public readonly float PoiseDamage;
        public readonly float KnockbackForce;

        public AttackDefinition(
            CombatActionID actionId,
            float minCancelNormalizedTime,
            float damageMultiplier,
            float poiseDamage,
            float knockbackForce)
        {
            ActionID = actionId;
            MinCancelNormalizedTime = minCancelNormalizedTime;
            DamageMultiplier = damageMultiplier;
            PoiseDamage = poiseDamage;
            KnockbackForce = knockbackForce;
        }

        public static AttackDefinition DefaultIdle => new AttackDefinition(CombatActionID.Idle, 0f, 1f, 0f, 0f);
        public static AttackDefinition DefaultLight1 => new AttackDefinition(CombatActionID.Light1, 0.50f, 1.0f, 15f, 1.0f);
        public static AttackDefinition DefaultLight2 => new AttackDefinition(CombatActionID.Light2, 0.50f, 1.2f, 20f, 1.2f);
        public static AttackDefinition DefaultLight3 => new AttackDefinition(CombatActionID.Light3, 0.55f, 1.6f, 35f, 1.8f);
        public static AttackDefinition DefaultHeavyFinisher1 => new AttackDefinition(CombatActionID.HeavyFinisher1, 0.55f, 1.9f, 40f, 2.2f);
        public static AttackDefinition DefaultHeavyFinisher2 => new AttackDefinition(CombatActionID.HeavyFinisher2, 0.60f, 2.4f, 55f, 2.8f);
        public static AttackDefinition DefaultChargedThrust => new AttackDefinition(CombatActionID.ChargedThrust, 0.50f, 2.5f, 50f, 2.5f);
        public static AttackDefinition DefaultDashSweep => new AttackDefinition(CombatActionID.DashSweep, 0.40f, 1.4f, 25f, 1.5f);
        public static AttackDefinition DefaultFrontKick => new AttackDefinition(CombatActionID.FrontKick, 0.45f, 0.8f, 45f, 3.0f);
        public static AttackDefinition DefaultLight4 => new AttackDefinition(CombatActionID.Light4, 0.65f, 1.85f, 45f, 2.1f);
        public static AttackDefinition DefaultHeavyChain2 => new AttackDefinition(CombatActionID.HeavyChain2, 0.60f, 2.2f, 50f, 2.4f);
        public static AttackDefinition DefaultHeavyChain3 => new AttackDefinition(CombatActionID.HeavyChain3, 0.65f, 2.5f, 60f, 2.7f);
        public static AttackDefinition DefaultHeavyChain4 => new AttackDefinition(CombatActionID.HeavyChain4, 0.70f, 3.0f, 75f, 3.2f);
    }
}
