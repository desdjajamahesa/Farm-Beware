using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using System;

public class ImportMainScene
{
    [MenuItem("Farm Beware/Import/MainScene as RafiScene")]
    public static void Import()
    {
        string packagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MainSceneExport.unitypackage"
        );

        if (!File.Exists(packagePath))
        {
            Debug.LogError($"Package not found: {packagePath}");
            return;
        }

        AssetDatabase.ImportPackage(packagePath, false);
        Debug.Log("Imported MainSceneExport.unitypackage");

        // Synchronous rename and setup
        RenameAndSetupScene();
    }

    private static void RenameAndSetupScene()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("MainScene t:Scene");
        if (sceneGuids.Length == 0)
        {
            Debug.LogError("MainScene not found after import");
            return;
        }

        string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
        string newScenePath = "Assets/Scenes/RafiScene.unity";

        if (File.Exists(newScenePath))
        {
            AssetDatabase.DeleteAsset(newScenePath);
            Debug.Log("Deleted existing RafiScene.unity");
        }

        string moveResult = AssetDatabase.MoveAsset(scenePath, newScenePath);
        if (!string.IsNullOrEmpty(moveResult))
        {
            Debug.LogError($"Failed to rename scene: {moveResult}");
            return;
        }
        Debug.Log($"Renamed MainScene to RafiScene at {newScenePath}");

        var scenes = EditorBuildSettings.scenes.ToList();
        int sampleIndex = scenes.FindIndex(s => s.path.Contains("SampleScene"));
        int rafiIndex = scenes.FindIndex(s => s.path.Contains("RafiScene"));

        if (sampleIndex >= 0)
        {
            scenes.RemoveAt(sampleIndex);
            Debug.Log("Removed SampleScene from build settings");
        }

        if (rafiIndex < 0)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(newScenePath, true));
            Debug.Log("Added RafiScene as scene 0 in build settings");
        }
        else
        {
            var rafiScene = scenes[rafiIndex];
            scenes.RemoveAt(rafiIndex);
            scenes.Insert(0, rafiScene);
            Debug.Log("Moved RafiScene to index 0");
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Updated EditorBuildSettings");

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Import and setup complete!");
    }
}