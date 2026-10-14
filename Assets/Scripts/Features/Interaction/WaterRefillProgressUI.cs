using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FarmBeware.Core.Runtime;

namespace FeaturesInteraction
{
    /// <summary>
    /// Screen-Space Projected UI untuk indikator progres pengisian air (Water Refill).
    /// Digunakan seragam untuk Kitchen Sink (Botol Minum) dan Garden Well (Penyiram Tanaman).
    /// Menggantikan ikon sprite mengambang dengan lingkaran progres radial yang bersih, tajam,
    /// dan konsisten di layar pemain tanpa distorsi perspektif.
    /// </summary>
    [DisallowMultipleComponent]
    public class WaterRefillProgressUI : MonoBehaviour
    {
        [Header("Offset Proyeksi 3D")]
        [Tooltip("Ketinggian posisi proyeksi di atas objek.")]
        [SerializeField] private Vector3 heightOffset = new Vector3(0f, 1.4f, 0f);

        [Header("Colors")]
        [SerializeField] private Color ringBgColor = new Color(0.05f, 0.08f, 0.14f, 0.92f);
        [SerializeField] private Color progressColor = new Color(0.22f, 0.74f, 1.0f, 1.0f); // Vibrant Azure Blue
        [SerializeField] private Color readyColor = new Color(0.15f, 0.92f, 0.45f, 1.0f);    // Emerald Green

        // UI References (Living on ScreenSpaceOverlay Canvas)
        private GameObject indicatorGO;
        private RectTransform indicatorRect;
        private Image backgroundImage;
        private Image radialFillImage;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI headerText;

        private static Sprite cachedCircleSprite;
        private static Sprite cachedRingSprite;
        private static Transform screenIndicatorContainer;

        private Camera cachedCamera;
        private bool isActiveState = false;
        private bool isReady = false;

        private void Awake()
        {
            EnsureSprites();
            BuildScreenUI();
            SetVisible(false);
        }

        private Camera GetPlayerCamera()
        {
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
                return cachedCamera;

            var camService = ServiceLocator.Resolve<ICameraService>();
            if (camService != null && camService.MainCamera != null)
                cachedCamera = camService.MainCamera;
            else if (Camera.main != null)
                cachedCamera = Camera.main;
            else
                cachedCamera = FindFirstObjectByType<Camera>();

            return cachedCamera;
        }

        private static IPlayerContext cachedPlayerControl;

        /// <summary>
        /// Sembunyikan indikator saat jendela inventori, storage, pause, atau modal UI terbuka.
        /// </summary>
        private static bool ShouldSuppressIndicators()
        {
            if (InventoryManagerUI.Instance != null && InventoryManagerUI.Instance.IsAnyInventoryUIRelatedOpen())
                return true;

            if (cachedPlayerControl == null)
                cachedPlayerControl = ServiceLocator.Resolve<IPlayerContext>();

            if (cachedPlayerControl != null && cachedPlayerControl.IsInputLocked)
                return true;

            if (UIModalHelper.IsSaveUIOpen)
                return true;

            if (Time.timeScale <= 0f)
                return true;

            return false;
        }

        private void LateUpdate()
        {
            if (indicatorGO == null) return;

            if (!isActiveState)
            {
                if (indicatorGO.activeSelf)
                    indicatorGO.SetActive(false);
                return;
            }

            if (ShouldSuppressIndicators())
            {
                if (indicatorGO.activeSelf)
                    indicatorGO.SetActive(false);
                return;
            }

            Camera cam = GetPlayerCamera();
            if (cam == null) return;

            Vector3 worldPos = transform.position + heightOffset;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            bool onScreen = screenPos.z > 0 &&
                            screenPos.x >= -80f && screenPos.x <= Screen.width + 80f &&
                            screenPos.y >= -80f && screenPos.y <= Screen.height + 80f;

            if (onScreen)
            {
                indicatorRect.position = new Vector3(screenPos.x, screenPos.y, 0f);
                if (!indicatorGO.activeSelf)
                    indicatorGO.SetActive(true);
            }
            else
            {
                if (indicatorGO.activeSelf)
                    indicatorGO.SetActive(false);
            }
        }

        /// <summary>
        /// Memperbarui progres dan teks countdown pengisian air.
        /// </summary>
        public void UpdateRefillState(bool active, float progress01, float remainingSeconds, bool isReadyToCollect = false, string customTitle = null)
        {
            if (!active && !isReadyToCollect)
            {
                isActiveState = false;
                SetVisible(false);
                return;
            }

            isActiveState = true;
            isReady = isReadyToCollect;

            if (radialFillImage != null)
            {
                radialFillImage.fillAmount = isReady ? 1f : Mathf.Clamp01(progress01);
                radialFillImage.color = isReady ? readyColor : progressColor;
            }

            if (headerText != null)
            {
                if (!string.IsNullOrEmpty(customTitle))
                {
                    headerText.text = customTitle;
                }
                else
                {
                    headerText.text = isReady ? "FULL" : "WATER";
                }
            }

            if (timerText != null)
            {
                if (isReady)
                {
                    timerText.text = "100%";
                    timerText.color = readyColor;
                }
                else
                {
                    int totalSec = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
                    timerText.text = $"{totalSec}s";
                    timerText.color = Color.white;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (indicatorGO != null && indicatorGO.activeSelf != visible)
            {
                indicatorGO.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (indicatorGO != null)
            {
                Destroy(indicatorGO);
            }
        }

        private void BuildScreenUI()
        {
            Transform parentContainer = GetOrCreateContainer();
            if (parentContainer == null) return;

            indicatorGO = new GameObject($"WaterRefillUI_{gameObject.name}", typeof(RectTransform));
            indicatorGO.transform.SetParent(parentContainer, false);
            indicatorRect = indicatorGO.GetComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(68f, 68f);
            indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);

            // Outer Border
            GameObject outerBorderGO = new GameObject("OuterBorder", typeof(RectTransform), typeof(Image));
            outerBorderGO.transform.SetParent(indicatorGO.transform, false);
            var obRect = outerBorderGO.GetComponent<RectTransform>();
            obRect.anchorMin = Vector2.zero;
            obRect.anchorMax = Vector2.one;
            obRect.sizeDelta = Vector2.zero;
            var obImg = outerBorderGO.GetComponent<Image>();
            obImg.sprite = cachedCircleSprite;
            obImg.color = new Color(0.12f, 0.28f, 0.45f, 0.85f);
            obImg.raycastTarget = false;

            // Background Disk
            GameObject bgGO = new GameObject("BgDisk", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(indicatorGO.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = new Vector2(-4f, -4f);

            backgroundImage = bgGO.GetComponent<Image>();
            backgroundImage.sprite = cachedCircleSprite;
            backgroundImage.color = ringBgColor;
            backgroundImage.raycastTarget = false;

            // Radial Fill Ring
            GameObject ringGO = new GameObject("RadialRing", typeof(RectTransform), typeof(Image));
            ringGO.transform.SetParent(indicatorGO.transform, false);
            var ringRect = ringGO.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.sizeDelta = new Vector2(-6f, -6f);

            radialFillImage = ringGO.GetComponent<Image>();
            radialFillImage.sprite = cachedRingSprite;
            radialFillImage.type = Image.Type.Filled;
            radialFillImage.fillMethod = Image.FillMethod.Radial360;
            radialFillImage.fillOrigin = (int)Image.Origin360.Top;
            radialFillImage.fillClockwise = true;
            radialFillImage.fillAmount = 0f;
            radialFillImage.color = progressColor;
            radialFillImage.raycastTarget = false;

            // Small Header / Glyph (e.g. "💧 WATER")
            GameObject headerGO = new GameObject("HeaderLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerGO.transform.SetParent(indicatorGO.transform, false);
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.60f);
            headerRect.anchorMax = new Vector2(1f, 0.92f);
            headerRect.sizeDelta = Vector2.zero;

            headerText = headerGO.GetComponent<TextMeshProUGUI>();
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.fontSize = 11;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = new Color(0.6f, 0.9f, 1f, 0.9f);
            headerText.raycastTarget = false;

            // Main Timer Text (e.g. "25s")
            GameObject textGO = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(indicatorGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.10f);
            textRect.anchorMax = new Vector2(1f, 0.65f);
            textRect.sizeDelta = Vector2.zero;

            timerText = textGO.GetComponent<TextMeshProUGUI>();
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.fontSize = 18;
            timerText.fontStyle = FontStyles.Bold;
            timerText.color = Color.white;
            timerText.outlineColor = new Color32(0, 0, 0, 220);
            timerText.outlineWidth = 0.25f;
            timerText.raycastTarget = false;

            indicatorGO.SetActive(false);
        }

        private static Transform GetOrCreateContainer()
        {
            if (screenIndicatorContainer != null)
                return screenIndicatorContainer;

            Canvas screenCanvas = null;

            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i].renderMode == RenderMode.ScreenSpaceOverlay &&
                    allCanvases[i].gameObject.name.Contains("UI_Canvas"))
                {
                    screenCanvas = allCanvases[i];
                    break;
                }
            }

            if (screenCanvas == null)
            {
                for (int i = 0; i < allCanvases.Length; i++)
                {
                    if (allCanvases[i].renderMode == RenderMode.ScreenSpaceOverlay &&
                        !allCanvases[i].name.Contains("Crop") &&
                        !allCanvases[i].name.Contains("Fade"))
                    {
                        screenCanvas = allCanvases[i];
                        break;
                    }
                }
            }

            if (screenCanvas == null)
            {
                var canvasGO = new GameObject("WaterRefillHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                screenCanvas = canvasGO.GetComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                screenCanvas.sortingOrder = 0;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            var existingContainer = GameObject.Find("WaterRefillContainer");
            if (existingContainer != null)
            {
                screenIndicatorContainer = existingContainer.transform;
                return screenIndicatorContainer;
            }

            var containerGO = new GameObject("WaterRefillContainer", typeof(RectTransform), typeof(Canvas));
            containerGO.transform.SetParent(screenCanvas.transform, false);
            containerGO.transform.SetAsFirstSibling();

            var crt = containerGO.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.sizeDelta = Vector2.zero;
            crt.anchoredPosition = Vector2.zero;

            var subCanvas = containerGO.GetComponent<Canvas>();
            subCanvas.overrideSorting = false;

            screenIndicatorContainer = containerGO.transform;
            return screenIndicatorContainer;
        }

        private static void EnsureSprites()
        {
            if (cachedCircleSprite != null && cachedRingSprite != null) return;

            int size = 128;
            float radius = (size - 2) * 0.5f;
            float innerRadius = radius * 0.72f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            // 1. Circle disk texture
            Texture2D circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            circleTex.wrapMode = TextureWrapMode.Clamp;
            circleTex.filterMode = FilterMode.Bilinear;

            Color[] circlePixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                    circlePixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            circleTex.SetPixels(circlePixels);
            circleTex.Apply();
            cachedCircleSprite = Sprite.Create(circleTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);

            // 2. Ring texture (for radial fill border)
            Texture2D ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ringTex.wrapMode = TextureWrapMode.Clamp;
            ringTex.filterMode = FilterMode.Bilinear;

            Color[] ringPixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float outerAlpha = Mathf.Clamp01((radius - dist) + 0.5f);
                    float innerAlpha = Mathf.Clamp01((dist - innerRadius) + 0.5f);
                    float ringAlpha = Mathf.Min(outerAlpha, innerAlpha);
                    ringPixels[y * size + x] = new Color(1f, 1f, 1f, ringAlpha);
                }
            }
            ringTex.SetPixels(ringPixels);
            ringTex.Apply();
            cachedRingSprite = Sprite.Create(ringTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
