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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
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
