using System;

public interface IFusePhaseSubsystem : IDisposable{
    IFusePhaseModel Model { get; }
    IFusePhaseController Controller { get; }
}
