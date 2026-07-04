using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public partial class PopupId
{
    public static string DialogPopup = "DialogPopup";
}

public class DialogPopup : PopupBase
{
    [Header("Dialog Settings")]
    [SerializeField] private Transform _entryContainer;
    [SerializeField] private DialogEntryItem _entryPrefab;
    [SerializeField] private TextMeshProUGUI _tapHintText;

    [Header("Animation")]
    [SerializeField] private float _entrySlideDistance = 180f;
    [SerializeField] private float _entryFadeAlpha = 0.3f;
    [SerializeField] private float _entryAnimationDuration = 0.4f;
    [SerializeField] private Ease _entryEase = Ease.OutCubic;

    [Header("Database")]
    [SerializeField] private DialogDatabaseSO _database;

    [Header("Character Data")]
    [SerializeField] private List<CharacterDataSO> _characterDataList = new();

    public System.Action OnClosed { private get; set; }

    private PhaseId _currentPhaseId;
    private DialogPhaseSO _currentPhase;
    private int _currentIndex;
    private bool _isAnimating;
    private readonly List<DialogEntryItem> _activeEntries = new();

    protected override void Awake()
    {
        base.Awake();
        _entryPrefab.gameObject.SetActive(false);
    }
    

    protected override void OnHide()
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
        // _content.DOScale(Vector3.zero, _timeDuration);
    }

    protected override void OnShow()
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
        _content.localScale = Vector3.one;
        // _content.DOScale(Vector3.one, _timeDuration);
        
    }


    public override void Show(params object[] args)
    {
        if (args.Length < 1)
        {
            Debug.LogError("[DialogPopup] Need args: phaseId");
            return;
        }

        _currentPhaseId = (PhaseId)args[0];

        Debug.Log("[DialogPopup] Showing " + _currentPhaseId);
        if (_database == null)
        {
            Debug.LogError("[DialogPopup] Database is not assigned in Inspector!");
            return;
        }

        _currentPhase = _database.GetPhase(_currentPhaseId);
        if (_currentPhase == null || _currentPhase.entries.Count == 0)
        {
            Debug.LogError($"[DialogPopup] No phase found for {_currentPhaseId}");
            return;
        }

        _currentIndex = 0;
        ClearEntries();
        base.Show(args);
        ShowEntry(0);
    }

    private void Update()
    {
        if (_isAnimating) return;
        if (_currentPhase == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            OnTap();
        }
    }

    private void OnTap()
    {
        if (_currentPhase == null) return;

        _currentIndex++;

        if (_currentIndex >= _currentPhase.entries.Count)
        {
            Hide();
            return;
        }

        // Push current entries up
        PushEntriesUp();

        // Show next entry
        ShowEntry(_currentIndex);
    }

    private void ShowEntry(int index)
    {
        if (index < 0 || index >= _currentPhase.entries.Count) return;

        var entry = _currentPhase.entries[index];
        var charData = GetCharacterData(entry.characterId);

        var item = Instantiate(_entryPrefab, _entryContainer);
        item.gameObject.SetActive(true);
        item.transform.SetAsLastSibling();

        var rect = item.transform as RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        var canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, _entryAnimationDuration).SetEase(_entryEase);
        }

        item.Setup(
            charData != null ? charData.icon : null,
            charData != null ? charData.displayName : entry.characterId.ToString(),
            entry.description,
            charData != null ? charData.nameColor : Color.white
        );

        _activeEntries.Add(item);

        UpdateTapHint();
    }

    private void PushEntriesUp()
    {
        _isAnimating = true;
        float delay = 0f;
        int count = _activeEntries.Count;

        for (int i = 0; i < count; i++)
        {
            var entry = _activeEntries[i];
            var rect = entry.transform as RectTransform;
            if (rect == null) continue;

            float targetY = rect.anchoredPosition.y + _entrySlideDistance;

            float targetAlpha = 1f;
            if (count > 1)
            {
                float t = (float)i / (count - 1);
                targetAlpha = Mathf.Lerp(_entryFadeAlpha, 1f, t);
            }

            rect.DOAnchorPosY(targetY, _entryAnimationDuration)
                .SetDelay(delay)
                .SetEase(_entryEase);

            var group = entry.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.DOFade(targetAlpha, _entryAnimationDuration)
                    .SetDelay(delay)
                    .SetEase(_entryEase);
            }

            delay += 0.02f;
        }

        float totalWait = _entryAnimationDuration + delay;
        DOVirtual.DelayedCall(totalWait, () =>
        {
            _isAnimating = false;
        });
    }

    private void ClearEntries()
    {
        foreach (var entry in _activeEntries)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }
        _activeEntries.Clear();
    }

    private CharacterDataSO GetCharacterData(CharacterId characterId)
    {
        return _characterDataList.Find(c => c.characterId == characterId);
    }

    private void UpdateTapHint()
    {
        if (_tapHintText == null) return;
        bool isLast = _currentIndex >= _currentPhase.entries.Count - 1;
        _tapHintText.text = isLast ? "Tap to close" : "Tap to continue";
    }

    public override void Hide()
    {
        _isAnimating = false;
        base.Hide();
        var callback = OnClosed;
        OnClosed = null;
        callback?.Invoke();
    }
}
