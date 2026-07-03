using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour
{
    
    public Button _button;
    void Start()
    {
        _button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        SoundManager.Instance.PlaySfx(SoundID.AUDIO_BTN);
    }

    private void OnValidate()
    {
        if (_button != null) return;
        _button = GetComponent<Button>();
    }
}
