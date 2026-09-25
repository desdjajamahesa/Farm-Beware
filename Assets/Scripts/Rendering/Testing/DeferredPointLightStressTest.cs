using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FeaturesRendering.Testing
{
    /// <summary>
    /// Skrip pengujian beban (stress test) untuk mensimulasikan lebih dari 200 Point Lights
    /// proyektil aktif secara simultan pada mode URP Deferred+.
    ///
    /// TUJUAN TESTING:
    /// 1. Menguji efisiensi light clustering URP Deferred+ pada beban pertempuran malam hari.
    /// 2. Memvalidasi bahwa 200 Point Lights tanpa bayangan (shadows = None) mengonsumsi <= 3.0 ms ALU GPU.
    /// 3. Menyediakan mode perbandingan interaktif untuk membuktikan mengapa proyektil dilarang menggunakan shadow casting.
    /// </summary>
    [ExecuteAlways]
    public class DeferredPointLightStressTest : MonoBehaviour
    {
        [Header("Stress Test Parameters")]
        [Tooltip("Jumlah Point Light proyektil yang disimulasikan (default: 200).")]
        [Range(50, 500)]
        [SerializeField] private int totalPointLights = 200;

        [Tooltip("Radius arena sebaran proyektil di sekitar posisi objek ini.")]
        [SerializeField] private float arenaRadius = 25f;

        [Tooltip("Jangkauan radius iluminasi masing-masing Point Light (meter).")]
        [SerializeField] private float pointLightRadius = 4.0f;

        [Tooltip("Intensitas masing-masing Point Light.")]
        [SerializeField] private float pointLightIntensity = 1.8f;

        [Tooltip("Kecepatan pergerakan terbang simulasi proyektil.")]
        [SerializeField] private float flightSpeed = 3.5f;

        [Header("Diagnostic & Comparison Mode")]
        [Tooltip("PERINGATAN: Mengaktifkan opsi ini akan memaksa 200 Point Lights merender shadow caster. Gunakan hanya untuk membandingkan lonjakan drastis GPU ALU!")]
        [SerializeField] private bool forceEnableShadows = false;

        [SerializeField] private int activeLightCount = 0;
        [SerializeField] private float currentFps = 60f;

        private GameObject lightRoot;
        private List<Light> spawnedLights = new List<Light>();
        private List<Vector3> initialOffsets = new List<Vector3>();
        private List<float> phaseOffsets = new List<float>();

        private float fpsDeltaTime = 0.0f;

        private void Update()
        {
            // Update FPS counter
            fpsDeltaTime += (Time.unscaledDeltaTime - fpsDeltaTime) * 0.1f;
            currentFps = 1.0f / Mathf.Max(0.0001f, fpsDeltaTime);

            // Animasi pergerakan proyektil dinamis
            if (spawnedLights.Count > 0 && Application.isPlaying)
            {
                float time = Time.time * flightSpeed;
                Vector3 center = transform.position;

                for (int i = 0; i < spawnedLights.Count; i++)
                {
                    if (spawnedLights[i] == null) continue;

                    float p = phaseOffsets[i];
                    Vector3 offset = initialOffsets[i];

                    // Gerakan orbit lissajous / swarming
                    float x = Mathf.Sin(time + p) * offset.x + Mathf.Cos(time * 0.5f + p) * 3f;
                    float z = Mathf.Cos(time + p) * offset.z + Mathf.Sin(time * 0.7f + p) * 3f;
                    float y = Mathf.Clamp(Mathf.Sin(time * 2f + p) * 1.5f + 1.2f, 0.5f, 3.5f);

                    spawnedLights[i].transform.position = center + new Vector3(x, y, z);
                }
            }
        }

        [ContextMenu("Spawn 200+ Deferred Point Lights")]
        public void SpawnLights()
        {
            ClearLights();

            lightRoot = new GameObject("DeferredPointLights_Root");
            lightRoot.transform.SetParent(transform, false);

            spawnedLights.Capacity = totalPointLights;
            initialOffsets.Capacity = totalPointLights;
            phaseOffsets.Capacity = totalPointLights;

            Color[] palette = new Color[]
            {
                new Color(0.2f, 0.8f, 1.0f),  // Magic Cyan
                new Color(0.8f, 0.2f, 1.0f),  // Void Purple
                new Color(1.0f, 0.5f, 0.1f),  // Ember Orange
                new Color(0.3f, 1.0f, 0.4f),  // Toxic Green
                new Color(1.0f, 0.2f, 0.4f)   // Crimson Flare
            };

            for (int i = 0; i < totalPointLights; i++)
            {
                var go = new GameObject($"ProjLight_{i:D3}");
                go.transform.SetParent(lightRoot.transform, false);

                // Buat titik acak di dalam arena
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(3f, arenaRadius);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 1.0f, Mathf.Sin(angle) * dist);
                go.transform.localPosition = offset;

                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = pointLightRadius;
                light.intensity = pointLightIntensity;
                light.color = palette[i % palette.Length];

                // ATURAN KRITIS DEFERRED+: Shadows WAJIB None
                light.shadows = forceEnableShadows ? LightShadows.Soft : LightShadows.None;

                spawnedLights.Add(light);
                initialOffsets.Add(offset);
                phaseOffsets.Add(Random.Range(0f, Mathf.PI * 2f));
            }

            activeLightCount = spawnedLights.Count;
            Debug.Log($"[Deferred Point Light Test] Berhasil memunculkan {activeLightCount} Point Lights aktif! " +
                      $"Shadows: {(forceEnableShadows ? "ENABLED (Performa Berat)" : "NONE (Optimal Deferred+)")}");
        }

        [ContextMenu("Clear All Lights")]
        public void ClearLights()
        {
            if (lightRoot != null)
            {
                if (Application.isPlaying) Destroy(lightRoot);
                else DestroyImmediate(lightRoot);
                lightRoot = null;
            }

            spawnedLights.Clear();
            initialOffsets.Clear();
            phaseOffsets.Clear();
            activeLightCount = 0;
        }

        public void SetForceEnableShadows(bool enable)
        {
            forceEnableShadows = enable;
            foreach (var l in spawnedLights)
            {
                if (l != null)
                {
                    l.shadows = enable ? LightShadows.Soft : LightShadows.None;
                }
            }
        }

        #region OnGUI Testing Overlay

        private void OnGUI()
        {
            if (!Application.isPlaying && activeLightCount == 0) return;

            GUILayout.BeginArea(new Rect(Screen.width - 400, 20, 380, 280), "Phase 3: 200+ Point Lights Profiling Harness", GUI.skin.window);

            GUILayout.Label($"Point Lights Aktif: {activeLightCount} Lampu");
            GUILayout.Label($"Framerate: {currentFps:F1} FPS", GUI.skin.label);

            GUI.color = forceEnableShadows ? Color.red : Color.green;
            GUILayout.Label($"Mode Bayangan: {(forceEnableShadows ? "SHADOWS ON (RISIKO GPU MACET!)" : "SHADOWS NONE (Optimal Deferred+)")}");
            GUI.color = Color.white;

            GUILayout.Space(8);

            if (activeLightCount == 0)
            {
                if (GUILayout.Button($"Spawn {totalPointLights} Proyektil Point Lights", GUILayout.Height(30)))
                {
                    SpawnLights();
                }
            }
            else
            {
                if (GUILayout.Button("Bersihkan Semua Lampu (Clear)", GUILayout.Height(26)))
                {
                    ClearLights();
                }

                GUILayout.Space(4);
                bool newShadowState = GUILayout.Toggle(forceEnableShadows, " Bandingkan: Paksa Aktifkan Shadows (Uji Lonjakan)");
                if (newShadowState != forceEnableShadows)
                {
                    SetForceEnableShadows(newShadowState);
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("PANDUAN GPU PROFILER:\n1. Buka 'Window > Analysis > Profiler' (Ctrl+7)\n2. Tambahkan modul 'GPU Profiler'\n3. Periksa pass 'UniversalRenderer.DeferredLights'\n4. Verifikasi konsumsi ALU GPU <= 3.0 ms.", GUI.skin.label);

            GUILayout.EndArea();
        }

        #endregion
    }
}
