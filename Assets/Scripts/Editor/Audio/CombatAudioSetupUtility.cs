using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace FarmBeware.Editor.Audio
{
    /// <summary>
    /// Setup utility to configure Player and Combat AudioSources and route them into AudioMixer groups
    /// in accordance with the audio-setup-mixers skill workflow.
    /// </summary>
    public static class CombatAudioSetupUtility
    {
        private const string SwingLightPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Light.wav";
        private const string SwingMediumPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Medium.wav";
        private const string SwingHeavyPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Heavy.wav";
        private const string SwingDashPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Dash.wav";
        private const string SwingSkillPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Skill.wav";
        private const string SwingPunchPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Punch.wav";

        private const string HitSlicePath = "Assets/Audio/SFX/Combat/SFX_Weapon_Hit_Slice.wav";
        private const string HitHeavyPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Hit_Heavy.wav";
        private const string HitCritPath = "Assets/Audio/SFX/Combat/SFX_Weapon_Hit_Crit.wav";

        [MenuItem("Tools/Farm-Beware/Audio/Setup Combat Audio & Mixer Route")]
        public static void SetupCombatAudioAndMixer()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Combat Audio & Mixer Route");

            Debug.Log("[CombatAudioSetupUtility] Starting combat audio setup...");

            // 1. Find Audio Mixer and 'SFX' group
            AudioMixerGroup sfxGroup = FindSfxMixerGroup();
            if (sfxGroup != null)
            {
                Debug.Log($"[CombatAudioSetupUtility] Found target AudioMixerGroup: '{sfxGroup.name}' in '{sfxGroup.audioMixer.name}'.");
            }
            else
            {
                Debug.LogWarning("[CombatAudioSetupUtility] No 'SFX' AudioMixerGroup found yet. Please create 'MainMixer.mixer' with group 'SFX' under Master in the Audio Mixer window.");
            }

            // 2. Setup PlayerEquipment AudioSource & Clips in Scene
            var players = Object.FindObjectsByType<PlayerEquipment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var player in players)
            {
                Undo.RegisterCompleteObjectUndo(player.gameObject, "Setup Player Combat AudioSource");
                Undo.RegisterCompleteObjectUndo(player, "Setup Player Combat Clips");

                var audioSrc = player.GetComponent<AudioSource>();
                if (audioSrc == null)
                {
                    audioSrc = Undo.AddComponent<AudioSource>(player.gameObject);
                }
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0.35f;

                if (sfxGroup != null)
                {
                    audioSrc.outputAudioMixerGroup = sfxGroup;
                }

                SerializedObject so = new SerializedObject(player);
                so.Update();
                SetClipProperty(so, "weaponAudioSource", audioSrc);
                SetClipProperty(so, "swingLightClip", AssetDatabase.LoadAssetAtPath<AudioClip>(SwingLightPath));
                SetClipProperty(so, "swingMediumClip", AssetDatabase.LoadAssetAtPath<AudioClip>(SwingMediumPath));
                SetClipProperty(so, "swingHeavyClip", AssetDatabase.LoadAssetAtPath<AudioClip>(SwingHeavyPath));
                SetClipProperty(so, "swingDashClip", AssetDatabase.LoadAssetAtPath<AudioClip>(SwingDashPath));
                SetClipProperty(so, "swingSkillClip", AssetDatabase.LoadAssetAtPath<AudioClip>(SwingSkillPath));
                SetClipProperty(so, "swingPunchClip", AssetDatabase.LoadAssetAtPath<AudioClip>(SwingPunchPath));
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(player);
                EditorUtility.SetDirty(player.gameObject);
                Debug.Log($"[CombatAudioSetupUtility] Successfully configured PlayerEquipment on '{player.gameObject.name}'.");
            }

            // 3. Setup CombatHitVfxEngine
            var hitEngines = Object.FindObjectsByType<FeaturesCombat.Adapters.CombatHitVfxEngine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hitEngine in hitEngines)
            {
                Undo.RegisterCompleteObjectUndo(hitEngine.gameObject, "Setup CombatHitVfxEngine AudioSource");
                Undo.RegisterCompleteObjectUndo(hitEngine, "Setup CombatHitVfxEngine Clips");

                var audioSrc = hitEngine.GetComponent<AudioSource>();
                if (audioSrc == null)
                {
                    audioSrc = Undo.AddComponent<AudioSource>(hitEngine.gameObject);
                }
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0.4f;

                if (sfxGroup != null)
                {
                    audioSrc.outputAudioMixerGroup = sfxGroup;
                }

                SerializedObject so = new SerializedObject(hitEngine);
                so.Update();
                SetClipProperty(so, "sliceHitClip", AssetDatabase.LoadAssetAtPath<AudioClip>(HitSlicePath));
                SetClipProperty(so, "heavyHitClip", AssetDatabase.LoadAssetAtPath<AudioClip>(HitHeavyPath));
                SetClipProperty(so, "critHitClip", AssetDatabase.LoadAssetAtPath<AudioClip>(HitCritPath));
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(hitEngine);
                EditorUtility.SetDirty(hitEngine.gameObject);
                Debug.Log($"[CombatAudioSetupUtility] Successfully configured CombatHitVfxEngine on '{hitEngine.gameObject.name}'.");
            }

            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            Debug.Log("[CombatAudioSetupUtility] Combat audio setup complete. Don't forget to save the scene (Ctrl+S).");
        }

        private static void SetClipProperty(SerializedObject so, string propertyName, Object targetAsset)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && targetAsset != null)
            {
                prop.objectReferenceValue = targetAsset;
            }
        }

        public static AudioMixerGroup FindSfxMixerGroup()
        {
            string[] mixerGuids = AssetDatabase.FindAssets("t:AudioMixer", new[] { "Assets" });
            foreach (string guid in mixerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
                if (mixer != null)
                {
                    AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
                    if (sfxGroups != null && sfxGroups.Length > 0)
                    {
                        return sfxGroups[0];
                    }

                    // Fallback to Master if SFX group is not found
                    AudioMixerGroup[] masterGroups = mixer.FindMatchingGroups("Master");
                    if (masterGroups != null && masterGroups.Length > 0)
                    {
                        return masterGroups[0];
                    }
                }
            }
            return null;
        }

        [MenuItem("Tools/Farm-Beware/Audio/Inventory Project Audio Mixers")]
        public static void InventoryMixers()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioMixer", new[] { "Assets" });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CombatAudioSetupUtility] No AudioMixer assets found under Assets/. Please create one via Window -> Audio -> Audio Mixer.");
                return;
            }

            var rows = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
                var groups = mixer.FindMatchingGroups("");
                var names = new List<string>();
                foreach (var g in groups) names.Add(g.name);
                rows.Add($"{path} ({groups.Length} groups): {string.Join(", ", names)}");
            }
            Debug.Log("[CombatAudioSetupUtility] Mixers Inventory:\n" + string.Join("\n", rows));
        }
    }
}
