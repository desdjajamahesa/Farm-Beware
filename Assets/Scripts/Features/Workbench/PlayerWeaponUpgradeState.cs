using System;
using UnityEngine;
using FeaturesEconomy;
using PlayerUI;

namespace FeaturesWorkbench
{
    /// <summary>
    /// Menyimpan state progres upgrade senjata pemain (Level dasar dan jalur spesialisasi bibit).
    /// </summary>
    public class PlayerWeaponUpgradeState : MonoBehaviour
    {
        public static PlayerWeaponUpgradeState Instance { get; private set; }

        public event Action OnUpgradesChanged;

        [Header("Base Weapon Level")]
        [Range(1, 3)] public int weaponLevel = 1;
        public int baseDamage = 25;
        public float baseKnockback = 7f;

        [Header("Special Paths Unlocked")]
        public bool sweetPotatoPathUnlocked = false; // Speed & Mobility
        public bool taroPathUnlocked = false;        // Heavy Knockback & Defense
        public bool cornPathUnlocked = false;        // Power & Precision

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

        public void RecalculateStats()
        {
            // Level 1 = 25, Level 2 = 40, Level 3 = 60
            baseDamage = weaponLevel switch
            {
                1 => 25,
                2 => 40,
                3 => 60,
                _ => 25
            };

            baseKnockback = weaponLevel switch
            {
                1 => 7f,
                2 => 9f,
                3 => 12f,
                _ => 7f
            };

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
            return weaponLevel switch
            {
                1 => 500,
                2 => 1200,
                _ => 0
            };
        }

        /// <summary>
        /// Memeriksa apakah pemain memiliki Dummy Sword di inventori (atau di hotbar).
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

            if (inv == null || inv.slots == null) return false;

            foreach (var slot in inv.slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item != null)
                {
                    if (string.Equals(slot.item.itemId, "dummysword", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(slot.item.itemName, "Dummy Sword", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool UpgradeBaseLevel(InventoryComponent inv = null)
        {
            if (weaponLevel >= 3) return false;

            if (!HasDummySword(inv))
            {
                Debug.LogWarning("[PlayerWeaponUpgradeState] Gagal upgrade: Pemain tidak memiliki Dummy Sword di inventori!");
                return false;
            }

            int cost = GetNextUpgradeCost();
            if (PlayerWallet.Instance != null && PlayerWallet.Instance.SpendGold(cost))
            {
                weaponLevel++;
                RecalculateStats();

                if (FloatingCombatTextManager.Instance != null)
                {
                    FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 1.5f,
                        $"⚔️ Weapon Upgraded to Lv.{weaponLevel}!",
                        new Color(0.2f, 0.95f, 0.4f));
                }

                OnUpgradesChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool UnlockSpecialPath(string pathName, int goldCost, ItemData requiredMat, int matCount, InventoryComponent inv)
        {
            if (inv == null || PlayerWallet.Instance == null) return false;

            if (!HasDummySword(inv))
            {
                Debug.LogWarning($"[PlayerWeaponUpgradeState] Gagal unlock {pathName}: Pemain tidak memiliki Dummy Sword!");
                return false;
            }

            if (requiredMat == null || inv.CountItem(requiredMat) < matCount)
            {
                Debug.LogWarning($"[PlayerWeaponUpgradeState] Gagal unlock {pathName}: Material '{requiredMat?.itemName ?? "null"}' tidak cukup di inventori!");
                return false;
            }

            if (!PlayerWallet.Instance.CanAfford(goldCost))
            {
                Debug.LogWarning($"[PlayerWeaponUpgradeState] Gagal unlock {pathName}: Gold tidak cukup ({goldCost} Gold)!");
                return false;
            }

            PlayerWallet.Instance.SpendGold(goldCost);
            if (matCount > 0)
            {
                inv.RemoveItem(requiredMat, matCount);
            }

            if (pathName.Equals("SweetPotato", StringComparison.OrdinalIgnoreCase))
                sweetPotatoPathUnlocked = true;
            else if (pathName.Equals("Taro", StringComparison.OrdinalIgnoreCase))
                taroPathUnlocked = true;
            else if (pathName.Equals("Corn", StringComparison.OrdinalIgnoreCase))
                cornPathUnlocked = true;

            RecalculateStats();

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.5f,
                    $"✨ {pathName} Path Unlocked!",
                    new Color(0.95f, 0.85f, 0.25f));
            }

            OnUpgradesChanged?.Invoke();
            return true;
        }
    }
}
