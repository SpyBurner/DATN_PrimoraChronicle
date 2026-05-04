using System;
using UnityEngine.Events;
using Zenject;

public class SettingSubsystem : ISettingSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly ISettingController _controller;
    [Inject] private readonly ISettingModel _model;

    public event UnityAction<float> MasterVolumeChanged;
    public event UnityAction<float> MusicVolumeChanged;
    public event UnityAction<float> SFXVolumeChanged;

    public void Initialize()
    {
        if (_model?.MasterVolume != null)
            _model.MasterVolume.OnChanged += HandleMasterVolumeChanged;
        
        if (_model?.MusicVolume != null)
            _model.MusicVolume.OnChanged += HandleMusicVolumeChanged;

        if (_model?.SFXVolume != null)
            _model.SFXVolume.OnChanged += HandleSFXVolumeChanged;
    }

    public void Dispose()
    {
        if (_model?.MasterVolume != null)
            _model.MasterVolume.OnChanged -= HandleMasterVolumeChanged;
        
        if (_model?.MusicVolume != null)
            _model.MusicVolume.OnChanged -= HandleMusicVolumeChanged;

        if (_model?.SFXVolume != null)
            _model.SFXVolume.OnChanged -= HandleSFXVolumeChanged;
    }

    public void SetMasterVolume(float volume) => _controller.SetMasterVolume(volume);
    public void SetMusicVolume(float volume) => _controller.SetMusicVolume(volume);
    public void SetSFXVolume(float volume) => _controller.SetSFXVolume(volume);
    public void ApplySettings() => _controller.ApplySettings();

    private void HandleMasterVolumeChanged()
    {
        try { MasterVolumeChanged?.Invoke(_model.MasterVolume.Value); } catch { }
    }

    private void HandleMusicVolumeChanged()
    {
        try { MusicVolumeChanged?.Invoke(_model.MusicVolume.Value); } catch { }
    }

    private void HandleSFXVolumeChanged()
    {
        try { SFXVolumeChanged?.Invoke(_model.SFXVolume.Value); } catch { }
    }
}
