using UnityEngine;
using UnityObservables;

internal class AudioManagerModel : IAudioManagerModel
{
    private Observable<float> _masterVolume = new(1f);
    private Observable<float> _musicVolume = new(1f);
    private Observable<float> _sfxVolume = new(1f);

    public Observable<float> MasterVolume { get => _masterVolume; }
    public Observable<float> MusicVolume { get => _musicVolume; }
    public Observable<float> SFXVolume { get => _sfxVolume; }

    public void Initialize()
    {
    }

    public void Dispose()
    {
        _masterVolume.Value = 1f;
        _musicVolume.Value = 1f;
        _sfxVolume.Value = 1f;
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume.Value = Mathf.Clamp01(volume);
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume.Value = Mathf.Clamp01(volume);
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume.Value = Mathf.Clamp01(volume);
    }
}
