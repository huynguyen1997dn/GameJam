# Day 1 Tutorial Setup

## Required Scene Objects

- `Managers` prefab includes `Day1TutorialController` for prefab-based map setup.
- `Day1TutorialBootstrap` also creates one controller at runtime if map managers exist and no controller is present.
- `OverviewMapRoot` contains `MapNode` objects for `Bridge` and `Darkwood`.
- `DaySystemUI` contains `DayHUD` and `ButtonNextDay`.
- `GamePlayView` contains `JournalMapButton` and `JournalUIView`.
- `MiniGameRoomBridge` maps `Darkwood` to `MiniGameType.CutTree`.

## Tutorial Target IDs

- Broken bridge node: `Bridge`
- Wood source node: `Darkwood`
- Journal button gate id: `JournalButton`
- Next Day button gate id: `NextDayButton`

## Runtime Flow

- The controller starts automatically on Day 1 when `DAY1_TUTORIAL_COMPLETED` is not set.
- The controller creates a dedicated high-sorting-order `TutorialCanvas` and fallback `Day1TutorialUIView` when no view reference is assigned.
- Bridge repair is automatic: tapping `Bridge` after `Darkwood` is solved runs the existing construct flow.
- There is no separate repair-confirm step; the tutorial continues after `MapNode.BuildSequence()` marks `Bridge` solved.
- The Journal entry unlocked by the tutorial is `day_01_bridge_repaired`.
- Completion is saved minimally with `PlayerPrefs` key `DAY1_TUTORIAL_COMPLETED`.

## Camera Focus

- While a map node is highlighted, the controller calls `MapCameraController.SetFocusTarget`, which bypasses the character-follow rule and glides the camera onto that node.
- Focus is released on every step change (and on tutorial end/skip); the camera then resumes following the character.
- Glide speed is `MapCameraController > Focus Smooth Time` (default `0.4`).

## Visual Tuning

- Dark overlay opacity is `Day1TutorialController > Dark Overlay Opacity`.
- Runtime fallback view also has `Day1TutorialUIView > Dark Overlay Opacity` and `Sorting Order`.
- Default opacity is `0.55`.
- Default tutorial canvas sorting order is `5000`.

## Existing Event Wiring Used

- `MapNode` dispatches `EventId.NodeClicked`.
- `MiniGameBase` dispatches `EventId.CompleteGame` and `EventId.FailGame`.
- `GameStateManager.OnNodeStateChanged` confirms `Bridge` repair.
- `JournalManager.OnJournalOpened` and `OnJournalClosed` drive the Journal tutorial step.
- `DayHUD` dispatches `EventId.NextDay`.
- `DayManager.OnDayChanged` starts the Day 2 reveal after the transition.

## Notes

- The fallback highlight intentionally uses the game jam version: a dark overlay plus a bright rectangular frame. Cutout masks, pulse, arrows, fades, and bounce animations are left as TODOs in `Day1TutorialUIView`.
- If the Journal page asset is renamed later, update `Day1TutorialController.bridgeJournalEntryId`.
