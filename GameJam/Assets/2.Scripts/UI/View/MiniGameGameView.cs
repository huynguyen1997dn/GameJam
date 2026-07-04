using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class MiniGameGameView : ViewBase
{
    [SerializeField] protected Slider _progressSlider;
    [SerializeField] protected Button _completeButton;
    [SerializeField] protected TextMeshProUGUI _progressText;

    public abstract MiniGameType GameType { get; }

    protected virtual void OnEnable()
    {
        EventDispatcher.Subscribe<MiniGameProgressData>(EventId.MiniGameProgressUpdate, OnProgressUpdate);
        EventDispatcher.Subscribe(EventId.CompleteGame, OnGameComplete);
    }

    protected virtual void OnDisable()
    {
        EventDispatcher.Unsubscribe<MiniGameProgressData>(EventId.MiniGameProgressUpdate, OnProgressUpdate);
        EventDispatcher.Unsubscribe(EventId.CompleteGame, OnGameComplete);
    }

    protected virtual void OnProgressUpdate(MiniGameProgressData data)
    {
        if (data.gameType != GameType) return;

        if (_progressSlider != null)
        {
            _progressSlider.maxValue = data.target;
            _progressSlider.value = data.current;
        }

        if (_progressText != null)
            _progressText.text = $"{data.current}/{data.target}";
    }

    protected virtual void OnGameComplete()
    {
        if (MiniGameManager.Instance.CurrentMiniGame?.MiniGameType != GameType) return;

        if (_completeButton != null)
            _completeButton.gameObject.SetActive(true);
    }

    protected virtual void Awake()
    {
        base.Awake();
        if (_completeButton != null)
            _completeButton.onClick.AddListener(OnCompleteClicked);
    }

    protected virtual void OnCompleteClicked()
    {
        UIManager.Instance.OnHideView(ViewId);
    }
}
