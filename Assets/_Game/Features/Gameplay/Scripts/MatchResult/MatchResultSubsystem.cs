using System;

public class MatchResultSubsystem : IMatchResultSubsystem {
    public IMatchResultModel Model { get; }
    public IMatchResultController Controller { get; }
    
    public MatchResultSubsystem(IMatchResultModel model, IMatchResultController controller) {
        Model = model;
        Controller = controller;
    }
    
    public void Dispose() {
        Model?.Dispose();
        Controller?.Dispose();
    }
}
