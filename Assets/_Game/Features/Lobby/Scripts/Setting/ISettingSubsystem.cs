using UnityEngine.Events;

public interface ISettingSubsystem : ISubsystem
{
    event UnityAction<float> MasterVolumeChanged;
    event UnityAction<float> MusicVolumeChanged;
    event UnityAction<float> SFXVolumeChanged;

    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
    void ApplySettings();
}
