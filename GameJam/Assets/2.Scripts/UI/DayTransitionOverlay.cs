using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Full-screen darken → "DAY XX" → reveal transition. The single subscriber to
/// EventId.NextDay (dispatched by DayHUD's Advance button and by NextDayPopup on
/// close), and the owner of the advance sequence: DayManager.AdvanceDay runs at the
/// midpoint, while the screen is fully dark, so node reveals and HUD updates are
/// never seen happening.
/// Lives on an always-active object placed as the LAST sibling under the Canvas.
/// Rest state: canvasGroup alpha 0 + blocksRaycasts off, day label inactive.
/// </summary>
public class DayTransitionOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup; // black full-screen Image keeps raycastTarget on
    [SerializeField] private TextMeshProUGUI dayLabel;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float holdDuration = 1.2f;

    private bool _isTransitioning;

    private void OnEnable()
    {
        EventDispatcher.Subscribe(EventId.NextDay, HandleNextDay);
    }

    private void OnDisable()
    {
        EventDispatcher.Unsubscribe(EventId.NextDay, HandleNextDay);

        // Coroutines die with the object — never leave the map frozen or blacked out.
        if (_isTransitioning)
        {
            _isTransitioning = false;
            DOTween.Kill(canvasGroup);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            if (dayLabel != null) dayLabel.gameObject.SetActive(false);
            MapInputLock.Unlock();
        }
    }

    private void HandleNextDay()
    {
        if (_isTransitioning) return;

        var dm = DayManager.Instance;
        if (dm == null || !dm.CanAdvance) return;

        StartCoroutine(TransitionSequence(dm));
    }

    private IEnumerator TransitionSequence(DayManager dm)
    {
        _isTransitioning = true;
        MapInputLock.Lock();
        canvasGroup.blocksRaycasts = true; // swallow UI clicks too, not just map input

        DOTween.Kill(canvasGroup);
        yield return canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutCubic).WaitForCompletion();

        // Fully dark: change the world now.
        dm.AdvanceDay();

        if (dayLabel != null)
        {
            dayLabel.text = $"DAY {dm.CurrentDay:00}";
            dayLabel.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(holdDuration);

        if (dayLabel != null) dayLabel.gameObject.SetActive(false);

        yield return canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InOutCubic).WaitForCompletion();

        canvasGroup.blocksRaycasts = false;
        MapInputLock.Unlock();
        _isTransitioning = false;
    }
}
