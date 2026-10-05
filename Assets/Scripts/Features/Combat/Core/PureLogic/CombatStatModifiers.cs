namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Pure C# immutable struct representing aggregated combat multipliers
    /// calculated from gastronomy food buffs and workbench progression paths.
    /// </summary>
    public readonly struct CombatStatModifiers
    {
        public readonly float AttackSpeedMultiplier;
        public readonly float DamageMultiplier;
        public readonly float FinisherDamageMultiplier;
        public readonly float PoiseDamageMultiplier;
        public readonly float KnockbackMultiplier;
        public readonly float ExtraParryWindow;
        public readonly float ExtraDodgeDuration;
        public readonly float DamageMitigationRatio;
        public readonly bool HasPassiveSuperArmor;

        public CombatStatModifiers(
            float attackSpeedMultiplier,
            float damageMultiplier,
            float finisherDamageMultiplier,
            float poiseDamageMultiplier,
            float knockbackMultiplier,
            float extraParryWindow,
            float extraDodgeDuration,
            float damageMitigationRatio,
            bool hasPassiveSuperArmor)
        {
            AttackSpeedMultiplier = attackSpeedMultiplier;
            DamageMultiplier = damageMultiplier;
            FinisherDamageMultiplier = finisherDamageMultiplier;
            PoiseDamageMultiplier = poiseDamageMultiplier;
            KnockbackMultiplier = knockbackMultiplier;
            ExtraParryWindow = extraParryWindow;
            ExtraDodgeDuration = extraDodgeDuration;
            DamageMitigationRatio = damageMitigationRatio;
            HasPassiveSuperArmor = hasPassiveSuperArmor;
        }

        public static CombatStatModifiers Default => new CombatStatModifiers(
            attackSpeedMultiplier: 1.0f,
            damageMultiplier: 1.0f,
            finisherDamageMultiplier: 1.0f,
            poiseDamageMultiplier: 1.0f,
            knockbackMultiplier: 1.0f,
            extraParryWindow: 0f,
            extraDodgeDuration: 0f,
            damageMitigationRatio: 0f,
            hasPassiveSuperArmor: false
        );

        /// <summary>
        /// Combines base modifiers with active gastronomy food buffs and workbench forging path bonuses.
        /// </summary>
        public static CombatStatModifiers Evaluate(
            bool hasAgilitySurge,
            bool hasIronRootStance,
            bool hasBerserkerHarvest,
            bool hasReflectiveShell,
            bool isSweetPotatoPath,
            bool isTaroPath)
        {
            float atkSpeed = 1.0f;
            float dmg = 1.0f;
            float finisherDmg = 1.0f;
            float poiseDmg = 1.0f;
            float kb = 1.0f;
            float parryBonus = 0f;
            float dodgeBonus = 0f;
            float mitigation = 0f;
            bool superArmor = false;

            // --- Gastronomy Food Buffs ---
            if (hasAgilitySurge)
            {
                atkSpeed += 0.20f;
                dodgeBonus += 0.07f; // +4 i-Frames
            }

            if (hasIronRootStance)
            {
                superArmor = true;
                mitigation += 0.25f; // 25% damage reduction
            }

            if (hasBerserkerHarvest)
            {
                finisherDmg += 0.40f; // +40% Finisher Smite damage
            }

            if (hasReflectiveShell)
            {
                parryBonus += 0.06f; // +60ms parry window
            }

            // --- Workbench Weapon Paths ---
            if (isSweetPotatoPath)
            {
                atkSpeed += 0.15f;
                dmg += 0.10f;
            }
            else if (isTaroPath)
            {
                poiseDmg += 0.35f;
                kb += 0.30f;
                mitigation += 0.10f;
            }

            return new CombatStatModifiers(
                atkSpeed,
                dmg,
                finisherDmg,
                poiseDmg,
                kb,
                parryBonus,
                dodgeBonus,
                mitigation,
                superArmor
            );
        }
    }
}
