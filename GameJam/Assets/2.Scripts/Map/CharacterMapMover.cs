using System;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// Moves the map character along the waypoint graph (BFS pathfinding) as a chain of
/// short parabolic jumps — up then fall, repeatedly, until the target is reached —
/// with a small landing beat between jumps and constant ground speed (projectile feel).
/// Turn `hopEnabled` off for a flat constant-speed glide at `moveSpeed` instead,
/// when a walk animation (e.g. Spine) supplies the motion.
/// Assign `visual` (the sprite child) so the jump offset stays off the root transform:
/// the camera follows the root, so jumps stay fully visible on screen instead of being
/// smoothed away by the follow damping.
/// Plain singleton (not Singleton&lt;T&gt;) because it lives on the character sprite,
/// typically a child of the map root where DontDestroyOnLoad is invalid.
/// </summary>
public class CharacterMapMover : MonoBehaviour
{
    public static CharacterMapMover Instance { get; private set; }

    [SerializeField] private Waypoint currentWaypoint; // starting waypoint, assign in Inspector
    [SerializeField] private Transform visual;         // sprite child lifted during jumps; root is used if empty

    [Header("Jump Feel")]
    [SerializeField] private bool hopEnabled = true;      // off = flat linear glide (e.g. when a Spine walk anim carries the motion)
    [SerializeField] private float jumpLength = 1f;       // max ground distance covered by one jump
    [SerializeField] private float jumpDuration = 0.25f;  // seconds one jump is airborne
    [SerializeField] private float jumpHeight = 0.5f;     // apex height of each jump
    [SerializeField] private float jumpInterval = 0.05f;  // landing beat between consecutive jumps

    [Header("Linear Move (hop disabled)")]
    [SerializeField] private float moveSpeed = 3.5f;      // ground units per second

    [Header("Sound")]
    [SerializeField] private float walkSfxInterval = 0.5f; // seconds between AUDIO_WALK plays while moving

    [Header("Animation")]
    [SerializeField] private SkeletonAnimation _skeletonFront;
    [SerializeField] private SkeletonAnimation _skeletonBack;

    public bool IsMoving { get; private set; }

    private Vector3 _visualBaseLocalPos;
    private SkeletonAnimation _currentSkeleton;
    private string _currentAnim;

    private void Awake()
    {
        Instance = this;
        if (visual != null) _visualBaseLocalPos = visual.localPosition;
        if (currentWaypoint != null)
            transform.position = currentWaypoint.transform.position;

        var skeletons = GetComponentsInChildren<SkeletonAnimation>();
        if (_skeletonFront == null && skeletons.Length > 0)
            _skeletonFront = skeletons[0];
        if (_skeletonBack == null && skeletons.Length > 1)
            _skeletonBack = skeletons[1];
    }

    private void Start()
    {
        var defaultSkel = _skeletonFront ?? _skeletonBack;
        if (defaultSkel != null)
            PlayAnimation(defaultSkel, "1.Idle");
    }

    // Returns false when movement can't start — target missing, or every route is
    // blocked by gated waypoints — so the caller can give "locked" feedback.
    public bool MoveToWaypoint(Waypoint target, Action onArrive)
    {
        if (IsMoving || target == null) return false;

        if (currentWaypoint == null)
        {
            Debug.LogWarning("[CharacterMapMover] No current waypoint assigned.");
            return false;
        }

        var path = FindPath(currentWaypoint, target);
        if (path == null || path.Count == 0)
        {
            Debug.Log($"[CharacterMapMover] No open path from '{currentWaypoint.name}' to '{target.name}' (gated or disconnected).");
            return false;
        }

        StartCoroutine(HopAlongPath(path, onArrive));
        return true;
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
                if (!next.IsPassable()) continue; // gated until its required rooms are solved
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        return null; // unreachable
    }

    private IEnumerator HopAlongPath(List<Waypoint> path, Action onArrive)
    {
        IsMoving = true;
        StartCoroutine(WalkSfxLoop());

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 from = path[i - 1].transform.position;
            Vector3 to = path[i].transform.position;

            SkeletonAnimation skel = SkeletonForDirection(from, to);
            if (skel != null)
                PlayAnimation(skel, "3.Run");

            yield return hopEnabled ? HopSegment(from, to) : SlideSegment(from, to);
            currentWaypoint = path[i];
        }

        IsMoving = false;
        PlayAnimation(_skeletonFront ?? _skeletonBack, "1.Idle");
        onArrive?.Invoke();
    }

    private void PlayAnimation(SkeletonAnimation skeleton, string anim)
    {
        if (skeleton == null) return;
        if (skeleton == _currentSkeleton && _currentAnim == anim) return;
        if (skeleton.AnimationState == null) return;

        if (_skeletonFront != null) _skeletonFront.gameObject.SetActive(skeleton == _skeletonFront);
        if (_skeletonBack != null) _skeletonBack.gameObject.SetActive(skeleton == _skeletonBack);

        skeleton.AnimationState.SetAnimation(0, anim, true);
        _currentSkeleton = skeleton;
        _currentAnim = anim;
    }

    private SkeletonAnimation SkeletonForDirection(Vector3 from, Vector3 to)
    {
        float dy = to.y - from.y;
        float dx = to.x - from.x;
        bool useBack = dy > 0.1f || dx < -0.1f;
        return useBack ? _skeletonBack : _skeletonFront;
    }

    private IEnumerator WalkSfxLoop()
    {
        while (IsMoving)
        {
            var sound = SoundManager.Instance;
            if (sound != null) sound.PlaySfx(SoundID.AUDIO_WALK);
            yield return new WaitForSeconds(Mathf.Max(0.05f, walkSfxInterval));
        }
    }

    // Cross one graph edge as several equal jumps of at most jumpLength each.
    private IEnumerator HopSegment(Vector3 from, Vector3 to)
    {
        int jumps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / Mathf.Max(0.01f, jumpLength)));

        for (int j = 0; j < jumps; j++)
        {
            Vector3 jumpFrom = Vector3.Lerp(from, to, (float)j / jumps);
            Vector3 jumpTo = Vector3.Lerp(from, to, (float)(j + 1) / jumps);

            float elapsed = 0f;
            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);
                // Linear ground motion + parabolic height 4h·t(1-t): rises to the apex
                // at mid-jump, then falls — like a projectile.
                SetPose(Vector3.Lerp(jumpFrom, jumpTo, t), 4f * jumpHeight * t * (1f - t));
                yield return null;
            }

            SetPose(jumpTo, 0f);
            if (jumpInterval > 0f)
                yield return new WaitForSeconds(jumpInterval);
        }
    }

    // Flat constant-speed glide across one graph edge — used when hopEnabled is off
    // so a walk animation (e.g. Spine) can carry the motion instead of the bounce.
    private IEnumerator SlideSegment(Vector3 from, Vector3 to)
    {
        float duration = Vector3.Distance(from, to) / Mathf.Max(0.01f, moveSpeed);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetPose(Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration)), 0f);
            yield return null;
        }

        SetPose(to, 0f);
    }

    // Root carries the ground position; the visual child carries the jump height,
    // so the camera (which follows the root) never dampens the arc.
    private void SetPose(Vector3 groundPos, float height)
    {
        if (visual != null)
        {
            transform.position = groundPos;
            visual.localPosition = _visualBaseLocalPos + new Vector3(0f, height, 0f);
        }
        else
        {
            transform.position = groundPos + new Vector3(0f, height, 0f);
        }
    }
}
