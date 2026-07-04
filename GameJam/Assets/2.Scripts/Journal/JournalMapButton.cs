using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Journal icon button on the map HUD, with its unread-content dot. Same
/// lifecycle as DayHUD: this object stays always-active and toggles the `root`
/// child, hiding the button while a room/minigame is open.
/// </summary>
public class JournalMapButton : MonoBehaviour
{
    [SerializeField] private GameObject root; // toggled child holding the visuals
    [SerializeField] private Button button;
    [SerializeField] private GameObject unreadIndicator;

    private bool _inRoom;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(OnClicked);
    }

    private void OnEnable()
    {
        var jm = JournalManager.Instance;
        if (jm != null) jm.OnUnreadStateChanged += HandleUnreadChanged;

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
        var jm = JournalManager.Instance;
        if (jm != null) jm.OnUnreadStateChanged -= HandleUnreadChanged;

        var vm = ViewManager.Instance;
        if (vm != null)
        {
            vm.OnRoomEnterRequested -= HandleRoomEntered;
            vm.OnRoomExited -= HandleRoomExited;
        }
    }

    private void HandleUnreadChanged(bool hasUnread) => Refresh();
    private void HandleRoomEntered(string nodeId) { _inRoom = true; Refresh(); }
    private void HandleRoomExited() { _inRoom = false; Refresh(); }

    private void Refresh()
    {
        if (root != null) root.SetActive(!_inRoom);

        var jm = JournalManager.Instance;
        if (unreadIndicator != null)
            unreadIndicator.SetActive(jm != null && jm.HasUnreadContent());
    }

    private void OnClicked()
    {
        // OpenJournal re-checks these, but bailing here keeps the button from even
        // reacting during a build animation or day transition.
        if (MapInputLock.IsLocked) return;
        JournalManager.Instance?.OpenJournal();
    }
}
