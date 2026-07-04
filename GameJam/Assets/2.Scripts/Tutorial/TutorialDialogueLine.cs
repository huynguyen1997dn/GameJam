using System;
using UnityEngine;

[Serializable]
public class TutorialDialogueLine
{
    public string SpeakerId;

    [TextArea(2, 4)]
    public string Text;

    public bool RequireTapToContinue = true;

    public TutorialDialogueLine(string speakerId, string text, bool requireTapToContinue = true)
    {
        SpeakerId = speakerId;
        Text = text;
        RequireTapToContinue = requireTapToContinue;
    }
}
