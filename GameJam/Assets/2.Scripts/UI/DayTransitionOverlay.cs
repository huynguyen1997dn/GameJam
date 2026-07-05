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
/// Also owns the ending: once the ending node (Shrine) is solved, the same overlay
/// fades to black, shows "THE END" and stays there — the game is over.
/// </summary>
public class DayTransitionOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup; // black full-screen Image keeps raycastTarget on
    [SerializeField] private TextMeshProUGUI dayLabel;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float holdDuration = 1.2f;

    [Header("Ending")]
    [Tooltip("Solving this node ends the game: narration dialogue, then the THE END screen.")]
    [SerializeField] private string endingNodeId = "Shrine";
    [Tooltip("DialogPopup phase played before the final fade. None = skip straight to THE END.")]
    [SerializeField] private PhaseId endingDialoguePhaseId = PhaseId.Ending;
    [SerializeField] private string endingText = "THE END";
    [Tooltip("Pause before the ending starts, so the solved node is seen on the map first.")]
    [SerializeField] private float endingDelay = 1.5f;

    private bool _isTransitioning;
    private bool _isEnding;   // ending sequence running; rewound if the object is disabled mid-way
    private bool _endingDone; // THE END is on screen — terminal, never reset

    private void OnEnable()
    {
        EventDispatcher.Subscribe(EventId.NextDay, HandleNextDay);
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged += HandleNodeStateChanged;

        // This overlay lives inside GamePlayView, which is inactive while a minigame
        // view is up — a Shrine solved inside a minigame fires OnNodeStateChanged
        // while nobody is listening. Re-check on every enable so the ending still
        // triggers once the player is back on the map.
        TryStartEnding();
    }

    private void OnDisable()
    {
        EventDispatcher.Unsubscribe(EventId.NextDay, HandleNextDay);
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.OnNodeStateChanged -= HandleNodeStateChanged;

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

        // Ending interrupted mid-sequence (e.g. a view switch): rewind so the next
        // OnEnable restarts it from the top. Once THE END is up it stays for good.
        if (_isEnding && !_endingDone)
        {
            _isEnding = false;
            DOTween.Kill(canvasGroup);
            MapInputLock.Unlock();
        }
    }

    private void HandleNextDay()
    {
        if (_isTransitioning || _isEnding) return;

        var dm = DayManager.Instance;
        if (dm == null || !dm.CanAdvance) return;

        StartCoroutine(TransitionSequence(dm));
    }

    private void HandleNodeStateChanged(string nodeId, int state)
    {
        if (nodeId == endingNodeId) TryStartEnding();
    }

    private void TryStartEnding()
    {
        if (_isEnding || _endingDone) return;

        var gsm = GameStateManager.Instance;
        if (gsm == null || !gsm.IsSolved(endingNodeId)) return;

        _isEnding = true;
        StartCoroutine(EndingSequence());
    }

    // Narration dialogue, then fade to black, show THE END, and never come back:
    // input stays locked and the overlay stays opaque — this is the end of the game.
    private IEnumerator EndingSequence()
    {
        // Let the room close and the solved shrine appear on the map first; if a day
        // transition is somehow mid-flight, let it finish rather than fight its tweens.
        yield return new WaitForSeconds(endingDelay);
        while (_isTransitioning) yield return null;

        MapInputLock.Lock();

        if (endingDialoguePhaseId != PhaseId.None)
        {
            UIManager.Instance.OnShowPopup(PopupId.DialogPopup, endingDialoguePhaseId);
            yield return null;

            var popup = UIManager.Instance.GetCurrentPopup() as DialogPopup;
            if (popup != null)
            {
                bool dialogueClosed = false;
                popup.OnClosed = () => dialogueClosed = true;
                while (!dialogueClosed) yield return null;
            }
            else
            {
                Debug.LogError("[DayTransitionOverlay] Ending DialogPopup not found — skipping narration.");
            }
        }

        canvasGroup.blocksRaycasts = true;
        DOTween.Kill(canvasGroup);
        yield return canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutCubic).WaitForCompletion();

        if (dayLabel != null)
        {
            dayLabel.text = endingText;
            dayLabel.gameObject.SetActive(true);
        }

        _endingDone = true;
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
