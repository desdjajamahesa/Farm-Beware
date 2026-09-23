using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FarmBeware.Editor.Rendering
{
    /// <summary>
    /// Utilitas Editor otomatis untuk mengelola URP Light Layers pada proyek Farm-Beware.
    /// Menetapkan segregasi cahaya bitwise:
    ///   - Light Layer 0 (bit 0): Exterior — Directional Light utama (matahari/bulan)
    ///   - Light Layer 1 (bit 1): Interior — Lampu dalam rumah (Point/Spot Light)
    ///
    /// Segregasi ini mencegah:
    ///   - Cahaya bulan biru dingin (Layer 0) menodai lantai rumah yang hangat
    ///   - Cahaya lampu interior (Layer 1) bocor ke pekarangan malam di luar
    ///
    /// PRASYARAT: Light Layers harus diaktifkan di URP Renderer Data:
    ///   PC_Renderer → Rendering → Use Light Layers → ✓ Enable
    ///
    /// Akses via menu: Tools > Farm-Beware > Rendering > Light Layer Assignment Utility
    /// </summary>
    public class LightLayerAssignmentUtility : EditorWindow
    {
        // ─────────────── Constants ───────────────

        /// <summary>
        /// Rendering Layer Mask bitwise untuk Light Layer 0 (Exterior).
        /// Bit 0 = 1 (decimal 1).
        /// </summary>
        private const uint EXTERIOR_LIGHT_LAYER = 1u << 0; // Layer 0

        /// <summary>
        /// Rendering Layer Mask bitwise untuk Light Layer 1 (Interior).
        /// Bit 1 = 1 (decimal 2).
        /// </summary>
        private const uint INTERIOR_LIGHT_LAYER = 1u << 1; // Layer 1

        /// <summary>
        /// Prefix nama GameObject untuk lampu interior yang akan dideteksi otomatis.
        /// </summary>
        private static readonly string[] INTERIOR_LIGHT_PREFIXES = new string[]
        {
            "InteriorLamp_",
            "Light_Interior_",
            "Interior_Light_",
            "HouseLight_"
        };

        /// <summary>
        /// Prefix nama GameObject untuk renderer interior (lantai, dinding dalam, furnitur).
        /// </summary>
        private static readonly string[] INTERIOR_RENDERER_PREFIXES = new string[]
        {
            "Floor_Interior_",
            "Wall_Interior_",
            "Furniture_",
            "Interior_"
        };

        // ─────────────── Editor Window ───────────────

        private Vector2 scrollPosition;

        [MenuItem("Tools/Farm-Beware/Rendering/Light Layer Assignment Utility", priority = 20)]
        public static void OpenWindow()
        {
            var window = GetWindow<LightLayerAssignmentUtility>("Light Layer Utility");
            window.minSize = new Vector2(520, 500);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("URP Light Layer Assignment Utility",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Alat ini mengelola segregasi Light Layers untuk mencegah kebocoran cahaya " +
                "(light leaking) antara eksterior dan interior bangunan.\n\n" +
                "• Light Layer 0 (Exterior): Directional Light, objek outdoor\n" +
                "• Light Layer 1 (Interior): Lampu rumah, lantai, furnitur",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // ── Audit Section ──
            EditorGUILayout.LabelField("1. Audit Konfigurasi Scene", EditorStyles.boldLabel);
            if (GUILayout.Button("Audit Seluruh Light & Renderer di Scene", GUILayout.Height(28)))
            {
                AuditSceneLightLayers();
            }

            EditorGUILayout.Space(10);

            // ── Auto-Assign Section ──
            EditorGUILayout.LabelField("2. Penetapan Otomatis", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Tombol berikut akan menetapkan renderingLayerMask secara otomatis " +
                "berdasarkan konvensi penamaan GameObject.",
                MessageType.Warning);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Assign Directional Light → Layer 0 (Exterior)",
                GUILayout.Height(28)))
            {
                AssignDirectionalLightsToExterior();
            }

            EditorGUILayout.Space(3);

            if (GUILayout.Button("Assign Lampu Interior (by prefix) → Layer 1 (Interior)",
                GUILayout.Height(28)))
            {
                AssignInteriorLightsToLayer1();
            }

            EditorGUILayout.Space(3);

            if (GUILayout.Button("Assign Renderer Interior (by prefix) → Layer 1 (Interior)",
                GUILayout.Height(28)))
            {
                AssignInteriorRenderersToLayer1();
            }

            EditorGUILayout.Space(10);

            // ── Selection-Based Assignment ──
            EditorGUILayout.LabelField("3. Penetapan Berdasarkan Seleksi",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Pilih satu atau beberapa GameObject di Hierarchy, lalu tekan tombol " +
                "di bawah untuk menetapkan Light Layer pada Light dan/atau Renderer yang terpilih.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Seleksi → Layer 0 (Exterior)", GUILayout.Height(28)))
            {
                AssignSelectionToLayer(EXTERIOR_LIGHT_LAYER);
            }
            if (GUILayout.Button("Seleksi → Layer 1 (Interior)", GUILayout.Height(28)))
            {
                AssignSelectionToLayer(INTERIOR_LIGHT_LAYER);
            }
            if (GUILayout.Button("Seleksi → Both Layers (0+1)", GUILayout.Height(28)))
            {
                AssignSelectionToLayer(EXTERIOR_LIGHT_LAYER | INTERIOR_LIGHT_LAYER);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ── Reference Table ──
            EditorGUILayout.LabelField("4. Referensi Konfigurasi", EditorStyles.boldLabel);
            DrawReferenceTable();

            EditorGUILayout.EndScrollView();
        }

        // ─────────────── Audit ───────────────

        private static void AuditSceneLightLayers()
        {
            Debug.Log("═══════════ LIGHT LAYER AUDIT ═══════════");

            // Audit Lights
            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            Debug.Log($"[Light Layer Audit] Ditemukan {allLights.Length} Light di scene:");
            foreach (var light in allLights)
            {
                uint mask = (uint)light.renderingLayerMask;
                string layers = DecodeLayers(mask);
                Debug.Log($"  💡 {light.gameObject.name} ({light.type}) → " +
                          $"renderingLayerMask = {mask} ({layers})");
            }

            // Audit Renderers
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int rendererCount = 0;
            foreach (var rend in allRenderers)
            {
                uint mask = rend.renderingLayerMask;
                if (mask != 1) // Hanya tampilkan yang non-default
                {
                    rendererCount++;
                    Debug.Log($"  🧱 {rend.gameObject.name} → " +
                              $"renderingLayerMask = {mask} ({DecodeLayers(mask)})");
                }
            }

            if (rendererCount == 0)
            {
                Debug.Log("  (Semua Renderer menggunakan Layer 0 default — belum ada customisasi)");
            }

            Debug.Log("═════════════════════════════════════════");
        }

        // ─────────────── Auto-Assignment ───────────────

        private static void AssignDirectionalLightsToExterior()
        {
            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var light in allLights)
            {
                if (light.type == LightType.Directional)
                {
                    Undo.RecordObject(light, "Assign Directional Light to Exterior Layer");
                    light.renderingLayerMask = (int)EXTERIOR_LIGHT_LAYER;
                    EditorUtility.SetDirty(light);
                    count++;
                    Debug.Log($"[Light Layer] '{light.gameObject.name}' → " +
                              $"Layer 0 (Exterior) [mask={EXTERIOR_LIGHT_LAYER}]");
                }
            }

            Debug.Log($"[Light Layer] Selesai: {count} Directional Light ditetapkan ke Layer 0.");
        }

        private static void AssignInteriorLightsToLayer1()
        {
            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var light in allLights)
            {
                if (MatchesAnyPrefix(light.gameObject.name, INTERIOR_LIGHT_PREFIXES))
                {
                    Undo.RecordObject(light, "Assign Interior Light to Interior Layer");
                    light.renderingLayerMask = (int)INTERIOR_LIGHT_LAYER;
                    EditorUtility.SetDirty(light);
                    count++;
                    Debug.Log($"[Light Layer] '{light.gameObject.name}' → " +
                              $"Layer 1 (Interior) [mask={INTERIOR_LIGHT_LAYER}]");
                }
            }

            Debug.Log($"[Light Layer] Selesai: {count} lampu interior ditetapkan ke Layer 1.");

            if (count == 0)
            {
                Debug.LogWarning(
                    "[Light Layer] Tidak ada lampu ditemukan dengan prefix yang cocok. " +
                    "Pastikan nama GameObject lampu interior dimulai dengan salah satu prefix: " +
                    string.Join(", ", INTERIOR_LIGHT_PREFIXES));
            }
        }

        private static void AssignInteriorRenderersToLayer1()
        {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var rend in allRenderers)
            {
                if (MatchesAnyPrefix(rend.gameObject.name, INTERIOR_RENDERER_PREFIXES))
                {
                    Undo.RecordObject(rend, "Assign Interior Renderer to Interior Layer");
                    rend.renderingLayerMask = INTERIOR_LIGHT_LAYER;
                    EditorUtility.SetDirty(rend);
                    count++;
                    Debug.Log($"[Light Layer] '{rend.gameObject.name}' → " +
                              $"Layer 1 (Interior) [mask={INTERIOR_LIGHT_LAYER}]");
                }
            }

            Debug.Log($"[Light Layer] Selesai: {count} renderer interior ditetapkan ke Layer 1.");

            if (count == 0)
            {
                Debug.LogWarning(
                    "[Light Layer] Tidak ada renderer ditemukan dengan prefix yang cocok. " +
                    "Pastikan nama GameObject geometri interior dimulai dengan salah satu prefix: " +
                    string.Join(", ", INTERIOR_RENDERER_PREFIXES));
            }
        }

        // ─────────────── Selection-Based ───────────────

        private static void AssignSelectionToLayer(uint layerMask)
        {
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                Debug.LogWarning("[Light Layer] Tidak ada GameObject terpilih di Hierarchy.");
                return;
            }

            int lightCount = 0;
            int rendererCount = 0;

            foreach (var go in selection)
            {
                // Assign pada Light (jika ada)
                var light = go.GetComponent<Light>();
                if (light != null)
                {
                    Undo.RecordObject(light, "Assign Light Layer");
                    light.renderingLayerMask = (int)layerMask;
                    EditorUtility.SetDirty(light);
                    lightCount++;
                }

                // Assign pada Renderer (jika ada)
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Undo.RecordObject(renderer, "Assign Rendering Layer");
                    renderer.renderingLayerMask = layerMask;
                    EditorUtility.SetDirty(renderer);
                    rendererCount++;
                }

                // Cari juga di children
                foreach (var childLight in go.GetComponentsInChildren<Light>(true))
                {
                    if (childLight == light) continue;
                    Undo.RecordObject(childLight, "Assign Light Layer (child)");
                    childLight.renderingLayerMask = (int)layerMask;
                    EditorUtility.SetDirty(childLight);
                    lightCount++;
                }

                foreach (var childRenderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (childRenderer == renderer) continue;
                    Undo.RecordObject(childRenderer, "Assign Rendering Layer (child)");
                    childRenderer.renderingLayerMask = layerMask;
                    EditorUtility.SetDirty(childRenderer);
                    rendererCount++;
                }
            }

            Debug.Log($"[Light Layer] Seleksi ditetapkan ke mask={layerMask} " +
                      $"({DecodeLayers(layerMask)}): " +
                      $"{lightCount} Light, {rendererCount} Renderer.");
        }

        // ─────────────── Helpers ───────────────

        private static bool MatchesAnyPrefix(string name, string[] prefixes)
        {
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (name.StartsWith(prefixes[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string DecodeLayers(uint mask)
        {
            if (mask == 0) return "None";

            var parts = new System.Collections.Generic.List<string>();
            if ((mask & EXTERIOR_LIGHT_LAYER) != 0) parts.Add("Layer 0 (Exterior)");
            if ((mask & INTERIOR_LIGHT_LAYER) != 0) parts.Add("Layer 1 (Interior)");
            for (int i = 2; i < 16; i++)
            {
                if ((mask & (1u << i)) != 0) parts.Add($"Layer {i}");
            }
            return string.Join(" + ", parts);
        }

        private void DrawReferenceTable()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Tabel Referensi Light Layer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawTableRow("Komponen", "Light Layer", "Mask Bitwise");
            DrawTableRow("────────────", "────────────", "────────────");
            DrawTableRow("Directional Light (Sun/Moon)", "Layer 0 (Ext)", "1");
            DrawTableRow("InteriorLamp_*, Light_Interior_*", "Layer 1 (Int)", "2");
            DrawTableRow("MeshRenderer (tanah, pohon, outdoor)", "Layer 0 (Ext)", "1");
            DrawTableRow("MeshRenderer (lantai dalam, furnitur)", "Layer 1 (Int)", "2");
            DrawTableRow("MeshRenderer (pintu, ambang, transisi)", "Layer 0+1", "3");

            EditorGUILayout.EndVertical();
        }

        private void DrawTableRow(string col1, string col2, string col3)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(col1, GUILayout.Width(260));
            EditorGUILayout.LabelField(col2, GUILayout.Width(120));
            EditorGUILayout.LabelField(col3, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
    }
}
