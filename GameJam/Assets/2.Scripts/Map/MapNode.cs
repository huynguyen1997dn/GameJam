using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public enum NodeInteraction
{
    OpenMiniGame, // hop to the node, then open its room/minigame
    Construct,    // hop to the node, then build it on the spot; prerequisites gate the build, not the click
}

/// <summary>
/// Clickable/reactive prop on the overview map (statue, temple, market, ...).
/// Interactable nodes need a Collider2D (PolygonCollider2D preferred) and a SpriteRenderer.
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

    [Header("Visuals")]
    public Sprite[] stateSprites; // index 0 = initial, index 1 = solved, ...

    [Header("Cosmetic Only (visual, not clickable)")]
    public bool isCosmeticOnly;
    public string[] watchedNodeIds; // used only when isCosmeticOnly == true

    [Header("Prerequisites")]
    public string[] prerequisiteNodeIds; // leave empty for none; all must be solved before this node unlocks
    public GameObject lockedIndicator;   // optional, shown while not interactable

    [Header("Construction (interaction = Construct)")]
    public string[] buildRequirementIds; // checked on arrival, independent of the click gate above
    [SerializeField] private float buildDuration = 1.5f;
    [SerializeField] private string buildBlockedMessage = "Need X woods"; // placeholder notification text

    [Header("Movement")]
    public Waypoint destinationWaypoint; // where the character stands for this node

    [Header("Hover Feedback")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private Color hoverTint = new Color(1f, 0.95f, 0.75f);

    private SpriteRenderer _spriteRenderer;
    private Vector3 _baseScale;
    private Color _baseColor;
    private bool _isHovered;
    private bool _isBuilding;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged += HandleNodeStateChanged;
        RefreshVisual();
    }

    private void OnDisable()
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged -= HandleNodeStateChanged;
        RevertHoverFeedback();

        // Coroutines die with the object — never leave the whole map locked behind.
        if (_isBuilding)
        {
            _isBuilding = false;
            MapInputLock.Unlock();
        }
    }

    private void HandleNodeStateChanged(string changedNodeId, int newState)
    {
        // Any change may affect this node (own state, watched nodes, prerequisites).
        RefreshVisual();
    }

    public void RefreshVisual()
    {
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

        if (_spriteRenderer != null && stateSprites != null && stateSprites.Length > 0)
        {
            int index = Mathf.Clamp(displayState, 0, stateSprites.Length - 1);
            _spriteRenderer.sprite = stateSprites[index];
        }

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isCosmeticOnly && !IsInteractable());
    }

    // The click gate applies to every node kind, construction included: e.g. Bridge
    // with prerequisite "Island" can't even be clicked until Island is solved.
    public bool IsInteractable()
    {
        if (isCosmeticOnly) return false;
        return AreAllSolved(prerequisiteNodeIds);
    }

    // An empty/null list means no requirement — the list itself is the flag.
    private bool AreAllSolved(string[] ids)
    {
        if (ids == null || ids.Length == 0) return true;

        var gsm = GameStateManager.Instance;
        if (gsm == null) return false;

        foreach (var id in ids)
            if (!gsm.IsSolved(id)) return false;
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (MapInputLock.IsLocked) return;
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
        if (eventData.dragging) return;     // was a camera pan, not a tap

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

        // Minigame flow — solved rooms don't open again; the node stays clickable and
        // the character still walks over, but arrival triggers nothing.
        if (!solved) ViewManager.Instance.EnterRoom(nodeId);
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

        yield return new WaitForSeconds(buildDuration);

        Debug.Log($"[MapNode] '{nodeId}' built.");
        GameStateManager.Instance.SetNodeState(nodeId, 1);

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
}
