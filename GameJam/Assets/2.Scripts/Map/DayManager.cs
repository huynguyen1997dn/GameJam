using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session-only day state: current day, per-day required nodes, and whether the
/// player may advance. Pure state — DayTransitionOverlay owns the advance sequence
/// (it calls AdvanceDay while the screen is dark) and DayHUD renders it.
/// </summary>
public class DayManager : Singleton<DayManager>
{
    [Serializable]
    public class DayObjective
    {
        [Tooltip("All must be solved to unlock the Advance button. Empty = free day.")]
        public string[] requiredNodeIds;
    }

    [Tooltip("Element 0 = Day 1, element 1 = Day 2, ... List length = last playable day.")]
    [SerializeField] private List<DayObjective> dayObjectives = new List<DayObjective>();

    // Field-initialized so even a lazily auto-created instance reports Day 1.
    public int CurrentDay { get; private set; } = 1;

    public event Action<int> OnDayChanged;
    public event Action OnObjectiveStateChanged;

    public bool HasNextDay => CurrentDay < dayObjectives.Count;

    public bool IsCurrentObjectiveComplete
    {
        get
        {
            if (CurrentDay < 1 || CurrentDay > dayObjectives.Count) return false;

            var objective = dayObjectives[CurrentDay - 1];
            if (objective == null || objective.requiredNodeIds == null || objective.requiredNodeIds.Length == 0)
                return true; // free day

            var gsm = GameStateManager.Instance;
            if (gsm == null) return false;

            foreach (var id in objective.requiredNodeIds)
                if (!gsm.IsSolved(id)) return false;
            return true;
        }
    }

    public bool CanAdvance => HasNextDay && IsCurrentObjectiveComplete;

    private void OnEnable()
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged += HandleNodeStateChanged;
    }

    private void OnDisable()
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged -= HandleNodeStateChanged;
    }

    private void HandleNodeStateChanged(string nodeId, int state)
    {
        OnObjectiveStateChanged?.Invoke();
    }

    public void AdvanceDay()
    {
        if (!CanAdvance) return;
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);
        OnObjectiveStateChanged?.Invoke();
    }

    // Cheat: instantly solves every required node of the current day. Each SetNodeState
    // fires the normal events, so sprites, gates and the HUD react exactly as if the
    // player had finished the nodes — the Advance button appears through the real path.
    public void CheatCompleteCurrentObjective()
    {
        if (CurrentDay < 1 || CurrentDay > dayObjectives.Count) return;

        var objective = dayObjectives[CurrentDay - 1];
        if (objective == null || objective.requiredNodeIds == null) return;

        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        foreach (var id in objective.requiredNodeIds)
            if (!gsm.IsSolved(id)) gsm.SetNodeState(id, 1);
    }
}
