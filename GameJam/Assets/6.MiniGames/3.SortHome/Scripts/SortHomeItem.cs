using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SortHomeItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private BoxCollider2D _collider;

    private SortHomeManager _manager;
    private PolygonCollider2D _shape;
    private Vector3 _targetPosition;
    private Vector3 _scatterPosition;
    private Vector3 _dragOffset;
    private bool _isPlaced;
    private bool _isDragging;
    private int _originalSortOrder;
    private Vector3 _originalScale;

    public bool IsPlaced => _isPlaced;

    // Used by the manager to prefer the smallest item under the finger,
    // so a big sprite's outline can't blanket a small neighbor.
    public float TouchArea => _shape != null
        ? _shape.bounds.size.x * _shape.bounds.size.y
        : float.MaxValue;

    public void Init(SortHomeManager manager, Sprite sprite, Vector3 targetPos, Vector3 scatterPos)
    {
        _manager = manager;
        _sprite.sprite = sprite;
        _targetPosition = targetPos;
        _scatterPosition = scatterPos;
        _isPlaced = false;
        _isDragging = false;

        _originalSortOrder = _sprite.sortingOrder;
        _originalScale = transform.localScale;

        transform.localPosition = _scatterPosition;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
    }

    // Replaces the prefab's box collider with a polygon matching the sprite's
    // opaque outline, so transparent pixels never catch a touch.
    public void FitColliderToSprite()
    {
        if (_sprite == null || _sprite.sprite == null) return;

        if (_collider != null) Destroy(_collider);
        if (_shape != null) Destroy(_shape);

        _shape = gameObject.AddComponent<PolygonCollider2D>();

        Sprite sprite = _sprite.sprite;
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0) return; // AddComponent already generated a default outline

        var points = new List<Vector2>(64);
        _shape.pathCount = shapeCount;
        for (int i = 0; i < shapeCount; i++)
        {
            points.Clear();
            sprite.GetPhysicsShape(i, points);
            _shape.SetPath(i, points);
        }
    }

    public bool ContainsPoint(Vector2 worldPoint)
    {
        return _shape != null && _shape.enabled && _shape.OverlapPoint(worldPoint);
    }

    public void BeginDrag(Vector3 pointerWorldPos)
    {
        if (_isPlaced || _isDragging) return;

        _isDragging = true;
        _sprite.sortingOrder = 100;
        _dragOffset = transform.position - pointerWorldPos;

        transform.DOKill();
        transform.DOScale(_originalScale * 1.1f, 0.15f);
    }

    public void Drag(Vector3 pointerWorldPos)
    {
        if (!_isDragging || _isPlaced) return;

        Vector3 pos = pointerWorldPos + _dragOffset;
        pos.z = transform.position.z;
        transform.position = pos;

        float dist = Vector3.Distance(transform.localPosition, _targetPosition);
        _sprite.color = dist <= _manager.Config.snapDistance * 2f
            ? new Color(0.7f, 1f, 0.7f, 1f)
            : Color.white;
    }

    public void EndDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;

        _sprite.color = Color.white;

        float dist = Vector3.Distance(transform.localPosition, _targetPosition);

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
        if (_shape != null) _shape.enabled = false;

        transform.DOKill();
        transform.localScale = _originalScale;
        transform.DOLocalMove(_targetPosition, 0.2f).SetEase(Ease.OutBack);
        transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 3, 0.5f);

        _manager.OnItemPlaced(this);
    }

    private void ReturnToScatter()
    {
        transform.DOKill();
        // Stay on top while flying home; restore the sorting order on arrival.
        transform.DOScale(_originalScale, 0.2f);
        transform.DOLocalMove(_scatterPosition, 0.35f).SetEase(Ease.OutQuad)
            .OnComplete(() => _sprite.sortingOrder = _originalSortOrder);
        transform.DOLocalRotate(
            new Vector3(0, 0, Random.Range(-15f, 15f)), 0.35f).SetEase(Ease.OutQuad);
    }

    private void OnValidate()
    {
        if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
        if (!_collider) _collider = GetComponent<BoxCollider2D>();
    }
}
