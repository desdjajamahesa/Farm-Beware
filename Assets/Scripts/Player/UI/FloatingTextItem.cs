using System.Collections;
using TMPro;
using UnityEngine;

namespace PlayerUI
{
    /// <summary>
    /// Elemen angka/teks melayang yang bergerak naik perlahan dan memudar (fade out).
    /// Dapat digunakan kembali melalui object pooling.
    /// </summary>
    public class FloatingTextItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMesh;
        [SerializeField] private float floatSpeed = 40f;
        [SerializeField] private float duration = 1.0f;

        private RectTransform rectTransform;
        private Coroutine floatCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (textMesh == null)
                textMesh = GetComponent<TextMeshProUGUI>() ?? GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Play(string text, Color color, Vector2 startScreenPos, System.Action onComplete = null)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (textMesh == null)
                textMesh = GetComponent<TextMeshProUGUI>() ?? GetComponentInChildren<TextMeshProUGUI>();

            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }

            rectTransform.position = startScreenPos;
            gameObject.SetActive(true);

            if (floatCoroutine != null)
                StopCoroutine(floatCoroutine);

            floatCoroutine = StartCoroutine(AnimateRoutine(color, onComplete));
        }

        private IEnumerator AnimateRoutine(Color baseColor, System.Action onComplete)
        {
            float elapsed = 0f;
            Vector3 startPos = rectTransform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Gerak naik dengan kurva perlambatan
                rectTransform.position = startPos + Vector3.up * (floatSpeed * Mathf.Sin(progress * Mathf.PI * 0.5f));

                // Fade out alpha di 40% durasi terakhir
                if (textMesh != null)
                {
                    float alpha = 1f;
                    if (progress > 0.6f)
                    {
                        alpha = Mathf.Clamp01(1f - ((progress - 0.6f) / 0.4f));
                    }
                    textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }

                yield return null;
            }

            gameObject.SetActive(false);
            onComplete?.Invoke();
            floatCoroutine = null;
        }
    }
}
