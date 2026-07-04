using System;
using System.Collections.Generic;

/// <summary>
/// Serializable snapshot of the journal's runtime state. The external save system
/// (outside this feature's scope) calls JournalManager.GetSaveData / LoadSaveData;
/// this type is JsonUtility-friendly on purpose.
/// </summary>
[Serializable]
public class JournalSaveData
{
    public List<string> unlockedEntryIds = new List<string>();
    public List<string> discoveredClueIds = new List<string>();
    public List<string> resolvedClueIds = new List<string>();
    public List<string> readEntryIds = new List<string>();
    public List<string> unreadEntryIds = new List<string>();
    public string lastSelectedEntryId;
}
