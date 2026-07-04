using System;

[Serializable]
public struct DialogEntry
{
    public CharacterId characterId;
    [UnityEngine.TextArea(2, 4)]
    public string description;
}
