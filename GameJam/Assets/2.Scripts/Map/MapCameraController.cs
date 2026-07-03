using UnityEngine;

/// <summary>
/// Camera rig for the big overview map: smoothly follows the character to keep it
/// centered, zooms via mouse scroll or two-finger pinch (legacy Input Manager), and
/// clamps the view so it never shows outside the map bounds. Freezes while the map
/// is hidden (room view). Camera must be orthographic.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform followTarget;   // the map character
    [SerializeField] private float followSmoothTime = 0.15f;

    [Header("Bounds")]
    [SerializeField] private SpriteRenderer mapBounds; // background sprite spanning the whole map

    [Header("Zoom (orthographic size)")]
    [SerializeField] private float minZoom = 2f;       // fully zoomed in
    [SerializeField] private float maxZoom = 12f;      // further limited so the view never leaves the map
    [SerializeField] private float scrollZoomStep = 1f;    // ortho units per mouse-wheel notch
    [SerializeField] private float pinchZoomSpeed = 0.02f; // ortho units per pixel of pinch distance change

    private Camera _cam;
    private Vector3 _followVelocity;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic)
            Debug.LogWarning("[MapCameraController] Camera must be orthographic — zoom drives orthographicSize.");
    }

    private void Start()
    {
        // Snap onto the character at load so the camera doesn't pan across the map at start.
        if (followTarget != null)
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(followTarget.position.x, followTarget.position.y, pos.z);
        }
        ClampToBounds();
    }

    private void LateUpdate()
    {
        // While in room view the map (and character) is inactive — freeze the map camera
        // so the room dev's view is unaffected by scroll/pinch.
        if (followTarget == null || !followTarget.gameObject.activeInHierarchy) return;

        HandleZoomInput();
        FollowTarget();
        ClampToBounds();
    }

    private void HandleZoomInput()
    {
        float delta = 0f;

        // Clamp per-frame scroll to one notch so mouse wheels (±1 per notch) and
        // trackpads (streams of small fractional values) both feel reasonable.
        float scroll = Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f);
        delta -= scroll * scrollZoomStep;

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
            float dist = (t0.position - t1.position).magnitude;
            delta += (prevDist - dist) * pinchZoomSpeed;
        }

        if (Mathf.Approximately(delta, 0f)) return;
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + delta, minZoom, MaxAllowedZoom());
    }

    // Largest orthographic size at which the view still fits inside the map on both axes.
    private float MaxAllowedZoom()
    {
        if (mapBounds == null) return maxZoom;
        var b = mapBounds.bounds;
        return Mathf.Min(maxZoom, b.extents.y, b.extents.x / _cam.aspect);
    }

    private void FollowTarget()
    {
        Vector3 pos = transform.position;
        Vector3 target = new Vector3(followTarget.position.x, followTarget.position.y, pos.z);
        transform.position = Vector3.SmoothDamp(pos, target, ref _followVelocity, followSmoothTime);
    }

    private void ClampToBounds()
    {
        if (mapBounds == null) return;

        var b = mapBounds.bounds;
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        Vector3 pos = transform.position;
        // If the view is wider/taller than the map on an axis, lock to the map's center there.
        pos.x = b.size.x <= halfW * 2f ? b.center.x : Mathf.Clamp(pos.x, b.min.x + halfW, b.max.x - halfW);
        pos.y = b.size.y <= halfH * 2f ? b.center.y : Mathf.Clamp(pos.y, b.min.y + halfH, b.max.y - halfH);
        transform.position = pos;
    }
}
