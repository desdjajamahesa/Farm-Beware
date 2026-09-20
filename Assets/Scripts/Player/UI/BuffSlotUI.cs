using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlayerUI
{
    /// <summary>
    /// Menampilkan satu slot buff aktif pada HUD:
    /// - Ikon tematik buff (Hati untuk darah, Kilat untuk stamina, Sepatu untuk speed, Pedang untuk damage, dll)
    /// - Frame rounded dark slate dengan border rim berwarna sesuai jenis buff
    /// - Efek radial cooldown sweep (bayangan memudar seiring durasi habis)
    /// - Countdown timer text yang kontras dan mudah dibaca
    /// - Tooltip interaktif saat mouse diarahkan ke slot buff
    /// </summary>
    public class BuffSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image radialFillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI nameText;

        private PlayerBuffManager.ActiveBuff boundBuff;
        private float totalDuration = 1f;
        private Color themeColor = Color.white;

        public void Bind(PlayerBuffManager.ActiveBuff buff, Sprite defaultIcon = null)
        {
            boundBuff = buff;

            if (buff == null || buff.data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            totalDuration = Mathf.Max(buff.data.duration, 0.1f);
            themeColor = GetBuffTypeColor(buff.data.buffType);

            // 1. Setup Background & Border
            if (backgroundImage != null && backgroundImage.sprite == null)
            {
                backgroundImage.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Frame_Background");
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = Color.white;
            }

            if (borderImage != null)
            {
                if (borderImage.sprite == null)
                {
                    borderImage.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Frame_Border");
                    borderImage.type = Image.Type.Sliced;
                }
                borderImage.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.9f);
            }

            // 2. Setup Icon Sprite
            if (iconImage != null)
            {
                Sprite icon = buff.data.buffIcon;
                if (icon == null) icon = defaultIcon;
                if (icon == null) icon = ActiveBuffHUDController.GetDefaultBuffIcon(buff.data.buffType);

                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    // Fallback jika asset sprite tidak ditemukan
                    iconImage.color = themeColor;
                }
            }

            // 3. Setup Radial Cooldown
            if (radialFillImage != null)
            {
                if (radialFillImage.sprite == null)
                {
                    radialFillImage.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Cooldown_Overlay");
                }
                radialFillImage.type = Image.Type.Filled;
                radialFillImage.fillMethod = Image.FillMethod.Radial360;
                radialFillImage.fillOrigin = (int)Image.Origin360.Top;
                radialFillImage.fillClockwise = true;
                radialFillImage.color = new Color(0f, 0f, 0f, 0.65f); // Translucent dark overlay sweep
            }

            // 4. Optional Name Text
            if (nameText != null)
            {
                nameText.text = !string.IsNullOrEmpty(buff.data.buffName) ? buff.data.buffName : buff.data.buffType.ToString();
            }

            UpdateVisual();
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (boundBuff == null) return;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (boundBuff == null) return;

            float remaining = Mathf.Max(boundBuff.remainingDuration, 0f);

            // Radial cooldown: bagian yang sudah habis terbayang gelap (clockwise sweep)
            if (radialFillImage != null)
            {
                float elapsedFraction = 1f - Mathf.Clamp01(remaining / totalDuration);
                radialFillImage.fillAmount = elapsedFraction;
            }

            // Timer Text formatting & low duration alert
            if (timerText != null)
            {
                if (remaining >= 60f)
                {
                    int mins = Mathf.FloorToInt(remaining / 60f);
                    int secs = Mathf.FloorToInt(remaining % 60f);
                    timerText.text = $"{mins}m{secs}s";
                }
                else
                {
                    timerText.text = $"{Mathf.CeilToInt(remaining)}s";
                }

                // Efek visual ketika buff hampir habis (< 5 detik)
                if (remaining <= 5.0f)
                {
                    // Berkedip lembut atau berwarna peringatan
                    float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                    timerText.color = Color.Lerp(new Color(1f, 0.35f, 0.35f, 1f), new Color(1f, 0.85f, 0.2f, 1f), pulse);
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (boundBuff != null && boundBuff.data != null)
            {
                BuffTooltipUI.Show(boundBuff, transform.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            BuffTooltipUI.Hide();
        }

        private void OnDisable()
        {
            BuffTooltipUI.Hide();
        }

        /// <summary>
        /// Palet warna tematik khas untuk setiap kategori buff
        /// </summary>
        public static Color GetBuffTypeColor(BuffType type)
        {
            return type switch
            {
                BuffType.MaxHealthPercent => new Color(1.0f, 0.24f, 0.32f, 1f),      // Ruby Red
                BuffType.HealthRegenTick => new Color(1.0f, 0.42f, 0.58f, 1f),       // Coral Pink
                BuffType.HealthRegenPercent => new Color(1.0f, 0.32f, 0.46f, 1f),    // Rose Red
                BuffType.MaxStaminaPercent => new Color(0.20f, 0.90f, 0.45f, 1f),    // Electric Emerald Green
                BuffType.StaminaRegenPercent => new Color(1.0f, 0.84f, 0.15f, 1f),  // Electric Golden Yellow
                BuffType.MoveSpeedPercent => new Color(0.0f, 0.82f, 1.0f, 1f),       // Electric Cyan / Sky Blue
                BuffType.AttackDamagePercent => new Color(1.0f, 0.52f, 0.12f, 1f),   // Flaming Amber / Orange
                BuffType.AttackSpeedPercent => new Color(0.72f, 0.35f, 1.0f, 1f),    // Royal Amethyst Purple
                _ => Color.white
            };
        }

        public void SetReferences(Image icon, Image radial, Image bg, Image border, TextMeshProUGUI timer, TextMeshProUGUI name = null)
        {
            iconImage = icon;
            radialFillImage = radial;
            backgroundImage = bg;
            borderImage = border;
            timerText = timer;
            nameText = name;
        }
    }
}
