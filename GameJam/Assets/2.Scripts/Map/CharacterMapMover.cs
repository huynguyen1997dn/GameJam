using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the map character hop-by-hop along the waypoint graph (BFS pathfinding).
/// Plain singleton (not Singleton&lt;T&gt;) because it lives on the character sprite,
/// typically a child of the map root where DontDestroyOnLoad is invalid.
/// </summary>
public class CharacterMapMover : MonoBehaviour
{
    public static CharacterMapMover Instance { get; private set; }

    [SerializeField] private Waypoint currentWaypoint; // starting waypoint, assign in Inspector
    [SerializeField] private float hopDuration = 0.3f; // seconds per hop between adjacent waypoints
    [SerializeField] private float hopArcHeight = 0.3f;
    [SerializeField] private AnimationCurve hopEase;   // horizontal easing; ease-in-out when left empty

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (currentWaypoint != null)
            transform.position = currentWaypoint.transform.position;
    }

    public void MoveToWaypoint(Waypoint target, Action onArrive)
    {
        if (IsMoving || target == null) return;

        if (currentWaypoint == null)
        {
            Debug.LogWarning("[CharacterMapMover] No current waypoint assigned.");
            return;
        }

        var path = FindPath(currentWaypoint, target);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[CharacterMapMover] No path found from '{currentWaypoint.name}' to '{target.name}'.");
            return;
        }

        StartCoroutine(HopAlongPath(path, onArrive));
    }

    private List<Waypoint> FindPath(Waypoint start, Waypoint target)
    {
        var queue = new Queue<Waypoint>();
        var cameFrom = new Dictionary<Waypoint, Waypoint> { { start, start } };
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                var path = new List<Waypoint>();
                for (var wp = target; wp != start; wp = cameFrom[wp])
                    path.Add(wp);
                path.Add(start);
                path.Reverse();
                return path;
            }

            if (current.neighbors == null) continue;
            foreach (var next in current.neighbors)
            {
                if (next == null || cameFrom.ContainsKey(next)) continue;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        return null; // unreachable
    }

    private IEnumerator HopAlongPath(List<Waypoint> path, Action onArrive)
    {
        IsMoving = true;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 from = path[i - 1].transform.position;
            Vector3 to = path[i].transform.position;

            float elapsed = 0f;
            while (elapsed < hopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hopDuration);
                float eased = (hopEase != null && hopEase.length > 1)
                    ? hopEase.Evaluate(t)
                    : Mathf.SmoothStep(0f, 1f, t);

                Vector3 pos = Vector3.Lerp(from, to, eased);
                pos.y += Mathf.Sin(t * Mathf.PI) * hopArcHeight;
                transform.position = pos;
                yield return null;
            }

            transform.position = to;
            currentWaypoint = path[i];
        }

        IsMoving = false;
        onArrive?.Invoke();
    }
}
