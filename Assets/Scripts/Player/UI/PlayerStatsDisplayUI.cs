using UnityEngine;
using UnityEngine.UI;
using FeaturesEconomy;
using FeaturesKitchen;
using FeaturesWorkbench;

namespace PlayerUI
{
    /// <summary>
    /// Menampilkan informasi status/atribut hidup dari Player secara real-time
    /// pada kolom kiri ("PLAYER") di jendela Character Sheet (Tab).
    /// </summary>
    public class PlayerStatsDisplayUI : MonoBehaviour
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
        }

        private void HandleHealthChanged(int cur, int max) => UpdateCoreStats();
        private void HandleStaminaChanged(float cur, float max) => UpdateCoreStats();
        private void HandleHungerChanged(float cur, float max) => UpdateCoreStats();
        private void HandleThirstChanged(float cur, float max) => UpdateCoreStats();
        private void HandleGoldChanged(int newGold) => UpdateGeneralStats();
        private void HandleWaterChanged(float cur, float max) => UpdateGeneralStats();

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

            if (txtHealth != null)
                txtHealth.text = $"{playerStats.currentHealth} / {playerStats.maxHealth}";

            if (txtStamina != null)
                txtStamina.text = $"{Mathf.CeilToInt(playerStats.currentStamina)} / {Mathf.CeilToInt(playerStats.maxStamina)} (+{playerStats.staminaRegenRate:0}/s)";

            if (txtHunger != null)
                txtHunger.text = $"{Mathf.CeilToInt(playerStats.currentHunger)} / {Mathf.CeilToInt(playerStats.maxHunger)}";

            if (txtThirst != null)
                txtThirst.text = $"{Mathf.CeilToInt(playerStats.currentThirst)} / {Mathf.CeilToInt(playerStats.maxThirst)}";
        }

        private void UpdateCombatStats()
        {
            // Active weapon & Level
            string weaponName = "Unarmed";
            int level = 1;
            int baseDmg = 10;

            if (weaponUpgradeState != null)
            {
                level = weaponUpgradeState.weaponLevel;
                baseDmg = weaponUpgradeState.baseDamage;
                weaponName = $"Hoe (Lv {level})";
            }

            if (playerEquipment != null && !playerEquipment.IsHoldingWeapon)
            {
                weaponName = "Unarmed";
            }

            if (txtWeaponName != null) txtWeaponName.text = weaponName;
            if (txtWeaponLevel != null) txtWeaponLevel.text = $"Level {level}";
            if (txtBaseDamage != null) txtBaseDamage.text = $"{baseDmg}";

            if (playerEquipment != null)
            {
                if (txtAttackSpeed != null) txtAttackSpeed.text = $"{playerEquipment.AttackAnimationSpeed:0.0}x";
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
                speed = playerControl.walkSpeed;
                sprint = playerControl.runSpeed;
            }

            if (txtMoveSpeed != null) txtMoveSpeed.text = $"{speed:0.0} m/s";
            if (txtSprintSpeed != null) txtSprintSpeed.text = $"{sprint:0.0} m/s";

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
