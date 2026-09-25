using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FeaturesRendering.Lighting;

namespace FeaturesRendering.Testing
{
    /// <summary>
    /// Skrip pengujian interaktif (Testing Harness) untuk memvalidasi pencahayaan siang (Phase 2).
    /// Memungkinkan developer untuk:
    /// 1. Merotasikan Directional Light matahari secara halus dari Azimuth 135° ke 225°.
    /// 2. Memvalidasi persilangan garis terminator bayangan (Shadow Terminator) pada hamparan botani.
    /// 3. Memverifikasi hilangnya artefak Shadow Acne pada sudut pandang isometrik curam berkat Shadow Pancaking.
    /// 4. Memverifikasi ketiadaan efek Peter-panning dengan memonitor Shadow Near Plane Offset.
    /// </summary>
    [ExecuteAlways]
    public class DayLightingValidationHarness : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("Directional Light matahari yang diuji. Jika kosong, otomatis mencari di scene.")]
        [SerializeField] private Light targetSunLight;

        [Tooltip("Komponen DaySunLightingObserver (opsional, untuk pengujian integrasi).")]
        [SerializeField] private DaySunLightingObserver sunObserver;

        [Header("1. Azimuth & Elevation Sweeper (135° -> 225°)")]
        [Tooltip("Kontrol slider sudut Azimuth horizontal. 135° = Pagi/Timur-Tenggara, 180° = Siang/Selatan, 225° = Senja/Barat-Daya.")]
        [Range(135f, 225f)]
        [SerializeField] private float testAzimuth = 180f;

        [Tooltip("Elevasi maksimum di tengah hari (180° Azimuth).")]
        [Range(30f, 75f)]
        [SerializeField] private float maxElevation = 55f;

        [Tooltip("Elevasi minimum di fajar/senja (135° dan 225° Azimuth).")]
        [Range(5f, 25f)]
        [SerializeField] private float minElevation = 12f;

        [Header("2. Auto-Sweep Animation (Uji Dinamis)")]
        [Tooltip("Aktifkan untuk memutar posisi matahari bolak-balik antara 135° dan 225° secara otomatis.")]
        [SerializeField] private bool autoSweep = false;

        [Tooltip("Kecepatan sapuan azimuth per detik.")]
        [Range(5f, 50f)]
        [SerializeField] private float sweepSpeed = 15f;

        [Header("3. Diagnostic Metrics (Read-Only)")]
        [SerializeField] private float calculatedElevation = 55f;
        [SerializeField] private float calculatedKelvin = 5500f;
        [SerializeField] private float calculatedIntensity = 1.0f;
        [SerializeField] private bool isPeterPanningSafe = true;
        [SerializeField] private bool isCameraRelativeCullingActive = false;

        private float sweepDirection = 1f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            CheckPipelineShadowSettings();
        }

        private void Update()
        {
            if (autoSweep && Application.isPlaying)
            {
                testAzimuth += sweepDirection * sweepSpeed * Time.deltaTime;
                if (testAzimuth >= 225f)
                {
                    testAzimuth = 225f;
                    sweepDirection = -1f;
                }
                else if (testAzimuth <= 135f)
                {
                    testAzimuth = 135f;
                    sweepDirection = 1f;
                }
            }

            ApplyTestLighting();
        }

        private void ResolveReferences()
        {
            if (targetSunLight == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional)
                    {
                        targetSunLight = l;
                        break;
                    }
                }
            }

            if (sunObserver == null && targetSunLight != null)
            {
                sunObserver = targetSunLight.GetComponent<DaySunLightingObserver>();
            }
        }

        public void CheckPipelineShadowSettings()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                isCameraRelativeCullingActive = urpAsset.conservativeEnclosingSphere;
            }

            if (targetSunLight != null)
            {
                // Shadow Near Plane harus <= 0.25f untuk mencegah Peter-panning
                isPeterPanningSafe = targetSunLight.shadowNearPlane <= 0.25f && targetSunLight.shadowNearPlane >= 0.05f;
            }
        }

        /// <summary>
        /// Mengalkulasi dan menerapkan parameter pencahayaan uji coba ke Directional Light.
        /// </summary>
        public void ApplyTestLighting()
        {
            if (targetSunLight == null) return;

            // Normalisasi posisi Azimuth dalam rentang [135°, 225°] -> [0, 1]
            float t = Mathf.Clamp01((testAzimuth - 135f) / (225f - 135f));

            // Elevasi: Kurva sinus simetris, puncak di t = 0.5 (Azimuth 180°)
            float elevationFactor = Mathf.Sin(t * Mathf.PI);
            calculatedElevation = Mathf.Lerp(minElevation, maxElevation, elevationFactor);

            // Kelvin: 5500K di t <= 0.5, lalu menurun secara eksponensial ke 3200K saat t -> 1.0 (senja)
            if (t <= 0.5f)
            {
                calculatedKelvin = 5500f;
            }
            else
            {
                float afternoonT = (t - 0.5f) * 2.0f; // [0, 1]
                calculatedKelvin = Mathf.Lerp(5500f, 3200f, Mathf.Pow(afternoonT, 2.2f));
            }

            calculatedIntensity = Mathf.Lerp(0.3f, 1.15f, elevationFactor);
            Color evaluatedColor = EnvironmentLightingTheme.ConvertKelvinToRGB(calculatedKelvin);

            if (sunObserver != null)
            {
                sunObserver.SetManualSunRotation(testAzimuth, calculatedElevation, calculatedKelvin, calculatedIntensity);
            }
            else
            {
                targetSunLight.transform.rotation = Quaternion.Euler(calculatedElevation, testAzimuth, 0f);
                targetSunLight.color = evaluatedColor;
                targetSunLight.intensity = calculatedIntensity;
            }

            CheckPipelineShadowSettings();
        }

        #region Context Menu Testing Commands

        [ContextMenu("Set Azimuth: 135° (Pagi Awal)")]
        public void SetAzimuth135() { testAzimuth = 135f; ApplyTestLighting(); }

        [ContextMenu("Set Azimuth: 180° (Tengah Hari)")]
        public void SetAzimuth180() { testAzimuth = 180f; ApplyTestLighting(); }

        [ContextMenu("Set Azimuth: 225° (Senja)")]
        public void SetAzimuth225() { testAzimuth = 225f; ApplyTestLighting(); }

        [ContextMenu("Fix Shadow Near Plane (Peter-Panning Guard)")]
        public void FixShadowNearPlane()
        {
            if (targetSunLight != null)
            {
                targetSunLight.shadowNearPlane = 0.15f;
                targetSunLight.shadows = LightShadows.Soft;
                isPeterPanningSafe = true;
                Debug.Log("[Harness] Shadow Near Plane disetel ke 0.15f (Aman dari Peter-panning).");
            }
        }

        #endregion

        #region OnGUI Testing Overlay

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(20, Screen.height - 310, 420, 290), "Phase 2: Day Lighting & Shadow Validator", GUI.skin.window);

            GUILayout.Label($"Sudut Azimuth: {testAzimuth:F1}° | Elevasi: {calculatedElevation:F1}°", GUI.skin.label);
            testAzimuth = GUILayout.HorizontalSlider(testAzimuth, 135f, 225f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("135° (Pagi)")) testAzimuth = 135f;
            if (GUILayout.Button("180° (Siang)")) testAzimuth = 180f;
            if (GUILayout.Button("225° (Senja)")) testAzimuth = 225f;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            autoSweep = GUILayout.Toggle(autoSweep, " Auto-Sweep Sapuan Matahari Bolak-Balik");

            GUILayout.Space(8);
            GUILayout.Label($"Suhu Kelvin: {calculatedKelvin:F0} K | Intensitas: {calculatedIntensity:F2} lux");

            GUILayout.Space(4);
            // Checklist Indikator
            GUI.color = isCameraRelativeCullingActive ? Color.green : Color.yellow;
            GUILayout.Label($"✔ Camera-Relative Culling: {(isCameraRelativeCullingActive ? "AKTIF (Bebas Kedip)" : "NONAKTIF")}");

            GUI.color = isPeterPanningSafe ? Color.green : Color.red;
            GUILayout.Label($"✔ Peter-Panning Guard (NearPlane={targetSunLight?.shadowNearPlane:F2}): {(isPeterPanningSafe ? "AMAN (Bayangan Nempel)" : "BAHAYA (Floating Shadow!)")}");

            GUI.color = Color.white;

            if (!isPeterPanningSafe && GUILayout.Button("Perbaiki Shadow Near Plane (Set 0.15f)"))
            {
                FixShadowNearPlane();
            }

            GUILayout.Space(4);
            GUILayout.Label("Validasi Visual: Periksa persilangan terminator bayangan dan pastikan tidak ada garis belang Shadow Acne pada kemiringan tanah isometrik.", GUI.skin.label);

            GUILayout.EndArea();
        }

        #endregion
    }
}
