using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FeaturesRendering.Testing
{
    /// <summary>
    /// Skrip pengujian beban (stress test) 5.000 mesh kubus untuk memvalidasi
    /// pipeline GPU Resident Drawer dan BatchRendererGroup (BRG) di Unity 6 URP.
    ///
    /// PANDUAN PENGUJIAN FRAME DEBUGGER:
    /// 1. Masuk ke Play Mode atau klik tombol 'Spawn 5,000 Cubes' di Inspector.
    /// 2. Buka menu Unity Editor: 'Window > Analysis > Frame Debugger'.
    /// 3. Klik tombol 'Enable' pada Frame Debugger.
    /// 4. Pada panel hirarki render pass di sisi kiri, telusuri pass:
    ///    'UniversalRenderer' -> 'DrawOpaqueObjects' (atau 'GPUResidentDrawer.Draw').
    /// 5. Verifikasi bahwa 5.000 kubus dirender hanya dalam 1 atau 2 indirect draw call
    ///    (menggunakan BatchRendererGroup / GPU Resident Drawer), bukan ribuan draw calls terpisah!
    /// </summary>
    [ExecuteAlways]
    public class GPUResidentDrawerStressTest : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [Tooltip("Jumlah kubus yang akan di-instansiasi (default: 5.000).")]
        [SerializeField] private int totalCubes = 5000;

        [Tooltip("Jarak spasi antar kubus dalam satuan unit dunia.")]
        [SerializeField] private float spacing = 2.0f;

        [Tooltip("Skala ukuran masing-masing kubus.")]
        [SerializeField] private Vector3 cubeScale = new Vector3(0.8f, 0.8f, 0.8f);

        [Tooltip("Material yang digunakan untuk kubus uji coba. Jika null, akan otomatis dibuat material URP Lit dengan GPU Instancing aktif.")]
        [SerializeField] private Material testMaterial;

        [Header("Runtime Animation (Opsional)")]
        [Tooltip("Animasi rotasi pada root transform untuk menguji performa dinamis.")]
        [SerializeField] private bool animateRotation = false;
        [SerializeField] private float rotationSpeed = 15f;

        [Header("Stats / State (Read Only)")]
        [SerializeField] private int currentSpawnedCount = 0;
        [SerializeField] private bool isGpuResidentDrawerActive = false;

        private GameObject cubeRoot;
        private List<GameObject> spawnedCubes = new List<GameObject>();

        // FPS counter variables
        private float deltaTime = 0.0f;
        private float currentFps = 0.0f;

        private void Start()
        {
            CheckGpuResidentDrawerStatus();
        }

        private void Update()
        {
            // Update FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            currentFps = 1.0f / Mathf.Max(0.0001f, deltaTime);

            if (animateRotation && cubeRoot != null && Application.isPlaying)
            {
                cubeRoot.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }

        public void CheckGpuResidentDrawerStatus()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                isGpuResidentDrawerActive = (urpAsset.gpuResidentDrawerMode == GPUResidentDrawerMode.InstancedDrawing);
            }
            else
            {
                isGpuResidentDrawerActive = false;
            }
        }

        [ContextMenu("Spawn 5,000 Cubes")]
        public void SpawnCubes()
        {
            ClearCubes();
            CheckGpuResidentDrawerStatus();

            cubeRoot = new GameObject("CubeStressTest_Root");
            cubeRoot.transform.SetParent(transform, false);

            Material mat = EnsureMaterial();

            // Gunakan primitive cube mesh standar
            Mesh cubeMesh = GetPrimitiveCubeMesh();

            // Hitung grid dimensi (misal 50 baris x 100 kolom = 5.000)
            int columns = Mathf.CeilToInt(Mathf.Sqrt(totalCubes));
            int rows = Mathf.CeilToInt((float)totalCubes / columns);

            float offsetX = (columns * spacing) * 0.5f;
            float offsetZ = (rows * spacing) * 0.5f;

            spawnedCubes.Capacity = totalCubes;

            for (int i = 0; i < totalCubes; i++)
            {
                int r = i / columns;
                int c = i % columns;

                Vector3 position = new Vector3(
                    (c * spacing) - offsetX,
                    Mathf.Sin(i * 0.1f) * 0.5f,
                    (r * spacing) - offsetZ
                );

                var go = new GameObject($"Cube_{i:D4}");
                go.transform.SetParent(cubeRoot.transform, false);
                go.transform.localPosition = position;
                go.transform.localScale = cubeScale;

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = cubeMesh;

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;

                spawnedCubes.Add(go);
            }

            currentSpawnedCount = spawnedCubes.Count;
            Debug.Log($"[GPU Stress Test] Berhasil memunculkan {currentSpawnedCount} kubus. " +
                      $"Status GPU Resident Drawer: {(isGpuResidentDrawerActive ? "AKTIF (InstancedDrawing)" : "NONAKTIF (Fallback standard)")}");
        }

        [ContextMenu("Clear All Cubes")]
        public void ClearCubes()
        {
            if (cubeRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(cubeRoot);
                else
                    DestroyImmediate(cubeRoot);
                cubeRoot = null;
            }

            // Bersihkan objek anak jika ada yang tersisa
            var children = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("CubeStressTest_Root") || child.name.StartsWith("Cube_"))
                    children.Add(child);
            }

            foreach (var t in children)
            {
                if (Application.isPlaying)
                    Destroy(t.gameObject);
                else
                    DestroyImmediate(t.gameObject);
            }

            spawnedCubes.Clear();
            currentSpawnedCount = 0;
        }

        private Material EnsureMaterial()
        {
            if (testMaterial != null)
                return testMaterial;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
                urpLit = Shader.Find("Standard");

            testMaterial = new Material(urpLit);
            testMaterial.name = "Mat_GPUResident_TestCube";
            testMaterial.color = new Color(0.25f, 0.65f, 0.95f);
            testMaterial.enableInstancing = true; // Wajib untuk GPU Resident Drawer / BRG

            return testMaterial;
        }

        private Mesh GetPrimitiveCubeMesh()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return mesh;
        }

        #region OnGUI HUD & Testing Overlay

        private void OnGUI()
        {
            if (!Application.isPlaying && currentSpawnedCount == 0)
                return;

            GUILayout.BeginArea(new Rect(20, 20, 360, 220), "URP GPU Resident Drawer Stress Test", GUI.skin.window);

            GUILayout.Label($"Objek Kubus Aktif: {currentSpawnedCount:N0} Mesh");
            GUILayout.Label($"Performa FPS: {currentFps:F1} FPS");

            GUI.color = isGpuResidentDrawerActive ? Color.green : Color.yellow;
            GUILayout.Label($"GPU Resident Drawer: {(isGpuResidentDrawerActive ? "AKTIF (InstancedDrawing / BRG)" : "NONAKTIF")}");
            GUI.color = Color.white;

            GUILayout.Space(10);

            if (currentSpawnedCount == 0)
            {
                if (GUILayout.Button("Spawn 5,000 Cubes", GUILayout.Height(30)))
                {
                    SpawnCubes();
                }
            }
            else
            {
                if (GUILayout.Button("Clear All Cubes", GUILayout.Height(28)))
                {
                    ClearCubes();
                }
            }

            animateRotation = GUILayout.Toggle(animateRotation, " Animasi Rotasi Root");

            GUILayout.Space(5);
            GUILayout.Label("Petunjuk: Buka 'Window > Analysis > Frame Debugger' untuk memverifikasi Draw Call tetap konstan.", GUI.skin.label);

            GUILayout.EndArea();
        }

        #endregion
    }
}
