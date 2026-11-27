using System;
using System.Collections;
using UnityEngine;

namespace FarmBeware.Features.Environment
{
    /// <summary>
    /// Interior Trigger System for buildings.
    /// Detects when the protagonist enters an interior area via a BoxCollider Trigger,
    /// then smoothly interpolates the _DitherFade parameter on roof/wall materials
    /// using a Coroutine and MaterialPropertyBlock (without instantiating new materials).
    ///
    /// Design:
    /// - OnTriggerEnter: Fade walls/roof to dithered transparency (_DitherFade → targetFade)
    /// - OnTriggerExit:  Restore to full solid (_DitherFade → 0.0)
    /// - Emits OnInteriorStateChanged(bool) for other systems to listen to
    ///   (Light Layer switcher, audio ambience, UI indicator, etc.).
    ///
    /// WARNING: This GameObject MUST have a BoxCollider with isTrigger = true.
    /// The protagonist tag must be set in the playerTag field (default: "Player").
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class HouseInteriorTrigger : MonoBehaviour
    {
        // ─────────────── Inspector Fields ───────────────

        [Header("Target Renderers")]
        [Tooltip("Daftar MeshRenderer atap dan dinding yang akan di-dither saat pemain masuk.")]
        [SerializeField] private Renderer[] buildingRenderers;

        [Header("Dither Settings")]
        [Tooltip("Nilai _DitherFade target saat pemain berada di dalam (0.0 = solid, 1.0 = tembus penuh). " +
                 "Rekomendasi: 0.85 untuk dinding masih terlihat samar.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float targetDitherFade = 0.85f;

        [Tooltip("Durasi transisi interpolasi dither (detik).")]
        [SerializeField] private float transitionDuration = 0.4f;

        [Header("Player Detection")]
        [Tooltip("Tag GameObject pemain.")]
        [SerializeField] private string playerTag = "Player";

        // ─────────────── Events ───────────────

        /// <summary>
        /// Emitted when the interior state changes.
        /// true = player entered interior, false = player exited.
        /// Recommended listeners: LightLayerAssignment, AudioAmbience, UI.
        /// </summary>
        public event Action<bool> OnInteriorStateChanged;
        public static event Action<bool> OnAnyInteriorStateChanged;

        // ─────────────── Private State ───────────────

        private static readonly int DitherFadeID = Shader.PropertyToID("_DitherFade");
        private MaterialPropertyBlock propertyBlock;
        private Coroutine fadeCoroutine;
        private float currentDitherValue = 0.0f;
        private bool isPlayerInside = false;

        // ─────────────── Lifecycle ───────────────

        private void Reset()
        {
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();

            // Validate trigger BoxCollider
            var col = GetComponent<BoxCollider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning(
                    $"[HouseInteriorTrigger] BoxCollider pada '{gameObject.name}' " +
                    "TIDAK diatur sebagai Trigger. Mengaktifkan isTrigger secara otomatis.",
                    this);
                col.isTrigger = true;
            }

            // Ensure all renderers start fully solid
            ApplyDitherImmediate(0.0f);
        }

        private void OnDestroy()
        {
            // Restore to solid when component is destroyed (cleanup)
            ApplyDitherImmediate(0.0f);
        }

        // ─────────────── Trigger Detection ───────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (isPlayerInside) return;

            isPlayerInside = true;
            StartDitherTransition(targetDitherFade);
            OnInteriorStateChanged?.Invoke(true);
            OnAnyInteriorStateChanged?.Invoke(true);
            FarmBeware.Core.Runtime.NarrativeTelemetry.ReportSafeZoneChanged(true);

            Debug.Log($"[HouseInteriorTrigger] Pemain masuk interior '{gameObject.name}'. " +
                      $"Dither fade → {targetDitherFade:F2}");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (!isPlayerInside) return;

            isPlayerInside = false;
            StartDitherTransition(0.0f);
            OnInteriorStateChanged?.Invoke(false);
            OnAnyInteriorStateChanged?.Invoke(false);
            FarmBeware.Core.Runtime.NarrativeTelemetry.ReportSafeZoneChanged(false);

            Debug.Log($"[HouseInteriorTrigger] Pemain keluar interior '{gameObject.name}'. " +
                      "Dither fade → 0.00");
        }

        // ─────────────── Dither Interpolation ───────────────

        private void StartDitherTransition(float target)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(DitherFadeRoutine(target));
        }

        /// <summary>
        /// Smoothly interpolates _DitherFade using SmoothStep.
        /// Uses MaterialPropertyBlock to avoid material instantiation
        /// and preserve GPU Resident Drawer / SRP Batcher compatibility.
        /// </summary>
        private IEnumerator DitherFadeRoutine(float target)
        {
            float startValue = currentDitherValue;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                currentDitherValue = Mathf.Lerp(startValue, target, t);

                ApplyDitherToRenderers(currentDitherValue);
                yield return null;
            }

            // Ensure exact final value
            currentDitherValue = target;
            ApplyDitherToRenderers(currentDitherValue);
            fadeCoroutine = null;
        }

        /// <summary>
        /// Applies the _DitherFade value to all target renderers
        /// via MaterialPropertyBlock (without instantiating new materials in RAM).
        /// </summary>
        private void ApplyDitherToRenderers(float ditherValue)
        {
            if (buildingRenderers == null) return;

            for (int i = 0; i < buildingRenderers.Length; i++)
            {
                if (buildingRenderers[i] == null) continue;

                buildingRenderers[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(DitherFadeID, ditherValue);
                buildingRenderers[i].SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>
        /// Applies the dither value immediately without interpolation.
        /// Used for initialization and cleanup.
        /// </summary>
        private void ApplyDitherImmediate(float ditherValue)
        {
            currentDitherValue = ditherValue;
            ApplyDitherToRenderers(ditherValue);
        }

        // ─────────────── Public API ───────────────

        /// <summary>
        /// Checks whether the player is currently inside the interior.
        /// </summary>
        public bool IsPlayerInside => isPlayerInside;

        /// <summary>
        /// Forces the dither value immediately (for debugging / editor scripting).
        /// </summary>
        public void ForceSetDither(float value)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            ApplyDitherImmediate(Mathf.Clamp01(value));
        }
    }
}
