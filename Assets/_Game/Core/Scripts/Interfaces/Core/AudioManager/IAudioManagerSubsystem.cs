using UnityEngine;
using UnityEngine.Events;

public interface IAudioManagerSubsystem : ISubsystem
{
    event UnityAction<float> MasterVolumeChanged;
    event UnityAction<float> MusicVolumeChanged;
    event UnityAction<float> SFXVolumeChanged;

    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
    void PlaySFX(AudioClip clip, float volumeScale = 1f);
    void PlayMusic(AudioClip clip, bool loop = true);
    void StopMusic();
}
