using DG.Tweening;
using UnityEngine;

public class TorPaintingPiece : MonoBehaviour
{
    private const int SortingPlaced = 1;
    private const int SortingIdle = 10;
    private const int SortingDragged = 100;

    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private PolygonCollider2D _collider;

    private TorPaintingManager _manager;
    private Vector3 _targetPosition;
    private bool _isPlaced;
    private bool _isDragging;
    private Vector3 _originalScale;

    public bool IsPlaced => _isPlaced;
    public int SortingOrder => _sprite.sortingOrder;
    public int GridRow { get; private set; }
    public int GridCol { get; private set; }

    public void Init(TorPaintingManager manager, Sprite sprite, Vector3 targetPos, Vector3 scatterPos, int row, int col)
    {
        _manager = manager;
        _sprite.sprite = sprite;
        _targetPosition = targetPos;
        GridRow = row;
        GridCol = col;
        _isPlaced = false;
        _isDragging = false;

        _originalScale = transform.localScale;
        _sprite.sortingOrder = SortingIdle;

        transform.localPosition = scatterPos;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
    }

    public void SetShape(Vector2[] path)
    {
        _collider.pathCount = 1;
        _collider.SetPath(0, path);
    }

    public void LockFinal()
    {
        _isPlaced = true;
        transform.localPosition = _targetPosition;
        transform.localScale = _originalScale;
        transform.localRotation = Quaternion.identity;
        _sprite.sortingOrder = SortingPlaced;
        _collider.enabled = false;
    }

    public bool HasVisiblePixelAt(Vector2 worldPos)
    {
        Sprite s = _sprite.sprite;
        if (s == null || !s.texture.isReadable) return true;

        Vector2 local = transform.InverseTransformPoint(worldPos);
        Vector2 px = local * s.pixelsPerUnit + s.pivot;
        int x = Mathf.FloorToInt(px.x);
        int y = Mathf.FloorToInt(px.y);
        if (x < 0 || y < 0 || x >= s.texture.width || y >= s.texture.height) return false;
        return s.texture.GetPixel(x, y).a > 0.1f;
    }

    public void BeginDrag(Vector3 pointerWorld)
    {
        if (_isPlaced) return;

        _isDragging = true;
        _sprite.sortingOrder = SortingDragged;

        transform.DOKill();
        transform.DOScale(_originalScale * 1.1f, 0.15f);
        transform.DOLocalRotate(Vector3.zero, 0.15f);

        MoveCenterTo(pointerWorld);
    }

    public void Drag(Vector3 pointerWorld)
    {
        if (!_isDragging || _isPlaced) return;

        MoveCenterTo(pointerWorld);

        float dist = Vector3.Distance(transform.localPosition, _targetPosition);
        _sprite.color = dist <= _manager.SnapDistance * 2f
            ? new Color(0.7f, 1f, 0.7f, 1f)
            : Color.white;
    }

    public void EndDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;

        transform.DOScale(_originalScale, 0.15f);
        _sprite.color = Color.white;

        float dist = Vector3.Distance(transform.localPosition, _targetPosition);
        if (dist <= _manager.SnapDistance)
        {
            SnapToTarget();
        }
        else
        {
            // Wrong spot: the piece stays where the player dropped it.
            _sprite.sortingOrder = SortingIdle;
        }
    }

    private void MoveCenterTo(Vector3 pointerWorld)
    {
        Vector3 delta = pointerWorld - _sprite.bounds.center;
        delta.z = 0;
        transform.position += delta;
    }

    private void SnapToTarget()
    {
        _isPlaced = true;
        _sprite.sortingOrder = SortingPlaced;
        _collider.enabled = false;

        transform.DOKill();
        transform.localScale = _originalScale;
        transform.DOLocalMove(_targetPosition, 0.2f).SetEase(Ease.OutBack);
        transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 3, 0.5f);

        _manager.OnPiecePlaced(this);
    }

    private void OnValidate()
    {
        if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
        if (!_collider) _collider = GetComponent<PolygonCollider2D>();
    }
}
