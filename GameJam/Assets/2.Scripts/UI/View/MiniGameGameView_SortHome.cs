using TMPro;
using UnityEngine;

public class MiniGameGameView_SortHome : MiniGameGameView
{
    [SerializeField] private SortHomeConfig _config;

    public override MiniGameType GameType => MiniGameType.SortHome;
    public override string ViewId => "MiniGameGameView_SortHome";

    protected override void OnShow()
    {
        base.OnShow();
        if (_config != null && _progressText != null)
            _progressText.text = $"0/{_config.items.Count}";
    }
}
