using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TheDarkForestManager : MiniGameBase
{
    [SerializeField] private TheDarkForestConfig _config;
    [SerializeField] private Camera _cam;

    private readonly List<TheDarkForestTree> _allTrees = new();
    private int _currentRow;
    private int _failCount;
    private int _totalRows;
    private bool _isGameOver;
    private Transform _rowsParent;
    private SpriteRenderer _darkOverlay;
    private GameObject _towerGO;
    private SpriteRenderer _towerSprite;

    public override MiniGameType MiniGameType => global::MiniGameType.TheDarkForest;
    public override string MiniGameId => "TheDarkForest";

    public override void Init()
    {
        if (!_cam) _cam = Camera.main;
        if (!ValidateConfig()) return;

        CreateRowsParent();
        CreateDarkOverlay();
        CreateTower();
        SpawnAllRows();
    }

    private bool ValidateConfig()
    {
        if (_config == null)
        {
            Debug.LogError("[TheDarkForest] Missing config!");
            return false;
        }
        if (_config.correctTreeSprite == null || _config.wrongTreeSprite == null)
        {
            Debug.LogError("[TheDarkForest] Missing tree sprites in config!");
            return false;
        }
        return true;
    }

    private void CreateRowsParent()
    {
        _rowsParent = new GameObject("Rows").transform;
        _rowsParent.SetParent(transform, false);
    }

    private void CreateDarkOverlay()
    {
        GameObject overlayGO = new GameObject("DarkOverlay");
        overlayGO.transform.SetParent(transform, false);

        _darkOverlay = overlayGO.AddComponent<SpriteRenderer>();
        _darkOverlay.sprite = CreateWhiteSprite();
        _darkOverlay.color = new Color(0, 0, 0, 0);
        _darkOverlay.sortingOrder = 1000;

        float camHeight = 2f * _cam.orthographicSize;
        float camWidth = camHeight * _cam.aspect;
        overlayGO.transform.localScale = new Vector3(camWidth, camHeight, 1);
    }

    private Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
    }

    private void CreateTower()
    {
        if (_config.towerSprite == null) return;

        _towerGO = new GameObject("Tower");
        _towerGO.transform.SetParent(transform, false);

        _towerSprite = _towerGO.AddComponent<SpriteRenderer>();
        _towerSprite.sprite = _config.towerSprite;
        _towerSprite.sortingOrder = 0;

        _towerGO.transform.localPosition = Vector3.zero;
        _towerGO.transform.localScale = Vector3.one * 0.01f;

        _towerGO.SetActive(false);
    }

    private void SpawnAllRows()
    {
        for (int r = 0; r < _totalRows; r++)
            SpawnRowAt(r);
    }

    private void SpawnRowAt(int rowIndex)
    {
        float totalWidth = (_config.treesPerRow - 1) * _config.spacingX;
        int correctIndex = Random.Range(0, _config.treesPerRow);

        for (int t = 0; t < _config.treesPerRow; t++)
        {
            bool isCorrect = (t == correctIndex);
            Sprite sprite = isCorrect ? _config.correctTreeSprite : _config.wrongTreeSprite;

            GameObject go = new GameObject($"Tree_R{rowIndex}_C{t}");
            go.transform.SetParent(_rowsParent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-rowIndex);

            var col = go.AddComponent<BoxCollider2D>();

            var tree = go.AddComponent<TheDarkForestTree>();

            float xOffset = Random.Range(-_config.randomOffsetX, _config.randomOffsetX);
            float yOffset = Random.Range(-_config.randomOffsetY, _config.randomOffsetY);

            float x = -totalWidth / 2f + t * _config.spacingX + xOffset;
            float y = rowIndex * _config.spacingY + yOffset;

            go.transform.localPosition = new Vector3(x, y, 0);

            tree.Init(this, isCorrect, sprite, rowIndex);
            tree.FitColliderToSprite();

            _allTrees.Add(tree);
        }
    }

    public void OnTreeClicked(TheDarkForestTree tree)
    {
        if (_isGameOver) return;
        if (tree.RowIndex != _currentRow) return;

        if (tree.IsCorrect)
        {
            SoundManager.Instance?.PlaySfx(_config.chopSoundId);
            ClearRow(_currentRow, tree);
            AdvanceToNextRow();
        }
        else
        {
            SoundManager.Instance?.PlaySfx(_config.failSoundId);
            ClearRow(_currentRow, tree);
            AdvanceToNextRow();
            OnWrongChop();
        }
    }

    private void ClearRow(int rowIndex, TheDarkForestTree clickedTree)
    {
        foreach (var t in _allTrees)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (t.RowIndex != rowIndex) continue;

            if (t == clickedTree)
            {
                if (t.IsCorrect)
                    t.ChopCorrect();
                else
                    t.ChopWrong();
            }
            else
            {
                t.Disappear();
            }
        }
    }

    private void AdvanceToNextRow()
    {
        _currentRow++;

        EventDispatcher.Dispatch(EventId.MiniGameProgressUpdate,
            new MiniGameProgressData { gameType = MiniGameType.TheDarkForest, current = _currentRow, target = _config.rows });

        if (_currentRow >= _totalRows)
        {
            ShowTower();
            return;
        }

        float moveAmount = -_config.spacingY;

        foreach (var tree in _allTrees)
        {
            if (tree == null || !tree.gameObject.activeInHierarchy) continue;
            if (tree.RowIndex < _currentRow) continue;

            Vector3 targetPos = tree.transform.localPosition + new Vector3(0, moveAmount, 0);
            tree.transform.DOLocalMove(targetPos, _config.rowTransitionDuration)
                .SetEase(Ease.OutCubic);
        }
    }

    private void ShowTower()
    {
        _isGameOver = true;

        foreach (var tree in _allTrees)
        {
            if (tree != null && tree.gameObject.activeInHierarchy)
            {
                tree.gameObject.SetActive(false);
            }
        }

        if (_towerGO == null)
        {
            CompleteGame();
            return;
        }

        _towerGO.SetActive(true);
        _towerGO.transform.localScale = Vector3.one * 0.01f;

        float camHeight = 2f * _cam.orthographicSize;
        float targetScale = (camHeight * 0.6f) / _config.towerSprite.bounds.size.y;

        _towerGO.transform.DOScale(targetScale, 1.2f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            SoundManager.Instance?.PlaySfx(_config.completeSoundId);
            CompleteGame();
        });
    }

    private void OnWrongChop()
    {
        if (_isGameOver) return;
        _failCount++;
        _totalRows++;

        SpawnRowAt(_totalRows - 1);

        float alpha = (float)_failCount / _config.maxFails;
        _darkOverlay.DOColor(new Color(0, 0, 0, alpha), 0.3f);
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (_darkOverlay != null) _darkOverlay.DOKill();
        if (_towerSprite != null) _towerSprite.DOKill();
    }

    private void OnValidate()
    {
        if (!_cam) _cam = Camera.main;
    }
}
