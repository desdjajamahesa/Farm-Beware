#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Memastikan saat menekan tombol Play di Unity Editor, game selalu mulai dari MainMenuScene
/// (scene index 0 pada Build Settings), terlepas dari scene apa yang sedang dibuka di Hierarchy.
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartSceneSetter
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";

    static PlayModeStartSceneSetter()
    {
        ApplyPlayModeStartScene();
    }

    [MenuItem("Tools/Scene Flow/Set Play Mode to MainMenuScene")]
    public static void ApplyPlayModeStartScene()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (sceneAsset != null)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }

    [MenuItem("Tools/Scene Flow/Reset Play Mode to Current Active Scene")]
    public static void ResetPlayModeStartScene()
    {
        EditorSceneManager.playModeStartScene = null;
        UnityEngine.Debug.Log("[PlayModeStartSceneSetter] Play Mode Start Scene direset (akan menjalankan scene yang sedang aktif dibuka).");
    }
}
#endif
