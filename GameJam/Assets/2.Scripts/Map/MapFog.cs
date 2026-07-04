using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Fog patch on the overview map: two overlapping cloud sprites hide an area until it
/// is revealed, then the clouds part in opposite directions (left/right) while fading
/// out. Reveals when ALL nodes in revealNodeIds are solved, or when the day reaches
/// revealFromDay — whichever comes first. Scatter many patches over the map; each one
/// reveals independently so the map opens up gradually with progress and day changes.
/// Purely visual (no collider) — gate the nodes underneath with availableFromDay or
/// prerequisites, not with the fog.
/// Setup: empty parent with this component + two child SpriteRenderers (the clouds),
/// sorted above the map props. Assign them or let Awake grab the first two children.
/// Nodes solved while the map is hidden (inside a room) part the clouds on return.
/// </summary>
public class MapFog : MonoBehaviour
{
    [Header("Reveal Triggers")]
    [Tooltip("Reveals when ALL of these nodes are solved. Empty = day trigger only.")]
    [SerializeField] private string[] revealNodeIds;
    [Tooltip("Reveals when the day reaches this number. 0 = node trigger only.")]
    [SerializeField] private int revealFromDay;

    [Header("Clouds")]
    [SerializeField] private SpriteRenderer leftCloud;  // drifts left on reveal
    [SerializeField] private SpriteRenderer rightCloud; // drifts right on reveal

    [Header("Reveal Animation")]
    [SerializeField] private float revealDistance = 3f; // world units each cloud drifts
    [SerializeField] private float revealDuration = 1.5f;
    [SerializeField] private Ease revealEase = Ease.InOutSine;

    private bool _revealed;

    private void Awake()
    {
        AutoAssignClouds();
    }

    private void OnEnable()
    {
        // Map re-shown after this fog already lifted (or mid-animation when a room
        // opened): snap straight to the hidden end state.
        if (_revealed)
        {
            FinishReveal();
            return;
        }

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged += HandleNodeStateChanged;
        var dm = DayManager.Instance;
        if (dm != null) dm.OnDayChanged += HandleDayChanged;

        // Covers progress made while the map was hidden — e.g. the room just solved
        // this fog's node — so the clouds part right as the player returns to the map.
        TryReveal();
    }

    private void OnDisable()
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged -= HandleNodeStateChanged;
        var dm = DayManager.Instance;
        if (dm != null) dm.OnDayChanged -= HandleDayChanged;

        KillCloudTweens();
    }

    private void HandleNodeStateChanged(string nodeId, int state) => TryReveal();

    private void HandleDayChanged(int day) => TryReveal();

    private void TryReveal()
    {
        if (_revealed || !ShouldReveal()) return;
        StartCoroutine(RevealRoutine());
    }

    private bool ShouldReveal()
    {
        var dm = DayManager.Instance;
        if (revealFromDay > 0 && dm != null && dm.CurrentDay >= revealFromDay) return true;
        return AllNodesSolved();
    }

    // Unlike MapNode's prerequisite check, an empty list is NOT "always true" here —
    // a fog with no node ids would otherwise lift on the first frame.
    private bool AllNodesSolved()
    {
        if (revealNodeIds == null || revealNodeIds.Length == 0) return false;

        var gsm = GameStateManager.Instance;
        if (gsm == null) return false;

        foreach (var id in revealNodeIds)
            if (!gsm.IsSolved(id)) return false;
        return true;
    }

    private IEnumerator RevealRoutine()
    {
        _revealed = true;

        // Day transitions and build sequences play behind a lock (often a dark
        // overlay) — hold the reveal until the player can actually see the clouds part.
        while (MapInputLock.IsLocked)
            yield return null;

        PlayCloudTween(leftCloud, -revealDistance);
        PlayCloudTween(rightCloud, revealDistance);

        yield return new WaitForSeconds(revealDuration);

        FinishReveal();
    }

    private void PlayCloudTween(SpriteRenderer cloud, float offsetX)
    {
        if (cloud == null) return;
        cloud.transform.DOKill();
        cloud.DOKill();
        cloud.transform.DOLocalMoveX(offsetX, revealDuration).SetRelative().SetEase(revealEase);
        cloud.DOFade(0f, revealDuration).SetEase(revealEase);
    }

    private void FinishReveal()
    {
        KillCloudTweens();
        gameObject.SetActive(false);
    }

    private void KillCloudTweens()
    {
        if (leftCloud != null)
        {
            leftCloud.transform.DOKill();
            leftCloud.DOKill();
        }

        if (rightCloud != null)
        {
            rightCloud.transform.DOKill();
            rightCloud.DOKill();
        }
    }

    private void AutoAssignClouds()
    {
        if (leftCloud != null && rightCloud != null) return;

        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.transform == transform) continue;
            if (leftCloud == null) leftCloud = renderer;
            else if (rightCloud == null && renderer != leftCloud) rightCloud = renderer;
        }

        if (leftCloud == null || rightCloud == null)
            Debug.LogWarning($"[MapFog] '{name}' needs two child cloud SpriteRenderers.", this);
    }
}
