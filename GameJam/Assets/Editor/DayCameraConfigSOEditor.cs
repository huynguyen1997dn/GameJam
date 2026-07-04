using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds a "Create/Sync Bounding Boxes In Scene" button to the DayCameraConfigSO
/// inspector: entries with an empty name get an auto name (CameraBounds_Day{n}),
/// then every named entry gets a matching CameraBoundsBox object in the open scene
/// (grouped under a "CameraBounds" root) if one doesn't exist yet. Existing boxes
/// are left untouched so hand-tuned sizes survive re-syncs.
/// </summary>
[CustomEditor(typeof(DayCameraConfigSO))]
public class DayCameraConfigSOEditor : Editor
{
    private const string RootName = "CameraBounds";
    private static readonly Vector2 DefaultBoxSize = new Vector2(20f, 12f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("Creates one BoxCollider2D per entry in the open scene so the camera can be clamped per day. Size the boxes in the scene view; they draw their outline permanently.", MessageType.Info);
        if (GUILayout.Button("Create/Sync Bounding Boxes In Scene"))
            SyncBoundingBoxes((DayCameraConfigSO)target);
    }

    private static void SyncBoundingBoxes(DayCameraConfigSO config)
    {
        var entries = config.Configs;
        if (entries == null || entries.Count == 0)
        {
            Debug.LogWarning("[DayCameraConfigSOEditor] No config entries — nothing to create.");
            return;
        }

        Undo.RecordObject(config, "Sync Camera Bounding Boxes");
        bool assetChanged = false;

        GameObject root = GameObject.Find(RootName);
        int created = 0;

        foreach (var entry in entries)
        {
            if (entry == null) continue;

            if (string.IsNullOrEmpty(entry.boundingBoxName))
            {
                entry.boundingBoxName = $"CameraBounds_Day{entry.day}";
                assetChanged = true;
            }

            if (CameraBoundsBox.Find(entry.boundingBoxName) != null) continue;

            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Sync Camera Bounding Boxes");
            }

            var go = new GameObject(entry.boundingBoxName);
            Undo.RegisterCreatedObjectUndo(go, "Sync Camera Bounding Boxes");
            go.transform.SetParent(root.transform, false);
            go.layer = 2; // Ignore Raycast — must not block map-node clicks

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = DefaultBoxSize;
            go.AddComponent<CameraBoundsBox>();
            created++;
        }

        if (assetChanged) EditorUtility.SetDirty(config);
        if (root != null) EditorGUIUtility.PingObject(root);
        Debug.Log($"[DayCameraConfigSOEditor] Sync done — {created} bounding box(es) created under '{RootName}'.");
    }
}
