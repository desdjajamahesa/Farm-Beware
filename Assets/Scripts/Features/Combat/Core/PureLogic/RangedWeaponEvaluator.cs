using System;

namespace FeaturesCombat.Core.PureLogic
{
    /// <summary>
    /// Immutable zero-GC descriptor for a ranged weapon projectile volley.
    /// </summary>
    public readonly struct RangedShotProfile
    {
        public readonly int DamagePerProjectile;
        public readonly float ProjectileSpeed;
        public readonly int ProjectileCount;
        public readonly float SpreadAngleDeg;
        public readonly int MaxPierceCount;
        public readonly float CritChance;
        public readonly bool HasCritExplosion;
        public readonly float ExplosionRadius;
        public readonly float KnockbackForce;

        public RangedShotProfile(
            int damagePerProjectile,
            float projectileSpeed,
            int projectileCount,
            float spreadAngleDeg,
            int maxPierceCount,
            float critChance,
            bool hasCritExplosion,
            float explosionRadius,
            float knockbackForce)
        {
            DamagePerProjectile = damagePerProjectile;
            ProjectileSpeed = projectileSpeed;
            ProjectileCount = projectileCount;
            SpreadAngleDeg = spreadAngleDeg;
            MaxPierceCount = maxPierceCount;
            CritChance = critChance;
            HasCritExplosion = hasCritExplosion;
            ExplosionRadius = explosionRadius;
            KnockbackForce = knockbackForce;
        }
    }

    /// <summary>
    /// Pure C# POCO evaluator governing ranged weapon projectile scaling, burst counts,
    /// and Corn Specialization path perks (Golden Kernel, Sharp Kernel, Piercing Shot, Rapid Harvest, Kernel Explosion, Golden Storm).
    /// Zero-GC allocation and zero assembly coupling.
    /// </summary>
    public static class RangedWeaponEvaluator
    {
        private static readonly float[] WeaponDamageBonusTable = new float[]
        {
            0f,    // Unused 0-index
            0.00f, // Lv 1
            0.10f, // Lv 2
            0.22f, // Lv 3
            0.35f, // Lv 4
            0.50f, // Lv 5
            0.68f, // Lv 6
            0.88f, // Lv 7
            1.10f, // Lv 8
            1.30f, // Lv 9
            1.50f  // Lv 10
        };

        public static RangedShotProfile EvaluateShot(
            int weaponBaseDamage,
            int weaponLevel,
            float playerDamageMultiplier,
            float baseProjectileSpeed,
            float baseKnockback,
            bool isCornSpecActive,
            int specializationNodeLevel,
            int comboStep = 0,
            bool isHeavyVolley = false,
            bool isSkillBarrage = false)
        {
            return EvaluateShot(
                weaponBaseDamage,
                baseProjectileSpeed,
                baseKnockback,
                weaponLevel,
                isCornSpecActive,
                specializationNodeLevel,
                comboStep,
                isHeavyVolley,
                isSkillBarrage,
                playerDamageMultiplier);
        }

        public static RangedShotProfile EvaluateShot(
            int weaponBaseDamage,
            float baseProjectileSpeed,
            float baseKnockback,
            int weaponLevel,
            bool isCornSpecActive,
            int cornSpecNodeLevel,
            int comboIndex = 0,
            bool isHeavyVolley = false,
            bool isSkillBarrage = false,
            float playerDamageMultiplier = 1.0f)
        {
            int clampedLv = Math.Clamp(weaponLevel, 1, 10);
            int scaledBase = (int)MathF.Round(Math.Max(1, weaponBaseDamage) * (1.0f + WeaponDamageBonusTable[clampedLv]));
            float dmgMult = Math.Max(0.1f, playerDamageMultiplier);
            float speed = baseProjectileSpeed > 1f ? baseProjectileSpeed : 20f;
            float knockback = baseKnockback > 0f ? baseKnockback : 5f;

            int projCount = 1;
            float spreadAngle = 0f;

            if (isSkillBarrage)
            {
                projCount = 5;
                spreadAngle = 28f;
                dmgMult *= 1.15f;
                knockback *= 1.35f;
            }
            else if (isHeavyVolley)
            {
                projCount = 3;
                spreadAngle = 16f;
                dmgMult *= 1.35f;
                knockback *= 1.50f;
            }
            else if (comboIndex == 2)
            {
                // 3rd shot in light combo fires a rapid twin-kernel burst
                projCount = 2;
                spreadAngle = 8f;
                dmgMult *= 1.10f;
            }

            int pierceCount = 0;
            float critChance = 0.10f;
            bool hasExplosion = false;
            float explosionRadius = 0f;

            // Apply Corn Specialization Path Node Bonuses (Nodes 1 to 6)
            if (isCornSpecActive && cornSpecNodeLevel > 0)
            {
                // Lv.1 - Golden Kernel: +10% Projectile Damage
                if (cornSpecNodeLevel >= 1)
                {
                    dmgMult *= 1.10f;
                }

                // Lv.2 - Sharp Kernel: +5% Critical Chance
                if (cornSpecNodeLevel >= 2)
                {
                    critChance += 0.05f;
                }

                // Lv.3 - Piercing Shot: Projectiles pass through 1 additional enemy
                if (cornSpecNodeLevel >= 3)
                {
                    pierceCount = 1;
                }

                // Lv.4 - Rapid Harvest: +15% Projectile Speed
                if (cornSpecNodeLevel >= 4)
                {
                    speed *= 1.15f;
                }

                // Lv.5 - Kernel Explosion: Critical hits create splash damage explosion
                if (cornSpecNodeLevel >= 5)
                {
                    hasExplosion = true;
                    explosionRadius = 2.5f;
                }

                // Lv.6 - Ultimate: Golden Storm: 150% projectile damage, pierces 3 enemies, guaranteed larger explosion
                if (cornSpecNodeLevel >= 6)
                {
                    dmgMult *= 1.50f;
                    pierceCount = 3;
                    hasExplosion = true;
                    explosionRadius = 3.2f;
                    critChance += 0.15f;
                }
            }

            int finalDamage = Math.Max(1, (int)MathF.Round(scaledBase * dmgMult));

            return new RangedShotProfile(
                damagePerProjectile: finalDamage,
                projectileSpeed: speed,
                projectileCount: projCount,
                spreadAngleDeg: spreadAngle,
                maxPierceCount: pierceCount,
                critChance: Math.Clamp(critChance, 0f, 1f),
                hasCritExplosion: hasExplosion,
                explosionRadius: explosionRadius,
                knockbackForce: knockback);
        }
    }
}
