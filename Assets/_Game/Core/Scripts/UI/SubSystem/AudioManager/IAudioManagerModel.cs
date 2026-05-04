using UnityObservables;

public interface IAudioManagerModel : IModel
{
    Observable<float> MasterVolume { get; }
    Observable<float> MusicVolume { get; }
    Observable<float> SFXVolume { get; }
}
