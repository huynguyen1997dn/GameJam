using DG.Tweening;
using UnityEngine;

public class TorPaintingPiece : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private BoxCollider2D _collider;

    private TorPaintingManager _manager;
    private Vector3 _targetPosition;
    private Vector3 _scatterPosition;
    private Vector3 _dragOffset;
    private bool _isPlaced;
    private bool _isDragging;
    private int _originalSortOrder;
    private Vector3 _originalScale;

    public bool IsPlaced => _isPlaced;
    public int GridRow { get; private set; }
    public int GridCol { get; private set; }

    public void Init(TorPaintingManager manager, Sprite sprite, Vector3 targetPos, Vector3 scatterPos, int row, int col)
    {
        _manager = manager;
        _sprite.sprite = sprite;
        _targetPosition = targetPos;
        _scatterPosition = scatterPos;
        GridRow = row;
        GridCol = col;
        _isPlaced = false;
        _isDragging = false;

        _originalSortOrder = _sprite.sortingOrder;
        _originalScale = transform.localScale;

        transform.localPosition = _scatterPosition;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
    }

    public void FitColliderToSprite()
    {
        if (_sprite != null && _sprite.sprite != null && _collider != null)
        {
            _collider.size = _sprite.sprite.bounds.size;
        }
    }

    public void LockFinal()
    {
        _isPlaced = true;
        transform.localPosition = _targetPosition;
        transform.localScale = _originalScale;
        transform.localRotation = Quaternion.identity;
        _sprite.sortingOrder = _originalSortOrder;
    }

    private void OnMouseDown()
    {
        if (_isPlaced || _manager.IsGameOver) return;
        if (!enabled) return;

        _isDragging = true;
        _sprite.sortingOrder = 100;

        Vector3 mouseWorldPos = GetMouseWorldPos();
        _dragOffset = transform.position - mouseWorldPos;

        transform.DOScale(_originalScale * 1.1f, 0.15f);
    }

    private void OnMouseDrag()
    {
        if (!_isDragging || _isPlaced) return;

        Vector3 mouseWorldPos = GetMouseWorldPos();
        transform.position = mouseWorldPos + _dragOffset;

        float dist = Vector3.Distance(transform.position, _targetPosition);
        _sprite.color = dist <= _manager.Config.snapDistance * 2f
            ? new Color(0.7f, 1f, 0.7f, 1f)
            : Color.white;
    }

    private void OnMouseUp()
    {
        if (!_isDragging) return;
        _isDragging = false;

        transform.DOScale(_originalScale, 0.15f);
        _sprite.color = Color.white;

        float dist = Vector3.Distance(transform.position, _targetPosition);

        if (dist <= _manager.Config.snapDistance)
        {
            SnapToTarget();
        }
        else
        {
            ReturnToScatter();
        }
    }

    private void SnapToTarget()
    {
        _isPlaced = true;
        _sprite.sortingOrder = _originalSortOrder;

        transform.DOKill();
        transform.localScale = _originalScale;
        transform.DOLocalMove(_targetPosition, 0.2f).SetEase(Ease.OutBack);
        transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 3, 0.5f);

        _manager.OnPiecePlaced(this);
    }

    private void ReturnToScatter()
    {
        _sprite.sortingOrder = _originalSortOrder;

        transform.DOKill();
        transform.localScale = _originalScale;
        transform.DOLocalMove(_scatterPosition, 0.3f).SetEase(Ease.OutBounce);
        transform.DOLocalRotate(
            new Vector3(0, 0, Random.Range(-15f, 15f)), 0.3f).SetEase(Ease.OutQuad);
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        if (Camera.main != null)
        {
            mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
            return Camera.main.ScreenToWorldPoint(mousePos);
        }
        return Vector3.zero;
    }

    private void OnValidate()
    {
        if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
        if (!_collider) _collider = GetComponent<BoxCollider2D>();
    }
}
