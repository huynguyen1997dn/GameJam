using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public enum NodeInteraction
{
    OpenMiniGame, // hop to the node, then open its room/minigame
    Construct,    // hop to the node, then build it on the spot; prerequisites gate the build, not the click
    Interact,     // hop to the node, then play a dialogue sequence; finishing it marks the node solved
}

/// <summary>
/// Clickable/reactive prop on the overview map (statue, temple, market, ...).
/// Preferred structure: this component on a parent object with two children — a
/// "Visual" child (SpriteRenderer + Collider2D, assign visualRenderer) and an
/// interact-indicator child (diamond SpriteRenderer + Collider2D, assign
/// interactIndicator). Pointer events on child colliders bubble up the hierarchy to
/// this component, so the diamond is clickable exactly like the node itself. Legacy
/// single-object nodes (renderer + collider on this object) keep working via fallbacks.
/// Cosmetic-only nodes need only a SpriteRenderer; their visual is driven by watchedNodeIds.
/// Pointer events come from the EventSystem (works with the legacy Input Manager via
/// StandaloneInputModule): requires a Physics2DRaycaster on the camera and an
/// EventSystem in the scene. Using these instead of OnMouse* gives proper touch
/// support and lets UI block map clicks automatically.
/// </summary>
public class MapNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDragHandler
{
    [Header("Identity")]
    public string nodeId; // must match the room dev's room identifier
    public NodeInteraction interaction = NodeInteraction.OpenMiniGame;

    // Per-state alignment fix for source sprites whose center/size don't match across
    // states. Values are the visual child's absolute localPosition/localScale for that
    // state; an empty array (or missing entry) leaves the transform untouched, so nodes
    // without adjustments behave exactly as before. Edited via the "State Preview &
    // Alignment" section of the MapNode inspector (MapNodeEditor).
    [System.Serializable]
    public class StateAdjust
    {
        public Vector2 offset = Vector2.zero;
        public Vector2 scale = Vector2.one;
    }

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer visualRenderer; // main node visual; falls back to a renderer on this object, then any child
    public Sprite[] stateSprites; // index 0 = initial, index 1 = solved, ...
    [HideInInspector] public StateAdjust[] stateAdjusts; // parallel to stateSprites; drawn by MapNodeEditor
    [SerializeField] private GameObject interactIndicator; // diamond child shown while the node is unlocked and unsolved; clickable too

#if UNITY_EDITOR
    // Which state the inspector is currently previewing; OnValidate re-applies it so
    // the scene view tracks inspector edits. Never read at runtime.
    [HideInInspector] public int editorPreviewState;
#endif

    [Header("Cosmetic Only (visual, not clickable)")]
    public bool isCosmeticOnly;
    public string[] watchedNodeIds; // used only when isCosmeticOnly == true

    [Header("Prerequisites")]
    public string[] prerequisiteNodeIds; // leave empty for none; all must be solved before this node unlocks
    public GameObject lockedIndicator;   // optional, shown while not interactable
    [Tooltip("Fully hide this node (like a future-day node) until all prerequisites are solved.")]
    public bool hideUntilPrerequisitesMet;

    [Header("Construction (interaction = Construct)")]
    public string[] buildRequirementIds; // checked on arrival, independent of the click gate above
    [SerializeField] private float buildDuration = 1.5f;
    [SerializeField] private string buildBlockedMessage = "Need X woods"; // placeholder notification text

    [Header("Dialogue (interaction = Interact)")]
    [Tooltip("DialogPopup phase played on arrival; finishing the sequence marks this node solved.")]
    [SerializeField] private PhaseId dialoguePhaseId = PhaseId.None;

    [Header("Info Popup (shown on solved click)")]
    public Sprite infoIcon;
    public string infoTitle;
    public List<StatInfo> statInfos = new();

    [Header("Day Availability")]
    public int availableFromDay = 1; // fully hidden until DayManager reaches this day

    [Header("Movement")]
    public Waypoint destinationWaypoint; // where the character stands for this node

    [Header("Hover Feedback")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private Color hoverTint = new Color(1f, 0.95f, 0.75f);

    private SpriteRenderer _spriteRenderer;
    private Collider2D[] _colliders;
    private Vector3 _baseScale;
    private Color _baseColor;
    private bool _isHovered;
    private bool _isBuilding;
    private bool _isInteracting;

    private void Awake()
    {
        _spriteRenderer = ResolveVisualRenderer();
        _colliders = GetComponentsInChildren<Collider2D>(true); // on self and/or children; empty on cosmetic nodes
        _baseScale = transform.localScale;
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
    }

    // Serialized reference first, then a renderer on this object (legacy single-object
    // nodes), then the first child renderer that isn't part of the interact indicator.
    private SpriteRenderer ResolveVisualRenderer()
    {
        if (visualRenderer != null) return visualRenderer;

        var own = GetComponent<SpriteRenderer>();
        if (own != null) return own;

        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            if (interactIndicator == null || !renderer.transform.IsChildOf(interactIndicator.transform))
                return renderer;
        return null;
    }

    // AABB of the collider on the visual child. Tighter than renderer.bounds, which
    // spans the sprite's full texture rect including transparent pixels; used by the
    // tutorial highlight so the frame hugs the clickable shape. Falls back to any
    // non-indicator collider on this node.
    public bool TryGetVisualColliderBounds(out Bounds bounds)
    {
        var visual = _spriteRenderer != null ? _spriteRenderer : ResolveVisualRenderer();
        Collider2D collider = visual != null ? visual.GetComponent<Collider2D>() : null;

        if (collider == null)
        {
            var colliders = _colliders ?? GetComponentsInChildren<Collider2D>(true);
            foreach (var candidate in colliders)
            {
                if (candidate == null) continue;
                if (interactIndicator != null && candidate.transform.IsChildOf(interactIndicator.transform)) continue;
                collider = candidate;
                break;
            }
        }

        // Disabled colliders report empty bounds, so let callers fall back instead.
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
        {
            bounds = default;
            return false;
        }

        bounds = collider.bounds;
        return true;
    }

    private void OnEnable()
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged += HandleNodeStateChanged;
        var dm = DayManager.Instance;
        if (dm != null) dm.OnDayChanged += HandleDayChanged;
        RefreshVisual();
    }

    private void OnDisable()
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged -= HandleNodeStateChanged;
        var dm = DayManager.Instance;
        if (dm != null) dm.OnDayChanged -= HandleDayChanged;
        RevertHoverFeedback();

        // Coroutines die with the object — never leave the whole map locked behind.
        if (_isBuilding)
        {
            _isBuilding = false;
            MapInputLock.Unlock();
        }

        // Same failsafe for a dialogue in flight: drop it without completing, so the
        // node stays unsolved and the player can trigger it again. The DialogPopup's
        // OnClosed callback no-ops once _isInteracting is cleared.
        if (_isInteracting)
        {
            _isInteracting = false;
            MapInputLock.Unlock();
        }
    }

    private void HandleNodeStateChanged(string changedNodeId, int newState)
    {
        // Any change may affect this node (own state, watched nodes, prerequisites).
        RefreshVisual();
    }

    private void HandleDayChanged(int newDay)
    {
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        // Future-day nodes are fully hidden (not just locked) until their day arrives;
        // hideUntilPrerequisitesMet extends the same full-hide to prerequisite gating.
        bool visible = IsAvailableToday() && (!hideUntilPrerequisitesMet || AreAllSolved(prerequisiteNodeIds));
        if (_spriteRenderer != null) _spriteRenderer.enabled = visible;
        SetCollidersEnabled(visible);
        if (!visible)
        {
            if (lockedIndicator != null) lockedIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);
            return;
        }

        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        int displayState;
        if (isCosmeticOnly)
        {
            int solvedCount = 0;
            if (watchedNodeIds != null)
            {
                foreach (var id in watchedNodeIds)
                    if (gsm.IsSolved(id)) solvedCount++;
            }
            displayState = solvedCount;
        }
        else
        {
            displayState = gsm.GetNodeState(nodeId);
        }

        ApplyStateVisual(_spriteRenderer, displayState);

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isCosmeticOnly && !IsInteractable());

        // The diamond marks "something to do here right now": unlocked and unsolved.
        // Locked nodes show only their sprite; clicking them plays the deny feedback.
        if (interactIndicator != null)
            interactIndicator.SetActive(!isCosmeticOnly && IsInteractable() && !gsm.IsSolved(nodeId));
    }

    // Sets the sprite for a state and applies that state's alignment adjust (absolute
    // localPosition/localScale of the visual child). Skipped when the renderer lives on
    // this object itself: that transform is the node's world placement, not a visual
    // child we can own.
    private void ApplyStateVisual(SpriteRenderer sr, int displayState)
    {
        if (sr == null || stateSprites == null || stateSprites.Length == 0) return;

        int index = Mathf.Clamp(displayState, 0, stateSprites.Length - 1);
        if (stateSprites[index] != null) sr.sprite = stateSprites[index];

        var visual = sr.transform;
        if (visual == transform) return;
        if (stateAdjusts == null || index >= stateAdjusts.Length || stateAdjusts[index] == null) return;

        var adjust = stateAdjusts[index];
        visual.localPosition = new Vector3(adjust.offset.x, adjust.offset.y, visual.localPosition.z);
        visual.localScale = new Vector3(adjust.scale.x, adjust.scale.y, visual.localScale.z);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null) return;
        foreach (var col in _colliders)
            if (col != null) col.enabled = enabled;
    }

    // The click gate applies to every node kind, construction included: e.g. Bridge
    // with prerequisite "Island" can't even be clicked until Island is solved.
    public bool IsInteractable()
    {
        if (isCosmeticOnly) return false;
        if (!IsAvailableToday()) return false; // belt-and-braces: the collider is off anyway
        return AreAllSolved(prerequisiteNodeIds);
    }

    private bool IsAvailableToday()
    {
        var dm = DayManager.Instance;
        return dm == null || dm.CurrentDay >= availableFromDay;
    }

    // An empty/null list means no requirement — the list itself is the flag.
    private bool AreAllSolved(string[] ids)
    {
        if (ids == null || ids.Length == 0) return true;

        var gsm = GameStateManager.Instance;
        if (gsm == null) return false;

        foreach (var id in ids)
        {
            if (!gsm.IsSolved(id)) return false;

        }
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging) return; // mid-pan: don't flash hover feedback
        if (MapInputLock.IsLocked) return;
        if (!TutorialInputGate.Allows(nodeId)) return;
        if (!IsInteractable()) return;
        _isHovered = true;
        transform.localScale = _baseScale * hoverScale;
        if (_spriteRenderer != null) _spriteRenderer.color = hoverTint;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RevertHoverFeedback();
    }

    // Empty on purpose: registers this node as the EventSystem's drag target, so a
    // camera pan that starts here sets eventData.dragging and is not treated as a tap.
    public void OnDrag(PointerEventData eventData) { }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isCosmeticOnly) return;
        if (MapInputLock.IsLocked) return;  // e.g. a build animation is playing
        if (!TutorialInputGate.Allows(nodeId)) return;
        if (eventData.dragging) return;     // was a camera pan, not a tap

        EventDispatcher.Dispatch(EventId.NodeClicked, nodeId);

        if (!IsInteractable())
        {
            PlayLockedFeedback();
            return;
        }

        var mover = CharacterMapMover.Instance;
        if (mover == null)
        {
            HandleArrival(); // no character on the map — resolve the interaction directly
            return;
        }

        if (mover.IsMoving) return;
        if (!mover.MoveToWaypoint(destinationWaypoint, HandleArrival))
            PlayLockedFeedback(); // no open route — e.g. a gated waypoint blocks the way
    }

    // Runs once the character stands at the node. State is re-read here, not at click
    // time, so it can't go stale during the hop.
    private void HandleArrival()
    {
        var gsm = GameStateManager.Instance;
        bool solved = gsm != null && gsm.IsSolved(nodeId);

        if (solved && infoIcon != null)
        {
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            RevertHoverFeedback();
            UIManager.Instance.OnShowPopup(PopupId.NodeInfoPopup, infoIcon, infoTitle, day, statInfos);
            return;
        }

        if (interaction == NodeInteraction.Construct)
        {
            if (solved) return; // already built — nothing left to do here

            if (AreAllSolved(buildRequirementIds))
            {
                StartCoroutine(BuildSequence());
            }
            else
            {
                // TODO(map): replace with a real notification UI popup.
                string needs = buildRequirementIds == null ? "" : string.Join(", ", buildRequirementIds);
                Debug.Log($"[MapNode] '{nodeId}': {buildBlockedMessage} (requires solved: {needs})");
            }
            return;
        }

        if (interaction == NodeInteraction.Interact)
        {
            if (!solved)
            {
                StartInteractDialogue();
            }
            else if (infoIcon != null || statInfos.Count > 0)
            {
                int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                RevertHoverFeedback();
                UIManager.Instance.OnShowPopup(PopupId.NodeInfoPopup, infoIcon, infoTitle, day, statInfos);
            }
            return;
        }

        // Minigame flow — solved rooms don't open again; the node stays clickable and
        // the character still walks over, but arrival triggers nothing.
        if (!solved) ViewManager.Instance.EnterRoom(nodeId);
    }

    // The map freezes for the dialogue; finishing it marks the node solved — the same
    // unlock currency as a cleared room, so prerequisite gates react identically.
    private void StartInteractDialogue()
    {
        if (dialoguePhaseId == PhaseId.None)
        {
            Debug.LogWarning($"[MapNode] '{nodeId}': no dialogue phase assigned — solving immediately.");
            GameStateManager.Instance.SetNodeState(nodeId, 1);
            return;
        }

        _isInteracting = true;
        MapInputLock.Lock();
        RevertHoverFeedback();
        StartCoroutine(InteractDialogueRoutine());
    }

    private IEnumerator InteractDialogueRoutine()
    {
        UIManager.Instance.OnShowPopup(PopupId.DialogPopup, dialoguePhaseId);
        yield return null;

        var popup = UIManager.Instance.GetCurrentPopup() as DialogPopup;
        if (popup != null)
        {
            popup.OnClosed = CompleteInteractDialogue;
        }
        else
        {
            Debug.LogError($"[MapNode] '{nodeId}': DialogPopup not found");
            CompleteInteractDialogue();
        }
    }

    private void CompleteInteractDialogue()
    {
        if (!_isInteracting) return; // node was disabled mid-dialogue — stays unsolved
        _isInteracting = false;
        // Unlock before SetNodeState: reveal effects (e.g. MapFog) wait on the lock.
        MapInputLock.Unlock();
        GameStateManager.Instance.SetNodeState(nodeId, 1);
    }

    // Everything on the map is frozen while the build plays out, then marking the node
    // solved swaps its sprite and opens every prerequisite/waypoint gate keyed to it.
    private IEnumerator BuildSequence()
    {
        _isBuilding = true;
        MapInputLock.Lock();
        RevertHoverFeedback();

        // TODO(map): replace with the real Spine build animation + SFX when assets land.
        Debug.Log($"[MapNode] Building '{nodeId}'...");
        transform.DOKill(true);
        transform.DOShakeScale(buildDuration, 0.15f, 8);
        SoundManager.Instance.PlaySfx("AUDIO_BUILD");

        yield return new WaitForSeconds(buildDuration);

        Debug.Log($"[MapNode] '{nodeId}' built.");
        GameStateManager.Instance.SetNodeState(nodeId, 1);
        interaction = NodeInteraction.Interact;

        _isBuilding = false;
        MapInputLock.Unlock();
    }

    private void RevertHoverFeedback()
    {
        if (!_isHovered) return;
        _isHovered = false;
        transform.localScale = _baseScale;
        if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
    }

    private void PlayLockedFeedback()
    {
        transform.DOKill(true);
        transform.DOShakePosition(0.25f, 0.1f, 20);
        var sound = SoundManager.Instance;
        if (sound != null) sound.PlaySfx(SoundID.AUDIO_WRONG);
    }

#if UNITY_EDITOR
    // Editor access for MapNodeEditor: the resolved visual renderer and a direct
    // "show this state now" entry point (sprite + alignment adjust).
    public SpriteRenderer EditorResolveVisualRenderer() => ResolveVisualRenderer();
    public void EditorApplyState(int state) => ApplyStateVisual(ResolveVisualRenderer(), state);

    // Keeps the editor preview in sync whenever the component is edited: re-applies the
    // currently previewed state (sprite + adjust), so tweaking offsets/scales in the
    // inspector is visible in the scene view immediately.
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (stateSprites == null || stateSprites.Length == 0) return;

        editorPreviewState = Mathf.Clamp(editorPreviewState, 0, stateSprites.Length - 1);
        EditorApplyState(editorPreviewState);
    }
#endif
}
