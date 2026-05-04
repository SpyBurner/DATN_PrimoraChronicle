using System;

public interface IMatchResultSubsystem : IDisposable{
    IMatchResultModel Model { get; }
    IMatchResultController Controller { get; }
}
