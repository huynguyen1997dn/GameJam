# Journal System — Setup & Wiring Notes

The Journal is the player's persistent memory tool: an overlay popup on top of the
map (no scene change), showing per-day entries with title, illustration, body text,
clues and a proof note. NPCs forget; the journal remembers.

## Quick Start (recommended path)

1. Open the map scene (the one containing `DayHUD` / the map canvas).
2. Menu: **GameJam → Journal → 1. Create Sample Journal Data**
   Creates `JournalDatabase` + 5 sample entries (D0 prologue … D4) + 9 clues under
   `Assets/8.Resources/Resources/Journal/`.
3. Select the map UI Canvas in the Hierarchy (optional — the tool falls back to the
   canvas holding `DayHUD`), then: **GameJam → Journal → 2. Build Journal UI In Scene**
   Creates `JournalSystem`, `JournalButton` and `JournalOverlay` fully wired.
4. Press Play. Tap the `J` button → journal opens on the **D0 prologue**, the only
   page written at the start. Finishing a day writes that day's page: advancing to
   Day 2 unlocks the D1 entry (with an unread dot), and so on.
   Cheats: **J** unlocks the next entry, **K** discovers the next clue
   (`JournalDebugCheats`, on the `JournalSystem` object — remove for release).

Everything the tool generates is placeholder styling (built-in sprites, TMP default
font) positioned for the 1125×2436 portrait canvas — restyle freely; only keep the
serialized references intact.

## 1. UI Hierarchy (generated)

```text
MapCanvas
 ├── ... existing map HUD (DayHUD, buttons, ...)
 ├── JournalButton                  (JournalMapButton)
 │    └── Root                      ← toggled off while a room is open
 │         ├── Bg                   (Image + Button)
 │         │    └── Icon            ("J" placeholder icon)
 │         └── UnreadDot            (red dot, off by default)
 ├── JournalOverlay                 (JournalUIView + CanvasGroup, always active)
 │    └── Root                      ← toggled child; inactive at rest
 │         ├── Dim                  (full-screen Image + Button — tap outside)
 │         ├── Panel                (parchment sheet, ~93% × ~91% of screen)
 │         │    ├── Header
 │         │    │    ├── DayListButton   ("=")
 │         │    │    ├── TitleText       ("JOURNAL")
 │         │    │    └── CloseButton     ("×")
 │         │    ├── ContentRoot
 │         │    │    ├── EntryTitle      ("Day 3 — The Bridge Was Repaired")
 │         │    │    └── ContentScroll   (ScrollRect)
 │         │    │         └── Viewport → Content (vertical layout)
 │         │    │              ├── Illustration (image or placeholder)
 │         │    │              ├── BodyText
 │         │    │              ├── ClueSection (header, empty text, ClueContainer)
 │         │    │              │    └── ClueItemTemplate (inactive, cloned per clue)
 │         │    │              └── ProofSection (header + proof text)
 │         │    ├── EmptyState          ("No journal entry yet." — inactive)
 │         │    ├── FooterNavigation    ([<]  D3  [>])
 │         │    └── LockedToast         ("This page has not been written yet.")
 │         └── DayListSidebar          (collapsible, inactive by default)
 │              ├── SidebarHeader      ("DAYS")
 │              └── DayButtonContainer
 │                   └── DayButtonTemplate (inactive, cloned per entry)
 └── DayTransitionOverlay           ← kept as LAST sibling (renders above journal)

JournalSystem (scene root object)
 ├── JournalManager                 (database assigned)
 └── JournalDebugCheats             (dev only)
```

## 2. Required Serialized References

Assigned automatically by the setup tool; listed here in case of manual rebuilds.

```text
JournalManager
- database              → JournalDatabase asset (or leave empty: falls back to
                          Resources.Load("Journal/JournalDatabase"))
- autoUnlockEntriesByDay→ on by default: advancing to a new day writes the pages of
                          every FINISHED day (dayNumber < new day) — reaching Day 2
                          unlocks D1, never D2 itself. Turn off if quests/puzzles
                          should be the only writers.

JournalMapButton
- root, button, unreadIndicator

JournalUIView
- root, canvasGroup
- closeButton, dayListButton, dimButton
- contentRoot, entryTitleText, illustrationImage, illustrationPlaceholder,
  bodyText, clueSection, clueEmptyText, clueContainer, clueItemTemplate,
  proofSection, proofText, contentScroll
- emptyStateRoot, emptyStateText
- previousButton, nextButton, footerDayLabel
- sidebarRoot, dayButtonContainer, dayButtonTemplate
- lockedToast, lockedToastText

JournalDayButton (template)
- button, label, selectedHighlight, lockIcon, unreadDot

JournalClueItem (template)
- clueText
```

## 3. Button Wiring

All listeners are added in code (`Awake`) — no `onClick` entries to set in the
Inspector:

```text
JournalButton (Bg)     → JournalManager.OpenJournal()
CloseButton            → JournalManager.CloseJournal()
Dim (tap outside)      → closes sidebar if open, else CloseJournal()
DayListButton          → toggles DayListSidebar
PreviousButton         → JournalManager.SelectPreviousUnlockedEntry()
NextButton             → JournalManager.SelectNextUnlockedEntry()
DayButton (clone)      → JournalManager.SelectEntry(entryId)
DayButton (locked)     → shows the LockedToast instead
```

## 4. Event Wiring / Integration Points

Inputs — call these from quest / puzzle / dialogue / world systems:

```csharp
JournalManager.Instance.UnlockEntry("day_03_bridge_repaired");
JournalManager.Instance.AddClue("day_03_bridge_repaired", "clue_old_carpenter");
JournalManager.Instance.SetClueDiscovered("clue_old_carpenter"); // entry inferred
JournalManager.Instance.MarkClueResolved("clue_old_carpenter");  // optional
JournalManager.Instance.AddProofNote("day_03_bridge_repaired", "The village has changed.");
JournalManager.Instance.OpenJournal("day_03_bridge_repaired");   // open on a specific page
```

Outputs — C# events on `JournalManager` (same style as DayManager/ViewManager):

```text
OnJournalOpened / OnJournalClosed      → map interaction gate (see below)
OnEntryUnlocked(entryId)
OnClueAdded(entryId, clueId)
OnEntrySelected(entryId)               → also fired when an entry is read
OnUnreadStateChanged(hasUnread)        → drives the button dot
OnContentChanged                       → view refresh signal
```

Existing systems it already hooks into:

```text
DayManager.OnDayChanged   → auto-writes finished days' pages (autoUnlockEntriesByDay,
                            on by default; dayNumber < new day)
MapInputLock              → held Lock()ed while the journal is open, so map nodes
                            and camera pan/zoom freeze; the Dim image swallows UI
                            clicks (NextDay/Advance stay visible but dead)
ViewManager room events   → JournalButton hides while a room/minigame is open
DayHUD-style guard        → journal refuses to open during a build animation,
                            day transition, or while the character is walking
```

Save/load (external save system is out of scope; these are the hook points):

```csharp
JournalSaveData data = JournalManager.Instance.GetSaveData();   // on save
JournalManager.Instance.LoadSaveData(data);                     // on load
// e.g. SaveSystem.RegisterProvider("Journal", JournalManager.Instance);
```

Persisted: unlocked entries, discovered clues, resolved clues, read/unread state,
last selected entry. `JournalSaveData` is JsonUtility-friendly.

## 5. Data Setup

To add a new journal entry:

1. `Create → Journal → Journal Entry` (or copy one in
   `8.Resources/Resources/Journal/Entries/`).
2. Set `entryId` (unique, e.g. `day_05_harvest`), `dayNumber`, `title`, `bodyText`.
   `dayNumber` 0 is the prologue slot; a day-N page unlocks when Day N is finished
   (i.e. when the day advances to N+1).
3. Optionally assign `illustration` (placeholder frame shows when empty) and
   `proofText`.
4. List its `clueIds`.
5. Tick `unlockedFromStart` only for pages readable before anything is played
   (the D0 prologue).
6. Add the asset to `JournalDatabase → entries`.

To add a new clue:

1. `Create → Journal → Journal Clue`.
2. Set `clueId` (unique), `entryId` (the owning entry), `text` (short, one line).
3. Add the asset to `JournalDatabase → clues` and its id to the entry's `clueIds`.

Clues start hidden; they appear once `AddClue`/`SetClueDiscovered` is called.

## 6. Testing Checklist

```text
- Open Journal from map (J button)              → overlay over dimmed map; only D0 at start
- Advance to Day 2 (finish objectives)          → D1 entry written + unread dot
- Close (× / tap outside)                       → map interaction returns
- Map nodes + camera frozen while open          → MapInputLock held
- NextDay/Advance not clickable behind the dim
- Press J cheat (unlock entry)                  → red dot on Journal button
- Open the new entry                            → dot clears
- Press K cheat (discover clue)                 → clue line appears / dot if closed
- Prev/Next navigate unlocked entries only; arrows disable at the ends
- Day list (= button)                           → sidebar; locked days dimmed
- Tap a locked day                              → "This page has not been written yet."
- Select a day                                  → sidebar closes, page changes
- Entry with no illustration                    → placeholder frame
- Entry with clues defined but none discovered  → "No clues recorded."
- No entries unlocked at all                    → "No journal entry yet." empty state
- GetSaveData → LoadSaveData round trip         → unlocked/discovered/read state restored
- Journal button hidden while inside a room/minigame
- Journal refuses to open mid-walk / during build / during day transition
```
