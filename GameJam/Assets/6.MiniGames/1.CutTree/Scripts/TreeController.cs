using DG.Tweening;
using UnityEngine;

public class TreeController : MonoBehaviour, IPooledObject
{
    [SerializeField] private SpriteRenderer _sprite;

    private CutTreeManager _manager;
    private Vector3 _originalPos;
    private bool _isFalling;

    public bool IsAvailable => !_isFalling && gameObject.activeInHierarchy;

    public void Init(CutTreeManager manager)
    {
        _manager = manager;
    }

    public void OnObjectSpawn()
    {
        _originalPos = transform.localPosition;
        _isFalling = false;

        if (_sprite)
        {
            _sprite.color = Color.white;
            _sprite.sortingOrder = 0;
            _sprite.DOKill();
        }


        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    public void FallDown()
    {
        if (_isFalling) return;
        _isFalling = true;


        float randDir = Random.value > 0.5f ? -1f : 1f;

        Sequence fallSeq = DOTween.Sequence();
        fallSeq.Join(transform.DORotate(new Vector3(0, 0, 90f * randDir), 0.3f).SetEase(Ease.InBack));
        fallSeq.Join(transform.DOLocalMoveY(_originalPos.y - 1.5f, 0.3f).SetEase(Ease.InQuad));
        fallSeq.Join(_sprite.DOFade(0, 0.25f).SetDelay(0.1f));

        fallSeq.OnComplete(() =>
        {
            _manager?.OnTreeFell(this);
            ObjectPoolManager.Instance.ReturnObject(gameObject);
        });
    }

    private void OnValidate()
    {
        if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
    }
}
