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
    /// </summary>
    public class ActiveBuffHUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBuffManager buffManager;
        [SerializeField] private GameObject buffSlotPrefab;
        [SerializeField] private Transform containerTransform;

        [Header("Buff Type Default Icons (Opsional)")]
        [SerializeField] private Sprite healthBuffIcon;
        [SerializeField] private Sprite staminaBuffIcon;
        [SerializeField] private Sprite speedBuffIcon;
        [SerializeField] private Sprite attackDamageBuffIcon;
        [SerializeField] private Sprite attackSpeedBuffIcon;

        private readonly List<BuffSlotUI> activeSlots = new List<BuffSlotUI>();

        private void Awake()
        {
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

            // Fallback: Bangun struktur UI Slot secara prosedural
            return BuildProceduralSlot(containerTransform);
        }

        public static BuffSlotUI BuildProceduralSlot(Transform parent)
        {
            // Root Slot
            GameObject slotObj = new GameObject("BuffSlot_Item", typeof(RectTransform));
            slotObj.transform.SetParent(parent, false);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48f, 58f);

            // Background Frame (Dark semi-transparent rounded box)
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(slotObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.2f);
            bgRt.anchorMax = new Vector2(1f, 1f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

            // Icon Image
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(bgObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.GetComponent<Image>();

            // Radial Cooldown Fill
            GameObject radialObj = new GameObject("RadialCooldown", typeof(RectTransform), typeof(Image));
            radialObj.transform.SetParent(bgObj.transform, false);
            RectTransform radialRt = radialObj.GetComponent<RectTransform>();
            radialRt.anchorMin = Vector2.zero;
            radialRt.anchorMax = Vector2.one;
            radialRt.offsetMin = Vector2.zero;
            radialRt.offsetMax = Vector2.zero;
            Image radialImg = radialObj.GetComponent<Image>();
            radialImg.type = Image.Type.Filled;
            radialImg.fillMethod = Image.FillMethod.Radial360;
            radialImg.fillOrigin = (int)Image.Origin360.Top;
            radialImg.fillClockwise = true;
            radialImg.color = new Color(1f, 1f, 1f, 0.35f);

            // Timer Text (TMP)
            GameObject textObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(slotObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0.22f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            TextMeshProUGUI timerTmp = textObj.GetComponent<TextMeshProUGUI>();
            timerTmp.fontSize = 11f;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.color = Color.white;
            timerTmp.enableWordWrapping = false;

            BuffSlotUI slotUi = slotObj.AddComponent<BuffSlotUI>();
            slotUi.SetReferences(iconImg, radialImg, bgImg, timerTmp);
            return slotUi;
        }

        private Sprite GetDefaultIconForType(BuffType type)
        {
            return type switch
            {
                BuffType.MaxHealthPercent or BuffType.HealthRegenTick or BuffType.HealthRegenPercent => healthBuffIcon,
                BuffType.MaxStaminaPercent or BuffType.StaminaRegenPercent => staminaBuffIcon,
                BuffType.MoveSpeedPercent => speedBuffIcon,
                BuffType.AttackDamagePercent => attackDamageBuffIcon,
                BuffType.AttackSpeedPercent => attackSpeedBuffIcon,
                _ => null
            };
        }
    }
}
