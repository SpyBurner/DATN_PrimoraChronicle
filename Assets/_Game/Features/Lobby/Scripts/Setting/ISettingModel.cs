using UnityObservables;

public interface ISettingModel : IModel
{
    Observable<float> MasterVolume { get; }
    Observable<float> MusicVolume { get; }
    Observable<float> SFXVolume { get; }
}
