using UnityEditor;
using UnityEngine;

public class TorPaintingSetupTool : EditorWindow
{
    [MenuItem("Tools/TorPainting Setup")]
    public static void ShowWindow()
    {
        GetWindow<TorPaintingSetupTool>("TorPainting Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("TorPainting - Setup Assets", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("1. Create PuzzlePiece Prefab", GUILayout.Height(30)))
        {
            CreatePiecePrefab();
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
            CreatePiecePrefab();
            CreateConfigAsset();
            CreateManagerPrefab();
            RegisterInMiniGameConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After setup:\n" +
            "- Assign a Sprite to TorPaintingConfig.asset (in Resources/)\n" +
            "- Set piecePrefab to PuzzlePiece.prefab\n" +
            "- Adjust pieceCount, puzzleSize, etc.\n" +
            "- Open TorPaintingManager.prefab to verify\n" +
            "- The MiniGameConfig.asset should now have TorPainting registered",
            MessageType.Info);
    }

    private void CreatePiecePrefab()
    {
        string path = "Assets/6.MiniGames/2.TorPainting/Prefabs/PuzzlePiece.prefab";

        GameObject go = new GameObject("PuzzlePiece");
        go.AddComponent<SpriteRenderer>();
        PolygonCollider2D col = go.AddComponent<PolygonCollider2D>();
        col.isTrigger = false;
        go.AddComponent<TorPaintingPiece>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        Debug.Log($"[TorPainting] Created piece prefab at: {path}");
    }

    private void CreateConfigAsset()
    {
        string path = "Assets/6.MiniGames/2.TorPainting/Resources/TorPaintingConfig.asset";

        TorPaintingConfig existing = AssetDatabase.LoadAssetAtPath<TorPaintingConfig>(path);
        if (existing != null)
        {
            Debug.Log("[TorPainting] Config asset already exists. Selecting it.");
            Selection.activeObject = existing;
            return;
        }

        TorPaintingConfig config = ScriptableObject.CreateInstance<TorPaintingConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        Debug.Log($"[TorPainting] Created config asset at: {path}");
    }

    private void RegisterInMiniGameConfig()
    {
        string configPath = "Assets/6.MiniGames/MiniGameConfig.asset";
        MiniGameConfigSO config = AssetDatabase.LoadAssetAtPath<MiniGameConfigSO>(configPath);
        if (config == null)
        {
            Debug.LogError("[TorPainting] MiniGameConfig.asset not found!");
            return;
        }

        SerializedObject so = new SerializedObject(config);
        SerializedProperty entries = so.FindProperty("_entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty typeProp = entry.FindPropertyRelative("type");
            if (typeProp.intValue == (int)MiniGameType.TorPainting)
            {
                Debug.Log("[TorPainting] Already registered in MiniGameConfig.asset");
                return;
            }
        }

        entries.InsertArrayElementAtIndex(entries.arraySize);
        SerializedProperty newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        newEntry.FindPropertyRelative("type").intValue = (int)MiniGameType.TorPainting;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/6.MiniGames/2.TorPainting/TorPaintingManager.prefab");
        if (prefab != null)
        {
            newEntry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }

        newEntry.FindPropertyRelative("displayName").stringValue = "TorPainting";

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log("[TorPainting] Registered in MiniGameConfig.asset successfully!");
    }

    private void CreateManagerPrefab()
    {
        string path = "Assets/6.MiniGames/2.TorPainting/TorPaintingManager.prefab";

        GameObject go = new GameObject("TorPaintingManager");
        TorPaintingManager manager = go.AddComponent<TorPaintingManager>();

        TorPaintingConfig config = AssetDatabase.LoadAssetAtPath<TorPaintingConfig>(
            "Assets/6.MiniGames/2.TorPainting/Resources/TorPaintingConfig.asset");

        if (config != null)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("_config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        Debug.Log($"[TorPainting] Created manager prefab at: {path}");
    }
}
