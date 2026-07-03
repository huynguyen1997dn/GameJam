using UnityEngine;

/// <summary>
/// Pure data holder placed on empty GameObjects along walkable paths of the
/// overview map. Neighbors are wired manually in the Inspector (undirected:
/// link both ends by hand).
/// </summary>
public class Waypoint : MonoBehaviour
{
    public string waypointId; // optional, for debugging
    public Waypoint[] neighbors;

    [Header("Progress Gate")]
    public string[] requiredRoomIds; // impassable (blocks the path through it) until all these rooms are solved

    public bool IsPassable()
    {
        if (requiredRoomIds == null || requiredRoomIds.Length == 0) return true;

        var gsm = GameStateManager.Instance;
        if (gsm == null) return true;

        foreach (var id in requiredRoomIds)
            if (!gsm.IsSolved(id)) return false;
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Orange marks gated waypoints. Static check only — querying solve state here
        // would lazily spawn the GameStateManager singleton in edit mode.
        Gizmos.color = requiredRoomIds != null && requiredRoomIds.Length > 0
            ? new Color(1f, 0.55f, 0.15f)
            : Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.08f);

        if (neighbors == null) return;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
#endif
}
