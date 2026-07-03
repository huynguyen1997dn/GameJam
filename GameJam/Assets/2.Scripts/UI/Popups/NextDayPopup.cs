using UnityEngine;
using UnityEngine.UI;

public partial class PopupId
{
    public const string NextDayPopup = "NextDayPopup";
}
public partial class EventId
{
    public const string NextDay = "NextDay";
}
public class NextDayPopup : PopupBase
{
    public override void OnPopupClose()
    {
        base.OnPopupClose();
        EventDispatcher.Dispatch(EventId.NextDay);
    }
}
