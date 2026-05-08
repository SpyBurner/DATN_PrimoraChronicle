using System;

public interface IDrawPhaseSubsystem : IDisposable{
    IDrawPhaseModel Model { get; }
    IDrawPhaseController Controller { get; }
}
