using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogEntryItem : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public void Setup(Sprite icon, string displayName, string description, Color nameColor)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
        }

        if (_nameText != null)
        {
            _nameText.text = displayName;
            _nameText.color = nameColor;
        }

        if (_descriptionText != null)
            _descriptionText.text = description;
    }

    public void SetAlpha(float alpha)
    {
        var group = GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = alpha;
    }
}
