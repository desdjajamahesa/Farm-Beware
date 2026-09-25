using System.Collections;
using TMPro;
using UnityEngine;

namespace PlayerUI
{
    /// <summary>
    /// Elemen angka/teks pertarungan melayang (Floating Combat Text) dengan animasi punch-scale,
    /// pelacakan dinamis posisi 3D di dunia ke layar kamera, outline kontras tinggi, dan fade-out halus.
    /// </summary>
    public class FloatingTextItem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private TextMeshProUGUI textMesh;
        [SerializeField] private float worldRiseDistance = 1.2f;
        [SerializeField] private float duration = 0.95f;

        private RectTransform rectTransform;
        private Coroutine floatCoroutine;

        private void Awake()
        {
            EnsureComponents();
        }

        public void EnsureComponents()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (textMesh == null)
                textMesh = GetComponent<TextMeshProUGUI>() ?? GetComponentInChildren<TextMeshProUGUI>();

            if (textMesh != null)
            {
                // Konfigurasi outline kontras tinggi agar angka damage selalu terbaca jelas
                textMesh.fontStyle = FontStyles.Bold;
                textMesh.outlineWidth = 0.28f;
                textMesh.outlineColor = new Color32(15, 15, 15, 255);
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.raycastTarget = false;
            }
        }

        /// <summary>
        /// Memutar animasi floating text dengan pelacakan posisi 3D dunia dinamis ke koordinat layar.
        /// </summary>
        public void PlayWorldTracked(string text, Color color, Vector3 worldStartPos, Camera cam, System.Action onComplete = null)
        {
            EnsureComponents();

            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }

            gameObject.SetActive(true);

            if (floatCoroutine != null)
                StopCoroutine(floatCoroutine);

            floatCoroutine = StartCoroutine(WorldTrackedRoutine(color, worldStartPos, cam, onComplete));
        }

        /// <summary>
        /// Fallback legacy untuk pemanggilan berbasis Vector2 layar langsung.
        /// </summary>
        public void Play(string text, Color color, Vector2 startScreenPos, System.Action onComplete = null)
        {
            EnsureComponents();

            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }

            rectTransform.position = startScreenPos;
            gameObject.SetActive(true);

            if (floatCoroutine != null)
                StopCoroutine(floatCoroutine);

            floatCoroutine = StartCoroutine(StaticScreenRoutine(color, startScreenPos, onComplete));
        }

        private IEnumerator WorldTrackedRoutine(Color baseColor, Vector3 worldStartPos, Camera cam, System.Action onComplete)
        {
            float elapsed = 0f;
            transform.localScale = Vector3.one * 1.35f; // Punchy start scale

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Punch scale bounce: 1.35 -> 1.0 dalam 0.15 detik pertama
                if (progress < 0.2f)
                {
                    float p = progress / 0.2f;
                    transform.localScale = Vector3.Lerp(Vector3.one * 1.35f, Vector3.one, p);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }

                // Posisi dunia naik perlahan
                Vector3 currentWorldPos = worldStartPos + Vector3.up * (worldRiseDistance * Mathf.Sin(progress * Mathf.PI * 0.5f));

                if (cam != null)
                {
                    Vector3 screenPoint = cam.WorldToScreenPoint(currentWorldPos);
                    if (screenPoint.z > 0)
                    {
                        rectTransform.position = screenPoint;
                        if (!textMesh.enabled) textMesh.enabled = true;
                    }
                    else
                    {
                        // Di belakang kamera
                        textMesh.enabled = false;
                    }
                }

                // Fade out di 35% durasi terakhir
                if (textMesh != null)
                {
                    float alpha = 1f;
                    if (progress > 0.65f)
                    {
                        alpha = Mathf.Clamp01(1f - ((progress - 0.65f) / 0.35f));
                    }
                    textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }

                yield return null;
            }

            gameObject.SetActive(false);
            onComplete?.Invoke();
            floatCoroutine = null;
        }

        private IEnumerator StaticScreenRoutine(Color baseColor, Vector2 startScreenPos, System.Action onComplete)
        {
            float elapsed = 0f;
            Vector3 startPos = startScreenPos;
            transform.localScale = Vector3.one * 1.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                if (progress < 0.2f)
                {
                    float p = progress / 0.2f;
                    transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, p);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }

                rectTransform.position = startPos + Vector3.up * (50f * Mathf.Sin(progress * Mathf.PI * 0.5f));

                if (textMesh != null)
                {
                    float alpha = 1f;
                    if (progress > 0.65f)
                    {
                        alpha = Mathf.Clamp01(1f - ((progress - 0.65f) / 0.35f));
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
