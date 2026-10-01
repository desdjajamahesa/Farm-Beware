using System.Collections;
using FeaturesCombat.Wave;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeaturesCombat.UI
{
    /// <summary>
    /// Visual component representing an individual wave milestone marker along the combat progression track.
    /// Uses normalized anchors for resolution independence and supports zero-allocation visual animations.
    /// </summary>
    public class WaveIndicator : MonoBehaviour
    {
        [Header("UI Element References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image flagBannerImage;
        [SerializeField] private Image poleImage;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI iconText;

        [Header("Color Palette")]
        [SerializeField] private Color standardFlagColor = new Color(0.95f, 0.72f, 0.15f, 0.95f); // Amber / Gold
        [SerializeField] private Color bossFlagColor = new Color(0.85f, 0.12f, 0.18f, 1.0f);        // Crimson / Skull Red
        [SerializeField] private Color passedFlagColor = new Color(0.35f, 0.85f, 0.40f, 0.90f);      // Cleared Emerald Green
        [SerializeField] private Color poleColor = new Color(0.35f, 0.35f, 0.40f, 0.85f);

        private WaveMilestoneData milestoneData;
        private bool isPassed = false;
        private Coroutine animCoroutine;

        public WaveMilestoneData MilestoneData => milestoneData;
        public bool IsPassed => isPassed;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Initializes the flag visual position and typography based on the precomputed milestone data.
        /// Zero heap allocation.
        /// </summary>
        public void Initialize(WaveMilestoneData data)
        {
            milestoneData = data;
            isPassed = false;

            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            // Anchor-normalized positioning: locks the flag precisely to the milestone percentage
            float clampedProgress = Mathf.Clamp01(data.NormalizedProgress);
            rectTransform.anchorMin = new Vector2(clampedProgress, 0f);
            rectTransform.anchorMax = new Vector2(clampedProgress, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            // Visual differentiation between Standard wave flag and Boss wave flag
            if (data.IsBossWave)
            {
                if (flagBannerImage != null) flagBannerImage.color = bossFlagColor;
                if (iconText != null) iconText.text = "BOSS";
                rectTransform.localScale = Vector3.one * 1.15f;
            }
            else
            {
                if (flagBannerImage != null) flagBannerImage.color = standardFlagColor;
                if (iconText != null) iconText.text = $"W{data.WaveIndex}";
                rectTransform.localScale = Vector3.one;
            }

            if (poleImage != null)
                poleImage.color = poleColor;

            if (waveText != null)
                waveText.text = $"W{data.WaveIndex}";
        }

        /// <summary>
        /// Triggers visual feedback when the progress tracker icon crosses this milestone.
        /// </summary>
        public void TriggerMilestonePassed()
        {
            if (isPassed) return;
            isPassed = true;

            if (animCoroutine != null)
                StopCoroutine(animCoroutine);

            animCoroutine = StartCoroutine(RoutinePassedFeedback());
        }

        private IEnumerator RoutinePassedFeedback()
        {
            Vector3 baseScale = milestoneData.IsBossWave ? Vector3.one * 1.15f : Vector3.one;
            Vector3 targetScale = baseScale * 1.45f;
            Color targetColor = passedFlagColor;
            Color initialColor = flagBannerImage != null ? flagBannerImage.color : standardFlagColor;

            float duration = 0.35f;
            float elapsed = 0f;

            // Punch up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Elastic bounce effect
                float curve = Mathf.Sin(t * Mathf.PI);

                transform.localScale = Vector3.Lerp(baseScale, targetScale, curve);
                if (flagBannerImage != null)
                    flagBannerImage.color = Color.Lerp(initialColor, targetColor, t);

                yield return null;
            }

            transform.localScale = baseScale;
            if (flagBannerImage != null)
                flagBannerImage.color = targetColor;

            animCoroutine = null;
        }

        public void ResetFlag()
        {
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
            isPassed = false;
            Initialize(milestoneData);
        }
    }
}
