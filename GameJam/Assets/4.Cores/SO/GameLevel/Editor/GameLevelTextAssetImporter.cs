using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GameLevelTextAssetImporter
{
    private const string TextAssetDir = "Assets/5.Configs/GameLevel/RefTextAsset";
    private const string OutputDir = "Assets/5.Configs/GameLevel/Levels/Imported";
    private const string ConfigsAssetPath = "Assets/5.Configs/GameLevel/GameLevelConfigs.asset";

    [MenuItem("Tools/GameLevel/Import From TextAssets")]
    public static void ImportAll()
    {
        if (!AssetDatabase.IsValidFolder(OutputDir))
        {
            string parent = "Assets/5.Configs/GameLevel/Levels";
            string folder = "Imported";
            AssetDatabase.CreateFolder(parent, folder);
        }

        string[] jsonFiles = Directory.GetFiles(TextAssetDir, "*.json")
            .Where(f => !f.EndsWith(".meta"))
            .OrderBy(f => f)
            .ToArray();

        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Import", "No JSON files found in TextAsset directory.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog(
            "Import Levels",
            $"Found {jsonFiles.Length} JSON files.\n" +
            $"Output: {OutputDir}\n" +
            $"This will rebuild GameLevelConfigs.asset.\n\n" +
            "Proceed?",
            "Import", "Cancel");

        if (!proceed) return;

        List<GameLevelConfig> createdConfigs = new List<GameLevelConfig>();
        HashSet<string> usedNames = new HashSet<string>();
        int successCount = 0;
        int errorCount = 0;

        for (int i = 0; i < jsonFiles.Length; i++)
        {
            string filePath = jsonFiles[i];
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (EditorUtility.DisplayCancelableProgressBar(
                    "Importing Levels",
                    $"({i + 1}/{jsonFiles.Length}) {fileName}",
                    (float)i / jsonFiles.Length))
            {
                EditorUtility.ClearProgressBar();
                Debug.LogWarning($"Import cancelled by user at file {i + 1}/{jsonFiles.Length}");
                break;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                LevelData levelData = JsonUtility.FromJson<LevelData>(json);

                if (levelData?.Arrows == null || levelData.Arrows.Length == 0)
                {
                    Debug.LogWarning($"Skip {fileName}: no arrows found");
                    errorCount++;
                    continue;
                }

                string assetName = GenerateUniqueAssetName(fileName, i + 1, usedNames);
                usedNames.Add(assetName);

                GameLevelConfig config = ScriptableObject.CreateInstance<GameLevelConfig>();
                MapLevelData(config, levelData);
                config.name = assetName;

                string assetPath = Path.Combine(OutputDir, assetName + ".asset");
                AssetDatabase.CreateAsset(config, assetPath);
                createdConfigs.Add(config);
                successCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error importing {fileName}: {ex.Message}");
                errorCount++;
            }
        }

        EditorUtility.ClearProgressBar();

        if (createdConfigs.Count > 0)
        {
            GameLevelConfigs configs = AssetDatabase.LoadAssetAtPath<GameLevelConfigs>(ConfigsAssetPath);
            if (configs == null)
            {
                configs = ScriptableObject.CreateInstance<GameLevelConfigs>();
                AssetDatabase.CreateAsset(configs, ConfigsAssetPath);
            }

            configs.configs = createdConfigs;
            EditorUtility.SetDirty(configs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log(
            $"Import complete: {successCount} success, {errorCount} errors. " +
            $"GameLevelConfigs.asset updated with {createdConfigs.Count} levels.");
    }

    internal static void MapLevelData(GameLevelConfig config, LevelData data)
    {
        config.width = data.XSize;
        config.height = data.YSize;
        config.seed = 0;
        config.autoGenerateAtRuntime = false;
        config.head = 3;
        config.snakeCount = data.Arrows.Length;

        int gridSize = config.width * config.height;
        config.gridData = new int[gridSize];
        for (int i = 0; i < gridSize; i++)
            config.gridData[i] = -1;

        config.ClearSegments();

        int minSnakeSize = int.MaxValue;
        int maxSnakeSize = 0;

        for (int a = 0; a < data.Arrows.Length; a++)
        {
            ArrowData arrow = data.Arrows[a];
            int value = a + 1;
            int width = config.width;

            int startX = arrow.X;
            int startY = arrow.Y;

            int lastIdx = arrow.Indices[arrow.Indices.Length - 1];
            int endX = lastIdx % width;
            int endY = lastIdx / width;

            int[] bodyIndices;
            if (arrow.Indices.Length >= 3)
            {
                bodyIndices = new int[arrow.Indices.Length - 2];
                System.Array.Copy(arrow.Indices, 1, bodyIndices, 0, arrow.Indices.Length - 2);
            }
            else
            {
                bodyIndices = new int[0];
            }

            foreach (int idx in arrow.Indices)
            {
                config.gridData[idx] = value;
            }

            config.AddSegment(value, startX, startY, endX, endY, bodyIndices);

            int snakeSize = arrow.Indices.Length;
            if (snakeSize < minSnakeSize) minSnakeSize = snakeSize;
            if (snakeSize > maxSnakeSize) maxSnakeSize = snakeSize;
        }

        config.minSnakeSize = minSnakeSize == int.MaxValue ? 2 : minSnakeSize;
        config.maxSnakeSize = maxSnakeSize;
    }

    private static string GenerateUniqueAssetName(string fileName, int index, HashSet<string> usedNames)
    {
        string theme = ExtractTheme(fileName);
        string baseName = $"Level_{index:D5}_{theme}";
        string sanitized = SanitizeAssetName(baseName);

        if (!usedNames.Contains(sanitized))
            return sanitized;

        int suffix = 1;
        while (usedNames.Contains($"{sanitized}_{suffix}"))
            suffix++;

        return $"{sanitized}_{suffix}";
    }

    private static string ExtractTheme(string fileName)
    {
        string[] parts = fileName.Split(new[] { "___" }, System.StringSplitOptions.None);
        if (parts.Length >= 2)
        {
            string themePart = parts[parts.Length - 1];
            if (themePart.EndsWith("__0") || themePart.EndsWith("__1"))
                themePart = themePart.Substring(0, themePart.Length - 3);
            return themePart;
        }
        return fileName;
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
