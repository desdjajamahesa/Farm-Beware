using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Unity Editor Extension: Menu items and custom Inspector button to generate
/// the 3D Room layout, wall BoxColliders, and Capsule player with one click.
/// </summary>
public class RoomBuilderEditor : EditorWindow
{
    [MenuItem("Tools/Generate 3D Room & Capsule Player")]
    public static void GenerateRoomFromMenu()
    {
        RoomBuilder builder = FindObjectOfType<RoomBuilder>();
        if (builder == null)
        {
            GameObject builderObj = new GameObject("SampleSceneManager");
            builder = builderObj.AddComponent<RoomBuilder>();
        }

        builder.BuildRoom();
        Selection.activeGameObject = builder.gameObject;
        
        // Mark scene dirty and save directly to disk
        EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        EditorSceneManager.SaveScene(builder.gameObject.scene);

        EditorUtility.DisplayDialog("3D Room Re-Generated & Saved", 
            "The 3D Room layout and Capsule Player have been generated and saved successfully!\n\n" +
            "1. Denah ruangan 3D 80x80 persis seperti di gambar sampel.\n" +
            "2. BoxCollider terpasang di semua tembok.\n" +
            "3. Karakter CapsulePutih siap berjalan dengan WASD.\n" +
            "4. Scene telah berhasil DISIMPAN (SampleScene.unity).\n\n" +
            "Tekan PLAY di Unity untuk menguji gerakan capsule!", "Siap!");
    }

    public static void SaveSceneBatchMode()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        RoomBuilder builder = FindObjectOfType<RoomBuilder>();
        if (builder == null)
        {
            GameObject builderObj = new GameObject("SampleSceneManager");
            builder = builderObj.AddComponent<RoomBuilder>();
        }

        builder.BuildRoom();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Batch mode: 80x80 Room Layout successfully built and saved to SampleScene.unity");
    }
}

[CustomEditor(typeof(RoomBuilder))]
public class RoomBuilderInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomBuilder builder = (RoomBuilder)target;

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
        if (GUILayout.Button("Generate & Save 3D Room Layout", GUILayout.Height(40)))
        {
            builder.BuildRoom();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            EditorSceneManager.SaveScene(builder.gameObject.scene);
        }
        GUI.backgroundColor = Color.white;
    }
}
