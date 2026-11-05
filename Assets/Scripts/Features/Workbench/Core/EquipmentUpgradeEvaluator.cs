using System;

namespace FeaturesWorkbench.Core
{
    public enum SpecializationPath
    {
        None = 0,
        SweetPotato = 1,
        Taro = 2,
        Corn = 3
    }

    public readonly struct SpecializationNodeInfo
    {
        public readonly int Level;
        public readonly string Title;
        public readonly string Description;
        public readonly string EffectSummary;
        public readonly int GoldCost;
        public readonly string RequiredMaterialId;
        public readonly int RequiredMaterialCount;
        public readonly int RequiredBossKills;
        public readonly bool IsUltimate;

        public SpecializationNodeInfo(
            int level,
            string title,
            string description,
            string effectSummary,
            int goldCost,
            string requiredMaterialId,
            int requiredMaterialCount,
            int requiredBossKills,
            bool isUltimate)
        {
            Level = level;
            Title = title;
            Description = description;
            EffectSummary = effectSummary;
            GoldCost = goldCost;
            RequiredMaterialId = requiredMaterialId;
            RequiredMaterialCount = requiredMaterialCount;
            RequiredBossKills = requiredBossKills;
            IsUltimate = isUltimate;
        }
    }

    public readonly struct SetBonusState
    {
        public readonly int EquippedPieceCount;
        public readonly float AttackSpeedBonus;
        public readonly float DodgeDamageBonus;
        public readonly bool HasShadowDashReset;

        public SetBonusState(int count, float atkSpd, float dodgeDmg, bool shadowDashReset)
        {
            EquippedPieceCount = count;
            AttackSpeedBonus = atkSpd;
            DodgeDamageBonus = dodgeDmg;
            HasShadowDashReset = shadowDashReset;
        }
    }

    /// <summary>
    /// Pure C# POCO class governing equipment upgrade formulas, specialization progression trees,
    /// costs, and set bonus calculations with zero heap allocation in loops (Zero-GC).
    /// </summary>
    public static class EquipmentUpgradeEvaluator
    {
        public const int MaxWeaponLevel = 10;
        public const int MaxArmorLevel = 10;
        public const int MaxSpecializationNodeLevel = 6; // 1 to 5 + 6 (Ultimate)

        // Material IDs matching ItemDatabase / Monster drops
        public const string MatCyclopsEye = "mat_cyclops_eye";
        public const string MatColossusCore = "mat_colossus_core";
        public const string MatCobCore = "mat_cob_core";
        public const string MatMutatedRoot = "mat_mutated_root";

        // Weapon Damage Progression Bonus: Base(0%), +15%, +30%, +45%, +60%, +75%, +90%, +105%, +120%, +150%
        private static readonly float[] WeaponDamageMultipliers =
        {
            1.00f, 1.15f, 1.30f, 1.45f, 1.60f, 1.75f, 1.90f, 2.05f, 2.20f, 2.50f
        };

        // Weapon Upgrade Gold Costs (Lv 1->2 up to Lv 9->10)
        private static readonly int[] WeaponUpgradeCosts =
        {
            300, 600, 1200, 2200, 4000, 7000, 11000, 16000, 22000
        };

        // Armor Mitigation Progression: Lv 1(0%), Lv 2(0%), Lv 3(2%), Lv 4(4%), Lv 5(6%), Lv 6(8%), Lv 7(12%), Lv 8(17%), Lv 9(22%), Lv 10(32%)
        private static readonly float[] ArmorMitigationRatios =
        {
            0.00f, 0.00f, 0.02f, 0.04f, 0.06f, 0.08f, 0.12f, 0.17f, 0.22f, 0.32f
        };

        // Armor Upgrade Gold Costs from GDD V0.1
        private static readonly int[] ArmorUpgradeCosts =
        {
            300, 500, 800, 1200, 1800, 12500, 23500, 35000, 47000
        };

        public static int CalculateWeaponDamage(int baseDamage, int level)
        {
            int clampedLevel = Math.Clamp(level, 1, MaxWeaponLevel);
            float multiplier = WeaponDamageMultipliers[clampedLevel - 1];
            return (int)MathF.Round(baseDamage * multiplier);
        }

        public static float GetWeaponDamageBonusPercent(int level)
        {
            int clampedLevel = Math.Clamp(level, 1, MaxWeaponLevel);
            return (WeaponDamageMultipliers[clampedLevel - 1] - 1.0f) * 100f;
        }

        public static int GetWeaponUpgradeCost(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= MaxWeaponLevel) return 0;
            return WeaponUpgradeCosts[currentLevel - 1];
        }

        public static float GetArmorMitigationRatio(int level)
        {
            int clampedLevel = Math.Clamp(level, 1, MaxArmorLevel);
            return ArmorMitigationRatios[clampedLevel - 1];
        }

        public static int GetArmorUpgradeCost(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= MaxArmorLevel) return 0;
            return ArmorUpgradeCosts[currentLevel - 1];
        }

        public static SpecializationNodeInfo GetNodeInfo(SpecializationPath path, int nodeLevel)
        {
            int level = Math.Clamp(nodeLevel, 1, MaxSpecializationNodeLevel);
            return path switch
            {
                SpecializationPath.SweetPotato => GetSweetPotatoNode(level),
                SpecializationPath.Taro => GetTaroNode(level),
                SpecializationPath.Corn => GetCornNode(level),
                _ => new SpecializationNodeInfo(0, "None", "", "", 0, null, 0, 0, false)
            };
        }

        private static SpecializationNodeInfo GetSweetPotatoNode(int level) => level switch
        {
            1 => new SpecializationNodeInfo(1, "Lv.1 – Quick Roots", "Faster weapon attacks", "+10% Attack Speed", 10000, null, 0, 0, false),
            2 => new SpecializationNodeInfo(2, "Lv.2 – Swift Movement", "Move faster during combat", "+10% Movement Speed", 20000, MatCyclopsEye, 1, 0, false),
            3 => new SpecializationNodeInfo(3, "Lv.3 – Dash Strike", "Deal damage when dashing through enemies", "Dash Attack Damage", 40000, null, 0, 0, false),
            4 => new SpecializationNodeInfo(4, "Lv.4 – Root Frenzy", "Consecutive attacks increase attack speed", "+15% Attack Speed", 45000, MatCyclopsEye, 2, 0, false),
            5 => new SpecializationNodeInfo(5, "Lv.5 – Burrow Instinct", "Successful dodges grant temporary damage reduction", "Evasion Damage Reduction", 60000, null, 0, 0, false),
            6 => new SpecializationNodeInfo(6, "Ultimate: Underground Predator", "Dash attacks deal 150% dmg; dodges grant +20% Atk Spd for 3s", "150% Dash Dmg + Dodge Boost", 100000, MatCyclopsEye, 3, 5, true),
            _ => default
        };

        private static SpecializationNodeInfo GetTaroNode(int level) => level switch
        {
            1 => new SpecializationNodeInfo(1, "Lv.1 – Heavy Roots", "Increase heavy attack damage", "+10% Heavy Attack Damage", 10000, null, 0, 0, false),
            2 => new SpecializationNodeInfo(2, "Lv.2 – Thick Skin", "Reduce incoming damage", "+10% Defense", 20000, MatColossusCore, 1, 0, false),
            3 => new SpecializationNodeInfo(3, "Lv.3 – Ground Breaker", "Heavy attacks push enemies backward", "Heavy Knockback", 40000, null, 0, 0, false),
            4 => new SpecializationNodeInfo(4, "Lv.4 – Iron Taro", "Increase maximum health", "+15% Max HP", 45000, MatColossusCore, 2, 0, false),
            5 => new SpecializationNodeInfo(5, "Lv.5 – Unbreakable Root", "Taking damage briefly reduces incoming damage", "Adaptive Damage Resistance", 60000, null, 0, 0, false),
            6 => new SpecializationNodeInfo(6, "Ultimate: Ancient Guardian", "Heavy attacks deal 150% dmg + shockwave; 20% less dmg for 3s", "150% Heavy Dmg + Shockwave + Armor", 100000, MatColossusCore, 3, 5, true),
            _ => default
        };

        private static SpecializationNodeInfo GetCornNode(int level) => level switch
        {
            1 => new SpecializationNodeInfo(1, "Lv.1 – Golden Kernel", "Increase ranged and projectile damage", "+10% Projectile Damage", 10000, null, 0, 0, false),
            2 => new SpecializationNodeInfo(2, "Lv.2 – Sharp Kernel", "Improve critical hit probability", "+5% Critical Chance", 20000, MatCobCore, 1, 0, false),
            3 => new SpecializationNodeInfo(3, "Lv.3 – Piercing Shot", "Projectiles pass through one additional enemy", "Piercing Projectiles", 40000, null, 0, 0, false),
            4 => new SpecializationNodeInfo(4, "Lv.4 – Rapid Harvest", "Faster projectile travel velocity", "+15% Projectile Speed", 45000, MatCobCore, 2, 0, false),
            5 => new SpecializationNodeInfo(5, "Lv.5 – Kernel Explosion", "Critical hits create small splash damage explosion", "Critical Area Explosion", 60000, null, 0, 0, false),
            6 => new SpecializationNodeInfo(6, "Ultimate: Golden Storm", "Projectiles deal 150% dmg; crit triggers 50% explosion; pierces 3 enemies", "150% Projectile Dmg + Piercing 3", 100000, MatCobCore, 3, 5, true),
            _ => default
        };

        public static SetBonusState EvaluateSweetPotatoSetBonus(int equippedPieces)
        {
            float atkSpd = 0f;
            float dodgeDmg = 0f;
            bool shadowDash = false;

            if (equippedPieces >= 2)
            {
                atkSpd += 0.10f; // 2 pieces: +10% Attack Speed
            }

            if (equippedPieces >= 4)
            {
                dodgeDmg += 0.15f; // 4 pieces: +15% damage for 3s after dodge
            }

            if (equippedPieces >= 5) // In GDD, 5 or 6 pieces triggers set capstone
            {
                shadowDash = true;
            }

            return new SetBonusState(equippedPieces, atkSpd, dodgeDmg, shadowDash);
        }
    }
}

