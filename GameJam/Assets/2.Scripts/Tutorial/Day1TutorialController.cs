using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hard-coded Day 1 tutorial sequence:
/// Bridge -> Darkwood CutTree -> Bridge construct repair -> Journal -> Next Day reveal.
/// </summary>
public class Day1TutorialController : MonoBehaviour
{
    private const string CompletionPrefsKey = "DAY1_TUTORIAL_COMPLETED";
    private const string LegacyBridgeJournalEntryId = "day_03_bridge_repaired";

    private enum TutorialStep
    {
        None,
        TUT_01_BRIDGE_INTRO,
        TUT_02_BRIDGE_INSPECT,
        TUT_03_GO_TO_FOREST,
        TUT_04_MINIGAME,
        TUT_05_RETURN_TO_BRIDGE,
        TUT_07_JOURNAL_UPDATED,
        TUT_08_OPEN_JOURNAL,
        TUT_09_JOURNAL_OPEN,
        TUT_10_NEXT_DAY,
        TUT_11_DAY_2_REVEAL,
        TUT_12_END,
    }

    [Header("Startup")]
    [SerializeField] private bool autoStartOnDayOne = true;
    [SerializeField] private bool persistCompletionWithPlayerPrefs = true;

    [Header("Targets")]
    [SerializeField] private string bridgeNodeId = "Bridge";
    [SerializeField] private string woodNodeId = "Darkwood";

    [Header("Journal")]
    [SerializeField] private string bridgeJournalEntryId = "day_01_bridge_repaired";
    [SerializeField] private string bridgeProofText =
        "The bridge remains, even if no one remembers who repaired it.";

    [Header("UI")]
    [SerializeField] private Day1TutorialUIView tutorialView;
    [Range(0f, 1f)]
    [SerializeField] private float darkOverlayOpacity = 0.55f;
    [SerializeField] private float journalToastDuration = 2f;
    [SerializeField] private float rewardToastDuration = 1.5f;

    public static bool IsRunning { get; private set; }

    private TutorialStep _currentStep = TutorialStep.None;
    private bool _completedThisSession;
    private bool _bridgeRepairSequenceStarted;
    private bool _subscribedGameState;
    private bool _subscribedDay;
    private bool _subscribedJournal;
    private string _resolvedJournalEntryId;
    private MapCameraController _mapCamera;

    private void OnEnable()
    {
        EventDispatcher.Subscribe<string>(EventId.NodeClicked, HandleNodeClicked);
        EventDispatcher.Subscribe(EventId.CompleteGame, HandleMiniGameCompleted);
        EventDispatcher.Subscribe(EventId.FailGame, HandleMiniGameFailed);
        EventDispatcher.Subscribe(EventId.NextDay, HandleNextDayRequested);
    }

    private void OnDisable()
    {
        EventDispatcher.Unsubscribe<string>(EventId.NodeClicked, HandleNodeClicked);
        EventDispatcher.Unsubscribe(EventId.CompleteGame, HandleMiniGameCompleted);
        EventDispatcher.Unsubscribe(EventId.FailGame, HandleMiniGameFailed);
        EventDispatcher.Unsubscribe(EventId.NextDay, HandleNextDayRequested);

        UnsubscribeManagerEvents();
        ReleaseCameraFocus();

        if (IsRunning)
        {
            IsRunning = false;
            TutorialInputGate.Clear();
        }

        if (tutorialView != null) tutorialView.HideAll();
    }

    private IEnumerator Start()
    {
        yield return null;

        SubscribeManagerEvents();
        EnsureView();

        if (!autoStartOnDayOne) yield break;
        if (IsTutorialCompleted()) yield break;

        var wait = new WaitForSeconds(0.1f);
        while (true)
        {
            var gameplay = FindObjectOfType<GamePlayView>();
            if (gameplay != null && gameplay.isActiveAndEnabled) break;
            yield return wait;
        }

        SubscribeManagerEvents();

        var dayManager = DayManager.Instance;
        if (dayManager != null && dayManager.CurrentDay == 1)
            StartTutorial();
    }

    public void StartTutorial()
    {
        SubscribeManagerEvents();
        EnsureView();

        if (IsTutorialCompleted()) return;

        var dayManager = DayManager.Instance;
        if (dayManager != null && dayManager.CurrentDay != 1) return;

        IsRunning = true;
        _bridgeRepairSequenceStarted = false;
        TutorialInputGate.Begin();

        if (IsNodeSolved(bridgeNodeId))
        {
            AdvanceToStep(TutorialStep.TUT_07_JOURNAL_UPDATED);
            return;
        }

        AdvanceToStep(TutorialStep.TUT_01_BRIDGE_INTRO);
    }

    public void EndTutorial()
    {
        _completedThisSession = true;
        IsRunning = false;
        _currentStep = TutorialStep.TUT_12_END;
        TutorialInputGate.Clear();
        ReleaseCameraFocus();

        if (tutorialView != null) tutorialView.HideAll();

        if (persistCompletionWithPlayerPrefs)
        {
            PlayerPrefs.SetInt(CompletionPrefsKey, 1);
            PlayerPrefs.Save();
        }

        Debug.Log("[Day1Tutorial] Tutorial completed.");
    }

    public void SkipTutorial()
    {
        EndTutorial();
    }

    public void AdvanceToStep(string stepId)
    {
        if (System.Enum.TryParse(stepId, true, out TutorialStep step))
        {
            AdvanceToStep(step);
            return;
        }

        Debug.LogError($"[Day1Tutorial] Unknown step id: {stepId}");
    }

    public void CompleteCurrentStep()
    {
        switch (_currentStep)
        {
            case TutorialStep.TUT_02_BRIDGE_INSPECT:
                AdvanceToStep(TutorialStep.TUT_03_GO_TO_FOREST);
                break;
            case TutorialStep.TUT_07_JOURNAL_UPDATED:
                AdvanceToStep(TutorialStep.TUT_08_OPEN_JOURNAL);
                break;
            case TutorialStep.TUT_09_JOURNAL_OPEN:
                AdvanceToStep(TutorialStep.TUT_10_NEXT_DAY);
                break;
            case TutorialStep.TUT_11_DAY_2_REVEAL:
                EndTutorial();
                break;
        }
    }

    public bool IsTutorialActive()
    {
        return IsRunning;
    }

    public bool IsTutorialCompleted()
    {
        if (_completedThisSession) return true;
        return persistCompletionWithPlayerPrefs && PlayerPrefs.GetInt(CompletionPrefsKey, 0) == 1;
    }

    private void AdvanceToStep(TutorialStep step)
    {
        if (!IsRunning && step != TutorialStep.TUT_12_END) return;

        _currentStep = step;
        Debug.Log($"[Day1Tutorial] Step: {_currentStep}");

        // Every step change drops the previous camera focus; steps that highlight a
        // map node re-focus inside ShowMapHighlight. Follow resumes automatically.
        ReleaseCameraFocus();

        switch (step)
        {
            case TutorialStep.TUT_01_BRIDGE_INTRO:
                ShowBridgeIntro();
                break;
            case TutorialStep.TUT_02_BRIDGE_INSPECT:
                ShowBridgeInspect();
                break;
            case TutorialStep.TUT_03_GO_TO_FOREST:
                ShowGoToForest();
                break;
            case TutorialStep.TUT_04_MINIGAME:
                ShowMinigameStep();
                break;
            case TutorialStep.TUT_05_RETURN_TO_BRIDGE:
                ShowReturnToBridge();
                break;
            case TutorialStep.TUT_07_JOURNAL_UPDATED:
                StartCoroutine(JournalUpdatedRoutine());
                break;
            case TutorialStep.TUT_08_OPEN_JOURNAL:
                ShowOpenJournal();
                break;
            case TutorialStep.TUT_09_JOURNAL_OPEN:
                ShowJournalOpen();
                break;
            case TutorialStep.TUT_10_NEXT_DAY:
                ShowNextDay();
                break;
            case TutorialStep.TUT_11_DAY_2_REVEAL:
                ShowDay2Reveal();
                break;
            case TutorialStep.TUT_12_END:
                EndTutorial();
                break;
        }
    }

    private void ShowBridgeIntro()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideAll();
        ShowDialogForPhase(PhaseId.Intro, () =>
        {
            TutorialInputGate.SetAllowedTarget(bridgeNodeId);
            tutorialView.ShowObjective("Inspect the bridge.");
            ShowMapHighlight(bridgeNodeId);
        });
    }

    private void ShowBridgeInspect()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideAll();
        ShowDialogForPhase(PhaseId.BridgeInspect, () =>
        {
            tutorialView.ShowObjective("Find repair material.");
            AdvanceToStep(TutorialStep.TUT_03_GO_TO_FOREST);
        });
    }

    private void ShowGoToForest()
    {
        if (IsNodeSolved(woodNodeId))
        {
            AdvanceToStep(TutorialStep.TUT_05_RETURN_TO_BRIDGE);
            return;
        }

        TutorialInputGate.BlockAll();
        tutorialView.HideAll();
        ShowDialogForPhase(PhaseId.Forest_01, () =>
        {
            TutorialInputGate.SetAllowedTarget(woodNodeId);
            tutorialView.ShowObjective("Find wood.");
            ShowMapHighlight(woodNodeId);
        });
    }

    private void ShowMinigameStep()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideHighlight();
        tutorialView.HideDialogue();
        tutorialView.ShowObjective("Collect wood.");
    }

    private void ShowReturnToBridge()
    {
        if (IsNodeSolved(bridgeNodeId))
        {
            StartBridgeRepairCompletedSequence();
            return;
        }

        TutorialInputGate.BlockAll();
        tutorialView.HideAll();
        ShowDialogForPhase(PhaseId.Forest_02, () =>
        {
            TutorialInputGate.SetAllowedTarget(bridgeNodeId);
            tutorialView.ShowObjective("Repair the bridge.");
            ShowMapHighlight(bridgeNodeId);
        });
    }

    private IEnumerator JournalUpdatedRoutine()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideHighlight();
        tutorialView.HideDialogue();
        tutorialView.ShowObjective("A trace remains.");

        UnlockBridgeJournalEntry();
        tutorialView.ShowToast("Journal updated.", journalToastDuration);

        yield return new WaitForSeconds(journalToastDuration);

        AdvanceToStep(TutorialStep.TUT_08_OPEN_JOURNAL);
    }

    private void ShowOpenJournal()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideAll();
        ShowDialogForPhase(PhaseId.Village_01, () =>
        {
            TutorialInputGate.SetAllowedTarget(TutorialInputGate.JournalButtonTargetId);
            tutorialView.ShowObjective("Read the trace.");

            var journalButton = FindObjectOfType<JournalMapButton>();
            if (journalButton != null && journalButton.ButtonTarget != null)
            {
                tutorialView.ShowHighlight(journalButton.ButtonTarget);
                return;
            }

            Debug.LogError("[Day1Tutorial] Journal button target not found.");
            tutorialView.HideHighlight();
            StartCoroutine(OpenJournalFallbackRoutine());
        });
    }

    private void ShowJournalOpen()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideHighlight();
        tutorialView.HideObjective();
        tutorialView.HideDialogue();
    }

    private void ShowNextDay()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideAll();
        ShowDialogForPhase(PhaseId.Village_02, () =>
        {
            TutorialInputGate.SetAllowedTarget(TutorialInputGate.NextDayButtonTargetId);
            tutorialView.ShowObjective("Start a new day.");

            var dayHud = FindObjectOfType<DayHUD>();
            if (dayHud != null && dayHud.AdvanceButtonTarget != null)
            {
                tutorialView.ShowHighlight(dayHud.AdvanceButtonTarget);
                return;
            }

            Debug.LogError("[Day1Tutorial] Next Day button target not found.");
            tutorialView.HideHighlight();
        });
    }

    private void ShowDay2Reveal()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideHighlight();
        tutorialView.ShowObjective("Explore the village.");
        ShowDialogForPhase(PhaseId.Day2Reveal, EndTutorial);
    }

    private void HandleNodeClicked(string nodeId)
    {
        if (!IsRunning) return;

        if (_currentStep == TutorialStep.TUT_01_BRIDGE_INTRO && nodeId == bridgeNodeId)
        {
            AdvanceToStep(TutorialStep.TUT_02_BRIDGE_INSPECT);
            return;
        }

        if (_currentStep == TutorialStep.TUT_03_GO_TO_FOREST && nodeId == woodNodeId)
        {
            AdvanceToStep(TutorialStep.TUT_04_MINIGAME);
            return;
        }

        if (_currentStep == TutorialStep.TUT_05_RETURN_TO_BRIDGE && nodeId == bridgeNodeId)
            WaitForBridgeConstructRepair();
    }

    private void HandleMiniGameCompleted()
    {
        if (!IsRunning || _currentStep != TutorialStep.TUT_04_MINIGAME) return;
        StartCoroutine(ResumeAfterMinigameRoutine(true));
    }

    private void HandleMiniGameFailed()
    {
        if (!IsRunning || _currentStep != TutorialStep.TUT_04_MINIGAME) return;
        StartCoroutine(ResumeAfterMinigameRoutine(false));
    }

    private IEnumerator ResumeAfterMinigameRoutine(bool success)
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideHighlight();
        tutorialView.HideDialogue();

        yield return null;

        float elapsed = 0f;
        while (MiniGameManager.Instance != null && MiniGameManager.Instance.CurrentMiniGame != null && elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (success)
        {
            tutorialView.ShowToast("Wood obtained.", rewardToastDuration);
            yield return new WaitForSeconds(0.25f);
            AdvanceToStep(TutorialStep.TUT_05_RETURN_TO_BRIDGE);
        }
        else
        {
            tutorialView.ShowToast("Try again. The bridge still needs wood.", rewardToastDuration);
            yield return new WaitForSeconds(rewardToastDuration);
            AdvanceToStep(TutorialStep.TUT_03_GO_TO_FOREST);
        }
    }

    private void HandleNodeStateChanged(string nodeId, int state)
    {
        if (!IsRunning) return;
        if (_currentStep != TutorialStep.TUT_05_RETURN_TO_BRIDGE) return;
        if (nodeId != bridgeNodeId || state < 1) return;

        StartBridgeRepairCompletedSequence();
    }

    private void HandleJournalOpened()
    {
        if (!IsRunning || _currentStep != TutorialStep.TUT_08_OPEN_JOURNAL) return;

        var journal = JournalManager.Instance;
        if (journal != null && !string.IsNullOrEmpty(_resolvedJournalEntryId) &&
            journal.IsEntryUnlocked(_resolvedJournalEntryId))
        {
            journal.SelectEntry(_resolvedJournalEntryId);
        }

        AdvanceToStep(TutorialStep.TUT_09_JOURNAL_OPEN);
    }

    private void HandleJournalClosed()
    {
        if (!IsRunning || _currentStep != TutorialStep.TUT_09_JOURNAL_OPEN) return;
        AdvanceToStep(TutorialStep.TUT_10_NEXT_DAY);
    }

    private void HandleNextDayRequested()
    {
        if (!IsRunning || _currentStep != TutorialStep.TUT_10_NEXT_DAY) return;

        TutorialInputGate.BlockAll();
        tutorialView.HideHighlight();
        tutorialView.HideDialogue();
        tutorialView.HideObjective();
    }

    private void HandleDayChanged(int dayNumber)
    {
        if (!IsRunning || _currentStep != TutorialStep.TUT_10_NEXT_DAY) return;
        if (dayNumber != 2) return;

        StartCoroutine(Day2RevealAfterTransitionRoutine());
    }

    private IEnumerator Day2RevealAfterTransitionRoutine()
    {
        while (MapInputLock.IsLocked)
            yield return null;

        yield return new WaitForSeconds(0.15f);
        AdvanceToStep(TutorialStep.TUT_11_DAY_2_REVEAL);
    }

    private void StartBridgeRepairCompletedSequence()
    {
        if (_bridgeRepairSequenceStarted) return;
        _bridgeRepairSequenceStarted = true;
        StartCoroutine(BridgeRepairCompletedRoutine());
    }

    private void WaitForBridgeConstructRepair()
    {
        TutorialInputGate.BlockAll();
        tutorialView.HideHighlight();
        tutorialView.HideDialogue();
        tutorialView.ShowObjective("Repair the bridge.");

        if (IsNodeSolved(bridgeNodeId))
            StartBridgeRepairCompletedSequence();
    }

    private IEnumerator BridgeRepairCompletedRoutine()
    {
        TutorialInputGate.BlockAll();

        while (MapInputLock.IsLocked)
            yield return null;

        tutorialView.HideHighlight();
        tutorialView.ShowObjective("Repair the bridge.");
        ShowDialogForPhase(PhaseId.BridgeRepaired, () => AdvanceToStep(TutorialStep.TUT_07_JOURNAL_UPDATED));
    }

    private IEnumerator OpenJournalFallbackRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (!IsRunning || _currentStep != TutorialStep.TUT_08_OPEN_JOURNAL) yield break;

        var journal = JournalManager.Instance;
        if (journal != null) journal.OpenJournal();
    }

    private void UnlockBridgeJournalEntry()
    {
        var journal = JournalManager.Instance;
        if (journal == null) return;

        _resolvedJournalEntryId = ResolveJournalEntryId(journal);
        if (string.IsNullOrEmpty(_resolvedJournalEntryId)) return;

        if (!journal.IsEntryUnlocked(_resolvedJournalEntryId))
            journal.UnlockEntry(_resolvedJournalEntryId);

        journal.AddProofNote(_resolvedJournalEntryId, bridgeProofText);
    }

    private string ResolveJournalEntryId(JournalManager journal)
    {
        if (journal.Database == null) return bridgeJournalEntryId;

        if (journal.Database.GetEntry(bridgeJournalEntryId) != null)
            return bridgeJournalEntryId;

        if (journal.Database.GetEntry(LegacyBridgeJournalEntryId) != null)
        {
            Debug.LogWarning($"[Day1Tutorial] Journal entry '{bridgeJournalEntryId}' not found. " +
                             $"Using '{LegacyBridgeJournalEntryId}' instead.");
            return LegacyBridgeJournalEntryId;
        }

        Debug.LogError($"[Day1Tutorial] Journal entry not found: {bridgeJournalEntryId}");
        return bridgeJournalEntryId;
    }

    private void ShowMapHighlight(string nodeId)
    {
        var node = FindMapNode(nodeId);
        if (node == null)
        {
            Debug.LogError($"[Day1Tutorial] Tutorial target not found: {nodeId}");
            tutorialView.HideHighlight();
            return;
        }

        tutorialView.ShowHighlight(node.transform);
        FocusCameraOn(node.transform);
    }

    // Bypasses the map camera's follow rule so the view glides onto the highlighted
    // node instead of staying centered on the character.
    private void FocusCameraOn(Transform target)
    {
        if (_mapCamera == null) _mapCamera = FindObjectOfType<MapCameraController>();
        if (_mapCamera != null) _mapCamera.SetFocusTarget(target);
        else Debug.LogWarning("[Day1Tutorial] MapCameraController not found — camera focus skipped.");
    }

    private void ReleaseCameraFocus()
    {
        if (_mapCamera != null) _mapCamera.ClearFocusTarget();
    }

    private MapNode FindMapNode(string nodeId)
    {
        var nodes = FindObjectsOfType<MapNode>();
        foreach (var node in nodes)
            if (node != null && node.nodeId == nodeId)
                return node;

        return null;
    }

    private bool IsNodeSolved(string nodeId)
    {
        var gameState = GameStateManager.Instance;
        return gameState != null && gameState.IsSolved(nodeId);
    }

    private void EnsureView()
    {
        if (tutorialView != null)
        {
            tutorialView.SetDarkOverlayOpacity(darkOverlayOpacity);
            return;
        }

        tutorialView = FindObjectOfType<Day1TutorialUIView>();
        if (tutorialView != null)
        {
            tutorialView.SetDarkOverlayOpacity(darkOverlayOpacity);
            return;
        }

        var canvasObject = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var viewObject = new GameObject("Day1TutorialUIView", typeof(RectTransform), typeof(Day1TutorialUIView));
        viewObject.transform.SetParent(canvas.transform, false);

        var rect = viewObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        tutorialView = viewObject.GetComponent<Day1TutorialUIView>();
        tutorialView.SetDarkOverlayOpacity(darkOverlayOpacity);
    }

    private void ShowDialogForPhase(PhaseId phaseId, System.Action onClosed)
    {
        StartCoroutine(ShowDialogForPhaseRoutine(phaseId, onClosed));
    }

    private System.Collections.IEnumerator ShowDialogForPhaseRoutine(PhaseId phaseId, System.Action onClosed)
    {
        UIManager.Instance.OnShowPopup(PopupId.DialogPopup, phaseId);
        yield return null;

        var popup = UIManager.Instance.GetCurrentPopup() as DialogPopup;
        if (popup != null)
        {
            popup.OnClosed = onClosed;
        }
        else
        {
            Debug.LogError("[Day1Tutorial] DialogPopup not found");
            onClosed?.Invoke();
        }
    }

    private void SubscribeManagerEvents()
    {
        if (!_subscribedGameState && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnNodeStateChanged += HandleNodeStateChanged;
            _subscribedGameState = true;
        }

        if (!_subscribedDay && DayManager.Instance != null)
        {
            DayManager.Instance.OnDayChanged += HandleDayChanged;
            _subscribedDay = true;
        }

        if (!_subscribedJournal && JournalManager.Instance != null)
        {
            JournalManager.Instance.OnJournalOpened += HandleJournalOpened;
            JournalManager.Instance.OnJournalClosed += HandleJournalClosed;
            _subscribedJournal = true;
        }
    }

    private void UnsubscribeManagerEvents()
    {
        if (_subscribedGameState && GameStateManager.Instance != null)
            GameStateManager.Instance.OnNodeStateChanged -= HandleNodeStateChanged;

        if (_subscribedDay && DayManager.Instance != null)
            DayManager.Instance.OnDayChanged -= HandleDayChanged;

        if (_subscribedJournal && JournalManager.Instance != null)
        {
            JournalManager.Instance.OnJournalOpened -= HandleJournalOpened;
            JournalManager.Instance.OnJournalClosed -= HandleJournalClosed;
        }

        _subscribedGameState = false;
        _subscribedDay = false;
        _subscribedJournal = false;
    }
}
