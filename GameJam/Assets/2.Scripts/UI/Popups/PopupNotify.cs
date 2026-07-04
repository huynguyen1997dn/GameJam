using TMPro;
using UnityEngine;

public class PopupNotify : PopupBase
{
    [SerializeField] private TextMeshProUGUI _messageText;

    public override void Show(params object[] args)
    {
        base.Show(args);
        if (args.Length > 0 && args[0] is string message && _messageText != null)
            _messageText.text = message;
    }
}
