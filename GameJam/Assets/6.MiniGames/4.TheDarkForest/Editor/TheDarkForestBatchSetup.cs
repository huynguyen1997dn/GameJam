using UnityEditor;
using UnityEngine;

public static class TheDarkForestBatchSetup
{
    [MenuItem("Tools/TheDarkForest Batch Setup")]
    public static void RunBatchSetup()
    {
        CreateTreePrefab();
        CreateConfigAsset();
        CreateManagerPrefab();
        RegisterInMiniGameConfig();
        Debug.Log("[TheDarkForest] Batch setup complete!");
    }

    private static void CreateTreePrefab()
    {
        string path = "Assets/6.MiniGames/4.TheDarkForest/Prefabs/TheDarkForestTree.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            Debug.Log("[TheDarkForest] Tree prefab already exists.");
            return;
        }

        GameObject go = new GameObject("TheDarkForestTree");
        go.AddComponent<SpriteRenderer>();
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        go.AddComponent<TheDarkForestTree>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        MonoBehaviour.DestroyImmediate(go);
        Debug.Log($"[TheDarkForest] Created tree prefab at: {path}");
    }

    private static void CreateConfigAsset()
    {
        string path = "Assets/6.MiniGames/4.TheDarkForest/Resources/TheDarkForestConfig.asset";

        TheDarkForestConfig existing = AssetDatabase.LoadAssetAtPath<TheDarkForestConfig>(path);
        if (existing != null)
        {
            Debug.Log("[TheDarkForest] Config asset already exists.");
            return;
        }

        TheDarkForestConfig config = ScriptableObject.CreateInstance<TheDarkForestConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TheDarkForest] Created config asset at: {path}");
    }

    private static void RegisterInMiniGameConfig()
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

    private static void CreateManagerPrefab()
    {
        string path = "Assets/6.MiniGames/4.TheDarkForest/TheDarkForestManager.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            Debug.Log("[TheDarkForest] Manager prefab already exists.");
            return;
        }

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
        // DestroyImmediate(go);
        MonoBehaviour.DestroyImmediate(go);

        Debug.Log($"[TheDarkForest] Created manager prefab at: {path}");
    }
}
