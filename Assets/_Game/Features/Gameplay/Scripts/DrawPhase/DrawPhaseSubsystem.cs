using System;

public class DrawPhaseSubsystem : IDrawPhaseSubsystem {
    public IDrawPhaseModel Model { get; }
    public IDrawPhaseController Controller { get; }
    
    public DrawPhaseSubsystem(IDrawPhaseModel model, IDrawPhaseController controller) {
        Model = model;
        Controller = controller;
    }
    
    public void Dispose() {
        Model?.Dispose();
        Controller?.Dispose();
    }
}
