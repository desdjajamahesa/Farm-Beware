using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FeaturesEconomy;

namespace FeaturesWorkbench.UI
{
    /// <summary>
    /// Modal UI untuk meja kerja upgrade senjata di garasi.
    /// Mematuhi standar resolusi 1920x1080 dan prioritas stack tombol ESC.
    /// </summary>
    public class WorkbenchUI : MonoBehaviour
    {
        public static WorkbenchUI Instance { get; private set; }

        [Header("State")]
        private bool isOpen = false;
        public bool IsOpen => isOpen;

        [Header("UI Elements")]
        private GameObject panelRoot;
        private TextMeshProUGUI goldText;
        private Image weaponIconImage;
        private TextMeshProUGUI weaponStatsText;
        private Button upgradeBaseButton;
        private TextMeshProUGUI upgradeBaseButtonText;

        // Path Buttons & Labels
        private Button sweetPotatoButton;
        private TextMeshProUGUI sweetPotatoButtonText;
        private Button taroButton;
        private TextMeshProUGUI taroButtonText;
        private Button cornButton;
        private TextMeshProUGUI cornButtonText;

        private PlayerControl playerControl;
        private InventoryComponent playerInventory;
        private PlayerWeaponUpgradeState upgradeState;

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
                playerControl = p.GetComponent<PlayerControl>();
                playerInventory = p.GetComponent<InventoryComponent>();
                upgradeState = p.GetComponent<PlayerWeaponUpgradeState>() ?? p.AddComponent<PlayerWeaponUpgradeState>();
            }
        }

        private void OnEnable()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnGoldChanged += HandleGoldChanged;
            }
        }

        private void OnDisable()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnGoldChanged -= HandleGoldChanged;
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

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (playerControl != null)
            {
                playerControl.isInputLocked = true;
                playerControl.StopMovement();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshView();
        }

        public void Close()
        {
            isOpen = false;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (playerControl != null)
            {
                playerControl.isInputLocked = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void RefreshView()
        {
            if (upgradeState == null) FindPlayerReferences();
            if (upgradeState == null) return;

            bool hasDummySword = upgradeState.HasDummySword(playerInventory);

            // 1. Gold balance
            int currentGold = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;
            if (goldText != null)
            {
                goldText.text = $"Gold: <color=#FFD700>{currentGold:N0}</color>";
            }

            // 2. Weapon Icon & Stats
            var db = ItemDatabase.Instance;
            var swordItem = db?.GetItem("dummysword");
            if (weaponIconImage != null && swordItem != null && swordItem.itemIcon != null)
            {
                weaponIconImage.sprite = swordItem.itemIcon;
                weaponIconImage.color = hasDummySword ? Color.white : new Color(0.40f, 0.40f, 0.40f, 0.55f);
            }

            if (weaponStatsText != null)
            {
                string swordStatus = hasDummySword
                    ? "<color=#4CAF50>Owned</color>"
                    : "<color=#FF5252>Not Found in Bag</color>";

                string perks = "";
                if (upgradeState.sweetPotatoPathUnlocked) perks += "\n • <color=#B388FF>Sweet Potato Path:</color> +20% Atk Spd, +10% Move Spd";
                if (upgradeState.taroPathUnlocked) perks += "\n • <color=#8D6E63>Taro Path:</color> +50% Knockback, +15 Armor";
                if (upgradeState.cornPathUnlocked) perks += "\n • <color=#FFD54F>Corn Path:</color> +25% Attack Damage";
                if (string.IsNullOrEmpty(perks)) perks = "\n • No Special Evolution Paths Unlocked";

                weaponStatsText.text = $"<b>Dummy Sword</b> (Lv. {upgradeState.weaponLevel} / 3)   [Status: {swordStatus}]\n" +
                                       $"Base Damage: <b>{upgradeState.baseDamage}</b>  |  Knockback: <b>{upgradeState.baseKnockback:F1}</b>\n" +
                                       $"<b>Special Perks:</b>{perks}";
            }

            // 3. Base Upgrade Button
            if (upgradeBaseButton != null && upgradeBaseButtonText != null)
            {
                if (upgradeState.weaponLevel >= 3)
                {
                    upgradeBaseButtonText.text = "MAX LEVEL REACHED";
                    upgradeBaseButton.interactable = false;
                }
                else if (!hasDummySword)
                {
                    upgradeBaseButtonText.text = $"<color=#FFAAAA>Dummy Sword Required</color> (To Lv.{upgradeState.weaponLevel + 1})";
                    upgradeBaseButton.interactable = false;
                }
                else
                {
                    int cost = upgradeState.GetNextUpgradeCost();
                    bool canAfford = currentGold >= cost;
                    if (canAfford)
                    {
                        upgradeBaseButtonText.text = $"Upgrade to Lv.{upgradeState.weaponLevel + 1} ({cost:N0} Gold)";
                        upgradeBaseButton.interactable = true;
                    }
                    else
                    {
                        upgradeBaseButtonText.text = $"Upgrade to Lv.{upgradeState.weaponLevel + 1} ({cost:N0} Gold) - <color=#FFAAAA>Need {cost - currentGold:N0} More</color>";
                        upgradeBaseButton.interactable = false;
                    }
                }
            }

            // 4. Special Paths
            // Sweet Potato Path
            if (sweetPotatoButton != null && sweetPotatoButtonText != null)
            {
                if (upgradeState.sweetPotatoPathUnlocked)
                {
                    sweetPotatoButtonText.text = "ACTIVE (UNLOCKED)";
                    sweetPotatoButton.interactable = false;
                }
                else if (!hasDummySword)
                {
                    sweetPotatoButtonText.text = "Sweet Potato Path (<color=#FFAAAA>Dummy Sword Required</color>)";
                    sweetPotatoButton.interactable = false;
                }
                else
                {
                    var mat = db?.GetItem("mat_mutated_root");
                    int count = playerInventory != null && mat != null ? playerInventory.CountItem(mat) : 0;
                    bool canAfford = currentGold >= 300 && count >= 1;
                    string matName = mat != null ? mat.itemName : "Mutated Root";
                    sweetPotatoButtonText.text = $"Unlock Sweet Potato (300G + 1 {matName}) [{count}/1]";
                    sweetPotatoButton.interactable = canAfford;
                }
            }

            // Taro Path
            if (taroButton != null && taroButtonText != null)
            {
                if (upgradeState.taroPathUnlocked)
                {
                    taroButtonText.text = "ACTIVE (UNLOCKED)";
                    taroButton.interactable = false;
                }
                else if (!hasDummySword)
                {
                    taroButtonText.text = "Taro Path (<color=#FFAAAA>Dummy Sword Required</color>)";
                    taroButton.interactable = false;
                }
                else
                {
                    var mat = db?.GetItem("mat_hardened_root");
                    int count = playerInventory != null && mat != null ? playerInventory.CountItem(mat) : 0;
                    bool canAfford = currentGold >= 300 && count >= 1;
                    string matName = mat != null ? mat.itemName : "Hardened Root";
                    taroButtonText.text = $"Unlock Taro (300G + 1 {matName}) [{count}/1]";
                    taroButton.interactable = canAfford;
                }
            }

            // Corn Path
            if (cornButton != null && cornButtonText != null)
            {
                if (upgradeState.cornPathUnlocked)
                {
                    cornButtonText.text = "ACTIVE (UNLOCKED)";
                    cornButton.interactable = false;
                }
                else if (!hasDummySword)
                {
                    cornButtonText.text = "Corn Path (<color=#FFAAAA>Dummy Sword Required</color>)";
                    cornButton.interactable = false;
                }
                else
                {
                    var mat = db?.GetItem("mat_kernel_shrapnel");
                    int count = playerInventory != null && mat != null ? playerInventory.CountItem(mat) : 0;
                    bool canAfford = currentGold >= 300 && count >= 1;
                    string matName = mat != null ? mat.itemName : "Kernel Shrapnel";
                    cornButtonText.text = $"Unlock Corn (300G + 1 {matName}) [{count}/1]";
                    cornButton.interactable = canAfford;
                }
            }
        }

        private void OnClickUpgradeBase()
        {
            if (upgradeState != null && upgradeState.UpgradeBaseLevel(playerInventory))
            {
                RefreshView();
            }
        }

        private void OnClickUnlockSweetPotato()
        {
            var mat = ItemDatabase.Instance?.GetItem("mat_mutated_root");
            if (upgradeState != null && upgradeState.UnlockSpecialPath("SweetPotato", 300, mat, 1, playerInventory))
            {
                RefreshView();
            }
        }

        private void OnClickUnlockTaro()
        {
            var mat = ItemDatabase.Instance?.GetItem("mat_hardened_root");
            if (upgradeState != null && upgradeState.UnlockSpecialPath("Taro", 300, mat, 1, playerInventory))
            {
                RefreshView();
            }
        }

        private void OnClickUnlockCorn()
        {
            var mat = ItemDatabase.Instance?.GetItem("mat_kernel_shrapnel");
            if (upgradeState != null && upgradeState.UnlockSpecialPath("Corn", 300, mat, 1, playerInventory))
            {
                RefreshView();
            }
        }

        private void BuildUIHierarchyIfNeeded()
        {
            if (panelRoot != null) return;

            // Clear any stray legacy children under transform
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Panel Root
            panelRoot = new GameObject("WorkbenchRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(transform, false);
            var rootRt = panelRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Dark Backdrop
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(panelRoot.transform, false);
            var bdRt = backdrop.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            backdrop.GetComponent<Button>().onClick.AddListener(Close);

            // Center Dialog Container (Enlarged to 740x660 for superior readability)
            var dialog = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(panelRoot.transform, false);
            var diagRt = dialog.GetComponent<RectTransform>();
            diagRt.anchorMin = new Vector2(0.5f, 0.5f);
            diagRt.anchorMax = new Vector2(0.5f, 0.5f);
            diagRt.sizeDelta = new Vector2(740f, 660f);
            dialog.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.15f, 0.98f);

            // Title
            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(dialog.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.91f);
            titleRt.anchorMax = new Vector2(0.60f, 0.985f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>GARAGE WORKBENCH</b> (Weapon Upgrade)";
            titleTmp.fontSize = 24f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Gold Display
            var goldObj = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goldObj.transform.SetParent(dialog.transform, false);
            var goldRt = goldObj.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0.60f, 0.91f);
            goldRt.anchorMax = new Vector2(0.90f, 0.985f);
            goldRt.offsetMin = Vector2.zero; goldRt.offsetMax = Vector2.zero;
            goldText = goldObj.GetComponent<TextMeshProUGUI>();
            goldText.text = "Gold: 0";
            goldText.fontSize = 20f;
            goldText.fontStyle = FontStyles.Bold;
            goldText.alignment = TextAlignmentOptions.MidlineRight;

            // Close X Button
            var closeX = new GameObject("CloseX", typeof(RectTransform), typeof(Button), typeof(Image));
            closeX.transform.SetParent(dialog.transform, false);
            var cxRt = closeX.GetComponent<RectTransform>();
            cxRt.anchorMin = new Vector2(0.92f, 0.915f);
            cxRt.anchorMax = new Vector2(0.965f, 0.98f);
            cxRt.offsetMin = Vector2.zero; cxRt.offsetMax = Vector2.zero;
            closeX.GetComponent<Image>().color = new Color(0.78f, 0.22f, 0.22f);
            closeX.GetComponent<Button>().onClick.AddListener(Close);

            var xTextObj = new GameObject("XText", typeof(RectTransform), typeof(TextMeshProUGUI));
            xTextObj.transform.SetParent(closeX.transform, false);
            var xRt = xTextObj.GetComponent<RectTransform>();
            xRt.anchorMin = Vector2.zero;
            xRt.anchorMax = Vector2.one;
            xRt.offsetMin = Vector2.zero;
            xRt.offsetMax = Vector2.zero;
            var xTmp = xTextObj.GetComponent<TextMeshProUGUI>();
            xTmp.text = "✕";
            xTmp.fontSize = 18f;
            xTmp.fontStyle = FontStyles.Bold;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;

            // Horizontal Separator
            var separator = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            separator.transform.SetParent(dialog.transform, false);
            var sepRt = separator.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0.05f, 0.895f);
            sepRt.anchorMax = new Vector2(0.95f, 0.898f);
            sepRt.offsetMin = Vector2.zero; sepRt.offsetMax = Vector2.zero;
            separator.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            // Weapon Icon Frame (Left of stats)
            var iconFrame = new GameObject("WeaponIconFrame", typeof(RectTransform), typeof(Image));
            iconFrame.transform.SetParent(dialog.transform, false);
            var ifRt = iconFrame.GetComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.05f, 0.635f);
            ifRt.anchorMax = new Vector2(0.22f, 0.875f);
            ifRt.offsetMin = Vector2.zero; ifRt.offsetMax = Vector2.zero;
            iconFrame.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.10f, 0.95f);

            var iconInner = new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
            iconInner.transform.SetParent(iconFrame.transform, false);
            var inRt = iconInner.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0.08f, 0.08f);
            inRt.anchorMax = new Vector2(0.92f, 0.92f);
            inRt.offsetMin = Vector2.zero; inRt.offsetMax = Vector2.zero;
            weaponIconImage = iconInner.GetComponent<Image>();
            weaponIconImage.preserveAspect = true;

            // Weapon Stats Area (Right of icon)
            var statsObj = new GameObject("WeaponStats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(dialog.transform, false);
            var statsRt = statsObj.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.24f, 0.63f);
            statsRt.anchorMax = new Vector2(0.95f, 0.88f);
            statsRt.offsetMin = Vector2.zero; statsRt.offsetMax = Vector2.zero;
            weaponStatsText = statsObj.GetComponent<TextMeshProUGUI>();
            weaponStatsText.fontSize = 17f;
            weaponStatsText.lineSpacing = 12f;
            weaponStatsText.color = new Color(0.92f, 0.92f, 0.92f);

            // Upgrade Base Button
            var upgBaseObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.505f), new Vector2(0.95f, 0.605f), new Color(0.18f, 0.55f, 0.25f), 18f);
            upgradeBaseButton = upgBaseObj.GetComponent<Button>();
            upgradeBaseButtonText = upgBaseObj.GetComponentInChildren<TextMeshProUGUI>();
            upgradeBaseButton.onClick.AddListener(OnClickUpgradeBase);

            // Header Special Paths
            var pathHeader = new GameObject("PathHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            pathHeader.transform.SetParent(dialog.transform, false);
            var phRt = pathHeader.GetComponent<RectTransform>();
            phRt.anchorMin = new Vector2(0.05f, 0.42f);
            phRt.anchorMax = new Vector2(0.95f, 0.485f);
            phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
            var phTmp = pathHeader.GetComponent<TextMeshProUGUI>();
            phTmp.text = "<b>Special Seed Evolution Paths:</b>";
            phTmp.fontSize = 18f;
            phTmp.fontStyle = FontStyles.Bold;
            phTmp.color = new Color(0.98f, 0.85f, 0.35f);
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Sweet Potato Button
            var spBtnObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.295f), new Vector2(0.95f, 0.395f), new Color(0.35f, 0.20f, 0.50f), 16f);
            sweetPotatoButton = spBtnObj.GetComponent<Button>();
            sweetPotatoButtonText = spBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            sweetPotatoButton.onClick.AddListener(OnClickUnlockSweetPotato);

            // Taro Button
            var taroBtnObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.27f), new Color(0.35f, 0.25f, 0.20f), 16f);
            taroButton = taroBtnObj.GetComponent<Button>();
            taroButtonText = taroBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            taroButton.onClick.AddListener(OnClickUnlockTaro);

            // Corn Button
            var cornBtnObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.145f), new Color(0.45f, 0.35f, 0.10f), 16f);
            cornButton = cornBtnObj.GetComponent<Button>();
            cornButtonText = cornBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            cornButton.onClick.AddListener(OnClickUnlockCorn);
        }

        private GameObject CreateButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color btnColor, float fontSize = 16f)
        {
            var btnObj = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            btnObj.GetComponent<Image>().color = btnColor;

            var btn = btnObj.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = btnColor;
            colors.highlightedColor = btnColor * 1.2f;
            colors.pressedColor = btnColor * 0.85f;
            colors.disabledColor = new Color(0.25f, 0.25f, 0.28f, 0.8f);
            btn.colors = colors;

            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-10f, 0f);
            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }
    }
}
