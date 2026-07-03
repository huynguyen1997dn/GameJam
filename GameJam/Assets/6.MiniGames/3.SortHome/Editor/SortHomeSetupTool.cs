using UnityEditor;
using UnityEngine;

public class SortHomeSetupTool : EditorWindow
{
    [MenuItem("Tools/SortHome Setup")]
    public static void ShowWindow()
    {
        GetWindow<SortHomeSetupTool>("SortHome Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("SortHome - Setup Assets", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("1. Create Item Prefab", GUILayout.Height(30)))
        {
            CreateItemPrefab();
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
            CreateItemPrefab();
            CreateConfigAsset();
            CreateManagerPrefab();
            RegisterInMiniGameConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After setup:\n" +
            "- Set sourceSprite (background map, full alpha) and add items with sprites & home positions\n" +
            "- Set itemPrefab to SortHomeItem.prefab\n" +
            "- Optional: set slotPrefab for custom placeholder visuals\n" +
            "- Adjust puzzleSize, scatterRadius, snapDistance\n" +
            "- Open SortHomeManager.prefab to verify\n" +
            "- The MiniGameConfig.asset should now have SortHome registered",
            MessageType.Info);
    }

    private void CreateItemPrefab()
    {
        string path = "Assets/6.MiniGames/3.SortHome/Prefabs/SortHomeItem.prefab";

        GameObject go = new GameObject("SortHomeItem");
        go.AddComponent<SpriteRenderer>();
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        go.AddComponent<SortHomeItem>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        Debug.Log($"[SortHome] Created item prefab at: {path}");
    }

    private void CreateConfigAsset()
    {
        string path = "Assets/6.MiniGames/3.SortHome/Resources/SortHomeConfig.asset";

        SortHomeConfig existing = AssetDatabase.LoadAssetAtPath<SortHomeConfig>(path);
        if (existing != null)
        {
            Debug.Log("[SortHome] Config asset already exists. Selecting it.");
            Selection.activeObject = existing;
            return;
        }

        SortHomeConfig config = ScriptableObject.CreateInstance<SortHomeConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        Debug.Log($"[SortHome] Created config asset at: {path}");
    }

    private void RegisterInMiniGameConfig()
    {
        string configPath = "Assets/6.MiniGames/MiniGameConfig.asset";
        MiniGameConfigSO miniGameConfig = AssetDatabase.LoadAssetAtPath<MiniGameConfigSO>(configPath);
        if (miniGameConfig == null)
        {
            Debug.LogError("[SortHome] MiniGameConfig.asset not found!");
            return;
        }

        SerializedObject so = new SerializedObject(miniGameConfig);
        SerializedProperty entries = so.FindProperty("_entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty typeProp = entry.FindPropertyRelative("type");
            if (typeProp.intValue == (int)MiniGameType.SortHome)
            {
                Debug.Log("[SortHome] Already registered in MiniGameConfig.asset");
                return;
            }
        }

        entries.InsertArrayElementAtIndex(entries.arraySize);
        SerializedProperty newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        newEntry.FindPropertyRelative("type").intValue = (int)MiniGameType.SortHome;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/6.MiniGames/3.SortHome/SortHomeManager.prefab");
        if (prefab != null)
        {
            newEntry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }

        newEntry.FindPropertyRelative("displayName").stringValue = "SortHome";

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log("[SortHome] Registered in MiniGameConfig.asset successfully!");
    }

    private void CreateManagerPrefab()
    {
        string path = "Assets/6.MiniGames/3.SortHome/SortHomeManager.prefab";

        GameObject go = new GameObject("SortHomeManager");
        SortHomeManager manager = go.AddComponent<SortHomeManager>();

        SortHomeConfig config = AssetDatabase.LoadAssetAtPath<SortHomeConfig>(
            "Assets/6.MiniGames/3.SortHome/Resources/SortHomeConfig.asset");

        if (config != null)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("_config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        Debug.Log($"[SortHome] Created manager prefab at: {path}");
    }
}
