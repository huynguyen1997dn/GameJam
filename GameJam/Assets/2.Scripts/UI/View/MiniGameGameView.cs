using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class MiniGameGameView : ViewBase
{
    [SerializeField] protected Slider _progressSlider;
    [SerializeField] protected Button _completeButton;
    [SerializeField] protected TextMeshProUGUI _progressText;

    public abstract MiniGameType GameType { get; }
    
    protected virtual void Awake()
    {
        base.Awake();
        _completeButton.gameObject.SetActive(false);
        _completeButton.onClick.AddListener(OnCompleteClicked);
        _progressSlider.value = 0;

    }

    protected virtual void OnCompleteClicked()
    {
        Hide();
        EventDispatcher.Dispatch(EventId.CompleteGame);
        UIManager.Instance.OnShowView(ViewID.GamePlayView);
    }

    protected virtual void OnEnable()
    {
        EventDispatcher.Subscribe<MiniGameProgressData>(EventId.MiniGameProgressUpdate, OnProgressUpdate);
        EventDispatcher.Subscribe(EventId.PreCompleteGame, OnPreGameComplete);

    }

    protected  virtual void  OnPreGameComplete()
    {
        _completeButton.gameObject.SetActive(true);
    }

    protected virtual void OnDisable()
    {
        EventDispatcher.Unsubscribe<MiniGameProgressData>(EventId.MiniGameProgressUpdate, OnProgressUpdate);
        EventDispatcher.Unsubscribe(EventId.PreCompleteGame, OnPreGameComplete);
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
}
