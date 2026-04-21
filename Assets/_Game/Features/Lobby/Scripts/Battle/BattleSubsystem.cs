using System;
using Zenject;

public class BattleSubsystem : IBattleSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IBattleController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void StartMatchmaking() => _controller.StartMatchmaking();
}
