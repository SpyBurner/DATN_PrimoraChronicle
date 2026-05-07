using UnityObservables;

public interface IAudioManagerModel : IModel
{
    Observable<float> MasterVolume { get; }
    Observable<float> MusicVolume { get; }
    Observable<float> SFXVolume { get; }

    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
}
