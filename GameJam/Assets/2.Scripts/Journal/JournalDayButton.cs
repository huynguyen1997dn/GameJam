using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One "D3"-style button in the journal's collapsible day list. Template child
/// cloned by JournalUIView. Locked days stay clickable on purpose so tapping them
/// can show the "not written yet" message instead of dead-buttoning.
/// </summary>
public class JournalDayButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject unreadDot;
    [SerializeField] private Color normalLabelColor = new Color(0.25f, 0.2f, 0.15f);
    [SerializeField] private Color lockedLabelColor = new Color(0.25f, 0.2f, 0.15f, 0.35f);

    private Action _onClicked;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(() => _onClicked?.Invoke());
    }

    public void Setup(int dayNumber, bool locked, bool selected, bool unread, Action onClicked)
    {
        _onClicked = onClicked;
        if (label != null)
        {
            label.text = $"D{dayNumber}";
            label.color = locked ? lockedLabelColor : normalLabelColor;
        }
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
        if (lockIcon != null) lockIcon.SetActive(locked);
        if (unreadDot != null) unreadDot.SetActive(unread && !locked);
    }
}
