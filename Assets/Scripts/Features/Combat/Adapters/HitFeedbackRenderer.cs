using UnityEngine;

namespace FeaturesCombat.Adapters
{
    /// <summary>
    /// Zero-GC Hit Flash and Visual Feedback Renderer.
    /// Modulates the shader property '_HitFlashAmount' using a cached MaterialPropertyBlock in Update()
    /// without instantiating materials, preserving Universal Render Pipeline (URP) GPU Resident Drawer (BRG) batching.
    /// </summary>
    public class HitFeedbackRenderer : MonoBehaviour
    {
        private static readonly int FlashAmountPropertyId = Shader.PropertyToID("_HitFlashAmount");

        [Tooltip("Target mesh renderer to modulate with hit flash. Defaults to first child renderer if unassigned.")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Standard duration of a single hit flash pulse in seconds.")]
        [SerializeField] private float defaultFlashDuration = 0.12f;

        private MaterialPropertyBlock _propertyBlock;
        private float _flashTimer = 0f;
        private float _activeFlashDuration = 0.12f;
        private bool _isFlashing = false;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Triggers an immediate white hit flash on the renderer.
        /// </summary>
        public void TriggerFlash(float duration = -1f)
        {
            if (targetRenderer == null) return;

            _activeFlashDuration = duration > 0f ? duration : defaultFlashDuration;
            _flashTimer = _activeFlashDuration;
            _isFlashing = true;

            ApplyFlash(1.0f);
        }

        private void Update()
        {
            if (!_isFlashing) return;

            _flashTimer -= Time.deltaTime;

            if (_flashTimer <= 0f)
            {
                _flashTimer = 0f;
                _isFlashing = false;
                ApplyFlash(0.0f);
            }
            else
            {
                float ratio = _flashTimer / _activeFlashDuration;
                ApplyFlash(ratio);
            }
        }

        private void ApplyFlash(float amount)
        {
            if (targetRenderer == null) return;

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(FlashAmountPropertyId, amount);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void OnDisable()
        {
            if (_isFlashing)
            {
                _isFlashing = false;
                _flashTimer = 0f;
                ApplyFlash(0.0f);
            }
        }
    }
}
