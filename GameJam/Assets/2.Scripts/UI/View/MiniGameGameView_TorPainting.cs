using TMPro;
using UnityEngine;

public class MiniGameGameView_TorPainting : MiniGameGameView
{
    [SerializeField] private TorPaintingConfig _config;

    public override MiniGameType GameType => MiniGameType.TorPainting;
    public override string ViewId => "MiniGameGameView_TorPainting";

    protected override void OnShow()
    {
        base.OnShow();
        if (_config == null) return;

        if (_progressText != null)
            _progressText.text = $"0/{_config.pieceCount}";
    }
}
