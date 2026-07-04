using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller and session state for the Journal ("People forget. The world remembers.").
/// Owns which entries are unlocked / read, which clues are discovered / resolved, the
/// current selection and the unread indicator state. Pure logic — JournalUIView renders
/// it, JournalMapButton opens it, external systems feed it via UnlockEntry / AddClue.
/// While open it holds MapInputLock so nodes and the camera freeze under the overlay;
/// the overlay's dim image blocks the UI buttons behind it.
/// </summary>
public class JournalManager : Singleton<JournalManager>
{
    [Tooltip("Optional. Falls back to Resources.Load(\"Journal/JournalDatabase\") when empty.")]
    [SerializeField] private JournalDatabase database;

    [Tooltip("If on, advancing to a new day auto-writes the pages of every FINISHED day " +
             "(dayNumber < the new day): reaching Day 2 unlocks the D1 entry, never D2 itself. " +
             "If off, a day's page is written the moment its objective completes (when the " +
             "Advance button appears), while still on that day. " +
             "Pair with a dayNumber-0 prologue entry marked unlockedFromStart.")]
    [SerializeField] private bool autoUnlockEntriesByDay = true;

    private readonly HashSet<string> _unlockedEntryIds = new HashSet<string>();
    private readonly HashSet<string> _discoveredClueIds = new HashSet<string>();
    private readonly HashSet<string> _resolvedClueIds = new HashSet<string>();
    private readonly HashSet<string> _readEntryIds = new HashSet<string>();
    private readonly HashSet<string> _unreadEntryIds = new HashSet<string>();
    // AddProofNote overrides at runtime without touching the authored assets.
    private readonly Dictionary<string, string> _runtimeProofNotes = new Dictionary<string, string>();

    private bool _initialized;

    public bool IsOpen { get; private set; }
    public string SelectedEntryId { get; private set; }

    public event Action OnJournalOpened;
    public event Action OnJournalClosed;
    public event Action<string> OnEntrySelected;          // entryId (may be null for empty state)
    public event Action<string> OnEntryUnlocked;          // entryId
    public event Action<string, string> OnClueAdded;      // (entryId, clueId)
    public event Action<bool> OnUnreadStateChanged;       // any unread content left?
    public event Action OnContentChanged;                 // visible data changed → view refreshes

    public JournalDatabase Database
    {
        get
        {
            InitializeIfNeeded();
            return database;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        var dm = DayManager.Instance;
        if (dm != null)
        {
            dm.OnDayChanged += HandleDayChanged;
            dm.OnObjectiveStateChanged += HandleObjectiveStateChanged;
        }
    }

    private void OnDisable()
    {
        var dm = DayManager.Instance;
        if (dm != null)
        {
            dm.OnDayChanged -= HandleDayChanged;
            dm.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
        }

        // Never leave the map frozen behind — same guarantee the build/transition code makes.
        if (IsOpen)
        {
            IsOpen = false;
            MapInputLock.Unlock();
        }
    }

    private void InitializeIfNeeded()
    {
        if (_initialized) return;
        _initialized = true;

        if (database == null)
            database = Resources.Load<JournalDatabase>(JournalDatabase.ResourcesPath);
        if (database == null)
        {
            Debug.LogError("[JournalManager] No JournalDatabase assigned and none found at " +
                           $"Resources/{JournalDatabase.ResourcesPath}. The journal will stay empty.");
            return;
        }

        // Start-unlocked pages (e.g. Day 1) arrive already read — no unread ping on boot.
        foreach (var entry in database.Entries)
        {
            if (entry == null || !entry.unlockedFromStart || string.IsNullOrEmpty(entry.entryId)) continue;
            _unlockedEntryIds.Add(entry.entryId);
            _readEntryIds.Add(entry.entryId);
        }
    }

    // ---------------------- OPEN / CLOSE ----------------------

    public void OpenJournal(string preferredEntryId = null)
    {
        InitializeIfNeeded();
        if (IsOpen) return;

        // Same gate as DayHUD's advance button: never open over a scripted moment
        // (build animation, day transition) or mid-hop, where the pending arrival
        // callback could pop a room open underneath the overlay.
        if (MapInputLock.IsLocked) return;
        var mover = CharacterMapMover.Instance;
        if (mover != null && mover.IsMoving) return;

        IsOpen = true;
        MapInputLock.Lock();

        string target = ResolveDefaultEntryId(preferredEntryId);
        SetSelectedEntry(target);

        OnJournalOpened?.Invoke();
    }

    public void CloseJournal()
    {
        if (!IsOpen) return;
        IsOpen = false;
        MapInputLock.Unlock();
        OnJournalClosed?.Invoke();
    }

    private string ResolveDefaultEntryId(string preferredEntryId)
    {
        if (!string.IsNullOrEmpty(preferredEntryId) && IsEntryUnlocked(preferredEntryId))
            return preferredEntryId;
        return GetLatestUnlockedEntryId();
    }

    // ---------------------- SELECTION / NAVIGATION ----------------------

    public bool IsEntryUnlocked(string entryId) =>
        !string.IsNullOrEmpty(entryId) && _unlockedEntryIds.Contains(entryId);

    public bool IsEntryUnread(string entryId) =>
        !string.IsNullOrEmpty(entryId) && _unreadEntryIds.Contains(entryId);

    public bool HasUnreadContent() => _unreadEntryIds.Count > 0;

    /// <summary>Unlocked entry with the highest day number, or null when none exist.</summary>
    public string GetLatestUnlockedEntryId()
    {
        InitializeIfNeeded();
        if (database == null) return null;

        var ordered = database.GetEntriesOrderedByDay();
        for (int i = ordered.Count - 1; i >= 0; i--)
            if (IsEntryUnlocked(ordered[i].entryId)) return ordered[i].entryId;
        return null;
    }

    public void SelectEntry(string entryId)
    {
        if (!IsEntryUnlocked(entryId))
        {
            // Locked page tapped in the day list — the view shows "not written yet".
            Debug.Log($"[JournalManager] Entry '{entryId}' is locked or unknown.");
            return;
        }
        SetSelectedEntry(entryId);
    }

    public void SelectPreviousUnlockedEntry() => SelectUnlockedNeighbor(-1);
    public void SelectNextUnlockedEntry() => SelectUnlockedNeighbor(+1);

    public bool HasPreviousUnlockedEntry() => FindUnlockedNeighbor(-1) != null;
    public bool HasNextUnlockedEntry() => FindUnlockedNeighbor(+1) != null;

    private void SelectUnlockedNeighbor(int direction)
    {
        var neighborId = FindUnlockedNeighbor(direction);
        if (neighborId != null) SetSelectedEntry(neighborId);
    }

    private string FindUnlockedNeighbor(int direction)
    {
        InitializeIfNeeded();
        if (database == null || string.IsNullOrEmpty(SelectedEntryId)) return null;

        var ordered = database.GetEntriesOrderedByDay();
        int index = ordered.FindIndex(e => e.entryId == SelectedEntryId);
        if (index < 0) return null;

        for (int i = index + direction; i >= 0 && i < ordered.Count; i += direction)
            if (IsEntryUnlocked(ordered[i].entryId)) return ordered[i].entryId;
        return null;
    }

    private void SetSelectedEntry(string entryId)
    {
        SelectedEntryId = entryId;
        if (!string.IsNullOrEmpty(entryId)) MarkEntryRead(entryId);
        OnEntrySelected?.Invoke(entryId);
    }

    private void MarkEntryRead(string entryId)
    {
        _readEntryIds.Add(entryId);
        if (_unreadEntryIds.Remove(entryId))
            OnUnreadStateChanged?.Invoke(HasUnreadContent());
    }

    // ---------------------- EXTERNAL INPUTS ----------------------

    /// <summary>Called by quest/puzzle/dialogue systems when a page gets written.</summary>
    public void UnlockEntry(string entryId)
    {
        InitializeIfNeeded();
        if (string.IsNullOrEmpty(entryId)) return;

        if (database == null || database.GetEntry(entryId) == null)
        {
            Debug.LogError($"[JournalManager] UnlockEntry: no entry data for '{entryId}'.");
            return;
        }
        if (!_unlockedEntryIds.Add(entryId)) return; // already unlocked

        _unreadEntryIds.Add(entryId);
        OnEntryUnlocked?.Invoke(entryId);
        OnUnreadStateChanged?.Invoke(true);
        OnContentChanged?.Invoke(); // day list / nav arrows may change while open
    }

    /// <summary>Marks a clue discovered. The owning entry comes from the clue asset.</summary>
    public void SetClueDiscovered(string clueId)
    {
        InitializeIfNeeded();
        var clue = database != null ? database.GetClue(clueId) : null;
        if (clue == null)
        {
            Debug.LogError($"[JournalManager] SetClueDiscovered: no clue data for '{clueId}'.");
            return;
        }
        AddClue(clue.entryId, clueId);
    }

    /// <summary>Spec-shaped overload; entryId must match the clue asset's entryId.</summary>
    public void AddClue(string entryId, string clueId)
    {
        InitializeIfNeeded();
        var clue = database != null ? database.GetClue(clueId) : null;
        if (clue == null)
        {
            Debug.LogError($"[JournalManager] AddClue: no clue data for '{clueId}'.");
            return;
        }
        if (!string.IsNullOrEmpty(entryId) && clue.entryId != entryId)
            Debug.LogWarning($"[JournalManager] AddClue: clue '{clueId}' belongs to " +
                             $"'{clue.entryId}', not '{entryId}'. Using the clue asset's entry.");

        if (!_discoveredClueIds.Add(clueId)) return; // already discovered

        bool viewingThatEntry = IsOpen && SelectedEntryId == clue.entryId;
        // Unread ping only for content the player can actually go read: the entry must
        // be unlocked, and not the page currently on screen (that one just refreshes).
        if (!viewingThatEntry && IsEntryUnlocked(clue.entryId))
        {
            _unreadEntryIds.Add(clue.entryId);
            _readEntryIds.Remove(clue.entryId);
            OnUnreadStateChanged?.Invoke(true);
        }

        OnClueAdded?.Invoke(clue.entryId, clueId);
        OnContentChanged?.Invoke();
    }

    public void MarkClueResolved(string clueId)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        if (!_resolvedClueIds.Add(clueId)) return;
        OnContentChanged?.Invoke();
    }

    /// <summary>World-state hook: replaces the entry's proof note for this session.</summary>
    public void AddProofNote(string entryId, string proofText)
    {
        if (string.IsNullOrEmpty(entryId)) return;
        _runtimeProofNotes[entryId] = proofText;
        OnContentChanged?.Invoke();
    }

    public bool IsClueDiscovered(string clueId) => _discoveredClueIds.Contains(clueId);
    public bool IsClueResolved(string clueId) => _resolvedClueIds.Contains(clueId);

    /// <summary>Runtime proof note if one was pushed, else the authored one.</summary>
    public string GetProofText(JournalEntryData entry)
    {
        if (entry == null) return null;
        return _runtimeProofNotes.TryGetValue(entry.entryId, out var runtimeNote)
            ? runtimeNote
            : entry.proofText;
    }

    // ---------------------- DAY SYSTEM ----------------------

    private void HandleDayChanged(int newDay)
    {
        if (!autoUnlockEntriesByDay || database == null) return;
        // Only finished days are written down — the new day's page stays locked
        // until the player has actually lived it.
        foreach (var entry in database.Entries)
        {
            if (entry == null || entry.dayNumber >= newDay) continue;
            UnlockEntry(entry.entryId);
        }
    }

    // Flag-off mode: the day's page is written the moment its objective completes —
    // the same signal that reveals DayHUD's Advance button. IsCurrentObjectiveComplete,
    // not CanAdvance, so the final day (no next day, button never shows) still writes.
    private void HandleObjectiveStateChanged()
    {
        if (autoUnlockEntriesByDay || database == null) return;

        var dm = DayManager.Instance;
        if (dm == null || !dm.IsCurrentObjectiveComplete) return;

        // <= CurrentDay is a self-healing catch-up: earlier days were necessarily
        // completed to get here, so any unlock moment missed while disabled heals now.
        foreach (var entry in database.Entries)
        {
            if (entry == null || entry.unlockedFromStart) continue;
            if (entry.dayNumber > dm.CurrentDay) continue;
            UnlockEntry(entry.entryId);
        }
    }

    // ---------------------- SAVE / LOAD ----------------------
    // The external save system (out of scope) owns when these run, e.g.:
    //   SaveSystem.OnSave -> journalSaveData = JournalManager.Instance.GetSaveData()
    //   SaveSystem.OnLoad -> JournalManager.Instance.LoadSaveData(journalSaveData)

    public JournalSaveData GetSaveData()
    {
        InitializeIfNeeded();
        return new JournalSaveData
        {
            unlockedEntryIds = new List<string>(_unlockedEntryIds),
            discoveredClueIds = new List<string>(_discoveredClueIds),
            resolvedClueIds = new List<string>(_resolvedClueIds),
            readEntryIds = new List<string>(_readEntryIds),
            unreadEntryIds = new List<string>(_unreadEntryIds),
            lastSelectedEntryId = SelectedEntryId,
        };
    }

    public void LoadSaveData(JournalSaveData saveData)
    {
        InitializeIfNeeded();
        if (saveData == null) return;

        _unlockedEntryIds.Clear();
        _discoveredClueIds.Clear();
        _resolvedClueIds.Clear();
        _readEntryIds.Clear();
        _unreadEntryIds.Clear();
        _runtimeProofNotes.Clear();

        if (saveData.unlockedEntryIds != null) _unlockedEntryIds.UnionWith(saveData.unlockedEntryIds);
        if (saveData.discoveredClueIds != null) _discoveredClueIds.UnionWith(saveData.discoveredClueIds);
        if (saveData.resolvedClueIds != null) _resolvedClueIds.UnionWith(saveData.resolvedClueIds);
        if (saveData.readEntryIds != null) _readEntryIds.UnionWith(saveData.readEntryIds);
        if (saveData.unreadEntryIds != null) _unreadEntryIds.UnionWith(saveData.unreadEntryIds);

        SelectedEntryId = IsEntryUnlocked(saveData.lastSelectedEntryId)
            ? saveData.lastSelectedEntryId
            : GetLatestUnlockedEntryId();

        OnUnreadStateChanged?.Invoke(HasUnreadContent());
        OnContentChanged?.Invoke();
    }

    // ---------------------- CHEATS (JournalDebugCheats) ----------------------

    /// <summary>Unlocks the first still-locked entry in day order. Returns its id or null.</summary>
    public string CheatUnlockNextEntry()
    {
        InitializeIfNeeded();
        if (database == null) return null;

        foreach (var entry in database.GetEntriesOrderedByDay())
        {
            if (IsEntryUnlocked(entry.entryId)) continue;
            UnlockEntry(entry.entryId);
            return entry.entryId;
        }
        return null;
    }

    /// <summary>Discovers the first hidden clue of an unlocked entry. Returns its id or null.</summary>
    public string CheatDiscoverNextClue()
    {
        InitializeIfNeeded();
        if (database == null) return null;

        foreach (var entry in database.GetEntriesOrderedByDay())
        {
            if (!IsEntryUnlocked(entry.entryId) || entry.clueIds == null) continue;
            foreach (var clueId in entry.clueIds)
            {
                if (string.IsNullOrEmpty(clueId) || IsClueDiscovered(clueId)) continue;
                if (database.GetClue(clueId) == null) continue;
                AddClue(entry.entryId, clueId);
                return clueId;
            }
        }
        return null;
    }
}
