using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TorPaintingManager : MiniGameBase
{
    [SerializeField] private TorPaintingConfig _config;
    [SerializeField] private Camera _cam;

    private readonly List<TorPaintingPiece> _pieces = new();
    private int _placedCount;
    private bool _isGameOver;
    private SpriteRenderer _ghostImage;

    public TorPaintingConfig Config => _config;
    public bool IsGameOver => _isGameOver;

    public override MiniGameType MiniGameType => global::MiniGameType.TorPainting;
    public override string MiniGameId => "TorPainting";

    public override void Init()
    {
        if (!_cam) _cam = Camera.main;

        if (!ValidateConfig()) return;

        CreateGhostReference();
        SliceAndSpawnPieces();
    }

    private bool ValidateConfig()
    {
        if (_config == null)
        {
            Debug.LogError("[TorPainting] Missing config!");
            return false;
        }
        if (_config.sourceSprite == null)
        {
            Debug.LogError("[TorPainting] Missing sourceSprite in config!");
            return false;
        }
        if (_config.piecePrefab == null)
        {
            Debug.LogError("[TorPainting] Missing piecePrefab in config!");
            return false;
        }
        if (_config.pieceCount < 1)
        {
            Debug.LogError("[TorPainting] pieceCount must be >= 1!");
            return false;
        }
        return true;
    }

    private void CreateGhostReference()
    {
        GameObject ghostGO = new GameObject("GhostReference");
        ghostGO.transform.SetParent(transform, false);

        _ghostImage = ghostGO.AddComponent<SpriteRenderer>();
        _ghostImage.sprite = _config.sourceSprite;
        _ghostImage.color = new Color(1, 1, 1, _config.ghostAlpha);
        _ghostImage.sortingOrder = -1;

        float scale = _config.puzzleSize / _config.sourceSprite.bounds.size.x;
        ghostGO.transform.localScale = new Vector3(scale, scale, 1);
    }

    private void SliceAndSpawnPieces()
    {
        Sprite source = _config.sourceSprite;
        int totalPieces = _config.pieceCount;

        int cols = Mathf.CeilToInt(Mathf.Sqrt(totalPieces));
        int rows = Mathf.CeilToInt((float)totalPieces / cols);

        while ((rows - 1) * cols >= totalPieces && rows > 1)
            rows--;
        while (rows * (cols - 1) >= totalPieces && cols > 1)
            cols--;

        Rect sourceRect = source.rect;
        float texW = sourceRect.width;
        float texH = sourceRect.height;
        float pieceTexW = texW / cols;
        float pieceTexH = texH / rows;

        float spriteAspect = texW / texH;
        float puzzleW = _config.puzzleSize;
        float puzzleH = _config.puzzleSize / spriteAspect;
        float pieceW = puzzleW / cols;
        float pieceH = puzzleH / rows;

        float pieceScale = _config.puzzleSize / source.bounds.size.x;

        Vector3 puzzleOrigin = new Vector3(
            -puzzleW / 2f + pieceW / 2f,
            -puzzleH / 2f + pieceH / 2f,
            0);

        int pieceIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float srcX = sourceRect.x + c * pieceTexW;
                float srcY = sourceRect.y + r * pieceTexH;

                Sprite pieceSprite = Sprite.Create(
                    source.texture,
                    new Rect(srcX, srcY, pieceTexW, pieceTexH),
                    new Vector2(0.5f, 0.5f),
                    source.pixelsPerUnit);

                Vector3 targetPos = puzzleOrigin + new Vector3(c * pieceW, r * pieceH, 0);

                GameObject go = Instantiate(_config.piecePrefab, transform);
                go.transform.localScale = new Vector3(pieceScale, pieceScale, 1);

                TorPaintingPiece piece = go.GetComponent<TorPaintingPiece>();
                if (piece == null)
                {
                    Debug.LogError("[TorPainting] piecePrefab missing TorPaintingPiece!");
                    Destroy(go);
                    continue;
                }

                bool isActive = pieceIndex < totalPieces;
                go.name = isActive ? $"Piece_{r}_{c}" : $"PrePlaced_{r}_{c}";

                if (isActive)
                {
                    Vector2 randomDir = Random.insideUnitCircle.normalized *
                        Random.Range(_config.scatterRadius * 0.5f, _config.scatterRadius);
                    Vector3 scatterPos = new Vector3(randomDir.x, randomDir.y, 0);

                    piece.Init(this, pieceSprite, targetPos, scatterPos, r, c);
                    piece.FitColliderToSprite();
                    _pieces.Add(piece);
                }
                else
                {
                    piece.Init(this, pieceSprite, targetPos, targetPos, r, c);
                    piece.FitColliderToSprite();
                    piece.LockFinal();
                }

                pieceIndex++;
            }
        }
    }

    public void OnPiecePlaced(TorPaintingPiece piece)
    {
        _placedCount++;

        EventDispatcher.Dispatch(EventId.MiniGameProgressUpdate,
            new MiniGameProgressData { gameType = MiniGameType.TorPainting, current = _placedCount, target = _config.pieceCount });

        if (!string.IsNullOrEmpty(_config.placeSoundId))
        {
            SoundManager.Instance.PlaySfx(_config.placeSoundId);
        }

        if (_placedCount >= _config.pieceCount)
        {
            CompletePuzzle();
        }
    }

    private void CompletePuzzle()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        if (_ghostImage != null)
        {
            _ghostImage.DOFade(0, 0.3f);
        }

        if (!string.IsNullOrEmpty(_config.completeSoundId))
        {
            SoundManager.Instance.PlaySfx(_config.completeSoundId);
        }

        CompleteGame();
    }

    private void OnDestroy()
    {
        foreach (var piece in _pieces)
        {
            if (piece != null && piece.gameObject != null)
            {
                piece.transform.DOKill();
            }
        }
        if (_ghostImage != null)
        {
            _ghostImage.DOKill();
        }
        transform.DOKill();
    }

    private void OnValidate()
    {
        if (!_cam) _cam = Camera.main;
    }
}
