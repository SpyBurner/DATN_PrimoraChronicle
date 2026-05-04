using System;

public interface IHandSubsystem : IDisposable{
    IHandModel Model { get; }
    IHandController Controller { get; }
}
