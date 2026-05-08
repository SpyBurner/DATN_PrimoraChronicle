public interface ISettingController : IController
{
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
    void ApplySettings();
}
