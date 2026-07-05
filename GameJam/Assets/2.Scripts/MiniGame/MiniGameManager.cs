using System.Collections.Generic;
using UnityEngine;

public class MiniGameManager : Singleton<MiniGameManager>
{
    [SerializeField] private MiniGameConfigSO _config;

    private MiniGameBase _currentMiniGame;
    private readonly Dictionary<MiniGameType, GameObject> _prefabMap = new();
    private readonly Dictionary<MiniGameType, string> _viewMap = new()
    {
        { MiniGameType.CutTree, ViewID.MiniGameGameView_CutTree },
        { MiniGameType.TorPainting, ViewID.MiniGameGameView_TorPainting },
        { MiniGameType.SortHome, ViewID.MiniGameGameView_SortHome },
        { MiniGameType.TheDarkForest, ViewID.MiniGameGameView_TheDarkForest },
        { MiniGameType.TorSortCombo, ViewID.MiniGameGameView_TorSortCombo },
    };

    public MiniGameBase CurrentMiniGame => _currentMiniGame;
    public MiniGameConfigSO Config => _config;

    protected override void Awake()
    {
        base.Awake();
        if (_config == null)
        {
            Debug.LogError("[MiniGameManager] Missing MiniGameConfigSO");
            return;
        }
        foreach (var entry in _config.Entries)
        {
            if (entry.prefab != null && !_prefabMap.ContainsKey(entry.type))
            {
                _prefabMap.Add(entry.type, entry.prefab);
            }
        }
    }

    public void StartGameWithView(MiniGameType type, Transform container = null)
    {
        StartGame(type, container);
        
        if (_viewMap.TryGetValue(type, out var viewId))
        {
            UIManager.Instance.OnShowView(viewId);
        }
    }

    public MiniGameBase StartGame(MiniGameType type, Transform container)
    {
        EndCurrentGame();

        if (!_prefabMap.TryGetValue(type, out var prefab))
        {
            Debug.LogError($"[MiniGameManager] No prefab for {type}");
            return null;
        }
        
        

        var go = Instantiate(prefab, container);
        go.transform.localPosition = Vector3.zero;

        _currentMiniGame = go.GetComponent<MiniGameBase>();
        if (_currentMiniGame == null)
        {
            Debug.LogError($"[MiniGameManager] Prefab for {type} missing MiniGameBase");
            Destroy(go);
            return null;
        }

        _currentMiniGame.Init();
        _currentMiniGame.StartGame();
        return _currentMiniGame;
    }

    public void EndCurrentGame()
    {
        if (_currentMiniGame != null)
        {
            _currentMiniGame.EndGame();
            Destroy(_currentMiniGame.gameObject);
            _currentMiniGame = null;
        }
    }
}
