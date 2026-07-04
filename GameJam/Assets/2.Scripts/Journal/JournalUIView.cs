using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the Journal overlay on top of the map: dim background, one entry page
/// (title, illustration, body, clues, proof), prev/next footer and the collapsible
/// day-list sidebar. Pure view — every click is forwarded to JournalManager and every
/// refresh re-reads its state. This object stays always-active (subscriptions live
/// here); it toggles the `root` child, mirroring the DayHUD pattern.
/// Rest state: root child inactive, canvasGroup alpha 0 + blocksRaycasts off.
/// </summary>
public class JournalUIView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;              // toggled child holding all visuals
    [SerializeField] private CanvasGroup canvasGroup;      // on this object, fades dim + panel together
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Header")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dayListButton;
    [SerializeField] private Button dimButton;             // tap outside: closes sidebar, else journal

    [Header("Entry Content")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private TextMeshProUGUI entryTitleText;
    [SerializeField] private Image illustrationImage;
    [SerializeField] private GameObject illustrationPlaceholder;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private GameObject clueSection;
    [SerializeField] private TextMeshProUGUI clueEmptyText; // "No clues recorded."
    [SerializeField] private Transform clueContainer;
    [SerializeField] private JournalClueItem clueItemTemplate; // inactive template child
    [SerializeField] private GameObject proofSection;
    [SerializeField] private TextMeshProUGUI proofText;
    [SerializeField] private ScrollRect contentScroll;

    [Header("Empty / Error State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Footer Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI footerDayLabel; // "D3"

    [Header("Day List Sidebar")]
    [SerializeField] private GameObject sidebarRoot;
    [SerializeField] private Transform dayButtonContainer;
    [SerializeField] private JournalDayButton dayButtonTemplate; // inactive template child
    [SerializeField] private bool closeSidebarOnSelect = true;

    [Header("Locked Toast")]
    [SerializeField] private GameObject lockedToast;
    [SerializeField] private TextMeshProUGUI lockedToastText;
    [SerializeField] private float lockedToastDuration = 1.5f;

    private readonly List<JournalClueItem> _spawnedClueItems = new List<JournalClueItem>();
    private readonly List<JournalDayButton> _spawnedDayButtons = new List<JournalDayButton>();
    private Coroutine _toastRoutine;

    private const string EmptyJournalMessage =
        "No journal entry yet.\nExplore the village to leave your first trace.";
    private const string MissingEntryMessage = "Missing journal entry.";
    private const string LockedEntryMessage = "This page has not been written yet.";
    private const string NoCluesMessage = "No clues recorded.";

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        if (dayListButton != null) dayListButton.onClick.AddListener(OnDayListClicked);
        if (dimButton != null) dimButton.onClick.AddListener(OnDimClicked);
        if (previousButton != null) previousButton.onClick.AddListener(OnPreviousClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);

        // Rest state, in case the editor left something visible.
        if (root != null) root.SetActive(false);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void OnEnable()
    {
        var jm = JournalManager.Instance;
        if (jm == null) return;
        jm.OnJournalOpened += HandleOpened;
        jm.OnJournalClosed += HandleClosed;
        jm.OnEntrySelected += HandleEntrySelected;
        jm.OnContentChanged += HandleContentChanged;
        jm.OnUnreadStateChanged += HandleUnreadChanged;
    }

    private void OnDisable()
    {
        var jm = JournalManager.Instance;
        if (jm == null) return;
        jm.OnJournalOpened -= HandleOpened;
        jm.OnJournalClosed -= HandleClosed;
        jm.OnEntrySelected -= HandleEntrySelected;
        jm.OnContentChanged -= HandleContentChanged;
        jm.OnUnreadStateChanged -= HandleUnreadChanged;

        // Tweens die with the object — never leave the overlay half-faded over the map.
        if (canvasGroup != null) DOTween.Kill(canvasGroup);
    }

    // ---------------------- MANAGER EVENTS ----------------------

    private void HandleOpened()
    {
        if (root != null) root.SetActive(true);
        SetSidebarVisible(false);
        HideToastImmediate();
        RefreshAll();

        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
            canvasGroup.alpha = 0f;
            // Blocks immediately: the map UI behind must be dead for the whole fade.
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutCubic);
        }
    }

    private void HandleClosed()
    {
        HideToastImmediate();
        if (canvasGroup == null)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        DOTween.Kill(canvasGroup);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            if (root != null) root.SetActive(false);
        });
    }

    private void HandleEntrySelected(string entryId) => RefreshIfOpen();
    private void HandleContentChanged() => RefreshIfOpen();
    private void HandleUnreadChanged(bool hasUnread) => RefreshIfOpen();

    private void RefreshIfOpen()
    {
        var jm = JournalManager.Instance;
        if (jm != null && jm.IsOpen) RefreshAll();
    }

    // ---------------------- USER INPUT ----------------------

    private void OnCloseClicked() => JournalManager.Instance?.CloseJournal();

    private void OnDayListClicked()
    {
        if (sidebarRoot == null) return;
        SetSidebarVisible(!sidebarRoot.activeSelf);
    }

    // Tap outside the panel: first dismisses the sidebar, a second tap closes the journal.
    private void OnDimClicked()
    {
        if (sidebarRoot != null && sidebarRoot.activeSelf)
        {
            SetSidebarVisible(false);
            return;
        }
        JournalManager.Instance?.CloseJournal();
    }

    private void OnPreviousClicked() => JournalManager.Instance?.SelectPreviousUnlockedEntry();
    private void OnNextClicked() => JournalManager.Instance?.SelectNextUnlockedEntry();

    private void OnDayButtonClicked(string entryId, bool locked)
    {
        if (locked)
        {
            ShowLockedToast();
            return;
        }
        JournalManager.Instance?.SelectEntry(entryId);
        if (closeSidebarOnSelect) SetSidebarVisible(false);
    }

    private void SetSidebarVisible(bool visible)
    {
        if (sidebarRoot != null) sidebarRoot.SetActive(visible);
    }

    // ---------------------- RENDERING ----------------------

    private void RefreshAll()
    {
        var jm = JournalManager.Instance;
        if (jm == null) return;

        RefreshEntry(jm);
        RefreshNavigation(jm);
        RefreshDayList(jm);
    }

    private void RefreshEntry(JournalManager jm)
    {
        string entryId = jm.SelectedEntryId;

        if (string.IsNullOrEmpty(entryId))
        {
            ShowEmptyState(EmptyJournalMessage);
            return;
        }

        var entry = jm.Database != null ? jm.Database.GetEntry(entryId) : null;
        if (entry == null)
        {
            Debug.LogError($"[JournalUIView] Entry '{entryId}' is unlocked but has no data.");
            ShowEmptyState(MissingEntryMessage);
            return;
        }

        if (emptyStateRoot != null) emptyStateRoot.SetActive(false);
        if (contentRoot != null) contentRoot.SetActive(true);

        if (entryTitleText != null)
            entryTitleText.text = $"Day {entry.dayNumber} — {entry.title}";

        bool hasIllustration = entry.illustration != null;
        if (illustrationImage != null)
        {
            illustrationImage.enabled = hasIllustration;
            illustrationImage.sprite = entry.illustration;
        }
        if (illustrationPlaceholder != null) illustrationPlaceholder.SetActive(!hasIllustration);

        if (bodyText != null) bodyText.text = entry.bodyText;

        RefreshClues(jm, entry);
        RefreshProof(jm, entry);

        // New page starts at the top, not wherever the last one was scrolled to.
        if (contentScroll != null) contentScroll.verticalNormalizedPosition = 1f;
    }

    private void ShowEmptyState(string message)
    {
        if (contentRoot != null) contentRoot.SetActive(false);
        if (emptyStateRoot != null) emptyStateRoot.SetActive(true);
        if (emptyStateText != null) emptyStateText.text = message;
        if (footerDayLabel != null) footerDayLabel.text = "-";
    }

    private void RefreshClues(JournalManager jm, JournalEntryData entry)
    {
        foreach (var item in _spawnedClueItems)
            if (item != null) item.gameObject.SetActive(false);

        bool hasAnyClueDefined = entry.clueIds != null && entry.clueIds.Count > 0;
        if (clueSection != null) clueSection.SetActive(hasAnyClueDefined);
        if (!hasAnyClueDefined) return;

        int shown = 0;
        foreach (var clueId in entry.clueIds)
        {
            if (!jm.IsClueDiscovered(clueId)) continue;
            var clue = jm.Database.GetClue(clueId);
            if (clue == null)
            {
                Debug.LogError($"[JournalUIView] Clue '{clueId}' discovered but has no data.");
                continue;
            }

            var item = GetOrSpawnClueItem(shown);
            if (item == null) break;
            item.gameObject.SetActive(true);
            item.Setup(clue.text, jm.IsClueResolved(clueId));
            shown++;
        }

        if (clueEmptyText != null) clueEmptyText.gameObject.SetActive(shown == 0);
        if (clueEmptyText != null && shown == 0) clueEmptyText.text = NoCluesMessage;
    }

    private JournalClueItem GetOrSpawnClueItem(int index)
    {
        if (index < _spawnedClueItems.Count) return _spawnedClueItems[index];
        if (clueItemTemplate == null || clueContainer == null) return null;

        var item = Instantiate(clueItemTemplate, clueContainer);
        _spawnedClueItems.Add(item);
        return item;
    }

    private void RefreshProof(JournalManager jm, JournalEntryData entry)
    {
        string proof = jm.GetProofText(entry);
        bool hasProof = !string.IsNullOrEmpty(proof);
        if (proofSection != null) proofSection.SetActive(hasProof);
        if (proofText != null) proofText.text = hasProof ? proof : string.Empty;
    }

    private void RefreshNavigation(JournalManager jm)
    {
        bool hasEntry = !string.IsNullOrEmpty(jm.SelectedEntryId);
        if (previousButton != null) previousButton.interactable = hasEntry && jm.HasPreviousUnlockedEntry();
        if (nextButton != null) nextButton.interactable = hasEntry && jm.HasNextUnlockedEntry();

        if (footerDayLabel != null && hasEntry)
        {
            var entry = jm.Database != null ? jm.Database.GetEntry(jm.SelectedEntryId) : null;
            footerDayLabel.text = entry != null ? $"D{entry.dayNumber}" : "-";
        }
    }

    private void RefreshDayList(JournalManager jm)
    {
        foreach (var button in _spawnedDayButtons)
            if (button != null) button.gameObject.SetActive(false);

        if (jm.Database == null || dayButtonTemplate == null || dayButtonContainer == null) return;

        int index = 0;
        foreach (var entry in jm.Database.GetEntriesOrderedByDay())
        {
            JournalDayButton button;
            if (index < _spawnedDayButtons.Count)
            {
                button = _spawnedDayButtons[index];
            }
            else
            {
                button = Instantiate(dayButtonTemplate, dayButtonContainer);
                _spawnedDayButtons.Add(button);
            }
            index++;

            bool locked = !jm.IsEntryUnlocked(entry.entryId);
            bool selected = entry.entryId == jm.SelectedEntryId;
            bool unread = jm.IsEntryUnread(entry.entryId);
            string entryId = entry.entryId; // capture per button, not the loop variable

            button.gameObject.SetActive(true);
            button.Setup(entry.dayNumber, locked, selected, unread,
                () => OnDayButtonClicked(entryId, locked));
        }
    }

    // ---------------------- LOCKED TOAST ----------------------

    private void ShowLockedToast()
    {
        if (lockedToast == null) return;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        if (lockedToastText != null) lockedToastText.text = LockedEntryMessage;
        lockedToast.SetActive(true);
        yield return new WaitForSeconds(lockedToastDuration);
        lockedToast.SetActive(false);
        _toastRoutine = null;
    }

    private void HideToastImmediate()
    {
        if (_toastRoutine != null)
        {
            StopCoroutine(_toastRoutine);
            _toastRoutine = null;
        }
        if (lockedToast != null) lockedToast.SetActive(false);
    }
}
