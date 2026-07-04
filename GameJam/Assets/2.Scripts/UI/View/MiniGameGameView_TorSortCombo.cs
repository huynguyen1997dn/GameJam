using TMPro;
using UnityEngine;

public class MiniGameGameView_TorSortCombo : MiniGameGameView
{
    [SerializeField] private TorSortComboConfig _config;
    [SerializeField] private TextMeshProUGUI _phaseText;

    public override MiniGameType GameType => MiniGameType.TorSortCombo;
    public override string ViewId => "MiniGameGameView_TorSortCombo";

    // Progress events come from the sub-games, tagged with their own type.
    protected override bool IsProgressForThisView(MiniGameType type)
    {
        return type == MiniGameType.TorPainting || type == MiniGameType.SortHome;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventDispatcher.Subscribe<int>(EventId.MiniGamePhaseChanged, OnPhaseChanged);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventDispatcher.Unsubscribe<int>(EventId.MiniGamePhaseChanged, OnPhaseChanged);
    }

    protected override void OnShow()
    {
        base.OnShow();

        // The view may be shown after the manager already started phase 1,
        // so sync from the running game instead of relying on the event.
        var combo = MiniGameManager.Instance != null
            ? MiniGameManager.Instance.CurrentMiniGame as TorSortComboManager
            : null;
        int phase = combo != null && combo.CurrentPhase > 0
            ? combo.CurrentPhase
            : TorSortComboManager.PhaseTorPainting;
        UpdatePhase(phase);
    }

    private void OnPhaseChanged(int phase)
    {
        UpdatePhase(phase);
    }

    private void UpdatePhase(int phase)
    {
        if (_phaseText != null)
            _phaseText.text = $"Phase {phase}/{TorSortComboManager.PhaseCount}";

        // Reset progress UI - the next MiniGameProgressUpdate fills in the real target.
        if (_progressSlider != null)
            _progressSlider.value = 0;
        if (_progressText != null)
            _progressText.text = string.Empty;
    }
}
