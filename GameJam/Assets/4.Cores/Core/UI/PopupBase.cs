using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class PopupBase : MonoBehaviour
{
    [SerializeField] protected string popupId = "";
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected Transform _content;
    [SerializeField] private Button _btnClose;

    protected float _timeDuration = 0.5f;
    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(popupId))
        {
            Debug.LogWarning($"[PopupBase] {gameObject.name} has ViewId set to None");
        }

        _btnClose?.onClick.AddListener(OnPopupClose);
    }

    public virtual void OnPopupClose()
    {
        Hide();
    }
    protected virtual void EndOnShow()
    {
        // Hide();
    }

    

    public virtual void Initialize()
    {
        gameObject.SetActive(false);
    }

    public virtual void Show(params object[] args)
    {
        gameObject.SetActive(true);
        OnShow();
        PlayAudioShow();
    }
    
    public virtual void PlayAudioShow()
    {
    }

    public virtual void Hide()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.RemovePopup(this);
        OnHide();
    }

    protected virtual void OnShow()
    {
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, _timeDuration)
        .SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            EndOnShow();
        });


        DOTween.Kill(_content);
        _content.localScale = Vector3.zero;
        _content.DOScale(Vector3.one, _timeDuration);
        
    }

    protected virtual void OnHide()
    {
        DOTween.Kill(canvasGroup);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0, _timeDuration)
        .SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });

        DOTween.Kill(_content);
        _content.localScale = Vector3.one;
        _content.DOScale(Vector3.zero, _timeDuration);
    }
    
    
}