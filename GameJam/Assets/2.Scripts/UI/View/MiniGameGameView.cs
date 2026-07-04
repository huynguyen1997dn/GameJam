using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class MiniGameGameView : ViewBase
{
    [SerializeField] protected Slider _progressSlider;
    [SerializeField] protected Button _completeButton;
    [SerializeField] protected TextMeshProUGUI _progressText;
    [SerializeField] protected Image _backgroundImage;

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

    protected virtual bool IsProgressForThisView(MiniGameType type)
    {
        return type == GameType;
    }

    // UI-space background (Screen Space - Overlay canvas draws over the world,
    // so only use this for sprites with a transparent gameplay area, e.g. frames).
    // Pass null to hide the background image.
    protected void ApplyBackground(Sprite sprite)
    {
        if (_backgroundImage == null) return;
        _backgroundImage.gameObject.SetActive(sprite != null);
        if (sprite != null)
            _backgroundImage.sprite = sprite;
    }

    protected virtual void OnProgressUpdate(MiniGameProgressData data)
    {
        if (!IsProgressForThisView(data.gameType)) return;

        if (_progressSlider != null)
        {
            _progressSlider.maxValue = data.target;
            _progressSlider.value = data.current;
        }

        if (_progressText != null)
            _progressText.text = $"{data.current}/{data.target}";
    }
}
