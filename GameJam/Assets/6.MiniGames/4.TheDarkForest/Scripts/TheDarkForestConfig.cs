using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/TheDarkForest Config", fileName = "TheDarkForestConfig")]
public class TheDarkForestConfig : ScriptableObject
{
    [TitleGroup("Sprites")]
    public Sprite correctTreeSprite;
    public Sprite wrongTreeSprite;
    public Sprite towerSprite;

    [TitleGroup("Layout")]
    public int rows = 5;
    public int treesPerRow = 4;
    public float spacingX = 2.5f;
    public float spacingY = 3f;
    public float randomOffsetX = 0.5f;
    public float randomOffsetY = 0.3f;
    public float rowTransitionDuration = 0.5f;

    [TitleGroup("Game Rules")]
    public int maxFails = 3;

    [TitleGroup("Audio")]
    public string chopSoundId = "AUDIO_CHOP";
    public string failSoundId = "AUDIO_FAIL";
    public string completeSoundId = "AUDIO_COMPLETE";
}
