using DG.Tweening;
using UnityEngine;

public class TheDarkForestTree : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private BoxCollider2D _collider;

    private TheDarkForestManager _manager;
    private bool _isCorrect;
    private bool _isChopped;
    private int _rowIndex;

    public bool IsCorrect => _isCorrect;
    public bool IsChopped => _isChopped;
    public int RowIndex => _rowIndex;

    public void Init(TheDarkForestManager manager, bool isCorrect, Sprite sprite, int rowIndex)
    {
        _manager = manager;
        _isCorrect = isCorrect;
        _rowIndex = rowIndex;
        _isChopped = false;

        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
        if (_collider == null) _collider = GetComponent<BoxCollider2D>();

        _sprite.sprite = sprite;
        _sprite.color = Color.white;
        _sprite.sortingOrder = Mathf.RoundToInt(100 - rowIndex);

        FitColliderToSprite();
    }

    public void FitColliderToSprite()
    {
        if (_sprite != null && _sprite.sprite != null && _collider != null)
        {
            _collider.size = _sprite.sprite.bounds.size;
            _collider.offset = _sprite.sprite.bounds.center;
        }
    }

    public bool ContainsPoint(Vector2 worldPoint)
    {
        return !_isChopped && _collider != null && _collider.enabled
            && _collider.OverlapPoint(worldPoint);
    }

    public void ChopCorrect()
    {

        if (_isChopped) return;
        _isChopped = true;

        _collider.enabled = false;

        Sequence seq = DOTween.Sequence();
        float dir = Random.value > 0.5f ? -1f : 1f;
        seq.Join(transform.DORotate(new Vector3(0, 0, 90f * dir), 0.3f).SetEase(Ease.InBack));
        seq.Join(transform.DOLocalMoveY(transform.localPosition.y - 1.5f, 0.3f).SetEase(Ease.InQuad));
        seq.Join(_sprite.DOFade(0, 0.25f).SetDelay(0.1f));
        seq.OnComplete(() => gameObject.SetActive(false));
    }

    public void ChopWrong()
    {
        if (_isChopped) return;
        _isChopped = true;

        _collider.enabled = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOPunchPosition(new Vector3(0.3f, 0, 0), 0.3f, 6, 0.5f));
        seq.Join(_sprite.DOColor(Color.red, 0.1f).SetLoops(4, LoopType.Yoyo));
        seq.OnComplete(() =>
        {
            _sprite.color = Color.white;
            gameObject.SetActive(false);
        });
    }

    public void Disappear()
    {
        if (_isChopped) return;
        _isChopped = true;
        _collider.enabled = false;
        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
        if (!_collider) _collider = GetComponent<BoxCollider2D>();
    }
}
