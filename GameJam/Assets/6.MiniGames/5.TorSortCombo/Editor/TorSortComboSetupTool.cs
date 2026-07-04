using UnityEditor;
using UnityEngine;

public class TorSortComboSetupTool : EditorWindow
{
    private const string ConfigPath = "Assets/6.MiniGames/5.TorSortCombo/Resources/TorSortComboConfig.asset";
    private const string ManagerPrefabPath = "Assets/6.MiniGames/5.TorSortCombo/TorSortComboManager.prefab";
    private const string SourceViewPrefabPath = "Assets/8.Resources/Resources/UI/Views/MiniGameGameView_TorPainting.prefab";
    private const string ViewPrefabPath = "Assets/8.Resources/Resources/UI/Views/MiniGameGameView_TorSortCombo.prefab";
    private const string TorPaintingPrefabPath = "Assets/6.MiniGames/2.TorPainting/TorPaintingManager.prefab";
    private const string SortHomePrefabPath = "Assets/6.MiniGames/3.SortHome/SortHomeManager.prefab";

    [MenuItem("Tools/TorSortCombo Setup")]
    public static void ShowWindow()
    {
        GetWindow<TorSortComboSetupTool>("TorSortCombo Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("TorSortCombo - Setup Assets", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("1. Create Config Asset", GUILayout.Height(30)))
        {
            CreateConfigAsset();
        }

        if (GUILayout.Button("2. Create Manager Prefab", GUILayout.Height(30)))
        {
            CreateManagerPrefab();
        }

        if (GUILayout.Button("3. Create View Prefab (clone TorPainting view)", GUILayout.Height(30)))
        {
            CreateViewPrefab();
        }

        if (GUILayout.Button("4. Register in MiniGameConfig.asset", GUILayout.Height(30)))
        {
            RegisterInMiniGameConfig();
        }

        if (GUILayout.Button("5. Do All Steps", GUILayout.Height(40)))
        {
            CreateConfigAsset();
            CreateManagerPrefab();
            CreateViewPrefab();
            RegisterInMiniGameConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After setup:\n" +
            "- TorSortComboConfig.asset references the TorPainting and SortHome manager prefabs\n" +
            "- Backgrounds come from each sub-game's own config (backgroundSprite)\n" +
            "- Open MiniGameGameView_TorSortCombo.prefab and add a Phase text (optional, wire _phaseText)\n" +
            "- The MiniGameConfig.asset should now have TorSortCombo registered",
            MessageType.Info);
    }

    private void CreateConfigAsset()
    {
        TorSortComboConfig existing = AssetDatabase.LoadAssetAtPath<TorSortComboConfig>(ConfigPath);
        if (existing != null)
        {
            Debug.Log("[TorSortCombo] Config asset already exists. Selecting it.");
            Selection.activeObject = existing;
            return;
        }

        EnsureFolder("Assets/6.MiniGames/5.TorSortCombo", "Resources");

        TorSortComboConfig config = ScriptableObject.CreateInstance<TorSortComboConfig>();
        config.torPaintingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TorPaintingPrefabPath);
        config.sortHomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SortHomePrefabPath);

        if (config.torPaintingPrefab == null)
            Debug.LogWarning($"[TorSortCombo] TorPainting prefab not found at {TorPaintingPrefabPath}, assign manually.");
        if (config.sortHomePrefab == null)
            Debug.LogWarning($"[TorSortCombo] SortHome prefab not found at {SortHomePrefabPath}, assign manually.");

        AssetDatabase.CreateAsset(config, ConfigPath);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        Debug.Log($"[TorSortCombo] Created config asset at: {ConfigPath}");
    }

    private void CreateManagerPrefab()
    {
        GameObject go = new GameObject("TorSortComboManager");
        TorSortComboManager manager = go.AddComponent<TorSortComboManager>();

        TorSortComboConfig config = AssetDatabase.LoadAssetAtPath<TorSortComboConfig>(ConfigPath);
        if (config != null)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("_config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(go, ManagerPrefabPath);
        DestroyImmediate(go);

        Debug.Log($"[TorSortCombo] Created manager prefab at: {ManagerPrefabPath}");
    }

    private void CreateViewPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath) != null)
        {
            Debug.Log("[TorSortCombo] View prefab already exists.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(SourceViewPrefabPath) == null)
        {
            Debug.LogError($"[TorSortCombo] Source view prefab not found at {SourceViewPrefabPath}");
            return;
        }

        if (!AssetDatabase.CopyAsset(SourceViewPrefabPath, ViewPrefabPath))
        {
            Debug.LogError("[TorSortCombo] Failed to copy view prefab!");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(ViewPrefabPath);
        try
        {
            var oldView = root.GetComponent<MiniGameGameView_TorPainting>();
            var newView = root.AddComponent<MiniGameGameView_TorSortCombo>();

            if (oldView != null)
            {
                // Carry over all serialized references (slider, button, text,
                // background image, canvas group) except the config, whose type differs.
                var oldSO = new SerializedObject(oldView);
                var newSO = new SerializedObject(newView);
                SerializedProperty prop = oldSO.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.name == "m_Script" || prop.name == "_config") continue;
                    if (newSO.FindProperty(prop.propertyPath) != null)
                        newSO.CopyFromSerializedProperty(prop);
                }
                newSO.ApplyModifiedPropertiesWithoutUndo();
                DestroyImmediate(oldView, true);
            }

            TorSortComboConfig config = AssetDatabase.LoadAssetAtPath<TorSortComboConfig>(ConfigPath);
            if (config != null)
            {
                var so = new SerializedObject(newView);
                so.FindProperty("_config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, ViewPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[TorSortCombo] Created view prefab at: {ViewPrefabPath}");
    }

    private void RegisterInMiniGameConfig()
    {
        string configPath = "Assets/6.MiniGames/MiniGameConfig.asset";
        MiniGameConfigSO config = AssetDatabase.LoadAssetAtPath<MiniGameConfigSO>(configPath);
        if (config == null)
        {
            Debug.LogError("[TorSortCombo] MiniGameConfig.asset not found!");
            return;
        }

        SerializedObject so = new SerializedObject(config);
        SerializedProperty entries = so.FindProperty("_entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty typeProp = entry.FindPropertyRelative("type");
            if (typeProp.intValue == (int)MiniGameType.TorSortCombo)
            {
                Debug.Log("[TorSortCombo] Already registered in MiniGameConfig.asset");
                return;
            }
        }

        entries.InsertArrayElementAtIndex(entries.arraySize);
        SerializedProperty newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        newEntry.FindPropertyRelative("type").intValue = (int)MiniGameType.TorSortCombo;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
        if (prefab != null)
        {
            newEntry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }

        newEntry.FindPropertyRelative("displayName").stringValue = "TorSortCombo";

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log("[TorSortCombo] Registered in MiniGameConfig.asset successfully!");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }
}
