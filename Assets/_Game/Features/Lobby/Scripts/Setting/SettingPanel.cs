using UnityEngine;
using UnityEngine.UI;
using Zenject;
using System.Linq;

public class SettingPanel : UIPanel
{
    [Inject] private readonly ISettingSubsystem _setting;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button applyButton;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[SettingPanel] Awake. Initial state: masterSlider={masterSlider != null}, musicSlider={musicSlider != null}, sfxSlider={sfxSlider != null}");
        
        // Fallback: try to find by name if not assigned in inspector (prefabs might be missing references)
        if (masterSlider == null) masterSlider = transform.GetComponentsInChildren<Slider>(true).FirstOrDefault(s => s.name.Contains("Master") || s.name.Contains("master"));
        if (musicSlider == null) musicSlider = transform.GetComponentsInChildren<Slider>(true).FirstOrDefault(s => s.name.Contains("Music") || s.name.Contains("music"));
        if (sfxSlider == null) sfxSlider = transform.GetComponentsInChildren<Slider>(true).FirstOrDefault(s => s.name.Contains("SFX") || s.name.Contains("sfx"));

        Debug.Log($"[SettingPanel] After Fallback: masterSlider={masterSlider != null}, musicSlider={musicSlider != null}, sfxSlider={sfxSlider != null}");

        // Initial values - use the subsystem's current values
        if (masterSlider != null) masterSlider.value = _setting.MasterVolume;
        if (musicSlider != null) musicSlider.value = _setting.MusicVolume;
        if (sfxSlider != null) sfxSlider.value = _setting.SFXVolume;
        
        Debug.Log("[SettingPanel] Awake complete.");
    }

    protected override void OnEnable()
    {
        Debug.Log("[SettingPanel] OnEnable called.");
        base.OnEnable();
        
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApply);
        }
    }

    protected override void OnDisable()
    {
        Debug.Log("[SettingPanel] OnDisable called.");
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        if (applyButton != null) applyButton.onClick.RemoveListener(OnApply);
        
        base.OnDisable();
    }

    private void OnMasterVolumeChanged(float value)
    {
        Debug.Log($"[SettingPanel] Master Volume changed to {value}");
        _setting.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        Debug.Log($"[SettingPanel] Music Volume changed to {value}");
        _setting.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        Debug.Log($"[SettingPanel] SFX Volume changed to {value}");
        _setting.SetSFXVolume(value);
    }

    private void OnApply()
    {
        Debug.Log("[SettingPanel] Applying Settings");
        _setting.ApplySettings();
    }
}
