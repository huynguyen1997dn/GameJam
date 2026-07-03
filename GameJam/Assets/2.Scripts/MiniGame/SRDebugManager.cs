using SRDebugger;
using UnityEngine;

public class SRDebugManager : Singleton<SRDebugManager>
{
    [SerializeField] private bool _autoDisableInRelease = true;

    protected override void Awake()
    {
        base.Awake();
        if (_autoDisableInRelease && !Debug.isDebugBuild && !Application.isEditor)
        {
            SetTriggerEnabled(false);
        }
    }

    public bool IsDebugVisible => SRDebug.Instance != null && SRDebug.Instance.IsDebugPanelVisible;

    public bool IsTriggerEnabled
    {
        get => SRDebug.Instance != null && SRDebug.Instance.IsTriggerEnabled;
        set => SetTriggerEnabled(value);
    }

    public void ShowDebugPanel()
    {
        if (SRDebug.Instance != null)
            SRDebug.Instance.ShowDebugPanel(false);
    }

    public void HideDebugPanel()
    {
        if (SRDebug.Instance != null)
            SRDebug.Instance.HideDebugPanel();
    }

    public void ToggleDebugPanel()
    {
        if (SRDebug.Instance == null) return;
        if (SRDebug.Instance.IsDebugPanelVisible)
            SRDebug.Instance.HideDebugPanel();
        else
            SRDebug.Instance.ShowDebugPanel(false);
    }

    private void SetTriggerEnabled(bool enabled)
    {
        if (SRDebug.Instance != null)
            SRDebug.Instance.IsTriggerEnabled = enabled;
    }
}
