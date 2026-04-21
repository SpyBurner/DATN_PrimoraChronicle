using System;
using Zenject;

public class ProfileSubsystem : IProfileSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IProfileController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void NavigateToMatchHistory() => _controller.NavigateToMatchHistory();
}
