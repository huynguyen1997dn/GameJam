using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/SortHome Config", fileName = "SortHomeConfig")]
public class SortHomeConfig : ScriptableObject
{
    [System.Serializable]
    public class ItemSlot
    {
        [Tooltip("Sprite for this item")]
        public Sprite sprite;
        [Tooltip("Target home position on the source sprite at scale 1 - scaled with the background at runtime")]
        public Vector2 homePosition;
        [Tooltip("Scale multiplier relative to the background scale. 0 = same scale as background (default)")]
        public float scale = 0f;
    }

    [TitleGroup("Item Settings")]
    [Tooltip("Background map sprite - shows where items belong (full alpha)")]
    public Sprite sourceSprite;
    [Tooltip("List of items with their sprites and home positions")]
    public List<ItemSlot> items = new();
    [Tooltip("Prefab for draggable items")]
    public GameObject itemPrefab;
    [Tooltip("Optional prefab for home slot placeholders (rendered below items)")]
    public GameObject slotPrefab;

    [TitleGroup("Layout")]
    [Tooltip("Width of the full puzzle area in world units")]
    public float puzzleSize = 6f;
    [Tooltip("Radius within which items are scattered")]
    public float scatterRadius = 4f;
    [Tooltip("Distance threshold for snapping an item to its target")]
    public float snapDistance = 0.5f;

    [TitleGroup("Background")]
    [Tooltip("Backdrop rendered in world space behind the map, scaled to fill the camera (optional)")]
    [UnityEngine.Serialization.FormerlySerializedAs("viewBackground")]
    public Sprite backgroundSprite;

    [TitleGroup("Audio")]
    public string placeSoundId = "AUDIO_PLACE";
    public string completeSoundId = "AUDIO_COMPLETE";
}
