using System.Collections;
using TMPro;
using UnityEngine;

namespace PlayerUI
{
    /// <summary>
    /// Floating combat text element featuring punch-scale animation,
    /// dynamic 3D world-to-screen camera tracking, high-contrast outline, and smooth fade-out.
    /// Supports custom scale and outline color per hit type.
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
                textMesh.fontStyle = FontStyles.Bold;
                textMesh.outlineWidth = 0.28f;
                textMesh.outlineColor = new Color32(15, 15, 15, 255);
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.raycastTarget = false;
            }
        }

        /// <summary>
        /// Plays floating text animation with dynamic 3D world-to-screen position tracking.
        /// </summary>
        public void PlayWorldTracked(string text, Color color, Vector3 worldStartPos, Camera cam, System.Action onComplete = null)
        {
            PlayWorldTracked(text, color, worldStartPos, cam, 1.0f, null, onComplete);
        }

        /// <summary>
        /// Full overload supporting scaleMultiplier and custom outline color.
        /// </summary>
        public void PlayWorldTracked(
            string text,
            Color color,
            Vector3 worldStartPos,
            Camera cam,
            float scaleMultiplier,
            Color32? customOutline = null,
            System.Action onComplete = null)
        {
            EnsureComponents();

            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
                if (customOutline.HasValue)
                {
                    textMesh.outlineColor = customOutline.Value;
                }
                else
                {
                    textMesh.outlineColor = new Color32(15, 15, 15, 255);
                }
            }

            gameObject.SetActive(true);

            if (floatCoroutine != null)
                StopCoroutine(floatCoroutine);

            floatCoroutine = StartCoroutine(WorldTrackedRoutine(color, worldStartPos, cam, scaleMultiplier, onComplete));
        }

        /// <summary>
        /// Legacy fallback for direct screen-space Vector2 calls.
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

        private IEnumerator WorldTrackedRoutine(
            Color baseColor,
            Vector3 worldStartPos,
            Camera cam,
            float scaleMultiplier,
            System.Action onComplete)
        {
            float elapsed = 0f;
            float initialPunch = 1.35f * scaleMultiplier;
            float endScale = 1.0f * scaleMultiplier;
            transform.localScale = Vector3.one * initialPunch;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;

                // Punch scale bounce
                if (progress < 0.2f)
                {
                    float p = progress / 0.2f;
                    transform.localScale = Vector3.Lerp(Vector3.one * initialPunch, Vector3.one * endScale, p);
                }
                else
                {
                    transform.localScale = Vector3.one * endScale;
                }

                // Rise smoothly in world space
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
                        textMesh.enabled = false;
                    }
                }

                // Fade out over the final 35% of duration
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

            transform.localScale = Vector3.one;
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
                elapsed += Time.unscaledDeltaTime;
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

            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
            onComplete?.Invoke();
            floatCoroutine = null;
        }
    }
}
