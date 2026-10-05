using UnityEngine;
using FeaturesCamera;

namespace FeaturesCombat.Adapters
{
    /// <summary>
    /// Trauma-based camera shake adapter.
    /// Computes non-linear trauma decay and Perlin noise offsets, delegating the final
    /// translation and rotation offsets to CameraManager.Instance without mutating camera transforms directly.
    /// Strictly clamps isometric roll on the Z-axis.
    /// </summary>
    public class TraumaCameraShake : MonoBehaviour
    {
        public static TraumaCameraShake Instance { get; private set; }

        [Header("Trauma Decay Configuration")]
        [Tooltip("Rate of trauma reduction per second.")]
        [SerializeField] private float traumaDecayRate = 1.6f;

        [Header("Amplitude Clamps (Isometric Tuned)")]
        [Tooltip("Maximum planar position displacement in world units.")]
        [SerializeField] private float maxTranslationOffset = 0.45f;

        [Tooltip("Maximum pitch/yaw angular displacement in degrees.")]
        [SerializeField] private float maxPitchYawOffset = 2.0f;

        [Tooltip("Maximum roll angular displacement around the Z-axis in degrees.")]
        [SerializeField] private float maxRollOffset = 0.8f;

        private float _trauma = 0f;
        private float _perlinTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Adds stress trauma (0.0 to 1.0) to the camera shake system.
        /// </summary>
        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        private void LateUpdate()
        {
            if (_trauma <= 0.001f)
            {
                _trauma = 0f;
                if (CameraManager.Instance != null && CameraManager.Instance.CurrentShakeOffset != Vector3.zero)
                {
                    CameraManager.Instance.SetShakeOffset(Vector3.zero, Quaternion.identity);
                }
                return;
            }

            _trauma = Mathf.Max(0f, _trauma - traumaDecayRate * Time.deltaTime);
            float shakeIntensity = _trauma * _trauma; // Quadratic curve for organic feel

            _perlinTime += Time.deltaTime * 25f;

            float offsetX = (Mathf.PerlinNoise(_perlinTime, 0f) * 2f - 1f) * maxTranslationOffset * shakeIntensity;
            float offsetY = (Mathf.PerlinNoise(0f, _perlinTime) * 2f - 1f) * maxTranslationOffset * shakeIntensity;

            float rotPitch = (Mathf.PerlinNoise(_perlinTime + 10f, 0f) * 2f - 1f) * maxPitchYawOffset * shakeIntensity;
            float rotYaw = (Mathf.PerlinNoise(0f, _perlinTime + 10f) * 2f - 1f) * maxPitchYawOffset * shakeIntensity;
            float rotRoll = (Mathf.PerlinNoise(_perlinTime + 20f, _perlinTime + 20f) * 2f - 1f) * maxRollOffset * shakeIntensity;

            Vector3 translation = new Vector3(offsetX, offsetY, 0f);
            Quaternion rotation = Quaternion.Euler(rotPitch, rotYaw, rotRoll);

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetShakeOffset(translation, rotation);
            }
        }

        private void OnDisable()
        {
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetShakeOffset(Vector3.zero, Quaternion.identity);
            }
        }
    }
}
