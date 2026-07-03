using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public static class SoundID
{
    public static string AUDIO_BTN = "AUDIO_BTN";
    public static string AUDIO_WRONG = "AUDIO_WRONG";
}

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private float _audioBGMVolumeDefault = 0.7f;

    [Header("BGM Source")]
    [SerializeField] private AudioSource _bgmAudioSource;
    
    [Header("SFX Source")]
    [SerializeField] private GameObject _sfxHolder;
    [SerializeField] private int audioSourceCount = 8;
    
    [Header("Sound Data")]
    [SerializeField] private List<SoundData> _soundsData;

    private Queue<AudioSource> _audioPool;
    
    private bool _isBgmMute;
    private bool _isSfxMute;
    private Tweener _bgmFadeTween;
    public bool IsBgmOn => !_isBgmMute;
    public bool IsSfxOn => !_isSfxMute;

    public Action OnSoundChange;

    protected override void Awake()
    {
        base.Awake();
        
        _audioPool = new Queue<AudioSource>();

        for (int i = 0; i < audioSourceCount; i++)
        {
            AudioSource src = _sfxHolder.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _audioPool.Enqueue(src);
        }
    }

    private void Start()
    {
        LoadSoundSetting();
    }

    public void ToggleSound(SoundType type)
    {
        if (type == SoundType.BGM)
        {
            _isBgmMute = !_isBgmMute;
            _bgmFadeTween?.Kill();
            _bgmAudioSource.volume = _isBgmMute ? 0f : _audioBGMVolumeDefault;
        }

        if (type == SoundType.SFX)
        {
            _isSfxMute = !_isSfxMute;
        }

        PlayerPrefs.SetInt("MUSIC_MUTE", _isBgmMute ? 0 : 1);
        PlayerPrefs.SetInt("SOUND_EFFECT_MUTE", _isSfxMute ? 0 : 1);
    }

    public void PlaySfx(string soundId)
    {
        if (_isSfxMute) return;

        var clip = _soundsData.FirstOrDefault(x => x.SoundId == soundId)?.SoundClip;
        if (!clip)
        {
            Debug.LogWarning($"Missing sound: {soundId}");
            return;
        }

        AudioSource src = GetSourceFromPool();
        if (src.clip != clip)
        {
            src.clip = clip;
        }
        src.pitch = 1;

        src.Play();
        
        StartCoroutine(ReturnAfterPlaying(src, clip.length));
    }
    
    public void PlaySfx(string soundId,float pitch,float maxPitch = 3)
    {
        if (_isSfxMute) return;

        var clip = _soundsData.FirstOrDefault(x => x.SoundId == soundId)?.SoundClip;
        if (!clip)
        {
            Debug.LogWarning($"Missing sound: {soundId}");
            return;
        }

        AudioSource src = GetSourceFromPool();
        if (src.clip != clip)
        {
            src.clip = clip;
        }
        src.pitch = Mathf.Clamp(pitch, 0, maxPitch);
        src.Play();
        StartCoroutine(ReturnAfterPlaying(src, clip.length));
    }

    public void PlayBgm(string soundId)
    {
        if (_isSfxMute)
        {
            return;
        }
        
        _bgmAudioSource.loop = true;
        _bgmAudioSource.clip = _soundsData.FirstOrDefault(x => x.SoundId == soundId)?.SoundClip;
        _bgmAudioSource.Play();
    }

    public void PlayBgmWithFadeIn(string soundId, float fadeDuration = 1f)
    {
        if (_isBgmMute)
        {
            _bgmAudioSource.volume = 0f;
            return;
        }

        var clip = _soundsData.FirstOrDefault(x => x.SoundId == soundId)?.SoundClip;
        if (clip == null) return;

        _bgmFadeTween?.Kill();

        if (_bgmAudioSource.isPlaying && _bgmAudioSource.clip == clip)
        {
            _bgmFadeTween = _bgmAudioSource.DOFade(_audioBGMVolumeDefault, fadeDuration)
                .SetEase(Ease.InOutCubic);
            return;
        }

        _bgmAudioSource.loop = true;
        _bgmAudioSource.clip = clip;
        _bgmAudioSource.volume = 0f;
        _bgmAudioSource.Play();

        _bgmFadeTween = _bgmAudioSource.DOFade(_audioBGMVolumeDefault, fadeDuration)
            .SetEase(Ease.InOutCubic);
    }

    public void StopBgmWithFadeOut(float fadeDuration = 1f)
    {
        _bgmFadeTween?.Kill();

        _bgmFadeTween = _bgmAudioSource.DOFade(0f, fadeDuration)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() =>
            {
                _bgmAudioSource.Stop();
                _bgmAudioSource.volume = _isBgmMute ? 0f : _audioBGMVolumeDefault;
            });
    }

    public void TriggerOnChangeSound()
    {
        OnSoundChange?.Invoke();
    }

    private void LoadSoundSetting()
    {
        _isBgmMute = PlayerPrefs.GetInt("MUSIC_MUTE", 1) == 0 ? true : false;
        _bgmFadeTween?.Kill();
        _bgmAudioSource.volume = _isBgmMute ? 0f : _audioBGMVolumeDefault;
        _isSfxMute = PlayerPrefs.GetInt("SOUND_EFFECT_MUTE", 1) == 0 ? true : false;
        TriggerOnChangeSound();
    }
    
    private AudioSource GetSourceFromPool()
    {
        if (_audioPool.Count == 0)
        {
            AudioSource src = _sfxHolder.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _audioPool.Enqueue(src);
        }

        return _audioPool.Dequeue();
    }

    private IEnumerator ReturnAfterPlaying(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(src);
    }

    private void ReturnToPool(AudioSource src)
    {
        src.Stop();
        src.clip = null;
        src.loop = false;
        _audioPool.Enqueue(src);
    }
}

[Serializable]
public class SoundData
{
    public string SoundId;
    public SoundType SoundType;
    public AudioClip SoundClip;
}

public enum SoundType
{
    BGM,
    SFX
}