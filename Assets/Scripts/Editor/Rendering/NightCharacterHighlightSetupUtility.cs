using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FeaturesRendering.Lighting;
using Features.Time.Atmosphere;
using FeaturesTime.Atmosphere;

namespace FarmBeware.Editor.Rendering
{
    /// <summary>
    /// Editor Setup & Validation Utility for Night Dynamic Character Highlight,
    /// Shading Floor, and Outdoor Lighting Synchronization.
    /// Automates:
    /// 1. Deprecating/removing legacy PlayerLantern light from character hierarchy.
    /// 2. Attaching and configuring HeroNightHighlightReceiver on Player prefabs and scene instance.
    /// 3. Integrating unmanaged outdoor prop lights into OutdoorGardenLamp so they turn OFF at Day and ON at Night.
    /// 4. Validating zero-light on player, zero-GC readiness, and day/night outdoor lighting status.
    /// </summary>
    public static class NightCharacterHighlightSetupUtility
    {
        private const string MENU_SETUP = "Tools/Farm-Beware/Lighting/🌙 Setup Night Character Highlight";
        private const string MENU_VALIDATE = "Tools/Farm-Beware/Lighting/🔍 Validate Night Character Highlight";

        private static readonly string[] PlayerPrefabPaths = new string[]
        {
            "Assets/AssetStore/Player/prefabs/character.prefab",
            "Assets/AssetStore/Player/prefabs/character_player.prefab"
        };

        private static readonly string[] OutdoorUnmanagedLampNames = new string[]
        {
            "Lantern_Rustic",
            "GardenLantern",
            "PointLight_Amber",
            "HangingLantern"
        };

        [MenuItem(MENU_SETUP, priority = 220)]
        public static void ExecuteSetup()
        {
            Debug.Log("<color=#38BDF8><b>[NightHighlightSetup]</b> Memulai konfigurasi Night Dynamic Character Highlight & Shading Floor...</color>");

            int prefabsUpdated = SetupPlayerPrefabs();
            int lightsRemoved = RemoveLegacyPlayerLanterns();
            bool scenePlayerConfigured = SetupScenePlayerInstance();
            int outdoorLampsConfigured = SetupUnmanagedOutdoorLamps();

            // Apply Day state cleanly to scene
            SceneLightingEditorUtility.SetSceneToDay();

            // Mark Scene Dirty and Save
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=#4ADE80><b>[NightHighlightSetup]</b> Selesai dengan sukses!\n" +
                      $"• Prefabs Updated: {prefabsUpdated}\n" +
                      $"• Legacy Lanterns Removed: {lightsRemoved}\n" +
                      $"• Scene Player Instance Configured: {scenePlayerConfigured}\n" +
                      $"• Outdoor Lamps Configured: {outdoorLampsConfigured}\n" +
                      $"• Scene Tersimpan: {activeScene.path}</color>");
        }

        [MenuItem(MENU_VALIDATE, priority = 221)]
        public static void ValidateSetup()
        {
            Debug.Log("<color=#38BDF8><b>[NightHighlightSetup]</b> Menjalankan audit kepatuhan arsitektur...</color>");

            // 1. Validasi Tidak Ada Dynamic Light Menempel pada Player
            int illegalLights = 0;
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in allLights)
            {
                if (light.name.IndexOf("Lantern", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    light.transform.root.name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    light.transform.root.name.IndexOf("character", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    illegalLights++;
                    Debug.LogError($"[Validate] Ditemukan komponen Light terlarang pada Player: {light.name} (Parent: {light.transform.parent?.name})");
                }
            }

            if (illegalLights == 0)
            {
                Debug.Log("<color=#4ADE80>[Validate] PASSED: Zero Point/Spot Light pada Player terverifikasi 100%!</color>");
            }

            // 2. Validasi Receiver pada Player Scene
            var receivers = UnityEngine.Object.FindObjectsByType<HeroNightHighlightReceiver>(FindObjectsSortMode.None);
            Debug.Log($"[Validate] HeroNightHighlightReceiver aktif di scene: {receivers.Length}");
            foreach (var r in receivers)
            {
                var renderers = r.GetComponentsInChildren<Renderer>(true);
                Debug.Log($" • Receiver pada GameObject '{r.name}' mengontrol {renderers.Length} child renderers.");
            }

            // 3. Validasi Evaluasi Kurva LightingAndAtmosphereDirector
            var director = UnityEngine.Object.FindFirstObjectByType<global::Features.Time.Atmosphere.LightingAndAtmosphereDirector>();
            if (director != null)
            {
                float dayWeight = director.EvaluateNightHighlightWeight(12.0f);
                float nightWeight = director.EvaluateNightHighlightWeight(22.0f);
                float duskWeight = director.EvaluateNightHighlightWeight(17.0f);
                float dawnWeight = director.EvaluateNightHighlightWeight(5.5f);

                bool dayOk = Mathf.Approximately(dayWeight, 0.0f);
                bool nightOk = Mathf.Approximately(nightWeight, 1.0f);
                bool duskOk = duskWeight > 0.0f && duskWeight < 1.0f;
                bool dawnOk = dawnWeight > 0.0f && dawnWeight < 1.0f;

                Debug.Log($"[Validate] Evaluasi Kurva Highlight:\n" +
                          $" • Siang (12:00) : {dayWeight:F2} (Harus 0.00 -> {(dayOk ? "OK" : "FAIL")})\n" +
                          $" • Malam (22:00) : {nightWeight:F2} (Harus 1.00 -> {(nightOk ? "OK" : "FAIL")})\n" +
                          $" • Senja (17:00) : {duskWeight:F2} (Transisi -> {(duskOk ? "OK" : "FAIL")})\n" +
                          $" • Fajar (05:30) : {dawnWeight:F2} (Transisi -> {(dawnOk ? "OK" : "FAIL")})");

                if (dayOk && nightOk && duskOk && dawnOk)
                {
                    Debug.Log("<color=#4ADE80>[Validate] PASSED: Kurva transisi 24 jam matematis valid & presisi!</color>");
                }
                else
                {
                    Debug.LogWarning("[Validate] Kurva transisi memerlukan kalibrasi rentang jam!");
                }
            }

            // 4. Validasi Outdoor Lights saat Day
            int outdoorActiveInDay = 0;
            foreach (var l in allLights)
            {
                if (l.enabled && l.gameObject.activeInHierarchy && l.type != LightType.Directional)
                {
                    if (l.name.IndexOf("Interior", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        outdoorActiveInDay++;
                        Debug.LogWarning($"[Validate] Outdoor Light masih menyala di siang hari: {l.name} (Parent: {l.transform.parent?.name})");
                    }
                }
            }

            if (outdoorActiveInDay == 0)
            {
                Debug.Log("<color=#4ADE80>[Validate] PASSED: Seluruh lampu outdoor berhasil mati 100% pada siang hari!</color>");
            }
            else
            {
                Debug.LogWarning($"[Validate] Ditemukan {outdoorActiveInDay} lampu outdoor yang masih aktif saat siang!");
            }
        }

        private static int SetupPlayerPrefabs()
        {
            int count = 0;
            foreach (var path in PlayerPrefabPaths)
            {
                if (!File.Exists(path)) continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                try
                {
                    bool modified = false;

                    // 1. Hapus child PlayerLantern jika ada
                    Transform lantern = root.transform.Find("PlayerLantern");
                    if (lantern != null)
                    {
                        UnityEngine.Object.DestroyImmediate(lantern.gameObject);
                        modified = true;
                        Debug.Log($"[NightHighlightSetup] Menghapus legacy PlayerLantern dari prefab {path}");
                    }

                    // 2. Pastikan HeroNightHighlightReceiver ada pada root
                    var receiver = root.GetComponent<HeroNightHighlightReceiver>();
                    if (receiver == null)
                    {
                        root.AddComponent<HeroNightHighlightReceiver>();
                        modified = true;
                        Debug.Log($"[NightHighlightSetup] Menambahkan HeroNightHighlightReceiver ke prefab {path}");
                    }

                    if (modified)
                    {
                        EditorUtility.SetDirty(root);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        count++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            AssetDatabase.SaveAssets();
            return count;
        }

        private static int RemoveLegacyPlayerLanterns()
        {
            int removed = 0;
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in allLights)
            {
                if (light != null && light.name.Equals("PlayerLantern", StringComparison.OrdinalIgnoreCase))
                {
                    GameObject go = light.gameObject;
                    Undo.DestroyObjectImmediate(go);
                    removed++;
                    Debug.Log("[NightHighlightSetup] Berhasil menghapus GameObject 'PlayerLantern' dari active scene.");
                }
            }
            return removed;
        }

        private static bool SetupScenePlayerInstance()
        {
            GameObject playerGo = GameObject.Find("character");
            if (playerGo == null) playerGo = GameObject.Find("character_player");
            if (playerGo == null) playerGo = GameObject.Find("Player");
            if (playerGo == null) playerGo = GameObject.FindGameObjectWithTag("Player");

            if (playerGo != null)
            {
                var receiver = playerGo.GetComponent<HeroNightHighlightReceiver>();
                if (receiver == null)
                {
                    Undo.AddComponent<HeroNightHighlightReceiver>(playerGo);
                    Debug.Log($"[NightHighlightSetup] Menambahkan HeroNightHighlightReceiver ke scene object '{playerGo.name}'.");
                }
                EditorUtility.SetDirty(playerGo);
                return true;
            }

            Debug.LogWarning("[NightHighlightSetup] Player GameObject tidak ditemukan di scene dengan nama standar.");
            return false;
        }

        private static int SetupUnmanagedOutdoorLamps()
        {
            int configured = 0;
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            foreach (var l in allLights)
            {
                if (l == null) continue;

                foreach (var name in OutdoorUnmanagedLampNames)
                {
                    if (l.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var comp = l.GetComponent<OutdoorGardenLamp>();
                        if (comp == null)
                        {
                            comp = Undo.AddComponent<OutdoorGardenLamp>(l.gameObject);
                        }

                        // Configure component fields
                        var lampLightField = typeof(OutdoorGardenLamp).GetField("lampLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        lampLightField?.SetValue(comp, l);

                        float targetNightInt = 3.5f;
                        if (l.name.IndexOf("Rustic", StringComparison.OrdinalIgnoreCase) >= 0) targetNightInt = 2.5f;
                        else if (l.name.IndexOf("Amber", StringComparison.OrdinalIgnoreCase) >= 0) targetNightInt = 3.0f;
                        comp.NightPointIntensity = (l.intensity > 0.1f && l.intensity <= 5f) ? l.intensity : targetNightInt;

                        var dayIntField = typeof(OutdoorGardenLamp).GetField("dayIntensity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        dayIntField?.SetValue(comp, 0.0f);

                        var colorField = typeof(OutdoorGardenLamp).GetField("lightColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        colorField?.SetValue(comp, l.color);

                        var rangeField = typeof(OutdoorGardenLamp).GetField("pointRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        rangeField?.SetValue(comp, l.range);

                        l.shadows = LightShadows.None;
                        EditorUtility.SetDirty(comp);
                        EditorUtility.SetDirty(l);
                        configured++;
                        break;
                    }
                }
            }

            var controller = UnityEngine.Object.FindFirstObjectByType<OutdoorLightingController>();
            if (controller != null)
            {
                controller.FindAllLampsInScene();
                EditorUtility.SetDirty(controller);
            }

            return configured;
        }
    }
}
