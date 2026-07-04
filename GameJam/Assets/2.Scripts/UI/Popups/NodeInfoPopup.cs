using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class PopupId
{
    public const string NodeInfoPopup = "NodeInfoPopup";
}

public class NodeInfoPopup : PopupBase
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Transform _statContainer;
    [SerializeField] private TextMeshProUGUI _statRowTemplate;

    private readonly List<TextMeshProUGUI> _rows = new();

    protected override void Awake()
    {
        base.Awake();
        if (_statRowTemplate != null)
            _statRowTemplate.gameObject.SetActive(false);
    }

    public override void Show(params object[] args)
    {
        base.Show(args);

        ClearRows();

        if (args.Length < 4) return;

        var icon = args[0] as Sprite;
        var title = args[1] as string;
        int currentDay = (int)args[2];
        var statInfos = args[3] as List<StatInfo>;

        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
        }

        if (_titleText != null)
            _titleText.text = title;

        if (_statContainer == null || _statRowTemplate == null || statInfos == null) return;

        foreach (var stat in statInfos)
        {
            int value = stat.baseValue + (currentDay - 1) * stat.dailyIncrement;
            var row = Instantiate(_statRowTemplate, _statContainer);
            row.gameObject.SetActive(true);
            row.text = stat.dailyIncrement > 0
                ? $"{stat.label}: {value} (+{stat.dailyIncrement}/ngày)"
                : $"{stat.label}: {value}";
            _rows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _rows.Clear();
    }
}
