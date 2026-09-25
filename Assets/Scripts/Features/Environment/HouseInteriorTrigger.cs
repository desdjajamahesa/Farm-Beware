using System;
using System.Collections;
using UnityEngine;

namespace FarmBeware.Features.Environment
{
    /// <summary>
    /// Sistem Trigger Interior untuk bangunan.
    /// Mendeteksi masuknya protagonis ke area interior melalui BoxCollider Trigger,
    /// lalu menginterpolasi parameter _DitherFade pada material atap/dinding secara halus
    /// menggunakan Coroutine dan MaterialPropertyBlock (tanpa instansiasi material baru).
    ///
    /// Desain:
    /// - OnTriggerEnter: Fade dinding/atap ke transparansi dithered (_DitherFade → targetFade)
    /// - OnTriggerExit:  Kembalikan ke solid penuh (_DitherFade → 0.0)
    /// - Memancarkan event OnInteriorStateChanged(bool) untuk didengarkan oleh
    ///   sistem lain (Light Layer switcher, audio ambience, UI indicator, dll).
    ///
    /// PERINGATAN: GameObject ini HARUS memiliki BoxCollider dengan isTrigger = true.
    /// Tag protagonis harus diatur di field playerTag (default: "Player").
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
        /// Dipancarkan saat status interior berubah.
        /// true = pemain masuk interior, false = pemain keluar.
        /// Listener yang direkomendasikan: LightLayerAssignment, AudioAmbience, UI.
        /// </summary>
        public event Action<bool> OnInteriorStateChanged;

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

            // Validasi BoxCollider trigger
            var col = GetComponent<BoxCollider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning(
                    $"[HouseInteriorTrigger] BoxCollider pada '{gameObject.name}' " +
                    "TIDAK diatur sebagai Trigger. Mengaktifkan isTrigger secara otomatis.",
                    this);
                col.isTrigger = true;
            }

            // Pastikan seluruh renderer dimulai dalam keadaan solid
            ApplyDitherImmediate(0.0f);
        }

        private void OnDestroy()
        {
            // Kembalikan ke solid saat komponen dihancurkan (pembersihan)
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
        /// Interpolasi halus _DitherFade menggunakan SmoothStep.
        /// Menggunakan MaterialPropertyBlock untuk menghindari instansiasi material
        /// dan menjaga kompatibilitas dengan GPU Resident Drawer / SRP Batcher.
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

            // Pastikan nilai akhir tepat
            currentDitherValue = target;
            ApplyDitherToRenderers(currentDitherValue);
            fadeCoroutine = null;
        }

        /// <summary>
        /// Menerapkan nilai _DitherFade ke seluruh renderer target
        /// via MaterialPropertyBlock (tanpa instansiasi material baru di RAM).
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
        /// Menerapkan nilai dither secara instan tanpa interpolasi.
        /// Digunakan untuk inisialisasi dan pembersihan.
        /// </summary>
        private void ApplyDitherImmediate(float ditherValue)
        {
            currentDitherValue = ditherValue;
            ApplyDitherToRenderers(ditherValue);
        }

        // ─────────────── Public API ───────────────

        /// <summary>
        /// Memeriksa apakah pemain saat ini berada di dalam interior.
        /// </summary>
        public bool IsPlayerInside => isPlayerInside;

        /// <summary>
        /// Mengatur nilai dither secara paksa (untuk debugging / editor scripting).
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
