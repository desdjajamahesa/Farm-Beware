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

            // 1. Gold balance
            int currentGold = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;
            if (goldText != null)
            {
                goldText.text = $"Gold: <color=#FFD700>{currentGold}</color>";
            }

            // 2. Weapon Stats
            if (weaponStatsText != null)
            {
                string perks = "";
                if (upgradeState.sweetPotatoPathUnlocked) perks += "\n • <color=#B388FF>Sweet Potato Path:</color> +20% Atk Spd, +10% Move Spd";
                if (upgradeState.taroPathUnlocked) perks += "\n • <color=#8D6E63>Taro Path:</color> +50% Knockback, +15 Armor";
                if (upgradeState.cornPathUnlocked) perks += "\n • <color=#FFD54F>Corn Path:</color> +25% Attack Damage";
                if (string.IsNullOrEmpty(perks)) perks = "\n • No Special Evolution Paths Unlocked";

                weaponStatsText.text = $"<b>Dummy Sword</b> (Lv. {upgradeState.weaponLevel} / 3)\n" +
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
                else
                {
                    int cost = upgradeState.GetNextUpgradeCost();
                    bool canAfford = currentGold >= cost;
                    upgradeBaseButtonText.text = $"Upgrade to Lv.{upgradeState.weaponLevel + 1} ({cost} Gold)";
                    upgradeBaseButton.interactable = canAfford;
                }
            }

            // 4. Special Paths
            var db = ItemDatabase.Instance;

            // Sweet Potato Path
            if (sweetPotatoButton != null && sweetPotatoButtonText != null)
            {
                if (upgradeState.sweetPotatoPathUnlocked)
                {
                    sweetPotatoButtonText.text = "ACTIVE (UNLOCKED)";
                    sweetPotatoButton.interactable = false;
                }
                else
                {
                    var mat = db?.GetItem("mat_mutated_root");
                    int count = playerInventory != null && mat != null ? playerInventory.CountItem(mat) : 0;
                    bool canAfford = currentGold >= 300 && count >= 1;
                    sweetPotatoButtonText.text = $"Unlock (300G + 1 Mutated Root) [{count}/1]";
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
                else
                {
                    var mat = db?.GetItem("mat_hardened_root");
                    int count = playerInventory != null && mat != null ? playerInventory.CountItem(mat) : 0;
                    bool canAfford = currentGold >= 300 && count >= 1;
                    taroButtonText.text = $"Unlock (300G + 1 Hardened Root) [{count}/1]";
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
                else
                {
                    var mat = db?.GetItem("mat_kernel_shrapnel");
                    int count = playerInventory != null && mat != null ? playerInventory.CountItem(mat) : 0;
                    bool canAfford = currentGold >= 300 && count >= 1;
                    cornButtonText.text = $"Unlock (300G + 1 Kernel Shrapnel) [{count}/1]";
                    cornButton.interactable = canAfford;
                }
            }
        }

        private void OnClickUpgradeBase()
        {
            if (upgradeState != null && upgradeState.UpgradeBaseLevel())
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

            // Center Dialog Container
            var dialog = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(panelRoot.transform, false);
            var diagRt = dialog.GetComponent<RectTransform>();
            diagRt.anchorMin = new Vector2(0.5f, 0.5f);
            diagRt.anchorMax = new Vector2(0.5f, 0.5f);
            diagRt.sizeDelta = new Vector2(580f, 520f);
            dialog.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.98f);

            // Title
            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(dialog.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.88f);
            titleRt.anchorMax = new Vector2(0.85f, 0.97f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>GARAGE WORKBENCH</b> (Weapon Upgrade)";
            titleTmp.fontSize = 18f;
            titleTmp.color = Color.white;

            // Gold Display
            var goldObj = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goldObj.transform.SetParent(dialog.transform, false);
            var goldRt = goldObj.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0.60f, 0.88f);
            goldRt.anchorMax = new Vector2(0.92f, 0.97f);
            goldRt.offsetMin = Vector2.zero; goldRt.offsetMax = Vector2.zero;
            goldText = goldObj.GetComponent<TextMeshProUGUI>();
            goldText.text = "Gold: 500";
            goldText.fontSize = 16f;
            goldText.alignment = TextAlignmentOptions.Right;

            // Close X Button
            var closeX = new GameObject("CloseX", typeof(RectTransform), typeof(Button), typeof(Image));
            closeX.transform.SetParent(dialog.transform, false);
            var cxRt = closeX.GetComponent<RectTransform>();
            cxRt.anchorMin = new Vector2(0.93f, 0.89f);
            cxRt.anchorMax = new Vector2(0.98f, 0.97f);
            cxRt.offsetMin = Vector2.zero; cxRt.offsetMax = Vector2.zero;
            closeX.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
            closeX.GetComponent<Button>().onClick.AddListener(Close);

            // Weapon Stats Area
            var statsObj = new GameObject("WeaponStats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(dialog.transform, false);
            var statsRt = statsObj.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.05f, 0.58f);
            statsRt.anchorMax = new Vector2(0.95f, 0.85f);
            statsRt.offsetMin = Vector2.zero; statsRt.offsetMax = Vector2.zero;
            weaponStatsText = statsObj.GetComponent<TextMeshProUGUI>();
            weaponStatsText.fontSize = 14f;
            weaponStatsText.color = new Color(0.9f, 0.9f, 0.9f);

            // Upgrade Base Button
            var upgBaseObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.55f), new Color(0.18f, 0.55f, 0.25f));
            upgradeBaseButton = upgBaseObj.GetComponent<Button>();
            upgradeBaseButtonText = upgBaseObj.GetComponentInChildren<TextMeshProUGUI>();
            upgradeBaseButton.onClick.AddListener(OnClickUpgradeBase);

            // Header Special Paths
            var pathHeader = new GameObject("PathHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            pathHeader.transform.SetParent(dialog.transform, false);
            var phRt = pathHeader.GetComponent<RectTransform>();
            phRt.anchorMin = new Vector2(0.05f, 0.38f);
            phRt.anchorMax = new Vector2(0.95f, 0.44f);
            phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
            var phTmp = pathHeader.GetComponent<TextMeshProUGUI>();
            phTmp.text = "<b>Special Seed Evolution Paths:</b>";
            phTmp.fontSize = 14f;
            phTmp.color = new Color(0.95f, 0.80f, 0.3f);

            // Sweet Potato Button
            var spBtnObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.36f), new Color(0.35f, 0.20f, 0.50f));
            sweetPotatoButton = spBtnObj.GetComponent<Button>();
            sweetPotatoButtonText = spBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            sweetPotatoButton.onClick.AddListener(OnClickUnlockSweetPotato);

            // Taro Button
            var taroBtnObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.25f), new Color(0.35f, 0.25f, 0.20f));
            taroButton = taroBtnObj.GetComponent<Button>();
            taroButtonText = taroBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            taroButton.onClick.AddListener(OnClickUnlockTaro);

            // Corn Button
            var cornBtnObj = CreateButton(dialog.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.14f), new Color(0.45f, 0.35f, 0.10f));
            cornButton = cornBtnObj.GetComponent<Button>();
            cornButtonText = cornBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            cornButton.onClick.AddListener(OnClickUnlockCorn);
        }

        private GameObject CreateButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color btnColor)
        {
            var btnObj = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            btnObj.GetComponent<Image>().color = btnColor;

            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 13f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }
    }
}
