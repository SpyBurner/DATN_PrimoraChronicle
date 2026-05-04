using System;

public class FusePhaseSubsystem : IFusePhaseSubsystem {
    public IFusePhaseModel Model { get; }
    public IFusePhaseController Controller { get; }
    
    public FusePhaseSubsystem(IFusePhaseModel model, IFusePhaseController controller) {
        Model = model;
        Controller = controller;
    }
    
    public void Dispose() {
        Model?.Dispose();
        Controller?.Dispose();
    }
}
