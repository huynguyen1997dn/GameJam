using UnityEngine;

/// <summary>
/// Per-day camera bounding box: an authoring rectangle (BoxCollider2D) the map
/// camera's view is clamped inside. Created by the Create/Sync button on
/// DayCameraConfigSO and resolved by name at runtime; the collider is a trigger on
/// the Ignore Raycast layer so it never swallows map-node clicks. Always draws its
/// outline in the scene view so the box can be sized without selecting it.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CameraBoundsBox : MonoBehaviour
{
    // Computed from offset/size instead of collider.bounds so it stays valid while
    // the object (or collider) is disabled.
    public Bounds WorldBounds
    {
        get
        {
            var box = GetComponent<BoxCollider2D>();
            Vector3 center = transform.TransformPoint(box.offset);
            Vector3 size = Vector3.Scale(box.size, transform.lossyScale);
            return new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 0f));
        }
    }

    public static CameraBoundsBox Find(string boundsName)
    {
        if (string.IsNullOrEmpty(boundsName)) return null;

        var all = FindObjectsByType<CameraBoundsBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var box in all)
            if (box.name == boundsName) return box;
        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var b = WorldBounds;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
        UnityEditor.Handles.Label(new Vector3(b.min.x, b.max.y, 0f), name);
    }
#endif
}
