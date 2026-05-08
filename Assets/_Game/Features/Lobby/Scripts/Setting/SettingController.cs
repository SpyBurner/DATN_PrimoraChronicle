using UnityEngine;
using Zenject;

internal class SettingController : ISettingController
{
    [Inject] private readonly ISettingModel _model;
    [Inject] private readonly IAudioManagerSubsystem _audioManager;

    public void Initialize() 
    { 
        _model.SetMasterVolume(PlayerPrefs.GetFloat("Setting_MasterVolume", 1f));
        _model.SetMusicVolume(PlayerPrefs.GetFloat("Setting_MusicVolume", 1f));
        _model.SetSFXVolume(PlayerPrefs.GetFloat("Setting_SFXVolume", 1f));
        
        ApplySettings();
    }
    
    public void Dispose() { }

    public void SetMasterVolume(float volume) => _model.SetMasterVolume(volume);
    public void SetMusicVolume(float volume) => _model.SetMusicVolume(volume);
    public void SetSFXVolume(float volume) => _model.SetSFXVolume(volume);

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("Setting_MasterVolume", _model.MasterVolume.Value);
        PlayerPrefs.SetFloat("Setting_MusicVolume", _model.MusicVolume.Value);
        PlayerPrefs.SetFloat("Setting_SFXVolume", _model.SFXVolume.Value);
        PlayerPrefs.Save();

        _audioManager.SetMasterVolume(_model.MasterVolume.Value);
        _audioManager.SetMusicVolume(_model.MusicVolume.Value);
        _audioManager.SetSFXVolume(_model.SFXVolume.Value);
        
        Debug.Log("Setting: Settings Applied and Saved");
    }
}
