using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerUI
{
    /// <summary>
    /// Menampilkan satu slot buff aktif pada HUD:
    /// - Ikon buff
    /// - Lingkaran waktu radial cooldown (Filled Image)
    /// - Sisa durasi (detik) dalam TextMeshProUGUI
    /// </summary>
    public class BuffSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image radialFillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI nameText;

        private PlayerBuffManager.ActiveBuff boundBuff;
        private float totalDuration = 1f;

        public void Bind(PlayerBuffManager.ActiveBuff buff, Sprite defaultIcon = null)
        {
            boundBuff = buff;

            if (buff == null || buff.data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            totalDuration = Mathf.Max(buff.data.duration, 0.1f);

            // Setup Icon
            if (iconImage != null)
            {
                if (buff.data.buffIcon != null)
                {
                    iconImage.sprite = buff.data.buffIcon;
                    iconImage.color = Color.white;
                }
                else if (defaultIcon != null)
                {
                    iconImage.sprite = defaultIcon;
                    iconImage.color = Color.white;
                }
                else
                {
                    // Fallback visual berdasarkan jenis buff jika tidak ada sprite
                    iconImage.color = GetBuffTypeColor(buff.data.buffType);
                }
            }

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

            if (radialFillImage != null)
            {
                radialFillImage.fillAmount = Mathf.Clamp01(remaining / totalDuration);
            }

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
            }
        }

        public static Color GetBuffTypeColor(BuffType type)
        {
            return type switch
            {
                BuffType.MaxHealthPercent => new Color(0.9f, 0.25f, 0.25f),      // Merah
                BuffType.MaxStaminaPercent => new Color(0.25f, 0.85f, 0.35f),     // Hijau
                BuffType.HealthRegenTick => new Color(0.95f, 0.45f, 0.55f),       // Pink
                BuffType.HealthRegenPercent => new Color(0.95f, 0.35f, 0.45f),
                BuffType.StaminaRegenPercent => new Color(0.35f, 0.95f, 0.65f),
                BuffType.MoveSpeedPercent => new Color(0.3f, 0.75f, 0.95f),       // Biru Langit
                BuffType.AttackDamagePercent => new Color(0.95f, 0.65f, 0.15f),   // Emas / Oranye
                BuffType.AttackSpeedPercent => new Color(0.85f, 0.35f, 0.95f),    // Ungu
                _ => Color.white
            };
        }

        public void SetReferences(Image icon, Image radial, Image bg, TextMeshProUGUI timer, TextMeshProUGUI name = null)
        {
            iconImage = icon;
            radialFillImage = radial;
            backgroundImage = bg;
            timerText = timer;
            nameText = name;
        }
    }
}
