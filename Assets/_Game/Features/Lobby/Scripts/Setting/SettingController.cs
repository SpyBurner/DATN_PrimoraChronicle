using UnityEngine;
using Zenject;

internal class SettingController : ISettingController
{
    [Inject] private readonly ISettingModel _model;

    public void Initialize() { }

    public void ApplySettings()
    {
        Debug.Log("Setting: Apply Settings");
    }
}
