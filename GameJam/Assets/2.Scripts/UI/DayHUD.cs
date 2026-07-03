using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Map-screen HUD: current-day label plus the "Advance to Next Day" button, which
/// appears once the day's objective is complete (all DayManager required nodes
/// solved) and dispatches EventId.NextDay — the same path NextDayPopup uses, handled
/// by DayTransitionOverlay. Pull-based: every event just re-reads the state.
/// This object stays always-active (subscriptions live here); it toggles the `root`
/// child, hiding the HUD while a room/minigame is open.
/// </summary>
public class DayHUD : MonoBehaviour
{
    [SerializeField] private GameObject root; // toggled child holding the visuals
    [SerializeField] private TextMeshProUGUI dayLabel;
    [SerializeField] private Button advanceButton;

    private bool _inRoom;

    private void Awake()
    {
        if (advanceButton != null) advanceButton.onClick.AddListener(OnAdvanceClicked);
    }

    private void OnEnable()
    {
        var dm = DayManager.Instance;
        if (dm != null)
        {
            dm.OnDayChanged += HandleDayChanged;
            dm.OnObjectiveStateChanged += Refresh;
        }

        var vm = ViewManager.Instance;
        if (vm != null)
        {
            vm.OnRoomEnterRequested += HandleRoomEntered;
            vm.OnRoomExited += HandleRoomExited;
        }

        Refresh();
    }

    private void OnDisable()
    {
        var dm = DayManager.Instance;
        if (dm != null)
        {
            dm.OnDayChanged -= HandleDayChanged;
            dm.OnObjectiveStateChanged -= Refresh;
        }

        var vm = ViewManager.Instance;
        if (vm != null)
        {
            vm.OnRoomEnterRequested -= HandleRoomEntered;
            vm.OnRoomExited -= HandleRoomExited;
        }
    }

    private void HandleDayChanged(int newDay) => Refresh();
    private void HandleRoomEntered(string nodeId) { _inRoom = true; Refresh(); }
    private void HandleRoomExited() { _inRoom = false; Refresh(); }

    private void Refresh()
    {
        if (root != null) root.SetActive(!_inRoom);

        var dm = DayManager.Instance;
        if (dm == null) return;

        if (dayLabel != null) dayLabel.text = $"Day {dm.CurrentDay}";
        if (advanceButton != null) advanceButton.gameObject.SetActive(dm.CanAdvance);
    }

    private void OnAdvanceClicked()
    {
        // The lock is a plain bool (not nestable): advancing during a build would let
        // the build's Unlock unfreeze the map mid-transition. Mid-hop is blocked too —
        // the pending arrival callback could open a room while the screen is dark.
        if (MapInputLock.IsLocked) return;
        var mover = CharacterMapMover.Instance;
        if (mover != null && mover.IsMoving) return;

        if (advanceButton != null) advanceButton.gameObject.SetActive(false);
        EventDispatcher.Dispatch(EventId.NextDay);
    }
}
