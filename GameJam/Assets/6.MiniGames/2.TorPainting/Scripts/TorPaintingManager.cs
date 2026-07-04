using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TorPaintingManager : MiniGameBase
{
    [SerializeField] private TorPaintingConfig _config;
    [SerializeField] private Camera _cam;

    private readonly List<TorPaintingPiece> _pieces = new();
    private readonly List<Texture2D> _generatedTextures = new();
    private TorPaintingPiece _draggedPiece;
    private int _placedCount;
    private bool _isGameOver;
    private SpriteRenderer _ghostImage;
    private float _snapDistance;

    public TorPaintingConfig Config => _config;
    public bool IsGameOver => _isGameOver;
    public float SnapDistance => _snapDistance;

    public override MiniGameType MiniGameType => global::MiniGameType.TorPainting;
    public override string MiniGameId => "TorPainting";

    public override void Init()
    {
        if (!_cam) _cam = Camera.main;

        if (!ValidateConfig()) return;

        CreateBackground();
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
        if (!_config.sourceSprite.texture.isReadable)
        {
            Debug.LogError("[TorPainting] sourceSprite texture needs Read/Write enabled for torn-edge slicing!");
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

    private void Update()
    {
        if (_isGameOver || _cam == null) return;

        if (Input.GetMouseButtonDown(0))
            TryBeginDrag();

        if (_draggedPiece == null) return;

        if (Input.GetMouseButton(0))
            _draggedPiece.Drag(GetPointerWorldPos());

        if (Input.GetMouseButtonUp(0))
        {
            _draggedPiece.EndDrag();
            _draggedPiece = null;
        }
    }

    private void TryBeginDrag()
    {
        Vector3 pointer = GetPointerWorldPos();
        Collider2D[] hits = Physics2D.OverlapPointAll(pointer);

        TorPaintingPiece best = null;
        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent(out TorPaintingPiece piece)) continue;
            if (piece.IsPlaced || !piece.isActiveAndEnabled) continue;
            if (!piece.HasVisiblePixelAt(pointer)) continue;
            if (best == null || piece.SortingOrder > best.SortingOrder) best = piece;
        }

        if (best == null) return;

        _draggedPiece = best;
        best.BeginDrag(pointer);
    }

    private Vector3 GetPointerWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(_cam.transform.position.z);
        Vector3 world = _cam.ScreenToWorldPoint(mousePos);
        world.z = transform.position.z;
        return world;
    }

    private void CreateBackground()
    {
        if (_config.backgroundSprite == null) return;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(transform, false);

        SpriteRenderer sr = bgGO.AddComponent<SpriteRenderer>();
        sr.sprite = _config.backgroundSprite;
        sr.sortingOrder = -10;

        // Cover the whole camera view, centered on the camera.
        Vector2 spriteSize = _config.backgroundSprite.bounds.size;
        float scale;
        if (_cam != null && _cam.orthographic)
        {
            float viewH = _cam.orthographicSize * 2f;
            float viewW = viewH * _cam.aspect;
            scale = Mathf.Max(viewW / spriteSize.x, viewH / spriteSize.y);
        }
        else
        {
            scale = _config.puzzleSize / spriteSize.x;
        }
        bgGO.transform.localScale = new Vector3(scale, scale, 1);

        if (_cam != null)
        {
            Vector3 camPos = _cam.transform.position;
            bgGO.transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }
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
        int texW = (int)sourceRect.width;
        int texH = (int)sourceRect.height;
        int srcOffsetX = (int)sourceRect.x;
        int srcOffsetY = (int)sourceRect.y;
        int fullTexW = source.texture.width;
        Color32[] srcPixels = source.texture.GetPixels32();

        float pieceTexW = (float)texW / cols;
        float pieceTexH = (float)texH / rows;

        float spriteAspect = (float)texW / texH;
        float puzzleW = _config.puzzleSize;
        float puzzleH = _config.puzzleSize / spriteAspect;
        float pieceW = puzzleW / cols;
        float pieceH = puzzleH / rows;
        _snapDistance = _config.snapDistancePercent * Mathf.Min(pieceW, pieceH);

        float pieceScale = _config.puzzleSize / source.bounds.size.x;

        Vector3 puzzleOrigin = new Vector3(
            -puzzleW / 2f + pieceW / 2f,
            -puzzleH / 2f + pieceH / 2f,
            0);

        // Shared tear-line network: neighbors reuse the same jagged edge, so pieces interlock.
        float tearAmpPx = _config.tearAmplitude * Mathf.Min(pieceTexW, pieceTexH);
        Vector2[,] nodes = BuildJitteredNodes(cols, rows, pieceTexW, pieceTexH, texW, texH, tearAmpPx);

        List<Vector2>[,] vEdges = new List<Vector2>[cols + 1, rows];
        for (int c = 0; c <= cols; c++)
            for (int r = 0; r < rows; r++)
                vEdges[c, r] = (c == 0 || c == cols)
                    ? StraightLine(nodes[c, r], nodes[c, r + 1])
                    : JaggedLine(nodes[c, r], nodes[c, r + 1], tearAmpPx);

        List<Vector2>[,] hEdges = new List<Vector2>[cols, rows + 1];
        for (int c = 0; c < cols; c++)
            for (int r = 0; r <= rows; r++)
                hEdges[c, r] = (r == 0 || r == rows)
                    ? StraightLine(nodes[c, r], nodes[c + 1, r])
                    : JaggedLine(nodes[c, r], nodes[c + 1, r], tearAmpPx);

        int pieceIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                List<Vector2> polygon = BuildPiecePolygon(vEdges, hEdges, r, c);
                Vector2 cellCenterTex = new Vector2((c + 0.5f) * pieceTexW, (r + 0.5f) * pieceTexH);

                Sprite pieceSprite = CreateTornSprite(polygon, cellCenterTex, srcPixels,
                    fullTexW, srcOffsetX, srcOffsetY, texW, texH, source.pixelsPerUnit,
                    out Vector2[] colliderPath);

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
                    piece.SetShape(colliderPath);
                    _pieces.Add(piece);
                }
                else
                {
                    piece.Init(this, pieceSprite, targetPos, targetPos, r, c);
                    piece.SetShape(colliderPath);
                    piece.LockFinal();
                }

                pieceIndex++;
            }
        }
    }

    private static Vector2[,] BuildJitteredNodes(int cols, int rows, float pieceTexW, float pieceTexH,
        int texW, int texH, float tearAmpPx)
    {
        Vector2[,] nodes = new Vector2[cols + 1, rows + 1];
        for (int c = 0; c <= cols; c++)
        {
            for (int r = 0; r <= rows; r++)
            {
                Vector2 p = new Vector2(Mathf.Min(c * pieceTexW, texW), Mathf.Min(r * pieceTexH, texH));
                bool interior = c > 0 && c < cols && r > 0 && r < rows;
                if (interior)
                    p += Random.insideUnitCircle * (tearAmpPx * 0.6f);
                nodes[c, r] = p;
            }
        }
        return nodes;
    }

    private static List<Vector2> StraightLine(Vector2 a, Vector2 b)
    {
        return new List<Vector2> { a, b };
    }

    private static List<Vector2> JaggedLine(Vector2 a, Vector2 b, float amplitude)
    {
        float length = Vector2.Distance(a, b);
        int segments = Mathf.Max(6, Mathf.RoundToInt(length / 20f));
        Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized;
        float noisePhase = Random.Range(0f, 100f);

        List<Vector2> points = new List<Vector2>(segments + 1) { a };
        for (int i = 1; i < segments; i++)
        {
            float t = i / (float)segments;
            // Pin the endpoints so neighboring edges meet exactly at the grid nodes.
            float envelope = Mathf.Sin(t * Mathf.PI);
            float noise = (Mathf.PerlinNoise(noisePhase + t * 4f, 0.5f) - 0.5f) * 2f;
            float jitter = Random.Range(-0.35f, 0.35f);
            points.Add(Vector2.Lerp(a, b, t) + normal * ((noise + jitter) * amplitude * envelope));
        }
        points.Add(b);
        return points;
    }

    private static List<Vector2> BuildPiecePolygon(List<Vector2>[,] vEdges, List<Vector2>[,] hEdges, int r, int c)
    {
        List<Vector2> poly = new List<Vector2>();
        AppendEdge(poly, hEdges[c, r], false);      // bottom: left -> right
        AppendEdge(poly, vEdges[c + 1, r], false);  // right: bottom -> top
        AppendEdge(poly, hEdges[c, r + 1], true);   // top: right -> left
        AppendEdge(poly, vEdges[c, r], true);       // left: top -> bottom
        return poly;
    }

    private static void AppendEdge(List<Vector2> poly, List<Vector2> edge, bool reversed)
    {
        // Skip each edge's final point: it is the next edge's first point.
        int count = edge.Count;
        for (int i = 0; i < count - 1; i++)
            poly.Add(reversed ? edge[count - 1 - i] : edge[i]);
    }

    private Sprite CreateTornSprite(List<Vector2> polygon, Vector2 cellCenterTex, Color32[] srcPixels,
        int fullTexW, int srcOffsetX, int srcOffsetY, int texW, int texH, float pixelsPerUnit,
        out Vector2[] colliderPath)
    {
        Vector2 min = polygon[0];
        Vector2 max = polygon[0];
        foreach (Vector2 p in polygon)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        int bx = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, texW - 1);
        int by = Mathf.Clamp(Mathf.FloorToInt(min.y), 0, texH - 1);
        int bw = Mathf.Clamp(Mathf.CeilToInt(max.x), bx + 1, texW) - bx;
        int bh = Mathf.Clamp(Mathf.CeilToInt(max.y), by + 1, texH) - by;

        Color32[] pixels = new Color32[bw * bh];
        bool[] inside = new bool[bw * bh];

        // Scanline fill of the tear polygon, copying source pixels.
        int n = polygon.Count;
        List<float> crossings = new List<float>(8);
        for (int y = 0; y < bh; y++)
        {
            float py = by + y + 0.5f;
            crossings.Clear();
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = polygon[i];
                Vector2 p2 = polygon[(i + 1) % n];
                if ((p1.y <= py) == (p2.y <= py)) continue;
                crossings.Add(p1.x + (py - p1.y) / (p2.y - p1.y) * (p2.x - p1.x));
            }
            crossings.Sort();

            for (int k = 0; k + 1 < crossings.Count; k += 2)
            {
                int x0 = Mathf.Max(0, Mathf.CeilToInt(crossings[k] - 0.5f - bx));
                int x1 = Mathf.Min(bw - 1, Mathf.FloorToInt(crossings[k + 1] - 0.5f - bx));
                for (int x = x0; x <= x1; x++)
                {
                    int idx = y * bw + x;
                    inside[idx] = true;
                    pixels[idx] = srcPixels[(srcOffsetY + by + y) * fullTexW + srcOffsetX + bx + x];
                }
            }
        }

        ApplyFiberEdge(pixels, inside, bw, bh);

        Texture2D tex = new Texture2D(bw, bh, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(false, false);
        _generatedTextures.Add(tex);

        // Pivot at the cell center so target positions stay on the plain grid.
        Vector2 pivot = new Vector2((cellCenterTex.x - bx) / bw, (cellCenterTex.y - by) / bh);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, bw, bh), pivot, pixelsPerUnit,
            0, SpriteMeshType.FullRect);

        colliderPath = new Vector2[n];
        for (int i = 0; i < n; i++)
            colliderPath[i] = (polygon[i] - cellCenterTex) / pixelsPerUnit;

        return sprite;
    }

    private void ApplyFiberEdge(Color32[] pixels, bool[] inside, int bw, int bh)
    {
        float width = _config.tearWhiteWidth;
        if (width <= 0f) return;

        // Two-pass chamfer distance from each opaque pixel to the torn edge.
        const float INF = 1e9f;
        const float DIAG = 1.4142f;
        float[] dist = new float[bw * bh];
        for (int i = 0; i < dist.Length; i++)
            dist[i] = inside[i] ? INF : 0f;

        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                int i = y * bw + x;
                float d = dist[i];
                if (d == 0f) continue;
                if (x > 0) d = Mathf.Min(d, dist[i - 1] + 1f);
                if (y > 0) d = Mathf.Min(d, dist[i - bw] + 1f);
                if (x > 0 && y > 0) d = Mathf.Min(d, dist[i - bw - 1] + DIAG);
                if (x < bw - 1 && y > 0) d = Mathf.Min(d, dist[i - bw + 1] + DIAG);
                dist[i] = d;
            }
        }
        for (int y = bh - 1; y >= 0; y--)
        {
            for (int x = bw - 1; x >= 0; x--)
            {
                int i = y * bw + x;
                float d = dist[i];
                if (d == 0f) continue;
                if (x < bw - 1) d = Mathf.Min(d, dist[i + 1] + 1f);
                if (y < bh - 1) d = Mathf.Min(d, dist[i + bw] + 1f);
                if (x < bw - 1 && y < bh - 1) d = Mathf.Min(d, dist[i + bw + 1] + DIAG);
                if (x > 0 && y < bh - 1) d = Mathf.Min(d, dist[i + bw - 1] + DIAG);
                dist[i] = d;
            }
        }

        // Torn paper shows its white fiber backing along the tear.
        Color32 fiber = new Color32(255, 250, 240, 255);
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!inside[i] || dist[i] >= width) continue;
            float t = Mathf.Clamp01(dist[i] / width);
            byte alpha = pixels[i].a;
            pixels[i] = Color32.Lerp(fiber, pixels[i], t * t);
            pixels[i].a = alpha;
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

        foreach (var tex in _generatedTextures)
        {
            if (tex != null) Destroy(tex);
        }
        _generatedTextures.Clear();
    }

    private void OnValidate()
    {
        if (!_cam) _cam = Camera.main;
    }
}
