using UnityEditor;
using UnityEngine;

public class TheDarkForestSetupTool : EditorWindow
{
    [MenuItem("Tools/TheDarkForest Setup")]
    public static void ShowWindow()
    {
        GetWindow<TheDarkForestSetupTool>("TheDarkForest Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("TheDarkForest - Setup Assets", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("1. Create Tree Prefab", GUILayout.Height(30)))
        {
            CreateTreePrefab();
        }

        if (GUILayout.Button("2. Create Config Asset", GUILayout.Height(30)))
        {
            CreateConfigAsset();
        }

        if (GUILayout.Button("3. Create Manager Prefab", GUILayout.Height(30)))
        {
            CreateManagerPrefab();
        }

        if (GUILayout.Button("4. Register in MiniGameConfig.asset", GUILayout.Height(30)))
        {
            RegisterInMiniGameConfig();
        }

        if (GUILayout.Button("5. Do All Steps", GUILayout.Height(40)))
        {
            CreateTreePrefab();
            CreateConfigAsset();
            CreateManagerPrefab();
            RegisterInMiniGameConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After setup:\n" +
            "- Place sprites in Art/ folder\n" +
            "- Open TheDarkForestConfig.asset and assign:\n" +
            "  - correctTreeSprite, wrongTreeSprite, towerSprite\n" +
            "- Adjust rows, treesPerRow, spacing, etc.\n" +
            "- Open TheDarkForestManager.prefab to verify\n" +
            "- The MiniGameConfig.asset should now have TheDarkForest registered",
            MessageType.Info);
    }

    private void CreateTreePrefab()
    {
        string path = "Assets/6.MiniGames/4.TheDarkForest/Prefabs/TheDarkForestTree.prefab";

        GameObject go = new GameObject("TheDarkForestTree");
        go.AddComponent<SpriteRenderer>();
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        go.AddComponent<TheDarkForestTree>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        Debug.Log($"[TheDarkForest] Created tree prefab at: {path}");
    }

    private void CreateConfigAsset()
    {
        string path = "Assets/6.MiniGames/4.TheDarkForest/Resources/TheDarkForestConfig.asset";

        TheDarkForestConfig existing = AssetDatabase.LoadAssetAtPath<TheDarkForestConfig>(path);
        if (existing != null)
        {
            Debug.Log("[TheDarkForest] Config asset already exists. Selecting it.");
            Selection.activeObject = existing;
            return;
        }

        TheDarkForestConfig config = ScriptableObject.CreateInstance<TheDarkForestConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        Debug.Log($"[TheDarkForest] Created config asset at: {path}");
    }

    private void RegisterInMiniGameConfig()
    {
        string configPath = "Assets/6.MiniGames/MiniGameConfig.asset";
        MiniGameConfigSO miniGameConfig = AssetDatabase.LoadAssetAtPath<MiniGameConfigSO>(configPath);
        if (miniGameConfig == null)
        {
            Debug.LogError("[TheDarkForest] MiniGameConfig.asset not found!");
            return;
        }

        SerializedObject so = new SerializedObject(miniGameConfig);
        SerializedProperty entries = so.FindProperty("_entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty typeProp = entry.FindPropertyRelative("type");
            if (typeProp.intValue == (int)MiniGameType.TheDarkForest)
            {
                Debug.Log("[TheDarkForest] Already registered in MiniGameConfig.asset");
                return;
            }
        }

        entries.InsertArrayElementAtIndex(entries.arraySize);
        SerializedProperty newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        newEntry.FindPropertyRelative("type").intValue = (int)MiniGameType.TheDarkForest;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/6.MiniGames/4.TheDarkForest/TheDarkForestManager.prefab");
        if (prefab != null)
        {
            newEntry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }

        newEntry.FindPropertyRelative("displayName").stringValue = "TheDarkForest";

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log("[TheDarkForest] Registered in MiniGameConfig.asset successfully!");
    }

    private void CreateManagerPrefab()
    {
        string path = "Assets/6.MiniGames/4.TheDarkForest/TheDarkForestManager.prefab";

        GameObject go = new GameObject("TheDarkForestManager");
        TheDarkForestManager manager = go.AddComponent<TheDarkForestManager>();

        TheDarkForestConfig config = AssetDatabase.LoadAssetAtPath<TheDarkForestConfig>(
            "Assets/6.MiniGames/4.TheDarkForest/Resources/TheDarkForestConfig.asset");

        if (config != null)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("_config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        Debug.Log($"[TheDarkForest] Created manager prefab at: {path}");
    }
}
