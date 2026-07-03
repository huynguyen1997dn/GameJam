using UnityEngine;
using UnityEngine.UI;

public partial class ViewID
{
    // public static string MainMenuView = "MainMenuView";
}
public class MainMenuView : ViewBase
{
    [SerializeField] private Button _btnPlay;


    protected override void Awake()
    {
        base.Awake();
        _btnPlay?.onClick.AddListener(OnPlay);
    }

    private void OnPlay()
    {
        UIManager.Instance.OnShowView(ViewID.GamePlayView, MiniGameType.CutTree);
    }
}
