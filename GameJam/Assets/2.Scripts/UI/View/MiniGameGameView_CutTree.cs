using TMPro;
using UnityEngine;

public class MiniGameGameView_CutTree : MiniGameGameView
{
    [SerializeField] private CutTreeConfig _config;

    public override MiniGameType GameType => MiniGameType.CutTree;
    public override string ViewId => "MiniGameGameView_CutTree";

    protected override void OnShow()
    {
        base.OnShow();
        if (_config != null && _progressText != null)
            _progressText.text = $"0/{_config.targetTreesToCut}";
    }
}
