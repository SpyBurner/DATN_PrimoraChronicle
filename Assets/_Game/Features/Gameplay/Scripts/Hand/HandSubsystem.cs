using System;

public class HandSubsystem : IHandSubsystem {
    public IHandModel Model { get; }
    public IHandController Controller { get; }
    
    public HandSubsystem(IHandModel model, IHandController controller) {
        Model = model;
        Controller = controller;
    }
    
    public void Dispose() {
        Model?.Dispose();
        Controller?.Dispose();
    }
}
