using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameLevelTextAssetImporter_Single
{
    [MenuItem("Assets/Create/GameLevelConfig from JSON", priority = 200)]
    public static void CreateFromSelected()
    {
        TextAsset textAsset = Selection.activeObject as TextAsset;
        if (textAsset == null) return;

        string jsonPath = AssetDatabase.GetAssetPath(textAsset);
        string json = textAsset.text;
        string fileName = Path.GetFileNameWithoutExtension(jsonPath);
        string dir = Path.GetDirectoryName(jsonPath);

        LevelData levelData = JsonUtility.FromJson<LevelData>(json);
        if (levelData?.Arrows == null || levelData.Arrows.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No arrows found in {fileName}", "OK");
            return;
        }

        string assetName = SanitizeAssetName(fileName);
        string assetPath = Path.Combine(dir, assetName + ".asset");

        if (File.Exists(assetPath))
        {
            if (!EditorUtility.DisplayDialog("Overwrite?",
                    $"{assetName}.asset already exists. Overwrite?", "Yes", "No"))
                return;
        }

        GameLevelConfig config = ScriptableObject.CreateInstance<GameLevelConfig>();
        GameLevelTextAssetImporter.MapLevelData(config, levelData);
        config.name = assetName;

        AssetDatabase.CreateAsset(config, assetPath);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);

        Debug.Log($"Created GameLevelConfig: {assetPath}");
    }

    [MenuItem("Assets/Create/GameLevelConfig from JSON", true)]
    public static bool ValidateCreateFromSelected()
    {
        return Selection.activeObject is TextAsset;
    }

    [MenuItem("Tools/GameLevel/Import Single JSON...", priority = 210)]
    public static void ShowImportWindow()
    {
        string path = EditorUtility.OpenFilePanel(
            "Select JSON level file",
            "Assets/5.Configs/GameLevel/TextAsset",
            "json");

        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        string fileName = Path.GetFileNameWithoutExtension(path);
        string assetDir = "Assets/5.Configs/GameLevel/Levels/Imported";

        if (!AssetDatabase.IsValidFolder(assetDir))
        {
            string parent = "Assets/5.Configs/GameLevel/Levels";
            string folder = "Imported";
            AssetDatabase.CreateFolder(parent, folder);
        }

        LevelData levelData = JsonUtility.FromJson<LevelData>(json);
        if (levelData?.Arrows == null || levelData.Arrows.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No arrows found in {fileName}", "OK");
            return;
        }

        string assetName = SanitizeAssetName(fileName);
        string assetPath = Path.Combine(assetDir, assetName + ".asset");

        GameLevelConfig config = ScriptableObject.CreateInstance<GameLevelConfig>();
        GameLevelTextAssetImporter.MapLevelData(config, levelData);
        config.name = assetName;

        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);

        Debug.Log($"Created GameLevelConfig: {assetPath}");
    }

    private static string SanitizeAssetName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            name = name.Replace(c.ToString(), "");
        name = name.Replace(" ", "_");
        if (name.Length > 200)
            name = name.Substring(0, 200);
        name = name.Trim('.');
        if (string.IsNullOrEmpty(name))
            name = "Level";
        return name;
    }
}
