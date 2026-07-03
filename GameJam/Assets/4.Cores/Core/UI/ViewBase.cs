using DG.Tweening;
using UnityEngine;

public abstract class ViewBase : MonoBehaviour
{
    [SerializeField] protected CanvasGroup canvasGroup;
    protected float _timeDuration = 0.5f;

    public virtual string ViewId => "";

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(ViewId))
        {
            Debug.LogWarning($"[PopupBase] {gameObject.name} has ViewId set to None");
        }
    }

    public virtual void Show(params object[] args)
    {
        gameObject.SetActive(true);
        Debug.Log($"[PopupBase] Showing popup: {ViewId}");
        OnShow();
    }

    public virtual void Hide(bool useAnimation = true)
    {
        Debug.Log($"[PopupBase] Hiding popup: {ViewId}");

        if (! useAnimation)
        {
            gameObject.SetActive(false);
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Debug.LogError("Hide without animation");
            return;
        }
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
        });
        
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
    }
}