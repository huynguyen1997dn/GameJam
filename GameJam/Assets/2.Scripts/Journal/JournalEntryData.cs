using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoring data for a single journal page ("Day 3 — The Bridge Was Repaired").
/// Pure content: whether the entry is unlocked / read lives in JournalManager's
/// runtime state, so the same assets can be reused across sessions untouched.
/// </summary>
[CreateAssetMenu(fileName = "JournalEntry", menuName = "Journal/Journal Entry")]
public class JournalEntryData : ScriptableObject
{
    public string entryId;

    [Tooltip("Which day this page belongs to. Drives ordering and the D1/D2/D3 labels.")]
    public int dayNumber = 1;

    public string title;

    [TextArea(4, 12)]
    public string bodyText;

    [Tooltip("Optional. A placeholder frame is shown when empty.")]
    public Sprite illustration;

    [Tooltip("Clues that belong to this page. Each starts hidden until discovered.")]
    public List<string> clueIds = new List<string>();

    [Tooltip("Optional 'the world remembers' note shown under the clues.")]
    [TextArea(2, 4)]
    public string proofText;

    [Tooltip("Unlocked as soon as the journal initializes — use for the Day 1 page.")]
    public bool unlockedFromStart;
}
