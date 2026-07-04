using System;
using UnityEngine;

/// <summary>
/// Handles overview-map visibility and exposes the entry point room logic hooks into.
/// Fully decoupled from any room implementation: the map side raises
/// OnRoomEnterRequested(nodeId) and reacts to ExitRoom() being called from outside.
/// </summary>
public class ViewManager : Singleton<ViewManager>
{
    [SerializeField] private GameObject overviewMapRoot;
    [SerializeField] private GameObject backButton;

    public event Action<string> OnRoomEnterRequested; // fired with nodeId
    public event Action OnRoomExited; // fired on any return to the map, incl. the back button

    public void EnterRoom(string nodeId)
    {
        if (overviewMapRoot != null) overviewMapRoot.SetActive(false);
        if (backButton != null) backButton.SetActive(true);
        OnRoomEnterRequested?.Invoke(nodeId);
        // Does not activate any room GameObject itself — the room system
        // shows its own content in response to the event above.
    }

    public bool IsMapVisible() => overviewMapRoot != null && overviewMapRoot.activeSelf;

    public void ExitRoom()
    {
        if (overviewMapRoot != null) overviewMapRoot.SetActive(true);
        if (backButton != null) backButton.SetActive(false);
        // The room system hides its own content before/when calling this; the event
        // lets it also clean up when the exit came from the map's back button.
        OnRoomExited?.Invoke();
    }
}
