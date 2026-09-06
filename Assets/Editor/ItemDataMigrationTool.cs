using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ItemDataMigrationTool : EditorWindow
{
    [MenuItem("Farm Beware/Migrate ItemData Hierarchy")]
    static void MigrateAll()
    {
        if (!EditorUtility.DisplayDialog("Migrate ItemData",
            "This will convert all ItemData assets to their correct subclass type.\n\n" +
            "Assets will be modified IN-PLACE (same file path, same GUID).\n\n" +
            "Proceed?", "Migrate", "Cancel"))
            return;

        // Find all ItemData .asset files
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        var log = new List<string>();
        int migrated = 0, skipped = 0, errors = 0;

        // Cache script references
        var scriptCache = new Dictionary<System.Type, MonoScript>();
        scriptCache[typeof(FoodItemData)] = FindMonoScript<FoodItemData>();
        scriptCache[typeof(ToolItemData)] = FindMonoScript<ToolItemData>();
        scriptCache[typeof(TrophyItemData)] = FindMonoScript<TrophyItemData>();
        scriptCache[typeof(MaterialItemData)] = FindMonoScript<MaterialItemData>();

        // Validate all scripts found
        foreach (var kvp in scriptCache)
        {
            if (kvp.Value == null)
            {
                EditorUtility.DisplayDialog("Error", $"Cannot find MonoScript for {kvp.Key.Name}. Aborting.", "OK");
                return;
            }
        }

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (asset == null)
            {
                log.Add($"SKIP (null): {assetPath}");
                skipped++;
                continue;
            }

            // Determine target type from the type field
            System.Type targetType = null;
            switch (asset.type)
            {
                case ItemData.ItemType.Consumable:
                    targetType = typeof(FoodItemData);
                    break;
                case ItemData.ItemType.Tool:
                    targetType = typeof(ToolItemData);
                    break;
                case ItemData.ItemType.Trophy:
                    targetType = typeof(TrophyItemData);
                    break;
                case ItemData.ItemType.Material:
                    targetType = typeof(MaterialItemData);
                    break;
            }

            if (targetType == null)
            {
                log.Add($"SKIP (unknown type): {assetPath} type={asset.type}");
                skipped++;
                continue;
            }

            // Check if already correct type
            if (asset.GetType() == targetType)
            {
                log.Add($"SKIP (already correct): {assetPath} -> {targetType.Name}");
                skipped++;
                continue;
            }

            // Migrate: change m_Script in-place using SerializedObject
            try
            {
                var so = new SerializedObject(asset);
                var scriptProp = so.FindProperty("m_Script");
                if (scriptProp == null)
                {
                    log.Add($"ERROR (no m_Script): {assetPath}");
                    errors++;
                    continue;
                }

                scriptProp.objectReferenceValue = scriptCache[targetType];
                so.ApplyModifiedPropertiesWithoutUndo();

                // Force re-serialize to ensure field layout is correct
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                log.Add($"OK: {assetPath} -> {targetType.Name}");
                migrated++;
            }
            catch (System.Exception ex)
            {
                log.Add($"ERROR: {assetPath} - {ex.Message}");
                errors++;
            }
        }

        AssetDatabase.Refresh();

        string summary = $"Migration complete: {migrated} migrated, {skipped} skipped, {errors} errors";
        Debug.Log($"[ItemDataMigration] {summary}");
        Debug.Log("[ItemDataMigration] Details:\n" + string.Join("\n", log));
        EditorUtility.DisplayDialog("Migration Complete", summary + "\n\nSee Console for details.", "OK");
    }

    [MenuItem("Farm Beware/Validate ItemData Types")]
    static void ValidateTypes()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        var issues = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (asset == null) continue;

            System.Type actualType = asset.GetType();
            bool mismatch = false;

            switch (asset.type)
            {
                case ItemData.ItemType.Consumable:
                    mismatch = actualType != typeof(FoodItemData);
                    break;
                case ItemData.ItemType.Tool:
                    mismatch = actualType != typeof(ToolItemData);
                    break;
                case ItemData.ItemType.Trophy:
                    mismatch = actualType != typeof(TrophyItemData);
                    break;
                case ItemData.ItemType.Material:
                    mismatch = actualType != typeof(MaterialItemData);
                    break;
            }

            if (mismatch)
                issues.Add($"{path}: type={asset.type} but actual class={actualType.Name}");
        }

        if (issues.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation OK", "All ItemData assets have correct types.", "OK");
        }
        else
        {
            string msg = $"{issues.Count} mismatches found:\n" + string.Join("\n", issues);
            Debug.LogWarning("[ItemDataMigration] " + msg);
            EditorUtility.DisplayDialog("Validation Issues", msg, "OK");
        }
    }

    static MonoScript FindMonoScript<T>() where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {typeof(T).Name}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() == typeof(T))
                return script;
        }
        return null;
    }
}
