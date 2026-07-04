using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Camera rig for the big overview map: smoothly follows the character to keep it
/// centered, zooms via mouse scroll or two-finger pinch (legacy Input Manager), and
/// clamps the view so it never shows outside the map bounds. Dragging (mouse or one
/// finger) pans to nearby zones; follow resumes when the character next moves.
/// A tutorial can override everything with SetFocusTarget to glide onto a node.
/// Freezes while the map is hidden (room view). Camera must be orthographic.
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

    [Header("Drag Pan")]
    [SerializeField] private bool dragPanEnabled = true;

    [Header("Day Config")]
    [SerializeField] private MapDayCameraConfigSO dayCameraConfig;

    [Header("Tutorial Focus")]
    [SerializeField] private float focusSmoothTime = 0.4f;

    private Camera _cam;
    private DayCameraConfig _currentConfig;
    private Transform _focusTarget;   // while set, the camera centers here instead of the character
    private Vector3 _followVelocity;
    private bool _pointerDown;        // pointer held, possibly still a tap
    private bool _isDragging;         // passed the drag threshold, actively panning
    private bool _isFreeLook;         // player panned away; follow is paused
    private Vector2 _pointerDownScreenPos;
    private Vector3 _dragOriginWorld; // world point pinned under the pointer while panning


    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic)
            Debug.LogWarning("[MapCameraController] Camera must be orthographic — zoom drives orthographicSize.");
    }

    private void Start()
    {
        if (dayCameraConfig != null)
        {
            var dm = DayManager.Instance;
            if (dm != null)
            {
                dm.OnDayChanged += HandleDayChanged;
                _currentConfig = dayCameraConfig.GetConfigForDay(dm.CurrentDay);
                ApplyDayConfig(_currentConfig);
            }
        }

        // Snap onto the character at load so the camera doesn't pan across the map at start.
        if (_currentConfig != null && _currentConfig.followCharacter && followTarget != null)
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(followTarget.position.x, followTarget.position.y, pos.z);
        }
        ClampToBounds();
    }

    private void OnDestroy()
    {
        var dm = DayManager.Instance;
        if (dm != null)
            dm.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int newDay)
    {
        if (dayCameraConfig == null) return;
        _currentConfig = dayCameraConfig.GetConfigForDay(newDay);
        ApplyDayConfig(_currentConfig);
    }

    private void ApplyDayConfig(DayCameraConfig config)
    {
        if (config == null) return;
        _cam.orthographicSize = config.cameraSize;
    }

    // Tutorial override: the camera bypasses the follow rule and glides onto this
    // target (a highlighted node) until ClearFocusTarget. Pan/zoom stay disabled by
    // the caller's TutorialInputGate, so the player just watches the move.
    public void SetFocusTarget(Transform target)
    {
        _focusTarget = target;
    }

    public void ClearFocusTarget()
    {
        _focusTarget = null;
    }

    private void LateUpdate()
    {
        // Focus beats follow. The activeInHierarchy check drops the override while the
        // map is hidden (room view deactivates the nodes along with everything else).
        if (_focusTarget != null && _focusTarget.gameObject.activeInHierarchy)
        {
            _pointerDown = false;
            _isDragging = false;
            _isFreeLook = false;
            GlideTo(_focusTarget.position, focusSmoothTime);
            ClampToBounds();
            return;
        }

        // While in room view the map (and character) is inactive — freeze the map camera
        // so the room dev's view is unaffected by scroll/pinch. Dropping free look here
        // means exiting a room always comes back centered on the character.
        if (followTarget == null || !followTarget.gameObject.activeInHierarchy)
        {
            _pointerDown = false;
            _isDragging = false;
            _isFreeLook = false;
            return;
        }

        if (MapInputLock.IsLocked || TutorialInputGate.IsActive)
        {
            // Scripted/tutorial moment: drop any drag, ignore zoom/pan, and glide
            // back to the character so the player watches the guided target.
            _pointerDown = false;
            _isDragging = false;
            _isFreeLook = false;
        }
        else
        {
            if (_currentConfig == null || _currentConfig.allowZoom)
                HandleZoomInput();
            else
                _cam.orthographicSize = _currentConfig.cameraSize;

            HandlePanInput();
        }

        // The character starting to move pulls the camera back into follow mode,
        // unless the player is still holding a drag.
        var mover = CharacterMapMover.Instance;
        if (_isFreeLook && !_pointerDown && mover != null && mover.IsMoving)
            _isFreeLook = false;

        if (!_isFreeLook && (_currentConfig == null || _currentConfig.followCharacter))
            FollowTarget();

        ApplyDragBounds();
        ClampToBounds();
    }

    private void HandlePanInput()
    {
        // Two fingers belong to pinch zoom — abandon any pan in progress.
        bool dragAllowed = _currentConfig != null ? _currentConfig.dragEnabled : dragPanEnabled;
        
        if (!dragAllowed || Input.touchCount >= 2)
        {
            _pointerDown = false;
            _isDragging = false;
            return;
        }

        bool held;
        Vector2 pos;
        bool overUI;
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            held = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
            pos = t.position;
            overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
        }
        else
        {
            held = Input.GetMouseButton(0);
            pos = Input.mousePosition;
            overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        if (!held)
        {
            _pointerDown = false;
            _isDragging = false;
            return;
        }

        if (!_pointerDown)
        {
            if (overUI) return; // don't start pans on UI
            _pointerDown = true;
            _pointerDownScreenPos = pos;
            return;
        }

        if (!_isDragging)
        {
            // Same threshold the EventSystem uses to tell clicks from drags, so a tap
            // that opens a room never nudges the camera.
            float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
            if ((pos - _pointerDownScreenPos).sqrMagnitude < threshold * threshold) return;
            _isDragging = true;
            _isFreeLook = true;
            _dragOriginWorld = _cam.ScreenToWorldPoint(pos); // anchor here to avoid a catch-up jump
        }

        // Grab-the-world pan: move the camera so the anchored world point stays under the pointer.
        Vector3 diff = _dragOriginWorld - _cam.ScreenToWorldPoint(pos);
        diff.z = 0f;
        transform.position += diff;
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
        GlideTo(followTarget.position, followSmoothTime);
    }

    // Shares _followVelocity between follow and focus so switching modes mid-glide
    // keeps the current momentum instead of jerking.
    private void GlideTo(Vector3 worldPos, float smoothTime)
    {
        Vector3 pos = transform.position;
        Vector3 target = new Vector3(worldPos.x, worldPos.y, pos.z);
        transform.position = Vector3.SmoothDamp(pos, target, ref _followVelocity, smoothTime);
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

    private void ApplyDragBounds()
    {
        if (_currentConfig == null || !_currentConfig.dragEnabled) return;

        if (_currentConfig.dragBoundsMin == Vector2.zero &&
            _currentConfig.dragBoundsMax == Vector2.zero)
            return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, _currentConfig.dragBoundsMin.x, _currentConfig.dragBoundsMax.x);
        pos.y = Mathf.Clamp(pos.y, _currentConfig.dragBoundsMin.y, _currentConfig.dragBoundsMax.y);
        transform.position = pos;
    }
}
