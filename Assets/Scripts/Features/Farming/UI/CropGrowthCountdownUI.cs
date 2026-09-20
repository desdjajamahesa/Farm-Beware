using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesFarming
{
    /// <summary>
    /// Screen-Space Projected UI untuk countdown pertumbuhan tanaman.
    /// Menggambar lingkaran countdown tajam dan konsisten langsung di layar pemain (ScreenSpaceOverlay)
    /// yang diproyeksikan dari posisi tanaman 3D di dunia.
    /// Sempurna untuk kamera 3D Isometric Top-down tanpa terpengaruh kemiringan sudut kamera atau distorsi perspektif.
    /// UI HANYA aktif saat tanaman sedang bertumbuh (PlantedWatered). Jika kosong, tanaman matang, atau panen, otomatis disembunyikan.
    /// </summary>
    public class CropGrowthCountdownUI : MonoBehaviour
    {
        [Header("Offset")]
        [Tooltip("Ketinggian posisi proyeksi di atas petak tanah.")]
        [SerializeField] private Vector3 heightOffset = new Vector3(0f, 1.4f, 0f);

        [Header("Colors")]
        [SerializeField] private Color ringBgColor = new Color(0.05f, 0.07f, 0.11f, 0.92f);
        [SerializeField] private Color progressColor = new Color(0.05f, 0.92f, 0.65f, 1f); // Vibrant Emerald-Mint

        // UI References (Living on ScreenSpaceOverlay Canvas)
        private GameObject indicatorGO;
        private RectTransform indicatorRect;
        private Image backgroundImage;
        private Image radialFillImage;
        private TextMeshProUGUI timerText;

        private static Sprite cachedCircleSprite;
        private static Sprite cachedRingSprite;
        private static Transform screenIndicatorContainer;

        private Camera cachedCamera;
        private bool isGrowing = false;

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

            if (FeaturesCamera.CameraManager.Instance != null && FeaturesCamera.CameraManager.Instance.MainCamera != null)
                cachedCamera = FeaturesCamera.CameraManager.Instance.MainCamera;
            else if (Camera.main != null)
                cachedCamera = Camera.main;
            else
                cachedCamera = FindFirstObjectByType<Camera>();

            return cachedCamera;
        }

        private void LateUpdate()
        {
            if (indicatorGO == null) return;

            if (!isGrowing)
            {
                if (indicatorGO.activeSelf)
                    indicatorGO.SetActive(false);
                return;
            }

            Camera cam = GetPlayerCamera();
            if (cam == null) return;

            Vector3 worldPos = transform.position + heightOffset;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // Cek apakah di depan kamera dan di dalam area pandang layar
            bool onScreen = screenPos.z > 0 &&
                            screenPos.x >= -60f && screenPos.x <= Screen.width + 60f &&
                            screenPos.y >= -60f && screenPos.y <= Screen.height + 60f;

            if (onScreen)
            {
                // Set z = 0f agar tidak merusak urutan depth rendering UI canvas
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
        /// Memperbarui visual countdown berdasarkan status tanaman.
        /// UI HANYA aktif saat tanaman sedang bertumbuh (PlantedWatered).
        /// Jika petak kosong (Untilled, Tilled), belum disiram (PlantedDry), sudah siap panen (ReadyToHarvest),
        /// atau tidak ada tanaman, UI langsung disembunyikan.
        /// </summary>
        public void UpdateTileState(TileState state, float growthProgress, float remainingSeconds, string seedName)
        {
            if (state != TileState.PlantedWatered || growthProgress >= 1f || string.IsNullOrEmpty(seedName))
            {
                isGrowing = false;
                SetVisible(false);
                return;
            }

            isGrowing = true;

            if (radialFillImage != null)
            {
                radialFillImage.fillAmount = Mathf.Clamp01(growthProgress);
            }

            if (timerText != null)
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
                timerText.text = $"{sec}s";
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

            // Root Screen Indicator GO
            indicatorGO = new GameObject($"CropCountdown_{gameObject.name}", typeof(RectTransform));
            indicatorGO.transform.SetParent(parentContainer, false);
            indicatorRect = indicatorGO.GetComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(68f, 68f);
            indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);

            // Outer Glow / Border Disk
            GameObject outerBorderGO = new GameObject("OuterBorder", typeof(RectTransform), typeof(Image));
            outerBorderGO.transform.SetParent(indicatorGO.transform, false);
            var obRect = outerBorderGO.GetComponent<RectTransform>();
            obRect.anchorMin = Vector2.zero;
            obRect.anchorMax = Vector2.one;
            obRect.sizeDelta = Vector2.zero;
            var obImg = outerBorderGO.GetComponent<Image>();
            obImg.sprite = cachedCircleSprite;
            obImg.color = new Color(0.18f, 0.24f, 0.33f, 0.85f);
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

            // Radial Progress Ring
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

            // Timer Text (e.g. "15s")
            GameObject textGO = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(indicatorGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            timerText = textGO.GetComponent<TextMeshProUGUI>();
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.fontSize = 20;
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

            // Cari Canvas ScreenSpaceOverlay
            Canvas screenCanvas = null;

            // 1. Cek UI_Canvas
            var uiCanvasGO = GameObject.Find("UI_Canvas");
            if (uiCanvasGO != null && uiCanvasGO.TryGetComponent<Canvas>(out var c) && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenCanvas = c;
            }

            // 2. Cek semua Canvas di scene yang ScreenSpaceOverlay
            if (screenCanvas == null)
            {
                var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var canvas in allCanvases)
                {
                    if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        screenCanvas = canvas;
                        break;
                    }
                }
            }

            // 3. Fallback: Buat Canvas ScreenSpaceOverlay khusus HUD indikator jika belum ada
            if (screenCanvas == null)
            {
                var canvasGO = new GameObject("CropHUD_ScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                screenCanvas = canvasGO.GetComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                screenCanvas.sortingOrder = 15;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Wadah khusus penampung indikator dengan Sub-Canvas terisolasi
            // Pergerakan posisi indikator di layar HANYA me-rebuild sub-canvas ini,
            // dan SAMA SEKALI tidak men-dirty atau me-rebuild kanvas HUD utama (UI_Canvas).
            var containerGO = new GameObject("CropCountdownContainer", typeof(RectTransform), typeof(Canvas));
            containerGO.transform.SetParent(screenCanvas.transform, false);
            var crt = containerGO.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.sizeDelta = Vector2.zero;
            crt.anchoredPosition = Vector2.zero;

            var subCanvas = containerGO.GetComponent<Canvas>();
            subCanvas.overrideSorting = true;
            subCanvas.sortingOrder = 16;

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
