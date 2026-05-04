using System;

public interface IBoardSubsystem : IDisposable{
    IBoardModel Model { get; }
    IBoardController Controller { get; }
}
