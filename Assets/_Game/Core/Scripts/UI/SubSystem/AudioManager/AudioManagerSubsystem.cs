using System;
using UnityEngine;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class AudioManagerSubsystem : IAudioManagerSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IAudioManagerController _controller;
    [Inject] private readonly IAudioManagerModel _model;

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

    public void PlaySFX(AudioClip clip, float volumeScale = 1f) => _controller.PlaySFX(clip, volumeScale);

    public void PlayMusic(AudioClip clip, bool loop = true) => _controller.PlayMusic(clip, loop);

    public void StopMusic() => _controller.StopMusic();

    private void HandleMasterVolumeChanged()
    {
        try
        {
            MasterVolumeChanged?.Invoke(_model.MasterVolume.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandleMusicVolumeChanged()
    {
        try
        {
            MusicVolumeChanged?.Invoke(_model.MusicVolume.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandleSFXVolumeChanged()
    {
        try
        {
            SFXVolumeChanged?.Invoke(_model.SFXVolume.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }
}
