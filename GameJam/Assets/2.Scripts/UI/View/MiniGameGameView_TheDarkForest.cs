using TMPro;
using UnityEngine;

public class MiniGameGameView_TheDarkForest : MiniGameGameView
{
    [SerializeField] private TheDarkForestConfig _config;
    [SerializeField] private TextMeshProUGUI _goalText;

    public override MiniGameType GameType => MiniGameType.TheDarkForest;
    public override string ViewId => "MiniGameGameView_TheDarkForest";

    protected override void OnShow()
    {
        base.OnShow();
        if (_config != null && _goalText != null)
            _goalText.text = _config.maxFails.ToString();
        if (_progressSlider != null) _progressSlider.gameObject.SetActive(false);
    }

    protected override void OnPreGameComplete()
    {
        base.OnPreGameComplete();
        if (_goalText != null)
            _goalText.text = "Done!";
    }
}
