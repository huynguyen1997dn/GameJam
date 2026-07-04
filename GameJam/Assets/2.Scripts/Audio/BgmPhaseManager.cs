using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the looping BGM through the game's phases (3 by design). A phase becomes
/// active when the day reaches its start day OR when one of its trigger nodes is
/// solved, whichever happens first. Phases only move forward, never back.
/// Playback goes through SoundManager so the BGM mute setting keeps working.
/// </summary>
public class BgmPhaseManager : Singleton<BgmPhaseManager>
{
    [Serializable]
    public class Phase
    {
        public AudioClip clip;

        [Tooltip("This phase starts when the day reaches this value (0 = no day trigger).")]
        public int startDay;

        [Tooltip("This phase starts when any of these nodes is solved (empty = no node trigger).")]
        public string[] triggerNodeIds;
    }

    [Tooltip("Element 0 = phase 1 (plays from the start), then phase 2, phase 3. Its triggers are ignored.")]
    [SerializeField] private List<Phase> phases = new List<Phase>();
    [SerializeField] private float crossfadeDuration = 2f;

    public int CurrentPhaseIndex { get; private set; } = -1;

    private void Start()
    {
        var dayManager = DayManager.Instance;
        if (dayManager != null) dayManager.OnDayChanged += HandleDayChanged;

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged += HandleNodeStateChanged;

        SetPhase(HighestPhaseForDay(dayManager != null ? dayManager.CurrentDay : 1));
    }

    protected override void OnDestroy()
    {
        var dayManager = DayManager.Instance;
        if (dayManager != null) dayManager.OnDayChanged -= HandleDayChanged;

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged -= HandleNodeStateChanged;

        base.OnDestroy();
    }

    private void HandleDayChanged(int day)
    {
        SetPhase(HighestPhaseForDay(day));
    }

    private void HandleNodeStateChanged(string nodeId, int state)
    {
        if (state < 1) return; // only "solved" advances a phase

        // Highest matching phase wins, so a late-game node can skip a phase.
        for (int i = phases.Count - 1; i > CurrentPhaseIndex; i--)
        {
            var ids = phases[i].triggerNodeIds;
            if (ids != null && Array.IndexOf(ids, nodeId) >= 0)
            {
                SetPhase(i);
                return;
            }
        }
    }

    private int HighestPhaseForDay(int day)
    {
        int index = 0; // phase 1 is always the baseline
        for (int i = 1; i < phases.Count; i++)
        {
            if (phases[i].startDay > 0 && day >= phases[i].startDay)
                index = i;
        }
        return index;
    }

    private void SetPhase(int index)
    {
        if (index <= CurrentPhaseIndex || index >= phases.Count) return;

        CurrentPhaseIndex = index;

        var clip = phases[index].clip;
        var soundManager = SoundManager.Instance;
        if (soundManager != null && clip != null)
            soundManager.CrossfadeBgm(clip, crossfadeDuration);
    }
}
