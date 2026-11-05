using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FarmBeware.Core.Runtime;
using FeaturesWorkbench.Core;
using FeaturesFarming;

namespace FeaturesWorkbench.UI
{
    /// <summary>
    /// Tactical Homestead Forge & Workbench UI.
    /// Architected with TasteSkill (tasteskill.dev) anti-slop layout discipline and AAA RPG forge ergonomics:
    /// - Widescreen 1240x760 asymmetric bento split-pane architecture
    /// - Live Material & Currency Telemetry Header (Gold, Mutated Roots, Husks, Boss Cores, Boss Kills)
    /// - Tab 01: Weapon Calibration (Current -> Next stat delta, 10-pip bar, Arsenal Loadout Switcher, Corn Cob Repeater foundry)
    ///           + Interactive 6-Node Seed Doctrine Specialization Tree Inspector (preview any path before committing)
    /// - Tab 02: Reinforced Armor Calibration + 5-Piece Sweet Potato Set Blueprint Matrix & Live Set Synergy Telemetry
    /// - Tab 03: Bio-Catalyst Night Overdrive Reactor (Standard vs. Overdrive systemic telemetry & 1-click catalyst injection)
    /// </summary>
    public class WorkbenchUI : MonoBehaviour, IModalWindow
    {
        public static WorkbenchUI Instance { get; private set; }

        public enum WorkbenchTab
        {
            Weapon = 0,
            Armor = 1,
            Farmland = 2,
            Overdrive = 3
        }

        // --- TasteSkill Tactical Color Palette (Dark Anodized Slate, Warm Amber Forge, Crisp Telemetry) ---
        private static readonly Color ColBackdrop       = new Color(0.02f, 0.03f, 0.05f, 0.86f);
        private static readonly Color ColWindowBg       = new Color(0.06f, 0.07f, 0.09f, 0.98f);
        private static readonly Color ColWindowBorder   = new Color(0.22f, 0.25f, 0.30f, 1.00f);
        private static readonly Color ColHeaderBg       = new Color(0.08f, 0.10f, 0.13f, 1.00f);
        private static readonly Color ColCardBg         = new Color(0.09f, 0.11f, 0.14f, 1.00f);
        private static readonly Color ColCardSubtle     = new Color(0.11f, 0.13f, 0.17f, 1.00f);
        private static readonly Color ColCardBorder     = new Color(0.18f, 0.21f, 0.27f, 1.00f);
        private static readonly Color ColTabInactive    = new Color(0.10f, 0.12f, 0.16f, 1.00f);
        private static readonly Color ColTabActive      = new Color(0.16f, 0.22f, 0.30f, 1.00f);
        private static readonly Color ColAccentAmber    = new Color(0.96f, 0.68f, 0.18f, 1.00f);
        private static readonly Color ColAccentCyan     = new Color(0.24f, 0.76f, 0.92f, 1.00f);
        private static readonly Color ColAccentEmerald  = new Color(0.22f, 0.78f, 0.45f, 1.00f);
        private static readonly Color ColAccentCrimson  = new Color(0.86f, 0.26f, 0.26f, 1.00f);
        private static readonly Color ColAccentViolet   = new Color(0.68f, 0.48f, 0.96f, 1.00f);
        private static readonly Color ColBtnPrimary     = new Color(0.16f, 0.44f, 0.30f, 1.00f);
        private static readonly Color ColBtnSecondary   = new Color(0.16f, 0.22f, 0.30f, 1.00f);
        private static readonly Color ColBtnDisabled    = new Color(0.13f, 0.15f, 0.18f, 0.85f);
        private static readonly Color ColTextPrimary    = new Color(0.95f, 0.96f, 0.98f, 1.00f);
        private static readonly Color ColTextMuted      = new Color(0.58f, 0.63f, 0.70f, 1.00f);

        [Header("State")]
        private bool isOpen = false;
        public bool IsOpen => isOpen;
        private WorkbenchTab currentTab = WorkbenchTab.Weapon;
        private SpecializationPath inspectedPath = SpecializationPath.SweetPotato;

        [Header("UI Elements (Backwards-Compatible Contracts)")]
        private GameObject panelRoot;
        private TextMeshProUGUI goldText;
        private TextMeshProUGUI materialLedgerText;

        // Top Navigation Tabs
        private Button tabWeaponButton;
        private TextMeshProUGUI tabWeaponButtonText;
        private Image tabWeaponIndicator;

        private Button tabArmorButton;
        private TextMeshProUGUI tabArmorButtonText;
        private Image tabArmorIndicator;

        private Button tabFarmlandButton;
        private TextMeshProUGUI tabFarmlandButtonText;
        private Image tabFarmlandIndicator;

        private Button tabOverdriveButton;
        private TextMeshProUGUI tabOverdriveButtonText;
        private Image tabOverdriveIndicator;

        private GameObject weaponTabContent;
        private GameObject armorTabContent;
        private GameObject farmlandTabContent;
        private GameObject overdriveTabContent;

        // Tab: Farmland Progression Elements
        private TextMeshProUGUI farmlandStatusBannerText;
        private TextMeshProUGUI farmlandTelemetryText;
        private Button expandFarmlandButton;
        private TextMeshProUGUI expandFarmlandButtonText;

        // Tab 01: Weapon & Arsenal Elements
        private Image weaponIconImage;
        private TextMeshProUGUI weaponStatsText;
        private TextMeshProUGUI weaponPipBarText;
        private Button upgradeBaseButton;
        private TextMeshProUGUI upgradeBaseButtonText;

        // Arsenal Loadout & Corn Repeater Foundry
        private TextMeshProUGUI arsenalStatusText;
        private Button equipCleaverButton;
        private TextMeshProUGUI equipCleaverButtonText;
        private Button craftOrEquipRepeaterButton;
        private TextMeshProUGUI craftOrEquipRepeaterButtonText;

        // Specialization Path Section & 6-Node Matrix
        private TextMeshProUGUI specializationHeader;
        private TextMeshProUGUI specializationSubHeader;
        private Button sweetPotatoButton;
        private TextMeshProUGUI sweetPotatoButtonText;
        private Button taroButton;
        private TextMeshProUGUI taroButtonText;
        private Button cornButton;
        private TextMeshProUGUI cornButtonText;

        private readonly Image[] nodeCardBackgrounds = new Image[6];
        private readonly Image[] nodeAccentBars = new Image[6];
        private readonly TextMeshProUGUI[] nodeCardTexts = new TextMeshProUGUI[6];

        private Button commitPathButton;
        private TextMeshProUGUI commitPathButtonText;
        private Button advanceSpecButton;
        private TextMeshProUGUI advanceSpecButtonText;

        // Tab 02: Armor & Set Crafting Elements
        private TextMeshProUGUI armorStatsText;
        private TextMeshProUGUI armorPipBarText;
        private Button upgradeArmorButton;
        private TextMeshProUGUI upgradeArmorButtonText;
        private TextMeshProUGUI setSynergyMatrixText;
        private Transform setCraftingContainer;

        private struct SetBlueprintBinding
        {
            public string ItemId;
            public string SlotLabel;
            public string DisplayName;
            public string StatDesc;
            public int GoldCost;
            public string MatId;
            public int MatCount;
            public EquipmentSlotType SlotType;
            public TextMeshProUGUI CardText;
            public Button ActionButton;
            public TextMeshProUGUI ActionButtonText;
            public Image StatusBadgeBg;
            public TextMeshProUGUI StatusBadgeText;
        }

        private readonly SetBlueprintBinding[] setBlueprints = new SetBlueprintBinding[5];

        // Tab 03: Bio-Catalyst Night Overdrive Elements
        private TextMeshProUGUI overdriveStatusBannerText;
        private Image overdriveStatusBannerBg;
        private TextMeshProUGUI overdriveTelemetryText;
        private Button armOverdriveButton;
        private TextMeshProUGUI armOverdriveButtonText;

        private IPlayerContext PlayerContext => ServiceLocator.Resolve<IPlayerContext>();
        private InventoryComponent playerInventory;
        private PlayerWeaponUpgradeState weaponState;
        private PlayerArmorUpgradeState armorState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUIHierarchyIfNeeded();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Start()
        {
            FindPlayerReferences();
        }

        private void FindPlayerReferences()
        {
            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (p != null)
            {
                playerInventory = p.GetComponent<InventoryComponent>();
                weaponState = p.GetComponent<PlayerWeaponUpgradeState>() ?? p.AddComponent<PlayerWeaponUpgradeState>();
                armorState = p.GetComponent<PlayerArmorUpgradeState>() ?? p.AddComponent<PlayerArmorUpgradeState>();
            }
        }

        private void OnEnable()
        {
            var wallet = ServiceLocator.Resolve<IWalletService>();
            if (wallet != null)
            {
                wallet.OnGoldChanged += HandleGoldChanged;
            }
        }

        private void OnDisable()
        {
            var wallet = ServiceLocator.Resolve<IWalletService>();
            if (wallet != null)
            {
                wallet.OnGoldChanged -= HandleGoldChanged;
            }
        }

        private void HandleGoldChanged(int newGold)
        {
            if (isOpen) RefreshView();
        }

        private void Update()
        {
            if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open()
        {
            FindPlayerReferences();
            isOpen = true;
            ModalStackManager.Instance.Push(this);

            BuildUIHierarchyIfNeeded();
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (PlayerContext != null)
            {
                PlayerContext.IsInputLocked = true;
                PlayerContext.StopMovement();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (weaponState != null && weaponState.activeSpecialization != SpecializationPath.None)
            {
                inspectedPath = weaponState.activeSpecialization;
            }

            SetTab(WorkbenchTab.Weapon);
            RefreshView();
        }

        public void Close()
        {
            isOpen = false;
            ModalStackManager.Instance.PopSpecific(this);

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (PlayerContext != null)
            {
                PlayerContext.IsInputLocked = false;
            }

            UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void IModalWindow.OpenModal() => Open();
        void IModalWindow.CloseModal() => Close();

        public void SetTab(WorkbenchTab tab)
        {
            currentTab = tab;
            if (weaponTabContent != null) weaponTabContent.SetActive(currentTab == WorkbenchTab.Weapon);
            if (armorTabContent != null) armorTabContent.SetActive(currentTab == WorkbenchTab.Armor);
            if (farmlandTabContent != null) farmlandTabContent.SetActive(currentTab == WorkbenchTab.Farmland);
            if (overdriveTabContent != null) overdriveTabContent.SetActive(currentTab == WorkbenchTab.Overdrive);

            UpdateTabVisual(tabWeaponButton, tabWeaponIndicator, tabWeaponButtonText, currentTab == WorkbenchTab.Weapon, ColAccentAmber);
            UpdateTabVisual(tabArmorButton, tabArmorIndicator, tabArmorButtonText, currentTab == WorkbenchTab.Armor, ColAccentCyan);
            UpdateTabVisual(tabFarmlandButton, tabFarmlandIndicator, tabFarmlandButtonText, currentTab == WorkbenchTab.Farmland, ColAccentEmerald);
            UpdateTabVisual(tabOverdriveButton, tabOverdriveIndicator, tabOverdriveButtonText, currentTab == WorkbenchTab.Overdrive, ColAccentViolet);

            RefreshView();
        }

        private static void UpdateTabVisual(Button btn, Image indicator, TextMeshProUGUI label, bool active, Color accentColor)
        {
            if (btn != null && btn.TryGetComponent<Image>(out var bg))
            {
                bg.color = active ? ColTabActive : ColTabInactive;
            }
            if (indicator != null)
            {
                indicator.color = active ? accentColor : new Color(1f, 1f, 1f, 0.08f);
            }
            if (label != null)
            {
                label.color = active ? Color.white : ColTextMuted;
            }
        }

        public void RefreshView()
        {
            if (weaponState == null || armorState == null) FindPlayerReferences();
            if (weaponState == null) return;

            int currentGold = ServiceLocator.Resolve<IWalletService>()?.CurrentGold ?? 0;
            RefreshTelemetryHeader(currentGold);

            if (currentTab == WorkbenchTab.Weapon)
            {
                RefreshWeaponTab(currentGold);
            }
            else if (currentTab == WorkbenchTab.Armor)
            {
                RefreshArmorTab(currentGold);
            }
            else if (currentTab == WorkbenchTab.Farmland)
            {
                RefreshFarmlandTab(currentGold);
            }
            else
            {
                RefreshOverdriveTab();
            }
        }

        private void RefreshTelemetryHeader(int currentGold)
        {
            if (goldText != null)
            {
                goldText.text = $"<color=#8A94A6>GOLD RESERVES</color>   <size=19><color=#FFD54F><b>{currentGold:N0} G</b></color></size>";
            }

            if (materialLedgerText != null)
            {
                int roots = CountMaterial("mat_mutated_root");
                int shrapnel = CountMaterial("mat_kernel_shrapnel");
                int eyes  = CountMaterial("mat_cyclops_eye");
                int cores = CountMaterial("mat_colossus_core");
                int cobs  = CountMaterial("mat_cob_core");
                int bosses = weaponState != null ? weaponState.totalBossesDefeated : 0;

                materialLedgerText.text =
                    $"<color=#8A94A6>ROOTS:</color> <b>{roots}</b>   " +
                    $"<color=#FFD54F>SHRAPNEL:</color> <b>{shrapnel}</b>   " +
                    $"<color=#B388FF>EYE:</color> <b>{eyes}</b>   " +
                    $"<color=#4FC3F7>COLOSSUS:</color> <b>{cores}</b>   " +
                    $"<color=#FFD54F>COB:</color> <b>{cobs}</b>   " +
                    $"<color=#FF8A65>BOSSES SLAIN:</color> <b>{bosses}</b>";
            }
        }

        private int CountMaterial(string itemId)
        {
            if (playerInventory == null || string.IsNullOrEmpty(itemId)) return 0;
            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
            var item = db != null ? db.GetItem(itemId) : null;
            if (item != null)
            {
                return playerInventory.CountItem(item);
            }

            int total = 0;
            if (playerInventory.slots != null)
            {
                for (int i = 0; i < playerInventory.slots.Count; i++)
                {
                    var s = playerInventory.slots[i];
                    if (s != null && !s.IsEmpty && s.item != null &&
                        string.Equals(s.item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        total += s.quantity;
                    }
                }
            }
            return total;
        }

        private bool OwnsItemInBagOrEquipped(string itemId, out bool isEquipped)
        {
            isEquipped = false;
            if (armorState != null)
            {
                foreach (EquipmentSlotType st in Enum.GetValues(typeof(EquipmentSlotType)))
                {
                    var eq = armorState.GetEquippedItem(st);
                    if (eq != null && string.Equals(eq.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        isEquipped = true;
                        return true;
                    }
                }
            }

            return CountMaterial(itemId) > 0;
        }

        private static string BuildPipBar(int current, int max, string activeHex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            sb.Append($"<color={activeHex}>");
            for (int i = 1; i <= max; i++)
            {
                if (i == current + 1) sb.Append("</color><color=#2C3340>");
                sb.Append(i <= current ? "■ " : "□ ");
            }
            sb.Append("</color>");
            return sb.ToString();
        }

        private void RefreshWeaponTab(int currentGold)
        {
            bool hasWeapon = weaponState.HasDummySword(playerInventory);
            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");

            var equippedWeapon = armorState != null ? armorState.GetEquippedItem(EquipmentSlotType.MainWeapon) : null;
            bool isRepeaterEquipped = equippedWeapon != null && string.Equals(equippedWeapon.itemId, "tool_weapon_corn_repeater", StringComparison.OrdinalIgnoreCase);

            var displayItem = equippedWeapon ?? db?.GetItem("tool_weapon_root_cleaver") ?? db?.GetItem("dummysword");
            if (weaponIconImage != null && displayItem != null)
            {
                Sprite resolvedIcon = displayItem.itemIcon;
                if (resolvedIcon == null && isRepeaterEquipped)
                {
                    resolvedIcon = FeaturesCombat.Adapters.SlingshotVisualFactory.GetSlingshotIconSprite();
                }
                if (resolvedIcon != null)
                {
                    weaponIconImage.sprite = resolvedIcon;
                    weaponIconImage.color = hasWeapon ? Color.white : new Color(0.40f, 0.40f, 0.40f, 0.55f);
                }
            }

            int curLv = weaponState.weaponLevel;
            bool isMaxLv = curLv >= EquipmentUpgradeEvaluator.MaxWeaponLevel;
            int nextLv = Mathf.Min(EquipmentUpgradeEvaluator.MaxWeaponLevel, curLv + 1);

            if (weaponPipBarText != null)
            {
                weaponPipBarText.text = $"{BuildPipBar(curLv, EquipmentUpgradeEvaluator.MaxWeaponLevel, "#F5AE2E")}  <b>LV. {curLv:00} / {EquipmentUpgradeEvaluator.MaxWeaponLevel:00}</b>";
            }

            if (weaponStatsText != null)
            {
                string weaponName = isRepeaterEquipped ? "Homestead Slingshot (Ranged)" : "Root Cleaver (Melee)";
                string statusBadge = hasWeapon
                    ? "<color=#38C773>[READY FOR NIGHT BRAWL]</color>"
                    : "<color=#FF5252>[NO WEAPON IN BAG]</color>";

                float curBonusPct = EquipmentUpgradeEvaluator.GetWeaponDamageBonusPercent(curLv);
                float nextBonusPct = EquipmentUpgradeEvaluator.GetWeaponDamageBonusPercent(nextLv);
                int nextDmg = EquipmentUpgradeEvaluator.CalculateWeaponDamage(25, nextLv);
                if (weaponState.cornPathUnlocked) nextDmg = Mathf.RoundToInt(nextDmg * 1.25f);
                float nextKb = 7f + ((nextLv - 1) * 0.75f);
                if (weaponState.taroPathUnlocked) nextKb *= 1.5f;

                string dmgDelta = isMaxLv
                    ? $"<b>{weaponState.baseDamage}</b> <color=#38C773>(MAX +{curBonusPct:F0}%)</color>"
                    : $"<b>{weaponState.baseDamage}</b> <color=#8A94A6>➔</color> <color=#38C773><b>{nextDmg}</b> (+{nextBonusPct:F0}%)</color>";

                string kbDelta = isMaxLv
                    ? $"<b>{weaponState.baseKnockback:F1}m</b> <color=#38C773>(MAX)</color>"
                    : $"<b>{weaponState.baseKnockback:F1}m</b> <color=#8A94A6>➔</color> <color=#38C773><b>{nextKb:F1}m</b></color>";

                string activeSpecName = weaponState.activeSpecialization switch
                {
                    SpecializationPath.SweetPotato => $"<color=#B388FF>Sweet Potato Doctrine (Node {weaponState.specializationNodeLevel}/6)</color>",
                    SpecializationPath.Taro => $"<color=#4FC3F7>Taro Doctrine (Node {weaponState.specializationNodeLevel}/6)</color>",
                    SpecializationPath.Corn => $"<color=#FFD54F>Corn Doctrine (Node {weaponState.specializationNodeLevel}/6)</color>",
                    _ => "<color=#8A94A6>Uncommitted (Select Doctrine on Right)</color>"
                };

                weaponStatsText.text =
                    $"<size=17><b>{weaponName}</b></size>   {statusBadge}\n" +
                    $"<color=#8A94A6>ATTACK POWER:</color> {dmgDelta}    <color=#8A94A6>IMPULSE:</color> {kbDelta}\n" +
                    $"<color=#8A94A6>ACTIVE DOCTRINE:</color> {activeSpecName}";
            }

            // Base Upgrade CTA Button
            if (upgradeBaseButton != null && upgradeBaseButtonText != null)
            {
                if (isMaxLv)
                {
                    upgradeBaseButtonText.text = "✓ MAXIMUM WEAPON CALIBRATION REACHED (LV. 10)";
                    upgradeBaseButton.interactable = false;
                    upgradeBaseButton.GetComponent<Image>().color = ColBtnDisabled;
                }
                else if (!hasWeapon)
                {
                    upgradeBaseButtonText.text = $"WEAPON REQUIRED IN BAG OR SLOT (TO LV. {nextLv})";
                    upgradeBaseButton.interactable = false;
                    upgradeBaseButton.GetComponent<Image>().color = ColBtnDisabled;
                }
                else
                {
                    int cost = weaponState.GetNextUpgradeCost();
                    bool canAfford = currentGold >= cost;
                    upgradeBaseButtonText.text = canAfford
                        ? $"▲ FORGE WEAPON CALIBRATION TO LV. {nextLv}   [{cost:N0} GOLD]"
                        : $"LOCKED: FORGE TO LV. {nextLv} [{cost:N0} G] — <color=#FF8A80>NEED {cost - currentGold:N0} MORE GOLD</color>";
                    upgradeBaseButton.interactable = canAfford;
                    upgradeBaseButton.GetComponent<Image>().color = canAfford ? ColBtnPrimary : ColBtnDisabled;
                }
            }

            // Arsenal Loadout & Ranged Weapon Foundry Card
            RefreshArsenalFoundryCard(currentGold, isRepeaterEquipped);

            // Right Pane: 6-Node Specialization Tree Inspector
            RefreshSpecializationTreePane(currentGold);
        }

        private void RefreshArsenalFoundryCard(int currentGold, bool isRepeaterEquipped)
        {
            bool ownsRepeater = OwnsItemInBagOrEquipped("tool_weapon_corn_repeater", out _);
            bool ownsCleaver = OwnsItemInBagOrEquipped("tool_weapon_root_cleaver", out bool isCleaverEquipped);

            int shrapnelCount = CountMaterial("mat_kernel_shrapnel");
            int rootCount = CountMaterial("mat_mutated_root");
            int repeaterMatCount = shrapnelCount >= 3 ? shrapnelCount : rootCount;
            string repeaterMatName = shrapnelCount >= 3 ? "Shrapnel" : "Roots";

            if (arsenalStatusText != null)
            {
                string activeLoadout = isRepeaterEquipped
                    ? "<color=#FFD54F><b>HOMESTEAD SLINGSHOT (RANGED)</b></color> — Cursor-Aimed 3D Slingshot (1/2/3/5 Volleys)"
                    : "<color=#4FC3F7><b>ROOT CLEAVER (MELEE)</b></color> — Auto-wielded at Night (3-Hit Combo, Heavy & Riposte)";

                arsenalStatusText.text =
                    $"<color=#8A94A6>NIGHT BRAWL MAIN WEAPON SLOT:</color> {activeLoadout}\n" +
                    $"<color=#8A94A6>HOMESTEAD SLINGSHOT BLUEPRINT:</color> 22 Base Dmg | Cursor-Aimed | Scales with Corn Doctrine (Pierce & Crit Splash)";
            }

            if (equipCleaverButton != null && equipCleaverButtonText != null)
            {
                if (!ownsCleaver)
                {
                    bool canCraftCleaver = currentGold >= 350 && rootCount >= 3;
                    equipCleaverButton.interactable = canCraftCleaver;
                    equipCleaverButton.GetComponent<Image>().color = canCraftCleaver ? ColAccentAmber : ColBtnDisabled;
                    equipCleaverButtonText.color = canCraftCleaver ? Color.black : ColTextPrimary;
                    equipCleaverButtonText.text =
                        $"<b>FORGE ROOT CLEAVER</b>\n<size=11>350G + 3 Roots [{rootCount}/3]</size>";
                }
                else
                {
                    equipCleaverButton.interactable = isRepeaterEquipped;
                    equipCleaverButton.GetComponent<Image>().color = isRepeaterEquipped ? ColBtnSecondary : ColBtnDisabled;
                    equipCleaverButtonText.color = Color.white;
                    equipCleaverButtonText.text = isRepeaterEquipped
                        ? "EQUIP ROOT CLEAVER (MELEE)"
                        : "✓ ROOT CLEAVER ACTIVE";
                }
            }

            if (craftOrEquipRepeaterButton != null && craftOrEquipRepeaterButtonText != null)
            {
                if (!ownsRepeater)
                {
                    bool canCraft = currentGold >= 450 && repeaterMatCount >= 3;
                    craftOrEquipRepeaterButton.interactable = canCraft;
                    craftOrEquipRepeaterButton.GetComponent<Image>().color = canCraft ? ColAccentAmber : ColBtnDisabled;
                    craftOrEquipRepeaterButtonText.color = canCraft ? Color.black : ColTextPrimary;
                    craftOrEquipRepeaterButtonText.text =
                        $"<b>FORGE HOMESTEAD SLINGSHOT</b>\n<size=11>450G + 3 {repeaterMatName} [{repeaterMatCount}/3]</size>";
                }
                else if (!isRepeaterEquipped)
                {
                    craftOrEquipRepeaterButton.interactable = true;
                    craftOrEquipRepeaterButton.GetComponent<Image>().color = ColBtnPrimary;
                    craftOrEquipRepeaterButtonText.color = Color.white;
                    craftOrEquipRepeaterButtonText.text = "<b>EQUIP HOMESTEAD SLINGSHOT</b>\n<size=11>Switch Night Stance to Ranged</size>";
                }
                else
                {
                    craftOrEquipRepeaterButton.interactable = false;
                    craftOrEquipRepeaterButton.GetComponent<Image>().color = ColBtnDisabled;
                    craftOrEquipRepeaterButtonText.color = ColTextPrimary;
                    craftOrEquipRepeaterButtonText.text = "✓ HOMESTEAD SLINGSHOT ACTIVE";
                }
            }
        }

        private void RefreshSpecializationTreePane(int currentGold)
        {
            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
            bool hasLockedPath = weaponState.activeSpecialization != SpecializationPath.None;

            // Keep buttons active at all times so the player can inspect all 3 paths or see which is locked!
            if (sweetPotatoButton != null) sweetPotatoButton.gameObject.SetActive(true);
            if (taroButton != null) taroButton.gameObject.SetActive(true);
            if (cornButton != null) cornButton.gameObject.SetActive(true);

            UpdatePathSelectorTab(sweetPotatoButton, sweetPotatoButtonText, SpecializationPath.SweetPotato, "01 // SWEET POTATO", "Speed & Dash Mobility", ColAccentViolet);
            UpdatePathSelectorTab(taroButton, taroButtonText, SpecializationPath.Taro, "02 // TARO GUARD", "Heavy Poise & Defense", ColAccentCyan);
            UpdatePathSelectorTab(cornButton, cornButtonText, SpecializationPath.Corn, "03 // CORN MARKSMAN", "Ranged Pierce & Blast", ColAccentAmber);

            if (specializationHeader != null)
            {
                if (hasLockedPath)
                {
                    specializationHeader.text = $"<b>DOCTRINE TREE // {GetPathDisplayName(inspectedPath)}</b>   <size=12><color=#38C773>[COMMITTED PATH: {weaponState.activeSpecialization.ToString().ToUpper()}]</color></size>";
                }
                else
                {
                    specializationHeader.text = $"<b>DOCTRINE TREE INSPECTOR // {GetPathDisplayName(inspectedPath)}</b>   <size=12><color=#F5AE2E>[UNCOMMITTED — PREVIEWING]</color></size>";
                }
            }

            if (specializationSubHeader != null)
            {
                specializationSubHeader.text = hasLockedPath && inspectedPath != weaponState.activeSpecialization
                    ? $"<color=#FF8A80>LOCKED OUT: You have permanently committed to the {weaponState.activeSpecialization} Doctrine (1 Specialization Rule).</color>"
                    : "Inspect all 6 nodes below. Committing to a Doctrine is permanent and unlocks its 6-tier mastery ladder.";
            }

            // Populate all 6 Node Cards for the currently inspected path
            for (int i = 0; i < 6; i++)
            {
                int nodeLv = i + 1;
                var info = EquipmentUpgradeEvaluator.GetNodeInfo(inspectedPath, nodeLv);
                bool isThisPathActive = hasLockedPath && weaponState.activeSpecialization == inspectedPath;
                bool isUnlocked = isThisPathActive && weaponState.specializationNodeLevel >= nodeLv;
                bool isNextToUnlock = isThisPathActive && weaponState.specializationNodeLevel == nodeLv - 1;
                bool isLockedOtherPath = hasLockedPath && weaponState.activeSpecialization != inspectedPath;

                if (nodeCardBackgrounds[i] != null)
                {
                    if (isUnlocked)
                        nodeCardBackgrounds[i].color = new Color(0.09f, 0.19f, 0.14f, 1f);
                    else if (isNextToUnlock)
                        nodeCardBackgrounds[i].color = new Color(0.16f, 0.15f, 0.10f, 1f);
                    else
                        nodeCardBackgrounds[i].color = ColCardSubtle;
                }

                if (nodeAccentBars[i] != null)
                {
                    if (isUnlocked) nodeAccentBars[i].color = ColAccentEmerald;
                    else if (isNextToUnlock) nodeAccentBars[i].color = ColAccentAmber;
                    else if (info.IsUltimate) nodeAccentBars[i].color = ColAccentViolet;
                    else nodeAccentBars[i].color = new Color(1f, 1f, 1f, 0.12f);
                }

                if (nodeCardTexts[i] != null)
                {
                    string stateBadge;
                    if (isUnlocked) stateBadge = "<color=#38C773><b>[✓ UNLOCKED]</b></color>";
                    else if (isNextToUnlock) stateBadge = "<color=#F5AE2E><b>[► NEXT NODE]</b></color>";
                    else if (isLockedOtherPath) stateBadge = "<color=#666E7A>[PATH LOCKED]</color>";
                    else stateBadge = "<color=#8A94A6>[PREVIEW]</color>";

                    string reqText = $"{info.GoldCost:N0}G";
                    if (!string.IsNullOrEmpty(info.RequiredMaterialId) && info.RequiredMaterialCount > 0)
                    {
                        var matItem = db?.GetItem(info.RequiredMaterialId);
                        string matName = matItem != null ? matItem.itemName : info.RequiredMaterialId;
                        int owned = CountMaterial(info.RequiredMaterialId);
                        string matCol = owned >= info.RequiredMaterialCount ? "#38C773" : "#FF8A80";
                        reqText += $" + <color={matCol}>{owned}/{info.RequiredMaterialCount} {matName}</color>";
                    }
                    if (info.IsUltimate && info.RequiredBossKills > 0)
                    {
                        int kills = weaponState.totalBossesDefeated;
                        string killCol = kills >= info.RequiredBossKills ? "#38C773" : "#FF8A80";
                        reqText += $" + <color={killCol}>{kills}/{info.RequiredBossKills} Bosses</color>";
                    }

                    nodeCardTexts[i].text =
                        $"<b>{info.Title}</b>   {stateBadge}\n" +
                        $"<color=#4FC3F7>{info.EffectSummary}</color> — <color=#B0B8C4>{info.Description}</color>\n" +
                        $"<size=11><color=#8A94A6>REQ:</color> {reqText}</size>";
                }
            }

            // Bottom Action Bar: Commit Path vs Advance Active Node
            bool hasWeapon = weaponState != null && weaponState.HasDummySword(playerInventory);
            if (commitPathButton != null)
            {
                commitPathButton.gameObject.SetActive(!hasLockedPath);
                if (!hasLockedPath && commitPathButtonText != null)
                {
                    if (!hasWeapon)
                    {
                        commitPathButton.interactable = false;
                        commitPathButton.GetComponent<Image>().color = ColBtnDisabled;
                        commitPathButtonText.text = "WEAPON REQUIRED IN BAG OR SLOT TO COMMIT DOCTRINE";
                    }
                    else
                    {
                        var node1 = EquipmentUpgradeEvaluator.GetNodeInfo(inspectedPath, 1);
                        bool canAffordLv1 = currentGold >= node1.GoldCost;
                        commitPathButton.interactable = true;
                        commitPathButton.GetComponent<Image>().color = ColBtnPrimary;
                        commitPathButtonText.text = canAffordLv1
                            ? $"★ COMMIT TO {GetPathDisplayName(inspectedPath)} & UNLOCK NODE 1 ({node1.GoldCost:N0} GOLD)"
                            : $"★ COMMIT TO {GetPathDisplayName(inspectedPath)} DOCTRINE (LOCKS PATH PERMANENTLY)";
                    }
                }
            }

            if (advanceSpecButton != null)
            {
                advanceSpecButton.gameObject.SetActive(hasLockedPath);
                if (hasLockedPath && advanceSpecButtonText != null)
                {
                    if (!hasWeapon)
                    {
                        advanceSpecButtonText.text = "WEAPON REQUIRED IN BAG OR SLOT TO ADVANCE DOCTRINE";
                        advanceSpecButton.interactable = false;
                        advanceSpecButton.GetComponent<Image>().color = ColBtnDisabled;
                    }
                    else if (inspectedPath != weaponState.activeSpecialization)
                    {
                        advanceSpecButtonText.text = $"SWITCH VIEW BACK TO ACTIVE {weaponState.activeSpecialization.ToString().ToUpper()} DOCTRINE";
                        advanceSpecButton.interactable = true;
                        advanceSpecButton.GetComponent<Image>().color = ColBtnSecondary;
                    }
                    else if (weaponState.specializationNodeLevel >= EquipmentUpgradeEvaluator.MaxSpecializationNodeLevel)
                    {
                        advanceSpecButtonText.text = "✓ ULTIMATE DOCTRINE MASTERED (6 / 6 NODES UNLOCKED)";
                        advanceSpecButton.interactable = false;
                        advanceSpecButton.GetComponent<Image>().color = ColBtnDisabled;
                    }
                    else
                    {
                        int nextLevel = weaponState.specializationNodeLevel + 1;
                        var node = EquipmentUpgradeEvaluator.GetNodeInfo(weaponState.activeSpecialization, nextLevel);

                        var mat = !string.IsNullOrEmpty(node.RequiredMaterialId) ? db?.GetItem(node.RequiredMaterialId) : null;
                        int matCount = CountMaterial(node.RequiredMaterialId);
                        bool hasMat = node.RequiredMaterialCount == 0 || matCount >= node.RequiredMaterialCount;
                        bool hasGold = currentGold >= node.GoldCost;
                        bool hasKills = !node.IsUltimate || weaponState.totalBossesDefeated >= node.RequiredBossKills;
                        bool canUnlock = hasGold && hasMat && hasKills;

                        string costDesc = $"{node.GoldCost:N0}G";
                        if (node.RequiredMaterialCount > 0)
                        {
                            string mName = mat != null ? mat.itemName : node.RequiredMaterialId;
                            costDesc += $" + { mName } [{matCount}/{node.RequiredMaterialCount}]";
                        }
                        if (node.IsUltimate)
                        {
                            costDesc += $" + Boss Kills [{weaponState.totalBossesDefeated}/{node.RequiredBossKills}]";
                        }

                        advanceSpecButtonText.text = canUnlock
                            ? $"▲ UNLOCK {node.Title.ToUpper()}   [{costDesc}]"
                            : $"LOCKED: {node.Title.ToUpper()}   [{costDesc}]";
                        advanceSpecButton.interactable = canUnlock;
                        advanceSpecButton.GetComponent<Image>().color = canUnlock ? ColBtnPrimary : ColBtnDisabled;
                    }
                }
            }
        }

        private void UpdatePathSelectorTab(Button btn, TextMeshProUGUI txt, SpecializationPath path, string title, string subtitle, Color pathAccent)
        {
            if (btn == null || txt == null) return;
            bool isInspected = inspectedPath == path;
            bool isCommitted = weaponState != null && weaponState.activeSpecialization == path;

            if (btn.TryGetComponent<Image>(out var bg))
            {
                bg.color = isInspected ? new Color(pathAccent.r * 0.28f, pathAccent.g * 0.28f, pathAccent.b * 0.28f, 1f) : ColTabInactive;
            }

            string lockTag = isCommitted ? " <color=#38C773>[ACTIVE]</color>" : "";
            txt.text = $"<b>{title}</b>{lockTag}\n<size=11><color=#B0B8C4>{subtitle}</color></size>";
        }

        private static string GetPathDisplayName(SpecializationPath path) => path switch
        {
            SpecializationPath.SweetPotato => "THE WAY OF SWEET POTATO",
            SpecializationPath.Taro => "HARDEN MIND OF TARO",
            SpecializationPath.Corn => "DEADLY ACCURACY OF CORN",
            _ => "UNSELECTED"
        };

        private void RefreshArmorTab(int currentGold)
        {
            if (armorState == null) return;

            int curLv = armorState.armorLevel;
            bool isMax = curLv >= EquipmentUpgradeEvaluator.MaxArmorLevel;
            int nextLv = Mathf.Min(EquipmentUpgradeEvaluator.MaxArmorLevel, curLv + 1);

            if (armorPipBarText != null)
            {
                armorPipBarText.text = $"{BuildPipBar(curLv, EquipmentUpgradeEvaluator.MaxArmorLevel, "#3DD5F3")}  <b>LV. {curLv:00} / {EquipmentUpgradeEvaluator.MaxArmorLevel:00}</b>";
            }

            if (armorStatsText != null)
            {
                float curMit = armorState.DamageMitigationRatio * 100f;
                float nextMit = EquipmentUpgradeEvaluator.GetArmorMitigationRatio(nextLv) * 100f;

                string mitDelta = isMax
                    ? $"<b>{curMit:F0}%</b> <color=#38C773>(MAX PLATING)</color>"
                    : $"<b>{curMit:F0}%</b> <color=#8A94A6>➔</color> <color=#3DD5F3><b>{nextMit:F0}%</b></color>";

                armorStatsText.text =
                    $"<size=17><b>Reinforced Homestead Plating</b></size>\n" +
                    $"<color=#8A94A6>DAMAGE MITIGATION:</color> {mitDelta}   <color=#8A94A6>(MAX CAP: 32% AT LV.10)</color>\n" +
                    $"<color=#B0B8C4>Reduces all incoming physical & projectile damage during Night Brawl waves.</color>";
            }

            bool hasArmor = armorState.HasAnyArmorPiece(playerInventory);
            if (upgradeArmorButton != null && upgradeArmorButtonText != null)
            {
                if (isMax)
                {
                    upgradeArmorButtonText.text = "✓ MAXIMUM ARMOR PLATING REACHED (LV. 10)";
                    upgradeArmorButton.interactable = false;
                    upgradeArmorButton.GetComponent<Image>().color = ColBtnDisabled;
                }
                else if (!hasArmor)
                {
                    upgradeArmorButtonText.text = "ARMOR REQUIRED IN BAG OR SLOT (CRAFT OR BUY FIRST)";
                    upgradeArmorButton.interactable = false;
                    upgradeArmorButton.GetComponent<Image>().color = ColBtnDisabled;
                }
                else
                {
                    int cost = armorState.GetNextUpgradeCost();
                    bool canAfford = currentGold >= cost;
                    upgradeArmorButtonText.text = canAfford
                        ? $"▲ REINFORCE ARMOR PLATING TO LV. {nextLv}   [{cost:N0} GOLD]"
                        : $"LOCKED: REINFORCE TO LV. {nextLv} [{cost:N0} G] — <color=#FF8A80>NEED {cost - currentGold:N0} MORE GOLD</color>";
                    upgradeArmorButton.interactable = canAfford;
                    upgradeArmorButton.GetComponent<Image>().color = canAfford ? ColBtnPrimary : ColBtnDisabled;
                }
            }

            // Set Bonus Synergy Telemetry Card
            if (setSynergyMatrixText != null)
            {
                var setBonus = armorState.GetCurrentSetBonus("Sweet Potato");
                int pcs = setBonus.EquippedPieceCount;

                string c2 = pcs >= 2 ? "#38C773" : "#666E7A";
                string c4 = pcs >= 4 ? "#38C773" : "#666E7A";
                string c5 = pcs >= 5 ? "#38C773" : "#666E7A";

                setSynergyMatrixText.text =
                    $"<b>SWEET POTATO COMBAT SET SYNERGY</b>   <color=#F5AE2E>[{pcs} / 5 EQUIPPED]</color>\n\n" +
                    $"<color={c2}>■ <b>[2-PIECE BONUS]</b>  +10% Weapon Attack Speed {(pcs >= 2 ? "(ACTIVE)" : "(LOCKED)")}</color>\n" +
                    $"<color={c4}>■ <b>[4-PIECE BONUS]</b>  +15% Phantom Dodge Counter Damage for 3.0s {(pcs >= 4 ? "(ACTIVE)" : "(LOCKED)")}</color>\n" +
                    $"<color={c5}>■ <b>[5-PIECE CAPSTONE]</b>  Shadow Dash Cooldown Reset on Monster Kill {(pcs >= 5 ? "(ACTIVE)" : "(LOCKED)")}</color>\n\n" +
                    $"<size=11><color=#8A94A6>TIP: Click 'EQUIP NOW' on any crafted piece on the right, or drag & drop in Tab Character Sheet.</color></size>";
            }

            // Refresh 5 Blueprint Cards on the Right Pane
            int ownedRoots = CountMaterial("mat_mutated_root");
            for (int i = 0; i < setBlueprints.Length; i++)
            {
                var bp = setBlueprints[i];
                bool owned = OwnsItemInBagOrEquipped(bp.ItemId, out bool isEquipped);
                bool canAfford = currentGold >= bp.GoldCost && ownedRoots >= bp.MatCount;

                if (bp.StatusBadgeBg != null && bp.StatusBadgeText != null)
                {
                    if (isEquipped)
                    {
                        bp.StatusBadgeBg.color = new Color(0.14f, 0.42f, 0.26f, 1f);
                        bp.StatusBadgeText.text = "✓ EQUIPPED";
                    }
                    else if (owned)
                    {
                        bp.StatusBadgeBg.color = new Color(0.20f, 0.36f, 0.52f, 1f);
                        bp.StatusBadgeText.text = "IN BAG";
                    }
                    else
                    {
                        bp.StatusBadgeBg.color = new Color(0.16f, 0.18f, 0.22f, 1f);
                        bp.StatusBadgeText.text = "UNFORGED";
                    }
                }

                if (bp.CardText != null)
                {
                    string rootColor = ownedRoots >= bp.MatCount ? "#38C773" : "#FF8A80";
                    bp.CardText.text =
                        $"<color=#8A94A6>[{bp.SlotLabel}]</color>  <b>{bp.DisplayName}</b>\n" +
                        $"<color=#B0B8C4>{bp.StatDesc}</color>\n" +
                        $"<size=11><color=#FFD54F>{bp.GoldCost:N0} Gold</color> + <color={rootColor}>{ownedRoots}/{bp.MatCount} Mutated Roots</color></size>";
                }

                if (bp.ActionButton != null && bp.ActionButtonText != null)
                {
                    if (isEquipped)
                    {
                        bp.ActionButton.interactable = false;
                        bp.ActionButton.GetComponent<Image>().color = ColBtnDisabled;
                        bp.ActionButtonText.text = "✓ EQUIPPED IN SLOT";
                    }
                    else if (owned)
                    {
                        bp.ActionButton.interactable = true;
                        bp.ActionButton.GetComponent<Image>().color = ColAccentCyan;
                        bp.ActionButtonText.color = Color.black;
                        bp.ActionButtonText.text = "<b>EQUIP TO SLOT NOW</b>";
                    }
                    else
                    {
                        bp.ActionButton.interactable = canAfford;
                        bp.ActionButton.GetComponent<Image>().color = canAfford ? ColBtnPrimary : ColBtnDisabled;
                        bp.ActionButtonText.color = Color.white;
                        bp.ActionButtonText.text = canAfford ? "<b>CRAFT & EQUIP</b>" : "INSUFFICIENT MATS";
                    }
                }
            }
        }

        private void RefreshFarmlandTab(int currentGold)
        {
            var mgr = FeaturesFarming.FarmlandExpansionManager.Instance;
            int unlocked = mgr != null ? mgr.UnlockedPlotCount : 6;
            bool isMax = unlocked >= FeaturesFarming.FarmlandExpansionManager.MaxPlotCount;
            int nextCount = Mathf.Min(FeaturesFarming.FarmlandExpansionManager.MaxPlotCount, unlocked + FeaturesFarming.FarmlandExpansionManager.PlotsPerExpansion);
            int cost = FeaturesFarming.FarmlandExpansionManager.ExpansionCostGold;
            bool canAfford = currentGold >= cost;

            if (farmlandStatusBannerText != null)
            {
                farmlandStatusBannerText.text =
                    $"<size=18><color=#38C773><b>FARMLAND EXPANSION & LAND DEEDS // CAPACITY PROGRESSION</b></color></size>\n" +
                    $"<color=#B0B8C4>Currently Arable: <b>{unlocked} / 16 Farmland Plots</b> active and ready for crop sowing.</color>";
            }

            if (farmlandTelemetryText != null)
            {
                string pips = BuildPipBar(unlocked, FeaturesFarming.FarmlandExpansionManager.MaxPlotCount, "#38C773");
                string nextPair = isMax ? "None (Full Capacity)" : $"FarmlandPlot_{unlocked + 1} & FarmlandPlot_{unlocked + 2}";

                farmlandTelemetryText.text =
                    $"<b>HOMESTEAD LAND SURVEY TELEMETRY</b>\n\n" +
                    $"• <color=#8A94A6>ACTIVE ARABLE PLOTS:</color>      {pips}  <b>{unlocked} / 16</b>\n\n" +
                    $"• <color=#8A94A6>INITIAL DAY 1 CAPACITY:</color>   6 Plots (FarmlandPlot_1 .. 6)\n\n" +
                    $"• <color=#8A94A6>NEXT EXPANSION DEED:</color>      <b>+2 Arable Plots</b> ({nextPair})\n\n" +
                    $"• <color=#8A94A6>EXPANSION DEED PRICE:</color>     <color=#FFD54F><b>{cost:N0} Gold</b></color> per 2 plots\n\n" +
                    $"• <color=#8A94A6>MAXIMUM POTENTIAL YIELD:</color>  Up to 16 plots producing continuous crop harvests.";
            }

            if (expandFarmlandButton != null && expandFarmlandButtonText != null)
            {
                if (isMax)
                {
                    expandFarmlandButtonText.text = "✓ MAXIMUM FARMLAND EXPANDED (16 / 16 PLOTS)";
                    expandFarmlandButton.interactable = false;
                    expandFarmlandButton.GetComponent<Image>().color = ColBtnDisabled;
                }
                else
                {
                    expandFarmlandButtonText.text = canAfford
                        ? $"▲ PURCHASE LAND DEED (+2 PLOTS: {unlocked} ➔ {nextCount})   [{cost:N0} GOLD]"
                        : $"LOCKED: LAND DEED (+2 PLOTS) [{cost:N0} G] — <color=#FF8A80>NEED {cost - currentGold:N0} MORE GOLD</color>";
                    expandFarmlandButton.interactable = canAfford;
                    expandFarmlandButton.GetComponent<Image>().color = canAfford ? ColBtnPrimary : ColBtnDisabled;
                }
            }
        }

        private void OnClickExpandFarmland()
        {
            var mgr = FeaturesFarming.FarmlandExpansionManager.Instance;
            if (mgr != null && mgr.TryExpand())
            {
                RefreshView();
            }
        }

        private void RefreshOverdriveTab()
        {
            bool isOverdrive = FeaturesCombat.NightBrawlManager.Instance != null &&
                               FeaturesCombat.NightBrawlManager.Instance.IsOverdriveNightActive;

            int totalCatalysts = CountAllMonsterMaterials();

            if (overdriveStatusBannerBg != null)
            {
                overdriveStatusBannerBg.color = isOverdrive
                    ? new Color(0.32f, 0.14f, 0.08f, 1f)
                    : new Color(0.10f, 0.14f, 0.18f, 1f);
            }

            if (overdriveStatusBannerText != null)
            {
                overdriveStatusBannerText.text = isOverdrive
                    ? "<size=18><color=#FFAB40><b>🔥 BIO-CATALYST NIGHT OVERDRIVE: ARMED FOR TONIGHT</b></color></size>\n" +
                      "<color=#FFE0B2>Nocturnal Soil Irrigator & Mutagenic Beacon are active. Crops will grow at 50% speed tonight and auto-purify!</color>"
                    : "<size=18><color=#4FC3F7><b>REACTOR STANDBY // STANDARD NIGHT PROTOCOL</b></color></size>\n" +
                      "<color=#B0B8C4>Inject 2x Monster Bio-Materials to supercharge farm irrigation tonight in exchange for heightened monster aggression.</color>";
            }

            if (overdriveTelemetryText != null)
            {
                overdriveTelemetryText.text =
                    "<b>SYSTEMIC IMPACT COMPARISON (STANDARD NIGHT VS. OVERDRIVE NIGHT)</b>\n\n" +
                    "• <color=#8A94A6>NOCTURNAL CROP GROWTH:</color>   0% (Paused at Night)   <color=#8A94A6>➔</color>   <color=#38C773><b>50% Daytime Growth Speed All Night</b></color>\n\n" +
                    "• <color=#8A94A6>HARVEST PURIFICATION:</color>    Dirty Crop (Needs Sink) <color=#8A94A6>➔</color>   <color=#38C773><b>100% Clean Harvest Automatically</b></color>\n\n" +
                    "• <color=#8A94A6>MONSTER GOLD BOUNTY:</color>     100% Base Gold         <color=#8A94A6>➔</color>   <color=#FFD54F><b>125% Gold (+25% Bonus Bounty)</b></color>\n\n" +
                    "• <color=#8A94A6>MATERIAL DROP RATE:</color>      Base Drop Chance       <color=#8A94A6>➔</color>   <color=#FFD54F><b>+30% Flat Material Drop Chance</b></color>\n\n" +
                    "• <color=#8A94A6>HAZARD ESCALATION:</color>       100% Monster Speed     <color=#8A94A6>➔</color>   <color=#FF8A65><b>115% Monster Move Speed (+15% Threat)</b></color>\n\n" +
                    $"<color=#8A94A6>AVAILABLE MONSTER BIO-MATERIALS IN BAG:</color> <b>{totalCatalysts}</b> <color=#8A94A6>(Requires {FeaturesCombat.Core.PureLogic.NightOverdriveEvaluator.RequiredCatalystMaterialCount})</color>";
            }

            if (armOverdriveButton != null && armOverdriveButtonText != null)
            {
                bool canArm = !isOverdrive && totalCatalysts >= FeaturesCombat.Core.PureLogic.NightOverdriveEvaluator.RequiredCatalystMaterialCount;
                armOverdriveButton.interactable = canArm;
                armOverdriveButton.GetComponent<Image>().color = isOverdrive
                    ? ColBtnDisabled
                    : (canArm ? new Color(0.85f, 0.38f, 0.12f, 1f) : ColBtnDisabled);

                armOverdriveButtonText.text = isOverdrive
                    ? "✓ BIO-CATALYST NIGHT OVERDRIVE IS ARMED"
                    : (canArm
                        ? $"🔥 INJECT 2x MONSTER MATERIALS & ARM NIGHT OVERDRIVE   [{totalCatalysts}/2 AVAILABLE]"
                        : $"LOCKED: NEED 2 MONSTER MATERIALS IN BAG   [{totalCatalysts}/2 AVAILABLE]");
            }
        }

        private int CountAllMonsterMaterials()
        {
            if (playerInventory == null || playerInventory.slots == null) return 0;
            int count = 0;
            for (int i = 0; i < playerInventory.slots.Count; i++)
            {
                var slot = playerInventory.slots[i];
                if (slot != null && !slot.IsEmpty && slot.item != null)
                {
                    if (slot.item.category == ItemCategory.Material ||
                        (slot.item.itemId != null && slot.item.itemId.StartsWith("mat_", StringComparison.OrdinalIgnoreCase)))
                    {
                        count += slot.quantity;
                    }
                }
            }
            return count;
        }

        public void CraftSetPiece(string itemId, int goldCost, string matId, int matCount)
        {
            var wallet = ServiceLocator.Resolve<IWalletService>();
            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
            var targetItem = db?.GetItem(itemId);
            var reqMat = db?.GetItem(matId);

            if (wallet == null || targetItem == null || reqMat == null || playerInventory == null) return;

            if (!wallet.CanAfford(goldCost) || playerInventory.CountItem(reqMat) < matCount)
            {
                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                fct?.SpawnText(transform.position + Vector3.up * 1.5f, "Insufficient Gold or Materials!", new Color(1f, 0.4f, 0.4f));
                return;
            }

            wallet.SpendGold(goldCost);
            playerInventory.RemoveItem(reqMat, matCount);
            playerInventory.AddItem(targetItem, 1);

            // Auto-equip if the corresponding slot is currently empty or if it's a newly forged piece
            if (armorState != null)
            {
                if (targetItem is EquipmentItemData eqData)
                {
                    if (armorState.GetEquippedItem(eqData.slotType) == null)
                    {
                        if (armorState.EquipItem(eqData.slotType, targetItem, out _))
                        {
                            playerInventory.RemoveItem(targetItem, 1);
                        }
                    }
                }
                else if (targetItem.category == ItemCategory.Weapon ||
                         targetItem.itemId == "tool_weapon_root_cleaver" ||
                         targetItem.itemId == "tool_weapon_corn_repeater" ||
                         targetItem.itemId == "dummysword")
                {
                    if (armorState.GetEquippedItem(EquipmentSlotType.MainWeapon) == null)
                    {
                        if (armorState.EquipItem(EquipmentSlotType.MainWeapon, targetItem, out _))
                        {
                            playerInventory.RemoveItem(targetItem, 1);
                        }
                    }
                }
            }

            var fctOk = ServiceLocator.Resolve<IFloatingTextService>();
            fctOk?.SpawnText(transform.position + Vector3.up * 1.5f, $"Forged: {targetItem.itemName}!", new Color(0.4f, 1f, 0.5f));

            RefreshView();
        }

        private void OnClickBlueprintAction(int index)
        {
            if (index < 0 || index >= setBlueprints.Length) return;
            var bp = setBlueprints[index];

            bool owned = OwnsItemInBagOrEquipped(bp.ItemId, out bool isEquipped);
            if (isEquipped) return;

            if (owned && armorState != null && playerInventory != null)
            {
                var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
                var item = db?.GetItem(bp.ItemId);
                if (item != null && armorState.EquipItem(bp.SlotType, item, out var prevItem))
                {
                    playerInventory.RemoveItem(item, 1);
                    if (prevItem != null) playerInventory.AddItem(prevItem, 1);
                    RefreshView();
                    return;
                }
            }

            CraftSetPiece(bp.ItemId, bp.GoldCost, bp.MatId, bp.MatCount);
        }

        private void OnClickEquipRootCleaver()
        {
            bool ownsCleaver = OwnsItemInBagOrEquipped("tool_weapon_root_cleaver", out bool isCleaverEquipped);
            if (isCleaverEquipped) return;

            if (!ownsCleaver)
            {
                CraftSetPiece("tool_weapon_root_cleaver", 350, "mat_mutated_root", 3);
                ownsCleaver = OwnsItemInBagOrEquipped("tool_weapon_root_cleaver", out _);
            }

            if (ownsCleaver && armorState != null)
            {
                var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
                var cleaver = db?.GetItem("tool_weapon_root_cleaver") ?? db?.GetItem("dummysword");
                if (cleaver != null)
                {
                    armorState.EquipItem(EquipmentSlotType.MainWeapon, cleaver, out _);
                    RefreshView();
                }
            }
        }

        private void OnClickCraftOrEquipRepeater()
        {
            bool owns = OwnsItemInBagOrEquipped("tool_weapon_corn_repeater", out bool isEquipped);
            if (isEquipped) return;

            var db = ItemDatabase.Instance ?? Resources.Load<ItemDatabase>("Database/ItemDatabase");
            var repeater = db?.GetItem("tool_weapon_corn_repeater");

            if (!owns)
            {
                int shrapnelCount = CountMaterial("mat_kernel_shrapnel");
                string matId = shrapnelCount >= 3 ? "mat_kernel_shrapnel" : "mat_mutated_root";
                CraftSetPiece("tool_weapon_corn_repeater", 450, matId, 3);
                owns = OwnsItemInBagOrEquipped("tool_weapon_corn_repeater", out _);
            }

            if (owns && repeater != null && armorState != null)
            {
                armorState.EquipItem(EquipmentSlotType.MainWeapon, repeater, out _);
                RefreshView();
            }
        }

        private void OnClickUpgradeBase()
        {
            if (weaponState != null && weaponState.UpgradeBaseLevel(playerInventory))
            {
                RefreshView();
            }
        }

        private void OnClickUpgradeArmor()
        {
            if (armorState != null && armorState.UpgradeArmor())
            {
                RefreshView();
            }
        }

        private void OnClickInspectPath(SpecializationPath path)
        {
            inspectedPath = path;
            RefreshView();
        }

        private void OnClickCommitInspectedPath()
        {
            if (weaponState == null) return;
            if (weaponState.SelectSpecialization(inspectedPath))
            {
                // Automatically unlock Node 1 if the player can afford it
                weaponState.UpgradeActiveSpecialization(playerInventory);
                RefreshView();
            }
        }

        private void OnClickAdvanceSpecialization()
        {
            if (weaponState == null) return;
            if (weaponState.activeSpecialization != SpecializationPath.None && inspectedPath != weaponState.activeSpecialization)
            {
                inspectedPath = weaponState.activeSpecialization;
                RefreshView();
                return;
            }

            if (weaponState.UpgradeActiveSpecialization(playerInventory))
            {
                RefreshView();
            }
        }

        private void OnClickArmNightOverdrive()
        {
            if (FeaturesCombat.NightBrawlManager.Instance != null &&
                FeaturesCombat.NightBrawlManager.Instance.TryActivateNightOverdrive(playerInventory))
            {
                RefreshView();
            }
            else
            {
                var fct = ServiceLocator.Resolve<IFloatingTextService>();
                fct?.SpawnText(transform.position + Vector3.up * 1.5f, "Need 2 Monster Materials (or Overdrive already active)!", new Color(1f, 0.4f, 0.4f));
            }
        }

        // =====================================================================
        // PROCEDURAL UI HIERARCHY BUILDER (1240x760 WIDESCREEN TACTICAL BENTO)
        // =====================================================================
        private void BuildUIHierarchyIfNeeded()
        {
            if (panelRoot != null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            panelRoot = new GameObject("WorkbenchRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(transform, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            // 1. Tactical Darkened Backdrop
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(panelRoot.transform, false);
            StretchFull(backdrop.GetComponent<RectTransform>());
            backdrop.GetComponent<Image>().color = ColBackdrop;
            backdrop.GetComponent<Button>().onClick.AddListener(Close);

            // 2. Outer Border Frame (1244x764) & Main Dialog Window (1240x760)
            var borderFrame = CreatePanel("DialogBorder", panelRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ColWindowBorder);
            borderFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(1244f, 764f);

            var dialog = CreatePanel("Dialog", borderFrame.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ColWindowBg);
            dialog.GetComponent<RectTransform>().sizeDelta = new Vector2(1240f, 760f);

            // 3. Top Command Header Bar (0.90 to 1.00)
            var headerBar = CreatePanel("HeaderBar", dialog.transform, new Vector2(0f, 0.905f), new Vector2(1f, 1f), ColHeaderBg);
            CreatePanel("HeaderAccentLine", headerBar.transform, new Vector2(0f, 0f), new Vector2(1f, 0.03f), ColAccentAmber);

            var titleTmp = CreateLabel("Title", headerBar.transform, new Vector2(0.025f, 0.12f), new Vector2(0.36f, 0.90f),
                "<size=20><b>HOMESTEAD TACTICAL FORGE</b></size>\n<size=11><color=#8A94A6>GARAGE WORKBENCH // CALIBRATION & DOCTRINE SYSTEM</color></size>",
                14f, TextAlignmentOptions.MidlineLeft);
            titleTmp.richText = true;

            materialLedgerText = CreateLabel("MaterialLedger", headerBar.transform, new Vector2(0.37f, 0.12f), new Vector2(0.74f, 0.90f),
                "", 12.5f, TextAlignmentOptions.Center);

            goldText = CreateLabel("GoldText", headerBar.transform, new Vector2(0.75f, 0.12f), new Vector2(0.92f, 0.90f),
                "GOLD: 0", 14f, TextAlignmentOptions.MidlineRight);

            var closeBtnObj = CreateButton(headerBar.transform, new Vector2(0.935f, 0.20f), new Vector2(0.985f, 0.82f), "<b>ESC ✕</b>", Close, ColAccentCrimson, 13f);
            closeBtnObj.name = "CloseX";

            // 4. Primary Navigation Strip (4 Tactical Module Tabs: 0.825 to 0.895)
            tabWeaponButton = CreateNavTab(dialog.transform, new Vector2(0.020f, 0.825f), new Vector2(0.255f, 0.892f),
                "<b>01 // WEAPONRY</b>\n<size=11>Forging & 6-Node Mastery</size>",
                () => SetTab(WorkbenchTab.Weapon), out tabWeaponButtonText, out tabWeaponIndicator);

            tabArmorButton = CreateNavTab(dialog.transform, new Vector2(0.265f, 0.825f), new Vector2(0.495f, 0.892f),
                "<b>02 // ARMOR SETS</b>\n<size=11>Plating & 5-Piece Combat Set</size>",
                () => SetTab(WorkbenchTab.Armor), out tabArmorButtonText, out tabArmorIndicator);

            tabFarmlandButton = CreateNavTab(dialog.transform, new Vector2(0.505f, 0.825f), new Vector2(0.735f, 0.892f),
                "<b>03 // LAND DEEDS</b>\n<size=11>Arable Plots Expansion (6 ➔ 16)</size>",
                () => SetTab(WorkbenchTab.Farmland), out tabFarmlandButtonText, out tabFarmlandIndicator);

            tabOverdriveButton = CreateNavTab(dialog.transform, new Vector2(0.745f, 0.825f), new Vector2(0.980f, 0.892f),
                "<b>04 // BIO-REACTOR</b>\n<size=11>Nocturnal Growth Overdrive</size>",
                () => SetTab(WorkbenchTab.Overdrive), out tabOverdriveButtonText, out tabOverdriveIndicator);

            // =================================================================
            // TAB 01 CONTENT: WEAPONRY & DOCTRINE TREE (Left 38% / Right 59%)
            // =================================================================
            weaponTabContent = CreateContainer("WeaponTabContent", dialog.transform, new Vector2(0.025f, 0.03f), new Vector2(0.975f, 0.805f));

            // Left Column: Weapon Calibration Card (Top) + Arsenal Loadout Foundry (Bottom)
            var wpLeftTop = CreateCard("WeaponCalibrationCard", weaponTabContent.transform, new Vector2(0f, 0.45f), new Vector2(0.385f, 1f));
            CreateLabel("SectionTag", wpLeftTop.transform, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.96f),
                "<color=#F5AE2E><b>PRIMARY WEAPON CALIBRATION</b></color>", 12f, TextAlignmentOptions.MidlineLeft);

            var iconFrame = CreatePanel("WeaponIconFrame", wpLeftTop.transform, new Vector2(0.05f, 0.50f), new Vector2(0.27f, 0.84f), new Color(0.05f, 0.06f, 0.08f, 1f));
            var iconInner = new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
            iconInner.transform.SetParent(iconFrame.transform, false);
            SetAnchors(iconInner.GetComponent<RectTransform>(), new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            weaponIconImage = iconInner.GetComponent<Image>();
            weaponIconImage.preserveAspect = true;

            weaponPipBarText = CreateLabel("WeaponPipBar", wpLeftTop.transform, new Vector2(0.30f, 0.74f), new Vector2(0.96f, 0.85f),
                "", 12.5f, TextAlignmentOptions.MidlineLeft);

            weaponStatsText = CreateLabel("WeaponStats", wpLeftTop.transform, new Vector2(0.30f, 0.34f), new Vector2(0.96f, 0.73f),
                "", 13f, TextAlignmentOptions.TopLeft);

            var upgBtnObj = CreateButton(wpLeftTop.transform, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.26f),
                "FORGE WEAPON UPGRADE", OnClickUpgradeBase, ColBtnPrimary, 13f);
            upgradeBaseButton = upgBtnObj.GetComponent<Button>();
            upgradeBaseButtonText = upgBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Left Column Bottom: Arsenal Loadout & Corn Cob Repeater Foundry
            var wpLeftBot = CreateCard("ArsenalFoundryCard", weaponTabContent.transform, new Vector2(0f, 0f), new Vector2(0.385f, 0.42f));
            CreateLabel("ArsenalHeader", wpLeftBot.transform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f),
                "<color=#4FC3F7><b>ARSENAL LOADOUT & RANGED FOUNDRY</b></color>", 12f, TextAlignmentOptions.MidlineLeft);

            arsenalStatusText = CreateLabel("ArsenalStatus", wpLeftBot.transform, new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.80f),
                "", 12f, TextAlignmentOptions.TopLeft);

            var eqCleaverObj = CreateButton(wpLeftBot.transform, new Vector2(0.05f, 0.08f), new Vector2(0.48f, 0.32f),
                "EQUIP ROOT CLEAVER", OnClickEquipRootCleaver, ColBtnSecondary, 11.5f);
            equipCleaverButton = eqCleaverObj.GetComponent<Button>();
            equipCleaverButtonText = eqCleaverObj.GetComponentInChildren<TextMeshProUGUI>();

            var repBtnObj = CreateButton(wpLeftBot.transform, new Vector2(0.51f, 0.08f), new Vector2(0.95f, 0.32f),
                "FORGE CORN COB REPEATER", OnClickCraftOrEquipRepeater, ColAccentAmber, 11.5f);
            craftOrEquipRepeaterButton = repBtnObj.GetComponent<Button>();
            craftOrEquipRepeaterButtonText = repBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Right Column: 6-Node Seed Doctrine Specialization Tree Inspector
            var wpRight = CreateCard("SpecializationMatrixCard", weaponTabContent.transform, new Vector2(0.405f, 0f), new Vector2(1f, 1f));

            specializationHeader = CreateLabel("SpecHeader", wpRight.transform, new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.98f),
                "<b>SEED DOCTRINE SPECIALIZATION MATRIX</b>", 14f, TextAlignmentOptions.MidlineLeft);

            specializationSubHeader = CreateLabel("SpecSubHeader", wpRight.transform, new Vector2(0.03f, 0.865f), new Vector2(0.97f, 0.915f),
                "", 11.5f, TextAlignmentOptions.MidlineLeft);

            // 3 Doctrine Path Selector Tabs (Horizontal Row: 0.755 to 0.855)
            var spBtnObj = CreateButton(wpRight.transform, new Vector2(0.03f, 0.755f), new Vector2(0.335f, 0.855f),
                "Sweet Potato", () => OnClickInspectPath(SpecializationPath.SweetPotato), ColTabInactive, 12f);
            sweetPotatoButton = spBtnObj.GetComponent<Button>();
            sweetPotatoButtonText = spBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            var taroBtnObj = CreateButton(wpRight.transform, new Vector2(0.347f, 0.755f), new Vector2(0.652f, 0.855f),
                "Taro", () => OnClickInspectPath(SpecializationPath.Taro), ColTabInactive, 12f);
            taroButton = taroBtnObj.GetComponent<Button>();
            taroButtonText = taroBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            var cornBtnObj = CreateButton(wpRight.transform, new Vector2(0.665f, 0.755f), new Vector2(0.97f, 0.855f),
                "Corn", () => OnClickInspectPath(SpecializationPath.Corn), ColTabInactive, 12f);
            cornButton = cornBtnObj.GetComponent<Button>();
            cornButtonText = cornBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // 6-Node Bento Grid (2 Columns x 3 Rows: 0.16 to 0.73)
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int row = i / 2; // 0 = top, 1 = mid, 2 = bottom

                float xMin = col == 0 ? 0.03f : 0.51f;
                float xMax = col == 0 ? 0.49f : 0.97f;
                float yMax = 0.73f - (row * 0.19f);
                float yMin = yMax - 0.172f;

                var nodeCard = CreatePanel($"NodeCard_{i + 1}", wpRight.transform, new Vector2(xMin, yMin), new Vector2(xMax, yMax), ColCardSubtle);
                nodeCardBackgrounds[i] = nodeCard.GetComponent<Image>();

                var accent = CreatePanel("AccentBar", nodeCard.transform, new Vector2(0f, 0f), new Vector2(0.018f, 1f), ColAccentAmber);
                nodeAccentBars[i] = accent.GetComponent<Image>();

                nodeCardTexts[i] = CreateLabel("NodeText", nodeCard.transform, new Vector2(0.045f, 0.06f), new Vector2(0.97f, 0.94f),
                    "", 11.5f, TextAlignmentOptions.MidlineLeft);
            }

            // Bottom Action Buttons for Doctrine Tree (0.03 to 0.13)
            var commitObj = CreateButton(wpRight.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.13f),
                "COMMIT TO DOCTRINE PATH", OnClickCommitInspectedPath, ColBtnPrimary, 13.5f);
            commitPathButton = commitObj.GetComponent<Button>();
            commitPathButtonText = commitObj.GetComponentInChildren<TextMeshProUGUI>();

            var advObj = CreateButton(wpRight.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.13f),
                "UNLOCK NEXT DOCTRINE NODE", OnClickAdvanceSpecialization, ColBtnPrimary, 13.5f);
            advanceSpecButton = advObj.GetComponent<Button>();
            advanceSpecButtonText = advObj.GetComponentInChildren<TextMeshProUGUI>();
            advanceSpecButton.gameObject.SetActive(false);

            // =================================================================
            // TAB 02 CONTENT: ARMOR & COMBAT SETS (Left 38% / Right 59%)
            // =================================================================
            armorTabContent = CreateContainer("ArmorTabContent", dialog.transform, new Vector2(0.025f, 0.03f), new Vector2(0.975f, 0.805f));

            var arLeftTop = CreateCard("ArmorCalibrationCard", armorTabContent.transform, new Vector2(0f, 0.48f), new Vector2(0.385f, 1f));
            CreateLabel("ArmorTag", arLeftTop.transform, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f),
                "<color=#3DD5F3><b>REINFORCED HOMESTEAD ARMOR</b></color>", 12f, TextAlignmentOptions.MidlineLeft);

            armorPipBarText = CreateLabel("ArmorPipBar", arLeftTop.transform, new Vector2(0.05f, 0.71f), new Vector2(0.95f, 0.83f),
                "", 12.5f, TextAlignmentOptions.MidlineLeft);

            armorStatsText = CreateLabel("ArmorStats", arLeftTop.transform, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.70f),
                "", 13f, TextAlignmentOptions.TopLeft);

            var upgArmObj = CreateButton(arLeftTop.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.28f),
                "UPGRADE ARMOR PLATING", OnClickUpgradeArmor, ColBtnPrimary, 13f);
            upgradeArmorButton = upgArmObj.GetComponent<Button>();
            upgradeArmorButtonText = upgArmObj.GetComponentInChildren<TextMeshProUGUI>();

            var arLeftBot = CreateCard("SetSynergyCard", armorTabContent.transform, new Vector2(0f, 0f), new Vector2(0.385f, 0.45f));
            setSynergyMatrixText = CreateLabel("SetSynergyText", arLeftBot.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f),
                "", 12f, TextAlignmentOptions.TopLeft);

            // Right Column: 5-Piece Sweet Potato Combat Set Blueprint Matrix
            var arRight = CreateCard("SetBlueprintMatrixCard", armorTabContent.transform, new Vector2(0.405f, 0f), new Vector2(1f, 1f));
            setCraftingContainer = arRight.transform;

            CreateLabel("BlueprintHeader", arRight.transform, new Vector2(0.03f, 0.91f), new Vector2(0.97f, 0.98f),
                "<b>SWEET POTATO COMBAT SET // FORGE BLUEPRINTS (5 PIECES)</b>", 14f, TextAlignmentOptions.MidlineLeft);
            CreateLabel("BlueprintSubHeader", arRight.transform, new Vector2(0.03f, 0.855f), new Vector2(0.97f, 0.905f),
                "<color=#B0B8C4>Craft set pieces using Mutated Roots dropped by Tuber Maws. Crafted pieces can be equipped in 1 click or via Tab Character Sheet.</color>",
                11.5f, TextAlignmentOptions.MidlineLeft);

            InitBlueprintData();
            for (int i = 0; i < setBlueprints.Length; i++)
            {
                int idx = i;
                float yMax = 0.83f - (i * 0.16f);
                float yMin = yMax - 0.142f;

                var bpRow = CreatePanel($"BlueprintRow_{i}", arRight.transform, new Vector2(0.03f, yMin), new Vector2(0.97f, yMax), ColCardSubtle);
                CreatePanel("LeftAccent", bpRow.transform, new Vector2(0f, 0f), new Vector2(0.012f, 1f), ColAccentViolet);

                var badgeObj = CreatePanel("StatusBadge", bpRow.transform, new Vector2(0.56f, 0.56f), new Vector2(0.71f, 0.88f), ColTabInactive);
                setBlueprints[i].StatusBadgeBg = badgeObj.GetComponent<Image>();
                setBlueprints[i].StatusBadgeText = CreateLabel("BadgeTxt", badgeObj.transform, Vector2.zero, Vector2.one,
                    "UNFORGED", 10.5f, TextAlignmentOptions.Center);

                setBlueprints[i].CardText = CreateLabel("BlueprintInfo", bpRow.transform, new Vector2(0.03f, 0.08f), new Vector2(0.55f, 0.92f),
                    "", 12f, TextAlignmentOptions.MidlineLeft);

                var actBtnObj = CreateButton(bpRow.transform, new Vector2(0.73f, 0.16f), new Vector2(0.98f, 0.84f),
                    "CRAFT & EQUIP", () => OnClickBlueprintAction(idx), ColBtnPrimary, 12f);
                setBlueprints[i].ActionButton = actBtnObj.GetComponent<Button>();
                setBlueprints[i].ActionButtonText = actBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            }

            armorTabContent.SetActive(false);

            // =================================================================
            // TAB 03 CONTENT: FARMLAND EXPANSION & LAND DEEDS
            // =================================================================
            farmlandTabContent = CreateContainer("FarmlandTabContent", dialog.transform, new Vector2(0.025f, 0.03f), new Vector2(0.975f, 0.805f));

            var flBanner = CreatePanel("FarmlandBanner", farmlandTabContent.transform, new Vector2(0f, 0.78f), new Vector2(1f, 1f), ColCardBg);
            CreatePanel("FlAccent", flBanner.transform, new Vector2(0f, 0f), new Vector2(0.008f, 1f), ColAccentEmerald);
            farmlandStatusBannerText = CreateLabel("FlBannerText", flBanner.transform, new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.90f),
                "", 14f, TextAlignmentOptions.MidlineLeft);

            var flBodyCard = CreateCard("FarmlandTelemetryCard", farmlandTabContent.transform, new Vector2(0f, 0.18f), new Vector2(1f, 0.75f));
            farmlandTelemetryText = CreateLabel("FlTelemetryText", flBodyCard.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                "", 14f, TextAlignmentOptions.MidlineLeft);

            var expandBtnObj = CreateButton(farmlandTabContent.transform, new Vector2(0f, 0.01f), new Vector2(1f, 0.14f),
                "PURCHASE LAND DEED (+2 PLOTS)", OnClickExpandFarmland, ColBtnPrimary, 15f);
            expandFarmlandButton = expandBtnObj.GetComponent<Button>();
            expandFarmlandButtonText = expandBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            farmlandTabContent.SetActive(false);

            // =================================================================
            // TAB 04 CONTENT: BIO-CATALYST NIGHT OVERDRIVE REACTOR
            // =================================================================
            overdriveTabContent = CreateContainer("OverdriveTabContent", dialog.transform, new Vector2(0.025f, 0.03f), new Vector2(0.975f, 0.805f));

            var odBanner = CreatePanel("OverdriveBanner", overdriveTabContent.transform, new Vector2(0f, 0.78f), new Vector2(1f, 1f), ColCardBg);
            overdriveStatusBannerBg = odBanner.GetComponent<Image>();
            CreatePanel("OdAccent", odBanner.transform, new Vector2(0f, 0f), new Vector2(0.008f, 1f), ColAccentEmerald);
            overdriveStatusBannerText = CreateLabel("OdBannerText", odBanner.transform, new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.90f),
                "", 14f, TextAlignmentOptions.MidlineLeft);

            var odBodyCard = CreateCard("OverdriveTelemetryCard", overdriveTabContent.transform, new Vector2(0f, 0.18f), new Vector2(1f, 0.75f));
            overdriveTelemetryText = CreateLabel("OdTelemetryText", odBodyCard.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f),
                "", 14f, TextAlignmentOptions.MidlineLeft);

            var odArmBtnObj = CreateButton(overdriveTabContent.transform, new Vector2(0f, 0.01f), new Vector2(1f, 0.14f),
                "INJECT 2x MONSTER BIO-MATERIALS & ARM NIGHT OVERDRIVE", OnClickArmNightOverdrive, new Color(0.85f, 0.38f, 0.12f, 1f), 15f);
            armOverdriveButton = odArmBtnObj.GetComponent<Button>();
            armOverdriveButtonText = odArmBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            overdriveTabContent.SetActive(false);
        }

        private void InitBlueprintData()
        {
            setBlueprints[0] = new SetBlueprintBinding
            {
                ItemId = "item_burrow_hood",
                SlotLabel = "HELMET",
                DisplayName = "Burrow Hood",
                StatDesc = "Sweet Potato Set (1/5) — Lightweight nocturnal scout hood.",
                GoldCost = 3000,
                MatId = "mat_mutated_root",
                MatCount = 5,
                SlotType = EquipmentSlotType.Helmet
            };
            setBlueprints[1] = new SetBlueprintBinding
            {
                ItemId = "item_swift_root_armor",
                SlotLabel = "ARMOR",
                DisplayName = "Swift Root Armor",
                StatDesc = "Sweet Potato Set (2/5) — Fibrous root-weave cuirass.",
                GoldCost = 6000,
                MatId = "mat_mutated_root",
                MatCount = 10,
                SlotType = EquipmentSlotType.Chest
            };
            setBlueprints[2] = new SetBlueprintBinding
            {
                ItemId = "item_fang_grips",
                SlotLabel = "GLOVES",
                DisplayName = "Fang Grips",
                StatDesc = "Sweet Potato Set (3/5) — Reinforced tuber-fang gauntlets.",
                GoldCost = 4000,
                MatId = "mat_mutated_root",
                MatCount = 6,
                SlotType = EquipmentSlotType.Gloves
            };
            setBlueprints[3] = new SetBlueprintBinding
            {
                ItemId = "item_underground_striders",
                SlotLabel = "BOOTS",
                DisplayName = "Underground Striders",
                StatDesc = "Sweet Potato Set (4/5) — High-traction burrower greaves.",
                GoldCost = 4000,
                MatId = "mat_mutated_root",
                MatCount = 6,
                SlotType = EquipmentSlotType.Boots
            };
            setBlueprints[4] = new SetBlueprintBinding
            {
                ItemId = "item_sweet_potato_heart",
                SlotLabel = "ACCESSORY",
                DisplayName = "Sweet Potato Heart",
                StatDesc = "Sweet Potato Set (5/5) — Mutagenic core talisman.",
                GoldCost = 8000,
                MatId = "mat_mutated_root",
                MatCount = 8,
                SlotType = EquipmentSlotType.Necklace
            };
        }

        // =====================================================================
        // CLEAN UI PRIMITIVE HELPERS
        // =====================================================================
        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateContainer(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateCard(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var border = CreatePanel(name + "_Border", parent, anchorMin, anchorMax, ColCardBorder);
            var inner = CreatePanel(name, border.transform, Vector2.zero, Vector2.one, ColCardBg);
            var rt = inner.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);
            return inner;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, float fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = ColTextPrimary;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Button CreateNavTab(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, Action onClick, out TextMeshProUGUI labelOut, out Image indicatorOut)
        {
            var go = CreateButton(parent, anchorMin, anchorMax, text, onClick, ColTabInactive, 13f);
            var btn = go.GetComponent<Button>();
            labelOut = go.GetComponentInChildren<TextMeshProUGUI>();

            var ind = CreatePanel("ActiveIndicator", go.transform, new Vector2(0f, 0f), new Vector2(1f, 0.08f), ColAccentAmber);
            indicatorOut = ind.GetComponent<Image>();
            return btn;
        }

        private static GameObject CreateButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, Action onClick, Color bgColor, float fontSize = 13f)
        {
            var btnObj = new GameObject("Btn_" + text.Substring(0, Mathf.Min(12, text.Length)).Replace(" ", ""), typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            SetAnchors(btnObj.GetComponent<RectTransform>(), anchorMin, anchorMax);

            var img = btnObj.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8f, 2f);
            trt.offsetMax = new Vector2(-8f, -2f);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btnObj;
        }
    }
}
