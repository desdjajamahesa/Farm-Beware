using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Elemen panah indikator off-screen tunggal.
    /// Menampilkan arah rotasi panah ke target monster, teks jarak, dan indikator visual Boss vs Normal.
    /// </summary>
    public class EnemyIndicatorArrow : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image arrowImage;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Styling")]
        [SerializeField] private Color normalColor = new Color(1f, 0.2f, 0.2f, 0.95f);
        [SerializeField] private Color bossColor = new Color(1f, 0.65f, 0.1f, 1f);

        private float pulseTimer = 0f;
        private bool isPulsing = false;
        private static Sprite cachedArrowSprite;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            EnsureVisualElements();
        }

        public void SetData(Vector2 screenPosition, float angleDegrees, float distanceMeters, bool isBoss, bool pulse)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // Posisi clamped di layar
            rectTransform.position = screenPosition;

            // Rotasi mengarah ke target (-90 derajat karena sprite standar menunjuk ke atas)
            rectTransform.rotation = Quaternion.Euler(0f, 0f, angleDegrees - 90f);

            // Warna & ukuran
            Color color = isBoss ? bossColor : normalColor;
            if (arrowImage != null)
            {
                arrowImage.color = color;
            }

            // Teks jarak (tetap tegak lurus terhadap layar agar mudah dibaca pemain)
            if (distanceText != null)
            {
                distanceText.text = $"{Mathf.RoundToInt(distanceMeters)}m";
                distanceText.color = color;
                distanceText.transform.rotation = Quaternion.identity; // Tetap tegak
            }

            if (pulse)
            {
                isPulsing = true;
                pulseTimer = 0f;
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (isPulsing)
            {
                pulseTimer += Time.deltaTime * 5f;
                float scale = 1f + Mathf.Sin(pulseTimer) * 0.35f;
                transform.localScale = Vector3.one * Mathf.Max(1f, scale);

                if (pulseTimer >= Mathf.PI * 2f)
                {
                    isPulsing = false;
                    transform.localScale = Vector3.one;
                }
            }
        }

        public void Hide()
        {
            isPulsing = false;
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void EnsureVisualElements()
        {
            if (arrowImage == null)
            {
                var imgObj = new GameObject("ArrowGraphic", typeof(RectTransform), typeof(Image));
                imgObj.transform.SetParent(transform, false);
                arrowImage = imgObj.GetComponent<Image>();
                var rt = imgObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(36f, 36f);
                rt.anchoredPosition = Vector2.zero;

                if (cachedArrowSprite == null)
                {
                    cachedArrowSprite = CreateArrowSprite();
                }
                arrowImage.sprite = cachedArrowSprite;
            }

            if (distanceText == null)
            {
                var txtObj = new GameObject("DistanceText", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(transform, false);
                distanceText = txtObj.GetComponent<TextMeshProUGUI>();
                var rt = txtObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(80f, 24f);
                rt.anchoredPosition = new Vector2(0f, -26f);
                distanceText.alignment = TextAlignmentOptions.Center;
                distanceText.fontSize = 14f;
                distanceText.fontStyle = FontStyles.Bold;
            }
        }

        /// <summary>
        /// Membuat sprite panah segitiga tajam secara prosedural (vector-clean).
        /// </summary>
        public static Sprite CreateArrowSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color transparent = new Color(0, 0, 0, 0);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }

            // Gambar segitiga menunjuk ke atas (Y = size-1)
            float half = size * 0.5f;
            for (int y = 4; y < size - 4; y++)
            {
                float progress = (float)(y - 4) / (size - 8); // 0 at base, 1 at tip
                float widthAtY = (1f - progress) * (half - 6f);
                int minX = Mathf.RoundToInt(half - widthAtY);
                int maxX = Mathf.RoundToInt(half + widthAtY);

                for (int x = minX; x <= maxX; x++)
                {
                    tex.SetPixel(x, y, white);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
