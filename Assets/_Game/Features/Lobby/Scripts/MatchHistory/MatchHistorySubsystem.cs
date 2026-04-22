using System;
using Zenject;

public class MatchHistorySubsystem : IMatchHistorySubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IMatchHistoryController _controller;

    public void Initialize() { }
    public void Dispose() { }
}
