using UnityEngine;

public interface IAudioManagerController : IController
{
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
    void PlaySFX(AudioClip clip, float volumeScale = 1f);
    void PlayMusic(AudioClip clip, bool loop = true);
    void StopMusic();
}
