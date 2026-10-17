using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PadiDaunAnimator brings the rice stalk and lush leaves decoration to life:
/// - Natural breeze wind sway with dual harmonic organic motion
/// - Gentle life breathing (scale pulse & subtle vertical bobbing)
/// - Golden grains sunlight shimmer (subtle color temperature/brightness breathing)
/// - Responsive interactive perk/rustle when player hovers over menu buttons
/// - Works in unscaled time (unscaledDeltaTime) for fluid motion in all menu states
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PadiDaunAnimator : MonoBehaviour
    {
        [Header("Wind Breeze Sway")]
        [Tooltip("Base speed of the wind sway oscillation.")]
        [SerializeField] private float swaySpeed = 1.35f;

        [Tooltip("Maximum sway angle in degrees.")]
        [SerializeField] private float swayAngleMax = 3.2f;

        [Tooltip("Horizontal swaying displacement in pixels.")]
        [SerializeField] private float swayDistanceX = 4.0f;

        [Tooltip("Secondary harmonic frequency multiplier for natural turbulent breeze.")]
        [SerializeField] private float secondaryHarmonicSpeed = 1.7f;

        [Header("Life Breathing & Bob")]
        [Tooltip("Breathing scale pulsation amount (e.g. 0.025 = 2.5%).")]
        [SerializeField] private float breathAmount = 0.022f;

        [Tooltip("Speed of the breathing cycle.")]
        [SerializeField] private float breathSpeed = 1.1f;

        [Tooltip("Vertical floating amplitude in pixels.")]
        [SerializeField] private float floatBobAmount = 3.5f;

        [Header("Sunlight Shimmer")]
        [Tooltip("Whether to apply subtle sunlight shimmer on the leaves and golden grains.")]
        [SerializeField] private bool enableShimmer = true;

        [Tooltip("Base color of the graphic.")]
        [SerializeField] private Color baseColor = Color.white;

        [Tooltip("Sunlight highlight tint on peak breeze.")]
        [SerializeField] private Color sunlightGlintColor = new Color(1.08f, 1.06f, 0.98f, 1f);

        [Header("Interactive Button Hover")]
        [Tooltip("Temporary sway and excitement boost multiplier when a button is hovered.")]
        [SerializeField] private float hoverExcitementMultiplier = 1.6f;

        [Tooltip("Smoothing speed for returning from hover reaction.")]
        [SerializeField] private float excitementSmoothSpeed = 4.0f;

        [Tooltip("Custom phase offset in seconds for wave staggering across multiple buttons.")]
        [SerializeField] private float phaseOffset = 0f;

        // References
        private RectTransform rectTransform;
        private Graphic targetGraphic;
        private CanvasGroup canvasGroup;

        // Internal State
        private Vector2 baseAnchoredPos;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float currentExcitement = 1.0f;
        private float targetExcitement = 1.0f;
        private float timeOffset;

        public float CurrentExcitement => currentExcitement;
        public float PhaseOffset { get => phaseOffset; set => phaseOffset = value; }


        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            targetGraphic = GetComponent<Graphic>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (rectTransform != null)
            {
                baseAnchoredPos = rectTransform.anchoredPosition;
                baseScale = rectTransform.localScale != Vector3.zero ? rectTransform.localScale : Vector3.one;
                baseRotation = rectTransform.localRotation;
            }

            if (targetGraphic != null && baseColor == Color.white)
            {
                baseColor = targetGraphic.color;
            }

            // Pseudo-random phase offset so multiple instances don't sway in lockstep
            timeOffset = (GetInstanceID() % 1000) * 0.005f;
        }

        private void OnEnable()
        {
            if (rectTransform != null)
            {
                if (baseScale == Vector3.zero) baseScale = Vector3.one;
                rectTransform.anchoredPosition = baseAnchoredPos;
                rectTransform.localScale = baseScale;
                rectTransform.localRotation = baseRotation;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime + timeOffset + phaseOffset;

            // Smooth excitement factor toward target
            currentExcitement = Mathf.MoveTowards(currentExcitement, targetExcitement, dt * excitementSmoothSpeed);

            // 1. Dual-Harmonic Wind Sway
            float wave1 = Mathf.Sin(time * swaySpeed);
            float wave2 = Mathf.Sin(time * (swaySpeed * secondaryHarmonicSpeed) + 1.15f) * 0.35f;
            float combinedWave = (wave1 + wave2) / 1.35f;

            float currentAngle = combinedWave * swayAngleMax * currentExcitement;
            float currentShiftX = combinedWave * swayDistanceX * currentExcitement;

            // 2. Organic Breathing & Vertical Floating
            float breath = Mathf.Sin(time * breathSpeed) * breathAmount;
            float floatY = Mathf.Sin(time * breathSpeed * 1.25f + 0.5f) * floatBobAmount;

            // Apply Transform
            if (rectTransform != null)
            {
                rectTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, currentAngle);
                rectTransform.anchoredPosition = baseAnchoredPos + new Vector2(currentShiftX, floatY);
                rectTransform.localScale = new Vector3(
                    baseScale.x * (1f - breath * 0.5f),
                    baseScale.y * (1f + breath),
                    baseScale.z
                );
            }

            // 3. Subtle Sunlight Shimmer
            if (enableShimmer && targetGraphic != null)
            {
                float shimmerT = Mathf.Clamp01((combinedWave + 1f) * 0.5f);
                targetGraphic.color = Color.Lerp(baseColor, sunlightGlintColor, shimmerT * 0.45f);
            }
        }

        /// <summary>
        /// Trigger a gentle rustle / excitement pulse (called when hovering buttons).
        /// </summary>
        public void OnButtonHoverEnter()
        {
            targetExcitement = hoverExcitementMultiplier;
        }

        /// <summary>
        /// Return to calm resting breeze sway.
        /// </summary>
        public void OnButtonHoverExit()
        {
            targetExcitement = 1.0f;
        }

        /// <summary>
        /// Update base position if moved by layout or script.
        /// </summary>
        public void SetBasePosition(Vector2 pos)
        {
            baseAnchoredPos = pos;
        }
}
