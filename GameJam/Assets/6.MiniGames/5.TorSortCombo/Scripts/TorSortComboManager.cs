using System.Collections;
using UnityEngine;

// Two-phase combo minigame: phase 1 is TorPainting, phase 2 is SortHome.
// Sub-games run with SuppressCompleteEvents so only the combo dispatches
// PreCompleteGame, after the SortHome phase is finished.
public class TorSortComboManager : MiniGameBase
{
    public const int PhaseTorPainting = 1;
    public const int PhaseSortHome = 2;
    public const int PhaseCount = 2;

    [SerializeField] private TorSortComboConfig _config;

    private MiniGameBase _currentPhaseGame;
    private int _currentPhase;
    private bool _isGameOver;

    public TorSortComboConfig Config => _config;
    public int CurrentPhase => _currentPhase;
    public bool IsGameOver => _isGameOver;

    public override MiniGameType MiniGameType => global::MiniGameType.TorSortCombo;
    public override string MiniGameId => "TorSortCombo";

    public override void Init()
    {
        ValidateConfig();
    }

    public override void StartGame()
    {
        if (!ValidateConfig()) return;
        StartPhase(PhaseTorPainting);
    }

    public override void EndGame()
    {
        if (_currentPhaseGame != null)
        {
            _currentPhaseGame.EndGame();
        }
    }

    private bool ValidateConfig()
    {
        if (_config == null)
        {
            Debug.LogError("[TorSortCombo] Missing config!");
            return false;
        }
        if (_config.torPaintingPrefab == null)
        {
            Debug.LogError("[TorSortCombo] Missing torPaintingPrefab in config!");
            return false;
        }
        if (_config.sortHomePrefab == null)
        {
            Debug.LogError("[TorSortCombo] Missing sortHomePrefab in config!");
            return false;
        }
        return true;
    }

    private void StartPhase(int phase)
    {
        CleanupCurrentPhase();

        _currentPhase = phase;
        GameObject prefab = phase == PhaseTorPainting
            ? _config.torPaintingPrefab
            : _config.sortHomePrefab;

        GameObject go = Instantiate(prefab, transform);
        go.transform.localPosition = Vector3.zero;

        _currentPhaseGame = go.GetComponent<MiniGameBase>();
        if (_currentPhaseGame == null)
        {
            Debug.LogError($"[TorSortCombo] Phase {phase} prefab missing MiniGameBase!");
            Destroy(go);
            return;
        }

        _currentPhaseGame.SuppressCompleteEvents = true;
        _currentPhaseGame.OnGameComplete += OnPhaseComplete;

        EventDispatcher.Dispatch(EventId.MiniGamePhaseChanged, phase);

        _currentPhaseGame.Init();
        _currentPhaseGame.StartGame();
    }

    private void OnPhaseComplete()
    {
        if (_isGameOver) return;

        if (_currentPhase == PhaseTorPainting)
        {
            StartCoroutine(TransitionToNextPhase());
        }
        else
        {
            _isGameOver = true;
            CompleteGame();
        }
    }

    private IEnumerator TransitionToNextPhase()
    {
        yield return new WaitForSeconds(_config.phaseTransitionDelay);
        StartPhase(PhaseSortHome);
    }

    private void CleanupCurrentPhase()
    {
        if (_currentPhaseGame == null) return;

        _currentPhaseGame.OnGameComplete -= OnPhaseComplete;
        Destroy(_currentPhaseGame.gameObject);
        _currentPhaseGame = null;
    }
}
