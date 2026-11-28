using UnityEngine;
using UnityEngine.UI;
using FarmBeware.Core.Runtime;
using FeaturesEconomy;
using FeaturesKitchen;
using FeaturesWorkbench;

namespace PlayerUI
{
    /// <summary>
    /// Displays real-time player status and attribute information
    /// in the left column ("PLAYER") of the Character Sheet (Tab) window.
    /// </summary>
    public class PlayerStatsDisplayUI : MonoBehaviour, IStatsDisplayRefreshable
    {
        [Header("Referensi Player Components (Auto-find jika kosong)")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerControl playerControl;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerWeaponUpgradeState weaponUpgradeState;
        [SerializeField] private PlayerBuffManager buffManager;

        [Header("UI Teks - Core Stats")]
        [SerializeField] private Text txtHealth;
        [SerializeField] private Text txtStamina;
        [SerializeField] private Text txtHunger;
        [SerializeField] private Text txtThirst;

        [Header("UI Teks - Combat & Weapon")]
        [SerializeField] private Text txtWeaponName;
        [SerializeField] private Text txtWeaponLevel;
        [SerializeField] private Text txtBaseDamage;
        [SerializeField] private Text txtAttackSpeed;
        [SerializeField] private Text txtKickDamage;
        [SerializeField] private Text txtSkillDamage;

        [Header("UI Teks - General & Movement")]
        [SerializeField] private Text txtMoveSpeed;
        [SerializeField] private Text txtSprintSpeed;
        [SerializeField] private Text txtGold;
        [SerializeField] private Text txtWaterBottle;

        [Header("UI Teks - Active Buffs")]
        [SerializeField] private Text txtActiveBuffs;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeEvents();
            UpdateAllStats();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void EnsureReferences()
        {
            if (playerStats == null)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                    if (playerControl == null) playerControl = player.GetComponent<PlayerControl>();
                    if (playerEquipment == null) playerEquipment = player.GetComponent<PlayerEquipment>();
                    if (weaponUpgradeState == null) weaponUpgradeState = player.GetComponent<PlayerWeaponUpgradeState>();
                    if (buffManager == null) buffManager = player.GetComponent<PlayerBuffManager>();
                }
            }
        }

        private void SubscribeEvents()
        {
            if (playerStats != null)
            {
                playerStats.OnHealthChanged += HandleHealthChanged;
                playerStats.OnStaminaChanged += HandleStaminaChanged;
                playerStats.OnHungerChanged += HandleHungerChanged;
                playerStats.OnThirstChanged += HandleThirstChanged;
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnGoldChanged += HandleGoldChanged;
            }

            if (PlayerWaterBottle.Instance != null)
            {
                PlayerWaterBottle.Instance.OnWaterChanged += HandleWaterChanged;
            }

            if (FeaturesFarming.PlantWaterer.Instance != null)
            {
                FeaturesFarming.PlantWaterer.Instance.OnWaterChanged += HandleWaterChanged;
            }

            if (PlayerArmorUpgradeState.Instance != null)
            {
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged += HandleEquipmentChanged;
                PlayerArmorUpgradeState.Instance.OnArmorUpgraded += HandleArmorUpgraded;
            }
        }

        private void UnsubscribeEvents()
        {
            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= HandleHealthChanged;
                playerStats.OnStaminaChanged -= HandleStaminaChanged;
                playerStats.OnHungerChanged -= HandleHungerChanged;
                playerStats.OnThirstChanged -= HandleThirstChanged;
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnGoldChanged -= HandleGoldChanged;
            }

            if (PlayerWaterBottle.Instance != null)
            {
                PlayerWaterBottle.Instance.OnWaterChanged -= HandleWaterChanged;
            }

            if (FeaturesFarming.PlantWaterer.Instance != null)
            {
                FeaturesFarming.PlantWaterer.Instance.OnWaterChanged -= HandleWaterChanged;
            }

            if (PlayerArmorUpgradeState.Instance != null)
            {
                PlayerArmorUpgradeState.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
                PlayerArmorUpgradeState.Instance.OnArmorUpgraded -= HandleArmorUpgraded;
            }
        }

        private void HandleHealthChanged(int cur, int max) => UpdateCoreStats();
        private void HandleStaminaChanged(float cur, float max) => UpdateCoreStats();
        private void HandleHungerChanged(float cur, float max) => UpdateCoreStats();
        private void HandleThirstChanged(float cur, float max) => UpdateCoreStats();
        private void HandleGoldChanged(int newGold) => UpdateGeneralStats();
        private void HandleWaterChanged(float cur, float max) => UpdateGeneralStats();
        private void HandleEquipmentChanged(FarmBeware.Core.Runtime.EquipmentSlotType slot, ItemData item) => UpdateAllStats();
        private void HandleArmorUpgraded() => UpdateAllStats();

        public void UpdateAllStats()
        {
            UpdateCoreStats();
            UpdateCombatStats();
            UpdateGeneralStats();
            UpdateBuffStats();
        }

        private void UpdateCoreStats()
        {
            if (playerStats == null) return;

            var armorState = PlayerArmorUpgradeState.Instance ?? playerStats.GetComponent<PlayerArmorUpgradeState>();
            var snap = armorState != null ? armorState.GetAggregatedArmorStats() : default;

            if (txtHealth != null)
            {
                txtHealth.text = snap.BonusHealth > 0
                    ? $"{playerStats.currentHealth} / {playerStats.maxHealth} (Base {playerStats.BaseMaxHealth} + Gear +{snap.BonusHealth})"
                    : $"{playerStats.currentHealth} / {playerStats.maxHealth}";
            }

            if (txtStamina != null)
                txtStamina.text = $"{Mathf.CeilToInt(playerStats.currentStamina)} / {Mathf.CeilToInt(playerStats.maxStamina)} (+{playerStats.staminaRegenRate:0}/s)";

            if (txtHunger != null)
                txtHunger.text = $"{Mathf.CeilToInt(playerStats.currentHunger)} / {Mathf.CeilToInt(playerStats.maxHunger)}";

            if (txtThirst != null)
                txtThirst.text = $"{Mathf.CeilToInt(playerStats.currentThirst)} / {Mathf.CeilToInt(playerStats.maxThirst)}";
        }

        private Text txtArmorDefense;

        private void EnsureArmorDefenseRow()
        {
            if (txtArmorDefense != null) return;
            Transform statsContent = transform.Find("StatsContent");
            if (statsContent == null) return;

            Transform existing = statsContent.Find("Row_ArmorDefense");
            if (existing != null)
            {
                txtArmorDefense = existing.Find("Value")?.GetComponent<Text>();
                return;
            }

            Transform template = statsContent.Find("Row_Base Damage");
            if (template == null) return;

            var rowGo = Instantiate(template.gameObject, statsContent);
            rowGo.name = "Row_ArmorDefense";
            rowGo.transform.SetSiblingIndex(Mathf.Min(statsContent.childCount - 1, template.GetSiblingIndex() + 1));

            var labelTmp = rowGo.transform.Find("Label")?.GetComponent<Text>();
            if (labelTmp != null) labelTmp.text = "Armor & Defense";

            txtArmorDefense = rowGo.transform.Find("Value")?.GetComponent<Text>();
            if (txtArmorDefense != null)
            {
                txtArmorDefense.color = new Color(0.45f, 0.90f, 1f, 1f);
            }
        }

        private void UpdateCombatStats()
        {
            var armorState = PlayerArmorUpgradeState.Instance ?? (playerStats != null ? playerStats.GetComponent<PlayerArmorUpgradeState>() : null);
            var snap = armorState != null ? armorState.GetAggregatedArmorStats() : default;
            int armorLv = armorState != null ? armorState.armorLevel : 1;

            // Active weapon & Level
            string weaponName = "Root Cleaver";
            int level = 1;
            int baseDmg = 10;

            if (weaponUpgradeState != null)
            {
                level = weaponUpgradeState.weaponLevel;
                baseDmg = weaponUpgradeState.baseDamage;
                weaponName = $"Root Cleaver (Lv {level})";
            }

            if (armorState != null)
            {
                var eqWeapon = armorState.GetEquippedItem(FarmBeware.Core.Runtime.EquipmentSlotType.MainWeapon);
                if (eqWeapon != null)
                {
                    if (eqWeapon is ToolItemData toolWeapon && toolWeapon.baseDamage > 0)
                    {
                        baseDmg = FeaturesWorkbench.Core.EquipmentUpgradeEvaluator.CalculateWeaponDamage(toolWeapon.baseDamage, level);
                    }
                    weaponName = $"{eqWeapon.itemName} (Lv {level})";
                }
            }

            if (txtWeaponName != null) txtWeaponName.text = weaponName;
            if (txtWeaponLevel != null) txtWeaponLevel.text = $"Level {level}";
            if (txtBaseDamage != null) txtBaseDamage.text = $"{baseDmg}";

            EnsureArmorDefenseRow();
            if (txtArmorDefense != null)
            {
                int totalMaxHp = playerStats != null ? playerStats.maxHealth : 100;
                float ehp = FeaturesWorkbench.Core.EquipmentUpgradeEvaluator.CalculateEffectiveHP(totalMaxHp, snap.TotalMitigationRatio);
                int critPct = Mathf.RoundToInt(snap.BonusCriticalChance * 100f);
                txtArmorDefense.text = snap.EquippedArmorPieceCount > 0
                    ? $"Lv.{armorLv} ({snap.EquippedArmorPieceCount}/5) • +{snap.BonusDefense} DEF ({snap.TotalMitigationRatio * 100f:0.0}% DR) • +{critPct}% Crit • EHP {ehp:0}"
                    : $"Unarmored (0/5) • 0.0% DR • EHP {totalMaxHp}";
            }

            if (playerEquipment != null)
            {
                float effectiveAtkSpd = playerEquipment.AttackAnimationSpeed * playerEquipment.GetArmorAttackSpeedMultiplier();
                int gearAtkPct = Mathf.RoundToInt(snap.TotalAttackSpeed * 100f);
                if (txtAttackSpeed != null)
                {
                    txtAttackSpeed.text = gearAtkPct > 0
                        ? $"{effectiveAtkSpd:0.00}x (+{gearAtkPct}% Gear)"
                        : $"{effectiveAtkSpd:0.00}x";
                }
                if (txtKickDamage != null) txtKickDamage.gameObject.SetActive(false);
                if (txtSkillDamage != null) txtSkillDamage.text = $"{baseDmg * 2} Dmg (2.0x)";
            }
        }

        private void UpdateGeneralStats()
        {
            float speed = 5.0f;
            float sprint = 8.0f;

            if (playerControl != null)
            {
                speed = playerControl.EffectiveWalkSpeed;
                sprint = playerControl.EffectiveRunSpeed;
            }

            var armorState = PlayerArmorUpgradeState.Instance ?? (playerStats != null ? playerStats.GetComponent<PlayerArmorUpgradeState>() : null);
            var snap = armorState != null ? armorState.GetAggregatedArmorStats() : default;
            int movePct = Mathf.RoundToInt(snap.BonusMoveSpeed * 100f);

            if (txtMoveSpeed != null)
                txtMoveSpeed.text = movePct > 0 ? $"{speed:0.00} m/s (+{movePct}%)" : $"{speed:0.0} m/s";
            if (txtSprintSpeed != null)
                txtSprintSpeed.text = movePct > 0 ? $"{sprint:0.00} m/s (+{movePct}%)" : $"{sprint:0.0} m/s";

            if (PlayerWallet.Instance != null && txtGold != null)
            {
                txtGold.text = $"{PlayerWallet.Instance.CurrentGold} G";
            }

            if (txtWaterBottle != null)
            {
                int bottleCur = PlayerWaterBottle.Instance != null ? Mathf.CeilToInt(PlayerWaterBottle.Instance.CurrentWater) : 0;
                int bottleMax = PlayerWaterBottle.Instance != null ? Mathf.CeilToInt(PlayerWaterBottle.Instance.MaxWater) : 4;
                int plantCur = FeaturesFarming.PlantWaterer.Instance != null ? Mathf.CeilToInt(FeaturesFarming.PlantWaterer.Instance.CurrentWater) : 0;
                int plantMax = FeaturesFarming.PlantWaterer.Instance != null ? Mathf.CeilToInt(FeaturesFarming.PlantWaterer.Instance.MaxWater) : 100;
                txtWaterBottle.text = $"{bottleCur}/{bottleMax} Sips | {plantCur}/{plantMax}L Farm";
            }
        }

        private void UpdateBuffStats()
        {
            if (txtActiveBuffs == null) return;

            if (buffManager != null && buffManager.ActiveBuffs.Count > 0)
            {
                string buffList = "";
                foreach (var b in buffManager.ActiveBuffs)
                {
                    if (b != null && b.data != null)
                    {
                        buffList += $"• {b.data.buffName} ({Mathf.CeilToInt(b.remainingDuration)}s)\n";
                    }
                }
                txtActiveBuffs.text = string.IsNullOrEmpty(buffList) ? "No active buffs" : buffList.TrimEnd();
            }
            else
            {
                txtActiveBuffs.text = "No active buffs";
            }
        }
    }
}
