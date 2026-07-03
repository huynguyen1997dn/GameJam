using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/CutTree Config", fileName = "CutTreeConfig")]
public class CutTreeConfig : ScriptableObject
{
    [TitleGroup("Spawn Area")]
    public Vector2 minBounds = new(-5f, -3f);
    public Vector2 maxBounds = new(5f, 3f);

    [TitleGroup("Tree Settings")]
    public GameObject treePrefab;
    public int initialTreeCount = 10;
    public int targetTreesToCut = 30;
    public float respawnDelay = 1.5f;

    [TitleGroup("Visual")]
    public Color treeColor = Color.green;
    public Vector2 treeSize = new(0.8f, 1.2f);

    [TitleGroup("Interaction")]
    public float chopRadius = 1.5f;

    [TitleGroup("Audio")]
    public string chopSoundId = "AUDIO_CHOP";
    public string completeSoundId = "AUDIO_COMPLETE";
}
