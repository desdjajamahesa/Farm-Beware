using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FarmBeware.Editor.Rendering
{
    /// <summary>
    /// Editor Window dan Skrip Validasi Otomatis untuk arsitektur grafis Unity 6 URP.
    /// Memvalidasi keselarasan konfigurasi:
    /// 1. Rendering Path eksklusif Deferred+ (UniversalRendererData.renderingMode)
    /// 2. GPU Resident Drawer via BatchRendererGroup (UniversalRenderPipelineAsset.gpuResidentDrawerMode)
    /// 3. Nonaktifkan fitur legacy Static Batching pada ProjectSettings guna mencegah
    ///    duplikasi geometri di CPU & RAM yang merusak efisiensi BRG.
    /// </summary>
    [InitializeOnLoad]
    public class URPGraphicsConfigurationValidator : EditorWindow
    {
        private const string PC_RENDERER_PATH = "Assets/Settings/PC_Renderer.asset";
        private const string PC_RPASSET_PATH = "Assets/Settings/PC_RPAsset.asset";
        private const string PROJECT_SETTINGS_PATH = "ProjectSettings/ProjectSettings.asset";

        private Vector2 scrollPosition;

        [MenuItem("Tools/Farm-Beware/Rendering/URP & BRG Validator", priority = 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<URPGraphicsConfigurationValidator>("URP & BRG Validator");
            window.minSize = new Vector2(500, 440);
            window.Show();
        }

        static URPGraphicsConfigurationValidator()
        {
            EditorApplication.delayCall += () =>
            {
                GetStandaloneBatching(out int staticBatching, out int _);
                if (staticBatching != 0)
                {
                    Debug.LogWarning("[URP Validator] Terdeteksi 'Static Batching' masih aktif pada Standalone platform. " +
                                     "Hal ini berisiko menduplikasi vertex/index buffer di CPU & RAM serta memecah efisiensi GPU Resident Drawer. " +
                                     "Buka 'Tools > Farm-Beware > Rendering > URP & BRG Validator' untuk memperbaikinya secara otomatis.");
                }
            };
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Unity 6 URP & GPU Resident Drawer Validator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Alat ini memverifikasi bahwa proyek Farm-Beware telah dikonfigurasi secara optimal untuk " +
                                    "Rendering Path Deferred+ dan penyerahan GPU via BatchRendererGroup (BRG).", MessageType.Info);

            EditorGUILayout.Space(10);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PC_RENDERER_PATH);
            var rpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PC_RPASSET_PATH);

            GetStandaloneBatching(out int staticBatching, out int dynamicBatching);

            // 1. Rendering Path: Deferred+
            DrawValidationRow(
                title: "1. Rendering Path: Deferred+",
                isOk: rendererData != null && rendererData.renderingMode == RenderingMode.DeferredPlus,
                description: rendererData == null 
                    ? "Renderer asset tidak ditemukan di " + PC_RENDERER_PATH 
                    : $"Status saat ini: {rendererData.renderingMode}. Diperlukan: DeferredPlus.",
                fixAction: () =>
                {
                    if (rendererData != null)
                    {
                        Undo.RecordObject(rendererData, "Set RenderingMode to DeferredPlus");
                        rendererData.renderingMode = RenderingMode.DeferredPlus;
                        EditorUtility.SetDirty(rendererData);
                        AssetDatabase.SaveAssets();
                    }
                }
            );

            EditorGUILayout.Space(5);

            // 2. GPU Resident Drawer
            bool brgOk = rpAsset != null && rpAsset.gpuResidentDrawerMode == GPUResidentDrawerMode.InstancedDrawing;
            DrawValidationRow(
                title: "2. GPU Resident Drawer (BRG)",
                isOk: brgOk,
                description: rpAsset == null 
                    ? "URP Asset tidak ditemukan di " + PC_RPASSET_PATH 
                    : $"Status saat ini: {rpAsset.gpuResidentDrawerMode}. Diperlukan: InstancedDrawing.",
                fixAction: () =>
                {
                    if (rpAsset != null)
                    {
                        Undo.RecordObject(rpAsset, "Enable GPU Resident Drawer");
                        rpAsset.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
                        EditorUtility.SetDirty(rpAsset);
                        AssetDatabase.SaveAssets();
                    }
                }
            );

            EditorGUILayout.Space(5);

            // 3. SRP Batcher
            bool srpBatcherOk = rpAsset != null && rpAsset.useSRPBatcher;
            DrawValidationRow(
                title: "3. SRP Batcher Enabled",
                isOk: srpBatcherOk,
                description: rpAsset == null 
                    ? "URP Asset tidak ditemukan." 
                    : $"Status saat ini: {(rpAsset.useSRPBatcher ? "Enabled" : "Disabled")}. Diperlukan: Enabled.",
                fixAction: () =>
                {
                    if (rpAsset != null)
                    {
                        Undo.RecordObject(rpAsset, "Enable SRP Batcher");
                        rpAsset.useSRPBatcher = true;
                        EditorUtility.SetDirty(rpAsset);
                        AssetDatabase.SaveAssets();
                    }
                }
            );

            EditorGUILayout.Space(5);

            // 4. Static Batching Disabled (Crucial!)
            bool staticBatchingOk = staticBatching == 0;
            DrawValidationRow(
                title: "4. Static Batching Disabled (CPU RAM & BRG Guard)",
                isOk: staticBatchingOk,
                description: staticBatchingOk 
                    ? "Static Batching sudah nonaktif (0). Kompatibel dengan BRG."
                    : "Static Batching terdeteksi AKTIF (1). Ini menduplikasi mesh ke CPU RAM saat build dan memecah instancing GPU Resident Drawer!",
                fixAction: () =>
                {
                    SetStandaloneBatching(0, dynamicBatching);
                }
            );

            EditorGUILayout.Space(5);

            // 5. Dynamic Batching Disabled
            bool dynamicBatchingOk = dynamicBatching == 0;
            DrawValidationRow(
                title: "5. Dynamic Batching Disabled",
                isOk: dynamicBatchingOk,
                description: dynamicBatchingOk
                    ? "Dynamic Batching nonaktif (0). Direkomendasikan untuk URP modern."
                    : "Dynamic Batching aktif (1). Sebaiknya dimatikan saat menggunakan SRP Batcher & BRG.",
                fixAction: () =>
                {
                    SetStandaloneBatching(staticBatching, 0);
                }
            );

            EditorGUILayout.Space(20);

            // Fix All Button
            bool allOk = (rendererData != null && rendererData.renderingMode == RenderingMode.DeferredPlus) &&
                         brgOk && srpBatcherOk && staticBatchingOk && dynamicBatchingOk;

            if (!allOk)
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                if (GUILayout.Button("Perbaiki Semua Konfigurasi Secara Otomatis (Fix All)", GUILayout.Height(36)))
                {
                    FixAll(rendererData, rpAsset);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("Selamat! Seluruh konfigurasi fondasi URP (Deferred+, GPU Resident Drawer, & Batching Guard) telah terkonfigurasi secara sempurna.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawValidationRow(string title, bool isOk, string description, System.Action fixAction)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            GUI.color = isOk ? Color.green : Color.red;
            EditorGUILayout.LabelField(isOk ? "✔ [PASS]" : "✖ [FAIL]", EditorStyles.boldLabel, GUILayout.Width(70));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (!isOk && GUILayout.Button("Fix", GUILayout.Width(60)))
            {
                fixAction?.Invoke();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        public static void GetStandaloneBatching(out int staticBatching, out int dynamicBatching)
        {
            staticBatching = 0;
            dynamicBatching = 0;

            var assets = AssetDatabase.LoadAllAssetsAtPath(PROJECT_SETTINGS_PATH);
            if (assets == null || assets.Length == 0) return;

            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("m_BuildTargetBatching");
            if (prop != null && prop.isArray)
            {
                for (int i = 0; i < prop.arraySize; i++)
                {
                    var elem = prop.GetArrayElementAtIndex(i);
                    if (elem.FindPropertyRelative("m_BuildTarget")?.stringValue == "Standalone")
                    {
                        staticBatching = elem.FindPropertyRelative("m_StaticBatching")?.intValue ?? 0;
                        dynamicBatching = elem.FindPropertyRelative("m_DynamicBatching")?.intValue ?? 0;
                        break;
                    }
                }
            }
        }

        public static void SetStandaloneBatching(int staticBatching, int dynamicBatching)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(PROJECT_SETTINGS_PATH);
            if (assets == null || assets.Length == 0) return;

            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("m_BuildTargetBatching");
            if (prop != null && prop.isArray)
            {
                bool found = false;
                for (int i = 0; i < prop.arraySize; i++)
                {
                    var elem = prop.GetArrayElementAtIndex(i);
                    if (elem.FindPropertyRelative("m_BuildTarget")?.stringValue == "Standalone")
                    {
                        elem.FindPropertyRelative("m_StaticBatching").intValue = staticBatching;
                        elem.FindPropertyRelative("m_DynamicBatching").intValue = dynamicBatching;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    int index = prop.arraySize;
                    prop.InsertArrayElementAtIndex(index);
                    var newElem = prop.GetArrayElementAtIndex(index);
                    newElem.FindPropertyRelative("m_BuildTarget").stringValue = "Standalone";
                    newElem.FindPropertyRelative("m_StaticBatching").intValue = staticBatching;
                    newElem.FindPropertyRelative("m_DynamicBatching").intValue = dynamicBatching;
                }

                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log($"[URP Validator] Standalone batching diperbarui: Static={staticBatching}, Dynamic={dynamicBatching}");
            }
        }

        public static void FixAll(UniversalRendererData rendererData, UniversalRenderPipelineAsset rpAsset)
        {
            if (rendererData != null)
            {
                Undo.RecordObject(rendererData, "Set DeferredPlus");
                rendererData.renderingMode = RenderingMode.DeferredPlus;
                EditorUtility.SetDirty(rendererData);
            }

            if (rpAsset != null)
            {
                Undo.RecordObject(rpAsset, "Configure URP Asset for BRG");
                rpAsset.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
                rpAsset.useSRPBatcher = true;
                EditorUtility.SetDirty(rpAsset);
            }

            SetStandaloneBatching(0, 0);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP Validator] Seluruh konfigurasi grafis URP 6 berhasil diselaraskan ke Deferred+ dan GPU Resident Drawer!");
        }
    }
}
