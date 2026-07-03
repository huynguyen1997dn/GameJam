using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SortHomeManager : MiniGameBase
{
    [SerializeField] private SortHomeConfig _config;
    [SerializeField] private Camera _cam;

    private readonly List<SortHomeItem> _items = new();
    private readonly List<GameObject> _slots = new();
    private int _placedCount;
    private bool _isGameOver;
    private SpriteRenderer _bgImage;

    public SortHomeConfig Config => _config;
    public bool IsGameOver => _isGameOver;

    public override MiniGameType MiniGameType => global::MiniGameType.SortHome;
    public override string MiniGameId => "SortHome";

    public override void Init()
    {
        if (!_cam) _cam = Camera.main;
        if (!ValidateConfig()) return;
        CreateBackground();
        CreatePlaceholders();
        SpawnItems();
    }

    private bool ValidateConfig()
    {
        if (_config == null)
        {
            Debug.LogError("[SortHome] Missing config!");
            return false;
        }
        if (_config.items == null || _config.items.Count == 0)
        {
            Debug.LogError("[SortHome] No items defined in config!");
            return false;
        }
        if (_config.itemPrefab == null)
        {
            Debug.LogError("[SortHome] Missing itemPrefab in config!");
            return false;
        }
        return true;
    }

    private void CreateBackground()
    {
        if (_config.sourceSprite == null) return;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(transform, false);

        _bgImage = bgGO.AddComponent<SpriteRenderer>();
        _bgImage.sprite = _config.sourceSprite;
        _bgImage.sortingOrder = -10;

        float scale = _config.puzzleSize / _config.sourceSprite.bounds.size.x;
        bgGO.transform.localScale = new Vector3(scale, scale, 1);
    }

    private void CreatePlaceholders()
    {
        for (int i = 0; i < _config.items.Count; i++)
        {
            var slot = _config.items[i];
            if (slot.sprite == null) continue;

            GameObject slotGO;
            if (_config.slotPrefab != null)
            {
                slotGO = Instantiate(_config.slotPrefab, transform);
            }
            else
            {
                slotGO = new GameObject($"Slot_{i}");
                slotGO.transform.SetParent(transform, false);

                var sr = slotGO.AddComponent<SpriteRenderer>();
                sr.sprite = slot.sprite;
                
                var color = Color.green;
                color.a = 0.7f;
                sr.color = color;

                sr.sortingOrder = -5;

                float s = _config.puzzleSize / slot.sprite.bounds.size.x * 0.5f;
                slotGO.transform.localScale = new Vector3(s, s, 1);
            }

            slotGO.transform.localPosition = slot.homePosition;
            _slots.Add(slotGO);
        }
    }

    private void SpawnItems()
    {
        for (int i = 0; i < _config.items.Count; i++)
        {
            var slot = _config.items[i];
            if (slot.sprite == null)
            {
                Debug.LogWarning($"[SortHome] Item {i} has no sprite, skipping.");
                continue;
            }

            GameObject go = Instantiate(_config.itemPrefab, transform);

            float itemScale = _config.puzzleSize / slot.sprite.bounds.size.x * 0.5f;
            go.transform.localScale = new Vector3(itemScale, itemScale, 1);

            Vector3 targetPos = slot.homePosition;

            Vector2 randomDir = Random.insideUnitCircle.normalized *
                Random.Range(_config.scatterRadius * 0.5f, _config.scatterRadius);
            Vector3 scatterPos = new Vector3(randomDir.x, randomDir.y, 0);

            SortHomeItem item = go.GetComponent<SortHomeItem>();
            if (item == null)
            {
                Debug.LogError("[SortHome] itemPrefab missing SortHomeItem component!");
                Destroy(go);
                continue;
            }

            go.name = $"Item_{i}";
            item.Init(this, slot.sprite, targetPos, scatterPos);
            item.FitColliderToSprite();
            _items.Add(item);
        }
    }

    public void OnItemPlaced(SortHomeItem item)
    {
        _placedCount++;

        if (!string.IsNullOrEmpty(_config.placeSoundId))
        {
            SoundManager.Instance?.PlaySfx(_config.placeSoundId);
        }

        if (_placedCount >= _config.items.Count)
        {
            CompletePuzzle();
        }
    }

    private void CompletePuzzle()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        if (!string.IsNullOrEmpty(_config.completeSoundId))
        {
            SoundManager.Instance?.PlaySfx(_config.completeSoundId);
        }

        CompleteGame();
    }

    private void OnDestroy()
    {
        foreach (var item in _items)
        {
            if (item != null && item.gameObject != null)
            {
                item.transform.DOKill();
            }
        }
        if (_bgImage != null)
        {
            _bgImage.DOKill();
        }
        transform.DOKill();
    }

    private void OnValidate()
    {
        if (!_cam) _cam = Camera.main;
    }
}
