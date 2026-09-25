using UnityEditor;
using UnityEngine;
using FeaturesTime.Atmosphere;

namespace FarmBeware.Editor.Rendering
{
    /// <summary>
    /// Utilitas Editor untuk beralih antara mode pencahayaan Day, Night, dan Work Light (Super Terang)
    /// di Scene View agar penataan objek (level design) di StagingScene menjadi sangat nyaman dan jelas.
    /// </summary>
    public static class SceneLightingEditorUtility
    {
        private const string MENU_DAY = "Tools/Farm-Beware/Lighting/☀️ Set Scene to Day (Bright)";
        private const string MENU_NIGHT = "Tools/Farm-Beware/Lighting/🌙 Set Scene to Night (Preview)";
        private const string MENU_WORK = "Tools/Farm-Beware/Lighting/💡 Set Scene to Work Light (Super Bright)";

        [MenuItem(MENU_DAY, priority = 100)]
        public static void SetSceneToDay()
        {
            var dnlc = Object.FindFirstObjectByType<DayNightLightingController>();
            var dnvc = Object.FindFirstObjectByType<DayNightVolumeController>();
            var hsz = Object.FindFirstObjectByType<HouseSafeZoneLighting>();

            if (dnlc != null) dnlc.ApplyPresetInstant(TimeManager.DayPhase.Day);
            if (dnvc != null) dnvc.ApplyInstant(TimeManager.DayPhase.Day);
            if (hsz != null) hsz.ApplyInstant(TimeManager.DayPhase.Day);

            MarkSceneDirty();
            Debug.Log("[SceneLightingEditorUtility] Pencahayaan scene diubah ke: SIANG (Day - Terang).");
        }

        [MenuItem(MENU_NIGHT, priority = 101)]
        public static void SetSceneToNight()
        {
            var dnlc = Object.FindFirstObjectByType<DayNightLightingController>();
            var dnvc = Object.FindFirstObjectByType<DayNightVolumeController>();
            var hsz = Object.FindFirstObjectByType<HouseSafeZoneLighting>();

            if (dnlc != null) dnlc.ApplyPresetInstant(TimeManager.DayPhase.Night);
            if (dnvc != null) dnvc.ApplyInstant(TimeManager.DayPhase.Night);
            if (hsz != null) hsz.ApplyInstant(TimeManager.DayPhase.Night);

            MarkSceneDirty();
            Debug.Log("[SceneLightingEditorUtility] Pencahayaan scene diubah ke: MALAM (Night - Preview).");
        }

        [MenuItem(MENU_WORK, priority = 102)]
        public static void SetSceneToWorkLight()
        {
            // Matikan kabut sementara agar jarak pandang tak terhalang
            RenderSettings.fog = false;

            // Ambient super terang merata (menghilangkan sudut hitam pekat di Scene View)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.92f, 0.94f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.80f, 0.82f, 0.88f);
            RenderSettings.ambientGroundColor = new Color(0.65f, 0.67f, 0.72f);

            // Arahkan directional light untuk penerangan menyeluruh
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = 1.3f;
                    l.color = Color.white;
                    l.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
                    break;
                }
            }

            MarkSceneDirty();
            Debug.Log("[SceneLightingEditorUtility] Mode WORK LIGHT aktif: Kabut dinonaktifkan & ambient diterangkan untuk mempermudah penataan objek.");
        }

        private static void MarkSceneDirty()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!Application.isPlaying && activeScene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }
    }

    /// <summary>
    /// Custom Inspector untuk DayNightLightingController yang menyediakan tombol cepat
    /// untuk mengubah mode pencahayaan langsung dari jendela Inspector _LIGHTING.
    /// </summary>
    [CustomEditor(typeof(DayNightLightingController))]
    public class DayNightLightingControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("🛠️ Scene View Lighting Controls (Editor Work Mode)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Gunakan tombol di bawah ini untuk mengatur pencahayaan di Scene View saat menata perabot/objek.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1.0f, 0.92f, 0.60f);
            if (GUILayout.Button("☀️ Day (Terang)", GUILayout.Height(32)))
            {
                SceneLightingEditorUtility.SetSceneToDay();
            }

            GUI.backgroundColor = new Color(0.60f, 0.75f, 1.0f);
            if (GUILayout.Button("🌙 Night (Preview)", GUILayout.Height(32)))
            {
                SceneLightingEditorUtility.SetSceneToNight();
            }

            GUI.backgroundColor = new Color(0.80f, 1.0f, 0.80f);
            if (GUILayout.Button("💡 Work Light (Super Terang)", GUILayout.Height(32)))
            {
                SceneLightingEditorUtility.SetSceneToWorkLight();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Tip Cepat: Anda juga dapat mengklik ikon 'Bohlam Lampu' (Scene Lighting) di toolbar atas Scene View untuk mematikan bayangan dan menggunakan lampu headlight default Unity.", MessageType.None);
        }
    }
}
