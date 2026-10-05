namespace FeaturesCombat.Melee
{
    /// <summary>
    /// Represents the discrete states of the player melee combat Finite State Machine.
    /// Supports 3-hit light combos, charging and release of heavy attacks, dash lunges,
    /// dynamic heavy finishers (L -> H, L -> L -> H), and melee kicks.
    /// </summary>
    public enum MeleeCombatState
    {
        Idle,
        Light1,
        Light2,
        Light3_Finisher,
        HeavyCharging,
        HeavyRelease,
        DashAttack,
        HeavyFinisher1, // L -> H
        HeavyFinisher2, // L -> L -> H
        FrontKick
    }
}
