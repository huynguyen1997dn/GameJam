using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Depth-sorts an object on the diagonal isometric map by driving a SortingGroup's
/// sortingOrder from its world position: lower on screen (smaller Y) = drawn in front.
/// Add to the root of buildings, trees, characters, etc. — the SortingGroup keeps
/// multi-sprite objects and Spine skeletons ordered as one block against neighbors.
///
/// Static props sort once on enable (and live in edit mode while decorating);
/// tick Dynamic for anything that moves so it resorts every LateUpdate.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SortingGroup))]
public class IsometricUnit : MonoBehaviour
{
    [Tooltip("Tick for moving objects (characters). Static props sort once on enable.")]
    [SerializeField] private bool dynamic;

    [Tooltip("Local offset from the transform to the visual base (feet / building footprint center). Depth is measured at this point.")]
    [SerializeField] private Vector2 sortPointOffset;

    [Header("Resolution")]
    [Tooltip("Sorting order steps per world unit of Y. Higher = finer separation, but sortingOrder is clamped to ±32767, so mapHeight * precision must stay under that.")]
    [SerializeField] private int precisionY = 100;

    [Tooltip("X tiebreaker steps per world unit, for objects at the same Y. Keep well below Precision Y or it will override Y ordering.")]
    [SerializeField] private int precisionX = 2;

    private SortingGroup _sortingGroup;

    /// <summary>World-space point depth is measured at.</summary>
    public Vector3 SortPoint => transform.position + (Vector3)sortPointOffset;

    private void OnEnable()
    {
        _sortingGroup = GetComponent<SortingGroup>();
        Resort();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            if (dynamic) Resort();
        }
#if UNITY_EDITOR
        else
        {
            Resort(); // live feedback while dragging props around in the scene view
        }
#endif
    }

    /// <summary>Recomputes the sorting order from the current position. Call after teleporting a static object at runtime.</summary>
    public void Resort()
    {
        if (_sortingGroup == null) return;

        var p = SortPoint;
        var order = Mathf.RoundToInt(-p.y * precisionY - p.x * precisionX);
        _sortingGroup.sortingOrder = Mathf.Clamp(order, short.MinValue, short.MaxValue);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        precisionY = Mathf.Max(1, precisionY);
        precisionX = Mathf.Max(0, precisionX);
    }

    private void OnDrawGizmosSelected()
    {
        // Yellow sphere = root, green cross = sort point — everything whose sort
        // point is above the cross draws behind.
        var root = transform.position;
        var p = SortPoint;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(root, 0.06f);

        Gizmos.color = new Color(0.3f, 1f, 0.4f);
        if (sortPointOffset != Vector2.zero)
            Gizmos.DrawLine(root, p);
        Gizmos.DrawLine(p + Vector3.left * 0.25f, p + Vector3.right * 0.25f);
        Gizmos.DrawLine(p + Vector3.down * 0.1f, p + Vector3.up * 0.1f);
    }
#endif
}
