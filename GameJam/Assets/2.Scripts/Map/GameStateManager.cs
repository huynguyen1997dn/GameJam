using System;
using System.Collections.Generic;

/// <summary>
/// Shared source of truth for node states. The map side reads states to drive
/// visuals/interactability; the room side calls SetNodeState when a puzzle is solved.
/// state >= 1 is treated as "solved".
/// </summary>
public class GameStateManager : Singleton<GameStateManager>
{
    private readonly Dictionary<string, int> _nodeStates = new Dictionary<string, int>();

    public event Action<string, int> OnNodeStateChanged; // (nodeId, newState)

    public int GetNodeState(string nodeId)
    {
        return _nodeStates.TryGetValue(nodeId, out var state) ? state : 0;
    }

    public void SetNodeState(string nodeId, int state)
    {
        _nodeStates[nodeId] = state;
        OnNodeStateChanged?.Invoke(nodeId, state);
    }

    public bool IsSolved(string nodeId) => GetNodeState(nodeId) >= 1;
}
