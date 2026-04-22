using System;
using Zenject;

public class SettingSubsystem : ISettingSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly ISettingController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void ApplySettings() => _controller.ApplySettings();
}
