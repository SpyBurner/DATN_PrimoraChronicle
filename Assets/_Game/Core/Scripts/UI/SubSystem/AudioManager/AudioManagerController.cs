using UnityEngine;
using Zenject;

internal class AudioManagerController : IAudioManagerController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly AudioManagerModel _model;

    private AudioSource _musicSource;

    public void Initialize()
    {
        LoadVolumeSettings();
        CreateAudioSources();
    }

    public void Dispose()
    {
        SaveVolumeSettings();
        if (_musicSource != null)
        {
            Object.Destroy(_musicSource.gameObject);
        }
    }

    public void SetMasterVolume(float volume)
    {
        _debugLogger.Log($"AudioManager: Setting master volume to {volume}");
        _model.SetMasterVolume(volume);
        SaveVolumeSettings();
        AudioListener.volume = _model.MasterVolume.Value;
    }

    public void SetMusicVolume(float volume)
    {
        _debugLogger.Log($"AudioManager: Setting music volume to {volume}");
        _model.SetMusicVolume(volume);
        SaveVolumeSettings();
        if (_musicSource != null)
        {
            _musicSource.volume = _model.MusicVolume.Value;
        }
    }

    public void SetSFXVolume(float volume)
    {
        _debugLogger.Log($"AudioManager: Setting SFX volume to {volume}");
        _model.SetSFXVolume(volume);
        SaveVolumeSettings();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        _debugLogger.Log($"AudioManager: Playing SFX {clip.name}");
        AudioSource.PlayClipAtPoint(clip, Vector3.zero, _model.MasterVolume.Value * _model.SFXVolume.Value * volumeScale);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        _debugLogger.Log($"AudioManager: Playing music {clip.name}");
        if (_musicSource == null)
        {
            CreateAudioSources();
        }

        _musicSource.clip = clip;
        _musicSource.loop = loop;
        _musicSource.volume = _model.MasterVolume.Value * _model.MusicVolume.Value;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        _debugLogger.Log("AudioManager: Stopping music");
        if (_musicSource != null)
        {
            _musicSource.Stop();
        }
    }

    private void CreateAudioSources()
    {
        if (_musicSource == null)
        {
            GameObject musicSourceGO = new GameObject("MusicSource");
            _musicSource = musicSourceGO.AddComponent<AudioSource>();
            _musicSource.volume = _model.MasterVolume.Value * _model.MusicVolume.Value;
            Object.DontDestroyOnLoad(musicSourceGO);
        }
    }

    private void LoadVolumeSettings()
    {
        float master = PlayerPrefs.GetFloat("AudioManager_MasterVolume", 1f);
        float music = PlayerPrefs.GetFloat("AudioManager_MusicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("AudioManager_SFXVolume", 1f);

        _model.SetMasterVolume(master);
        _model.SetMusicVolume(music);
        _model.SetSFXVolume(sfx);
        AudioListener.volume = master;
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("AudioManager_MasterVolume", _model.MasterVolume.Value);
        PlayerPrefs.SetFloat("AudioManager_MusicVolume", _model.MusicVolume.Value);
        PlayerPrefs.SetFloat("AudioManager_SFXVolume", _model.SFXVolume.Value);
        PlayerPrefs.Save();
    }
}
