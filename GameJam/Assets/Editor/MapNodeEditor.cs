using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds a "State Preview & Alignment" section to the MapNode inspector for fixing
/// source sprites whose center/size don't match across states:
/// - toolbar to preview each state's sprite in the scene view (Before/After for 2-state nodes)
/// - per-state offset & scale, applied to the visual child the moment they change
/// - capture/reset helpers and a size-mismatch readout of the sprite bounds
/// Adjust data lives in MapNode.stateAdjusts and is re-applied at runtime on every
/// state change, so what you align here is exactly what ships.
/// </summary>
[CustomEditor(typeof(MapNode))]
[CanEditMultipleObjects]
public class MapNodeEditor : Editor
{
    private const string UndoLabel = "MapNode State Alignment";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox("State preview & alignment is available when a single MapNode is selected.", MessageType.Info);
            return;
        }

        var node = (MapNode)target;
        var sprites = node.stateSprites;
        if (sprites == null || sprites.Length == 0) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("State Preview & Alignment", EditorStyles.boldLabel);

        var sr = node.EditorResolveVisualRenderer();
        if (sr == null)
        {
            EditorGUILayout.HelpBox("No SpriteRenderer found — assign Visual Renderer or add one on a child.", MessageType.Warning);
            return;
        }

        bool rendererOnRoot = sr.transform == node.transform;
        if (rendererOnRoot)
            EditorGUILayout.HelpBox("The visual renderer sits on the MapNode object itself, so offset/scale can't be applied (that transform is the node's map placement). Move the renderer to a \"Visual\" child to enable alignment.", MessageType.Warning);

        DrawSpriteSizeReadout(sprites);

        serializedObject.Update();
        SyncAdjustArray(node, sr);

        var previewProp = serializedObject.FindProperty("editorPreviewState");
        int preview = Mathf.Clamp(previewProp.intValue, 0, sprites.Length - 1);

        // State switch toolbar — the whole point of the tool: flip Before/After in place.
        var labels = new string[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            labels[i] = StateLabel(i, sprites.Length);

        EditorGUI.BeginChangeCheck();
        preview = GUILayout.Toolbar(preview, labels);
        bool stateSwitched = EditorGUI.EndChangeCheck();
        previewProp.intValue = preview;

        bool adjustChanged = false;
        if (!rendererOnRoot)
        {
            var adjust = serializedObject.FindProperty("stateAdjusts").GetArrayElementAtIndex(preview);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(adjust.FindPropertyRelative("offset"), new GUIContent("Offset", "Visual child localPosition for this state"));
            EditorGUILayout.PropertyField(adjust.FindPropertyRelative("scale"), new GUIContent("Scale", "Visual child localScale for this state"));
            adjustChanged = EditorGUI.EndChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Capture From Visual", "Copy the visual child's current localPosition/localScale into this state (after nudging it in the scene view)")))
                {
                    adjust.FindPropertyRelative("offset").vector2Value = sr.transform.localPosition;
                    adjust.FindPropertyRelative("scale").vector2Value = sr.transform.localScale;
                    adjustChanged = true;
                }
                if (GUILayout.Button(new GUIContent("Reset", "Back to offset (0,0), scale (1,1)")))
                {
                    adjust.FindPropertyRelative("offset").vector2Value = Vector2.zero;
                    adjust.FindPropertyRelative("scale").vector2Value = Vector2.one;
                    adjustChanged = true;
                }
            }
        }

        // Record before ApplyModifiedProperties: it triggers OnValidate, which already
        // moves the renderer/transform, and changes recorded after the fact don't undo.
        if (stateSwitched || adjustChanged)
        {
            Undo.RecordObject(sr, UndoLabel);
            Undo.RecordObject(sr.transform, UndoLabel);
        }

        serializedObject.ApplyModifiedProperties();

        // Apply immediately so the scene view always shows the previewed state as tuned.
        if (stateSwitched || adjustChanged)
            node.EditorApplyState(preview);
    }

    // Missing/short adjust arrays mean "don't touch the transform" at runtime, so only
    // grow the array here, seeding new entries from the visual child's current values —
    // creating the data must never move anything.
    private void SyncAdjustArray(MapNode node, SpriteRenderer sr)
    {
        var adjusts = serializedObject.FindProperty("stateAdjusts");
        int from = adjusts.arraySize;
        int to = node.stateSprites.Length;
        if (from >= to) return;

        adjusts.arraySize = to;
        for (int i = from; i < to; i++)
        {
            var entry = adjusts.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("offset").vector2Value = sr.transform.localPosition;
            entry.FindPropertyRelative("scale").vector2Value = sr.transform.localScale;
        }
    }

    // Surfaces the root cause this tool exists for: state sprites authored at different
    // sizes. Bounds are in world units (pixels-per-unit already applied).
    private static void DrawSpriteSizeReadout(Sprite[] sprites)
    {
        bool mismatch = false;
        var sb = new System.Text.StringBuilder();
        Vector2? firstSize = null;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            Vector2 size = sprites[i].bounds.size;
            if (firstSize == null) firstSize = size;
            else if ((size - firstSize.Value).sqrMagnitude > 0.0001f) mismatch = true;
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"{StateLabel(i, sprites.Length)}: {sprites[i].name} — {size.x:0.###} x {size.y:0.###}");
        }
        if (sb.Length == 0) return;
        EditorGUILayout.HelpBox(sb.ToString(), mismatch ? MessageType.Warning : MessageType.None);
    }

    private static string StateLabel(int index, int count)
    {
        if (count == 2) return index == 0 ? "Before" : "After";
        return index == 0 ? "State 0 (Before)" : $"State {index}";
    }
}
