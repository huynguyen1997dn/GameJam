using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Clickable/reactive prop on the overview map (statue, temple, market, ...).
/// Interactable nodes need a Collider2D (PolygonCollider2D preferred) and a SpriteRenderer.
/// Cosmetic-only nodes need only a SpriteRenderer; their visual is driven by watchedNodeIds.
/// Pointer events come from the EventSystem (works with the legacy Input Manager via
/// StandaloneInputModule): requires a Physics2DRaycaster on the camera and an
/// EventSystem in the scene. Using these instead of OnMouse* gives proper touch
/// support and lets UI block map clicks automatically.
/// </summary>
public class MapNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Identity")]
    public string nodeId; // must match the room dev's room identifier

    [Header("Visuals")]
    public Sprite[] stateSprites; // index 0 = initial, index 1 = solved, ...

    [Header("Cosmetic Only (visual, not clickable)")]
    public bool isCosmeticOnly;
    public string[] watchedNodeIds; // used only when isCosmeticOnly == true

    [Header("Prerequisites")]
    public bool requiresPrerequisite;
    public string[] prerequisiteNodeIds; // all must be solved before this node unlocks
    public GameObject lockedIndicator;   // optional, shown while not interactable

    [Header("Movement")]
    public Waypoint destinationWaypoint; // where the character stands for this node

    [Header("Hover Feedback")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private Color hoverTint = new Color(1f, 0.95f, 0.75f);

    private SpriteRenderer _spriteRenderer;
    private Vector3 _baseScale;
    private Color _baseColor;
    private bool _isHovered;

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

    public bool IsInteractable()
    {
        if (isCosmeticOnly) return false;
        if (!requiresPrerequisite) return true;

        var gsm = GameStateManager.Instance;
        if (gsm == null) return false;
        if (prerequisiteNodeIds == null) return true;

        foreach (var id in prerequisiteNodeIds)
            if (!gsm.IsSolved(id)) return false;
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        _isHovered = true;
        transform.localScale = _baseScale * hoverScale;
        if (_spriteRenderer != null) _spriteRenderer.color = hoverTint;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RevertHoverFeedback();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isCosmeticOnly) return;

        if (!IsInteractable())
        {
            PlayLockedFeedback();
            return;
        }

        var mover = CharacterMapMover.Instance;
        if (mover == null)
        {
            // No character on the map — open the room directly.
            ViewManager.Instance.EnterRoom(nodeId);
            return;
        }

        if (mover.IsMoving) return;
        mover.MoveToWaypoint(destinationWaypoint, () => ViewManager.Instance.EnterRoom(nodeId));
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
