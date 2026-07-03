using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource _sfxAudioSource;

    protected override void Awake()
    {
        base.Awake();
        if (!_sfxAudioSource)
        {
            _sfxAudioSource = gameObject.GetComponent<AudioSource>();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxAudioSource == null)
            return;

        _sfxAudioSource.PlayOneShot(clip);
    }
}
