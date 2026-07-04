using UnityEngine;

public partial class EventId
{
    public const string NodeClicked = "NodeClicked";
}

public class GamePlayView : ViewBase
{
    [SerializeField] private CutTreeConfig _cutTreeConfig;

    private void OnEnable()
    {
        EventDispatcher.Subscribe<string>(EventId.NodeClicked, HandleNodeClicked);
    }

    private void OnDisable()
    {
        EventDispatcher.Unsubscribe<string>(EventId.NodeClicked, HandleNodeClicked);
    }

    private void HandleNodeClicked(string nodeId)
    {
        if (nodeId != "Bridge") return;
        if (UIManager.Instance.ActivePopups.Count > 1) return;

        UIManager.Instance.ShowNoti($"You need to collect {_cutTreeConfig.targetTreesToCut} woods");
    }
}
