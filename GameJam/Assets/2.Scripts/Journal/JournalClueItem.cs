using TMPro;
using UnityEngine;

/// <summary>
/// One clue line inside the journal's clue box. Lives as an inactive template child
/// under the clue container; JournalUIView clones and fills it per discovered clue.
/// </summary>
public class JournalClueItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clueText;
    [SerializeField] private Color normalColor = new Color(0.25f, 0.2f, 0.15f);
    [SerializeField] private Color resolvedColor = new Color(0.25f, 0.2f, 0.15f, 0.45f);

    public void Setup(string text, bool resolved)
    {
        if (clueText == null) return;
        clueText.text = resolved ? $"<s>• {text}</s>" : $"• {text}";
        clueText.color = resolved ? resolvedColor : normalColor;
    }
}
