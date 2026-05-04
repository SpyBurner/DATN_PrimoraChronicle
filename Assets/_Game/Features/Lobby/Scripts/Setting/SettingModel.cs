using UnityObservables;

internal class SettingModel : ISettingModel
{
    private Observable<float> _masterVolume = new(1f);
    private Observable<float> _musicVolume = new(1f);
    private Observable<float> _sfxVolume = new(1f);

    public Observable<float> MasterVolume { get => _masterVolume; }
    public Observable<float> MusicVolume { get => _musicVolume; }
    public Observable<float> SFXVolume { get => _sfxVolume; }

    public void Initialize() { }
    
    public void Dispose() 
    { 
        _masterVolume.Value = 1f;
        _musicVolume.Value = 1f;
        _sfxVolume.Value = 1f;
    }

    internal void SetMasterVolume(float value) => _masterVolume.Value = value;
    internal void SetMusicVolume(float value) => _musicVolume.Value = value;
    internal void SetSFXVolume(float value) => _sfxVolume.Value = value;
}
