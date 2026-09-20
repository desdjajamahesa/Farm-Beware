using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerUI
{
    /// <summary>
    /// Mengontrol container bar daftar buff aktif di HUD.
    /// Berlangganan langsung ke PlayerBuffManager.OnBuffsChanged untuk merefresh tampilan slot.
    /// Mendukung pembangunan UI prosedural otomatis jika prefab belum di-assign di Inspector.
    /// Memuat ikon tematik secara otomatis dari Resources/Icons/Buffs/ (Hati untuk darah, Listrik untuk stamina, dll).
    /// </summary>
    public class ActiveBuffHUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBuffManager buffManager;
        [SerializeField] private GameObject buffSlotPrefab;
        [SerializeField] private Transform containerTransform;

        [Header("Buff Type Default Icons (Opsional / Override)")]
        [SerializeField] private Sprite healthBuffIcon;
        [SerializeField] private Sprite healthRegenBuffIcon;
        [SerializeField] private Sprite staminaBuffIcon;
        [SerializeField] private Sprite staminaRegenBuffIcon;
        [SerializeField] private Sprite speedBuffIcon;
        [SerializeField] private Sprite attackDamageBuffIcon;
        [SerializeField] private Sprite attackSpeedBuffIcon;

        private static ActiveBuffHUDController instance;
        private readonly List<BuffSlotUI> activeSlots = new List<BuffSlotUI>();

        private void Awake()
        {
            instance = this;

            if (containerTransform == null)
                containerTransform = transform;

            EnsureLayoutGroup();
        }

        private void Start()
        {
            EnsureBuffManagerBound();
            RefreshHUD();
        }

        private void OnEnable()
        {
            EnsureBuffManagerBound();
            if (buffManager != null)
            {
                buffManager.OnBuffsChanged += RefreshHUD;
            }
            RefreshHUD();
        }

        private void OnDisable()
        {
            if (buffManager != null)
            {
                buffManager.OnBuffsChanged -= RefreshHUD;
            }
        }

        private void EnsureBuffManagerBound()
        {
            if (buffManager == null)
            {
                buffManager = FindFirstObjectByType<PlayerBuffManager>();
            }
        }

        private void EnsureLayoutGroup()
        {
            var hlg = containerTransform.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = containerTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            var csf = containerTransform.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = containerTransform.gameObject.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        public void RefreshHUD()
        {
            if (buffManager == null) return;

            var buffs = buffManager.ActiveBuffs;

            // Sesuaikan jumlah slot dengan jumlah buff aktif
            while (activeSlots.Count < buffs.Count)
            {
                activeSlots.Add(CreateSlotInstance());
            }

            for (int i = 0; i < activeSlots.Count; i++)
            {
                if (i < buffs.Count)
                {
                    Sprite icon = GetDefaultIconForType(buffs[i].data.buffType);
                    activeSlots[i].Bind(buffs[i], icon);
                }
                else
                {
                    activeSlots[i].gameObject.SetActive(false);
                }
            }
        }

        private BuffSlotUI CreateSlotInstance()
        {
            if (buffSlotPrefab != null)
            {
                GameObject obj = Instantiate(buffSlotPrefab, containerTransform);
                return obj.GetComponent<BuffSlotUI>();
            }

            // Bangun struktur UI Slot secara prosedural dengan visual rounded slate & rim
            return BuildProceduralSlot(containerTransform);
        }

        public static BuffSlotUI BuildProceduralSlot(Transform parent)
        {
            // Root Slot
            GameObject slotObj = new GameObject("BuffSlot_Item", typeof(RectTransform));
            slotObj.transform.SetParent(parent, false);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(46f, 62f);

            // Container untuk Icon & Frame (44 x 44)
            GameObject frameObj = new GameObject("FrameContainer", typeof(RectTransform));
            frameObj.transform.SetParent(slotObj.transform, false);
            RectTransform frameRt = frameObj.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 1f);
            frameRt.anchorMax = new Vector2(0.5f, 1f);
            frameRt.pivot = new Vector2(0.5f, 1f);
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(44f, 44f);

            // 1. Background Image (Dark slate rounded glass)
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(frameObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Frame_Background");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // 2. Icon Image (Ikon tematik: Hati, Listrik, Sepatu, Pedang, dll)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(frameObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.10f, 0.10f);
            iconRt.anchorMax = new Vector2(0.90f, 0.90f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;

            // 3. Radial Cooldown Overlay (Clockwise dark sweep)
            GameObject radialObj = new GameObject("RadialCooldown", typeof(RectTransform), typeof(Image));
            radialObj.transform.SetParent(frameObj.transform, false);
            RectTransform radialRt = radialObj.GetComponent<RectTransform>();
            radialRt.anchorMin = Vector2.zero;
            radialRt.anchorMax = Vector2.one;
            radialRt.offsetMin = Vector2.zero;
            radialRt.offsetMax = Vector2.zero;
            Image radialImg = radialObj.GetComponent<Image>();
            radialImg.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Cooldown_Overlay");
            radialImg.type = Image.Type.Filled;
            radialImg.fillMethod = Image.FillMethod.Radial360;
            radialImg.fillOrigin = (int)Image.Origin360.Top;
            radialImg.fillClockwise = true;
            radialImg.color = new Color(0f, 0f, 0f, 0.65f);

            // 4. Border Rim (Border rounded menyala sesuai warna buff)
            GameObject borderObj = new GameObject("BorderRim", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(frameObj.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Frame_Border");
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.white;
            borderImg.raycastTarget = false;

            // 5. Timer Text (TMP di bawah frame icon)
            GameObject textObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(slotObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0f);
            textRt.pivot = new Vector2(0.5f, 0f);
            textRt.anchoredPosition = new Vector2(0f, 0f);
            textRt.sizeDelta = new Vector2(46f, 16f);
            TextMeshProUGUI timerTmp = textObj.GetComponent<TextMeshProUGUI>();
            timerTmp.fontSize = 11f;
            timerTmp.fontStyle = FontStyles.Bold;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.color = Color.white;
            timerTmp.enableWordWrapping = false;
            timerTmp.raycastTarget = false;

            BuffSlotUI slotUi = slotObj.AddComponent<BuffSlotUI>();
            slotUi.SetReferences(iconImg, radialImg, bgImg, borderImg, timerTmp);
            return slotUi;
        }

        private Sprite GetDefaultIconForType(BuffType type)
        {
            Sprite icon = type switch
            {
                BuffType.MaxHealthPercent => healthBuffIcon,
                BuffType.HealthRegenTick or BuffType.HealthRegenPercent => healthRegenBuffIcon != null ? healthRegenBuffIcon : healthBuffIcon,
                BuffType.MaxStaminaPercent => staminaBuffIcon,
                BuffType.StaminaRegenPercent => staminaRegenBuffIcon != null ? staminaRegenBuffIcon : staminaBuffIcon,
                BuffType.MoveSpeedPercent => speedBuffIcon,
                BuffType.AttackDamagePercent => attackDamageBuffIcon,
                BuffType.AttackSpeedPercent => attackSpeedBuffIcon,
                _ => null
            };

            if (icon != null) return icon;
            return GetDefaultBuffIcon(type);
        }

        /// <summary>
        /// Mengambil ikon default bawaan berdasarkan BuffType langsung dari Resources/Icons/Buffs/.
        /// </summary>
        public static Sprite GetDefaultBuffIcon(BuffType type)
        {
            if (instance != null)
            {
                Sprite instIcon = type switch
                {
                    BuffType.MaxHealthPercent => instance.healthBuffIcon,
                    BuffType.HealthRegenTick or BuffType.HealthRegenPercent => instance.healthRegenBuffIcon != null ? instance.healthRegenBuffIcon : instance.healthBuffIcon,
                    BuffType.MaxStaminaPercent => instance.staminaBuffIcon,
                    BuffType.StaminaRegenPercent => instance.staminaRegenBuffIcon != null ? instance.staminaRegenBuffIcon : instance.staminaBuffIcon,
                    BuffType.MoveSpeedPercent => instance.speedBuffIcon,
                    BuffType.AttackDamagePercent => instance.attackDamageBuffIcon,
                    BuffType.AttackSpeedPercent => instance.attackSpeedBuffIcon,
                    _ => null
                };
                if (instIcon != null) return instIcon;
            }

            return type switch
            {
                BuffType.MaxHealthPercent => Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_Heart"),
                BuffType.HealthRegenTick or BuffType.HealthRegenPercent => Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_HealthRegen") ?? Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_Heart"),
                BuffType.MaxStaminaPercent => Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_Lightning"),
                BuffType.StaminaRegenPercent => Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_StaminaRegen") ?? Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_Lightning"),
                BuffType.MoveSpeedPercent => Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_Speed"),
                BuffType.AttackDamagePercent => Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_AttackDamage"),
                BuffType.AttackSpeedPercent => Resources.Load<Sprite>("Icons/Buffs/Icon_Buff_AttackSpeed"),
                _ => null
            };
        }
    }
}
