using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single lookup point for all journal content. JournalManager loads this from
/// Resources (Journal/JournalDatabase) when no reference is assigned, so external
/// systems can call UnlockEntry/AddClue with plain string ids and nothing else.
/// </summary>
[CreateAssetMenu(fileName = "JournalDatabase", menuName = "Journal/Journal Database")]
public class JournalDatabase : ScriptableObject
{
    public const string ResourcesPath = "Journal/JournalDatabase";

    [SerializeField] private List<JournalEntryData> entries = new List<JournalEntryData>();
    [SerializeField] private List<JournalClueData> clues = new List<JournalClueData>();

    private Dictionary<string, JournalEntryData> _entryById;
    private Dictionary<string, JournalClueData> _clueById;

    public IReadOnlyList<JournalEntryData> Entries => entries;
    public IReadOnlyList<JournalClueData> Clues => clues;

    public JournalEntryData GetEntry(string entryId)
    {
        if (string.IsNullOrEmpty(entryId)) return null;
        EnsureLookups();
        return _entryById.TryGetValue(entryId, out var entry) ? entry : null;
    }

    public JournalClueData GetClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId)) return null;
        EnsureLookups();
        return _clueById.TryGetValue(clueId, out var clue) ? clue : null;
    }

    /// <summary>All entries sorted by day number (authoring list order breaks ties).</summary>
    public List<JournalEntryData> GetEntriesOrderedByDay()
    {
        var ordered = new List<JournalEntryData>();
        foreach (var entry in entries)
            if (entry != null) ordered.Add(entry);
        // Stable sort so same-day entries keep their authored order.
        for (int i = 1; i < ordered.Count; i++)
        {
            var current = ordered[i];
            int j = i - 1;
            while (j >= 0 && ordered[j].dayNumber > current.dayNumber)
            {
                ordered[j + 1] = ordered[j];
                j--;
            }
            ordered[j + 1] = current;
        }
        return ordered;
    }

    private void EnsureLookups()
    {
        if (_entryById != null) return;

        _entryById = new Dictionary<string, JournalEntryData>();
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.entryId)) continue;
            if (!_entryById.TryAdd(entry.entryId, entry))
                Debug.LogError($"[JournalDatabase] Duplicate entryId '{entry.entryId}'.");
        }

        _clueById = new Dictionary<string, JournalClueData>();
        foreach (var clue in clues)
        {
            if (clue == null || string.IsNullOrEmpty(clue.clueId)) continue;
            if (!_clueById.TryAdd(clue.clueId, clue))
                Debug.LogError($"[JournalDatabase] Duplicate clueId '{clue.clueId}'.");
        }
    }

#if UNITY_EDITOR
    // Editor tooling (sample-data generator) uses these; lookups reset so play mode
    // right after an edit never sees a stale cache.
    public void EditorSetContent(List<JournalEntryData> newEntries, List<JournalClueData> newClues)
    {
        entries = newEntries;
        clues = newClues;
        _entryById = null;
        _clueById = null;
    }
#endif
}
