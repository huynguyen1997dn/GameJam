using System.Collections;
using UnityEngine;

/// <summary>
/// TEMPORARY — lets the map flow be tested end-to-end before the real room system
/// exists. Simulates "puzzle instantly solved" after a short delay so node sprite
/// swap and return-to-map can be verified. Delete or disable once the room dev's
/// system is integrated.
/// Must live on an always-active GameObject (NOT under OverviewMapRoot), otherwise
/// its coroutine dies when the map is hidden.
/// </summary>
public class RoomPlaceholderStub : MonoBehaviour
{
    [SerializeField] private bool autoSolveAndReturn = true;
    [SerializeField] private float autoSolveDelay = 1f;

    private void OnEnable()
    {
        var vm = ViewManager.Instance;
        if (vm != null) vm.OnRoomEnterRequested += HandleRoomEnterRequested;
    }

    private void OnDisable()
    {
        var vm = ViewManager.Instance;
        if (vm != null) vm.OnRoomEnterRequested -= HandleRoomEnterRequested;
    }

    private void HandleRoomEnterRequested(string nodeId)
    {
        Debug.Log($"[Placeholder] Entering room: {nodeId}");
        if (autoSolveAndReturn) StartCoroutine(SolveAfterDelay(nodeId));
    }

    private IEnumerator SolveAfterDelay(string nodeId)
    {
        yield return new WaitForSeconds(autoSolveDelay);
        Debug.Log($"[Placeholder] Auto-solving room: {nodeId}");
        GameStateManager.Instance.SetNodeState(nodeId, 1);
        ViewManager.Instance.ExitRoom();
    }
}
