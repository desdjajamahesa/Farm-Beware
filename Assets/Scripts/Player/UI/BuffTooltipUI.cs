using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerUI
{
    /// <summary>
    /// Tooltip mengambang untuk menampilkan informasi detail buff aktif saat kursor mouse diarahkan ke slot buff.
    /// Dibangun secara prosedural otomatis jika belum ada di scene/canvas.
    /// </summary>
    public class BuffTooltipUI : MonoBehaviour
    {
        public static BuffTooltipUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI durationText;

        private PlayerBuffManager.ActiveBuff currentBuff;
        private Canvas rootCanvas;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (rootRect == null) rootRect = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (currentBuff == null || currentBuff.data == null)
            {
                Hide();
                return;
            }

            if (durationText != null)
            {
                float rem = Mathf.Max(currentBuff.remainingDuration, 0f);
                float total = Mathf.Max(currentBuff.data.duration, 0.1f);
                durationText.text = $"Remaining: <color=#FFFFFF><b>{FormatTime(rem)}</b></color> <size=80%>(Total {FormatTime(total)})</size>";
            }
        }

        public static void Show(PlayerBuffManager.ActiveBuff buff, Vector3 targetWorldPos)
        {
            EnsureInstanceExists();
            if (Instance == null || buff == null || buff.data == null) return;

            Instance.currentBuff = buff;

            // Set Content
            Color themeColor = BuffSlotUI.GetBuffTypeColor(buff.data.buffType);
            string buffName = !string.IsNullOrEmpty(buff.data.buffName) ? buff.data.buffName : buff.data.buffType.ToString();

            if (Instance.titleText != null)
            {
                Instance.titleText.text = buffName;
                Instance.titleText.color = themeColor;
            }

            if (Instance.descText != null)
            {
                Instance.descText.text = !string.IsNullOrEmpty(buff.data.description) 
                    ? buff.data.description 
                    : GetDefaultDescription(buff.data);
            }

            if (Instance.borderImage != null)
            {
                Instance.borderImage.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.75f);
            }

            // Force layout rebuild so size fits text cleanly
            if (Instance.rootRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(Instance.rootRect);
            }

            // Position below the slot
            Instance.PositionAt(targetWorldPos);
            Instance.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (Instance == null) return;
            Instance.currentBuff = null;
            Instance.gameObject.SetActive(false);
        }

        private void PositionAt(Vector3 targetWorldPos)
        {
            if (rootRect == null) return;

            // Convert world pos to screen pos or canvas pos
            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = rootCanvas.worldCamera;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, targetWorldPos);
            
            // Offset below the buff slot
            screenPoint.y -= 38f;

            RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            if (canvasRect != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint))
                {
                    // Clamp inside canvas bounds
                    Vector2 halfSize = rootRect.sizeDelta * 0.5f;
                    float minX = -canvasRect.rect.width * 0.5f + halfSize.x + 10f;
                    float maxX = canvasRect.rect.width * 0.5f - halfSize.x - 10f;
                    float minY = -canvasRect.rect.height * 0.5f + halfSize.y + 10f;
                    float maxY = canvasRect.rect.height * 0.5f - halfSize.y - 10f;

                    localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
                    localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

                    rootRect.anchoredPosition = localPoint;
                }
            }
        }

        private static string FormatTime(float seconds)
        {
            if (seconds >= 60f)
            {
                int mins = Mathf.FloorToInt(seconds / 60f);
                int secs = Mathf.FloorToInt(seconds % 60f);
                return $"{mins}m {secs}s";
            }
            return $"{Mathf.CeilToInt(seconds)}s";
        }

        private static string GetDefaultDescription(BuffEffectData data)
        {
            string sign = data.value >= 0 ? "+" : "";
            return data.buffType switch
            {
                BuffType.MaxHealthPercent => $"{sign}{data.value * 100:0.#}% Max Health",
                BuffType.MaxStaminaPercent => $"{sign}{data.value * 100:0.#}% Max Stamina",
                BuffType.HealthRegenTick => $"{sign}{data.value:0.#} HP per second",
                BuffType.HealthRegenPercent => $"{sign}{data.value * 100:0.#}% Health Regen",
                BuffType.StaminaRegenPercent => $"{sign}{data.value * 100:0.#}% Stamina Regen",
                BuffType.MoveSpeedPercent => $"{sign}{data.value * 100:0.#}% Movement Speed",
                BuffType.AttackDamagePercent => $"{sign}{data.value * 100:0.#}% Attack Damage",
                BuffType.AttackSpeedPercent => $"{sign}{data.value * 100:0.#}% Attack Speed",
                _ => $"{sign}{data.value}"
            };
        }

        private static void EnsureInstanceExists()
        {
            if (Instance != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject tooltipObj = new GameObject("HUD_BuffTooltip", typeof(RectTransform));
            tooltipObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = tooltipObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(180f, 78f);

            // Background Frame (Dark Slate Glass)
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Frame_Background");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);

            // Border Rim
            GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.sprite = Resources.Load<Sprite>("Icons/Buffs/Buff_Frame_Border");
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(1f, 1f, 1f, 0.5f);

            // Content Layout
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(10f, 8f);
            contentRt.offsetMax = new Vector2(-10f, -8f);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 3f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title TMP
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.fontSize = 12f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.color = Color.white;
            titleTmp.textWrappingMode = TextWrappingModes.Normal;

            // Desc TMP
            GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
            descTmp.fontSize = 11f;
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.color = new Color(0.9f, 0.92f, 0.95f, 1f);
            descTmp.textWrappingMode = TextWrappingModes.Normal;

            // Duration TMP
            GameObject durObj = new GameObject("DurationText", typeof(RectTransform), typeof(TextMeshProUGUI));
            durObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI durTmp = durObj.GetComponent<TextMeshProUGUI>();
            durTmp.fontSize = 10f;
            durTmp.alignment = TextAlignmentOptions.Left;
            durTmp.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            durTmp.textWrappingMode = TextWrappingModes.NoWrap;

            BuffTooltipUI ui = tooltipObj.AddComponent<BuffTooltipUI>();
            ui.rootRect = rt;
            ui.backgroundImage = bgImg;
            ui.borderImage = borderImg;
            ui.titleText = titleTmp;
            ui.descText = descTmp;
            ui.durationText = durTmp;
            ui.rootCanvas = canvas;

            Instance = ui;
        }
    }
}
