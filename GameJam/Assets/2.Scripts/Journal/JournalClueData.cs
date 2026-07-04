using UnityEngine;

/// <summary>
/// Authoring data for one clue line. Discovered / resolved state is runtime-only
/// and tracked by JournalManager, keyed by clueId.
/// </summary>
[CreateAssetMenu(fileName = "JournalClue", menuName = "Journal/Journal Clue")]
public class JournalClueData : ScriptableObject
{
    public string clueId;

    [Tooltip("The entry this clue appears under. Must match a JournalEntryData.entryId.")]
    public string entryId;

    [TextArea(1, 3)]
    public string text;
}
