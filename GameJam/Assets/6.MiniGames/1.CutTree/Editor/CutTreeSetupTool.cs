using System.IO;
using UnityEditor;
using UnityEngine;

public static class CutTreeSetupTool
{
    private const string FolderPath = "Assets/6.MiniGames/1.CutTree";

    [MenuItem("Tools/CutTree/Setup Placeholder Assets", false, 300)]
    public static void Setup()
    {
        CreatePlaceholderTexture();
        CreatePrefab();
        CreateConfigAsset();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CutTree] Setup complete! Placeholder assets created.");
    }

    private static void CreatePlaceholderTexture()
    {
        string texPath = $"{FolderPath}/Textures";
        Directory.CreateDirectory(texPath);
        string path = $"{texPath}/TreePlaceholder.png";

        if (File.Exists(path)) return;

        Texture2D tex = new Texture2D(64, 64);
        Color green = new Color(0.2f, 0.7f, 0.2f);
        Color darkGreen = new Color(0.1f, 0.4f, 0.1f);
        Color brown = new Color(0.5f, 0.3f, 0.1f);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                // Trunk
                if (x >= 28 && x <= 36 && y >= 0 && y <= 20)
                    tex.SetPixel(x, y, brown);
                // Foliage (triangle shape)
                else if (y >= 18 && y <= 55)
                {
                    float halfWidth = (y - 18) / 37f * 30f + 5f;
                    if (Mathf.Abs(x - 32) <= halfWidth)
                        tex.SetPixel(x, y, y > 40 ? green : darkGreen);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.SaveAndReimport();
        }

        Debug.Log("[CutTree] Placeholder texture created.");
    }

    private static void CreatePrefab()
    {
        string prefabPath = $"{FolderPath}/Prefabs/Tree.prefab";
        if (File.Exists(prefabPath)) return;

        string texPath = $"{FolderPath}/Textures/TreePlaceholder.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (!sprite)
        {
            Debug.LogError("[CutTree] Cannot find placeholder texture!");
            return;
        }

        GameObject go = new GameObject("Tree", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(TreeController));

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 0;

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1f);
        col.offset = new Vector2(0, 0.1f);

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log("[CutTree] Prefab created.");
    }

    private static void CreateConfigAsset()
    {
        string configPath = $"{FolderPath}/Resources/CutTreeConfig.asset";
        if (File.Exists(configPath)) return;

        CutTreeConfig config = ScriptableObject.CreateInstance<CutTreeConfig>();

        string prefabPath = $"{FolderPath}/Prefabs/Tree.prefab";
        config.treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        config.minBounds = new Vector2(-5f, -3f);
        config.maxBounds = new Vector2(5f, 3f);
        config.initialTreeCount = 10;
        config.targetTreesToCut = 30;
        config.respawnDelay = 1.5f;
        config.treeColor = Color.green;
        config.treeSize = new Vector2(0.8f, 1.2f);
        config.chopSoundId = "AUDIO_CHOP";
        config.completeSoundId = "AUDIO_COMPLETE";

        AssetDatabase.CreateAsset(config, configPath);
        Debug.Log("[CutTree] Config asset created.");
    }
}
