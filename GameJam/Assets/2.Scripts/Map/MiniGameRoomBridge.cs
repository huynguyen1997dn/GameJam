using System;
using UnityEngine;

/// <summary>
/// Glue between the map side and the minigame system — replaces RoomPlaceholderStub.
/// Entering a node's room starts the mapped minigame via MiniGameManager; the
/// minigame's CompleteGame/FailGame dispatches close the room. Complete marks the
/// node solved; fail returns to the map unsolved so the player can retry.
/// Must live on an always-active GameObject (NOT under OverviewMapRoot).
/// </summary>
public class MiniGameRoomBridge : MonoBehaviour
{
    [Serializable]
    private class NodeGameMapping
    {
        public string nodeId;
        public MiniGameType game;
    }

    [Tooltip("nodeId -> minigame. Nodes named exactly like a MiniGameType value can be omitted.")]
    [SerializeField] private NodeGameMapping[] mappings;
    [SerializeField] private Transform miniGameContainer; // optional parent for spawned minigame prefabs

    private string _activeNodeId; // null = no map-initiated game running

    private void OnEnable()
    {
        var vm = ViewManager.Instance;
        if (vm != null)
        {
            vm.OnRoomEnterRequested += HandleRoomEnterRequested;
            vm.OnRoomExited += HandleRoomExited;
        }
        EventDispatcher.Subscribe(EventId.CompleteGame, HandleGameComplete);
        EventDispatcher.Subscribe(EventId.FailGame, HandleGameFailed);
    }

    private void OnDisable()
    {
        var vm = ViewManager.Instance;
        if (vm != null)
        {
            vm.OnRoomEnterRequested -= HandleRoomEnterRequested;
            vm.OnRoomExited -= HandleRoomExited;
        }
        EventDispatcher.Unsubscribe(EventId.CompleteGame, HandleGameComplete);
        EventDispatcher.Unsubscribe(EventId.FailGame, HandleGameFailed);
    }

    private void HandleRoomEnterRequested(string nodeId)
    {
        if (!TryGetGameFor(nodeId, out var game))
        {
            Debug.LogError($"[MiniGameRoomBridge] No minigame mapped for node '{nodeId}' — add it to Mappings or name the node after a MiniGameType.");
            ViewManager.Instance.ExitRoom(); // don't leave the player stuck on a hidden map
            return;
        }

        _activeNodeId = nodeId;
        // MiniGameManager.Instance.StartGame(game, miniGameContainer);
        MiniGameManager.Instance.StartGameWithView(game, miniGameContainer);
    }

    private bool TryGetGameFor(string nodeId, out MiniGameType game)
    {
        game = default;
        if (string.IsNullOrEmpty(nodeId)) return false;

        if (mappings != null)
        {
            foreach (var m in mappings)
            {
                if (m != null && m.nodeId == nodeId)
                {
                    game = m.game;
                    return true;
                }
            }
        }

        // Name-based fallback. Digits are rejected: Enum.TryParse would happily turn a
        // leftover default nodeId like "1" into the enum value 1.
        return !char.IsDigit(nodeId[0]) && Enum.TryParse(nodeId, true, out game);
    }

    private void HandleGameComplete()
    {
        if (_activeNodeId == null) return; // game started elsewhere (e.g. SRDebugger panel)
        GameStateManager.Instance.SetNodeState(_activeNodeId, 1);
        CloseRoom();
    }

    private void HandleGameFailed()
    {
        if (_activeNodeId == null) return;
        CloseRoom(); // node stays unsolved — walk back and retry
    }

    private void CloseRoom()
    {
        _activeNodeId = null;
        var mgm = MiniGameManager.Instance;
        if (mgm != null) mgm.EndCurrentGame(); // nothing else destroys the finished instance
        ViewManager.Instance.ExitRoom();
    }

    // Covers exits the bridge didn't initiate (the map's back button): abandon the
    // running minigame so it doesn't keep playing on top of the map. Counts as a fail.
    private void HandleRoomExited()
    {
        if (_activeNodeId == null) return;
        _activeNodeId = null;
        var mgm = MiniGameManager.Instance;
        if (mgm != null) mgm.EndCurrentGame();
    }
}
