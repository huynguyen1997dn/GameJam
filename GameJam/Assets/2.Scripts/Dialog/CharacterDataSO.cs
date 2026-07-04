using UnityEngine;

[CreateAssetMenu(menuName = "Dialog/Character Data", fileName = "CharacterData")]
public class CharacterDataSO : ScriptableObject
{
    public CharacterId characterId;
    public string displayName;
    public Sprite icon;
    public Color nameColor = Color.white;
}
