using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutTreeManager : MiniGameBase
{
    [SerializeField] private CutTreeConfig _config;
    [SerializeField] private Camera _cam;

    private int _currentCutCount;
    private bool _isDragging;
    private bool _isGameOver;
    private TreeController _lastTouchedTree;
    public List<TreeController> _aliveTrees = new();

    public override MiniGameType MiniGameType => global::MiniGameType.CutTree;
    public override string MiniGameId => "CutTree";

    public int CurrentCutCount => _currentCutCount;
    public int TargetCutCount => _config ? _config.targetTreesToCut : 0;
    public bool IsGameOver => _isGameOver;

    public override void Init()
    {
        if (!_cam) _cam = Camera.main;
        InitTrees();
    }

    private void InitTrees()
    {
        if (!_config || !_config.treePrefab)
        {
            Debug.LogError("[CutTree] Missing config or treePrefab!");
            return;
        }

        for (int i = 0; i < _config.initialTreeCount; i++)
        {
            SpawnTree();
        }

        Debug.Log($"[CutTree] Spawned {_aliveTrees.Count} trees. Target: {_config.targetTreesToCut}");
    }

    private void SpawnTree()
    {
        Vector2 pos = GetRandomPosition();
        GameObject go = ObjectPoolManager.Instance.GetObject2D(_config.treePrefab, pos, transform);
        if (!go) return;

        TreeController tree = go.GetComponent<TreeController>();
        if (!tree)
        {
            Debug.LogError("[CutTree] treePrefab missing TreeController!");
            return;
        }

        tree.Init(this);
        _aliveTrees.Add(tree);
    }

    private void RespawnTree()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_config.respawnDelay);

        if (_isGameOver) yield break;

        Vector2 pos = GetRandomPosition();
        GameObject go = ObjectPoolManager.Instance.GetObject2D(_config.treePrefab, pos, transform);
        if (!go) yield break;

        TreeController tree = go.GetComponent<TreeController>();
        if (!tree) yield break;

        tree.Init(this);
        _aliveTrees.Add(tree);
    }

    private Vector2 GetRandomPosition()
    {
        float x = Random.Range(_config.minBounds.x, _config.maxBounds.x);
        float y = Random.Range(_config.minBounds.y, _config.maxBounds.y);
        return new Vector2(x, y);
    }

    private void Update()
    {
        if (_isGameOver) return;

        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            TryChop();
        }

        if (_isDragging && Input.GetMouseButton(0))
        {
            TryChop();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            _lastTouchedTree = null;
        }
    }

    private void TryChop()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(_cam.transform.position.z);
        
        Vector3 mousePos = _cam.ScreenToWorldPoint(mouseScreenPos);

        foreach (var tree in _aliveTrees)
        {
            if (!tree.IsAvailable) continue;
            if (tree == _lastTouchedTree) continue;
            

            if (Vector2.Distance(mousePos, tree.transform.position) > _config.chopRadius) continue;

            tree.FallDown();
            _lastTouchedTree = tree;
            return;
        }
    }

    public void OnTreeFell(TreeController tree)
    {
        _aliveTrees.Remove(tree);

        if (_isGameOver) return;

        _currentCutCount++;

        EventDispatcher.Dispatch(EventId.MiniGameProgressUpdate,
            new MiniGameProgressData { gameType = MiniGameType.CutTree, current = _currentCutCount, target = _config.targetTreesToCut });

        SoundManager.Instance.PlaySfx(_config.chopSoundId);

        if (_currentCutCount >= _config.targetTreesToCut)
        {
            WinGame();
            return;
        }

        RespawnTree();
    }

    private void WinGame()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        SoundManager.Instance.PlaySfx(_config.completeSoundId);
        Debug.Log($"[CutTree] WIN! Chopped {_currentCutCount} trees!");

        OnGameWin();
        CompleteGame();
    }

    protected virtual void OnGameWin()
    {
        Debug.Log("[CutTree] Victory! Implement UI or callback here.");
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Vector3 center = new(
            (_config.minBounds.x + _config.maxBounds.x) / 2f,
            (_config.minBounds.y + _config.maxBounds.y) / 2f,
            0
        );
        Vector3 size = new(
            _config.maxBounds.x - _config.minBounds.x,
            _config.maxBounds.y - _config.minBounds.y,
            0
        );
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
