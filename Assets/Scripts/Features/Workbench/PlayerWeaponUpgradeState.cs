using System;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesWorkbench.Core;

namespace FeaturesWorkbench
{
    /// <summary>
    /// Manages player weapon upgrade state (Basic Level 1-10) and the single active seed specialization path (Level 1-5 + Ultimate).
    /// Fully decoupled with pure logic delegated to EquipmentUpgradeEvaluator.
    /// Preserves complete backwards-compatibility for legacy callers and tests.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerWeaponUpgradeState : MonoBehaviour
    {
        public static PlayerWeaponUpgradeState Instance { get; private set; }

        public event Action OnUpgradesChanged;

        [Header("Base Weapon Level")]
        [Range(1, EquipmentUpgradeEvaluator.MaxWeaponLevel)] public int weaponLevel = 1;
        public int baseDamage = 25;
        public float baseKnockback = 7f;

        [Header("Single Active Specialization")]
        [Tooltip("Active seed specialization path. Only 1 path can be active at a time.")]
        public SpecializationPath activeSpecialization = SpecializationPath.None;

        [Range(0, EquipmentUpgradeEvaluator.MaxSpecializationNodeLevel)]
        public int specializationNodeLevel = 0;

        [Header("Boss Encounter Record")]
        [Min(0)] public int totalBossesDefeated = 0;

        // Legacy compatibility properties
        public bool sweetPotatoPathUnlocked
        {
            get => activeSpecialization == SpecializationPath.SweetPotato && specializationNodeLevel > 0;
            set
            {
                if (value)
                {
                    activeSpecialization = SpecializationPath.SweetPotato;
                    specializationNodeLevel = Math.Max(1, specializationNodeLevel);
                    RecalculateStats();
                }
            }
        }

        public bool taroPathUnlocked
        {
            get => activeSpecialization == SpecializationPath.Taro && specializationNodeLevel > 0;
            set
            {
                if (value)
                {
                    activeSpecialization = SpecializationPath.Taro;
                    specializationNodeLevel = Math.Max(1, specializationNodeLevel);
                    RecalculateStats();
                }
            }
        }

        public bool cornPathUnlocked
        {
            get => activeSpecialization == SpecializationPath.Corn && specializationNodeLevel > 0;
            set
            {
                if (value)
                {
                    activeSpecialization = SpecializationPath.Corn;
                    specializationNodeLevel = Math.Max(1, specializationNodeLevel);
                    RecalculateStats();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            RecalculateStats();
        }

        private void OnEnable()
        {
            FeaturesCombat.EnemyBase.OnAnyEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            FeaturesCombat.EnemyBase.OnAnyEnemyDied -= HandleEnemyDied;
        }

        private void HandleEnemyDied(FeaturesCombat.EnemyBase enemy)
        {
            if (enemy != null && enemy.isBoss)
            {
                totalBossesDefeated++;
            }
        }

        public void RecalculateStats()
        {
            // Level 1 = 25 Base Damage, scales up to Lv 10 (+150% = 63 damage)
            baseDamage = EquipmentUpgradeEvaluator.CalculateWeaponDamage(25, weaponLevel);

            // Knockback scales from 7.0f to 14.0f
            baseKnockback = 7f + ((weaponLevel - 1) * 0.75f);

            if (taroPathUnlocked)
            {
                baseKnockback *= 1.5f; // +50% knockback
            }

            if (cornPathUnlocked)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * 1.25f); // +25% damage
            }
        }

        public int GetNextUpgradeCost()
        {
            return EquipmentUpgradeEvaluator.GetWeaponUpgradeCost(weaponLevel);
        }

        /// <summary>
        /// Checks whether the player possesses the Dummy Sword / Root Cleaver in inventory or hotbar.
        /// </summary>
        public bool HasDummySword(InventoryComponent inv = null)
        {
            if (inv == null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null)
                {
                    inv = player.GetComponent<InventoryComponent>();
                }
            }

            // Also check equipped weapon slot in PlayerArmorUpgradeState if present
            if (PlayerArmorUpgradeState.Instance != null)
            {
                var equipped = PlayerArmorUpgradeState.Instance.GetEquippedItem(EquipmentSlotType.MainWeapon);
                if (equipped != null &&
                    ((equipped is ToolItemData eqTool && eqTool.isWeapon) ||
                     string.Equals(equipped.itemId, "dummysword", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(equipped.itemId, "tool_weapon_root_cleaver", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(equipped.itemId, "tool_weapon_corn_repeater", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(equipped.itemName, "Dummy Sword", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(equipped.itemName, "Root Cleaver", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(equipped.itemName, "Homestead Slingshot", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(equipped.itemName, "Corn Cob Repeater", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            if (inv == null || inv.slots == null) return false;

            foreach (var slot in inv.slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item != null)
                {
                    if ((slot.item is ToolItemData slotTool && slotTool.isWeapon) ||
                        string.Equals(slot.item.itemId, "dummysword", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(slot.item.itemId, "tool_weapon_root_cleaver", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(slot.item.itemId, "tool_weapon_corn_repeater", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(slot.item.itemName, "Dummy Sword", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(slot.item.itemName, "Root Cleaver", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(slot.item.itemName, "Homestead Slingshot", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(slot.item.itemName, "Corn Cob Repeater", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool UpgradeBaseLevel(InventoryComponent inv = null)
        {
            if (weaponLevel >= EquipmentUpgradeEvaluator.MaxWeaponLevel) return false;

            if (!HasDummySword(inv))
            {
                Debug.LogWarning("[PlayerWeaponUpgradeState] Failed upgrade: Player does not possess weapon!");
                return false;
            }

            int cost = GetNextUpgradeCost();
            var wallet = ServiceLocator.Resolve<IWalletService>();
            if (wallet != null && wallet.SpendGold(cost))
            {
                weaponLevel++;
                RecalculateStats();

                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                if (fct != null)
                {
                    fct.SpawnText(
                        transform.position + Vector3.up * 1.5f,
                        $"⚔️ Weapon Upgraded to Lv.{weaponLevel}!",
                        new Color(0.2f, 0.95f, 0.4f));
                }

                OnUpgradesChanged?.Invoke();
                FarmBeware.Core.Runtime.NarrativeTelemetry.ReportWeaponUpgraded(weaponLevel, sweetPotatoPathUnlocked, taroPathUnlocked, cornPathUnlocked);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Selects the single active specialization path. Enforces the 1-path exclusivity rule.
        /// </summary>
        public bool SelectSpecialization(SpecializationPath path)
        {
            if (path == SpecializationPath.None) return false;

            if (activeSpecialization != SpecializationPath.None && activeSpecialization != path)
            {
                Debug.LogWarning($"[PlayerWeaponUpgradeState] Cannot switch path to {path}: Player already specialized in {activeSpecialization}!");
                return false;
            }

            activeSpecialization = path;
            if (specializationNodeLevel == 0)
            {
                specializationNodeLevel = 0; // ready to unlock node 1
            }

            RecalculateStats();
            OnUpgradesChanged?.Invoke();
            return true;
        }

        public bool UpgradeActiveSpecialization(InventoryComponent inv = null)
        {
            if (activeSpecialization == SpecializationPath.None) return false;
            if (specializationNodeLevel >= EquipmentUpgradeEvaluator.MaxSpecializationNodeLevel) return false;

            if (!HasDummySword(inv))
            {
                Debug.LogWarning("[PlayerWeaponUpgradeState] Cannot upgrade specialization doctrine: Player does not possess weapon!");
                return false;
            }

            int nextNodeLevel = specializationNodeLevel + 1;
            var nodeInfo = EquipmentUpgradeEvaluator.GetNodeInfo(activeSpecialization, nextNodeLevel);

            if (nodeInfo.IsUltimate && totalBossesDefeated < nodeInfo.RequiredBossKills)
            {
                Debug.LogWarning($"[PlayerWeaponUpgradeState] Ultimate requires {nodeInfo.RequiredBossKills} boss kills! Current: {totalBossesDefeated}");
                return false;
            }

            var wallet = ServiceLocator.Resolve<IWalletService>();
            if (wallet == null || !wallet.CanAfford(nodeInfo.GoldCost))
            {
                Debug.LogWarning("[PlayerWeaponUpgradeState] Insufficient gold for specialization node!");
                return false;
            }

            if (inv == null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null) inv = player.GetComponent<InventoryComponent>();
            }

            ItemData requiredItem = null;
            if (!string.IsNullOrEmpty(nodeInfo.RequiredMaterialId) && nodeInfo.RequiredMaterialCount > 0)
            {
                requiredItem = ItemDatabase.Instance?.GetItem(nodeInfo.RequiredMaterialId);
                if (requiredItem == null || inv == null || inv.CountItem(requiredItem) < nodeInfo.RequiredMaterialCount)
                {
                    Debug.LogWarning($"[PlayerWeaponUpgradeState] Insufficient materials ({nodeInfo.RequiredMaterialId} x{nodeInfo.RequiredMaterialCount})!");
                    return false;
                }
            }

            // Deduct costs
            wallet.SpendGold(nodeInfo.GoldCost);
            if (requiredItem != null && inv != null)
            {
                inv.RemoveItem(requiredItem, nodeInfo.RequiredMaterialCount);
            }

            specializationNodeLevel = nextNodeLevel;
            RecalculateStats();

            // Corn Doctrine Node 1 rewards the Homestead Slingshot
            if (activeSpecialization == SpecializationPath.Corn && specializationNodeLevel == 1)
            {
                var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
                var slingshot = db?.GetItem("tool_weapon_corn_repeater") 
                    ?? Resources.Load<ItemData>("Items/Equipment/Tool_CornRepeater");
                if (slingshot != null && inv != null)
                {
                    inv.AddItem(slingshot, 1);
                    Debug.Log("[PlayerWeaponUpgradeState] 🏹 Awarded Homestead Slingshot for unlocking Corn Doctrine Node 1!");
                }
            }

            var fct = ServiceLocator.Resolve<IFloatingTextService>();
            if (fct != null)
            {
                fct.SpawnText(
                    transform.position + Vector3.up * 1.5f,
                    $"✨ {nodeInfo.Title} Unlocked!",
                    new Color(0.95f, 0.85f, 0.25f));
            }

            OnUpgradesChanged?.Invoke();
            FarmBeware.Core.Runtime.NarrativeTelemetry.ReportWeaponUpgraded(weaponLevel, sweetPotatoPathUnlocked, taroPathUnlocked, cornPathUnlocked);
            return true;
        }

        public void IncrementBossKill()
        {
            totalBossesDefeated++;
        }

        // Legacy wrapper preserving old signature
        public bool UnlockSpecialPath(string pathName, int goldCost, ItemData requiredMat, int matCount, InventoryComponent inv)
        {
            if (Enum.TryParse<SpecializationPath>(pathName, true, out var path))
            {
                if (activeSpecialization != SpecializationPath.None && activeSpecialization != path)
                {
                    return false;
                }

                activeSpecialization = path;
                specializationNodeLevel = Math.Max(1, specializationNodeLevel + 1);
                RecalculateStats();
                OnUpgradesChanged?.Invoke();
                return true;
            }
            return false;
        }
    }
}
