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
    private SortHomeItem _draggedItem;
    private float _mapScale = 1f;

    public SortHomeConfig Config => _config;
    public bool IsGameOver => _isGameOver;

    public override MiniGameType MiniGameType => global::MiniGameType.SortHome;
    public override string MiniGameId => "SortHome";

    public override void Init()
    {
        if (!_cam) _cam = Camera.main;
        if (!ValidateConfig()) return;

        // Items are authored at the same pixel density as the source sprite,
        // so everything shares the background's scale factor.
        _mapScale = _config.sourceSprite != null
            ? _config.puzzleSize / _config.sourceSprite.bounds.size.x
            : 1f;

        CreateBackdrop();
        CreateBackground();
        CreatePlaceholders();
        SpawnItems();
    }

    private void Update()
    {
        if (_isGameOver) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 pointerPos = GetPointerWorldPos();
            _draggedItem = PickItemAt(pointerPos);
            _draggedItem?.BeginDrag(pointerPos);
        }
        else if (_draggedItem != null)
        {
            if (Input.GetMouseButton(0))
            {
                _draggedItem.Drag(GetPointerWorldPos());
            }

            if (Input.GetMouseButtonUp(0))
            {
                _draggedItem.EndDrag();
                _draggedItem = null;
            }
        }
    }

    // Picks the smallest unplaced item under the pointer, so overlapping
    // big sprites can't steal touches aimed at small ones.
    private SortHomeItem PickItemAt(Vector2 worldPos)
    {
        Physics2D.SyncTransforms();

        SortHomeItem best = null;
        float bestArea = float.MaxValue;
        foreach (var item in _items)
        {
            if (item == null || item.IsPlaced || !item.isActiveAndEnabled) continue;
            if (!item.ContainsPoint(worldPos)) continue;

            float area = item.TouchArea;
            if (area < bestArea)
            {
                best = item;
                bestArea = area;
            }
        }
        return best;
    }

    private Vector3 GetPointerWorldPos()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(_cam.transform.position.z - transform.position.z);
        Vector3 worldPos = _cam.ScreenToWorldPoint(screenPos);
        worldPos.z = transform.position.z;
        return worldPos;
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

    private void CreateBackdrop()
    {
        if (_config.backgroundSprite == null) return;

        GameObject bgGO = new GameObject("Backdrop");
        bgGO.transform.SetParent(transform, false);

        SpriteRenderer sr = bgGO.AddComponent<SpriteRenderer>();
        sr.sprite = _config.backgroundSprite;
        sr.sortingOrder = -20;

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

    private void CreateBackground()
    {
        if (_config.sourceSprite == null) return;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(transform, false);

        _bgImage = bgGO.AddComponent<SpriteRenderer>();
        _bgImage.sprite = _config.sourceSprite;
        _bgImage.sortingOrder = -10;

        bgGO.transform.localScale = new Vector3(_mapScale, _mapScale, 1);
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
                float s = GetSlotScale(slot);
                slotGO.transform.localScale = new Vector3(s, s, 1);
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

                float s = GetSlotScale(slot);
                slotGO.transform.localScale = new Vector3(s, s, 1);
            }

            slotGO.transform.localPosition = slot.homePosition * _mapScale;
            _slots.Add(slotGO);
        }
    }

    // Items scale with the background so their authored size relative to the
    // source sprite is preserved. Per-slot scale acts as a multiplier on top.
    private float GetSlotScale(SortHomeConfig.ItemSlot slot)
    {
        return slot.scale > 0f ? _mapScale * slot.scale : _mapScale;
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

            float itemScale = GetSlotScale(slot);
            go.transform.localScale = new Vector3(itemScale, itemScale, 1);

            // Authored on the scale-1 source sprite; scaled with the background.
            Vector3 targetPos = slot.homePosition * _mapScale;

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

        EventDispatcher.Dispatch(EventId.MiniGameProgressUpdate,
            new MiniGameProgressData { gameType = MiniGameType.SortHome, current = _placedCount, target = _config.items.Count });

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
