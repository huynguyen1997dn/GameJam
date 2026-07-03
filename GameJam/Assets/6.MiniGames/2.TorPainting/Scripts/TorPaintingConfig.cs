using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/TorPainting Config", fileName = "TorPaintingConfig")]
public class TorPaintingConfig : ScriptableObject
{
    [TitleGroup("Puzzle Settings")]
    [Tooltip("The image to slice into puzzle pieces")]
    public Sprite sourceSprite;
    [Tooltip("Total number of puzzle pieces")]
    public int pieceCount = 9;
    [Tooltip("Prefab for individual puzzle pieces")]
    public GameObject piecePrefab;

    [TitleGroup("Layout")]
    [Tooltip("Width of the full puzzle in world units")]
    public float puzzleSize = 6f;
    [Tooltip("Radius within which pieces are scattered")]
    public float scatterRadius = 4f;
    [Tooltip("Distance threshold for snapping a piece to its target")]
    public float snapDistance = 0.5f;

    [TitleGroup("Ghost Reference")]
    [Range(0f, 1f)]
    [Tooltip("Opacity of the ghost reference image in background")]
    public float ghostAlpha = 0.2f;

    [TitleGroup("Audio")]
    public string placeSoundId = "AUDIO_PLACE";
    public string completeSoundId = "AUDIO_COMPLETE";
}
