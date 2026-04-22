using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SettingPanel : UIPanel
{
    [Inject] private readonly ISettingSubsystem _setting;


    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    private void OnApply() => _setting.ApplySettings();
}
